# Tenants bootstrap acceptance — 2026-09-25

**Result: pass.** This independent Tenants consumer run rendered Tenants' generated projection and command surfaces against an exact FrontComposer candidate. This is render and registration evidence; it did not submit a command or contact EventStore. G-6 review and any D-7 decision remain with their owners.

## Candidate and package identity

- FrontComposer candidate SHA: `05d122005d328ec8f15ecd276058be29730288c2` (clean checkout).
- Tenants source SHA: `8431d9926ed655d32396f1f68973e0573ccc38e2`.
- Package version: `4.4.0-g6.05d12200`, locally packed from that FrontComposer checkout. The copied candidate runner checked each archive's exact ID, version, repository URL, and repository commit before restore. It then checked the restored package hashes in a fresh NuGet cache. The hashes were independently recomputed from the four candidate archives and matched.

| Package ID | Archive SHA-512 (Base64) |
| --- | --- |
| `Hexalith.FrontComposer.Contracts` | `VlwTn+q+L6VnKclhJ9tHaELmEz9iPPBAUXQO2PXLFQxixv5TYe4npJPGgByf0JWIf5btGX3QfjtijS0612+sMA==` |
| `Hexalith.FrontComposer.Contracts.UI` | `cYjyr97X7DGvQvX4/qlJMTc6VCUBThqveMa/K1C1WBRVvP7i3oX54gLFw0wHPwJCEuabr6k59ObhQX5Ut0fT4Q==` |
| `Hexalith.FrontComposer.Shell` | `36bPql6tWPX1xfXzub1vTHFRYPtXqvoiNTqzUmybv7BqQ2LHlYrj0VwV9ua/nsAuFO1dPximx/l9spNIc4/snw==` |
| `Hexalith.FrontComposer.SourceTools` | `9Ii4HX711fAMNXN2AktEaP+G4Q/scWnzkPh0XPOKaWeiA+bNZf7l4RXWbIRVWA8sBFFGZI9F8T1yHXIhLqhDNA==` |

## Runtime and method

- SDK: `10.0.401`; `Microsoft.NETCore.App`: `10.0.12`; `Microsoft.AspNetCore.App`: `10.0.12`.
- Copied `samples/AdopterProofKit/v1` and `eng/adopter_proof.py` from the candidate SHA into a temporary consumer layout. Replaced the kit's `Proof` project and assertions with a reference to the Tenants UI project and its own annotated `TenantsFrontComposerDomain`, `TenantSummaryProjectionView`, and `CreateTenantCommandForm`. No `ProofDomain`, `ProofProjection`, or `CreateProofCommand` remained in the consumer fixture.
- The success host called `AddHexalithFrontComposerQuickstart(o => o.ScanAssemblies(typeof(TenantsFrontComposerDomain).Assembly))` → `AddHexalithDomain<TenantsFrontComposerDomain>()` → `AddHexalithEventStore(...)`, then added Tenants' module services. The host retained the real EventStore command and query registrations. It used a fixed public test identity to pass the Shell's tenant/user gate. The endpoint was a redacted loopback placeholder; no domain payload was sent.
- The temporary host exposed the generated Tenants components at unique `/adopter/` routes to avoid collisions with Tenants UI's own routes. It disabled referenced web-project static assets and build compression to avoid duplicate Blazor assets, and serialized the build. These temporary build settings did not alter the four packages, their provenance verification, or the bootstrap and render checks. Raw build and host diagnostics stayed local.

## Assertions

| Check | Surface or expected behavior | Result |
| --- | --- | --- |
| Generated projection render | `TenantSummaryProjectionView@/adopter/tenants-projection`; heading and generated empty projection placeholder present | Pass |
| Generated command render | `CreateTenantCommandForm@/adopter/tenants-command`; generated form and required `Tenant ID` field present | Pass |
| Invalid bootstrap rejection | Missing Quickstart and misordered EventStore each produced the documented startup diagnostic before a page rendered | Pass |
| Empty registry render | Quickstart alone rendered `fc-home-empty-no-microservices` | Pass |

The redacted [machine result](tenants-bootstrap-acceptance.json) reports `package_identity.verified: true`, all four assertions `true`, `result: pass`, and `failure: null`. It contains no endpoint, token, tenant or user identifier, payload, stack trace, or machine-specific path.
