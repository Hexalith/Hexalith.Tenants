---
title: 'DW-234/DW-251/DW-316 command failure and status localization'
type: 'bugfix'
created: '2026-10-16'
status: 'done'
route: 'dispatch'
baseline_commit: '6da0ed99ed87dd0c6811e9685e432f29afa151c5'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-234, DW-251 and DW-316 describe shared command validation, failure, rejection, status and unavailable results showing English to French operators.

**Approach:** Give affected results Tenants-owned whole-string EN/FR resource keys, preserved through typed state and resolved when rendered or announced.

## Boundaries & Constraints

**Always:** Preserve lifecycle, rejection codes, pending/retryable/ambiguity flags, identity, proof, admission, audit and projection timeouts. Keep shared status command-neutral, submissions operation-specific, and intentional consumer overrides/stronger membership-expiry guidance intact. Current keys precede fixed safe English compatibility fallbacks. Keep public signatures; keyed factories are additive. Follow the baseline's single-type C# rule.

**Never:** Display backend details, derive keys from them, broaden exact failure-code matching or concatenate translated fragments. No dispatch/retry changes, dependency/gitlink updates, gate relaxation, stage/commit/push/deploy, archive/sprint rewrite, unrelated closures or DW-323 work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Submission | Validation, rejection, HTTP/transport failure | Key for existing safe meaning | Classification retained |
| Status | Pending, transient fault, malformed response, mismatch, unknown enum | Localized status sentence | Evidence/retry rules retained |
| Unavailable | Concrete/interface-default entries | Localized result, including lookup | No dispatch |
| Snapshot | Fresh key, unknown status, zero-event role completion | Current key or localized fallback | Progress clears failures; expiry survives |
| Safety | Backend strings or previous failure | Approved DOM/live-region copy | No fabricated proof |

</frozen-after-approval>

## Code Map

No intent gaps or irreversible actions. Shared producers/models/resources/tests require dispatch. Paths are repository-relative.

- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`: validation, `Map*GatewayException`, `Safe*Rejection`, `GetStatusAsync`; recognized status rejections lack keys.
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs` and `ITenantCommandGateway.cs`: twelve remaining concrete literal results and five interface defaults; tracked entries already have keys.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`: fourteen types; five default-status arms discard keys; change-role already-applied copy is literal. Status factories need keyed counterparts.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `TenantsResources.fr.resx`: reuse `Tenants.Commands.Failure.*`; add missing whole messages under `Tenants.Commands` and operation-specific namespaces.
- `src/Hexalith.Tenants.UI/Components/Tenants/`: create/member/metadata consumers already preserve keys; configuration/lifecycle/global-administrator state deliberately overrides operation-specific copy.

## Tasks & Acceptance

**Execution:**

- [x] `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` — move its fourteen types to same-directory type-named files without API changes; add keyed status factories and localize six snapshot arms.
- [x] `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`, `TenantsResources.fr.resx` — pair whole messages; reused keys must match meaning/arguments.
- [x] `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`, `UnavailableTenantCommandGateway.cs`, `ITenantCommandGateway.cs` — key all affected validation/submission/rejection/status/unavailable results without changing classification or evidence checks.
- [x] `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs` — cover the matrix and all unavailable/default entries, retaining flags, identity, code and safety assertions.
- [x] `tests/Hexalith.Tenants.UI.Tests/State/TenantCommandSafeMessageTests.cs` and `Components/SupportSafetyBoundaryTests.cs` — cover matrix edges and real-resource EN/FR visible/announced messages; reuse `CultureBoundTenantsLocalizer`, include stale-key replacement, progress clearing, defaults and membership expiry.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md`, `release-readiness.md`, `docs/maintenance-backlog.md` — close these IDs after verification; reconcile counts, append evidence, preserve history.

**Acceptance Criteria:**

- Given EN or FR, when an affected result is rendered/announced, then its whole localized message appears without English fallback or key leakage on French paths.
- Given a localized failure, when state progresses or a newer result arrives, then the correct fresh message or intentional operation-specific override wins without changing command truth.
- Given existing command suites, when localization changes, then classification, admission, proof, audit and safety assertions pass.

## Implementation Notes

- All fourteen former `TenantCreateCommandModels.cs` types now have same-directory type-named files; public declarations are preserved. The gateway status DTO and existing gateway-test helper types are likewise in their own files.
- Added 80 paired EN/FR whole-message resources, including the previously missing tracked global-administrator removal resource. Producers preserve existing fixed safe English fallbacks and flags while carrying current keys.
- Added 164 cases across gateway, state and real-resource component coverage. Final focused 1,825/full UI 5,532 pass in both configurations; both unchanged Chromium lanes pass. See [exact evidence and remaining blockers](dw-234-251-316-validation-evidence.md).
- Parent acceptance audit verified all task changes and the five matrix rows against the complete unified diff (including untracked files), per-type semantic comparisons, executed Debug/Release test XML and saved output hashes. All 697 validation inputs and six compiled outputs remain unchanged. Independent review findings and disposition are recorded below.

