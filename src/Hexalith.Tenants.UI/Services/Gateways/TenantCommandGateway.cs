using System.Net;
using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Problems;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class TenantCommandGateway(
    IEventStoreGatewayClient gatewayClient,
    IUlidFactory ulidFactory,
    HttpClient statusClient) : ITenantCommandGateway {
    private const string SystemTenant = "system";
    private const string TenantsDomain = "tenants";
    private const string GlobalAdministratorsDomain = "global-administrators";
    private const string GlobalAdministratorsAggregateId = "global-administrators";

    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    public bool SupportsTrackedLifecycleDispatch => true;

    // Markers for support-unsafe content (AC9 / Story 1.8 discipline) that must never be echoed
    // from a backend failure reason into the visible command lifecycle copy.
    private static readonly string[] UnsafeSupportMarkers =
    [
        "payload",
        "token",
        "bearer",
        "jwt",
        "stack",
        "correlation",
        "cursor",
        "etag",
    ];

    public async Task<TenantCommandSubmissionResult> CreateTenantAsync(
        CreateTenant request,
        string? messageId = null,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.TenantId) || string.IsNullOrWhiteSpace(request.Name)) {
            return TenantCommandSubmissionResult.Failed("Tenant id and name are required before the command can be submitted.");
        }

        if (!TryResolveMessageId(messageId, out string resolvedMessageId)) {
            return TenantCommandSubmissionResult.FailedWithKey("Tenants.Commands.Unavailable.InvalidTrackingReference");
        }
        var submit = new SubmitCommandRequest(
            resolvedMessageId,
            SystemTenant,
            TenantsDomain,
            request.TenantId,
            nameof(CreateTenant),
            JsonSerializer.SerializeToElement(request));

        try {
            SubmitCommandResponse response = await gatewayClient
                .SubmitCommandAsync(submit, cancellationToken)
                .ConfigureAwait(false);

            return TenantCommandSubmissionResult.Accepted(resolvedMessageId, response.CorrelationId);
        }
        catch (EventStoreGatewayException ex) {
            return MapGatewayException(ex) with { MessageId = resolvedMessageId };
        }
    }

    public async Task<TenantCommandSubmissionResult> AddUserToTenantAsync(
        AddUserToTenant request,
        string? messageId = null,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.TenantId)
            || string.IsNullOrWhiteSpace(request.UserId)
            || !IsAssignableTenantRole(request.Role)) {
            return TenantCommandSubmissionResult.Failed("Tenant id, user id, and role are required before the command can be submitted.");
        }

        if (!TryResolveMessageId(messageId, out string resolvedMessageId)) {
            return TenantCommandSubmissionResult.FailedWithKey("Tenants.Commands.Unavailable.InvalidTrackingReference");
        }
        var submit = new SubmitCommandRequest(
            resolvedMessageId,
            SystemTenant,
            TenantsDomain,
            request.TenantId,
            nameof(AddUserToTenant),
            JsonSerializer.SerializeToElement(request));

        try {
            SubmitCommandResponse response = await gatewayClient
                .SubmitCommandAsync(submit, cancellationToken)
                .ConfigureAwait(false);

            return TenantCommandSubmissionResult.Accepted(resolvedMessageId, response.CorrelationId);
        }
        catch (EventStoreGatewayException ex) {
            return MapAddUserToTenantGatewayException(ex) with { MessageId = resolvedMessageId };
        }
    }

    public async Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(
        ChangeUserRole request,
        string? messageId = null,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.TenantId)
            || string.IsNullOrWhiteSpace(request.UserId)
            || !IsAssignableTenantRole(request.NewRole)) {
            return TenantCommandSubmissionResult.Failed("Tenant id, user id, and new role are required before the command can be submitted.");
        }

        if (!TryResolveMessageId(messageId, out string resolvedMessageId)) {
            return TenantCommandSubmissionResult.FailedWithKey("Tenants.Commands.Unavailable.InvalidTrackingReference");
        }
        var submit = new SubmitCommandRequest(
            resolvedMessageId,
            SystemTenant,
            TenantsDomain,
            request.TenantId,
            nameof(ChangeUserRole),
            JsonSerializer.SerializeToElement(request));

        try {
            SubmitCommandResponse response = await gatewayClient
                .SubmitCommandAsync(submit, cancellationToken)
                .ConfigureAwait(false);

            return TenantCommandSubmissionResult.Accepted(resolvedMessageId, response.CorrelationId);
        }
        catch (EventStoreGatewayException ex) {
            return MapChangeUserRoleGatewayException(ex) with { MessageId = resolvedMessageId };
        }
    }

    public async Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(
        RemoveUserFromTenant request,
        string? messageId = null,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.TenantId) || string.IsNullOrWhiteSpace(request.UserId)) {
            return TenantCommandSubmissionResult.Failed("Tenant id and user id are required before the command can be submitted.");
        }

        if (!TryResolveMessageId(messageId, out string resolvedMessageId)) {
            return TenantCommandSubmissionResult.FailedWithKey("Tenants.Commands.Unavailable.InvalidTrackingReference");
        }
        var submit = new SubmitCommandRequest(
            resolvedMessageId,
            SystemTenant,
            TenantsDomain,
            request.TenantId,
            nameof(RemoveUserFromTenant),
            JsonSerializer.SerializeToElement(request));

        try {
            SubmitCommandResponse response = await gatewayClient
                .SubmitCommandAsync(submit, cancellationToken)
                .ConfigureAwait(false);

            return TenantCommandSubmissionResult.Accepted(resolvedMessageId, response.CorrelationId);
        }
        catch (EventStoreGatewayException ex) {
            return MapRemoveUserFromTenantGatewayException(ex) with { MessageId = resolvedMessageId };
        }
    }

    public async Task<TenantCommandSubmissionResult> UpdateTenantAsync(
        UpdateTenant request,
        string? messageId = null,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.TenantId) || string.IsNullOrWhiteSpace(request.Name)) {
            return TenantCommandSubmissionResult.Failed("Tenant id and name are required before the command can be submitted.");
        }

        if (!TryResolveMessageId(messageId, out string resolvedMessageId)) {
            return TenantCommandSubmissionResult.FailedWithKey("Tenants.Commands.Unavailable.InvalidTrackingReference");
        }
        var submit = new SubmitCommandRequest(
            resolvedMessageId,
            SystemTenant,
            TenantsDomain,
            request.TenantId,
            nameof(UpdateTenant),
            JsonSerializer.SerializeToElement(request));

        try {
            SubmitCommandResponse response = await gatewayClient
                .SubmitCommandAsync(submit, cancellationToken)
                .ConfigureAwait(false);

            return TenantCommandSubmissionResult.Accepted(resolvedMessageId, response.CorrelationId);
        }
        catch (EventStoreGatewayException ex) {
            return MapUpdateTenantGatewayException(ex) with { MessageId = resolvedMessageId };
        }
    }

    public async Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(
        SetTenantConfiguration request,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.TenantId)
            || string.IsNullOrWhiteSpace(request.Key)
            || request.Value is null) {
            return TenantCommandSubmissionResult.Failed("Tenant id, configuration key, and value are required before the command can be submitted.");
        }

        string messageId = ulidFactory.NewUlid();
        var submit = new SubmitCommandRequest(
            messageId,
            SystemTenant,
            TenantsDomain,
            request.TenantId,
            nameof(SetTenantConfiguration),
            JsonSerializer.SerializeToElement(request));

        try {
            SubmitCommandResponse response = await gatewayClient
                .SubmitCommandAsync(submit, cancellationToken)
                .ConfigureAwait(false);

            return TenantCommandSubmissionResult.Accepted(messageId, response.CorrelationId);
        }
        catch (EventStoreGatewayException ex) {
            return MapSetTenantConfigurationGatewayException(ex);
        }
    }

    public async Task<TenantCommandSubmissionResult> RemoveTenantConfigurationAsync(
        RemoveTenantConfiguration request,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.TenantId) || string.IsNullOrWhiteSpace(request.Key)) {
            return TenantCommandSubmissionResult.Failed("Tenant id and configuration key are required before the command can be submitted.");
        }

        string messageId = ulidFactory.NewUlid();
        var submit = new SubmitCommandRequest(
            messageId,
            SystemTenant,
            TenantsDomain,
            request.TenantId,
            nameof(RemoveTenantConfiguration),
            JsonSerializer.SerializeToElement(request));

        try {
            SubmitCommandResponse response = await gatewayClient
                .SubmitCommandAsync(submit, cancellationToken)
                .ConfigureAwait(false);

            return TenantCommandSubmissionResult.Accepted(messageId, response.CorrelationId);
        }
        catch (EventStoreGatewayException ex) {
            return MapRemoveTenantConfigurationGatewayException(ex);
        }
    }

    public async Task<TenantCommandSubmissionResult> SetGlobalAdministratorAsync(
        SetGlobalAdministrator request,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.UserId)) {
            return TenantCommandSubmissionResult.Failed("User id is required before the global administrator command can be submitted.");
        }

        string messageId = ulidFactory.NewUlid();
        var submit = new SubmitCommandRequest(
            messageId,
            SystemTenant,
            GlobalAdministratorsDomain,
            GlobalAdministratorsAggregateId,
            nameof(SetGlobalAdministrator),
            JsonSerializer.SerializeToElement(request));

        try {
            SubmitCommandResponse response = await gatewayClient
                .SubmitCommandAsync(submit, cancellationToken)
                .ConfigureAwait(false);

            return TenantCommandSubmissionResult.Accepted(messageId, response.CorrelationId);
        }
        catch (EventStoreGatewayException ex) {
            return MapSetGlobalAdministratorGatewayException(ex);
        }
    }

    public async Task<TenantCommandSubmissionResult> RemoveGlobalAdministratorAsync(
        RemoveGlobalAdministrator request,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.UserId)) {
            return TenantCommandSubmissionResult.Failed("User id is required before the global administrator command can be submitted.");
        }

        string messageId = ulidFactory.NewUlid();
        var submit = new SubmitCommandRequest(
            messageId,
            SystemTenant,
            GlobalAdministratorsDomain,
            GlobalAdministratorsAggregateId,
            nameof(RemoveGlobalAdministrator),
            JsonSerializer.SerializeToElement(request));

        try {
            SubmitCommandResponse response = await gatewayClient
                .SubmitCommandAsync(submit, cancellationToken)
                .ConfigureAwait(false);

            return TenantCommandSubmissionResult.Accepted(messageId, response.CorrelationId);
        }
        catch (EventStoreGatewayException ex) {
            return MapRemoveGlobalAdministratorGatewayException(ex);
        }
    }

    public Task<TenantCommandSubmissionResult> EnableTenantAsync(
        TenantLifecycleCommandRequest request,
        CancellationToken cancellationToken = default)
        => EnableTenantTrackedAsync(request, ulidFactory.NewUlid(), cancellationToken);

    public async Task<TenantCommandSubmissionResult> EnableTenantTrackedAsync(
        TenantLifecycleCommandRequest request,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.TenantId) || request.Operation is not TenantLifecycleOperation.EnableTenant)
        {
            return TenantCommandSubmissionResult.Failed("Tenant id and lifecycle operation are required before the command can be submitted.");
        }

        if (string.IsNullOrWhiteSpace(messageId)
            || !TryResolveMessageId(messageId, out string resolvedMessageId))
        {
            return TenantCommandSubmissionResult.FailedWithKey("Tenants.Commands.Unavailable.InvalidTrackingReference");
        }

        var command = new EnableTenant(request.TenantId);
        var submit = new SubmitCommandRequest(
            resolvedMessageId,
            SystemTenant,
            TenantsDomain,
            request.TenantId,
            nameof(EnableTenant),
            JsonSerializer.SerializeToElement(command));

        try
        {
            SubmitCommandResponse response = await gatewayClient
                .SubmitCommandAsync(submit, cancellationToken)
                .ConfigureAwait(false);

            return TenantCommandSubmissionResult.Accepted(resolvedMessageId, response.CorrelationId);
        }
        catch (EventStoreGatewayException ex)
        {
            return MapLifecycleGatewayException(ex) with { MessageId = resolvedMessageId };
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return TenantCommandSubmissionResult.Ambiguous(
                resolvedMessageId,
                "Tenants.Lifecycle.SubmissionEvidence.Ambiguous");
        }
        catch (HttpRequestException)
        {
            return TenantCommandSubmissionResult.Ambiguous(
                resolvedMessageId,
                "Tenants.Lifecycle.SubmissionEvidence.Ambiguous");
        }
    }

    public Task<TenantCommandSubmissionResult> DisableTenantAsync(
        TenantLifecycleCommandRequest request,
        CancellationToken cancellationToken = default)
        => DisableTenantTrackedAsync(request, ulidFactory.NewUlid(), cancellationToken);

    public async Task<TenantCommandSubmissionResult> DisableTenantTrackedAsync(
        TenantLifecycleCommandRequest request,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.TenantId) || request.Operation is not TenantLifecycleOperation.DisableTenant)
        {
            return TenantCommandSubmissionResult.Failed("Tenant id and lifecycle operation are required before the command can be submitted.");
        }

        if (string.IsNullOrWhiteSpace(messageId)
            || !TryResolveMessageId(messageId, out string resolvedMessageId))
        {
            return TenantCommandSubmissionResult.FailedWithKey("Tenants.Commands.Unavailable.InvalidTrackingReference");
        }

        var command = new DisableTenant(request.TenantId);
        var submit = new SubmitCommandRequest(
            resolvedMessageId,
            SystemTenant,
            TenantsDomain,
            request.TenantId,
            nameof(DisableTenant),
            JsonSerializer.SerializeToElement(command));

        try
        {
            SubmitCommandResponse response = await gatewayClient
                .SubmitCommandAsync(submit, cancellationToken)
                .ConfigureAwait(false);

            return TenantCommandSubmissionResult.Accepted(resolvedMessageId, response.CorrelationId);
        }
        catch (EventStoreGatewayException ex)
        {
            return MapLifecycleGatewayException(ex) with { MessageId = resolvedMessageId };
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return TenantCommandSubmissionResult.Ambiguous(
                resolvedMessageId,
                "Tenants.Lifecycle.SubmissionEvidence.Ambiguous");
        }
        catch (HttpRequestException)
        {
            return TenantCommandSubmissionResult.Ambiguous(
                resolvedMessageId,
                "Tenants.Lifecycle.SubmissionEvidence.Ambiguous");
        }
    }

    public async Task<TenantCommandStatusResult> GetStatusAsync(
        TenantCommandTrackingHandle handle,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handle);

        try
        {
            using HttpResponseMessage response = await statusClient
                .GetAsync($"api/v1/commands/status/{Uri.EscapeDataString(handle.CorrelationId)}", cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return TenantCommandStatusResult.Pending("Command status is not available yet.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return IsRetryableStatusCode(response.StatusCode)
                    ? TenantCommandStatusResult.RetryableFailure("Command status could not be verified yet.")
                    : TenantCommandStatusResult.Unknown("Command status request was rejected.");
            }

            TenantCommandStatusResponse? status = await response.Content
                .ReadFromJsonAsync<TenantCommandStatusResponse>(WebJsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (status is null)
            {
                return TenantCommandStatusResult.RetryableFailure("Command status response was unavailable.");
            }

            if (string.IsNullOrWhiteSpace(status.CorrelationId)
                || !string.Equals(status.CorrelationId, handle.CorrelationId, StringComparison.Ordinal))
            {
                return TenantCommandStatusResult.Unknown("Command status response did not match the tracked command.");
            }

            if (!Enum.TryParse(status.Status, ignoreCase: false, out CommandStatus parsedStatus))
            {
                return TenantCommandStatusResult.RetryableFailure("Command status response was unavailable.");
            }

            if (!Enum.IsDefined(parsedStatus))
            {
                return TenantCommandStatusResult.Unknown("Command status response was unavailable.");
            }

            bool hasVerifiedCommandIdentity = !string.IsNullOrWhiteSpace(status.MessageId)
                && string.Equals(status.MessageId, handle.MessageId, StringComparison.Ordinal)
                && (string.IsNullOrWhiteSpace(handle.AggregateId)
                    || string.Equals(status.AggregateId, handle.AggregateId, StringComparison.Ordinal));
            if (!string.IsNullOrWhiteSpace(handle.AggregateId) && !hasVerifiedCommandIdentity)
            {
                return new TenantCommandStatusResult(
                    parsedStatus,
                    "Command status response did not match the tracked lifecycle command.",
                    EventCount: status.EventCount,
                    HasVerifiedCommandIdentity: false);
            }

            return new TenantCommandStatusResult(
                parsedStatus,
                SafeMessageForStatus(parsedStatus, status.RejectionEventType, status.FailureReason),
                SafeRejectionCode(status.RejectionEventType),
                status.EventCount,
                HasVerifiedCommandIdentity: hasVerifiedCommandIdentity);
        }
        catch (JsonException)
        {
            return TenantCommandStatusResult.RetryableFailure("Command status response was unavailable.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return TenantCommandStatusResult.RetryableFailure("Command status could not be verified yet.");
        }
        catch (HttpRequestException)
        {
            return TenantCommandStatusResult.RetryableFailure("Command status could not be verified yet.");
        }
    }

    private static bool IsRetryableStatusCode(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
        || (int)statusCode >= 500;

    private static TenantCommandSubmissionResult MapGatewayException(EventStoreGatewayException exception) {
        if (IsTenantAlreadyExists(exception)) {
            return TenantCommandSubmissionResult.Rejected(
                "A tenant with this id already exists. Refresh the list or open the existing tenant if it is visible.",
                "TenantAlreadyExists");
        }

        return exception.StatusCode switch {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden
                => TenantCommandSubmissionResult.Rejected("You are not authorized to create tenants.", "InsufficientPermissions"),
            (int)HttpStatusCode.BadRequest
                => TenantCommandSubmissionResult.Failed("The create tenant request was not accepted. Check the form fields and try again."),
            (int)HttpStatusCode.ServiceUnavailable
                => TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."),
            _ => TenantCommandSubmissionResult.Failed("Tenant command submission failed before it could be verified."),
        };
    }

    private static TenantCommandSubmissionResult MapAddUserToTenantGatewayException(EventStoreGatewayException exception) {
        (string Code, string Message)? rejection = SafeAddMemberRejection(exception);
        if (rejection is not null) {
            return TenantCommandSubmissionResult.Rejected(rejection.Value.Message, rejection.Value.Code);
        }

        return exception.StatusCode switch {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden
                => TenantCommandSubmissionResult.Rejected("You are not authorized to add members to this tenant.", "InsufficientPermissions"),
            (int)HttpStatusCode.BadRequest
                => TenantCommandSubmissionResult.Failed("The add member request was not accepted. Check the form fields and try again."),
            (int)HttpStatusCode.ServiceUnavailable
                => TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."),
            _ => TenantCommandSubmissionResult.Failed("Tenant command submission failed before it could be verified."),
        };
    }

    private static TenantCommandSubmissionResult MapChangeUserRoleGatewayException(EventStoreGatewayException exception) {
        (string Code, string Message)? rejection = SafeChangeRoleRejection(exception);
        if (rejection is not null) {
            return TenantCommandSubmissionResult.Rejected(rejection.Value.Message, rejection.Value.Code);
        }

        return exception.StatusCode switch {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden
                => TenantCommandSubmissionResult.Rejected("You are not authorized to change member roles in this tenant.", "InsufficientPermissions"),
            (int)HttpStatusCode.BadRequest
                => TenantCommandSubmissionResult.Failed("The change role request was not accepted. Check the form fields and try again."),
            (int)HttpStatusCode.ServiceUnavailable
                => TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."),
            _ => TenantCommandSubmissionResult.Failed("Tenant command submission failed before it could be verified."),
        };
    }

    private static TenantCommandSubmissionResult MapRemoveUserFromTenantGatewayException(EventStoreGatewayException exception) {
        (string Code, string Message)? rejection = SafeRemoveMemberRejection(exception);
        if (rejection is not null) {
            return TenantCommandSubmissionResult.Rejected(rejection.Value.Message, rejection.Value.Code);
        }

        return exception.StatusCode switch {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden
                => TenantCommandSubmissionResult.Rejected("You are not authorized to remove members from this tenant.", "InsufficientPermissions"),
            (int)HttpStatusCode.BadRequest
                => TenantCommandSubmissionResult.Failed("The remove member request was not accepted. Check the visible member evidence and try again."),
            (int)HttpStatusCode.ServiceUnavailable
                => TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."),
            _ => TenantCommandSubmissionResult.Failed("Tenant command submission failed before it could be verified."),
        };
    }

    private static TenantCommandSubmissionResult MapUpdateTenantGatewayException(EventStoreGatewayException exception) {
        (string Code, string Message)? rejection = SafeUpdateTenantRejection(exception);
        if (rejection is not null) {
            return TenantCommandSubmissionResult.Rejected(rejection.Value.Message, rejection.Value.Code);
        }

        return exception.StatusCode switch {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden
                => TenantCommandSubmissionResult.Rejected("You are not authorized to edit this tenant's metadata.", "InsufficientPermissions"),
            (int)HttpStatusCode.BadRequest
                => TenantCommandSubmissionResult.Failed("The metadata update request was not accepted. Check the form fields and try again."),
            (int)HttpStatusCode.ServiceUnavailable
                => TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."),
            _ => TenantCommandSubmissionResult.Failed("Tenant command submission failed before it could be verified."),
        };
    }

    private static TenantCommandSubmissionResult MapSetTenantConfigurationGatewayException(EventStoreGatewayException exception) {
        (string Code, string Message)? rejection = SafeSetConfigurationRejection(exception);
        if (rejection is not null) {
            return TenantCommandSubmissionResult.Rejected(rejection.Value.Message, rejection.Value.Code);
        }

        return exception.StatusCode switch {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden
                => TenantCommandSubmissionResult.Rejected("You are not authorized to set configuration for this tenant.", "InsufficientPermissions"),
            (int)HttpStatusCode.BadRequest
                => TenantCommandSubmissionResult.Failed("The configuration change request was not accepted. Check the form fields and try again."),
            (int)HttpStatusCode.ServiceUnavailable
                => TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."),
            _ => TenantCommandSubmissionResult.Failed("Tenant command submission failed before it could be verified."),
        };
    }

    private static TenantCommandSubmissionResult MapRemoveTenantConfigurationGatewayException(EventStoreGatewayException exception) {
        (string Code, string Message)? rejection = SafeRemoveConfigurationRejection(exception);
        if (rejection is not null) {
            return TenantCommandSubmissionResult.Rejected(rejection.Value.Message, rejection.Value.Code);
        }

        return exception.StatusCode switch {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden
                => TenantCommandSubmissionResult.Rejected("You are not authorized to remove configuration for this tenant.", "InsufficientPermissions"),
            (int)HttpStatusCode.BadRequest
                => TenantCommandSubmissionResult.Failed("The configuration removal request was not accepted. Check the visible key evidence and try again."),
            (int)HttpStatusCode.ServiceUnavailable
                => TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."),
            _ => TenantCommandSubmissionResult.Failed("Tenant command submission failed before it could be verified."),
        };
    }

    private static TenantCommandSubmissionResult MapLifecycleGatewayException(EventStoreGatewayException exception) {
        (string Code, string Message)? rejection = SafeLifecycleRejection(exception);
        if (rejection is not null) {
            return TenantCommandSubmissionResult.Rejected(rejection.Value.Message, rejection.Value.Code);
        }

        return exception.StatusCode switch {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden
                => TenantCommandSubmissionResult.Rejected("You are not authorized to submit tenant lifecycle commands.", "InsufficientPermissions"),
            (int)HttpStatusCode.BadRequest
                => TenantCommandSubmissionResult.Failed("The lifecycle request was not accepted. Check the visible tenant evidence and try again."),
            (int)HttpStatusCode.RequestTimeout or (int)HttpStatusCode.TooManyRequests
                => TenantCommandSubmissionResult.Ambiguous(
                    string.Empty,
                    "Tenants.Lifecycle.SubmissionEvidence.Ambiguous"),
            >= 500
                => TenantCommandSubmissionResult.Ambiguous(
                    string.Empty,
                    "Tenants.Lifecycle.SubmissionEvidence.Ambiguous"),
            _ => TenantCommandSubmissionResult.Failed("The lifecycle command submission was rejected before dispatch could be verified."),
        };
    }

    private static TenantCommandSubmissionResult MapSetGlobalAdministratorGatewayException(EventStoreGatewayException exception) {
        (string Code, string Message)? rejection = SafeGlobalAdministratorRejection(exception);
        if (rejection is not null) {
            return TenantCommandSubmissionResult.Rejected(rejection.Value.Message, rejection.Value.Code);
        }

        return exception.StatusCode switch {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden
                => TenantCommandSubmissionResult.Rejected("You are not authorized to change platform governance.", "InsufficientPermissions"),
            (int)HttpStatusCode.BadRequest
                => TenantCommandSubmissionResult.Failed("The global administrator grant request was not accepted. Check the visible user id and try again."),
            (int)HttpStatusCode.ServiceUnavailable
                => TenantCommandSubmissionResult.Failed("Global administrator command gateway is unavailable."),
            _ => TenantCommandSubmissionResult.Failed("Global administrator command submission failed before it could be verified."),
        };
    }

    private static TenantCommandSubmissionResult MapRemoveGlobalAdministratorGatewayException(EventStoreGatewayException exception) {
        (string Code, string Message)? rejection = SafeGlobalAdministratorRejection(exception);
        if (rejection is not null) {
            return TenantCommandSubmissionResult.Rejected(rejection.Value.Message, rejection.Value.Code);
        }

        return exception.StatusCode switch {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden
                => TenantCommandSubmissionResult.Rejected("You are not authorized to change platform governance.", "InsufficientPermissions"),
            (int)HttpStatusCode.BadRequest
                => TenantCommandSubmissionResult.Failed("The global administrator removal request was not accepted. Check the visible user id and try again."),
            (int)HttpStatusCode.ServiceUnavailable
                => TenantCommandSubmissionResult.Failed("Global administrator command gateway is unavailable."),
            _ => TenantCommandSubmissionResult.Failed("Global administrator command submission failed before it could be verified."),
        };
    }

    private static bool IsTenantAlreadyExists(EventStoreGatewayException exception)
        => Contains(exception.ReasonCode, "tenant-already-exists")
        || Contains(exception.Reason, "TenantAlreadyExists")
        || Contains(exception.Title, "TenantAlreadyExists")
        || Contains(exception.Type, "tenant-already-exists")
        || Contains(exception.Detail, "TenantAlreadyExists")
        || Contains(ProblemDetailsExtension(exception, GatewayProblemDetailsExtensions.RejectionType), "TenantAlreadyExists")
        || Contains(ProblemDetailsExtension(exception, GatewayProblemDetailsExtensions.CorrectiveAction), "already exists");

    private static string? SafeMessageForStatus(CommandStatus status, string? rejectionEventType, string? failureReason)
        => status switch {
            CommandStatus.Rejected when SafeSharedStatusRejection(rejectionEventType) is { } rejection
                => rejection.Message,
            // GetStatusAsync is shared by every Tenants command (create-tenant, add-member,
            // change-role), so the generic rejected fallback must stay command-neutral instead of
            // naming a single command, otherwise an unrecognized rejection would surface one
            // command's copy in another command's lifecycle panel.
            CommandStatus.Rejected => "The command was rejected.",
            CommandStatus.PublishFailed => "The command was accepted, but publication could not be verified.",
            CommandStatus.TimedOut => "The command status timed out before the result could be verified.",
            _ => BoundSafeFailureReason(failureReason),
        };

    private static string? SafeRejectionCode(string? rejectionEventType)
        => SafeSharedStatusRejection(rejectionEventType)?.Code;

    // GetStatusAsync is shared by every Tenants command (create-tenant, add-member, change-role,
    // remove-member)
    // and only carries a correlation id, so it cannot tell which command produced a rejection.
    // Command-UNIQUE rejection types (TenantAlreadyExists -> create, UserAlreadyInTenant -> add,
    // UserNotInTenant -> change-role) keep command-specific copy. Rejection types that are SHARED
    // across commands (InsufficientPermissions, TenantDisabled, TenantNotFound, RoleEscalation)
    // must stay command-neutral so one command's copy never leaks into another command's lifecycle
    // panel. This keeps the Story 2.2 shared-status discipline symmetric for member commands.
    private static (string Code, string Message)? SafeSharedStatusRejection(string? value) {
        if (Contains(value, "TenantAlreadyExists")) {
            return ("TenantAlreadyExists", "A tenant with this id already exists. Refresh the list or open the existing tenant if it is visible.");
        }

        if (Contains(value, "UserAlreadyInTenant")) {
            return ("UserAlreadyInTenant", "This user is already a member of the tenant. Refresh the member table before trying another action.");
        }

        if (Contains(value, "UserNotInTenant")) {
            return ("UserNotInTenant", "The target user is not a visible member of this tenant. Refresh the member table before trying again.");
        }

        if (Contains(value, "RoleEscalation")) {
            return ("RoleEscalation", "The requested tenant role cannot be assigned by this command.");
        }

        if (Contains(value, "InsufficientPermissions")) {
            return ("InsufficientPermissions", "You are not authorized to submit this command.");
        }

        if (Contains(value, "TenantDisabled")) {
            return ("TenantDisabled", "This tenant is disabled, so the command cannot be completed.");
        }

        if (Contains(value, "TenantNotFound")) {
            return ("TenantNotFound", "The tenant was not found. Refresh the tenant detail before trying again.");
        }

        if (Contains(value, "ConfigurationLimitExceeded")) {
            return ("ConfigurationLimitExceeded", "The configuration change exceeded tenant configuration limits.");
        }

        if (Contains(value, "ConfigurationKeyNotFound")) {
            return ("ConfigurationKeyNotFound", "The configuration key was not found in the current tenant projection. Refresh tenant detail before treating removal as complete.");
        }

        if (Contains(value, "TenantLifecycleStateAlreadySet")) {
            return ("TenantLifecycleStateAlreadySet", "The tenant lifecycle already matches the requested state. Refresh tenant detail before trying another lifecycle action.");
        }

        if (Contains(value, "GlobalAdministratorAlreadyExists")) {
            return ("GlobalAdministratorAlreadyExists", "This user is already a global administrator. Refresh the platform authority projection before trying another action.");
        }

        if (Contains(value, "LastGlobalAdministrator")) {
            return ("LastGlobalAdministrator", "The last global administrator cannot be removed. Keep the current projection visible and add another global administrator before trying again.");
        }

        if (Contains(value, "GlobalAdministratorNotFound")) {
            return ("GlobalAdministratorNotFound", "The target user is not a global administrator in the last-confirmed projection. Refresh platform authority before trying another action.");
        }

        return null;
    }

    private static (string Code, string Message)? SafeAddMemberRejection(EventStoreGatewayException exception)
        => SafeAddMemberRejection(
            string.Join(
                "|",
                exception.ReasonCode,
                exception.Reason,
                exception.Title,
                exception.Type,
                exception.Detail));

    private static (string Code, string Message)? SafeAddMemberRejection(string? value) {
        if (Contains(value, "UserAlreadyInTenant")) {
            return ("UserAlreadyInTenant", "This user is already a member of the tenant. Refresh the member table before trying another action.");
        }

        if (Contains(value, "RoleEscalation")) {
            return ("RoleEscalation", "The requested tenant role cannot be assigned by this command.");
        }

        if (Contains(value, "InsufficientPermissions")) {
            return ("InsufficientPermissions", "You are not authorized to add members to this tenant.");
        }

        if (Contains(value, "TenantDisabled")) {
            return ("TenantDisabled", "This tenant is disabled, so members cannot be added.");
        }

        if (Contains(value, "TenantNotFound")) {
            return ("TenantNotFound", "The tenant was not found. Refresh the tenant detail before trying again.");
        }

        return null;
    }

    private static (string Code, string Message)? SafeChangeRoleRejection(EventStoreGatewayException exception)
        => SafeChangeRoleRejection(
            string.Join(
                "|",
                exception.ReasonCode,
                exception.Reason,
                exception.Title,
                exception.Type,
                exception.Detail));

    private static (string Code, string Message)? SafeChangeRoleRejection(string? value) {
        if (Contains(value, "RoleEscalation")) {
            return ("RoleEscalation", "The requested tenant role cannot be assigned by this command.");
        }

        if (Contains(value, "UserNotInTenant")) {
            return ("UserNotInTenant", "The target user is not a visible member of this tenant. Refresh the member table before trying again.");
        }

        if (Contains(value, "InsufficientPermissions")) {
            return ("InsufficientPermissions", "You are not authorized to change member roles in this tenant.");
        }

        if (Contains(value, "TenantDisabled")) {
            return ("TenantDisabled", "This tenant is disabled, so member roles cannot be changed.");
        }

        if (Contains(value, "TenantNotFound")) {
            return ("TenantNotFound", "The tenant was not found. Refresh the tenant detail before trying again.");
        }

        return null;
    }

    private static (string Code, string Message)? SafeRemoveMemberRejection(EventStoreGatewayException exception)
        => SafeRemoveMemberRejection(
            string.Join(
                "|",
                exception.ReasonCode,
                exception.Reason,
                exception.Title,
                exception.Type,
                exception.Detail));

    private static (string Code, string Message)? SafeRemoveMemberRejection(string? value) {
        if (Contains(value, "UserNotInTenant")) {
            return ("UserNotInTenant", "The target user is not a visible member of this tenant. Refresh the member table before treating removal as already applied.");
        }

        if (Contains(value, "InsufficientPermissions")) {
            return ("InsufficientPermissions", "You are not authorized to remove members from this tenant.");
        }

        if (Contains(value, "TenantDisabled")) {
            return ("TenantDisabled", "This tenant is disabled, so members cannot be removed.");
        }

        if (Contains(value, "TenantNotFound")) {
            return ("TenantNotFound", "The tenant was not found. Refresh the tenant detail before trying again.");
        }

        return null;
    }

    private static (string Code, string Message)? SafeRemoveConfigurationRejection(EventStoreGatewayException exception)
        => SafeRemoveConfigurationRejection(
            string.Join(
                "|",
                exception.ReasonCode,
                exception.Reason,
                exception.Title,
                exception.Type,
                exception.Detail));

    private static (string Code, string Message)? SafeRemoveConfigurationRejection(string? value) {
        if (Contains(value, "ConfigurationKeyNotFound")) {
            return ("ConfigurationKeyNotFound", "The configuration key was not found in the current tenant projection. Refresh tenant detail before treating removal as complete.");
        }

        if (Contains(value, "InsufficientPermissions")) {
            return ("InsufficientPermissions", "You are not authorized to remove configuration for this tenant.");
        }

        if (Contains(value, "TenantDisabled")) {
            return ("TenantDisabled", "This tenant is disabled, so configuration cannot be removed.");
        }

        if (Contains(value, "TenantNotFound")) {
            return ("TenantNotFound", "The tenant was not found. Refresh the tenant detail before trying again.");
        }

        return null;
    }

    private static (string Code, string Message)? SafeUpdateTenantRejection(EventStoreGatewayException exception)
        => SafeUpdateTenantRejection(
            string.Join(
                "|",
                exception.ReasonCode,
                exception.Reason,
                exception.Title,
                exception.Type,
                exception.Detail));

    private static (string Code, string Message)? SafeUpdateTenantRejection(string? value) {
        if (Contains(value, "InsufficientPermissions")) {
            return ("InsufficientPermissions", "You are not authorized to edit this tenant's metadata.");
        }

        if (Contains(value, "TenantDisabled")) {
            return ("TenantDisabled", "This tenant is disabled, so metadata cannot be edited.");
        }

        if (Contains(value, "TenantNotFound")) {
            return ("TenantNotFound", "The tenant was not found. Refresh the tenant detail before trying again.");
        }

        return null;
    }

    private static (string Code, string Message)? SafeSetConfigurationRejection(EventStoreGatewayException exception)
        => SafeSetConfigurationRejection(
            string.Join(
                "|",
                exception.ReasonCode,
                exception.Reason,
                exception.Title,
                exception.Type,
                exception.Detail));

    private static (string Code, string Message)? SafeSetConfigurationRejection(string? value) {
        if (Contains(value, "ConfigurationLimitExceeded")) {
            return ("ConfigurationLimitExceeded", "The configuration value exceeds the tenant configuration limits.");
        }

        if (Contains(value, "InsufficientPermissions")) {
            return ("InsufficientPermissions", "You are not authorized to set configuration for this tenant.");
        }

        if (Contains(value, "TenantDisabled")) {
            return ("TenantDisabled", "This tenant is disabled, so configuration cannot be changed.");
        }

        if (Contains(value, "TenantNotFound")) {
            return ("TenantNotFound", "The tenant was not found. Refresh the tenant detail before trying again.");
        }

        return null;
    }

    private static (string Code, string Message)? SafeLifecycleRejection(EventStoreGatewayException exception)
        => SafeLifecycleRejection(
            string.Join(
                "|",
                exception.ReasonCode,
                exception.Reason,
                exception.Title,
                exception.Type,
                exception.Detail));

    private static (string Code, string Message)? SafeLifecycleRejection(string? value) {
        if (Contains(value, "TenantLifecycleStateAlreadySet")) {
            return ("TenantLifecycleStateAlreadySet", "The tenant lifecycle already matches the requested state. Refresh tenant detail before trying another lifecycle action.");
        }

        if (Contains(value, "TenantDisabled")) {
            return ("TenantDisabled", "This tenant is disabled, so the requested tenant command cannot be completed.");
        }

        if (Contains(value, "TenantNotFound")) {
            return ("TenantNotFound", "The tenant was not found. Refresh the tenant detail before trying again.");
        }

        if (Contains(value, "InsufficientPermissions")) {
            return ("InsufficientPermissions", "You are not authorized to submit tenant lifecycle commands.");
        }

        return null;
    }

    private static (string Code, string Message)? SafeGlobalAdministratorRejection(EventStoreGatewayException exception)
        => SafeGlobalAdministratorRejection(
            string.Join(
                "|",
                exception.ReasonCode,
                exception.Reason,
                exception.Title,
                exception.Type,
                exception.Detail));

    private static (string Code, string Message)? SafeGlobalAdministratorRejection(string? value) {
        if (Contains(value, "GlobalAdministratorAlreadyExists")) {
            return ("GlobalAdministratorAlreadyExists", "This user is already a global administrator. Refresh the platform authority projection before trying another action.");
        }

        if (Contains(value, "LastGlobalAdministrator")) {
            return ("LastGlobalAdministrator", "The last global administrator cannot be removed. Keep the current projection visible and add another global administrator before trying again.");
        }

        if (Contains(value, "GlobalAdministratorNotFound")) {
            return ("GlobalAdministratorNotFound", "The target user is not a global administrator in the last-confirmed projection. Refresh platform authority before trying another action.");
        }

        if (Contains(value, "InsufficientPermissions")) {
            return ("InsufficientPermissions", "You are not authorized to change platform governance.");
        }

        return null;
    }

    private static bool IsAssignableTenantRole(TenantRole role)
        => role is TenantRole.TenantOwner or TenantRole.TenantContributor or TenantRole.TenantReader;

    private bool TryResolveMessageId(string? messageId, out string resolvedMessageId) {
        string candidate = string.IsNullOrWhiteSpace(messageId)
            ? ulidFactory.NewUlid()
            : messageId.Trim().ToUpperInvariant();
        if (!NUlid.Ulid.TryParse(candidate, out NUlid.Ulid parsed)) {
            resolvedMessageId = string.Empty;
            return false;
        }

        resolvedMessageId = parsed.ToString();
        return string.Equals(candidate, resolvedMessageId, StringComparison.Ordinal);
    }

    private static string? BoundSafeFailureReason(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        return UnsafeSupportMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase))
                ? "The command status included an unavailable support detail."
                : value[..Math.Min(value.Length, 160)];
    }

    private static bool Contains(string? value, string expected)
        => value?.Contains(expected, StringComparison.OrdinalIgnoreCase) == true;

    private static string? ProblemDetailsExtension(EventStoreGatewayException exception, string key)
        => exception.Extensions.TryGetValue(key, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private sealed record TenantCommandStatusResponse(
        string CorrelationId,
        string Status,
        int StatusCode,
        DateTimeOffset Timestamp,
        string? AggregateId,
        int? EventCount,
        string? RejectionEventType,
        string? FailureReason,
        string? TimeoutDuration,
        string? MessageId = null);
}
