using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.Services.Gateways;

public interface ITenantCommandGateway
{
    Task<TenantCommandSubmissionResult> CreateTenantAsync(
        CreateTenantCommandRequest request,
        CancellationToken cancellationToken = default);

    Task<TenantCommandStatusResult> GetStatusAsync(
        TenantCommandTrackingHandle handle,
        CancellationToken cancellationToken = default);
}
