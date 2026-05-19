using System.Text.Json;

using Dapr.Client;

using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;
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
    private const string TenantAuditKeyCategory = "tenant audit";
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

    public async Task<ProjectionResponse> ProjectAsync(ProjectionRequest request, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<ProjectionEventDto?> events = request.Events ?? [];

        // Build (and validate) the incoming audit model first so a missing
        // MessageId/UserId invariant violation aborts the whole batch before
        // any state-store write commits — extending the spirit of AC12 to the
        // tenant read-model and singleton-index writes too.
        TenantAuditReadModel incomingAuditModel = TenantAuditProjection.ProjectAuditEvents(events.OfType<ProjectionEventDto>());
        cancellationToken.ThrowIfCancellationRequested();

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
                ApplyEvent,
                cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        _ = await TenantProjectionWritePolicy
            .SaveMergedWithOptimisticConcurrencyAsync(
                _stateStore,
                _logger,
                StateStoreName,
                TenantAuditProjectionKeyPrefix + request.AggregateId,
                TenantAuditKeyCategory,
                nameof(TenantProjectionHandler) + "." + nameof(ProjectAsync) + ":" + request.AggregateId,
                events,
                incomingAuditModel,
                static () => new TenantAuditReadModel(),
                MergeAuditState,
                cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

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
                ApplyIndexEvent,
                cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return new ProjectionResponse(
            "tenants",
            JsonSerializer.SerializeToElement(state));
    }

    private static DaprTenantProjectionStateStore CreateStateStore(DaprClient daprClient) {
        ArgumentNullException.ThrowIfNull(daprClient);
        return new DaprTenantProjectionStateStore(daprClient);
    }

    private static TenantAuditReadModel MergeAuditState(TenantAuditReadModel persisted, TenantAuditReadModel incoming) {
        ArgumentNullException.ThrowIfNull(persisted);
        ArgumentNullException.ThrowIfNull(incoming);

        // Build into a new model so the caller's persisted instance is never
        // mutated. Required for any state-store implementation that returns a
        // cached/shared reference from GetStateAndETagAsync.
        TenantAuditReadModel merged = new() {
            Entries = [.. persisted.Entries],
        };

        // Null/whitespace EventIds cannot participate in dedup. Persisted
        // entries are preserved verbatim above so audit history is never
        // silently dropped; only the dedup set excludes them.
        HashSet<string> seenEventIds = merged
            .Entries
            .Select(e => e.EventId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        foreach (TenantAuditEntry entry in incoming.Entries) {
            if (string.IsNullOrWhiteSpace(entry.EventId)) {
                continue;
            }

            if (seenEventIds.Add(entry.EventId)) {
                merged.Entries.Add(entry);
            }
        }

        merged.SortEntries();
        return merged;
    }

    private static void ApplyEvent(TenantReadModel state, ProjectionEventDto evt) {
        string name = evt.EventTypeName;
        if (string.IsNullOrEmpty(name)) {
            return;
        }

        if (name.EndsWith(nameof(TenantCreated), StringComparison.Ordinal)) {
            TenantCreated? e = JsonSerializer.Deserialize<TenantCreated>(evt.Payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(TenantUpdated), StringComparison.Ordinal)) {
            TenantUpdated? e = JsonSerializer.Deserialize<TenantUpdated>(evt.Payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(TenantDisabled), StringComparison.Ordinal)) {
            TenantDisabled? e = JsonSerializer.Deserialize<TenantDisabled>(evt.Payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(TenantEnabled), StringComparison.Ordinal)) {
            TenantEnabled? e = JsonSerializer.Deserialize<TenantEnabled>(evt.Payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(UserAddedToTenant), StringComparison.Ordinal)) {
            UserAddedToTenant? e = JsonSerializer.Deserialize<UserAddedToTenant>(evt.Payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(UserRemovedFromTenant), StringComparison.Ordinal)) {
            UserRemovedFromTenant? e = JsonSerializer.Deserialize<UserRemovedFromTenant>(evt.Payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(UserRoleChanged), StringComparison.Ordinal)) {
            UserRoleChanged? e = JsonSerializer.Deserialize<UserRoleChanged>(evt.Payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(TenantConfigurationSet), StringComparison.Ordinal)) {
            TenantConfigurationSet? e = JsonSerializer.Deserialize<TenantConfigurationSet>(evt.Payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(TenantConfigurationRemoved), StringComparison.Ordinal)) {
            TenantConfigurationRemoved? e = JsonSerializer.Deserialize<TenantConfigurationRemoved>(evt.Payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
    }

    /// <summary>
    /// Applies a projection event to the shared tenant index read model.
    /// </summary>
    /// <param name="indexModel">The tenant index model to update.</param>
    /// <param name="evt">The projection event to apply.</param>
    internal static void ApplyIndexEvent(TenantIndexReadModel indexModel, ProjectionEventDto evt) {
        string name = evt.EventTypeName;
        if (string.IsNullOrEmpty(name)) {
            return;
        }

        if (name.EndsWith(nameof(TenantCreated), StringComparison.Ordinal)) {
            TenantCreated? e = JsonSerializer.Deserialize<TenantCreated>(evt.Payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(TenantUpdated), StringComparison.Ordinal)) {
            TenantUpdated? e = JsonSerializer.Deserialize<TenantUpdated>(evt.Payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(TenantDisabled), StringComparison.Ordinal)) {
            TenantDisabled? e = JsonSerializer.Deserialize<TenantDisabled>(evt.Payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(TenantEnabled), StringComparison.Ordinal)) {
            TenantEnabled? e = JsonSerializer.Deserialize<TenantEnabled>(evt.Payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(UserAddedToTenant), StringComparison.Ordinal)) {
            UserAddedToTenant? e = JsonSerializer.Deserialize<UserAddedToTenant>(evt.Payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(UserRemovedFromTenant), StringComparison.Ordinal)) {
            UserRemovedFromTenant? e = JsonSerializer.Deserialize<UserRemovedFromTenant>(evt.Payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
        else if (name.EndsWith(nameof(UserRoleChanged), StringComparison.Ordinal)) {
            UserRoleChanged? e = JsonSerializer.Deserialize<UserRoleChanged>(evt.Payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
    }
}
