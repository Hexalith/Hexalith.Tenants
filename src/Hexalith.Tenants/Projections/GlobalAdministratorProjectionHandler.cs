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

    private static readonly JsonSerializerOptions s_options = new() {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<ProjectionResponse> ProjectAsync(ProjectionRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        GlobalAdministratorReadModel state = new();
        foreach (ProjectionEventDto? evt in request.Events ?? []) {
            if (evt is null) {
                continue;
            }

            ApplyEvent(state, evt);
        }

        await daprClient.SaveStateAsync(
            StateStoreName,
            GlobalAdministratorsProjectionKey,
            state).ConfigureAwait(false);

        return new ProjectionResponse(
            "global-administrators",
            JsonSerializer.SerializeToElement(state));
    }

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
