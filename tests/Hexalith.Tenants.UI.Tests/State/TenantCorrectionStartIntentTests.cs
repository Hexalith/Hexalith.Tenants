using System.Globalization;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantCorrectionStartIntentTests
{
    [Fact]
    public void Removed_member_with_explicit_role_prepares_add_user_intent_without_history_mutation()
    {
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant", "userId: target-user"),
            intendedRole: TenantRole.TenantReader));

        intent.IsAvailable.ShouldBeTrue();
        intent.IntendedCommandDomain.ShouldBe(TenantCorrectionCommandDomain.Tenants);
        intent.IntendedCommandType.ShouldBe(TenantCorrectionCommandType.AddUserToTenant);
        intent.IntendedRole.ShouldBe(TenantRole.TenantReader);
        intent.OriginalAuditReference.ShouldBe("event-safe-reference");
        intent.RequiredPreviewInputs["tenantId"].ShouldBe("tenant.alpha");
        intent.RequiredPreviewInputs["userId"].ShouldBe("target-user");
        intent.RequiredPreviewInputs["intendedRole"].ShouldBe("TenantReader");
        intent.RequiredPreviewInputs["originalAuditReference"].ShouldBe("event-safe-reference");
        intent.RequiredPreviewInputs.ShouldNotContainKey("payload");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(TenantRole.Unknown)]
    public void Removed_member_requires_explicit_non_unknown_role(TenantRole? role)
    {
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant", "userId: target-user; previousRole: TenantOwner"),
            intendedRole: role));

        intent.IsAvailable.ShouldBeFalse();
        intent.IntendedCommandType.ShouldBe(TenantCorrectionCommandType.AddUserToTenant);
        intent.UnavailableReasons.ShouldContain(TenantCorrectionUnavailableReason.ExplicitRoleRequired);
        intent.RequiredPreviewInputs.ShouldNotContainKey("previousRole");
    }

    [Fact]
    public void Wrong_role_with_explicit_role_and_current_projection_prepares_change_role_intent()
    {
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRoleChanged", "userId: target-user; oldRole: TenantReader; newRole: TenantContributor"),
            intendedRole: TenantRole.TenantReader,
            currentRole: TenantRole.TenantContributor));

        intent.IsAvailable.ShouldBeTrue();
        intent.IntendedCommandDomain.ShouldBe(TenantCorrectionCommandDomain.Tenants);
        intent.IntendedCommandType.ShouldBe(TenantCorrectionCommandType.ChangeUserRole);
        intent.RequiredPreviewInputs["currentRole"].ShouldBe("TenantContributor");
        intent.RequiredPreviewInputs["intendedRole"].ShouldBe("TenantReader");
    }

    [Fact]
    public void Already_applied_member_state_blocks_stale_correction_start()
    {
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRoleChanged", "userId: target-user; newRole: TenantReader"),
            intendedRole: TenantRole.TenantReader,
            currentRole: TenantRole.TenantReader));

        intent.IsAvailable.ShouldBeFalse();
        intent.UnavailableReasons.ShouldContain(TenantCorrectionUnavailableReason.AlreadyApplied);
    }

    [Theory]
    [InlineData(ReadModelFreshnessState.Stale, TenantCorrectionUnavailableReason.FreshnessIndeterminate)]
    [InlineData(ReadModelFreshnessState.Unknown, TenantCorrectionUnavailableReason.FreshnessIndeterminate)]
    public void Non_current_projection_fails_closed(ReadModelFreshnessState freshness, TenantCorrectionUnavailableReason reason)
    {
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant", "userId: target-user", freshness),
            intendedRole: TenantRole.TenantReader));

        intent.IsAvailable.ShouldBeFalse();
        intent.UnavailableReasons.ShouldContain(reason);
    }

    [Fact]
    public void Legacy_current_freshness_without_lifecycle_evidence_fails_closed()
    {
        // The fail-open this gate closes: ResolveFreshness reports Current from the legacy
        // `IsStale == false` fall-through when no authoritative lifecycle evidence exists, but
        // ProjectionLifecyclePolicy.CanMutate denies a mutation on Unknown lifecycle. Gating on
        // freshness alone would arm a correction the platform policy forbids (Story 2.11).
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row(
                "UserRemovedFromTenant",
                "userId: target-user",
                lifecycle: ProjectionLifecycleState.Unknown,
                provenance: QueryResponseProvenance.ProjectionBacked),
            intendedRole: TenantRole.TenantReader));

        intent.IsAvailable.ShouldBeFalse();
        intent.UnavailableReasons.ShouldContain(TenantCorrectionUnavailableReason.FreshnessIndeterminate);
    }

    [Theory]
    [InlineData(QueryResponseProvenance.Unknown)]
    [InlineData(QueryResponseProvenance.HandlerComputed)]
    public void Non_projection_backed_provenance_fails_closed(QueryResponseProvenance provenance)
    {
        // Current lifecycle evidence is only authoritative on a projection-backed route; a
        // handler-computed or unrecognized route may never arm a correction.
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row(
                "UserRemovedFromTenant",
                "userId: target-user",
                lifecycle: ProjectionLifecycleState.Current,
                provenance: provenance),
            intendedRole: TenantRole.TenantReader));

        intent.IsAvailable.ShouldBeFalse();
        intent.UnavailableReasons.ShouldContain(TenantCorrectionUnavailableReason.FreshnessIndeterminate);
    }

    [Theory]
    [InlineData(ProjectionLifecycleState.Stale)]
    [InlineData(ProjectionLifecycleState.Rebuilding)]
    [InlineData(ProjectionLifecycleState.Degraded)]
    [InlineData(ProjectionLifecycleState.Unavailable)]
    [InlineData(ProjectionLifecycleState.LocalOnly)]
    public void Non_current_lifecycle_fails_closed_even_when_freshness_reports_current(
        ProjectionLifecycleState lifecycle)
    {
        // Freshness is deliberately forced to Current so the assertion isolates the lifecycle gate:
        // no operational lifecycle other than Current is projection-confirmed evidence for a mutation.
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row(
                "UserRemovedFromTenant",
                "userId: target-user",
                lifecycle: lifecycle,
                provenance: QueryResponseProvenance.ProjectionBacked),
            intendedRole: TenantRole.TenantReader));

        intent.IsAvailable.ShouldBeFalse();
        intent.UnavailableReasons.ShouldContain(TenantCorrectionUnavailableReason.FreshnessIndeterminate);
    }

    [Fact]
    public void Projection_confirmed_evidence_matches_the_platform_mutation_policy()
    {
        // Positive control: the only combination that arms a correction is exactly the combination
        // ProjectionLifecyclePolicy.CanMutate permits.
        TenantAuditRow row = Row(
            "UserRemovedFromTenant",
            "userId: target-user",
            lifecycle: ProjectionLifecycleState.Current,
            provenance: QueryResponseProvenance.ProjectionBacked);

        ProjectionLifecyclePolicy
            .CanMutate(isAuthorized: true, row.Provenance, row.Lifecycle)
            .ShouldBeTrue();
        TenantCorrectionStartIntent.Evaluate(Context(row, intendedRole: TenantRole.TenantReader))
            .IsAvailable
            .ShouldBeTrue();
    }

    [Theory]
    [InlineData(TenantStatus.Disabled, TenantCorrectionUnavailableReason.TenantDisabled)]
    [InlineData(TenantStatus.Unknown, TenantCorrectionUnavailableReason.TenantLifecycleUnknown)]
    public void Member_correction_blocks_when_tenant_lifecycle_is_not_active(TenantStatus status, TenantCorrectionUnavailableReason reason)
    {
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant", "userId: target-user"),
            intendedRole: TenantRole.TenantReader,
            tenantStatus: status));

        intent.IsAvailable.ShouldBeFalse();
        intent.UnavailableReasons.ShouldContain(reason);
    }

    [Fact]
    public void Global_admin_outcome_uses_global_administrators_domain_when_command_support_exists()
    {
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("GlobalAdministratorRemoved", "userId: admin-user"),
            hasGlobalAdministratorCommandSupport: true));

        intent.IsAvailable.ShouldBeTrue();
        intent.IntendedCommandDomain.ShouldBe(TenantCorrectionCommandDomain.GlobalAdministrators);
        intent.IntendedCommandType.ShouldBe(TenantCorrectionCommandType.SetGlobalAdministrator);
        intent.TenantScope.ShouldBe("global-administrators");
        intent.RequiredPreviewInputs["userId"].ShouldBe("admin-user");
    }

    [Fact]
    public void Global_admin_outcome_blocks_when_command_support_is_absent()
    {
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("GlobalAdministratorRemoved", "userId: admin-user")));

        intent.IsAvailable.ShouldBeFalse();
        intent.IntendedCommandDomain.ShouldBe(TenantCorrectionCommandDomain.GlobalAdministrators);
        intent.IntendedCommandType.ShouldBe(TenantCorrectionCommandType.SetGlobalAdministrator);
        intent.UnavailableReasons.ShouldContain(TenantCorrectionUnavailableReason.GlobalAdministratorCommandSupportUnavailable);
    }

    [Fact]
    public void Membership_correction_fails_closed_when_audit_evidence_lacks_tenant_id()
    {
        // Receipt stays Ready (Scope is present) but the tenant-domain command identifiers are
        // incomplete: a missing tenant id must block correction start rather than prepare a command
        // against an empty aggregate (AC4 fail-closed).
        TenantAuditRow row = new(
            "event-safe-reference",
            "UserRemovedFromTenant",
            AuditEventCategory.Access,
            "actor-user",
            DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
            TenantId: string.Empty,
            Target: "target-user",
            Scope: "tenant.alpha",
            "UserRemovedFromTenant",
            "userId: target-user",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            QueryResponseProvenance.ProjectionBacked);

        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(new(
            TenantAuditReceipt.FromRow(row),
            row,
            IsAuthorized: true,
            HasCurrentProjectionSnapshot: true,
            CurrentProjectionSnapshotReference: "tenant.alpha@current",
            IntendedRole: TenantRole.TenantReader));

        intent.IsAvailable.ShouldBeFalse();
        intent.UnavailableReasons.ShouldContain(TenantCorrectionUnavailableReason.AuditEvidenceUnavailable);
    }

    [Fact]
    public void Global_admin_outcome_fails_closed_when_audit_evidence_lacks_target_user_id()
    {
        // A global-administrator row whose system-scope evidence carries no target user id must block
        // correction start rather than prepare a command against an empty user (AC8 fail-closed),
        // even when command support is otherwise present.
        TenantAuditRow row = new(
            "event-safe-reference",
            "GlobalAdministratorRemoved",
            AuditEventCategory.Administrative,
            "actor-user",
            DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
            TenantId: "system",
            Target: string.Empty,
            Scope: "global-administrators",
            "GlobalAdministratorRemoved",
            ReferenceContext: string.Empty,
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            QueryResponseProvenance.ProjectionBacked);

        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(new(
            TenantAuditReceipt.FromRow(row),
            row,
            IsAuthorized: true,
            HasCurrentProjectionSnapshot: true,
            CurrentProjectionSnapshotReference: "global-administrators@current",
            HasTenantCommandSupport: false,
            HasGlobalAdministratorCommandSupport: true));

        intent.IsAvailable.ShouldBeFalse();
        intent.IntendedCommandDomain.ShouldBe(TenantCorrectionCommandDomain.GlobalAdministrators);
        intent.UnavailableReasons.ShouldContain(TenantCorrectionUnavailableReason.AuditEvidenceUnavailable);
    }

    [Fact]
    public void Unsupported_outcome_fails_closed_without_command_selection()
    {
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("TenantConfigurationSet", "key: billing.mode")));

        intent.IsAvailable.ShouldBeFalse();
        intent.IntendedCommandDomain.ShouldBeNull();
        intent.IntendedCommandType.ShouldBeNull();
        intent.UnavailableReasons.ShouldContain(TenantCorrectionUnavailableReason.UnsupportedOutcome);
    }

    private static TenantCorrectionStartContext Context(
        TenantAuditRow row,
        TenantRole? intendedRole = null,
        TenantRole? currentRole = null,
        TenantStatus tenantStatus = TenantStatus.Active,
        bool hasGlobalAdministratorCommandSupport = false)
        => new(
            TenantAuditReceipt.FromRow(row),
            row,
            IsAuthorized: true,
            HasCurrentProjectionSnapshot: true,
            CurrentProjectionSnapshotReference: "tenant.alpha@current",
            TenantStatus: tenantStatus,
            CurrentRole: currentRole,
            IntendedRole: intendedRole,
            HasTenantCommandSupport: true,
            HasGlobalAdministratorCommandSupport: hasGlobalAdministratorCommandSupport);

    // Defaults describe the only evidence shape that may arm a correction: a projection-backed route
    // reporting Current lifecycle. Tests that exercise a fail-closed path override them explicitly.
    private static TenantAuditRow Row(
        string eventType,
        string referenceContext,
        ReadModelFreshnessState freshness = ReadModelFreshnessState.Current,
        ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Current,
        QueryResponseProvenance provenance = QueryResponseProvenance.ProjectionBacked)
        => new(
            "event-safe-reference",
            eventType,
            eventType.StartsWith("GlobalAdministrator", StringComparison.Ordinal) ? AuditEventCategory.Administrative : AuditEventCategory.Access,
            "actor-user",
            DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
            "tenant.alpha",
            referenceContext.Contains("admin-user", StringComparison.Ordinal) ? "admin-user" : "target-user",
            eventType.StartsWith("GlobalAdministrator", StringComparison.Ordinal) ? "global-administrators" : "tenant.alpha",
            eventType,
            referenceContext,
            freshness,
            lifecycle,
            provenance);
}
