using Dapr.Client;

using FluentValidation;

using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Middleware;

using EventStoreWebExtensions = Hexalith.EventStore.Extensions.EventStoreServiceCollectionExtensions;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Server.Configuration;
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
EventStoreWebExtensions.AddEventStore(builder.Services);
builder.Services.AddEventStoreServer(builder.Configuration);
builder.Services.AddEventStore(typeof(TenantAggregate).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(TenantSubmitCommandValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(TenantAggregate).Assembly);
builder.Services.AddHostedService<TenantBootstrapHostedService>();
builder.Services.AddScoped<DomainServiceRequestHandler>();
builder.Services.Configure<TenantBootstrapOptions>(
    builder.Configuration.GetSection("Tenants"));
builder.Services.AddActors(options => options.Actors.RegisterActor<TenantsProjectionActor>());

WebApplication app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
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
app.UseEventStore();

await app.RunAsync().ConfigureAwait(false);
