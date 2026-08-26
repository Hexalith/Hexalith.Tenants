using System.Security.Cryptography;
using System.Text;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Creates ordinal fingerprints used only for configuration proof comparison.</summary>
internal static class TenantSetConfigurationValueFingerprint
{
    /// <summary>Creates a deterministic fingerprint of the exact UTF-8 value.</summary>
    /// <param name="value">Literal configuration value.</param>
    /// <returns>Lower-case SHA-256 fingerprint.</returns>
    internal static string Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
