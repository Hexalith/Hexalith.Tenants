"""Reproduce this fixed documentation snapshot's conservation/provenance checks.

Run from the repository root with Python 3; no dependencies or checkout writes.
Future unrelated ledger changes intentionally fail the fixed-snapshot checks.
"""

from collections import Counter
import hashlib
import importlib.util
from pathlib import Path
import re
import subprocess
import sys
import tempfile


ROOT = Path(__file__).resolve().parents[2]
BASE = "0c10ef637171f9ac2639d4b5ac42f513ea9ef133"
ARTIFACTS = "_bmad-output/implementation-artifacts/"
SELECTED = {297, 462, 533, 549, 558}
REVERIFICATION = ARTIFACTS + "spec-5-6-preview-confirm-and-link-a-tenant-correction-2.md"
ENDPOINT = "2b91b05be9c927319fd207b02552d1213997776b"
HISTORICAL_SPECS = {
    ARTIFACTS + "spec-2-4b-wp-2a-removal-proof-and-audit-available.md":
        "b2b80941df874c2ee6772ca316841c480e0e493b",
    REVERIFICATION: ENDPOINT,
}


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT, text=True).strip()


def previous(path):
    return subprocess.check_output(["git", "show", f"{BASE}:{path}"], cwd=ROOT, text=True)


def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def records(text):
    matches = list(re.finditer(r"^### DW-(\d+): [^\n]*\n.*?(?=^##|\Z)", text, re.M | re.S))
    result = {int(match[1]): match[0] for match in matches}
    require(len(result) == len(matches) == 657, "Expected 657 unique ledger records")
    return result


