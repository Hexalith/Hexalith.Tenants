using System.Text.Json;

using Dapr;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Registration;
using Hexalith.Tenants.Client.Subscription;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.Tenants.Client.Tests.Subscription;

public class TenantEventSubscriptionEndpointsTests {
    private static readonly DateTimeOffset _occurredAt = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task MapTenantEventSubscription_KnownTenantEvent_ReturnsOkAndDispatchesHandler() {
        // Arrange
        var sink = new TrackingSink();
        await using WebApplication app = CreateApp(
            services => {
                _ = services.AddSingleton(sink);
                _ = services
                    .AddHexalithTenants()
                    .AddTenantEventHandler<TenantCreated, TrackingTenantCreatedHandler>();
            });
        RouteEndpoint endpoint = GetTenantEventEndpoint(app);
        TenantEventEnvelope envelope = CreateEnvelope("msg-1", new TenantCreated("acme", "Acme Corp", null, _occurredAt));

        // Act
        int statusCode = await PostEnvelopeAsync(endpoint, app.Services, envelope);

        // Assert
        statusCode.ShouldBe(StatusCodes.Status200OK);
        sink.TenantIds.ShouldBe(["acme"]);
    }

    [Fact]
    public async Task MapTenantEventSubscription_DuplicateMessage_ReturnsOkWithoutRedispatchingHandler() {
        // Arrange
        var sink = new TrackingSink();
        await using WebApplication app = CreateApp(
            services => {
                _ = services.AddSingleton(sink);
                _ = services
                    .AddHexalithTenants()
                    .AddTenantEventHandler<TenantCreated, TrackingTenantCreatedHandler>();
            });
        RouteEndpoint endpoint = GetTenantEventEndpoint(app);
        TenantEventEnvelope envelope = CreateEnvelope("msg-1", new TenantCreated("acme", "Acme Corp", null, _occurredAt));

        // Act
        int firstStatusCode = await PostEnvelopeAsync(endpoint, app.Services, envelope);
        int duplicateStatusCode = await PostEnvelopeAsync(endpoint, app.Services, envelope);

        // Assert
        firstStatusCode.ShouldBe(StatusCodes.Status200OK);
        duplicateStatusCode.ShouldBe(StatusCodes.Status200OK);
        sink.TenantIds.ShouldBe(["acme"]);
    }

