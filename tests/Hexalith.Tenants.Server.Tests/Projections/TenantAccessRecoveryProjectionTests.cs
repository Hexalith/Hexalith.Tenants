using System.Text.Json;

using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Projections;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Server.Tests.Routing;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public sealed class TenantAccessRecoveryProjectionTests
{
    [Fact]
    public async Task FullReplayRecognizesRefreshAsAuditEvidenceWithoutChangingBusinessReadModels()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        object[] payloads = [new TenantCreated("acme", "Unchanged", "description", now),
            new UserAddedToTenant("acme", "owner", TenantRole.TenantOwner),
            new TenantConfigurationSet("acme", "secret", "retained-value"),
            new TenantAccessRefreshed("acme", TenantStatus.Active,
                new Dictionary<string, TenantRole> { ["owner"] = TenantRole.TenantOwner }, ["secret"])];
        ProjectionEventDto[] events = payloads.Select((payload, index) => new ProjectionEventDto(payload.GetType().FullName!,
            JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType()), "json", index + 1, now, "correlation", "event-" + index, "owner")).ToArray();
        CatalogReadModelStore backend = new();
        _ = await new TenantProjectionHandler(backend).ProjectAsync(new("system", "tenants", "acme", events), TestContext.Current.CancellationToken);
        TenantReadModel saved = JsonSerializer.Deserialize<TenantReadModel>(backend.Contents["projection:tenants:acme"])!;
        saved.Name.ShouldBe("Unchanged");
        saved.Description.ShouldBe("description");
        saved.Status.ShouldBe(TenantStatus.Active);
        saved.Members["owner"].ShouldBe(TenantRole.TenantOwner);
        saved.Configuration["secret"].ShouldBe("retained-value");
        saved.ProjectionVersion.ShouldBe("tenant-sequence:4");
        TenantAuditReadModel audit = JsonSerializer.Deserialize<TenantAuditReadModel>(backend.Contents["audit:acme"])!;
        audit.Entries.ShouldContain(entry => entry.EventType == nameof(TenantAccessRefreshed));
        audit.ProjectionVersion.ShouldBe("tenant-sequence:4");
    }
}
