using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

/// <summary>Exercises the audit ownership boundary using the supported public event contract.</summary>
public sealed class TenantAuditOwnershipTests
{
    [Theory]
    [InlineData(nameof(TenantCreated), AuditEventCategory.Administrative, "tenant")]
    [InlineData(nameof(TenantUpdated), AuditEventCategory.Administrative, "tenant")]
    [InlineData(nameof(TenantDisabled), AuditEventCategory.Administrative, "tenant")]
    [InlineData(nameof(TenantEnabled), AuditEventCategory.Administrative, "tenant")]
    [InlineData(nameof(TenantConfigurationSet), AuditEventCategory.Administrative, "tenant")]
    [InlineData(nameof(TenantConfigurationRemoved), AuditEventCategory.Administrative, "tenant")]
    [InlineData(nameof(UserAddedToTenant), AuditEventCategory.Access, "tenant")]
    [InlineData(nameof(UserRemovedFromTenant), AuditEventCategory.Access, "tenant")]
    [InlineData(nameof(UserRoleChanged), AuditEventCategory.Access, "tenant")]
    [InlineData(nameof(GlobalAdministratorSet), AuditEventCategory.Access, "global-administrators")]
    [InlineData(nameof(GlobalAdministratorRemoved), AuditEventCategory.Access, "global-administrators")]
    public void KnownContractsEstablishExactOwnership(string eventType, AuditEventCategory category, string expectedOwner)
    {
        TenantAuditOwnership.GetOwner(Row(eventType, category), "system").ShouldBe(expectedOwner);
        if (expectedOwner == TenantAuditStreams.Tenant)
        {
            TenantAuditOwnership.GetOwner(Row(eventType, category) with { TenantId = "ordinary" }, "ordinary")
                .ShouldBe(TenantAuditStreams.Tenant);
        }
    }

    [Theory]
    [InlineData(null, "system", true)]
    [InlineData(null, "ordinary", true)]
    [InlineData("tenant", "ordinary", true)]
    [InlineData("tenant", "system", true)]
    [InlineData("global-administrators", "system", true)]
    [InlineData("global-administrators", "ordinary", false)]
    [InlineData("", "system", false)]
    [InlineData("Tenant", "system", false)]
    [InlineData("unknown", "system", false)]
    public void SelectorCannotExtendGlobalAuthorityOutsideSystem(string? selector, string tenantId, bool allowed)
        => TenantAuditOwnership.IsValidSelector(selector, tenantId).ShouldBe(allowed);

    [Theory]
    [InlineData("null-row")]
    [InlineData("missing-id")]
    [InlineData("blank-id")]
    [InlineData("missing-actor")]
    [InlineData("null-narrative")]
    [InlineData("wrong-tenant")]
    [InlineData("missing-tenant")]
    public void IncompleteOrForeignRowsCannotEstablishOwnership(string shape)
    {
        TenantAuditEntry? row = Row();
        row = shape switch
        {
            "null-row" => null,
            "missing-id" => row with { EventId = null! },
            "blank-id" => row with { EventId = " " },
            "missing-actor" => row with { ActorId = null! },
            "null-narrative" => row with { NarrativePayload = null! },
            "wrong-tenant" => row with { TenantId = "foreign" },
            "missing-tenant" => row with { TenantId = null! },
            _ => row,
        };
        TenantAuditOwnership.GetOwner(row, "system").ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("UnknownEvent")]
    [InlineData("Forged.TenantCreated")]
    [InlineData("Hexalith.Tenants.Contracts.Events.TenantCreated")]
    [InlineData("TenantCreatedSuffix")]
    [InlineData("InsufficientPermissionsRejection")]
    [InlineData("LastGlobalAdministratorRejection")]
    [InlineData("TenantNotFoundRejection")]
    public void UnknownNamesNamespacesAndRejectionsCannotBeAttributedAsSuccessAuditEvidence(string? eventType)
        => TenantAuditOwnership.GetOwner(Row() with { EventType = eventType! }, "system").ShouldBeNull();

    [Theory]
    [InlineData(nameof(TenantCreated), AuditEventCategory.Access, "system")]
    [InlineData(nameof(UserRemovedFromTenant), AuditEventCategory.Administrative, "system")]
    [InlineData(nameof(GlobalAdministratorSet), AuditEventCategory.Administrative, "system")]
    [InlineData(nameof(GlobalAdministratorSet), AuditEventCategory.Access, "ordinary")]
    [InlineData(nameof(GlobalAdministratorRemoved), AuditEventCategory.Access, "ordinary")]
    public void InconsistentCategoryOrGlobalTenantCannotEstablishOwnership(string type, AuditEventCategory category, string tenantId)
        => TenantAuditOwnership.GetOwner(Row(type, category) with { TenantId = tenantId }, tenantId).ShouldBeNull();

    private static TenantAuditEntry Row(string type = nameof(TenantCreated), AuditEventCategory category = AuditEventCategory.Administrative)
        => new("event-reference", type, category, "actor", DateTimeOffset.UtcNow, "system",
            new Dictionary<string, string> { ["name"] = "System tenant" });
}