    [Fact]
    public async Task MapTenantEventSubscription_UnknownEventType_ReturnsOkAndSkipsHandlerDispatch() {
        // Arrange
        var sink = new TrackingSink();
        await using WebApplication app = CreateApp(
            services => {
                _ = services.AddSingleton(sink);
                _ = services
                    .AddHexalithTenants()
                    .AddTenantEventHandler<TenantCreated, TrackingTenantCreatedHandler>();
            });
        RouteEndpoint endpoint = GetTenantEventEndpoint(app);
        var envelope = new TenantEventEnvelope(
            "msg-unknown",
            "acme",
            "system",
            "Unknown.Event.Type",
            1,
            _occurredAt,
            "corr-1",
            "json",
            []);

        // Act
        int statusCode = await PostEnvelopeAsync(endpoint, app.Services, envelope);

        // Assert
        statusCode.ShouldBe(StatusCodes.Status200OK);
        sink.TenantIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task MapTenantEventSubscription_InvalidKnownEventPayload_ReturnsProblemResponse() {
        // Arrange
        await using WebApplication app = CreateApp(services => _ = services.AddHexalithTenants());
        RouteEndpoint endpoint = GetTenantEventEndpoint(app);
        var envelope = new TenantEventEnvelope(
            "msg-invalid",
            "acme",
            "system",
            typeof(TenantCreated).FullName!,
            1,
            _occurredAt,
            "corr-1",
            "json",
            [1, 2, 3]);

        // Act
        int statusCode = await PostEnvelopeAsync(endpoint, app.Services, envelope);

        // Assert
        statusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task MapTenantEventSubscription_PayloadTenantIdMismatch_ReturnsProblemResponse() {
        // Arrange
        var sink = new TrackingSink();
        await using WebApplication app = CreateApp(
            services => {
                _ = services.AddSingleton(sink);
                _ = services
                    .AddHexalithTenants()
                    .AddTenantEventHandler<TenantCreated, TrackingTenantCreatedHandler>();
            });
        RouteEndpoint endpoint = GetTenantEventEndpoint(app);
        var envelope = new TenantEventEnvelope(
            "msg-mismatch",
            "acme",
            "system",
            typeof(TenantCreated).FullName!,
            1,
            _occurredAt,
            "corr-1",
            "json",
            JsonSerializer.SerializeToUtf8Bytes(new TenantCreated("beta", "Beta Corp", null, _occurredAt)));

        // Act
        int statusCode = await PostEnvelopeAsync(endpoint, app.Services, envelope);

        // Assert
        statusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        sink.TenantIds.ShouldBeEmpty();
    }

    [Fact]
    public void MapTenantEventSubscription_MapsConfiguredPubSubTopicToPostEndpoint() {
        // Arrange
        using WebApplication app = CreateApp(
            services => _ = services.AddHexalithTenants(options => {
                options.PubSubName = "consumer-pubsub";
                options.TopicName = "consumer.tenants.events";
            }));

        // Act
        RouteEndpoint endpoint = GetTenantEventEndpoint(app);
        TopicAttribute topic = endpoint.Metadata.GetMetadata<TopicAttribute>()
            ?? throw new ShouldAssertException("Expected DAPR topic metadata on tenant event endpoint.");
        HttpMethodMetadata httpMethods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()
            ?? throw new ShouldAssertException("Expected HTTP method metadata on tenant event endpoint.");

        // Assert
        endpoint.RoutePattern.RawText.ShouldBe("/tenants/events");
        httpMethods.HttpMethods.ShouldContain(HttpMethods.Post);
        topic.PubsubName.ShouldBe("consumer-pubsub");
        topic.Name.ShouldBe("consumer.tenants.events");
    }

    [Fact]
    public void MapTenantEventSubscription_NullEndpoints_ThrowsArgumentNullException() =>
        // Assert
        Should.Throw<ArgumentNullException>(() => TenantEventSubscriptionEndpoints.MapTenantEventSubscription(null!));

    private static WebApplication CreateApp(Action<IServiceCollection> configureServices) {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        configureServices(builder.Services);
        WebApplication app = builder.Build();
        _ = app.MapTenantEventSubscription();
        return app;
    }

    private static TenantEventEnvelope CreateEnvelope<TEvent>(string messageId, TEvent @event)
        where TEvent : IEventPayload => new(
            messageId,
            "acme",
            "system",
            typeof(TEvent).FullName!,
            1,
            _occurredAt,
            "corr-1",
            "json",
            JsonSerializer.SerializeToUtf8Bytes(@event));

    private static RouteEndpoint GetTenantEventEndpoint(WebApplication app) {
        IEndpointRouteBuilder routeBuilder = app;
        return routeBuilder.DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.RoutePattern.RawText == "/tenants/events");
    }

    private static async Task<int> PostEnvelopeAsync(RouteEndpoint endpoint, IServiceProvider services, TenantEventEnvelope envelope) {
        var context = new DefaultHttpContext {
            RequestServices = services,
        };
        context.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature());
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/tenants/events";
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream();
        context.Response.Body = new MemoryStream();

        await JsonSerializer.SerializeAsync(context.Request.Body, envelope);
        context.Request.ContentLength = context.Request.Body.Length;
        context.Request.Body.Position = 0;

        await endpoint.RequestDelegate!(context);

        return context.Response.StatusCode;
    }

    private sealed class TrackingSink {
        public List<string> TenantIds { get; } = [];
    }

    private sealed class TrackingTenantCreatedHandler : ITenantEventHandler<TenantCreated> {
        private readonly TrackingSink _sink;

        public TrackingTenantCreatedHandler(TrackingSink sink) {
            ArgumentNullException.ThrowIfNull(sink);
            _sink = sink;
        }

        public Task HandleAsync(TenantCreated @event, TenantEventContext context, CancellationToken cancellationToken = default) {
            _sink.TenantIds.Add(@event.TenantId);
            return Task.CompletedTask;
        }
    }

    private sealed class RequestBodyDetectionFeature : IHttpRequestBodyDetectionFeature {
        public bool CanHaveBody => true;
    }
}
