# DW-656 validation evidence — 2026-10-19

Entry revision: `43786ea8c4a3cdd69562f7eec1109c1b891f6492`, clean `main`.

## Scope and behavior

Default-HEAD validation now uses a dependency's HEAD only when Git reports that
the dependency directory owns its worktree. An empty/deinitialized directory
discovers the superproject instead, so the guard reads the exact stage-zero
gitlink from the index. This preserves staged changes, including a staged
reversion that makes the story's baseline-to-working-tree comparison empty.
The index reader uses NUL-delimited entries and verifies the exact path, gitlink
mode and stage. Unresolved exact-path index stages refuse validation before
checkout lookup, including when a checkout exists. Working-tree diffs refuse
unmerged gitlinks too, so changed paths cannot bypass this check. Index-read errors also fail
validation. Explicit revision audits continue to read that revision's tree.
Absent resolved pointers use the existing zero-SHA representation: nonzero claims
fail rather than being skipped, while zero claims remain valid for actual absence.
Empty and ordinary tracked directories cannot substantiate a dependency SHA.

The regressions cover matching, stale and false-superproject-HEAD claims in both
table and arrow forms. Initialized directory and gitfile checkouts retain their
own HEAD semantics; moving those checkouts still rejects an incorrect claim,
while explicit revision audits ignore checkout movement. The checkout-to-baseline
control also directly checks the resolver against an isolated Git repository,
with only its repository root redirected. Removing the checkout and leaving its
directory empty restores index resolution without initialization.

A completely missing directory continues to be reported as a working-tree
removal. This repair does not reinterpret deletions or change raw movement
detection, provenance, parsing or baseline validation. Explicit-ref resolution
remains unchanged; its pre-existing unchecked absent-pointer claims now receive
the same zero-SHA validation as default-mode claims.
The tests use temporary local repositories and clones; no workspace submodule is
initialized, updated or modified.

## Commands and results

Commands run from the repository root:

| Command or control | Result |
| --- | --- |
| `python3 tests/scripts/test_validate_story_gitlinks.py` before edits | Exit 0; 61 tests pass. |
| First new 65-test lane with entry production | Exit 1; nine assertion/subtest failures, including two unsafe PASS outcomes for table/arrow claims naming the superproject HEAD. |
| First repaired 65-test lane | Exit 1; one fixture error because the separate Git-directory parent did not exist. The fixture now creates that parent. |
| First expanded 66-test lane | Exit 1; four assertion failures in an over-specific test expectation that the raw diff would be empty when a checkout returned to baseline. Git reports a resolved same-target entry; matching/stale verdicts were already correct. The test now checks resolver and CLI behavior directly. |
| Pre-review `python3 tests/scripts/test_validate_story_gitlinks.py` | Exit 0; 66 tests pass, zero failures/skips. |
| Pre-review isolated entry-production replay | Control exits 0 after confirming the original script exits 1 with nine assertion/subtest failures across the same 66 tests. |
| Initial two review-regression tests before correction | Exit 1; seven assertion/subtest failures reproduce unchecked nonzero absent-pointer claims and stale/zero/matching unresolved-pointer claims. |
| First `python3 tests/scripts/test_validate_story_gitlinks.py` after review corrections | Exit 0; 68 tests pass, zero failures/skips. |
| First review-correction entry-production replay | Control exits 0 after confirming the original script exits 1 with nineteen assertion/subtest failures across 68 tests. |
| Changed-path conflict recheck before its correction | Exit 1; the new test reproduces two assertion/subtest failures, including one legacy-mode unsafe PASS when the claim names index stage 1. |
| Final `python3 tests/scripts/test_validate_story_gitlinks.py` | Exit 0; 69 tests pass, zero failures/skips. |
| Final isolated entry-production replay below | Control exits 0 after confirming the original script exits 1 with twenty-one assertion/subtest failures across the final 69 tests. |
| Pre-review `python3 -m unittest discover -s tests/scripts -p 'test_*.py'` | Exit 0; 66 tests pass, zero failures/skips. This directory contains one maintained test file. |
| Final `python3 -m unittest discover -s tests/scripts -p 'test_*.py'` | Exit 0; 69 tests pass, zero failures/skips. |
| Recorded integrity script below | Exit 0; all 656 IDs and historical fields preserved; only DW-656 closes; 383 done/273 open across 245 exact-covering rows. |
| `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-656-uninitialized-gitlink-resolution.md` | Exit 0, RESULT: PASS; no pointer changes. |
| `git diff --check` | Exit 0. |

The original-production replay is independent of temporary logs and never
replaces the workspace script:

```python
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

baseline = "43786ea8c4a3cdd69562f7eec1109c1b891f6492"
original = subprocess.run(
    ["git", "show", f"{baseline}:scripts/validate-story-gitlinks.py"],
    check=True, capture_output=True, text=True,
).stdout
with tempfile.TemporaryDirectory(prefix="dw-656-original-") as name:
    root = Path(name)
    (root / "scripts").mkdir()
    (root / "tests/scripts").mkdir(parents=True)
    (root / "scripts/validate-story-gitlinks.py").write_text(original)
    shutil.copy2(
        "tests/scripts/test_validate_story_gitlinks.py",
        root / "tests/scripts/test_validate_story_gitlinks.py",
    )
    result = subprocess.run(
        [sys.executable, str(root / "tests/scripts/test_validate_story_gitlinks.py")],
        capture_output=True, text=True,
    )
    print(result.stdout + result.stderr)
    if result.returncode != 1 or "FAILED (failures=21)" not in result.stderr:
        raise SystemExit("Original-production control did not reproduce twenty-one failures")
```

