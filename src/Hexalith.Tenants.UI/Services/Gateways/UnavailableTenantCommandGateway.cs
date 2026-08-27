using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class UnavailableTenantCommandGateway : ITenantCommandGateway
{
    public bool SupportsTrackedLifecycleDispatch => false;

    public bool SupportsTrackedSetConfigurationDispatch => false;

    public bool SupportsTrackedRemoveConfigurationDispatch => false;

    public Task<TenantCommandSubmissionResult> CreateTenantAsync(
        CreateTenant request,
        string? messageId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(
        AddUserToTenant request,
        string? messageId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(
        ChangeUserRole request,
        string? messageId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(
        RemoveUserFromTenant request,
        string? messageId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> UpdateTenantAsync(
        UpdateTenant request,
        string? messageId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(
        SetTenantConfiguration request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> SetTenantConfigurationTrackedAsync(
        SetTenantConfiguration request,
        string messageId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.FailedWithKey(
            "Tenants.Configuration.Set.Unavailable.TrackedDispatch"));

    public Task<TenantCommandSubmissionResult> RemoveTenantConfigurationAsync(
        RemoveTenantConfiguration request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> RemoveTenantConfigurationTrackedAsync(
        RemoveTenantConfiguration request,
        string messageId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.FailedWithKey(
            "Tenants.Configuration.Remove.Unavailable.TrackedDispatch"));

    public Task<TenantCommandSubmissionResult> SetGlobalAdministratorAsync(
        SetGlobalAdministrator request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Global administrator command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> RemoveGlobalAdministratorAsync(
        RemoveGlobalAdministrator request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Global administrator command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> EnableTenantAsync(
        TenantLifecycleCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> EnableTenantTrackedAsync(
        TenantLifecycleCommandRequest request,
        string messageId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.FailedWithKey(
            "Tenants.Lifecycle.Unavailable.CommandSurface"));

    public Task<TenantCommandSubmissionResult> DisableTenantAsync(
        TenantLifecycleCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing."));

    public Task<TenantCommandSubmissionResult> DisableTenantTrackedAsync(
        TenantLifecycleCommandRequest request,
        string messageId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.FailedWithKey(
            "Tenants.Lifecycle.Unavailable.CommandSurface"));

    public Task<TenantCommandStatusResult> GetStatusAsync(
        TenantCommandTrackingHandle handle,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandStatusResult.Unknown("Tenant command status lookup is unavailable."));
}
