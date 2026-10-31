using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Routing;

#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
[assembly: RouteCatalogAdapterSource(typeof(Hexalith.Tenants.Server.Routing.TenantIntentAdapters))]
#endif

namespace Hexalith.Tenants.Server.Routing;

/// <summary>Typed owner semantic policies, constructed offline without registering runtime admission.</summary>
public static class TenantIntentAdapters
{
    /// <summary>Constructs all twelve approved per-operation adapters.</summary>
    public static IReadOnlyList<IDeclaredIdempotencyIntentAdapter> Create() => [
        Adapter<CreateTenant>(c => Description(c.Name, c.Description)),
        Adapter<UpdateTenant>(c => Description(c.Name, c.Description)),
        Adapter<DisableTenant>(_ => Fields()), Adapter<EnableTenant>(_ => Fields()),
        Adapter<AddUserToTenant>(c => Fields(("user-id", c.UserId), ("role", Role(c.Role)))),
        Adapter<RemoveUserFromTenant>(c => Fields(("user-id", c.UserId))),
        Adapter<ChangeUserRole>(c => Fields(("user-id", c.UserId), ("new-role", Role(c.NewRole)))),
        Adapter<SetTenantConfiguration>(c => Fields(("key", c.Key), ("value", c.Value))),
        Adapter<RemoveTenantConfiguration>(c => Fields(("key", c.Key))),
        Adapter<BootstrapGlobalAdmin>(c => Fields(("user-id", c.UserId))),
        Adapter<SetGlobalAdministrator>(c => Fields(("user-id", c.UserId))),
        Adapter<RemoveGlobalAdministrator>(c => Fields(("user-id", c.UserId)))];

    private static DeclaredIdempotencyIntentAdapter<T> Adapter<T>(Func<T, IReadOnlyDictionary<string, object?>> fields) => new(
        TenantCatalogDeclarations.Commands.Single(c => c.Route.Execution!.HandlerType == typeof(T).FullName), fields);

    private static IReadOnlyDictionary<string, object?> Description(string name, string? description) =>
        Fields(("name", name), ("description-is-null", description is null), ("description", description ?? string.Empty));

    private static IReadOnlyDictionary<string, object?> Fields(params (string Name, object? Value)[] fields) =>
        fields.ToDictionary(f => f.Name, f => f.Value, StringComparer.Ordinal);

    private static string Role(TenantRole role) => role is TenantRole.TenantOwner or TenantRole.TenantContributor or TenantRole.TenantReader
        ? role.ToString() : throw new InvalidOperationException("The validated role is required.");
}
