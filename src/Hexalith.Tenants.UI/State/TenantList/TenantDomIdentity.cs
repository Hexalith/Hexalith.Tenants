using System.Text;

using Hexalith.Tenants.UI.State.TenantAudit;

namespace Hexalith.Tenants.UI.State.TenantList;

/// <summary>Builds stable DOM anchors without changing logical tenant identity.</summary>
public static class TenantDomIdentity
{
    private const string TokenPrefix = "id-";

    private static readonly UTF8Encoding _utf8 = new(false, true);

    /// <summary>Encodes an exact identifier under an owning surface's fixed element prefix.</summary>
    /// <param name="prefix">The fixed row or detail element prefix, including its trailing separator.</param>
    /// <param name="tenantId">The exact tenant identifier.</param>
    /// <returns>A DOM-safe anchor, or null for blank or malformed identifiers.</returns>
    public static string? ElementId(string prefix, string? tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        try
        {
            return prefix + TokenPrefix + Convert.ToBase64String(_utf8.GetBytes(tenantId))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
        catch (EncoderFallbackException)
        {
            return null;
        }
    }

    /// <summary>Checks canonical encoded tenant tokens inside the existing bounded focus contract.</summary>
    /// <param name="token">The suffix after the owning element prefix.</param>
    /// <returns>Whether the token encodes one nonblank, well-formed identifier.</returns>
    internal static bool IsEncodedToken(string token)
    {
        if (!token.StartsWith(TokenPrefix, StringComparison.Ordinal)
            || token[TokenPrefix.Length..].Any(character => !char.IsAsciiLetterOrDigit(character)
                && character is not '-' and not '_'))
        {
            return false;
        }

        string payload = token[TokenPrefix.Length..];
        try
        {
            string padded = payload.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight((padded.Length + 3) / 4 * 4, '=');
            string tenantId = _utf8.GetString(Convert.FromBase64String(padded));
            return TenantAuditNavigationSafety.IsSafeIdentifier(tenantId)
                && string.Equals(ElementId("token-", tenantId), "token-" + token, StringComparison.Ordinal);
        }
        catch (Exception exception) when (exception is FormatException or DecoderFallbackException)
        {
            return false;
        }
    }
}
