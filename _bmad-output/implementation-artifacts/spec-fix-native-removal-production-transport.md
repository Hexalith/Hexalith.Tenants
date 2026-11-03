---
title: 'Qualify native revocation through the canonical production owner transport'
type: 'bugfix'
created: '2026-11-03'
status: 'in-review'
baseline_commit: 'bcd923781e97cf7ea6391f73eb0b3d5469c254c4'
route: 'dispatch'
context:
  - '/home/administrator/projects/hexalith/tenants/AGENTS.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
---

<frozen-after-approval reason="User authorized fixing CI failures and bypassing human validation">

## Intent

Repair the live native-removal regression's outdated test setup. It currently constructs a generic client with a mocked legacy system-bound assertion, no signed stream proof source, and raw Dapr metadata reads. Qualify the same committed native removal and retained granting projection using the actual production owner gateway, canonical scope, genuine workload assertion issuer, bound RSA proof, durable catalog authority, and final native metadata observation. Preserve every security/refusal assertion and the requirement to run without skips.

## Boundaries & Constraints

Do not change production trust or validators, fake native readers/metadata/authority, copy candidate activation identities into a pretend authority, read unrelated private runtime state, warm or retry the test into passing, or weaken expected results. Use fixture-owned ephemeral credentials and actual independently produced owner observation/custody. Never print secret keys/configuration or put RSA material in process arguments. Preserve parallel-owned changes to Program.cs and EventStore source; do not edit them or package-version pins. No staging, commits, pushes, nested submodule initialization, or skill loading under references.

</frozen-after-approval>

## Code Map

