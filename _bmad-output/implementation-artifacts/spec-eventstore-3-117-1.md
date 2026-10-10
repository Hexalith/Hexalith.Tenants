---
title: 'Update EventStore dependencies to 3.120.0'
type: 'chore'
created: '2026-10-08'
status: 'in-progress'
route: 'oneshot'
revised: '2026-10-10'
review_loop_iteration: 0
baseline_commit: '9e9e04308ea1060010a64bb8b800e755c4aaf99d'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The user requests EventStore `3.120.0` from the committed `3.119.0` dependency catalog.

**Approach:** Update the single Builds-owned EventStore property so all 13 family packages use exactly `3.120.0`, refresh live authoritative evidence with supported tooling before selecting the version, and verify the Tenants package consumer. Preserve previous evidence and unrelated work. Do not stage, commit, push, move gitlinks, initialize nested modules, change package policy or perform product migrations.

</frozen-after-approval>

## Implementation Notes

### 2026-10-10 continuation to 3.120.0

- On 2026-10-10, the user revised the frozen target from `3.117.1` to `3.120.0`. Historical notes and evidence below remain records of the prior run. The current Builds checkout is clean at `2cf00028bbe563d80d4d12b5fb2054914f14fcb6` with a committed `3.119.0` catalog; the root worktree has unrelated existing edits that are preserved.
- The supported pre-edit EventStore-only audit cannot start feed discovery from the checked-in prior audit: it reports `unrequested family 'hexalith-folders' changed its catalog selection.` A supported full audit against committed Builds `2cf00028bbe563d80d4d12b5fb2054914f14fcb6` succeeded for 305 packages in complete mode: all 13 EventStore IDs are listed with latest stable `3.120.0` and selected baseline `3.119.0`.
- Updated only `HexalithEventStoreVersion` from `3.119.0` to `3.120.0` in the Builds-owned catalog. Byte checks confirm the file retains its UTF-8 BOM and 354 CRLF line endings. The central catalog validator passes for 305 entries and the SDK/tool exception validator passes for 15 entries.
- Post-edit supported audit generation fails as designed because the catalog is dirty relative to committed Builds `2cf00028bbe563d80d4d12b5fb2054914f14fcb6`. The checked-in audit validator exits 1 with 1,563 errors, including a catalog hash mismatch and unavailable historical revisions. Deterministic validation of the saved pre-edit full audit also exits 1 with 51 errors: one expected temporary catalog-path mismatch and 50 unavailable historical revision errors. The live discovery output is evidence of availability, not accepted audit provenance; governance remains pending.
- Both Release solution restores and warning-as-error builds pass with zero warnings/errors, including the standalone FrontComposer package lane. Six required test projects pass **5,228 tests** with zero failures/skips; the two focused domain-service HTTP tests pass. All nine owned production resolved assets contain only EventStore `3.120.0` packages. Debug/source and Release/package evaluations remain complementary at `net10.0`. Root and Builds whitespace checks and the current-baseline gitlink declaration pass.
- Review follow-up: the six AppHost tests pass. The package-mode command API runtime class passes 85 of 86 tests; its `Commands_endpoint_rejects_client_supplied_globalAdmin_extension_metadata` test expects HTTP 400 but receives 202 under both committed baseline `3.119.0` and target `3.120.0`. This exact pre-existing failure is already recorded in `deferred-work.md` (2026-10-08 pass 17). The source build uses EventStore checkout `37451b529ab21869fa4e2806968b5143ddeea14b` (`v3.119.0-5-g37451b52`), whereas the Release package build resolves published `3.120.0`; source tests do not prove target-package behavior.
- Current commands, results, asset hashes, source/package observations, pre-edit generator output, and raw logs are recorded in `dependency-refresh-eventstore-3.120.0-evidence-2026-10-10.json` and its adjacent ZIP. The prior `3.117.1` artifacts remain unchanged. No stage, commit, push, or gitlink movement was performed.

### Historical 3.117.1 run

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

