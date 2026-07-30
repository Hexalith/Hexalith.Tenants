namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>
/// Signals that a response body exceeded the bounded number of bytes this client will buffer.
/// </summary>
/// <remarks>
/// Thrown only inside the client and never surfaced: callers map it to a fixed support-safe failure
/// category, so no size, content, or transport detail crosses the BFF boundary. It carries no message for
/// the same reason.
/// </remarks>
internal sealed class ResponseContentTooLargeException : Exception
{
}
