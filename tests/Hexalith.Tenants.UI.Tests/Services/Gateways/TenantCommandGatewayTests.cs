using System.Net;
using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Problems;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

public sealed class TenantCommandGatewayTests
{
    [Fact]
    public void FixedScopeGatewayCapabilitiesFailClosedOrAdvertiseOnlyConcreteSupport()
    {
        TenantCommandGateway configured = CreateLifecycleGateway(new SubmitCommandResponse("correlation"));
        var unavailable = new UnavailableTenantCommandGateway();

        configured.SupportsGlobalAdministratorDispatch.ShouldBeTrue();
        configured.SupportsTrackedGlobalAdministratorDispatch.ShouldBeTrue();
        configured.SupportsCommandStatusLookup.ShouldBeTrue();
        unavailable.SupportsGlobalAdministratorDispatch.ShouldBeFalse();
        unavailable.SupportsTrackedGlobalAdministratorDispatch.ShouldBeFalse();
        unavailable.SupportsCommandStatusLookup.ShouldBeFalse();
    }

    [Fact]
    public async Task TrackedGlobalAdministratorGrantPreservesExactLiteralAndCallerUlid()
    {
        const string messageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        const string target = "  User/CaseSensitive.01  ";
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-global-admin"));
        TenantCommandGateway gateway = CreateGateway(client);

        TenantCommandSubmissionResult result = await gateway.SetGlobalAdministratorTrackedAsync(
            new SetGlobalAdministrator(target),
            messageId,
            CancellationToken.None);

        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe(messageId);
        submitted.Tenant.ShouldBe("system");
        submitted.Domain.ShouldBe("global-administrators");
        submitted.AggregateId.ShouldBe("global-administrators");
        submitted.Payload.GetProperty("UserId").GetString().ShouldBe(target);
        result.MessageId.ShouldBe(messageId);
    }

    [Theory]
    [InlineData("not-a-ulid")]
    [InlineData("01arz3ndektsv4rrffq69g5fav")]
    [InlineData(" 01ARZ3NDEKTSV4RRFFQ69G5FAV")]
    public async Task TrackedGlobalAdministratorGrantRejectsNonCanonicalMessageId(string messageId)
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-unused"));

        TenantCommandSubmissionResult result = await CreateGateway(client)
            .SetGlobalAdministratorTrackedAsync(
                new SetGlobalAdministrator("target-admin"),
                messageId,
                CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
        result.SafeMessageKey.ShouldBe("Tenants.Commands.Unavailable.InvalidTrackingReference");
        client.SubmittedCommands.ShouldBeEmpty();
    }

    [Fact]
    public async Task SameIdRedispatchUsesNoReplacementIdentity()
    {
        const string messageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-global-admin"));
        TenantCommandGateway gateway = CreateGateway(client);
        var request = new SetGlobalAdministrator("target-admin");

        _ = await gateway.SetGlobalAdministratorTrackedAsync(request, messageId, CancellationToken.None);
        _ = await gateway.SetGlobalAdministratorTrackedAsync(request, messageId, CancellationToken.None);

        client.SubmittedCommands.Count.ShouldBe(2);
        client.SubmittedCommands.ShouldAllBe(command => command.MessageId == messageId);
    }

    public static IEnumerable<object[]> AmbiguousGrantExceptions()
    {
        yield return [new EventStoreGatewayException((int)HttpStatusCode.RequestTimeout, "timeout")];
        yield return [new EventStoreGatewayException((int)HttpStatusCode.TooManyRequests, "throttled")];
        yield return [new EventStoreGatewayException((int)HttpStatusCode.ServiceUnavailable, "unavailable")];
        yield return [new EventStoreGatewayException((int)HttpStatusCode.BadRequest, "retryable", retryable: true)];
        yield return [new OperationCanceledException("transport cancellation")];
        yield return [new TaskCanceledException("transport timeout")];
        yield return [new HttpRequestException("connection failed")];
        yield return [new TimeoutException("plain timeout")];
    }

