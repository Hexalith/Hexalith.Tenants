using System.Reflection;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Tenants.Contracts.Routing;
using Hexalith.Tenants.Server.Routing;

string[] expected = File.ReadAllLines("approved-route-ids.txt").Order(StringComparer.Ordinal).ToArray();
Assembly contracts = typeof(TenantCatalogDeclarations).Assembly;
Assembly server = typeof(TenantIntentAdapters).Assembly;
if (contracts.GetCustomAttributes<RouteCatalogDeclarationSourceAttribute>().Single().SourceType != typeof(TenantCatalogDeclarations)) throw new InvalidDataException("Wrong packaged declaration source.");
RouteCatalogAdapterSourceAttribute source = server.GetCustomAttributes<RouteCatalogAdapterSourceAttribute>().Single();
if (source.SourceType != typeof(TenantIntentAdapters)) throw new InvalidDataException("Wrong packaged adapter source.");
var routes = CompiledRouteCatalogDeclarations.Read([contracts]);
var operations = CompiledRouteCatalogDeclarations.ReadOperations([contracts]);
var adapters = ((IEnumerable<IDeclaredIdempotencyIntentAdapter>)source.SourceType.GetMethod("Create")!.Invoke(null, null)!).ToArray();
if (!routes.Select(route => route.RouteId).Order(StringComparer.Ordinal).SequenceEqual(expected, StringComparer.Ordinal)
    || !operations.Select(operation => operation.RouteId).Order(StringComparer.Ordinal).SequenceEqual(expected, StringComparer.Ordinal)
    || routes.Count(route => route.Kind == "command") != 12 || routes.Count(route => route.Kind == "query") != 8 || routes.Count(route => route.Kind == "projection") != 2
    || adapters.Length != 12 || !adapters.Select(adapter => adapter.Declaration.Route.RouteId).Order(StringComparer.Ordinal)
        .SequenceEqual(routes.Where(route => route.Kind == "command").Select(route => route.RouteId).Order(StringComparer.Ordinal), StringComparer.Ordinal)
    || operations.Any(operation => operation.Digest != RouteCatalogCodec.ComputeOperationContractDigest(operation))) throw new InvalidDataException("The actual packages differ from the approved complete catalog inventory.");
Console.WriteLine(JsonSerializer.Serialize(new { routes = routes.Count, operations = operations.Count, adapters = adapters.Length,
    declarations = routes.ToDictionary(route => route.RouteId, RouteCatalogCodec.ComputeDeclarationDigest),
    operationDigests = operations.ToDictionary(operation => operation.RouteId, operation => operation.Digest) }));
