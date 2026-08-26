using System.Security.Cryptography;
using System.Text;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Creates ordinal fingerprints used only for configuration proof comparison.</summary>
internal static class TenantSetConfigurationValueFingerprint
{
    private static readonly byte[] ProcessKey = RandomNumberGenerator.GetBytes(32);
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Creates a process-bound fingerprint of the exact UTF-8 value.</summary>
    /// <param name="value">Literal configuration value.</param>
    /// <returns>Lower-case, process-bound HMAC-SHA-256 fingerprint.</returns>
    internal static string Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        using var hmac = new HMACSHA256(ProcessKey);
        return Convert.ToHexStringLower(hmac.ComputeHash(StrictUtf8.GetBytes(value)));
    }
}
