namespace Hexalith.Tenants.Projections;

internal sealed record ProjectionWriteDiagnosticsContext(
    string TenantId,
    string Domain,
    string AggregateId,
    string ProjectionType) {
    public const string CausationIdStatusUnavailable = "unavailable-from-projection-dto";
}
