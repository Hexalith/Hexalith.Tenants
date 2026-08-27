using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Contains support-safe identity for one exact remove-configuration intent.</summary>
/// <param name="TenantId">Literal tenant identifier.</param>
/// <param name="NamespacePrefix">Literal server-reflected namespace prefix.</param>
/// <param name="FullKey">Literal configuration key.</param>
public sealed record TenantRemoveConfigurationIntent(
    string TenantId,
    string NamespacePrefix,
    string FullKey)
{
    /// <summary>Gets a fixed fingerprint binding proof to this tenant and key.</summary>
    internal string AttemptFingerprint
    {
        get
        {
            string material = string.Create(
                CultureInfo.InvariantCulture,
                $"{TenantId.Length}:{TenantId}|{NamespacePrefix.Length}:{NamespacePrefix}|{FullKey.Length}:{FullKey}");
            return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
        }
    }

    /// <summary>Returns a support-safe description that omits identity and key material.</summary>
    /// <returns>A fixed-shape diagnostic description.</returns>
    public override string ToString()
        => $"{nameof(TenantRemoveConfigurationIntent)} {{ HasTenantId = {!string.IsNullOrEmpty(TenantId)}, HasKey = {!string.IsNullOrEmpty(FullKey)} }}";
}
