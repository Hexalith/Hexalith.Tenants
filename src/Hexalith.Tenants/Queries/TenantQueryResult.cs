using System.Text.Json;

using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.Queries;

internal sealed record TenantQueryResult : QueryResult {
    public TenantQueryResult(
        bool success,
        byte[]? payloadBytes = null,
        string? errorMessage = null,
        string? projectionType = null,
        QueryResponseMetadata? metadata = null)
        : base(success, payloadBytes, errorMessage, projectionType) => Metadata = metadata;

    public QueryResponseMetadata? Metadata { get; init; }

    public static TenantQueryResult FromPayload(JsonElement payload, string? projectionType, string? eTag) {
        if (payload.ValueKind == JsonValueKind.Undefined) {
            throw new ArgumentException("Payload element must not be Undefined.", nameof(payload));
        }

        string? normalizedETag = NormalizeETag(eTag);
        QueryResponseMetadata? metadata = normalizedETag is null
            ? null
            : new QueryResponseMetadata(
                ETag: normalizedETag,
                IsNotModified: false,
                ProjectionVersion: normalizedETag,
                ServedAt: DateTimeOffset.UtcNow);

        return new TenantQueryResult(
            true,
            JsonSerializer.SerializeToUtf8Bytes(payload),
            projectionType: projectionType,
            metadata: metadata);
    }

    private static string? NormalizeETag(string? eTag) {
        if (string.IsNullOrWhiteSpace(eTag)) {
            return null;
        }

        return eTag.Trim().Trim('"');
    }
}
