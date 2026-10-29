---
title: 'Verify latest EventStore dependencies at 3.128.4'
type: 'chore'
created: '2026-10-08'
status: 'done'
route: 'oneshot'
revised: '2026-10-29'
review_loop_iteration: 0
baseline_commit: '1846c7128cf0f6b16f9e62f38032bcab78e26e38'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The user requests EventStore `3.120.0` from the committed `3.119.0` dependency catalog.

**Approach:** Update the single Builds-owned EventStore property so all 13 family packages use exactly `3.120.0`, refresh live authoritative evidence with supported tooling before selecting the version, and verify the Tenants package consumer. Preserve previous evidence and unrelated work. Do not stage, commit, push, move gitlinks, initialize nested modules, change package policy or perform product migrations.

</frozen-after-approval>

## Implementation Notes

### 2026-10-29 — Historical gitlink reconciliation

- The creation snapshot `1dffe336c9fde823b7b8ed6df9d63019dae64b90` records the true original baseline `1846c7128cf0f6b16f9e62f38032bcab78e26e38`. October 10 commit `ef4b7f2f99b2a60910b94481a198f591c51bfe5c` narrowed it to `9e9e04308ea1060010a64bb8b800e755c4aaf99d`. Restore the original start, broadening coverage rather than resetting it to a continuation's HEAD. The creation commit carries Builds, EventStore, Memories and Platform moves; its recorded external checkout observation is not the same as its committed Builds target.
- Retire active `gitlink_audit_ref: ef4b7f2f99b2a60910b94481a198f591c51bfe5c`. That endpoint's committed Markdown carries the narrowed baseline; using it with that baseline omitted the creation's four pointers. The direct validator never consumed this field; CI requires its baseline to match the endpoint snapshot. The full original-baseline-to-current audit now makes an endpoint exemption unnecessary. The former value and all earlier passing/failing observations remain historical evidence.
- The File List inventories every pointer in the full range through `73478ea3b21dbdc416d5b54b59260f851fbcd965`. These are **range contents**, not a claim that independent dependency updates belong to this story. The manifest records each first-parent pointer landing, its complete changed-file set, exact before/after objects and matches to this spec or its versioned evidence. Only the creation landing carries both this story's named artifacts and pointers; later story edits carry no pointers. No strict Story-Owned Gitlink Commits list is fabricated.
- No validator change is required. Required checks cover the default current-tree guard, CI range-selection contract, original and continuation history, all nine exact targets, declaration/target rejection controls, maintained gitlink suites, whitespace and preservation. Existing EventStore `3.128.4` verification is retained byte-for-byte; no application build or dependency update is repeated for this records-only repair. Final results are in `eventstore-gitlink-reconciliation-2026-10-29.md`, its history manifest and verifier.

### 2026-10-29 — Latest-package continuation (pre-reconciliation observations)

