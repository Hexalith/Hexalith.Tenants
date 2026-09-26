using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text.Json;

namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>Validates the limited tenant audit navigation contract.</summary>
public static partial class TenantAuditNavigationSafety
{
    private static readonly HashSet<string> _returnKeys = new(StringComparer.Ordinal)
    {
        "tab", "scope", "userId", "search", "status", "sort", "desc", "cursor", "selected", "anchor",
        "auditFocus", "auditPartialReturn", "returnUrl",
    };

    private static readonly HashSet<string> _sources = new(StringComparer.Ordinal)
    {
        "tenant-list", "tenant-detail", "my-tenants", "user-lookup", "member-row", "command-result",
    };

    /// <summary>Returns whether a literal tenant or user identifier can safely be placed in a route.</summary>
    public static bool IsSafeIdentifier(string? value)
        => !string.IsNullOrWhiteSpace(value)
            && value.Length <= 256
            && value is not ("." or "..")
            && !value.Any(character => char.IsControl(character) || char.IsWhiteSpace(character)
                || char.GetUnicodeCategory(character) is UnicodeCategory.Format or UnicodeCategory.Surrogate
                || character is '/' or '\\' or '?' or '#' or '%' or '&' or '=')
            && !LooksLikeCredential(value);

    /// <summary>Returns whether an identifier follows the persisted tenant aggregate route contract.</summary>
    public static bool IsSafeTenantId(string? value)
        => IsSafeIdentifier(value)
            && Regex.IsMatch(value!, @"^[A-Za-z0-9](?:[A-Za-z0-9._-]*[A-Za-z0-9])?$", RegexOptions.CultureInvariant);

    /// <summary>Returns whether a known audit source label is safe.</summary>
    public static bool IsSafeSource(string? source)
        => source is not null && _sources.Contains(source);

    /// <summary>Returns whether a known launcher's focus identifier is safe.</summary>
    public static bool IsSafeFocus(string? focus)
        => focus is not null
            && focus.Length <= 512
            && (focus.StartsWith("tenant-row-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-my-row-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-my-detail-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-user-row-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-member-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-detail-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-create-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-add-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-change-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-remove-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-edit-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-lifecycle-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-config-", StringComparison.Ordinal))
            && IsSafeIdentifier(focus);

    /// <summary>Canonicalizes an approved return route, dropping a protected cursor and reporting partial restoration.</summary>
    public static string? SafeReturnUrl(string? value, out bool partial)
        => SafeReturnUrl(value, out partial, allowNested: true);

    /// <summary>Canonicalizes a return route only when every detail hop belongs to the audited tenant.</summary>
    public static string? SafeReturnUrlForTenant(string? value, string? tenantId, out bool partial)
    {
        string? safe = SafeReturnUrl(value, out partial);
        if (safe is null || !IsSafeTenantId(tenantId))
        {
            return null;
        }

        string path = PathOf(safe);
        if (!IsListPath(path) && !string.Equals(path, "/tenants/" + tenantId, StringComparison.Ordinal))
        {
            return null;
        }

        string? nested = QueryValue(safe, "returnUrl");
        return nested is null || IsSafeListReturnUrl(nested) ? safe : null;
    }

    /// <summary>Canonicalizes a list or standalone origin for a detail page.</summary>
    public static string? SafeListReturnUrl(string? value, out bool partial)
    {
        string? safe = SafeReturnUrl(value, out partial);
        return safe is not null && IsSafeListReturnUrl(safe) ? safe : null;
    }

    /// <summary>Checks that a launcher focus identifier belongs to the validated origin route.</summary>
    public static bool IsFocusForReturnUrl(string? safeReturnUrl, string? focus)
        => safeReturnUrl is not null && focus is { } safeFocus && IsSafeFocus(safeFocus)
            && FocusMatchesPath(PathOf(safeReturnUrl), safeReturnUrl, safeFocus);

    /// <summary>Reads a canonical return query value from its own query layer.</summary>
    public static string? ReturnQueryValue(string? safeReturnUrl, string? key)
        => safeReturnUrl is not null && key is not null ? QueryValue(safeReturnUrl, key) : null;

