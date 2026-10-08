---
title: 'Update EventStore dependencies to 3.117.1'
type: 'chore'
created: '2026-10-08'
status: 'in-progress'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '1846c7128cf0f6b16f9e62f38032bcab78e26e38'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The user requests EventStore `3.117.1` after the verified `3.117.0` dependency refresh.

**Approach:** Update the single Builds-owned EventStore property so all 13 family packages use exactly `3.117.1`, refresh live authoritative evidence with supported tooling before selecting the version, and verify the Tenants package consumer. Preserve previous evidence and unrelated work. Do not stage, commit, push, move gitlinks, initialize nested modules, change package policy or perform product migrations.

</frozen-after-approval>

## Implementation Notes

- This is a small follow-up to the completed dependency refresh. Existing dirty root documentation and Builds audit evidence are authorized prior work and must be preserved. Root baseline is `1846c7128cf0f6b16f9e62f38032bcab78e26e38`; Builds begins at `893db14b25843db140942d839e4d659584221315`.
- Change `references/Hexalith.Builds/Props/Directory.Packages.props` only at `HexalithEventStoreVersion`; preserve its BOM/CRLF. Tenants' `Directory.Packages.props` is a read-only import shim.
- Before editing, run `pwsh -NoProfile -File ./Tools/audit-central-package-versions.ps1 -PriorAuditPath ./Tools/package-version-audit.json -ChangedFamily hexalith-eventstore` in Builds. Verify all 13 IDs list stable `3.117.1` and preserve other families. Keep the inherited audit bytes separately for provenance.
- The supported generator binds a committed catalog and rejects dirty declarations. After the pin edit, attempt supported regeneration and audit validation, record exact results, and never invent accepted audit provenance. If blocked, leave the concrete tested upgrade prepared and the spec in progress pending governed catalog acceptance; no commit is authorized.
- Reconcile the current dependency facts in `_bmad-output/project-context.md` with the prepared version and governance state. Record commands, owning directories, revisions, counts and raw-log digests in a new `dependency-refresh-eventstore-3.117.1-evidence-2026-10-08.json` and preserve raw logs beside it. Keep all `3.117.0` evidence historical and unchanged.
- Verify both specified Release solution restore/build gates with warnings as errors, including an explicit standalone FrontComposer package build. Run the six existing required test projects individually with current package-mode builds, plus the two focused `DomainServiceEndpointsTests` HTTP tests. The default UI retains its documented FrontComposer-source exception; do not repeat the already-established forced-FrontComposer UI failure lane or claim it passes.
- Confirm all owned production resolved assets use only EventStore `3.117.1`, Debug/source and Release/package evaluations remain complementary at `net10.0`, and central-catalog/SDK-exception, gitlink declaration and whitespace checks pass. Neither local pack nor a new isolated consumer probe is required or claimed.
- Initial file snapshots and command logs are under `/tmp/tenants-eventstore-31171-7i0s_4xs`. The earlier eight deferred issues and completed spec are historical context; this follow-up neither fixes nor duplicates their ledger entries.

- Supported pre-edit incremental audit succeeded: all 13 IDs list stable `3.117.1`; 145 other families and every non-EventStore package row are byte-for-byte equivalent as parsed data. Prepared the single property change to `3.117.1` with catalog BOM/CRLF preserved and reconciled the project context without altering prior evidence.

- A concurrent external commit advanced the Builds checkout from `893db14b25843db140942d839e4d659584221315` to `2169b866912786536c6e338440b8436e5b826c88`. Its diff changes only generated audit evidence, with no catalog/build-source change. This run neither made that commit nor moved a gitlink; the observed checkout transition is declared below from this follow-up's root baseline.

| Root gitlink | Baseline to observed checkout | Attribution |
| --- | --- | --- |
| `references/Hexalith.Builds` | `893db14b25843db140942d839e4d659584221315 -> 2169b866912786536c6e338440b8436e5b826c88` | External audit-only commit; preserved. |

- Supported post-edit audit generation exits 1 because the catalog is dirty relative to the current committed Builds revision. Audit validation exits 1 with a catalog hash mismatch and 13 unmatched accepted selections (14 errors). The truthful pre-edit snapshot still selects `3.117.0` and lists `3.117.1`; no provenance check was bypassed. Central catalog validation (304 entries) and SDK/tool exception validation (15 entries) pass.


## File List

