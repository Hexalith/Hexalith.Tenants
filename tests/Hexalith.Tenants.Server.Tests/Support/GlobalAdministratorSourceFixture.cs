using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.Tenants.Authorization;
using Hexalith.Tenants.Contracts.Events;
using NSubstitute;

namespace Hexalith.Tenants.Server.Tests.Support;

internal static class GlobalAdministratorSourceFixture
{
    internal static AuthoritativeEventStream Source(int pairs = 0, bool revoked = false, object[]? appended = null)
    {
        var identity = new AggregateIdentity("system", "global-administrators", "global-administrators");
        var now = DateTimeOffset.UtcNow.AddSeconds(-1);
        var proof = NativeSourceProofCodec.CreateSeed(identity, "01ARZ3NDEKTSV4RRFFQ69G5FAV");
        var events = new List<StreamReadEvent>();
        List<object> payloads = [new GlobalAdministratorSet("system", "admin")];
        for (int i = 0; i < pairs; i++)
        {
            payloads.Add(new GlobalAdministratorSet("system", "other"));
            payloads.Add(new GlobalAdministratorRemoved("system", "other"));
        }
        if (revoked) payloads.Add(new GlobalAdministratorRemoved("system", "admin"));
        payloads.AddRange(appended ?? []);
        foreach (var payload in payloads)
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonSerializerOptions.Web);
            int sequence = events.Count + 1;
            var item = new StreamReadEvent(sequence, payload.GetType().FullName!, bytes, "json", 1,
                "event-" + sequence, null, null, now, "actor");
            proof = NativeSourceProofCodec.ExtendDigest(proof, identity, sequence, item.MessageId,
                Convert.ToHexStringLower(SHA256.HashData(bytes)));
            events.Add(item);
        }
        return new(identity, events.Count, now, events, "native-fixture") { NativeProof = proof };
    }

    internal static TenantsGlobalAdministratorAuthority Authority(AuthoritativeEventStream source,
        out IAuthoritativeEventStreamReader reader, out INativeSourceMetadataReader metadata)
    {
        reader = Substitute.For<IAuthoritativeEventStreamReader>();
        reader.ReadAsync(Arg.Any<AggregateIdentity>(), Arg.Any<CancellationToken>()).Returns(new AuthoritativeStreamReadResult(source, null));
        metadata = Substitute.For<INativeSourceMetadataReader>();
        metadata.ReadAsync(source.Identity, Arg.Any<CancellationToken>()).Returns(new NativeSourceObservation(source.NativeProof!, source.ObservedAt));
        return new(reader, metadata, TimeProvider.System);
    }
}
