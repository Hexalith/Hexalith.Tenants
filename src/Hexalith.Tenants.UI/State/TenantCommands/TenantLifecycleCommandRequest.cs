using Hexalith.Tenants.UI.State.TenantDetail;

namespace Hexalith.Tenants.UI.State.TenantCommands;

// CreateTenant, AddUserToTenant, ChangeUserRole, RemoveUserFromTenant, UpdateTenant,
// SetTenantConfiguration, and RemoveTenantConfiguration intents reuse the domain command records
// from Hexalith.Tenants.Contracts.Commands directly instead of mirroring them with UI-only request
// records. TenantLifecycleCommandRequest stays UI-owned because it carries a lifecycle operation
// the gateway resolves to either EnableTenant or DisableTenant.
public sealed record TenantLifecycleCommandRequest(
    string TenantId,
    TenantLifecycleOperation Operation);
