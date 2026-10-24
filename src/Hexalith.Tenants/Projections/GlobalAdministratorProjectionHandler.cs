using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Contracts.Projections;
using Hexalith.Tenants.Server.Projections;

using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.Tenants.Projections;

/// <summary>
/// Handles global-administrator projection requests, rebuilding the singleton read model
/// from the full event history supplied by EventStore and writing it through the platform
/// <see cref="IReadModelStore"/>.
/// </summary>
/// <remarks>
/// Domain authorization key consumed by the tenant query handlers:
/// <c>projection:global-administrators:singleton</c>. Any other key is bogus for this domain.
/// </remarks>
public sealed class GlobalAdministratorProjectionHandler {
    public const string StateStoreName = "statestore";
    public const string GlobalAdministratorsProjectionKey = "projection:global-administrators:singleton";
    public const string GlobalAdministratorsAggregateId = "global-administrators";
    public const string SystemTenantId = "system";

    private static readonly JsonSerializerOptions s_options = new() {
        PropertyNameCaseInsensitive = true,
    };

    private readonly ILogger<GlobalAdministratorProjectionHandler> _logger;
    private readonly IReadModelStore _store;
    private readonly TimeProvider _timeProvider;

    public GlobalAdministratorProjectionHandler(IReadModelStore store)
        : this(store, TimeProvider.System) {
    }

    public GlobalAdministratorProjectionHandler(IReadModelStore store, TimeProvider timeProvider)
        : this(store, timeProvider, NullLogger<GlobalAdministratorProjectionHandler>.Instance) {
    }

    /// <summary>Initializes the handler with the dispatcher's typed policy diagnostics logger.</summary>
    /// <param name="store">The persisted read-model store.</param>
    /// <param name="timeProvider">The clock used for persisted projection freshness.</param>
    /// <param name="logger">The typed logger for guarded write diagnostics.</param>
    internal GlobalAdministratorProjectionHandler(IReadModelStore store, TimeProvider timeProvider,
        ILogger<GlobalAdministratorProjectionHandler> logger) {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(timeProvider);

        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _store = store;
        _timeProvider = timeProvider;
    }

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
        TenantAuditStreams.ValidateIncoming(auditState, SystemTenantId, TenantAuditStreams.GlobalAdministrators);
        ReadModelEntry<GlobalAdministratorReadModel> initialAuthority = await _store.GetAsync<GlobalAdministratorReadModel>(
            StateStoreName, GlobalAdministratorsProjectionKey, cancellationToken).ConfigureAwait(false);
        ValidateAuthorityVersion(initialAuthority.Value);
        DateTimeOffset projectedAt = _timeProvider.GetUtcNow();
        auditState.ProjectedAt = projectedAt;
        cancellationToken.ThrowIfCancellationRequested();

