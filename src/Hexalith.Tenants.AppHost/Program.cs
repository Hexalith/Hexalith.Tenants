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
string tenantsUiAccessControlConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "accesscontrol.tenants-ui.yaml");
string tenantsApiAccessControlConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "accesscontrol.tenants-api.yaml");
string adminServerAccessControlConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "accesscontrol.eventstore-admin.yaml");
string resiliencyConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "resiliency.yaml");
string stateStoreComponentPath = ResolveDaprConfigPath(builder.AppHostDirectory, "statestore.yaml");
string pubSubComponentPath = ResolveDaprConfigPath(builder.AppHostDirectory, "pubsub.yaml");

// Local runs own an isolated mTLS placement/scheduler plane. The published test harness appends
// standard loopback prerequisites even when the fixture selects empty endpoints. Accept only those
// legacy local hints; external hosts and custom ports cannot join the private identity boundary.
if (builder.ExecutionContext.IsRunMode) {
    foreach (string key in new[] { CommonsDaprLocalServiceEndpoints.PlacementHostAddressKey, CommonsDaprLocalServiceEndpoints.SchedulerHostAddressKey }) {
        string? configured = builder.Configuration[key]?.Trim();
        if (string.IsNullOrWhiteSpace(configured)) {
            continue;
        }
        string[] legacyHints = key == CommonsDaprLocalServiceEndpoints.PlacementHostAddressKey
            ? ["localhost:50005", "localhost:6050", "127.0.0.1:50005", "127.0.0.1:6050"]
            : ["localhost:50006", "localhost:6060", "127.0.0.1:50006", "127.0.0.1:6060"];
        if (!legacyHints.Contains(configured, StringComparer.OrdinalIgnoreCase)) {
            throw new InvalidOperationException($"Configuration '{key}' is incompatible with the private local Dapr mTLS control plane. Remove this setting for local runs.");
        }
        Console.WriteLine($"Configuration '{key}' contains a legacy local prerequisite hint ({configured}). Private allocated Dapr mTLS listeners supersede this hint; no connection to that prerequisite endpoint will be used.");
    }
}
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

// The pinned platform helper owns EventStore's run-mode app-channel token and shares it with
// its sidecar. Registering it here again creates a duplicate resource before the topology starts.
if (builder.ExecutionContext.IsPublishMode) {
    _ = eventStore.WithGeneratedEventStoreAppChannelToken();
}

// Add the Tenants domain service runtime via the Tenants platform Aspire helper. Gateway-side tenants /
// global-administrators routing remains explicit AppHost composition above; the helper adds the server project
// and a DAPR sidecar that shares the EventStore state store + pub/sub.
IResourceBuilder<ProjectResource> tenants = builder.AddHexalithTenantsServer(
        eventStoreResources,
        accessControlConfigPath,
        daprPlacementHostAddress: daprPlacementHostAddress,
        daprSchedulerHostAddress: daprSchedulerHostAddress)
    // Must be the Keycloak user's stable subject (sub) GUID — the global-admin projection is keyed by
    // the JWT sub, so a username here would never match. The realm keeps this id stable while Aspire
    // injects a fresh user name and password into the import for every non-persistent run.
    .WithEnvironment("Tenants__BootstrapGlobalAdminUserId", "11111111-1111-1111-1111-111111111111")
    .WithGeneratedEventStoreAppChannelToken();

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
            AppProtocol = "https",
            Config = tenantsApiAccessControlConfigPath,
            ResourcesPaths = [Path.GetDirectoryName(resiliencyConfigPath)!],
            PlacementHostAddress = daprPlacementHostAddress,
            SchedulerHostAddress = daprSchedulerHostAddress,
        }));

