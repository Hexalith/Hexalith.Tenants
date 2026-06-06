using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantList;

namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

public sealed record GlobalAdministratorRow(
    string UserId,
    TenantFreshnessState Freshness)
{
    public static GlobalAdministratorRow FromSummary(GlobalAdministratorSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        return new(summary.UserId, TenantFreshnessState.Unknown);
    }
}
