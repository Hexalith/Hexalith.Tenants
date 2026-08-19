---
title: 'Story 1.6: Read-Only Tenant Configuration'
type: 'feature'
created: '2026-07-22'
status: 'done'
baseline_revision: '91d59806c5731661fdf156cfa833293adc69c98d'
baseline_commit: '020b099a5170b98fef177ce42b1d5d106e0dc81d'
review_loop_iteration: 1
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
warnings:
  - oversized
---

<intent-contract>

## Intent

**Problem:** Raw tenant configuration currently reaches Razor-facing state, full-dictionary counts and groups reveal hidden population, a deny-list decides value display only after state construction, and mutation controls live inside the nominally read-only region.

**Approach:** Add a deployment-owned, server-side BFF policy that composes a positive safe configuration model before component state, then render it in a strict read landmark while preserving Epic 3 set/remove flows in a separate sibling management landmark.

## Boundaries & Constraints

**Always:** Ordinary access comes only from literal `(tenantId, authenticated sub, prefix)` grants; role alone grants nothing. Comparisons are ordinal and case-sensitive without trimming or normalization. Prefix `P` authorizes only `P` and `P.*`, with the longest matching grant naming the namespace. A proven global administrator gets the sole namespace wildcard, while values still require exact-full-key positive `DisplaySafe` registration. Both gates must pass before any key or value enters Razor-facing state. Valid-empty policy means safe empty; missing, malformed, duplicate/conflicting, or indeterminate policy/authentication means unavailable. Derive all summaries, filters, counts, announcements, targets, and empty states from the safe model.

**Block If:** Implementation cannot distinguish proven global administrator, proven non-administrator, and indeterminate principal evidence; cannot bind policy failures into a safe unavailable result without startup failure; or cannot preserve set/remove projection confirmation using proof-only evidence.

