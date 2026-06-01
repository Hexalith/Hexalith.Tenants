using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Telemetry;

public class ServiceDefaultsTelemetryRegistrationTests {
    [Fact]
    public void ConfigureOpenTelemetry_ShouldRegisterTenantsMeterAndTraceSources() {
        string serviceDefaults = ReadRepositoryFile("src", "Hexalith.Tenants.ServiceDefaults", "Extensions.cs");

        serviceDefaults.ShouldContain(".AddMeter(\"Hexalith.Tenants\")");
        serviceDefaults.ShouldContain(".AddSource(\"Hexalith.Tenants\")");
        serviceDefaults.ShouldContain(".AddSource(\"Hexalith.EventStore\")");
    }

    [Fact]
    public void EventStorePublicationTelemetry_ShouldRemainVisibleThroughRegisteredSource() {
        string eventStoreActivitySource = ReadRepositoryFile(
            "Hexalith.EventStore",
            "src",
            "Hexalith.EventStore.Server",
            "Telemetry",
            "EventStoreActivitySource.cs");
        string eventPublisher = ReadRepositoryFile(
            "Hexalith.EventStore",
            "src",
            "Hexalith.EventStore.Server",
            "Events",
            "EventPublisher.cs");

        eventStoreActivitySource.ShouldContain("SourceName = \"Hexalith.EventStore\"");
        eventStoreActivitySource.ShouldContain("EventsPublish = \"EventStore.Events.Publish\"");
        eventPublisher.ShouldContain("EventStoreActivitySource.EventsPublish");
        eventPublisher.ShouldContain("EventId = 3100");
        eventPublisher.ShouldContain("EventId = 3101");
        eventPublisher.ShouldContain("DurationMs={DurationMs}");
        eventPublisher.ShouldNotContain("Payload={Payload}");
    }

    private static string ReadRepositoryFile(params string[] pathParts) =>
        File.ReadAllText(Path.Combine([FindRepositoryRoot(), .. pathParts]));

    private static string FindRepositoryRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null) {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.Tenants.slnx"))) {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
    }
}
