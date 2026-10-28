using System.Globalization;
using System.Text;

using Hexalith.Tenants.UI.Services.SupportSafety;

using NUlid;

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
        string? candidate = CanonicalizeForInspection(value);
        if (candidate is null || ContainsInvisibleOrControl(candidate)
            || ContainsFieldBoundary(candidate)
            || candidate.Contains('%', StringComparison.Ordinal)
            || (kind is SupportSafeCopyValueKind.UserId && !ContainsOnlyUserIdentifierCharacters(candidate)))
        {
            return false;
        }

        string skeleton = InspectionSkeleton(candidate);
        if (ContainsFieldBoundary(skeleton)
            || (kind is SupportSafeCopyValueKind.UserId
                && (skeleton.Contains('@', StringComparison.Ordinal)
                    || LooksLikePhoneNumber(skeleton))))
        {
            return false;
        }

        string normalized = new(skeleton
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
        // Only this already approved reference contract permits the two incidental fragments.
        // Parsing and exact round-trip prevent encoded, normalized or lowercase lookalikes from
        // acquiring the exception. A ULID shape alone never establishes that arbitrary data is safe.
        bool canonicalReference = kind is SupportSafeCopyValueKind.ApprovedReference
            && Ulid.TryParse(value, out Ulid reference)
            && string.Equals(reference.ToString(), value, StringComparison.Ordinal);
        return !fragments.Any(fragment =>
        {
            if (canonicalReference && fragment is "jwt" or "eyj")
            {
                return false;
            }

            string normalizedFragment = Normalize(fragment);
            return skeleton.Contains(fragment, StringComparison.OrdinalIgnoreCase)
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
            or '\u2016' or '\u2502' or '\u2551' or '\u2503' or '\u275A'
            or '\u01C0' or '\u23D0' or '\u204F' or '\u061B');

    private static bool ContainsOnlyUserIdentifierCharacters(string value)
    {
        foreach (Rune rune in value.EnumerateRunes())
        {
            // Letters and marks preserve supported Unicode IDs. Only explicit ASCII punctuation and
            // visible spacing may accompany them; general punctuation/symbol categories are not IDs.
            if (Rune.GetUnicodeCategory(rune) is UnicodeCategory.UppercaseLetter
                or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter
                or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter
                or UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark
                or UnicodeCategory.DecimalDigitNumber or UnicodeCategory.SpaceSeparator
                || rune.Value is '-' or '_' or '.' or '/' or ':' or '+' or '(' or ')' or '@')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static bool LooksLikePhoneNumber(string value)
    {
        string candidate = value.Trim();
        if (candidate.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
        {
            candidate = candidate[4..].Trim();
        }

        int digits = 0;
        bool first = true;
        foreach (Rune rune in candidate.EnumerateRunes())
        {
            if (Rune.GetUnicodeCategory(rune) is UnicodeCategory.DecimalDigitNumber)
            {
                digits++;
            }
            else if ((first && rune.Value == '+')
                || rune.Value is '-' or '.' or '/' or '(' or ')'
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

    private static string? CanonicalizeForInspection(string value)
    {
        try
        {
            string candidate = value;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                // Compatibility normalization must precede decoding: fullwidth percent signs can
                // otherwise hide encoded PII. Repeat both steps for nested encoded forms.
                candidate = candidate.Normalize(NormalizationForm.FormKC);
                if (!candidate.Contains('%', StringComparison.Ordinal))
                {
                    return candidate;
                }

                string decoded = Uri.UnescapeDataString(candidate);
                if (string.Equals(decoded, candidate, StringComparison.Ordinal))
                {
                    return null;
                }

                candidate = decoded;
            }

            return candidate.Normalize(NormalizationForm.FormKC);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (UriFormatException)
        {
            return null;
        }
    }

    private static string InspectionSkeleton(string value)
    {
        var skeleton = new StringBuilder(value.Length);
        foreach (Rune rune in value.Normalize(NormalizationForm.FormD).EnumerateRunes())
        {
            if (Rune.GetUnicodeCategory(rune) is UnicodeCategory.NonSpacingMark
                or UnicodeCategory.SpacingCombiningMark)
            {
                continue;
            }

            // Inspect common Greek/Cyrillic ASCII lookalikes and letter-like field dividers.
            // This representation is only used for safety checks; the original literal is returned.
            string replacement = Rune.ToLowerInvariant(rune).Value switch
            {
                0x01C0 or 0x2503 or 0x275A or 0x23D0 => "|",
                0x204F or 0x061B => ";",
                0x0430 or 0x03B1 => "a",
                0x0441 or 0x03F2 => "c",
                0x0435 or 0x03B5 => "e",
                0x0456 or 0x03B9 => "i",
                0x0458 => "j",
                0x043A or 0x03BA => "k",
                0x04CF => "l",
                0x043C => "m",
                0x043E or 0x03BF => "o",
                0x0440 or 0x03C1 => "p",
                0x0455 => "s",
                0x0442 or 0x03C4 => "t",
                0x0443 or 0x03C5 => "y",
                0x0445 or 0x03C7 => "x",
                _ => rune.ToString(),
            };
            skeleton.Append(replacement);
        }

        return skeleton.ToString();
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