// Memories search-index server (O4): the Tenants list searches the curated tenants-index. The reusable
// Memories hosting recipe (memories-vectors Redis Stack store + memories-graphs FalkorDB store + secret store
// + conversation component + the memories project and its Dapr sidecar on a unique HTTP port) now lives in the
// Hexalith.Memories.Aspire platform library; this AppHost owns only the component YAML paths and the
// Tenants-specific source->index routing.
// End-to-end ingestion/search is gated on the Memories handoff (memories-search-index-handoff-2026-06-21.md).
IResourceBuilder<ProjectResource>? memoriesService = null;
if (!string.Equals(builder.Configuration["EnableMemories"], "false", StringComparison.OrdinalIgnoreCase)) {
    string memoriesSecretStorePath = ResolveDaprConfigPath(builder.AppHostDirectory, "secretstore.memories.yaml");
    string memoriesLlmConfigPath = ResolveDaprConfigPath(builder.AppHostDirectory, "llm.memories.yaml");

    // Hexalith.Memories.Aspire 2.25.0 and current source builds require the consumer-owned secret-store resource.
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
    memoriesService = memories.Server
        // Route the Tenants producer's CloudEvents (source "hexalith-tenants") into the curated tenants-index
        // partition, and auto-provision that index tenant at startup so it is Active before the first event
        // arrives (otherwise the router drops SearchIndexEntryChanged as TenantNotFound). Memories handoff §3.1.
        .WithEnvironment("EventStoreIntegration__Routing__SourceToTenantMap__hexalith-tenants", "tenants-index")
        .WithEnvironment("EventStoreIntegration__Routing__AutoProvisionRoutedTenants", "true");
}

IResourceBuilder<ProjectResource> tenantsUI = builder.AddProject<HexalithTenantsUI>("tenants-ui")
    .WithReference(eventStore)
    // Reads use this BFF's sidecar and the API receiver's GET-only caller policy.
    .WithReference(tenantsApi)
    .WaitFor(eventStore)
    .WaitFor(tenantsApi)
    .WithEnvironment("EventStore__BaseAddress", eventStoreHttps)
    .WithExternalHttpEndpoints()
    .WithDaprSidecar(sidecar => sidecar.WithOptions(new DaprSidecarOptions {
        AppId = "tenants-ui",
        AppProtocol = "https",
        Config = tenantsUiAccessControlConfigPath,
        ResourcesPaths = [Path.GetDirectoryName(resiliencyConfigPath)!],
        PlacementHostAddress = daprPlacementHostAddress,
        SchedulerHostAddress = daprSchedulerHostAddress,
    }));

// Search is optional for focused REST-route qualification; the ordinary topology retains it by default.
if (memoriesService is not null) {
    _ = tenantsUI.WithReference(memoriesService)
        .WaitFor(memoriesService)
        .WithEnvironment("Memories__BaseAddress", memoriesService.GetEndpoint("http"));
}

// Add the Sample consuming service (a pub/sub subscriber) via the platform domain-module extension.
// It subscribes tenants.events, so it shares the pub/sub component (no isolated resources path).
IResourceBuilder<ProjectResource> sample = builder.AddProject<HexalithTenantsSample>("sample")
    .AddEventStoreDomainModule(eventStoreResources, "sample", accessControlConfigPath,
        daprPlacementHostAddress: daprPlacementHostAddress,
        daprSchedulerHostAddress: daprSchedulerHostAddress)
    .WithGeneratedEventStoreAppChannelToken();