**Never:** Use tenant role, visible keys, entered prefixes, client claims, blacklist-negative classification, masking, or absence in a projection as authorization/display approval. Never expose hidden literals, counts, policy contents, raw configuration, tokens, metadata, correlations, or stack traces through state, DOM, accessibility, clipboard, logs, telemetry, exceptions, or tests. Do not change public query contracts/endpoints, absorb Story 1.10 transport work, modify `references/`, add packages, or weaken Epic 3 command safety.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Ordinary grant | Valid policy, proven non-admin, grant `P` | Only exact `P`/`P.*` entries with exact `DisplaySafe` approval become safe rows | Role or visible keys add no scope |
| Global admin | Proven admin, valid safe-key registry | All namespaces are eligible; only positively approved full keys become rows | Indeterminate evidence is unavailable |
| Mixed/undefined data | Authorized and hidden keys; unregistered values | Hidden/undefined entries are wholly absent; derived text describes safe rows only | No placeholder or reveal control |
| Policy states | Valid-empty versus missing/malformed/conflicting policy | Empty renders authorization-safe empty; invalid renders localized unavailable | No raw fallback or exception detail |
| Refresh/commands | Last-confirmed safe model; set/remove lifecycle | Failed refresh may retain only safe data as degraded; commands use policy scope and proof-only projection evidence | Unknown/stale/degraded truth fails management closed |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` -- currently retains raw `TenantDetail.Configuration` across ready/304/degraded paths.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs` -- current two-state claim reflection cannot represent configuration policy outcomes.
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantDetailSnapshot.cs` -- currently exposes the raw detail contract to Razor consumers.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` -- derives raw counts and returns raw configuration as command evidence.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor` -- performs late deny-list filtering and embeds mutation flows.
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/` -- existing Epic 3 flows require relocation and proof-only inputs.
- `tests/Hexalith.Tenants.UI.Tests/` -- has historical redaction/command tests but no positive-policy boundary suite.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Tenants.UI/Services/Configuration/ITenantConfigurationPrincipalResolver.cs`, `TenantConfigurationPrincipalResolver.cs`, `TenantConfigurationPrincipalEvidence.cs`, `TenantConfigurationReadPolicyOptions.cs`, `TenantConfigurationPrefixGrantOptions.cs`, `TenantConfigurationReadPolicyProvider.cs`, `TenantConfigurationSafeComposer.cs`, and `TenantConfigurationServiceCollectionExtensions.cs` -- add one-type-per-file typed binding, non-startup-fatal semantic validation, three-outcome principal reflection, longest-prefix ordinal authorization, exact-key positive approval, safe result types, and idempotent registration. Resolve one authenticated identity from `IHttpContextAccessor` during SSR or `AuthenticationStateProvider` through FrontComposer's `CircuitServicesAccessor` during interactive activity; subject, system scope, and administrator claims must come from that same identity. Unsupported/malformed role encodings, multiple authenticated identities, scalar values on collection-shaped policy sections, whitespace/trailing-dot prefixes, and duplicate/conflicting grants or safe keys are unavailable without logging claim/policy data.
- [x] `src/Hexalith.Tenants.UI/Program.cs`, `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs`, and `src/Hexalith.Tenants.UI/appsettings.json` -- register the same composition seam in standalone/embedded hosts and declare a valid-empty `Tenants:ConfigurationReadPolicy`; do not use `ValidateOnStart` or repository-default grants.
- [x] `src/Hexalith.Tenants.UI/State/TenantDetail/TenantConfigurationSafeRow.cs`, `TenantConfigurationSafeModel.cs`, `TenantConfigurationManagementContext.cs`, `TenantConfigurationProjectionProof.cs`, and `TenantDetailSnapshot.cs` -- introduce immutable non-sensitive read/management/proof DTOs with defensive copies and no public constructor/`with` path capable of accepting raw configuration. Snapshots and last-confirmed state contain only factory-sanitized tenant detail plus safe configuration; no caller-owned mutable collection is shared across read and management boundaries.
- [x] `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`, `ITenantQueryGateway.cs`, `ITenantsBffComposition.cs`, `TenantsBffComposition.cs`, and `UnavailableTenantQueryGateway.cs` -- compose immediately after the server response and before snapshots. Require every detail/proof payload and every reusable prior snapshot to match the requested tenant ordinally. A `304` must perform one unconditional detail read before applying current principal/policy because safe rows cannot reconstruct newly approved raw entries. A degraded/failed refresh may retain only a same-tenant last-confirmed safe model reauthorized against current policy. Set/remove proof is available only from a matching-tenant, projection-backed, explicitly current, non-degraded response; missing payload, `304`, unknown/stale metadata, exceptions, or mismatches fail closed.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` and `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor` -- derive summary/state only from the safe model; make the read landmark inspection-only with `tenants-config-read-*` selectors, Fluent/FrontComposer composition, multi-expand grouping, distinct truth states, literal Unicode support, and accessible responsive overflow. Remove action columns, command parameters, and `LegacyConfigurationDisplaySanitizer` from the read path. Accessible value text must include the approved literal value rather than replacing it with a key-only label.
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/TenantConfigurationManagement.razor`, `SetTenantConfigurationFlow.razor`, `RemoveTenantConfigurationFlow.razor`, and `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` -- host commands in a sibling landmark in the same expanded Configuration accordion. Re-resolve principal and deployment policy immediately before each command dispatch; revoked/indeterminate scope blocks the gateway call. Set accepts an exact literal full key so grant `P` covers both exact `P` and `P.*`; remove uses only current safe keys. Unavailable management never also renders valid-empty copy; remove launch eligibility includes lifecycle/truth/command availability. Each action accessible name identifies its literal key, and each focusable overflow region has a localized accessible name. Projection callbacks/snapshots retain proof status rather than raw dictionaries while preserving preview, focus, locking, duplicate prevention, confirmation, audit, and recovery behavior.
- [x] `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `TenantsResources.fr.resx` -- provide whole-string parity for read-only, valid-empty, policy-unavailable, recovery, truth-state, and management copy.
- [x] `tests/Hexalith.Tenants.UI.Tests/Services/Configuration/TenantConfigurationReadPolicyTests.cs`, `Services/Gateways/TenantQueryGatewayTests.cs`, `Components/TenantDetailSurfaceTests.cs`, `Components/SetTenantConfigurationFlowTests.cs`, `Components/RemoveTenantConfigurationFlowTests.cs`, `TenantsUiCompositionTests.cs`, `TenantConfigurationEndToEndTests.cs`, and `DomainUiFluentConformanceTests.cs` -- cover the matrix plus: circuit/SSR principal resolution from one identity; every supported positive administrator claim shape; malformed JSON-like and cross-identity claims; scalar/duplicate/conflicting policy; `a`/`a.`/`ab`/`A`; exact-key set; mutable input defense; wrong-tenant detail/prior/proof; unconditional `304` policy recovery; degraded payload with same-tenant prior safe state; proof gateway query shape and matching/nonmatching/missing/304/stale/degraded/unknown/exception outcomes; submission-time policy revocation; mutually exclusive empty/unavailable management; remove fail-closed lifecycle/truth/command states; target-specific accessible names; literal accessible values; Unicode/confusables; and hidden-state absence. Add one outer tenant-detail test that runs an authenticated circuit/SSR principal plus configured policy through raw gateway response, BFF composition, snapshot, and rendered DOM/accessibility state. Preserve EN/FR parity, focus/live regions, responsive/forced-colors/reduced-motion hooks, and stable data-independent selectors.

