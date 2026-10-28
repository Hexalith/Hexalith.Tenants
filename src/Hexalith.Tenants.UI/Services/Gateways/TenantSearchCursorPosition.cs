using System.Globalization;

namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>A fixed raw window start and the count of returned entries already consumed from it.</summary>
/// <param name="WindowStart">The raw offset used to request the complete fixed-size window.</param>
/// <param name="ConsumedResults">The returned-result count to skip when replaying that window.</param>
internal readonly record struct TenantSearchCursorPosition(int WindowStart, int ConsumedResults)
{
    /// <summary>The platform cursor query type reserved for authoritative tenant search.</summary>
    public const string QueryType = "search-tenants";

    /// <summary>Formats the version and both non-negative numbers using invariant decimal notation.</summary>
    public static string Format(int windowStart, int consumedResults = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(windowStart);
        ArgumentOutOfRangeException.ThrowIfNegative(consumedResults);
        return string.Create(CultureInfo.InvariantCulture, $"2:{windowStart}:{consumedResults}");
    }

    /// <summary>Parses only the current version with canonical non-negative invariant numbers.</summary>
    public static bool TryParse(string? value, out TenantSearchCursorPosition position)
    {
        position = default;
        string[]? parts = value?.Split(':');
        if (parts is not { Length: 3 }
            || parts[0] != "2"
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int windowStart)
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out int consumedResults)
            || windowStart < 0 || consumedResults < 0
            || !string.Equals(value, Format(windowStart, consumedResults), StringComparison.Ordinal))
        {
            return false;
        }
        position = new(windowStart, consumedResults);
        return true;
    }
}
