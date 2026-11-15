---
title: 'Complete the .NET 11 source and validation graph'
type: 'bugfix'
created: '2026-11-13'
updated: '2026-11-15'
status: 'done'
route: 'dispatch'
baseline_commit: '401d926eb7d51bdf332696ed3f0fe39a812b3501'
review_loop_iteration: 0
context:
  - '/home/administrator/projects/hexalith/tenants/AGENTS.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
---

<frozen-after-approval reason="User explicitly requested fixing the mismatch by updating to .NET 11">

## Intent

Complete the .NET 11 migration so Tenants and its FrontComposer/Memories source dependencies restore, build and execute the current validation tools against the same framework. Fix .NET 11 API incompatibilities exposed by that migration, including generated asynchronous form validation.

## Boundaries & Constraints

Preserve existing native timeout work, authorization, validation ordering, cancellation and warnings-as-errors. Work in the repository owning each edit. Keep intentional netstandard2.0 analyzer/library targets, useful NET10_0_OR_GREATER guards and explicit historical/archive compatibility cases. Align current project targets, runtime images, CI executable paths and package-boundary assertions together. Do not hide mismatches with a global framework override or warning suppression. No business runtime launches, data changes, publication, commits, staging, pushes or nested submodule initialization. Existing uncommitted work is authorized context and must remain intact.

</frozen-after-approval>

## Code Map

- Root `global.json` already pins SDK 11.0.100; `Directory.Build.props` already targets net11.0. The central Builds catalog already selects Microsoft ASP.NET/Extensions 11.0.0. Do not replace those selections.
- `references/Hexalith.FrontComposer`: 26 runtime/sample/test project files still contain net10.0. Contracts/Schema retain their standard target; SourceTools stays netstandard2.0. SDK is already 11. FrontComposer owns generated form validation, active adopter/package scripts, runtime provisioning, quality workflow and package-boundary/governance assertions.
- FrontComposer `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` emits obsolete synchronous Validate inside SubmitAsync. Existing emitter tests protect admission, validation, cancellation and dispatch ordering. Async validation must not dispatch after disposal/cancellation or reuse disposed edit context.
- `references/Hexalith.Memories/Directory.Build.props` still defaults net10.0; five project overrides and `Directory.Build.targets` retain old framework/runtime images. Its SDK already pins 11. Align current CI/prerequisite/output-path checks with those declared targets.
- Root `tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-{live-routes,administrator-dialogs,navigation-focus,detail-audit-return,audit-layout,workspace-focus}.mjs` and `validate-status-filter-label-browser.mjs` launch or hash old net10 outputs. Their callers build net11; change all related binary/deps/hash paths coherently.
- Root integration source baseline fails six NU1201/NU1202 errors in these two dependencies. Package fallback separately fails generated Validate with CS0618. Exact baseline command/log and starting source hashes are in `/tmp/tenants-net11-repair-ng54_u4z/`.

## Tasks & Acceptance

- [x] FrontComposer runtime/sample/test project targets and current CI/tooling/runtime declarations — migrate to net11, preserving standard and archive compatibility.
- [x] FrontComposer CommandFormEmitter and its existing tests — emit awaited ValidateAsync with correct cancellation/disposal handling and prove no dispatch after canceled or invalid validation.
- [x] Memories Directory.Build.props, explicit framework overrides, Directory.Build.targets and current CI/runtime checks — align with SDK 11 and its net11 dependency graph.
- [x] Root seven browser validators — use current net11 assemblies consistently for execution and provenance.
- [x] Build the affected source graphs with warnings as errors, run focused generator/owner and Tenants tests, and retain commands/results with source/package distinctions. Record remaining external/runtime prerequisites without weakening gates.

Acceptance:
- Given SDK 11 and current source dependencies, when the Tenants integration/UI projects build in Debug source mode, then NU1201/NU1202 framework errors and generated-validation CS0618 are absent.
- Given a generated command form, when asynchronous validation completes, then invalid/canceled/disposed submissions do not dispatch and valid submissions retain their existing order and authority checks.
- Given only current net11 outputs, when current validators resolve binaries, then execution and evidence capture select those outputs without requiring stale net10 artifacts.
- Given retained archive fixtures and standard library/analyzer faces, when migration is applied, then those explicit compatibility contracts remain intact.

## Implementation Notes (original 2026-11-13 handoff)

User authorization to update to 11 supersedes earlier timeout-spec restrictions against changing the dependency cohort. This is a separate repair; historical timeout evidence is preserved. No intent gaps or irreversible operations were identified.

