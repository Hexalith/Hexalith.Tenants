using Dapr.Client;

using FluentValidation;

using Hexalith.EventStore.Authentication;
using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Client.Discovery;
using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Configuration;
using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.ErrorHandling;
using Hexalith.EventStore.Middleware;
using Hexalith.EventStore.Pipeline;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.Indexes;
using Hexalith.EventStore.Server.Pipeline;
using Hexalith.EventStore.Server.Queries;
using Hexalith.EventStore.Validation;
using Hexalith.Tenants.Actors;
using Hexalith.Tenants.Bootstrap;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.DomainProcessing;
using Hexalith.Tenants.Health;
using Hexalith.Tenants.Projections;
using Hexalith.Tenants.Queries;
using Hexalith.Tenants.Server.Aggregates;
using Hexalith.Tenants.ServiceDefaults;
using Hexalith.Tenants.Validation;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDaprClient();
builder.Services.AddHealthChecks()
    .AddCheck<DaprStateStoreHealthCheck>(
        "dapr-statestore",
        failureStatus: HealthStatus.Degraded,
        tags: ["ready"]);
// Domain service only — do NOT register AddEventStoreServer or server-side EventStore extensions here.
// AggregateActor must only be hosted by the EventStore, not domain services.
// The bootstrap service sends commands to EventStore via DAPR HTTP.
builder.Services.AddEventStore(typeof(TenantAggregate).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(TenantSubmitCommandValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(TenantAggregate).Assembly);
builder.Services.AddHostedService<TenantBootstrapHostedService>();
builder.Services.AddScoped<DomainServiceRequestHandler>();
builder.Services.Configure<TenantBootstrapOptions>(
    builder.Configuration.GetSection("Tenants"));
builder.Services.AddProblemDetails();

// Data Protection backs the opaque query cursor codec (TenantQueryCursorCodec). SetApplicationName
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

// IETagService is required by TenantsProjectionActor (inherits CachingProjectionActor).
// Registered directly (not via AddEventStoreServer) to avoid hosting AggregateActor/ETagActor here.
builder.Services.TryAddScoped<IETagService, DaprETagService>();
builder.Services.TryAddSingleton<ITenantQueryCursorCodec, TenantQueryCursorCodec>();

// Command status and archive stores required by SubmitCommandHandler
builder.Services.Configure<CommandStatusOptions>(
    builder.Configuration.GetSection("EventStore:CommandStatus"));
builder.Services.TryAddSingleton<ICommandStatusStore, DaprCommandStatusStore>();
builder.Services.TryAddSingleton<ICommandArchiveStore, DaprCommandArchiveStore>();
builder.Services.TryAddScoped<ITenantValidator, ClaimsTenantValidator>();
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
builder.Services.AddExceptionHandler<DomainCommandRejectedExceptionHandler>();
builder.Services.AddExceptionHandler<QueryNotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<QueryExecutionFailedExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers()
    .AddApplicationPart(typeof(Hexalith.EventStore.Controllers.CommandsController).Assembly);
builder.Services.AddActors(options => options.Actors.RegisterActor<TenantsProjectionActor>());

WebApplication app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.UseCloudEvents();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapPost("/process", async (
    DomainServiceRequest request,
    DomainServiceRequestHandler handler,
    CancellationToken cancellationToken) =>
    Results.Ok(await handler.ProcessAsync(request, cancellationToken).ConfigureAwait(false)));
app.MapPost("/project", async (
    ProjectionRequest request,
    DaprClient daprClient,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
    => await new ProjectionDispatcher(daprClient, loggerFactory).DispatchAsync(request, cancellationToken).ConfigureAwait(false));
app.MapPost("/admin/operational-index-metadata", (
    AdminOperationalIndexMetadataRequest request,
    DiscoveryResult discovery)
    => Results.Ok(Hexalith.Tenants.AdminOperationalIndexMetadata.Create(discovery, request.Domains)));
app.MapSubscribeHandler();
app.MapActorsHandlers();

await app.RunAsync().ConfigureAwait(false);
