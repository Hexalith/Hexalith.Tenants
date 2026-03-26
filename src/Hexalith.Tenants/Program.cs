using FluentValidation;

using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.CommandApi.Extensions;
using Hexalith.EventStore.CommandApi.Middleware;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Server.Configuration;
using Hexalith.Tenants.Actors;
using Hexalith.Tenants.Bootstrap;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.DomainProcessing;
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
builder.Services.AddCommandApi();
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
app.MapSubscribeHandler();
app.MapActorsHandlers();
app.UseEventStore();

await app.RunAsync().ConfigureAwait(false);
