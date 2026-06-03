using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.Tenants.Contracts.Events;

namespace Hexalith.Tenants.Sample.Handlers;

/// <summary>
/// Sample handler that logs tenant events. Demonstrates how consuming services
/// can register additional handlers alongside the built-in projection handler.
/// </summary>
public class SampleLoggingEventHandler :
    IEventStoreDomainEventHandler<UserAddedToTenant>,
    IEventStoreDomainEventHandler<UserRemovedFromTenant>,
    IEventStoreDomainEventHandler<TenantDisabled> {
    private readonly ILogger<SampleLoggingEventHandler> _logger;

    public SampleLoggingEventHandler(ILogger<SampleLoggingEventHandler> logger) {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task HandleAsync(UserAddedToTenant @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);
        _logger.LogInformation(
            "[Sample] UserAddedToTenant processed for tenant {TenantId}; message {MessageId}; correlation {CorrelationId}",
            context.AggregateId, context.MessageId, context.CorrelationId);
        return Task.CompletedTask;
    }

    public Task HandleAsync(UserRemovedFromTenant @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);
        _logger.LogWarning(
            "[Sample] UserRemovedFromTenant processed for tenant {TenantId}; revoking projected access; message {MessageId}; correlation {CorrelationId}",
            context.AggregateId, context.MessageId, context.CorrelationId);
        return Task.CompletedTask;
    }

    public Task HandleAsync(TenantDisabled @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);
        _logger.LogWarning(
            "[Sample] TenantDisabled processed for tenant {TenantId}; blocking projected access; message {MessageId}; correlation {CorrelationId}",
            context.AggregateId, context.MessageId, context.CorrelationId);
        return Task.CompletedTask;
    }
}