- `tests/Hexalith.Tenants.IntegrationTests/AuthenticatedTenantsRouteTests.cs`, native-removal method: replace only obsolete transport/metadata setup and standalone source-read admission. Retain persisted Redis snapshot, committed grant/removal sequence assertions, native proof/head checks, and final refusal while retained projection still grants.
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs` and its authenticated subclass: fixture owns actual Tenants Dapr endpoint and startup. Existing static `WithBootstrapStartupSettingsAsync` API/tests must remain compatible.
- `src/Hexalith.Tenants.AppHost/Program.cs` already accepts secret `Parameters:tenants-source-private-key` and derives the verifier from the effective key. Supply a generated RSA2048 PKCS8 key through scoped inherited `Parameters__tenants-source-private-key` only during base initialization, restore prior environment in finally; do not modify AppHost.
- `src/Hexalith.Tenants/Authorization/TenantOwnerSourceGatewayRegistration.cs` and `TenantOwnerSourceGateway.cs` implement production catalog/Dapr transport, canonical platform translation, assertion issuance and RSA proof. Reuse their actual registrations/types.
- `SourceCatalogBootstrapEvidence.ReadCommittedAsync` returns real private custody bound to committed Redis attempt/root. Read bounded private `tenants.observation.json`, derive `ObservedRouteCatalogHost.Identity`, verify exact Tenants host/app and authority coordinates plus membership in actual activation.RequiredHosts, then use the real SDK RouteCatalogAuthority/DaprRouteCatalogStateStore through AddEventStoreRouteCatalog and AddEventStoreRouteCatalogObservation. Re-read the actual observation on validation; never use TenantOwnerCatalogTestAuthority.
- Parallel work added EventStore commit 9aa4e5d690db3cce734ae38942b05c23c8380bac, a configurable GatewayNativeSourceMetadataReader deadline; package publication is pending. The root agent will coordinate producer/consumer version updates.

## Tasks & Acceptance

- [x] Add fixture-owned key/startup composition and a CreateNativeOwnerReadServicesAsync helper. Use Development host environment, existing demo JWT issuer/signing key, workload tenants, exact Tenants sidecar, genuine AddEventStoreWorkloadAssertionIssuer, AddTenantOwnerSourceGateway and actual catalog authority where the active profile requires it. Keep fixture key private and restore inherited environment on success and exception.
- [x] Construct authoritative stream reader with a bounded 30-second deadline and native proof required; use GatewayNativeSourceMetadataReader with bounded 30-second observation deadline once the supporting SDK API is selected. Package-mode compilation must wait for the root-approved published SDK; source-mode compilation against the actual pending SDK may be reported separately.
- [x] Pass actual invocation scope/configuration to a fresh TenantsGlobalAdministratorAuthority for each handler execution. Wrap standalone stream reads in ExecuteNativeSourceReadAsync so the same durable admission protocol applies. No raw metadata fallback in any lane.
- [x] Add or extend narrow existing fixture composition/environment tests to protect key privacy/restoration and canonical production registration. Build the focused graph and run applicable non-live tests; identify any package-availability blocker precisely. Do not launch overlapping AppHosts/private runtimes while other work is active; root will run the actual isolated live qualification.

## Implementation Notes

- Current failing CI: run 41562173225 at bcd923781e97cf7ea6391f73eb0b3d5469c254c4, job 125675093178. Native-removal test fails beforeSource.IsAuthoritative with source-unavailable. This transport defect is independent of production native-reader deadlines and the separate restart bootstrap-command-timeout.
- Legacy fixture 3.128.4 supports inherited environment and ExtraAppArgs but no builder service accessor; use environment rather than arguments. Its timeout diagnostics print resource states/errors, and AppHost marks this parameter secret. Preserve support-safe output.
- Inspect current files before each edit and preserve concurrent changes. Keep new C# types in separate object-named files. Return concise changed paths, exact checks, limitations, and any remaining package publication dependency to root.

## Verification

- Source-mode integration graph against actual pending EventStore 9aa4e5d690db3cce734ae38942b05c23c8380bac built with explicit `-p:HexalithEventStoreVersion=4.0.3`, zero warnings/errors. Fixture isolation: 10 passed; existing startup compatibility: 2 passed. No AppHost or private runtime was launched.
- Package-mode compilation attempted before publication returned NU1102 for unavailable 4.0.3 (nearest 4.0.2); this is not successful package qualification. The source proof is separate. Actual observation trust and live removal qualification remain required in the authenticated current-source CI lane before release.

## Review Triage Log

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| Blind 1: unconfigured alternate profile | false | The only helper caller is the canonical native-removal qualification. Its blocking matrix prepares and enables the actual catalog profile; generic CI relocates that complete class to the prepared lane. An unconfigured catalog is deliberately refused. Composing a transport without catalog admission would weaken the protocol. |
| Edge 1: profile disabled or cohort absent | false | The only live caller is scheduled in the prepared canonical profile. Missing identity is a deliberate refusal for unsupported, unconfigured runtime use. The SDK requires catalog admission for this transport, so an alternate path without admission is not valid. |
| Blind 2: metadata deadline differs from production | medium; patch | The helper selects 30 seconds while actual Program.cs selects 8 seconds. Align this observation deadline with production; keep the authoritative stream reader's actual 30-second bound. |
| Blind 3: custody rejection paths not exercised | medium; patch | The added CreateServices test bypasses the new observation registration. Add focused negative tests of the real registered observer for foreign identity/coordinates/membership and missing or changed private evidence. Unit data must be explicitly synthetic and must never substitute for the actual live authority proof. |
| Blind 4: per-read durable lease sequence assertions | low; rejected | The changed test invokes the real SDK admission scope, and the existing SourceCatalogOwnerProfileTests verifies actual durable outstanding leases reach zero. Adding another persistence inspection sequence to this native-removal test duplicates the SDK admission contract and does not identify a missing caller edge. |
| Blind 5: argument-privacy check uses empty new ProcessStartInfo | low; patch | Its empty argument-list assertion cannot observe real launch arguments. Delete that vacuous assertion; retain actual child environment inheritance, RSA import, and success/exception restoration checks. Source inspection confirms startup only changes inherited environment and never writes ExtraAppArgs. |
| Blind 6: unit JWT parsing does not authenticate | false | The real issuer registration is unchanged SDK code, and the required live native read authenticates the generated token through the actual verifier before the retained-projection refusal proof. A wrong signing key/lifetime cannot pass that live qualification. The unit test's claim is registration and canonical claims, not independent cryptographic qualification. |
| Verification gap: revalidation callback can cache stale observation undetected | medium; patch | Add a focused test of the actual registered observer which first accepts private evidence, then changes or removes that file and requires the next observation to refuse. Do not fake native transport or authority, and do not report this synthetic negative test as runtime acceptance. |

## Review Follow-up

- Preserve all actual custody checks and their live source. Expose only an internal registration seam if needed to exercise the existing real observer callback with explicitly synthetic, private temporary test evidence. Add focused rejection/freshness cases and keep all cryptographic/runtime admission unchanged.
- Match production metadata observation bound: 8 seconds, while authoritative stream reads retain 30 seconds. These are test configuration corrections, not changes to production trust or deadlines.
