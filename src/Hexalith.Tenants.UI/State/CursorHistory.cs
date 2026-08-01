namespace Hexalith.Tenants.UI.State;

/// <summary>
/// Bounds the protected paging history a surface retains for its Previous control.
/// </summary>
/// <remarks>
/// Shared by the member, audit, and global-administrator pagers so the first-page rule is stated and tested
/// once. A long paging session in one circuit would otherwise grow each history without limit.
/// </remarks>
internal static class CursorHistory
{
    /// <summary>Default upper bound on retained paging history.</summary>
    public const int DefaultMaximum = 50;

    /// <summary>
    /// Trims <paramref name="history"/> to <paramref name="maximum"/> entries while preserving the oldest
    /// entry, which is the first-page sentinel.
    /// </summary>
    /// <param name="history">The paging history, newest entry on top.</param>
    /// <param name="maximum">The maximum number of retained entries.</param>
    /// <returns><see langword="true"/> when entries were dropped; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>
    /// The oldest entry is the route back to page one. A plain trim drops it, leaving Previous able to walk
    /// back only as far as page two on pagers that offer no First or Reset control -- so the sentinel is
    /// re-appended after the newest <c>maximum - 1</c> entries rather than discarded with them.
    /// </para>
    /// <para>
    /// The return value exists because the re-appended sentinel makes one later Previous click jump the
    /// operator from the middle of the sequence straight to page one. Without a signal the pager renders that
    /// as an ordinary one-page step back, so callers use this to announce the jump.
    /// </para>
    /// </remarks>
    public static bool Trim(Stack<string?> history, int maximum = DefaultMaximum)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximum, 2);

        if (history.Count <= maximum)
        {
            return false;
        }

        string?[] entries = [.. history];
        string? firstPageCursor = entries[^1];
        string?[] retained =
        [
            .. entries.Take(maximum - 1),
            firstPageCursor,
        ];
        history.Clear();
        for (int index = retained.Length - 1; index >= 0; index--)
        {
            history.Push(retained[index]);
        }

        return true;
    }
}
