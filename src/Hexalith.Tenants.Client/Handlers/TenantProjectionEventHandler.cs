using System.Collections.Concurrent;

using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;

namespace Hexalith.Tenants.Client.Handlers;

/// <summary>
/// Built-in domain consumer handler that applies tenant events to <see cref="TenantLocalState"/>
/// projections. Implements the platform <see cref="IEventStoreDomainEventHandler{TEvent}"/> (A3) for all
/// tenant event types; the generic subscription/dedup plumbing is provided by the EventStore client SDK.
/// </summary>
public class TenantProjectionEventHandler :
    IEventStoreDomainEventHandler<TenantCreated>,
    IEventStoreDomainEventHandler<TenantUpdated>,
    IEventStoreDomainEventHandler<TenantDisabled>,
    IEventStoreDomainEventHandler<TenantEnabled>,
    IEventStoreDomainEventHandler<UserAddedToTenant>,
    IEventStoreDomainEventHandler<UserRemovedFromTenant>,
    IEventStoreDomainEventHandler<UserRoleChanged>,
    IEventStoreDomainEventHandler<TenantConfigurationSet>,
    IEventStoreDomainEventHandler<TenantConfigurationRemoved> {
    private readonly ITenantProjectionStore _store;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _tenantLocks = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantProjectionEventHandler"/> class.
    /// </summary>
    /// <param name="store">The tenant projection store.</param>
    public TenantProjectionEventHandler(ITenantProjectionStore store) {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    /// <inheritdoc/>
    public Task HandleAsync(TenantCreated @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(
            @event,
            context,
            static (state, tenantCreated) => {
                state.Name = tenantCreated.Name;
                state.Description = tenantCreated.Description;
                state.Status = TenantStatus.Active;
            },
            cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(TenantUpdated @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(
            @event,
            context,
            static (state, tenantUpdated) => {
                state.Name = tenantUpdated.Name;
                state.Description = tenantUpdated.Description;
            },
            cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(TenantDisabled @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(
            @event,
            context,
            static (state, _) => state.Status = TenantStatus.Disabled,
            cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(TenantEnabled @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(
            @event,
            context,
            static (state, _) => state.Status = TenantStatus.Active,
            cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(UserAddedToTenant @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(
            @event,
            context,
            static (state, userAdded) => state.Members[userAdded.UserId] = userAdded.Role,
            cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(UserRemovedFromTenant @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(
            @event,
            context,
            static (state, userRemoved) => _ = state.Members.Remove(userRemoved.UserId),
            cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(UserRoleChanged @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(
            @event,
            context,
            static (state, roleChanged) => state.Members[roleChanged.UserId] = roleChanged.NewRole,
            cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(TenantConfigurationSet @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(
            @event,
            context,
            static (state, configurationSet) => state.Configuration[configurationSet.Key] = configurationSet.Value,
            cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(TenantConfigurationRemoved @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(
            @event,
            context,
            static (state, configurationRemoved) => _ = state.Configuration.Remove(configurationRemoved.Key),
            cancellationToken);

    private async Task ApplyAsync<TEvent>(
        TEvent @event,
        EventStoreDomainEventContext context,
        Action<TenantLocalState, TEvent> apply,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(apply);

        SemaphoreSlim tenantLock = _tenantLocks.GetOrAdd(context.AggregateId, static _ => new SemaphoreSlim(1, 1));
        await tenantLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            TenantLocalState state = await GetOrCreateStateAsync(context.AggregateId, cancellationToken).ConfigureAwait(false);
            if (state.LastEvent is { } lastEvent && lastEvent.LastSequenceNumber > context.SequenceNumber) {
                return;
            }

            apply(state, @event);
            state.LastEvent = new TenantProjectionEventMetadata(
                context.MessageId,
                context.SequenceNumber,
                context.Timestamp,
                context.CorrelationId);
            await _store.SaveAsync(state, cancellationToken).ConfigureAwait(false);
        }
        finally {
            _ = tenantLock.Release();
        }
    }

    private async Task<TenantLocalState> GetOrCreateStateAsync(string tenantId, CancellationToken cancellationToken) {
        TenantLocalState? state = await _store.GetAsync(tenantId, cancellationToken).ConfigureAwait(false);
        state ??= new TenantLocalState { TenantId = tenantId };

        return state;
    }
}
