using System.Text.Json;

namespace Hexalith.Tenants.Queries;

/// <summary>
/// Parses common pagination payload fields for standard tenant query endpoints.
/// </summary>
internal static class TenantQueryPaginationPayloadParser {
    /// <summary>
    /// Parses the optional protected cursor and requested page size for standard tenant queries.
    /// </summary>
    /// <param name="payload">Serialized query payload.</param>
    /// <returns>Parsed cursor and bounded page size.</returns>
    public static TenantQueryPaginationPayload DeserializeStandardPayload(byte[]? payload) {
        if (payload is null || payload.Length == 0) {
            return new(null, TenantQueryPaginationPolicy.StandardDefaultPageSize);
        }

        try {
            using var doc = JsonDocument.Parse(payload);
            JsonElement root = doc.RootElement;

            string? cursor = root.TryGetProperty("cursor", out JsonElement cursorElement)
                && cursorElement.ValueKind == JsonValueKind.String
                    ? cursorElement.GetString()
                    : null;

            int pageSize = TenantQueryPaginationPolicy.StandardDefaultPageSize;
            if (root.TryGetProperty("pageSize", out JsonElement pageSizeElement)
                && pageSizeElement.ValueKind == JsonValueKind.Number
                && pageSizeElement.TryGetInt32(out int parsedPageSize)) {
                pageSize = parsedPageSize;
            }

            return new(cursor, TenantQueryPaginationPolicy.ClampStandardPageSize(pageSize));
        }
        catch (JsonException) {
            return new(null, TenantQueryPaginationPolicy.StandardDefaultPageSize);
        }
    }
}

/// <summary>
/// Common standard tenant query pagination fields.
/// </summary>
/// <param name="Cursor">Protected cursor submitted by the client.</param>
/// <param name="PageSize">Bounded page size.</param>
internal sealed record TenantQueryPaginationPayload(string? Cursor, int PageSize);
