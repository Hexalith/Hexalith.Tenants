# DW-367 accepted-disposition closure — 2026-10-29

Baseline: `03615cc91d940ca37b47fb13413a90a31151dd71`.
Scope: [DW-367 closure spec](spec-dw-367-accepted-disposition-closure.md).

The user explicitly answered “close” after being offered preservation or a new documentation-only closure plan. This supersedes open-for-visibility tracking for DW-367 alone. The [withdrawn five-item proposal](spec-dw-117-118-133-365-367-accepted-dispositions.md) remains unchanged; DW-117/DW-118/DW-133/DW-365 were already completed by separate approved work and remain untouched.

## Retained limitation and reopening condition

The [removal event](../../src/Hexalith.Tenants.Contracts/Events/UserRemovedFromTenant.cs) still contains only `TenantId` and `UserId`. [Story 5.5](spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md) requires deliberate role selection and forbids inferring roles from history or adding contract/history fields. The [current evaluator](../../src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartIntent.cs) requires `context.IntendedRole`; absent/invalid selections produce `ExplicitRoleRequired`.

Closing the tracking record accepts this policy. Historical role information remains unavailable. Reopen only for separately approved contract/product work requiring historical role evidence. The original finding and every replaced field are retained losslessly under `pre-accepted-close-` keys; the dependency requiring explicit contract/product approval is unchanged. No runtime fix, historical role recovery, deployment or release qualification follows from this closure.

## Verification

Run the maintained, read-only conservation check:

```bash
python3 _bmad-output/implementation-artifacts/dw-367-validate-closure.py
```

Expected: 657 unique numbered IDs, only DW-367 changed, 656 byte-identical unselected bodies, lossless reconstruction of the original DW-367 body, 652 done/5 open, and five unchanged remaining maintenance rows covering each open ID exactly once. Remaining dispositions: one actionable, one blocked and three uncertain; no open accepted limitation. Source/tests, dependency gitlinks, the archive, sprint tracking, owning Story 5.5 and withdrawn draft remain unchanged.

The entry worktree also contains unrelated dependency-documentation work: three modified tracked files and two untracked evidence files. A private entry snapshot captured their bytes and all other entry files before edits. The optional `--entry` check verifies those hashes and all nine gitlinks; the local snapshot is not a portable prerequisite for the baseline conservation check.

## Executed checks and concurrency limits

- `python3 _bmad-output/implementation-artifacts/dw-367-validate-closure.py --entry /tmp/dw367-closure-fcgsi1c1/entry.json` initially exited 1 at the strict `_bmad-output/project-context.md` hash comparison: that file changed externally after the entry snapshot. Later inspection found external updates in the existing EventStore spec and both dependency evidence files as well, plus a separate unnumbered ledger appendix. This command is not claimed as passing.
- The initial numbered-body parser also treated the appended unnumbered section as part of the last numbered record, DW-657. Parsing now stops at a level-two section heading, so independent unnumbered follow-ups do not masquerade as numbered-body edits. The external appendix remains untouched and outside this closure commit.
- Source/tests, dependency gitlinks, archive, sprint, original five-item draft and Story 5.5 remain unchanged against the immutable baseline. Unrelated concurrent dependency documentation is preserved as ongoing work, rather than claimed byte-identical to the entry snapshot.

No .NET, browser or runtime tests are required or claimed for this documentation-only closure.

The focused validator exits 0: 657 IDs, 652 done/5 open, five exact-covering maintenance rows, all 656 other numbered bodies preserved, and the full original DW-367 body reconstructed. The seven closure documentation links resolve, and `git diff --check` exits 0 with no output.

Pinned commitlint version check: `node node_modules/@commitlint/cli/cli.js --version` returns `@commitlint/cli@21.2.3`. Exact full commit candidate, including its final newline:

```text
docs: close DW-367 as an accepted disposition
```

`node node_modules/@commitlint/cli/cli.js --edit /tmp/dw367-closure-fcgsi1c1/commit-message.txt --verbose` exits 0: `found 0 problems, 0 warnings`. The commit uses this unchanged candidate.

## Independent review and correction checks

The one-shot blind-hunter review reported five validator findings; all were verified and corrected. The [spec triage log](spec-dw-367-accepted-disposition-closure.md) records each verdict and correction. Nothing is deferred from this review.

The corrected validator restores this closure's record/preface edits and requires every baseline ledger byte, including existing unnumbered sections and historical holds, to remain intact. Only the separately identified external dependency appendix is permitted afterward. It also checks the exact current resolution link/date/reopening condition, retained role-selection and missing-history facts, uses explicit UTF-8 decoding without newline conversion, and refuses optimized Python execution.

- Positive control and six in-memory `runpy` rejection controls pass: altered baseline unnumbered heading, removed closure link, weakened reopening condition, wrong current date, false historical-role recovery claim and a CRLF rewrite all fail validation. No worktree file is mutated by these controls.
- `env LC_ALL=C PYTHONUTF8=0 PYTHONCOERCECLOCALE=0 python3 _bmad-output/implementation-artifacts/dw-367-validate-closure.py` exits 0 with the same valid conservation report.
- `python3 -O _bmad-output/implementation-artifacts/dw-367-validate-closure.py` intentionally exits 1 with `Validation requires assertions; rerun without -O or PYTHONOPTIMIZE.` and prints no successful report.

The independent reviewer rechecked every original finding and independently ran the positive and six rejection controls: all five findings are corrected, with no residual problem within the correction scope. Nothing is deferred.

## Final staged acceptance

The validator was also run with staged document bytes substituted through a read-only `Path.read_bytes` adapter: it exits 0 with the same 657/652/5 conservation result, intact baseline unnumbered history and no external appendix in the staged ledger. Exactly the five closure paths are staged. The worktree retains the separate external appendix unchanged, SHA-256 `601932fde78181bddf8856484b6f7230265d30dafce815e294b7567d361b6b0a`; it is excluded from this commit.

All eight final closure documentation links resolve. `git diff --cached --check` exits 0 with no output. `git fetch origin main` succeeds, and local/remote main both resolve to the recorded baseline before this closure commit. No unrelated tracked/untracked dependency work is staged.
