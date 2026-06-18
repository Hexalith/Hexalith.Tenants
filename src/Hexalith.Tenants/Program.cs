using System.Text.Json;

using Dapr.Actors.Client;

using FluentValidation;

using Hexalith.Commons.ServiceDefaults;
using Hexalith.EventStore.Authentication;
using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Client.Projections;
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
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Platform Aspire service defaults (observability, health, resilience) — the domain no longer ships
// its own ServiceDefaults copy (Epic B2). Convention-named OpenTelemetry source/meter for the tenants
// domain (Epic A5) replace the per-domain ActivitySource/Meter declarations.
builder.AddServiceDefaults();
builder.AddEventStoreDomainTelemetry("tenants");
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
// Domain service only — do NOT register AddEventStoreServer or server-side EventStore extensions here.
// AggregateActor must only be hosted by the EventStore, not domain services.
// The bootstrap service sends commands to EventStore via DAPR HTTP.
builder.Services.AddEventStore(typeof(TenantAggregate).Assembly);
// Persisted multi-read-model store for the tenant /project build path (platform A8 abstraction,
// replacing the hand-rolled DaprTenantProjectionStateStore + TenantProjectionWritePolicy).
builder.Services.AddEventStoreReadModelStore();
builder.Services.AddValidatorsFromAssembly(typeof(TenantSubmitCommandValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(TenantAggregate).Assembly);
builder.Services.AddHostedService<TenantBootstrapHostedService>();
// Domain telemetry instruments (query/projection duration histograms), sourced from the platform
// convention-named diagnostics registered by AddEventStoreDomainTelemetry above (Epic A5 rehome).
builder.Services.AddSingleton<TenantTelemetry>();
builder.Services.Configure<TenantBootstrapOptions>(
    builder.Configuration.GetSection("Tenants"));
builder.Services.AddProblemDetails();

// Data Protection backs the opaque query cursor codec. SetApplicationName
// anchors the key ring to a stable application identity so the keyring path/purpose chain is not
// influenced by IHostEnvironment.ApplicationName drift across host variants.
// DEFERRED (Epic 11 — Production Authorization Readiness): configure a shared, persisted key ring
// (Azure Blob + Key Vault / Redis / Dapr secret store) so cursors issued by one replica can be
// unprotected by another and survive pod restarts. Without persistence, every restart/rollout
// invalidates outstanding cursors and multi-replica deployments will see intermittent 400s.
builder.Services.AddDataProtection()
    .SetApplicationName("Hexalith.Tenants");

// MediatR pipeline - registers SubmitQueryHandler and SubmitCommandHandler for controller dispatch.
// Authorization stays narrow: Tenants uses EventStore claim/RBAC validation without registering
// the full EventStore server extension or its rate limiter.
builder.Services.AddMediatR(cfg => {
    _ = cfg.RegisterServicesFromAssemblyContaining<SubmitQueryHandler>();
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

// Tenant query handlers (platform A7 seam). Discovered/registered explicitly while the host retains
// its manual wiring; dispatched in-process by TenantsQueryController via DomainQueryDispatcher.
builder.Services.AddScoped<IDomainQueryHandler, GetTenantQueryHandler>();
builder.Services.AddScoped<IDomainQueryHandler, GetTenantUsersQueryHandler>();
builder.Services.AddScoped<IDomainQueryHandler, GetUserTenantsQueryHandler>();
builder.Services.AddScoped<IDomainQueryHandler, ListTenantsQueryHandler>();
builder.Services.AddScoped<IDomainQueryHandler, GetTenantAuditQueryHandler>();
builder.Services.AddScoped<IDomainQueryHandler, GetGlobalAdministratorsQueryHandler>();

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

builder.Services.AddControllers()
    .AddApplicationPart(typeof(Hexalith.EventStore.Controllers.CommandsController).Assembly);

WebApplication app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.MapHexalithDefaultEndpoints(ConfigureTenantsHealthEndpoints);
app.UseCloudEvents();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Bespoke multi-read-model projection build path stays Tenants-mapped (persisted TenantReadModel +
// cross-aggregate index + audit, merge-on-write); the SDK yields /project because it is already mapped.
app.MapPost("/project", async (
    ProjectionRequest request,
    IReadModelStore readModelStore,
    TenantTelemetry telemetry,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
    => await new ProjectionDispatcher(readModelStore, telemetry, loggerFactory).DispatchAsync(request, cancellationToken).ConfigureAwait(false));
// Canonical DAPR-invoked domain-service endpoints from the SDK: /process (keyed domain processor),
// /replay-state, /query (in-process IDomainQueryHandler dispatch), and /admin/operational-index-metadata
// (now reporting handler-served query types). Replaces the hand-rolled DomainServiceRequestHandler and
// the host AdminOperationalIndexMetadata copy.
app.MapEventStoreDomainService();
app.MapSubscribeHandler();

await app.RunAsync().ConfigureAwait(false);

static void ConfigureTenantsHealthEndpoints(HexalithServiceDefaultsOptions options) {
    options.HealthEndpointPath = "/health";
    options.LivenessEndpointPath = "/alive";
    options.ReadinessEndpointPath = "/ready";
    options.DevelopmentHealthResponseWriter = WriteSupportSafeDevelopmentHealthResponseAsync;
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
