using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.IntegrationTests.Fixtures;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.UI.Services.Gateways;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Exercises exact corpus credentials using synthetic typed pages without starting a backend.</summary>
public sealed class AuthenticatedRouteCorpusTokenTests
{
    [Fact]
    public async Task CompletePaginationGrantsOnlyDistinctObservedTenantsAndPreservesTheNarrowToken()
    {
        DateTime startedUtc = DateTime.UtcNow;
        AuthenticatedRouteState state = CreateState();
        string originalToken = state.Token;
        var requests = new List<string?>();
        var pages = new Queue<TenantsRestQueryResponse<PaginatedResult<TenantSummary>>>([
            Page([new("older-tenant", "Earlier seed", TenantStatus.Active)], "protected-first", true),
            Page([new("older-tenant", "Earlier seed", TenantStatus.Active), Seed(state)], "protected-second", true),
            Page([new("last-tenant", "Later row", TenantStatus.Disabled), Seed(state)]),
        ]);

        Task<TenantsRestQueryResponse<PaginatedResult<TenantSummary>>> Read(ListTenantsQuery query, CancellationToken token)
        {
            query.PageSize.ShouldBe(100);
            token.ShouldBe(TestContext.Current.CancellationToken);
            requests.Add(query.Cursor);
            return Task.FromResult(pages.Dequeue());
        }

        string corpusToken = await state.CreateCorpusReadTokenAsync(Read, TestContext.Current.CancellationToken);

        requests.ShouldBe([null, "protected-first", "protected-second"]);
        pages.Count.ShouldBe(0);
        string.Equals(state.Token, originalToken, StringComparison.Ordinal).ShouldBeTrue();
        TenantGrants(state.Token).ShouldBe([state.TenantId]);
        string[] grants = TenantGrants(corpusToken);
        grants.Order(StringComparer.Ordinal).ShouldBe(new[] { "last-tenant", "older-tenant", state.TenantId }.Order(StringComparer.Ordinal));
        grants.Distinct(StringComparer.Ordinal).Count().ShouldBe(3);
        grants.Any(static grant => grant is "*" or "system").ShouldBeFalse();
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(corpusToken);
        DateTime completedUtc = DateTime.UtcNow;
        parsed.ValidTo.ShouldBeInRange(startedUtc.AddMinutes(30).AddSeconds(-1), completedUtc.AddMinutes(30));
        parsed.Claims.Where(static claim => claim.Type == "eventstore:permission").Select(static claim => claim.Value)
            .Order(StringComparer.Ordinal).ShouldBe(["global-administrators:read", "tenants:read"]);
        JsonSerializer.Deserialize<string[]>(parsed.Claims.Single(static claim => claim.Type == "permissions").Value)
            .ShouldBe(["query:read"]);
        var original = new JwtSecurityTokenHandler().ReadJwtToken(state.Token);
        original.ValidTo.ShouldBeInRange(startedUtc.AddMinutes(10).AddSeconds(-1), completedUtc.AddMinutes(10));
        DateTime commandIssuedUtc = DateTime.UtcNow;
        var command = new JwtSecurityTokenHandler().ReadJwtToken(AspireTopologyTests.CreateDemoJwt());
        command.ValidTo.ShouldBeInRange(commandIssuedUtc.AddMinutes(10).AddSeconds(-1), DateTime.UtcNow.AddMinutes(10));
        parsed.Claims.Single(static claim => claim.Type == "sub").Value
            .ShouldBe(original.Claims.Single(static claim => claim.Type == "sub").Value);
        original.Claims.ShouldContain(static claim => claim.Type == "eventstore:permission" && claim.Value == "tenants:create");
        JsonSerializer.Deserialize<string[]>(original.Claims.Single(static claim => claim.Type == "permissions").Value)
            .ShouldBe(["command:submit", "query:read"]);
    }

