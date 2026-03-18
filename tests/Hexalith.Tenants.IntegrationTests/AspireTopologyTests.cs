#pragma warning disable CA2007

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.IntegrationTests.Fixtures;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>
/// Aspire topology smoke tests that verify the full AppHost starts correctly
/// and the end-to-end command pipeline works through the Aspire orchestration layer.
/// </summary>
[Collection("AspireTopology")]
[Trait("Category", "Integration")]
public class AspireTopologyTests
{
    private readonly AspireTopologyFixture _fixture;

    public AspireTopologyTests(AspireTopologyFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CommandApi_resource_starts_and_is_healthy()
    {
        using HttpResponseMessage response = await _fixture.CommandApiClient.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Sample_resource_starts_and_is_healthy()
    {
        using HttpResponseMessage response = await _fixture.SampleClient.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CommandApi_process_endpoint_dispatches_command()
    {
        string tenantId = $"aspire-test-{Guid.NewGuid():N}";
        var request = new DomainServiceRequest(
            new CommandEnvelope(
                Guid.NewGuid().ToString(),
                "system",
                "tenants",
                tenantId,
                nameof(CreateTenant),
                JsonSerializer.SerializeToUtf8Bytes(new CreateTenant(tenantId, "Aspire Topology Test Tenant", "Created by Aspire topology smoke test")),
                Guid.NewGuid().ToString(),
                null,
                "aspire-test-user",
                null),
            null);

        using HttpResponseMessage response = await _fixture.CommandApiClient.PostAsJsonAsync("/process", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DomainServiceWireResult? result = await response.Content.ReadFromJsonAsync<DomainServiceWireResult>();
        result.ShouldNotBeNull();
        result.IsRejection.ShouldBeFalse();
        result.Events.Count.ShouldBe(1);
        result.Events[0].EventTypeName.ShouldEndWith("TenantCreated");
    }
}
