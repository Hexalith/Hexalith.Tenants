using CommunityToolkit.Aspire.Hosting.Dapr;

using Aspire.Hosting.ApplicationModel;

using Hexalith.Commons.Aspire;
using Hexalith.EventStore.Aspire;
using Hexalith.Memories.Aspire;
using Hexalith.Tenants.AppHost;
using Hexalith.Tenants.Aspire;

using CommonsDaprLocalServiceEndpoints = Hexalith.Commons.Aspire.AspireDaprLocalServiceEndpoints;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Resolve DAPR access control + resiliency configuration paths.
// Uses builder.AppHostDirectory to work under both `dotnet run` and Aspire testing.
string accessControlConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "accesscontrol.yaml");
string adminServerAccessControlConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "accesscontrol.eventstore-admin.yaml");
string resiliencyConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "resiliency.yaml");
string stateStoreComponentPath = ResolveDaprConfigPath(builder.AppHostDirectory, "statestore.yaml");
string pubSubComponentPath = ResolveDaprConfigPath(builder.AppHostDirectory, "pubsub.yaml");

// Optional DAPR placement/scheduler service addresses. Explicit configuration still wins, but local AppHosts
// auto-detect the common containerized (6050/6060) and slim/native (50005/50006) DAPR service ports so every
// Aspire-managed sidecar connects to the actual local actor infrastructure.
(string? daprPlacementHostAddress, string? daprSchedulerHostAddress) = CommonsDaprLocalServiceEndpoints.Resolve(
    builder.Configuration[CommonsDaprLocalServiceEndpoints.PlacementHostAddressKey],
    builder.Configuration[CommonsDaprLocalServiceEndpoints.SchedulerHostAddressKey]);

// Local security service for JWT/OIDC authentication. Keycloak remains the implementation,
// but the shared EventStore Aspire helper exposes it as the "security" resource.
// Set EnableKeycloak=false in environment or appsettings to run without it
// (falls back to symmetric key auth via Authentication:JwtBearer:SigningKey).
HexalithEventStoreSecurityResources? security = builder.AddHexalithEventStoreSecurity();

// Add EventStore (command gateway), Admin Server, and Admin UI projects via the platform Aspire helper.
// The cross-repo IProjectMetadata wiring (and the repository-path resolution it relies on) now lives in
// the Hexalith.EventStore.Aspire library, so every domain-module AppHost calls this single helper instead
// of re-declaring identical metadata classes.
HexalithEventStorePlatformProjects eventStorePlatform = builder.AddHexalithEventStorePlatformProjects();
IResourceBuilder<ProjectResource> eventStore = eventStorePlatform.EventStore;
IResourceBuilder<ProjectResource> adminServer = eventStorePlatform.AdminServer;
IResourceBuilder<ProjectResource> adminUI = eventStorePlatform.AdminUI;

// Register the Tenants domain service's two domains with the EventStore command gateway so it routes
// tenants/global-administrators commands to the "tenants" app, and publish global-administrators events on the
// shared tenants.events topic. This gateway-side composition stays in the AppHost (the helper adds only the
// Tenants service runtime), mirroring how the EventStore platform projects are composed here.
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

// Add the Tenants domain service runtime via the Tenants platform Aspire helper. Gateway-side tenants /
// global-administrators routing remains explicit AppHost composition above; the helper adds the server project
// and a DAPR sidecar that shares the EventStore state store + pub/sub.
IResourceBuilder<ProjectResource> tenants = builder.AddHexalithTenantsServer(
        eventStoreResources,
        accessControlConfigPath,
        daprPlacementHostAddress: daprPlacementHostAddress,
        daprSchedulerHostAddress: daprSchedulerHostAddress)
    // Must be the Keycloak user's stable subject (sub) GUID — the global-admin projection is keyed by
    // the JWT sub, so a username here would never match. admin-user's id is pinned in the realm import.
    .WithEnvironment("Tenants__BootstrapGlobalAdminUserId", "11111111-1111-1111-1111-111111111111");

// Wire Admin.UI to Admin.Server + EventStore SignalR (domain-agnostic composition kept in the AppHost).
EndpointReference adminServerHttps = adminServer.GetEndpoint("https");
EndpointReference eventStoreHttps = eventStore.GetEndpoint("https");
_ = adminUI
    .WithReference(adminServer)
    .WaitFor(adminServer)
    .WithEnvironment("EventStore__SignalR__HubUrl", ReferenceExpression.Create($"{eventStoreHttps}/hubs/projection-changes"))
    .WithExternalHttpEndpoints();

// External-facing generated Tenants REST API host. It owns no state/pubsub components; it forwards caller
// bearer tokens to EventStore through DAPR service invocation (dapr-app-id: eventstore).
IResourceBuilder<ProjectResource> tenantsApi = builder.AddProject<HexalithTenantsApi>("tenants-api")
    .WithReference(eventStore)
    .WaitFor(eventStore)
    .WithExternalHttpEndpoints()
    .WithDaprSidecar(sidecar => sidecar
        .WithOptions(new DaprSidecarOptions {
            AppId = "tenants-api",
            PlacementHostAddress = daprPlacementHostAddress,
            SchedulerHostAddress = daprSchedulerHostAddress,
        }));