**Acceptance Criteria:**
- Given mixed authorized, hidden, and undefined-policy configuration, when the BFF composes tenant detail, then only entries passing literal prefix authorization and exact-key positive approval reach snapshot/component state and no observable UI or diagnostic surface reveals the rest.
- Given an ordinary member without an explicit grant, a proven administrator, or indeterminate principal evidence, when scope is resolved, then the outcomes are respectively no scope, namespace wildcard, and unavailable without role-based or deny-list fallback.
- Given valid-empty or invalid policy, when tenant configuration renders, then the user sees respectively an EN/FR authorization-safe empty state or distinct unavailable/recovery state without raw counts or error details.
- Given a failed refresh after a qualifying safe result, when the page updates, then only the last-confirmed safe model may remain and it is labeled degraded; no raw response or false current state appears.
- Given the Configuration accordion is inspected, when read and management landmarks render, then the read landmark contains no mutation/action/command lifecycle controls while the sibling management landmark preserves all set/remove safety and fails closed on unsafe truth or scope.
- Given long, markup-like, bidi, Unicode, case-confusable, and empty-segment literals, when authorized safe rows render at supported widths and assistive modes, then literal meaning, namespace relationships, keyboard order, focus, truth state, and stable selectors remain usable without data-derived IDs.
- Given focused and full verification runs, when tests and Release builds complete, then policy, gateway, bUnit, command-regression, localization, accessibility, responsive, and Fluent conformance checks pass with no warnings.
- Given an SSR request or interactive Blazor circuit, when configuration identity is resolved, then exactly one authenticated identity supplies the subject, system scope, and administrator evidence, and missing, stale, cross-identity, or malformed evidence renders configuration unavailable.
- Given a gateway detail/proof payload or reusable snapshot from a different tenant, when configuration is composed, refreshed, or confirmed, then the result fails closed and no cross-tenant detail, safe row, management scope, or command confirmation is produced.
- Given deployment policy changes while a page is open, when a `304` refresh or command submission occurs, then the BFF re-fetches/re-authorizes against current policy and principal before exposing new rows or dispatching a command.
- Given projection proof metadata is missing, stale, degraded, non-projection-backed, or for another tenant, when set/remove confirmation runs, then the command remains unconfirmed with support-safe recovery.
- Given an unavailable management context or a safe removable-row table, when it renders for keyboard and assistive technology, then unavailable and valid-empty states are mutually exclusive, every action names its target, overflow regions are named, and lifecycle-ineligible actions cannot open.

## Spec Change Log

- 2026-07-22: Preserved the prior human resolution selecting deployment-owned per-tenant/per-subject prefix grants, the explicit global-administrator wildcard, exact-key positive display approval, and sibling read/management landmarks.
- 2026-07-22: Review pass 1 found that the plan left trust-boundary mechanics underspecified. Amended principal resolution, malformed-policy handling, immutable/factory-safe state, same-tenant cache/proof validation, unconditional `304` policy recovery, current projection evidence, submission-time reauthorization, exact-key set semantics, mutually exclusive management states, accessible action/value/overflow behavior, and outer-surface verification. This avoids cross-circuit/cross-tenant authorization, stale false confirmation, policy-revocation races, raw-state bypass, and inaccessible or contradictory UI. KEEP: ordinal longest-prefix authorization, exact-key positive display approval, valid-empty versus unavailable, the safe read/management/proof split, raw configuration removal before Razor state, strict read-only plus sibling management composition, EN/FR parity, stable selectors, existing command lifecycle behavior, unchanged public endpoints/contracts, and the clean 977-test/Release-build first-pass baseline.

## Review Triage Log

