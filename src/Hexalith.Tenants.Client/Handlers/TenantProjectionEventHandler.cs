using System.Collections.Concurrent;

using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;

namespace Hexalith.Tenants.Client.Handlers;

/// <summary>
/// Built-in handler that applies tenant events to <see cref="TenantLocalState"/> projections.
/// Implements <see cref="ITenantEventHandler{TEvent}"/> for all tenant event types.
/// </summary>
public class TenantProjectionEventHandler :
    ITenantEventHandler<TenantCreated>,
    ITenantEventHandler<TenantUpdated>,
    ITenantEventHandler<TenantDisabled>,
    ITenantEventHandler<TenantEnabled>,
    ITenantEventHandler<UserAddedToTenant>,
    ITenantEventHandler<UserRemovedFromTenant>,
    ITenantEventHandler<UserRoleChanged>,
    ITenantEventHandler<TenantConfigurationSet>,
    ITenantEventHandler<TenantConfigurationRemoved>
{
    private readonly ITenantProjectionStore _store;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _tenantLocks = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantProjectionEventHandler"/> class.
    /// </summary>
    /// <param name="store">The tenant projection store.</param>
    public TenantProjectionEventHandler(ITenantProjectionStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    /// <inheritdoc/>
    public Task HandleAsync(TenantCreated @event, TenantEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(
            @event,
            context,
            static (state, tenantCreated) =>
            {
                state.Name = tenantCreated.Name;
                state.Description = tenantCreated.Description;
                state.Status = TenantStatus.Active;
            },
            cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(TenantUpdated @event, TenantEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(
            @event,
            context,
            static (state, tenantUpdated) =>
            {
                state.Name = tenantUpdated.Name;
                state.Description = tenantUpdated.Description;
            },
            cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(TenantDisabled @event, TenantEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(
            @event,
            context,
            static (state, _) => state.Status = TenantStatus.Disabled,
            cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(TenantEnabled @event, TenantEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(
            @event,
            context,
            static (state, _) => state.Status = TenantStatus.Active,
            cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(UserAddedToTenant @event, TenantEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(
            @event,
            context,
            static (state, userAdded) => state.Members[userAdded.UserId] = userAdded.Role,
            cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(UserRemovedFromTenant @event, TenantEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(
            @event,
            context,
            static (state, userRemoved) => _ = state.Members.Remove(userRemoved.UserId),
            cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(UserRoleChanged @event, TenantEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(
            @event,
            context,
            static (state, roleChanged) => state.Members[roleChanged.UserId] = roleChanged.NewRole,
            cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(TenantConfigurationSet @event, TenantEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(
            @event,
            context,
            static (state, configurationSet) => state.Configuration[configurationSet.Key] = configurationSet.Value,
            cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(TenantConfigurationRemoved @event, TenantEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(
            @event,
            context,
            static (state, configurationRemoved) => _ = state.Configuration.Remove(configurationRemoved.Key),
            cancellationToken);

    private async Task ApplyAsync<TEvent>(
        TEvent @event,
        TenantEventContext context,
        Action<TenantLocalState, TEvent> apply,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(apply);

        SemaphoreSlim tenantLock = _tenantLocks.GetOrAdd(context.TenantId, static _ => new SemaphoreSlim(1, 1));
        await tenantLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            TenantLocalState state = await GetOrCreateStateAsync(context.TenantId, cancellationToken).ConfigureAwait(false);
            apply(state, @event);
            await _store.SaveAsync(state, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            tenantLock.Release();
        }
    }

    private async Task<TenantLocalState> GetOrCreateStateAsync(string tenantId, CancellationToken cancellationToken)
    {
        TenantLocalState? state = await _store.GetAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (state is null)
        {
            state = new TenantLocalState { TenantId = tenantId };
        }

        return state;
    }
}
