using Hexalith.EventStore.Server.Configuration;

using Microsoft.Extensions.Configuration;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Configuration;

public class SnapshotConfigurationTests {
    [Fact]
    public void AppSettings_SnapshotConfiguration_ShouldLoadCorrectly() {
        // Arrange
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var options = new SnapshotOptions();
        configuration.GetSection("EventStore:Snapshots").Bind(options);

        // Act & Assert
        options.DomainIntervals.ShouldContainKey("tenants");
        options.DomainIntervals["tenants"].ShouldBe(50);
        options.DefaultInterval.ShouldBe(100);
    }

    [Fact]
    public void AppSettings_SnapshotConfiguration_ShouldLeaveGlobalAdministratorsOnEventStoreDefault() {
        // Story 7.5 AC4: the global administrator singleton state must use the EventStore default
        // snapshot interval (100) — there is no per-domain override for it — so only the tenants
        // domain carries the documented 50-event interval.
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var options = new SnapshotOptions();
        configuration.GetSection("EventStore:Snapshots").Bind(options);

        options.DomainIntervals.ShouldNotContainKey("global-administrators");
        options.DefaultInterval.ShouldBe(100);
    }

    [Fact]
    public void AppSettings_SnapshotConfiguration_ShouldPassValidation() {
        // Arrange
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var options = new SnapshotOptions();
        configuration.GetSection("EventStore:Snapshots").Bind(options);

        // Act & Assert - Should not throw
        Should.NotThrow(options.Validate);
    }

    [Fact]
    public void AppSettings_SnapshotConfiguration_ShouldNotHaveTenantDomainIntervals() {
        // Arrange
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var options = new SnapshotOptions();
        configuration.GetSection("EventStore:Snapshots").Bind(options);

        // Act & Assert - No per-tenant-domain overrides should be set
        _ = options.TenantDomainIntervals.ShouldNotBeNull();
        options.TenantDomainIntervals.ShouldBeEmpty();
    }
}
