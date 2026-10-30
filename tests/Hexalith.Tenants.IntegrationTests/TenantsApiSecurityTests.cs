extern alias TenantsApi;

using System.Net;
using System.Net.Http.Headers;
using System.Text;

using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Exercises the actual generated public API before any gateway dispatch.</summary>
public sealed class TenantsApiSecurityTests
{
    private const string SigningKey = "tenants-api-security-test-key-at-least-32-bytes";

    /// <summary>Resolves the shared policy and refuses absent or workload credentials at the real generated route.</summary>
    [Theory]
    [InlineData(false, HttpStatusCode.Unauthorized)]
    [InlineData(true, HttpStatusCode.Forbidden)]
    public async Task PublicGeneratedRouteRejectsNonHumanCredentials(bool workload, HttpStatusCode expected)
    {
        using WebApplicationFactory<TenantsApi::Program> factory = new WebApplicationFactory<TenantsApi::Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("EventStore:Authentication:SigningKey", SigningKey);
                builder.UseSetting("DAPR_HTTP_ENDPOINT", "http://127.0.0.1:4367");
            });
        using HttpClient client = factory.CreateClient();
        if (workload)
        {
            string token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = "hexalith-dev", Audience = "hexalith-eventstore", Expires = DateTime.UtcNow.AddMinutes(5),
                Claims = new Dictionary<string, object>
                {
                    ["sub"] = "api-security-test", [EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType] = "service-caller",
                },
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256),
            });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        using HttpResponseMessage response = await client.GetAsync("/api/tenants/security-test", TestContext.Current.CancellationToken).ConfigureAwait(true);
        response.StatusCode.ShouldBe(expected);
    }
}
