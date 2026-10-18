using System.Text.Json;

using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.Queries;

/// <summary>
/// Parses common pagination payload fields for standard tenant query endpoints.
/// </summary>
internal static class TenantQueryPaginationPayloadParser
{
    /// <summary>
    /// Parses the optional protected cursor and requested page size for standard tenant queries.
    /// </summary>
    /// <param name="payload">Serialized query payload.</param>
    /// <returns>Parsed cursor and bounded page size, or a validation error for an undecodable cursor.</returns>
    public static TenantQueryPaginationPayload DeserializeStandardPayload(byte[]? payload)
    {
        if (payload is null || payload.Length == 0)
        {
            return new(null, TenantQueryPaginationPolicy.StandardDefaultPageSize);
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            JsonElement root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return new(null, TenantQueryPaginationPolicy.StandardDefaultPageSize);
            }

            string? cursor = null;
            if (root.TryGetProperty("cursor", out JsonElement cursorElement)
                && cursorElement.ValueKind == JsonValueKind.String)
            {
                try
                {
                    cursor = cursorElement.GetString();
                }
                catch (InvalidOperationException)
                {
                    return new(null, TenantQueryPaginationPolicy.StandardDefaultPageSize, QueryAdapterFailureReason.InvalidEnvelope);
                }
            }

            int pageSize = TenantQueryPaginationPolicy.StandardDefaultPageSize;
            if (root.TryGetProperty("pageSize", out JsonElement pageSizeElement)
                && pageSizeElement.ValueKind == JsonValueKind.Number
                && pageSizeElement.TryGetInt32(out int parsedPageSize))
            {
                pageSize = parsedPageSize;
            }

            return new(cursor, TenantQueryPaginationPolicy.ClampStandardPageSize(pageSize));
        }
        catch (JsonException)
        {
            return new(null, TenantQueryPaginationPolicy.StandardDefaultPageSize);
        }
    }
}
