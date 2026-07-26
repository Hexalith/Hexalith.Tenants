using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantList;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

public sealed class TenantSearchCursorTests {
    [Fact]
    public void Dedicated_codec_round_trips_only_in_the_exact_seven_field_scope() {
        var codec = new TenantSearchCursorCodec(new EphemeralDataProtectionProvider());
        string scope = Scope(user: "user.one", search: "alpha");
        string cursor = codec.Encode(scope, 42);

        codec.TryDecode(cursor, scope, out int offset).ShouldBeTrue();
        offset.ShouldBe(42);

        codec.TryDecode(cursor, Scope(user: "user.two", search: "alpha"), out _).ShouldBeFalse();
        codec.TryDecode(cursor, Scope(user: "user.one", search: "beta"), out _).ShouldBeFalse();
        codec.TryDecode(cursor, Scope(user: "user.one", search: "alpha", status: "Active"), out _).ShouldBeFalse();
        codec.TryDecode(cursor, Scope(user: "user.one", search: "alpha", sort: TenantListSortColumns.Name), out _).ShouldBeFalse();
        codec.TryDecode(cursor, Scope(user: "user.one", search: "alpha", descending: true), out _).ShouldBeFalse();
        codec.TryDecode(cursor, Scope(user: "user.one", search: "alpha", pageSize: 50), out _).ShouldBeFalse();
    }

    [Fact]
    public void Codec_rejects_tampering_key_invalidation_and_noncanonical_positions() {
        string scope = Scope(user: "user.one", search: "alpha");
        var first = new TenantSearchCursorCodec(new EphemeralDataProtectionProvider());
        string cursor = first.Encode(scope, 7);
        string tampered = cursor[..^1] + (cursor[^1] == 'A' ? 'B' : 'A');

        first.TryDecode(tampered, scope, out _).ShouldBeFalse();
        new TenantSearchCursorCodec(new EphemeralDataProtectionProvider())
            .TryDecode(cursor, scope, out _)
            .ShouldBeFalse();
        TenantSearchCursorPosition.TryParse("-1", out _).ShouldBeFalse();
        TenantSearchCursorPosition.TryParse("01", out _).ShouldBeFalse();
        TenantSearchCursorPosition.TryParse("1", out int offset).ShouldBeTrue();
        offset.ShouldBe(1);
    }

    [Fact]
    public void Dedicated_codec_is_not_replaced_by_a_preexisting_unkeyed_platform_codec() {
        var services = new ServiceCollection();
        services.AddDataProtection();
        services.AddSingleton<IQueryCursorCodec>(
            new QueryCursorCodec(new EphemeralDataProtectionProvider(), "host-purpose"));
        services.AddSingleton<ITenantSearchCursorCodec, TenantSearchCursorCodec>();
        using ServiceProvider provider = services.BuildServiceProvider();
        ITenantSearchCursorCodec searchCodec = provider.GetRequiredService<ITenantSearchCursorCodec>();
        IQueryCursorCodec hostCodec = provider.GetRequiredService<IQueryCursorCodec>();
        string scope = Scope(user: "user.one", search: "alpha");
        string cursor = searchCodec.Encode(scope, 3);

        hostCodec.TryDecode(
            cursor,
            TenantSearchCursorPosition.QueryType,
            scope,
            out _,
            out _).ShouldBeFalse();
        searchCodec.TryDecode(cursor, scope, out int offset).ShouldBeTrue();
        offset.ShouldBe(3);
    }

    [Fact]
    public void Scope_hashes_unbounded_user_and_search_values_to_fixed_length_material() {
        string scope = Scope(new string('u', 200_000), new string('q', 200_000));
        string[] segments = scope.Split('|');

        segments.Length.ShouldBe(7);
        segments[0].ShouldStartWith("user:");
        segments[0].Length.ShouldBe("user:".Length + 64);
        segments[2].ShouldStartWith("search:");
        segments[2].Length.ShouldBe("search:".Length + 64);
        scope.ShouldNotContain(new string('u', 100), Case.Sensitive);
        scope.ShouldNotContain(new string('q', 100), Case.Sensitive);
    }

    [Fact]
    public void Request_snapshot_and_paging_diagnostics_never_expose_cursors_or_search_material() {
        const string ordinaryCursor = "ordinary-secret-cursor";
        const string searchCursor = "search-secret-cursor";
        const string search = "private-index-query";
        var request = new TenantListRequest(
            Cursor: ordinaryCursor,
            Search: search,
            ETag: "etag-secret",
            SearchCursor: searchCursor);
        var snapshot = TenantListSnapshot.Ready([], searchCursor, true, "etag-secret", ReadModelFreshnessState.Unknown, false);

        // Pinned exactly rather than substring-scanned: a substring scan over a format that only ever emits
        // enums, bools, and counts can never fail, so it would certify nothing. Any field added to either
        // diagnostic surface has to be reviewed here before it can ship.
        request.ToString().ShouldBe(
            "TenantListRequest { PageSize = 20, HasSearch = True, Status = , SortColumn = tenantId, "
            + "SortDescending = False, HasETag = True }");
        snapshot.ToString().ShouldBe(
            "TenantListSnapshot { Kind = Ready, RowCount = 0, HasMore = True, Freshness = Unknown, "
            + "IsDegraded = False, IsAuthorizationScopedEmpty = False, Reason = None, Notice = None, "
            + "IsAuthoritativeSearch = False, PagingRecovered = False, FallbackPagingRecovered = False, "
            + "PagingNotice = None }");
    }

