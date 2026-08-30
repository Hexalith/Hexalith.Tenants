using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantDetail;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class GlobalAdministratorActionAvailabilityTests
{
    [Fact]
    public void EligibleEvidenceAllowsGrantAndVisibleRemovalIndependently()
    {
        GlobalAdministratorActionEvidence evidence = ReadyEvidence();

        GlobalAdministratorActionAvailabilityEvaluator.EvaluateGrant(evidence).IsAvailable.ShouldBeTrue();
        GlobalAdministratorActionAvailabilityEvaluator.EvaluateRemove(evidence, "admin-a").IsAvailable.ShouldBeTrue();
        GlobalAdministratorActionAvailabilityEvaluator.EvaluateRemove(evidence, "missing").UnavailableReason
            .ShouldBe(GlobalAdministratorActionUnavailableReason.TargetMissing);
    }

    [Fact]
    public void IncompletePopulationBlocksOnlyRemoval()
    {
        GlobalAdministratorActionEvidence evidence = ReadyEvidence() with { HasCompletePopulation = false };

        GlobalAdministratorActionAvailabilityEvaluator.EvaluateGrant(evidence).IsAvailable.ShouldBeTrue();
        GlobalAdministratorActionAvailabilityEvaluator.EvaluateRemove(evidence, "admin-a").UnavailableReason
            .ShouldBe(GlobalAdministratorActionUnavailableReason.IncompletePopulation);
    }

    [Fact]
    public void CompleteSingleAdministratorBlocksRemovalBeforePreviewSubmission()
    {
        GlobalAdministratorActionEvidence evidence = ReadyEvidence() with
        {
            VisibleRows = [Row("admin-a")],
            CompleteRows = [Row("admin-a")],
        };

        GlobalAdministratorActionAvailabilityEvaluator.EvaluateRemove(evidence, "admin-a").UnavailableReason
            .ShouldBe(GlobalAdministratorActionUnavailableReason.LastAdministrator);
    }

    [Fact]
    public void SafeSeedWithoutBrowserMeasurementFailsClosed()
    {
        GlobalAdministratorActionEvidence evidence = ReadyEvidence() with { HasViewportMeasurement = false };

        GlobalAdministratorActionAvailabilityEvaluator.EvaluateGrant(evidence).UnavailableReason
            .ShouldBe(GlobalAdministratorActionUnavailableReason.UnsafeViewport);
    }

    [Fact]
    public void AuthorizationScopedRowlessEmptyCanGrantButCannotRemove()
    {
        GlobalAdministratorActionEvidence evidence = ReadyEvidence() with
        {
            VisibleKind = GlobalAdministratorsSurfaceKind.Empty,
            VisibleRows = [],
            VisibleIsAuthorizationScopedEmpty = true,
        };

        GlobalAdministratorActionAvailabilityEvaluator.EvaluateGrant(evidence).IsAvailable.ShouldBeTrue();
        GlobalAdministratorActionAvailabilityEvaluator.EvaluateRemove(evidence, "admin-a").UnavailableReason
            .ShouldBe(GlobalAdministratorActionUnavailableReason.TargetMissing);
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void EachLifecycleCapabilityFailsClosedIndependently(
        bool supportsDispatch,
        bool supportsStatus,
        bool supportsRequery)
    {
        GlobalAdministratorActionEvidence evidence = ReadyEvidence() with
        {
            SupportsDispatch = supportsDispatch,
            SupportsStatus = supportsStatus,
            SupportsRequery = supportsRequery,
        };

        GlobalAdministratorActionAvailabilityEvaluator.EvaluateGrant(evidence).UnavailableReason
            .ShouldBe(GlobalAdministratorActionUnavailableReason.MissingLifecycleSupport);
    }

    [Fact]
    public void DuplicateOrInvalidIdentityEvidenceFailsClosedAndDiagnosticsAreSupportSafe()
    {
        GlobalAdministratorActionEvidence evidence = ReadyEvidence() with
        {
            VisibleRows = [Row("admin-secret"), Row("admin-secret")],
            VisibleProjectionVersion = "projection-secret",
        };

        GlobalAdministratorActionAvailability availability =
            GlobalAdministratorActionAvailabilityEvaluator.EvaluateRemove(evidence, "admin-secret");

        availability.IsAvailable.ShouldBeFalse();
    }

    [Fact]
    public void MissingPreviewIsSelectedOnlyAfterEveryRemovalSafetyPrerequisitePasses()
    {
        GlobalAdministratorActionEvidence evidence = ReadyEvidence() with { IsRemovePreviewReady = false };

        GlobalAdministratorActionAvailabilityEvaluator.EvaluateRemove(evidence, "admin-a").UnavailableReason
            .ShouldBe(GlobalAdministratorActionUnavailableReason.MissingConsequencePreview);
    }

    [Theory]
    [InlineData("invalid-row")]
    [InlineData("duplicate-row")]
    [InlineData("surface-kind")]
    [InlineData("freshness")]
    [InlineData("lifecycle")]
    [InlineData("blank-version")]
    [InlineData("version-mismatch")]
    public void IncompletePopulationPrerequisitesPrecedeMissingPreview(string prerequisite)
    {
        GlobalAdministratorActionEvidence evidence = ReadyEvidence() with { IsRemovePreviewReady = false };
        evidence = prerequisite switch
        {
            "invalid-row" => evidence with { CompleteRows = [Row("admin-a"), Row("admin\u0001")] },
            "duplicate-row" => evidence with { CompleteRows = [Row("admin-a"), Row("admin-a")] },
            "surface-kind" => evidence with { CompleteKind = GlobalAdministratorsSurfaceKind.Empty },
            "freshness" => evidence with { CompleteFreshness = ReadModelFreshnessState.Stale },
            "lifecycle" => evidence with { CompleteLifecycle = ProjectionLifecycleState.Unknown },
            "blank-version" => evidence with { CompleteProjectionVersion = " " },
            "version-mismatch" => evidence with { CompleteProjectionVersion = "v2" },
            _ => throw new InvalidOperationException($"Unknown prerequisite '{prerequisite}'."),
        };

        GlobalAdministratorActionAvailabilityEvaluator.EvaluateRemove(evidence, "admin-a").UnavailableReason
            .ShouldBe(GlobalAdministratorActionUnavailableReason.IncompletePopulation);
    }

    private static GlobalAdministratorActionEvidence ReadyEvidence()
        => new(
            IsAuthorized: true,
            GlobalAdministratorsSurfaceKind.Ready,
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            "v1",
            VisibleIsAuthorizationScopedEmpty: false,
            [Row("admin-a"), Row("admin-b")],
            GlobalAdministratorsSurfaceKind.Ready,
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            "v1",
            CompleteIsAuthorizationScopedEmpty: false,
            [Row("admin-a"), Row("admin-b")],
            HasCompletePopulation: true,
            SupportsDispatch: true,
            SupportsStatus: true,
            SupportsRequery: true,
            IsAdmissionAvailable: true,
            IsRemovePreviewReady: true,
            TenantHighImpactViewportState.Safe,
            HasViewportMeasurement: true);

    private static GlobalAdministratorRow Row(string userId)
        => new(userId, ReadModelFreshnessState.Current, ProjectionLifecycleState.Current);
}
