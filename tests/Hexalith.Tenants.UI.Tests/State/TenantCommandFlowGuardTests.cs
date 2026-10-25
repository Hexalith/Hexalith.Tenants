using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.Resources;

using Microsoft.Extensions.Localization;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantCommandFlowGuardTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("fallback", "Safe fallback")]
    [InlineData("missing-fallback", null)]
    public void Safe_message_lookup_preserves_per_flow_fallback_without_exposing_resource_identifiers(string? fallback, string? expected)
    {
        IStringLocalizer<TenantsResources> localizer = Substitute.For<IStringLocalizer<TenantsResources>>();
        localizer["missing"].Returns(new LocalizedString("missing", "missing", resourceNotFound: true));
        localizer["fallback"].Returns(new LocalizedString("fallback", "Safe fallback"));
        localizer["missing-fallback"].Returns(new LocalizedString("missing-fallback", "missing-fallback", resourceNotFound: true));
        TenantCommandFlowGuard.ResolveSafeMessage(localizer, "missing", "Literal", fallback).ShouldBe(expected);
        TenantCommandFlowGuard.ResolveSafeMessage(localizer, null, "Literal", fallback).ShouldBe("Literal");
    }

    [Theory]
    [InlineData(TenantCommandLifecycleState.Idle, false)]
    [InlineData(TenantCommandLifecycleState.Previewed, false)]
    [InlineData(TenantCommandLifecycleState.RequestSent, true)]
    [InlineData(TenantCommandLifecycleState.Accepted, true)]
    [InlineData(TenantCommandLifecycleState.ProjectionPending, true)]
    [InlineData(TenantCommandLifecycleState.Confirmed, false)]
    [InlineData(TenantCommandLifecycleState.Rejected, false)]
    [InlineData(TenantCommandLifecycleState.AlreadyApplied, false)]
    [InlineData(TenantCommandLifecycleState.DuplicatePrevented, false)]
    [InlineData(TenantCommandLifecycleState.Failed, false)]
    [InlineData(TenantCommandLifecycleState.Degraded, false)]
    [InlineData(TenantCommandLifecycleState.UnableToVerify, false)]
    public void Guard_retains_parent_activity_only_through_submission_and_projection_reconciliation(
        TenantCommandLifecycleState state,
        bool expected)
    {
        TenantCommandFlowGuard.RetainsCommandActivity(state).ShouldBe(expected);
    }

    [Theory]
    [InlineData(TenantCommandLifecycleState.Idle)]
    [InlineData(TenantCommandLifecycleState.Previewed)]
    [InlineData(TenantCommandLifecycleState.RequestSent)]
    [InlineData(TenantCommandLifecycleState.Accepted)]
    [InlineData(TenantCommandLifecycleState.ProjectionPending)]
    [InlineData(TenantCommandLifecycleState.Confirmed)]
    [InlineData(TenantCommandLifecycleState.Rejected)]
    [InlineData(TenantCommandLifecycleState.AlreadyApplied)]
    [InlineData(TenantCommandLifecycleState.DuplicatePrevented)]
    [InlineData(TenantCommandLifecycleState.Failed)]
    [InlineData(TenantCommandLifecycleState.Degraded)]
    [InlineData(TenantCommandLifecycleState.UnableToVerify)]
    public void Guard_keeps_activity_raised_while_local_submission_is_running(TenantCommandLifecycleState state)
    {
        TenantCommandFlowGuard.RetainsCommandActivity(state, isSubmitting: true).ShouldBeTrue();
    }
}
