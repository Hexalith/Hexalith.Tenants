using System.Globalization;
using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Contracts.Projections;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;

using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.Tenants.Projections;

/// <summary>
/// Handles tenant projection requests using the full event history supplied by EventStore.
/// </summary>
/// <remarks>
/// Persists three read models — the per-aggregate <see cref="TenantReadModel"/>, the per-aggregate
/// <see cref="TenantAuditReadModel"/>, and the cross-aggregate singleton <see cref="TenantIndexReadModel"/> —
/// through the platform <see cref="IReadModelStore"/> + <see cref="ReadModelWritePolicy"/>
/// (optimistic-concurrency, reload-and-merge). The per-aggregate rebuild from a fresh state is correct under
/// the full-replay projection contract; the singleton-index read-and-merge path is independent.
/// </remarks>
public sealed class TenantProjectionHandler {
    private const string StateStoreName = "statestore";
    private const string TenantAuditKeyCategory = "tenant audit";
    private const string TenantIndexKeyCategory = "tenant index";
    private const string TenantIndexProjectionKey = "projection:tenant-index:singleton";
    private const string TenantProjectionKeyCategory = "tenant read-model";
    private const string TenantProjectionKeyPrefix = "projection:tenants:";
    private const string TenantProjectionVersionPrefix = TenantProjectionVersionFormat.SequencePrefix;

    private static readonly EventId NullProjectionEventSkippedEvent = new(2001, "TenantProjectionNullEventSkipped");

    private static readonly JsonSerializerOptions s_options = new() {
        PropertyNameCaseInsensitive = true,
    };

    private readonly ILogger<TenantProjectionHandler> _logger;
    private readonly IReadModelStore _store;
    private readonly TimeProvider _timeProvider;

    public TenantProjectionHandler(IReadModelStore store)
        : this(store, NullLogger<TenantProjectionHandler>.Instance) {
    }

    public TenantProjectionHandler(IReadModelStore store, ILogger<TenantProjectionHandler> logger)
        : this(store, logger, TimeProvider.System) {
    }

