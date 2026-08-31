using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.Services.Gateways;

public interface ITenantCommandGateway {
    /// <summary>Gets whether fixed-scope global-administrator dispatch is supported.</summary>
    bool SupportsGlobalAdministratorDispatch => false;

    /// <summary>Gets whether caller-owned tracked global-administrator dispatch is supported.</summary>
    bool SupportsTrackedGlobalAdministratorDispatch => false;

    /// <summary>Gets whether authoritative command-status lookup is supported.</summary>
    bool SupportsCommandStatusLookup => false;

    bool SupportsTrackedLifecycleDispatch => false;

    bool SupportsTrackedSetConfigurationDispatch => false;

    bool SupportsTrackedRemoveConfigurationDispatch => false;

    Task<TenantCommandSubmissionResult> CreateTenantAsync(
        CreateTenant request,
        string? messageId = null,
        CancellationToken cancellationToken = default);

    Task<TenantCommandSubmissionResult> AddUserToTenantAsync(
        AddUserToTenant request,
        string? messageId = null,
        CancellationToken cancellationToken = default);

    Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(
        ChangeUserRole request,
        string? messageId = null,
        CancellationToken cancellationToken = default);

    Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(
        RemoveUserFromTenant request,
        string? messageId = null,
        CancellationToken cancellationToken = default);

    Task<TenantCommandSubmissionResult> UpdateTenantAsync(
        UpdateTenant request,
        string? messageId = null,
        CancellationToken cancellationToken = default);

    Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(
        SetTenantConfiguration request,
        CancellationToken cancellationToken = default);

    Task<TenantCommandSubmissionResult> SetTenantConfigurationTrackedAsync(
        SetTenantConfiguration request,
        string messageId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.FailedWithKey(
            "Tenants.Configuration.Set.Unavailable.TrackedDispatch"));

    Task<TenantCommandSubmissionResult> RemoveTenantConfigurationAsync(
        RemoveTenantConfiguration request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant configuration removal gateway is unavailable."));

    Task<TenantCommandSubmissionResult> RemoveTenantConfigurationTrackedAsync(
        RemoveTenantConfiguration request,
        string messageId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.FailedWithKey(
            "Tenants.Configuration.Remove.Unavailable.TrackedDispatch"));

    Task<TenantCommandSubmissionResult> SetGlobalAdministratorAsync(
        SetGlobalAdministrator request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Global administrator command gateway is unavailable."));

    /// <summary>Dispatches a grant with the exact caller-owned ULID retained by the preview lifecycle.</summary>
    /// <param name="request">Literal grant intent.</param>
    /// <param name="messageId">Exact caller-owned ULID.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>Tracked submission evidence.</returns>
    Task<TenantCommandSubmissionResult> SetGlobalAdministratorTrackedAsync(
        SetGlobalAdministrator request,
        string messageId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.FailedWithKey(
            "Tenants.GlobalAdministrators.Grant.Unavailable.TrackedDispatch"));

    Task<TenantCommandSubmissionResult> RemoveGlobalAdministratorAsync(
        RemoveGlobalAdministrator request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Global administrator command gateway is unavailable."));

    Task<TenantCommandSubmissionResult> EnableTenantAsync(
        TenantLifecycleCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant lifecycle command gateway is unavailable."));

    Task<TenantCommandSubmissionResult> EnableTenantTrackedAsync(
        TenantLifecycleCommandRequest request,
        string messageId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.FailedWithKey(
            "Tenants.Lifecycle.Unavailable.CommandSurface"));

    Task<TenantCommandSubmissionResult> DisableTenantAsync(
        TenantLifecycleCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant lifecycle command gateway is unavailable."));

    Task<TenantCommandSubmissionResult> DisableTenantTrackedAsync(
        TenantLifecycleCommandRequest request,
        string messageId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.FailedWithKey(
            "Tenants.Lifecycle.Unavailable.CommandSurface"));

    Task<TenantCommandStatusResult> GetStatusAsync(
        TenantCommandTrackingHandle handle,
        CancellationToken cancellationToken = default);
}
