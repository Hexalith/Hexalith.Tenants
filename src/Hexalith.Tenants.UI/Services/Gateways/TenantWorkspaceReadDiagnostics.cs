using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>Classifies workspace read failures without logging exception or request material.</summary>
internal static partial class TenantWorkspaceReadDiagnostics
{
    /// <summary>Identifies fatal failures that the workspace must propagate.</summary>
    internal static bool IsFatal(Exception exception)
        => exception is OutOfMemoryException or StackOverflowException or AccessViolationException
            || (exception is AggregateException aggregate && aggregate.InnerExceptions.Any(IsFatal))
            || (exception.InnerException is { } inner && IsFatal(inner));

    /// <summary>Maps nonfatal failures to fixed support-safe diagnostic categories.</summary>
    internal static string Classify(Exception exception)
        => exception switch
        {
            ObjectDisposedException => "provider-disposed",
            ArgumentException => "invalid-argument",
            NullReferenceException => "programming-failure",
            OperationCanceledException => "provider-cancelled",
            _ => "read-failed",
        };

    /// <summary>Records a contained read failure using a fixed category only.</summary>
    [LoggerMessage(EventId = 1904, Level = LogLevel.Warning,
        Message = "Tenant workspace read failed. Category={FailureCategory}")]
    internal static partial void ReadFailed(ILogger logger, string failureCategory);
}
