using System.Reflection;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Rest;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Identity;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests.CommandContracts;

public sealed class CommandContractRestMetadataTests
{
    [Fact]
    public void TenantAggregateCommands_DefineRestContractMetadata()
    {
        AssertTenantCommand(new CreateTenant("tenant-a", "Tenant A", "Description"), "create-tenant", RestVerb.Post, "{tenantId}");
        AssertTenantCommand(new UpdateTenant("tenant-a", "Tenant A", "Description"), "update-tenant", RestVerb.Put, "{tenantId}");
        AssertTenantCommand(new EnableTenant("tenant-a"), "enable-tenant", RestVerb.Post, "{tenantId}/enable");
        AssertTenantCommand(new DisableTenant("tenant-a"), "disable-tenant", RestVerb.Post, "{tenantId}/disable");
        AssertTenantCommand(new AddUserToTenant("tenant-a", "user-a", Contracts.Enums.TenantRole.TenantReader), "add-user-to-tenant", RestVerb.Post, "{tenantId}/users/{userId}/add");
        AssertTenantCommand(new ChangeUserRole("tenant-a", "user-a", Contracts.Enums.TenantRole.TenantOwner), "change-user-role", RestVerb.Patch, "{tenantId}/users/{userId}/role");
        AssertTenantCommand(new RemoveUserFromTenant("tenant-a", "user-a"), "remove-user-from-tenant", RestVerb.Post, "{tenantId}/users/{userId}/remove");
        AssertTenantCommand(new SetTenantConfiguration("tenant-a", "billing.mode", "enterprise"), "set-tenant-configuration", RestVerb.Put, "{tenantId}/configuration/{key}");
        AssertTenantCommand(new RemoveTenantConfiguration("tenant-a", "billing.mode"), "remove-tenant-configuration", RestVerb.Post, "{tenantId}/configuration/{key}/remove");
    }

    [Fact]
    public void GlobalAdministratorCommands_DefineRestContractMetadata()
    {
        AssertGlobalAdministratorCommand(new SetGlobalAdministrator("user-a"), "set-global-administrator", RestVerb.Post, "~/api/global-administrators/{userId}/set");
        AssertGlobalAdministratorCommand(new RemoveGlobalAdministrator("user-a"), "remove-global-administrator", RestVerb.Post, "~/api/global-administrators/{userId}/remove");
    }

    [Fact]
    public void BootstrapGlobalAdmin_IsContractAddressableButNotExternallyRouted()
    {
        var command = new BootstrapGlobalAdmin("user-a");

        typeof(ICommandContract).IsAssignableFrom(typeof(BootstrapGlobalAdmin)).ShouldBeTrue();
        BootstrapGlobalAdmin.Domain.ShouldBe(TenantIdentity.GlobalAdministratorsDomain);
        BootstrapGlobalAdmin.CommandType.ShouldBe("bootstrap-global-admin");
        command.AggregateId.ShouldBe(TenantIdentity.GlobalAdministratorsAggregateId);
        typeof(BootstrapGlobalAdmin).GetCustomAttribute<RestRouteAttribute>().ShouldBeNull();
    }

    private static void AssertTenantCommand<TCommand>(
        TCommand command,
        string commandType,
        RestVerb verb,
        string template)
        where TCommand : ICommandContract
    {
        TCommand.Domain.ShouldBe(TenantIdentity.Domain);
        TCommand.CommandType.ShouldBe(commandType);
        command.AggregateId.ShouldBe("tenant-a");
        AssertRoute<TCommand>(verb, template);
    }

    private static void AssertGlobalAdministratorCommand<TCommand>(
        TCommand command,
        string commandType,
        RestVerb verb,
        string template)
        where TCommand : ICommandContract
    {
        TCommand.Domain.ShouldBe(TenantIdentity.GlobalAdministratorsDomain);
        TCommand.CommandType.ShouldBe(commandType);
        command.AggregateId.ShouldBe(TenantIdentity.GlobalAdministratorsAggregateId);
        AssertRoute<TCommand>(verb, template);
    }

    private static void AssertRoute<TCommand>(RestVerb verb, string template)
    {
        RestRouteAttribute? route = typeof(TCommand).GetCustomAttribute<RestRouteAttribute>();
        route.ShouldNotBeNull();
        route.Verb.ShouldBe(verb);
        route.Template.ShouldBe(template);
    }
}
