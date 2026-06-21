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
    .AddEventStoreDomainEventHandler<TenantDisabled, SampleLoggingEventHandler>()
    // Memories search-index maintenance: publish one curated SearchIndexEntryChanged per tenant lifecycle
    // event to the Memories ingestion topic (search-as-index-only). Co-located with the local projection
    // the publisher reads; kept out of the broker-free Client package.
    .AddEventStoreDomainEventHandler<TenantCreated, MemoriesSearchIndexEventPublisher>()
    .AddEventStoreDomainEventHandler<TenantUpdated, MemoriesSearchIndexEventPublisher>()
    .AddEventStoreDomainEventHandler<TenantDisabled, MemoriesSearchIndexEventPublisher>()
    .AddEventStoreDomainEventHandler<TenantEnabled, MemoriesSearchIndexEventPublisher>();

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

// 7. The shared EventStore app may probe every configured app-id for admin metadata.
//    This sample is a pub/sub consumer, so it explicitly reports no domain catalog.
app.MapPost(
    "/admin/operational-index-metadata",
    (AdminOperationalIndexMetadata.Request _) => Results.Ok(new AdminOperationalIndexMetadata.Response([])));

// 8. Liveness/health endpoints for Aspire topology verification (Sample does not use
//    ServiceDefaults). /alive matches the ASP.NET Core liveness convention used by the
//    AspireTopologyFixture process-liveness probe; /health is kept for backwards-compatibility.
app.MapGet("/alive", () => Results.Ok("alive"));
app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();
