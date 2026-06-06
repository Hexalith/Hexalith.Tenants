using System.Globalization;
using System.Text;

using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.UI.State.TenantList;

public sealed record TenantListNavigationContext(
    string? Search,
    string? Status,
    string SortColumn,
    bool SortDescending,
    string? Cursor,
    string? SelectedTenantId,
    string? Anchor)
{
    public string ToReturnUrl()
    {
        StringBuilder builder = new("/tenants");
        AppendQuery(builder, "search", Search);
        AppendQuery(builder, "status", Status);
        AppendQuery(builder, "sort", SortColumn == TenantListSortColumns.TenantId ? null : SortColumn);
        AppendQuery(builder, "desc", SortDescending ? bool.TrueString : null);
        AppendQuery(builder, "cursor", Cursor);
        AppendQuery(builder, "selected", SelectedTenantId);
        AppendQuery(builder, "anchor", Anchor);
        return builder.ToString();
    }

    public string ToDetailUrl(TenantListRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        TenantListNavigationContext selected = this with
        {
            SelectedTenantId = row.TenantId,
            Anchor = $"tenant-row-{row.TenantId}",
        };
        string tenantId = Uri.EscapeDataString(row.TenantId);
        string returnUrl = Uri.EscapeDataString(selected.ToReturnUrl());
        return string.Create(CultureInfo.InvariantCulture, $"/tenants/{tenantId}?returnUrl={returnUrl}");
    }

    public string ToAuditUrl(TenantListRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        TenantListNavigationContext selected = this with
        {
            SelectedTenantId = row.TenantId,
            Anchor = $"tenant-row-{row.TenantId}",
        };
        string tenantId = Uri.EscapeDataString(row.TenantId);
        string returnUrl = Uri.EscapeDataString(selected.ToReturnUrl());
        return string.Create(CultureInfo.InvariantCulture, $"/tenants/{tenantId}/audit?source=tenant-list&returnUrl={returnUrl}&returnFocus={Uri.EscapeDataString(selected.Anchor)}");
    }

    private static void AppendQuery(StringBuilder builder, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        builder.Append(builder.Length > "/tenants".Length ? '&' : '?');
        builder.Append(Uri.EscapeDataString(key));
        builder.Append('=');
        builder.Append(Uri.EscapeDataString(value));
    }
}
