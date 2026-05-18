using System.Text.Json;

using Dapr.Client;

using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Server.Projections;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.Tenants.Projections;

/// <summary>
/// Handles tenant projection requests using the full event history supplied by EventStore.
/// </summary>
/// <remarks>
/// The per-aggregate <see cref="TenantReadModel"/> rebuild from a fresh state is correct
/// under the full-replay projection contract. The index-side state-store read-and-merge
/// path is independent and orthogonal.
/// </remarks>
public sealed class TenantProjectionHandler {
    private const string StateStoreName = "statestore";
    private const string TenantAuditProjectionKeyPrefix = "audit:";
    private const string TenantIndexKeyCategory = "tenant index";
    private const string TenantIndexProjectionKey = "projection:tenant-index:singleton";
    private const string TenantProjectionKeyCategory = "tenant read-model";
    private const string TenantProjectionKeyPrefix = "projection:tenants:";

    private static readonly JsonSerializerOptions s_options = new() {
        PropertyNameCaseInsensitive = true,
    };

    private readonly ILogger<TenantProjectionHandler> _logger;
    private readonly ITenantProjectionStateStore _stateStore;

    public TenantProjectionHandler(DaprClient daprClient)
        : this(CreateStateStore(daprClient), NullLogger<TenantProjectionHandler>.Instance) {
    }

    public TenantProjectionHandler(DaprClient daprClient, ILogger<TenantProjectionHandler> logger)
        : this(CreateStateStore(daprClient), logger) {
    }

    internal TenantProjectionHandler(ITenantProjectionStateStore stateStore, ILogger<TenantProjectionHandler> logger) {
        ArgumentNullException.ThrowIfNull(stateStore);
        ArgumentNullException.ThrowIfNull(logger);

        _stateStore = stateStore;
        _logger = logger;
    }

    public async Task<ProjectionResponse> ProjectAsync(ProjectionRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyCollection<ProjectionEventDto?> events = request.Events ?? [];

        TenantReadModel state = await TenantProjectionWritePolicy
            .SaveWithOptimisticConcurrencyAsync(
                _stateStore,
                _logger,
                StateStoreName,
                TenantProjectionKeyPrefix + request.AggregateId,
                TenantProjectionKeyCategory,
                nameof(TenantProjectionHandler) + "." + nameof(ProjectAsync),
                events,
                static () => new TenantReadModel(),
                ApplyEvent)
            .ConfigureAwait(false);

        TenantAuditReadModel auditModel = TenantAuditProjection.ProjectAuditEvents(events.OfType<ProjectionEventDto>());
        await _stateStore.SaveStateAsync(
            StateStoreName,
            TenantAuditProjectionKeyPrefix + request.AggregateId,
            auditModel).ConfigureAwait(false);

        _ = await TenantProjectionWritePolicy
            .SaveWithOptimisticConcurrencyAsync(
                _stateStore,
                _logger,
                StateStoreName,
                TenantIndexProjectionKey,
                TenantIndexKeyCategory,
                nameof(TenantProjectionHandler) + "." + nameof(ProjectAsync),
                events,
                static () => new TenantIndexReadModel(),
                ApplyIndexEvent)
            .ConfigureAwait(false);

        return new ProjectionResponse(
            "tenants",
            JsonSerializer.SerializeToElement(state));
    }

    private static DaprTenantProjectionStateStore CreateStateStore(DaprClient daprClient) {
        ArgumentNullException.ThrowIfNull(daprClient);
        return new DaprTenantProjectionStateStore(daprClient);
    }

    private static void ApplyEvent(TenantReadModel state, ProjectionEventDto evt) {
        string name = evt.EventTypeName;
        if (string.IsNullOrEmpty(name)) {
            return;
        }

        JsonElement payload = JsonSerializer.Deserialize<JsonElement>(evt.Payload, s_options);

        if (name.EndsWith(nameof(TenantCreated), StringComparison.Ordinal)) {
            TenantCreated? e = JsonSerializer.Deserialize<TenantCreated>(payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(TenantUpdated), StringComparison.Ordinal)) {
            TenantUpdated? e = JsonSerializer.Deserialize<TenantUpdated>(payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(TenantDisabled), StringComparison.Ordinal)) {
            TenantDisabled? e = JsonSerializer.Deserialize<TenantDisabled>(payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(TenantEnabled), StringComparison.Ordinal)) {
            TenantEnabled? e = JsonSerializer.Deserialize<TenantEnabled>(payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(UserAddedToTenant), StringComparison.Ordinal)) {
            UserAddedToTenant? e = JsonSerializer.Deserialize<UserAddedToTenant>(payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(UserRemovedFromTenant), StringComparison.Ordinal)) {
            UserRemovedFromTenant? e = JsonSerializer.Deserialize<UserRemovedFromTenant>(payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(UserRoleChanged), StringComparison.Ordinal)) {
            UserRoleChanged? e = JsonSerializer.Deserialize<UserRoleChanged>(payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(TenantConfigurationSet), StringComparison.Ordinal)) {
            TenantConfigurationSet? e = JsonSerializer.Deserialize<TenantConfigurationSet>(payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(TenantConfigurationRemoved), StringComparison.Ordinal)) {
            TenantConfigurationRemoved? e = JsonSerializer.Deserialize<TenantConfigurationRemoved>(payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
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
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(TenantUpdated), StringComparison.Ordinal)) {
            TenantUpdated? e = JsonSerializer.Deserialize<TenantUpdated>(payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(TenantDisabled), StringComparison.Ordinal)) {
            TenantDisabled? e = JsonSerializer.Deserialize<TenantDisabled>(payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(TenantEnabled), StringComparison.Ordinal)) {
            TenantEnabled? e = JsonSerializer.Deserialize<TenantEnabled>(payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(UserAddedToTenant), StringComparison.Ordinal)) {
            UserAddedToTenant? e = JsonSerializer.Deserialize<UserAddedToTenant>(payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(UserRemovedFromTenant), StringComparison.Ordinal)) {
            UserRemovedFromTenant? e = JsonSerializer.Deserialize<UserRemovedFromTenant>(payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(UserRoleChanged), StringComparison.Ordinal)) {
            UserRoleChanged? e = JsonSerializer.Deserialize<UserRoleChanged>(payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
    }
}
