namespace Hexalith.Tenants.UI.State.TenantUsers;

/// <summary>
/// Identifies one server-side tenant-members page request.
/// </summary>
/// <param name="TenantId">Literal tenant scope.</param>
/// <param name="Cursor">Opaque paging cursor held only by the server-side circuit.</param>
/// <param name="PageSize">Requested page size.</param>
/// <param name="ETag">Prior strong ETag held only by the server-side circuit.</param>
public sealed record TenantUsersRequest(
    string TenantId,
    string? Cursor = null,
    int PageSize = 20,
    string? ETag = null)
{
    /// <summary>Returns a support-safe description that omits literal scope, cursor, and ETag values.</summary>
    public override string ToString()
        => $"{nameof(TenantUsersRequest)} {{ PageSize = {PageSize}, HasCursor = {Cursor is not null}, HasETag = {ETag is not null} }}";
}