### 2026-07-22 — Review pass
- intent_gap: 0
- bad_spec: 23: (high 12, medium 11, low 0)
- patch: 0
- defer: 1: (high 1, medium 0, low 0)
- reject: 3: (high 0, medium 0, low 3)
- addressed_findings:
  - `[high]` `[bad_spec]` Require circuit-aware principal resolution instead of `HttpContext`-only evidence.
  - `[high]` `[bad_spec]` Bind subject, system scope, and administrator claims to one authenticated identity.
  - `[high]` `[bad_spec]` Treat unsupported or malformed role claim encodings as indeterminate.
  - `[high]` `[bad_spec]` Reject scalar values on collection-shaped policy sections.
  - `[high]` `[bad_spec]` Make safe rows, prefixes, and management targets immutable and defensively copied.
  - `[high]` `[bad_spec]` Remove public snapshot construction paths that can accept raw configuration.
  - `[high]` `[bad_spec]` Reject tenant-detail payloads whose tenant differs from the request.
  - `[high]` `[bad_spec]` Reuse prior snapshots only for the same literal tenant.
  - `[high]` `[bad_spec]` Re-fetch raw detail after `304` so current policy can add, remove, or recover safe rows.
  - `[high]` `[bad_spec]` Reject projection-proof payloads for another tenant.
  - `[high]` `[bad_spec]` Require current, non-degraded, projection-backed proof metadata.
  - `[high]` `[bad_spec]` Reauthorize current policy and principal immediately before command dispatch.
  - `[medium]` `[bad_spec]` Support setting the exact key `P` as well as descendants `P.*`.
  - `[medium]` `[bad_spec]` Keep unavailable management distinct from valid-empty remove targets.
  - `[medium]` `[bad_spec]` Include lifecycle eligibility in remove-launch availability.
  - `[medium]` `[bad_spec]` Give each remove action a target-specific accessible name.
  - `[medium]` `[bad_spec]` Name focusable horizontal-overflow regions.
  - `[medium]` `[bad_spec]` Preserve approved literal values in accessibility text.
  - `[medium]` `[bad_spec]` Exercise the complete authenticated principal/policy/raw-response/render chain at the outer tenant-detail surface.
  - `[medium]` `[bad_spec]` Directly test set/remove proof gateway query and fail-closed paths.
  - `[medium]` `[bad_spec]` Test positive administrator claims through the real BFF bridge.
  - `[medium]` `[bad_spec]` Test degraded payload refresh with a same-tenant previous safe snapshot.
  - `[medium]` `[bad_spec]` Test remove management across unsafe lifecycle, truth, policy, and command states.

### 2026-08-19 — Review pass 2
- intent_gap: 0
- bad_spec: 0
- patch: 16
- defer: 2
- reject: 5
- addressed_findings:
  - Contained circuit-service lookup faults and failed closed when an active circuit lacks its authoritative authentication provider.
  - Proved static-SSR request identity precedence over a distinct injected provider and added malformed-request coverage.
  - Exercised every supported positive administrator claim shape through the real resolver and BFF composition bridge.
  - Made configuration filtering ordinal, case-sensitive, and literal for whitespace-only input, with key-specific regression coverage.
  - Made a second `304` without same-tenant prior evidence return `Unknown` rather than fabricate a degraded state.
  - Added same-provider policy reload coverage across `304` expansion, revocation, and invalid-policy outcomes.
  - Propagated cancellation from the concrete unavailable projection-proof methods and mutation-tested those concrete methods.
  - Corrected the stale localization test double so the full UI suite verifies shipped EN/FR resource parity.
  - Persisted executed verification evidence and the unchanged `Hexalith.Memories` solution-build blocker below.
- deferred_findings:
  - Require explicit set/remove projection-proof implementations from every `ITenantQueryGateway` implementation instead of retaining pre-existing interface defaults.
  - Adopt a shape-preserving policy schema or discriminator because `IConfiguration` cannot distinguish JSON `[]` from scalar `""` while preserving the valid-empty default.

## Design Notes

Raw configuration may exist only transiently inside the server-side gateway/composer. A safe snapshot carries immutable approved rows plus non-sensitive policy state; management receives immutable proven prefixes/safe targets, while projection comparison occurs server-side and returns only proof status from matching, current projection evidence. `LegacyConfigurationDisplaySanitizer` may remain transitional inside command previews only and is never read approval.

