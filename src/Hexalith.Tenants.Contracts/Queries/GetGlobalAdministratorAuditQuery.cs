using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Rest;
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>Reads the retained global-administrator audit owner through an explicit platform scope.</summary>
[RestRoute(RestVerb.Get, "~/api/global-administrators/audit", ApiScope = "tenants")]
[RestQueryBinding(RestQueryBindingSource.Constant, "system", RestQueryBindingSource.Constant, "system")]
public sealed class GetGlobalAdministratorAuditQuery : IQueryContract
{
    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    public AuditEventCategory? Category { get; init; }

    public string? Cursor { get; init; }

    public int PageSize { get; init; }

    public static string QueryType => "get-global-administrator-audit";

    public static string Domain => "tenants";

    public static string ProjectionType => "tenants";
}