- The user's current instruction, “move to latest eventstore package,” supersedes the historical `3.120.0` target below. The frozen block and earlier evidence remain historical records. This continuation starts at Tenants `03615cc91d940ca37b47fb13413a90a31151dd71`; Builds is clean at the root-pinned `4775fb72e3ca3c05059830733e31a98995818cb6`.
- The shared catalog already selects stable `3.128.4` for all 13 EventStore packages. Supported live incremental discovery confirms every package is listed and its latest stable version is `3.128.4`; one family was refreshed and 150 were preserved. No dependency edit or shared audit replacement is necessary. The temporary live audit and checked-in audit both pass deterministic validation for 307 packages, 151 families and one source. The catalog validator passes 307 entries and the exception validator passes 16 entries. The old uncommitted-catalog/audit blocker is historical for this checkout; this observation does not adjudicate other deferred work.
- The default Release restore and warning-as-error build pass with zero warnings/errors. Captured all 10 owned production asset files immediately after that build; every resolved EventStore package is `3.128.4`. All seven per-project Release test suites pass: Contracts 771, Client 50, Testing 182, Sample 40, UI 8,011, AppHost 42 and Server 1,624 (10,720 total), plus five domain-service HTTP tests and 162 command-API runtime tests (10,887 combined). All have zero failures/skips and use the repository-selected Microsoft.Testing.Platform runner. Standalone Release restore and warning-as-error build also pass with FrontComposer and Memories forced to packages, with zero warnings/errors; a separate post-build capture of all 10 owned production assets likewise resolves only EventStore `3.128.4` packages. Results are recorded in the new `dependency-refresh-eventstore-3.128.4-evidence-2026-10-29.json` and matching raw-log archive.
- The required `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-eventstore-3-117-1.md` exits 1 because eight unrelated historical gitlink changes since the retained `9e9e04308ea1060010a64bb8b800e755c4aaf99d` baseline are undeclared. This continuation does not reset the historical baseline, claim ownership of intervening changes, or revert them. Its starting root revision is recorded separately above. The spec remains `in-progress` pending historical gitlink reconciliation; current-package verification is tracked separately.
- Supplemental historical audit with `--ref ef4b7f2f99b2a60910b94481a198f591c51bfe5c` passes. This validates only the retained historical endpoint; it does not turn the default current-range failure into a pass. Final owned-source diff and consumed FrontComposer/Memories checkout statuses are empty. Initial per-source and executed-binary snapshots were not archived; no byte-for-byte execution provenance is claimed. The standalone post-build integration dependency graph includes EventStore Testing and Testing.Integration `3.128.4`.
- No staging, commit, push, submodule initialization, gitlink movement, product migration or package-policy change is performed. The default solution build automatically produced six Commons package/symbol files; no explicit Tenants pack or isolated packaged Tenants consumer validation is claimed.


### 2026-10-21 — Evidence wording clarification (DW-532)

The historical 3.117.1 note below uses “byte-for-byte equivalent as parsed data.” Its package/family comparison used Python equality on parsed JSON: that proves **structural equality of parsed rows**, not byte equality of JSON serialization. The independent catalog replacement/hash checks establish preservation of every catalog byte except the intended version substitution, including its BOM and CRLF representation. These are separate guarantees; the original note and review R7 are preserved as historical evidence. This annotation does not accept either dependency audit or close the 3.120.0 governance work.

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

Current reconciliation evidence:

- `_bmad-output/implementation-artifacts/eventstore-gitlink-reconciliation-2026-10-29.md`
- `_bmad-output/implementation-artifacts/eventstore-gitlink-reconciliation-2026-10-29.json`
- `_bmad-output/implementation-artifacts/eventstore-gitlink-reconciliation-logs-2026-10-29.zip`
- `_bmad-output/implementation-artifacts/eventstore-gitlink-reconciliation-verify.py`

### Full historical range inventory

These declarations cover the original baseline through the reconciliation entry `73478ea3b21dbdc416d5b54b59260f851fbcd965`. They include independent range contents without claiming story ownership. Exact creation and intervening targets are recorded separately in the manifest; the table binds the current compared tree. A later independent update requires an explicit inventory correction rather than silently passing.