    [Theory]
    [MemberData(nameof(AmbiguousGrantExceptions))]
    public async Task AmbiguousGrantTransportRetainsCallerMessageId(Exception exception)
    {
        const string messageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        CapturingGatewayClient client = new(exception);

        TenantCommandSubmissionResult result = await CreateGateway(client)
            .SetGlobalAdministratorTrackedAsync(
                new SetGlobalAdministrator("target-admin"),
                messageId,
                CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.RequestSent);
        result.IsAmbiguousFailure.ShouldBeTrue();
        result.MessageId.ShouldBe(messageId);
        client.SubmittedCommands.ShouldHaveSingleItem().MessageId.ShouldBe(messageId);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, TenantCommandLifecycleState.Failed)]
    [InlineData(HttpStatusCode.Unauthorized, TenantCommandLifecycleState.Rejected)]
    [InlineData(HttpStatusCode.Forbidden, TenantCommandLifecycleState.Rejected)]
    [InlineData(HttpStatusCode.Conflict, TenantCommandLifecycleState.Failed)]
    public async Task NonRetryableGrantRejectionsStayTerminal(
        HttpStatusCode statusCode,
        TenantCommandLifecycleState expectedState)
    {
        const string messageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)statusCode,
            "definite rejection",
            retryable: false));

        TenantCommandSubmissionResult result = await CreateGateway(client)
            .SetGlobalAdministratorTrackedAsync(
                new SetGlobalAdministrator("target-admin"),
                messageId,
                CancellationToken.None);

        result.State.ShouldBe(expectedState);
        result.IsAmbiguousFailure.ShouldBeFalse();
        result.MessageId.ShouldBe(messageId);
        client.SubmittedCommands.ShouldHaveSingleItem().MessageId.ShouldBe(messageId);
    }

    [Fact]
    public async Task GlobalAdministratorStatusVerifiesFixedAggregateTrackingHandle()
    {
        const string body = """
            {
              "correlationId": "correlation-global-admin",
              "status": "EventsPublished",
              "statusCode": 3,
              "timestamp": "2026-08-31T02:00:00Z",
              "aggregateId": "global-administrators",
              "eventCount": 1,
              "messageId": "01ARZ3NDEKTSV4RRFFQ69G5FAV"
            }
            """;
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("unused")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(new StatusHandler(body)) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle(
                "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                "correlation-global-admin",
                "global-administrators"),
            CancellationToken.None);

        result.Status.ShouldBe(CommandStatus.EventsPublished);
        result.EventCount.ShouldBe(1);
        result.HasVerifiedCommandIdentity.ShouldBeTrue();
    }

    [Fact]
    public async Task Set_global_administrator_submits_fixed_scope_command_with_literal_user_payload()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-global-admin"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.SetGlobalAdministratorAsync(
            new SetGlobalAdministrator("User/CaseSensitive.01"),
            CancellationToken.None);

        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        submitted.Tenant.ShouldBe("system");
        submitted.Domain.ShouldBe("global-administrators");
        submitted.AggregateId.ShouldBe("global-administrators");
        submitted.CommandType.ShouldBe(nameof(SetGlobalAdministrator));
        submitted.Payload.GetProperty("UserId").GetString().ShouldBe("User/CaseSensitive.01");
        submitted.Payload.TryGetProperty("TenantId", out _).ShouldBeFalse();
        submitted.Payload.TryGetProperty("Role", out _).ShouldBeFalse();
        result.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.CorrelationId.ShouldBe("correlation-global-admin");
    }

    [Fact]
    public async Task Remove_global_administrator_submits_fixed_scope_command_with_literal_user_payload()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-global-admin-remove"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.RemoveGlobalAdministratorAsync(
            new RemoveGlobalAdministrator("User/CaseSensitive.01"),
            CancellationToken.None);

        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        submitted.Tenant.ShouldBe("system");
        submitted.Domain.ShouldBe("global-administrators");
        submitted.AggregateId.ShouldBe("global-administrators");
        submitted.CommandType.ShouldBe(nameof(RemoveGlobalAdministrator));
        submitted.Payload.GetProperty("UserId").GetString().ShouldBe("User/CaseSensitive.01");
        submitted.Payload.TryGetProperty("TenantId", out _).ShouldBeFalse();
        submitted.Payload.TryGetProperty("Role", out _).ShouldBeFalse();
        result.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.CorrelationId.ShouldBe("correlation-global-admin-remove");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Remove_global_administrator_validation_failure_does_not_submit_to_eventstore(string? userId)
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-global-admin-remove"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.RemoveGlobalAdministratorAsync(
            new RemoveGlobalAdministrator(userId!),
            CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
        result.SafeMessage.ShouldNotBeNull().ShouldContain("User id");
        client.SubmittedCommands.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("LastGlobalAdministratorRejection", "LastGlobalAdministrator", "last global administrator")]
    [InlineData("GlobalAdministratorNotFoundRejection", "GlobalAdministratorNotFound", "not a global administrator")]
    [InlineData("InsufficientPermissionsRejection", "InsufficientPermissions", "platform governance")]
    public async Task Remove_global_administrator_maps_safe_rejection_text(
        string reason,
        string expectedCode,
        string expectedText)
    {
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)HttpStatusCode.Conflict,
            reason,
            detail: "raw payload bearer-token stack trace correlation-global-admin UserId=secret-user"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.RemoveGlobalAdministratorAsync(
            new RemoveGlobalAdministrator("secret-user"),
            CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        result.RejectionCode.ShouldBe(expectedCode);
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain(expectedText, Case.Insensitive);
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-global-admin", Case.Insensitive);
        safeMessage.ShouldNotContain("secret-user", Case.Insensitive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Set_global_administrator_validation_failure_does_not_submit_to_eventstore(string? userId)
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-global-admin"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.SetGlobalAdministratorAsync(
            new SetGlobalAdministrator(userId!),
            CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
        result.SafeMessage.ShouldNotBeNull().ShouldContain("User id");
        client.SubmittedCommands.ShouldBeEmpty();
    }

    [Fact]
    public async Task Set_global_administrator_service_unavailable_uses_platform_governance_copy()
    {
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)HttpStatusCode.ServiceUnavailable,
            "gateway unavailable",
            detail: "raw payload bearer-token stack trace correlation-global-admin"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.SetGlobalAdministratorAsync(
            new SetGlobalAdministrator("target-user"),
            CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.RequestSent);
        result.IsAmbiguousFailure.ShouldBeTrue();
        result.SafeMessageKey.ShouldBe("Tenants.GlobalAdministrators.Grant.SubmissionEvidence.Ambiguous");
        result.SafeMessage.ShouldBeNull();
    }

    [Theory]
    [InlineData("GlobalAdministratorAlreadyExistsRejection", "GlobalAdministratorAlreadyExists", "already a global administrator")]
    [InlineData("InsufficientPermissionsRejection", "InsufficientPermissions", "platform governance")]
    public async Task Set_global_administrator_maps_safe_rejection_text(
        string reason,
        string expectedCode,
        string expectedText)
    {
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)HttpStatusCode.Conflict,
            reason,
            detail: "raw payload bearer-token stack trace correlation-global-admin UserId=secret-user"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.SetGlobalAdministratorAsync(
            new SetGlobalAdministrator("secret-user"),
            CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        result.RejectionCode.ShouldBe(expectedCode);
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain(expectedText, Case.Insensitive);
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-global-admin", Case.Insensitive);
        safeMessage.ShouldNotContain("secret-user", Case.Insensitive);
    }

    [Fact]
    public async Task Status_lookup_maps_global_administrator_already_exists_to_rejected_safe_text()
    {
        StatusHandler handler = new("""
            {
              "correlationId": "correlation-global-admin",
              "status": "Rejected",
              "statusCode": 5,
              "timestamp": "2026-06-06T02:00:00Z",
              "aggregateId": "global-administrators",
              "eventCount": 0,
              "rejectionEventType": "Hexalith.Tenants.Contracts.Events.Rejections.GlobalAdministratorAlreadyExistsRejection",
              "failureReason": "raw payload token stack trace correlation-global-admin UserId=secret-user",
              "timeoutDuration": null
            }
            """);
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-global-admin")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(handler) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("message-global-admin", "correlation-global-admin"),
            CancellationToken.None);

        result.Status.ShouldBe(CommandStatus.Rejected);
        result.RejectionCode.ShouldBe("GlobalAdministratorAlreadyExists");
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain("already a global administrator", Case.Insensitive);
        safeMessage.ShouldNotContain("already applied", Case.Insensitive);
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-global-admin", Case.Insensitive);
        safeMessage.ShouldNotContain("secret-user", Case.Insensitive);
    }

    [Theory]
    [InlineData("Hexalith.Tenants.Contracts.Events.Rejections.LastGlobalAdministratorRejection", "LastGlobalAdministrator", "last global administrator")]
    [InlineData("Hexalith.Tenants.Contracts.Events.Rejections.GlobalAdministratorNotFoundRejection", "GlobalAdministratorNotFound", "not a global administrator")]
    public async Task Status_lookup_maps_global_administrator_remove_rejections_to_safe_text(
        string rejectionEventType,
        string expectedCode,
        string expectedText)
    {
        StatusHandler handler = new($$"""
            {
              "correlationId": "correlation-global-admin-remove",
              "status": "Rejected",
              "statusCode": 5,
              "timestamp": "2026-06-06T02:00:00Z",
              "aggregateId": "global-administrators",
              "eventCount": 0,
              "rejectionEventType": "{{rejectionEventType}}",
              "failureReason": "raw payload token stack trace correlation-global-admin-remove UserId=secret-user",
              "timeoutDuration": null
            }
            """);
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-global-admin-remove")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(handler) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("message-global-admin-remove", "correlation-global-admin-remove"),
            CancellationToken.None);

        result.Status.ShouldBe(CommandStatus.Rejected);
        result.RejectionCode.ShouldBe(expectedCode);
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain(expectedText, Case.Insensitive);
        safeMessage.ShouldNotContain("success", Case.Insensitive);
        safeMessage.ShouldNotContain("remove member", Case.Insensitive);
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-global-admin-remove", Case.Insensitive);
        safeMessage.ShouldNotContain("secret-user", Case.Insensitive);
    }

    [Theory]
    [InlineData(TenantLifecycleOperation.EnableTenant, nameof(EnableTenant), "correlation-enable")]
    [InlineData(TenantLifecycleOperation.DisableTenant, nameof(DisableTenant), "correlation-disable")]
    public async Task Lifecycle_command_submits_literal_tenant_id_payload_and_captures_correlation_id(
        TenantLifecycleOperation operation,
        string expectedCommandType,
        string correlationId)
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse(correlationId));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });
        var request = new TenantLifecycleCommandRequest("Tenant.Mixed-01", operation);
        const string stableMessageId = "01ARZ3NDEKTSV4RRFFQ69G5FB0";

        TenantCommandSubmissionResult result = operation is TenantLifecycleOperation.EnableTenant
            ? await gateway.EnableTenantTrackedAsync(request, stableMessageId, CancellationToken.None)
            : await gateway.DisableTenantTrackedAsync(request, stableMessageId, CancellationToken.None);

        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe(stableMessageId);
        submitted.Tenant.ShouldBe("system");
        submitted.Domain.ShouldBe("tenants");
        submitted.AggregateId.ShouldBe("Tenant.Mixed-01");
        submitted.CommandType.ShouldBe(expectedCommandType);
        submitted.Payload.GetProperty("TenantId").GetString().ShouldBe("Tenant.Mixed-01");
        result.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        result.MessageId.ShouldBe(stableMessageId);
        result.CorrelationId.ShouldBe(correlationId);
    }

    [Theory]
    [InlineData("TenantLifecycleStateAlreadySetRejection", "TenantLifecycleStateAlreadySet", "already matches")]
    [InlineData("TenantDisabledRejection", "TenantDisabled", "disabled")]
    [InlineData("TenantNotFoundRejection", "TenantNotFound", "not found")]
    [InlineData("InsufficientPermissionsRejection", "InsufficientPermissions", "not authorized")]
    public async Task Lifecycle_command_maps_safe_rejection_text(
        string reason,
        string expectedCode,
        string expectedText)
    {
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)HttpStatusCode.Conflict,
            reason,
            detail: "raw payload bearer-token stack trace correlation-life"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.DisableTenantAsync(
            new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant),
            cancellationToken: CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        result.RejectionCode.ShouldBe(expectedCode);
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain(expectedText, Case.Insensitive);
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-life", Case.Insensitive);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Lifecycle_submission_maps_ambiguous_http_failures_to_same_identity_retry(
        HttpStatusCode statusCode)
    {
        const string messageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        TenantCommandGateway gateway = CreateLifecycleGateway(
            new EventStoreGatewayException((int)statusCode, "raw transport token"));

        TenantCommandSubmissionResult result = await gateway.DisableTenantTrackedAsync(
            new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant),
            messageId,
            CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.RequestSent);
        result.IsAmbiguousFailure.ShouldBeTrue();
        result.MessageId.ShouldBe(messageId);
        result.SafeMessageKey.ShouldBe("Tenants.Lifecycle.SubmissionEvidence.Ambiguous");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, TenantCommandLifecycleState.Failed)]
    [InlineData(HttpStatusCode.Unauthorized, TenantCommandLifecycleState.Rejected)]
    [InlineData(HttpStatusCode.Forbidden, TenantCommandLifecycleState.Rejected)]
    public async Task Lifecycle_submission_keeps_permanent_http_rejections_terminal(
        HttpStatusCode statusCode,
        TenantCommandLifecycleState expectedState)
    {
        TenantCommandGateway gateway = CreateLifecycleGateway(
            new EventStoreGatewayException((int)statusCode, "raw rejection token"));

        TenantCommandSubmissionResult result = await gateway.DisableTenantTrackedAsync(
            new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant),
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            CancellationToken.None);

        result.State.ShouldBe(expectedState);
        result.IsAmbiguousFailure.ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(AmbiguousTransportExceptions))]
    public async Task Lifecycle_submission_maps_transport_and_timeout_exceptions_to_same_identity_retry(
        Exception exception)
    {
        const string messageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        TenantCommandGateway gateway = CreateLifecycleGateway(exception);

        TenantCommandSubmissionResult result = await gateway.EnableTenantTrackedAsync(
            new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.EnableTenant),
            messageId,
            CancellationToken.None);

        result.IsAmbiguousFailure.ShouldBeTrue();
        result.MessageId.ShouldBe(messageId);
    }

    public static TheoryData<Exception> AmbiguousTransportExceptions
        => new()
        {
            new HttpRequestException("transport unavailable"),
            new TaskCanceledException("gateway timeout"),
        };

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Tracked_lifecycle_dispatch_rejects_blank_explicit_message_id_before_transport(
        string? messageId)
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-life"));
        TenantCommandGateway gateway = new(
            client,
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(new StatusHandler("{}")) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandSubmissionResult result = await gateway.DisableTenantTrackedAsync(
            new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant),
            messageId!,
            CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
        result.SafeMessageKey.ShouldBe("Tenants.Commands.Unavailable.InvalidTrackingReference");
        client.SubmittedCommands.ShouldBeEmpty();
    }

    [Fact]
    public async Task Update_tenant_submits_literal_command_with_payload_and_captures_correlation_id()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-update"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.UpdateTenantAsync(
            new UpdateTenant("Tenant.Mixed-01", "Updated tenant", string.Empty),
            cancellationToken: CancellationToken.None);

        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        submitted.Tenant.ShouldBe("system");
        submitted.Domain.ShouldBe("tenants");
        submitted.AggregateId.ShouldBe("Tenant.Mixed-01");
        submitted.CommandType.ShouldBe(nameof(UpdateTenant));
        submitted.Payload.GetProperty("TenantId").GetString().ShouldBe("Tenant.Mixed-01");
        submitted.Payload.GetProperty("Name").GetString().ShouldBe("Updated tenant");
        submitted.Payload.GetProperty("Description").GetString().ShouldBe(string.Empty);
        result.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.CorrelationId.ShouldBe("correlation-update");
    }

    [Fact]
    public async Task Update_tenant_reuses_provided_message_id_instead_of_minting()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-reuse"));
        StubUlidFactory ulids = new("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        TenantCommandGateway gateway = new(client, ulids, new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.UpdateTenantAsync(
            new UpdateTenant("tenant.alpha", "Alpha", null),
            messageId: "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            cancellationToken: CancellationToken.None);

        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        ulids.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Update_tenant_gateway_exception_retains_resolved_message_id_for_exact_retry()
    {
        const string messageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)HttpStatusCode.ServiceUnavailable,
            "gateway unavailable"));
        StubUlidFactory ulids = new(messageId);
        TenantCommandGateway gateway = new(client, ulids, new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.UpdateTenantAsync(
            new UpdateTenant("tenant.alpha", "Alpha", null),
            cancellationToken: CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
        result.MessageId.ShouldBe(messageId);
        client.SubmittedCommands.ShouldHaveSingleItem().MessageId.ShouldBe(messageId);
        ulids.CallCount.ShouldBe(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Update_tenant_validation_failure_does_not_submit_to_eventstore(string? name)
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-update"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.UpdateTenantAsync(
            new UpdateTenant("tenant.alpha", name!, "description"),
            cancellationToken: CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
        result.SafeMessage.ShouldNotBeNull().ShouldContain("Tenant id and name are required");
        client.SubmittedCommands.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("InsufficientPermissionsRejection", "InsufficientPermissions", "not authorized")]
    [InlineData("TenantDisabledRejection", "TenantDisabled", "disabled")]
    [InlineData("TenantNotFoundRejection", "TenantNotFound", "not found")]
    public async Task Update_tenant_maps_safe_rejection_text(
        string reason,
        string expectedCode,
        string expectedText)
    {
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)HttpStatusCode.Conflict,
            reason,
            detail: "raw payload bearer-token stack trace correlation-update"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.UpdateTenantAsync(
            new UpdateTenant("tenant.alpha", "Alpha", null),
            cancellationToken: CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        result.RejectionCode.ShouldBe(expectedCode);
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain(expectedText, Case.Insensitive);
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-update", Case.Insensitive);
    }

    [Fact]
    public async Task Set_tenant_configuration_submits_literal_command_with_payload_and_captures_correlation_id()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-config"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.SetTenantConfigurationAsync(
            new SetTenantConfiguration("Tenant.Mixed-01", "billing.mode", "enterprise"),
            CancellationToken.None);

        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        submitted.Tenant.ShouldBe("system");
        submitted.Domain.ShouldBe("tenants");
        submitted.AggregateId.ShouldBe("Tenant.Mixed-01");
        submitted.CommandType.ShouldBe(nameof(SetTenantConfiguration));
        submitted.Payload.GetProperty("TenantId").GetString().ShouldBe("Tenant.Mixed-01");
        submitted.Payload.GetProperty("Key").GetString().ShouldBe("billing.mode");
        submitted.Payload.GetProperty("Value").GetString().ShouldBe("enterprise");
        result.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.CorrelationId.ShouldBe("correlation-config");
    }

    [Fact]
    public async Task Tracked_set_configuration_uses_the_callers_exact_message_id()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-config"));
        TenantCommandGateway gateway = new(
            client,
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(new StatusHandler("{}")) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandSubmissionResult result = await gateway.SetTenantConfigurationTrackedAsync(
            new SetTenantConfiguration("tenant.alpha", "Billing.Mode", "enterprise"),
            "01ARZ3NDEKTSV4RRFFQ69G5FAA",
            CancellationToken.None);

        gateway.SupportsTrackedSetConfigurationDispatch.ShouldBeTrue();
        client.SubmittedCommands.ShouldHaveSingleItem().MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAA");
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAA");
    }

    [Fact]
    public async Task Tracked_set_configuration_retains_message_identity_when_delivery_is_ambiguous()
    {
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)HttpStatusCode.ServiceUnavailable,
            "temporarily-unavailable",
            detail: "raw payload Value=super-secret"));
        TenantCommandGateway gateway = new(
            client,
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(new StatusHandler("{}")) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandSubmissionResult result = await gateway.SetTenantConfigurationTrackedAsync(
            new SetTenantConfiguration("tenant.alpha", "billing.mode", "super-secret"),
            "01ARZ3NDEKTSV4RRFFQ69G5FAA",
            CancellationToken.None);

        result.IsAmbiguousFailure.ShouldBeTrue();
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAA");
        result.SafeMessageKey.ShouldBe("Tenants.Configuration.Set.SubmissionEvidence.Ambiguous");
        result.SafeMessage.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Set_tenant_configuration_validation_failure_does_not_submit_to_eventstore(string? key)
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-config"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.SetTenantConfigurationAsync(
            new SetTenantConfiguration("tenant.alpha", key!, "value"),
            CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
        result.SafeMessage.ShouldNotBeNull().ShouldContain("configuration key");
        client.SubmittedCommands.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("ConfigurationLimitExceededRejection", "ConfigurationLimitExceeded", "limits")]
    [InlineData("InsufficientPermissionsRejection", "InsufficientPermissions", "not authorized")]
    [InlineData("TenantDisabledRejection", "TenantDisabled", "disabled")]
    [InlineData("TenantNotFoundRejection", "TenantNotFound", "not found")]
    public async Task Set_tenant_configuration_maps_safe_rejection_text(
        string reason,
        string expectedCode,
        string expectedText)
    {
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)HttpStatusCode.Conflict,
            reason,
            detail: "raw payload bearer-token stack trace correlation-config Value=super-secret"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.SetTenantConfigurationAsync(
            new SetTenantConfiguration("tenant.alpha", "billing.mode", "super-secret"),
            CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        result.RejectionCode.ShouldBe(expectedCode);
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain(expectedText, Case.Insensitive);
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-config", Case.Insensitive);
        safeMessage.ShouldNotContain("super-secret", Case.Insensitive);
    }

    [Fact]
    public async Task Status_lookup_maps_configuration_limit_rejection_to_command_neutral_safe_text()
    {
        StatusHandler handler = new("""
            {
              "correlationId": "correlation-config",
              "status": "Rejected",
              "statusCode": 5,
              "timestamp": "2026-06-06T02:00:00Z",
              "aggregateId": "tenant.alpha",
              "eventCount": 0,
              "rejectionEventType": "Hexalith.Tenants.Contracts.Events.Rejections.ConfigurationLimitExceededRejection",
              "failureReason": "raw payload token stack trace correlation-config Value=super-secret",
              "timeoutDuration": null
            }
            """);
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-config")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(handler) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("message-config", "correlation-config"),
            CancellationToken.None);

        result.Status.ShouldBe(CommandStatus.Rejected);
        result.RejectionCode.ShouldBe("ConfigurationLimitExceeded");
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain("configuration limits", Case.Insensitive);
        safeMessage.ShouldNotContain("set configuration", Case.Insensitive);
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-config", Case.Insensitive);
        safeMessage.ShouldNotContain("super-secret", Case.Insensitive);
    }

    [Fact]
    public async Task Remove_tenant_configuration_submits_literal_command_with_payload_and_captures_correlation_id()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-remove-config"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.RemoveTenantConfigurationAsync(
            new RemoveTenantConfiguration("Tenant.Mixed-01", "billing.mode"),
            CancellationToken.None);

        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        submitted.Tenant.ShouldBe("system");
        submitted.Domain.ShouldBe("tenants");
        submitted.AggregateId.ShouldBe("Tenant.Mixed-01");
        submitted.CommandType.ShouldBe(nameof(RemoveTenantConfiguration));
        submitted.Payload.GetProperty("TenantId").GetString().ShouldBe("Tenant.Mixed-01");
        submitted.Payload.GetProperty("Key").GetString().ShouldBe("billing.mode");
        result.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.CorrelationId.ShouldBe("correlation-remove-config");
    }

    [Fact]
    public async Task Tracked_remove_configuration_uses_the_callers_exact_message_id()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-remove-config"));
        var ulidFactory = new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        TenantCommandGateway gateway = new(
            client,
            ulidFactory,
            new HttpClient(new StatusHandler("{}")) { BaseAddress = new Uri("https://eventstore.example/") });
        const string messageId = "01ARZ3NDEKTSV4RRFFQ69G5FAA";

        TenantCommandSubmissionResult result = await gateway.RemoveTenantConfigurationTrackedAsync(
            new RemoveTenantConfiguration("Tenant.Mixed-01", "Billing.Mode"),
            messageId,
            CancellationToken.None);

        gateway.SupportsTrackedRemoveConfigurationDispatch.ShouldBeTrue();
        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe(messageId);
        submitted.AggregateId.ShouldBe("Tenant.Mixed-01");
        submitted.Payload.GetProperty("TenantId").GetString().ShouldBe("Tenant.Mixed-01");
        submitted.Payload.GetProperty("Key").GetString().ShouldBe("Billing.Mode");
        result.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        result.MessageId.ShouldBe(messageId);
        result.CorrelationId.ShouldBe("correlation-remove-config");
        ulidFactory.CallCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Tracked_remove_configuration_retains_message_identity_when_http_delivery_is_ambiguous(
        HttpStatusCode statusCode)
    {
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)statusCode,
            "temporarily-unavailable",
            detail: "raw payload Key=billing.secret Value=super-secret"));
        TenantCommandGateway gateway = new(
            client,
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(new StatusHandler("{}")) { BaseAddress = new Uri("https://eventstore.example/") });
        const string messageId = "01ARZ3NDEKTSV4RRFFQ69G5FAA";

        TenantCommandSubmissionResult result = await gateway.RemoveTenantConfigurationTrackedAsync(
            new RemoveTenantConfiguration("tenant.alpha", "billing.secret"),
            messageId,
            CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.RequestSent);
        result.IsAmbiguousFailure.ShouldBeTrue();
        result.MessageId.ShouldBe(messageId);
        result.SafeMessageKey.ShouldBe("Tenants.Configuration.Remove.SubmissionEvidence.Ambiguous");
        result.SafeMessage.ShouldBeNull();
        client.SubmittedCommands.ShouldHaveSingleItem().MessageId.ShouldBe(messageId);
    }

    [Theory]
    [MemberData(nameof(AmbiguousTransportExceptions))]
    public async Task Tracked_remove_configuration_retains_message_identity_for_ambiguous_transport_exceptions(
        Exception exception)
    {
        CapturingGatewayClient client = new(exception);
        TenantCommandGateway gateway = new(
            client,
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(new StatusHandler("{}")) { BaseAddress = new Uri("https://eventstore.example/") });
        const string messageId = "01ARZ3NDEKTSV4RRFFQ69G5FAA";

        TenantCommandSubmissionResult result = await gateway.RemoveTenantConfigurationTrackedAsync(
            new RemoveTenantConfiguration("tenant.alpha", "billing.mode"),
            messageId,
            CancellationToken.None);

        result.IsAmbiguousFailure.ShouldBeTrue();
        result.MessageId.ShouldBe(messageId);
        result.SafeMessageKey.ShouldBe("Tenants.Configuration.Remove.SubmissionEvidence.Ambiguous");
        result.SafeMessage.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Remove_tenant_configuration_validation_failure_does_not_submit_to_eventstore(string? key)
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-remove-config"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.RemoveTenantConfigurationAsync(
            new RemoveTenantConfiguration("tenant.alpha", key!),
            CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
        result.SafeMessage.ShouldNotBeNull().ShouldContain("configuration key");
        client.SubmittedCommands.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("ConfigurationKeyNotFoundRejection", "ConfigurationKeyNotFound", "not found")]
    [InlineData("InsufficientPermissionsRejection", "InsufficientPermissions", "not authorized")]
    [InlineData("TenantDisabledRejection", "TenantDisabled", "disabled")]
    [InlineData("TenantNotFoundRejection", "TenantNotFound", "not found")]
    public async Task Remove_tenant_configuration_maps_safe_rejection_text(
        string reason,
        string expectedCode,
        string expectedText)
    {
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)HttpStatusCode.Conflict,
            reason,
            detail: "raw payload bearer-token stack trace correlation-config Key=billing.secret Value=super-secret"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.RemoveTenantConfigurationAsync(
            new RemoveTenantConfiguration("tenant.alpha", "billing.secret"),
            CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        result.RejectionCode.ShouldBe(expectedCode);
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain(expectedText, Case.Insensitive);
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-config", Case.Insensitive);
        safeMessage.ShouldNotContain("super-secret", Case.Insensitive);
    }

    [Fact]
    public async Task Status_lookup_maps_configuration_key_not_found_to_rejected_safe_text()
    {
        StatusHandler handler = new("""
            {
              "correlationId": "correlation-remove-config",
              "status": "Rejected",
              "statusCode": 5,
              "timestamp": "2026-06-06T02:00:00Z",
              "aggregateId": "tenant.alpha",
              "eventCount": 0,
              "rejectionEventType": "Hexalith.Tenants.Contracts.Events.Rejections.ConfigurationKeyNotFoundRejection",
              "failureReason": "raw payload token stack trace correlation-remove-config Value=super-secret",
              "timeoutDuration": null
            }
            """);
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-remove-config")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(handler) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("message-config", "correlation-remove-config"),
            CancellationToken.None);

        result.Status.ShouldBe(CommandStatus.Rejected);
        result.RejectionCode.ShouldBe("ConfigurationKeyNotFound");
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain("not found", Case.Insensitive);
        safeMessage.ShouldNotContain("success", Case.Insensitive);
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-remove-config", Case.Insensitive);
        safeMessage.ShouldNotContain("super-secret", Case.Insensitive);
    }

    [Fact]
    public async Task Remove_user_from_tenant_submits_literal_command_and_captures_correlation_id()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-999"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.RemoveUserFromTenantAsync(
            new RemoveUserFromTenant("Tenant.Mixed-01", "User/CaseSensitive.01"),
            cancellationToken: CancellationToken.None);

        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        submitted.Tenant.ShouldBe("system");
        submitted.Domain.ShouldBe("tenants");
        submitted.AggregateId.ShouldBe("Tenant.Mixed-01");
        submitted.CommandType.ShouldBe(nameof(RemoveUserFromTenant));
        submitted.Payload.GetProperty("TenantId").GetString().ShouldBe("Tenant.Mixed-01");
        submitted.Payload.GetProperty("UserId").GetString().ShouldBe("User/CaseSensitive.01");
        result.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.CorrelationId.ShouldBe("correlation-999");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Remove_user_from_tenant_validation_failure_does_not_submit_to_eventstore(string? userId)
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-999"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.RemoveUserFromTenantAsync(
            new RemoveUserFromTenant("tenant.alpha", userId!),
            cancellationToken: CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
        result.SafeMessage.ShouldNotBeNull().ShouldContain("Tenant id and user id are required");
        client.SubmittedCommands.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("UserNotInTenantRejection", "UserNotInTenant", "already applied")]
    [InlineData("InsufficientPermissionsRejection", "InsufficientPermissions", "not authorized")]
    [InlineData("TenantDisabledRejection", "TenantDisabled", "disabled")]
    [InlineData("TenantNotFoundRejection", "TenantNotFound", "not found")]
    public async Task Remove_user_from_tenant_maps_safe_rejection_text(
        string reason,
        string expectedCode,
        string expectedText)
    {
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)HttpStatusCode.Conflict,
            reason,
            detail: "raw payload bearer-token stack trace correlation-999"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.RemoveUserFromTenantAsync(
            new RemoveUserFromTenant("tenant.alpha", "literal-user"),
            cancellationToken: CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        result.RejectionCode.ShouldBe(expectedCode);
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain(expectedText, Case.Insensitive);
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-999", Case.Insensitive);
    }

    [Fact]
    public async Task Change_user_role_submits_literal_command_with_new_role_and_captures_correlation_id()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-789"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.ChangeUserRoleAsync(
            new ChangeUserRole("Tenant.Mixed-01", "User/CaseSensitive.01", TenantRole.TenantReader),
            cancellationToken: CancellationToken.None);

        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        submitted.Tenant.ShouldBe("system");
        submitted.Domain.ShouldBe("tenants");
        submitted.AggregateId.ShouldBe("Tenant.Mixed-01");
        submitted.CommandType.ShouldBe(nameof(ChangeUserRole));
        submitted.Payload.GetProperty("TenantId").GetString().ShouldBe("Tenant.Mixed-01");
        submitted.Payload.GetProperty("UserId").GetString().ShouldBe("User/CaseSensitive.01");
        submitted.Payload.GetProperty("NewRole").GetString().ShouldBe(nameof(TenantRole.TenantReader));
        result.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.CorrelationId.ShouldBe("correlation-789");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Change_user_role_validation_failure_does_not_submit_to_eventstore(string? userId)
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-789"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.ChangeUserRoleAsync(
            new ChangeUserRole("tenant.alpha", userId!, TenantRole.TenantReader),
            cancellationToken: CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
        result.SafeMessage.ShouldNotBeNull().ShouldContain("Tenant id, user id, and new role are required");
        client.SubmittedCommands.ShouldBeEmpty();
    }

    [Fact]
    public async Task Change_user_role_rejects_unknown_role_before_submission()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-789"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.ChangeUserRoleAsync(
            new ChangeUserRole("tenant.alpha", "literal-user", TenantRole.Unknown),
            cancellationToken: CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
        result.SafeMessage.ShouldNotBeNull().ShouldContain("new role");
        client.SubmittedCommands.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("RoleEscalationRejection", "RoleEscalation", "role cannot be assigned")]
    [InlineData("UserNotInTenantRejection", "UserNotInTenant", "not a visible member")]
    [InlineData("InsufficientPermissionsRejection", "InsufficientPermissions", "not authorized")]
    [InlineData("TenantDisabledRejection", "TenantDisabled", "disabled")]
    [InlineData("TenantNotFoundRejection", "TenantNotFound", "not found")]
    public async Task Change_user_role_maps_safe_rejection_text(
        string reason,
        string expectedCode,
        string expectedText)
    {
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)HttpStatusCode.Conflict,
            reason,
            detail: "raw payload bearer-token stack trace correlation-789"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.ChangeUserRoleAsync(
            new ChangeUserRole("tenant.alpha", "literal-user", TenantRole.TenantContributor),
            cancellationToken: CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        result.RejectionCode.ShouldBe(expectedCode);
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain(expectedText, Case.Insensitive);
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-789", Case.Insensitive);
    }

    [Theory]
    [InlineData("RoleEscalationRejection", "RoleEscalation", "role cannot be assigned")]
    [InlineData("UserNotInTenantRejection", "UserNotInTenant", "not a visible member")]
    [InlineData("InsufficientPermissionsRejection", "InsufficientPermissions", "not authorized")]
    [InlineData("TenantDisabledRejection", "TenantDisabled", "disabled")]
    [InlineData("TenantNotFoundRejection", "TenantNotFound", "not found")]
    public async Task Status_lookup_maps_change_role_rejections_to_safe_text(
        string rejectionType,
        string expectedCode,
        string expectedText)
    {
        StatusHandler handler = new($$"""
            {
              "correlationId": "correlation-789",
              "status": "Rejected",
              "statusCode": 5,
              "timestamp": "2026-06-06T02:00:00Z",
              "aggregateId": "tenant.alpha",
              "eventCount": 0,
              "rejectionEventType": "Hexalith.Tenants.Contracts.Events.Rejections.{{rejectionType}}",
              "failureReason": "raw payload token stack trace correlation-789",
              "timeoutDuration": null
            }
            """);
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-789")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(handler) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("message-789", "correlation-789"),
            CancellationToken.None);

        result.Status.ShouldBe(CommandStatus.Rejected);
        result.RejectionCode.ShouldBe(expectedCode);
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain(expectedText, Case.Insensitive);
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-789", Case.Insensitive);
    }

    [Fact]
    public async Task Status_lookup_exposes_missing_member_rejection_code_without_success_or_raw_details()
    {
        StatusHandler handler = new("""
            {
              "correlationId": "correlation-remove",
              "status": "Rejected",
              "statusCode": 5,
              "timestamp": "2026-06-06T02:00:00Z",
              "aggregateId": "tenant.alpha",
              "eventCount": 0,
              "rejectionEventType": "Hexalith.Tenants.Contracts.Events.Rejections.UserNotInTenantRejection",
              "failureReason": "raw payload token stack trace correlation-remove",
              "timeoutDuration": null
            }
            """);
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-remove")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(handler) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("message-remove", "correlation-remove"),
            CancellationToken.None);

        result.Status.ShouldBe(CommandStatus.Rejected);
        result.RejectionCode.ShouldBe("UserNotInTenant");
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain("not a visible member", Case.Insensitive);
        safeMessage.ShouldNotContain("already applied", Case.Insensitive);
        safeMessage.ShouldNotContain("success", Case.Insensitive);
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-remove", Case.Insensitive);
    }

    [Theory]
    [InlineData("InsufficientPermissionsRejection")]
    [InlineData("TenantDisabledRejection")]
    [InlineData("TenantNotFoundRejection")]
    [InlineData("RoleEscalationRejection")]
    public async Task Status_lookup_keeps_shared_rejection_copy_command_neutral(string rejectionType)
    {
        // GetStatusAsync is shared across create-tenant, add-member, and change-role and only sees
        // a correlation id, so a rejection type that several commands can produce must not surface
        // one command's wording inside another command's lifecycle panel.
        StatusHandler handler = new($$"""
            {
              "correlationId": "correlation-shared",
              "status": "Rejected",
              "statusCode": 5,
              "timestamp": "2026-06-06T02:00:00Z",
              "aggregateId": "tenant.alpha",
              "eventCount": 0,
              "rejectionEventType": "Hexalith.Tenants.Contracts.Events.Rejections.{{rejectionType}}",
              "failureReason": "raw payload token stack trace correlation-shared",
              "timeoutDuration": null
            }
            """);
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-shared")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(handler) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("message-shared", "correlation-shared"),
            CancellationToken.None);

        result.Status.ShouldBe(CommandStatus.Rejected);
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldNotContain("add members", Case.Insensitive);
        safeMessage.ShouldNotContain("members cannot be added", Case.Insensitive);
        safeMessage.ShouldNotContain("change member roles", Case.Insensitive);
        safeMessage.ShouldNotContain("member roles cannot be changed", Case.Insensitive);
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-shared", Case.Insensitive);
    }

    [Fact]
    public async Task Add_user_to_tenant_submits_literal_command_with_explicit_role_and_captures_correlation_id()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-456"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.AddUserToTenantAsync(
            new AddUserToTenant("Tenant.Mixed-01", "User/CaseSensitive.01", TenantRole.TenantContributor),
            cancellationToken: CancellationToken.None);

        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        submitted.Tenant.ShouldBe("system");
        submitted.Domain.ShouldBe("tenants");
        submitted.AggregateId.ShouldBe("Tenant.Mixed-01");
        submitted.CommandType.ShouldBe(nameof(AddUserToTenant));
        submitted.Payload.GetProperty("TenantId").GetString().ShouldBe("Tenant.Mixed-01");
        submitted.Payload.GetProperty("UserId").GetString().ShouldBe("User/CaseSensitive.01");
        submitted.Payload.GetProperty("Role").GetString().ShouldBe(nameof(TenantRole.TenantContributor));
        result.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.CorrelationId.ShouldBe("correlation-456");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Add_user_to_tenant_validation_failure_does_not_submit_to_eventstore(string? userId)
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-456"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.AddUserToTenantAsync(
            new AddUserToTenant("tenant.alpha", userId!, TenantRole.TenantReader),
            cancellationToken: CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
        result.SafeMessage.ShouldNotBeNull().ShouldContain("Tenant id, user id, and role are required");
        client.SubmittedCommands.ShouldBeEmpty();
    }

    [Fact]
    public async Task Add_user_to_tenant_rejects_unknown_role_before_submission()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-456"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.AddUserToTenantAsync(
            new AddUserToTenant("tenant.alpha", "literal-user", TenantRole.Unknown),
            cancellationToken: CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
        result.SafeMessage.ShouldNotBeNull().ShouldContain("role");
        client.SubmittedCommands.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("UserAlreadyInTenantRejection", "UserAlreadyInTenant", "already a member")]
    [InlineData("RoleEscalationRejection", "RoleEscalation", "role cannot be assigned")]
    [InlineData("InsufficientPermissionsRejection", "InsufficientPermissions", "not authorized")]
    [InlineData("TenantDisabledRejection", "TenantDisabled", "disabled")]
    [InlineData("TenantNotFoundRejection", "TenantNotFound", "not found")]
    public async Task Add_user_to_tenant_maps_safe_rejection_text(
        string reason,
        string expectedCode,
        string expectedText)
    {
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)HttpStatusCode.Conflict,
            reason,
            detail: "raw payload bearer-token stack trace correlation-456"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.AddUserToTenantAsync(
            new AddUserToTenant("tenant.alpha", "literal-user", TenantRole.TenantReader),
            cancellationToken: CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        result.RejectionCode.ShouldBe(expectedCode);
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain(expectedText, Case.Insensitive);
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-456", Case.Insensitive);
    }

    [Fact]
    public async Task Add_user_to_tenant_reuses_provided_message_id_instead_of_minting()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-reuse"));
        StubUlidFactory ulids = new("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        TenantCommandGateway gateway = new(client, ulids, new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.AddUserToTenantAsync(
            new AddUserToTenant("tenant.alpha", "literal-user", TenantRole.TenantReader),
            messageId: "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            cancellationToken: CancellationToken.None);

        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.CorrelationId.ShouldBe("correlation-reuse");
        ulids.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Add_user_to_tenant_mints_message_id_only_when_absent()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-new"));
        StubUlidFactory ulids = new("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        TenantCommandGateway gateway = new(client, ulids, new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.AddUserToTenantAsync(
            new AddUserToTenant("tenant.alpha", "literal-user", TenantRole.TenantReader),
            cancellationToken: CancellationToken.None);

        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        ulids.CallCount.ShouldBe(1);
    }

    [Theory]
    [InlineData("not-a-ulid")]
    [InlineData("8ZZZZZZZZZZZZZZZZZZZZZZZZZ")]
    [InlineData("01ARZ3NDEKTSV4RRFFQ69G5FAI")]
    public async Task Add_user_to_tenant_rejects_noncanonical_reusable_message_id(string messageId)
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-unused"));
        StubUlidFactory ulids = new("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        TenantCommandGateway gateway = new(client, ulids, new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.AddUserToTenantAsync(
            new AddUserToTenant("tenant.alpha", "literal-user", TenantRole.TenantReader),
            messageId,
            CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
        result.SafeMessageKey.ShouldBe("Tenants.Commands.Unavailable.InvalidTrackingReference");
        result.SafeMessage.ShouldBeNull();
        client.SubmittedCommands.ShouldBeEmpty();
        ulids.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Add_user_to_tenant_retains_minted_message_id_when_submission_is_indeterminate()
    {
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)HttpStatusCode.ServiceUnavailable,
            "gateway unavailable"));
        StubUlidFactory ulids = new("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        TenantCommandGateway gateway = new(client, ulids, new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.AddUserToTenantAsync(
            new AddUserToTenant("tenant.alpha", "literal-user", TenantRole.TenantReader),
            cancellationToken: CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        client.SubmittedCommands.ShouldHaveSingleItem().MessageId.ShouldBe(result.MessageId);
        ulids.CallCount.ShouldBe(1);
    }

    // Retention was proven for add-member only. The same indeterminate-submission contract carries the
    // idempotency key for every submit path: without it a 503 loses the key, the flow's retry mints a new
    // ULID, and a command that may already have applied is dispatched twice.
    [Fact]
    public async Task Change_user_role_retains_minted_message_id_when_submission_is_indeterminate()
    {
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)HttpStatusCode.ServiceUnavailable,
            "gateway unavailable"));
        StubUlidFactory ulids = new("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        TenantCommandGateway gateway = new(client, ulids, new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.ChangeUserRoleAsync(
            new ChangeUserRole("tenant.alpha", "literal-user", TenantRole.TenantOwner),
            cancellationToken: CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        client.SubmittedCommands.ShouldHaveSingleItem().MessageId.ShouldBe(result.MessageId);
        ulids.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Remove_user_from_tenant_retains_minted_message_id_when_submission_is_indeterminate()
    {
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)HttpStatusCode.ServiceUnavailable,
            "gateway unavailable"));
        StubUlidFactory ulids = new("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        TenantCommandGateway gateway = new(client, ulids, new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.RemoveUserFromTenantAsync(
            new RemoveUserFromTenant("tenant.alpha", "literal-user"),
            cancellationToken: CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        client.SubmittedCommands.ShouldHaveSingleItem().MessageId.ShouldBe(result.MessageId);
        ulids.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Change_user_role_reuses_provided_message_id_instead_of_minting()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-reuse"));
        StubUlidFactory ulids = new("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        TenantCommandGateway gateway = new(client, ulids, new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.ChangeUserRoleAsync(
            new ChangeUserRole("tenant.alpha", "literal-user", TenantRole.TenantContributor),
            messageId: "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            cancellationToken: CancellationToken.None);

        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.CorrelationId.ShouldBe("correlation-reuse");
        ulids.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Change_user_role_mints_message_id_only_when_absent()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-new"));
        StubUlidFactory ulids = new("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        TenantCommandGateway gateway = new(client, ulids, new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.ChangeUserRoleAsync(
            new ChangeUserRole("tenant.alpha", "literal-user", TenantRole.TenantContributor),
            cancellationToken: CancellationToken.None);

        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        ulids.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Remove_user_from_tenant_reuses_provided_message_id_instead_of_minting()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-reuse"));
        StubUlidFactory ulids = new("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        TenantCommandGateway gateway = new(client, ulids, new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.RemoveUserFromTenantAsync(
            new RemoveUserFromTenant("tenant.alpha", "literal-user"),
            messageId: "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            cancellationToken: CancellationToken.None);

        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.CorrelationId.ShouldBe("correlation-reuse");
        ulids.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Remove_user_from_tenant_mints_message_id_only_when_absent()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-new"));
        StubUlidFactory ulids = new("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        TenantCommandGateway gateway = new(client, ulids, new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.RemoveUserFromTenantAsync(
            new RemoveUserFromTenant("tenant.alpha", "literal-user"),
            cancellationToken: CancellationToken.None);

        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        ulids.CallCount.ShouldBe(1);
    }

    [Theory]
    [InlineData("UserAlreadyInTenantRejection", "UserAlreadyInTenant", "already a member")]
    [InlineData("RoleEscalationRejection", "RoleEscalation", "role cannot be assigned")]
    [InlineData("InsufficientPermissionsRejection", "InsufficientPermissions", "not authorized")]
    [InlineData("TenantDisabledRejection", "TenantDisabled", "disabled")]
    [InlineData("TenantNotFoundRejection", "TenantNotFound", "not found")]
    public async Task Status_lookup_maps_add_member_rejections_to_safe_text(
        string rejectionType,
        string expectedCode,
        string expectedText)
    {
        StatusHandler handler = new($$"""
            {
              "correlationId": "correlation-456",
              "status": "Rejected",
              "statusCode": 5,
              "timestamp": "2026-06-06T02:00:00Z",
              "aggregateId": "tenant.alpha",
              "eventCount": 1,
              "rejectionEventType": "Hexalith.Tenants.Contracts.Events.Rejections.{{rejectionType}}",
              "failureReason": "raw payload token stack trace correlation-456",
              "timeoutDuration": null
            }
            """);
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-456")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(handler) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("message-456", "correlation-456"),
            CancellationToken.None);

        result.Status.ShouldBe(CommandStatus.Rejected);
        result.RejectionCode.ShouldBe(expectedCode);
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain(expectedText, Case.Insensitive);
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-456", Case.Insensitive);
    }

    [Fact]
    public async Task Create_tenant_submits_literal_command_with_ulid_message_id_and_captures_correlation_id()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-123"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.CreateTenantAsync(
            new CreateTenant("Tenant.Mixed-01", "Mixed Tenant", "literal id"),
            cancellationToken: CancellationToken.None);

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
    public async Task Create_tenant_reuses_provided_message_id_instead_of_minting()
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-reuse"));
        StubUlidFactory ulids = new("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        TenantCommandGateway gateway = new(client, ulids, new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.CreateTenantAsync(
            new CreateTenant("tenant.alpha", "Alpha", null),
            messageId: "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            cancellationToken: CancellationToken.None);

        SubmitCommandRequest submitted = client.SubmittedCommands.ShouldHaveSingleItem();
        submitted.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        ulids.CallCount.ShouldBe(0);
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
            new CreateTenant("tenant.alpha", "Alpha", null),
            cancellationToken: CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        result.RejectionCode.ShouldBe("TenantAlreadyExists");
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain("already exists");
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation-123", Case.Insensitive);
    }

    [Fact]
    public async Task Create_tenant_maps_problem_details_rejection_type_extension_to_duplicate_safe_text()
    {
        CapturingGatewayClient client = new(new EventStoreGatewayException(
            (int)HttpStatusCode.Conflict,
            "Domain rejection",
            detail: "Domain rejection returned by the aggregate.",
            extensions: new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                [GatewayProblemDetailsExtensions.RejectionType] = JsonSerializer.SerializeToElement(
                    "Hexalith.Tenants.Contracts.Events.Rejections.TenantAlreadyExistsRejection"),
                [GatewayProblemDetailsExtensions.CorrectiveAction] = JsonSerializer.SerializeToElement(
                    "Refresh the list or open the existing tenant."),
            }));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.CreateTenantAsync(
            new CreateTenant("tenant.alpha", "Alpha", null),
            cancellationToken: CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        result.RejectionCode.ShouldBe("TenantAlreadyExists");
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldContain("already exists", Case.Insensitive);
        safeMessage.ShouldNotContain("raw payload", Case.Insensitive);
        safeMessage.ShouldNotContain("token", Case.Insensitive);
        safeMessage.ShouldNotContain("correlation", Case.Insensitive);
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
            new CreateTenant("", "Alpha", null),
            cancellationToken: CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
        result.SafeMessage.ShouldBe("Tenant id and name are required before the command can be submitted.");
        client.SubmittedCommands.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("   ", "Alpha")]
    [InlineData("tenant.alpha", "   ")]
    public async Task Create_tenant_whitespace_validation_failure_does_not_submit_to_eventstore(string tenantId, string name)
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-123"));
        TenantCommandGateway gateway = new(client, new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"), new HttpClient(new StatusHandler("{}"))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });

        TenantCommandSubmissionResult result = await gateway.CreateTenantAsync(
            new CreateTenant(tenantId, name, null),
            cancellationToken: CancellationToken.None);

        result.State.ShouldBe(TenantCommandLifecycleState.Failed);
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
            new CreateTenant("tenant.alpha", "Alpha", null),
            cancellationToken: CancellationToken.None);

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
        handler.RequestUri.ShouldNotBeNull().AbsoluteUri.ShouldNotContain("01ARZ3NDEKTSV4RRFFQ69G5FAV");
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
    public async Task Status_lookup_maps_not_found_to_pending_without_raw_details()
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
        result.IsPending.ShouldBeTrue();
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldBe("Command status is not available yet.");
        safeMessage.ShouldNotContain("correlation-123", Case.Insensitive);
    }

    [Fact]
    public async Task Lifecycle_status_lookup_verifies_message_and_aggregate_identity()
    {
        StatusHandler handler = new("""
            {
              "correlationId": "correlation-123",
              "status": "Completed",
              "statusCode": 4,
              "timestamp": "2026-06-06T02:00:00Z",
              "aggregateId": "tenant.alpha",
              "eventCount": 1,
              "rejectionEventType": null,
              "failureReason": null,
              "timeoutDuration": null,
              "messageId": "message-123"
            }
            """);
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-123")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(handler) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("message-123", "correlation-123", "tenant.alpha"),
            CancellationToken.None);

        result.Status.ShouldBe(CommandStatus.Completed);
        result.HasVerifiedCommandIdentity.ShouldBeTrue();
    }

    [Theory]
    [InlineData("message-other", "tenant.alpha")]
    [InlineData("message-123", "Tenant.Alpha")]
    // CommandStatusRecord.MessageId is contractually null for a legacy record. Identity must stay unproven
    // rather than fall back to the correlation id, which is matched earlier and is not command-exact.
    [InlineData("", "tenant.alpha")]
    public async Task Lifecycle_status_lookup_rejects_mismatched_command_identity(
        string messageId,
        string aggregateId)
    {
        StatusHandler handler = new($$"""
            {
              "correlationId": "correlation-123",
              "status": "Completed",
              "statusCode": 4,
              "timestamp": "2026-06-06T02:00:00Z",
              "aggregateId": "{{aggregateId}}",
              "eventCount": 1,
              "rejectionEventType": null,
              "failureReason": null,
              "timeoutDuration": null,
              "messageId": "{{messageId}}"
            }
            """);
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-123")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(handler) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("message-123", "correlation-123", "tenant.alpha"),
            CancellationToken.None);

        result.Status.ShouldBe(CommandStatus.Completed);
        result.HasVerifiedCommandIdentity.ShouldBeFalse();
        result.SafeMessage.ShouldBe("Command status response did not match the tracked lifecycle command.");
    }

    [Fact]
    public async Task Status_lookup_maps_malformed_payload_to_retryable_failure()
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
        result.IsRetryableFailure.ShouldBeTrue();
        result.SafeMessage.ShouldBe("Command status response was unavailable.");
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    public async Task Status_lookup_retries_only_transient_http_failures(
        HttpStatusCode statusCode,
        bool expectedRetryable)
    {
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-123")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(new StatusHandler("{}", statusCode))
            {
                BaseAddress = new Uri("https://eventstore.example/"),
            });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("message-123", "correlation-123"),
            CancellationToken.None);

        result.Status.ShouldBeNull();
        result.IsRetryableFailure.ShouldBe(expectedRetryable);
    }

    [Fact]
    public async Task Status_lookup_maps_http_transport_exception_to_retryable_failure()
    {
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-123")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(new ThrowingStatusHandler(new HttpRequestException("transport unavailable")))
            {
                BaseAddress = new Uri("https://eventstore.example/"),
            });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("message-123", "correlation-123"),
            CancellationToken.None);

        result.Status.ShouldBeNull();
        result.IsRetryableFailure.ShouldBeTrue();
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

    [Fact]
    public async Task Status_lookup_generic_rejection_stays_command_neutral_for_shared_status_path()
    {
        // GetStatusAsync is shared by create-tenant and add-member; an unrecognized rejection type
        // must not surface create-tenant-specific copy in another command's lifecycle panel.
        StatusHandler handler = new("""
            {
              "correlationId": "correlation-456",
              "status": "Rejected",
              "statusCode": 5,
              "timestamp": "2026-06-06T02:00:00Z",
              "aggregateId": "tenant.alpha",
              "eventCount": 0,
              "rejectionEventType": "Hexalith.Tenants.Contracts.Events.Rejections.SomeUnmappedRejection",
              "failureReason": null,
              "timeoutDuration": null
            }
            """);
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-456")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(handler) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("message-456", "correlation-456"),
            CancellationToken.None);

        result.Status.ShouldBe(CommandStatus.Rejected);
        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldBe("The command was rejected.");
        safeMessage.ShouldNotContain("create tenant", Case.Insensitive);
    }

    [Theory]
    [InlineData("leaked bearer value")]
    [InlineData("decoded jwt eyJhbGciOiJ")]
    [InlineData("internal cursor 00ff")]
    [InlineData("eventstore etag abc123")]
    public async Task Status_lookup_redacts_unsafe_support_markers_in_failure_reason(string failureReason)
    {
        StatusHandler handler = new($$"""
            {
              "correlationId": "correlation-456",
              "status": "Processing",
              "statusCode": 1,
              "timestamp": "2026-06-06T02:00:00Z",
              "aggregateId": "tenant.alpha",
              "eventCount": 0,
              "rejectionEventType": null,
              "failureReason": "{{failureReason}}",
              "timeoutDuration": null
            }
            """);
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-456")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(handler) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("message-456", "correlation-456"),
            CancellationToken.None);

        string safeMessage = result.SafeMessage.ShouldNotBeNull();
        safeMessage.ShouldBe("The command status included an unavailable support detail.");
        safeMessage.ShouldNotContain("bearer", Case.Insensitive);
        safeMessage.ShouldNotContain("jwt", Case.Insensitive);
        safeMessage.ShouldNotContain("cursor", Case.Insensitive);
        safeMessage.ShouldNotContain("etag", Case.Insensitive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task Membership_commands_reject_blank_identifiers_without_dispatch(string blank)
    {
        CapturingGatewayClient client = new(new SubmitCommandResponse("correlation-unused"));
        TenantCommandGateway gateway = new(
            client,
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(new StatusHandler("{}")) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandSubmissionResult add = await gateway.AddUserToTenantAsync(
            new AddUserToTenant("tenant.alpha", blank, TenantRole.TenantReader),
            cancellationToken: CancellationToken.None);
        TenantCommandSubmissionResult change = await gateway.ChangeUserRoleAsync(
            new ChangeUserRole(blank, "literal-user", TenantRole.TenantReader),
            cancellationToken: CancellationToken.None);
        TenantCommandSubmissionResult remove = await gateway.RemoveUserFromTenantAsync(
            new RemoveUserFromTenant("tenant.alpha", blank),
            cancellationToken: CancellationToken.None);

        add.State.ShouldBe(TenantCommandLifecycleState.Failed);
        change.State.ShouldBe(TenantCommandLifecycleState.Failed);
        remove.State.ShouldBe(TenantCommandLifecycleState.Failed);
        client.SubmittedCommands.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("different-correlation")]
    public async Task Status_lookup_fails_closed_when_response_correlation_is_not_the_requested_handle(string responseCorrelation)
    {
        StatusHandler handler = new($$"""
            {
              "correlationId": "{{responseCorrelation}}",
              "status": "Completed",
              "statusCode": 4,
              "timestamp": "2026-06-06T02:00:00Z",
              "aggregateId": "tenant.alpha",
              "eventCount": 1
            }
            """);
        TenantCommandGateway gateway = new(
            new CapturingGatewayClient(new SubmitCommandResponse("correlation-requested")),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(handler) { BaseAddress = new Uri("https://eventstore.example/") });

        TenantCommandStatusResult result = await gateway.GetStatusAsync(
            new TenantCommandTrackingHandle("message-1", "correlation-requested"),
            CancellationToken.None);

        result.Status.ShouldBeNull();
        result.IsRetryableFailure.ShouldBeFalse();
        result.SafeMessage.ShouldBe("Command status response did not match the tracked command.");
    }

    [Fact]
    public async Task Stable_lifecycle_overload_fails_closed_for_a_legacy_style_gateway()
    {
        var legacy = new LegacyLifecycleGateway();
        ITenantCommandGateway gateway = legacy;
        var disable = new TenantLifecycleCommandRequest("Tenant.Mixed-01", TenantLifecycleOperation.DisableTenant);
        var enable = new TenantLifecycleCommandRequest("Tenant.Mixed-01", TenantLifecycleOperation.EnableTenant);
        using var cancellation = new CancellationTokenSource();

        TenantCommandSubmissionResult legacyResult = await gateway.DisableTenantAsync(disable);
        TenantCommandSubmissionResult stableDisableResult = await gateway.DisableTenantTrackedAsync(
            disable,
            "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
            cancellation.Token);
        TenantCommandSubmissionResult stableResult = await gateway.EnableTenantTrackedAsync(
            enable,
            "01ARZ3NDEKTSV4RRFFQ69G5FB0",
            cancellation.Token);

        legacy.DisableCalls.ShouldBe(1);
        legacy.EnableCalls.ShouldBe(0);
        legacy.LastEnableRequest.ShouldBeNull();
        legacyResult.MessageId.ShouldBe("legacy-disable-message");
        stableResult.State.ShouldBe(TenantCommandLifecycleState.Failed);
        stableResult.SafeMessageKey.ShouldBe("Tenants.Lifecycle.Unavailable.CommandSurface");
        stableDisableResult.State.ShouldBe(TenantCommandLifecycleState.Failed);
        stableDisableResult.SafeMessageKey.ShouldBe("Tenants.Lifecycle.Unavailable.CommandSurface");
    }

    [Fact]
    public async Task Unavailable_gateway_tracked_lifecycle_methods_use_localized_command_surface_key()
    {
        var gateway = new UnavailableTenantCommandGateway();
        var request = new TenantLifecycleCommandRequest("Tenant.Mixed-01", TenantLifecycleOperation.DisableTenant);

        TenantCommandSubmissionResult enable = await gateway.EnableTenantTrackedAsync(
            request with { Operation = TenantLifecycleOperation.EnableTenant },
            "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
            CancellationToken.None);
        TenantCommandSubmissionResult disable = await gateway.DisableTenantTrackedAsync(
            request,
            "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
            CancellationToken.None);

        enable.State.ShouldBe(TenantCommandLifecycleState.Failed);
        enable.SafeMessageKey.ShouldBe("Tenants.Lifecycle.Unavailable.CommandSurface");
        enable.SafeMessage.ShouldBeNull();
        disable.State.ShouldBe(TenantCommandLifecycleState.Failed);
        disable.SafeMessageKey.ShouldBe("Tenants.Lifecycle.Unavailable.CommandSurface");
        disable.SafeMessage.ShouldBeNull();
    }

    private sealed class LegacyLifecycleGateway : ITenantCommandGateway
    {
        public int EnableCalls { get; private set; }

        public int DisableCalls { get; private set; }

        public TenantLifecycleCommandRequest? LastEnableRequest { get; private set; }

        public CancellationToken LastEnableCancellation { get; private set; }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(
            CreateTenant request,
            string? messageId = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(
            AddUserToTenant request,
            string? messageId = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(
            ChangeUserRole request,
            string? messageId = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(
            RemoveUserFromTenant request,
            string? messageId = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(
            UpdateTenant request,
            string? messageId = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(
            SetTenantConfiguration request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> EnableTenantAsync(
            TenantLifecycleCommandRequest request,
            CancellationToken cancellationToken = default)
        {
            EnableCalls++;
            LastEnableRequest = request;
            LastEnableCancellation = cancellationToken;
            return Task.FromResult(TenantCommandSubmissionResult.Accepted(
                "legacy-enable-message",
                "legacy-enable-correlation"));
        }

        public Task<TenantCommandSubmissionResult> DisableTenantAsync(
            TenantLifecycleCommandRequest request,
            CancellationToken cancellationToken = default)
        {
            DisableCalls++;
            return Task.FromResult(TenantCommandSubmissionResult.Accepted(
                "legacy-disable-message",
                "legacy-disable-correlation"));
        }

        public Task<TenantCommandStatusResult> GetStatusAsync(
            TenantCommandTrackingHandle handle,
            CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandStatusResult.Unknown("Not used."));
    }

    private static TenantCommandGateway CreateLifecycleGateway(object response)
        => new(
            new CapturingGatewayClient(response),
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(new StatusHandler("{}"))
            {
                BaseAddress = new Uri("https://eventstore.example/"),
            });

    private static TenantCommandGateway CreateGateway(CapturingGatewayClient client)
        => new(
            client,
            new StubUlidFactory("01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new HttpClient(new StatusHandler("{}"))
            {
                BaseAddress = new Uri("https://eventstore.example/"),
            });

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
        public int CallCount { get; private set; }

        public string NewUlid()
        {
            CallCount++;
            return id;
        }
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

    private sealed class ThrowingStatusHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromException<HttpResponseMessage>(exception);
    }
}
