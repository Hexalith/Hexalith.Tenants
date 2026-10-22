namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Owns cancellation-source disposal for one scoped tenant proof read.</summary>
/// <param name="generation">Scope generation captured when the read began.</param>
/// <param name="callerScope">Authenticated caller scope captured by the read.</param>
/// <param name="cancellation">Cancellation source owned by this read.</param>
/// <param name="complete">Removes and disposes the source after the read finishes.</param>
internal sealed class TenantProofOperation(
    long generation,
    string? callerScope,
    CancellationTokenSource cancellation,
    Action complete) : IDisposable
{
    private Action? _complete = complete;

    /// <summary>Gets the proof scope generation captured by the read.</summary>
    internal long Generation { get; } = generation;

    /// <summary>Gets the authenticated caller scope captured when the read began.</summary>
    internal string? CallerScope { get; } = callerScope;

    /// <summary>Gets cancellation for this read without exposing disposal ownership.</summary>
    internal CancellationToken CancellationToken { get; } = cancellation.Token;

    /// <summary>Completes this read at most once.</summary>
    public void Dispose()
        => Interlocked.Exchange(ref _complete, null)?.Invoke();
}
