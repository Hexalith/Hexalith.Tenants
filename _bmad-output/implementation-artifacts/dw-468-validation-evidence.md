# DW-468 formatter reconciliation — 2026-10-16

Entry: clean `main`, HEAD `4652bf3922b4c8d5d2877942ff7a18fdd7089ec9`. DW-468 concerns the formatter requesting same-line braces despite the required Allman policy. The root option was `csharp_new_line_before_open_brace = all:warning`. Changing its value to `all` corrects the formatting option; changing the conflicting contributor sentence to Allman aligns the existing documentation. Analyzer severities and every other formatting setting remain unchanged.

The required Hexalith baseline, `_bmad-output/project-context.md:55` and the architecture's coding-pattern reference already require Allman. Microsoft's [C# formatting options documentation](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/csharp-formatting-options#csharp_new_line_before_open_brace) specifies `all` for that style. Local executable evidence below uses the repository-pinned .NET SDK `10.0.401`.

## Historical command reproduction and limitation

Run before and after the correction, from the repository root:

```bash
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --include tests/Hexalith.Tenants.UI.Tests/Components/AuditAvailabilityStateTests.cs tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs tests/Hexalith.Tenants.UI.Tests/GeneratedTenantsSurfaceTests.cs --no-restore --verify-no-changes --verbosity quiet
```

Before: exit **2**, **306 WHITESPACE** and **1,419 ENDOFLINE** diagnostics. Raw command/output: `/tmp/dw-468-red-project.log`. After: exit **2**, **three WHITESPACE** and **1,523 ENDOFLINE** diagnostics. Raw command/output: `/tmp/dw-468-green-historical.log`. Diagnostic counts can change when the formatter changes its proposed brace layout; they do not imply edits occurred. All three source files are byte-identical to entry.

The three remaining whitespace diagnostics are the range-expression continuation at `AuditAvailabilityStateTests.cs:1036` and indentation of single-line `with` initializers at `TenantListSurfaceTests.cs:825` and `:847`. Inspection confirms these are not requests to put multiline opening braces on the preceding line. The range-expression diagnostic is identical before and after. The two initializer indentation diagnostics become visible with the corrected policy. Existing LF files also conflict with the retained CRLF requirement. These source-formatting issues are outside this configuration repair; this command remains failing and no general formatting-cleanliness claim is made. Historical reports and DW-606's separately scoped formatting work remain preserved.

## Focused project verification

```bash
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --include src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionAvailabilityEvaluator.cs tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorActionAvailabilityTests.cs --no-restore --verify-no-changes --verbosity quiet
```

Initial result: exit **0**, zero formatting diagnostics, `/tmp/dw-468-green-dw331.log`. Review established that project mode skips referenced projects, so this invocation checks only the test file. Its production-file coverage claim is superseded by the separate owning-project commands below. The prior HEAD/worktree control logged 51/60 whitespace diagnostics in `/tmp/tenants-dw331-commit-5ize9f9c/format-head.log` and `format-worktree.log`, using folder mode. Both files remain byte-identical to entry.

Reviewed owning-project checks, executed separately:

```bash
dotnet format whitespace src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj --include src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionAvailabilityEvaluator.cs --no-restore --verify-no-changes --verbosity diagnostic
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --include tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorActionAvailabilityTests.cs --no-restore --verify-no-changes --verbosity diagnostic
```

Both exit **0** with no formatting diagnostics. Full diagnostic logs: `/tmp/dw-468-reviewed-production.log` and `/tmp/dw-468-reviewed-tests.log`. Each reports loading the root `.editorconfig`; the production project is checked directly rather than skipped as a reference. Both also report existing duplicate source entries for FrontComposer SourceTools' `AnalyzerReleases.Shipped.md` and `AnalyzerReleases.Unshipped.md`. These are workspace messages, so “zero formatting diagnostics” does not mean a warning-free workspace. No duplicate-source, audit or package policy was changed.

The reviewer separately ran the initial mixed-include command above with `--verbosity diagnostic`. It exited **0** but printed an MSBuild processing failure for the UI test project naming pinned `Microsoft.AspNetCore.SignalR.Common` 10.0.12 and advisory `GHSA-334f-vvp4-7g6p`, plus the duplicate-source messages. That earlier observation is preserved as a workspace-loading limitation. The later separate test-project log also retains this MSBuild/advisory failure at `/tmp/dw-468-reviewed-tests.log:81`, despite exit **0** and no formatting diagnostics. The production-project log does not contain that failure; both logs retain the duplicate-source messages. The passing workspace-free folder check below supplies independent formatting evidence for both files. This correction is a formatting verification, not evidence of a successful test-project build or a vulnerability-free dependency graph.

## Positive and negative controls

Executed `python3 /tmp/dw-468-probe.py`. The script copies the entry/current root `.editorconfig` and pinned `global.json` into isolated temporary directories, writes CRLF C# covering type, method, property/accessor and control-block braces, and runs the real formatter:

```bash
dotnet format whitespace --folder --include BraceProbe.cs --verify-no-changes --verbosity quiet
```

Results: old `all:warning` plus Allman exits **2**; corrected `all` plus Allman exits **0**; corrected `all` plus same-line braces exits **2**; corrected `all` plus bad indentation exits **2**. Every verify-only run preserves its input bytes. Removing only `--verify-no-changes` from the same command autoformats the same-line negative control with exit **0** and produces exactly the expected Allman CRLF bytes. A subsequent verify-only run exits **0** and preserves those bytes, proving idempotence. Individual commands, outputs and source fixtures: `/tmp/dw-468-format-probes-4maw35mc`; summary: `/tmp/dw-468-probe-results.json`. The runner exits **0** only if all expected outcomes and byte comparisons hold.

## Scope and review

No application code, build/package configuration, dependency, universal agent entry point, archive, sprint or earlier evidence file is edited. The pre-code AppHost baseline rule does not apply to this formatting-configuration/documentation correction; no runtime resources are started or changed. Behavioral tests, browser and package lanes are not rerun because no executable application/test source changes. Existing release and provenance holds remain; DW-468 closure does not establish release readiness or a clean repository-wide formatter run.

Entry state `/tmp/dw-468-entry-state.json` captures all 643 ledger record bodies, 3,555 owned tracked-file hashes, full HEAD and root gitlinks. The original DW-468 legacy detail and all seven previous status/disposition/verification/action fields are retained in its closure record; no unrelated ledger record changes.

## Review corrections and final integrity

Blind Hunter computed `min(floor(sqrt(9.176) + 1), 10) = 4` and returned four findings. All four were patched in verification/evidence: production-project coverage, distinction between formatting diagnostics and workspace messages, reproducible embedded probe code, and explicit disposition of the remaining historical-file formatting limitation. Finding-by-finding verdicts are in the owning spec; no finding was deferred and no application source or validation policy changed.

The embedded runner was executed as `python3 /tmp/dw-468-reproduce.py`, exit **0**, with artifacts `/tmp/dw-468-format-probes-4su0hykn`. The additional workspace-free command `dotnet format whitespace --folder --include src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionAvailabilityEvaluator.cs tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorActionAvailabilityTests.cs --verify-no-changes --verbosity minimal` exits **0**, checking both actual files independently of project-loading messages.

`python3 /tmp/dw-468-verify-ledger.py` passes: only DW-468 changes among all 643 record bodies; all seven prior fields and legacy detail remain; 341 done/302 open with 270 rows covering open IDs exactly once. Reconciled counts: 137 done, 215 actionable, 15 blocked, 26 awaiting-decision, nine accepted limitations and 37 uncertain. Its SHA-256 comparisons verify the exact two-line config/contributor correction and byte preservation of every application/test source, archive, sprint, previous evidence and other tracked build/package file; root gitlinks and entry HEAD are unchanged before commit. Story-gitlink and Git whitespace validation pass. The historical three-file lane remains exit 2 as documented above; no release-readiness claim follows from this closure.

Final independent recheck confirms all four original findings resolved; the diagnostic errata accurately supersede the earlier claims and no unresolved correction issue remains. The repository-pinned and installed commitlint CLI versions both equal **21.2.3**. `node_modules/.bin/commitlint --edit /tmp/tenants-dw468-commit-wvhc6you/message.txt --verbose` exits **0**, zero problems/warnings, validating the exact full message `chore(style): align formatter with required Allman braces`. Successful validation evidence is retained in `/tmp/tenants-dw468-commit-wvhc6you/commitlint.log`. Local commit follows the user's existing commit authorization and the invoked workflow; no push or dependency mutation is performed.

## Reproduce the controls from a clean checkout

Run from the repository root with the pinned SDK installed. This complete runner includes the fixture and byte assertions; only the old configuration is read from the immutable entry commit, so the probe remains valid after this repair is committed. Temporary outputs are diagnostic artifacts, not required source inputs.

```bash
python3 - <<'PY'
import json
from pathlib import Path
import re
import subprocess
import tempfile

ROOT = Path.cwd()
BASELINE = '4652bf3922b4c8d5d2877942ff7a18fdd7089ec9'
ARTIFACTS = Path(tempfile.mkdtemp(prefix='dw-468-format-probes-'))
ALLMAN = '''namespace FormattingProbe;

internal sealed class BraceProbe
{
    public int Value
    {
        get
        {
            return 1;
        }
    }

    public int Evaluate(bool enabled)
    {
        if (enabled)
        {
            return Value;
        }

        return 0;
    }
}
'''.replace('\n', '\r\n').encode()
SAME_LINE = re.sub(rb'\r\n[ ]*\{', b' {', ALLMAN)
OLD = subprocess.check_output(['git', 'show', f'{BASELINE}:.editorconfig'], cwd=ROOT)
NEW = (ROOT / '.editorconfig').read_bytes()
PIN = (ROOT / 'global.json').read_bytes()
COMMAND = ['dotnet', 'format', 'whitespace', '--folder', '--include', 'BraceProbe.cs', '--verify-no-changes', '--verbosity', 'quiet']

def run(name, config, content, expected):
    folder = ARTIFACTS / name
    folder.mkdir()
    (folder / '.editorconfig').write_bytes(config)
    (folder / 'global.json').write_bytes(PIN)
    source = folder / 'BraceProbe.cs'
    source.write_bytes(content)
    result = subprocess.run(COMMAND, cwd=folder, capture_output=True, text=True)
    output = result.stdout + result.stderr
    (folder / 'result.log').write_text(f'Working directory: {folder}\nCommand: {COMMAND!r}\nExit code: {result.returncode}\n{output}')
    assert source.read_bytes() == content, name
    assert result.returncode == expected, (name, result.returncode, output)
    if expected == 2:
        assert 'error WHITESPACE:' in output, (name, output)
    print(f'{name}: expected/actual exit {expected}/{result.returncode}', flush=True)
    return folder

run('old-allman', OLD, ALLMAN, 2)
run('corrected-allman', NEW, ALLMAN, 0)
negative = run('corrected-same-line', NEW, SAME_LINE, 2)
run('corrected-bad-indent', NEW, ALLMAN.replace(b'            return Value;', b'        return Value;'), 2)
command = [value for value in COMMAND if value != '--verify-no-changes']
result = subprocess.run(command, cwd=negative, capture_output=True, text=True)
(negative / 'autofix.log').write_text(f'Command: {command!r}\nExit code: {result.returncode}\n{result.stdout}{result.stderr}')
assert result.returncode == 0, result.stderr
assert (negative / 'BraceProbe.cs').read_bytes() == ALLMAN
result = subprocess.run(COMMAND, cwd=negative, capture_output=True, text=True)
(negative / 'idempotence.log').write_text(f'Command: {COMMAND!r}\nExit code: {result.returncode}\n{result.stdout}{result.stderr}')
assert result.returncode == 0
assert (negative / 'BraceProbe.cs').read_bytes() == ALLMAN
print('Autofix yields exact Allman CRLF bytes; idempotent verification passes.', flush=True)
(ARTIFACTS / 'summary.json').write_text(json.dumps({'artifacts': str(ARTIFACTS), 'old_allman': 2, 'corrected_allman': 0, 'corrected_same_line': 2, 'corrected_bad_indent': 2, 'autofix': 0, 'idempotence': 0}, indent=2))
print(f'Artifacts: {ARTIFACTS}')
PY
```
