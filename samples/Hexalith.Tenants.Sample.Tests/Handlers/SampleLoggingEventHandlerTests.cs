using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Sample.Handlers;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Sample.Tests.Handlers;

public class SampleLoggingEventHandlerTests
{
    private static TenantEventContext CreateContext(string tenantId = "acme")
        => new(tenantId, "msg-1", 1, DateTimeOffset.UtcNow, "corr-1");

    [Fact]
    public async Task HandleAsync_UserAddedToTenant_DoesNotThrow()
    {
        // Arrange
        ILogger<SampleLoggingEventHandler> logger = Substitute.For<ILogger<SampleLoggingEventHandler>>();
        var handler = new SampleLoggingEventHandler(logger);
        var @event = new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner);

        // Act & Assert
        await Should.NotThrowAsync(() => handler.HandleAsync(@event, CreateContext()));
    }

    [Fact]
    public async Task HandleAsync_UserRemovedFromTenant_DoesNotThrow()
    {
        // Arrange
        ILogger<SampleLoggingEventHandler> logger = Substitute.For<ILogger<SampleLoggingEventHandler>>();
        var handler = new SampleLoggingEventHandler(logger);
        var @event = new UserRemovedFromTenant("acme", "user1");

        // Act & Assert
        await Should.NotThrowAsync(() => handler.HandleAsync(@event, CreateContext()));
    }

    [Fact]
    public async Task HandleAsync_TenantDisabled_DoesNotThrow()
    {
        // Arrange
        ILogger<SampleLoggingEventHandler> logger = Substitute.For<ILogger<SampleLoggingEventHandler>>();
        var handler = new SampleLoggingEventHandler(logger);
        var @event = new TenantDisabled("acme", DateTimeOffset.UtcNow);

        // Act & Assert
        await Should.NotThrowAsync(() => handler.HandleAsync(@event, CreateContext()));
    }

    [Fact]
    public async Task HandleAsync_UserAddedToTenant_LogsInformation()
    {
        // Arrange
        ILogger<SampleLoggingEventHandler> logger = Substitute.For<ILogger<SampleLoggingEventHandler>>();
        var handler = new SampleLoggingEventHandler(logger);
        var @event = new UserAddedToTenant("acme", "user1", TenantRole.TenantContributor);

        // Act
        await handler.HandleAsync(@event, CreateContext());

        // Assert
        logger.ReceivedWithAnyArgs(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task HandleAsync_UserRemovedFromTenant_LogsWarning()
    {
        // Arrange
        ILogger<SampleLoggingEventHandler> logger = Substitute.For<ILogger<SampleLoggingEventHandler>>();
        var handler = new SampleLoggingEventHandler(logger);
        var @event = new UserRemovedFromTenant("acme", "user1");

        // Act
        await handler.HandleAsync(@event, CreateContext());

        // Assert
        logger.ReceivedWithAnyArgs(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task HandleAsync_TenantDisabled_LogsWarning()
    {
        // Arrange
        ILogger<SampleLoggingEventHandler> logger = Substitute.For<ILogger<SampleLoggingEventHandler>>();
        var handler = new SampleLoggingEventHandler(logger);
        var @event = new TenantDisabled("acme", DateTimeOffset.UtcNow);

        // Act
        await handler.HandleAsync(@event, CreateContext());

        // Assert
        logger.ReceivedWithAnyArgs(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SampleLoggingEventHandler(null!));
    }
}