    private static string? SafeReturnUrl(string? value, out bool partial, bool allowNested)
    {
        partial = false;
        if (string.IsNullOrWhiteSpace(value) || value.Length > 8192 || !value.StartsWith("/", StringComparison.Ordinal)
            || value.StartsWith("//", StringComparison.Ordinal) || value.Contains('#') || value.Contains('\\')
            || value.Any(char.IsControl))
        {
            return null;
        }

        int queryIndex = value.IndexOf('?', StringComparison.Ordinal);
        string path = queryIndex < 0 ? value : value[..queryIndex];
        if (path.Contains('%') || path.Contains("//", StringComparison.Ordinal)
            || path.EndsWith("/", StringComparison.Ordinal) || !IsApprovedPath(path))
        {
            return null;
        }

        if (queryIndex < 0)
        {
            return path;
        }

        var result = new StringBuilder(path);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string part in value[(queryIndex + 1)..].Split('&'))
        {
            int equalsIndex = part.IndexOf('=');
            if (equalsIndex <= 0 || equalsIndex == part.Length - 1)
            {
                return null;
            }

            string key = part[..equalsIndex];
            if (!_returnKeys.Contains(key) || !seen.Add(key))
            {
                return null;
            }

            if (key is "cursor")
            {
                partial = true;
                continue;
            }

            // Search is form-style query text, where a raw plus means a space. Identity and
            // focus values can contain a literal plus after an outer returnUrl was decoded.
            string? decoded = DecodeQueryValue(part[(equalsIndex + 1)..], plusAsSpace: key is "search");
            if (decoded is null || decoded.Length > 4096 || decoded.Any(char.IsControl)
                || (key is not "returnUrl" && ContainsEncodedCredential(decoded)))
            {
                return null;
            }

            if (key is "returnUrl")
            {
                if (!allowNested)
                {
                    return null;
                }

                decoded = SafeReturnUrl(decoded, out bool nestedPartial, allowNested: false);
                if (decoded is null)
                {
                    return null;
                }

                partial |= nestedPartial;
            }
            else if (key is ("auditFocus" or "anchor") && !IsSafeFocus(decoded))
            {
                return null;
            }
            else if (key is "auditPartialReturn" && !bool.TryParse(decoded, out _))
            {
                return null;
            }
            else if (key is "userId" && !IsSafeIdentifier(decoded))
            {
                return null;
            }
            else if (key is "selected" && !IsSafeTenantId(decoded))
            {
                return null;
            }

            result.Append(result.ToString().Contains('?', StringComparison.Ordinal) ? '&' : '?');
            result.Append(key).Append('=').Append(Uri.EscapeDataString(decoded));
        }

        string canonical = result.ToString();
        string? anchor = QueryValue(canonical, "anchor");
        string? auditFocus = QueryValue(canonical, "auditFocus");
        if ((anchor is not null && !IsFocusForReturnUrl(canonical, anchor))
            || (auditFocus is not null && !IsFocusForReturnUrl(canonical, auditFocus))
            || (anchor is not null && auditFocus is not null
                && !string.Equals(anchor, auditFocus, StringComparison.Ordinal)))
        {
            return null;
        }

