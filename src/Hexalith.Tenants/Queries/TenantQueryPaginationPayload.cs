namespace Hexalith.Tenants.Queries;

/// <summary>
/// Common standard tenant query pagination fields and cursor decoding validation.
/// </summary>
/// <param name="Cursor">Protected cursor submitted by the client.</param>
/// <param name="PageSize">Bounded page size.</param>
/// <param name="ErrorMessage">Validation error when the cursor cannot be decoded as a string or the paging policy is unsupported.</param>
/// <param name="FromEnvelope">Whether the query envelope's paging policy, rather than the legacy payload fields, governs the page.</param>
internal sealed record TenantQueryPaginationPayload(string? Cursor, int PageSize, string? ErrorMessage = null, bool FromEnvelope = false);