- `_bmad-output/implementation-artifacts/dependency-refresh-eventstore-3.120.0-evidence-2026-10-10.json`
- `_bmad-output/implementation-artifacts/dependency-refresh-eventstore-3.120.0-raw-logs-2026-10-10.zip`
- `references/Hexalith.Builds`
- `references/Hexalith.Builds/Props/Directory.Packages.props` -- current single aligned EventStore pin change to `3.120.0` (historical `3.117.1` change was superseded by intervening commits).
- `references/Hexalith.Builds/Tools/package-version-audit.json` -- supported pre-edit registry evidence, subsequently committed externally.
- `_bmad-output/project-context.md` -- current prepared dependency and pending-governance facts.
- `_bmad-output/implementation-artifacts/spec-eventstore-3-117-1.md`
- `_bmad-output/implementation-artifacts/dependency-refresh-eventstore-3.117.1-evidence-2026-10-08.json`
- `_bmad-output/implementation-artifacts/dependency-refresh-eventstore-3.117.1-raw-logs-2026-10-08.zip`

## Completion Notes List

The 2026-10-10 continuation prepares and verifies the `3.120.0` package upgrade. Supported audit regeneration still requires a committed Builds catalog; this spec remains `in-progress` until that governed acceptance occurs. Its `baseline_commit` was reset to the root HEAD at continuation start so the gitlink check covers this run without attributing intervening historical pointer changes to it.

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

### 2026-10-10 continuation review

The Blind Hunter finding floor was 10; all 13 findings were checked against the current files. `HALT` reflects the existing no-commit constraint, so this continuation remains `in-progress`.

| ID | Verdict | Route | Evidence and result |
| --- | --- | --- | --- |
| R1 | high | HALT | A fresh root checkout still points to committed Builds `2cf0002` with EventStore `3.119.0`. The prepared `3.120.0` pin needs a governed Builds commit, accepted audit and root gitlink change; the frozen intent forbids those actions in this run. |
| R2 | medium | defer | Saved pre-edit audit validation has 50 unavailable historical Git origin revisions in addition to one temporary path mismatch. This inherited Builds provenance gap is recorded in `deferred-work.md`; no history or audit data was invented. |
| R3 | false | reject | The `retained` decision belongs to the committed `3.119.0` baseline audit. The `3.120.0` selection is an uncommitted preparation and is explicitly not represented as accepted owner review or audit evidence. |
| R4 | false | reject | The full refresh is used only for live EventStore candidate discovery after the incremental audit rejected Folders drift. No unrelated family decision is adopted or claimed as accepted. |
| R5 | low | reject | Validation against the saved temporary catalog introduces one known `catalogPath` mismatch, stated separately from the 50 historical revision errors. A canonical-path rerun requires a clean committed checkout and would not remove the inherited history errors. |
| R6 | medium | patch | Recorded EventStore source checkout `37451b529ab21869fa4e2806968b5143ddeea14b` (`v3.119.0-5-g37451b52`) and clarified that the source lane is not a build of the published `3.120.0` release. |
| R7 | medium | patch | Ran the six AppHost tests in Release package mode; all six pass. The exact command and log were added to the evidence archive. |
| R8 | medium | patch | Ran the command API runtime class in Release package mode: 85/86 pass. Its one HTTP 400-versus-202 test fails identically under `3.119.0` and `3.120.0` and was already recorded in deferred work on 2026-10-08; no new regression or duplicate ledger entry is claimed. |
| R9 | false | reject | The evidence claims restore, build, package resolution and focused HTTP tests only. It does not claim a running Dapr/AppHost smoke test or operational compatibility. |
| R10 | low | patch | Evidence now names `src/Hexalith.Tenants/Hexalith.Tenants.csproj` as the evaluated source/package project. The nine production assets and standalone solution build cover package resolution in the other owned projects without claiming equivalent per-project evaluation. |
| R11 | low | patch | Added the exact evaluation commands, UTC file capture times, exit results, root/Builds revisions, catalog hash and source revision to both observations. Archived the raw evaluation outputs. |
| R12 | false | reject | The assets record explicitly states that it is a final-restored-worktree snapshot after tests, not a per-build graph. Separate build logs and the standalone package build provide the build-lane results. |
| R13 | false | reject | Current notes and File List labels identify the `3.120.0` evidence separately from the named `3.117.1` historical artifacts; the spec explicitly says no accepted `3.120.0` audit exists. |

No commit, audit acceptance or gitlink movement was performed. The supplemental integration failure and governance blockers prevent a `done` status.
