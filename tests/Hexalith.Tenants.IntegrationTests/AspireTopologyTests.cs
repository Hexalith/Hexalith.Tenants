#pragma warning disable CA2007

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Commons.UniqueIds;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Models;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.Memories.Client.Rest;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.IntegrationTests.Fixtures;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantAudit;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>
/// Aspire topology smoke tests that verify the full AppHost starts correctly
/// and the end-to-end command pipeline works through the Aspire orchestration layer.
/// </summary>
[Collection("AspireTopology")]
[DaprTestSerialization]
[Trait("Category", "Integration")]
public class AspireTopologyTests : IDisposable {
    private const string JwtAudience = "hexalith-eventstore";
    private const string JwtIssuer = "hexalith-dev";
    private const string JwtSigningKey = "DevOnlySigningKey-AtLeast32Chars!";
    private const string GlobalAdminExtensionKey = "actor:globalAdmin";
    private static readonly JsonSerializerOptions CommandPayloadJsonOptions = new() {
        Converters = { new JsonStringEnumConverter() },
    };
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan CommandStatusTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan SampleProjectionTimeout = TimeSpan.FromSeconds(60);

    private readonly IDisposable _daprTestLease;
    private readonly AspireTopologyFixture _fixture;

    public AspireTopologyTests(AspireTopologyFixture fixture) {
        _daprTestLease = DaprTestExecutionGate.Enter();
        _fixture = fixture;
    }

    public void Dispose() {
        _daprTestLease.Dispose();
        GC.SuppressFinalize(this);
    }

