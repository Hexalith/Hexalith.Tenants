namespace Hexalith.Tenants.UI.Tests;

/// <summary>One timer owned by the deterministic test clock.</summary>
/// <param name="owner">The clock that schedules it.</param>
/// <param name="callback">The timer callback.</param>
/// <param name="state">The callback state.</param>
internal sealed class ManualTimeProviderTimer(ManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
{
    /// <summary>Gets the timer callback.</summary>
    internal TimerCallback Callback { get; } = callback;

    /// <summary>Gets the callback state.</summary>
    internal object? State { get; } = state;

    /// <summary>Gets or sets the next due timestamp.</summary>
    internal long DueTimestamp { get; set; } = long.MaxValue;

    /// <summary>Gets or sets the repeat period.</summary>
    internal TimeSpan Period { get; set; }

    /// <inheritdoc />
    public bool Change(TimeSpan dueTime, TimeSpan period) => owner.ChangeTimer(this, dueTime, period);

    /// <inheritdoc />
    public void Dispose() => owner.RemoveTimer(this);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
