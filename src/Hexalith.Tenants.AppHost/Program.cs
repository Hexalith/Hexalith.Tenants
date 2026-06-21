using Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.EventStore.Aspire;
using Hexalith.Tenants.AppHost;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Resolve DAPR access control + resiliency configuration paths.
// Uses builder.AppHostDirectory to work under both `dotnet run` and Aspire testing.
string accessControlConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "accesscontrol.yaml");
string adminServerAccessControlConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "accesscontrol.eventstore-admin.yaml");
string resiliencyConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "resiliency.yaml");
string stateStoreComponentPath = ResolveDaprConfigPath(builder.AppHostDirectory, "statestore.yaml");
string pubSubComponentPath = ResolveDaprConfigPath(builder.AppHostDirectory, "pubsub.yaml");

// Optional DAPR placement/scheduler service addresses. When left unset, the DAPR sidecars fall back to
// the daprd default (localhost:50005 / :50006), which matches a slim-mode `dapr init`. A containerized
// `dapr init` (the default for Dapr 1.15+) publishes those services on host ports 6050/6060 instead, so
// environments using Docker-based DAPR set Dapr:PlacementHostAddress / Dapr:SchedulerHostAddress (e.g. to
// "localhost:6050" / "localhost:6060") to point the Aspire-managed sidecars at the real services. The
// integration-test AppHost is launched with these set to the auto-detected ports.
string? daprPlacementHostAddress = builder.Configuration["Dapr:PlacementHostAddress"];
string? daprSchedulerHostAddress = builder.Configuration["Dapr:SchedulerHostAddress"];

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

// Add EventStore (command gateway), Admin Server, and Admin UI projects.
// Project paths are resolved cross-repo via the IProjectMetadata classes in this AppHost.
IResourceBuilder<ProjectResource> eventStore = builder.AddProject<HexalithEventStore>("eventstore");
_ = eventStore
    .WithEnvironment("EventStore__DomainServices__Registrations__system|tenants|v1__AppId", "tenants")
    .WithEnvironment("EventStore__DomainServices__Registrations__system|tenants|v1__MethodName", "process")
    .WithEnvironment("EventStore__DomainServices__Registrations__system|tenants|v1__TenantId", "system")
    .WithEnvironment("EventStore__DomainServices__Registrations__system|tenants|v1__Domain", "tenants")
    .WithEnvironment("EventStore__DomainServices__Registrations__system|tenants|v1__Version", "v1")
    .WithEnvironment("EventStore__DomainServices__Registrations__system|global-administrators|v1__AppId", "tenants")
    .WithEnvironment("EventStore__DomainServices__Registrations__system|global-administrators|v1__MethodName", "process")
    .WithEnvironment("EventStore__DomainServices__Registrations__system|global-administrators|v1__TenantId", "system")
    .WithEnvironment("EventStore__DomainServices__Registrations__system|global-administrators|v1__Domain", "global-administrators")
    .WithEnvironment("EventStore__DomainServices__Registrations__system|global-administrators|v1__Version", "v1")
    .WithEnvironment("EventStore__Publisher__TopicOverrides__global-administrators", "tenants.events");
IResourceBuilder<ProjectResource> adminServer = builder.AddProject<HexalithEventStoreAdminServerHost>("eventstore-admin");
IResourceBuilder<ProjectResource> adminUI = builder.AddProject<HexalithEventStoreAdminUI>("eventstore-admin-ui");

// Wire the EventStore + Admin DAPR topology (shared state store + pub/sub, sidecars, resiliency)
// using the platform Aspire extension — the reusable boilerplate now lives in the EventStore platform
// Aspire library rather than a per-domain re-implementation.
HexalithEventStoreResources eventStoreResources = builder.AddHexalithEventStore(
    eventStore,
    adminServer,
    adminUI,
    eventStoreDaprConfigPath: accessControlConfigPath,
    adminServerDaprConfigPath: adminServerAccessControlConfigPath,
    resiliencyConfigPath: resiliencyConfigPath,
    stateStoreComponentPath: stateStoreComponentPath,
    daprPlacementHostAddress: daprPlacementHostAddress,
    daprSchedulerHostAddress: daprSchedulerHostAddress,
    pubSubComponentPath: pubSubComponentPath);

