using System.Net;
using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

public sealed class TenantCommandGatewayTests
{
    [Fact]
    public async Task Create_tenant_submits_literal_command_with_ulid_message_id_and_captures_correlation_id()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-123"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.CreateTenantAsync(
            new CreateTenantCommandRequest("Tenant.Mixed-01", "Mixed Tenant", "literal id"),
            CancellationToken.None);

        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        submitted.Tenant.ShouldBe("system");
        submitted.Domain.ShouldBe("tenants");
        submitted.AggregateId.ShouldBe("Tenant.Mixed-01");
        submitted.CommandType.ShouldBe(nameof(CreateTenant));
        submitted.CorrelationId.ShouldBeNull();
        submitted.Payload.GetProperty("TenantId").GetString().ShouldBe("Tenant.Mixed-01");
        submitted.Payload.GetProperty("Name").GetString().ShouldBe("Mixed Tenant");
        result.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.CorrelationId.ShouldBe("correlation-123");
    }

    [Fact]
    public async Task Create_tenant_maps_already_exists_rejection_to_safe_text()
    {
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)HttpStatusCode.Conflict,
            "TenantAlreadyExistsRejection",
            detail: "raw payload token stack trace correlation-123"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.CreateTenantAsync(
            new CreateTenantCommandRequest("tenant.alpha", "Alpha", null),
            CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        result.RejectionCode.ShouldBe("TenantAlreadyExists");
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain("already exists");
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-123", Case.Insensitive);
    }

    [Fact]
    public async Task Create_tenant_validation_failure_does_not_submit_to_eventstore()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-123"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.CreateTenantAsync(
            new CreateTenantCommandRequest("", "Alpha", null),
            CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
        result.SafeMessage.ShouldBe("Tenant id and name are required before the command can be submitted.");
        client.SubmittedCommands.ShouldBeEmpty();
    }

    [Fact]
    public async Task Create_tenant_maps_forbidden_rejection_to_safe_authorization_text()
    {
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)HttpStatusCode.Forbidden,
            "InsufficientPermissionsRejection",
            detail: "raw payload bearer-token stack trace correlation-123"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.CreateTenantAsync(
            new CreateTenantCommandRequest("tenant.alpha", "Alpha", null),
            CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        result.RejectionCode.ShouldBe("InsufficientPermissions");
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain("not authorized");
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-123", Case.Insensitive);
    }

    [Fact]
    public async Task Status_lookup_uses_returned_correlation_id_not_message_id()
    {
        StatusHandler handler = new("""
            {
              "correlationId": "correlation-123",
              "status": "Completed",
              "statusCode": 4,
              "timestamp": "2026-06-06T02:00:00Z",
              "aggregateId": "Tenant.Mixed-01",
              "eventCount": 1,
              "rejectionEventType": null,
              "failureReason": null,
              "timeoutDuration": null
            }
            """);
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-123")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(handler) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-123"),
            CancellationToken.None);

        handler.RequestUri.ShouldNotBeNull().ToString().ShouldEndWith("/api/v1/commands/status/correlation-123");
        handler.RequestUri.ToString().ShouldNotContain("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.Status.ShouldBe(CommandStatus.Completed);
    }

    [Fact]
    public async Task Status_lookup_maps_rejection_without_raw_failure_details()
    {
        StatusHandler handler = new("""
            {
              "correlationId": "correlation-123",
              "status": "Rejected",
              "statusCode": 5,
              "timestamp": "2026-06-06T02:00:00Z",
              "aggregateId": "tenant.alpha",
              "eventCount": 1,
              "rejectionEventType": "Hexalith.Tenants.Contracts.Events.Rejections.TenantAlreadyExistsRejection",
              "failureReason": "raw payload token stack trace correlation-123",
              "timeoutDuration": null
            }
            """);
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-123")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(handler) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("message-123", "correlation-123"),
            CancellationToken.None);

        result.Status.ShouldBe(CommandStatus.Rejected);
        result.RejectionCode.ShouldBe("TenantAlreadyExists");
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain("already exists");
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-123", Case.Insensitive);
    }

    [Fact]
    public async Task Status_lookup_maps_not_found_to_unable_to_verify_without_raw_details()
    {
        StatusHandler handler = new("{}", HttpStatusCode.NotFound);
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-123")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(handler) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("message-123", "correlation-123"),
            CancellationToken.None);

        result.Status.ShouldBeNull();
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldBe("Command status is not available yet.");
        safeMessage.ShouldNotContain("correlation-123", Case.Insensitive);
    }

    [Fact]
    public async Task Status_lookup_maps_malformed_payload_to_unable_to_verify()
    {
        StatusHandler handler = new("{ not-json");
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-123")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(handler) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("message-123", "correlation-123"),
            CancellationToken.None);

        result.Status.ShouldBeNull();
        result.SafeMessage.ShouldBe("Command status response was unavailable.");
    }

    [Theory]
    [InlineData("PublishFailed", CommandStatus.PublishFailed, "publication could not be verified")]
    [InlineData("TimedOut", CommandStatus.TimedOut, "timed out")]
    public async Task Status_lookup_maps_terminal_non_success_states_to_safe_messages(
        string statusText,
        CommandStatus expectedStatus,
        string expectedMessage)
    {
        StatusHandler handler = new($$"""
            {
              "correlationId": "correlation-123",
              "status": "{{statusText}}",
              "statusCode": 6,
              "timestamp": "2026-06-06T02:00:00Z",
              "aggregateId": "tenant.alpha",
              "eventCount": 0,
              "rejectionEventType": null,
              "failureReason": "raw payload token stack trace correlation-123",
              "timeoutDuration": null
            }
            """);
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-123")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(handler) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("message-123", "correlation-123"),
            CancellationToken.None);

        result.Status.ShouldBe(expectedStatus);
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain(expectedMessage);
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-123", Case.Insensitive);
    }

    private sealed class CapturingGatewayClient(object response) : IEventStoreGatewayClient
    {
        public List<SubmitCommandRequest> SubmittedCommands { get; } = [];

        public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
        {
            SubmittedCommands.Add(request);
            if (response is Exception exception)
            {
                throw exception;
            }

            return Task.FromResult((SubmitCommandResponse)response);
        }

        public Task<EventStoreQueryResult> SubmitQueryAsync(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubUlidFactory(string id) : IUlidFactory
    {
        public string NewUlid() => id;
    }

    private sealed class StatusHandler(string body, HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body),
            });
        }
    }
}
