#!/usr/bin/env python3
"""Validate CI coverage gates from Cobertura XML files.

The blocking Tier 1 + Tier 2 lanes each emit a separate ``coverage.cobertura.xml`` that
includes the full dependency closure (submodule assemblies and every other assembly that
happened to load during that test run). Summing covered/valid counts naively across those
files is meaningless: the same assembly is counted once per report, so a project that is
fully covered by its own test lane is drowned out by the unexercised closure in the other
lanes. This script therefore *merges by union* and *scopes* the gates to Hexalith.Tenants
production code so the thresholds reflect reality:

  * Overall line coverage gate: union of covered lines across all reports, scoped to the
    five publishable package projects under ``src/`` (the platform deliverables). Must be
    strictly greater than ``--minimum-line-coverage``.
  * Isolation/auth branch gate: union (best-per-line) of covered branch conditions for the
    named tenant isolation/authorization production files. Must be at least
    ``--required-branch-coverage`` (100%).

Branch data is read from per-line ``condition-coverage="X% (covered/total)"`` markers. The
coverlet collector emits ``branch="True"`` (capitalized), so the branch flag is matched
case-insensitively — a previous version compared against ``"true"`` and silently found no
branch data at all.
"""

from __future__ import annotations

import argparse
import os
import re
import sys
from pathlib import Path
from xml.etree import ElementTree


# Line-coverage gate scope: the five publishable NuGet package projects (the actual platform
# deliverables). Host (src/Hexalith.Tenants), AppHost, ServiceDefaults and samples are
# intentionally excluded — they are application/composition infrastructure, not the library
# surface the >80% line gate is meant to protect.
PACKAGE_LINE_SCOPE = [
    "src/Hexalith.Tenants.Contracts/",
    "src/Hexalith.Tenants.Client/",
    "src/Hexalith.Tenants.Server/",
    "src/Hexalith.Tenants.Testing/",
    "src/Hexalith.Tenants.Aspire/",
]

# Production files implementing tenant isolation and role authorization logic. The PRD scopes
# the 100% branch requirement to this logic specifically — NOT to total solution branch
# coverage. Add query/projection isolation files here as those implementation areas land.
DEFAULT_ISOLATION_AUTH_TARGETS = [
    "src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs",
    "src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs",
    "src/Hexalith.Tenants.Server/Validators/ChangeUserRoleValidator.cs",
]


class CoverageGateError(Exception):
    """Raised when a coverage gate fails or is misconfigured."""


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate Cobertura coverage gates.")
    parser.add_argument("--coverage-root", type=Path, default=Path("TestResults"))
    parser.add_argument("--minimum-line-coverage", type=float, required=True)
    parser.add_argument("--required-branch-coverage", type=float, required=True)
    parser.add_argument(
        "--isolation-auth-target",
        action="append",
        dest="isolation_auth_targets",
        default=[],
        help="Named production file included in the isolation/auth branch coverage gate.",
    )
    parser.add_argument("--summary-file", type=Path)
    args = parser.parse_args()

    coverage_files = sorted(args.coverage_root.glob("**/coverage.cobertura.xml"))
    if not coverage_files:
        raise CoverageGateError(f"No coverage.cobertura.xml files found under {args.coverage_root}.")

    classes = list(iter_classes(coverage_files))
    targets = args.isolation_auth_targets or DEFAULT_ISOLATION_AUTH_TARGETS

    line = measure_line_coverage(classes, PACKAGE_LINE_SCOPE)
    branch = measure_named_branch_coverage(classes, targets)

    if line.valid == 0:
        raise CoverageGateError(
            "No publishable-package line coverage data found. Check that coverage filenames are "
            f"under one of: {', '.join(PACKAGE_LINE_SCOPE)}."
        )

    if branch.valid == 0:
        raise CoverageGateError(
            "No isolation/auth branch coverage data found. Check that the named targets contain "
            "branch data in the Cobertura reports."
        )

    summary = format_summary(line, branch, targets)
    print(summary)
    write_summary(args.summary_file, summary)

    if line.percentage <= args.minimum_line_coverage:
        raise CoverageGateError(
            f"Overall line coverage {line.percentage:.2f}% ({line.covered}/{line.valid}) is not "
            f"greater than {args.minimum_line_coverage:.2f}% for the publishable packages."
        )

    if branch.percentage < args.required_branch_coverage:
        raise CoverageGateError(
            f"Isolation/auth branch coverage {branch.percentage:.2f}% ({branch.covered}/{branch.valid}) "
            f"is below {args.required_branch_coverage:.2f}% for: {', '.join(branch.matched_targets)}."
        )

    return 0


