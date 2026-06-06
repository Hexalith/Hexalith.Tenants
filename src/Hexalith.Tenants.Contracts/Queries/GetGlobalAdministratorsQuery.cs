using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Query contract for listing global administrators from the fixed platform authority scope.
/// </summary>
public sealed class GetGlobalAdministratorsQuery : IQueryContract {
    public static string QueryType => "get-global-administrators";

    public static string Domain => "global-administrators";

    public static string ProjectionType => "global-administrators";
}
