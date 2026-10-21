using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.UI.Components.Pages;

/// <summary>Records support-safe tenant detail notification diagnostics.</summary>
internal static partial class TenantDetailPageLog
{
    /// <summary>Records a contained optional membership refresh failure without provider or caller data.</summary>
    /// <param name="logger">The tenant detail logger.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning,
        Message = "Optional membership projection refresh failed. ReasonCode=notification-callback-failed")]
    internal static partial void MembershipNudgeFailed(ILogger logger);
}
