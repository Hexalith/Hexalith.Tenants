namespace Hexalith.Tenants.UI.State.TenantList;

/// <summary>Holds protected search and fallback paging only inside one server-side circuit scope.</summary>
internal sealed class TenantSearchPagingState {
    /// <summary>
    /// Maximum retained back-steps per paging mode. History previously grew one protected cursor per Next
    /// for the lifetime of the circuit, with nothing to cap it and a diagnostic that deliberately hides the
    /// count, so the growth was not observable from support output either. Past this depth the oldest
    /// back-step is dropped: Previous stops short rather than the circuit accumulating without bound.
    /// </summary>
    internal const int MaximumRetainedHistoryDepth = 200;

    private readonly List<string?> _searchHistory = [];
    private readonly List<string?> _fallbackHistory = [];
    private string? _scope;

    /// <summary>Gets the current protected authoritative-search cursor.</summary>
    public string? SearchCursor { get; private set; }

    /// <summary>Gets the current ordinary-list fallback cursor.</summary>
    public string? FallbackCursor { get; private set; }

    /// <summary>
    /// Gets the paging mode the retained cursors currently describe: <see langword="true"/> for
    /// authoritative whole-set search, <see langword="false"/> for the ordinary-list fallback, and
    /// <see langword="null"/> when no load has resolved yet. Paging identity lives here, beside the paging
    /// position it describes, because the workspace component is recreated by a tenant-detail return while
    /// this circuit-scoped service survives. A mode kept on the component would be lost exactly when the
    /// cursors are not, leaving the authoritative/fallback crossing undetectable and letting a retained
    /// protected cursor resume a deep page.
    /// </summary>
    public bool? ActiveModeAuthoritative { get; private set; }

    /// <summary>
    /// Gets the protected search scope whose retained paging could not be validated, so an honest page-one
    /// recovery notice is still owed for that exact scope. It lives here, beside the cursors it governs,
    /// because a paging decision that outlives a single load must never sit in a component field that a
    /// tenant-detail return discards.
    /// </summary>
    public string? PendingRecoveryScope { get; private set; }

    /// <summary>Records the paging mode that produced the retained cursors.</summary>
    public void SetActiveMode(bool authoritative) => ActiveModeAuthoritative = authoritative;

    /// <summary>Records that a page-one recovery notice is owed for the given protected search scope.</summary>
    public void SetPendingRecoveryScope(string? scope) => PendingRecoveryScope = scope;

    /// <summary>Drops an owed page-one recovery notice.</summary>
    public void ClearPendingRecoveryScope() => PendingRecoveryScope = null;

    /// <summary>Forgets the active paging mode without discarding the retained query identity.</summary>
    public void ClearActiveMode() => ActiveModeAuthoritative = null;

    /// <summary>Gets whether the retained state belongs to the exact protected search scope.</summary>
    public bool MatchesScope(string? scope)
        => string.Equals(_scope, scope, StringComparison.Ordinal);

    /// <summary>Resets paging when the exact search identity changes.</summary>
    public void EnsureScope(string? scope) {
        if (string.Equals(_scope, scope, StringComparison.Ordinal)) {
            return;
        }

        _scope = scope;
        ResetPositions();
    }

    /// <summary>Gets whether a previous page exists for the active paging mode.</summary>
    public bool HasPrevious(bool authoritative)
        => authoritative ? _searchHistory.Count > 0 : _fallbackHistory.Count > 0;

    /// <summary>
    /// Moves to the next page in the active paging mode. Advancing without a next cursor is refused:
    /// recording a back-step and then setting the position to null made Next reload page one while
    /// simultaneously enabling a Previous that also loaded page one, stranding the operator on a page that
    /// reported more results with no way forward and no notice.
    /// </summary>
    /// <param name="authoritative">Whether the authoritative-search mode is advancing.</param>
    /// <param name="cursor">The protected cursor for the next page.</param>
    /// <returns><see langword="true"/> when the position advanced.</returns>
    public bool MoveNext(bool authoritative, string? cursor) {
        if (cursor is null) {
            return false;
        }

        List<string?> history = authoritative ? _searchHistory : _fallbackHistory;
        history.Add(authoritative ? SearchCursor : FallbackCursor);
        if (history.Count > MaximumRetainedHistoryDepth) {
            history.RemoveAt(0);
        }

        if (authoritative) {
            SearchCursor = cursor;
        }
        else {
            FallbackCursor = cursor;
        }

        return true;
    }

    /// <summary>Moves to the previous page in the active paging mode.</summary>
    public bool TryMovePrevious(bool authoritative) {
        List<string?> history = authoritative ? _searchHistory : _fallbackHistory;
        if (history.Count == 0) {
            return false;
        }

        string? previous = history[^1];
        history.RemoveAt(history.Count - 1);
        if (authoritative) {
            SearchCursor = previous;
        }
        else {
            FallbackCursor = previous;
        }

        return true;
    }

    /// <summary>Clears invalidated authoritative search paging while retaining its query identity.</summary>
    public void RecoverSearch() {
        _searchHistory.Clear();
        SearchCursor = null;
    }

    /// <summary>Clears invalidated ordinary-list fallback paging while retaining its search identity.</summary>
    public void RecoverFallback() {
        _fallbackHistory.Clear();
        FallbackCursor = null;
    }

    /// <summary>Clears all server-held paging state.</summary>
    public void Reset() {
        _scope = null;
        ResetPositions();
    }

    /// <summary>
    /// Returns support-safe paging diagnostics. Scope values, cursor values, and exact page depth are
    /// omitted because page depth reconstructs the protected raw offset.
    /// </summary>
    public override string ToString()
        => $"{nameof(TenantSearchPagingState)} {{ HasScope = {_scope is not null}, HasSearchCursor = {SearchCursor is not null}, HasFallbackCursor = {FallbackCursor is not null}, HasSearchHistory = {_searchHistory.Count > 0}, HasFallbackHistory = {_fallbackHistory.Count > 0}, ActiveModeAuthoritative = {ActiveModeAuthoritative?.ToString() ?? "none"}, HasPendingRecoveryScope = {PendingRecoveryScope is not null} }}";

    private void ResetPositions() {
        _searchHistory.Clear();
        _fallbackHistory.Clear();
        SearchCursor = null;
        FallbackCursor = null;
        ActiveModeAuthoritative = null;
        PendingRecoveryScope = null;
    }
}
