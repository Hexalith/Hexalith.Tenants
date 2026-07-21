using Hexalith.EventStore.Client.Queries;

using Microsoft.AspNetCore.DataProtection;

namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>Purpose-isolated Data Protection wrapper for tenant-search cursors.</summary>
internal sealed class TenantSearchCursorCodec : ITenantSearchCursorCodec {
    private const string DataProtectionPurpose = "Hexalith.Tenants.UI.AuthoritativeTenantSearch.v1";
    private readonly IQueryCursorCodec _codec;

    /// <summary>Initializes a dedicated codec that cannot be replaced by an unkeyed platform registration.</summary>
    public TenantSearchCursorCodec(IDataProtectionProvider dataProtectionProvider) {
        _codec = new QueryCursorCodec(dataProtectionProvider, DataProtectionPurpose);
    }

    /// <inheritdoc/>
    public string Encode(string scope, int offset)
        => _codec.Encode(TenantSearchCursorPosition.QueryType, scope, TenantSearchCursorPosition.Format(offset));

    /// <inheritdoc/>
    public bool TryDecode(string? cursor, string scope, out int offset) {
        bool decoded = _codec.TryDecode(
            cursor,
            TenantSearchCursorPosition.QueryType,
            scope,
            out string? position,
            out _);
        if (!decoded) {
            offset = 0;
            return false;
        }

        if (position is null) {
            offset = 0;
            return true;
        }

        return TenantSearchCursorPosition.TryParse(position, out offset);
    }
}
