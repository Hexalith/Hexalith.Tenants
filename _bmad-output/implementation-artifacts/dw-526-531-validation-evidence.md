# DW-526/DW-531 validation evidence — 2026-10-20

Entry revision: `6b62b6b307b10d5acaa86e5a722223bb34dd1b58`, `main` with existing contract/query edits and new tests. The user authorized continuation while preserving them.

## Scope and attribution

This is acceptance reconciliation of existing developer-tooling work. Commit `316a6880b5e94b4847416137e4b06011c4218c55` (DW-643) already normalized absent recorded-provenance baselines to Git's zero SHA and added recorded addition/rename/removal, complete-chain and missing-intermediate CLI cases. [Earlier evidence](dw-643-validation-evidence.md) records that implementation and its original-production failures. DW-526 and DW-531 still described the previous missing code and coverage; this run closes those stale records without changing the guard or tests.

The five focused cases run the real CLI against temporary Git repositories. A complete two-commit chain passes. Omitting a pointer-changing intermediate commit fails with `[CHAIN GAP]`; endpoint checks alone cannot detect that omission when the final pointer matches. Recorded additions, renames and removals pass with normalized absence. This qualifies the tested chain discontinuity, not a complete audit of pointer history: unrecorded intermediate moves that restore the recorded endpoint may still pass, as explicitly preserved by DW-643 and `test_recorded_story_commit_passes_after_unrecorded_move_is_reverted`.

## Commands and results

Run from the repository root:

| Command or control | Result |
| --- | --- |
| `python3 tests/scripts/test_validate_story_gitlinks.py` | Exit 0; 69/69 pass in 7.743 seconds, zero failures/skips. |
| `python3 /tmp/tenants-dw-526-531-udf5vz7w/qualify.py` (complete source below) | Exit 0; current focused cases pass 5/5; both isolated mutations are detected as specified below. |
| `python3 /tmp/tenants-dw-526-531-udf5vz7w/integrity.py` | Exit 0; only the selected records close, with original fields, full unrelated record bytes and exact index coverage preserved. |
| `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-526-531-recorded-gitlink-acceptance.md` | Exit 0; RESULT: PASS, no pointer changes. |
| Scoped `git diff --check` for the four documentation paths | Exit 0. New artifact whitespace also checked directly. |
| Current focused cases, unchanged script | Exit 0; 5/5 pass. |
| Remove only `if expected_old != old_sha:` in a temporary copy | Child exit 1; five cases run, exactly the missing-intermediate test fails after the weakened CLI incorrectly returns PASS. The other four cases pass. |
| Remove only absent-pointer normalization in a temporary copy | Child exit 1; five cases run, exactly valid addition and rename fail with CHAIN GAP. Complete chain, missing-intermediate refusal and removal still pass. |

The corrected harness first requires the exact guard/test SHA-256 inputs below, then checks exact failure names, exit codes, five-test totals and absence of errors/skips; expected child failures make the enclosing qualification succeed. It never replaces workspace source. Raw logs and machine-readable results are retained under `/tmp/tenants-dw-526-531-udf5vz7w`; reproduction does not require those ephemeral files.

Qualified SHA-256 inputs:

- Guard: `beae32d1c74bd07127637e79e6c69f2a072f76236977d27109ccf17bf0088fe4`.
- Tests: `da4cf038fa1032f1df143f2a62a5907e4a5de5e52724b583798277d12449e9d4`.

### Reproduce focused controls

Save this script to a temporary file and execute it with `python3` from the repository root. It writes logs beside that temporary file and needs only Python 3 and Git. Assertions are explicit runtime checks, including under Python optimization.

