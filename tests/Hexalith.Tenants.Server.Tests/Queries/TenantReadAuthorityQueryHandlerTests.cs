using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Queries.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Queries;

public sealed class TenantReadAuthorityQueryHandlerTests
{
    [Theory]
    [InlineData(TenantRole.TenantReader)]
    [InlineData(TenantRole.TenantContributor)]
    [InlineData(TenantRole.TenantOwner)]
    public async Task NativeReadPolicyUsesTheCurrentCanonicalTenantPrefixWithoutChangingManagedCallerTenant(TenantRole role)
    {
        var source = Source(role);
        var reader = Substitute.For<IAuthoritativeEventStreamReader>();
        reader.ReadAsync(source.Identity, Arg.Any<CancellationToken>()).Returns(new AuthoritativeStreamReadResult(source, null));
        var metadata = Substitute.For<INativeSourceMetadataReader>();
        metadata.ReadAsync(source.Identity, Arg.Any<CancellationToken>()).Returns(new NativeSourceObservation(source.NativeProof!, source.ObservedAt));
        QueryResult result = await DispatchAsync(reader, metadata);
        result.Success.ShouldBeTrue();
        TenantReadAuthorityResult value = result.GetPayload().Deserialize<TenantReadAuthorityResult>(JsonSerializerOptions.Web)!;
        value.Allowed.ShouldBeTrue(); value.TenantId.ShouldBe("tenant-a"); value.PrincipalId.ShouldBe("actor");
        value.NativeProof.ShouldBe(source.NativeProof);
        result.GetPayload().EnumerateObject().Select(property => property.Name).ShouldBe(["tenantId", "principalId", "allowed", "nativeProof", "verifiedAt"]);
        JsonSerializer.Serialize(result).ShouldNotContain("users");
        await reader.Received(1).ReadAsync(new AggregateIdentity("system", "tenants", "tenant-a"), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("legacy")]
    [InlineData("source-replacement")]
    [InlineData("prefix-replacement")]
    [InlineData("incomplete")]
    public async Task UnprovenOrChangedNativePrefixCannotProduceReadAuthority(string change)
    {
        var source = Source(TenantRole.TenantReader);
        var reader = Substitute.For<IAuthoritativeEventStreamReader>();
        var supplied = change == "legacy" ? source with { NativeProof = null } : change == "incomplete" ? source with { Head = 3 } : source;
        reader.ReadAsync(Arg.Any<AggregateIdentity>(), Arg.Any<CancellationToken>()).Returns(new AuthoritativeStreamReadResult(supplied, null));
        var metadata = Substitute.For<INativeSourceMetadataReader>();
        var final = source.NativeProof!;
        if (change == "source-replacement") final = final with { SourceIncarnation = "01ARZ3NDEKTSV4RRFFQ69G5FAW" };
        if (change == "prefix-replacement") final = final with { CumulativeDigest = new string('c', 64) };
        metadata.ReadAsync(source.Identity, Arg.Any<CancellationToken>()).Returns(new NativeSourceObservation(final, source.ObservedAt));
        (await DispatchAsync(reader, metadata)).Success.ShouldBeFalse();
    }

    [Theory]
    [InlineData("actor")]
    [InlineData("tenant")]
    [InlineData("workload")]
    [InlineData("origin")]
    [InlineData("payload")]
    [InlineData("oversized")]
    [InlineData("unverified")]
    public async Task CallerScopeAndSdkVerificationAreRequiredBeforeAnyProtectedSourceLookup(string change)
    {
        var reader = Substitute.For<IAuthoritativeEventStreamReader>();
        var metadata = Substitute.For<INativeSourceMetadataReader>();
        var query = Query();
        query = change switch
        {
            "actor" => query with { OriginalActorId = "another" },
            "tenant" => query with { TenantId = "tenant-b" },
            "workload" => query with { AuthenticatedWorkloadId = "another" },
            "origin" => query with { CallerAuthority = query.CallerAuthority! with { OriginatingContext = null } },
            "payload" => query with { Payload = "{\"actor\":\"another\"}"u8.ToArray() },
            "oversized" => query with { Payload = new byte[8193] },
            _ => query,
        };
        (await DispatchAsync(reader, metadata, query, change != "unverified")).Success.ShouldBeFalse();
        await reader.DidNotReceiveWithAnyArgs().ReadAsync(default!);
    }

    [Fact]
    public async Task RevokedMembershipReturnsOnlyFalseAgainstTheActualNewNativeRevision()
    {
        var source = Source(TenantRole.TenantReader, revoked: true);
        var reader = Substitute.For<IAuthoritativeEventStreamReader>();
        reader.ReadAsync(source.Identity, Arg.Any<CancellationToken>()).Returns(new AuthoritativeStreamReadResult(source, null));
        var metadata = Substitute.For<INativeSourceMetadataReader>();
        metadata.ReadAsync(source.Identity, Arg.Any<CancellationToken>()).Returns(new NativeSourceObservation(source.NativeProof!, source.ObservedAt));
        var value = (await DispatchAsync(reader, metadata)).GetPayload().Deserialize<TenantReadAuthorityResult>(JsonSerializerOptions.Web)!;
        value.Allowed.ShouldBeFalse(); value.NativeProof.HeadSequence.ShouldBe(3);
    }

    [Theory]
    [InlineData(TenantRole.TenantReader)]
    [InlineData(TenantRole.TenantContributor)]
    [InlineData(TenantRole.TenantOwner)]
    public async Task DisabledNativeTenantDeniesReadWithRetainedMembership(TenantRole role)
    {
        var source = Source(role, disabled: true);
        source.Events.Count.ShouldBe(3);
        source.Events[1].EventTypeName.ShouldBe(typeof(UserAddedToTenant).FullName);
        var reader = Substitute.For<IAuthoritativeEventStreamReader>();
        reader.ReadAsync(source.Identity, Arg.Any<CancellationToken>()).Returns(new AuthoritativeStreamReadResult(source, null));
        var metadata = Substitute.For<INativeSourceMetadataReader>();
        metadata.ReadAsync(source.Identity, Arg.Any<CancellationToken>()).Returns(new NativeSourceObservation(source.NativeProof!, source.ObservedAt));
        QueryResult result = await DispatchAsync(reader, metadata);
        result.Success.ShouldBeTrue();
        var value = result.GetPayload().Deserialize<TenantReadAuthorityResult>(JsonSerializerOptions.Web)!;
        value.Allowed.ShouldBeFalse();
        value.NativeProof.ShouldBe(source.NativeProof);
        value.NativeProof.HeadSequence.ShouldBe(3);
    }

    private static QueryEnvelope Query() => new("tenant-a", "tenants", "tenant-a", ResolveTenantReadAuthority.QueryType,
        "{}"u8.ToArray(), "corr", "actor")
    {
        IsDelegated = true, AuthenticatedWorkloadId = "conversations", OriginalActorId = "actor",
        CallerAuthority = new("delegated-owner-query", [], [])
        { OriginatingContext = new("actor", "original", false, null, ["projects.read"], ["hexalith-eventstore"]) },
    };

    private static async Task<QueryResult> DispatchAsync(IAuthoritativeEventStreamReader reader, INativeSourceMetadataReader metadata,
        QueryEnvelope? query = null, bool verified = true)
    {
        query ??= Query();
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        List<Claim> claims = [new(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, "eventstore"),
            new(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, EventStoreWorkloadOperations.DomainServiceQuery)];
        if (verified) claims.AddRange(QueryCallerAuthorityBinding.Bind(query).Select(pair => new Claim(pair.Key, pair.Value)));
        accessor.HttpContext.User = new(new ClaimsIdentity(claims, "workload"));
        var handler = new TenantReadAuthorityQueryHandler(reader, metadata, accessor, TimeProvider.System);
        await using var services = new ServiceCollection().AddSingleton<IHttpContextAccessor>(accessor)
            .AddSingleton<IDomainQueryHandler>(handler).BuildServiceProvider();
        return await DomainQueryDispatcher.ExecuteAsync(services, query, TestContext.Current.CancellationToken);
    }

    private static AuthoritativeEventStream Source(TenantRole role, bool revoked = false, bool disabled = false)
    {
        var identity = new AggregateIdentity("system", "tenants", "tenant-a");
        var now = DateTimeOffset.UtcNow.AddSeconds(-1);
        object[] payloads = disabled ? [new TenantCreated("tenant-a", "name", null, now),
            new UserAddedToTenant("tenant-a", "actor", role), new TenantDisabled("tenant-a", now)]
            : revoked ? [new TenantCreated("tenant-a", "name", null, now),
            new UserAddedToTenant("tenant-a", "actor", role), new UserRemovedFromTenant("tenant-a", "actor")]
            : [new TenantCreated("tenant-a", "name", null, now), new UserAddedToTenant("tenant-a", "actor", role)];
        var proof = NativeSourceProofCodec.CreateSeed(identity, "01ARZ3NDEKTSV4RRFFQ69G5FAV");
        var events = new List<StreamReadEvent>();
        foreach (object payload in payloads)
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonSerializerOptions.Web);
            int sequence = events.Count + 1;
            var item = new StreamReadEvent(sequence, payload.GetType().FullName!, bytes, "json", 1, "event-" + sequence,
                null, null, now, "actor");
            proof = NativeSourceProofCodec.ExtendDigest(proof, identity, sequence, item.MessageId, Convert.ToHexStringLower(SHA256.HashData(bytes)));
            events.Add(item);
        }
        return new(identity, events.Count, now, events, "fixture-native-fold") { NativeProof = proof };
    }
}
