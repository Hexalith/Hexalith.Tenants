using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.Tenants.Aspire;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Resolve DAPR access control configuration path.
// Both runtime (bin) and source directory are checked for compatibility.
string accessControlConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "DaprComponents", "accesscontrol.yaml");
if (!File.Exists(accessControlConfigPath)) {
    accessControlConfigPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DaprComponents", "accesscontrol.yaml"));
}

if (!File.Exists(accessControlConfigPath)) {
    throw new FileNotFoundException(
        "DAPR access control configuration not found. "
        + "Ensure accesscontrol.yaml exists in the DaprComponents directory.",
        accessControlConfigPath);
}

// Keycloak identity provider for JWT authentication.
// Enabled by default for local development with real OIDC token testing.
// Set EnableKeycloak=false in environment or appsettings to run without Keycloak
// (falls back to symmetric key auth via Authentication:JwtBearer:SigningKey).
IResourceBuilder<KeycloakResource>? keycloak = null;
ReferenceExpression? realmUrl = null;
if (!string.Equals(builder.Configuration["EnableKeycloak"], "false", StringComparison.OrdinalIgnoreCase)) {
    keycloak = builder.AddKeycloak("keycloak", 8180)
        .WithRealmImport("./KeycloakRealms");
    EndpointReference keycloakEndpoint = keycloak.GetEndpoint("http");
    realmUrl = ReferenceExpression.Create($"{keycloakEndpoint}/realms/hexalith");
}

// Add EventStore CommandApi (command gateway) with DAPR sidecar.
// The EventStore receives commands from clients and dispatches to domain services
// (including Tenants) via DAPR service invocation.
IResourceBuilder<ProjectResource> eventStore = builder.AddProject<Projects.Hexalith_EventStore_CommandApi>("eventstore");

// Add Tenants project and wire DAPR topology via Aspire extension.
// The Tenants extension provisions shared DAPR state store and pub/sub components.
IResourceBuilder<ProjectResource> tenants = builder.AddProject<Projects.Hexalith_Tenants>("tenants");
HexalithTenantsResources tenantsResources = builder.AddHexalithTenants(tenants, accessControlConfigPath);

// Wire EventStore with DAPR sidecar sharing the same state store and pub/sub.
_ = eventStore
    .WithDaprSidecar(sidecar => sidecar
        .WithOptions(new DaprSidecarOptions {
            AppId = "eventstore",
            Config = accessControlConfigPath,
        })
        .WithReference(tenantsResources.StateStore)
        .WithReference(tenantsResources.PubSub));

// Wire Keycloak auth to EventStore and Tenants if enabled.
if (keycloak is not null && realmUrl is not null) {
    _ = eventStore
        .WithReference(keycloak)
        .WaitFor(keycloak)
        .WithEnvironment("Authentication__JwtBearer__Authority", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Issuer", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Audience", "hexalith-eventstore")
        .WithEnvironment("Authentication__JwtBearer__RequireHttpsMetadata", "false")
        .WithEnvironment("Authentication__JwtBearer__SigningKey", "");

    _ = tenants
        .WithReference(keycloak)
        .WaitFor(keycloak)
        .WithEnvironment("Authentication__JwtBearer__Authority", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Issuer", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Audience", "hexalith-eventstore")
        .WithEnvironment("Authentication__JwtBearer__RequireHttpsMetadata", "false")
        .WithEnvironment("Authentication__JwtBearer__SigningKey", "");
}

// FrontShell — React/Vite frontend (pnpm monorepo).
// Workspace packages (shell-api, cqrs-client, ui) export from dist/ and must be built
// before Vite can start. Install then build from the monorepo root with proper ordering.
IResourceBuilder<ExecutableResource> frontshellInstall = builder
    .AddExecutable("frontshell-install", "pnpm", "../../Hexalith.FrontShell", "install");

IResourceBuilder<ExecutableResource> frontshellBuild = builder
    .AddExecutable("frontshell-build", "pnpm", "../../Hexalith.FrontShell", "run", "build")
    .WaitForCompletion(frontshellInstall);

// Points to the shell app inside the Hexalith.FrontShell submodule.
// Aspire injects services__eventstore__* env vars via WithReference.
// The Vite middleware in vite.config.ts reads these to generate /config.json dynamically.
var frontshell = builder.AddViteApp("frontshell", "../../Hexalith.FrontShell/apps/shell")
    .WithPnpm(installArgs: ["--dir", "../.."])
    .WaitForCompletion(frontshellBuild)
    .WithReference(eventStore)
    .WithExternalHttpEndpoints();

// Pass OIDC config so the Vite middleware can generate config.json with correct URLs.
if (keycloak is not null && realmUrl is not null) {
    _ = frontshell
        .WithReference(keycloak)
        .WaitFor(keycloak)
        .WithEnvironment("OIDC_AUTHORITY", realmUrl)
        .WithEnvironment("OIDC_CLIENT_ID", "hexalith-frontshell");
}

// Add Sample consuming service with DAPR sidecar for pub/sub event subscription.
// The Sample is a subscriber only — it does NOT reference StateStore (only Tenants needs actor state).
_ = builder.AddProject<Projects.Hexalith_Tenants_Sample>("sample")
    .WithDaprSidecar(sidecar => sidecar
        .WithOptions(new DaprSidecarOptions {
            AppId = "sample",
            Config = accessControlConfigPath,
        })
        .WithReference(tenantsResources.PubSub));

await builder
    .Build()
    .RunAsync()
    .ConfigureAwait(false);
