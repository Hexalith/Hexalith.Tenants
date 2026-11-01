using System.Text.Json;

using Dapr.Actors.Client;

using FluentValidation;

using Hexalith.EventStore.Authentication;
using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Configuration;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ErrorHandling;
using Hexalith.EventStore.Middleware;
using Hexalith.EventStore.Pipeline;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.Server.Pipeline;
using Hexalith.EventStore.Server.Queries;
using Hexalith.EventStore.ServiceDefaults;
using Hexalith.EventStore.Validation;
using Hexalith.Tenants.Authorization;
using Hexalith.Tenants.Bootstrap;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Projections;
using Hexalith.Tenants.Queries.Handlers;
using Hexalith.Tenants.Server.Aggregates;
using Hexalith.Tenants.Telemetry;
using Hexalith.Tenants.Validation;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Domain service only — do NOT register AddEventStoreServer or server-side EventStore extensions here.
// AggregateActor must only be hosted by the EventStore, not domain services. The domain-service SDK owns
// ServiceDefaults, aggregate discovery/activation, query-handler discovery, telemetry, and the canonical
// DAPR-invoked endpoints. The bootstrap service sends commands to EventStore via DAPR HTTP.
builder.AddEventStoreDomainService(typeof(TenantAggregate).Assembly, typeof(GetTenantQueryHandler).Assembly);
builder.Services.Configure<EventStoreServiceDefaultsOptions>(
    options => options.DevelopmentHealthResponseWriter = WriteSupportSafeDevelopmentHealthResponseAsync);
