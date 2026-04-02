using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.Tenants.Aspire;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Resolve DAPR access control configuration paths.
// Both runtime (bin) and source directory are checked for compatibility.
string accessControlConfigPath = ResolveDaprConfigPath("accesscontrol.yaml");
string adminServerAccessControlConfigPath = ResolveDaprConfigPath("accesscontrol.eventstore-admin.yaml");

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

// Add EventStore (command gateway) with DAPR sidecar.
// The EventStore receives commands from clients and dispatches to domain services
// (including Tenants) via DAPR service invocation.
// DaprHttpPort is fixed (3501) so Admin.Server can query the EventStore
// sidecar's metadata endpoint for actor type discovery.
const int EventStoreDaprHttpPort = 3501;
IResourceBuilder<ProjectResource> eventStore = builder.AddProject<Projects.Hexalith_EventStore>("eventstore");

// Add EventStore Admin Server and Admin UI for event store inspection.
IResourceBuilder<ProjectResource> adminServer = builder.AddProject<Projects.Hexalith_EventStore_Admin_Server_Host>("eventstore-admin");
IResourceBuilder<ProjectResource> adminUI = builder.AddProject<Projects.Hexalith_EventStore_Admin_UI>("eventstore-admin-ui");

// Add Tenants project and wire DAPR topology via Aspire extension.
// The Tenants extension provisions shared DAPR state store and pub/sub components.
IResourceBuilder<ProjectResource> tenants = builder.AddProject<Projects.Hexalith_Tenants>("tenants");
HexalithTenantsResources tenantsResources = builder.AddHexalithTenants(tenants, accessControlConfigPath);

// Wire EventStore with DAPR sidecar sharing the same state store and pub/sub.
_ = eventStore
    .WithDaprSidecar(sidecar => sidecar
        .WithOptions(new DaprSidecarOptions {
            AppId = "eventstore",
            DaprHttpPort = EventStoreDaprHttpPort,
            Config = accessControlConfigPath,
        })
        .WithReference(tenantsResources.StateStore)
        .WithReference(tenantsResources.PubSub));

// Wire Admin.Server with DAPR sidecar.
// Admin.Server needs state store for direct reads (health, admin indexes)
// and service invocation to EventStore for write delegation.
// It does not publish or subscribe directly, so it does not reference pub/sub.
_ = adminServer
    .WithReference(eventStore)
    .WithEnvironment("AdminServer__EventStoreDaprHttpEndpoint", "http://localhost:" + EventStoreDaprHttpPort)
    .WithDaprSidecar(sidecar => sidecar
        .WithOptions(new DaprSidecarOptions {
            AppId = "eventstore-admin",
            Config = adminServerAccessControlConfigPath,
        })
        .WithReference(tenantsResources.StateStore));

// Wire Admin.UI with Admin.Server reference for HTTP API calls.
// Also pass the EventStore endpoint so the SignalR client can connect
// for real-time projection change signals.
EndpointReference adminServerHttps = adminServer.GetEndpoint("https");
EndpointReference eventStoreHttps = eventStore.GetEndpoint("https");
_ = adminUI
    .WithReference(adminServer)
    .WaitFor(adminServer)
    .WithEnvironment("EventStore__SignalR__HubUrl", ReferenceExpression.Create($"{eventStoreHttps}/hubs/projection-changes"))
    .WithExternalHttpEndpoints();

// Wire Keycloak auth to EventStore, Tenants, Admin.Server, and Admin.UI if enabled.
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

    _ = adminServer
        .WithReference(keycloak)
        .WaitFor(keycloak)
        .WithEnvironment("Authentication__JwtBearer__Authority", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Issuer", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Audience", "hexalith-eventstore")
        .WithEnvironment("Authentication__JwtBearer__RequireHttpsMetadata", "false")
        .WithEnvironment("Authentication__JwtBearer__SigningKey", "");

    _ = adminUI
        .WithReference(keycloak)
        .WaitFor(keycloak)
        .WithEnvironment("EventStore__AdminServer__SwaggerUrl", ReferenceExpression.Create($"{adminServerHttps}/swagger/index.html"))
        .WithEnvironment("EventStore__Authentication__Authority", realmUrl)
        .WithEnvironment("EventStore__Authentication__ClientId", "hexalith-eventstore")
        .WithEnvironment("EventStore__Authentication__Username", "admin-user")
        .WithEnvironment("EventStore__Authentication__Password", "admin-pass");
}
else {
    _ = adminUI.WithEnvironment("EventStore__AdminServer__SwaggerUrl", ReferenceExpression.Create($"{adminServerHttps}/swagger/index.html"));
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

static string ResolveDaprConfigPath(string fileName) {
    string configPath = Path.Combine(Directory.GetCurrentDirectory(), "DaprComponents", fileName);
    if (!File.Exists(configPath)) {
        configPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DaprComponents", fileName));
    }

    if (!File.Exists(configPath)) {
        throw new FileNotFoundException(
            "DAPR access control configuration not found. "
            + $"Ensure {fileName} exists in the DaprComponents directory.",
            configPath);
    }

    return configPath;
}
