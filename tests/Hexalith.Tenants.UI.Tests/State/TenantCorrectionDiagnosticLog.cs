using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.UI.Tests.State;

/// <summary>Captures formatted correction diagnostic messages and disclosure controls.</summary>
internal sealed class TenantCorrectionDiagnosticLog : ILogger
{
    /// <summary>Gets the messages written to the diagnostic sink.</summary>
    internal List<string> Messages { get; } = [];

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
        => Messages.Add(formatter(state, exception));
}