        return canonical;
    }

    /// <summary>Returns whether a support-safe, non-credential hint can be displayed.</summary>
    public static bool IsSafeHint(string? value)
        => IsSafeIdentifier(value);

    private static bool IsApprovedPath(string path)
    {
        string[] parts = path.Split('/');
        return parts.Length >= 2 && parts[0].Length == 0 && parts[1] == "tenants"
            && (parts.Length == 2 || (parts.Length == 3 && (parts[2] is "my" or "users" || IsSafeTenantId(parts[2]))));
    }

    private static bool IsListPath(string path)
        => path is "/tenants" or "/tenants/my" or "/tenants/users";

    private static bool IsSafeListReturnUrl(string safe)
        => IsListPath(PathOf(safe)) && QueryValue(safe, "returnUrl") is null;

    private static string PathOf(string value)
    {
        int index = value.IndexOf('?', StringComparison.Ordinal);
        return index < 0 ? value : value[..index];
    }

    private static string? QueryValue(string value, string key)
    {
        int queryIndex = value.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex < 0)
        {
            return null;
        }

        foreach (string part in value[(queryIndex + 1)..].Split('&'))
        {
            int equalsIndex = part.IndexOf('=');
            if (equalsIndex > 0 && string.Equals(part[..equalsIndex], key, StringComparison.Ordinal))
            {
                return DecodeQueryValue(part[(equalsIndex + 1)..]);
            }
        }

        return null;
    }

    private static bool FocusMatchesPath(string path, string url, string focus)
    {
        if (path is "/tenants/my")
        {
            return focus.StartsWith("tenants-my-row-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-my-detail-", StringComparison.Ordinal);
        }

        if (path is "/tenants/users")
        {
            return focus.StartsWith("tenants-user-row-", StringComparison.Ordinal);
        }

        if (path is "/tenants")
        {
            string? tab = QueryValue(url, "tab");
            string? scope = QueryValue(url, "scope");
            return tab is "users"
                ? focus.StartsWith("tenants-user-row-", StringComparison.Ordinal)
                : scope is "mine"
                    ? focus.StartsWith("tenants-my-row-", StringComparison.Ordinal)
                        || focus.StartsWith("tenants-my-detail-", StringComparison.Ordinal)
                    : focus.StartsWith("tenant-row-", StringComparison.Ordinal)
                        || focus.StartsWith("tenants-create-", StringComparison.Ordinal);
        }

        return path.StartsWith("/tenants/", StringComparison.Ordinal)
            && (focus.StartsWith("tenants-detail-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-member-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-add-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-change-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-remove-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-edit-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-lifecycle-", StringComparison.Ordinal)
                || focus.StartsWith("tenants-config-", StringComparison.Ordinal));
    }

    private static string? DecodeQueryValue(string encoded, bool plusAsSpace = false)
    {
        try
        {
            // Decode only this query layer. Repeated decoding here changes literal escaped search text
            // and turns a nested focus identifier's escaped plus into a space on the next parse.
            return Uri.UnescapeDataString(plusAsSpace ? encoded.Replace('+', ' ') : encoded);
        }
        catch (UriFormatException)
        {
            return null;
        }
    }

    private static bool ContainsEncodedCredential(string value)
    {
        string current = value;
        for (int pass = 0; pass < 4; pass++)
        {
            if (LooksLikeCredential(current))
            {
                return true;
            }

            string? next = DecodeQueryValue(current);
            if (next is null || next == current)
            {
                return false;
            }

            current = next;
        }

        return LooksLikeCredential(current);
    }

    private static bool LooksLikeCredential(string value)
        => LooksLikeJwt(value)
            || Regex.IsMatch(value, @"(?i)(?:^|[^A-Za-z0-9_])bearer\s+\S+", RegexOptions.CultureInvariant)
            || Regex.IsMatch(value, @"(?i)(?:^|[^A-Za-z0-9_])(?:bearer|(?:access|refresh|id)[_-]?token|token|etag|api[_-]?key|client[_-]?secret|secret|password)\s*[:=]\s*\S+", RegexOptions.CultureInvariant);

    private static bool LooksLikeJwt(string value)
    {
        foreach (Match candidate in JwtShape().Matches(value))
        {
            string header = candidate.Value[..candidate.Value.IndexOf('.', StringComparison.Ordinal)];
            try
            {
                string padded = header.Replace('-', '+').Replace('_', '/').PadRight((header.Length + 3) / 4 * 4, '=');
                using JsonDocument document = JsonDocument.Parse(Convert.FromBase64String(padded));
                if (document.RootElement.ValueKind is JsonValueKind.Object
                    && document.RootElement.TryGetProperty("alg", out _))
                {
                    return true;
                }
            }
            catch (Exception exception) when (exception is FormatException or JsonException)
            {
                // An ordinary dotted search term is not a credential.
            }
        }

        return false;
    }

    [GeneratedRegex(@"(?<![A-Za-z0-9_-])[A-Za-z0-9_-]{2,}\.[A-Za-z0-9_-]{2,}\.[A-Za-z0-9_-]{2,}(?![A-Za-z0-9_-])", RegexOptions.CultureInvariant)]
    private static partial Regex JwtShape();
}
