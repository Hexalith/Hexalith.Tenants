using System.Runtime.ExceptionServices;
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

    /// <summary>Preserves an already completed fatal load when its deadline also expires.</summary>
    internal static void ThrowIfFatal(Task task)
    {
        // Async methods retain an OperationCanceledException in a cancelled task, outside Exception.
        // Inspect only completed cancellation; the original wrapper can itself carry a fatal cause.
        if (task.IsCanceled)
        {
            try { task.GetAwaiter().GetResult(); }
            catch (OperationCanceledException exception) when (!IsFatal(exception)) { }
        }
        if (task.Exception is { } aggregate && IsFatal(aggregate))
        {
            Exception original = aggregate.InnerExceptions.Count == 1 ? aggregate.InnerExceptions[0] : aggregate;
            ExceptionDispatchInfo.Capture(original).Throw();
        }
    }

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
    /// <summary>Records only elapsed load time, including failure and expiry.</summary>
    internal static void SearchLoadCompleted(ILogger? logger, double milliseconds)
    {
        RecordTiming(logger, "load", milliseconds);
    }

    /// <summary>Records only elapsed time of one raw index window.</summary>
    internal static void SearchWindowCompleted(ILogger? logger, double milliseconds)
    {
        RecordTiming(logger, "window", milliseconds);
    }

    private static void RecordTiming(ILogger? logger, string stage, double milliseconds)
    {
        try { if (logger is not null) SearchTiming(logger, stage, milliseconds); }
        catch (Exception exception) when (!IsFatal(exception)) { }
    }

    /// <summary>Records load-budget expiry without any request or identifier material.</summary>
    [LoggerMessage(EventId = 1906, Level = LogLevel.Warning,
        Message = "Tenant search load budget expired.")]
    internal static partial void SearchBudgetExpired(ILogger logger);

    [LoggerMessage(EventId = 1905, Level = LogLevel.Debug,
        Message = "Tenant search timing. Stage={Stage} DurationMilliseconds={DurationMilliseconds}")]
    private static partial void SearchTiming(ILogger logger, string stage, double durationMilliseconds);
}
