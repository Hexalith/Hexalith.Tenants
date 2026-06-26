---
title: "Adopt HexalithEventStoreSecurityExtensions in Tenants AppHost"
status: implemented
created: 2026-06-26
scope: minor
---

# Sprint Change Proposal - 2026-06-26

## 1. Issue Summary

The Tenants Aspire AppHost still hand-rolled the local Keycloak identity provider as an Aspire resource named `keycloak`, then duplicated JWT bearer, service-credential, and OpenID Connect environment wiring across EventStore, Tenants, Admin.Server, Admin.UI, Tenants.UI, and the sample service.

The EventStore platform now provides `HexalithEventStoreSecurityExtensions` in `Hexalith.EventStore.Aspire`. That helper initializes the local Keycloak-backed security resource as the shared Aspire service named `security` and exposes reusable wiring helpers for JWT bearer validation, EventStore client credentials, OpenID Connect, and plain security dependencies.

Trigger: direct user instruction, `$bmad-correct-course HexalithEventStoreSecurityExtensions to initialize the security service in aspire host`.

Evidence:

- `src/Hexalith.Tenants.AppHost/Program.cs` manually called `builder.AddKeycloak("keycloak", 8180)` and built the realm URL locally.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreSecurityExtensions.cs` already ships `AddHexalithEventStoreSecurity`, `WithJwtBearerSecurity`, `WithEventStoreClientCredentials`, `WithOpenIdConnectSecurity`, and `WithSecurityDependency`.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreSecurityOptions.cs` defaults the Aspire resource name to `security`, the realm to `hexalith`, the audience to `hexalith-eventstore`, and the `EnableKeycloak` disable key to the current Tenants AppHost contract.

## 2. Change Analysis Checklist Results

| Item | Status | Finding |
|---|---:|---|
| 1.1 Triggering story | [N/A] | No active user-facing story was invalidated. This is a cross-cutting AppHost/platform-alignment correction. |
| 1.2 Core problem | [x] | Technical duplication and stale resource naming in the Tenants AppHost after EventStore introduced the shared security helper. |
| 1.3 Evidence | [x] | Current Tenants source used `AddKeycloak("keycloak", 8180)` and duplicated auth env vars; EventStore helper exists and exposes the desired `security` resource. |
| 2.1 Current epic impact | [x] | Epic 1 Story 1.1 AppHost bootstrap is the historical affected area, but no epic scope changes are needed. |
| 2.2 Epic-level changes | [N/A] | No new epic, scope removal, or redefinition required. |
| 2.3 Remaining epics | [N/A] | Completed UI/domain epics are unaffected. |
| 2.4 Future epic invalidation | [N/A] | None. |
| 2.5 Epic order/priority | [N/A] | None. |
| 3.1 PRD conflicts | [x] | No product requirement change. PRD security posture remains server-enforced and support-safe. |
| 3.2 Architecture conflicts | [x] | Aligns with the architecture/AppHost guidance to reuse shared platform helpers and avoid generic infrastructure duplication in Tenants. |
| 3.3 UI/UX conflicts | [N/A] | No UI surface or interaction change. |
| 3.4 Secondary artifacts | [x] | AppHost static conformance test and local run docs need resource-name updates from `keycloak` to `security`. |
| 4.1 Direct adjustment | [x] | Viable. Low effort, low risk, confined to AppHost composition, tests, and docs. |
| 4.2 Potential rollback | [N/A] | Not useful; reverting shared-helper adoption would preserve duplicated platform plumbing. |
| 4.3 PRD/MVP review | [N/A] | MVP/product scope unchanged. |
| 4.4 Recommended path | [x] | Direct Adjustment. |
| 5.1 Issue summary | [x] | Documented above. |
| 5.2 Epic/artifact needs | [x] | Only AppHost, docs, and static tests require edits. |
| 5.3 Recommendation | [x] | Use `HexalithEventStoreSecurityExtensions` directly in the Tenants AppHost. |
| 5.4 MVP impact/action plan | [x] | No MVP scope impact; action plan is a minor refactor plus verification. |
| 5.5 Handoff plan | [x] | Developer agent direct implementation. |
| 6.1 Checklist completion | [x] | All applicable items addressed. |
| 6.2 Proposal accuracy | [x] | Proposal is consistent with current PRD, architecture, project context, and EventStore helper source. |
| 6.3 User approval | [x] | The user directly requested this Correct Course change in the session. |
| 6.4 Sprint status update | [N/A] | No epics/stories added, removed, or renumbered. |
| 6.5 Next steps/handoff | [x] | Implement and verify the minor AppHost alignment. |

## 3. Impact Analysis

Epic Impact: No product epic changes. The relevant historical implementation area is Epic 1 Story 1.1 (`tenants-ui-host-bootstrap`) because it owns AppHost/UI host composition, but the story remains done.

Story Impact: No new functional story required. This is a cross-cutting Correct Course implementation task.

Artifact Conflicts:

- PRD: no change required.
- Architecture: no change required; the correction reinforces the shared-helper/domain-boundary policy.
- UX: no change required.
- Docs/tests: update resource-name guidance and static AppHost conformance assertions.

Technical Impact:

- Tenants AppHost initializes the security service through `builder.AddHexalithEventStoreSecurity()`.
- Aspire dashboard/resource lookup changes from `keycloak` to `security` while Keycloak remains the local IdP implementation.
- JWT/OIDC/service credential environment variables are supplied by EventStore.Aspire helpers instead of duplicated in Tenants.
- `EnableKeycloak=false` remains the local no-Keycloak fallback path.

