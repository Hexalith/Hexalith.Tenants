using System.Text.Json;

using Hexalith.EventStore.Client.Routing;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Tenants.Contracts.Routing;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Supplies the compiled owner catalog for isolated HTTP and real-handler proofs.</summary>
internal static class TenantOwnerCatalogTestAuthority
{
    /// <summary>Activates the compiled catalog for the isolated gateway.</summary>
    internal static IRouteCatalogAuthority Create() => Create(CreateSnapshot("eventstore"));

    /// <summary>Builds the owner declarations using the published catalog API.</summary>
    /// <param name="localAppId">The independently selected acquiring application.</param>
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

    /// <summary>Commits an isolated catalog and returns the SDK authority that owns each durable lease.</summary>
    /// <param name="snapshot">The catalog and local application selected by the test composition.</param>
    internal static IRouteCatalogAuthority Create(CommittedRouteCatalogSnapshot snapshot)
    {
        var store = new TenantOwnerCatalogTestStateStore(snapshot.AuthorityCoordinates);
        RouteCatalogEnvelope envelope = snapshot.Envelope;
        string localAppId = snapshot.LocalAppId ?? "eventstore";
        string[] appIds = [.. envelope.Routes.Select(route => route.AppId)
            .Concat(envelope.Idempotency.Select(policy => policy.ConsumerAppId))
            .Concat(snapshot.TechnicalHttpReceivers.Select(receiver => receiver.AppId))
            .Concat([localAppId, "eventstore"]).Distinct(StringComparer.Ordinal)];
        RouteCatalogAuxiliaryOperation[] auxiliary = [.. snapshot.AuxiliaryOperations];
        RouteCatalogHostObservation[] observations = [.. appIds.Select(appId =>
        {
            RouteCatalogTechnicalHttpReceiver? receiver = snapshot.TechnicalHttpReceivers.SingleOrDefault(item => item.AppId == appId);
            return new RouteCatalogHostObservation(
                appId, appId,
                envelope.Declarations.Where(declaration => envelope.Routes.Any(route => route.RouteId == declaration.RouteId && route.AppId == appId)).ToArray(),
                envelope.Routes.Where(route => route.AppId == appId).ToArray(), appIds,
                [JsonSerializer.SerializeToUtf8Bytes(new
                {
                    kind = "Component", metadata = new { name = store.Coordinates.StateStore },
                    spec = new { type = "state.redis", metadata = new[] { new { name = "keyPrefix", value = "none" } } }, scopes = appIds,
                }), JsonSerializer.SerializeToUtf8Bytes(new { spec = new { accessControl = new { defaultAction = "deny", trustDomain = "public" } } })],
                "isolated-owner-authentication-v1"u8.ToArray(),
                envelope.Idempotency.Where(policy => policy.ConsumerAppId == appId).ToArray(),
                snapshot.ProjectionCapabilities.Where(capability => capability.AppId == appId).ToArray(),
                auxiliary.Where(operation => operation.AppId == appId).ToArray(), store.Coordinates,
                ReceiverSurface: receiver is null ? null : new RouteCatalogReceiverSurface(receiver.FenceVersion,
                    [.. receiver.Endpoints.Select(endpoint => new RouteCatalogReceiverEndpoint(endpoint.Pattern, endpoint.Methods,
                        RouteCatalogTechnicalHttpPolicy.Operation, endpoint.Policies, false, endpoint.Pattern.Contains('{'), true))])
                    { TechnicalHttp = receiver.Endpoints },
                Operations: envelope.Operations?.Where(operation => envelope.Routes.Any(route => route.RouteId == operation.RouteId && route.AppId == appId)).ToArray())
            {
                SelectedAppIds = receiver?.Invocations is null ? null : appIds,
                DeclaredInvocations = receiver?.Invocations,
            };
        })];
        RouteCatalogHostIdentity[] hosts = [.. observations.Select(ObservedRouteCatalogHost.Identity)];
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Development);
        RouteCatalogAuthority[] authorities = [.. observations.Select(observation =>
        {
            var options = Options.Create(new RouteCatalogHostOptions
            {
                Identity = hosts.Single(host => host.HostId == observation.HostId),
                Profile = "Development",
                DevelopmentFixture = true,
            });
            return new RouteCatalogAuthority(store, new DevelopmentRouteCatalogTrustValidator(options, environment),
                options, new ObservedRouteCatalogHost(_ => Task.FromResult(observation)));
        })];
        var activation = new RouteCatalogActivation("isolated-owner", snapshot.GetCanonicalBytes(), snapshot.RootDigest,
            null, hosts, "Development-fixture", "")
        {
            AuxiliaryOperations = auxiliary,
            ProjectionCapabilities = snapshot.ProjectionCapabilities,
            TechnicalHttpReceivers = snapshot.TechnicalHttpReceivers.Count == 0 ? null : snapshot.TechnicalHttpReceivers,
        };
        activation = activation with { TrustProof = RouteCatalogActivationBinding.ComputeDigest(activation) };
        authorities[0].PrepareAsync(activation).GetAwaiter().GetResult();
        foreach (RouteCatalogAuthority authority in authorities)
        {
            authority.ReadyAsync(activation.AttemptId).GetAwaiter().GetResult();
        }
        authorities[0].CommitAsync(activation.AttemptId).GetAwaiter().GetResult();
        return authorities[Array.FindIndex(observations, observation => observation.AppId == localAppId)];
    }
}
