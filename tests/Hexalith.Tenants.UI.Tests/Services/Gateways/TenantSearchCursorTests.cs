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

        request.ToString().ShouldNotContain(ordinaryCursor, Case.Sensitive);
        request.ToString().ShouldNotContain(searchCursor, Case.Sensitive);
        request.ToString().ShouldNotContain(search, Case.Sensitive);
        request.ToString().ShouldNotContain("etag-secret", Case.Sensitive);
        snapshot.ToString().ShouldNotContain(searchCursor, Case.Sensitive);
        snapshot.ToString().ShouldNotContain("etag-secret", Case.Sensitive);
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

    private static string Scope(
        string user,
        string search,
        string? status = null,
        string sort = TenantListSortColumns.TenantId,
        bool descending = false,
        int pageSize = 20)
        => TenantSearchCursorScopes.Create(user, search, status, sort, descending, pageSize);
}