```python
from pathlib import Path
import hashlib
import json
import re
import shutil
import subprocess
import sys
import tempfile

ROOT = Path.cwd()
OUTPUT = Path(__file__).resolve().parent
SCRIPT = Path("scripts/validate-story-gitlinks.py")
TESTS = Path("tests/scripts/test_validate_story_gitlinks.py")
CLASS = "GuardCommandIntegrationTests."
METHODS = [
    CLASS + "test_recorded_story_commits_pass_recorded_addition",
    CLASS + "test_recorded_story_commits_pass_recorded_rename",
    CLASS + "test_recorded_story_commits_pass_complete_multi_commit_chain",
    CLASS + "test_recorded_story_commits_still_reject_missing_intermediate_commit",
    CLASS + "test_recorded_story_commits_pass_recorded_removal",
]


def require(condition, message):
    if not condition:
        raise SystemExit(message)


def run_case(name, source, expected_failures):
    with tempfile.TemporaryDirectory(prefix=f"dw-526-531-{name}-") as directory:
        root = Path(directory)
        (root / SCRIPT.parent).mkdir(parents=True)
        (root / TESTS.parent).mkdir(parents=True)
        (root / SCRIPT).write_text(source, encoding="utf-8")
        shutil.copy2(ROOT / TESTS, root / TESTS)
        command = [sys.executable, str(root / TESTS), "-v", *METHODS]
        result = subprocess.run(command, capture_output=True, text=True, timeout=60)
        log = result.stdout + result.stderr
        (OUTPUT / f"{name}.log").write_text(log, encoding="utf-8")
        failures = re.findall(r"^FAIL: (\w+) ", log, re.M)
        expected_code = 1 if expected_failures else 0
        require(result.returncode == expected_code, f"{name}: wrong exit code\n{log}")
        require(re.search(r"Ran 5 tests in", log), f"{name}: wrong test count\n{log}")
        require(sorted(failures) == sorted(expected_failures), f"{name}: wrong failures\n{log}")
        require(not re.search(r"^ERROR:|skipped=|errors=", log, re.M), f"{name}: unexpected error/skip\n{log}")
        if not expected_failures:
            require(log.rstrip().endswith("OK"), f"{name}: missing success verdict\n{log}")
        else:
            require(
                log.rstrip().endswith(f"FAILED (failures={len(expected_failures)})"),
                f"{name}: wrong failure summary\n{log}",
            )
        return {
            "name": name,
            "command": command,
            "source_sha256": hashlib.sha256(source.encode()).hexdigest(),
            "returncode": result.returncode,
            "tests": 5,
            "failures": failures,
        }


for path, expected in (
    (SCRIPT, "beae32d1c74bd07127637e79e6c69f2a072f76236977d27109ccf17bf0088fe4"),
    (TESTS, "da4cf038fa1032f1df143f2a62a5907e4a5de5e52724b583798277d12449e9d4"),
):
    require(hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == expected,
            f"Qualified input mismatch: {path}")
source = (ROOT / SCRIPT).read_text(encoding="utf-8")
chain = "if expected_old != old_sha:"
absent = 'owned_targets.get(path, current_pointer(path, baseline) or "0" * 40)'
require(source.count(chain) == 1, "Chain mutation must match exactly once")
require(source.count(absent) == 1, "Absent-pointer mutation must match exactly once")
results = [run_case("current", source, [])]
results.append(run_case(
    "without-chain-check", source.replace(chain, "if False:", 1),
    ["test_recorded_story_commits_still_reject_missing_intermediate_commit"],
))
results.append(run_case(
    "without-absent-normalization",
    source.replace(absent, "owned_targets.get(path, current_pointer(path, baseline))", 1),
    ["test_recorded_story_commits_pass_recorded_addition", "test_recorded_story_commits_pass_recorded_rename"],
))
require((ROOT / SCRIPT).read_text(encoding="utf-8") == source, "Workspace guard changed")
report = {
    "script_sha256": hashlib.sha256((ROOT / SCRIPT).read_bytes()).hexdigest(),
    "tests_sha256": hashlib.sha256((ROOT / TESTS).read_bytes()).hexdigest(),
    "results": results,
}
(OUTPUT / "qualification.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
for result in results:
    print(f"{result['name']}: {result['tests']} tests, exit {result['returncode']}, failures={result['failures']}")
print("PASS: current protections pass; isolated regressions detect both removed protections")
```

## Ledger integrity and preservation

Only DW-526 and DW-531 close. All replaced fields remain under `pre-dw-526-*` or `pre-dw-531-*`, and every unrelated ledger block, including its heading and line endings, remains byte-identical. The index removes exactly their two rows and adds the dated closure summary; prior summaries and other rows remain unchanged. Totals are 656 IDs, 385 done/271 open across 243 rows, with every open ID indexed exactly once.

The byte-level check passes, including headings and line endings. Run `python3 /tmp/tenants-dw-526-531-udf5vz7w/integrity.py`; its complete reproducible source follows. It compares against the canonical entry revision, independently of ephemeral snapshots.

