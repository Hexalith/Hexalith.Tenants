using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.Sample.Handlers;

/// <summary>
/// Sample handler that logs tenant events. Demonstrates how consuming services
/// can register additional handlers alongside the built-in projection handler.
/// </summary>
public class SampleLoggingEventHandler :
    ITenantEventHandler<UserAddedToTenant>,
    ITenantEventHandler<UserRemovedFromTenant>,
    ITenantEventHandler<TenantDisabled>
{
    private readonly ILogger<SampleLoggingEventHandler> _logger;

    public SampleLoggingEventHandler(ILogger<SampleLoggingEventHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task HandleAsync(UserAddedToTenant @event, TenantEventContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);
        _logger.LogInformation(
            "[Sample] User {UserId} added to tenant {TenantId} with role {Role}",
            @event.UserId, context.TenantId, @event.Role);
        return Task.CompletedTask;
    }

    public Task HandleAsync(UserRemovedFromTenant @event, TenantEventContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);
        _logger.LogWarning(
            "[Sample] User {UserId} REMOVED from tenant {TenantId} — revoking access",
            @event.UserId, context.TenantId);
        return Task.CompletedTask;
    }

    public Task HandleAsync(TenantDisabled @event, TenantEventContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);
        _logger.LogWarning(
            "[Sample] Tenant {TenantId} DISABLED — blocking all operations",
            context.TenantId);
        return Task.CompletedTask;
    }
}
