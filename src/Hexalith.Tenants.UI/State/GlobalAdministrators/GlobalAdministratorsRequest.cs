namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

public sealed record GlobalAdministratorsRequest(
    string? Cursor = null,
    int PageSize = 20,
    string? ETag = null)
{
    /// <summary>
    /// Returns a support-safe description that omits the protected cursor and validator.
    /// </summary>
    /// <returns>A bounded support-safe request description.</returns>
    public override string ToString()
        => $"{nameof(GlobalAdministratorsRequest)} {{ PageSize = {PageSize} }}";
}