def main():
    ledger = ARTIFACTS + "deferred-work.md"
    old, new = records(previous(ledger)), records(read(ledger))
    require(old.keys() == new.keys(), "Ledger IDs changed")
    require({n for n in new if old[n] != new[n]} == SELECTED, "Unexpected changed records")
    for number in SELECTED:
        require(re.search(r"^status: done 2026-10-21$", new[number], re.M), f"DW-{number} not closed")
        for line in old[number].splitlines():
            require(not line or line in new[number]
                    or "pre-evidence-reconciliation-" + line in new[number],
                    f"Lost DW-{number} field: {line}")
    counts = Counter(re.search(r"^status: (\w+)", body, re.M)[1] for body in new.values())
    require(counts == {"done": 460, "open": 197}, f"Unexpected status counts: {counts}")
    rows = [line for line in read("docs/maintenance-backlog.md").splitlines()
            if line.startswith("| DW-")]
    # The first Markdown table cell contains explicit DW IDs, including grouped
    # IDs separated by commas/slashes. Each ID must expand to exactly one cover.
    covered = [int(value) for row in rows for value in re.findall(r"DW-(\d+)", row.split("|")[1])]
    opened = {n for n, body in new.items() if re.search(r"^status: open$", body, re.M)}
    require(len(rows) == 181 and len(covered) == len(set(covered)) and set(covered) == opened,
            "Maintenance rows do not exactly cover open records")
    print("PASS conservation: 657 IDs, exactly five closures, 460 done/197 open, 181 rows")

    for name in ["5-4-audit-availability-state-recovery.md", "spec-global-admin-projection-paging.md"]:
        path = ARTIFACTS + name
        require(read(path) == previous(path), f"Blocked historical artifact changed: {name}")
        print("PASS unchanged:", name, hashlib.sha256((ROOT / path).read_bytes()).hexdigest())
    frozen = r"<(?P<tag>frozen-after-approval|intent-contract)\b[^>]*>.*?</(?P=tag)>"
    for path in HISTORICAL_SPECS:
        before = [match[0] for match in re.finditer(frozen, previous(path), re.S)]
        after = [match[0] for match in re.finditer(frozen, read(path), re.S)]
        require(bool(before) and before == after, f"Frozen intent changed: {path}")
    print("PASS both edited historical specs retain exact frozen intent")

    module_spec = importlib.util.spec_from_file_location(
        "changed_story_guard", ROOT / "scripts/validate-changed-story-gitlinks.py")
    ci = importlib.util.module_from_spec(module_spec)
    module_spec.loader.exec_module(ci)
    require(git("log", "-1", "--format=%H", BASE, "--", REVERIFICATION) == ENDPOINT,
            "Endpoint is not the final historical artifact edit")
    for path, endpoint in HISTORICAL_SPECS.items():
        text = read(path)
        require(ci.guard.read_frontmatter(text)["baseline_commit"]
                == ci.guard.read_frontmatter(previous(path))["baseline_commit"],
                f"Historical baseline changed: {path}")
        require(ci.audit_ref(text, {path}, path, BASE, BASE, False) == endpoint,
                f"Unexpected CI endpoint: {path}")
        print("PASS CI endpoint:", path, endpoint)
    batch = ARTIFACTS + "spec-dw-297-462-533-549-558-evidence-reconciliation.md"
    require(ci.audit_ref(read(batch), {batch}, batch, BASE, BASE, False) == BASE,
            "Unexpected batch endpoint")
    print("PASS CI endpoint:", batch, BASE)

    text = read(REVERIFICATION)
    targets = ci.guard.stated_targets(text)
    require(set(targets) == {"references/Hexalith.Builds", "references/Hexalith.Memories",
                             "references/Hexalith.Platform"}, "Historical target set changed")
    for path, target in targets.items():
        require(target == git("rev-parse", f"{ENDPOINT}:{path}"), f"Wrong target: {path}")
    # Validate every baseline/target table cell against the immutable Git trees.
    for path, baseline, target in re.findall(
            r"^\| `(references/[^`]+)` \| `([0-9a-f]{40})` \| `([0-9a-f]{40})` \|$",
            text, re.M):
        require(baseline == git("rev-parse", f"{ci.guard.read_frontmatter(text)['baseline_commit']}:{path}")
                and target == targets[path], f"Incorrect table endpoints: {path}")
    print("PASS exact historical table baselines and targets")

    pointer_chain = git("log", "--first-parent", "--reverse", "--format=%H",
                        f"{ci.guard.read_frontmatter(text)['baseline_commit']}..{ENDPOINT}",
                        "--", *sorted(targets)).splitlines()
    require(pointer_chain == ["5bfe0715591ecae8790472b29bb0e7d8986da4ae",
                              "9ee73062b7e540b69f824803d182f151c96ddb24",
                              "416ab32cca5d4b9d1f56bc5558bc2c974912a8cf"],
            "Historical first-parent pointer landing chain changed")
    print("PASS complete three-commit first-parent pointer landing chain")

    controls = [("wrong target " + path, text.replace("`" + target + "`", "`" + "f" * 40 + "`"),
                 ENDPOINT, "[MISSTATED]") for path, target in sorted(targets.items())]
    controls.append(("missing Builds declaration", text.replace("- references/Hexalith.Builds\n", ""),
                     ENDPOINT, "[UNDECLARED]"))
    controls.append(("original checkpoint with final targets", text,
                     "5bfe0715591ecae8790472b29bb0e7d8986da4ae", "[MISSTATED]"))
    with tempfile.TemporaryDirectory(prefix="dw-evidence-controls-") as directory:
        copy = Path(directory) / Path(REVERIFICATION).name
        for label, content, endpoint, diagnostic in controls:
            copy.write_text(content, encoding="utf-8")
            command = [sys.executable, str(ROOT / "scripts/validate-story-gitlinks.py"),
                       str(copy), "--ref", endpoint]
            result = subprocess.run(command, cwd=ROOT, text=True, capture_output=True, check=False)
            require(result.returncode == 1 and diagnostic in result.stdout,
                    f"Wrong control result for {label}: {result.stdout} {result.stderr}")
            print(f"REJECTED {label}: exit 1 {diagnostic}")
    print("PASS three CI endpoint preflights and five focused guard controls")


if __name__ == "__main__":
    main()