// Add the Tenants domain service via the platform domain-module extension (A4): its sidecar shares the
// EventStore state store + pub/sub. Replaces the per-domain Aspire wiring library.
IResourceBuilder<ProjectResource> tenants = builder.AddProject<HexalithTenants>("tenants")
    .AddEventStoreDomainModule(eventStoreResources, "tenants", accessControlConfigPath,
        daprPlacementHostAddress: daprPlacementHostAddress,
        daprSchedulerHostAddress: daprSchedulerHostAddress)
    // Must be the Keycloak user's stable subject (sub) GUID — the global-admin projection is keyed by
    // the JWT sub, so a username here would never match. admin-user's id is pinned in the realm import.
    .WithEnvironment("Tenants__BootstrapGlobalAdminUserId", "11111111-1111-1111-1111-111111111111");

// Wire Admin.UI to Admin.Server + EventStore SignalR (domain-agnostic composition kept in the AppHost).
EndpointReference adminServerHttps = adminServer.GetEndpoint("https");
EndpointReference eventStoreHttps = eventStore.GetEndpoint("https");
EndpointReference tenantsHttps = tenants.GetEndpoint("https");
_ = adminUI
    .WithReference(adminServer)
    .WaitFor(adminServer)
    .WithEnvironment("EventStore__SignalR__HubUrl", ReferenceExpression.Create($"{eventStoreHttps}/hubs/projection-changes"))
    .WithExternalHttpEndpoints();

// Memories search-index server (O4): started inline so the Tenants list can search the curated
// tenants-index. Reuses the shared statestore + pub/sub; runs its own FalkorDB; static dev secretstore +
// conversation (echo) components; a Dapr sidecar on a unique HTTP port (EventStore uses 3501). The server
// is built independently (HexalithMemoriesServer.SuppressBuild) so the two repos' package graphs stay
// isolated. End-to-end ingestion/search is gated on the Memories handoff
// (memories-search-index-handoff-2026-06-21.md).
// TODO: extract to a reusable Memories Aspire extension if a second consumer appears.
string memoriesSecretStorePath = ResolveDaprConfigPath(builder.AppHostDirectory, "secretstore.memories.yaml");
string memoriesLlmConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "llm.memories.yaml");

IResourceBuilder<IDaprComponentResource> memoriesSecretStore = builder.AddDaprComponent(
    "memories-secretstore",
    "secretstores.local.file",
    new DaprComponentOptions { LocalPath = memoriesSecretStorePath });
IResourceBuilder<IDaprComponentResource> memoriesLlm = builder.AddDaprComponent(
    "memories-llm",
    "conversation.echo",
    new DaprComponentOptions { LocalPath = memoriesLlmConfigPath });

IResourceBuilder<ContainerResource> memoriesFalkorDb = builder
    .AddContainer("memories-falkordb", "falkordb/falkordb")
    .WithEndpoint(targetPort: 6379, name: "falkordb");
EndpointReference memoriesFalkorDbEndpoint = memoriesFalkorDb.GetEndpoint("falkordb");

IResourceBuilder<ProjectResource> memoriesServer = builder
    .AddProject<HexalithMemoriesServer>("memories-server", launchProfileName: "http")
    .WithDaprSidecar(sidecar => sidecar
        .WithOptions(new DaprSidecarOptions {
            AppId = "memories-server",
            DaprHttpPort = 3502,
            DaprGrpcPort = 50002,
            PlacementHostAddress = daprPlacementHostAddress,
            SchedulerHostAddress = daprSchedulerHostAddress,
        })
        .WithReference(eventStoreResources.StateStore)
        .WithReference(eventStoreResources.PubSub)
        .WithReference(memoriesSecretStore)
        .WithReference(memoriesLlm))
    // Shared dapr-init Redis (localhost:6379) for the Memories vector/search store; FalkorDB for the graph store.
    .WithEnvironment("ConnectionStrings__redis", "localhost:6379")
    .WithEnvironment("ConnectionStrings__falkordb", ReferenceExpression.Create($"{memoriesFalkorDbEndpoint.Property(EndpointProperty.HostAndPort)}"))
    // Memories subscribes this topic; the Tenants consumer publishes SearchIndexEntryChanged to it.
    .WithEnvironment("MEMORIES_EVENTSTORE_TOPIC", "memories-events")
    // Route the Tenants producer's CloudEvents (source "hexalith-tenants") into the curated tenants-index
    // partition, and auto-provision that index tenant at startup so it is Active before the first event
    // arrives (otherwise the router drops SearchIndexEntryChanged as TenantNotFound). Memories handoff §3.1.
    .WithEnvironment("EventStoreIntegration__Routing__SourceToTenantMap__hexalith-tenants", "tenants-index")
    .WithEnvironment("EventStoreIntegration__Routing__AutoProvisionRoutedTenants", "true")
    .WaitFor(memoriesFalkorDb)
    .WaitFor(memoriesSecretStore)
    .WaitFor(memoriesLlm);

