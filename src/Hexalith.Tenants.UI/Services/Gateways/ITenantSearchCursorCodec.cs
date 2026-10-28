namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>Protects numeric Memories window positions for the tenant-search surface.</summary>
internal interface ITenantSearchCursorCodec {
    /// <summary>Encodes a fixed window start and consumed returned-result count for a nonblank scope.</summary>
    string Encode(string scope, int windowStart, int consumedResults = 0);

    /// <summary>Validates and decodes a numeric window position for a nonblank scope.</summary>
    bool TryDecode(string? cursor, string scope, out TenantSearchCursorPosition position);
}
