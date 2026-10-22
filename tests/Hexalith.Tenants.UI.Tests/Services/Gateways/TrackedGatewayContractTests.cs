using System.Reflection;

using Hexalith.Tenants.UI.Services.Gateways;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

public sealed class TrackedGatewayContractTests
{
    [Fact]
    public void ConfigurationProofReadersRequireExplicitImplementations()
    {
        MethodInfo[] methods = typeof(ITenantQueryGateway).GetMethods()
            .Where(method => method.Name is "GetSetConfigurationProjectionProofAsync" or "GetRemoveConfigurationProjectionProofAsync")
            .ToArray();
        methods.Length.ShouldBe(4);
        methods.ShouldAllBe(method => method.IsAbstract);
    }

    [Theory]
    [InlineData("SetTenantConfiguration")]
    [InlineData("RemoveTenantConfiguration")]
    [InlineData("SetGlobalAdministrator")]
    [InlineData("RemoveGlobalAdministrator")]
    [InlineData("EnableTenant")]
    [InlineData("DisableTenant")]
    public void LegacyWrappersNameTheirRequiredIdentityReplacement(string operation)
    {
        foreach (Type gateway in new[] { typeof(ITenantCommandGateway), typeof(TenantCommandGateway), typeof(UnavailableTenantCommandGateway) })
        {
            MethodInfo method = gateway.GetMethod(operation + "Async").ShouldNotBeNull();
            ObsoleteAttribute obsolete = method.GetCustomAttribute<ObsoleteAttribute>().ShouldNotBeNull();
            obsolete.IsError.ShouldBeFalse();
            obsolete.Message.ShouldNotBeNull().ShouldContain(operation + "TrackedAsync");
            MethodInfo tracked = gateway.GetMethod(operation + "TrackedAsync").ShouldNotBeNull();
            ParameterInfo identity = tracked.GetParameters().Single(parameter => parameter.Name == "messageId");
            identity.IsOptional.ShouldBeFalse();
        }
    }
}
