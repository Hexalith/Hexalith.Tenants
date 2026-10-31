using System.Reflection;

using Hexalith.EventStore.Contracts.Routing;
using Hexalith.Tenants.Contracts.Routing;
using Hexalith.Tenants.Server.Routing;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests;

/// <summary>Qualifies owner enrollment independently of conditional catalog test compilation.</summary>
public class PublishedTenantCatalogTests
{
    /// <summary>Matches the compiled owner artifacts to capabilities present in the selected SDK.</summary>
    [Fact]
    public void CompiledEnrollmentMatchesTheSelectedSdkAvailability()
    {
        const string declarationAttribute = "Hexalith.EventStore.Contracts.Routing.RouteCatalogDeclarationSourceAttribute";
        const string adapterAttribute = "Hexalith.EventStore.DomainService.RouteCatalogAdapterSourceAttribute";
        Assembly contracts = typeof(TenantCatalogDeclarations).Assembly;
        Assembly server = typeof(TenantIntentAdapters).Assembly;
        CustomAttributeData[] declarations = CustomAttributeData.GetCustomAttributes(contracts)
            .Where(attribute => attribute.AttributeType.FullName == declarationAttribute).ToArray();
        CustomAttributeData[] adapters = CustomAttributeData.GetCustomAttributes(server)
            .Where(attribute => attribute.AttributeType.FullName == adapterAttribute).ToArray();
        PropertyInfo? operations = typeof(TenantCatalogDeclarations).GetProperty("Operations");

        // Read the actual SDK surface, independently of the build's catalog symbol.
        // Removing the symbol must fail a normal package build rather than hide this test.
        if (typeof(RouteContractDeclaration).Assembly.GetType(declarationAttribute) is null)
        {
            declarations.ShouldBeEmpty();
            adapters.ShouldBeEmpty();
            operations.ShouldBeNull();
            return;
        }

        declarations.ShouldHaveSingleItem().ConstructorArguments.ShouldHaveSingleItem()
            .Value.ShouldBe(typeof(TenantCatalogDeclarations));
        adapters.ShouldHaveSingleItem().ConstructorArguments.ShouldHaveSingleItem()
            .Value.ShouldBe(typeof(TenantIntentAdapters));
        operations.ShouldNotBeNull();
        ((System.Collections.IEnumerable)operations.GetValue(null)!).Cast<object>().Count()
            .ShouldBe(TenantCatalogDeclarations.Routes.Count);
    }
}
