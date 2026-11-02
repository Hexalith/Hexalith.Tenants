#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
using System.Security.Cryptography;
using System.Text;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Projections.Checkpointed;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Contracts.Identity;

using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.Client.Projections;

/// <summary>
/// Opt-in durable tenant projection persisted through the EventStore checkpointed projection writer.
/// </summary>
/// <remarks>
/// <para>
/// Each tenant has one read-model record holding its <see cref="TenantLocalState"/>, health, contiguous
/// checkpoint, gap horizon, pending evidence, and recovery lease. Every write is an ETag compare-and-swap owned
/// by EventStore; this type owns only the tenant transition and key schema, so every replica and every restart
/// reads the same persisted decision without redelivery.
/// </para>
/// <para>
/// <see cref="GetAsync"/> returns state only for a healthy record, so legacy consumers fail closed.
/// <see cref="SaveAsync"/> is unsupported: the durable record is never saved unconditionally.
/// </para>
/// </remarks>
public sealed class DurableTenantProjectionStore : ITenantProjectionStore, ITenantProjectionSnapshotStore {
    /// <summary>The versioned key prefix of tenant projection records.</summary>
    public const string KeyPrefix = "tenants-projection:v1:";

    /// <summary>
    /// The marker in front of a hashed tenant segment. It contains <c>:</c>, which a tenant identifier kept verbatim
    /// can never contain, so a verbatim key and a hashed key never collide.
    /// </summary>
    public const string HashedKeyMarker = "sha256:";

    private readonly CheckpointedProjectionWriter<TenantLocalState> _writer;
    private readonly string _storeName;

    /// <summary>
    /// Initializes a new instance of the <see cref="DurableTenantProjectionStore"/> class.
    /// </summary>
    /// <param name="store">The EventStore read-model store.</param>
    /// <param name="options">The state-store binding and checkpoint options.</param>
    /// <param name="gateway">The EventStore gateway used for authoritative range reads; without it, verification and recovery stay unavailable.</param>
    /// <param name="timeProvider">The recovery lease clock.</param>
    /// <param name="logger">The logger.</param>
    public DurableTenantProjectionStore(
        IReadModelStore store,
        DurableTenantProjectionOptions options,
        IEventStoreGatewayClient? gateway = null,
        TimeProvider? timeProvider = null,
        ILogger<DurableTenantProjectionStore>? logger = null)
        : this(
            store,
            options ?? throw new ArgumentNullException(nameof(options)),
            gateway is null ? null : new CheckpointedProjectionHistory(gateway, options.Checkpoint),
            timeProvider,
            logger) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DurableTenantProjectionStore"/> class over an explicit
    /// authoritative history reader.
    /// </summary>
    /// <param name="store">The EventStore read-model store.</param>
    /// <param name="options">The state-store binding and checkpoint options.</param>
    /// <param name="history">The authoritative history reader; without it, verification and recovery stay unavailable.</param>
    /// <param name="timeProvider">The recovery lease clock.</param>
    /// <param name="logger">The logger.</param>
    public DurableTenantProjectionStore(
        IReadModelStore store,
        DurableTenantProjectionOptions options,
        CheckpointedProjectionHistory? history,
        TimeProvider? timeProvider,
        ILogger<DurableTenantProjectionStore>? logger) {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);
        _storeName = options.StateStoreName;
        _writer = new CheckpointedProjectionWriter<TenantLocalState>(
            store,
            TenantProjectionCheckpointTransition.Instance,
            history,
            options.Checkpoint,
            timeProvider,
            logger);
    }

    /// <summary>
    /// Gets the state key of a tenant's projection record.
    /// </summary>
    /// <remarks>
    /// Identifiers of at most 128 ASCII letters, digits, <c>-</c>, <c>_</c> or <c>.</c> are kept verbatim; any other
    /// identifier is replaced by <see cref="HashedKeyMarker"/> and its lowercase SHA-256.
    /// </remarks>
    /// <param name="tenantId">The managed tenant identifier.</param>
    /// <returns>The state key.</returns>
    public static string KeyFor(string tenantId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return tenantId.Length <= 128 && tenantId.All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.')
            ? KeyPrefix + tenantId
            : KeyPrefix + HashedKeyMarker + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(tenantId)));
    }

    /// <inheritdoc/>
    public async Task<TenantProjectionSnapshot?> GetSnapshotAsync(string tenantId, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        CheckpointedProjectionRecord<TenantLocalState>? record;
        try {
            record = await _writer.ReadAsync(_storeName, KeyFor(tenantId), cancellationToken).ConfigureAwait(false);
        }
        catch (CheckpointedProjectionUnavailableException exception)
            when (exception.Reason == CheckpointedProjectionUnavailableReason.MalformedRecord) {
            return new TenantProjectionSnapshot(tenantId, CheckpointedProjectionHealth.Unavailable, null, 0, 0);
        }

        if (record is null) {
            return null;
        }

        if (record.IsHealthy && record.State is null) {
            // The checkpoint advanced only through events that precede creation: the tenant was never created.
            return null;
        }

        bool trustedState = record.State is { } state
            && string.Equals(state.TenantId, tenantId, StringComparison.Ordinal)
            && state.Members is not null
            && state.Configuration is not null
            && state.LastEvent?.LastSequenceNumber == record.LastContiguousSequence;
        if (record.IsHealthy && !trustedState) {
            return new TenantProjectionSnapshot(tenantId, CheckpointedProjectionHealth.Unavailable, null, record.LastContiguousSequence, record.Version);
        }

        // Only a healthy record exposes state; every other health denies and carries no tenant data.
        return new TenantProjectionSnapshot(
            tenantId,
            record.Health,
            record.IsHealthy ? record.State!.Clone() : null,
            record.LastContiguousSequence,
            record.Version);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A delivery from any domain other than <see cref="TenantIdentity.Domain"/>, such as a <c>global-administrators</c>
    /// rejection, never addresses a tenant record: it is acknowledged as <see cref="CheckpointedProjectionOutcome.Duplicate"/>
    /// without reading or writing.
    /// </remarks>
    public async Task<CheckpointedProjectionOutcome> ApplyAsync(
        IEventPayload @event,
        EventStoreDomainEventContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);
        CheckpointedProjectionEvent delivery = CheckpointedProjectionEvent.FromDelivery(@event, context);
        if (!string.Equals(context.Domain, TenantIdentity.Domain, StringComparison.Ordinal)) {
            return CheckpointedProjectionOutcome.Duplicate;
        }

        CheckpointedProjectionResult<TenantLocalState> result = await _writer
            .ApplyAsync(_storeName, KeyFor(context.AggregateId), delivery, cancellationToken)
            .ConfigureAwait(false);
        return result.Outcome;
    }

    /// <inheritdoc/>
    public async Task<TenantLocalState?> GetAsync(string tenantId, CancellationToken cancellationToken = default) {
        TenantProjectionSnapshot? snapshot = await GetSnapshotAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return snapshot is { IsHealthy: true } ? snapshot.State!.Clone() : null;
    }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">Always: the durable projection is written only by checkpointed deliveries.</exception>
    public Task SaveAsync(TenantLocalState state, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("The durable tenant projection is written only through checkpointed deliveries.");
}
#endif
