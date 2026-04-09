using Dapr.Client;

using FluentValidation;

using Hexalith.EventStore.Client.Registration;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Actors;
using Hexalith.Tenants.Bootstrap;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.DomainProcessing;
using Hexalith.Tenants.Projections;
using Hexalith.Tenants.Health;
using Hexalith.Tenants.Validation;
using Hexalith.Tenants.Server.Aggregates;
using Hexalith.Tenants.ServiceDefaults;

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
builder.Services.AddControllers();
builder.Services.AddActors(options => options.Actors.RegisterActor<TenantsProjectionActor>());

WebApplication app = builder.Build();

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