builder.Services.AddDaprClient();
// Readiness dependency: a Tenants instance is only "ready" for traffic once its DAPR state
// store is reachable. The platform DAPR state-store health check (Epic A5) self-reports Unhealthy
// on failure; registering the failure status as Unhealthy (not Degraded) guarantees that even an
// unexpected throw classifies the readiness dependency as Unhealthy → HTTP 503 on /ready, never
// Degraded → HTTP 200 (Story 7.5 AC1).
builder.Services.AddHealthChecks()
    .AddEventStoreDomainStateStoreHealthCheck(
        "tenants",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"]);
// Persisted multi-read-model store for the tenant /project build path (platform A8 abstraction,
// replacing the hand-rolled DaprTenantProjectionStateStore + TenantProjectionWritePolicy).
builder.Services.AddEventStoreReadModelStore();
builder.Services.AddSingleton(ProjectionDispatcher.CatalogRegistration);
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
builder.Services.AddEventStoreDevelopmentDomainCatalog(builder.Configuration, typeof(Hexalith.Tenants.Contracts.Routing.TenantCatalogDeclarations).Assembly);
#endif
builder.Services.TryAddSingleton(TimeProvider.System);
// Current TenantOwner evidence is rebuilt from the authoritative tenant stream on every request.
builder.Services.AddTenantOwnerSourceGateway(builder.Configuration);
builder.Services.AddScoped<IAuthoritativeEventStreamReader>(provider =>
    new AuthoritativeEventStreamReader(provider.GetRequiredService<TenantOwnerSourceGateway>(),
        provider.GetRequiredService<TimeProvider>(), TimeSpan.FromSeconds(10)));
builder.Services.AddKeyedScoped<IAuthoritativeEventStreamReader>("native-tenant-source", (provider, _) =>
    new AuthoritativeEventStreamReader(provider.GetRequiredService<TenantOwnerSourceGateway>(),
        provider.GetRequiredService<TimeProvider>(), TimeSpan.FromSeconds(10), requireNativeProof: true));
// The final native observation uses the same enrolled bound workload transport. Raw actor
// HTTP has no typed catalog scope and is deliberately refused by the shared invocation guard.
builder.Services.AddScoped<INativeSourceMetadataReader>(provider =>
    new GatewayNativeSourceMetadataReader(provider.GetRequiredService<TenantOwnerSourceGateway>(),
        provider.GetRequiredService<TimeProvider>()));
builder.Services.AddValidatorsFromAssembly(typeof(TenantSubmitCommandValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(TenantAggregate).Assembly);
builder.Services.AddTenantBootstrap();
// Wire administrator flags are untrusted at the domain-service boundary (EventStore Story 5.5): the SDK keeps
// command hints only after a final verified native source observation. Query hints are stripped;
// every query handler independently establishes its native grant or existing tenant permission.
builder.Services.AddScoped<ITenantsGlobalAdministratorAuthority, TenantsGlobalAdministratorAuthority>();
builder.Services.AddScoped<IDomainServiceAdministratorVerifier, TenantsGlobalAdministratorVerifier>();
// Domain telemetry instruments (query/projection duration histograms), sourced from the platform
// convention-named diagnostics registered by AddEventStoreDomainService. Tenants hosts more than one
// domain, so resolve the keyed tenants diagnostics instead of the single-domain unkeyed shortcut.
builder.Services.AddSingleton<TenantTelemetry>(serviceProvider => new TenantTelemetry(
    serviceProvider.GetRequiredKeyedService<EventStoreDomainDiagnostics>("tenants")));
builder.Services.Configure<TenantBootstrapOptions>(
    builder.Configuration.GetSection("Tenants"));
builder.Services.AddOptions<ReadModelFreshnessOptions>()
    .Bind(builder.Configuration.GetSection(ReadModelFreshnessOptions.SectionName))
    .Validate(
        static options => IsValidReadModelFreshnessOptions(options),
        "Tenants read-model freshness thresholds must be non-negative and Stale must be greater than or equal to Aging.")
    .ValidateOnStart();
builder.Services.AddProblemDetails();

// Data Protection backs the opaque query cursor codec. SetApplicationName
// anchors the key ring to a stable application identity so the keyring path/purpose chain is not
// influenced by IHostEnvironment.ApplicationName drift across host variants.
// Epic 11 — Production Authorization Readiness: AddEventStoreDataProtection persists the key ring to a
// shared DAPR state store when configured (EventStore:DataProtection:PersistToStateStore = true) so
// cursors issued by one replica can be unprotected by another and survive pod restarts/rollouts. The
// backing store is selected by DAPR YAML (deploy/dapr/statestore.yaml) — no infrastructure SDK enters
// this domain assembly. Local/dev keeps the framework's ephemeral key ring (PersistToStateStore = false).
builder.Services.AddEventStoreDataProtection(builder.Configuration, "Hexalith.Tenants");

// Register only owner handlers. The domain host receives canonical workload calls from EventStore;
// importing the gateway handler assembly would require the gateway's actor/admission infrastructure.
builder.Services.AddMediatR(cfg => {
    _ = cfg.RegisterServicesFromAssemblyContaining<Program>();
    _ = cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    _ = cfg.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
});

// Query/command routing defaults (tests may override via RemoveAll + AddSingleton)
builder.Services.TryAddScoped<IQueryRouter, QueryRouter>();
builder.Services.TryAddSingleton<ICommandRouter, CommandRouter>();
// EventStore's QueryRouter/CommandRouter resolve projection actors through the DAPR weak
// actor-proxy path, so both constructors require IActorProxyFactory. Tenants hosts no actors
// itself (AggregateActor lives only in EventStore, per the boundary note above) — it just
// needs the client-side proxy factory registered to satisfy the routers it wires manually.
builder.Services.TryAddSingleton<IActorProxyFactory>(_ => new ActorProxyFactory());

// Protected pagination cursor codec (platform A9 abstraction). The purpose string is kept identical
// to the retired TenantQueryCursorCodec so cursors issued before this refactor remain decodable.
builder.Services.AddEventStoreQueryCursorCodec("Hexalith.Tenants.QueryCursor.v1");

// Command status and archive stores required by SubmitCommandHandler
builder.Services.Configure<CommandStatusOptions>(
    builder.Configuration.GetSection("EventStore:CommandStatus"));
builder.Services.TryAddSingleton<ICommandStatusStore, DaprCommandStatusStore>();
builder.Services.TryAddSingleton<ICommandArchiveStore, DaprCommandArchiveStore>();
builder.Services.TryAddScoped<ClaimsTenantValidator>();
builder.Services.TryAddScoped<ITenantValidator, TenantsSystemTenantValidator>();
builder.Services.TryAddScoped<IRbacValidator, ClaimsRbacValidator>();

// ExtensionMetadataSanitizer required by CommandsController
builder.Services.Configure<ExtensionMetadataOptions>(
    builder.Configuration.GetSection("EventStore:ExtensionMetadata"));
builder.Services.TryAddSingleton<ExtensionMetadataSanitizer>();

// JWT bearer authentication for controllers imported from EventStore and Tenants query routes.
builder.Services.AddOptions<EventStoreAuthenticationOptions>()
    .BindConfiguration("Authentication:JwtBearer")
    .ValidateOnStart();
builder.Services.TryAddSingleton<IValidateOptions<EventStoreAuthenticationOptions>, ValidateEventStoreAuthenticationOptions>();
builder.Services.AddSingleton<IValidateOptions<EventStoreAuthenticationOptions>, ValidateTenantProductionAuthenticationOptions>();
builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();
builder.Services.AddTransient<IClaimsTransformation, EventStoreClaimsTransformation>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddAuthorization();

// Exception handlers — map domain exceptions to RFC 7807 HTTP responses (order: specific before generic)
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<AuthorizationServiceUnavailableHandler>();
builder.Services.AddExceptionHandler<AuthorizationExceptionHandler>();
builder.Services.AddExceptionHandler<ConcurrencyConflictExceptionHandler>();
builder.Services.AddExceptionHandler<DomainCommandRejectedExceptionHandler>();
builder.Services.AddExceptionHandler<QueryNotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<QueryExecutionFailedExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers().ConfigureApplicationPartManager(parts => {
    var gateway = parts.ApplicationParts.SingleOrDefault(part => part.Name == typeof(Hexalith.EventStore.Controllers.CommandsController).Assembly.GetName().Name);
    if (gateway is not null) parts.ApplicationParts.Remove(gateway);
});

WebApplication app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseCloudEvents();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Bespoke multi-read-model projection build path stays Tenants-mapped (persisted TenantReadModel +
// cross-aggregate index + audit, merge-on-write); the SDK yields /project because it is already mapped.
// The override carries the SDK /project policy (EventStore Story 5.5): only an EventStore workload assertion granting
// the projection operation, plus the Dapr app-channel token, reaches it; the startup route inventory refuses weaker.
app.MapPost("/project", async (
    ProjectionRequest request,
    IReadModelStore readModelStore,
    TenantTelemetry telemetry,
    ILoggerFactory loggerFactory,
    TimeProvider timeProvider,
    CancellationToken cancellationToken)
    => await new ProjectionDispatcher(readModelStore, telemetry, loggerFactory, timeProvider).DispatchAsync(request, cancellationToken).ConfigureAwait(false))
    .RequireEventStoreDomainServicePolicy("/project");
// Canonical DAPR-invoked domain-service endpoints from the SDK: /process (keyed domain processor),
// /replay-state, /query (in-process IDomainQueryHandler dispatch), and /admin/operational-index-metadata
// (now reporting handler-served query types). Replaces the hand-rolled DomainServiceRequestHandler and
// the host AdminOperationalIndexMetadata copy.
app.UseEventStoreDomainService();
// Subscription discovery is sidecar-originated: it presents the Dapr app-channel token only.
if (!app.Configuration.GetValue<bool>("Dapr:DeclarativeSubscriptions"))
{
    app.MapSubscribeHandler().RequireEventStoreSidecarChannel();
}

await app.RunAsync().ConfigureAwait(false);

static bool IsValidReadModelFreshnessOptions(ReadModelFreshnessOptions options) {
    ArgumentNullException.ThrowIfNull(options);

    try {
        _ = options.ToThresholds();
        return true;
    }
    catch (ArgumentOutOfRangeException) {
        return false;
    }
}

static async Task WriteSupportSafeDevelopmentHealthResponseAsync(
    HttpContext httpContext,
    HealthReport healthReport) {
    ArgumentNullException.ThrowIfNull(httpContext);
    ArgumentNullException.ThrowIfNull(healthReport);

    httpContext.Response.ContentType = "application/json; charset=utf-8";

    using MemoryStream stream = new();
    using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true })) {
        writer.WriteStartObject();
        writer.WriteString("status", healthReport.Status.ToString());
        writer.WriteStartObject("results");

        foreach (KeyValuePair<string, HealthReportEntry> entry in healthReport.Entries) {
            writer.WriteStartObject(entry.Key);
            writer.WriteString("status", entry.Value.Status.ToString());
            writer.WriteString("description", entry.Value.Description);
            writer.WriteString("duration", entry.Value.Duration.ToString());
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    stream.Position = 0;
    await stream.CopyToAsync(httpContext.Response.Body, httpContext.RequestAborted).ConfigureAwait(false);
}
