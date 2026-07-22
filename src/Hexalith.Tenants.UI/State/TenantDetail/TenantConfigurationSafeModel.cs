using System.Collections.ObjectModel;

namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Contains only configuration rows approved before Razor-facing state construction.
/// </summary>
public sealed class TenantConfigurationSafeModel
{
    private TenantConfigurationSafeModel(
        string tenantId,
        bool isAvailable,
        bool isDegraded,
        IEnumerable<TenantConfigurationSafeRow> rows)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(rows);
        TenantId = tenantId;
        IsAvailable = isAvailable;
        IsDegraded = isDegraded;
        Rows = new ReadOnlyCollection<TenantConfigurationSafeRow>(rows.ToArray());
    }

    /// <summary>Gets the literal tenant identifier.</summary>
    public string TenantId { get; }

    /// <summary>Gets whether policy and principal evidence were usable.</summary>
    public bool IsAvailable { get; }

    /// <summary>Gets whether rows were retained from last-confirmed safe state.</summary>
    public bool IsDegraded { get; }

    /// <summary>Gets defensively copied approved rows.</summary>
    public IReadOnlyList<TenantConfigurationSafeRow> Rows { get; }

    internal static TenantConfigurationSafeModel Available(
        string tenantId,
        IEnumerable<TenantConfigurationSafeRow> rows,
        bool isDegraded = false)
        => new(tenantId, true, isDegraded, rows);

    internal static TenantConfigurationSafeModel Unavailable(string tenantId)
        => new(tenantId, false, false, []);
}
