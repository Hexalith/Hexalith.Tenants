using System.Text.Json;

using Dapr.Client;

using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Server.Projections;

namespace Hexalith.Tenants.Projections;

/// <summary>
/// Handles global-administrator projection requests, rebuilding the singleton read model
/// from the full event history supplied by EventStore and writing it to the DAPR state store.
/// </summary>
/// <remarks>
/// Domain authorization key consumed by <c>TenantsProjectionActor.IsGlobalAdminAsync</c>:
/// <c>projection:global-administrators:singleton</c>. Any other key is bogus for this domain.
/// </remarks>
public sealed class GlobalAdministratorProjectionHandler(DaprClient daprClient) {
    public const string StateStoreName = "statestore";
    public const string GlobalAdministratorsProjectionKey = "projection:global-administrators:singleton";
    public const string GlobalAdministratorsAggregateId = "global-administrators";
    public const string SystemTenantId = "system";
    internal const string TenantAuditProjectionKeyPrefix = "audit:";

    private static readonly JsonSerializerOptions s_options = new() {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<ProjectionResponse> ProjectAsync(ProjectionRequest request, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        if (!IsValidGlobalAdministratorIdentity(request)) {
            throw new ArgumentException(
                "Global-administrator projections must use tenant 'system' and aggregate 'global-administrators'.",
                nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<ProjectionEventDto?> events = request.Events ?? [];
        TenantAuditReadModel auditState = TenantAuditProjection.ProjectAuditEvents(events.OfType<ProjectionEventDto>());
        cancellationToken.ThrowIfCancellationRequested();

        GlobalAdministratorReadModel state = new();
        foreach (ProjectionEventDto? evt in events) {
            if (evt is null) {
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            ApplyEvent(state, evt);
        }

        cancellationToken.ThrowIfCancellationRequested();
        await daprClient.SaveStateAsync(
            StateStoreName,
            GlobalAdministratorsProjectionKey,
            state,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        await daprClient.SaveStateAsync(
            StateStoreName,
            TenantAuditProjectionKeyPrefix + request.TenantId,
            auditState,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return new ProjectionResponse(
            "global-administrators",
            JsonSerializer.SerializeToElement(state));
    }

    internal static bool IsValidGlobalAdministratorIdentity(ProjectionRequest request)
        => string.Equals(request.TenantId, SystemTenantId, StringComparison.Ordinal)
            && string.Equals(request.AggregateId, GlobalAdministratorsAggregateId, StringComparison.Ordinal);

    private static void ApplyEvent(GlobalAdministratorReadModel state, ProjectionEventDto evt) {
        string name = evt.EventTypeName;
        if (string.IsNullOrEmpty(name)) {
            return;
        }

        JsonElement payload = JsonSerializer.Deserialize<JsonElement>(evt.Payload, s_options);

        if (name.EndsWith(nameof(GlobalAdministratorSet), StringComparison.Ordinal)) {
            GlobalAdministratorSet? e = JsonSerializer.Deserialize<GlobalAdministratorSet>(payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(GlobalAdministratorRemoved), StringComparison.Ordinal)) {
            GlobalAdministratorRemoved? e = JsonSerializer.Deserialize<GlobalAdministratorRemoved>(payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
    }
}