- `references/Hexalith.AI.Tools`
- `references/Hexalith.Builds`
- `references/Hexalith.Commons`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.McpCli`
- `references/Hexalith.Memories`
- `references/Hexalith.Platform`
- `references/Hexalith.PolymorphicSerializations`

| Range pointer | Original baseline | Reconciliation target |
| --- | --- | --- |
| `references/Hexalith.AI.Tools` | `3f194e17174994d308ec84af9ee2b5aa68674d0d` | `70dbf73e80787299c832d30d911b16b105ab194d` |
| `references/Hexalith.Builds` | `893db14b25843db140942d839e4d659584221315` | `4775fb72e3ca3c05059830733e31a98995818cb6` |
| `references/Hexalith.Commons` | `116d26815eb81e35b3c161e1799e5ee12805fc0a` | `3edfbdafc7d4fa915286dce8a37dc4cc7c06085a` |
| `references/Hexalith.EventStore` | `0dad344d37343f589d859d6d8d6701283122b338` | `4cf93f91e760e2d09e4fc64e1bd33a3c0a79a24d` |
| `references/Hexalith.FrontComposer` | `0e114214007c22f5cdbac21a6853cff4208340ee` | `612d23485d7e2845d22102621a38d6c904e1ea90` |
| `references/Hexalith.McpCli` | `e159f82b7528797fc245045625ff387d65294ba9` | `9ba8e5bd0e213501b39d33cedf46859566fc89a4` |
| `references/Hexalith.Memories` | `aac6d9054cb138881e6e49c8e48233553123ffce` | `daa06183c0c9baa8bda71df259a7264d34684e2a` |
| `references/Hexalith.Platform` | `f043a2f242762233091abdaa5bbe1ab777bd0f12` | `3659a44d86c08645215689eaeff8b3624d357dd5` |
| `references/Hexalith.PolymorphicSerializations` | `98de6e013840ece9f0fa7c68ab7dcdf2bba3b375` | `221ac4592ba908ae7661ba1fa9e2d1fcc23baee0` |

### Earlier continuation file records

Current continuation:

- `_bmad-output/implementation-artifacts/dependency-refresh-eventstore-3.128.4-evidence-2026-10-29.json`
- `_bmad-output/implementation-artifacts/dependency-refresh-eventstore-3.128.4-raw-logs-2026-10-29.zip`
- `_bmad-output/project-context.md` — current package and verification facts.
- `_bmad-output/implementation-artifacts/spec-eventstore-3-117-1.md` — current continuation, preserving historical intent and baseline.

- `_bmad-output/implementation-artifacts/deferred-work.md` — appended one unnumbered historical-gitlink reconciliation follow-up; unrelated concurrent ledger edits are preserved.

Historical file records (no dependency or gitlink edits in the 2026-10-29 continuation):

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

**2026-10-29 — Done:** Historical gitlink reconciliation passes from the restored original baseline. All four creation-carried moves and every intervening pointer landing are covered, with independent work explicitly attributed as range contents. The default guard, 23 snapshot/control checks, maintained suites (77 + 78 tests), tracked/new-file whitespace and scoped preservation checks pass. Independent review and recheck are complete, with all ten evidence findings addressed and no deferrals. EventStore `3.128.4` and its earlier 10,887 passing tests remain preserved evidence. Concurrent paging/UI changes are untouched; no fresh verification of that independent work is claimed. No dependency, gitlink, validator, stage, commit or push is performed.

### Pre-reconciliation completion observation (2026-10-29)

**2026-10-29:** Latest stable EventStore `3.128.4` is already selected in the committed root-pinned Builds catalog and verified by current live and deterministic audits. Both Release restore/build modes pass, as do 10,887 tests with no failures/skips. The spec remains `in-progress` because the retained historical gitlink declaration audit fails on eight intervening undeclared pointers. This run has no gitlink movement and preserves the original baseline rather than masking the audit gap. No dependency, shared audit, product or package-policy change is necessary; no commit is made under the retained no-commit constraint.

### Historical completion notes

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


### 2026-10-29 continuation review

Blind Hunter computed `min(floor(sqrt(1743.58) + 1), 10) = 10`. All ten findings were checked against the actual files and archived logs.

| ID | Verdict | Route | Evidence and result |
| --- | --- | --- | --- |
| R1 | medium | defer | The retained gitlink_audit_ref passes when supplied with --ref ef4b7f2f99b2a60910b94481a198f591c51bfe5c. The repository-required default command still exits 1 for eight intervening undeclared pointers. Current continuation has zero pointer diff; reconcile long-lived historical scope without rewriting baseline or claiming unrelated ownership. |
| R2 | low | patch | Dated the adjacent Aspire audit statement explicitly as historical 2026-10-10 evidence and pointed to current EventStore audit status; did not refresh unrelated version selections. |
| R3 | low | patch | Archived IntegrationTests assets after the standalone build: Testing and Testing.Integration both resolve package 3.128.4. Capture phase is explicit and does not claim this is the earlier executed-test graph. |
| R4 | low | reject | Build/test commands and passing logs substantiate this limited verification, while no binary-level execution provenance is claimed. Pre-overwrite hashes cannot be reconstructed without rebuilding and rerunning the suites. That exceeds a simple evidence correction and rarely affects normal use; documented the limitation instead. |
| R5 | low | patch | Archived final owned-source diff plus FrontComposer and Memories statuses, all empty. Initial clean root status was observed in the session but not saved as raw evidence; explicitly disclosed that initial per-source and binary snapshots are unavailable. |
| R6 | low | patch | The default solution build created six Commons package/symbol artifacts. Narrowed the limitation to no explicit Tenants pack or isolated Tenants package consumer validation and listed automatic dependency artifacts. |
| R7 | low | patch | Archived committed BOM/LF Git catalog and original BOM/CRLF checkout catalog, identified both hashes, and verified their bytes differ only by line endings. catalogRawSha256 binds Git bytes; catalogSha256 binds normalized audit bytes. |
| R8 | low | patch | Kept actual executed minimum-expected-tests 1 commands immutable. Added recommended repeat commands using each observed complete-suite count. The recorded results show 10887 tests, so no discovery loss is demonstrated in this run. |
| R9 | low | patch | Parsed numeric total/succeeded/failed/skipped counts per suite and derived the aggregate; collector asserts 10887 total/succeeded and zero failed/skipped. |
| R10 | medium | patch | Added a gate summary distinguishing passed package currency, both builds and tests, current no-pointer diff, passed historical endpoint audit and failed legacy baseline-to-current story check. Overall historical story remains in-progress. |

Eight evidence/documentation corrections were applied, one costly binary-provenance enhancement was rejected with explicit limits, and the historical gitlink reconciliation was deferred. Package verification passes; the historical story remains `in-progress`. No dependency, gitlink or product source changes are made.

### 2026-10-29 gitlink reconciliation review

Blind Hunter computed `min(floor(sqrt(229.084) + 1), 10) = 10`. All ten findings concern evidence verification, rather than incorrect pointer targets. They were checked and corrected within the authorized audit scope; none is deferred.

| ID | Verdict | Route | Evidence and result |
| --- | --- | --- | --- |
| G1 | medium | patch | An empty artifact list could vacuously pass preservation. Require the exact nine historical/current evidence paths, with no omissions or duplicates. |
| G2 | medium | patch | Reconciliation log bindings were recorded but not checked. Require validation metadata, exact command inventory, unique ZIP entries, archive hash/CRC, original snapshots and every log digest/result. |
| G3 | medium | patch | A manifest-only frozen-intent hash was insufficient. Compare against both archived entry bytes and the committed entry Git snapshot. The creation's older 3.117.1 intent was legitimately superseded; restoring it would contradict the retained negotiated 3.120.0 intent. |
| G4 | medium | patch | Historical package hashes lacked independent anchors. Require exact entry Git-blob equality for seven committed artifacts, and entry-worktree snapshot hashes for the two uncommitted 3.128.4 artifacts. Retain package archive hash/CRC/embedded-manifest checks. |
| G5 | medium | patch | A recorded pre-repair command depended on an ephemeral snapshot. Archive original spec/ledger/context bytes and replay the old document against fixed current/historical trees. Also check preservation of every pre-existing spec section. |
| G6 | medium | patch | Retired provenance fields were checked only for ancestry. Match the creation baseline to creation Git, narrowed baseline to retired endpoint Git, and former endpoint to the original entry snapshot. |
| G7 | medium | patch | A mutable association filter could omit story work. Require this exact story identity and its complete versioned evidence path set, independently of the manifest's history rows. |
| G8 | medium | patch | Recorded subjects and attribution prose were unchecked. Match subjects to Git and require qualified categories derived from actual artifact/pointer matches. Independent range contents cannot become ownership claims. |
| G9 | medium | patch | Historical replay could silently use changed tooling. Bind both imported production validators to their entry Git blobs and recorded hashes before import. |
| G10 | low | patch | `git diff --check` omits untracked new evidence. Explicitly check final newlines and trailing whitespace in the new Markdown, JSON and Python files. |

The verifier now includes 23 snapshot/control checks, including both archived pre-repair observations. Independent recheck confirmed all ten findings addressed and all earlier counterexamples rejected, with no remaining concrete issue. Final preservation checks confirm unchanged HEAD, index bytes, gitlink entries, validators and dependency configuration. The ledger differs from entry only by this task's single resolution line, and project context only by the scoped reconciliation sentence. Concurrent paging/UI edits remain untouched. All required audit checks pass; the spec is done without staging, committing or pushing.
