using System.Globalization;

namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>Canonicalizes tenant-search cursor positions.</summary>
internal static class TenantSearchCursorPosition {
    /// <summary>The platform cursor query type reserved for authoritative tenant search.</summary>
    public const string QueryType = "search-tenants";

    /// <summary>Formats a non-negative raw offset using invariant decimal notation.</summary>
    public static string Format(int offset) {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        return offset.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Parses only canonical, non-negative invariant decimal offsets.</summary>
    public static bool TryParse(string? value, out int offset) {
        offset = 0;
        return value is not null
            && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out offset)
            && offset >= 0
            && string.Equals(value, Format(offset), StringComparison.Ordinal);
    }
}
