using System.Buffers;
using System.Globalization;
using System.Text;

namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

/// <summary>Creates an injective, visibly literal representation of a global-administrator identity.</summary>
public static class GlobalAdministratorIdentityDisplay
{
    /// <summary>Encodes invisible scalars and escapes literal backslashes for safe operator display.</summary>
    /// <param name="userId">Original literal identity.</param>
    /// <returns>A display-only representation. The original value must still be used for commands and copying.</returns>
    public static string Encode(string? userId)
        => EncodeCore(userId, tokenizeWhitespace: false);

    internal static string EncodeAccessible(string? userId)
        => EncodeCore(userId, tokenizeWhitespace: true);

    private static string EncodeCore(string? userId, bool tokenizeWhitespace)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return userId ?? string.Empty;
        }

        StringBuilder result = new(userId.Length);
        ReadOnlySpan<char> remaining = userId.AsSpan();
        while (!remaining.IsEmpty)
        {
            if (remaining[0] == '\\')
            {
                result.Append("\\\\");
                remaining = remaining[1..];
                continue;
            }

            OperationStatus status = Rune.DecodeFromUtf16(remaining, out Rune rune, out int consumed);
            if (status is not OperationStatus.Done)
            {
                AppendScalarToken(result, remaining[0]);
                remaining = remaining[1..];
                continue;
            }

            if (ShouldTokenize(rune) || tokenizeWhitespace && Rune.IsWhiteSpace(rune))
            {
                AppendScalarToken(result, rune.Value);
            }
            else
            {
                result.Append(rune.ToString());
            }

            remaining = remaining[consumed..];
        }

        return result.ToString();
    }

    private static bool ShouldTokenize(Rune rune)
    {
        int scalar = rune.Value;
        return Rune.GetUnicodeCategory(rune) is UnicodeCategory.Control or UnicodeCategory.Format
            || scalar is 0x034F
            || scalar is >= 0x115F and <= 0x1160
            || scalar is >= 0x17B4 and <= 0x17B5
            || scalar is >= 0x180B and <= 0x180F
            || scalar is 0x3164
            || scalar is >= 0xFE00 and <= 0xFE0F
            || scalar is 0xFFA0
            || scalar is >= 0xFFF0 and <= 0xFFF8
            || scalar is >= 0x1BCA0 and <= 0x1BCA3
            || scalar is >= 0x1D173 and <= 0x1D17A
            || scalar is >= 0xE0000 and <= 0xE0FFF;
    }

    private static void AppendScalarToken(StringBuilder result, int scalar)
    {
        result.Append("\\{U+");
        result.Append(scalar.ToString(
            scalar <= char.MaxValue ? "X4" : "X8",
            CultureInfo.InvariantCulture));
        result.Append('}');
    }
}