IResourceBuilder<ProjectResource> tenantsUI = builder.AddProject<HexalithTenantsUI>("tenants-ui")
    .WithReference(tenants)
    .WithReference(eventStore)
    .WithReference(memoriesServer)
    .WaitFor(tenants)
    .WaitFor(eventStore)
    .WaitFor(memoriesServer)
    .WithEnvironment("Tenants__BaseAddress", tenantsHttps)
    .WithEnvironment("EventStore__BaseAddress", eventStoreHttps)
    // Aspire service discovery (not a hardcoded :5000); the BFF reads Memories:BaseAddress and calls
    // AddMemoriesClient. Memories.Server exposes only an http endpoint.
    .WithEnvironment("Memories__BaseAddress", memoriesServer.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

// Add the Sample consuming service (a pub/sub subscriber) via the platform domain-module extension.
// It subscribes tenants.events, so it shares the pub/sub component (no isolated resources path).
IResourceBuilder<ProjectResource> sample = builder.AddProject<HexalithTenantsSample>("sample")
    .AddEventStoreDomainModule(eventStoreResources, "sample", accessControlConfigPath,
        daprPlacementHostAddress: daprPlacementHostAddress,
        daprSchedulerHostAddress: daprSchedulerHostAddress);

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
        .WithEnvironment("Authentication__JwtBearer__SigningKey", "")
        // Service credentials so the startup global-admin bootstrap can obtain a Keycloak token
        // (resource-owner-password grant) and call the secured EventStore command endpoint.
        .WithEnvironment("EventStore__Authentication__Authority", realmUrl)
        .WithEnvironment("EventStore__Authentication__ClientId", "hexalith-eventstore")
        .WithEnvironment("EventStore__Authentication__Username", "admin-user")
        .WithEnvironment("EventStore__Authentication__Password", "admin-pass");

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

    _ = tenantsUI
        .WithReference(keycloak)
        .WaitFor(keycloak)
        .WithEnvironment("Authentication__JwtBearer__Authority", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Issuer", realmUrl)
        .WithEnvironment("Authentication__JwtBearer__Audience", "hexalith-eventstore")
        .WithEnvironment("Authentication__JwtBearer__RequireHttpsMetadata", "false")
        .WithEnvironment("Authentication__JwtBearer__SigningKey", "")
        // Interactive browser sign-in (authorization-code flow) for the Tenants UI. Uses a
        // confidential Keycloak client; the relayed access token carries the hexalith-eventstore
        // audience so EventStore gateway calls authorize per-user (Story: per-user UI auth).
        .WithEnvironment("Authentication__OpenIdConnect__Authority", realmUrl)
        .WithEnvironment("Authentication__OpenIdConnect__ClientId", "hexalith-tenants-ui")
        .WithEnvironment("Authentication__OpenIdConnect__ClientSecret", "tenants-ui-dev-secret")
        .WithEnvironment("Authentication__OpenIdConnect__Audience", "hexalith-eventstore");

    _ = sample
        .WithReference(keycloak)
        .WaitFor(keycloak);
}
else {
    _ = adminUI.WithEnvironment("EventStore__AdminServer__SwaggerUrl", ReferenceExpression.Create($"{adminServerHttps}/swagger/index.html"));
}

await builder
    .Build()
    .RunAsync()
    .ConfigureAwait(false);

static string ResolveDaprConfigPath(string appHostDirectory, string fileName) {
    // Primary: resolve relative to AppHost project directory (works for dotnet run and Aspire testing)
    string configPath = Path.Combine(appHostDirectory, "DaprComponents", fileName);
    if (File.Exists(configPath)) {
        return configPath;
    }

    // Fallback: working directory (backwards compat for direct launch)
    configPath = Path.Combine(Directory.GetCurrentDirectory(), "DaprComponents", fileName);
    if (File.Exists(configPath)) {
        return configPath;
    }

    throw new FileNotFoundException(
        "DAPR access control configuration not found. "
        + $"Ensure {fileName} exists in the DaprComponents directory.",
        configPath);
}