    [Fact]
    public void Scoped_paging_tracks_authoritative_and_fallback_histories_without_serializing_them() {
        var state = new TenantSearchPagingState();
        state.EnsureScope("scope-one");
        state.MoveNext(authoritative: true, "protected-two");
        state.MoveNext(authoritative: false, "ordinary-two");

        state.HasPrevious(authoritative: true).ShouldBeTrue();
        state.HasPrevious(authoritative: false).ShouldBeTrue();
        state.TryMovePrevious(authoritative: true).ShouldBeTrue();
        state.SearchCursor.ShouldBeNull();
        state.FallbackCursor.ShouldBe("ordinary-two");

        state.EnsureScope("scope-two");
        state.SearchCursor.ShouldBeNull();
        state.FallbackCursor.ShouldBeNull();
        state.HasPrevious(authoritative: true).ShouldBeFalse();
        state.HasPrevious(authoritative: false).ShouldBeFalse();
    }

    [Fact]
    public void Scoped_paging_matches_only_its_exact_server_held_scope_and_recovers_each_mode_independently() {
        var state = new TenantSearchPagingState();
        state.EnsureScope("scope-one");
        state.MoveNext(authoritative: true, "protected-two");
        state.MoveNext(authoritative: false, "ordinary-two");

        state.MatchesScope("scope-one").ShouldBeTrue();
        state.MatchesScope("scope-two").ShouldBeFalse();
        state.MatchesScope(null).ShouldBeFalse();

        state.RecoverFallback();

        state.SearchCursor.ShouldBe("protected-two");
        state.FallbackCursor.ShouldBeNull();
        state.HasPrevious(authoritative: true).ShouldBeTrue();
        state.HasPrevious(authoritative: false).ShouldBeFalse();

        state.RecoverSearch();

        state.SearchCursor.ShouldBeNull();
        state.HasPrevious(authoritative: true).ShouldBeFalse();
        state.MatchesScope("scope-one").ShouldBeTrue();
    }

    [Fact]
    public void Scoped_paging_diagnostics_omit_scope_cursor_and_reconstructable_page_depth() {
        var state = new TenantSearchPagingState();
        state.EnsureScope("scope-one");
        state.MoveNext(authoritative: true, "protected-two");
        state.MoveNext(authoritative: true, "protected-three");
        state.MoveNext(authoritative: false, "ordinary-two");

        // Pinned exactly. Substring scans over this format cannot fail and are therefore not used here.
        state.ToString().ShouldBe(
            "TenantSearchPagingState { HasScope = True, HasSearchCursor = True, HasFallbackCursor = True, "
            + "HasSearchHistory = True, HasFallbackHistory = True, ActiveModeAuthoritative = none, "
            + "HasPendingRecoveryScope = False }");

        state.SetActiveMode(authoritative: true);
        state.SetPendingRecoveryScope("scope-one");
        state.ToString().ShouldBe(
            "TenantSearchPagingState { HasScope = True, HasSearchCursor = True, HasFallbackCursor = True, "
            + "HasSearchHistory = True, HasFallbackHistory = True, ActiveModeAuthoritative = True, "
            + "HasPendingRecoveryScope = True }");
    }

    [Fact]
    public void Scoped_paging_drops_an_owed_recovery_notice_when_the_query_identity_changes() {
        var state = new TenantSearchPagingState();
        state.EnsureScope("scope-one");
        state.SetPendingRecoveryScope("scope-one");
        state.MoveNext(authoritative: true, "protected-two");

        state.PendingRecoveryScope.ShouldBe("scope-one");

        // A query-identity change resets every retained paging decision, the owed recovery notice included,
        // so an outstanding invalidation can never be reported against a different search.
        state.EnsureScope("scope-two");

        state.PendingRecoveryScope.ShouldBeNull();
        state.SearchCursor.ShouldBeNull();
        state.HasPrevious(authoritative: true).ShouldBeFalse();

        state.SetPendingRecoveryScope("scope-two");
        state.ClearPendingRecoveryScope();
        state.PendingRecoveryScope.ShouldBeNull();
    }

    [Fact]
    public void Scoped_paging_keeps_the_active_mode_beside_the_cursors_it_describes() {
        var state = new TenantSearchPagingState();
        state.EnsureScope("scope-one");
        state.ActiveModeAuthoritative.ShouldBeNull();

        state.SetActiveMode(authoritative: true);
        state.MoveNext(authoritative: true, "protected-two");
        state.ActiveModeAuthoritative.ShouldBe(true);

        // The mode survives everything the retained cursors survive, including component recreation, and
        // is discarded exactly when they are.
        state.EnsureScope("scope-one");
        state.ActiveModeAuthoritative.ShouldBe(true);
        state.SearchCursor.ShouldBe("protected-two");

        state.ClearActiveMode();
        state.ActiveModeAuthoritative.ShouldBeNull();
        state.SearchCursor.ShouldBe("protected-two");

        state.SetActiveMode(authoritative: false);
        state.EnsureScope("scope-two");
        state.ActiveModeAuthoritative.ShouldBeNull();
        state.SearchCursor.ShouldBeNull();
    }

    private static string Scope(
        string user,
        string search,
        string? status = null,
        string sort = TenantListSortColumns.TenantId,
        bool descending = false,
        int pageSize = 20)
        => TenantSearchCursorScopes.Create(user, search, status, sort, descending, pageSize);
}
