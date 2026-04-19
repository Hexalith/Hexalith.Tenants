using Dapr.Client;

using FluentValidation;

using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Configuration;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.ErrorHandling;
using Hexalith.EventStore.Middleware;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.Server.Pipeline;
using Hexalith.EventStore.Server.Queries;
using Hexalith.EventStore.Validation;
using Hexalith.Tenants.Actors;
using Hexalith.Tenants.Bootstrap;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.DomainProcessing;
using Hexalith.Tenants.Health;
using Hexalith.Tenants.Projections;
using Hexalith.Tenants.Server.Aggregates;
using Hexalith.Tenants.ServiceDefaults;
using Hexalith.Tenants.Validation;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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

// MediatR pipeline — registers SubmitQueryHandler and SubmitCommandHandler for controller dispatch
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<SubmitQueryHandler>());

// Query/command routing defaults (tests may override via RemoveAll + AddSingleton)
builder.Services.TryAddScoped<IQueryRouter, QueryRouter>();
builder.Services.TryAddSingleton<ICommandRouter, CommandRouter>();

// IETagService is required by TenantsProjectionActor (inherits CachingProjectionActor).
// Registered directly (not via AddEventStoreServer) to avoid hosting AggregateActor/ETagActor here.
builder.Services.TryAddScoped<IETagService, DaprETagService>();

// Command status and archive stores required by SubmitCommandHandler
builder.Services.Configure<CommandStatusOptions>(
    builder.Configuration.GetSection("EventStore:CommandStatus"));
builder.Services.TryAddSingleton<ICommandStatusStore, DaprCommandStatusStore>();
builder.Services.TryAddSingleton<ICommandArchiveStore, DaprCommandArchiveStore>();

// ExtensionMetadataSanitizer required by CommandsController
builder.Services.Configure<ExtensionMetadataOptions>(
    builder.Configuration.GetSection("EventStore:ExtensionMetadata"));
builder.Services.TryAddSingleton<ExtensionMetadataSanitizer>();

// Exception handlers — map domain exceptions to RFC 7807 HTTP responses (order: specific before generic)
builder.Services.AddExceptionHandler<DomainCommandRejectedExceptionHandler>();
builder.Services.AddExceptionHandler<QueryNotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<QueryExecutionFailedExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers();
builder.Services.AddActors(options => options.Actors.RegisterActor<TenantsProjectionActor>());

WebApplication app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.UseCloudEvents();
app.MapControllers();
app.MapPost("/process", async (
    DomainServiceRequest request,
    DomainServiceRequestHandler handler,
    CancellationToken cancellationToken) =>
    Results.Ok(await handler.ProcessAsync(request, cancellationToken).ConfigureAwait(false)));
app.MapPost("/project", async (ProjectionRequest request, DaprClient daprClient)
    => Results.Ok(await new TenantProjectionHandler(daprClient).ProjectAsync(request).ConfigureAwait(false)));
app.MapSubscribeHandler();
app.MapActorsHandlers();

await app.RunAsync().ConfigureAwait(false);
