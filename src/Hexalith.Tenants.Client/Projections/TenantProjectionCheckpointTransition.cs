#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
using System.Globalization;
using System.Reflection;
using System.Text.Json;

using Hexalith.EventStore.Client.Projections.Checkpointed;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Serialization;
using Hexalith.Tenants.Contracts.Events;

namespace Hexalith.Tenants.Client.Projections;

/// <summary>
/// Adapts <see cref="TenantProjectionTransition"/> to the EventStore checkpointed projection substrate,
/// decoding authoritative history payloads that recovery replays.
/// </summary>
/// <remarks>
/// Until <see cref="TenantCreated"/> is applied, a supported non-creating event only advances the checkpoint.
/// Unsupported or invalid events fail before advancing the checkpoint.
/// </remarks>
internal sealed class TenantProjectionCheckpointTransition : ICheckpointedProjectionTransition<TenantLocalState> {
    private static readonly IReadOnlyDictionary<string, (Type Type, PropertyInfo TenantIdProperty, int DeclaredVersion)> s_historyTypes =
        TenantProjectionTransition.ProjectedEventTypes
            .Concat(TenantProjectionTransition.RejectionEventTypes)
            .ToDictionary(
                static type => type.FullName!,
                static type => (type, ResolveTenantIdProperty(type), EventPayloadVersionResolver.GetDeclaredVersion(type)),
                StringComparer.Ordinal);

    /// <summary>Gets the shared stateless instance.</summary>
    public static TenantProjectionCheckpointTransition Instance { get; } = new();

    /// <inheritdoc/>
    public TenantLocalState? Apply(TenantLocalState? current, CheckpointedProjectionEvent @event) {
        ArgumentNullException.ThrowIfNull(@event);
        if (!s_historyTypes.TryGetValue(@event.Evidence.EventType, out (Type Type, PropertyInfo TenantIdProperty, int DeclaredVersion) contract)) {
            throw new NotSupportedException(
                $"The authoritative tenant event '{@event.Evidence.EventType}' at sequence {@event.Sequence} is unsupported by this consumer.");
        }

        IEventPayload payload;
        if (@event.DecodedEvent is not null) {
            if (@event.DecodedEvent is not IEventPayload decoded || decoded.GetType() != contract.Type) {
                throw new NotSupportedException(
                    $"Delivered tenant event '{@event.DecodedEvent.GetType().FullName}' does not match supported contract '{@event.Evidence.EventType}' at sequence {@event.Sequence}.");
            }

            payload = decoded;
        }
        else {
            payload = Decode(@event, contract.Type, contract.DeclaredVersion);
        }

        ValidateTenantIdentity(payload, contract.Type, contract.TenantIdProperty, @event);

        if (current is null && payload is not TenantCreated) {
            return null;
        }

        var metadata = new TenantProjectionEventMetadata(
            @event.Evidence.MessageId,
            @event.Sequence,
            @event.Timestamp,
            @event.CorrelationId ?? string.Empty);
        return TenantProjectionTransition.Apply(current, @event.Stream.AggregateId, payload, metadata);
    }

    private static IEventPayload Decode(CheckpointedProjectionEvent @event, Type type, int declaredVersion) {
        int payloadVersion = @event.PayloadVersion ?? 1;
        if (@event.Payload is null
            || !string.Equals(@event.SerializationFormat, "json", StringComparison.OrdinalIgnoreCase)
            || payloadVersion != declaredVersion) {
            string format = @event.SerializationFormat ?? "<null>";
            string version = payloadVersion.ToString(CultureInfo.InvariantCulture);
            throw new NotSupportedException(
                $"Authoritative tenant event {type.Name} (format '{format}', version {version}) at sequence {@event.Sequence} cannot be decoded by this consumer.");
        }

        return JsonSerializer.Deserialize(@event.Payload, type, EventStorePayloadSerialization.Options) as IEventPayload
            ?? throw new NotSupportedException($"Authoritative tenant event {type.Name} at sequence {@event.Sequence} decoded to no payload.");
    }

    private static void ValidateTenantIdentity(
        IEventPayload payload,
        Type type,
        PropertyInfo tenantIdProperty,
        CheckpointedProjectionEvent @event) {
        var payloadTenantId = tenantIdProperty.GetValue(payload) as string;
        if (string.IsNullOrWhiteSpace(payloadTenantId)
            || !string.Equals(payloadTenantId, @event.Stream.AggregateId, StringComparison.Ordinal)) {
            throw new InvalidDataException($"Authoritative tenant event {type.Name} at sequence {@event.Sequence} has an invalid tenant identity.");
        }
    }

    private static PropertyInfo ResolveTenantIdProperty(Type type) {
        PropertyInfo? property = type.GetProperty(nameof(TenantCreated.TenantId), BindingFlags.Instance | BindingFlags.Public);
        return property is { CanRead: true }
            && property.PropertyType == typeof(string)
            && property.GetIndexParameters().Length == 0
                ? property
                : throw new InvalidOperationException(
                    $"Registered tenant event contract '{type.FullName}' must expose a readable public string '{nameof(TenantCreated.TenantId)}' property.");
    }
}
#endif
