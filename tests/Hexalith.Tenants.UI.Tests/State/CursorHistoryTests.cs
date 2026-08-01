using Hexalith.Tenants.UI.State;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class CursorHistoryTests
{
    [Fact]
    public void Trim_below_the_bound_changes_nothing()
    {
        Stack<string?> history = Build(null, "c1", "c2");

        CursorHistory.Trim(history, maximum: 50).ShouldBeFalse();

        history.ToArray().ShouldBe(["c2", "c1", null]);
    }

    /// <summary>
    /// The drop must be reported, because it changes what the next Previous click means.
    /// </summary>
    /// <remarks>
    /// Re-appending the sentinel beneath the newest entries is what keeps page one reachable, but it also
    /// means one later Previous click walks the operator from the middle of the sequence straight to page
    /// one. Callers announce that jump; a <see langword="void"/> trim gave them nothing to announce it from,
    /// so the pagers rendered it as an ordinary one-page step back.
    /// </remarks>
    [Fact]
    public void Trim_reports_whether_it_dropped_entries()
    {
        Stack<string?> atBound = Build([null, .. Enumerable.Range(1, 49).Select(index => $"c{index}")]);
        CursorHistory.Trim(atBound, maximum: 50).ShouldBeFalse();

        Stack<string?> overBound = Build([null, .. Enumerable.Range(1, 50).Select(index => $"c{index}")]);
        CursorHistory.Trim(overBound, maximum: 50).ShouldBeTrue();
        overBound.ToArray()[^1].ShouldBeNull();
    }

    [Fact]
    public void Trim_at_the_bound_changes_nothing()
    {
        Stack<string?> history = Build([null, .. Enumerable.Range(1, 49).Select(index => $"c{index}")]);

        CursorHistory.Trim(history, maximum: 50);

        history.Count.ShouldBe(50);
        history.ToArray()[^1].ShouldBeNull();
    }

    [Fact]
    public void Trim_preserves_the_first_page_sentinel_when_dropping_the_oldest_entries()
    {
        // The whole point of the rule. Without the sentinel re-append, Previous can only walk back to page
        // two and these pagers offer no First or Reset control, so page one becomes unreachable.
        Stack<string?> history = Build([null, .. Enumerable.Range(1, 60).Select(index => $"c{index}")]);

        CursorHistory.Trim(history, maximum: 50);

        string?[] retained = [.. history];
        retained.Length.ShouldBe(50);
        retained[^1].ShouldBeNull();

        // The newest 49 entries survive, oldest non-sentinel entries are the ones dropped.
        retained[0].ShouldBe("c60");
        retained[48].ShouldBe("c12");
        retained.ShouldNotContain("c11");
    }

    [Fact]
    public void Trim_keeps_the_sentinel_reachable_across_repeated_trims()
    {
        Stack<string?> history = Build([null]);
        for (int page = 1; page <= 200; page++)
        {
            history.Push($"c{page}");
            CursorHistory.Trim(history, maximum: 50);
        }

        history.Count.ShouldBe(50);
        history.ToArray()[^1].ShouldBeNull();
    }

    [Fact]
    public void Trim_rejects_a_bound_that_cannot_hold_both_a_page_and_the_sentinel()
        => Should.Throw<ArgumentOutOfRangeException>(() => CursorHistory.Trim(Build([null]), maximum: 1));

    [Fact]
    public void Trim_rejects_a_null_history()
        => Should.Throw<ArgumentNullException>(() => CursorHistory.Trim(null!));

    private static Stack<string?> Build(params string?[] oldestFirst)
    {
        Stack<string?> history = new();
        foreach (string? entry in oldestFirst)
        {
            history.Push(entry);
        }

        return history;
    }
}