- FrontComposer and Memories current source targets/tooling are migrated; SDK and central package pins were already 11. NU1510 redundant framework references were removed and the Contracts JSON metadata call uses the generic API under its existing modern-framework guard.
- .NET 11 exposes BL0012/BL0013/BL0016 diagnostics. Actual component event/focus/authentication lifecycle issues are repaired. Nine member-scoped exceptions preserve deliberately propagated JS faults and one-shot fresh authorization reads, following Microsoft guidance for reusable wrappers; they do not conceal framework mismatches or change project-wide severities. The active FrontComposer exception census is updated while historical approvals/censuses remain unchanged.
- Full Tenants UI Debug/source build now passes with warnings as errors and zero warnings/errors. A filesystem-full failure was isolated separately; only disposable build outputs and downloadable HTTP cache were cleared. Cleanup of obsolete ignored net10 output directories is inventoried in `/tmp/tenants-net11-repair-ng54_u4z/obsolete-output-cleanup.json`.

- Final generator validation preserves the Blazor dispatcher across ValidateAsync and is covered by six actual generated-form scenarios. The SourceTools copied generator hash matches its owning build; 68 emitter/snapshot and 331 other focused owner controls pass (399 total). Seven owner Debug/source projects build cleanly with -warnaserror.
- After that final emitter correction, both Tenants UI and integration source graphs were rebuilt cleanly. All 8,082 UI tests passed again with zero failures/skips; the earlier 35 native/API controls passed and their transport implementation is unchanged. Actual Chromium status-label qualification passes using the migrated net11 executable. Prerequisite/adopter/CI/coverage/readiness focused tools add 100 passing cases.
- Four broader owner governance tests remain unqualified: one requires an unavailable nested Builds checkout; three use unchanged synthetic release fixtures omitting the Builds package catalog. Full readiness has a separate unavailable nested baseline prerequisite. No nested checkout was initialized, no guard relaxed, and no fresh Release/package proof is claimed. The historical net10 matrix remains matched to its dated evidence.
- Commands, counts, SHA-256 and proof boundaries are retained under `evidence/dotnet-11-source-graph-2026-11-13/`; raw XML is under `/tmp/tenants-net11-repair-ng54_u4z/`. Native timeout edits present before this repair remain byte-for-byte intact.

## Spec Change Log

## Review Triage Log

All three independent layers completed before triage. Each finding was checked against its actual source/callers; the verification-gap finding is accepted from its filed test search. All surviving fixes are private, bounded corrections within the existing migration goal and add no public API.

