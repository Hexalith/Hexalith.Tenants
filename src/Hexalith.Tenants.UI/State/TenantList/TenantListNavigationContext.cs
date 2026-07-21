using System.Globalization;

namespace Hexalith.Tenants.UI.State.TenantList;

/// <summary>
/// Builds tenant detail and audit links from canonical workspace state.
/// </summary>
/// <param name="WorkspaceState">The canonical workspace state to preserve in return links.</param>
public sealed record TenantListNavigationContext(TenantWorkspaceState WorkspaceState)
{
    /// <summary>
    /// Returns the canonical workspace URL.
    /// </summary>
    public string ToReturnUrl()
        => WorkspaceState.ToCanonicalUrl();

    /// <summary>
    /// Builds a tenant detail URL with canonical return state.
    /// </summary>
    public string ToDetailUrl(TenantListRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return ToDetailUrl(row.TenantId, $"tenant-row-{row.TenantId}");
    }

    /// <summary>
    /// Builds a tenant detail URL with canonical return state for a literal tenant identifier and
    /// return-focus anchor. Shared by the scope=all list grid and the scope=mine self-audit grid so both
    /// surfaces reuse the same detail route and restore their own selection/anchor on return.
    /// </summary>
    /// <param name="tenantId">The literal, caller-supplied tenant identifier.</param>
    /// <param name="anchor">The return-focus anchor matching the originating row element id.</param>
    public string ToDetailUrl(string tenantId, string anchor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(anchor);

        TenantWorkspaceState selected = WorkspaceState with
        {
            Cursor = null,
            SelectedTenantId = tenantId,
            Anchor = anchor,
        };
        string encodedTenantId = Uri.EscapeDataString(tenantId);
        string returnUrl = Uri.EscapeDataString(selected.ToCanonicalUrl());
        return string.Create(CultureInfo.InvariantCulture, $"/tenants/{encodedTenantId}?returnUrl={returnUrl}");
    }

    /// <summary>
    /// Builds a tenant audit URL with canonical return and focus state.
    /// </summary>
    public string ToAuditUrl(TenantListRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        TenantWorkspaceState selected = WorkspaceState with
        {
            Cursor = null,
            SelectedTenantId = row.TenantId,
            Anchor = $"tenant-row-{row.TenantId}",
        };
        string tenantId = Uri.EscapeDataString(row.TenantId);
        string returnUrl = Uri.EscapeDataString(selected.ToCanonicalUrl());
        return string.Create(CultureInfo.InvariantCulture, $"/tenants/{tenantId}/audit?source=tenant-list&returnUrl={returnUrl}&returnFocus={Uri.EscapeDataString(selected.Anchor)}");
    }
}
