namespace Hexalith.Tenants.UI.State.TenantList;

/// <summary>Holds protected search and fallback paging only inside one server-side circuit scope.</summary>
internal sealed class TenantSearchPagingState {
    private readonly Stack<string?> _searchHistory = new();
    private readonly Stack<string?> _fallbackHistory = new();
    private string? _scope;

    /// <summary>Gets the current protected authoritative-search cursor.</summary>
    public string? SearchCursor { get; private set; }

    /// <summary>Gets the current ordinary-list fallback cursor.</summary>
    public string? FallbackCursor { get; private set; }

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

    /// <summary>Moves to the next page in the active paging mode.</summary>
    public void MoveNext(bool authoritative, string? cursor) {
        if (authoritative) {
            _searchHistory.Push(SearchCursor);
            SearchCursor = cursor;
            return;
        }

        _fallbackHistory.Push(FallbackCursor);
        FallbackCursor = cursor;
    }

    /// <summary>Moves to the previous page in the active paging mode.</summary>
    public bool TryMovePrevious(bool authoritative) {
        Stack<string?> history = authoritative ? _searchHistory : _fallbackHistory;
        if (history.Count == 0) {
            return false;
        }

        if (authoritative) {
            SearchCursor = history.Pop();
        }
        else {
            FallbackCursor = history.Pop();
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

    /// <summary>Returns support-safe paging diagnostics without scope or cursor material.</summary>
    public override string ToString()
        => $"{nameof(TenantSearchPagingState)} {{ HasScope = {_scope is not null}, HasSearchCursor = {SearchCursor is not null}, HasFallbackCursor = {FallbackCursor is not null}, HasSearchHistory = {_searchHistory.Count > 0}, HasFallbackHistory = {_fallbackHistory.Count > 0} }}";

    private void ResetPositions() {
        _searchHistory.Clear();
        _fallbackHistory.Clear();
        SearchCursor = null;
        FallbackCursor = null;
    }
}
