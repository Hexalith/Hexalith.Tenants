using System.Text.Json;

using Dapr.Client;

using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Server.Projections;

namespace Hexalith.Tenants.Projections;

public sealed class TenantProjectionHandler(DaprClient daprClient) {
    private const string StateStoreName = "statestore";
    private const string TenantIndexProjectionKey = "projection:tenant-index:singleton";
    private const string TenantProjectionKeyPrefix = "projection:tenants:";

    private static readonly JsonSerializerOptions s_options = new() {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<ProjectionResponse> ProjectAsync(ProjectionRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        // Build per-aggregate projection
        TenantReadModel state = new();
        foreach (ProjectionEventDto? evt in request.Events ?? []) {
            if (evt is null) {
                continue;
            }

            ApplyEvent(state, evt);
        }

        // Write per-tenant projection to state store
        await daprClient.SaveStateAsync(
            StateStoreName,
            TenantProjectionKeyPrefix + request.AggregateId,
            state).ConfigureAwait(false);

        // Update tenant index projection
        TenantIndexReadModel? indexModel = await daprClient
            .GetStateAsync<TenantIndexReadModel>(StateStoreName, TenantIndexProjectionKey)
            .ConfigureAwait(false);

        indexModel ??= new TenantIndexReadModel();

        foreach (ProjectionEventDto? evt in request.Events ?? []) {
            if (evt is null) {
                continue;
            }

            ApplyIndexEvent(indexModel, evt);
        }

        await daprClient.SaveStateAsync(
            StateStoreName,
            TenantIndexProjectionKey,
            indexModel).ConfigureAwait(false);

        return new ProjectionResponse(
            "tenants",
            JsonSerializer.SerializeToElement(state));
    }

    private static void ApplyEvent(TenantReadModel state, ProjectionEventDto evt) {
        string name = evt.EventTypeName;
        if (string.IsNullOrEmpty(name)) {
            return;
        }

        JsonElement payload = JsonSerializer.Deserialize<JsonElement>(evt.Payload, s_options);

        if (name.EndsWith(nameof(TenantCreated), StringComparison.Ordinal)) {
            TenantCreated? e = JsonSerializer.Deserialize<TenantCreated>(payload, s_options);
            if (e is not null) state.Apply(e);
        }
        else if (name.EndsWith(nameof(TenantUpdated), StringComparison.Ordinal)) {
            TenantUpdated? e = JsonSerializer.Deserialize<TenantUpdated>(payload, s_options);
            if (e is not null) state.Apply(e);
        }
        else if (name.EndsWith(nameof(TenantDisabled), StringComparison.Ordinal)) {
            TenantDisabled? e = JsonSerializer.Deserialize<TenantDisabled>(payload, s_options);
            if (e is not null) state.Apply(e);
        }
        else if (name.EndsWith(nameof(TenantEnabled), StringComparison.Ordinal)) {
            TenantEnabled? e = JsonSerializer.Deserialize<TenantEnabled>(payload, s_options);
            if (e is not null) state.Apply(e);
        }
        else if (name.EndsWith(nameof(UserAddedToTenant), StringComparison.Ordinal)) {
            UserAddedToTenant? e = JsonSerializer.Deserialize<UserAddedToTenant>(payload, s_options);
            if (e is not null) state.Apply(e);
        }
        else if (name.EndsWith(nameof(UserRemovedFromTenant), StringComparison.Ordinal)) {
            UserRemovedFromTenant? e = JsonSerializer.Deserialize<UserRemovedFromTenant>(payload, s_options);
            if (e is not null) state.Apply(e);
        }
        else if (name.EndsWith(nameof(UserRoleChanged), StringComparison.Ordinal)) {
            UserRoleChanged? e = JsonSerializer.Deserialize<UserRoleChanged>(payload, s_options);
            if (e is not null) state.Apply(e);
        }
        else if (name.EndsWith(nameof(TenantConfigurationSet), StringComparison.Ordinal)) {
            TenantConfigurationSet? e = JsonSerializer.Deserialize<TenantConfigurationSet>(payload, s_options);
            if (e is not null) state.Apply(e);
        }
        else if (name.EndsWith(nameof(TenantConfigurationRemoved), StringComparison.Ordinal)) {
            TenantConfigurationRemoved? e = JsonSerializer.Deserialize<TenantConfigurationRemoved>(payload, s_options);
            if (e is not null) state.Apply(e);
        }
    }

    private static void ApplyIndexEvent(TenantIndexReadModel indexModel, ProjectionEventDto evt) {
        string name = evt.EventTypeName;
        if (string.IsNullOrEmpty(name)) {
            return;
        }

        JsonElement payload = JsonSerializer.Deserialize<JsonElement>(evt.Payload, s_options);

        if (name.EndsWith(nameof(TenantCreated), StringComparison.Ordinal)) {
            TenantCreated? e = JsonSerializer.Deserialize<TenantCreated>(payload, s_options);
            if (e is not null) indexModel.Apply(e);
        }
        else if (name.EndsWith(nameof(TenantUpdated), StringComparison.Ordinal)) {
            TenantUpdated? e = JsonSerializer.Deserialize<TenantUpdated>(payload, s_options);
            if (e is not null) indexModel.Apply(e);
        }
        else if (name.EndsWith(nameof(TenantDisabled), StringComparison.Ordinal)) {
            TenantDisabled? e = JsonSerializer.Deserialize<TenantDisabled>(payload, s_options);
            if (e is not null) indexModel.Apply(e);
        }
        else if (name.EndsWith(nameof(TenantEnabled), StringComparison.Ordinal)) {
            TenantEnabled? e = JsonSerializer.Deserialize<TenantEnabled>(payload, s_options);
            if (e is not null) indexModel.Apply(e);
        }
        else if (name.EndsWith(nameof(UserAddedToTenant), StringComparison.Ordinal)) {
            UserAddedToTenant? e = JsonSerializer.Deserialize<UserAddedToTenant>(payload, s_options);
            if (e is not null) indexModel.Apply(e);
        }
        else if (name.EndsWith(nameof(UserRemovedFromTenant), StringComparison.Ordinal)) {
            UserRemovedFromTenant? e = JsonSerializer.Deserialize<UserRemovedFromTenant>(payload, s_options);
            if (e is not null) indexModel.Apply(e);
        }
        else if (name.EndsWith(nameof(UserRoleChanged), StringComparison.Ordinal)) {
            UserRoleChanged? e = JsonSerializer.Deserialize<UserRoleChanged>(payload, s_options);
            if (e is not null) indexModel.Apply(e);
        }
    }
}
