namespace Hexalith.Tenants.UI.Tests;

/// <summary>Advances deadline timers deterministically without increasing test wait bounds.</summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly object _sync = new();
    private readonly List<ManualTimeProviderTimer> _timers = [];
    private long _timestamp;

    /// <inheritdoc />
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    /// <summary>Gets the number of currently scheduled timers.</summary>
    internal int PendingTimerCount
    {
        get
        {
            lock (_sync)
            {
                return _timers.Count(timer => timer.DueTimestamp != long.MaxValue);
            }
        }
    }

    /// <inheritdoc />
    public override long GetTimestamp()
    {
        lock (_sync)
        {
            return _timestamp;
        }
    }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch.AddTicks(GetTimestamp());

    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimeProviderTimer(this, callback, state);
        lock (_sync)
        {
            _timers.Add(timer);
            ChangeTimer(timer, dueTime, period);
        }

        return timer;
    }

    /// <summary>Advances the clock and synchronously invokes the timers that become due.</summary>
    /// <param name="duration">The nonnegative duration to advance.</param>
    internal void Advance(TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
        long target = checked(GetTimestamp() + duration.Ticks);
        while (true)
        {
            ManualTimeProviderTimer[] due;
            lock (_sync)
            {
                long next = _timers.Count == 0 ? long.MaxValue : _timers.Min(timer => timer.DueTimestamp);
                if (next > target)
                {
                    _timestamp = target;
                    return;
                }

                _timestamp = next;
                due = [.. _timers.Where(timer => timer.DueTimestamp == next)];
                foreach (ManualTimeProviderTimer timer in due)
                {
                    timer.DueTimestamp = timer.Period > TimeSpan.Zero
                        ? checked(next + timer.Period.Ticks)
                        : long.MaxValue;
                }
            }

            foreach (ManualTimeProviderTimer timer in due)
            {
                timer.Callback(timer.State);
            }
        }
    }

    /// <summary>Changes a live timer's deadline.</summary>
    /// <param name="timer">The timer.</param>
    /// <param name="dueTime">Its next delay.</param>
    /// <param name="period">Its repeat period.</param>
    /// <returns>Whether the timer remains live.</returns>
    internal bool ChangeTimer(ManualTimeProviderTimer timer, TimeSpan dueTime, TimeSpan period)
    {
        lock (_sync)
        {
            if (!_timers.Contains(timer))
            {
                return false;
            }

            timer.DueTimestamp = dueTime == Timeout.InfiniteTimeSpan
                ? long.MaxValue
                : checked(_timestamp + dueTime.Ticks);
            timer.Period = period;
            return true;
        }
    }

    /// <summary>Removes a disposed timer.</summary>
    /// <param name="timer">The timer being released.</param>
    internal void RemoveTimer(ManualTimeProviderTimer timer)
    {
        lock (_sync)
        {
            _ = _timers.Remove(timer);
        }
    }
}
