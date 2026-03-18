using System.Text.Json;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Testing.Fakes;

namespace Hexalith.Tenants.Testing.Helpers;

/// <summary>
/// Common setup patterns for tenant integration tests, reducing test authoring to under 10 lines per test.
/// </summary>
public static class TenantTestHelpers
{
    private const string DefaultDomain = "tenants";
    private const string GlobalAdminExtensionKey = "actor:globalAdmin";
    private const string SystemTenantId = "system";

    /// <summary>
    /// Bootstraps a global administrator via the given service.
    /// </summary>
    /// <param name="service">The in-memory tenant service.</param>
    /// <param name="userId">The user ID to bootstrap as global admin. Defaults to "global-admin".</param>
    /// <returns>The domain result containing the GlobalAdministratorSet event.</returns>
    public static DomainResult BootstrapGlobalAdmin(
        InMemoryTenantService service,
        string userId = "global-admin")
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return service.ProcessCommand(new BootstrapGlobalAdmin(userId));
    }

    /// <summary>
    /// Builds a <see cref="CommandEnvelope"/> with an explicit aggregate ID parameter.
    /// Does NOT use <c>dynamic</c> to extract TenantId — requires the caller to pass it explicitly for type safety.
    /// </summary>
    /// <typeparam name="T">The command type.</typeparam>
    /// <param name="command">The command payload.</param>
    /// <param name="aggregateId">The aggregate identifier (the managed tenant ID for tenant commands, "system" for global admin commands).</param>
    /// <param name="userId">The acting user ID.</param>
    /// <param name="isGlobalAdmin">Whether the actor is a global administrator.</param>
    /// <returns>A fully constructed <see cref="CommandEnvelope"/>.</returns>
    public static CommandEnvelope CreateCommandEnvelope<T>(
        T command,
        string aggregateId,
        string userId,
        bool isGlobalAdmin = false)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new CommandEnvelope(
            Guid.NewGuid().ToString(),
            SystemTenantId,
            DefaultDomain,
            aggregateId,
            typeof(T).Name,
            JsonSerializer.SerializeToUtf8Bytes(command),
            Guid.NewGuid().ToString(),
            null,
            userId,
            isGlobalAdmin
                ? new Dictionary<string, string> { [GlobalAdminExtensionKey] = "true" }
                : null);
    }

    /// <summary>
    /// Creates a tenant via the given service.
    /// </summary>
    /// <param name="service">The in-memory tenant service.</param>
    /// <param name="tenantId">The tenant identifier. Defaults to "test-tenant".</param>
    /// <param name="name">The tenant name. Defaults to "Test Tenant".</param>
    /// <param name="description">The tenant description. Defaults to null.</param>
    /// <returns>The domain result containing the TenantCreated event.</returns>
    public static DomainResult CreateTenant(
        InMemoryTenantService service,
        string tenantId = "test-tenant",
        string name = "Test Tenant",
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return service.ProcessCommand(new CreateTenant(tenantId, name, description));
    }

    /// <summary>
    /// Creates a tenant and adds an owner user, returning the combined results.
    /// </summary>
    /// <param name="service">The in-memory tenant service.</param>
    /// <param name="tenantId">The tenant identifier. Defaults to "test-tenant".</param>
    /// <param name="ownerUserId">The owner user ID. Defaults to "owner".</param>
    /// <param name="tenantName">The tenant name. Defaults to "Test Tenant".</param>
    /// <returns>A tuple of (createResult, addOwnerResult).</returns>
    public static (DomainResult CreateResult, DomainResult AddOwnerResult) CreateTenantWithOwner(
        InMemoryTenantService service,
        string tenantId = "test-tenant",
        string ownerUserId = "owner",
        string tenantName = "Test Tenant")
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantName);

        DomainResult createResult = service.ProcessCommand(new CreateTenant(tenantId, tenantName, null));

        // First user on a new tenant bypasses RBAC (empty tenant bootstrap).
        // Use isGlobalAdmin: true to guarantee success regardless of state.
        DomainResult addOwnerResult = service.ProcessCommand(
            new AddUserToTenant(tenantId, ownerUserId, TenantRole.TenantOwner),
            userId: ownerUserId,
            isGlobalAdmin: true);

        return (createResult, addOwnerResult);
    }
}
