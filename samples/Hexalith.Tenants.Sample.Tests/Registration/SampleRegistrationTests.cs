using Shouldly;

namespace Hexalith.Tenants.Sample.Tests.Registration;

public class SampleRegistrationTests {
    [Fact]
    public void Program_standard_tenant_event_registration_stays_under_twenty_meaningful_lines() {
        // Arrange
        string program = File.ReadAllText(SampleProgramPath());
        string[] meaningfulLines = program
            .Split(Environment.NewLine)
            .Select(line => line.Trim())
            .Where(IsTenantRegistrationLine)
            .ToArray();

        // Act
        int count = meaningfulLines.Length;

        // Assert
        count.ShouldBeLessThan(20);
        meaningfulLines.ShouldContain(line => line.Contains("AddHexalithTenants", StringComparison.Ordinal));
        meaningfulLines.ShouldContain(line => line.Contains("AddTenantEventHandler<UserAddedToTenant, SampleLoggingEventHandler>", StringComparison.Ordinal));
        meaningfulLines.ShouldContain(line => line.Contains("UseCloudEvents", StringComparison.Ordinal));
        meaningfulLines.ShouldContain(line => line.Contains("MapSubscribeHandler", StringComparison.Ordinal));
        meaningfulLines.ShouldContain(line => line.Contains("MapTenantEventSubscription", StringComparison.Ordinal));
    }

    private static bool IsTenantRegistrationLine(string line) =>
        !string.IsNullOrWhiteSpace(line)
        && !line.StartsWith("//", StringComparison.Ordinal)
        && (
            line.Contains("AddHexalithTenants", StringComparison.Ordinal)
            || line.Contains("AddTenantEventHandler", StringComparison.Ordinal)
            || line.Contains("UseCloudEvents", StringComparison.Ordinal)
            || line.Contains("MapSubscribeHandler", StringComparison.Ordinal)
            || line.Contains("MapTenantEventSubscription", StringComparison.Ordinal));

    private static string SampleProgramPath()
        => Path.Combine(FindRepoRoot(), "samples", "Hexalith.Tenants.Sample", "Program.cs");

    private static string FindRepoRoot() {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null) {
            if (File.Exists(Path.Combine(current.FullName, "Hexalith.Tenants.slnx"))) {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate Hexalith.Tenants repository root.");
    }
}
