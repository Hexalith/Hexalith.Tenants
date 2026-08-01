using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.UI.State.TenantDetail;

public enum TenantLifecycleOperation {
    EnableTenant,
    DisableTenant,
}

public enum TenantLifecycleAuthorizationReflectionState {
    Indeterminate,
    Authorized,
    MissingPermission,
}

public enum TenantLifecycleGovernanceReadiness {
    Unresolved,
    Ready,
    Blocked,
}

public enum TenantLifecycleUnavailableReasonCategory {
    None,
    MissingPermission,
    StaleData,
    MissingLifecycleSupport,
    HighImpactFlowNotReady,
}

public sealed record TenantLifecycleAvailabilityInput(
    string TenantId,
    TenantStatus CurrentStatus,
    ReadModelFreshnessState Freshness,
    TenantDetailSurfaceKind SurfaceKind,
    bool IsCommandSurfaceConnected,
    TenantLifecycleGovernanceReadiness GovernanceReadiness = TenantLifecycleGovernanceReadiness.Unresolved,
    TenantLifecycleAuthorizationReflectionState AuthorizationReflection = TenantLifecycleAuthorizationReflectionState.Indeterminate,
    bool IsNarrowSafetyContext = false,
    ProjectionLifecycleState Lifecycle = ProjectionLifecycleState.Unknown) {
    public TenantLifecycleAvailability Evaluate(TenantLifecycleOperation operation) {
        // Clause order is part of the contract, not incidental. The strict lifecycle gate stays -- decision
        // D-F (2026-07-31) upheld it and reversed D6, so `IsStale: false` with no lifecycle header is no
        // longer sufficient evidence to enable a mutation -- but it runs AFTER the freshness and surface
        // clauses. Every non-Current surface state (Unavailable, Degraded, Stale, Unknown) also carries a
        // non-Current lifecycle, so with the lifecycle test first it answered for all of them, and an
        // operator whose read had simply failed was told to refresh the projection lifecycle while
        // Tenants.Lifecycle.Unavailable.StaleFreshness became unreachable. Each clause must report the
        // condition the operator can actually act on.
        if (SurfaceKind is TenantDetailSurfaceKind.Stale || Freshness is ReadModelFreshnessState.Stale or ReadModelFreshnessState.Unknown) {
            return Blocked(operation, TenantLifecycleUnavailableReasonCategory.StaleData, "Tenants.Lifecycle.Unavailable.StaleFreshness", TenantCommandFocusTarget.Refresh);
        }

        if (SurfaceKind is TenantDetailSurfaceKind.Unauthorized) {
            return Blocked(operation, TenantLifecycleUnavailableReasonCategory.MissingPermission, "Tenants.Lifecycle.Unavailable.MissingPermission", TenantCommandFocusTarget.Lifecycle);
        }

        if (SurfaceKind is TenantDetailSurfaceKind.Unavailable or TenantDetailSurfaceKind.Unknown or TenantDetailSurfaceKind.Degraded) {
            return Blocked(operation, TenantLifecycleUnavailableReasonCategory.StaleData, "Tenants.Lifecycle.Unavailable.StaleFreshness", TenantCommandFocusTarget.Refresh);
        }

        if (Lifecycle is not ProjectionLifecycleState.Current) {
            return Blocked(operation, TenantLifecycleUnavailableReasonCategory.StaleData, "Tenants.Lifecycle.Unavailable.ProjectionLifecycle", TenantCommandFocusTarget.Refresh);
        }

        if (CurrentStatus is TenantStatus.Unknown) {
            return Blocked(operation, TenantLifecycleUnavailableReasonCategory.MissingLifecycleSupport, "Tenants.Lifecycle.Unavailable.UnknownStatus", TenantCommandFocusTarget.Lifecycle);
        }

        if (operation is TenantLifecycleOperation.EnableTenant && CurrentStatus is TenantStatus.Active) {
            return ExpectedSameStateRejection(operation, "Tenants.Lifecycle.Unavailable.AlreadyActive");
        }

        if (operation is TenantLifecycleOperation.DisableTenant && CurrentStatus is TenantStatus.Disabled) {
            return ExpectedSameStateRejection(operation, "Tenants.Lifecycle.Unavailable.AlreadyDisabled");
        }

        if (IsNarrowSafetyContext) {
            return Blocked(operation, TenantLifecycleUnavailableReasonCategory.HighImpactFlowNotReady, "Tenants.Lifecycle.Unavailable.Mobile", TenantCommandFocusTarget.Lifecycle);
        }

        if (AuthorizationReflection is not TenantLifecycleAuthorizationReflectionState.Authorized) {
            return Blocked(operation, TenantLifecycleUnavailableReasonCategory.MissingPermission, "Tenants.Lifecycle.Unavailable.MissingPermission", TenantCommandFocusTarget.Lifecycle);
        }

        if (!IsCommandSurfaceConnected) {
            return Blocked(operation, TenantLifecycleUnavailableReasonCategory.MissingLifecycleSupport, "Tenants.Lifecycle.Unavailable.CommandSurface", TenantCommandFocusTarget.Lifecycle);
        }

        if (GovernanceReadiness is not TenantLifecycleGovernanceReadiness.Ready) {
            return Blocked(operation, TenantLifecycleUnavailableReasonCategory.HighImpactFlowNotReady, "Tenants.Lifecycle.Unavailable.Governance", TenantCommandFocusTarget.Lifecycle);
        }

        return new(
            TenantId,
            CurrentStatus,
            operation,
            Freshness,
            SurfaceKind,
            IsCommandSurfaceConnected,
            GovernanceReadiness,
            AuthorizationReflection,
            IsUnavailable: false,
            TenantLifecycleUnavailableReasonCategory.None,
            "Tenants.Lifecycle.Available",
            ExpectedDomainOutcomeKey: null,
            TenantCommandFocusTarget.Submit,
            TenantCommandLiveRegionPoliteness.Polite);
    }

    private TenantLifecycleAvailability Blocked(
        TenantLifecycleOperation operation,
        TenantLifecycleUnavailableReasonCategory category,
        string messageKey,
        TenantCommandFocusTarget focusTarget)
        => new(
            TenantId,
            CurrentStatus,
            operation,
            Freshness,
            SurfaceKind,
            IsCommandSurfaceConnected,
            GovernanceReadiness,
            AuthorizationReflection,
            IsUnavailable: true,
            category,
            messageKey,
            ExpectedDomainOutcomeKey: null,
            focusTarget,
            TenantCommandLiveRegionPoliteness.Assertive);

    private TenantLifecycleAvailability ExpectedSameStateRejection(TenantLifecycleOperation operation, string messageKey)
        => new(
            TenantId,
            CurrentStatus,
            operation,
            Freshness,
            SurfaceKind,
            IsCommandSurfaceConnected,
            GovernanceReadiness,
            AuthorizationReflection,
            IsUnavailable: true,
            TenantLifecycleUnavailableReasonCategory.MissingLifecycleSupport,
            messageKey,
            ExpectedDomainOutcomeKey: "TenantLifecycleStateAlreadySet",
            TenantCommandFocusTarget.Lifecycle,
            TenantCommandLiveRegionPoliteness.Polite);
}

public sealed record TenantLifecycleAvailability(
    string TenantId,
    TenantStatus CurrentStatus,
    TenantLifecycleOperation Operation,
    ReadModelFreshnessState Freshness,
    TenantDetailSurfaceKind SurfaceKind,
    bool IsCommandSurfaceConnected,
    TenantLifecycleGovernanceReadiness GovernanceReadiness,
    TenantLifecycleAuthorizationReflectionState AuthorizationReflection,
    bool IsUnavailable,
    TenantLifecycleUnavailableReasonCategory UnavailableReasonCategory,
    string SafeMessageKey,
    string? ExpectedDomainOutcomeKey,
    TenantCommandFocusTarget FocusTarget,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness);
