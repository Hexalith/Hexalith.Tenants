using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class UnavailableTenantCommandGateway : ITenantCommandGateway
{
    public Task<TenantCommandSubmissionResult> CreateTenantAsync(
        CreateTenantCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(
        AddUserToTenantCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(
        ChangeUserRoleCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(
        RemoveUserFromTenantCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> UpdateTenantAsync(
        UpdateTenantCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(
        SetTenantConfigurationCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> RemoveTenantConfigurationAsync(
        RemoveTenantConfigurationCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> SetGlobalAdministratorAsync(
        SetGlobalAdministratorCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Global administrator command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> EnableTenantAsync(
        TenantLifecycleCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> DisableTenantAsync(
        TenantLifecycleCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing."));

    public Task<TenantCommandStatusResult> GetStatusAsync(
        TenantCommandTrackingHandle handle,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandStatusResult.Unknown("Tenant command status lookup is unavailable."));
}
