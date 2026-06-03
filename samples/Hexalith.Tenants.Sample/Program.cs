using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Client.Registration;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Sample.Endpoints;
using Hexalith.Tenants.Sample.Handlers;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 1. Register the Tenants domain consumer (DaprClient, platform subscription plumbing, local projection)
//    plus any extra handlers via the platform A3 generic registration.
builder.Services
    .AddHexalithTenants()
    .AddEventStoreDomainEventHandler<UserAddedToTenant, SampleLoggingEventHandler>()
    .AddEventStoreDomainEventHandler<UserRemovedFromTenant, SampleLoggingEventHandler>()
    .AddEventStoreDomainEventHandler<TenantDisabled, SampleLoggingEventHandler>();

WebApplication app = builder.Build();

// 3. Enable CloudEvents middleware (required for DAPR pub/sub)
app.UseCloudEvents();

// 4. Map DAPR subscription handler (discovers subscriptions)
app.MapSubscribeHandler();

// 5. Map the tenant event subscription endpoint (platform A3 generic, configured for /tenants/events)
app.MapEventStoreDomainEvents();

// 6. Map sample access-check endpoint
app.MapAccessCheckEndpoints();
app.MapTenantConfigurationEndpoints();

// 7. Liveness/health endpoints for Aspire topology verification (Sample does not use
//    ServiceDefaults). /alive matches the ASP.NET Core liveness convention used by the
//    AspireTopologyFixture process-liveness probe; /health is kept for backwards-compatibility.
app.MapGet("/alive", () => Results.Ok("alive"));
app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();
