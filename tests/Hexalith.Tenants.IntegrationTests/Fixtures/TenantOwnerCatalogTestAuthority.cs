#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
using Hexalith.EventStore.Client.Routing;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Tenants.Contracts.Routing;

using NSubstitute;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Supplies the compiled owner catalog for isolated HTTP and real-handler proofs.</summary>
internal static class TenantOwnerCatalogTestAuthority
{
    internal static IRouteCatalogAuthority Create() => Create(CreateSnapshot("eventstore"));

    internal static CommittedRouteCatalogSnapshot CreateSnapshot(string localAppId)
    {
        IReadOnlyList<RouteContractDeclaration> declarations = CompiledRouteCatalogDeclarations.Read([typeof(TenantCatalogDeclarations).Assembly]);
        RouteCatalogEnvelope envelope = VersionedRouteCatalogBuilder.Build(new(
            1, "Development", declarations,
            CompiledRouteCatalogDeclarations.ReadOperations([typeof(TenantCatalogDeclarations).Assembly]),
            declarations.Select(declaration => new RouteCompositionBinding(
                declaration.RouteId, "tenants", TenantCatalogDeclarations.ReceiverMethods[declaration.RouteId],
                declaration.ContractVersion, false, "*")).ToArray(),
            TenantCatalogDeclarations.Commands.Select(command => DevelopmentIdempotencyBindings.Bind(command, 1)).ToArray()));
        return new CommittedRouteCatalogSnapshot(
            RouteCatalogCodec.Encode(envelope), RouteCatalogCodec.ComputeRootDigest(envelope),
            1, localAppId: localAppId);
    }

    internal static IRouteCatalogAuthority Create(CommittedRouteCatalogSnapshot snapshot)
    {
        // Distributed activation is outside these isolated HTTP proofs. Each generated action and
        // real router still validates the compiled owner operation, policy and projection selection.
        IRouteCatalogAuthority authority = Substitute.For<IRouteCatalogAuthority>();
        authority.AcquireAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new RouteCatalogDispatchLease(snapshot, () => Task.CompletedTask));
        return authority;
    }
}
#endif
