using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.Contracts.Events;

namespace Hexalith.Tenants.Server.Projections;

public sealed class GlobalAdministratorReadModel : IReadModelFreshness {
    public HashSet<string> Administrators { get; set; } = [];
    public DateTimeOffset? ProjectedAt { get; set; }
    public string? ProjectionVersion { get; set; }

    public void Apply(GlobalAdministratorSet e) {
        ArgumentNullException.ThrowIfNull(e);
        _ = Administrators.Add(e.UserId);
    }

    public void Apply(GlobalAdministratorRemoved e) {
        ArgumentNullException.ThrowIfNull(e);
        _ = Administrators.Remove(e.UserId);
    }
}
