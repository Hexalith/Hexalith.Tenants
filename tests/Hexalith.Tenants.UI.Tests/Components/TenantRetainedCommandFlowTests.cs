using System.Reflection;

using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants;
using Hexalith.Tenants.UI.Components.Tenants.Members;
using Hexalith.Tenants.UI.Components.Tenants.Metadata;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Hexalith.Tenants.UI.Resources;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>Exercises held delivery and renderer replacement against the real shared circuit retention.</summary>
public sealed class TenantRetainedCommandFlowTests : FluentBunitContext
{
    [Theory]
    [InlineData("create", false)]
    [InlineData("create", true)]
    [InlineData("add", false)]
    [InlineData("add", true)]
    [InlineData("change", false)]
    [InlineData("change", true)]
    [InlineData("remove", false)]
    [InlineData("remove", true)]
    [InlineData("metadata", false)]
    [InlineData("metadata", true)]
    public async Task HeldDeliverySurvivesReplacementBeforeOrAfterCompletion(string kind, bool adoptBeforeCompletion)
    {
        var delivery = new TaskCompletionSource<TenantCommandSubmissionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        ITenantCommandGateway gateway = RegisterGateway((_, _) => delivery.Task);
        (IRenderedComponent<IComponent> cut, object component) = RenderFlow(kind);
        Submit(kind, cut);
        TenantAggregateCommandAdmissionGate gate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        TenantCommandAttemptTracker tracker = Services.GetRequiredService<TenantCommandAttemptTracker>();
        object initial = Snapshot(component);
        Property<string>(initial, "MessageId").ShouldNotBeNullOrWhiteSpace();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
        gate.TryAcquire(TenantCommandAggregateLock.ForTenant("tenant.beta"), new object()).ShouldBeTrue();
        gate.TryAcquire(TenantCommandAggregateLock.ForTenant("tenant.alpha"), new object()).ShouldBeFalse();
        ((IDisposable)component).Dispose();
        IRenderedComponent<IComponent>? replacement = null;
        object? replacementComponent = null;
        if (adoptBeforeCompletion)
        {
            (replacement, replacementComponent) = RenderFlow(kind);
            if (kind is "create")
            {
                Submit(kind, replacement);
            }
            Property<TenantCommandLifecycleState>(Snapshot(replacementComponent), "State").ShouldBe(TenantCommandLifecycleState.RequestSent);
        }
        delivery.SetResult(TenantCommandSubmissionResult.Accepted(Property<string>(initial, "MessageId")!, "correlation-original"));
        await WaitForAsync(() => ReadRetained(tracker, kind) is { } retained
            && Property<string>(retained, "CorrelationId") is "correlation-original");
        if (!adoptBeforeCompletion)
        {
            (replacement, replacementComponent) = RenderFlow(kind);
            if (kind is "create")
            {
                Submit(kind, replacement);
            }
        }
        await WaitForAsync(() => Property<string>(Snapshot(replacementComponent!), "CorrelationId") is "correlation-original");
        object latest = Snapshot(replacementComponent!);
        Property<string>(latest, "MessageId").ShouldBe(Property<string>(initial, "MessageId"));
        Property<string>(latest, "BaselineProjectionVersion").ShouldBe(Property<string>(initial, "BaselineProjectionVersion"));
        Property<object>(latest, "Intent").ShouldBe(Property<object>(initial, "Intent"));
        if (kind is "create" or "remove" or "metadata")
        {
            Property<DateTimeOffset?>(latest, "AttemptStartedAtUtc").ShouldBe(Property<DateTimeOffset?>(initial, "AttemptStartedAtUtc"));
        }
        tracker.HasPendingOwnership("tenant.alpha").ShouldBeTrue();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
        gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == DispatchName(kind)).ShouldBe(1);
        // A different typed surface cannot replace the retained attempt or obtain its admission.
        (IRenderedComponent<IComponent> competitor, _) = RenderFlow(kind is "create" ? "add" : "create");
        Submit(kind is "create" ? "add" : "create", competitor);
        gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == DispatchName(kind is "create" ? "add" : "create")).ShouldBe(0);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("metadata")]
    public async Task AmbiguousRetryUsesTheOriginalIntentIdentityAndBaseline(string kind)
    {
        List<(object Intent, string MessageId)> deliveries = [];
        RegisterGateway((intent, id) =>
        {
            deliveries.Add((intent, id));
            return Task.FromResult(deliveries.Count == 1
                ? TenantCommandSubmissionResult.Failed("Delivery is uncertain.") with { MessageId = id, IsAmbiguousFailure = true }
                : TenantCommandSubmissionResult.Accepted(id, "correlation-original"));
        });
        (IRenderedComponent<IComponent> cut, object component) = RenderFlow(kind);
        Submit(kind, cut);
        object initial = Snapshot(component);
        Property<TenantCommandLifecycleState>(initial, "State").ShouldBe(TenantCommandLifecycleState.RequestSent);
        ((IDisposable)component).Dispose();
        (IRenderedComponent<IComponent> replacement, object replacementComponent) = RenderFlow(kind, "tenant-sequence:900");
        if (kind is "create")
        {
            Submit(kind, replacement);
        }
        else
        {
            replacement.Find("form").Submit();
        }
        await WaitForAsync(() => deliveries.Count == 2);
        deliveries[1].ShouldBe(deliveries[0]);
        object latest = Snapshot(replacementComponent);
        Property<string>(latest, "BaselineProjectionVersion").ShouldBe(Property<string>(initial, "BaselineProjectionVersion"));
        Property<object>(latest, "Intent").ShouldBe(Property<object>(initial, "Intent"));
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    [InlineData("metadata")]
    public void RefusedPreDispatchAdmissionDoesNotRetainALease(string kind)
    {
        ITenantCommandGateway gateway = RegisterGateway((_, id) => Task.FromResult(TenantCommandSubmissionResult.Accepted(id, "unused")));
        (IRenderedComponent<IComponent> cut, _) = RenderFlow(kind, lease: _ => Task.FromResult(false));
        Submit(kind, cut);
        Services.GetRequiredService<TenantAggregateCommandAdmissionGate>()
            .IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
        Services.GetRequiredService<TenantCommandAttemptTracker>().HasPendingOwnership("tenant.alpha").ShouldBeFalse();
        gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == DispatchName(kind)).ShouldBe(0);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    [InlineData("metadata")]
    public async Task HeldStatusCompletesOwnerlessWithoutReleasingAReplacementLease(string kind)
    {
        var status = new TaskCompletionSource<TenantCommandStatusResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        ITenantCommandGateway gateway = RegisterGateway((_, id) => Task.FromResult(TenantCommandSubmissionResult.Accepted(id, "correlation-original")));
        gateway.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>()).Returns(status.Task);
        (IRenderedComponent<IComponent> cut, object component) = RenderFlow(kind);
        Submit(kind, cut);
        await WaitForAsync(() => gateway.ReceivedCalls().Any(call => call.GetMethodInfo().Name is nameof(ITenantCommandGateway.GetStatusAsync)));
        ((IDisposable)component).Dispose();
        status.SetResult(new(CommandStatus.Rejected, HasVerifiedCommandIdentity: true));
        TenantCommandAttemptTracker tracker = Services.GetRequiredService<TenantCommandAttemptTracker>();
        await WaitForAsync(() => ReadRetained(tracker, kind) is { } retained
            && Property<TenantCommandLifecycleState>(retained, "State") is TenantCommandLifecycleState.Rejected);
        TenantAggregateCommandAdmissionGate gate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        string key = TenantCommandAggregateLock.ForTenant("tenant.alpha");
        gate.IsLocked(key).ShouldBeFalse();
        object foreignOwner = new();
        gate.TryAcquire(key, foreignOwner).ShouldBeTrue();
        ((IDisposable)component).Dispose();
        gate.IsLocked(key).ShouldBeTrue();
        tracker.HasPendingOwnership("tenant.alpha").ShouldBeFalse();
    }

    [Theory]
    [InlineData("create", "authorization")]
    [InlineData("create", "freshness")]
    [InlineData("add", "authorization")]
    [InlineData("add", "freshness")]
    [InlineData("change", "authorization")]
    [InlineData("change", "freshness")]
    [InlineData("metadata", "authorization")]
    [InlineData("metadata", "freshness")]
    public void AmbiguousRecoveryHonorsExistingAuthorizationAndFreshnessGates(string kind, string deniedGate)
    {
        ITenantCommandGateway gateway = RegisterGateway((_, id) => Task.FromResult(
            TenantCommandSubmissionResult.Failed("Delivery is uncertain.") with { MessageId = id, IsAmbiguousFailure = true }));
        (IRenderedComponent<IComponent> cut, object component) = RenderFlow(kind);
        Submit(kind, cut);
        object original = Snapshot(component);
        if (deniedGate is "authorization")
        {
            component.GetType().GetProperty("IsAuthorized")!.SetValue(component, false);
        }
        else if (kind is "create")
        {
            component.GetType().GetProperty("IsFresh")!.SetValue(component, false);
        }
        else
        {
            component.GetType().GetProperty("Freshness")!.SetValue(component, ReadModelFreshnessState.Stale);
        }
        cut.Find("form").Submit();
        gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == DispatchName(kind)).ShouldBe(1);
        Property<string>(Snapshot(component), "MessageId").ShouldBe(Property<string>(original, "MessageId"));
        Property<object>(Snapshot(component), "Intent").ShouldBe(Property<object>(original, "Intent"));
        Services.GetRequiredService<TenantAggregateCommandAdmissionGate>()
            .IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public async Task ContinueDuringHeldStatusCannotResurrectThePanelOrReleaseAForeignLease(string kind)
    {
        ITenantCommandGateway gateway = RegisterGateway((_, id) => Task.FromResult(TenantCommandSubmissionResult.Accepted(id, "correlation-original")));
        gateway.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantCommandStatusResult.Unknown("Temporarily unavailable")));
        (IRenderedComponent<IComponent> cut, object component) = RenderFlow(kind);
        Submit(kind, cut);
        Property<TenantCommandLifecycleState>(Snapshot(component), "State").ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        var held = new TaskCompletionSource<TenantCommandStatusResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>()).Returns(held.Task);
        MethodInfo refresh = component.GetType().GetMethod("RefreshStatusAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Task refreshTask = cut.InvokeAsync(() => (Task)refresh.Invoke(component, null)!);
        await WaitForAsync(() => gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name is nameof(ITenantCommandGateway.GetStatusAsync)) == 2);
        MethodInfo continueReadOnly = component.GetType().GetMethod("ContinueReadOnlyAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await cut.InvokeAsync(() => (Task)continueReadOnly.Invoke(component, null)!);
        Property<TenantCommandLifecycleState>(Snapshot(component), "State").ShouldBe(TenantCommandLifecycleState.Idle);
        TenantCommandAttemptTracker tracker = Services.GetRequiredService<TenantCommandAttemptTracker>();
        object qualifiedTerminalOwner = new();
        tracker.TryAdopt<object>("tenant.alpha", qualifiedTerminalOwner, out TenantRetainedCommandAttempt? oldAttempt, out object? retained).ShouldBeTrue();
        object terminal = ApplyStatus(retained!, new(CommandStatus.Rejected, HasVerifiedCommandIdentity: true));
        tracker.TryUpdate(oldAttempt!, retained!, terminal).ShouldBeTrue();
        TenantAggregateCommandAdmissionGate gate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        object foreignOwner = new();
        string key = TenantCommandAggregateLock.ForTenant("tenant.alpha");
        gate.TryAcquireLease(key, foreignOwner, out TenantAggregateCommandLease? foreignLease).ShouldBeTrue();
        foreignLease!.TryMarkDispatched(foreignOwner).ShouldBeTrue();
        held.SetResult(new(CommandStatus.Processing, HasVerifiedCommandIdentity: true));
        await refreshTask.WaitAsync(TimeSpan.FromSeconds(5));
        Property<TenantCommandLifecycleState>(Snapshot(component), "State").ShouldBe(TenantCommandLifecycleState.Idle);
        Property<TenantCommandLifecycleState>(ReadRetained(tracker, kind)!, "State").ShouldBe(TenantCommandLifecycleState.Rejected);
        gate.IsLocked(key).ShouldBeTrue();
        foreignLease.TryReleaseTerminal(foreignOwner, TenantCommandLifecycleState.Rejected).ShouldBeTrue();
    }

    [Theory]
    [InlineData("create")]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("metadata")]
    public async Task CurrentAuthoritativeConfirmationSurvivesLateProcessingStatusAndProtectsForeignAdmission(string kind)
    {
        ITenantCommandGateway gateway = RegisterGateway((_, id) => Task.FromResult(TenantCommandSubmissionResult.Accepted(id, "correlation-original")));
        gateway.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true)));
        (IRenderedComponent<IComponent> cut, object component) = RenderFlow(kind);
        Submit(kind, cut);
        Property<TenantCommandLifecycleState>(Snapshot(component), "State").ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        var held = new TaskCompletionSource<TenantCommandStatusResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>()).Returns(held.Task);
        MethodInfo refresh = component.GetType().GetMethod("RefreshStatusAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Task refreshTask = cut.InvokeAsync(() => (Task)refresh.Invoke(component, null)!);
        await WaitForAsync(() => gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name is nameof(ITenantCommandGateway.GetStatusAsync)) == 2);
        MethodInfo applyProjection = component.GetType().GetMethod("ApplyProjectionEvidence", BindingFlags.Instance | BindingFlags.NonPublic)!;
        object proof;
        if (kind is "create")
        {
            proof = TenantCommandProofFixtures.ListProof(new("tenant.alpha", "Alpha", TenantStatus.Active), "tenant-sequence:2")!;
        }
        else
        {
            TenantDetail detail = new("tenant.alpha", kind is "metadata" ? "Renamed" : "Alpha", "Original", TenantStatus.Active,
                [new("owner", TenantRole.TenantOwner), new(kind is "add" ? "new-reader" : "reader", kind is "add" ? TenantRole.TenantReader : TenantRole.TenantContributor)],
                new Dictionary<string, string>(), DateTimeOffset.UtcNow);
            proof = TenantCommandProofFixtures.Proof(detail, "tenant-sequence:2")!;
        }
        await cut.InvokeAsync(() => applyProjection.Invoke(component, [proof]));
        Property<TenantCommandLifecycleState>(Snapshot(component), "State").ShouldBe(TenantCommandLifecycleState.Confirmed);
        TenantAggregateCommandAdmissionGate gate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        string key = TenantCommandAggregateLock.ForTenant("tenant.alpha");
        object foreignOwner = new();
        gate.TryAcquireLease(key, foreignOwner, out TenantAggregateCommandLease? foreign).ShouldBeTrue();
        held.SetResult(new(CommandStatus.Processing, HasVerifiedCommandIdentity: true));
        await refreshTask.WaitAsync(TimeSpan.FromSeconds(5));
        Property<TenantCommandLifecycleState>(Snapshot(component), "State").ShouldBe(TenantCommandLifecycleState.Confirmed);
        gate.IsLocked(key).ShouldBeTrue();
        foreign!.TryAbandonBeforeDispatch(foreignOwner).ShouldBeTrue();
    }

    private static object ApplyStatus(object snapshot, TenantCommandStatusResult status)
        => snapshot.GetType().GetMethod("ApplyStatus")!.Invoke(snapshot, [status])!;

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    [InlineData("metadata")]
    public async Task DisposedPreDispatchAcquisitionCannotMutateAReplacementSubmission(string kind)
    {
        var firstAcquisition = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var acquisitionEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var replacementDelivery = new TaskCompletionSource<TenantCommandSubmissionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        ITenantCommandGateway gateway = RegisterGateway((_, _) => replacementDelivery.Task);
        int acquisitions = 0;
        Func<bool, Task<bool>> lease = active =>
        {
            if (active && Interlocked.Increment(ref acquisitions) == 1)
            {
                acquisitionEntered.TrySetResult();
                return firstAcquisition.Task;
            }
            return Task.FromResult(true);
        };
        (IRenderedComponent<IComponent> first, object firstComponent) = RenderFlow(kind, lease: lease);
        Submit(kind, first, send: false);
        Task initialSubmit = first.Find("form").SubmitAsync();
        await acquisitionEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        ((IDisposable)firstComponent).Dispose();
        TenantAggregateCommandAdmissionGate gate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        string key = TenantCommandAggregateLock.ForTenant("tenant.alpha");
        gate.IsLocked(key).ShouldBeFalse();
        (IRenderedComponent<IComponent> replacement, object component) = RenderFlow(kind, lease: lease);
        Submit(kind, replacement, send: false);
        Task nextSubmit = replacement.Find("form").SubmitAsync();
        await WaitForAsync(() => gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == DispatchName(kind)) == 1);
        FieldInfo submitting = component.GetType().GetField("_isSubmitting", BindingFlags.Instance | BindingFlags.NonPublic)!;
        ((bool)submitting.GetValue(component)!).ShouldBeTrue();
        firstAcquisition.SetResult(true);
        await initialSubmit.WaitAsync(TimeSpan.FromSeconds(5));
        ((bool)submitting.GetValue(component)!).ShouldBeTrue();
        gate.IsLocked(key).ShouldBeTrue();
        object pending = Snapshot(component);
        replacementDelivery.SetResult(TenantCommandSubmissionResult.Accepted(Property<string>(pending, "MessageId")!, "replacement-correlation"));
        await nextSubmit.WaitAsync(TimeSpan.FromSeconds(5));
        Property<string>(Snapshot(component), "CorrelationId").ShouldBe("replacement-correlation");
        gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == DispatchName(kind)).ShouldBe(1);
        gate.IsLocked(key).ShouldBeTrue();
    }

    [Theory]
    [InlineData("create")]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    [InlineData("metadata")]
    public void MissingCircuitAdmissionFailsClosedThroughRenderingSubmissionAndDisposal(string kind)
    {
        ITenantCommandGateway gateway = RegisterGateway((_, id) => Task.FromResult(TenantCommandSubmissionResult.Accepted(id, "unused")));
        Services.RemoveAll<TenantAggregateCommandAdmissionGate>();
        (IRenderedComponent<IComponent> cut, object component) = RenderFlow(kind);
        if (kind is "metadata")
        {
            cut.FindAll("[data-testid='tenants-edit-metadata-open']").ShouldBeEmpty();
        }
        else
        {
            Submit(kind, cut);
        }
        ((IDisposable)component).Dispose();
        gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == DispatchName(kind)).ShouldBe(0);
    }

    [Theory]
    [InlineData("add", "user")]
    [InlineData("add", "role")]
    [InlineData("add", "invalid-role")]
    [InlineData("change", "role")]
    [InlineData("change", "invalid-role")]
    [InlineData("metadata", "name")]
    [InlineData("metadata", "description")]
    public void ChangedFormCannotSilentlyRedispatchTheOriginalAmbiguousIntent(string kind, string changedField)
    {
        ITenantCommandGateway gateway = RegisterGateway((_, id) => Task.FromResult(
            TenantCommandSubmissionResult.Failed("Delivery is uncertain.") with { MessageId = id, IsAmbiguousFailure = true }));
        (IRenderedComponent<IComponent> cut, object component) = RenderFlow(kind);
        Submit(kind, cut);
        object original = Snapshot(component);
        if (changedField is "user")
        {
            cut.Find("[data-testid='tenants-add-member-user-id']").Change("different-user");
        }
        else if (changedField is "role" or "invalid-role")
        {
            ChangeRoleSelection(cut, kind is "add" ? "tenants-add-member-role" : "tenants-change-role-new-role",
                changedField is "invalid-role" ? "Unknown" : nameof(TenantRole.TenantOwner));
        }
        else
        {
            cut.Find(changedField is "name" ? "[data-testid='tenants-edit-metadata-name']" : "[data-testid='tenants-edit-metadata-description']")
                .Change("A different intended value");
        }
        cut.Find("form").Submit();
        gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == DispatchName(kind)).ShouldBe(1);
        object latest = Snapshot(component);
        Property<object>(latest, "Intent").ShouldBe(Property<object>(original, "Intent"));
        Property<string>(latest, "MessageId").ShouldBe(Property<string>(original, "MessageId"));
        Property<string>(latest, "BaselineProjectionVersion").ShouldBe(Property<string>(original, "BaselineProjectionVersion"));
        Property<TenantCommandLifecycleState>(latest, "State").ShouldBe(TenantCommandLifecycleState.RequestSent);
        Services.GetRequiredService<TenantAggregateCommandAdmissionGate>().IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    [InlineData("metadata")]
    public async Task FaultedOldActivityAcquisitionCannotDetachTheNewRouteAttempt(string kind)
    {
        var oldAcquisition = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var nextDelivery = new TaskCompletionSource<TenantCommandSubmissionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        ITenantCommandGateway gateway = RegisterGateway((_, _) => nextDelivery.Task);
        int acquisitions = 0;
        Func<bool, Task<bool>> lease = active =>
        {
            if (active && Interlocked.Increment(ref acquisitions) == 1)
            {
                oldEntered.TrySetResult();
                return oldAcquisition.Task;
            }
            return Task.FromResult(true);
        };
        (IRenderedComponent<IComponent> cut, object component) = RenderFlow(kind, lease: lease);
        Submit(kind, cut, send: false);
        Task oldSubmit = cut.Find("form").SubmitAsync();
        await oldEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        TenantDetail oldDetail = (TenantDetail)component.GetType().GetProperty("Detail")!.GetValue(component)!;
        await cut.InvokeAsync(() => ((IComponent)component).SetParametersAsync(ParameterView.FromDictionary(
            new Dictionary<string, object?> { ["Detail"] = oldDetail with { TenantId = "tenant.beta" } })));
        Task newSubmit = cut.Find("form").SubmitAsync();
        await WaitForAsync(() => gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == DispatchName(kind)) == 1);
        object newSnapshot = Snapshot(component);
        TenantAggregateCommandAdmissionGate gate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.beta")).ShouldBeTrue();
        oldAcquisition.SetException(new InvalidOperationException("Old parent callback faulted"));
        await oldSubmit.WaitAsync(TimeSpan.FromSeconds(5));
        component.GetType().GetField("_retainedAttempt", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(component).ShouldNotBeNull();
        nextDelivery.SetResult(TenantCommandSubmissionResult.Accepted(Property<string>(newSnapshot, "MessageId")!, "new-route-correlation"));
        await newSubmit.WaitAsync(TimeSpan.FromSeconds(5));
        Property<string>(Snapshot(component), "CorrelationId").ShouldBe("new-route-correlation");
        Property<object>(Snapshot(component), "Intent").ShouldBe(Property<object>(newSnapshot, "Intent"));
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.beta")).ShouldBeTrue();
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public async Task AmbiguousDeliveryNudgePreservesOriginalIdRecovery(string kind)
    {
        List<(object Intent, string Id)> deliveries = [];
        ITenantCommandGateway gateway = RegisterGateway((intent, id) =>
        {
            deliveries.Add((intent, id));
            return Task.FromResult(deliveries.Count == 1
                ? TenantCommandSubmissionResult.Failed("Uncertain") with { MessageId = id, IsAmbiguousFailure = true }
                : TenantCommandSubmissionResult.Accepted(id, "original-correlation"));
        });
        (IRenderedComponent<IComponent> cut, object component) = RenderFlow(kind);
        Submit(kind, cut);
        object original = Snapshot(component);
        MethodInfo nudge = component.GetType().GetMethod("HandleAuthoritativeRefreshNudgeAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await cut.InvokeAsync(() => (Task)nudge.Invoke(component, null)!);
        Property<TenantCommandLifecycleState>(Snapshot(component), "State").ShouldBe(TenantCommandLifecycleState.RequestSent);
        gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name is nameof(ITenantCommandGateway.GetStatusAsync)).ShouldBe(0);
        cut.Find("form").Submit();
        await WaitForAsync(() => deliveries.Count == 2);
        deliveries[1].ShouldBe(deliveries[0]);
        Property<string>(Snapshot(component), "BaselineProjectionVersion").ShouldBe(Property<string>(original, "BaselineProjectionVersion"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RemountedAmbiguousRemovalRequiresLiteralConfirmationAndCompletePreview(bool incompletePreview)
    {
        ITenantCommandGateway gateway = RegisterGateway((_, id) => Task.FromResult(
            TenantCommandSubmissionResult.Failed("Uncertain") with { MessageId = id, IsAmbiguousFailure = true }));
        bool complete = true;
        IStringLocalizer<TenantsResources> localizer = Substitute.For<IStringLocalizer<TenantsResources>>();
        localizer[Arg.Any<string>()].Returns(call => new LocalizedString(call.Arg<string>(),
            !complete && call.Arg<string>() is "Tenants.RemoveMember.Preview.AccessPath.Value" ? "" : call.Arg<string>()));
        localizer[Arg.Any<string>(), Arg.Any<object[]>()].Returns(call => new LocalizedString(call.ArgAt<string>(0), call.ArgAt<string>(0)));
        Services.AddSingleton(localizer);
        (IRenderedComponent<IComponent> first, object firstComponent) = RenderFlow("remove");
        Submit("remove", first);
        object original = Snapshot(firstComponent);
        ((IDisposable)firstComponent).Dispose();
        complete = !incompletePreview;
        (IRenderedComponent<IComponent> replacement, object component) = RenderFlow("remove");
        if (incompletePreview)
        {
            replacement.Find("[data-testid='tenants-remove-member-confirmation']").Change("reader");
        }
        await replacement.Find("form").SubmitAsync();
        gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name is nameof(ITenantCommandGateway.RemoveUserFromTenantAsync)).ShouldBe(1);
        Property<string>(Snapshot(component), "MessageId").ShouldBe(Property<string>(original, "MessageId"));
        Property<object>(Snapshot(component), "Intent").ShouldBe(Property<object>(original, "Intent"));
        Property<TenantCommandLifecycleState>(Snapshot(component), "State").ShouldBe(TenantCommandLifecycleState.RequestSent);
        Services.GetRequiredService<TenantAggregateCommandAdmissionGate>().IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
    }

    [Theory]
    [InlineData("add", false)]
    [InlineData("add", true)]
    [InlineData("change", false)]
    [InlineData("change", true)]
    [InlineData("remove", false)]
    [InlineData("remove", true)]
    [InlineData("metadata", false)]
    [InlineData("metadata", true)]
    public async Task HeldAdmissionCannotDispatchAfterSuppliedEligibilityDenial(string kind, bool changedRoute)
    {
        var approval = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        ITenantCommandGateway gateway = RegisterGateway((_, id) => Task.FromResult(TenantCommandSubmissionResult.Accepted(id, "unused")));
        (IRenderedComponent<IComponent> cut, object component) = RenderFlow(kind, lease: active => active ? approval.Task : Task.FromResult(true));
        Submit(kind, cut, send: false);
        Task submission = cut.Find("form").SubmitAsync();
        TenantAggregateCommandAdmissionGate gate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        await WaitForAsync(() => gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")));
        TenantDetail detail = (TenantDetail)component.GetType().GetProperty("Detail")!.GetValue(component)!;
        await cut.InvokeAsync(() => ((IComponent)component).SetParametersAsync(ParameterView.FromDictionary(
            new Dictionary<string, object?> { ["IsAuthorized"] = false, ["Detail"] = detail with { TenantId = changedRoute ? "tenant.beta" : detail.TenantId } })));
        approval.SetResult(true);
        await submission.WaitAsync(TimeSpan.FromSeconds(5));
        gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == DispatchName(kind)).ShouldBe(0);
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.beta")).ShouldBeFalse();
        Services.GetRequiredService<TenantCommandAttemptTracker>().HasPendingOwnership("tenant.alpha").ShouldBeFalse();
    }

    [Theory]
    [InlineData("add", false)]
    [InlineData("add", true)]
    [InlineData("change", false)]
    [InlineData("change", true)]
    [InlineData("remove", false)]
    [InlineData("remove", true)]
    [InlineData("metadata", false)]
    [InlineData("metadata", true)]
    public async Task CallbackOnlyFaultAbandonsOnlyItsCapturedPreDispatchAttempt(string kind, bool replace)
    {
        var callback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int acquisitions = 0;
        EventCallback<bool> activity = EventCallback.Factory.Create<bool>(this, active => active && Interlocked.Increment(ref acquisitions) == 1 ? callback.Task : Task.CompletedTask);
        ITenantCommandGateway gateway = RegisterGateway((_, id) => Task.FromResult(TenantCommandSubmissionResult.Accepted(id, "replacement-correlation")));
        (IRenderedComponent<IComponent> cut, object component) = RenderFlow(kind, activity: activity);
        Submit(kind, cut, send: false);
        Task old = cut.Find("form").SubmitAsync();
        await WaitForAsync(() => acquisitions == 1);
        if (replace)
        {
            ((IDisposable)component).Dispose();
            (IRenderedComponent<IComponent> replacement, object replacementComponent) = RenderFlow(kind, activity: activity);
            Submit(kind, replacement);
            component = replacementComponent;
        }
        callback.SetException(new InvalidOperationException("Admission callback faulted"));
        await old.WaitAsync(TimeSpan.FromSeconds(5));
        TenantAggregateCommandAdmissionGate gate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBe(replace);
        gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == DispatchName(kind)).ShouldBe(replace ? 1 : 0);
        if (replace)
        {
            Property<string>(Snapshot(component), "CorrelationId").ShouldBe("replacement-correlation");
        }
    }

    [Theory]
    [InlineData("caller-a", true)]
    [InlineData("caller-b", false)]
    [InlineData(null, false)]
    public void RetainedMetadataReadPresentationUsesCurrentAuthorizedCallerDetail(string? currentCaller, bool sameCaller)
    {
        RegisterGateway((_, id) => Task.FromResult(TenantCommandSubmissionResult.Accepted(id, "unused")));
        TenantDetail privateDetail = new("tenant.alpha", "Caller A name", "Caller A description", TenantStatus.Active,
            [new("owner", TenantRole.TenantOwner)], new Dictionary<string, string>(), DateTimeOffset.UtcNow);
        TenantUpdateMetadataCommandSnapshot pending = (TenantUpdateMetadataCommandSnapshot.Idle(privateDetail.Name, privateDetail.Description, privateDetail)
            with { LastConfirmedCallerScope = "caller-a" })
            .RequestSent(new("tenant.alpha", "Original intent", "Original intent description"), "tenant-sequence:1")
            .Accepted(TenantCommandSubmissionResult.Accepted("original-message", "original-correlation"));
        TenantCommandAttemptTracker tracker = Services.GetRequiredService<TenantCommandAttemptTracker>();
        object owner = new();
        tracker.TryBegin("tenant.alpha", owner, pending, out TenantRetainedCommandAttempt? attempt).ShouldBeTrue();
        tracker.TryStartDelivery(attempt!, owner).ShouldBeTrue();
        tracker.TryCompleteDelivery(attempt!, pending, pending).ShouldBeTrue();
        tracker.Detach(attempt, owner);
        TenantDetail currentDetail = privateDetail with { Name = "Current authorized name", Description = "Current authorized description" };
        IRenderedComponent<CascadingValue<string?>> wrapper = Render<CascadingValue<string?>>(p => p
            .Add(x => x.Name, "CurrentTenantCallerScope").Add(x => x.Value, currentCaller)
            .AddChildContent<EditTenantMetadataFlow>(child => child.Add(x => x.Detail, currentDetail)
                .Add(x => x.SurfaceKind, TenantDetailSurfaceKind.Ready).Add(x => x.Freshness, ReadModelFreshnessState.Current)
                .Add(x => x.Lifecycle, ProjectionLifecycleState.Current).Add(x => x.ProjectionVersion, "tenant-sequence:900")));
        IRenderedComponent<EditTenantMetadataFlow> cut = wrapper.FindComponent<EditTenantMetadataFlow>();
        string display = cut.Find("[data-testid='tenants-edit-metadata-confirmed']").TextContent;
        display.ShouldContain(sameCaller ? privateDetail.Name : currentDetail.Name);
        display.ShouldContain(sameCaller ? privateDetail.Description! : currentDetail.Description!);
        if (!sameCaller)
        {
            display.ShouldNotContain("Caller A");
        }
        cut.Instance.Snapshot.ShouldBe(pending);
        cut.Instance.Snapshot.Intent.ShouldBe(pending.Intent);
        cut.Instance.Snapshot.MessageId.ShouldBe(pending.MessageId);
        cut.Instance.Snapshot.BaselineProjectionVersion.ShouldBe(pending.BaselineProjectionVersion);
        Services.GetRequiredService<TenantAggregateCommandAdmissionGate>().IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
    }

    [Fact]
    public async Task CallerBoundLocalMetadataReadIsWithheldWhileCurrentCallerResolutionIsPending()
    {
        RegisterGateway((_, id) => Task.FromResult(TenantCommandSubmissionResult.Accepted(id, "original-correlation")));
        TenantDetail originalDetail = new("tenant.alpha", "Caller A name", "Caller A description", TenantStatus.Active,
            [new("owner", TenantRole.TenantOwner)], new Dictionary<string, string>(), DateTimeOffset.UtcNow);
        TenantDetail currentDetail = originalDetail with { Name = "Current authorized name", Description = "Current authorized description" };
        Action<ComponentParameterCollectionBuilder<EditTenantMetadataFlow>> originalParameters = child => child
            .Add(x => x.Detail, originalDetail).Add(x => x.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(x => x.Freshness, ReadModelFreshnessState.Current).Add(x => x.Lifecycle, ProjectionLifecycleState.Current)
            .Add(x => x.ProjectionVersion, "tenant-sequence:1");
        IRenderedComponent<CascadingValue<string?>> wrapper = Render<CascadingValue<string?>>(p => p
            .Add(x => x.Name, "CurrentTenantCallerScope").Add(x => x.Value, "caller-a").AddChildContent(originalParameters));
        IRenderedComponent<EditTenantMetadataFlow> cut = wrapper.FindComponent<EditTenantMetadataFlow>();
        cut.Find("[data-testid='tenants-edit-metadata-open']").Click();
        cut.Find("[data-testid='tenants-edit-metadata-name']").Change("Original command intent");
        await cut.Find("form").SubmitAsync();
        TenantUpdateMetadataCommandSnapshot original = cut.Instance.Snapshot;
        original.LastConfirmedCallerScope.ShouldBe("caller-a");
        wrapper.Render(p => p.Add(x => x.Value, (string?)null).AddChildContent<EditTenantMetadataFlow>(child => child
            .Add(x => x.Detail, currentDetail).Add(x => x.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(x => x.Freshness, ReadModelFreshnessState.Current).Add(x => x.Lifecycle, ProjectionLifecycleState.Current)
            .Add(x => x.ProjectionVersion, "tenant-sequence:900")));
        string display = cut.Find("[data-testid='tenants-edit-metadata-confirmed']").TextContent;
        display.ShouldContain(currentDetail.Name);
        display.ShouldContain(currentDetail.Description!);
        display.ShouldNotContain("Caller A");
        cut.Instance.Snapshot.ShouldBe(original);
        Services.GetRequiredService<TenantAggregateCommandAdmissionGate>().IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
    }

    [Theory]
    [InlineData("authorization")]
    [InlineData("freshness")]
    [InlineData("command-surface")]
    public async Task CreateRechecksSuppliedEligibilityAfterAwaitedBaseline(string deniedInput)
    {
        ITenantCommandGateway gateway = RegisterGateway((_, id) => Task.FromResult(TenantCommandSubmissionResult.Accepted(id, "unused")));
        var baseline = new TaskCompletionSource<TenantDetailSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        Services.GetRequiredService<ITenantQueryGateway>().GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>()).Returns(baseline.Task);
        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>();
        cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
        cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
        Task submission = cut.Find("form").SubmitAsync();
        await WaitForAsync(() => Services.GetRequiredService<TenantAggregateCommandAdmissionGate>().IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")));
        cut.Render(p => p.Add(x => x.IsAuthorized, deniedInput is not "authorization")
            .Add(x => x.IsFresh, deniedInput is not "freshness").Add(x => x.IsCommandSurfaceAvailable, deniedInput is not "command-surface"));
        baseline.SetResult(TenantDetailSnapshot.NotFound("tenant.alpha"));
        await submission.WaitAsync(TimeSpan.FromSeconds(5));
        gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name is nameof(ITenantCommandGateway.CreateTenantAsync)).ShouldBe(0);
        Services.GetRequiredService<TenantAggregateCommandAdmissionGate>().IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
        Services.GetRequiredService<TenantCommandAttemptTracker>().HasPendingOwnership("tenant.alpha").ShouldBeFalse();
    }

    private ITenantCommandGateway RegisterGateway(Func<object, string, Task<TenantCommandSubmissionResult>> submit)
    {
        Services.AddLocalization();
        ITenantCommandGateway gateway = Substitute.For<ITenantCommandGateway>();
        gateway.CreateTenantAsync(Arg.Any<CreateTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call => submit(call.ArgAt<CreateTenant>(0), call.ArgAt<string>(1)));
        gateway.AddUserToTenantAsync(Arg.Any<AddUserToTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call => submit(call.ArgAt<AddUserToTenant>(0), call.ArgAt<string>(1)));
        gateway.ChangeUserRoleAsync(Arg.Any<ChangeUserRole>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call => submit(call.ArgAt<ChangeUserRole>(0), call.ArgAt<string>(1)));
        gateway.RemoveUserFromTenantAsync(Arg.Any<RemoveUserFromTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call => submit(call.ArgAt<RemoveUserFromTenant>(0), call.ArgAt<string>(1)));
        gateway.UpdateTenantAsync(Arg.Any<UpdateTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call => submit(call.ArgAt<UpdateTenant>(0), call.ArgAt<string>(1)));
        gateway.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new TenantCommandStatusResult(CommandStatus.Received, HasVerifiedCommandIdentity: true)));
        Services.AddSingleton(gateway);
        Services.AddSingleton(Substitute.For<ITenantQueryGateway>());
        return gateway;
    }

    private (IRenderedComponent<IComponent> Cut, object Component) RenderFlow(string kind, string baseline = "tenant-sequence:1", Func<bool, Task<bool>>? lease = null, EventCallback<bool> activity = default)
    {
        TenantDetail detail = new("tenant.alpha", "Alpha", "Original", TenantStatus.Active,
            [new TenantMember("owner", TenantRole.TenantOwner), new TenantMember("reader", TenantRole.TenantReader)],
            new Dictionary<string, string>(), DateTimeOffset.UtcNow);
        switch (kind)
        {
            case "create":
                IRenderedComponent<CreateTenantFlow> create = Render<CreateTenantFlow>(p => p.Add(x => x.BaselineTenantAbsent, true).Add(x => x.BaselineProjectionVersion, baseline));
                return (create, create.Instance);
            case "add":
                IRenderedComponent<AddTenantMemberFlow> add = Render<AddTenantMemberFlow>(p => p.Add(x => x.Detail, detail).Add(x => x.SurfaceKind, TenantDetailSurfaceKind.Ready).Add(x => x.Freshness, ReadModelFreshnessState.Current).Add(x => x.ProjectionVersion, baseline).Add(x => x.CommandActivityLease, lease).Add(x => x.OnCommandActivityChanged, activity));
                return (add, add.Instance);
            case "change":
                IRenderedComponent<ChangeTenantMemberRoleFlow> change = Render<ChangeTenantMemberRoleFlow>(p => p.Add(x => x.Detail, detail).Add(x => x.Member, detail.Members[1]).Add(x => x.SurfaceKind, TenantDetailSurfaceKind.Ready).Add(x => x.Freshness, ReadModelFreshnessState.Current).Add(x => x.ProjectionVersion, baseline).Add(x => x.CommandActivityLease, lease).Add(x => x.OnCommandActivityChanged, activity));
                return (change, change.Instance);
            case "remove":
                IRenderedComponent<RemoveTenantMemberFlow> remove = Render<RemoveTenantMemberFlow>(p => p.Add(x => x.Detail, detail).Add(x => x.Member, detail.Members[1]).Add(x => x.SurfaceKind, TenantDetailSurfaceKind.Ready).Add(x => x.Freshness, ReadModelFreshnessState.Current).Add(x => x.ProjectionVersion, baseline).Add(x => x.AuditProofCapabilityAvailable, true).Add(x => x.TargetPlatformStandingResolved, true).Add(x => x.CommandActivityLease, lease).Add(x => x.OnCommandActivityChanged, activity));
                return (remove, remove.Instance);
            default:
                IRenderedComponent<EditTenantMetadataFlow> metadata = Render<EditTenantMetadataFlow>(p => p.Add(x => x.Detail, detail).Add(x => x.SurfaceKind, TenantDetailSurfaceKind.Ready).Add(x => x.Freshness, ReadModelFreshnessState.Current).Add(x => x.Lifecycle, ProjectionLifecycleState.Current).Add(x => x.ProjectionVersion, baseline).Add(x => x.ProjectionEvidenceProvider, _ => Task.FromResult(TenantCommandProofFixtures.Proof(detail, baseline))).Add(x => x.CommandActivityLease, lease).Add(x => x.OnCommandActivityChanged, activity));
                return (metadata, metadata.Instance);
        }
    }

    private static void Submit(string kind, IRenderedComponent<IComponent> cut, bool send = true)
    {
        switch (kind)
        {
            case "create":
                cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
                cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
                break;
            case "add":
                cut.Find("[data-testid='tenants-add-member-user-id']").Change("new-reader");
                ChangeRoleSelection(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
                break;
            case "change":
                ChangeRoleSelection(cut, "tenants-change-role-new-role", nameof(TenantRole.TenantContributor));
                break;
            case "remove":
                cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("reader");
                break;
            case "metadata":
                if (cut.FindAll("[data-testid='tenants-edit-metadata-open']").Count > 0)
                {
                    cut.Find("[data-testid='tenants-edit-metadata-open']").Click();
                }
                cut.Find("[data-testid='tenants-edit-metadata-name']").Change("Renamed");
                break;
        }
        if (send)
        {
            cut.Find("form").Submit();
        }
    }

    private static void ChangeRoleSelection(IRenderedComponent<IComponent> cut, string testId, string value)
    {
        Microsoft.FluentUI.AspNetCore.Components.FluentSelect<string, string> select = cut
            .FindComponents<Microsoft.FluentUI.AspNetCore.Components.FluentSelect<string, string>>()
            .Single(rendered => rendered.Instance.AdditionalAttributes?.TryGetValue("data-testid", out object? id) is true
                && Equals(id, testId)).Instance;
        cut.InvokeAsync(() => select.ValueChanged.InvokeAsync(value)).GetAwaiter().GetResult();
    }

    private static object Snapshot(object component)
        => component.GetType().GetProperty("Snapshot", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(component)!;

    private static T? Property<T>(object snapshot, string property)
        => (T?)snapshot.GetType().GetProperty(property)!.GetValue(snapshot);

    private static object? ReadRetained(TenantCommandAttemptTracker tracker, string kind)
        => kind switch
        {
            "create" => tracker.Find<TenantCreateCommandSnapshot>("tenant.alpha"),
            "add" => tracker.Find<TenantAddMemberCommandSnapshot>("tenant.alpha"),
            "change" => tracker.Find<TenantChangeRoleCommandSnapshot>("tenant.alpha"),
            "remove" => tracker.Find<TenantRemoveMemberCommandSnapshot>("tenant.alpha"),
            _ => tracker.Find<TenantUpdateMetadataCommandSnapshot>("tenant.alpha"),
        };

    private static string DispatchName(string kind)
        => kind switch
        {
            "create" => nameof(ITenantCommandGateway.CreateTenantAsync),
            "add" => nameof(ITenantCommandGateway.AddUserToTenantAsync),
            "change" => nameof(ITenantCommandGateway.ChangeUserRoleAsync),
            "remove" => nameof(ITenantCommandGateway.RemoveUserFromTenantAsync),
            _ => nameof(ITenantCommandGateway.UpdateTenantAsync),
        };

    private static async Task WaitForAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }
}
