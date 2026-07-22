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
    public static TenantConfigurationProjectionProof Unavailable(string tenantId)
        => new(tenantId, TenantConfigurationProjectionProofKind.Unavailable);
}
