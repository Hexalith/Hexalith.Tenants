using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Registration;
using Hexalith.Tenants.Client.Subscription;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Sample.Endpoints;
using Hexalith.Tenants.Sample.Handlers;

var builder = WebApplication.CreateBuilder(args);

// 1. Register all tenant client services (DaprClient, options, event handlers, projections)
builder.Services.AddHexalithTenants();

// 2. Register sample-specific logging handler (demonstrates extensibility)
builder.Services.AddSingleton<SampleLoggingEventHandler>();
builder.Services.AddSingleton<ITenantEventHandler<UserAddedToTenant>>(sp => sp.GetRequiredService<SampleLoggingEventHandler>());
builder.Services.AddSingleton<ITenantEventHandler<UserRemovedFromTenant>>(sp => sp.GetRequiredService<SampleLoggingEventHandler>());
builder.Services.AddSingleton<ITenantEventHandler<TenantDisabled>>(sp => sp.GetRequiredService<SampleLoggingEventHandler>());

var app = builder.Build();

// 3. Enable CloudEvents middleware (required for DAPR pub/sub)
app.UseCloudEvents();

// 4. Map DAPR subscription handler (discovers subscriptions)
app.MapSubscribeHandler();

// 5. Map tenant event subscription endpoint
app.MapTenantEventSubscription();

// 6. Map sample access-check endpoint
app.MapAccessCheckEndpoints();

// 7. Health endpoint for Aspire topology verification (Sample does not use ServiceDefaults)
app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();