## 4. Recommended Approach

Recommended path: Direct Adjustment.

Rationale: The EventStore helper already covers the needed behavior and preserves the Tenants configuration contract. Reusing it removes duplicated platform plumbing from the domain repository and aligns the Aspire resource graph with the shared `security` service name.

Effort: Low.

Risk: Low. The main risks are stale static source tests, stale docs, or accidental loss of one environment setting. Mitigation is focused source assertions plus AppHost build/test verification.

## 5. Detailed Change Proposals

### AppHost Security Initialization

File: `src/Hexalith.Tenants.AppHost/Program.cs`

OLD:

```csharp
IResourceBuilder<KeycloakResource>? keycloak = null;
ReferenceExpression? realmUrl = null;
if (!string.Equals(builder.Configuration["EnableKeycloak"], "false", StringComparison.OrdinalIgnoreCase)) {
    keycloak = builder.AddKeycloak("keycloak", 8180)
        .WithRealmImport("./KeycloakRealms");
    EndpointReference keycloakEndpoint = keycloak.GetEndpoint("http");
    realmUrl = ReferenceExpression.Create($"{keycloakEndpoint}/realms/hexalith");
}
```

NEW:

```csharp
HexalithEventStoreSecurityResources? security = builder.AddHexalithEventStoreSecurity();
```

Rationale: The security service is shared EventStore platform plumbing. Tenants should initialize it through the shared Aspire helper instead of owning local Keycloak resource construction.

### AppHost Authentication Wiring

File: `src/Hexalith.Tenants.AppHost/Program.cs`

OLD:

```csharp
_ = eventStore
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithEnvironment("Authentication__JwtBearer__Authority", realmUrl)
    .WithEnvironment("Authentication__JwtBearer__Issuer", realmUrl)
    .WithEnvironment("Authentication__JwtBearer__Audience", "hexalith-eventstore")
    .WithEnvironment("Authentication__JwtBearer__RequireHttpsMetadata", "false")
    .WithEnvironment("Authentication__JwtBearer__SigningKey", "");
```

NEW:

```csharp
_ = eventStore.WithJwtBearerSecurity(security);
```

Additional helper use:

```csharp
_ = tenants.WithJwtBearerSecurity(security).WithEventStoreClientCredentials(security);
_ = adminServer.WithJwtBearerSecurity(security);
_ = adminUI.WithEventStoreClientCredentials(security);
_ = tenantsUI
    .WithJwtBearerSecurity(security)
    .WithOpenIdConnectSecurity(security, clientId: "hexalith-tenants-ui", clientSecret: "tenants-ui-dev-secret");
_ = sample.WithSecurityDependency(security);
```

Rationale: Preserve the existing auth behavior while moving generic resource dependencies and environment names into the shared EventStore.Aspire API.

### AppHost Project Reference Cleanup

File: `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`

OLD:

```xml
<PackageReference Include="Aspire.Hosting.Keycloak" />
```

NEW:

```xml
<!-- Direct Keycloak hosting package removed; EventStore.Aspire owns Keycloak hosting. -->
```

Rationale: Tenants no longer constructs a `KeycloakResource` directly.

### Documentation

Files:

- `docs/quickstart.md`
- `docs/demo.md`

OLD:

```md
`keycloak`: local identity provider
Find the `keycloak` base URL...
```

NEW:

```md
`security`: local Keycloak-backed identity provider
Find the `security` base URL...
```

Rationale: The implementation remains Keycloak, but the Aspire resource exposed to users is now `security`.

### Static Conformance Tests

File: `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs`

OLD:

```csharp
normalizedProgram.ShouldContain("        .WithReference(keycloak)");
normalizedProgram.ShouldContain("        .WaitFor(keycloak);");
```

NEW:

```csharp
program.ShouldContain("AddHexalithEventStoreSecurity(");
program.ShouldNotContain("AddKeycloak(\"keycloak\"");
normalizedProgram.ShouldContain("    _ = sample.WithSecurityDependency(security);");
normalizedProgram.ShouldContain("    _ = eventStore.WithJwtBearerSecurity(security);");
normalizedProgram.ShouldContain(".WithOpenIdConnectSecurity(");
```

Rationale: Guard the new shared-helper contract and prevent regression to the old manual resource name.

## 6. Implementation Handoff

Scope: Minor.

Route to: Developer agent for direct implementation.

Responsibilities:

- Replace manual Tenants AppHost Keycloak setup with `HexalithEventStoreSecurityExtensions`.
- Keep the `EnableKeycloak=false` fallback behavior.
- Update local docs and static tests for the `security` Aspire resource name.
- Verify AppHost and affected Server.Tests build/test cleanly.

Success criteria:

- Tenants AppHost source contains `AddHexalithEventStoreSecurity`.
- Tenants AppHost source does not construct `AddKeycloak("keycloak", ...)` directly.
- Local docs tell users to find the `security` resource in Aspire.
- Affected AppHost/static documentation tests pass.
- Release build succeeds for the touched projects.

Verification completed:

- `MSBUILDDISABLENODEREUSE=1 dotnet build src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --configuration Release -m:1` — passed, 0 warnings, 0 errors.
- `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release -m:1 --no-restore` — passed, 735/735 tests.
- `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -m:1 --no-restore` — passed, 0 warnings, 0 errors.
