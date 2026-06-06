using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class TenantCommandGateway(
    IEventStoreGatewayClient gatewayClient,
    IUlidFactory ulidFactory,
    HttpClient statusClient) : ITenantCommandGateway
{
    private const string SystemTenant = "system";
    private const string TenantsDomain = "tenants";

    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

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
        CreateTenantCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrEmpty(request.TenantId) || string.IsNullOrEmpty(request.Name))
        {
            return TenantCommandSubmissionResult.Failed("Tenant id and name are required before the command can be submitted.");
        }

        string messageId = ulidFactory.NewUlid();
        var command = new CreateTenant(request.TenantId, request.Name, request.Description);
        var submit = new SubmitCommandRequest(
            messageId,
            SystemTenant,
            TenantsDomain,
            request.TenantId,
            nameof(CreateTenant),
            JsonSerializer.SerializeToElement(command));

        try
        {
            SubmitCommandResponse response = await gatewayClient
                .SubmitCommandAsync(submit, cancellationToken)
                .ConfigureAwait(false);

            return TenantCommandSubmissionResult.Accepted(messageId, response.CorrelationId);
        }
        catch (EventStoreGatewayException ex)
        {
            return MapGatewayException(ex);
        }
    }

    public async Task<TenantCommandSubmissionResult> AddUserToTenantAsync(
        AddUserToTenantCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrEmpty(request.TenantId)
            || string.IsNullOrEmpty(request.UserId)
            || !IsAssignableTenantRole(request.Role))
        {
            return TenantCommandSubmissionResult.Failed("Tenant id, user id, and role are required before the command can be submitted.");
        }

        string messageId = ulidFactory.NewUlid();
        var command = new AddUserToTenant(request.TenantId, request.UserId, request.Role);
        var submit = new SubmitCommandRequest(
            messageId,
            SystemTenant,
            TenantsDomain,
            request.TenantId,
            nameof(AddUserToTenant),
            JsonSerializer.SerializeToElement(command));

        try
        {
            SubmitCommandResponse response = await gatewayClient
                .SubmitCommandAsync(submit, cancellationToken)
                .ConfigureAwait(false);

            return TenantCommandSubmissionResult.Accepted(messageId, response.CorrelationId);
        }
        catch (EventStoreGatewayException ex)
        {
            return MapAddUserToTenantGatewayException(ex);
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
                return TenantCommandStatusResult.Unknown("Command status is not available yet.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return TenantCommandStatusResult.Unknown("Command status could not be verified.");
            }

            TenantCommandStatusResponse? status = await response.Content
                .ReadFromJsonAsync<TenantCommandStatusResponse>(WebJsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (status is null || !Enum.TryParse(status.Status, ignoreCase: false, out CommandStatus parsedStatus))
            {
                return TenantCommandStatusResult.Unknown("Command status response was unavailable.");
            }

            return new TenantCommandStatusResult(
                parsedStatus,
                SafeMessageForStatus(parsedStatus, status.RejectionEventType, status.FailureReason),
                SafeRejectionCode(status.RejectionEventType));
        }
        catch (JsonException)
        {
            return TenantCommandStatusResult.Unknown("Command status response was unavailable.");
        }
        catch (HttpRequestException)
        {
            return TenantCommandStatusResult.Unknown("Command status could not be verified.");
        }
    }

    private static TenantCommandSubmissionResult MapGatewayException(EventStoreGatewayException exception)
    {
        if (IsTenantAlreadyExists(exception))
        {
            return TenantCommandSubmissionResult.Rejected(
                "A tenant with this id already exists. Refresh the list or open the existing tenant if it is visible.",
                "TenantAlreadyExists");
        }

        return exception.StatusCode switch
        {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden
                => TenantCommandSubmissionResult.Rejected("You are not authorized to create tenants.", "InsufficientPermissions"),
            (int)HttpStatusCode.BadRequest
                => TenantCommandSubmissionResult.Failed("The create tenant request was not accepted. Check the form fields and try again."),
            (int)HttpStatusCode.ServiceUnavailable
                => TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."),
            _ => TenantCommandSubmissionResult.Failed("Tenant command submission failed before it could be verified."),
        };
    }

    private static TenantCommandSubmissionResult MapAddUserToTenantGatewayException(EventStoreGatewayException exception)
    {
        (string Code, string Message)? rejection = SafeAddMemberRejection(exception);
        if (rejection is not null)
        {
            return TenantCommandSubmissionResult.Rejected(rejection.Value.Message, rejection.Value.Code);
        }

        return exception.StatusCode switch
        {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden
                => TenantCommandSubmissionResult.Rejected("You are not authorized to add members to this tenant.", "InsufficientPermissions"),
            (int)HttpStatusCode.BadRequest
                => TenantCommandSubmissionResult.Failed("The add member request was not accepted. Check the form fields and try again."),
            (int)HttpStatusCode.ServiceUnavailable
                => TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."),
            _ => TenantCommandSubmissionResult.Failed("Tenant command submission failed before it could be verified."),
        };
    }

    private static bool IsTenantAlreadyExists(EventStoreGatewayException exception)
        => Contains(exception.ReasonCode, "tenant-already-exists")
        || Contains(exception.Reason, "TenantAlreadyExists")
        || Contains(exception.Title, "TenantAlreadyExists")
        || Contains(exception.Type, "tenant-already-exists")
        || Contains(exception.Detail, "TenantAlreadyExists");

    private static string? SafeMessageForStatus(CommandStatus status, string? rejectionEventType, string? failureReason)
        => status switch
        {
            CommandStatus.Rejected when Contains(rejectionEventType, "TenantAlreadyExists")
                => "A tenant with this id already exists. Refresh the list or open the existing tenant if it is visible.",
            CommandStatus.Rejected when Contains(rejectionEventType, "InsufficientPermissions")
                => "You are not authorized to submit this tenant command.",
            CommandStatus.Rejected when SafeAddMemberRejection(rejectionEventType) is { } addMemberRejection
                => addMemberRejection.Message,
            // GetStatusAsync is shared by every Tenants command (create-tenant and add-member),
            // so the generic rejected fallback must stay command-neutral instead of naming a
            // single command, otherwise an unrecognized add-member rejection would surface
            // create-tenant copy in the add-member lifecycle panel.
            CommandStatus.Rejected => "The command was rejected.",
            CommandStatus.PublishFailed => "The command was accepted, but publication could not be verified.",
            CommandStatus.TimedOut => "The command status timed out before the result could be verified.",
            _ => BoundSafeFailureReason(failureReason),
        };

    private static string? SafeRejectionCode(string? rejectionEventType)
        => Contains(rejectionEventType, "TenantAlreadyExists")
            ? "TenantAlreadyExists"
            : Contains(rejectionEventType, "InsufficientPermissions")
                ? "InsufficientPermissions"
                : SafeAddMemberRejection(rejectionEventType)?.Code;

    private static (string Code, string Message)? SafeAddMemberRejection(EventStoreGatewayException exception)
        => SafeAddMemberRejection(
            string.Join(
                "|",
                exception.ReasonCode,
                exception.Reason,
                exception.Title,
                exception.Type,
                exception.Detail));

    private static (string Code, string Message)? SafeAddMemberRejection(string? value)
    {
        if (Contains(value, "UserAlreadyInTenant"))
        {
            return ("UserAlreadyInTenant", "This user is already a member of the tenant. Refresh the member table before trying another action.");
        }

        if (Contains(value, "RoleEscalation"))
        {
            return ("RoleEscalation", "The requested tenant role cannot be assigned by this command.");
        }

        if (Contains(value, "InsufficientPermissions"))
        {
            return ("InsufficientPermissions", "You are not authorized to add members to this tenant.");
        }

        if (Contains(value, "TenantDisabled"))
        {
            return ("TenantDisabled", "This tenant is disabled, so members cannot be added.");
        }

        if (Contains(value, "TenantNotFound"))
        {
            return ("TenantNotFound", "The tenant was not found. Refresh the tenant detail before trying again.");
        }

        return null;
    }

    private static bool IsAssignableTenantRole(TenantRole role)
        => role is TenantRole.TenantOwner or TenantRole.TenantContributor or TenantRole.TenantReader;

    private static string? BoundSafeFailureReason(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return UnsafeSupportMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase))
                ? "The command status included an unavailable support detail."
                : value[..Math.Min(value.Length, 160)];
    }

    private static bool Contains(string? value, string expected)
        => value?.Contains(expected, StringComparison.OrdinalIgnoreCase) == true;

    private sealed record TenantCommandStatusResponse(
        string CorrelationId,
        string Status,
        int StatusCode,
        DateTimeOffset Timestamp,
        string? AggregateId,
        int? EventCount,
        string? RejectionEventType,
        string? FailureReason,
        string? TimeoutDuration);
}
