namespace Hexalith.Tenants.Client.Subscription;

/// <summary>
/// Represents the outcome of processing a tenant event.
/// </summary>
public enum TenantEventProcessingResult
{
    /// <summary>
    /// The event was dispatched to at least one handler.
    /// </summary>
    Processed,

    /// <summary>
    /// The event message ID was already processed or is currently being processed.
    /// </summary>
    Duplicate,

    /// <summary>
    /// The event type is not recognized by the current service and was intentionally skipped.
    /// </summary>
    SkippedUnknownEventType,

    /// <summary>
    /// The event type is known, but no handlers were registered for it.
    /// </summary>
    SkippedNoHandlers,

    /// <summary>
    /// The event payload could not be deserialized into the resolved event type.
    /// </summary>
    FailedInvalidPayload,
}