| Finding | Verdict | Route and evidence |
| --- | --- | --- |
| B1: adopter runtime verification still rejects SDK 11 | medium | patch: verify_runtime explicitly requires sdk.startswith("10.") while SDK_POLICY selects 11.0.100. Its existing mocked tests also encode SDK 11 rejection; update the detector and distinguishing SDK/runtime controls. |
| B2: Quality Gate 4a still selects ASP.NET 10 | medium | patch: the actual workflow awk selector is /^Microsoft.AspNetCore.App 10\./ and supplies its result to the migrated adopter host. Select the matching current framework and reject a missing selection. |
| B3: earlier asynchronous callbacks can lose the renderer context | medium | patch: PresentHiddenOutcomeAsync and ClearBlockedOutcomeAsync are awaited with ConfigureAwait(false) before ValidateAsync. A real asynchronous host callback can therefore remove the context that the new ConfigureAwait(true) would capture. Preserve/marshal the submit renderer continuation and exercise that callback. |
| B4: model edits during asynchronous validation can dispatch unvalidated values | medium | patch: OnEditContextFieldChanged only marks IsDirty, while dispatch reads the mutable _model after validation. The actual generated-form reproduction changes Name and calls NotifyFieldChanged while a validator awaits, then captures the changed name at dispatch. Invalidate that attempt on field changes or freeze its validated input; retain admission release and retry. |
| B5: pending authentication retains the previous account display | low | patch: the account observer updates _user only after its new task resolves. Withdraw old identity when a newer pending observation begins, preserving revision checks and anonymous markup. |
| B6: discarded account observer task hides unexpected failures | medium | patch: OnAuthenticationStateChanged discards InvokeAsync; fatal or renderer faults can consequently remain in an unobserved task. Observe completion and route unexpected component failures through Blazor while retaining fatal/disposal semantics. |
| B7: synchronous initial provider failure bypasses fallback | medium | patch: OnInitializedAsync invokes GetAuthenticationStateAsync before entering ApplyAuthenticationStateAsync's catch. Guard provider invocation as well as task completion and test synchronous failure. |
| B8: focus helper does not handle module disposal races | low | patch: DisposeAsync releases the still-cached _keyboardModule; the new best-effort focus catches cover JS/OCE but not ObjectDisposedException. Handle that demonstrated teardown failure without masking unrelated programming errors. |
| B9: Memories quickstart still installs/requires runtime 10 | medium | patch: the current heading, Windows/macOS/Linux instructions, runtime checks, walkthrough and recovery steps contradict the migrated CLI's required runtime 11. Correct current instructions; preserve historical evidence. |
| B10: current operator commands still launch net10 outputs | medium | patch: Memories CONTRIBUTING sandbox command, CounterFixture README and current Pact instructions name paths removed by the migration. Align those current executable instructions with their actual project targets. |
| B11: current framework/governance descriptions still require net10 | low | patch: the active source-generation concept and dependency-governance SDK/runtime paragraph contradict current project targets. Correct current guidance while preserving dated proof and retained external compatibility SDKs. |
| E1: field edits invalidate the pending validation result | medium | patch, same root cause as B4. The separate generated-form runner at /tmp/net11-edge-repro-4ddchtsy dispatches forbidden after an allowed value was captured by its pending validator; NotifyFieldChanged is explicitly raised. |
| V1: initial pending authentication race lacks regression coverage | low | patch: existing tests initialize the provider with a completed task, so they cannot distinguish stale initial completion after a newer anonymous notification. Add the filed pending-initial-read case and wait for initialization completion before checking markup. |
| R1: earlier EventStore package cohorts may lack snapshot-budget APIs | maybe-false | defer (medium, unverified): cumulative later tenant-access recovery code uses the APIs, while the current 5.1.0 source graph builds. Compiling production sources against every older advertised package is needed; synthetic marker fixtures are insufficient. |
| R2: retained net10 consumer uses the later recovery-inclusive inventory | medium | defer: the later recovery feature added a route/adapter after this migration; retained historical consumer qualification needs an explicit historical inventory. No historical inventory was changed by this resumed repair. |
| R3: canonical package guard rejects normalized version aliases | false | The canonical boundary intentionally pins exact published package-cohort strings; catalog artifact normalization is a separate compatibility check and does not authorize alternate publication selections. |
| R4: later RefreshTenantAccess command lacks a dedicated McpCli vector | medium | defer: the independent recovery command is present in the cumulative diff but absent from the 14 earlier operation vectors; qualify that command's wire routing in its owning work. |
| R5: field edits during authorization/BeforeSubmit bypass the validated revision | medium | patch: retain the validation revision/context through the final pre-submission check. Three real generated-form controls hold first authorization, confirmation and second authorization, assert no dispatch of changed values, and require a freshly validated retry. |
| R6: evaluator does not revalidate tenant snapshot after a policy await | medium | defer: pre-existing delayed authorization behavior; the accessor exposes Revalidate but this evaluator does not call it. Preserve the migration's existing authority contract and qualify the independent tenant-context transition before changing it. |
| R7: stalled authentication/policy tasks delay cancellation and subscription removal | medium | defer: both external awaits already lacked a cancellation-aware wait before this migration's observer; the existing explicit post-await cancellation contract is retained. Qualify an uncooperative provider/handler and cleanup before changing that contract. |
| R8: private Stryker preparation leaves an incomplete deterministic cache after failure | medium | defer: this independently added tooling fails closed on an existing incomplete directory, preventing retry without operator cleanup. Add owned staging/atomic completion and failure/cancellation retry controls in the tool's own maintenance work. |
| R9: Fluent time-editor tests do not exercise actual browser parsing | maybe-false | defer (medium, unverified): the later typed-field tests inspect emitted parameters/callbacks; real invalid, cleared and second-bearing native input must establish whether any visible defect exists. No parsing failure is claimed. |
| R10: Tenants should duplicate exact snapshot-budget boundary controls | false | The reused EventStore ProjectionSnapshotLimitsTests already covers exact encoded-byte acceptance, one-byte overflow and escaped/non-ASCII values; Tenants passes that event payload to the shared validator before length-preserving ordering. No distinct migration boundary failure was established. |
| G1: governance assumes a nested Builds catalog | medium | patch: resolve the same nested/sibling/parent-sibling catalog candidates as the effective package import; retain the actual package pin assertion and fail if no catalog exists. |
| G2: sealed-release fixtures omit their authenticated catalog | medium | patch: commit a minimal catalog into the synthetic Builds fixture before computing its immutable graph; keep all graph/seal checks and artifact/definition mutation negatives enabled. |
| G3: final UI verification races the bUnit invocation dictionary | medium | patch: the existing cancel-focus test polled a concurrently changing global invocation collection. Await its exact acknowledgement-focus handler and assert the single matching invocation, preserving the current-preview identity check. |


