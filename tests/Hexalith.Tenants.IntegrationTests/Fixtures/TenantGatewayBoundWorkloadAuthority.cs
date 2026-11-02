using System.Net.Http.Json;
using System.Security.Cryptography;

using Dapr.Client;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Tenants.Bootstrap;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Issues genuine SDK workload assertions and RSA command proofs for isolated internal gateway controls.</summary>
/// <param name="workload">The separately signed workload identity, including an untrusted caller for negative controls.</param>
internal sealed class TenantGatewayBoundWorkloadAuthority(string workload = "tenants") : IDisposable
{
    private readonly RSA _proofKey = RSA.Create(2048);
    private readonly string _channelToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    /// <summary>Gets the receiver configuration; the public bearer and catalog fixtures keep their existing authority.</summary>
    internal IReadOnlyDictionary<string, string?> Settings => new Dictionary<string, string?>
    {
        ["APP_API_TOKEN"] = _channelToken,
        ["Authentication:DaprInternal:Audience"] = "eventstore",
        ["Authentication:DaprInternal:AllowedCallers:0"] = "tenants",
        ["Authentication:WorkloadIssuer:Workload"] = workload,
        ["Authentication:BoundCommand:AllowedDomains:tenants"] = "global-administrators",
        ["Authentication:BoundStreamRead:PublicKeys:tenants"] = Convert.ToBase64String(_proofKey.ExportSubjectPublicKeyInfo()),
        ["Authentication:BoundStreamRead:PrivateKey"] = Convert.ToBase64String(_proofKey.ExportPkcs8PrivateKey()),
    };

    /// <summary>Authenticates the human JWT before binding its exact raw subject with independent workload credentials.</summary>
    /// <param name="services">The real gateway services.</param>
    /// <param name="humanToken">The represented human's signed bearer token.</param>
    /// <param name="command">The exact command, including a deliberately invalid business target when required.</param>
    /// <returns>A request for the existing internal endpoint.</returns>
    internal async Task<HttpRequestMessage> CreateRequestAsync(IServiceProvider services, string humanToken, SubmitCommandRequest command)
    {
        using IServiceScope scope = services.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Headers.Authorization = "Bearer " + humanToken;
        AuthenticateResult authenticated = await scope.ServiceProvider.GetRequiredService<IAuthenticationService>()
            .AuthenticateAsync(context, JwtBearerDefaults.AuthenticationScheme);
        string[] subjects = authenticated.Principal?.Identities.Where(identity => identity.IsAuthenticated)
            .SelectMany(identity => identity.FindAll("sub")).Select(claim => claim.Value).ToArray() ?? [];
        if (!authenticated.Succeeded || subjects.Length != 1 || string.IsNullOrWhiteSpace(subjects[0]))
        {
            throw new InvalidOperationException("The represented subject must come from the validated human JWT.");
        }

        string assertion = await scope.ServiceProvider.GetRequiredService<IWorkloadAssertionIssuer>()
            .IssueAsync(new WorkloadAssertionRequest("eventstore", EventStoreWorkloadOperations.GatewayCommandSubmit))
            ?? throw new InvalidOperationException("The real SDK workload issuer refused the internal assertion.");
        var bound = new BoundWorkloadCommandRequest(command, subjects[0]);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/commands/bound/workload")
        {
            Content = JsonContent.Create(bound),
        };
        request.Headers.Add(DaprAppChannelToken.HeaderName, _channelToken);
        request.Headers.Add(EventStoreWorkloadAuthenticationDefaults.DaprCallerHeaderName, workload);
        request.Headers.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion);
        request.Headers.Add(BoundWorkloadCommandProof.HeaderName, BoundWorkloadCommandProof.Issue(
            bound, workload, Convert.ToBase64String(_proofKey.ExportPkcs8PrivateKey()), DateTimeOffset.UtcNow));
        return request;
    }

    /// <summary>Uses the real bootstrap creator; only the sidecar delivery address and attribution are supplied locally.</summary>
    /// <param name="services">The gateway services that authenticate the administrator and issue workload assertions.</param>
    /// <param name="humanToken">The human administrator's signed token.</param>
    /// <param name="command">The bootstrap command whose target must match the authenticated subject.</param>
    /// <param name="administrator">The expected authenticated administrator.</param>
    /// <param name="cancellationToken">Cancels request creation.</param>
    /// <returns>The original buffered and signed request, or the creator's existing refusal.</returns>
    internal async Task<HttpRequestMessage?> CreateProductionBootstrapRequestAsync(IServiceProvider services,
        string humanToken, SubmitCommandRequest command, string administrator, CancellationToken cancellationToken)
    {
        using IServiceScope scope = services.CreateScope();
        IConfiguration configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        using DaprClient dapr = new DaprClientBuilder().UseHttpEndpoint("http://127.0.0.1:3500").Build();
        HttpRequestMessage? request = await TenantBootstrapBoundCommand.CreateAsync(scope.ServiceProvider, configuration,
            dapr, command, administrator, humanToken, TimeProvider.System, TimeSpan.FromSeconds(1), cancellationToken);
        if (request is null) return null;
        request.RequestUri = new Uri("/api/v1/commands/bound/workload", UriKind.Relative);
        request.Headers.Add(DaprAppChannelToken.HeaderName, _channelToken);
        request.Headers.Add(EventStoreWorkloadAuthenticationDefaults.DaprCallerHeaderName, workload);
        return request;
    }

    /// <inheritdoc/>
    public void Dispose() => _proofKey.Dispose();
}
