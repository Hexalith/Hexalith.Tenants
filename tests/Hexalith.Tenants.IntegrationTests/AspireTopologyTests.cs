#pragma warning disable CA2007

using System.Globalization;
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
using Hexalith.EventStore.Models;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;
using Hexalith.Memories.Client.Rest;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Projections;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.IntegrationTests.Fixtures;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Shouldly;

using RedisConfigurationOptions = StackExchange.Redis.ConfigurationOptions;
using RedisConnection = StackExchange.Redis.ConnectionMultiplexer;
using RedisConnectionException = StackExchange.Redis.RedisConnectionException;
using RedisConnectionMultiplexer = StackExchange.Redis.IConnectionMultiplexer;
using RedisDatabase = StackExchange.Redis.IDatabase;
using RedisValue = StackExchange.Redis.RedisValue;

using CommandStatus = Hexalith.EventStore.Contracts.Commands.CommandStatus;
using SubmitCommandRequest = Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest;

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
    private const string BootstrapGlobalAdminUserId = "11111111-1111-1111-1111-111111111111";
    private const string GlobalAdminExtensionKey = "actor:globalAdmin";
    private static readonly JsonSerializerOptions CommandPayloadJsonOptions = new() {
        Converters = { new JsonStringEnumConverter() },
    };
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan CommandStatusTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan SampleProjectionTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan TenantsApiAlivenessTimeout = TimeSpan.FromMinutes(4);

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
    [Trait("Tier", "3")]
    public async Task Generated_tenants_api_get_tenant_reads_verified_redis_state_with_projection_authority() {
        _fixture.SkipIfUnavailable();
        await WaitForTenantsApiAliveAsync();

        string token = CreateDemoJwt();
        string tenantId = $"provenance-{Guid.NewGuid():N}";
        string tenantName = $"Provenance {Guid.NewGuid():N}";
        const string tenantDescription = "Created by the Story 4.7 persisted-route proof";
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        CommandStatusResponse bootstrapStatus = await SubmitAndWaitForTerminalStatusAsync(
            _fixture.CommandApiClient,
            CreateCommand(
                "global-administrators",
                "global-administrators",
                nameof(BootstrapGlobalAdmin),
                new BootstrapGlobalAdmin(BootstrapGlobalAdminUserId)),
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
                new CreateTenant(tenantId, tenantName, tenantDescription)),
            token,
            timeout.Token);
        if (createStatus.Status == "PublishFailed") {
            Assert.Skip($"Aspire pub/sub publication is unavailable: {createStatus.FailureReason ?? "unknown reason"}");
        }

        createStatus.Status.ShouldBe("Completed");
        createStatus.TenantId.ShouldBe("system");
        createStatus.Domain.ShouldBe("tenants");
        createStatus.AggregateId.ShouldBe(tenantId);
        createStatus.EventCount.HasValue.ShouldBeTrue();
        createStatus.EventCount.Value.ShouldBeGreaterThan(0);
        createStatus.CommittedEventSequence.HasValue.ShouldBeTrue();
        createStatus.CommittedEventSequence.Value.ShouldBeGreaterThan(0);

        TenantReadModel persisted = await WaitForPersistedTenantAsync(tenantId, timeout.Token);
        persisted.TenantId.ShouldBe(tenantId);
        persisted.Name.ShouldBe(tenantName);
        persisted.Description.ShouldBe(tenantDescription);
        persisted.Status.ShouldBe(TenantStatus.Active);
        persisted.ProjectedAt.ShouldNotBeNull();
        persisted.ProjectionVersion.ShouldBe(
            TenantProjectionVersionFormat.SequencePrefix
            + createStatus.CommittedEventSequence.Value.ToString(CultureInfo.InvariantCulture));

        var eventStoreQuery = new SubmitQueryRequest(
            "system",
            GetTenantQuery.Domain,
            tenantId,
            GetTenantQuery.QueryType,
            GetTenantQuery.ProjectionType,
            EntityId: tenantId);
        using var eventStoreRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/queries") {
            Content = JsonContent.Create(eventStoreQuery, options: WebJsonOptions),
        };
        eventStoreRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        eventStoreRequest.Headers.IfNoneMatch.ParseAdd("\"conflicting-validator\"");
        using HttpResponseMessage eventStoreResponse = await _fixture.CommandApiClient.SendAsync(
            eventStoreRequest,
            timeout.Token);

        eventStoreResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        eventStoreResponse.Headers.GetValues("X-Hexalith-Query-Provenance")
            .ShouldHaveSingleItem()
            .ShouldBe("ProjectionBacked");
        EntityTagHeaderValue eventStoreETag = eventStoreResponse.Headers.ETag.ShouldNotBeNull();
        eventStoreETag.IsWeak.ShouldBeFalse();
        eventStoreETag.Tag.Trim('"').ShouldNotBeNullOrWhiteSpace();
        eventStoreResponse.Headers.GetValues(ProjectionLifecyclePolicy.HeaderName)
            .ShouldHaveSingleItem()
            .ShouldBe(nameof(ProjectionLifecycleState.Current));
        SubmitQueryResponse eventStoreResult = (await eventStoreResponse.Content.ReadFromJsonAsync<SubmitQueryResponse>(
            WebJsonOptions,
            timeout.Token)).ShouldNotBeNull();
        eventStoreResult.Success.ShouldBeTrue();
        TenantDetail eventStorePayload = eventStoreResult.Payload
            .Deserialize<TenantDetail>(WebJsonOptions)
            .ShouldNotBeNull();
        AssertTenantDetailMatchesPersisted(eventStorePayload, persisted);
        QueryResponseMetadata eventStoreMetadata = eventStoreResult.Metadata.ShouldNotBeNull();
        eventStoreMetadata.Provenance.ShouldBe(QueryResponseProvenance.ProjectionBacked);
        eventStoreMetadata.Lifecycle.ShouldBe(ProjectionLifecycleState.Current);
        eventStoreMetadata.ETag.ShouldBe(eventStoreETag?.Tag.Trim('"'));
        eventStoreMetadata.IsNotModified.ShouldBe(false);
        eventStoreMetadata.ProjectionVersion.ShouldBe(persisted.ProjectionVersion);
        eventStoreMetadata.IsStale.ShouldBe(false);
        eventStoreMetadata.IsDegraded.ShouldBeNull();

        using var rawRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/tenants/{tenantId}");
        rawRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        rawRequest.Headers.IfNoneMatch.ParseAdd("\"conflicting-validator\"");
        using HttpResponseMessage rawResponse = await _fixture.TenantsApiClient.SendAsync(
            rawRequest,
            timeout.Token);
        string rawContent = await rawResponse.Content.ReadAsStringAsync(timeout.Token);

        rawResponse.StatusCode.ShouldBe(HttpStatusCode.OK, rawContent);
        rawResponse.Headers.GetValues("X-Hexalith-Query-Provenance").ShouldHaveSingleItem().ShouldBe("ProjectionBacked");
        EntityTagHeaderValue? rawETag = rawResponse.Headers.ETag;
        rawETag.ShouldNotBeNull().IsWeak.ShouldBeFalse();
        rawETag.Tag.Trim('"').ShouldBe(eventStoreMetadata.ETag);
        rawResponse.Headers.GetValues("X-Hexalith-Projection-Version")
            .ShouldHaveSingleItem()
            .ShouldBe(persisted.ProjectionVersion);
        rawResponse.Headers.GetValues("X-Hexalith-Is-Stale").ShouldHaveSingleItem().ShouldBe("false");
        rawResponse.Headers.Contains("X-Hexalith-Is-Degraded").ShouldBeFalse();
        rawResponse.Headers.GetValues(ProjectionLifecyclePolicy.HeaderName)
            .ShouldHaveSingleItem()
            .ShouldBe(nameof(ProjectionLifecycleState.Current));

        using JsonDocument rawDocument = JsonDocument.Parse(rawContent);
        JsonElement rawPayload = rawDocument.RootElement;
        TenantDetail rawTenant = rawPayload.Deserialize<TenantDetail>(WebJsonOptions).ShouldNotBeNull();
        AssertTenantDetailMatchesPersisted(rawTenant, persisted);
        rawPayload.TryGetProperty("metadata", out _).ShouldBeFalse();
        rawPayload.TryGetProperty("projectionVersion", out _).ShouldBeFalse();
        rawPayload.TryGetProperty("projectedAt", out _).ShouldBeFalse();

        EntityTagHeaderValue? typedResponseETag = null;
        using HttpClient typedHttp = CreateIsolatedTenantsApiClient(
            _fixture.TenantsApiClient,
            token,
            eTag => typedResponseETag = eTag);
        var client = new TenantsRestQueryClient(typedHttp);

        TenantsRestQueryResponse<TenantDetail> typed = await client.GetTenantAsync(
            new GetTenantQuery { TenantId = tenantId },
            "conflicting-validator",
            timeout.Token);

        typed.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
        typed.StatusCode.ShouldBe((int)HttpStatusCode.OK);
        AssertTenantDetailMatchesPersisted(typed.Payload.ShouldNotBeNull(), persisted);
        typed.Metadata.Provenance.ShouldBe(QueryResponseProvenance.ProjectionBacked);
        typed.Metadata.Lifecycle.ShouldBe(ProjectionLifecycleState.Current);
        typedResponseETag.ShouldNotBeNull().IsWeak.ShouldBeFalse();
        typedResponseETag.Tag.Trim('"').ShouldBe(eventStoreMetadata.ETag);
        typed.Metadata.ETag.ShouldBe(eventStoreMetadata.ETag);
        typed.Metadata.IsNotModified.ShouldBe(false);
        typed.Metadata.ProjectionVersion.ShouldBe(persisted.ProjectionVersion);
        typed.Metadata.IsStale.ShouldBe(false);
        typed.Metadata.IsDegraded.ShouldBeNull();
    }

    private static void AssertTenantDetailMatchesPersisted(
        TenantDetail actual,
        TenantReadModel persisted) {
        actual.TenantId.ShouldBe(persisted.TenantId);
        actual.Name.ShouldBe(persisted.Name);
        actual.Description.ShouldBe(persisted.Description);
        actual.Status.ShouldBe(persisted.Status);
        actual.CreatedAt.ShouldBe(persisted.CreatedAt);
        actual.Members.Count.ShouldBe(persisted.Members.Count);
        foreach (KeyValuePair<string, TenantRole> member in persisted.Members) {
            actual.Members.ShouldContain(candidate =>
                candidate.UserId == member.Key && candidate.Role == member.Value);
        }

        actual.Configuration.Count.ShouldBe(persisted.Configuration.Count);
        foreach (KeyValuePair<string, string> setting in persisted.Configuration) {
            actual.Configuration.TryGetValue(setting.Key, out string? value).ShouldBeTrue();
            value.ShouldBe(setting.Value);
        }
    }

    [DaprFact]
    public async Task DomainServiceProcessRejectsUnauthenticatedDirectRequest() {
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

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [DaprFact]
    [Trait("Tier", "3")]
    public async Task Aha_moment_demo_revokes_sample_access_from_tenant_events() {
        _fixture.SkipIfUnavailable();

        string token = CreateDemoJwt();
        string tenantId = $"aha-{Guid.NewGuid():N}";
        string userId = $"jane-{Guid.NewGuid():N}";
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var statusClient = new HttpClient {
            BaseAddress = _fixture.CommandApiClient.BaseAddress,
            Timeout = TimeSpan.FromSeconds(60),
        };
        statusClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var gatewayClient = new EventStoreGatewayClient(
            statusClient,
            Options.Create(new EventStoreGatewayClientOptions()));
        var commandGateway = new TenantCommandGateway(gatewayClient, new UlidFactory(), statusClient);

        CommandStatusResponse bootstrapStatus = await SubmitAndWaitForTerminalStatusAsync(
            _fixture.CommandApiClient,
            CreateCommand(
                "global-administrators",
                "global-administrators",
                nameof(BootstrapGlobalAdmin),
                new BootstrapGlobalAdmin(BootstrapGlobalAdminUserId)),
            token,
            timeout.Token,
            allowAlreadyBootstrappedConflict: true);

        (bootstrapStatus.Status == "Completed"
            || (bootstrapStatus.Status == "Rejected" && bootstrapStatus.RejectionEventType == "GlobalAdminAlreadyBootstrappedRejection"))
            .ShouldBeTrue($"Bootstrap status was {bootstrapStatus.Status}:{bootstrapStatus.RejectionEventType}.");

        SubmitCommandRequest createCommand = CreateCommand(
            "tenants",
            tenantId,
            nameof(CreateTenant),
            new CreateTenant(tenantId, "Aha Moment Demo Tenant", "Created by Story 8.4 E2E test"));
        CommandStatusResponse createStatus = await SubmitAndWaitForTerminalStatusAsync(
            _fixture.CommandApiClient,
            createCommand,
            token,
            timeout.Token);
        if (createStatus.Status == "PublishFailed") {
            Assert.Skip($"Aspire pub/sub publication is unavailable: {createStatus.FailureReason ?? "unknown reason"}");
        }

        createStatus.Status.ShouldBe("Completed");
        createStatus.TenantId.ShouldBe("system");
        createStatus.Domain.ShouldBe("tenants");
        createStatus.AggregateId.ShouldBe(tenantId);
        createStatus.EventCount.HasValue.ShouldBeTrue();
        createStatus.EventCount.Value.ShouldBeGreaterThan(0);
        createStatus.CommittedEventSequence.HasValue.ShouldBeTrue();
        createStatus.CommittedEventSequence.Value.ShouldBeGreaterThan(0);
        TenantCommandStatusResult verifiedCreate = await commandGateway.GetStatusAsync(
            new TenantCommandTrackingHandle(createCommand.MessageId, createStatus.CorrelationId, tenantId),
            timeout.Token);
        verifiedCreate.Status.ShouldBe(CommandStatus.Completed);
        verifiedCreate.HasVerifiedCommandIdentity.ShouldBeTrue();
        verifiedCreate.CommittedEventSequence.ShouldBe(createStatus.CommittedEventSequence);

        SubmitCommandRequest addCommand = CreateCommand(
            "tenants",
            tenantId,
            nameof(AddUserToTenant),
            new AddUserToTenant(tenantId, userId, TenantRole.TenantContributor));
        CommandStatusResponse addStatus = await SubmitAndWaitForTerminalStatusAsync(
            _fixture.CommandApiClient,
            addCommand,
            token,
            timeout.Token);
        addStatus.Status.ShouldBe("Completed");
        addStatus.TenantId.ShouldBe("system");
        addStatus.Domain.ShouldBe("tenants");
        addStatus.AggregateId.ShouldBe(tenantId);
        addStatus.EventCount.HasValue.ShouldBeTrue();
        addStatus.EventCount.Value.ShouldBeGreaterThan(0);
        addStatus.CommittedEventSequence.HasValue.ShouldBeTrue();
        addStatus.CommittedEventSequence.Value.ShouldBe(
            createStatus.CommittedEventSequence.Value + addStatus.EventCount.Value);
        TenantCommandStatusResult verifiedAdd = await commandGateway.GetStatusAsync(
            new TenantCommandTrackingHandle(addCommand.MessageId, addStatus.CorrelationId, tenantId),
            timeout.Token);
        verifiedAdd.Status.ShouldBe(CommandStatus.Completed);
        verifiedAdd.HasVerifiedCommandIdentity.ShouldBeTrue();
        verifiedAdd.CommittedEventSequence.ShouldBe(addStatus.CommittedEventSequence);

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
        auditSnapshot.Kind.ShouldBe(TenantAuditSurfaceKind.Ready);
        auditSnapshot.Reason.ShouldBe(TenantAuditReason.None);
        auditSnapshot.Freshness.ShouldBe(ReadModelFreshnessState.Current);
        auditSnapshot.Lifecycle.ShouldBe(ProjectionLifecycleState.Current);
        auditSnapshot.Rows.Select(static row => row.EventType).ShouldContain(nameof(TenantCreated));
        auditSnapshot.Rows.Select(static row => row.EventType).ShouldContain(nameof(UserAddedToTenant));
        auditSnapshot.Rows.Select(static row => row.EventType).ShouldContain(nameof(UserRemovedFromTenant));
        auditSnapshot.Rows.ShouldAllBe(row => row.TenantId == tenantId);
        auditSnapshot.Rows.ShouldAllBe(row => row.Provenance == QueryResponseProvenance.ProjectionBacked);
        TenantAuditRow removal = auditSnapshot.Rows.Single(row => row.EventType == nameof(UserRemovedFromTenant));
        TenantAuditReceipt.FromRow(removal).State.ShouldBe(TenantAuditReceiptState.Ready);
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

    private static async Task<TenantReadModel> WaitForPersistedTenantAsync(
        string tenantId,
        CancellationToken cancellationToken) {
        string redisEndpoint = $"localhost:{DaprDiagnostics.DefaultRedisPort}";
        string persistedKey = $"tenants||projection:tenants:{tenantId}";
        RedisConnectionMultiplexer redis;
        try {
            redis = await RedisConnection.ConnectAsync(new RedisConfigurationOptions {
                EndPoints = { redisEndpoint },
                ConnectTimeout = 5_000,
                SyncTimeout = 5_000,
                AbortOnConnectFail = true,
                AllowAdmin = false,
            });
        }
        catch (RedisConnectionException ex) {
            throw new InvalidOperationException(
                $"Redis at '{redisEndpoint}' was unreachable while waiting for persisted tenant '{tenantId}'. {ex.Message}",
                ex);
        }

        using (redis) {
            RedisDatabase database = redis.GetDatabase();
            DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(SampleProjectionTimeout);
            string? lastPayload = null;
            string? lastJsonError = null;

            while (DateTimeOffset.UtcNow <= deadline) {
                cancellationToken.ThrowIfCancellationRequested();
                RedisValue value;
                try {
                    value = await database
                        .HashGetAsync(persistedKey, "data")
                        .WaitAsync(cancellationToken);
                }
                catch (RedisConnectionException ex) {
                    throw new InvalidOperationException(
                        $"Redis at '{redisEndpoint}' dropped the connection while waiting for '{persistedKey}'. {ex.Message}",
                        ex);
                }

                if (value.HasValue) {
                    lastPayload = value.ToString();
                    TenantReadModel? model;
                    try {
                        model = JsonSerializer.Deserialize<TenantReadModel>(lastPayload, WebJsonOptions);
                        lastJsonError = null;
                    }
                    catch (JsonException ex) {
                        lastJsonError = ex.Message;
                        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                        continue;
                    }

                    if (model is not null
                        && string.Equals(model.TenantId, tenantId, StringComparison.Ordinal)) {
                        return model;
                    }
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            }

            string jsonSuffix = lastJsonError is null
                ? string.Empty
                : $" Last JSON error: {lastJsonError}.";
            throw new TimeoutException(
                $"Redis key '{persistedKey}' did not contain the expected tenant read model within {SampleProjectionTimeout}. "
                + $"Last payload present: {lastPayload is not null}.{jsonSuffix}");
        }
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
        while (true) {
            EventStoreQueryResult rawResult = await gatewayClient
                .SubmitQueryAsync(auditQuery, cancellationToken: cancellationToken);
            if (rawResult.Payload is JsonElement rawPayload
                && rawPayload.TryGetProperty("items", out JsonElement auditItems)
                && auditItems.ValueKind == JsonValueKind.Array
                && auditItems.GetArrayLength() > 0
                && rawResult.Metadata is QueryResponseMetadata metadata
                && metadata.Provenance == QueryResponseProvenance.ProjectionBacked
                && metadata.Lifecycle == ProjectionLifecycleState.Current) {
                string?[] eventTypes = auditItems.EnumerateArray()
                    .Select(static item => GetStringProperty(item, "eventType")).ToArray();
                if (eventTypes.Contains(nameof(TenantCreated))
                    && eventTypes.Contains(nameof(UserAddedToTenant))
                    && eventTypes.Contains(nameof(UserRemovedFromTenant))) {
                    foreach (JsonElement auditItem in auditItems.EnumerateArray()) {
                        GetStringProperty(auditItem, "tenantId").ShouldBe(tenantId);
                    }

                    break;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }
        using var memoriesHttpClient = new HttpClient { BaseAddress = new Uri("https://memories.invalid") };
        var memoriesClient = new MemoriesClient(
            memoriesHttpClient,
            Options.Create(new MemoriesClientOptions()),
            NullLogger<MemoriesClient>.Instance);
        var gateway = new TenantQueryGateway(
            new AuditRestQueryClientAdapter(gatewayClient),
            new FixedUserContextAccessor("system", BootstrapGlobalAdminUserId),
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
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AspireTopologyFixture.DemoSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        Claim[] claims =
        [
            new("sub", BootstrapGlobalAdminUserId),
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

    /// <summary>
    /// Bridges the audit read onto the EventStore gateway for this topology assertion only.
    /// </summary>
    /// <remarks>
    /// This is NOT the production transport. Story 1.10 routes the six UI reads through
    /// <c>TenantsRestQueryClient</c> against the <c>tenants-api</c> resource, and that agreement is proved
    /// in process against the generated controllers by
    /// <c>TenantsApiGeneratedControllerTests.Direct_rest_client_routes_match_the_generated_controllers_and_parse_their_real_headers</c>
    /// -- routes, query strings, metadata headers and the conditional 304 path, all against the real
    /// emitter. What remains uncovered, and is recorded as an owned limitation rather than implied here, is
    /// a live socket-level probe of all six routes against the running topology: driving them through the
    /// real client against <c>tenants-api</c> times out at the client's 60 s bound in the local
    /// slim-mode topology, so this lane cannot serve as that oracle. This adapter exists solely so the
    /// audit-consumer assertion below can reach persisted evidence.
    /// </remarks>
    private sealed class AuditRestQueryClientAdapter(IEventStoreGatewayClient client) : ITenantsRestQueryClient {
        public Task<TenantsRestQueryResponse<PaginatedResult<TenantAuditEntry>>> GetTenantAuditAsync(
            GetTenantAuditQuery query,
            string? eTag,
            CancellationToken cancellationToken = default)
            => ConvertAsync(client.SubmitQueryAsync<PaginatedResult<TenantAuditEntry>>(
                new SubmitQueryRequest(
                    "system",
                    GetTenantAuditQuery.Domain,
                    query.TenantId,
                    GetTenantAuditQuery.QueryType,
                    GetTenantAuditQuery.ProjectionType,
                    JsonSerializer.SerializeToElement(new {
                        from = query.From,
                        to = query.To,
                        category = query.Category?.ToString(),
                        cursor = query.Cursor,
                        pageSize = query.PageSize,
                    }),
                    EntityId: query.TenantId),
                eTag,
                cancellationToken));

        public Task<TenantsRestQueryResponse<PaginatedResult<TenantSummary>>> ListTenantsAsync(
            ListTenantsQuery query,
            string? eTag,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantsRestQueryResponse<TenantDetail>> GetTenantAsync(
            GetTenantQuery query,
            string? eTag,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantsRestQueryResponse<PaginatedResult<TenantMember>>> GetTenantUsersAsync(
            GetTenantUsersQuery query,
            string? eTag,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantsRestQueryResponse<PaginatedResult<UserTenantMembership>>> GetUserTenantsAsync(
            GetUserTenantsQuery query,
            string? eTag,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantsRestQueryResponse<PaginatedResult<GlobalAdministratorSummary>>> GetGlobalAdministratorsAsync(
            GetGlobalAdministratorsQuery query,
            string? eTag,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        /// <summary>Converts a gateway result that has already been proven successful.</summary>
        /// <remarks>
        /// <c>EventStoreGatewayClient</c> throws <c>EventStoreGatewayException</c> for non-success
        /// HTTP responses, which the UI gateway maps separately. A successful query may have no
        /// payload when the requested read model is absent; the caller evaluates that evidence.
        /// The live audit check waits for non-empty projected rows before invoking this adapter.
        /// </remarks>
        private static async Task<TenantsRestQueryResponse<TPayload>> ConvertAsync<TPayload>(
            Task<EventStoreQueryResult<TPayload>> resultTask) {
            EventStoreQueryResult<TPayload> result = await resultTask.ConfigureAwait(false);
            QueryResponseMetadata metadata = (result.Metadata ?? new QueryResponseMetadata()) with {
                ETag = result.ETag ?? result.Metadata?.ETag,
                IsNotModified = result.IsNotModified,
            };
            return new(
                result.Payload,
                metadata,
                TenantsRestQueryFailureKind.None,
                result.IsNotModified ? (int)HttpStatusCode.NotModified : (int)HttpStatusCode.OK);
        }
    }

    private sealed record FixedUserContextAccessor(string? TenantId, string? UserId) : IUserContextAccessor;

    private async Task WaitForTenantsApiAliveAsync() {
        using var timeout = new CancellationTokenSource(TenantsApiAlivenessTimeout);
        HttpStatusCode? lastStatus = null;
        string? lastError = null;

        while (!timeout.IsCancellationRequested) {
            try {
                using HttpResponseMessage response = await _fixture.TenantsApiClient.GetAsync("/alive", timeout.Token);
                lastStatus = response.StatusCode;
                lastError = null;
                if (response.StatusCode == HttpStatusCode.OK) {
                    return;
                }
            }
            catch (HttpRequestException ex) {
                lastError = ex.Message;
            }
            catch (TaskCanceledException) when (timeout.IsCancellationRequested) {
                break;
            }
            catch (TaskCanceledException) {
                lastError = "request timed out";
            }

            try {
                await Task.Delay(TimeSpan.FromSeconds(2), timeout.Token);
            }
            catch (TaskCanceledException) {
                break;
            }
        }

        throw new TimeoutException(
            $"tenants-api /alive did not return HTTP 200 within {TenantsApiAlivenessTimeout}. "
            + $"Last status: {lastStatus?.ToString() ?? "n/a"}, Last error: {lastError ?? "n/a"}.");
    }

    private static HttpClient CreateIsolatedTenantsApiClient(
        HttpClient shared,
        string token,
        Action<EntityTagHeaderValue?> observeETag) {
        ArgumentNullException.ThrowIfNull(shared);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentNullException.ThrowIfNull(observeETag);

        var client = new HttpClient(new SharedClientRelayHandler(shared, observeETag), disposeHandler: true) {
            BaseAddress = shared.BaseAddress,
            Timeout = shared.Timeout,
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private sealed class SharedClientRelayHandler(
        HttpClient inner,
        Action<EntityTagHeaderValue?> observeETag) : HttpMessageHandler {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            ArgumentNullException.ThrowIfNull(request);
            var clone = new HttpRequestMessage(request.Method, request.RequestUri) {
                Version = request.Version,
                VersionPolicy = request.VersionPolicy,
            };
            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers) {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            HttpResponseMessage response = await inner.SendAsync(clone, cancellationToken).ConfigureAwait(false);
            observeETag(response.Headers.ETag);
            return response;
        }
    }
}
