---
title: 'Complete the .NET 11 source and validation graph'
type: 'bugfix'
created: '2026-11-13'
status: 'in-review'
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

## Implementation Notes

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


## File List

- `references/Hexalith.FrontComposer` — owned framework, form generator and validation-tool migration; source edits only, no pointer movement.
- `references/Hexalith.Memories` — owned framework/runtime/tooling migration; source edits only, no pointer movement.
- Root browser validator paths listed above.

## Verification

- Individual Debug source builds of `tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj` and `tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj`, serialized with warnings as errors and `UseHexalithProjectReferences=true`.
- Individual FrontComposer generator, Shell/package-boundary and focused Memories checks selected from actual changed callers.
- Node syntax checks for changed validators and actual status-label fixture/browser control when available.
- `git diff --check` in each owning repository and root story-gitlink validation.

## Review Corrections Verified

All thirteen logged findings were patched with the existing implementation agent. The final source rebuilds pass with zero warnings/errors, regenerated Tenants UI passes 8,082 cases, transport evidence remains 35 passing cases, and final focused owner selections pass 411 cases. The review-specific scenarios/runtime controls pass 119 cases; the post-patch governance/fatal-taxonomy/exception-census controls pass nine cases. Actual Chromium net11 execution and the installed SDK/runtime identity probe pass.

All pre-existing tracked edits retain their original SHA-256. No source pointer moved, no nested checkout was initialized and no VCS publication occurred. Consolidated evidence: `evidence/dotnet-11-source-graph-2026-11-13/verification.md` and `verification.json`. Implementation of the requested source migration is complete; status remains in-review because the separately documented broader owner governance/readiness and package lanes are unqualified.