```python
from pathlib import Path
import re
import subprocess

BASE = "6b62b6b307b10d5acaa86e5a722223bb34dd1b58"
SELECTED = {"526", "531"}
FIELDS = {b"status", b"disposition", b"verification-date", b"verification", b"verification-files", b"evidence-kind", b"next-action"}


def require(condition, message):
    if not condition:
        raise SystemExit(message)


def original(path):
    return subprocess.check_output(["git", "show", f"{BASE}:{path}"])


def records(text):
    pairs = [(match[1].decode(), match[0]) for match in re.finditer(
        rb"^### DW-(\d+):[^\n]*\n.*?(?=^### DW-|\Z)", text, re.M | re.S,
    )]
    require(len(pairs) == len(dict(pairs)), "Duplicate ledger IDs")
    return dict(pairs)


def remove_summary(text):
    title, summary, rest = text.split(b"\n\n", 2)
    require(summary.startswith(b"**DW-526/DW-531 closure (2026-10-20):**"), "Closure summary missing")
    return title + b"\n\n" + rest


ledger = "_bmad-output/implementation-artifacts/deferred-work.md"
before_text, after_text = original(ledger), Path(ledger).read_bytes()
before, after = records(before_text), records(after_text)
require(set(before) == set(after) and len(after) == 656, "IDs changed")
require({key for key in before if before[key] != after[key]} == SELECTED, "Unrelated record changed")
for key in SELECTED:
    expected = [f"pre-dw-{key}-".encode() + line if line.partition(b":")[0] in FIELDS else line for line in before[key].splitlines(keepends=True)]
    retained = [line for line in after[key].splitlines(keepends=True) if line.partition(b":")[0] not in FIELDS | {b"resolution-evidence", b"resolution-spec"}]
    require(retained == expected, f"DW-{key}: historical fields changed")
require(remove_summary(after_text).split(b"### DW-1:")[0] == before_text.split(b"### DW-1:")[0], "Ledger history changed")
open_ids = {key for key, value in after.items() if re.search(rb"^status: open\b", value, re.M)}
done_ids = {key for key, value in after.items() if re.search(rb"^status: done\b", value, re.M)}
require(len(open_ids) == 271 and len(done_ids) == 385 and open_ids | done_ids == set(after), "Incorrect statuses/counts")
index = "docs/maintenance-backlog.md"
old_index, new_index = original(index), Path(index).read_bytes()
removed = [line for line in old_index.splitlines(keepends=True) if any(line.startswith(f"| DW-{key} |".encode()) for key in SELECTED)]
require(len(removed) == 2, "Expected two original index rows")
for row in removed:
    old_index = old_index.replace(row, b"", 1)
require(remove_summary(new_index) == old_index, "Unrelated index content changed")
rows = [line for line in new_index.splitlines() if line.startswith(b"| DW-")]
covered = [value.decode() for line in rows for value in re.findall(rb"DW-(\d+)", line.split(b"|")[1])]
require(len(rows) == 243 and len(covered) == len(set(covered)) and set(covered) == open_ids, "Incorrect index coverage")
subprocess.run([
    "git", "diff", "--exit-code", BASE, "--", "references/",
    "_bmad-output/implementation-artifacts/deferred-work-archive.md",
    "_bmad-output/implementation-artifacts/sprint-status.yaml",
    "scripts/validate-story-gitlinks.py", "tests/scripts/test_validate_story_gitlinks.py",
], check=True)
subprocess.run(["git", "diff", "--cached", "--exit-code"], check=True)
print("PASS: only DW-526/DW-531 close; historical fields preserved; 656 IDs / 385 done / 271 open / 243 rows")
```

### Review-correction controls

The initial review exposed that the first integrity parser omitted headings and normalized line endings. The corrected byte-level parser rejects both corruptions. It also rejects changed qualified inputs before running the controls. Run `python3 /tmp/tenants-dw-526-531-udf5vz7w/review_controls.py`: exit 0; four deliberate corruptions refused, zero workspace mutation. To reproduce elsewhere, save the preceding scripts as `qualify.py` and `integrity.py` beside this third script, then run it from the repository root:

