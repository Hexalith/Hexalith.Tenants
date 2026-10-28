using Hexalith.EventStore.Client.Queries;

using Microsoft.AspNetCore.DataProtection;

namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>Purpose-isolated Data Protection wrapper for tenant-search cursors.</summary>
internal sealed class TenantSearchCursorCodec : ITenantSearchCursorCodec
{
    private const string DataProtectionPurpose = "Hexalith.Tenants.UI.AuthoritativeTenantSearch.v2";
    private readonly IQueryCursorCodec _codec;

    /// <summary>Initializes a dedicated codec that cannot be replaced by an unkeyed platform registration.</summary>
    public TenantSearchCursorCodec(IDataProtectionProvider dataProtectionProvider)
    {
        ArgumentNullException.ThrowIfNull(dataProtectionProvider);
        _codec = new QueryCursorCodec(dataProtectionProvider, DataProtectionPurpose);
    }

    /// <inheritdoc/>
    public string Encode(string scope, int windowStart, int consumedResults = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        return _codec.Encode(TenantSearchCursorPosition.QueryType, scope, TenantSearchCursorPosition.Format(windowStart, consumedResults));
    }

    /// <inheritdoc/>
    public bool TryDecode(string? cursor, string scope, out TenantSearchCursorPosition decodedPosition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        bool decoded = _codec.TryDecode(
            cursor,
            TenantSearchCursorPosition.QueryType,
            scope,
            out string? position,
            out _);
        if (!decoded)
        {
            decodedPosition = default;
            return false;
        }

        if (position is null)
        {
            decodedPosition = default;
            return true;
        }

        return TenantSearchCursorPosition.TryParse(position, out decodedPosition);
    }
}
