using Hexalith.Tenants.UI.Components.Pages;

using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>Captures source-generated log state synchronously, before its pooled storage is released.</summary>
internal sealed class WorkspaceReadSafetyLogger(Exception? failure = null) : ILogger<TenantsWorkspace>
{
    /// <summary>Gets the formatted support-facing messages.</summary>
    internal List<string> Messages { get; } = [];

    /// <summary>Gets attached exceptions, which must be absent from safe diagnostics.</summary>
    internal List<Exception?> Exceptions { get; } = [];

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (failure is not null)
        {
            throw failure;
        }
        Messages.Add(formatter(state, exception));
        Exceptions.Add(exception);
    }
}
