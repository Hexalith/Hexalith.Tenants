namespace Hexalith.Tenants.Queries;

/// <summary>
/// Common standard tenant query pagination fields and cursor decoding validation.
/// </summary>
/// <param name="Cursor">Protected cursor submitted by the client.</param>
/// <param name="PageSize">Bounded page size.</param>
/// <param name="ErrorMessage">Validation error when the cursor cannot be decoded as a string.</param>
internal sealed record TenantQueryPaginationPayload(string? Cursor, int PageSize, string? ErrorMessage = null);
