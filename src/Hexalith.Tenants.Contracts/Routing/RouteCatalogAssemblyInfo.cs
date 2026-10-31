#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.Tenants.Contracts.Routing;

[assembly: RouteCatalogDeclarationSource(typeof(TenantCatalogDeclarations))]
#endif
