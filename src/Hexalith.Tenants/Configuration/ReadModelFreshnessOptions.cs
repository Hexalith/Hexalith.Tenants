using Hexalith.EventStore.Client.Projections;

namespace Hexalith.Tenants.Configuration;

public sealed class ReadModelFreshnessOptions {
    public const string SectionName = "Tenants:ReadModelFreshness";

    public TimeSpan Aging { get; set; } = TimeSpan.FromDays(365);
    public TimeSpan Stale { get; set; } = TimeSpan.FromDays(3650);

    public ReadModelFreshnessThresholds ToThresholds() =>
        ReadModelFreshnessThresholds.Create(Aging, Stale);
}
