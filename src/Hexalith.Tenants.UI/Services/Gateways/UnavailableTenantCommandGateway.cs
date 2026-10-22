using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class UnavailableTenantCommandGateway : ITenantCommandGateway
{
    public bool SupportsGlobalAdministratorDispatch => false;

    public bool SupportsTrackedGlobalAdministratorDispatch => false;

    public bool SupportsTrackedGlobalAdministratorRemoveDispatch => false;

    public bool SupportsCommandStatusLookup => false;

    public bool SupportsTrackedLifecycleDispatch => false;

    public bool SupportsTrackedSetConfigurationDispatch => false;

    public bool SupportsTrackedRemoveConfigurationDispatch => false;

    public Task<TenantCommandSubmissionResult> CreateTenantAsync(
        CreateTenant request,
        string? messageId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing.") with { SafeMessageKey = "Tenants.Create.Unavailable.GatewayConfiguration" });

    public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(
        AddUserToTenant request,
        string? messageId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing.") with { SafeMessageKey = "Tenants.AddMember.Unavailable.GatewayConfiguration" });

    public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(
        ChangeUserRole request,
        string? messageId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing.") with { SafeMessageKey = "Tenants.ChangeRole.Unavailable.GatewayConfiguration" });

    public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(
        RemoveUserFromTenant request,
        string? messageId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing.") with { SafeMessageKey = "Tenants.RemoveMember.Unavailable.GatewayConfiguration" });

    public Task<TenantCommandSubmissionResult> UpdateTenantAsync(
        UpdateTenant request,
        string? messageId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing.") with { SafeMessageKey = "Tenants.EditMetadata.Unavailable.GatewayConfiguration" });

    [Obsolete("Use SetTenantConfigurationTrackedAsync with a caller-owned messageId retained across retries.")]
    public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(
        SetTenantConfiguration request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing.") with { SafeMessageKey = "Tenants.Configuration.Set.Unavailable.GatewayConfiguration" });

    public Task<TenantCommandSubmissionResult> SetTenantConfigurationTrackedAsync(
        SetTenantConfiguration request,
        string messageId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.FailedWithKey(
            "Tenants.Configuration.Set.Unavailable.TrackedDispatch"));

    [Obsolete("Use RemoveTenantConfigurationTrackedAsync with a caller-owned messageId retained across retries.")]
    public Task<TenantCommandSubmissionResult> RemoveTenantConfigurationAsync(
        RemoveTenantConfiguration request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing.") with { SafeMessageKey = "Tenants.Configuration.Remove.Unavailable.GatewayConfiguration" });

    public Task<TenantCommandSubmissionResult> RemoveTenantConfigurationTrackedAsync(
        RemoveTenantConfiguration request,
        string messageId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.FailedWithKey(
            "Tenants.Configuration.Remove.Unavailable.TrackedDispatch"));

    [Obsolete("Use SetGlobalAdministratorTrackedAsync with a caller-owned messageId retained across retries.")]
    public Task<TenantCommandSubmissionResult> SetGlobalAdministratorAsync(
        SetGlobalAdministrator request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Global administrator command gateway configuration is missing.") with { SafeMessageKey = "Tenants.GlobalAdministrators.Grant.Unavailable.GatewayConfiguration" });

    public Task<TenantCommandSubmissionResult> SetGlobalAdministratorTrackedAsync(
        SetGlobalAdministrator request,
        string messageId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.FailedWithKey(
            "Tenants.GlobalAdministrators.Grant.Unavailable.TrackedDispatch"));

    [Obsolete("Use RemoveGlobalAdministratorTrackedAsync with a caller-owned messageId retained across retries.")]
    public Task<TenantCommandSubmissionResult> RemoveGlobalAdministratorAsync(
        RemoveGlobalAdministrator request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Global administrator command gateway configuration is missing.") with { SafeMessageKey = "Tenants.GlobalAdministrators.Remove.Unavailable.GatewayConfiguration" });

    public Task<TenantCommandSubmissionResult> RemoveGlobalAdministratorTrackedAsync(
        RemoveGlobalAdministrator request,
        string messageId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.FailedWithKey(
            "Tenants.GlobalAdministrators.Remove.Unavailable.TrackedDispatch"));

    [Obsolete("Use EnableTenantTrackedAsync with a caller-owned messageId retained across retries.")]
    public Task<TenantCommandSubmissionResult> EnableTenantAsync(
        TenantLifecycleCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing.") with { SafeMessageKey = "Tenants.Lifecycle.Unavailable.GatewayConfiguration" });

    public Task<TenantCommandSubmissionResult> EnableTenantTrackedAsync(
        TenantLifecycleCommandRequest request,
        string messageId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.FailedWithKey(
            "Tenants.Lifecycle.Unavailable.CommandSurface"));

    [Obsolete("Use DisableTenantTrackedAsync with a caller-owned messageId retained across retries.")]
    public Task<TenantCommandSubmissionResult> DisableTenantAsync(
        TenantLifecycleCommandRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway configuration is missing.") with { SafeMessageKey = "Tenants.Lifecycle.Unavailable.GatewayConfiguration" });

    public Task<TenantCommandSubmissionResult> DisableTenantTrackedAsync(
        TenantLifecycleCommandRequest request,
        string messageId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandSubmissionResult.FailedWithKey(
            "Tenants.Lifecycle.Unavailable.CommandSurface"));

    public Task<TenantCommandStatusResult> GetStatusAsync(
        TenantCommandTrackingHandle handle,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantCommandStatusResult.Unknown("Tenant command status lookup is unavailable.") with
        {
            SafeMessageKey = "Tenants.Commands.Status.Unavailable",
        });
}
