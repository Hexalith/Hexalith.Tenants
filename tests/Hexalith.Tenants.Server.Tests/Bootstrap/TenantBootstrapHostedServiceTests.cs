using System.Collections.Concurrent;

using Hexalith.EventStore.Server.Pipeline.Commands;
using Hexalith.Tenants.Bootstrap;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Contracts.Events.Rejections;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Bootstrap;

public class TenantBootstrapHostedServiceTests {
    [Fact]
    public async Task StartAsync_with_configured_userId_sends_BootstrapGlobalAdmin_command() {
        // Arrange
        IMediator mediator = Substitute.For<IMediator>();
        _ = mediator.Send(Arg.Any<SubmitCommand>(), Arg.Any<CancellationToken>())
            .Returns(new SubmitCommandResult("test-correlation"));

        ServiceCollection services = new();
        _ = services.AddSingleton(mediator);
        ServiceProvider provider = services.BuildServiceProvider();

        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions {
            BootstrapGlobalAdminUserId = "admin-user-1",
        });

        var service = new TenantBootstrapHostedService(
            scopeFactory,
            options,
            NullLogger<TenantBootstrapHostedService>.Instance);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        _ = await mediator.Received(1).Send(
            Arg.Is<SubmitCommand>(cmd =>
                cmd != null
                && cmd.Tenant == "system"
                && cmd.Domain == "tenants"
                && cmd.AggregateId == "global-administrators"
                && cmd.CommandType == "BootstrapGlobalAdmin"
                && cmd.UserId == "admin-user-1"),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task StartAsync_with_empty_userId_skips_bootstrap(string? userId) {
        // Arrange
        IMediator mediator = Substitute.For<IMediator>();
        ServiceCollection services = new();
        _ = services.AddSingleton(mediator);
        ServiceProvider provider = services.BuildServiceProvider();

        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions {
            BootstrapGlobalAdminUserId = userId,
        });

        var service = new TenantBootstrapHostedService(
            scopeFactory,
            options,
            NullLogger<TenantBootstrapHostedService>.Instance);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert — no command sent
        _ = await mediator.DidNotReceive().Send(
            Arg.Any<SubmitCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_handles_infrastructure_exception_without_crashing() {
        // Arrange
        IMediator mediator = Substitute.For<IMediator>();
        _ = mediator.Send(Arg.Any<SubmitCommand>(), Arg.Any<CancellationToken>())
            .Returns<SubmitCommandResult>(_ => throw new InvalidOperationException("DAPR sidecar not ready"));

        ServiceCollection services = new();
        _ = services.AddSingleton(mediator);
        ServiceProvider provider = services.BuildServiceProvider();

        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions {
            BootstrapGlobalAdminUserId = "admin-user-1",
        });

        var service = new TenantBootstrapHostedService(
            scopeFactory,
            options,
            NullLogger<TenantBootstrapHostedService>.Instance);

        // Act & Assert — should not throw
        await Should.NotThrowAsync(
            () => service.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_when_bootstrap_already_completed_logs_skip_message() {
        // Arrange — mediator throws DomainCommandRejectedException (real pipeline behavior)
        IMediator mediator = Substitute.For<IMediator>();
        _ = mediator.Send(Arg.Any<SubmitCommand>(), Arg.Any<CancellationToken>())
            .Returns<SubmitCommandResult>(_ => throw new DomainCommandRejectedException(
                "rejected-correlation",
                "system",
                typeof(GlobalAdminAlreadyBootstrappedRejection).FullName!,
                "Global administrator already bootstrapped"));

        ServiceCollection services = new();
        _ = services.AddSingleton(mediator);
        ServiceProvider provider = services.BuildServiceProvider();

        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions {
            BootstrapGlobalAdminUserId = "admin-user-1",
        });

        var logger = new TestLogger<TenantBootstrapHostedService>();
        var service = new TenantBootstrapHostedService(scopeFactory, options, logger);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        logger.Messages.ShouldContain(message => message.Contains("Global administrator already bootstrapped, skipping", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StartAsync_when_cancelled_propagates_OperationCanceledException() {
        // Arrange
        IMediator mediator = Substitute.For<IMediator>();
        _ = mediator.Send(Arg.Any<SubmitCommand>(), Arg.Any<CancellationToken>())
            .Returns<SubmitCommandResult>(_ => throw new OperationCanceledException("Shutdown"));

        ServiceCollection services = new();
        _ = services.AddSingleton(mediator);
        ServiceProvider provider = services.BuildServiceProvider();

        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        IOptions<TenantBootstrapOptions> options = Options.Create(new TenantBootstrapOptions {
            BootstrapGlobalAdminUserId = "admin-user-1",
        });

        var service = new TenantBootstrapHostedService(
            scopeFactory,
            options,
            NullLogger<TenantBootstrapHostedService>.Instance);

        // Act & Assert — should propagate directly, not be swallowed
        _ = await Should.ThrowAsync<OperationCanceledException>(
            () => service.StartAsync(CancellationToken.None));
    }

    private sealed class TestLogger<T> : ILogger<T> {
        private readonly ConcurrentQueue<string> _messages = new();

        public IReadOnlyCollection<string> Messages => _messages.ToArray();

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => _messages.Enqueue(formatter(state, exception));

        private sealed class NullScope : IDisposable {
            public static readonly NullScope Instance = new();

            public void Dispose() {
            }
        }
    }
}