    [Theory]
    [InlineData("missing-metadata")]
    [InlineData("unknown-provenance")]
    [InlineData("unknown-lifecycle")]
    [InlineData("stale-lifecycle")]
    [InlineData("degraded-lifecycle")]
    [InlineData("missing-etag")]
    [InlineData("blank-etag")]
    [InlineData("stale")]
    [InlineData("unknown-staleness")]
    [InlineData("degraded")]
    [InlineData("missing-served-at")]
    [InlineData("old-served-at")]
    [InlineData("future-served-at")]
    public async Task SuccessfulPagesWithUnqualifiedProjectionMetadataRefuseCredentials(string scenario)
    {
        AuthenticatedRouteState state = CreateState();
        var page = Page([Seed(state)]);
        QueryResponseMetadata metadata = scenario switch
        {
            "missing-metadata" => null!,
            "unknown-provenance" => page.Metadata with { Provenance = QueryResponseProvenance.Unknown },
            "unknown-lifecycle" => page.Metadata with { Lifecycle = ProjectionLifecycleState.Unknown },
            "stale-lifecycle" => page.Metadata with { Lifecycle = ProjectionLifecycleState.Stale },
            "degraded-lifecycle" => page.Metadata with { Lifecycle = ProjectionLifecycleState.Degraded },
            "missing-etag" => page.Metadata with { ETag = null },
            "blank-etag" => page.Metadata with { ETag = " " },
            "stale" => page.Metadata with { IsStale = true },
            "unknown-staleness" => page.Metadata with { IsStale = null },
            "degraded" => page.Metadata with { IsDegraded = true },
            "missing-served-at" => page.Metadata with { ServedAt = null },
            "old-served-at" => page.Metadata with { ServedAt = DateTimeOffset.UtcNow.AddMinutes(-6) },
            "future-served-at" => page.Metadata with { ServedAt = DateTimeOffset.UtcNow.AddMinutes(1) },
            _ => throw new InvalidOperationException("Unknown synthetic metadata control."),
        };
        int reads = 0;
        var refusal = await Should.ThrowAsync<InvalidOperationException>(() => state.CreateCorpusReadTokenAsync(
            (_, _) => Task.FromResult(++reads == 1 ? Page([Seed(state)], "protected-next", true) : page with { Metadata = metadata }),
            TestContext.Current.CancellationToken));
        refusal.Message.ShouldBe("Live corpus read credentials require a complete valid index containing the current seed.");
        reads.ShouldBe(2);
        TenantGrants(state.Token).ShouldBe([state.TenantId]);
    }