// Memories search-index server (O4): the Tenants list searches the curated tenants-index. The reusable
// Memories hosting recipe (memories-vectors Redis Stack store + memories-graphs FalkorDB store + secret store
// + conversation component + the memories project and its Dapr sidecar on a unique HTTP port) now lives in the
// Hexalith.Memories.Aspire platform library; this AppHost owns only the component YAML paths and the
// Tenants-specific source->index routing.
// End-to-end ingestion/search is gated on the Memories handoff (memories-search-index-handoff-2026-06-21.md).
string memoriesSecretStorePath = ResolveDaprConfigPath(builder.AppHostDirectory, "secretstore.memories.yaml");
string memoriesLlmConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "llm.memories.yaml");

// Hexalith.Memories.Aspire 2.24.1 and current source builds require the consumer-owned secret-store resource.
IResourceBuilder<IDaprComponentResource> memoriesSecretStore = builder.AddDaprComponent(
    "memories-secretstore",
    "secretstores.local.file",
    new DaprComponentOptions { LocalPath = memoriesSecretStorePath });

HexalithMemoriesSearchIndexServerResources memories = builder.AddHexalithMemoriesSearchIndexServer(
    eventStoreResources.StateStore,
    eventStoreResources.PubSub,
    memoriesSecretStore,
    memoriesLlmConfigPath,
    serverName: "memories",
    daprPlacementHostAddress: daprPlacementHostAddress,
    daprSchedulerHostAddress: daprSchedulerHostAddress);
IResourceBuilder<ProjectResource> memoriesService = memories.Server
    // Route the Tenants producer's CloudEvents (source "hexalith-tenants") into the curated tenants-index
    // partition, and auto-provision that index tenant at startup so it is Active before the first event
    // arrives (otherwise the router drops SearchIndexEntryChanged as TenantNotFound). Memories handoff §3.1.
    .WithEnvironment("EventStoreIntegration__Routing__SourceToTenantMap__hexalith-tenants", "tenants-index")
    .WithEnvironment("EventStoreIntegration__Routing__AutoProvisionRoutedTenants", "true");

IResourceBuilder<ProjectResource> tenantsUI = builder.AddProject<HexalithTenantsUI>("tenants-ui")
    .WithReference(eventStore)
    .WithReference(memoriesService)
    // Story 1.10: the BFF's six canonical reads go direct to the generated Tenants REST API, not through
    // the EventStore query gateway. Without this reference and base address every read resolves
    // UnavailableTenantQueryGateway, so the read surfaces render fail-closed. This closes HOST-REF-1.
    .WithReference(tenantsApi)
    .WaitFor(eventStore)
    .WaitFor(memoriesService)
    .WaitFor(tenantsApi)
    .WithEnvironment("EventStore__BaseAddress", eventStoreHttps)
    // Commands/status stay on EventStore; reads resolve independently from Tenants__BaseAddress. A missing
    // reference must fail closed on its own side -- neither side falls back to the other.
    .WithEnvironment("Tenants__BaseAddress", tenantsApi.GetEndpoint("https"))
    // Aspire service discovery (not a hardcoded :5000); the BFF reads Memories:BaseAddress and calls
    // AddMemoriesClient. Memories.Server exposes only an http endpoint.
    .WithEnvironment("Memories__BaseAddress", memoriesService.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

// Add the Sample consuming service (a pub/sub subscriber) via the platform domain-module extension.
// It subscribes tenants.events, so it shares the pub/sub component (no isolated resources path).
IResourceBuilder<ProjectResource> sample = builder.AddProject<HexalithTenantsSample>("sample")
    .AddEventStoreDomainModule(eventStoreResources, "sample", accessControlConfigPath,
        daprPlacementHostAddress: daprPlacementHostAddress,
        daprSchedulerHostAddress: daprSchedulerHostAddress);

// Wire local security to EventStore, Tenants, Admin.Server, Admin.UI, Tenants.UI, and Sample if enabled.
if (security is not null) {
    _ = eventStore.WithJwtBearerSecurity(security);

    _ = tenants
        .WithJwtBearerSecurity(security)
        // Service credentials so the startup global-admin bootstrap can obtain a Keycloak token
        // (resource-owner-password grant) and call the secured EventStore command endpoint.
        .WithEventStoreClientCredentials(security);

    _ = adminServer.WithJwtBearerSecurity(security);

    _ = adminUI
        .WithEnvironment("EventStore__AdminServer__SwaggerUrl", ReferenceExpression.Create($"{adminServerHttps}/swagger/index.html"))
        .WithEventStoreClientCredentials(security);

    _ = tenantsUI
        .WithJwtBearerSecurity(security)
        // Interactive browser sign-in (authorization-code flow) for the Tenants UI. Uses a
        // confidential Keycloak client; the relayed access token carries the hexalith-eventstore
        // audience so EventStore gateway calls authorize per-user (Story: per-user UI auth).
        .WithOpenIdConnectSecurity(
            security,
            clientId: "hexalith-tenants-ui",
            clientSecret: "tenants-ui-dev-secret");

    // tenants-api validates inbound callers against the same realm and forwards the validated bearer
    // to EventStore; this mirrors the Sample external API host.
    _ = tenantsApi.WithEventStoreClientCredentials(security);

    _ = sample.WithSecurityDependency(security);
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