        GlobalAdministratorReadModel state = new() {
            ProjectedAt = projectedAt,
        };
        foreach (ProjectionEventDto? evt in events.OfType<ProjectionEventDto>().OrderBy(evt => evt.SequenceNumber)) {
            if (evt is null) {
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            ApplyEvent(state, evt);
            if (evt.SequenceNumber >= 0
                && (!TenantAuditStreams.TryGetSequence(state.ProjectionVersion, out long previousSequence)
                    || previousSequence < evt.SequenceNumber))
            {
                state.ProjectionVersion = TenantProjectionVersionFormat.SequencePrefix
                    + evt.SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        bool completeReplay = IsCompleteGlobalReplay(events, auditState);
        state.ProjectionVersion = completeReplay ? state.ProjectionVersion : null;
        auditState.ProjectionVersion = state.ProjectionVersion;
        ValidateAuthorityInitialization(initialAuthority.Value, state, completeReplay);
        cancellationToken.ThrowIfCancellationRequested();
        _ = await ReadModelWritePolicy.MergeAsync(
            _store,
            StateStoreName,
            TenantAuditStreams.GlobalAdministratorsKey,
            auditState,
            static () => new TenantAuditReadModel(),
            static (persisted, incoming) => TenantAuditStreams.Merge(persisted, incoming),
            new ReadModelWriteContext("global-administrator audit", nameof(TenantAuditReadModel)).WithEventDiagnostics(events),
            _logger,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        GlobalAdministratorReadModel incomingState = state;
        state = await ReadModelWritePolicy.UpdateAsync<GlobalAdministratorReadModel>(
            _store,
            StateStoreName,
            GlobalAdministratorsProjectionKey,
            persisted =>
            {
                ValidateAuthorityVersion(persisted);
                ValidateAuthorityInitialization(persisted, incomingState, completeReplay);
                // Compare inside the retried transform: a competing full replay may have
                // committed newer authority since the previous read. Equal versions are
                // already authoritative; unversioned requests cannot replace ordered state.
                if (persisted is not null
                    && TenantAuditStreams.TryGetSequence(persisted.ProjectionVersion, out long persistedSequence)
                    && (!TenantAuditStreams.TryGetSequence(incomingState.ProjectionVersion, out long incomingSequence)
                        || incomingSequence <= persistedSequence))
                {
                    return persisted;
                }

                if (persisted?.ProjectedAt > incomingState.ProjectedAt)
                {
                    incomingState.ProjectedAt = persisted.ProjectedAt;
                }

                return incomingState;
            },
            new ReadModelWriteContext("global-administrator authority", nameof(GlobalAdministratorReadModel)).WithEventDiagnostics(events),
            _logger,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return new ProjectionResponse(
            "global-administrators",
            JsonSerializer.SerializeToElement(state));
    }

    private static void ValidateAuthorityInitialization(GlobalAdministratorReadModel? persisted,
        GlobalAdministratorReadModel incoming, bool completeReplay)
    {
        if (!completeReplay && incoming.Administrators.Count > 0
            && !TenantAuditStreams.TryGetSequence(persisted?.ProjectionVersion, out _))
        {
            throw new InvalidOperationException("Populated global-administrator authority requires a complete canonical replay before initialization.");
        }
    }

    private static void ValidateAuthorityVersion(GlobalAdministratorReadModel? state)
    {
        if (state?.Administrators?.Count > 0
            && !TenantAuditStreams.TryGetSequence(state.ProjectionVersion, out _))
        {
            throw new InvalidOperationException("Populated global-administrator authority has no ordered version; a coordinated projection rebuild is required.");
        }
    }

    internal static bool IsValidGlobalAdministratorIdentity(ProjectionRequest request)
        => string.Equals(request.TenantId, SystemTenantId, StringComparison.Ordinal)
            && string.Equals(request.AggregateId, GlobalAdministratorsAggregateId, StringComparison.Ordinal);

    private static bool IsCompleteGlobalReplay(IReadOnlyCollection<ProjectionEventDto?> events, TenantAuditReadModel audit)
    {
        // A suffix, a gapped history, or a malformed/unknown event cannot establish canonical ordering.
        var sequences = new HashSet<long>();
        var eventBySequence = new Dictionary<long, ProjectionEventDto>();
        var successIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ProjectionEventDto? evt in events)
        {
            if (evt is null || evt.SequenceNumber <= 0 || string.IsNullOrWhiteSpace(evt.MessageId))
            {
                return false;
            }

            string type = evt.EventTypeName;
            bool success = type == typeof(GlobalAdministratorSet).FullName || type == nameof(GlobalAdministratorSet)
                || type == typeof(GlobalAdministratorRemoved).FullName || type == nameof(GlobalAdministratorRemoved);
            string? rejectionName = new[] { typeof(LastGlobalAdministratorRejection), typeof(GlobalAdminAlreadyBootstrappedRejection),
                typeof(GlobalAdministratorAlreadyExistsRejection), typeof(GlobalAdministratorNotFoundRejection), typeof(InsufficientPermissionsRejection) }
                .FirstOrDefault(contract => type == contract.Name || type == contract.FullName)?.Name;
            if (!success && rejectionName is null)
            {
                return false;
            }

            try
            {
                using JsonDocument payload = JsonDocument.Parse(evt.Payload);
                JsonElement root = payload.RootElement;
                if (root.ValueKind != JsonValueKind.Object || !RequiredString(root, "TenantId", out string? tenantId)
                    || tenantId != SystemTenantId
                    || (success || rejectionName is nameof(LastGlobalAdministratorRejection)
                        or nameof(GlobalAdministratorAlreadyExistsRejection) or nameof(GlobalAdministratorNotFoundRejection))
                        && !RequiredString(root, "UserId", out _)
                    || rejectionName == nameof(InsufficientPermissionsRejection)
                        && (!RequiredString(root, "ActorUserId", out _) || !RequiredString(root, "CommandName", out _)
                            || !Property(root, "ActorRole", out JsonElement role) || !ValidRole(role)))
                {
                    return false;
                }
            }
            catch (JsonException)
            {
                return false;
            }

            if (eventBySequence.TryGetValue(evt.SequenceNumber, out ProjectionEventDto? previous)
                && !JsonSerializer.SerializeToUtf8Bytes(previous).AsSpan().SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(evt)))
            {
                return false;
            }
            eventBySequence[evt.SequenceNumber] = evt;
            sequences.Add(evt.SequenceNumber);
            if (success) successIds.Add(evt.MessageId);
        }

        return sequences.Count > 0 && sequences.Max() == sequences.Count
            && successIds.SetEquals(audit.Entries.Select(row => row.EventId));
    }

    private static bool Property(JsonElement root, string name, out JsonElement value)
        => root.TryGetProperty(name, out value) || root.TryGetProperty(char.ToLowerInvariant(name[0]) + name[1..], out value);

    private static bool RequiredString(JsonElement root, string name, out string? value)
    {
        value = null;
        if (!Property(root, name, out JsonElement element) || element.ValueKind != JsonValueKind.String) return false;
        value = element.GetString();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool ValidRole(JsonElement role)
        => role.ValueKind == JsonValueKind.Null
            || role.ValueKind == JsonValueKind.String && Enum.TryParse(role.GetString(), out TenantRole parsed) && Enum.IsDefined(parsed)
            || role.ValueKind == JsonValueKind.Number && role.TryGetInt32(out int number) && Enum.IsDefined((TenantRole)number);

    private static void ApplyEvent(GlobalAdministratorReadModel state, ProjectionEventDto evt) {
        string name = evt.EventTypeName;
        if (string.IsNullOrEmpty(name)) {
            return;
        }

        if ((name == nameof(GlobalAdministratorSet) || name == typeof(GlobalAdministratorSet).FullName)) {
            GlobalAdministratorSet? e = JsonSerializer.Deserialize<GlobalAdministratorSet>(evt.Payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
        else if ((name == nameof(GlobalAdministratorRemoved) || name == typeof(GlobalAdministratorRemoved).FullName)) {
            GlobalAdministratorRemoved? e = JsonSerializer.Deserialize<GlobalAdministratorRemoved>(evt.Payload, s_options);
            if (e is not null) {
                state.Apply(e);
            }
        }
    }
}