class LineCoverage:
    def __init__(self, covered: int, valid: int) -> None:
        self.covered = covered
        self.valid = valid

    @property
    def percentage(self) -> float:
        return percentage(self.covered, self.valid)


class BranchCoverage:
    def __init__(self, covered: int, valid: int, matched_targets: list[str]) -> None:
        self.covered = covered
        self.valid = valid
        self.matched_targets = matched_targets

    @property
    def percentage(self) -> float:
        return percentage(self.covered, self.valid)


def iter_classes(coverage_files: list[Path]):
    """Yield (filename, class_element) pairs from every report, filenames normalized."""
    for coverage_file in coverage_files:
        root = ElementTree.parse(coverage_file).getroot()
        for class_element in root.findall(".//class"):
            yield normalize_path(class_element.attrib.get("filename", "")), class_element


def measure_line_coverage(classes, scope_prefixes: list[str]) -> LineCoverage:
    """Union of covered line numbers across all reports, scoped to the given path prefixes."""
    scope = [normalize_path(prefix) for prefix in scope_prefixes]
    covered: set[tuple[str, str]] = set()
    valid: set[tuple[str, str]] = set()

    for filename, class_element in classes:
        if not any(path_matches_prefix(filename, prefix) for prefix in scope):
            continue
        for line_element in class_element.findall(".//line"):
            key = (filename, line_element.attrib.get("number", ""))
            valid.add(key)
            if int(line_element.attrib.get("hits", "0")) > 0:
                covered.add(key)

    return LineCoverage(len(covered), len(valid))


def measure_named_branch_coverage(classes, targets: list[str]) -> BranchCoverage:
    """Best-per-line union of branch conditions for each named target file.

    A given (file, line) branch can appear in several reports with different covered counts;
    the merged covered count is the maximum observed (the lane that exercised it best). A
    target that is found but contains no branches counts as vacuously satisfied (0/0). A
    target that is absent from every report is a misconfiguration (renamed/typo'd path).
    """
    # (filename, line) -> (best_covered, valid)
    per_line: dict[tuple[str, str], tuple[int, int]] = {}
    found_targets: set[str] = set()

    for filename, class_element in classes:
        target = match_target(filename, targets)
        if target is None:
            continue
        found_targets.add(target)
        for line_element in class_element.findall(".//line"):
            if line_element.attrib.get("branch", "").lower() != "true":
                continue
            match = re.search(r"\((\d+)/(\d+)\)", line_element.attrib.get("condition-coverage", ""))
            if not match:
                continue
            covered, valid = int(match.group(1)), int(match.group(2))
            key = (filename, line_element.attrib.get("number", ""))
            prev_covered, _ = per_line.get(key, (0, valid))
            per_line[key] = (max(prev_covered, covered), valid)

    missing = [target for target in targets if target not in found_targets]
    if missing:
        raise CoverageGateError(
            f"Isolation/auth coverage target(s) not found in any report (renamed or mis-scoped): "
            f"{', '.join(missing)}."
        )

    total_covered = sum(covered for covered, _ in per_line.values())
    total_valid = sum(valid for _, valid in per_line.values())
    return BranchCoverage(total_covered, total_valid, list(targets))


def match_target(filename: str, targets: list[str]) -> str | None:
    for target in targets:
        if filename.endswith(normalize_path(target)):
            return target
    return None


def path_matches_prefix(filename: str, prefix: str) -> bool:
    return filename.startswith(prefix) or f"/{prefix}" in filename


def percentage(covered: int, valid: int) -> float:
    if valid == 0:
        return 100.0
    return covered / valid * 100


def format_summary(line: LineCoverage, branch: BranchCoverage, targets: list[str]) -> str:
    target_lines = "\n".join(f"- {target}" for target in targets)
    return "\n".join(
        [
            "## Coverage Gates",
            f"- Overall line coverage (publishable packages): {line.percentage:.2f}% ({line.covered}/{line.valid})",
            (
                "- Isolation/auth branch coverage: "
                f"{branch.percentage:.2f}% ({branch.covered}/{branch.valid})"
            ),
            "",
            "### Isolation/Auth Gate Inputs",
            target_lines,
            "",
        ]
    )


def write_summary(summary_file: Path | None, summary: str) -> None:
    path = summary_file
    if path is None and "GITHUB_STEP_SUMMARY" in os.environ:
        path = Path(os.environ["GITHUB_STEP_SUMMARY"])

    if path is None:
        return

    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("a", encoding="utf-8") as handle:
        handle.write(summary)
        handle.write("\n")


def normalize_path(path: str) -> str:
    return path.replace("\\", "/").lstrip("./")


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except CoverageGateError as exc:
        print(f"Coverage gate failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
