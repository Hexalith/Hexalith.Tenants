using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>
/// Runs a test only when local DAPR infrastructure from dapr init is available.
/// </summary>
public sealed class DaprFactAttribute : FactAttribute {
    public DaprFactAttribute(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber) {
        Skip = DaprTestPrerequisites.SkipReason;
        SkipUnless = nameof(DaprTestPrerequisites.IsAvailable);
        SkipType = typeof(DaprTestPrerequisites);
    }
}

/// <summary>
/// Discovery-time prerequisite check for DAPR-backed integration tests.
/// </summary>
public static class DaprTestPrerequisites {
    private static readonly int PlacementPort = OperatingSystem.IsWindows() ? 6050 : 50005;
    private static readonly int SchedulerPort = OperatingSystem.IsWindows() ? 6060 : 50006;
    private static readonly Lazy<bool> s_isAvailable = new(CheckAvailability);

    /// <summary>
    /// Gets a value indicating whether local DAPR runtime dependencies are reachable.
    /// </summary>
    public static bool IsAvailable => s_isAvailable.Value;

    /// <summary>
    /// Gets the skip reason used when DAPR runtime dependencies are not reachable.
    /// </summary>
    public static string SkipReason
        => "DAPR integration prerequisites are unavailable. Run 'dapr init' and ensure Redis, placement, and scheduler are reachable.";

    private static bool CheckAvailability()
        => IsPortReachable(6379)
            && IsPortReachable(PlacementPort)
            && IsPortReachable(SchedulerPort);

    private static bool IsPortReachable(int port) {
        try {
            using var client = new TcpClient();
            Task connect = client.ConnectAsync("localhost", port);
            return connect.Wait(TimeSpan.FromMilliseconds(500)) && client.Connected;
        }
        catch {
            return false;
        }
    }
}
