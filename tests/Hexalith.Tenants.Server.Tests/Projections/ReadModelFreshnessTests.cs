using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Server.Projections;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public class ReadModelFreshnessTests {
    private static readonly DateTimeOffset ProjectionTime = new(2026, 6, 25, 10, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DomainCreatedAt = new(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AuditEventTimestamp = new(2026, 2, 20, 9, 15, 0, TimeSpan.Zero);

    [Theory]
    [MemberData(nameof(FreshnessAwareReadModels))]
    public void Read_models_expose_persisted_freshness_metadata(IReadModelFreshness model) {
        ArgumentNullException.ThrowIfNull(model);

        model.ProjectedAt.ShouldBeNull();
        model.ProjectionVersion.ShouldBeNull();
    }

    public static IEnumerable<object[]> FreshnessAwareReadModels() {
        yield return [new TenantReadModel()];
        yield return [new TenantIndexReadModel()];
        yield return [new TenantAuditReadModel()];
        yield return [new GlobalAdministratorReadModel()];
    }

    [Fact]
    public void Tenant_read_model_projection_timestamp_is_not_domain_creation_time() {
        var model = new TenantReadModel();

        model.Apply(new TenantCreated("tenant-1", "Tenant One", null, DomainCreatedAt));

        model.CreatedAt.ShouldBe(DomainCreatedAt);
        model.ProjectedAt.ShouldBeNull();

        model.ProjectedAt = ProjectionTime;

        model.ProjectedAt.ShouldBe(ProjectionTime);
        model.CreatedAt.ShouldBe(DomainCreatedAt);
    }

    [Fact]
    public void Tenant_audit_model_projection_timestamp_is_not_audit_entry_timestamp() {
        var model = new TenantAuditReadModel();

        model.Apply(CreateEvent(new TenantCreated("tenant-1", "Tenant One", null, DomainCreatedAt), AuditEventTimestamp));

        model.Entries.Single().Timestamp.ShouldBe(AuditEventTimestamp);
        model.ProjectedAt.ShouldBeNull();

        model.ProjectedAt = ProjectionTime;

        model.ProjectedAt.ShouldBe(ProjectionTime);
        model.Entries.Single().Timestamp.ShouldBe(AuditEventTimestamp);
    }

    private static ProjectionEventDto CreateEvent(IEventPayload payload, DateTimeOffset timestamp) =>
        new(
            payload.GetType().FullName!,
            JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType()),
            "json",
            1,
            timestamp,
            "corr-1",
            "evt-1",
            "actor-1");
}
