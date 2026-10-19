# DW-643 validation evidence — 2026-10-19

Entry revision: `63b7b3d8d6cd18db189c7dbfaf3cd94491115fa9`, clean `main`.

## Behavior and scope

The story-owned gitlink guard now reconciles the audited revision with every
recorded final target and every other baseline-to-revision gitlink change. It
rejects unrecorded advances, reversions to baseline, additions, removals,
renames and changes to an existing unowned path. The union includes owned paths
even if their current pointer equals baseline and no net diff remains.
Recorded additions and removals use the same absent-pointer representation as
Git's raw diff in chain and endpoint checks.
Rename detection is disabled so each path is checked as an addition or removal.

This deliberately corrects the previous blanket exclusion of later committed
dependency updates. The existing `--ref <story-endpoint>` remains the way to
audit a historical story before later updates; it is exercised after a new
pointer commit. No ownership is inferred from commit subjects, no exemption
list is introduced, and the removed Non-Story mode stays removed.

Unrecorded intermediate moves restored to the recorded endpoint pass: this is
an endpoint check, not a complete intermediate-history audit. Existing canonical
commit, chain, declaration, target-claim and working-tree checks remain. Real
isolated-Git fixtures prove matching endpoints, a complete multi-commit chain,
recorded addition/rename/deletion, unchanged existing unowned pointers, later
changes without pointer movement and historical audits pass. A missing
intermediate commit, staged pointer movement and actual unstaged submodule
checkout movement still fail. Endpoint diagnostics name the complete expected
and actual target; historical-ref advice appears only for endpoint mismatches.

Only the Python developer guard and its regression lane change. No application,
.NET build, distributed runtime, package publication or release-readiness result
is claimed. DW-585's actual-story CI invocation remains separate. No tracked
dependency pointers, sprint status or archive entries are changed.

## Commands and results

Run from the repository root:

| Command or control | Result |
| --- | --- |
| `python3 tests/scripts/test_validate_story_gitlinks.py` before edits | Exit 0; 23 tests pass. |
| Final regression tests with the original script from the entry revision, in an isolated temporary directory | Exit 1; 39 tests run; eight unsafe PASS failures and two valid recorded-addition/rename controls fail. |
| `python3 tests/scripts/test_validate_story_gitlinks.py` after implementation and independent recheck | Exit 0; 39 tests pass, zero failures/skips. |
| `python3 -m unittest discover -s tests/scripts -p 'test_*.py'` | Exit 0; 39 tests pass, zero failures/skips; this is the only Python test file in that directory. |
| `git diff --check` | Exit 0. |
| `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-643-story-gitlink-endpoints.md` | Exit 0; no `references/` pointer movement in this task. |

The original-script control uses the maintained CLI integration tests unchanged:

```python
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

baseline = "63b7b3d8d6cd18db189c7dbfaf3cd94491115fa9"
original = subprocess.run(
    ["git", "show", f"{baseline}:scripts/validate-story-gitlinks.py"],
    check=True, capture_output=True, text=True,
).stdout
with tempfile.TemporaryDirectory(prefix="dw-643-original-") as name:
    root = Path(name)
    (root / "scripts").mkdir()
    (root / "tests/scripts").mkdir(parents=True)
    (root / "scripts/validate-story-gitlinks.py").write_text(original)
    shutil.copy2("tests/scripts/test_validate_story_gitlinks.py", root / "tests/scripts/test_validate_story_gitlinks.py")
    result = subprocess.run(
        [sys.executable, str(root / "tests/scripts/test_validate_story_gitlinks.py")],
        capture_output=True, text=True,
    )
    print(result.stdout + result.stderr)
    if result.returncode != 1 or "FAILED (failures=10)" not in result.stderr:
        raise SystemExit("Original-script control did not reproduce ten failures")
```

The eight failures are the later advance under both default HEAD and explicit
revision, baseline reversion, unrecorded removal, new declared-but-unowned path,
change and removal of an existing unowned path, and unrecorded rename. Two
further failures reproduce the inherited absent-pointer chain mismatch for valid
recorded additions and renames. The other matching/chain/deletion/restore
controls still pass against the original script.

## Review and ledger reconciliation

The configured independent Blind Hunter review returned six findings. All six
were applied and recorded in the spec's Review Triage Log: absent-pointer chain
normalization; unchanged-unowned control; actual unstaged checkout control;
complete diagnostic assertions; endpoint-only historical-ref advice; and final
completion reconciliation. The reviewer independently rechecked the corrections
and ran all 39 tests successfully, finding no remaining concrete code defects.
No findings were deferred. No additional review layers are configured for this
small-change route.

The lossless ledger/index check against the entry revision passes: all 655 IDs
remain, 380 done and 275 open, with the open
set covered exactly once by 247 maintenance rows. Only DW-643's record changes;
every replaced original field is retained under `pre-dw-643-*`, all other record
bodies and maintenance rows are identical, and the only removed row is DW-643.
`git diff --raw 63b7b3d8d6cd18db189c7dbfaf3cd94491115fa9 -- references/` is empty.

No commit or push is performed under the repository's instruction to avoid
version-control mutations unless the task explicitly requires them. This task
implements the selected deferred item and leaves its changes reviewable.
