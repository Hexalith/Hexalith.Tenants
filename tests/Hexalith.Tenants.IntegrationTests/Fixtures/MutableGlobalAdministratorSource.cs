using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.Tenants.Contracts.Events;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Independent source evidence for HTTP composition tests; never reads the projection under test.</summary>
internal sealed class MutableGlobalAdministratorSource : IAuthoritativeEventStreamReader, INativeSourceMetadataReader
{
    private static readonly AggregateIdentity Identity = new("system", "global-administrators", "global-administrators");
    internal int ReadCount { get; private set; }
    internal bool Available { get; set; } = true;
    private readonly List<StreamReadEvent> _events = [];
    private NativeSourceProof _proof = NativeSourceProofCodec.CreateSeed(Identity, "01ARZ3NDEKTSV4RRFFQ69G5FAV");

    internal MutableGlobalAdministratorSource(params string[] administrators)
    {
        foreach (string user in administrators) Append(new GlobalAdministratorSet("system", user));
    }

    internal void Append(object payload)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonSerializerOptions.Web);
        int sequence = _events.Count + 1;
        var item = new StreamReadEvent(sequence, payload.GetType().FullName!, bytes, "json", 1,
            "source-" + sequence, null, null, DateTimeOffset.UtcNow, "source-actor")
        { NativeEventDigest = Convert.ToHexStringLower(SHA256.HashData(bytes)) };
        _proof = NativeSourceProofCodec.ExtendDigest(_proof, Identity, sequence, item.MessageId,
            Convert.ToHexStringLower(SHA256.HashData(bytes)));
        _events.Add(item);
    }

    public Task<AuthoritativeStreamReadResult> ReadAsync(AggregateIdentity identity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadCount++;
        if (identity != Identity) throw new InvalidOperationException("Unexpected authority source identity.");
        return Task.FromResult(Available ? new AuthoritativeStreamReadResult(Snapshot(), null)
            : new AuthoritativeStreamReadResult(null, "source-unavailable"));
    }

    internal AuthoritativeEventStream Snapshot() => new(Identity, _events.Count,
        DateTimeOffset.UtcNow, _events.ToArray(), "http-source")
    { NativeProof = _proof };

    Task<NativeSourceObservation?> INativeSourceMetadataReader.ReadAsync(AggregateIdentity identity, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<NativeSourceObservation?>(identity == Identity ? new(_proof, DateTimeOffset.UtcNow) : null);
    }
}
