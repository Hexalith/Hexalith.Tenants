using System.Security.Cryptography;
using System.Text;

using Hexalith.EventStore.Client.Queries;

namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>Builds the complete, fixed-size tenant-search cursor scope.</summary>
internal static class TenantSearchCursorScopes {
    /// <summary>The fixed Memories tenant/index used for tenant candidates.</summary>
    public const string SearchIndex = "tenants-index";

    /// <summary>Builds a seven-field scope bound to caller and canonical query identity.</summary>
    public static string Create(
        string userId,
        string search,
        string? status,
        string sort,
        bool descending,
        int pageSize)
        => QueryCursorScope.Create()
            .Add("user", Hash(userId))
            .Add("index", SearchIndex)
            .Add("search", Hash(search))
            .Add("status", status)
            .Add("sort", sort)
            .Add("direction", descending ? "descending" : "ascending")
            .Add("pageSize", pageSize.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Build();

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
