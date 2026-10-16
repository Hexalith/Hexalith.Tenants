---
title: 'DW-234/DW-251/DW-316 command failure and status localization'
type: 'bugfix'
created: '2026-10-16'
status: 'ready-for-dev'
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

- [ ] `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` — move its fourteen types to same-directory type-named files without API changes; add keyed status factories and localize six snapshot arms.
- [ ] `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`, `TenantsResources.fr.resx` — pair whole messages; reused keys must match meaning/arguments.
- [ ] `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`, `UnavailableTenantCommandGateway.cs`, `ITenantCommandGateway.cs` — key all affected validation/submission/rejection/status/unavailable results without changing classification or evidence checks.
- [ ] `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs` — cover the matrix and all unavailable/default entries, retaining flags, identity, code and safety assertions.
- [ ] `tests/Hexalith.Tenants.UI.Tests/State/TenantCommandSafeMessageTests.cs` and `Components/SupportSafetyBoundaryTests.cs` — cover matrix edges and real-resource EN/FR visible/announced messages; reuse `CultureBoundTenantsLocalizer`, include stale-key replacement, progress clearing, defaults and membership expiry.
- [ ] `_bmad-output/implementation-artifacts/deferred-work.md`, `release-readiness.md`, `docs/maintenance-backlog.md` — close these IDs after verification; reconcile counts, append evidence, preserve history.

**Acceptance Criteria:**

- Given EN or FR, when an affected result is rendered/announced, then its whole localized message appears without English fallback or key leakage on French paths.
- Given a localized failure, when state progresses or a newer result arrives, then the correct fresh message or intentional operation-specific override wins without changing command truth.
- Given existing command suites, when localization changes, then classification, admission, proof, audit and safety assertions pass.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

- Build the individual UI test project in Debug with source references: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false`.
- Run built xUnit suites with `-class` filters for gateway/state/rendering/parity and affected consumers, then full individual-project UI MTP. Repeat maintained Release with `-p:UseHexalithProjectReferences=false`.
- Run the unchanged `tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` with `TENANTS_BROWSER_BUILD_CONFIGURATION=Debug` and `Release`; record exact commands/results and independent review.
- Before code edits, establish Aspire health with the local skill. Preserve broad Release SignalR and package-mode API blockers recorded in `dw-247-validation-evidence.md`; maintained passes do not establish release readiness.
