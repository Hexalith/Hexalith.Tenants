namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>Protects raw Memories offsets for the tenant-search surface.</summary>
internal interface ITenantSearchCursorCodec {
    /// <summary>Encodes a non-negative raw offset for the supplied search scope.</summary>
    string Encode(string scope, int offset);

    /// <summary>Validates and decodes a raw offset for the supplied search scope.</summary>
    bool TryDecode(string? cursor, string scope, out int offset);
}
