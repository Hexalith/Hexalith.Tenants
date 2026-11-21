# Technology review · 21 November 2026

The package and hosted qualification sections agree with the source. Two runtime contracts need clarification, and the local development instructions need one correction. The review inspected the working tree at Tenants `f15f6170`, including the separate Builds checkout `e1380b8`.

### Medium · Projection lifecycle on a conditional read

The description at `architecture.md:443–449` and `:470–476` describes three freshness values on the wire. The nullable `IsStale` flag has three interpretations, but REST also carries an independent projection lifecycle. For `Rebuilding`, `Degraded`, `Unavailable` and `LocalOnly`, a 304 fails cache validation even with a matching validator and version. A new client using only the flag could reuse an inadmissible payload. State the precedence of lifecycle and provenance over the legacy flag in D6 and link that rule from AD-8. Evidence appears in `src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:351–403,406–449` and `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsRestQueryClientTests.cs:1111–1137`.

### Medium · Ordinary status and correction proof have different identity requirements

The confirmation instruction at `architecture.md:502` asks the reader to match the returned domain, aggregate and message identity for every status result. For ordinary tracked status, a supplied, conflicting domain fails the check. An omitted domain can still yield `Completed` with verified message and aggregate identity. To award a committed sequence for a correction, the gateway additionally requires the explicit system tenant, expected domain, aggregate and matching completion status code. Governance recovery without a saved correlation also requires the full identity. Distinguish these cases so implementers preserve recovery for older status records and require full evidence for correction. `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:593–606,630–655,678–686` and `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs:2102–2130` specify the distinction.

### Low · Local Dapr startup instructions

The dev-server instruction at `architecture.md:782` says placement and scheduler start first in slim mode. The supported quickstart directs developers to full `dapr init` to supply prerequisites such as Redis. In run mode, AppHost calls the Platform helper, which creates Sentry and private placement and scheduler endpoints for the authenticated sidecars. Describe these two responsibilities and link to the quickstart. Evidence appears in `docs/quickstart.md:42–48`, `src/Hexalith.Tenants.AppHost/Program.cs:33–49,338–345` and `references/Hexalith.Platform/DaprSelfHostedMtls.cs:66–88`.

### Verified claims

- Tenants declares McpCli 3.0.0 and `net11.0` in `Directory.Build.props:4,102`. The refreshed Builds catalog selects EventStore 5.4.0, FrontComposer 6.0.1, Memories 4.1.0, Dapr runtime 1.19.2, Dapr CLI 1.19.1 and Fluent 5.1.0 in `references/Hexalith.Builds/Props/Directory.Packages.props:27–44,434`. The Tenants authenticated-routes job specifies CLI 1.18.2 and runtime 1.18.7 in `.github/workflows/ci.yml:249–257`.
- The independent query and command clients match AD-6. Queries invoke `tenants-api` through Dapr and command/status traffic uses the EventStore base address. See `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:139–160,186–198`.
- The release matrix, package producer check, AppHost hash verification and per-host manifest digests agree with AD-14. See `.github/workflows/ci.yml:188`, `scripts/prepare-bootstrap-compatibility.sh:18–26` and `scripts/prepare-source-catalog-profile.py:243–276`. The candidate's recorded Builds prerequisite and hosted execution remain release-owner checks. This review established source conformance only.
