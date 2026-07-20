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

        TenantWorkspaceState selected = WorkspaceState with
        {
            Cursor = null,
            SelectedTenantId = row.TenantId,
            Anchor = $"tenant-row-{row.TenantId}",
        };
        string tenantId = Uri.EscapeDataString(row.TenantId);
        string returnUrl = Uri.EscapeDataString(selected.ToCanonicalUrl());
        return string.Create(CultureInfo.InvariantCulture, $"/tenants/{tenantId}?returnUrl={returnUrl}");
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