    [Theory]
    [InlineData("null-response")]
    [InlineData("failed-response")]
    [InlineData("unsuccessful-status")]
    [InlineData("missing-payload")]
    [InlineData("missing-items")]
    [InlineData("missing-item")]
    [InlineData("oversized-page")]
    [InlineData("empty-corpus")]
    [InlineData("empty-continuation")]
    [InlineData("missing-cursor")]
    [InlineData("blank-cursor")]
    [InlineData("repeating-cursor")]
    [InlineData("cyclic-cursors")]
    [InlineData("unexpected-final-cursor")]
    [InlineData("missing-seed")]
    [InlineData("stale-seed-name")]
    [InlineData("stale-seed-status")]
    [InlineData("stale-duplicate-seed")]
    public async Task IncompleteMalformedOrStalePagesRefuseCredentials(string scenario)
    {
        AuthenticatedRouteState state = CreateState();
        TenantSummary seed = Seed(state);
        var pages = new Queue<TenantsRestQueryResponse<PaginatedResult<TenantSummary>>>();
        // Seed is already present on page one: every subsequent page must still be validated.
        pages.Enqueue(Page([seed], "protected-next", true));
        switch (scenario)
        {
            case "null-response": pages.Enqueue(null!); break;
            case "failed-response": pages.Enqueue(Page([seed]) with { FailureKind = TenantsRestQueryFailureKind.Unavailable }); break;
            case "unsuccessful-status": pages.Enqueue(Page([seed]) with { StatusCode = (int)HttpStatusCode.PartialContent }); break;
            case "missing-payload": pages.Enqueue(Page([seed]) with { Payload = null }); break;
            case "missing-items": pages.Enqueue(Page(null!)); break;
            case "missing-item": pages.Enqueue(Page([null!])); break;
            case "oversized-page": pages.Enqueue(Page(Enumerable.Repeat(seed, 101).ToArray())); break;
            case "empty-corpus": pages.Clear(); pages.Enqueue(Page([])); break;
            case "empty-continuation": pages.Enqueue(Page([], "protected-empty", true)); break;
            case "missing-cursor": pages.Enqueue(Page([seed], null, true)); break;
            case "blank-cursor": pages.Enqueue(Page([seed], " ", true)); break;
            case "repeating-cursor": pages.Enqueue(Page([seed], "protected-next", true)); break;
            case "cyclic-cursors":
                pages.Enqueue(Page([seed], "protected-second", true));
                pages.Enqueue(Page([seed], "protected-next", true));
                break;
            case "unexpected-final-cursor": pages.Enqueue(Page([seed], "protected-final")); break;
            case "missing-seed": pages.Clear(); pages.Enqueue(Page([new("older-tenant", "Earlier seed", TenantStatus.Active)])); break;
            case "stale-seed-name": pages.Clear(); pages.Enqueue(Page([seed with { Name = "Old name" }])); break;
            case "stale-seed-status": pages.Clear(); pages.Enqueue(Page([seed with { Status = TenantStatus.Disabled }])); break;
            case "stale-duplicate-seed": pages.Enqueue(Page([seed with { Name = "Old name" }])); break;
            default: throw new InvalidOperationException("Unknown synthetic corpus control.");
        }

        var refusal = await Should.ThrowAsync<InvalidOperationException>(() => state.CreateCorpusReadTokenAsync(
            (_, _) => Task.FromResult(pages.Dequeue()), TestContext.Current.CancellationToken));

        refusal.Message.ShouldBe("Live corpus read credentials require a complete valid index containing the current seed.");
        TenantGrants(state.Token).ShouldBe([state.TenantId]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("tenant id")]
    [InlineData("tenant\nunsafe")]
    [InlineData("tenant/unsafe")]
    [InlineData("tenant\\unsafe")]
    [InlineData("..")]
    [InlineData("*")]
    [InlineData("system")]
    [InlineData("System")]
    [InlineData("platform:tenants")]
    [InlineData("Tenant-alias")]
    [InlineData("tenant_unsafe")]
    [InlineData("-tenant")]
    [InlineData("tenant-")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task UnsafeOrNonexactIdentifiersRefuseTheEntireCorpus(string? tenantId)
    {
        AuthenticatedRouteState state = CreateState();
        var refusal = await Should.ThrowAsync<InvalidOperationException>(() => state.CreateCorpusReadTokenAsync(
            (_, _) => Task.FromResult(Page([Seed(state), new(tenantId!, "Unsafe synthetic row", TenantStatus.Active)])),
            TestContext.Current.CancellationToken));

        refusal.Message.ShouldBe("Live corpus read credentials require a complete valid index containing the current seed.");
        TenantGrants(state.Token).ShouldBe([state.TenantId]);
    }

    [Theory]
    [InlineData("before-enumeration")]
    [InlineData("during-page")]
    [InlineData("between-pages")]
    [InlineData("before-issuance")]
    public async Task CallerCancellationTerminatesEnumerationWithoutIssuingCredentials(string phase)
    {
        AuthenticatedRouteState state = CreateState();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        if (phase == "before-enumeration") cancellation.Cancel();
        int reads = 0;
        Task<TenantsRestQueryResponse<PaginatedResult<TenantSummary>>> Read(ListTenantsQuery query, CancellationToken token)
        {
            token.ShouldBe(cancellation.Token);
            reads++;
            cancellation.Cancel();
            if (phase == "during-page")
            {
                // Even a delegate that ignores cancellation cannot keep enumeration waiting.
                return new TaskCompletionSource<TenantsRestQueryResponse<PaginatedResult<TenantSummary>>>().Task;
            }

            return Task.FromResult(Page([Seed(state)], phase == "between-pages" ? "protected-next" : null,
                phase == "between-pages"));
        }

        var refused = await Should.ThrowAsync<OperationCanceledException>(() => state.CreateCorpusReadTokenAsync(Read, cancellation.Token));
        refused.CancellationToken.ShouldBe(cancellation.Token);
        reads.ShouldBe(phase == "before-enumeration" ? 0 : 1);
        TenantGrants(state.Token).ShouldBe([state.TenantId]);
    }

    [Fact]
    public async Task ExactlyMaxCorpusPagesSucceedsAndExceedingStopsWithoutExtraPageFetch()
    {
        AuthenticatedRouteState state = CreateState();
        int passReads = 0;
        string token = await state.CreateCorpusReadTokenAsync(
            (_, _) =>
            {
                passReads++;
                bool hasMore = passReads < AuthenticatedRouteState.MaxCorpusPages;
                TenantSummary item = passReads == AuthenticatedRouteState.MaxCorpusPages
                    ? Seed(state)
                    : new($"page-tenant-{passReads:D2}", "Bounded page row", TenantStatus.Active);
                return Task.FromResult(Page([item], hasMore ? $"cursor-{passReads}" : null, hasMore));
            },
            TestContext.Current.CancellationToken);

        passReads.ShouldBe(AuthenticatedRouteState.MaxCorpusPages);
        TenantGrants(token).Length.ShouldBe(AuthenticatedRouteState.MaxCorpusPages);

        int overflowReads = 0;
        InvalidOperationException refusal = await Should.ThrowAsync<InvalidOperationException>(() => state.CreateCorpusReadTokenAsync(
            (_, _) =>
            {
                overflowReads++;
                overflowReads.ShouldBeLessThanOrEqualTo(AuthenticatedRouteState.MaxCorpusPages);
                TenantSummary item = overflowReads == 1
                    ? Seed(state)
                    : new($"overflow-page-tenant-{overflowReads:D2}", "Bounded page row", TenantStatus.Active);
                return Task.FromResult(Page([item], $"cursor-overflow-{overflowReads}", hasMore: true));
            },
            TestContext.Current.CancellationToken));

        refusal.Message.ShouldBe("Live corpus read credentials require a complete valid index containing the current seed.");
        overflowReads.ShouldBe(AuthenticatedRouteState.MaxCorpusPages);
        TenantGrants(state.Token).ShouldBe([state.TenantId]);
    }

    [Fact]
    public async Task ExactlyMaxCorpusTenantsSucceedsAndExceedingStopsWithoutExtraPageFetch()
    {
        AuthenticatedRouteState state = CreateState();
        TenantSummary[] pageOne = [Seed(state), .. Enumerable.Range(1, AuthenticatedRouteState.MaxCorpusTenants - 1)
            .Select(i => new TenantSummary($"t-{i:D3}", "Bounded tenant", TenantStatus.Active))];
        int passReads = 0;
        string token = await state.CreateCorpusReadTokenAsync(
            (_, _) =>
            {
                passReads++;
                return Task.FromResult(passReads == 1
                    ? Page(pageOne, "cursor-dup-page", hasMore: true)
                    : Page([Seed(state)]));
            },
            TestContext.Current.CancellationToken);

        passReads.ShouldBe(2);
        TenantGrants(token).Length.ShouldBe(AuthenticatedRouteState.MaxCorpusTenants);
        token.Length.ShouldBeLessThanOrEqualTo(AuthenticatedRouteState.MaxEncodedTokenLength);

        int overflowReads = 0;
        InvalidOperationException refusal = await Should.ThrowAsync<InvalidOperationException>(() => state.CreateCorpusReadTokenAsync(
            (_, _) =>
            {
                overflowReads++;
                overflowReads.ShouldBeLessThanOrEqualTo(2);
                return Task.FromResult(overflowReads == 1
                    ? Page(pageOne, "cursor-overflow-tenant", hasMore: true)
                    : Page([new("t-overflow-101", "Extra tenant", TenantStatus.Active)], "cursor-unreachable", hasMore: true));
            },
            TestContext.Current.CancellationToken));

        refusal.Message.ShouldBe("Live corpus read credentials require a complete valid index containing the current seed.");
        overflowReads.ShouldBe(2);
        TenantGrants(state.Token).ShouldBe([state.TenantId]);
    }

    [Fact]
    public async Task EncodedTokenWithinBoundSucceedsAndExceedingMaxEncodedTokenLengthRefusesWithoutLeaking()
    {
        AuthenticatedRouteState state = CreateState();
        static string LongTenantId(int index) => $"t-{index:D2}-{new string('a', 59)}";

        List<TenantSummary> boundedItems = [Seed(state)];
        int nextIndex = 1;
        while (boundedItems.Count < AuthenticatedRouteState.MaxCorpusTenants - 1)
        {
            TenantSummary candidate = new(LongTenantId(nextIndex), "Long bounded tenant", TenantStatus.Active);
            int candidateLength = AspireTopologyTests.CreateDemoJwt(
                boundedItems.Select(static item => item.TenantId).Append(candidate.TenantId),
                readOnly: true).Length;
            if (candidateLength > AuthenticatedRouteState.MaxEncodedTokenLength)
            {
                break;
            }

            boundedItems.Add(candidate);
            nextIndex++;
        }

        string boundedToken = await state.CreateCorpusReadTokenAsync(
            (_, _) => Task.FromResult(Page(boundedItems)),
            TestContext.Current.CancellationToken);

        boundedToken.Length.ShouldBeInRange(
            AuthenticatedRouteState.MaxEncodedTokenLength - 100,
            AuthenticatedRouteState.MaxEncodedTokenLength);
        TenantGrants(boundedToken).Length.ShouldBe(boundedItems.Count);

        List<TenantSummary> oversizedItems = [.. boundedItems, new(LongTenantId(nextIndex), "Long oversized tenant", TenantStatus.Active)];
        oversizedItems.Count.ShouldBeLessThanOrEqualTo(AuthenticatedRouteState.MaxCorpusTenants);
        string rawCandidateJwt = AspireTopologyTests.CreateDemoJwt(oversizedItems.Select(static item => item.TenantId), readOnly: true);
        rawCandidateJwt.Length.ShouldBeInRange(
            AuthenticatedRouteState.MaxEncodedTokenLength + 1,
            AuthenticatedRouteState.MaxEncodedTokenLength + 100);

        InvalidOperationException refusal = await Should.ThrowAsync<InvalidOperationException>(() => state.CreateCorpusReadTokenAsync(
            (_, _) => Task.FromResult(Page(oversizedItems)),
            TestContext.Current.CancellationToken));

        refusal.Message.ShouldBe("Live corpus read credentials require a complete valid index containing the current seed.");
        refusal.Message.ShouldNotContain(rawCandidateJwt);
        refusal.Message.ShouldNotContain(LongTenantId(1));
        TenantGrants(state.Token).ShouldBe([state.TenantId]);
    }

    private static AuthenticatedRouteState CreateState()
    {
        const string tenantId = "zz-synthetic-seed";
        return new(tenantId, "synthetic-member", AspireTopologyTests.CreateDemoJwt(tenantId), new TenantReadModel
        {
            TenantId = tenantId,
            Name = "Current synthetic seed",
            Status = TenantStatus.Active,
        });
    }

    private static TenantSummary Seed(AuthenticatedRouteState state)
        => new(state.TenantId, state.Persisted.Name, state.Persisted.Status);

    private static TenantsRestQueryResponse<PaginatedResult<TenantSummary>> Page(
        IReadOnlyList<TenantSummary> items, string? cursor = null, bool hasMore = false)
        => new(new(items, cursor, hasMore), new QueryResponseMetadata(
            ETag: "\"synthetic-index\"", IsStale: false, IsDegraded: false, ProjectionVersion: "17", ServedAt: DateTimeOffset.UtcNow)
        {
            Provenance = QueryResponseProvenance.ProjectionBacked,
            Lifecycle = ProjectionLifecycleState.Current,
        }, TenantsRestQueryFailureKind.None, (int)HttpStatusCode.OK);

    private static string[] TenantGrants(string token)
        => new JwtSecurityTokenHandler().ReadJwtToken(token).Claims
            .Where(static claim => claim.Type == "eventstore:tenant").Select(static claim => claim.Value).ToArray();
}
