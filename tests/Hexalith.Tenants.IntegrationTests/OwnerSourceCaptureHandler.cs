using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.Http.Json;

using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Captures the owner-source HTTP boundary without a sidecar or network listener.</summary>
internal sealed class OwnerSourceCaptureHandler(AuthoritativeEventStream? source = null, RSA? scopeKey = null) : HttpMessageHandler
{
    internal int ReadCount { get; private set; }
    internal HttpMethod? Method { get; private set; }
    internal Uri? Uri { get; private set; }
    internal Dictionary<string, string[]> Headers { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    internal string Body { get; private set; } = string.Empty;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Method = request.Method;
        Uri = request.RequestUri;
        Headers = request.Headers.ToDictionary(static header => header.Key, static header => header.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
        Body = await request.Content!.ReadAsStringAsync(cancellationToken);
        ReadCount++;
        var read = JsonSerializer.Deserialize<StreamReadRequest>(Body, JsonSerializerOptions.Web)!;
        if (source is not null)
        {
            string[] proof = Headers["X-Hexalith-Bound-Stream-Read-Proof"].Single().Split('.');
            string signature = proof[1].Replace('-', '+').Replace('_', '/');
            if (scopeKey is null || !scopeKey.VerifyData(Encoding.ASCII.GetBytes(proof[0]),
                Convert.FromBase64String(signature.PadRight((signature.Length + 3) / 4 * 4, '=')),
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) throw new InvalidOperationException("Invalid source scope proof.");
            var items = source.Events.Where(item => item.SequenceNumber > read.FromSequence
                && item.SequenceNumber <= (read.ToSequence ?? source.Head)).Take(read.PageSize).ToArray();
            long last = items.Length == 0 ? read.FromSequence : items[^1].SequenceNumber;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new StreamReadPage(
                read.Tenant, read.Domain, read.AggregateId, items, new StreamReadMetadata(read.FromSequence,
                    read.ToSequence, items.Length == 0 ? null : last, source.Head, items.Length,
                    last < (read.ToSequence ?? source.Head), null) { NativeProof = source.NativeProof })) };
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new StreamReadPage("system",
                read.Domain, read.AggregateId,
                [], new StreamReadMetadata(7, 12, null, 12, 0, false, null))),
        };
    }
}