- `references/Hexalith.Builds`
- `references/Hexalith.Builds/Props/Directory.Packages.props` -- single aligned EventStore pin change.
- `references/Hexalith.Builds/Tools/package-version-audit.json` -- supported pre-edit registry evidence, subsequently committed externally.
- `_bmad-output/project-context.md` -- current prepared dependency and pending-governance facts.
- `_bmad-output/implementation-artifacts/spec-eventstore-3-117-1.md`
- `_bmad-output/implementation-artifacts/dependency-refresh-eventstore-3.117.1-evidence-2026-10-08.json`
- `_bmad-output/implementation-artifacts/dependency-refresh-eventstore-3.117.1-raw-logs-2026-10-08.zip`

## Completion Notes List

The observed Builds transition is external and preserved, not performed by this implementation: `references/Hexalith.Builds` moves from baseline `893db14b25843db140942d839e4d659584221315` to checkout `2169b866912786536c6e338440b8436e5b826c88`. That audit-only commit records the live candidate evidence consumed by the prepared `3.117.1` update. No root index or gitlink was staged or changed by this run. The initial declaration check did not recognize the table under Implementation Notes; adding the required File List records this observed scope without reverting concurrent work.

Both Release solution restores and warning-as-error builds pass with zero warnings/errors, including standalone mode with FrontComposer forced to packages. Fresh package-mode tests pass: Contracts 144, Server 825, Client 50, Testing 181, Sample 39 and default UI 3,986 (**5,225 required tests**); two focused administrator HTTP tests also pass, with zero failures or skips in these lanes. All nine owned production resolved assets use EventStore `3.117.1`; Debug/source and Release/package evaluation retains `net10.0` and complementary edges. Final gitlink declaration and root/Builds whitespace checks pass. Raw command logs, prepared/committed catalogs and audit snapshots are preserved in the adjacent ZIP, with SHA-256 bindings. The spec remains in progress pending governed catalog acceptance; no local pack, new isolated-consumer probe or forced-FrontComposer UI test success is claimed.


## Review Triage Log

The one configured reviewer completed the focused continuation review with finding floor `min(floor(sqrt(48.602) + 1), 10) = 7`. Each finding was classified separately before routing.

| ID | Finding | Verdict | Route | Evidence and result |
| --- | --- | --- | --- | --- |
| R1 | Source/package evaluations omit direct archive entries. | low | patch | Both JSON observations were archived but consumers had to infer filenames from temporary paths. Added explicit `archiveEntry` fields and verified each archived byte hash. |
| R2 | Recorded asset hashes cannot be rechecked from summary-only archives. | low | patch | The original ZIP held the extracted EventStore list, while the recorded hashes bound full workspace assets that could later change. Archived all nine original `project.assets.json` files and verified their byte hashes and EventStore `3.117.1` package entries. |
| R3 | Assets snapshot lacks capture phase/time and can be mistaken for both build-lane graphs. | low | patch | The final snapshot was collected after test restores, including the default UI source exception, rather than after each earlier solution build. Added capture time, repository/catalog bindings, and explicit final-restored-worktree scope. Build commands and passed logs remain the evidence for their respective build modes; final assets are not attributed to those earlier capture points. |
| R4 | Evaluation observations omit timing, revisions and catalog binding. | low | patch | The separate evaluation collector originally stored only command/output/properties. Re-evaluated both read-only modes and recorded UTC capture time, canonical root/Builds revisions and prepared-catalog SHA-256, without rebuilding applications or repeating tests. |
| R5 | Actual SDK and PowerShell versions are absent. | low | patch | The configured SDK permits patch roll-forward, so the pin alone does not identify the installed toolchain. Captured `dotnet --version` and `$PSVersionTable.PSVersion.ToString()` with command, output bytes, hashes and archive entries. |
| R6 | Preserved prior evidence is referenced without byte hashes. | low | patch | The historical path alone cannot detect later edits. Added SHA-256 bindings and archived copies of the prior final JSON and its matching raw-log ZIP, preserving their original bytes. |
| R7 | The spec's parsed-row equality claim uses byte-equality wording. | low | defer | The verification compares parsed package/family objects with Python equality; it establishes structural equality, while exact byte preservation is separately established for the catalog by replacement/hash checks. The wording conflates these guarantees. This finding's fix edits a spec, so the one-shot review routes it to deferred work; the original sentence remains historical and this row states the actual proof. |

The six independent evidence omissions were corrected locally without changing dependency pins, product behavior, validation policy or consumer results. One cosmetic spec wording item was appended to deferred work. All observed source/package evaluation and final-assets archive hashes pass. Governance acceptance remains the existing disclosed blocker, so the spec remains `in-progress` rather than claiming a completed audited update. No staging or commit was performed.