    [DaprFact]
    public async Task CommandApi_resource_starts_and_is_alive() {
        _fixture.SkipIfUnavailable();

        using HttpResponseMessage response = await _fixture.CommandApiClient.GetAsync("/alive");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [DaprFact]
    public async Task Tenants_resource_starts_and_is_alive() {
        _fixture.SkipIfUnavailable();

        using HttpResponseMessage response = await _fixture.TenantsClient.GetAsync("/alive");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [DaprFact]
    public async Task Tenants_resource_reports_ready_only_after_prepared_dependencies_are_available() {
        _fixture.SkipIfUnavailable();

        using HttpResponseMessage response = await _fixture.TenantsClient.GetAsync("/ready");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [DaprFact]
    public async Task Sample_resource_starts_and_is_alive() {
        _fixture.SkipIfUnavailable();

        using HttpResponseMessage response = await _fixture.SampleClient.GetAsync("/alive");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [DaprFact]
    public async Task CommandApi_process_endpoint_dispatches_command() {
        _fixture.SkipIfUnavailable();

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
                GlobalAdminExtensions()),
            null);

        using HttpResponseMessage response = await _fixture.TenantsClient.PostAsJsonAsync("/process", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DomainServiceWireResult? result = await response.Content.ReadFromJsonAsync<DomainServiceWireResult>();
        _ = result.ShouldNotBeNull();
        result.IsRejection.ShouldBeFalse();
        result.Events.Count.ShouldBe(1);
        result.Events[0].EventTypeName.ShouldEndWith("TenantCreated");
    }

    [DaprFact]
    [Trait("Tier", "3")]
    public async Task Aha_moment_demo_revokes_sample_access_from_tenant_events() {
        _fixture.SkipIfUnavailable();

        string token = CreateDemoJwt();
        string tenantId = $"aha-{Guid.NewGuid():N}";
        string userId = $"jane-{Guid.NewGuid():N}";
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        CommandStatusResponse bootstrapStatus = await SubmitAndWaitForTerminalStatusAsync(
            _fixture.CommandApiClient,
            CreateCommand(
                "global-administrators",
                "global-administrators",
                nameof(BootstrapGlobalAdmin),
                new BootstrapGlobalAdmin("admin-user")),
            token,
            timeout.Token,
            allowAlreadyBootstrappedConflict: true);

        (bootstrapStatus.Status == "Completed"
            || (bootstrapStatus.Status == "Rejected" && bootstrapStatus.RejectionEventType == "GlobalAdminAlreadyBootstrappedRejection"))
            .ShouldBeTrue($"Bootstrap status was {bootstrapStatus.Status}:{bootstrapStatus.RejectionEventType}.");

        CommandStatusResponse createStatus = await SubmitAndWaitForTerminalStatusAsync(
            _fixture.CommandApiClient,
            CreateCommand(
                "tenants",
                tenantId,
                nameof(CreateTenant),
                new CreateTenant(tenantId, "Aha Moment Demo Tenant", "Created by Story 8.4 E2E test")),
            token,
            timeout.Token);
        if (createStatus.Status == "PublishFailed") {
            Assert.Skip($"Aspire pub/sub publication is unavailable: {createStatus.FailureReason ?? "unknown reason"}");
        }

        createStatus.Status.ShouldBe("Completed");

        CommandStatusResponse addStatus = await SubmitAndWaitForTerminalStatusAsync(
            _fixture.CommandApiClient,
            CreateCommand(
                "tenants",
                tenantId,
                nameof(AddUserToTenant),
                new AddUserToTenant(tenantId, userId, TenantRole.TenantContributor)),
            token,
            timeout.Token);
        addStatus.Status.ShouldBe("Completed");

        JsonElement granted = await WaitForAccessAsync(tenantId, userId, "granted", timeout.Token);
        GetStringProperty(granted, "role").ShouldBe(nameof(TenantRole.TenantContributor));

        CommandStatusResponse removeStatus = await SubmitAndWaitForTerminalStatusAsync(
            _fixture.CommandApiClient,
            CreateCommand(
                "tenants",
                tenantId,
                nameof(RemoveUserFromTenant),
                new RemoveUserFromTenant(tenantId, userId)),
            token,
            timeout.Token);
        removeStatus.Status.ShouldBe("Completed");

        JsonElement denied = await WaitForAccessAsync(tenantId, userId, "denied", timeout.Token);
        GetStringProperty(denied, "reason").ShouldBe("User is not a member");

        TenantAuditSnapshot auditSnapshot = await LoadPersistedAuditConsumerSnapshotAsync(
            tenantId,
            token,
            timeout.Token);
        auditSnapshot.Kind.ShouldBe(TenantAuditSurfaceKind.Degraded);
        auditSnapshot.Reason.ShouldBe(TenantAuditReason.MissingPayload);
        auditSnapshot.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
        auditSnapshot.Lifecycle.ShouldBe(ProjectionLifecycleState.Unknown);
        auditSnapshot.Rows.ShouldBeEmpty(
            "the pre-Story-4.7 producer alias must not become correction-eligible audit evidence.");
    }

    private static Dictionary<string, string> GlobalAdminExtensions()
        => new(StringComparer.OrdinalIgnoreCase) { [GlobalAdminExtensionKey] = "true" };

    private static Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest CreateCommand<TPayload>(
        string domain,
        string aggregateId,
        string commandType,
        TPayload payload)
        where TPayload : class {
        ArgumentNullException.ThrowIfNull(payload);

        return new Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest(
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            "system",
            domain,
            aggregateId,
            commandType,
            JsonSerializer.SerializeToElement(payload, CommandPayloadJsonOptions));
    }

    private static async Task<CommandStatusResponse> SubmitAndWaitForTerminalStatusAsync(
        HttpClient client,
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request,
        string token,
        CancellationToken cancellationToken,
        bool allowAlreadyBootstrappedConflict = false) {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/commands") {
            Content = JsonContent.Create(request, options: WebJsonOptions),
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);
        if (allowAlreadyBootstrappedConflict && response.StatusCode == HttpStatusCode.Conflict) {
            return new CommandStatusResponse(
                request.MessageId,
                "Rejected",
                StatusCode: (int)CommandStatus.Rejected,
                DateTimeOffset.UtcNow,
                request.AggregateId,
                EventCount: 1,
                RejectionEventType: "GlobalAdminAlreadyBootstrappedRejection",
                FailureReason: null,
                TimeoutDuration: null);
        }

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        Hexalith.EventStore.Contracts.Commands.SubmitCommandResponse? accepted = await response.Content.ReadFromJsonAsync<Hexalith.EventStore.Contracts.Commands.SubmitCommandResponse>(
            WebJsonOptions,
            cancellationToken);
        _ = accepted.ShouldNotBeNull();
        accepted.CorrelationId.ShouldNotBeNullOrWhiteSpace();

        return await WaitForTerminalStatusAsync(client, accepted.CorrelationId, token, cancellationToken);
    }

    private static async Task<CommandStatusResponse> WaitForTerminalStatusAsync(
        HttpClient client,
        string correlationId,
        string token,
        CancellationToken cancellationToken) {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(CommandStatusTimeout);
        CommandStatusResponse? lastStatus = null;

        while (DateTimeOffset.UtcNow <= deadline) {
            using var message = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/commands/status/{correlationId}");
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK) {
                lastStatus = await response.Content.ReadFromJsonAsync<CommandStatusResponse>(
                    WebJsonOptions,
                    cancellationToken);
                _ = lastStatus.ShouldNotBeNull();

                if (lastStatus.Status is "Completed" or "Rejected" or "PublishFailed" or "TimedOut") {
                    return lastStatus;
                }
            }
            else {
                response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new TimeoutException(
            $"Command {correlationId} did not reach a terminal status within {CommandStatusTimeout}. Last status: {lastStatus?.Status ?? "not found"}.");
    }

    private async Task<JsonElement> WaitForAccessAsync(
        string tenantId,
        string userId,
        string expectedAccess,
        CancellationToken cancellationToken) {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(SampleProjectionTimeout);
        string lastAccess = "not found";

        while (DateTimeOffset.UtcNow <= deadline) {
            using HttpResponseMessage response = await _fixture.SampleClient.GetAsync(
                $"/access/{tenantId}/{userId}",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.OK) {
                JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(
                    WebJsonOptions,
                    cancellationToken);
                lastAccess = GetStringProperty(body, "access") ?? "missing";
                if (lastAccess == expectedAccess) {
                    return body.Clone();
                }
            }
            else {
                response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new TimeoutException(
            $"Sample projection did not report access '{expectedAccess}' for {tenantId}/{userId} within {SampleProjectionTimeout}. Last access: {lastAccess}.");
    }

    private async Task<TenantAuditSnapshot> LoadPersistedAuditConsumerSnapshotAsync(
        string tenantId,
        string token,
        CancellationToken cancellationToken) {
        using var eventStoreHttpClient = new HttpClient {
            BaseAddress = _fixture.CommandApiClient.BaseAddress,
            Timeout = TimeSpan.FromSeconds(60),
        };
        eventStoreHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var gatewayClient = new EventStoreGatewayClient(
            eventStoreHttpClient,
            Options.Create(new EventStoreGatewayClientOptions()));
        var auditQuery = new SubmitQueryRequest(
            "system",
            GetTenantAuditQuery.Domain,
            tenantId,
            GetTenantAuditQuery.QueryType,
            GetTenantAuditQuery.ProjectionType,
            JsonSerializer.SerializeToElement(new {
                from = (DateTimeOffset?)null,
                to = (DateTimeOffset?)null,
                category = (string?)null,
                cursor = (string?)null,
                pageSize = 50,
            }),
            EntityId: tenantId);
        EventStoreQueryResult rawResult = await gatewayClient
            .SubmitQueryAsync(auditQuery, cancellationToken: cancellationToken);
        JsonElement rawPayload = rawResult.Payload.ShouldNotBeNull();
        rawPayload.GetProperty("TenantId").GetString().ShouldBe(tenantId);
        rawPayload.GetProperty("ProjectedAt").ValueKind.ShouldBe(JsonValueKind.String);
        QueryResponseMetadata metadata = rawResult.Metadata.ShouldNotBeNull();
        metadata.Provenance.ShouldBe(QueryResponseProvenance.ProjectionBacked);
        using var memoriesHttpClient = new HttpClient { BaseAddress = new Uri("https://memories.invalid") };
        var memoriesClient = new MemoriesClient(
            memoriesHttpClient,
            Options.Create(new MemoriesClientOptions()),
            NullLogger<MemoriesClient>.Instance);
        var gateway = new TenantQueryGateway(
            gatewayClient,
            new FixedUserContextAccessor("system", "admin-user"),
            memoriesClient,
            new TenantSearchCursorCodec(new EphemeralDataProtectionProvider()));
        return await gateway.GetTenantAuditAsync(
            new TenantAuditRequest(tenantId),
            previous: null,
            cancellationToken);
    }

    private static string? GetStringProperty(JsonElement element, string propertyName) {
        foreach (JsonProperty property in element.EnumerateObject()) {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase)) {
                return property.Value.GetString();
            }
        }

        return null;
    }

    private static string CreateDemoJwt() {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        Claim[] claims =
        [
            new("sub", "admin-user"),
            new("tenants", "[\"system\"]"),
            new("domains", "[\"global-administrators\",\"tenants\"]"),
            new("permissions", "[\"command:submit\",\"query:read\"]"),
            new("roles", "[\"GlobalAdministrator\"]"),
        ];

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed record FixedUserContextAccessor(string? TenantId, string? UserId) : IUserContextAccessor;
}
