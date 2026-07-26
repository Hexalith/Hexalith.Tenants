using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.Memories.Client.Rest;
using Hexalith.Tenants.Contracts;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Configuration;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.UI.State.UserTenants;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using MemoriesSearchResult = Hexalith.Memories.Contracts.V1.SearchResult;
using MemoriesScoredResult = Hexalith.Memories.Contracts.V1.ScoredResult;
using MemoriesSourceType = Hexalith.Memories.Contracts.V1.SourceType;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

public sealed class TenantQueryGatewayTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Get_tenant_without_authenticated_user_fails_closed_without_querying_event_store(string? userId)
    {
        CapturingGatewayClient client = new();
        TenantQueryGateway gateway = CreateGateway(client, userId);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Unauthorized);
        client.SubmittedQueries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_tenant_submits_literal_detail_query_and_maps_counts_source()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            Detail("tenant.alpha"),
            metadata: ProjectionBackedMetadata(isStale: false, servedAt: DateTimeOffset.UtcNow));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha", ETag: "\"known\""), null, CancellationToken.None);

        SubmittedQuery query = client.SubmittedQueries[0];
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(GetTenantQuery.Domain);
        query.Request.ProjectionType.ShouldBe(GetTenantQuery.ProjectionType);
        query.Request.AggregateId.ShouldBe("tenant.alpha");
        query.Request.EntityId.ShouldBe("tenant.alpha");
        query.Request.QueryType.ShouldBe(GetTenantQuery.QueryType);
        query.IfNoneMatch.ShouldBe("\"known\"");
        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Ready);
        snapshot.Detail.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Current);
    }

    [Theory]
    [InlineData(QueryResponseProvenance.Unknown, false)]
    [InlineData(QueryResponseProvenance.Unknown, true)]
    [InlineData(QueryResponseProvenance.HandlerComputed, false)]
    [InlineData(QueryResponseProvenance.HandlerComputed, true)]
    [InlineData((QueryResponseProvenance)999, false)]
    public async Task Get_tenant_non_projection_backed_freshness_evidence_remains_unknown(
        QueryResponseProvenance provenance,
        bool isStale)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            Detail("tenant.alpha"),
            metadata: new QueryResponseMetadata(IsStale: isStale)
            {
                Provenance = provenance,
            });

        TenantQueryGateway gateway = CreateGateway(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Ready);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
    }

    [Theory]
    [InlineData(ProjectionLifecycleState.Current, true, ReadModelFreshnessState.Current, TenantDetailSurfaceKind.Ready)]
    [InlineData(ProjectionLifecycleState.Stale, false, ReadModelFreshnessState.Stale, TenantDetailSurfaceKind.Stale)]
    [InlineData(ProjectionLifecycleState.Rebuilding, false, ReadModelFreshnessState.Unknown, TenantDetailSurfaceKind.Ready)]
    [InlineData(ProjectionLifecycleState.Degraded, false, ReadModelFreshnessState.Unknown, TenantDetailSurfaceKind.Ready)]
    [InlineData(ProjectionLifecycleState.Unavailable, false, ReadModelFreshnessState.Unknown, TenantDetailSurfaceKind.Ready)]
    [InlineData(ProjectionLifecycleState.LocalOnly, false, ReadModelFreshnessState.Unknown, TenantDetailSurfaceKind.Ready)]
    [InlineData((ProjectionLifecycleState)999, false, ReadModelFreshnessState.Unknown, TenantDetailSurfaceKind.Ready)]
    public async Task Get_tenant_projection_lifecycle_precedes_legacy_stale_evidence(
        ProjectionLifecycleState lifecycle,
        bool isStale,
        ReadModelFreshnessState expectedFreshness,
        TenantDetailSurfaceKind expectedKind)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            Detail("tenant.alpha"),
            metadata: ProjectionBackedMetadata(isStale: isStale, lifecycle: lifecycle));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha"), null, CancellationToken.None);

        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Lifecycle.ShouldBe(
            lifecycle is >= ProjectionLifecycleState.Unknown and <= ProjectionLifecycleState.LocalOnly
                ? lifecycle
                : ProjectionLifecycleState.Unknown);

        // The surface kind is what the operator actually sees. Asserting freshness alone left the
        // Stale/Ready branch selection at TenantQueryGateway.cs:109-113 unpinned.
        snapshot.Kind.ShouldBe(expectedKind);
    }

    [Fact]
    public async Task Get_tenant_not_modified_refetches_without_etag_before_composing_current_state()
    {
        TenantDetailSnapshot previous = TenantDetailSnapshot.Ready(
            Detail("tenant.alpha"),
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current);
        CapturingGatewayClient client = new();
        client.EnqueueDetailNotModified("\"known\"");
        client.EnqueueQueryResult(Detail("tenant.alpha"), eTag: "\"known\"");

        TenantQueryGateway gateway = CreateGateway(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha", ETag: "\"known\""), previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Ready);
        snapshot.Detail.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
        snapshot.ETag.ShouldBe("\"known\"");
        client.SubmittedQueries.Count.ShouldBe(2);
        client.SubmittedQueries[1].IfNoneMatch.ShouldBeNull();
    }

    [Fact]
    public async Task Get_tenant_applies_stale_freshness_from_not_modified_response()
    {
        TenantDetailSnapshot previous = TenantDetailSnapshot.Ready(
            Detail("tenant.alpha"),
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current);
        CapturingGatewayClient client = new();
        client.EnqueueDetailNotModified("\"known\"", isStale: true);
        client.EnqueueQueryResult(
            Detail("tenant.alpha"),
            eTag: "\"known\"",
            metadata: ProjectionBackedMetadata(isStale: true));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha", ETag: "\"known\""), previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Stale);
        snapshot.Detail.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Stale);
    }

    [Theory]
    [InlineData(ProjectionLifecycleState.Current, true, TenantDetailSurfaceKind.Ready, ReadModelFreshnessState.Current)]
    [InlineData(ProjectionLifecycleState.Stale, false, TenantDetailSurfaceKind.Stale, ReadModelFreshnessState.Stale)]
    [InlineData(ProjectionLifecycleState.Rebuilding, false, TenantDetailSurfaceKind.Ready, ReadModelFreshnessState.Unknown)]
    [InlineData(ProjectionLifecycleState.Degraded, false, TenantDetailSurfaceKind.Ready, ReadModelFreshnessState.Unknown)]
    [InlineData(ProjectionLifecycleState.Unavailable, false, TenantDetailSurfaceKind.Ready, ReadModelFreshnessState.Unknown)]
    [InlineData(ProjectionLifecycleState.LocalOnly, false, TenantDetailSurfaceKind.Ready, ReadModelFreshnessState.Unknown)]
    [InlineData((ProjectionLifecycleState)999, false, TenantDetailSurfaceKind.Ready, ReadModelFreshnessState.Unknown)]
    public async Task Get_tenant_not_modified_discards_conditional_evidence_and_applies_refetched_lifecycle(
        ProjectionLifecycleState lifecycle,
        bool isStale,
        TenantDetailSurfaceKind expectedKind,
        ReadModelFreshnessState expectedFreshness)
    {
        TenantDetailSnapshot previous = TenantDetailSnapshot.Ready(
            Detail("tenant.alpha"),
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current);
        CapturingGatewayClient client = new();

        // The detail path re-queries unconditionally on 304 and resolves from the refetched response
        // alone. The 304 therefore carries deliberately CONTRADICTORY evidence: if a future change
        // started honouring conditional-response metadata, these assertions would fail instead of
        // silently passing. Enqueueing the same values in both responses made the 304 half inert.
        client.EnqueueDetailNotModified(
            "\"known\"",
            isStale: !isStale,
            lifecycle: lifecycle == ProjectionLifecycleState.Stale
                ? ProjectionLifecycleState.Current
                : ProjectionLifecycleState.Stale);
        client.EnqueueQueryResult(
            Detail("tenant.alpha"),
            eTag: "\"known\"",
            metadata: ProjectionBackedMetadata(isStale: isStale, lifecycle: lifecycle));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha", ETag: "\"known\""), previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.Freshness.ShouldBe(expectedFreshness);
        client.SubmittedQueries.Count.ShouldBe(2, "a 304 on the detail surface must trigger an unconditional refetch.");
        client.SubmittedQueries[1].IfNoneMatch.ShouldBeNull("the refetch must not be conditional.");
    }

    [Fact]
    public async Task Get_tenant_filters_raw_configuration_before_constructing_snapshot_state()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(Detail(
            "tenant.alpha",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["billing.mode"] = "visible",
                ["billing.secret"] = "hidden-undefined",
                ["private.mode"] = "hidden-namespace",
            }));
        TenantQueryGateway gateway = CreateGateway(
            client,
            bffComposition: ConfigurationComposition(
                """
                {
                  "Tenants": {
                    "ConfigurationReadPolicy": {
                      "PrefixGrants": [
                        { "TenantId": "tenant.alpha", "Subject": "operator-user", "Prefix": "billing" }
                      ],
                      "DisplaySafe": ["billing.mode", "private.mode"]
                    }
                  }
                }
                """));

        TenantDetailSnapshot snapshot = await gateway.GetTenantAsync(
            new TenantDetailRequest("tenant.alpha"),
            previous: null,
            CancellationToken.None);

        snapshot.Detail.ShouldNotBeNull().Configuration.ShouldBeEmpty();
        TenantConfigurationSafeRow row = snapshot.Configuration.Rows.ShouldHaveSingleItem();
        row.Key.ShouldBe("billing.mode");
        row.Value.ShouldBe("visible");
        snapshot.ConfigurationManagement.RemovableRows.ShouldHaveSingleItem().Key.ShouldBe("billing.mode");
        string snapshotText = snapshot.ToString().ShouldNotBeNull();
        snapshotText.ShouldNotContain("hidden-undefined", Case.Sensitive);
        snapshotText.ShouldNotContain("hidden-namespace", Case.Sensitive);
    }

    [Fact]
    public async Task Get_tenant_initial_composition_failure_is_unavailable_without_raw_fallback()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(Detail("tenant.alpha"));
        ITenantsBffComposition composition = Substitute.For<ITenantsBffComposition>();
        composition
            .ComposeTenantDetailAsync(Arg.Any<TenantDetail>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromException<TenantConfigurationComposition>(
                new InvalidOperationException("raw secret policy details")));
        TenantQueryGateway gateway = CreateGateway(client, bffComposition: composition);

        TenantDetailSnapshot snapshot = await gateway.GetTenantAsync(
            new TenantDetailRequest("tenant.alpha"),
            previous: null,
            CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Unavailable);
        snapshot.Detail.ShouldBeNull();
        snapshot.Configuration.IsAvailable.ShouldBeFalse();
        snapshot.ErrorMessage.ShouldNotBeNull().ShouldNotContain("raw secret policy details", Case.Sensitive);
    }

    [Fact]
    public async Task Get_tenant_never_reuses_previous_safe_state_from_a_different_literal_tenant()
    {
        TenantConfigurationSafeRow priorRow = new("billing", "billing.mode", "prior-visible");
        TenantConfigurationComposition priorComposition = new(
            TenantConfigurationSafeComposer.SanitizeDetail(Detail("tenant.other")),
            TenantConfigurationSafeModel.Available("tenant.other", [priorRow]),
            TenantConfigurationManagementContext.Available(
                "tenant.other",
                TenantStatus.Active,
                false,
                ["billing"],
                [priorRow]));
        TenantDetailSnapshot previous = TenantDetailSnapshot.Ready(
            priorComposition,
            "\"prior\"",
            ReadModelFreshnessState.Current);
        CapturingGatewayClient client = new();
        client.EnqueueException(new InvalidOperationException("gateway unavailable"));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantDetailSnapshot snapshot = await gateway.GetTenantAsync(
            new TenantDetailRequest("tenant.alpha"),
            previous,
            CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Unavailable);
        snapshot.Detail.ShouldBeNull();
        snapshot.Configuration.IsAvailable.ShouldBeFalse();
        (snapshot.ToString() ?? string.Empty).ShouldNotContain("prior-visible", Case.Sensitive);
    }

    [Fact]
    public async Task Get_tenant_wrong_tenant_payload_without_same_tenant_prior_is_unavailable()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(Detail(
            "tenant.other",
            new Dictionary<string, string> { ["billing.mode"] = "wrong-tenant-value" }));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantDetailSnapshot snapshot = await gateway.GetTenantAsync(
            new TenantDetailRequest("tenant.alpha"),
            previous: null,
            CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Unavailable);
        snapshot.Detail.ShouldBeNull();
        snapshot.Configuration.IsAvailable.ShouldBeFalse();
        (snapshot.ToString() ?? string.Empty).ShouldNotContain("wrong-tenant-value", Case.Sensitive);
    }

    [Fact]
    public async Task Get_tenant_degraded_payload_retains_only_reauthorized_same_tenant_safe_rows()
    {
        TenantConfigurationSafeRow priorRow = new("billing", "billing.mode", "prior-visible");
        TenantConfigurationComposition priorComposition = new(
            TenantConfigurationSafeComposer.SanitizeDetail(Detail("tenant.alpha")),
            TenantConfigurationSafeModel.Available("tenant.alpha", [priorRow]),
            TenantConfigurationManagementContext.Available(
                "tenant.alpha",
                TenantStatus.Active,
                false,
                ["billing"],
                [priorRow]));
        TenantDetailSnapshot previous = TenantDetailSnapshot.Ready(
            priorComposition,
            "\"prior\"",
            ReadModelFreshnessState.Current);
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            Detail("tenant.alpha", new Dictionary<string, string> { ["billing.secret"] = "new-raw-secret" }),
            metadata: ProjectionBackedMetadata(isStale: false, isDegraded: true));
        TenantQueryGateway gateway = CreateGateway(
            client,
            bffComposition: ConfigurationComposition(
                """
                {
                  "Tenants": {
                    "ConfigurationReadPolicy": {
                      "PrefixGrants": [
                        { "TenantId": "tenant.alpha", "Subject": "operator-user", "Prefix": "billing" }
                      ],
                      "DisplaySafe": ["billing.mode"]
                    }
                  }
                }
                """));

        TenantDetailSnapshot snapshot = await gateway.GetTenantAsync(
            new TenantDetailRequest("tenant.alpha"),
            previous,
            CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Degraded);
        snapshot.Configuration.IsDegraded.ShouldBeTrue();
        snapshot.Configuration.Rows.ShouldHaveSingleItem().Value.ShouldBe("prior-visible");
        snapshot.Detail.ShouldNotBeNull().Configuration.ShouldBeEmpty();
        (snapshot.ToString() ?? string.Empty).ShouldNotContain("new-raw-secret", Case.Sensitive);
    }

    [Theory]
    [InlineData("trial", TenantConfigurationProjectionProofKind.SetConfirmed)]
    [InlineData("different", TenantConfigurationProjectionProofKind.SetNotConfirmed)]
    public async Task Set_configuration_projection_proof_uses_current_matching_tenant_detail_only(
        string expectedValue,
        TenantConfigurationProjectionProofKind expectedKind)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantConfigurationProjectionProof proof = await gateway.GetSetConfigurationProjectionProofAsync(
            new SetTenantConfiguration("tenant.alpha", "billing.mode", expectedValue),
            CancellationToken.None);

        proof.TenantId.ShouldBe("tenant.alpha");
        proof.Kind.ShouldBe(expectedKind);
        SubmittedQuery query = client.SubmittedQueries.ShouldHaveSingleItem();
        query.Request.QueryType.ShouldBe(GetTenantQuery.QueryType);
        query.Request.AggregateId.ShouldBe("tenant.alpha");
        query.IfNoneMatch.ShouldBeNull();
    }

    [Theory]
    [InlineData(true, TenantConfigurationProjectionProofKind.RemoveNotConfirmed)]
    [InlineData(false, TenantConfigurationProjectionProofKind.RemoveConfirmed)]
    public async Task Remove_configuration_projection_proof_reports_only_key_presence(bool containsTarget, TenantConfigurationProjectionProofKind expectedKind)
    {
        CapturingGatewayClient client = new();
        IReadOnlyDictionary<string, string> configuration = containsTarget
            ? new Dictionary<string, string> { ["billing.mode"] = "trial" }
            : new Dictionary<string, string> { ["billing.other"] = "kept" };
        client.EnqueueQueryResult(Detail("tenant.alpha", configuration));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantConfigurationProjectionProof proof = await gateway.GetRemoveConfigurationProjectionProofAsync(
            new RemoveTenantConfiguration("tenant.alpha", "billing.mode"),
            CancellationToken.None);

        proof.Kind.ShouldBe(expectedKind);
        string proofText = proof.ToString() ?? string.Empty;
        proofText.ShouldNotContain("trial", Case.Sensitive);
        proofText.ShouldNotContain("billing.mode", Case.Sensitive);
    }

    [Fact]
    public async Task Configuration_projection_proof_rejects_wrong_tenant_payload()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(Detail("tenant.other", new Dictionary<string, string> { ["billing.mode"] = "trial" }));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantConfigurationProjectionProof proof = await gateway.GetSetConfigurationProjectionProofAsync(
            new SetTenantConfiguration("tenant.alpha", "billing.mode", "trial"),
            CancellationToken.None);

        proof.Kind.ShouldBe(TenantConfigurationProjectionProofKind.Unavailable);
        proof.TenantId.ShouldBe("tenant.alpha");
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("not-modified")]
    [InlineData("stale")]
    [InlineData("degraded")]
    [InlineData("unknown")]
    [InlineData("exception")]
    public async Task Configuration_projection_proof_fails_closed_without_current_payload(string outcome)
    {
        CapturingGatewayClient client = new();
        switch (outcome)
        {
            case "missing":
                client.EnqueueDetailResult(null, ProjectionBackedMetadata(isStale: false));
                break;
            case "not-modified":
                client.EnqueueDetailNotModified("\"etag\"");
                break;
            case "stale":
                client.EnqueueQueryResult(Detail("tenant.alpha"), metadata: ProjectionBackedMetadata(isStale: true));
                break;
            case "degraded":
                client.EnqueueQueryResult(Detail("tenant.alpha"), metadata: ProjectionBackedMetadata(isStale: false, isDegraded: true));
                break;
            case "unknown":
                client.EnqueueQueryResult(Detail("tenant.alpha"), metadata: new QueryResponseMetadata(IsStale: false));
                break;
            case "exception":
                client.EnqueueException(new InvalidOperationException("raw projection secret"));
                break;
        }

        TenantQueryGateway gateway = CreateGateway(client);
        TenantConfigurationProjectionProof proof = await gateway.GetSetConfigurationProjectionProofAsync(
            new SetTenantConfiguration("tenant.alpha", "billing.mode", "trial"),
            CancellationToken.None);

        proof.Kind.ShouldBe(TenantConfigurationProjectionProofKind.Unavailable);
        string proofText = proof.ToString() ?? string.Empty;
        proofText.ShouldNotContain("raw projection secret", Case.Sensitive);
    }

    [Fact]
    public async Task Configuration_projection_proof_without_authenticated_user_does_not_query()
    {
        CapturingGatewayClient client = new();
        TenantQueryGateway gateway = CreateGateway(client, userId: null);

        TenantConfigurationProjectionProof proof = await gateway.GetSetConfigurationProjectionProofAsync(
            new SetTenantConfiguration("tenant.alpha", "billing.mode", "trial"),
            CancellationToken.None);

        proof.Kind.ShouldBe(TenantConfigurationProjectionProofKind.Unavailable);
        client.SubmittedQueries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_tenant_without_previous_snapshot_reports_unavailable_when_unconditional_refetch_fails()
    {
        CapturingGatewayClient client = new();
        client.EnqueueDetailNotModified("\"known\"");

        TenantQueryGateway gateway = CreateGateway(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha", ETag: "\"known\""), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Unavailable);
        snapshot.Detail.ShouldBeNull();
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
    }

    [Fact]
    public async Task Get_tenant_with_etag_but_no_freshness_metadata_reports_unknown()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            Detail("tenant.alpha"),
            eTag: "\"tenant-etag\"",
            metadata: new QueryResponseMetadata(ETag: "\"tenant-etag\""));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Ready);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
    }

    [Theory]
    [InlineData(401, TenantDetailSurfaceKind.Unauthorized)]
    [InlineData(403, TenantDetailSurfaceKind.Unauthorized)]
    [InlineData(404, TenantDetailSurfaceKind.NotFound)]
    [InlineData(503, TenantDetailSurfaceKind.Unavailable)]
    public async Task Get_tenant_maps_gateway_status_to_safe_detail_state(int statusCode, TenantDetailSurfaceKind expected)
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(
            statusCode,
            "Problem title",
            detail: "raw payload token secret stack trace correlation-123"));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expected);
        string errorMessage = snapshot.ErrorMessage.ShouldNotBeNull();
        errorMessage.ShouldNotContain("raw payload", Case.Insensitive);
        errorMessage.ShouldNotContain("token", Case.Insensitive);
        errorMessage.ShouldNotContain("correlation-123", Case.Insensitive);
    }

    [Theory]
    [InlineData(true, false, TenantDetailSurfaceKind.Stale, ReadModelFreshnessState.Stale)]
    [InlineData(false, true, TenantDetailSurfaceKind.Degraded, ReadModelFreshnessState.Unknown)]
    public async Task Get_tenant_maps_stale_and_degraded_metadata_to_safe_states(
        bool isStale,
        bool isDegraded,
        TenantDetailSurfaceKind expectedKind,
        ReadModelFreshnessState expectedFreshness)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            Detail("tenant.alpha"),
            metadata: ProjectionBackedMetadata(isStale: isStale, isDegraded: isDegraded));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.Freshness.ShouldBe(expectedFreshness);
        if (isDegraded)
        {
            snapshot.Detail.ShouldBeNull();
        }
        else
        {
            snapshot.Detail.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task List_tenants_without_authenticated_user_fails_closed_without_querying_dependencies(string? userId)
    {
        CapturingGatewayClient client = new();
        TenantQueryGateway gateway = CreateGateway(client, userId);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(Search: "term"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Unauthorized);
        client.SubmittedQueries.ShouldBeEmpty();
    }

    [Fact]
    public async Task List_tenants_passes_cursor_without_offset_conversion()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantSummary>(
            [new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)],
            "next-cursor",
            true));
        client.EnqueueQueryResult(new TenantDetail(
            "tenant.alpha",
            "Alpha",
            null,
            TenantStatus.Active,
            [
                new TenantMember("owner-user", TenantRole.TenantOwner),
                new TenantMember("reader-user", TenantRole.TenantReader),
            ],
            new Dictionary<string, string>(),
            DateTimeOffset.UtcNow));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(Cursor: "opaque-cursor", PageSize: 10), null, CancellationToken.None);

        SubmittedQuery listQuery = client.SubmittedQueries[0];
        listQuery.Request.Tenant.ShouldBe("system");
        listQuery.Request.QueryType.ShouldBe(ListTenantsQuery.QueryType);
        JsonElement payload = listQuery.Request.Payload.ShouldNotBeNull();
        payload.GetProperty("cursor").GetString().ShouldBe("opaque-cursor");
        payload.TryGetProperty("offset", out _).ShouldBeFalse();
        snapshot.NextCursor.ShouldBe("next-cursor");
        snapshot.Rows.ShouldHaveSingleItem().MemberCount.ShouldBe(TenantCountValue.Known(2));
        snapshot.Rows[0].OwnerCount.ShouldBe(TenantCountValue.Known(1));
    }

    [Fact]
    public async Task List_tenants_requeries_page_one_once_for_safe_invalid_cursor_reason()
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(
            400,
            "Bad request",
            reasonCode: "invalid-cursor",
            detail: "expired-protected-cursor token correlation-123"));
        client.EnqueueQueryResult(new PaginatedResult<TenantSummary>(
            [new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)],
            "fresh-protected-cursor",
            true));
        client.EnqueueQueryResult(Detail("tenant.alpha"));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(
                Cursor: "expired-protected-cursor",
                PageSize: 50,
                ETag: "\"stale-etag\""),
            previous: null,
            CancellationToken.None);

        client.SubmittedQueries.Count.ShouldBe(3);
        client.SubmittedQueries[0].Request.Payload.ShouldNotBeNull().GetProperty("cursor").GetString().ShouldBe("expired-protected-cursor");
        client.SubmittedQueries[0].IfNoneMatch.ShouldBe("\"stale-etag\"");
        client.SubmittedQueries[1].Request.Payload.ShouldNotBeNull().GetProperty("cursor").ValueKind.ShouldBe(JsonValueKind.Null);
        client.SubmittedQueries[1].Request.Payload.ShouldNotBeNull().GetProperty("pageSize").GetInt32().ShouldBe(50);
        client.SubmittedQueries[1].IfNoneMatch.ShouldBeNull();
        snapshot.Rows.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        snapshot.NextCursor.ShouldBe("fresh-protected-cursor");
        snapshot.Notice.ShouldBe(TenantListReason.ListRefreshed);
        snapshot.ToString().ShouldNotContain("expired-protected-cursor", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("token", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("correlation-123", Case.Insensitive);
    }

    [Fact]
    public async Task List_tenants_invalid_cursor_retry_failure_is_sanitized_and_not_retried_again()
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(400, "Bad request", reasonCode: "invalid-cursor"));
        client.EnqueueException(new EventStoreGatewayException(
            503,
            "Unavailable",
            detail: "raw cursor token stack trace correlation-123"));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Cursor: "expired-protected-cursor"),
            previous: null,
            CancellationToken.None);

        client.SubmittedQueries.Count.ShouldBe(2);
        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Error);
        snapshot.Reason.ShouldBe(TenantListReason.GatewayUnavailable);
        snapshot.Notice.ShouldBe(TenantListReason.None);
        snapshot.ToString().ShouldNotContain("expired-protected-cursor", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("correlation-123", Case.Insensitive);
    }

    [Fact]
    public async Task List_tenants_does_not_retry_unrecognized_bad_request_as_invalid_cursor()
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(
            400,
            "Bad request",
            reasonCode: "validation-failed",
            detail: "invalid-cursor appears only in unsafe detail"));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Cursor: "opaque-protected-cursor"),
            previous: null,
            CancellationToken.None);

        client.SubmittedQueries.Count.ShouldBe(1);
        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Error);
        snapshot.Reason.ShouldBe(TenantListReason.GatewayUnavailable);
        snapshot.Notice.ShouldBe(TenantListReason.None);
    }

    [Fact]
    public async Task List_tenants_maps_authorized_empty_without_error()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantSummary>([], null, false));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Empty);
        snapshot.IsAuthorizationScopedEmpty.ShouldBeTrue();
        snapshot.Reason.ShouldBe(TenantListReason.None);
    }

    [Fact]
    public async Task Get_global_administrators_submits_fixed_platform_scope_query()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<GlobalAdministratorSummary>(
            [new GlobalAdministratorSummary("admin-1")],
            "next-cursor",
            true));

        TenantQueryGateway gateway = CreateGateway(client);

        GlobalAdministratorsSnapshot snapshot = await gateway
            .GetGlobalAdministratorsAsync(new GlobalAdministratorsRequest(Cursor: "opaque-cursor", PageSize: 10, ETag: "\"known\""), null, CancellationToken.None);

        SubmittedQuery query = client.SubmittedQueries.ShouldHaveSingleItem();
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(GetGlobalAdministratorsQuery.Domain);
        query.Request.AggregateId.ShouldBe("global-administrators");
        query.Request.EntityId.ShouldBe("global-administrators");
        query.Request.QueryType.ShouldBe(GetGlobalAdministratorsQuery.QueryType);
        query.Request.ProjectionType.ShouldBe(GetGlobalAdministratorsQuery.ProjectionType);
        query.IfNoneMatch.ShouldBe("\"known\"");
        JsonElement payload = query.Request.Payload.ShouldNotBeNull();
        payload.GetProperty("cursor").GetString().ShouldBe("opaque-cursor");
        payload.GetProperty("pageSize").GetInt32().ShouldBe(10);
        snapshot.Kind.ShouldBe(GlobalAdministratorsSurfaceKind.Ready);
        snapshot.Rows.ShouldHaveSingleItem().UserId.ShouldBe("admin-1");
        snapshot.NextCursor.ShouldBe("next-cursor");
        snapshot.HasMore.ShouldBeTrue();
    }

    [Fact]
    public async Task Get_global_administrators_preserves_previous_rows_for_not_modified()
    {
        GlobalAdministratorsSnapshot previous = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current);
        CapturingGatewayClient client = new();
        client.EnqueueGlobalAdministratorsNotModified("\"known\"");

        TenantQueryGateway gateway = CreateGateway(client);

        GlobalAdministratorsSnapshot snapshot = await gateway
            .GetGlobalAdministratorsAsync(new GlobalAdministratorsRequest(ETag: "\"known\""), previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(GlobalAdministratorsSurfaceKind.Ready);
        snapshot.Rows.ShouldHaveSingleItem().UserId.ShouldBe("admin-1");
        snapshot.ETag.ShouldBe("\"known\"");
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Current);
    }

    [Fact]
    public async Task Get_global_administrators_applies_stale_freshness_from_not_modified_response()
    {
        GlobalAdministratorsSnapshot previous = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current);
        CapturingGatewayClient client = new();
        client.EnqueueGlobalAdministratorsNotModified("\"known\"", isStale: true);

        TenantQueryGateway gateway = CreateGateway(client);

        GlobalAdministratorsSnapshot snapshot = await gateway
            .GetGlobalAdministratorsAsync(new GlobalAdministratorsRequest(ETag: "\"known\""), previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(GlobalAdministratorsSurfaceKind.Stale);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Stale);
        snapshot.Reason.ShouldBe(GlobalAdministratorsReason.ProjectionStale);
    }

    // ResolveNotModifiedFreshness carries its own AD-15 provenance gate, because its fall-through
    // returns the retained `previous` freshness WITHOUT passing through ResolveFreshness. Without
    // these rows, deleting either the provenance gate or the lifecycle clause leaves the suite green
    // while a non-projection-backed 304 keeps re-affirming a Current claim.
    [Theory]
    [InlineData(false, QueryResponseProvenance.ProjectionBacked, ProjectionLifecycleState.Unknown, null, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Unknown, GlobalAdministratorsSurfaceKind.Ready, GlobalAdministratorsReason.None)]
    [InlineData(true, QueryResponseProvenance.HandlerComputed, ProjectionLifecycleState.Stale, true, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Unknown, GlobalAdministratorsSurfaceKind.Ready, GlobalAdministratorsReason.None)]
    [InlineData(true, QueryResponseProvenance.Unknown, ProjectionLifecycleState.Stale, true, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Unknown, GlobalAdministratorsSurfaceKind.Ready, GlobalAdministratorsReason.None)]
    [InlineData(true, (QueryResponseProvenance)999, ProjectionLifecycleState.Stale, true, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Unknown, GlobalAdministratorsSurfaceKind.Ready, GlobalAdministratorsReason.None)]
    [InlineData(true, QueryResponseProvenance.ProjectionBacked, ProjectionLifecycleState.Stale, false, ReadModelFreshnessState.Stale, ProjectionLifecycleState.Stale, GlobalAdministratorsSurfaceKind.Stale, GlobalAdministratorsReason.ProjectionStale)]
    [InlineData(true, QueryResponseProvenance.ProjectionBacked, ProjectionLifecycleState.Current, true, ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, GlobalAdministratorsSurfaceKind.Ready, GlobalAdministratorsReason.None)]
    [InlineData(true, QueryResponseProvenance.ProjectionBacked, ProjectionLifecycleState.Degraded, false, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Degraded, GlobalAdministratorsSurfaceKind.Ready, GlobalAdministratorsReason.None)]
    public async Task Get_global_administrators_not_modified_gates_freshness_on_provenance_and_lifecycle(
        bool emitMetadata,
        QueryResponseProvenance provenance,
        ProjectionLifecycleState lifecycle,
        bool? isStale,
        ReadModelFreshnessState expectedFreshness,
        ProjectionLifecycleState expectedLifecycle,
        GlobalAdministratorsSurfaceKind expectedKind,
        GlobalAdministratorsReason expectedReason)
    {
        GlobalAdministratorsSnapshot previous = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current);
        CapturingGatewayClient client = new();
        client.EnqueueGlobalAdministratorsNotModified("\"known\"", isStale, lifecycle, provenance, emitMetadata);

        TenantQueryGateway gateway = CreateGateway(client);

        GlobalAdministratorsSnapshot snapshot = await gateway
            .GetGlobalAdministratorsAsync(new GlobalAdministratorsRequest(ETag: "\"known\""), previous, CancellationToken.None);

        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Lifecycle.ShouldBe(expectedLifecycle);
        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.Reason.ShouldBe(expectedReason);
        snapshot.Rows.ShouldHaveSingleItem().Lifecycle.ShouldBe(expectedLifecycle);
    }

    [Theory]
    [InlineData(ProjectionLifecycleState.Current, true, ReadModelFreshnessState.Current, GlobalAdministratorsSurfaceKind.Ready)]
    [InlineData(ProjectionLifecycleState.Stale, false, ReadModelFreshnessState.Stale, GlobalAdministratorsSurfaceKind.Stale)]
    [InlineData(ProjectionLifecycleState.Rebuilding, true, ReadModelFreshnessState.Unknown, GlobalAdministratorsSurfaceKind.Ready)]
    public async Task Get_global_administrators_projection_lifecycle_precedes_legacy_stale_evidence(
        ProjectionLifecycleState lifecycle,
        bool isStale,
        ReadModelFreshnessState expectedFreshness,
        GlobalAdministratorsSurfaceKind expectedKind)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<GlobalAdministratorSummary>([new GlobalAdministratorSummary("admin-1")], null, false),
            metadata: ProjectionBackedMetadata(isStale: isStale, lifecycle: lifecycle));

        GlobalAdministratorsSnapshot snapshot = await CreateGateway(client)
            .GetGlobalAdministratorsAsync(new GlobalAdministratorsRequest(), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Lifecycle.ShouldBe(lifecycle);
        snapshot.Rows.ShouldHaveSingleItem().Lifecycle.ShouldBe(lifecycle);
    }

    [Theory]
    [InlineData(true, false, GlobalAdministratorsSurfaceKind.Stale, ReadModelFreshnessState.Stale)]
    [InlineData(false, true, GlobalAdministratorsSurfaceKind.Degraded, ReadModelFreshnessState.Unknown)]
    public async Task Get_global_administrators_maps_stale_and_degraded_metadata_without_losing_rows(
        bool isStale,
        bool isDegraded,
        GlobalAdministratorsSurfaceKind expectedKind,
        ReadModelFreshnessState expectedFreshness)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<GlobalAdministratorSummary>([new GlobalAdministratorSummary("admin-1")], null, false),
            metadata: ProjectionBackedMetadata(isStale: isStale, isDegraded: isDegraded));

        TenantQueryGateway gateway = CreateGateway(client);

        GlobalAdministratorsSnapshot snapshot = await gateway
            .GetGlobalAdministratorsAsync(new GlobalAdministratorsRequest(), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Rows.ShouldHaveSingleItem().UserId.ShouldBe("admin-1");
    }

    [Theory]
    [InlineData(QueryResponseProvenance.Unknown)]
    [InlineData(QueryResponseProvenance.HandlerComputed)]
    [InlineData((QueryResponseProvenance)999)]
    public async Task Get_global_administrators_non_projection_stale_evidence_remains_unknown(
        QueryResponseProvenance provenance)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<GlobalAdministratorSummary>([new GlobalAdministratorSummary("admin-1")], null, false),
            metadata: new QueryResponseMetadata(IsStale: true)
            {
                Provenance = provenance,
            });

        TenantQueryGateway gateway = CreateGateway(client);

        GlobalAdministratorsSnapshot snapshot = await gateway
            .GetGlobalAdministratorsAsync(new GlobalAdministratorsRequest(), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(GlobalAdministratorsSurfaceKind.Ready);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
        snapshot.Rows.ShouldHaveSingleItem().Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
    }

    [Theory]
    [InlineData(401, GlobalAdministratorsSurfaceKind.Unauthorized)]
    [InlineData(403, GlobalAdministratorsSurfaceKind.Unauthorized)]
    [InlineData(400, GlobalAdministratorsSurfaceKind.Invalid)]
    [InlineData(404, GlobalAdministratorsSurfaceKind.Unavailable)]
    [InlineData(501, GlobalAdministratorsSurfaceKind.Unavailable)]
    [InlineData(503, GlobalAdministratorsSurfaceKind.Unavailable)]
    public async Task Get_global_administrators_maps_gateway_status_to_safe_snapshot_state(
        int statusCode,
        GlobalAdministratorsSurfaceKind expected)
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(
            statusCode,
            "Problem title",
            detail: "raw payload token secret stack trace correlation-123 cursor etag"));

        TenantQueryGateway gateway = CreateGateway(client);

        GlobalAdministratorsSnapshot snapshot = await gateway
            .GetGlobalAdministratorsAsync(new GlobalAdministratorsRequest(), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expected);
        snapshot.Rows.ShouldBeEmpty();
        snapshot.Reason.ToString().ShouldNotContain("raw", Case.Insensitive);
        snapshot.Reason.ToString().ShouldNotContain("token", Case.Insensitive);
        client.SubmittedQueries.Count.ShouldBe(1);
        client.SubmittedQueries[0].Request.QueryType.ShouldBe(GetGlobalAdministratorsQuery.QueryType);
        string[] tenantSubstituteQueries = ["list-tenants", "get-tenant", "get-user-tenants", "get-tenant-users"];
        client.SubmittedQueries
            .Any(q => tenantSubstituteQueries.Contains(q.Request.QueryType, StringComparer.Ordinal))
            .ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Get_tenant_audit_without_authenticated_user_fails_closed_without_querying_event_store(string? userId)
    {
        CapturingGatewayClient client = new();
        TenantQueryGateway gateway = CreateGateway(client, userId);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(new TenantAuditRequest("tenant.alpha"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantAuditSurfaceKind.Unauthorized);
        client.SubmittedQueries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_tenant_audit_submits_exact_audit_query_shape_and_preserves_opaque_cursor()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantAuditEntry>(
            [AuditEntry("event-1", AuditEventCategory.Access)],
            "next-audit-cursor",
            true));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(
                new TenantAuditRequest(
                    "tenant.alpha",
                    From: DateTimeOffset.Parse("2026-06-01T00:00:00Z", CultureInfo.InvariantCulture),
                    To: DateTimeOffset.Parse("2026-06-02T00:00:00Z", CultureInfo.InvariantCulture),
                    Category: AuditEventCategory.Access,
                    Cursor: "opaque-audit-cursor",
                    PageSize: 25,
                    ETag: "\"known\""),
                null,
                CancellationToken.None);

        SubmittedQuery query = client.SubmittedQueries.ShouldHaveSingleItem();
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(GetTenantAuditQuery.Domain);
        query.Request.AggregateId.ShouldBe("tenant.alpha");
        query.Request.EntityId.ShouldBe("tenant.alpha");
        query.Request.QueryType.ShouldBe(GetTenantAuditQuery.QueryType);
        query.Request.ProjectionType.ShouldBe(GetTenantAuditQuery.ProjectionType);
        query.IfNoneMatch.ShouldBe("\"known\"");
        JsonElement payload = query.Request.Payload.ShouldNotBeNull();
        payload.GetProperty("from").GetDateTimeOffset().ShouldBe(DateTimeOffset.Parse("2026-06-01T00:00:00Z", CultureInfo.InvariantCulture));
        payload.GetProperty("to").GetDateTimeOffset().ShouldBe(DateTimeOffset.Parse("2026-06-02T00:00:00Z", CultureInfo.InvariantCulture));
        payload.GetProperty("category").GetString().ShouldBe(nameof(AuditEventCategory.Access));
        payload.GetProperty("cursor").GetString().ShouldBe("opaque-audit-cursor");
        payload.GetProperty("pageSize").GetInt32().ShouldBe(25);
        payload.TryGetProperty("offset", out _).ShouldBeFalse();
        payload.TryGetProperty("limit", out _).ShouldBeFalse();
        snapshot.Kind.ShouldBe(TenantAuditSurfaceKind.Ready);
        snapshot.NextCursor.ShouldBe("next-audit-cursor");
        snapshot.HasMore.ShouldBeTrue();
        snapshot.Rows.ShouldHaveSingleItem().ReferenceContext.ShouldContain("userId: target-user");
    }

    [Fact]
    public async Task Get_tenant_audit_requeries_page_one_for_invalid_cursor_and_reports_list_refreshed()
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(
            400,
            "Bad request",
            reasonCode: "invalid-cursor",
            detail: "cursor raw payload token correlation-123"));
        client.EnqueueQueryResult(new PaginatedResult<TenantAuditEntry>(
            [AuditEntry("event-2", AuditEventCategory.Administrative)],
            "fresh-cursor",
            true));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(
                new TenantAuditRequest(
                    "tenant.alpha",
                    Category: AuditEventCategory.Administrative,
                    Cursor: "expired-protected-cursor",
                    PageSize: 25),
                null,
                CancellationToken.None);

        client.SubmittedQueries.Count.ShouldBe(2);
        client.SubmittedQueries[0].Request.Payload.ShouldNotBeNull().GetProperty("cursor").GetString().ShouldBe("expired-protected-cursor");
        client.SubmittedQueries[1].Request.Payload.ShouldNotBeNull().GetProperty("cursor").ValueKind.ShouldBe(JsonValueKind.Null);
        snapshot.Kind.ShouldBe(TenantAuditSurfaceKind.ListRefreshed);
        snapshot.Reason.ShouldBe(TenantAuditReason.ListRefreshed);
        snapshot.NextCursor.ShouldBe("fresh-cursor");
        snapshot.ToString().ShouldNotContain("expired-protected-cursor", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("token", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("correlation-123", Case.Insensitive);
    }

    [Theory]
    [InlineData(true, false, TenantAuditSurfaceKind.Stale, ReadModelFreshnessState.Stale)]
    [InlineData(false, true, TenantAuditSurfaceKind.Degraded, ReadModelFreshnessState.Unknown)]
    public async Task Get_tenant_audit_maps_stale_and_degraded_metadata_to_distinct_states(
        bool isStale,
        bool isDegraded,
        TenantAuditSurfaceKind expectedKind,
        ReadModelFreshnessState expectedFreshness)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<TenantAuditEntry>([AuditEntry("event-3", AuditEventCategory.Access)], null, false),
            metadata: ProjectionBackedMetadata(isStale: isStale, isDegraded: isDegraded));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(new TenantAuditRequest("tenant.alpha"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Rows.ShouldHaveSingleItem().Freshness.ShouldBe(expectedFreshness);
    }

    [Theory]
    [InlineData(QueryResponseProvenance.Unknown)]
    [InlineData(QueryResponseProvenance.HandlerComputed)]
    [InlineData((QueryResponseProvenance)999)]
    public async Task Get_tenant_audit_non_projection_stale_evidence_remains_unknown(
        QueryResponseProvenance provenance)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<TenantAuditEntry>([AuditEntry("event-3", AuditEventCategory.Access)], null, false),
            metadata: new QueryResponseMetadata(IsStale: true)
            {
                Provenance = provenance,
            });
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(new TenantAuditRequest("tenant.alpha"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantAuditSurfaceKind.Ready);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
        snapshot.Rows.ShouldHaveSingleItem().Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
    }

    [Fact]
    public async Task Get_tenant_audit_reuses_not_modified_snapshot_only_for_same_scope()
    {
        TenantAuditRequest originalRequest = new("tenant.alpha", Category: AuditEventCategory.Access, ETag: "\"known\"");
        TenantAuditSnapshot previous = TenantAuditSnapshot.Ready(
            [TenantAuditRow.FromEntry(AuditEntry("event-4", AuditEventCategory.Access), ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current,
            originalRequest);
        CapturingGatewayClient client = new();
        client.EnqueueAuditNotModified("\"known\"");
        client.EnqueueAuditNotModified("\"known\"");
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot sameScope = await gateway
            .GetTenantAuditAsync(originalRequest, previous, CancellationToken.None);
        TenantAuditSnapshot differentScope = await gateway
            .GetTenantAuditAsync(originalRequest with { Category = AuditEventCategory.Administrative }, previous, CancellationToken.None);

        sameScope.Rows.ShouldHaveSingleItem().EventReference.ShouldBe("event-4");
        differentScope.Kind.ShouldBe(TenantAuditSurfaceKind.Degraded);
        differentScope.Reason.ShouldBe(TenantAuditReason.NotModifiedWithoutSnapshot);
        differentScope.Rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_tenant_audit_applies_stale_freshness_from_not_modified_response()
    {
        TenantAuditRequest request = new("tenant.alpha", Category: AuditEventCategory.Access, ETag: "\"known\"");
        TenantAuditSnapshot previous = TenantAuditSnapshot.Ready(
            [TenantAuditRow.FromEntry(AuditEntry("event-4", AuditEventCategory.Access), ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current,
            request);
        CapturingGatewayClient client = new();
        client.EnqueueAuditNotModified("\"known\"", isStale: true);
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(request, previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantAuditSurfaceKind.Stale);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Stale);
        snapshot.Reason.ShouldBe(TenantAuditReason.ProjectionStale);
    }

    [Theory]
    [InlineData(false, QueryResponseProvenance.ProjectionBacked, ProjectionLifecycleState.Unknown, null, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Unknown, TenantAuditSurfaceKind.Ready, TenantAuditReason.None)]
    [InlineData(true, QueryResponseProvenance.HandlerComputed, ProjectionLifecycleState.Stale, true, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Unknown, TenantAuditSurfaceKind.Ready, TenantAuditReason.None)]
    [InlineData(true, QueryResponseProvenance.Unknown, ProjectionLifecycleState.Stale, true, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Unknown, TenantAuditSurfaceKind.Ready, TenantAuditReason.None)]
    [InlineData(true, (QueryResponseProvenance)999, ProjectionLifecycleState.Stale, true, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Unknown, TenantAuditSurfaceKind.Ready, TenantAuditReason.None)]
    [InlineData(true, QueryResponseProvenance.ProjectionBacked, ProjectionLifecycleState.Stale, false, ReadModelFreshnessState.Stale, ProjectionLifecycleState.Stale, TenantAuditSurfaceKind.Stale, TenantAuditReason.ProjectionStale)]
    [InlineData(true, QueryResponseProvenance.ProjectionBacked, ProjectionLifecycleState.Current, true, ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, TenantAuditSurfaceKind.Ready, TenantAuditReason.None)]
    [InlineData(true, QueryResponseProvenance.ProjectionBacked, ProjectionLifecycleState.Degraded, false, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Degraded, TenantAuditSurfaceKind.Ready, TenantAuditReason.None)]
    public async Task Get_tenant_audit_not_modified_gates_freshness_on_provenance_and_lifecycle(
        bool emitMetadata,
        QueryResponseProvenance provenance,
        ProjectionLifecycleState lifecycle,
        bool? isStale,
        ReadModelFreshnessState expectedFreshness,
        ProjectionLifecycleState expectedLifecycle,
        TenantAuditSurfaceKind expectedKind,
        TenantAuditReason expectedReason)
    {
        TenantAuditRequest request = new("tenant.alpha", Category: AuditEventCategory.Access, ETag: "\"known\"");
        TenantAuditSnapshot previous = TenantAuditSnapshot.Ready(
            [TenantAuditRow.FromEntry(AuditEntry("event-4", AuditEventCategory.Access), ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current,
            request);
        CapturingGatewayClient client = new();
        client.EnqueueAuditNotModified("\"known\"", isStale, lifecycle, provenance, emitMetadata);
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(request, previous, CancellationToken.None);

        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Lifecycle.ShouldBe(expectedLifecycle);
        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.Reason.ShouldBe(expectedReason);
        snapshot.Rows.ShouldHaveSingleItem().Lifecycle.ShouldBe(expectedLifecycle);
    }

    [Theory]
    [InlineData(ProjectionLifecycleState.Current, true, ReadModelFreshnessState.Current, TenantAuditSurfaceKind.Ready)]
    [InlineData(ProjectionLifecycleState.Stale, false, ReadModelFreshnessState.Stale, TenantAuditSurfaceKind.Stale)]
    [InlineData(ProjectionLifecycleState.Degraded, true, ReadModelFreshnessState.Unknown, TenantAuditSurfaceKind.Ready)]
    public async Task Get_tenant_audit_projection_lifecycle_precedes_legacy_stale_evidence(
        ProjectionLifecycleState lifecycle,
        bool isStale,
        ReadModelFreshnessState expectedFreshness,
        TenantAuditSurfaceKind expectedKind)
    {
        TenantAuditRequest request = new("tenant.alpha");
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<TenantAuditEntry>([AuditEntry("event-lifecycle", AuditEventCategory.Access)], null, false),
            metadata: ProjectionBackedMetadata(isStale: isStale, lifecycle: lifecycle));

        TenantAuditSnapshot snapshot = await CreateGateway(client)
            .GetTenantAuditAsync(request, null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Lifecycle.ShouldBe(lifecycle);
        snapshot.Rows.ShouldHaveSingleItem().Lifecycle.ShouldBe(lifecycle);
    }

    [Fact]
    public async Task Get_tenant_audit_maps_missing_payload_to_safe_degraded_state()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult<PaginatedResult<TenantAuditEntry>?>(null, metadata: new QueryResponseMetadata(ServedAt: DateTimeOffset.UtcNow));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(new TenantAuditRequest("tenant.alpha"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantAuditSurfaceKind.Degraded);
        snapshot.Reason.ShouldBe(TenantAuditReason.MissingPayload);
        snapshot.Rows.ShouldBeEmpty();
        client.SubmittedQueries.ShouldHaveSingleItem().Request.QueryType.ShouldBe(GetTenantAuditQuery.QueryType);
        string[] tenantSubstituteQueries = ["list-tenants", "get-tenant", "get-user-tenants", "get-tenant-users"];
        client.SubmittedQueries
            .Any(q => tenantSubstituteQueries.Contains(q.Request.QueryType, StringComparer.Ordinal))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task Get_tenant_audit_preserves_previous_rows_for_missing_payload_when_scope_matches()
    {
        TenantAuditRequest request = new("tenant.alpha", Category: AuditEventCategory.Access);
        TenantAuditSnapshot previous = TenantAuditSnapshot.Ready(
            [TenantAuditRow.FromEntry(AuditEntry("event-5", AuditEventCategory.Access), ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current,
            request);
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult<PaginatedResult<TenantAuditEntry>?>(null, metadata: new QueryResponseMetadata(ServedAt: DateTimeOffset.UtcNow));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(request, previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantAuditSurfaceKind.Degraded);
        snapshot.Reason.ShouldBe(TenantAuditReason.MissingPayload);
        snapshot.Rows.ShouldHaveSingleItem().EventReference.ShouldBe("event-5");
    }

    [Theory]
    [InlineData(401, TenantAuditSurfaceKind.Unauthorized)]
    [InlineData(403, TenantAuditSurfaceKind.Unauthorized)]
    [InlineData(404, TenantAuditSurfaceKind.Unavailable)]
    [InlineData(503, TenantAuditSurfaceKind.Unavailable)]
    [InlineData(500, TenantAuditSurfaceKind.Error)]
    public async Task Get_tenant_audit_maps_gateway_status_to_safe_snapshot_state(int statusCode, TenantAuditSurfaceKind expected)
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(
            statusCode,
            "Problem title",
            detail: "raw payload token secret stack trace correlation-123 EventStore metadata cursor etag"));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(new TenantAuditRequest("tenant.alpha"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expected);
        snapshot.Rows.ShouldBeEmpty();
        snapshot.ToString().ShouldNotContain("raw payload", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("token", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("stack trace", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("correlation-123", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("EventStore metadata", Case.Insensitive);
    }

    [Fact]
    public async Task Get_tenant_audit_maps_only_support_safe_narrative_fields()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantAuditEntry>(
            [
                new TenantAuditEntry(
                    "event-safe-reference",
                    "TenantConfigurationSet",
                    AuditEventCategory.Administrative,
                    "actor-user",
                    DateTimeOffset.UtcNow,
                    "tenant.alpha",
                    new Dictionary<string, string>
                    {
                        ["userId"] = "target-user",
                        ["key"] = "billing.mode",
                        ["rawPayload"] = "raw payload token secret",
                        ["correlationId"] = "correlation-123",
                        ["etag"] = "\"etag\"",
                    }),
            ],
            null,
            false));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(new TenantAuditRequest("tenant.alpha"), null, CancellationToken.None);

        TenantAuditRow row = snapshot.Rows.ShouldHaveSingleItem();
        row.ReferenceContext.ShouldContain("userId: target-user");
        row.ReferenceContext.ShouldContain("key: billing.mode");
        row.ReferenceContext.ShouldNotContain("raw payload", Case.Insensitive);
        row.ReferenceContext.ShouldNotContain("token", Case.Insensitive);
        row.ReferenceContext.ShouldNotContain("correlation-123", Case.Insensitive);
        row.ReferenceContext.ShouldNotContain("etag", Case.Insensitive);
    }

    [Fact]
    public async Task Get_tenant_audit_scrubs_unsafe_row_fields_before_rendering()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantAuditEntry>(
            [
                new TenantAuditEntry(
                    "event-safe-reference",
                    "stack trace internal detail",
                    AuditEventCategory.Administrative,
                    "actor-user",
                    DateTimeOffset.UtcNow,
                    "cursor protected value",
                    new Dictionary<string, string>
                    {
                        ["userId"] = "raw payload token secret",
                        ["key"] = "billing.mode",
                    }),
            ],
            null,
            false));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(new TenantAuditRequest("tenant.alpha"), null, CancellationToken.None);

        TenantAuditRow row = snapshot.Rows.ShouldHaveSingleItem();
        row.Target.ShouldBeEmpty();
        row.Scope.ShouldBeEmpty();
        row.Outcome.ShouldBeEmpty();
        row.ReferenceContext.ShouldContain("key: billing.mode");
        row.ReferenceContext.ShouldNotContain("raw payload", Case.Insensitive);
        row.ReferenceContext.ShouldNotContain("token", Case.Insensitive);
        row.Target.ShouldNotContain("raw payload", Case.Insensitive);
        row.Scope.ShouldNotContain("cursor", Case.Insensitive);
        row.Outcome.ShouldNotContain("stack trace", Case.Insensitive);
    }

    [Fact]
    public async Task List_tenants_reports_unknown_freshness_when_no_evidence_exists()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<TenantSummary>([], null, false),
            eTag: null,
            metadata: null,
            emitDefaultMetadata: false);

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Empty);
    }

    [Fact]
    public async Task List_tenants_does_not_treat_served_at_as_freshness_evidence()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<TenantSummary>([], null, false),
            eTag: null,
            metadata: new QueryResponseMetadata(ServedAt: DateTimeOffset.UtcNow));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Empty);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
    }

    [Fact]
    public async Task List_tenants_uses_previous_snapshot_for_not_modified_response()
    {
        TenantListSnapshot previous = TenantListSnapshot.Ready(
            [TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active))],
            nextCursor: null,
            hasMore: false,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current,
            isDegraded: false);
        CapturingGatewayClient client = new();
        client.EnqueueNotModified("\"known\"");

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10, ETag: "\"known\""), previous, CancellationToken.None);

        client.SubmittedQueries[0].IfNoneMatch.ShouldBe("\"known\"");
        snapshot.Rows.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Current);
    }

    [Theory]
    [InlineData(false, QueryResponseProvenance.ProjectionBacked, ProjectionLifecycleState.Unknown, null, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Unknown, TenantListSurfaceKind.Ready, TenantListReason.None)]
    [InlineData(true, QueryResponseProvenance.HandlerComputed, ProjectionLifecycleState.Stale, true, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Unknown, TenantListSurfaceKind.Ready, TenantListReason.None)]
    [InlineData(true, QueryResponseProvenance.Unknown, ProjectionLifecycleState.Stale, true, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Unknown, TenantListSurfaceKind.Ready, TenantListReason.None)]
    [InlineData(true, (QueryResponseProvenance)999, ProjectionLifecycleState.Stale, true, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Unknown, TenantListSurfaceKind.Ready, TenantListReason.None)]
    [InlineData(true, QueryResponseProvenance.ProjectionBacked, ProjectionLifecycleState.Stale, false, ReadModelFreshnessState.Stale, ProjectionLifecycleState.Stale, TenantListSurfaceKind.Stale, TenantListReason.None)]
    [InlineData(true, QueryResponseProvenance.ProjectionBacked, ProjectionLifecycleState.Current, true, ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, TenantListSurfaceKind.Ready, TenantListReason.None)]
    [InlineData(true, QueryResponseProvenance.ProjectionBacked, ProjectionLifecycleState.Degraded, false, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Degraded, TenantListSurfaceKind.Ready, TenantListReason.None)]
    public async Task List_tenants_not_modified_gates_freshness_on_provenance_and_lifecycle(
        bool emitMetadata,
        QueryResponseProvenance provenance,
        ProjectionLifecycleState lifecycle,
        bool? isStale,
        ReadModelFreshnessState expectedFreshness,
        ProjectionLifecycleState expectedLifecycle,
        TenantListSurfaceKind expectedKind,
        TenantListReason expectedReason)
    {
        TenantListSnapshot previous = TenantListSnapshot.Ready(
            [TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active))],
            nextCursor: null,
            hasMore: false,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current,
            isDegraded: false);
        CapturingGatewayClient client = new();
        client.EnqueueNotModified("\"known\"", isStale, lifecycle, provenance, emitMetadata);

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10, ETag: "\"known\""), previous, CancellationToken.None);

        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Lifecycle.ShouldBe(expectedLifecycle);
        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.Reason.ShouldBe(expectedReason);
        snapshot.Rows.ShouldHaveSingleItem().Lifecycle.ShouldBe(expectedLifecycle);
    }

    [Theory]
    [InlineData(ProjectionLifecycleState.Current, true, ReadModelFreshnessState.Current, TenantListSurfaceKind.Ready)]
    [InlineData(ProjectionLifecycleState.Stale, false, ReadModelFreshnessState.Stale, TenantListSurfaceKind.Stale)]
    [InlineData(ProjectionLifecycleState.Unavailable, true, ReadModelFreshnessState.Unknown, TenantListSurfaceKind.Ready)]
    public async Task List_tenants_projection_lifecycle_precedes_legacy_stale_evidence(
        ProjectionLifecycleState lifecycle,
        bool isStale,
        ReadModelFreshnessState expectedFreshness,
        TenantListSurfaceKind expectedKind)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<TenantSummary>([new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)], null, false),
            metadata: ProjectionBackedMetadata(isStale: isStale, lifecycle: lifecycle));
        client.EnqueueQueryResult(Detail("tenant.alpha"));

        TenantListSnapshot snapshot = await CreateGateway(client)
            .ListTenantsAsync(new TenantListRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Lifecycle.ShouldBe(lifecycle);
        snapshot.Rows.ShouldHaveSingleItem().Lifecycle.ShouldBe(lifecycle);
    }

    // The tenant-list surface was the only query surface with no 200-path provenance theory, while
    // routing through the same ResolveFreshness gate as the four that had one.
    [Theory]
    [InlineData(QueryResponseProvenance.HandlerComputed)]
    [InlineData(QueryResponseProvenance.Unknown)]
    [InlineData((QueryResponseProvenance)999)]
    public async Task List_tenants_rejects_non_projection_backed_provenance(QueryResponseProvenance provenance)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<TenantSummary>([], null, false),
            metadata: ProjectionBackedMetadata(isStale: false, provenance: provenance));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Freshness.ShouldBe(
            ReadModelFreshnessState.Unknown,
            "only projection-backed evidence may claim a lifecycle state (AD-15).");
    }

    [Fact]
    public async Task List_tenants_not_modified_preserves_previous_unknown_freshness_without_freshness_header()
    {
        TenantListSnapshot previous = TenantListSnapshot.Ready(
            [TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active))],
            nextCursor: null,
            hasMore: false,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Unknown,
            isDegraded: false);
        CapturingGatewayClient client = new();
        client.EnqueueNotModified("\"known\"");

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10, ETag: "\"known\""), previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Ready);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
    }

    [Fact]
    public async Task List_tenants_not_modified_uses_stale_header_from_conditional_response()
    {
        TenantListSnapshot previous = TenantListSnapshot.Ready(
            [TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active))],
            nextCursor: "next",
            hasMore: true,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current,
            isDegraded: false);
        CapturingGatewayClient client = new();
        client.EnqueueNotModified("\"known\"", isStale: true);

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10, ETag: "\"known\""), previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Stale);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Stale);
        snapshot.NextCursor.ShouldBe("next");
        snapshot.HasMore.ShouldBeTrue();
    }

    [Fact]
    public async Task List_tenants_stale_empty_response_surfaces_stale_state()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<TenantSummary>([], null, false),
            metadata: ProjectionBackedMetadata(isStale: true));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Stale);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Stale);
    }

    [Fact]
    public async Task Detail_enrichment_failure_keeps_unknown_counts_and_degraded_state()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantSummary>(
            [new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)],
            null,
            false));
        client.EnqueueException(new EventStoreGatewayException(403, "Forbidden"));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Degraded);
        snapshot.Rows.ShouldHaveSingleItem().MemberCount.IsKnown.ShouldBeFalse();
        snapshot.Rows[0].OwnerCount.IsKnown.ShouldBeFalse();
    }

    [Theory]
    [InlineData(401, TenantListSurfaceKind.Unauthorized)]
    [InlineData(403, TenantListSurfaceKind.Unauthorized)]
    [InlineData(400, TenantListSurfaceKind.Error)]
    [InlineData(503, TenantListSurfaceKind.Error)]
    public async Task List_tenants_maps_gateway_status_to_safe_state(int statusCode, TenantListSurfaceKind expected)
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(
            statusCode,
            "Problem title",
            detail: "raw payload token secret stack trace correlation-123"));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expected);
    }

    [Fact]
    public async Task Get_my_tenants_submits_self_user_query_with_cursor_payload()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<UserTenantMembership>(
            [new UserTenantMembership("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantOwner)],
            "opaque-next",
            true));
        TenantQueryGateway gateway = CreateGateway(client, "user.self");

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(Cursor: "signed-cursor", PageSize: 12), null, CancellationToken.None);

        SubmittedQuery query = client.SubmittedQueries[0];
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(GetUserTenantsQuery.Domain);
        query.Request.ProjectionType.ShouldBe(GetUserTenantsQuery.ProjectionType);
        query.Request.AggregateId.ShouldBe("index");
        query.Request.EntityId.ShouldBe("user.self");
        query.Request.QueryType.ShouldBe(GetUserTenantsQuery.QueryType);
        JsonElement payload = query.Request.Payload.ShouldNotBeNull();
        payload.GetProperty("cursor").GetString().ShouldBe("signed-cursor");
        payload.GetProperty("pageSize").GetInt32().ShouldBe(12);
        payload.TryGetProperty("offset", out _).ShouldBeFalse();
        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Ready);
        snapshot.NextCursor.ShouldBe("opaque-next");
        snapshot.Rows.ShouldHaveSingleItem().Role.ShouldBe(TenantRole.TenantOwner);
    }

    [Fact]
    public async Task Get_my_tenants_keeps_signed_in_user_as_target_even_when_request_has_target()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<UserTenantMembership>([], null, false));
        TenantQueryGateway gateway = CreateGateway(client, "user.self");

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(TargetUserId: "user.other"), null, CancellationToken.None);

        SubmittedQuery query = client.SubmittedQueries[0];
        query.Request.Tenant.ShouldBe("system");
        query.Request.EntityId.ShouldBe("user.self");
        snapshot.TargetUserId.ShouldBe("user.self");
    }

    [Fact]
    public async Task Get_user_tenants_submits_authenticated_requester_and_explicit_target_user_query()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<UserTenantMembership>(
            [new UserTenantMembership("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantReader)],
            "opaque-next",
            true));
        TenantQueryGateway gateway = CreateGateway(client, "operator-user");

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetUserTenantsAsync(
                new UserTenantMembershipRequest(
                    TargetUserId: "target.user@example",
                    Cursor: "signed-target-cursor",
                    PageSize: 12,
                    ETag: "\"known\""),
                null,
                CancellationToken.None);

        SubmittedQuery query = client.SubmittedQueries[0];
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(GetUserTenantsQuery.Domain);
        query.Request.ProjectionType.ShouldBe(GetUserTenantsQuery.ProjectionType);
        query.Request.AggregateId.ShouldBe("index");
        query.Request.EntityId.ShouldBe("target.user@example");
        query.Request.QueryType.ShouldBe(GetUserTenantsQuery.QueryType);
        query.IfNoneMatch.ShouldBe("\"known\"");
        JsonElement payload = query.Request.Payload.ShouldNotBeNull();
        payload.GetProperty("cursor").GetString().ShouldBe("signed-target-cursor");
        payload.GetProperty("pageSize").GetInt32().ShouldBe(12);
        payload.TryGetProperty("offset", out _).ShouldBeFalse();
        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Ready);
        snapshot.TargetUserId.ShouldBe("target.user@example");
        snapshot.NextCursor.ShouldBe("opaque-next");
    }

    [Fact]
    public async Task Get_user_tenants_reuses_not_modified_snapshot_only_for_same_target_user()
    {
        UserTenantMembershipSnapshot previous = UserTenantMembershipSnapshot.Ready(
            [new UserTenantMembershipRow("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantReader, ReadModelFreshnessState.Current)],
            nextCursor: "next",
            hasMore: true,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current,
            targetUserId: "target.one");
        CapturingGatewayClient client = new();
        client.EnqueueUserTenantsNotModified("\"known\"");
        client.EnqueueUserTenantsNotModified("\"known\"");
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot sameTarget = await gateway
            .GetUserTenantsAsync(new UserTenantMembershipRequest(TargetUserId: "target.one", ETag: "\"known\""), previous, CancellationToken.None);
        UserTenantMembershipSnapshot differentTarget = await gateway
            .GetUserTenantsAsync(new UserTenantMembershipRequest(TargetUserId: "target.two", ETag: "\"known\""), previous, CancellationToken.None);

        sameTarget.Rows.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        sameTarget.TargetUserId.ShouldBe("target.one");
        differentTarget.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Degraded);
        differentTarget.Reason.ShouldBe(UserTenantMembershipReason.NotModifiedWithoutSnapshot);
        differentTarget.TargetUserId.ShouldBe("target.two");
    }

    [Fact]
    public async Task Get_user_tenants_applies_stale_freshness_from_not_modified_response()
    {
        UserTenantMembershipSnapshot previous = UserTenantMembershipSnapshot.Ready(
            [new UserTenantMembershipRow("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantReader, ReadModelFreshnessState.Current)],
            nextCursor: "next",
            hasMore: true,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current,
            targetUserId: "target.one");
        CapturingGatewayClient client = new();
        client.EnqueueUserTenantsNotModified("\"known\"", isStale: true);
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetUserTenantsAsync(new UserTenantMembershipRequest(TargetUserId: "target.one", ETag: "\"known\""), previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Stale);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Stale);
        snapshot.Reason.ShouldBe(UserTenantMembershipReason.ProjectionStale);
    }

    [Fact]
    public async Task Get_user_tenants_rejects_missing_target_without_backend_call()
    {
        CapturingGatewayClient client = new();
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetUserTenantsAsync(new UserTenantMembershipRequest(TargetUserId: ""), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Invalid);
        snapshot.Reason.ShouldBe(UserTenantMembershipReason.MissingTargetUser);
        client.SubmittedQueries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_user_tenants_maps_authorization_scoped_empty_without_disclosing_hidden_memberships()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<UserTenantMembership>([], null, false));
        TenantQueryGateway gateway = CreateGateway(client, "operator-user");

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetUserTenantsAsync(new UserTenantMembershipRequest(TargetUserId: "target.user", PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Empty);
        snapshot.IsAuthorizationScopedEmpty.ShouldBeTrue();
        snapshot.TargetUserId.ShouldBe("target.user");
        snapshot.Rows.ShouldBeEmpty();
        snapshot.ToString().ShouldNotContain("hidden", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("missing user", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("orphan", Case.Insensitive);
    }

    [Theory]
    [InlineData(true, false, UserTenantMembershipSurfaceKind.Stale, ReadModelFreshnessState.Stale, UserTenantMembershipReason.ProjectionStale)]
    [InlineData(false, true, UserTenantMembershipSurfaceKind.Degraded, ReadModelFreshnessState.Unknown, UserTenantMembershipReason.ProjectionDegraded)]
    public async Task Get_user_tenants_maps_target_lookup_stale_and_degraded_metadata_to_distinct_states(
        bool isStale,
        bool isDegraded,
        UserTenantMembershipSurfaceKind expectedKind,
        ReadModelFreshnessState expectedFreshness,
        UserTenantMembershipReason expectedReason)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<UserTenantMembership>(
                [new UserTenantMembership("tenant.alpha", "Alpha", TenantStatus.Disabled, TenantRole.TenantReader)],
                "next",
                true),
            metadata: ProjectionBackedMetadata(isStale: isStale, isDegraded: isDegraded));
        TenantQueryGateway gateway = CreateGateway(client, "operator-user");

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetUserTenantsAsync(new UserTenantMembershipRequest(TargetUserId: "target.user", PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.TargetUserId.ShouldBe("target.user");
        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Reason.ShouldBe(expectedReason);
        snapshot.NextCursor.ShouldBe("next");
        snapshot.HasMore.ShouldBeTrue();
        snapshot.Rows.ShouldHaveSingleItem().Freshness.ShouldBe(expectedFreshness);
    }

    [Theory]
    [InlineData(QueryResponseProvenance.Unknown)]
    [InlineData(QueryResponseProvenance.HandlerComputed)]
    [InlineData((QueryResponseProvenance)999)]
    public async Task Get_user_tenants_non_projection_stale_evidence_remains_unknown(
        QueryResponseProvenance provenance)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<UserTenantMembership>(
                [new UserTenantMembership("tenant.alpha", "Alpha", TenantStatus.Disabled, TenantRole.TenantReader)],
                "next",
                true),
            metadata: new QueryResponseMetadata(IsStale: true)
            {
                Provenance = provenance,
            });
        TenantQueryGateway gateway = CreateGateway(client, "operator-user");

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetUserTenantsAsync(new UserTenantMembershipRequest(TargetUserId: "target.user", PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Ready);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
        snapshot.Reason.ShouldBe(UserTenantMembershipReason.None);
        snapshot.Rows.ShouldHaveSingleItem().Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
    }

    [Theory]
    [InlineData(401, UserTenantMembershipSurfaceKind.Unauthorized)]
    [InlineData(403, UserTenantMembershipSurfaceKind.Unauthorized)]
    [InlineData(400, UserTenantMembershipSurfaceKind.Invalid)]
    [InlineData(503, UserTenantMembershipSurfaceKind.Unavailable)]
    [InlineData(500, UserTenantMembershipSurfaceKind.Degraded)]
    public async Task Get_user_tenants_maps_target_lookup_gateway_failures_to_sanitized_states(
        int statusCode,
        UserTenantMembershipSurfaceKind expected)
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(
            statusCode,
            "Problem title",
            detail: "raw payload token secret stack trace correlation-123 EventStore metadata"));
        TenantQueryGateway gateway = CreateGateway(client, "operator-user");

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetUserTenantsAsync(new UserTenantMembershipRequest(TargetUserId: "target.user"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expected);
        snapshot.TargetUserId.ShouldBe("target.user");
        snapshot.ToString().ShouldNotContain("raw payload", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("token", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("stack trace", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("correlation-123", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("EventStore metadata", Case.Insensitive);
    }

    [Fact]
    public async Task Get_my_tenants_requires_authenticated_user_context()
    {
        CapturingGatewayClient client = new();
        TenantQueryGateway gateway = CreateGateway(client, userId: null);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Unauthorized);
        snapshot.Reason.ShouldBe(UserTenantMembershipReason.MissingAuthenticatedUser);
        client.SubmittedQueries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_my_tenants_maps_authorized_empty_without_error()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<UserTenantMembership>([], null, false));
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Empty);
        snapshot.IsAuthorizationScopedEmpty.ShouldBeTrue();
        snapshot.Rows.ShouldBeEmpty();
        snapshot.Reason.ShouldBe(UserTenantMembershipReason.None);
    }

    [Fact]
    public async Task Get_my_tenants_uses_previous_snapshot_for_not_modified_response()
    {
        UserTenantMembershipSnapshot previous = UserTenantMembershipSnapshot.Ready(
            [new UserTenantMembershipRow("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantReader, ReadModelFreshnessState.Current)],
            nextCursor: "next",
            hasMore: true,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current,
            targetUserId: "operator-user");
        CapturingGatewayClient client = new();
        client.EnqueueUserTenantsNotModified("\"known\"");
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(ETag: "\"known\""), previous, CancellationToken.None);

        client.SubmittedQueries[0].IfNoneMatch.ShouldBe("\"known\"");
        snapshot.Rows.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        snapshot.NextCursor.ShouldBe("next");
        snapshot.HasMore.ShouldBeTrue();
        snapshot.ETag.ShouldBe("\"known\"");
    }

    [Theory]
    [InlineData(false, QueryResponseProvenance.ProjectionBacked, ProjectionLifecycleState.Unknown, null, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Unknown, UserTenantMembershipSurfaceKind.Ready, UserTenantMembershipReason.None)]
    [InlineData(true, QueryResponseProvenance.HandlerComputed, ProjectionLifecycleState.Stale, true, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Unknown, UserTenantMembershipSurfaceKind.Ready, UserTenantMembershipReason.None)]
    [InlineData(true, QueryResponseProvenance.Unknown, ProjectionLifecycleState.Stale, true, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Unknown, UserTenantMembershipSurfaceKind.Ready, UserTenantMembershipReason.None)]
    [InlineData(true, (QueryResponseProvenance)999, ProjectionLifecycleState.Stale, true, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Unknown, UserTenantMembershipSurfaceKind.Ready, UserTenantMembershipReason.None)]
    [InlineData(true, QueryResponseProvenance.ProjectionBacked, ProjectionLifecycleState.Stale, false, ReadModelFreshnessState.Stale, ProjectionLifecycleState.Stale, UserTenantMembershipSurfaceKind.Stale, UserTenantMembershipReason.ProjectionStale)]
    [InlineData(true, QueryResponseProvenance.ProjectionBacked, ProjectionLifecycleState.Current, true, ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, UserTenantMembershipSurfaceKind.Ready, UserTenantMembershipReason.None)]
    [InlineData(true, QueryResponseProvenance.ProjectionBacked, ProjectionLifecycleState.Degraded, false, ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Degraded, UserTenantMembershipSurfaceKind.Ready, UserTenantMembershipReason.None)]
    public async Task Get_my_tenants_not_modified_gates_freshness_on_provenance_and_lifecycle(
        bool emitMetadata,
        QueryResponseProvenance provenance,
        ProjectionLifecycleState lifecycle,
        bool? isStale,
        ReadModelFreshnessState expectedFreshness,
        ProjectionLifecycleState expectedLifecycle,
        UserTenantMembershipSurfaceKind expectedKind,
        UserTenantMembershipReason expectedReason)
    {
        UserTenantMembershipSnapshot previous = UserTenantMembershipSnapshot.Ready(
            [new UserTenantMembershipRow("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantReader, ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current,
            targetUserId: "operator-user");
        CapturingGatewayClient client = new();
        client.EnqueueUserTenantsNotModified("\"known\"", isStale, lifecycle, provenance, emitMetadata);
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(ETag: "\"known\""), previous, CancellationToken.None);

        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Lifecycle.ShouldBe(expectedLifecycle);
        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.Reason.ShouldBe(expectedReason);
        snapshot.Rows.ShouldHaveSingleItem().Lifecycle.ShouldBe(expectedLifecycle);
    }

    [Theory]
    [InlineData(ProjectionLifecycleState.Current, true, ReadModelFreshnessState.Current, UserTenantMembershipSurfaceKind.Ready)]
    [InlineData(ProjectionLifecycleState.Stale, false, ReadModelFreshnessState.Stale, UserTenantMembershipSurfaceKind.Stale)]
    [InlineData(ProjectionLifecycleState.LocalOnly, true, ReadModelFreshnessState.Unknown, UserTenantMembershipSurfaceKind.Ready)]
    public async Task Get_my_tenants_projection_lifecycle_precedes_legacy_stale_evidence(
        ProjectionLifecycleState lifecycle,
        bool isStale,
        ReadModelFreshnessState expectedFreshness,
        UserTenantMembershipSurfaceKind expectedKind)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<UserTenantMembership>(
                [new UserTenantMembership("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantReader)],
                null,
                false),
            metadata: ProjectionBackedMetadata(isStale: isStale, lifecycle: lifecycle));

        UserTenantMembershipSnapshot snapshot = await CreateGateway(client)
            .GetMyTenantsAsync(new UserTenantMembershipRequest(), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Lifecycle.ShouldBe(lifecycle);
        snapshot.Rows.ShouldHaveSingleItem().Lifecycle.ShouldBe(lifecycle);
    }

    [Fact]
    public async Task Get_my_tenants_without_previous_snapshot_reports_degraded_not_modified_state()
    {
        CapturingGatewayClient client = new();
        client.EnqueueUserTenantsNotModified("\"known\"");
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(ETag: "\"known\""), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Degraded);
        snapshot.Reason.ShouldBe(UserTenantMembershipReason.NotModifiedWithoutSnapshot);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
    }

    [Theory]
    [InlineData(true, false, UserTenantMembershipSurfaceKind.Stale, ReadModelFreshnessState.Stale, UserTenantMembershipReason.ProjectionStale)]
    [InlineData(false, true, UserTenantMembershipSurfaceKind.Degraded, ReadModelFreshnessState.Unknown, UserTenantMembershipReason.ProjectionDegraded)]
    public async Task Get_my_tenants_maps_stale_and_degraded_metadata_to_distinct_states(
        bool isStale,
        bool isDegraded,
        UserTenantMembershipSurfaceKind expectedKind,
        ReadModelFreshnessState expectedFreshness,
        UserTenantMembershipReason expectedReason)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<UserTenantMembership>(
                [new UserTenantMembership("tenant.alpha", "Alpha", TenantStatus.Disabled, TenantRole.TenantReader)],
                "next",
                true),
            metadata: ProjectionBackedMetadata(isStale: isStale, isDegraded: isDegraded));
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Reason.ShouldBe(expectedReason);
        snapshot.NextCursor.ShouldBe("next");
        snapshot.HasMore.ShouldBeTrue();
        snapshot.Rows.ShouldHaveSingleItem().Freshness.ShouldBe(expectedFreshness);
    }

    [Theory]
    [InlineData(401, UserTenantMembershipSurfaceKind.Unauthorized)]
    [InlineData(403, UserTenantMembershipSurfaceKind.Unauthorized)]
    [InlineData(400, UserTenantMembershipSurfaceKind.Invalid)]
    [InlineData(503, UserTenantMembershipSurfaceKind.Unavailable)]
    [InlineData(500, UserTenantMembershipSurfaceKind.Degraded)]
    public async Task Get_my_tenants_maps_gateway_failures_to_sanitized_states(int statusCode, UserTenantMembershipSurfaceKind expected)
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(
            statusCode,
            "Problem title",
            detail: "raw payload token secret stack trace correlation-123"));
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expected);
        snapshot.ToString().ShouldNotContain("raw payload", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("token", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("correlation-123", Case.Insensitive);
    }

    [Fact]
    public async Task List_empty_search_uses_ordinary_cursor_path_without_notice()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantSummary>([new TenantSummary("alpha", "Alpha", TenantStatus.Active)], null, false));
        client.EnqueueQueryResult(Detail("alpha"));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Search: "   "), previous: null, CancellationToken.None); // whitespace term

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Ready);
        snapshot.Notice.ShouldBe(TenantListReason.None);
    }

    [Fact]
    public async Task List_non_empty_search_uses_ordinary_cursor_list_without_memories_or_plaintext_cursor()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantSummary>(
            [new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)],
            "opaque-protected-next-cursor",
            true));
        client.EnqueueQueryResult(Detail("tenant.alpha"));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Search: "term", PageSize: 50),
            previous: null,
            CancellationToken.None);

        client.SubmittedQueries[0].Request.QueryType.ShouldBe(ListTenantsQuery.QueryType);
        client.SubmittedQueries[0].Request.Payload.ShouldNotBeNull().GetProperty("pageSize").GetInt32().ShouldBe(50);
        client.SubmittedQueries[0].Request.Payload.ShouldNotBeNull().TryGetProperty("offset", out _).ShouldBeFalse();
        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Ready);
        snapshot.Rows.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        snapshot.NextCursor.ShouldBe("opaque-protected-next-cursor");
        snapshot.Notice.ShouldBe(TenantListReason.SearchUnavailable);
        snapshot.ToString().ShouldNotContain("memories-search", Case.Insensitive);
    }

    [Fact]
    public async Task List_search_uses_exact_memories_request_and_only_authoritative_hydrated_fields()
    {
        StubMemoriesClient memories = new();
        memories.Enqueue(SearchResult(
            "needle",
            totalCount: 8,
            Hit("not-a-tenant"),
            Hit("tenant:alpha"),
            Hit("tenant:alpha"),
            Hit("tenant:hidden"),
            Hit("tenant:gamma")));
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(Detail("alpha") with { Name = "Authoritative Alpha" });
        client.EnqueueException(new EventStoreGatewayException((int)HttpStatusCode.Forbidden, "hidden"));
        client.EnqueueQueryResult(Detail("gamma") with { Name = "Authoritative Gamma" });
        var codec = new TenantSearchCursorCodec(new EphemeralDataProtectionProvider());
        TenantQueryGateway gateway = CreateGateway(client, memoriesClient: memories, searchCursorCodec: codec);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(
                Search: "needle",
                Status: TenantStatus.Active,
                SortColumn: TenantListSortColumns.Name,
                SortDescending: true,
                PageSize: 5),
            previous: null,
            CancellationToken.None);

        SearchRequest request = memories.SearchRequests.ShouldHaveSingleItem();
        request.TenantId.ShouldBe("tenants-index");
        request.Axis.ShouldBe("syntactic");
        request.Query.ShouldBe("needle");
        request.Offset.ShouldBe(0);
        request.MaxResults.ShouldBe(5);
        request.Explain.ShouldBeFalse();
        request.TokenBudget.ShouldBeNull();
        request.AttributeFilters.ShouldNotBeNull()["status"].ShouldBe(nameof(TenantStatus.Active));
        client.SubmittedQueries.Select(query => query.Request.AggregateId).ShouldBe(["alpha", "hidden", "gamma"]);
        snapshot.IsAuthoritativeSearch.ShouldBeTrue();
        snapshot.Rows.Select(row => row.TenantId).ShouldBe(["gamma", "alpha"]);
        snapshot.Rows.ShouldAllBe(row => row.PendingState == TenantPendingState.Unknown);
        snapshot.Rows.ShouldAllBe(row => row.Name.StartsWith("Authoritative", StringComparison.Ordinal));
        snapshot.HasMore.ShouldBeTrue();
        snapshot.NextCursor.ShouldNotBeNull();
        snapshot.NextCursor.ShouldNotBe("5");
        string scope = TenantSearchCursorScopes.Create(
            "operator-user",
            "needle",
            nameof(TenantStatus.Active),
            TenantListSortColumns.Name,
            descending: true,
            pageSize: 5);
        codec.TryDecode(snapshot.NextCursor, scope, out int nextOffset).ShouldBeTrue();
        nextOffset.ShouldBe(5);
    }

    [Fact]
    public async Task List_search_operational_partial_keeps_verified_rows_and_reports_generic_degradation()
    {
        StubMemoriesClient memories = new();
        memories.Enqueue(SearchResult("needle", 2, Hit("tenant:alpha"), Hit("tenant:beta")));
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(Detail("alpha"));
        client.EnqueueException(new EventStoreGatewayException((int)HttpStatusCode.ServiceUnavailable, "raw secret"));
        TenantQueryGateway gateway = CreateGateway(client, memoriesClient: memories);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Search: "needle", PageSize: 2),
            previous: null,
            CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Degraded);
        snapshot.IsAuthoritativeSearch.ShouldBeTrue();
        snapshot.Reason.ShouldBe(TenantListReason.SearchPartiallyAvailable);
        snapshot.Rows.ShouldHaveSingleItem().TenantId.ShouldBe("alpha");
        snapshot.ToString().ShouldNotContain("raw secret", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("needle", Case.Sensitive);
    }

    [Fact]
    public async Task List_search_total_operational_hydration_loss_falls_back_to_ordinary_list()
    {
        StubMemoriesClient memories = new();
        memories.Enqueue(SearchResult("needle", 1, Hit("tenant:alpha")));
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException((int)HttpStatusCode.ServiceUnavailable, "unavailable"));
        client.EnqueueQueryResult(new PaginatedResult<TenantSummary>(
            [new TenantSummary("fallback", "Fallback", TenantStatus.Active)],
            "ordinary-next",
            true));
        client.EnqueueQueryResult(Detail("fallback"));
        TenantQueryGateway gateway = CreateGateway(client, memoriesClient: memories);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Search: "needle", Cursor: "ordinary-current"),
            previous: null,
            CancellationToken.None);

        snapshot.IsAuthoritativeSearch.ShouldBeFalse();
        snapshot.Notice.ShouldBe(TenantListReason.SearchUnavailable);
        snapshot.Rows.ShouldHaveSingleItem().TenantId.ShouldBe("fallback");
        client.SubmittedQueries[1].Request.QueryType.ShouldBe(ListTenantsQuery.QueryType);
        client.SubmittedQueries[1].Request.Payload.ShouldNotBeNull().GetProperty("cursor").GetString().ShouldBe("ordinary-current");
    }

    [Fact]
    public async Task List_search_recovers_once_at_page_zero_when_index_shrinks()
    {
        var codec = new TenantSearchCursorCodec(new EphemeralDataProtectionProvider());
        string scope = TenantSearchCursorScopes.Create(
            "operator-user",
            "needle",
            status: null,
            TenantListSortColumns.TenantId,
            descending: false,
            pageSize: 20);
        string cursor = codec.Encode(scope, 50);
        StubMemoriesClient memories = new();
        memories.Enqueue(SearchResult("needle", totalCount: 1));
        memories.Enqueue(SearchResult("needle", 1, Hit("tenant:alpha")));
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(Detail("alpha"));
        TenantQueryGateway gateway = CreateGateway(client, memoriesClient: memories, searchCursorCodec: codec);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Search: "needle", SearchCursor: cursor),
            previous: null,
            CancellationToken.None);

        memories.SearchRequests.Select(request => request.Offset).ShouldBe([50, 0]);
        snapshot.IsAuthoritativeSearch.ShouldBeTrue();
        snapshot.PagingRecovered.ShouldBeTrue();
        snapshot.Notice.ShouldBe(TenantListReason.SearchRefreshed);
        snapshot.Rows.ShouldHaveSingleItem().TenantId.ShouldBe("alpha");
    }

    [Fact]
    public async Task List_search_rejects_contradictory_response_and_uses_sanitized_fallback()
    {
        StubMemoriesClient memories = new();
        memories.Enqueue(SearchResult("needle", 1, Hit("tenant:unsafe")) with
        {
            Degraded = true,
            OmittedCount = 1,
            OmittedReason = Hexalith.Memories.Contracts.V1.OmittedReason.Combined,
            UnavailableAxes = ["semantic"],
        });
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantSummary>([], null, false));
        TenantQueryGateway gateway = CreateGateway(client, memoriesClient: memories);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Search: "needle"),
            previous: null,
            CancellationToken.None);

        client.SubmittedQueries.ShouldHaveSingleItem().Request.QueryType.ShouldBe(ListTenantsQuery.QueryType);
        snapshot.IsAuthoritativeSearch.ShouldBeFalse();
        snapshot.Notice.ShouldBe(TenantListReason.SearchUnavailable);
        snapshot.ToString().ShouldNotContain("unsafe", Case.Insensitive);
    }

    [Fact]
    public async Task List_search_propagates_caller_cancellation_without_fallback()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        StubMemoriesClient memories = new();
        memories.Enqueue(new OperationCanceledException(cancellation.Token));
        CapturingGatewayClient client = new();
        TenantQueryGateway gateway = CreateGateway(client, memoriesClient: memories);

        await Should.ThrowAsync<OperationCanceledException>(() => gateway.ListTenantsAsync(
            new TenantListRequest(Search: "needle"),
            previous: null,
            cancellation.Token));

        client.SubmittedQueries.ShouldBeEmpty();
    }

    private static TenantQueryGateway CreateGateway(
        CapturingGatewayClient client,
        string? userId = "operator-user",
        StubMemoriesClient? memoriesClient = null,
        ITenantSearchCursorCodec? searchCursorCodec = null,
        ITenantsBffComposition? bffComposition = null)
        => CreateGateway((IEventStoreGatewayClient)client, userId, memoriesClient, searchCursorCodec, bffComposition);

    private static TenantQueryGateway CreateGateway(
        IEventStoreGatewayClient client,
        string? userId = "operator-user",
        StubMemoriesClient? memoriesClient = null,
        ITenantSearchCursorCodec? searchCursorCodec = null,
        ITenantsBffComposition? bffComposition = null)
    {
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        userContext.UserId.Returns(userId);
        userContext.TenantId.Returns("tenant.context");

        return new TenantQueryGateway(
            client,
            userContext,
            memoriesClient ?? new StubMemoriesClient(),
            searchCursorCodec ?? new TenantSearchCursorCodec(new EphemeralDataProtectionProvider()),
            bffComposition);
    }

    private static ITenantsBffComposition ConfigurationComposition(string json)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
            .Build();
        ITenantConfigurationPrincipalResolver principalResolver = new StubConfigurationPrincipalResolver(
            TenantConfigurationPrincipalEvidence.NonAdministrator("operator-user"));
        return new TenantsBffComposition(
            new UnavailableTenantCommandGateway(),
            principalResolver: principalResolver,
            policyProvider: new TenantConfigurationReadPolicyProvider(configuration));
    }

    private static QueryResponseMetadata ProjectionBackedMetadata(
        bool? isStale = null,
        bool? isDegraded = null,
        DateTimeOffset? servedAt = null,
        string? eTag = null,
        bool? isNotModified = null,
        ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Unknown,
        QueryResponseProvenance provenance = QueryResponseProvenance.ProjectionBacked)
        => new(eTag, isNotModified, isStale, isDegraded, ServedAt: servedAt)
        {
            Provenance = provenance,
            Lifecycle = lifecycle,
        };

    private static MemoriesSearchResult SearchResult(
        string query,
        long totalCount,
        params MemoriesScoredResult[] results)
        => new()
        {
            Query = query,
            TotalCount = totalCount,
            HasIndexedMemoryUnits = totalCount > 0,
            Results = results,
            AxesUsed = ["syntactic"],
        };

    private static MemoriesScoredResult Hit(string sourceUri)
        => new()
        {
            MemoryUnitId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
            Score = 1,
            ContentSnippet = "index-only content that must never render",
            SourceUri = sourceUri,
            SourceType = MemoriesSourceType.Projection,
            Axis = "syntactic",
        };

    private sealed class CapturingGatewayClient : IEventStoreGatewayClient
    {
        private readonly Queue<object> _responses = new();

        public List<SubmittedQuery> SubmittedQueries { get; } = [];

        public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(
            SubmitQueryRequest request,
            string? ifNoneMatch = null,
            CancellationToken cancellationToken = default)
        {
            SubmittedQueries.Add(new SubmittedQuery(request, ifNoneMatch));
            object next = _responses.Dequeue();
            if (next is Exception exception)
            {
                throw exception;
            }

            return Task.FromResult((EventStoreQueryResult<T>)next);
        }

        public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<EventStoreQueryResult> SubmitQueryAsync(
            SubmitQueryRequest request,
            string? ifNoneMatch = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public void EnqueueQueryResult<T>(
            T payload,
            string? eTag = "\"etag\"",
            QueryResponseMetadata? metadata = null,
            bool emitDefaultMetadata = true)
            => _responses.Enqueue(new EventStoreQueryResult<T>(
                "correlation",
                payload,
                IsNotModified: false,
                eTag)
            {
                Metadata = metadata ?? (emitDefaultMetadata
                    ? ProjectionBackedMetadata(eTag: eTag, isStale: false)
                    : null),
            });

        public void EnqueueNotModified(
            string? eTag,
            bool? isStale = null,
            ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Unknown,
            QueryResponseProvenance provenance = QueryResponseProvenance.ProjectionBacked,
            bool emitMetadata = true)
            => _responses.Enqueue(new EventStoreQueryResult<PaginatedResult<TenantSummary>>(
                null,
                null,
                IsNotModified: true,
                eTag)
            {
                Metadata = emitMetadata
                    ? ProjectionBackedMetadata(
                        eTag: eTag,
                        isNotModified: true,
                        isStale: isStale,
                        lifecycle: lifecycle,
                        provenance: provenance)
                    : null,
            });

        public void EnqueueDetailNotModified(
            string? eTag,
            bool? isStale = null,
            ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Unknown)
            => _responses.Enqueue(new EventStoreQueryResult<TenantDetail>(
                null,
                null,
                IsNotModified: true,
                eTag)
            {
                Metadata = ProjectionBackedMetadata(
                    eTag: eTag,
                    isNotModified: true,
                    isStale: isStale,
                    lifecycle: lifecycle),
            });

        public void EnqueueDetailResult(TenantDetail? payload, QueryResponseMetadata? metadata = null)
            => _responses.Enqueue(new EventStoreQueryResult<TenantDetail>(
                "correlation",
                payload,
                IsNotModified: false,
                ETag: metadata?.ETag)
            {
                Metadata = metadata,
            });

        public void EnqueueUserTenantsNotModified(
            string? eTag,
            bool? isStale = null,
            ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Unknown,
            QueryResponseProvenance provenance = QueryResponseProvenance.ProjectionBacked,
            bool emitMetadata = true)
            => _responses.Enqueue(new EventStoreQueryResult<PaginatedResult<UserTenantMembership>>(
                null,
                null,
                IsNotModified: true,
                eTag)
            {
                Metadata = emitMetadata
                    ? ProjectionBackedMetadata(
                        eTag: eTag,
                        isNotModified: true,
                        isStale: isStale,
                        lifecycle: lifecycle,
                        provenance: provenance)
                    : null,
            });

        public void EnqueueGlobalAdministratorsNotModified(
            string? eTag,
            bool? isStale = null,
            ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Unknown,
            QueryResponseProvenance provenance = QueryResponseProvenance.ProjectionBacked,
            bool emitMetadata = true)
            => _responses.Enqueue(new EventStoreQueryResult<PaginatedResult<GlobalAdministratorSummary>>(
                null,
                null,
                IsNotModified: true,
                eTag)
            {
                Metadata = emitMetadata
                    ? ProjectionBackedMetadata(
                        eTag: eTag,
                        isNotModified: true,
                        isStale: isStale,
                        lifecycle: lifecycle,
                        provenance: provenance)
                    : null,
            });

        public void EnqueueAuditNotModified(
            string? eTag,
            bool? isStale = null,
            ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Unknown,
            QueryResponseProvenance provenance = QueryResponseProvenance.ProjectionBacked,
            bool emitMetadata = true)
            => _responses.Enqueue(new EventStoreQueryResult<PaginatedResult<TenantAuditEntry>>(
                null,
                null,
                IsNotModified: true,
                eTag)
            {
                Metadata = emitMetadata
                    ? ProjectionBackedMetadata(
                        eTag: eTag,
                        isNotModified: true,
                        isStale: isStale,
                        lifecycle: lifecycle,
                        provenance: provenance)
                    : null,
            });

        public void EnqueueException(Exception exception)
            => _responses.Enqueue(exception);
    }

    private sealed class StubMemoriesClient : MemoriesClient
    {
        private readonly Queue<object> _responses = new();

        public StubMemoriesClient()
            : base(
                new HttpClient { BaseAddress = new Uri("https://memories.invalid") },
                Options.Create(new MemoriesClientOptions()),
                NullLogger<MemoriesClient>.Instance)
        {
        }

        public List<SearchRequest> SearchRequests { get; } = [];

        public void Enqueue(MemoriesSearchResult result)
            => _responses.Enqueue(result);

        public void Enqueue(Exception exception)
            => _responses.Enqueue(exception);

        public override Task<MemoriesSearchResult> SearchAsync(SearchRequest request, CancellationToken ct)
        {
            SearchRequests.Add(request);
            if (_responses.Count == 0)
            {
                throw new HttpRequestException("Memories unavailable.");
            }

            object response = _responses.Dequeue();
            if (response is Exception exception)
            {
                throw exception;
            }

            return Task.FromResult((MemoriesSearchResult)response);
        }
    }

    private sealed class StubConfigurationPrincipalResolver(TenantConfigurationPrincipalEvidence evidence)
        : ITenantConfigurationPrincipalResolver
    {
        public ValueTask<TenantConfigurationPrincipalEvidence> ResolveAsync(
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(evidence);
    }

    private sealed record SubmittedQuery(SubmitQueryRequest Request, string? IfNoneMatch);

    private static TenantDetail Detail(string tenantId)
        => Detail(
            tenantId,
            new Dictionary<string, string>
            {
                ["billing.mode"] = "trial",
            });

    private static TenantDetail Detail(string tenantId, IReadOnlyDictionary<string, string> configuration)
        => new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            TenantStatus.Active,
            [
                new TenantMember("owner-user", TenantRole.TenantOwner),
                new TenantMember("reader-user", TenantRole.TenantReader),
            ],
            configuration,
            DateTimeOffset.UtcNow);

    private static TenantAuditEntry AuditEntry(string eventId, AuditEventCategory category)
        => new(
            eventId,
            category is AuditEventCategory.Access ? "UserAddedToTenant" : "TenantConfigurationSet",
            category,
            "actor-user",
            DateTimeOffset.UtcNow,
            "tenant.alpha",
            new Dictionary<string, string>
            {
                ["userId"] = "target-user",
                ["key"] = "billing.mode",
                ["role"] = "TenantReader",
            });
}
