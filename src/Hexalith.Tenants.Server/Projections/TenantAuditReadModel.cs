using System.Text.Json;

using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;

namespace Hexalith.Tenants.Server.Projections;

/// <summary>
/// Materialized tenant audit entries built from tenant projection events.
/// </summary>
public sealed class TenantAuditReadModel {
    private static readonly JsonSerializerOptions s_options = new() {
        PropertyNameCaseInsensitive = true,
    };

    public List<TenantAuditEntry> Entries { get; set; } = [];

    public void Apply(ProjectionEventDto evt) {
        ArgumentNullException.ThrowIfNull(evt);
        Entries ??= [];

        if (string.IsNullOrWhiteSpace(evt.MessageId) || string.IsNullOrWhiteSpace(evt.UserId)) {
            throw new InvalidOperationException(
                "Audit projection received an event without MessageId or UserId. The orchestrator additive contract guarantees both fields; this indicates an upstream bug.");
        }

        TenantAuditEntry? entry = CreateEntry(evt);
        if (entry is null) {
            return;
        }

        Entries.Add(entry);
    }

    public void SortEntries() {
        Entries = (Entries ?? [])
            .OrderBy(e => e.Timestamp)
            .ThenBy(e => e.EventId, StringComparer.Ordinal)
            .ToList();
    }

    private static TenantAuditEntry? CreateEntry(ProjectionEventDto evt) {
        string eventType = GetEventType(evt.EventTypeName);

        return eventType switch {
            nameof(UserAddedToTenant) => CreateAccessEntry(evt, Deserialize<UserAddedToTenant>(evt.Payload), eventType, e => new Dictionary<string, string>(StringComparer.Ordinal) {
                ["userId"] = e.UserId,
                ["role"] = e.Role.ToString(),
            }),
            nameof(UserRemovedFromTenant) => CreateAccessEntry(evt, Deserialize<UserRemovedFromTenant>(evt.Payload), eventType, e => new Dictionary<string, string>(StringComparer.Ordinal) {
                ["userId"] = e.UserId,
            }),
            nameof(UserRoleChanged) => CreateAccessEntry(evt, Deserialize<UserRoleChanged>(evt.Payload), eventType, e => new Dictionary<string, string>(StringComparer.Ordinal) {
                ["userId"] = e.UserId,
                ["oldRole"] = e.OldRole.ToString(),
                ["newRole"] = e.NewRole.ToString(),
            }),
            nameof(GlobalAdministratorSet) => CreateAccessEntry(evt, Deserialize<GlobalAdministratorSet>(evt.Payload), eventType, e => new Dictionary<string, string>(StringComparer.Ordinal) {
                ["userId"] = e.UserId,
                ["actorUserId"] = e.ActorUserId,
                ["setAt"] = e.SetAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            }),
            nameof(GlobalAdministratorRemoved) => CreateAccessEntry(evt, Deserialize<GlobalAdministratorRemoved>(evt.Payload), eventType, e => new Dictionary<string, string>(StringComparer.Ordinal) {
                ["userId"] = e.UserId,
                ["actorUserId"] = e.ActorUserId,
                ["removedAt"] = e.RemovedAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            }),
            nameof(TenantCreated) => CreateAdministrativeEntry(evt, Deserialize<TenantCreated>(evt.Payload), eventType, e => new Dictionary<string, string>(StringComparer.Ordinal) {
                ["name"] = e.Name,
                ["createdAt"] = e.CreatedAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            }),
            nameof(TenantUpdated) => CreateAdministrativeEntry(evt, Deserialize<TenantUpdated>(evt.Payload), eventType, e => new Dictionary<string, string>(StringComparer.Ordinal) {
                ["name"] = e.Name,
            }),
            nameof(TenantDisabled) => CreateAdministrativeEntry(evt, Deserialize<TenantDisabled>(evt.Payload), eventType, e => new Dictionary<string, string>(StringComparer.Ordinal) {
                ["disabledAt"] = e.DisabledAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            }),
            nameof(TenantEnabled) => CreateAdministrativeEntry(evt, Deserialize<TenantEnabled>(evt.Payload), eventType, e => new Dictionary<string, string>(StringComparer.Ordinal) {
                ["enabledAt"] = e.EnabledAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            }),
            nameof(TenantConfigurationSet) => CreateAdministrativeEntry(evt, Deserialize<TenantConfigurationSet>(evt.Payload), eventType, e => new Dictionary<string, string>(StringComparer.Ordinal) {
                ["key"] = e.Key,
            }),
            nameof(TenantConfigurationRemoved) => CreateAdministrativeEntry(evt, Deserialize<TenantConfigurationRemoved>(evt.Payload), eventType, e => new Dictionary<string, string>(StringComparer.Ordinal) {
                ["key"] = e.Key,
            }),
            _ => null,
        };
    }

    private static TenantAuditEntry CreateAccessEntry<TEvent>(
        ProjectionEventDto evt,
        TEvent payload,
        string eventType,
        Func<TEvent, Dictionary<string, string>> narrative)
        where TEvent : IEventPayload =>
        CreateEntry(
            evt,
            eventType,
            AuditEventCategory.Access,
            GetTenantId(payload),
            narrative(payload));

    private static TenantAuditEntry CreateAdministrativeEntry<TEvent>(
        ProjectionEventDto evt,
        TEvent payload,
        string eventType,
        Func<TEvent, Dictionary<string, string>> narrative)
        where TEvent : IEventPayload =>
        CreateEntry(
            evt,
            eventType,
            AuditEventCategory.Administrative,
            GetTenantId(payload),
            narrative(payload));

    private static TenantAuditEntry CreateEntry(
        ProjectionEventDto evt,
        string eventType,
        AuditEventCategory category,
        string tenantId,
        IReadOnlyDictionary<string, string> narrative) =>
        new(
            evt.MessageId!,
            eventType,
            category,
            evt.UserId!,
            evt.Timestamp,
            tenantId,
            narrative);

    private static TEvent Deserialize<TEvent>(byte[] payload) where TEvent : IEventPayload =>
        JsonSerializer.Deserialize<TEvent>(payload, s_options)
        ?? throw new InvalidOperationException($"Unable to deserialize audit event payload as {typeof(TEvent).Name}.");

    private static string GetEventType(string eventTypeName) {
        int index = eventTypeName.LastIndexOf('.');
        return index < 0 ? eventTypeName : eventTypeName[(index + 1)..];
    }

    private static string GetTenantId(IEventPayload payload) =>
        payload switch {
            TenantCreated e => e.TenantId,
            TenantUpdated e => e.TenantId,
            TenantDisabled e => e.TenantId,
            TenantEnabled e => e.TenantId,
            UserAddedToTenant e => e.TenantId,
            UserRemovedFromTenant e => e.TenantId,
            UserRoleChanged e => e.TenantId,
            TenantConfigurationSet e => e.TenantId,
            TenantConfigurationRemoved e => e.TenantId,
            GlobalAdministratorSet e => e.TenantId,
            GlobalAdministratorRemoved e => e.TenantId,
            _ => throw new InvalidOperationException(
                $"GetTenantId received an unsupported payload type '{payload.GetType().Name}'. CreateEntry and GetTenantId must stay in lockstep with the classification switch."),
        };
}
