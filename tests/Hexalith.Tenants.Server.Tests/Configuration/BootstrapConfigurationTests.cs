using Hexalith.Tenants.Contracts.Identity;

using Microsoft.Extensions.Configuration;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Configuration;

public class BootstrapConfigurationTests {
    [Fact]
    public void AppSettings_registers_global_administrators_domain_service() {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        IConfigurationSection section = configuration.GetSection(
            "EventStore:DomainServices:Registrations:system|global-administrators|v1");

        section.Exists().ShouldBeTrue();
        section["AppId"].ShouldBe("tenants");
        section["MethodName"].ShouldBe("process");
        section["TenantId"].ShouldBe(TenantIdentity.DefaultTenantId);
        section["Domain"].ShouldBe(TenantIdentity.GlobalAdministratorsDomain);
        section["Version"].ShouldBe("v1");
    }

    [Fact]
    public void Program_does_not_map_public_bootstrap_routes() {
        string source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "Hexalith.Tenants",
            "Program.cs"));

        source.ShouldNotContain("MapPost(\"/bootstrap\"", Case.Insensitive);
        source.ShouldNotContain("MapPost(\"/global-admin/bootstrap\"", Case.Insensitive);
        source.ShouldNotContain("MapPost(\"/global-administrators/bootstrap\"", Case.Insensitive);
        source.ShouldNotContain("[Route(\"bootstrap", Case.Insensitive);
        source.ShouldNotContain("[Route(\"global-admin/bootstrap", Case.Insensitive);
        source.ShouldNotContain("[Route(\"global-administrators/bootstrap", Case.Insensitive);
    }
}