## Aspire baseline and limits

The repository-required baseline used the local Aspire skill. `aspire ps
--format Json --non-interactive` initially returned no running AppHosts.
`aspire run --detach --no-build --isolated --apphost
src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
--non-interactive` exited 0. `aspire describe --apphost
src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
--non-interactive` exited 0 and reported all 23 resources Running/Healthy.
`aspire stop --apphost
src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive`
exited 0; the final `aspire ps` returned an empty array.

These observations concern existing Debug assets. No fresh .NET build, .NET test,
browser, CI, publication or release qualification is claimed for this Python-only
repair. No runtime/application behavior or dependency pointer changes.

## Independent review and record reconciliation

Blind Hunter review returned four initial findings and one recheck finding, all addressed. Missing-gitlink
claims now validate as absence instead of being skipped; unresolved index stages
refuse claims before checkout lookup. Empty and ordinary tracked-directory
controls cover both comparison modes, and conflict controls cover empty and
initialized checkouts plus an explicit-ref positive control. The recheck found
that changed paths could bypass the resolver check and accept index stage 1 as
a target. The working-tree comparison now refuses unmerged gitlinks before
reading raw changes, in both provenance modes; explicit-ref controls still pass.
The other findings
clarified missing checkout metadata versus directory removal and required the
planned durable evidence/maintenance reconciliation. The spec records every
verdict and reproduction; its frozen intent is unchanged. No new work is deferred.

The ledger retains all 656 IDs. Only DW-656 closes, with every replaced original
field preserved under `pre-dw-656-*`; all unrelated records remain byte-identical.
The maintenance index removes only DW-656's row and preserves every other row.
Final totals are 383 done/273 open across 245 maintenance rows, with every open
ID covered exactly once. Dependency pointers, archive and sprint tracking remain
unchanged. Final independent recheck and integrity checks are recorded below.
Changes remain uncommitted under the user-provided Git-mutation safeguard.

The exact integrity check below compares the final records with entry production:

```python
from pathlib import Path
import re
import subprocess

base = "43786ea8c4a3cdd69562f7eec1109c1b891f6492"
def original(path):
    return subprocess.run(
        ["git", "show", f"{base}:{path}"],
        check=True, capture_output=True, text=True,
    ).stdout
def require(condition, message):
    if not condition:
        raise SystemExit(message)
def records(text):
    return dict(re.findall(
        r"^### DW-(\d+):[^\n]*\n(.*?)(?=^### DW-|\Z)", text, re.M | re.S,
    ))
def remove_summary(text):
    title, summary, rest = text.split("\n\n", 2)
    return title + "\n\n" + rest

ledger = "_bmad-output/implementation-artifacts/deferred-work.md"
before_text, after_text = original(ledger), Path(ledger).read_text()
before, after = records(before_text), records(after_text)
require(set(before) == set(after) and len(after) == 656, "IDs changed")
require([key for key in before if before[key] != after[key]] == ["656"], "Unrelated record changed")
fields = {"status", "disposition", "verification", "verification-files", "evidence-kind", "next-action"}
expected = [
    "pre-dw-656-" + line if line.partition(":")[0] in fields else line
    for line in before["656"].splitlines()
]
retained = [
    line for line in after["656"].splitlines()
    if line.partition(":")[0] not in fields | {"resolution-evidence", "resolution-spec"}
]
require(retained == expected, "Historical fields changed")
require(
    remove_summary(after_text).split("### DW-656:")[0] == before_text.split("### DW-656:")[0],
    "Ledger history changed",
)
open_ids = {key for key, value in after.items() if re.search(r"^status: open\b", value, re.M)}
done_ids = {key for key, value in after.items() if re.search(r"^status: done\b", value, re.M)}
require(len(open_ids) == 273 and len(done_ids) == 383, "Counts changed")
index = "docs/maintenance-backlog.md"
old_index, new_index = original(index), Path(index).read_text()
removed = [line for line in old_index.splitlines(keepends=True) if line.startswith("| DW-656 |")]
require(len(removed) == 1, "Original index row missing")
require(remove_summary(new_index) == old_index.replace(removed[0], "", 1), "Unrelated index content changed")
rows = [line for line in new_index.splitlines() if line.startswith("| DW-")]
covered = [value for line in rows for value in re.findall(r"DW-(\d+)", line.split("|")[1])]
require(len(rows) == 245 and len(covered) == len(set(covered)) and set(covered) == open_ids, "Coverage changed")
subprocess.run([
    "git", "diff", "--exit-code", base, "--", "references/",
    "_bmad-output/implementation-artifacts/deferred-work-archive.md",
    "_bmad-output/implementation-artifacts/sprint-status.yaml",
], check=True)
print("PASS: only DW-656 closes; all history preserved; 656 IDs / 383 done / 273 open / 245 rows")
```


Final independent recheck runs the maintained lane successfully (69/69), passes
the story gitlink guard and scoped whitespace checks, executes the recorded
integrity script successfully, and finds no further concrete defects in DW-656.

Concurrent unrelated edits appeared in `Directory.Build.props`,
`src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj`,
`tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` and untracked
`src/Hexalith.Tenants.Contracts/Module.cs` during final review. They were left
untouched and excluded from this repair's tests and review. The broad whitespace
check exited 0 with Git's CRLF-normalization warning for that unrelated test
file; the final scoped check of the six DW-656 paths exits 0 without warnings.