    public TenantProjectionHandler(IReadModelStore store, ILogger<TenantProjectionHandler> logger, TimeProvider timeProvider) {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _store = store;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<ProjectionResponse> ProjectAsync(ProjectionRequest request, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AggregateId);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<ProjectionEventDto?> events = (request.Events ?? [])
            .OrderBy(evt => evt?.SequenceNumber ?? long.MaxValue)
            .ToArray();
        if (!events.Any(e => e is not null)) {
            return new ProjectionResponse(
                "tenants",
                JsonSerializer.SerializeToElement(new TenantReadModel()));
        }

        // Build (and validate) the incoming audit model first so a missing MessageId/UserId invariant
        // violation aborts the whole batch before any state-store write commits — extending the spirit of
        // AC12 to the tenant read-model and singleton-index writes too.
        TenantAuditReadModel incomingAuditModel = TenantAuditProjection.ProjectAuditEvents(events.OfType<ProjectionEventDto>());
        TenantAuditStreams.ValidateIncoming(incomingAuditModel, request.AggregateId, TenantAuditStreams.Tenant);
        // Refuse unsafe initialization before the first detail/audit write. The shared-key
        // transform repeats this check after every reload in case a competing writer changed it.
        ReadModelEntry<TenantIndexReadModel> initialIndex = await _store.GetAsync<TenantIndexReadModel>(
            StateStoreName, TenantIndexProjectionKey, cancellationToken).ConfigureAwait(false);
        ValidateIndexOwner(initialIndex.Value, events, request.AggregateId);
        DateTimeOffset projectedAt = _timeProvider.GetUtcNow();
        incomingAuditModel.ProjectedAt = projectedAt;
        cancellationToken.ThrowIfCancellationRequested();

        // Logged once here, outside the ReadModelWritePolicy.UpdateAsync transform below: that transform
        // is retried (and MUST be idempotent) on optimistic-concurrency conflict, so logging from inside it
        // would emit this warning once per retry attempt for a single real occurrence.
        foreach (ProjectionEventDto? evt in events) {
            if (evt is null) {
                _logger.LogWarning(
                    NullProjectionEventSkippedEvent,
                    "Skipped a null projection event while projecting tenant aggregate. AggregateId={TenantAggregateId}",
                    request.AggregateId);
            }
        }

        TenantReadModel state = await ReadModelWritePolicy
            .UpdateAsync<TenantReadModel>(
                _store,
                StateStoreName,
                TenantProjectionKeyPrefix + request.AggregateId,
                current => {
                    TenantReadModel model = current ?? new TenantReadModel();
                    long? persistedSequence = TryParseAggregateProjectionVersion(
                        model.ProjectionVersion,
                        out long parsedPersistedSequence)
                            ? parsedPersistedSequence
                            : null;
                    long? batchHighWatermark = null;

                    foreach (ProjectionEventDto? evt in events) {
                        if (evt is null) {
                            continue;
                        }

                        if (HasAlreadyAppliedAggregateSequence(
                                persistedSequence,
                                batchHighWatermark,
                                evt.SequenceNumber)) {
                            continue;
                        }

                        ApplyEvent(model, evt);
                        StampProjection(model, projectedAt);
                        StampAggregateProjectionVersion(model, evt.SequenceNumber);
                        if (evt.SequenceNumber >= 0
                            && (batchHighWatermark is null || evt.SequenceNumber > batchHighWatermark.Value)) {
                            batchHighWatermark = evt.SequenceNumber;
                        }
                    }

                    return model;
                },
                new ReadModelWriteContext(TenantProjectionKeyCategory, nameof(TenantReadModel))
                    .WithEventDiagnostics(events),
                _logger,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        _ = await ReadModelWritePolicy
            .MergeAsync(
                _store,
                StateStoreName,
                TenantAuditStreams.GetTenantKey(request.AggregateId),
                incomingAuditModel,
                static () => new TenantAuditReadModel(),
                (persisted, incoming) => TenantAuditStreams.Merge(persisted, incoming,
                    rejectConflicts: request.AggregateId == "system"),
                // MergeAsync does not auto-enrich the context (unlike ApplyEventsAsync), so derive the
                // correlation id from the batch here for parity in conflict/exhaustion diagnostics.
                new ReadModelWriteContext(TenantAuditKeyCategory, nameof(TenantAuditReadModel)).WithEventDiagnostics(events),
                _logger,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        _ = await ReadModelWritePolicy
            .UpdateAsync<TenantIndexReadModel>(
                _store,
                StateStoreName,
                TenantIndexProjectionKey,
                current =>
                {
                    TenantIndexReadModel model = current ?? new TenantIndexReadModel();
                    ValidateIndexOwner(model, events, request.AggregateId);
                    model.AggregateSequences ??= new(StringComparer.Ordinal);
                    long? persistedSequence = model.AggregateSequences.TryGetValue(request.AggregateId, out long sequence)
                        ? sequence : null;
                    long? batchHighWatermark = null;
                    foreach (ProjectionEventDto evt in events.OfType<ProjectionEventDto>())
                    {
                        if (HasAlreadyAppliedAggregateSequence(persistedSequence, batchHighWatermark, evt.SequenceNumber)
                            || evt.SequenceNumber < 0 && persistedSequence is not null)
                        {
                            continue;
                        }

                        ApplyIndexEvent(model, evt);
                        StampProjection(model, projectedAt);
                        if (evt.SequenceNumber >= 0
                            && (batchHighWatermark is null || evt.SequenceNumber > batchHighWatermark))
                        {
                            batchHighWatermark = evt.SequenceNumber;
                        }
                    }

                    if (batchHighWatermark is not null)
                    {
                        model.AggregateSequences[request.AggregateId] = batchHighWatermark.Value;
                    }

                    return model;
                },
                new ReadModelWriteContext(TenantIndexKeyCategory, nameof(TenantIndexReadModel)).WithEventDiagnostics(events),
                _logger,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return new ProjectionResponse(
            "tenants",
            JsonSerializer.SerializeToElement(state));
    }

    private static void ValidateIndexOwner(TenantIndexReadModel? model,
        IReadOnlyCollection<ProjectionEventDto?> events, string aggregateId)
    {
        if (model?.AggregateSequences?.TryGetValue(aggregateId, out long sequence) == true)
        {
            if (sequence <= 0)
            {
                throw new InvalidOperationException("Tenant index watermark is invalid; a coordinated tenant projection rebuild is required.");
            }

            return;
        }

        // Detail freshness and a merely contiguous incoming prefix cannot establish the
        // newest history already reflected by an unwatermarked legacy shared-key slice.
        if ((model?.Tenants?.ContainsKey(aggregateId) ?? false)
            || (model?.UserTenants?.Values.Any(tenants => tenants.ContainsKey(aggregateId)) ?? false))
        {
            throw new InvalidOperationException("Tenant index watermarks are incomplete; a coordinated tenant projection rebuild is required.");
        }

        if (!HasCompleteTenantHistory(events, aggregateId))
        {
            throw new InvalidOperationException("A new tenant index owner requires complete contiguous history from sequence one, with creation before successful tenant changes.");
        }
    }

    private static bool HasCompleteTenantHistory(IReadOnlyCollection<ProjectionEventDto?> events, string aggregateId)
    {
        ProjectionEventDto[] ordered = events.OfType<ProjectionEventDto>().ToArray();
        if (ordered.Length == 0 || ordered[0].SequenceNumber != 1)
        {
            return false;
        }

        long previous = 1;
        bool created = false;
        foreach (ProjectionEventDto evt in ordered)
        {
            if (evt.SequenceNumber != previous && evt.SequenceNumber != previous + 1)
            {
                return false;
            }

            previous = evt.SequenceNumber;
            if (evt.EventTypeName == nameof(TenantCreated) || evt.EventTypeName == typeof(TenantCreated).FullName)
            {
                if (!string.Equals(JsonSerializer.Deserialize<TenantCreated>(evt.Payload, s_options)?.TenantId,
                    aggregateId, StringComparison.Ordinal))
                {
                    return false;
                }

                created = true;
            }
            else if (!created && !IsPreCreationRejection(evt, aggregateId))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsPreCreationRejection(ProjectionEventDto evt, string aggregateId)
    {
        // Rejected commands are persisted events even before creation. A rejection-only
        // prefix establishes an empty owner slice, without manufacturing a tenant row.
        string? tenantId = evt.EventTypeName switch
        {
            nameof(TenantNotFoundRejection) or "Hexalith.Tenants.Contracts.Events.Rejections.TenantNotFoundRejection"
                => JsonSerializer.Deserialize<TenantNotFoundRejection>(evt.Payload, s_options)?.TenantId,
            nameof(InsufficientPermissionsRejection) or "Hexalith.Tenants.Contracts.Events.Rejections.InsufficientPermissionsRejection"
                => JsonSerializer.Deserialize<InsufficientPermissionsRejection>(evt.Payload, s_options)?.TenantId,
            _ => null,
        };
        return string.Equals(tenantId, aggregateId, StringComparison.Ordinal);
    }

    private static void StampProjection(IReadModelFreshness model, DateTimeOffset projectedAt) {
        DateTimeOffset? effectiveProjectedAt = LatestProjectedAt(model.ProjectedAt, projectedAt);
        switch (model) {
            case TenantReadModel tenant:
                tenant.ProjectedAt = effectiveProjectedAt;
                break;
            case TenantIndexReadModel index:
                index.ProjectedAt = effectiveProjectedAt;
                break;
            case TenantAuditReadModel audit:
                audit.ProjectedAt = effectiveProjectedAt;
                break;
        }
    }

    private static DateTimeOffset? LatestProjectedAt(DateTimeOffset? existing, DateTimeOffset? incoming) {
        if (existing is null) {
            return incoming;
        }

        return incoming is null || existing.Value >= incoming.Value
            ? existing
            : incoming;
    }

    private static void StampAggregateProjectionVersion(TenantReadModel model, long sequenceNumber) {
        if (sequenceNumber < 0) {
            return;
        }

        if (TryParseAggregateProjectionVersion(model.ProjectionVersion, out long currentSequence)
            && currentSequence >= sequenceNumber) {
            return;
        }

        model.ProjectionVersion = TenantProjectionVersionPrefix
            + sequenceNumber.ToString(CultureInfo.InvariantCulture);
    }

    private static bool HasAlreadyAppliedAggregateSequence(
        long? persistedSequence,
        long? batchHighWatermark,
        long sequenceNumber)
        => sequenceNumber >= 0
        && (persistedSequence >= sequenceNumber || batchHighWatermark > sequenceNumber);

    private static bool TryParseAggregateProjectionVersion(string? value, out long sequenceNumber) {
        sequenceNumber = 0;
        return value is not null
            && value.StartsWith(TenantProjectionVersionPrefix, StringComparison.Ordinal)
            && long.TryParse(
                value.AsSpan(TenantProjectionVersionPrefix.Length),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out sequenceNumber);
    }

    private static void ApplyEvent(TenantReadModel state, ProjectionEventDto evt) {
        string name = evt.EventTypeName;
        if (string.IsNullOrEmpty(name)) {
            return;
        }

        if ((name == nameof(TenantCreated) || name == typeof(TenantCreated).FullName)) {
            TenantCreated? e = JsonSerializer.Deserialize<TenantCreated>(evt.Payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if ((name == nameof(TenantUpdated) || name == typeof(TenantUpdated).FullName)) {
            TenantUpdated? e = JsonSerializer.Deserialize<TenantUpdated>(evt.Payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if ((name == nameof(TenantDisabled) || name == typeof(TenantDisabled).FullName)) {
            TenantDisabled? e = JsonSerializer.Deserialize<TenantDisabled>(evt.Payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if ((name == nameof(TenantEnabled) || name == typeof(TenantEnabled).FullName)) {
            TenantEnabled? e = JsonSerializer.Deserialize<TenantEnabled>(evt.Payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if ((name == nameof(UserAddedToTenant) || name == typeof(UserAddedToTenant).FullName)) {
            UserAddedToTenant? e = JsonSerializer.Deserialize<UserAddedToTenant>(evt.Payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if ((name == nameof(UserRemovedFromTenant) || name == typeof(UserRemovedFromTenant).FullName)) {
            UserRemovedFromTenant? e = JsonSerializer.Deserialize<UserRemovedFromTenant>(evt.Payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if ((name == nameof(UserRoleChanged) || name == typeof(UserRoleChanged).FullName)) {
            UserRoleChanged? e = JsonSerializer.Deserialize<UserRoleChanged>(evt.Payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if ((name == nameof(TenantConfigurationSet) || name == typeof(TenantConfigurationSet).FullName)) {
            TenantConfigurationSet? e = JsonSerializer.Deserialize<TenantConfigurationSet>(evt.Payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if ((name == nameof(TenantConfigurationRemoved) || name == typeof(TenantConfigurationRemoved).FullName)) {
            TenantConfigurationRemoved? e = JsonSerializer.Deserialize<TenantConfigurationRemoved>(evt.Payload, s_options);
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

        if ((name == nameof(TenantCreated) || name == typeof(TenantCreated).FullName)) {
            TenantCreated? e = JsonSerializer.Deserialize<TenantCreated>(evt.Payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
        else if ((name == nameof(TenantUpdated) || name == typeof(TenantUpdated).FullName)) {
            TenantUpdated? e = JsonSerializer.Deserialize<TenantUpdated>(evt.Payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
        else if ((name == nameof(TenantDisabled) || name == typeof(TenantDisabled).FullName)) {
            TenantDisabled? e = JsonSerializer.Deserialize<TenantDisabled>(evt.Payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
        else if ((name == nameof(TenantEnabled) || name == typeof(TenantEnabled).FullName)) {
            TenantEnabled? e = JsonSerializer.Deserialize<TenantEnabled>(evt.Payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
        else if ((name == nameof(UserAddedToTenant) || name == typeof(UserAddedToTenant).FullName)) {
            UserAddedToTenant? e = JsonSerializer.Deserialize<UserAddedToTenant>(evt.Payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
        else if ((name == nameof(UserRemovedFromTenant) || name == typeof(UserRemovedFromTenant).FullName)) {
            UserRemovedFromTenant? e = JsonSerializer.Deserialize<UserRemovedFromTenant>(evt.Payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
        else if ((name == nameof(UserRoleChanged) || name == typeof(UserRoleChanged).FullName)) {
            UserRoleChanged? e = JsonSerializer.Deserialize<UserRoleChanged>(evt.Payload, s_options);
            if (e is not null) {
                indexModel.Apply(e);
            }
        }
    }
}
