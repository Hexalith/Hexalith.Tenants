using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Contains proof status without retaining a raw projection dictionary.
/// </summary>
public sealed class TenantConfigurationProjectionProof
{
    private TenantConfigurationProjectionProof(
        string tenantId,
        TenantConfigurationProjectionProofKind kind,
        string? projectionVersion,
        string? attemptFingerprint)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        TenantId = tenantId;
        Kind = kind;
        ProjectionVersion = projectionVersion;
        AttemptFingerprint = attemptFingerprint;
    }

    /// <summary>Gets the literal tenant identifier associated with the proof.</summary>
    public string TenantId { get; }

    /// <summary>Gets the proof outcome.</summary>
    public TenantConfigurationProjectionProofKind Kind { get; }

    /// <summary>Gets the ordered projection version captured with this proof.</summary>
    public string? ProjectionVersion { get; }

    /// <summary>Gets the non-reversible fingerprint binding this proof to one exact safe intent.</summary>
    internal string? AttemptFingerprint { get; }

    internal static TenantConfigurationProjectionProof Create(
        string tenantId,
        TenantConfigurationProjectionProofKind kind,
        string? projectionVersion = null,
        string? attemptFingerprint = null)
        => new(tenantId, kind, projectionVersion, attemptFingerprint);

    /// <summary>Checks that proof tenant and fingerprint match one exact safe intent.</summary>
    internal bool Matches(TenantSetConfigurationIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        return string.Equals(TenantId, intent.TenantId, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(AttemptFingerprint)
            && string.Equals(AttemptFingerprint, intent.AttemptFingerprint, StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns a support-safe description that omits the tenant identifier and every configuration key or
    /// value. Without an override this class rendered as its own type name, so absence assertions written
    /// against it could never fail.
    /// </summary>
    /// <returns>A fixed-shape diagnostic string carrying no disclosable material.</returns>
    public override string ToString()
        => $"{nameof(TenantConfigurationProjectionProof)} {{ Kind = {Kind}, HasTenantId = {!string.IsNullOrEmpty(TenantId)} }}";

    /// <summary>Creates an unavailable proof result.</summary>
    /// <param name="tenantId">Literal requested tenant identifier.</param>
    /// <returns>Unavailable proof.</returns>
    public static TenantConfigurationProjectionProof Unavailable(string tenantId)
        => new(tenantId, TenantConfigurationProjectionProofKind.Unavailable, projectionVersion: null, attemptFingerprint: null);
}
