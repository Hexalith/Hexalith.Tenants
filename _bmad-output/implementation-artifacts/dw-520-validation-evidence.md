# DW-520 validation evidence — 2026-10-19

Entry revision: `c4c781791b0b50eabeb0b58505d943be40538e7f`, clean `main`.

## Scope and behavior

The story gitlink guard now reads strict three-cell dependency rows of the form
`path | baseline | target` in its existing binding sections. SHA cells accept
7–40 hexadecimal characters; `absent` is also accepted in the baseline cell for
the addition format already used by tracked stories. Full zero-SHA targets use
the guard's existing removal representation. Optional outer pipes/backticks,
case normalization and the existing Unicode normalization are supported. New
table claims inside backtick/tilde fences, indented code or a whole inline-code
span are ignored, so examples cannot replace a live claim. Cell-local paired
backticks remain supported; fence closure restores table parsing.

Only the target is a binding claim; this repair does not validate the table's
baseline cell against Git. Existing canonical baseline/provenance/chain checks
retain their own contracts. Tables require an explicit bullet-path declaration.
Headers, separators, prose and other table shapes do not create table claims;
existing arrow parsing remains available, including arrow claims within tables.

The existing section and last-claim rules apply to both forms: File List includes
subsections, Completion Notes List stops at its first subheading, and unrelated
sections do not bind. Matching changed-pointer targets pass and stale targets
produce MISSTATED in legacy and recorded-provenance modes, under both default
HEAD with an actual dependency checkout and explicit `--ref`. An unchanged
pointer claim is checked under explicit `--ref` in legacy mode. Recorded mode
continues to reject claims for unchanged, unowned dependencies with UNOWNED
CLAIM. Recorded additions/removals, undeclared-path refusals and all pre-existing
chain/endpoint/working-tree tests remain covered.

Only developer tooling and completion records change. There are no application,
runtime, .NET, browser, CI, publication or release-qualification claims. DW-526,
DW-531 and actual-story CI wiring remain separately tracked. Dependency pointers,
archive and sprint tracking remain unchanged.

## Commands and results

Commands run from the repository root:

| Command or control | Result |
| --- | --- |
| `python3 tests/scripts/test_validate_story_gitlinks.py` before edits | Exit 0; 39 tests pass. |
| New tests with original production, before the parser repair | Exit 1; 54 tests run, 19 assertion/subtest failures. Two legacy CLI cases incorrectly return PASS for a stale table. Recorded-mode tests also demonstrate missing table binding through UNSTATED or missing MISSTATED. |
| Initial repaired direct/discovery lanes | Each exits 0; 54 tests pass, zero failures/skips. The first isolated original replay reproduces the same 19 assertion/subtest failures. |
| Expanded review-correction lane before repairing its checkout fixture | Exit 1; 61 tests run with three assertion/subtest failures. Default-HEAD controls had no dependency checkout and therefore saw a removed pointer. Creating an isolated local checkout fixes the fixture without changing the production movement checks. |
| `python3 tests/scripts/test_validate_story_gitlinks.py` after review corrections | Exit 0; 61 tests pass, zero failures/skips. |
| Original-script replay below with final tests | Control exits 0 after verifying the original suite invocation exits 1 with 45 assertion/subtest failures across 61 tests. |
| `python3 -m unittest discover -s tests/scripts -p 'test_*.py'` after review corrections | Exit 0; 61 tests pass, zero failures/skips. This is the only Python test file in that directory. |
| `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-520-gitlink-table-targets.md` | Exit 0, RESULT: PASS; no pointer changes. |
| `git diff --check` | Exit 0. |

Initial logs were captured under `/tmp/tenants-dw-520`; they are not required for
reproduction. The following isolated original-production replay uses the tracked
tests and never replaces production source in the workspace:

```python
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

baseline = "c4c781791b0b50eabeb0b58505d943be40538e7f"
original = subprocess.run(
    ["git", "show", f"{baseline}:scripts/validate-story-gitlinks.py"],
    check=True, capture_output=True, text=True,
).stdout
with tempfile.TemporaryDirectory(prefix="dw-520-original-") as name:
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
    if result.returncode != 1 or "FAILED (failures=45)" not in result.stderr:
        raise SystemExit("Original-script control did not reproduce forty-five failures")
```

## Independent review and ledger reconciliation

Independent Blind Hunter review returned five findings. Four were patched:
fenced/inline-code examples cannot override a real table; default-mode table
coverage uses an actual isolated checkout; recorded-mode unowned-claim behavior
is explicitly covered and qualified; addition/removal spellings have refusal
controls alongside matching controls. The fifth finding establishes an inherited
default-HEAD empty-checkout resolution bug and is retained as DW-656. All five
verdicts and their evidence are in the spec's Review Triage Log. No additional
review layers are configured for this route. The independent correction recheck
runs the maintained lane successfully (61/61), passes `git diff --check`,
confirms the five dispositions and finds no further concrete defects. It also
independently reproduces DW-656 with original production.

DW-656 is not fixed by this parser repair. If an unchanged dependency directory
exists without its own Git checkout, `current_pointer(path, None)` runs
`git -C path rev-parse HEAD`, which ascends to the superproject. A matching claim
then receives MISSTATED using the superproject SHA. Current table and arrow
claims reproduce it. Replacing only the script in the temporary fixture with
original production reproduces the identical arrow failure, establishing that
the resolution bug predates this change. An absent directory and a real checkout
have different behavior; no general uninitialized-submodule correctness claim
is made. Reproduce the verified empty-directory case from the repository root:

```python
from pathlib import Path
import importlib.util
import subprocess

spec = importlib.util.spec_from_file_location(
    "dw520_tests", Path("tests/scripts/test_validate_story_gitlinks.py").resolve(),
)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
original = subprocess.run(
    ["git", "show", "c4c781791b0b50eabeb0b58505d943be40538e7f:scripts/validate-story-gitlinks.py"],
    check=True, capture_output=True, text=True,
).stdout
case = module.GuardCommandIntegrationTests()
case.setUp()
try:
    case.baseline = case.pointer_commit
    (case.repo / "references/Dependency").mkdir()
    for source, form in ((None, "table"), (None, "arrow"), (original, "arrow")):
        if source is not None:
            (case.repo / "scripts/validate-story-gitlinks.py").write_text(source)
        claim = (
            f"| references/Dependency | {case.target} | {case.target} |"
            if form == "table"
            else f"- `references/Dependency` {case.target} -> {case.target}"
        )
        result = case._run_guard("- `references/Dependency`", claim, include_worktree=True)
        print("original" if source is not None else "current", form, result.returncode, result.stdout)
        if result.returncode != 1 or "[MISSTATED]" not in result.stdout:
            raise SystemExit("Inherited empty-directory control did not reproduce")
finally:
    case.tearDown()
```

Entry counts recompute to 655 IDs, 381 done/274 open across 246 maintenance rows.
The final inline Python integrity check compares both current files against
`git show c4c781791b0b50eabeb0b58505d943be40538e7f:<path>` and exits 0:
all 655 original IDs remain, only DW-520's original record changes, and every
replaced original line is retained under `pre-dw-520-*`. DW-656 is the sole new
ID. The index removes only DW-520's row and adds only DW-656's row; all other
rows are identical. Totals are 656 IDs, 382 done/274 open across 246 rows
covering each open ID exactly once. The independent reviewer confirms the same
record/row preservation. `git diff --raw` against the entry revision is empty
for `references/`, `deferred-work-archive.md` and `sprint-status.yaml`.
No commit or push is performed under the repository's Git-mutation safeguard.
