namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>
/// Records what a <c>400</c> response body proved about the shared <c>invalid-cursor</c> sentinel.
/// </summary>
/// <remarks>
/// Distinguishing <see cref="Absent"/> from <see cref="Timeout"/> and <see cref="Unavailable"/> keeps
/// page-one recovery conditional on an explicit contract signal: a body that could not be read is not
/// evidence that the cursor was valid, and must not be treated as such.
/// </remarks>
internal enum InvalidCursorSignal
{
    /// <summary>The body was read and carried no invalid-cursor sentinel.</summary>
    Absent,

    /// <summary>The body was read and carried the invalid-cursor sentinel.</summary>
    Present,

    /// <summary>The body could not be read within the read deadline, so nothing was proven.</summary>
    Timeout,

    /// <summary>The body could not be read at all, so nothing was proven.</summary>
    Unavailable,
}