Use FrontComposer's established SSR/circuit principal resolution pattern; do not invent a second browser claim bridge. A `304` cannot be safely recomposed from already-filtered rows, so retry once without ETag and apply current policy to the returned raw detail. Re-check current policy/principal before dispatch, not only when the page snapshot was created. KEEP the first pass's strict read/management separation and proof-only command state, but close cross-tenant, stale-evidence, and TOCTOU paths during re-derivation.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -warnaserror -m:1 -nr:false` -- expected: zero warnings/errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Services.Configuration.TenantConfigurationReadPolicyTests` -- expected: all policy/composer boundaries pass.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests` -- expected: safe ready/304/degraded behavior passes.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Components.TenantDetailSurfaceTests` -- expected: strict read surface and state behavior pass.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Components.SetTenantConfigurationFlowTests` -- expected: set-flow behavior passes.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Components.RemoveTenantConfigurationFlowTests` -- expected: remove-flow behavior passes.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.DomainUiFluentConformanceTests` -- expected: Fluent/FrontComposer governance passes.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.TenantConfigurationEndToEndTests` -- expected: authenticated principal/policy/raw-response/render chain passes at the tenant-detail surface.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` -- expected: full UI suite passes.
- `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false` -- expected: zero warnings/errors.

**Executed 2026-08-19:**
- UI Release warning-as-error build passed with 0 warnings and 0 errors.
- Focused suites passed without skips or not-run cases: policy 78/78, gateway 400/400, tenant detail 154/154, set flow 55/55, remove flow 32/32, Fluent conformance 51/51, and circuit/static-SSR end-to-end 2/2.
- Full UI suite passed 2010/2010; localization parity is included.
- `git diff --check` and `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration-2.md` passed; no `references/` pointer changed.
- The exact solution build reached and built the Tenants projects, then failed only in unchanged `references/Hexalith.Memories`: `CS0618` at `TenantExportService.cs:134`, `TenantExportService.cs:417`, and `TenantIsolationVerifier.cs:785`; `SER301` at `ReleaseDedupKeyIfOwnedActivity.cs:35`; and aggregate `MSB4181` from `Directory.Solution.targets:3`. The story forbids modifying `references/`.

## Suggested Review Order

**Identity trust boundary**

- Start here: selects one authoritative identity and contains provider failures.
  [`TenantConfigurationPrincipalResolver.cs:12`](../../src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs#L12)

- Separates static SSR from active circuit evidence without cross-source fallback.
  [`TenantConfigurationPrincipalResolver.cs:45`](../../src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs#L45)

**Projection refresh and proof**

- Re-fetches every conditional hit before applying current deployment policy.
  [`TenantQueryGateway.cs:145`](../../src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs#L145)

- Keeps double-304 first loads unknown instead of fabricating retained evidence.
  [`TenantQueryGateway.cs:163`](../../src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs#L163)

- Reauthorizes same-tenant retained state and drops rows when current proof fails.
  [`TenantQueryGateway.cs:2106`](../../src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs#L2106)

- Makes the unavailable proof seam explicit and cancellation-aware.
  [`UnavailableTenantQueryGateway.cs:12`](../../src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantQueryGateway.cs#L12)

**Literal read surface**

- Applies ordinal case-sensitive filtering while preserving literal whitespace and Unicode.
  [`TenantConfigurationView.razor:175`](../../src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor#L175)

**Boundary verification**

- Proves request identity precedence and real-BFF administrator claim handling.
  [`TenantConfigurationReadPolicyTests.cs:345`](../../tests/Hexalith.Tenants.UI.Tests/Services/Configuration/TenantConfigurationReadPolicyTests.cs#L345)

- Exercises expansion, revocation, and invalid policy across one reloaded provider.
  [`TenantQueryGatewayTests.cs:1250`](../../tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs#L1250)

- Pins case-sensitive key matching and literal whitespace filters in rendered UI.
  [`TenantDetailSurfaceTests.cs:3717`](../../tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs#L3717)

- Runs circuit and static-SSR identities through raw response to rendered DOM.
  [`TenantConfigurationEndToEndTests.cs:43`](../../tests/Hexalith.Tenants.UI.Tests/TenantConfigurationEndToEndTests.cs#L43)

**Peripherals and follow-up**

- Aligns the removal-flow localizer double with shipped resource truth.
  [`RemoveTenantMemberFlowTests.cs:888`](../../tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs#L888)

- Records interface-contract and configuration-shape debts outside this patch.
  [`deferred-work.md:1068`](deferred-work.md#L1068)