// Wire local security to EventStore, Tenants, Admin.Server, Admin.UI, Tenants.UI, and Sample if enabled.
if (security is not null) {
    // The local realm imports an EventStore service-account client with this per-run secret. Both
    // Keycloak and EventStore receive the same secret parameter; it is never checked into the realm.
    bool persistWorkloadSecret = bool.TryParse(
        builder.Configuration[HexalithEventStoreSecurityOptions.DefaultPersistentConfigurationKey]?.Trim(),
        out bool keycloakPersistent) && keycloakPersistent;
    IResourceBuilder<ParameterResource> workloadClientSecret = builder.AddParameter(
        "eventstore-workload-client-secret",
        new GenerateParameterDefault {
            MinLength = 32,
            Lower = true,
            MinLower = 1,
            Numeric = true,
            MinNumeric = 1,
            Upper = true,
            MinUpper = 1,
            Special = false,
        },
        secret: true,
        persist: persistWorkloadSecret);
    _ = security.Keycloak.WithEnvironment("HEXALITH_EVENTSTORE_WORKLOAD_CLIENT_SECRET", workloadClientSecret);
    _ = eventStore.WithEventStoreWorkloadClientCredentials("eventstore", workloadClientSecret);

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

    // Symmetric Development mode still validates every bearer and workload assertion. The integration
    // fixture supplies its existing demo key; other callers must provide their own explicit key.
    string? configuredSigningKey = builder.Configuration["Authentication:JwtBearer:SigningKey"];
    if (string.IsNullOrWhiteSpace(configuredSigningKey)
        || System.Text.Encoding.UTF8.GetByteCount(configuredSigningKey) < 32) {
        throw new InvalidOperationException(
            "Authentication:JwtBearer:SigningKey must be configured with at least 32 UTF-8 bytes when EnableKeycloak=false.");
    }

    IResourceBuilder<ParameterResource> signingKey = builder.AddParameter(
        "tenants-local-jwt-signing-key",
        () => configuredSigningKey,
        secret: true);
    ConfigureLocalSymmetricValidation(eventStore, signingKey);
    ConfigureLocalSymmetricValidation(adminServer, signingKey);
    ConfigureLocalSymmetricValidation(tenants, signingKey);
    ConfigureLocalSymmetricValidation(tenantsApi, signingKey);
    _ = tenantsApi
        .WithEnvironment("EventStore__Authentication__Authority", string.Empty)
        .WithEnvironment("EventStore__Authentication__Issuer", "hexalith-dev")
        .WithEnvironment("EventStore__Authentication__Audience", HexalithEventStoreSecurityOptions.DefaultAudience)
        .WithEnvironment("EventStore__Authentication__SigningKey", signingKey);
    ConfigureLocalSymmetricValidation(sample, signingKey);
}

// The connected sidecars share authenticated peer identity and the actor control plane locally.
// Receiver ACLs require a verified SPIFFE caller; healthy plaintext sidecars cannot satisfy them.
(string RunDirectory, string CertificateDirectory)? localDapr = builder.ExecutionContext.IsRunMode
    ? DaprSelfHostedMtls.AddLocalApplicationInfrastructure(
        builder,
        Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../..")),
        ResolveDaprConfigPath(builder.AppHostDirectory, "sentry.yaml"))
    : null;
try
{
    DistributedApplication application = builder.Build();
    await using (application.ConfigureAwait(false))
    {
        await application.RunAsync().ConfigureAwait(false);
    }
}
finally
{
    if (localDapr is { } owned)
    {
        _ = await Hexalith.Builds.Tooling.Runtime.CompositionWorkspace.TryDeleteAsync(owned.CertificateDirectory, CancellationToken.None).ConfigureAwait(false);
        _ = await Hexalith.Builds.Tooling.Runtime.CompositionWorkspace.TryDeleteAsync(owned.RunDirectory, CancellationToken.None).ConfigureAwait(false);
    }
}

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

static void ConfigureLocalSymmetricValidation(
    IResourceBuilder<ProjectResource> resource,
    IResourceBuilder<ParameterResource> signingKey) {
    _ = resource
        .WithEnvironment("Authentication__JwtBearer__Authority", string.Empty)
        .WithEnvironment("Authentication__JwtBearer__Issuer", "hexalith-dev")
        .WithEnvironment("Authentication__JwtBearer__Audience", HexalithEventStoreSecurityOptions.DefaultAudience)
        .WithEnvironment("Authentication__JwtBearer__ValidAudiences__0", HexalithEventStoreSecurityOptions.DefaultAudience)
        .WithEnvironment("Authentication__JwtBearer__AllowedAlgorithms__0", "HS256")
        .WithEnvironment("Authentication__JwtBearer__SigningKey", signingKey)
        .WithEnvironment("Authentication__JwtBearer__RequireHttpsMetadata", "false");
}
