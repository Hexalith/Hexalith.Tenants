using Hexalith.Tenants.UI.Services.SupportSafety;

using System.Globalization;
using System.Text;

namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>
/// Preserves the existing audit-field display and receipt safety policy independently from clipboard approval.
/// </summary>
internal static class TenantAuditSupportSafety
{
    private static readonly string[] IdentifierUnsafeFragments =
    [
        "bearer ",
        "accesstoken",
        "authorization",
        "secret",
        "password",
        "token",
        "credential",
        "connectionstring",
        "jwt",
        "eyj",
        "metadata",
        "correlation",
        "stack trace",
        "exception",
        "cursor",
        "etag",
        "messageid",
        "payload",
        "problem detail",
    ];

    private static readonly string[] StrictUnsafeFragments =
    [
        "secret",
        "password",
        "token",
        "credential",
        "connectionstring",
        "authorization",
        "accesstoken",
        "bearer ",
        "metadata",
        "correlation",
        "stack trace",
        "exception",
        "jwt",
        "eyj",
        "cursor",
        "etag",
        "messageid",
        "payload",
        "problem detail",
        "infrastructure",
        "@",
    ];

    /// <summary>
    /// Returns an identifier only when it satisfies the established audit-field policy.
    /// </summary>
    /// <param name="value">The projected audit identifier.</param>
    /// <param name="kind">The identifier contract.</param>
    /// <returns>The original literal when safe; otherwise an empty string.</returns>
    internal static string SafeIdentifier(string? value, SupportSafeCopyValueKind kind)
        => IsSafe(value, kind) ? value! : string.Empty;

    /// <summary>
    /// Returns a reference only when it satisfies the established audit-field policy.
    /// </summary>
    /// <param name="value">The projected audit reference.</param>
    /// <returns>The original literal when safe; otherwise <see langword="null"/>.</returns>
    internal static string? SafeApprovedReference(string? value)
        => IsSafe(value, SupportSafeCopyValueKind.ApprovedReference) ? value : null;

    /// <summary>
    /// Determines whether an audit field satisfies the established display and receipt policy.
    /// </summary>
    /// <param name="value">The projected audit field.</param>
    /// <param name="kind">The field contract.</param>
    /// <returns><see langword="true"/> when the field is safe for audit display and receipt use.</returns>
    internal static bool IsSafe(string? value, SupportSafeCopyValueKind kind)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (ContainsInvisibleOrControl(value) || ContainsFieldBoundary(value))
        {
            return false;
        }

        string[] fragments = kind is SupportSafeCopyValueKind.TenantId or SupportSafeCopyValueKind.UserId
            ? IdentifierUnsafeFragments
            : StrictUnsafeFragments;
        string candidate = CanonicalizeForInspection(value);
        if (ContainsInvisibleOrControl(candidate)
            || ContainsFieldBoundary(candidate)
            || candidate.Contains('%', StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            candidate = candidate.Normalize(NormalizationForm.FormKC);
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (ContainsInvisibleOrControl(candidate)
            || ContainsFieldBoundary(candidate)
            || (kind is SupportSafeCopyValueKind.UserId
                && (candidate.Contains('@', StringComparison.Ordinal)
                    || LooksLikePhoneNumber(candidate))))
        {
            return false;
        }

        string normalized = new(candidate
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
        return !fragments.Any(fragment =>
        {
            string normalizedFragment = Normalize(fragment);
            return candidate.Contains(fragment, StringComparison.OrdinalIgnoreCase)
                || (normalizedFragment.Length > 0
                    && normalized.Contains(normalizedFragment, StringComparison.Ordinal));
        });
    }

    private static bool ContainsInvisibleOrControl(string value)
    {
        if (value.Any(char.IsControl))
        {
            return true;
        }

        for (int index = 0; index < value.Length; index++)
        {
            if (!Rune.TryGetRuneAt(value, index, out Rune rune))
            {
                return true;
            }

            if (Rune.GetUnicodeCategory(rune) is UnicodeCategory.Format)
            {
                return true;
            }

            index += rune.Utf16SequenceLength - 1;
        }

        return false;
    }

    /// <summary>Rejects characters that can split or visually spoof a copied field.</summary>
    internal static bool ContainsFieldBoundary(string value)
        => value.Any(character => character is '|' or ';' or '\r' or '\n' or '\u2028' or '\u2029'
            or '\u00A6' or '\uFF5C' or '\u2223' or '\u2225' or '\u2758' or '\u2759'
            or '\u2016' or '\u2502' or '\u2551');

    private static bool LooksLikePhoneNumber(string value)
    {
        int digits = 0;
        bool first = true;
        foreach (Rune rune in value.EnumerateRunes())
        {
            if (Rune.GetUnicodeCategory(rune) is UnicodeCategory.DecimalDigitNumber)
            {
                digits++;
            }
            else if ((first && rune.Value == '+')
                || rune.Value is '-' or '(' or ')'
                || Rune.GetUnicodeCategory(rune) is UnicodeCategory.SpaceSeparator)
            {
                // Phone punctuation is accepted only for detection, never as evidence of a safe ID.
            }
            else
            {
                return false;
            }

            first = false;
        }

        return digits is >= 7 and <= 15;
    }

    private static string CanonicalizeForInspection(string value)
    {
        string candidate = value;
        for (int attempt = 0; attempt < 3 && candidate.Contains('%', StringComparison.Ordinal); attempt++)
        {
            try
            {
                string decoded = Uri.UnescapeDataString(candidate);
                if (string.Equals(decoded, candidate, StringComparison.Ordinal))
                {
                    break;
                }

                candidate = decoded;
            }
            catch (UriFormatException)
            {
                return value;
            }
        }

        return candidate;
    }

    private static string Normalize(string value)
    {
        var result = new StringBuilder(value.Length);
        foreach (char character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                result.Append(char.ToLowerInvariant(character));
            }
        }

        return result.ToString();
    }
}
