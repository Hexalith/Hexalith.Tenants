namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Contains proof status without retaining a raw projection dictionary.
/// </summary>
public sealed class TenantConfigurationProjectionProof
{
    private TenantConfigurationProjectionProof(string tenantId, TenantConfigurationProjectionProofKind kind)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        TenantId = tenantId;
        Kind = kind;
    }

    /// <summary>Gets the literal tenant identifier associated with the proof.</summary>
    public string TenantId { get; }

    /// <summary>Gets the proof outcome.</summary>
    public TenantConfigurationProjectionProofKind Kind { get; }

    internal static TenantConfigurationProjectionProof Create(
        string tenantId,
        TenantConfigurationProjectionProofKind kind)
        => new(tenantId, kind);

    /// <summary>Creates an unavailable proof result.</summary>
    /// <param name="tenantId">Literal requested tenant identifier.</param>
    /// <returns>Unavailable proof.</returns>
    /// <summary>
    /// Returns a support-safe description that omits the tenant identifier and every configuration key or
    /// value. Without an override this class rendered as its own type name, so absence assertions written
    /// against it could never fail.
    /// </summary>
    /// <returns>A fixed-shape diagnostic string carrying no disclosable material.</returns>
    public override string ToString()
        => $"{nameof(TenantConfigurationProjectionProof)} {{ Kind = {Kind}, HasTenantId = {!string.IsNullOrEmpty(TenantId)} }}";

    public static TenantConfigurationProjectionProof Unavailable(string tenantId)
        => new(tenantId, TenantConfigurationProjectionProofKind.Unavailable);
}
