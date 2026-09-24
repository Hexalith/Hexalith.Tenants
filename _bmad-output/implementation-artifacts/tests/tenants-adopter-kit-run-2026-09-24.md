# Tenants adopter kit run — 2026-09-24

**Result:** The published FrontComposer package-consumer fixture passed. **G-6 named-adopter acceptance remains open** because this run rendered the kit's `Proof` domain, not generated Tenants projection and command surfaces.

## Identity and method

- Accountable Tenants maintainer: `jpiquot`, confirmed in [Tenants issue #48](https://github.com/Hexalith/Hexalith.Tenants/issues/48#issuecomment-5817694948).
- Tenants checkout: `2729deefdde44dd89ded1e410abb9ef767b0fb9d`.
- FrontComposer candidate: `1b5b6353532c0c6f3c3ff8b04ac3a3804b8dccc4`.
- Four locally packed candidate packages: `Hexalith.FrontComposer.Contracts`, `Hexalith.FrontComposer.Contracts.UI`, `Hexalith.FrontComposer.Shell`, and `Hexalith.FrontComposer.SourceTools`, all at `4.4.0-ci`. The runner verified each nuspec's ID, version, repository URL, and exact candidate commit, then verified the restored package hashes.
- Runtime: .NET SDK `10.0.401`; `Microsoft.NETCore.App` and `Microsoft.AspNetCore.App` `10.0.12`.
- The kit was copied from the exact candidate commit into an isolated clean consumer directory and run with the documented `eng/adopter_proof.py` procedure. Its EventStore endpoint was a loopback placeholder because the kit tests rendering and registration; no domain command or backend request was executed.

## Result

The allowlisted, redacted [runner result](tenants-adopter-kit-run-2026-09-24.json) records `pass` and these four true assertions:

| Assertion | Rendered or checked surface | Result |
| --- | --- | --- |
| Generated projection | `ProofProjectionView@/proof/proof-projection` | Pass |
| Generated command | `CreateProofCommandPage@/commands/Proof/CreateProofCommand` | Pass |
| Bad bootstrap rejection | Missing Quickstart and misordered EventStore calls | Pass |
| Empty registry | Existing no-modules Shell state | Pass |

The four recorded package archive SHA-512 values were independently recomputed from the candidate archives and matched. The result contains no endpoint, tenant or user identity, token, payload, stack trace, or machine-specific path.

The redacted result and its gate limitation were also [recorded on Tenants issue #48](https://github.com/Hexalith/Hexalith.Tenants/issues/48#issuecomment-5817818115).

## Gate boundary

The fixture's annotated types are `AdopterProofKit.Domain.ProofDomain`, `ProofProjection`, and `CreateProofCommand`. The Tenants UI host has the supported Quickstart and domain calls in `src/Hexalith.Tenants.UI/Program.cs`, and `AddHexalithTenantsUiModule` conditionally registers EventStore when `EventStore:BaseAddress` is valid. Its current `TenantsFrontComposerRegistration.Manifest` declares empty projection and command lists, and the Tenants source has no `[Projection]` or `[Command]` annotated types. The Tenants production host was not started or probed in this run.

This result proves the external maintainer can execute the candidate-bound kit. It does **not** establish the PRD's requirement that the Tenants module itself renders a generated projection and generated command. Keep `EXT-ADOPTER-1`, G-6, and SM-1 open; the expected `tenants-bootstrap-acceptance.md` is intentionally absent until that named-module proof exists. No D-7 fallback is invoked.

Static verification: `rg -n '^\s*\[(Projection|Command)(\]|\()' src -g '*.cs'` returned no matches in the Tenants checkout. The current registration and EventStore call sites are `src/Hexalith.Tenants.UI/Program.cs` and `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs`.
