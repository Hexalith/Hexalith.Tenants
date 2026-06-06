namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

public sealed record GlobalAdministratorsRequest(
    string? Cursor = null,
    int PageSize = 20,
    string? ETag = null);
