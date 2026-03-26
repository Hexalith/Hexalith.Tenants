using System.Text.Json;

using Hexalith.EventStore.Server.Pipeline.Commands;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Events.Rejections;

using MediatR;

using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Bootstrap;

public partial class TenantBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<TenantBootstrapOptions> options,
    ILogger<TenantBootstrapHostedService> logger) : IHostedService {
    public async Task StartAsync(CancellationToken cancellationToken) {
        string? userId = options.Value.BootstrapGlobalAdminUserId;

        if (string.IsNullOrWhiteSpace(userId)) {
            Log.BootstrapSkipped(logger);
            return;
        }

        try {
            AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            await using (scope.ConfigureAwait(false)) {
                IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var command = new BootstrapGlobalAdmin(userId);
                byte[] payload = JsonSerializer.SerializeToUtf8Bytes(command);

                var submitCommand = new SubmitCommand(
                    MessageId: Guid.NewGuid().ToString(),
                    Tenant: "system",
                    Domain: "tenants",
                    AggregateId: "global-administrators",
                    CommandType: nameof(BootstrapGlobalAdmin),
                    Payload: payload,
                    CorrelationId: Guid.NewGuid().ToString(),
                    UserId: userId);

                _ = await mediator.Send(submitCommand, cancellationToken).ConfigureAwait(false);
            }

            Log.BootstrapCommandSent(logger, userId);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (DomainCommandRejectedException ex)
            when (ex.RejectionType.EndsWith(nameof(GlobalAdminAlreadyBootstrappedRejection), StringComparison.Ordinal)) {
            Log.BootstrapAlreadyCompleted(logger);
        }
        catch (Exception ex) {
            Log.BootstrapFailed(logger, ex);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static partial class Log {
        [LoggerMessage(
            EventId = 2000,
            Level = LogLevel.Information,
            Message = "Bootstrap skipped: Tenants:BootstrapGlobalAdminUserId is not configured")]
        public static partial void BootstrapSkipped(ILogger logger);

        [LoggerMessage(
            EventId = 2001,
            Level = LogLevel.Information,
            Message = "Bootstrap command sent for global administrator: UserId={UserId}")]
        public static partial void BootstrapCommandSent(ILogger logger, string userId);

        [LoggerMessage(
            EventId = 2003,
            Level = LogLevel.Information,
            Message = "Global administrator already bootstrapped, skipping")]
        public static partial void BootstrapAlreadyCompleted(ILogger logger);

        [LoggerMessage(
            EventId = 2002,
            Level = LogLevel.Warning,
            Message = "Bootstrap failed — the global administrator may not have been created. The service will retry on next restart")]
        public static partial void BootstrapFailed(ILogger logger, Exception ex);
    }
}
