namespace Hexalith.Tenants.Server.Tests.Bootstrap;

/// <summary>Signals when the hosted bootstrap has disposed its real asynchronous scope.</summary>
/// <param name="completion">The completion signal observed by the test.</param>
internal sealed class BootstrapCommandScopeCompletion(TaskCompletionSource completion) : IAsyncDisposable
{
    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        completion.TrySetResult();
        return ValueTask.CompletedTask;
    }
}
