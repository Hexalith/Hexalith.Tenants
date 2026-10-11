using System.Text.Json;

using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Queries.Handlers;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Queries;

public sealed class TenantOwnerAuthorityQueryHandlerTests
{
    [Fact]
    public async Task Current_owner_grants_only_the_bound_requester_and_revocation_denies()
    {
        IAuthoritativeEventStreamReader reader = Substitute.For<IAuthoritativeEventStreamReader>();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var identity = new AggregateIdentity("system", "tenants", "tenant-a");
        var created = new TenantCreated("tenant-a", "Tenant A", null, now);
        var added = new UserAddedToTenant("tenant-a", "user-a", TenantRole.TenantOwner);
        var removed = new UserRemovedFromTenant("tenant-a", "user-a");
        StreamReadEvent[] active = [Event(1, created, now), Event(2, added, now)];
        reader.ReadAsync(identity, Arg.Any<CancellationToken>()).Returns(_ =>
            new AuthoritativeStreamReadResult(new AuthoritativeEventStream(identity, active.Length, now, active, "observation-a"), null));
        var handler = new TenantOwnerAuthorityQueryHandler(reader, TimeProvider.System);
        QueryEnvelope envelope = Envelope("user-a");

        QueryResult allowed = await handler.ExecuteAsync(envelope, CancellationToken.None);
        allowed.Success.ShouldBeTrue();
        TenantOwnerAuthorityResult result = JsonSerializer.Deserialize<TenantOwnerAuthorityResult>(allowed.PayloadBytes!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        result.Allowed.ShouldBeTrue();
        result.PrincipalId.ShouldBe("user-a");
        result.SourceVersion.ShouldBe(2);

        (await handler.ExecuteAsync(Envelope("user-b") with { Payload = envelope.Payload }, CancellationToken.None))
            .Success.ShouldBeFalse();

        StreamReadEvent[] revoked = [.. active, Event(3, removed, now)];
        reader.ReadAsync(identity, Arg.Any<CancellationToken>()).Returns(_ =>
            new AuthoritativeStreamReadResult(new AuthoritativeEventStream(identity, revoked.Length, now, revoked, "observation-b"), null));
        QueryResult denied = await handler.ExecuteAsync(envelope, CancellationToken.None);
        denied.Success.ShouldBeTrue();
        JsonSerializer.Deserialize<TenantOwnerAuthorityResult>(denied.PayloadBytes!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!.Allowed.ShouldBeFalse();

        reader.ReadAsync(identity, Arg.Any<CancellationToken>()).Returns(_ =>
            new AuthoritativeStreamReadResult(new AuthoritativeEventStream(identity, 3, now, active, "incomplete"), null));
        (await handler.ExecuteAsync(envelope, CancellationToken.None)).Success.ShouldBeFalse();
    }

    private static QueryEnvelope Envelope(string requester)
    {
        var payload = new ResolveTenantOwnerAuthority("tenant-a", "user-a", "GrantChatBotAuthority", "human", "Http");
        return new QueryEnvelope("system", "tenants", "tenant-a", ResolveTenantOwnerAuthority.QueryType,
            JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            "corr-1", requester) { AuthenticatedWorkloadId = "chatbot", IsDelegated = true, OriginalActorId = requester };
    }

    private static StreamReadEvent Event(long sequence, object payload, DateTimeOffset now)
        => new(sequence, payload.GetType().FullName!, JsonSerializer.SerializeToUtf8Bytes(payload,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)), "json", 1, $"message-{sequence}", null, null,
            now, "owner");
}
