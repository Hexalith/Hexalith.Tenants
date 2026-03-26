using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Tenants.Client.Handlers;

/// <summary>
/// Handles a specific tenant event type in a consuming service.
/// </summary>
/// <typeparam name="TEvent">The event payload type.</typeparam>
public interface ITenantEventHandler<in TEvent>
    where TEvent : IEventPayload {
    /// <summary>
    /// Handles the specified tenant event asynchronously.
    /// </summary>
    /// <param name="event">The event payload.</param>
    /// <param name="context">The event processing context with envelope metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleAsync(TEvent @event, TenantEventContext context, CancellationToken cancellationToken = default);
}
