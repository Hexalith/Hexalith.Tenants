using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.Tenants.UI.Tests.Services.Configuration;

/// <summary>
/// Test <see cref="IUserContextAccessor"/> whose <see cref="UserId"/> getter throws a configured exception.
/// </summary>
internal sealed class ThrowingUserContextAccessor(
    Exception exception,
    Action? beforeThrow = null) : IUserContextAccessor
{
    /// <inheritdoc />
    public string? TenantId => null;

    /// <inheritdoc />
    public string? UserId
    {
        get
        {
            beforeThrow?.Invoke();
            throw exception;
        }
    }
}
