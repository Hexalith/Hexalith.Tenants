using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.Services.Gateways;

public interface ITenantCommandGateway
{
    Task<TenantCommandSubmissionResult> CreateTenantAsync(
        CreateTenantCommandRequest request,
        CancellationToken cancellationToken = default);

    Task<TenantCommandSubmissionResult> AddUserToTenantAsync(
        AddUserToTenantCommandRequest request,
        CancellationToken cancellationToken = default);

    Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(
        ChangeUserRoleCommandRequest request,
        CancellationToken cancellationToken = default);

    Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(
        RemoveUserFromTenantCommandRequest request,
        CancellationToken cancellationToken = default);

    Task<TenantCommandSubmissionResult> UpdateTenantAsync(
        UpdateTenantCommandRequest request,
        CancellationToken cancellationToken = default);

    Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(
        SetTenantConfigurationCommandRequest request,
        CancellationToken cancellationToken = default);

    Task<TenantCommandSubmissionResult> RemoveTenantConfigurationAsync(
        RemoveTenantConfigurationCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant configuration removal gateway is unavailable."));

    Task<TenantCommandSubmissionResult> SetGlobalAdministratorAsync(
        SetGlobalAdministratorCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Global administrator command gateway is unavailable."));

    Task<TenantCommandSubmissionResult> EnableTenantAsync(
        TenantLifecycleCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant lifecycle command gateway is unavailable."));

    Task<TenantCommandSubmissionResult> DisableTenantAsync(
        TenantLifecycleCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant lifecycle command gateway is unavailable."));

    Task<TenantCommandStatusResult> GetStatusAsync(
        TenantCommandTrackingHandle handle,
        CancellationToken cancellationToken = default);
}