## File List

- `references/Hexalith.FrontComposer`
- `references/Hexalith.Memories`
- `references/Hexalith.EventStore`
- `references/Hexalith.Commons`
- `references/Hexalith.AI.Tools`
- `references/Hexalith.Builds`
- `references/Hexalith.PolymorphicSerializations`
- `references/Hexalith.McpCli`
- `references/Hexalith.Platform`

FrontComposer and Memories own the original framework/runtime/tooling migration; this resume adds only the FrontComposer source fixes listed below. All root pointer changes since the long-lived original baseline were inherited from independent synchronization work, including concurrent updates during verification. They are explicitly declared for audit and were preserved. The resumed agent performed no pointer updates. Root browser validators are the seven paths in the Code Map.


### Resumed verification on 2026-11-15

- `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` — refuse field/context changes through the final pre-submission boundary.
- `references/Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Generated/AsyncCommandFormValidationTests.cs` — three awaited authorization/confirmation regression cases with admission release and valid retry.
- `references/Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` and `references/Hexalith.FrontComposer/tests/ci-governance/stage_release_state.py` — close the documented catalog fixture blockers without nested initialization or weaker guards.
- Both existing CommandFormEmitter snapshots — match the final emitted validation lifetime.
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs` — observe the exact return-focus invocation rather than enumerating bUnit's mutable invocation dictionary during polling.
- Root evidence under `evidence/dotnet-11-source-graph-resume-2026-11-15/` — current revisions, commands, before/after proof and review disposition. The initial relocated-output UI attempt is an infrastructure failure; the conventional-layout disposable checkout is the acceptance lane.
- `references/Hexalith.EventStore`, `references/Hexalith.Commons`, `references/Hexalith.AI.Tools`, `references/Hexalith.Builds`, `references/Hexalith.PolymorphicSerializations`, `references/Hexalith.McpCli`, and `references/Hexalith.Platform` — inherited independent pointer changes since the original baseline, declared for the long-lived story audit. The resumed agent did not move these pointers. FrontComposer and Memories also have inherited pointer updates; this resume changes FrontComposer source only.


## Verification

- Individual Debug source builds of `tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj` and `tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj`, serialized with warnings as errors and `UseHexalithProjectReferences=true`.
- Individual FrontComposer generator, Shell/package-boundary and focused Memories checks selected from actual changed callers.
- Node syntax checks for changed validators and actual status-label fixture/browser control when available.
- `git diff --check` in each owning repository and root story-gitlink validation.

## Review Corrections Verified (original handoff)

All thirteen logged findings were patched with the existing implementation agent. The final source rebuilds pass with zero warnings/errors, regenerated Tenants UI passes 8,082 cases, transport evidence remains 35 passing cases, and final focused owner selections pass 411 cases. The review-specific scenarios/runtime controls pass 119 cases; the post-patch governance/fatal-taxonomy/exception-census controls pass nine cases. Actual Chromium net11 execution and the installed SDK/runtime identity probe pass.

All pre-existing tracked edits retain their original SHA-256. No source pointer moved, no nested checkout was initialized and no VCS publication occurred. Consolidated evidence: `evidence/dotnet-11-source-graph-2026-11-13/verification.md` and `verification.json`. At that original handoff, implementation was complete but status remained in-review because broader owner governance/readiness and package lanes were unqualified. The resumed completion below supersedes that workflow state.


## Resumed Completion — 2026-11-15

The remaining source-validation work is complete. R5 and the three verification corrections G1–G3 are implemented and verified. All 8,686 distinct final tests pass with zero failures/skips: 8,082 Tenants UI, 428 generated-form, 105 CI-governance and 71 emitter/snapshot cases. The 355 focused administrator cases overlap the UI total. Both Tenants source graphs and both owner test projects build with zero warnings/errors. All three negative controls fail against the earlier form behavior and pass in the corrected generated-form suite. Actual net11 Chromium qualification and seven validator syntax checks pass.

The previously recorded four governance fixture blockers are resolved without nested initialization or weaker gates. Seven independent findings from the cumulative review are recorded in the deferred ledger. Fresh Release/package and full business-topology readiness remain separate qualification work, not acceptance claims for this completed Debug source graph. Source changes remain uncommitted under the frozen scope. Final commands, counts, source identities and limitations: [completion verification](evidence/dotnet-11-source-graph-resume-2026-11-15/verification.md).
