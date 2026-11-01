using Hexalith.Tenants.Bootstrap;

using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.Server.Tests.Bootstrap;

/// <summary>Observes the hosted bootstrap's terminal diagnostic without retaining credentials.</summary>
/// <param name="outcome">The diagnostic event identifier observed by the test.</param>
internal sealed class BootstrapCommandOutcomeLogger(TaskCompletionSource<int> outcome) : ILogger<TenantBootstrapHostedService>
{
    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (eventId.Id is 2001 or 2002 or 2003 or 2004) outcome.TrySetResult(eventId.Id);
    }
}