## Spec Change Log

## Review Triage Log

Independent review used three context-free agents against the complete 713,160-byte unified diff, including every untracked file. Blind Hunter floor: `min(floor(sqrt(713.16) + 1), 10) = 10`; Edge Case Hunter returned no findings. Every Blind Hunter and Verification Gap finding is triaged individually below before grouping.

| ID | Verdict | Route | Evidence and disposition |
| --- | --- | --- | --- |
| BH-1 | medium | patch | Unavailable status lookup changed from a fixed `SafeMessage` to keyed-only output, unlike its prior contract. Restore the exact safe fallback alongside the key and pin both. |
| BH-2 | medium | patch | Configuration set/remove generic gateway failures select existing resources saying submission was unsafe, while the retained sentence says verification failed. Add operation-specific whole messages matching the original meaning; preserve the existing consumer resources. |
| BH-3 | medium | patch | Five unknown-enum arms newly retain incoming `status.SafeMessage`; these arms previously guaranteed a fixed safe sentence. Keep the fresh key while restoring that fixed compatibility fallback, with unsafe-input state assertions. |
| BH-4 | low | reject | Blank keys require a fabricated invalid caller result: every configured producer uses fixed nonblank keys that were checked in both resource sets. Null keys already select the tested localized fallback. No ordinary changed caller emits empty/whitespace keys; adding new validation/normalization guards is disproportionate to this negligible invalid-input case. |
| BH-5 | medium | patch | The new 44-case submission matrix accepts any translated key, so swapping operation keys would pass. Assert the intended operation/status key in each case. |
| BH-6 | medium | patch | The all-entry unavailable/default checks accept any translated key. Bind each concrete/default entry to its intended operation/tracked-dispatch key. |
| BH-7 | medium | patch | Existing transport/timeout submission cases check ambiguity and identity but not the newly assigned keys. Pin operation keys and shipped resources, extending the same test to JSON/cancellation catches. Same missing transport-localization assertion root cause as VG-1. |
| BH-8 | low | patch | Metadata already has real-gateway EN/FR coverage in `MetadataConsumerPrefersTheFreshLocalizedFailureKey` and the unsafe-detail corpus; the new rejection matrix still exercises membership meanings only through create. Add representative actual member-flow EN/FR gateway rendering coverage without duplicating the metadata coverage. |
| BH-9 | low | patch | Change-role live markup intentionally announces `StateText`, while the new explanation is visible separately. Preserve that consumer policy and make the test name/assertion distinguish the localized visible explanation from the existing whole-state announcement. |
| BH-10 | low | patch | The extracted internal status DTO has only a summary. Add constructor-property XML documentation, including optional identity/commit-proof semantics, as the baseline requires. |
| VG-1 | medium | patch | Pre-verified regression gap: deleting a plain transport catch key leaves both new HTTP-exception localization matrices and the old identity-only transport checks green. Extend the existing ambiguous-delivery test with exact operation-key and shipped-resource assertions; group with BH-7 only after this individual verdict. |

All accepted findings are direct code, documentation, or test corrections with no new public surface or lifecycle behavior. No intent gap, bad-spec loopback, or deferred-work entry was identified. All accepted patches are complete. The independent Blind Hunter rechecked every accepted correction and found no remaining concrete defect; the existing announcement policy is preserved. The correction-only Debug build and affected suites passed 654/654; parent Debug/maintained Release builds, focused 1,825/full UI 5,532, both unchanged Chromium runners and gitlink/whitespace checks pass after correction. The original 143-case qualification remains in the evidence JSON; final qualification contains 164 added cases and 80 resource pairs.


## Verification

- Build the individual UI test project in Debug with source references: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false`.
- Run built xUnit suites with `-class` filters for gateway/state/rendering/parity and affected consumers, then full individual-project UI MTP. Repeat maintained Release with `-p:UseHexalithProjectReferences=false`.
- Run the unchanged `tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` with `TENANTS_BROWSER_BUILD_CONFIGURATION=Debug` and `Release`; record exact commands/results and independent review.
- Before code edits, establish Aspire health with the local skill. Preserve broad Release SignalR and package-mode API blockers recorded in `dw-247-validation-evidence.md`; maintained passes do not establish release readiness.
