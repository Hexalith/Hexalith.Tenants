#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
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
/// Until <see cref="TenantCreated"/> is applied, no tenant state is materialized: a rejection or any other
/// non-creating event only advances the checkpoint, so a never-created tenant keeps no durable state and reads as
/// unknown.
/// </remarks>
internal sealed class TenantProjectionCheckpointTransition : ICheckpointedProjectionTransition<TenantLocalState> {
    private static readonly IReadOnlyDictionary<string, Type> s_projectedTypes = TenantProjectionTransition.ProjectedEventTypes
        .ToDictionary(static type => type.FullName!, StringComparer.Ordinal);

    /// <summary>Gets the shared stateless instance.</summary>
    public static TenantProjectionCheckpointTransition Instance { get; } = new();

    /// <inheritdoc/>
    public TenantLocalState? Apply(TenantLocalState? current, CheckpointedProjectionEvent @event) {
        ArgumentNullException.ThrowIfNull(@event);
        IEventPayload? payload = @event.DecodedEvent as IEventPayload ?? Decode(@event);
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

    private static IEventPayload? Decode(CheckpointedProjectionEvent @event) {
        if (!s_projectedTypes.TryGetValue(@event.Evidence.EventType, out Type? type)) {
            // Not a projected tenant event (for example a persisted rejection): it only advances the checkpoint.
            return null;
        }

        if (@event.Payload is null
            || !string.Equals(@event.SerializationFormat ?? "json", "json", StringComparison.OrdinalIgnoreCase)
            || @event.PayloadVersion is > 1) {
            throw new NotSupportedException($"Authoritative tenant event {type.Name} at sequence {@event.Sequence} cannot be decoded by this consumer.");
        }

        return JsonSerializer.Deserialize(@event.Payload, type, EventStorePayloadSerialization.Options) as IEventPayload
            ?? throw new NotSupportedException($"Authoritative tenant event {type.Name} at sequence {@event.Sequence} decoded to no payload.");
    }
}
#endif
