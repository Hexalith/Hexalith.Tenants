using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Testing.Fakes;
using Hexalith.Tenants.Testing.Helpers;

using Shouldly;

namespace Hexalith.Tenants.Testing.Tests.Helpers;

public class TenantTestHelpersTests
{
    // ─── 4.2: CreateTenant returns success with TenantCreated event ───

    [Fact]
    public void CreateTenant_returns_success_with_TenantCreated_event()
    {
        var svc = new InMemoryTenantService();

        var result = TenantTestHelpers.CreateTenant(svc, "acme", "Acme Corp", "Test desc");

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        var evt = result.Events[0].ShouldBeOfType<TenantCreated>();
        evt.TenantId.ShouldBe("acme");
        evt.Name.ShouldBe("Acme Corp");
        evt.Description.ShouldBe("Test desc");
    }

    // ─── 4.3: CreateTenantWithOwner returns success with both events ───

    [Fact]
    public void CreateTenantWithOwner_returns_both_TenantCreated_and_UserAddedToTenant_events()
    {
        var svc = new InMemoryTenantService();

        var (createResult, addOwnerResult) = TenantTestHelpers.CreateTenantWithOwner(svc, "acme", "owner-user", "Acme Corp");

        createResult.IsSuccess.ShouldBeTrue();
        createResult.Events[0].ShouldBeOfType<TenantCreated>();

        addOwnerResult.IsSuccess.ShouldBeTrue();
        addOwnerResult.Events[0].ShouldBeOfType<UserAddedToTenant>();
        var addEvt = (UserAddedToTenant)addOwnerResult.Events[0];
        addEvt.UserId.ShouldBe("owner-user");
    }

    // ─── 4.4: BootstrapGlobalAdmin returns success with GlobalAdministratorSet event ───

    [Fact]
    public void BootstrapGlobalAdmin_returns_success_with_GlobalAdministratorSet_event()
    {
        var svc = new InMemoryTenantService();

        var result = TenantTestHelpers.BootstrapGlobalAdmin(svc, "admin-user");

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        result.Events[0].ShouldBeOfType<GlobalAdministratorSet>();
        var evt = (GlobalAdministratorSet)result.Events[0];
        evt.UserId.ShouldBe("admin-user");
    }

    // ─── 4.5: CreateCommandEnvelope builds valid CommandEnvelope ───

    [Fact]
    public void CreateCommandEnvelope_builds_valid_envelope_with_correct_fields()
    {
        var command = new Hexalith.Tenants.Contracts.Commands.CreateTenant("acme", "Acme Corp", null);

        var envelope = TenantTestHelpers.CreateCommandEnvelope(
            command,
            aggregateId: "acme",
            userId: "test-user",
            isGlobalAdmin: true);

        envelope.ShouldNotBeNull();
        envelope.TenantId.ShouldBe("system");
        envelope.Domain.ShouldBe("tenants");
        envelope.AggregateId.ShouldBe("acme");
        envelope.CommandType.ShouldBe("CreateTenant");
        envelope.UserId.ShouldBe("test-user");
        envelope.MessageId.ShouldNotBeNullOrWhiteSpace();
        envelope.CorrelationId.ShouldNotBeNullOrWhiteSpace();
        envelope.Payload.ShouldNotBeNull();
        envelope.Payload.Length.ShouldBeGreaterThan(0);
        envelope.Extensions.ShouldNotBeNull();
        envelope.Extensions!["actor:globalAdmin"].ShouldBe("true");
    }

    [Fact]
    public void CreateCommandEnvelope_without_globalAdmin_has_null_extensions()
    {
        var command = new Hexalith.Tenants.Contracts.Commands.CreateTenant("acme", "Acme Corp", null);

        var envelope = TenantTestHelpers.CreateCommandEnvelope(
            command,
            aggregateId: "acme",
            userId: "test-user",
            isGlobalAdmin: false);

        envelope.Extensions.ShouldBeNull();
    }
}
