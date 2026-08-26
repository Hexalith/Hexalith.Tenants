using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Contains support-safe identity for one exact set-configuration intent.</summary>
/// <param name="TenantId">Literal tenant identifier.</param>
/// <param name="NamespacePrefix">Literal server-reflected namespace prefix.</param>
/// <param name="KeySuffix">Literal non-empty key suffix.</param>
/// <param name="FullKey">Literal composed full key.</param>
/// <param name="ValueFingerprint">Ordinal fingerprint of the intended value.</param>
public sealed record TenantSetConfigurationIntent(
    string TenantId,
    string NamespacePrefix,
    string KeySuffix,
    string FullKey,
    string ValueFingerprint)
{
    /// <summary>Gets a fixed fingerprint binding proof to this tenant, key, and value.</summary>
    internal string AttemptFingerprint
    {
        get
        {
            string material = string.Create(
                CultureInfo.InvariantCulture,
                $"{TenantId.Length}:{TenantId}|{NamespacePrefix.Length}:{NamespacePrefix}|{KeySuffix.Length}:{KeySuffix}|{FullKey.Length}:{FullKey}|{ValueFingerprint.Length}:{ValueFingerprint}");
            return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
        }
    }

    /// <summary>Returns a support-safe description that omits identity, key, and fingerprint material.</summary>
    /// <returns>A fixed-shape diagnostic description.</returns>
    public override string ToString()
        => $"{nameof(TenantSetConfigurationIntent)} {{ HasTenantId = {!string.IsNullOrEmpty(TenantId)}, HasKey = {!string.IsNullOrEmpty(FullKey)}, HasValueFingerprint = {!string.IsNullOrEmpty(ValueFingerprint)} }}";
}