```python
from pathlib import Path
import re
import runpy
from unittest.mock import patch

ROOT = Path.cwd()
RUN = Path(__file__).resolve().parent
READ_BYTES = Path.read_bytes


def expect_refusal(script, path, replacement, expected):
    def read_bytes(instance):
        return replacement if instance.resolve() == path.resolve() else READ_BYTES(instance)

    with patch.object(Path, "read_bytes", read_bytes):
        try:
            runpy.run_path(str(RUN / script), run_name="__main__")
        except SystemExit as error:
            if str(error) != expected:
                raise SystemExit(f"Wrong refusal: {error}") from error
        else:
            raise SystemExit(f"Missing refusal: {expected}")


ledger = ROOT / "_bmad-output/implementation-artifacts/deferred-work.md"
data = ledger.read_bytes()
heading = data.replace(b"### DW-527:", b"### DW-527: changed heading", 1)
line_endings = re.sub(rb"(^### DW-527:[^\n]*)\n", rb"\1\r\n", data, count=1, flags=re.M)
if heading == data or line_endings == data:
    raise SystemExit("Integrity corruption must change the fixture")
for name, replacement in (("heading", heading), ("line ending", line_endings)):
    expect_refusal("integrity.py", ledger, replacement, "Unrelated record changed")
    print(f"PASS: integrity refuses an unrelated {name} change")
for relative in ("scripts/validate-story-gitlinks.py", "tests/scripts/test_validate_story_gitlinks.py"):
    path = ROOT / relative
    expect_refusal("qualify.py", path, path.read_bytes() + b"\n", f"Qualified input mismatch: {relative}")
    print(f"PASS: qualification refuses changed input {relative}")
```

### Session-local user-change check

Separate from the reproducible ledger check, this session compared SHA-256 values of 3,685 entry files against `/tmp/tenants-dw-526-531-udf5vz7w/entry.json`. At the pre-review check, only the two planned existing documentation files changed; the other 3,683 files, including the existing tracked and untracked contract/query work, matched. The two new documentation artifacts are this evidence and its spec. HEAD and staging remain unchanged. This entry manifest is ephemeral: the session-local user-edit comparison cannot be independently repeated after its temporary directory is removed. The published integrity script proves the ledger/index, guard/tests, dependency-pointer, archive and sprint comparisons against the recorded revision; it does not substitute for that entry-manifest check.

The final snapshot comparison initially exited 1 because seven existing files changed concurrently after the earlier successful comparison. Those changes were recorded and left untouched, without resetting or claiming them as this workflow's work:

- `src/Hexalith.Tenants.Contracts/Queries/GetTenantAuditQuery.cs`
- `src/Hexalith.Tenants.Contracts/Queries/GetTenantQuery.cs`
- `src/Hexalith.Tenants.Contracts/Queries/GetTenantUsersQuery.cs`
- `src/Hexalith.Tenants.Contracts/Queries/GetUserTenantsQuery.cs`
- `src/Hexalith.Tenants.Contracts/Queries/ListTenantsQuery.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/McpCliDecorationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Queries/EnvelopePagingQueryHandlerTests.cs`

The final receipt, `/tmp/tenants-dw-526-531-udf5vz7w/preservation-final.json`, records their before/after hashes, no unexpected new paths, 3,676 other unchanged entry files and unchanged canonical HEAD. This does not claim that all external files stayed byte-identical through completion. All workflow writes remain confined to the four planned documentation paths. Guard/test input hashes and the ledger/index integrity check still pass. All three published scripts match their executed copies exactly.

## Review and limits

Independent Blind Hunter review reproduced the maintained lane and the original controls, confirmed DW-643 attribution, and returned five findings. The integrity parser, input pins, evidence limits and session-local preservation explanation were corrected; the pending-status finding reflected the normal pre-finalization review phase. Complete verdicts and evidence are in the [spec's Review Triage Log](spec-dw-526-531-recorded-gitlink-acceptance.md#review-triage-log). The independent correction recheck reran all three embedded scripts, the story gitlink guard and scoped whitespace check successfully, accepted the corrections and found no remaining concrete issue. No findings were deferred, and no additional review layers are configured for this small-change route. The spec is finalized as done.

No application, .NET build, distributed runtime, package publication, CI-invocation or release-readiness qualification is claimed. No application or test-source changes were needed, so no Aspire topology was started for this documentation reconciliation. Existing unresolved release holds remain. Changes stay uncommitted under the user-provided Git-mutation safeguard.
