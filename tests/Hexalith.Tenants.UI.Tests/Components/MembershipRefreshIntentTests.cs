using System.Reflection;
using Bunit;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Members;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using NSubstitute;
using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;
/// <summary>Exercises queued intent against real Fluent flows and genuinely held operations.</summary>
public sealed class MembershipRefreshIntentTests : FluentBunitContext
{
    private IRenderedComponent<IComponent> _cut = null!;
    private IRenderedComponent<CascadingValue<bool>> _host = null!;
    private IComponent _flow = null!;
    private ITenantCommandGateway _gateway = null!;
    private bool _change;
    private int _statusReads;
    private int _reloads;
    private int _evidenceReads;
    private int _activeStatus;
    private int _statusOverlapped;
    private string? _messageId;
    private string _expectedTenant = "tenant.alpha";
    private Func<int, Task<TenantCommandStatusResult>> _status = _ => Task.FromResult(Completed());
    private Func<int, Task> _reload = _ => Task.CompletedTask;
    private Func<int, Task<TenantDetailSnapshot?>> _evidence = _ => Task.FromResult<TenantDetailSnapshot?>(null);
    [Theory]
    [InlineData(false, false, "PSS")]
    [InlineData(false, false, "SSP")]
    [InlineData(false, false, "SPS")]
    [InlineData(false, false, "PPP")]
    [InlineData(false, false, "SSS")]
    [InlineData(false, true, "PSS")]
    [InlineData(false, true, "SSP")]
    [InlineData(false, true, "PPP")]
    [InlineData(false, true, "SSS")]
    [InlineData(true, false, "PSS")]
    [InlineData(true, false, "SSP")]
    [InlineData(true, false, "SPS")]
    [InlineData(true, false, "PPP")]
    [InlineData(true, false, "SSS")]
    [InlineData(true, true, "PSS")]
    [InlineData(true, true, "SSP")]
    [InlineData(true, true, "PPP")]
    [InlineData(true, true, "SSS")]
    public async Task MixedRequestsOrProjectionIntentAndConsumeOneBatch(bool change, bool initialProjection, string order)
    {
        ArgumentNullException.ThrowIfNull(order);
        await StartAsync(change);
        object original = Snapshot();
        var entered = Signal();
        var release = Signal();
        _status = async n =>
        {
            if (n == 2)
            {
                entered.SetResult();
                await release.Task;
            }

            return Completed();
        };
        Task owner = RequestAsync(initialProjection);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        List<Task> queued = await QueueAsync(order);
        queued.Where((_, n) => order[n] == 'P').ShouldAllBe(task => !task.IsCompleted);
        AssertCounts(2, 1, 1);
        release.SetResult();
        await Task.WhenAll(queued.Prepend(owner)).WaitAsync(TimeSpan.FromSeconds(5));
        AssertCounts(3, 1 + (initialProjection ? 1 : 0) + (order.Contains('P') ? 1 : 0), 3);
        State().ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        AssertRetained(original);
    }

    [Theory]
    [InlineData(false, "status")]
    [InlineData(false, "reload")]
    [InlineData(false, "evidence")]
    [InlineData(true, "status")]
    [InlineData(true, "reload")]
    [InlineData(true, "evidence")]
    public async Task RequestsDuringReplaySurviveUntilTheirOwnBatchAndOnlyProjectionConfirms(bool change, string heldStage)
    {
        await StartAsync(change);
        var entered = Signal();
        var release = Signal();
        var replayEntered = Signal();
        var releaseReplay = Signal();
        _status = async n =>
        {
            if (n == 2)
            {
                entered.SetResult();
                await release.Task;
            }

            if (n == 3 && heldStage == "status")
            {
                replayEntered.SetResult();
                await releaseReplay.Task;
            }

            return Completed();
        };
        _reload = async n =>
        {
            if (n == 2 && heldStage == "reload")
            {
                replayEntered.SetResult();
                await releaseReplay.Task;
            }
        };
        _evidence = async n =>
        {
            if (n == 3 && heldStage == "evidence")
            {
                replayEntered.SetResult();
                await releaseReplay.Task;
            }

            return Proof(matching: n == 4);
        };
        Task owner = RequestAsync(false);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        List<Task> first = await QueueAsync("PSS");
        release.SetResult();
        await replayEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        State().ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        List<Task> second = await QueueAsync("SPPS");
        AssertCounts(3, heldStage == "status" ? 1 : 2, heldStage == "evidence" ? 3 : 2);
        releaseReplay.SetResult();
        await Task.WhenAll(first.Concat(second).Prepend(owner)).WaitAsync(TimeSpan.FromSeconds(5));
        AssertCounts(4, 3, 4);
        State().ShouldBe(TenantCommandLifecycleState.Confirmed);
        Gate().IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
        Tracker().HasPendingOwnership("tenant.alpha").ShouldBeFalse();
        await RequestAsync(true);
        AssertCounts(4, 3, 4);
    }

    [Theory]
    [InlineData(false, "status", false)]
    [InlineData(false, "status", true)]
    [InlineData(false, "reload", false)]
    [InlineData(false, "reload", true)]
    [InlineData(false, "evidence", false)]
    [InlineData(false, "evidence", true)]
    [InlineData(true, "status", false)]
    [InlineData(true, "status", true)]
    [InlineData(true, "reload", false)]
    [InlineData(true, "reload", true)]
    [InlineData(true, "evidence", false)]
    [InlineData(true, "evidence", true)]
    public async Task FailedBatchDrainsOnlyExplicitQueuedWorkAndReportsFailure(bool change, string failureStage, bool duringReplay)
    {
        await StartAsync(change);
        object original = Snapshot();
        var entered = Signal();
        var release = Signal();
        var firstEntered = Signal();
        var releaseFirst = Signal();
        int failingCall = duringReplay ? 3 : 2;
        async Task FailAsync()
        {
            entered.SetResult();
            await release.Task;
            throw new InvalidOperationException("held failure");
        }

        _status = async n =>
        {
            if (duringReplay && n == 2)
            {
                firstEntered.SetResult();
                await releaseFirst.Task;
            }

            if (n == failingCall && failureStage == "status")
                await FailAsync();
            return Completed();
        };
        _reload = async n =>
        {
            if (n == 2 && failureStage == "reload")
                await FailAsync();
        };
        _evidence = async n =>
        {
            if (n == failingCall && failureStage == "evidence")
                await FailAsync();
            return Proof(false);
        };
        Task owner = RequestAsync(!duringReplay && failureStage != "status", observeFailure: true);
        Task? firstManual = null;
        if (duringReplay)
        {
            await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            firstManual = RequestAsync(true, observeFailure: true);
            releaseFirst.SetResult();
        }

        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        List<Task> queued = await QueueAsync("PSS", observeFailure: true);
        release.SetResult();
        await Should.ThrowAsync<InvalidOperationException>(() => owner.WaitAsync(TimeSpan.FromSeconds(5)));
        await Should.ThrowAsync<InvalidOperationException>(() => queued[0].WaitAsync(TimeSpan.FromSeconds(5)));
        if (firstManual is not null)
            await Should.ThrowAsync<InvalidOperationException>(() => firstManual.WaitAsync(TimeSpan.FromSeconds(5)));
        await Task.WhenAll(queued.Skip(1));
        AssertCounts(duringReplay ? 4 : 3, failureStage == "status" ? 2 : 3, (failureStage == "evidence" ? 3 : 2) + (duringReplay ? 1 : 0));
        State().ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        AssertRetained(original);
        // Failure does not manufacture retries, and the next explicit batch can still run.
        await RequestAsync(true);
        AssertCounts(duringReplay ? 5 : 4, failureStage == "status" ? 3 : 4, (failureStage == "evidence" ? 4 : 3) + (duringReplay ? 1 : 0));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnknownStatusDoesNotEraseQueuedManualRefreshOrConfirmMatchingEvidence(bool change)
    {
        await StartAsync(change, completed: false);
        var entered = Signal();
        var release = Signal();
        _status = async n =>
        {
            if (n == 2)
            {
                entered.SetResult();
                await release.Task;
                return TenantCommandStatusResult.Unknown("unavailable");
            }

            return new TenantCommandStatusResult(CommandStatus.Received, HasVerifiedCommandIdentity: true);
        };
        _evidence = _ => Task.FromResult(Proof(true));
        Task owner = RequestAsync(false);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task manual = RequestAsync(true);
        release.SetResult();
        await Task.WhenAll(owner, manual).WaitAsync(TimeSpan.FromSeconds(5));
        AssertCounts(3, 2, 3);
        State().ShouldBe(TenantCommandLifecycleState.Accepted);
        Gate().IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task TerminalEvidenceRetiresQueuedBatchWithoutRedundantWork(bool change, bool rejected)
    {
        await StartAsync(change);
        var entered = Signal();
        var release = Signal();
        _status = async _ =>
        {
            entered.SetResult();
            await release.Task;
            return rejected ? new TenantCommandStatusResult(CommandStatus.Rejected, HasVerifiedCommandIdentity: true) : Completed();
        };
        _evidence = _ => Task.FromResult(Proof(true));
        Task owner = RequestAsync(false);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task manual = RequestAsync(true);
        release.SetResult();
        await Task.WhenAll(owner, manual).WaitAsync(TimeSpan.FromSeconds(5));
        State().ShouldBe(rejected ? TenantCommandLifecycleState.Rejected : TenantCommandLifecycleState.Confirmed);
        AssertCounts(2, 1, 2);
        Gate().IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false, "status", false)]
    [InlineData(false, "status", true)]
    [InlineData(false, "reload", false)]
    [InlineData(false, "reload", true)]
    [InlineData(false, "evidence", false)]
    [InlineData(false, "evidence", true)]
    [InlineData(true, "status", false)]
    [InlineData(true, "status", true)]
    [InlineData(true, "reload", false)]
    [InlineData(true, "reload", true)]
    [InlineData(true, "evidence", false)]
    [InlineData(true, "evidence", true)]
    public async Task DisposalRetiresQueuedIntentAndLateOutcomesCannotMutateReplacement(bool change, string stage, bool fault)
    {
        await StartAsync(change);
        var entered = Signal();
        var release = Signal();
        async Task HoldAsync()
        {
            entered.SetResult();
            await release.Task;
            if (fault)
                throw new InvalidOperationException("late failure");
        }

        _status = async n =>
        {
            if (n == 2 && stage == "status")
                await HoldAsync();
            return Completed();
        };
        _reload = async n =>
        {
            if (n == 2 && stage == "reload")
                await HoldAsync();
        };
        _evidence = async n =>
        {
            if (n == 2 && stage == "evidence")
                await HoldAsync();
            return Proof(true);
        };
        Task owner = RequestAsync(stage != "status", observeFailure: true);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task manual = RequestAsync(true, observeFailure: true);
        IRenderedComponent<CascadingValue<bool>> oldHost = _host;
        await oldHost.InvokeAsync(() => oldHost.Render(p => p.Add(x => x.Value, true).Add(x => x.ChildContent, (RenderFragment)(_ =>
        {
        }))));
        oldHost.FindAll("[data-testid='tenants-add-member-flow'],[data-testid='tenants-change-role-flow']").ShouldBeEmpty();
        RenderFlow(change);
        object replacement = Snapshot();
        Gate().IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
        release.SetResult();
        if (fault)
        {
            await Should.ThrowAsync<InvalidOperationException>(() => owner.WaitAsync(TimeSpan.FromSeconds(5)));
            await Should.ThrowAsync<InvalidOperationException>(() => manual.WaitAsync(TimeSpan.FromSeconds(5)));
        }
        else
            await Task.WhenAll(owner, manual).WaitAsync(TimeSpan.FromSeconds(5));
        AssertCounts(2, stage == "status" ? 1 : 2, stage == "evidence" ? 2 : 1);
        AssertIdentity(replacement);
        if (stage == "evidence" && !fault)
        {
            // Qualified evidence may finish the retained attempt after unmount; its replacement adopts it.
            State().ShouldBe(TenantCommandLifecycleState.Confirmed);
            Tracker().HasPendingOwnership("tenant.alpha").ShouldBeFalse();
            Gate().IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
            return;
        }

        AssertRetained(replacement);
        State().ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        // The replacement owns recovery of the same identity; the disposed owner cannot drain for it.
        _status = _ => Task.FromResult(Completed());
        _reload = _ => Task.CompletedTask;
        _evidence = _ => Task.FromResult(Proof(true));
        await RequestAsync(true);
        State().ShouldBe(TenantCommandLifecycleState.Confirmed);
        Gate().IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailureWithoutPendingWorkRetiresGateAndDoesNotRetry(bool change)
    {
        await StartAsync(change);
        _status = _ => Task.FromException<TenantCommandStatusResult>(new InvalidOperationException("status failure"));
        await Should.ThrowAsync<InvalidOperationException>(() => RequestAsync(false).WaitAsync(TimeSpan.FromSeconds(5)));
        AssertCounts(2, 1, 1);
        _status = _ => Task.FromResult(Completed());
        await RequestAsync(true);
        AssertCounts(3, 2, 2);
        State().ShouldBe(TenantCommandLifecycleState.ProjectionPending);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CurrentGenerationRequestSurvivesOldOwnerAndKeepsBothAggregateIdentities(bool change, bool rejectOld)
    {
        await StartAsync(change);
        string alphaMessage = _messageId.ShouldNotBeNull();
        var entered = Signal();
        var release = Signal();
        _status = async n =>
        {
            if (n == 2)
            {
                entered.SetResult();
                await release.Task;
                if (rejectOld)
                    return new TenantCommandStatusResult(CommandStatus.Rejected, HasVerifiedCommandIdentity: true);
            }

            return Completed();
        };
        Task oldOwner = RequestAsync(false);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task obsoleteManual = RequestAsync(true);
        _expectedTenant = "tenant.beta";
        await _cut.InvokeAsync(() => _flow.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Detail"] = Original("tenant.beta", includeTarget: change) })));
        _evidence = _ => Task.FromResult(TenantCommandProofFixtures.Proof(Original("tenant.beta"), "tenant-sequence:1"));
        Task newSubmit = _cut.InvokeAsync(() => (Task)_flow.GetType().GetMethod("SubmitAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_flow, null)!);
        _cut.WaitForAssertion(() => DispatchCount().ShouldBe(2));
        string betaMessage = _messageId.ShouldNotBeNull();
        betaMessage.ShouldNotBe(alphaMessage);
        newSubmit.IsCompleted.ShouldBeFalse();
        release.SetResult();
        await Task.WhenAll(oldOwner, obsoleteManual, newSubmit).WaitAsync(TimeSpan.FromSeconds(5));
        _statusReads.ShouldBe(3);
        _reloads.ShouldBe(2);
        _evidenceReads.ShouldBe(2);
        _statusOverlapped.ShouldBe(0);
        DispatchCount().ShouldBe(2);
        State().ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        Snapshot().GetType().GetProperty("MessageId")!.GetValue(Snapshot()).ShouldBe(betaMessage);
        Gate().IsLocked(TenantCommandAggregateLock.ForTenant("tenant.beta")).ShouldBeTrue();
        Gate().IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBe(!rejectOld);
        TenantCommandTrackingHandle[] handles = _gateway.ReceivedCalls().Where(call => call.GetMethodInfo().Name == nameof(ITenantCommandGateway.GetStatusAsync)).Select(call => (TenantCommandTrackingHandle)call.GetArguments()[0]!).ToArray();
        handles.Select(handle => (handle.MessageId, handle.AggregateId)).ShouldBe([(alphaMessage, "tenant.alpha"), (alphaMessage, "tenant.alpha"), (betaMessage, "tenant.beta")]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentArrivalAndIdleHandoffKeepEveryManualIntentAndSuccessorOwner(bool change)
    {
        await StartAsync(change);
        for (int round = 0; round < 16; round++)
        {
            int firstRead = 2 + round * 3;
            var firstEntered = Signal();
            var releaseFirst = Signal();
            var secondEntered = Signal();
            var releaseSecond = Signal();
            _status = async n =>
            {
                if (n == firstRead)
                {
                    firstEntered.SetResult();
                    await releaseFirst.Task;
                }

                if (n == firstRead + 1)
                {
                    secondEntered.SetResult();
                    await releaseSecond.Task;
                }

                return Completed();
            };
            Task owner = RequestAsync(false);
            await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            // The arrival races the first owner's drain/idle handoff on another execution context.
            Task manual = Task.Run(() => RequestAsync(true, observeFailure: true));
            releaseFirst.SetResult();
            await secondEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Task successorManual = RequestAsync(true, observeFailure: true);
            await RequestAsync(false);
            _statusReads.ShouldBe(firstRead + 1);
            releaseSecond.SetResult();
            await Task.WhenAll(owner, manual, successorManual).WaitAsync(TimeSpan.FromSeconds(5));
            AssertCounts(1 + (round + 1) * 3, 1 + (round + 1) * 2, 1 + (round + 1) * 3);
        }
    }

    private async Task StartAsync(bool change, bool completed = true)
    {
        _change = change;
        _evidence = _ => Task.FromResult(Proof(false));
        if (!completed)
            _status = _ => Task.FromResult(new TenantCommandStatusResult(CommandStatus.Received, HasVerifiedCommandIdentity: true));
        Services.AddLocalization();
        RenderTree.Add<CascadingValue<string>>(p => p.Add(x => x.Name, "CurrentTenantCallerScope").Add(x => x.Value, "test-caller"));
        _gateway = Substitute.For<ITenantCommandGateway>();
        _gateway.AddUserToTenantAsync(Arg.Any<AddUserToTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call => Accept(call.ArgAt<string>(1), call.ArgAt<CancellationToken>(2)));
        _gateway.ChangeUserRoleAsync(Arg.Any<ChangeUserRole>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call => Accept(call.ArgAt<string>(1), call.ArgAt<CancellationToken>(2)));
        _gateway.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            TenantCommandTrackingHandle handle = call.ArgAt<TenantCommandTrackingHandle>(0);
            handle.MessageId.ShouldBe(_messageId);
            handle.CorrelationId.ShouldBe("correlation-original");
            handle.AggregateId.ShouldBe(_expectedTenant);
            handle.ExpectedDomain.ShouldBe(TenantIdentity.Domain);
            call.ArgAt<CancellationToken>(1).ShouldBe(CancellationToken.None);
            int active = Interlocked.Increment(ref _activeStatus);
            if (active > 1)
                Interlocked.Exchange(ref _statusOverlapped, 1);
            try
            {
                return await _status(Interlocked.Increment(ref _statusReads));
            }
            finally
            {
                Interlocked.Decrement(ref _activeStatus);
            }
        });
        Services.AddSingleton(_gateway);
        Services.AddSingleton(Substitute.For<ITenantQueryGateway>());
        RenderFlow(change);
        if (!change)
            _cut.Find("[data-testid='tenants-add-member-user-id']").Change("target");
        FluentSelectInterop.ChangeFluentSelect(_cut, change ? "tenants-change-role-new-role" : "tenants-add-member-role", nameof(TenantRole.TenantContributor));
        await _cut.InvokeAsync(() => (Task)_flow.GetType().GetMethod("SubmitAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_flow, null)!);
        AssertCounts(1, 1, 1);
        State().ShouldBe(completed ? TenantCommandLifecycleState.ProjectionPending : TenantCommandLifecycleState.Accepted);
    }

    private Task<TenantCommandSubmissionResult> Accept(string message, CancellationToken token)
    {
        token.ShouldBe(CancellationToken.None);
        _messageId = message;
        return Task.FromResult(TenantCommandSubmissionResult.Accepted(message, "correlation-original"));
    }

    private void RenderFlow(bool change)
    {
        IComponent? renderingFlow = null;
        var values = new Dictionary<string, object>
        {
            ["IsAuthorized"] = true,
            ["AuthorizationReflection"] = TenantLifecycleAuthorizationReflectionState.Authorized,
            ["AuthorizationReflectionProvider"] = (Func<CancellationToken, Task<TenantLifecycleAuthorizationReflectionState>>)(_ => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized)),
            ["Detail"] = Original(includeTarget: change),
            ["SurfaceKind"] = TenantDetailSurfaceKind.Ready,
            ["Freshness"] = ReadModelFreshnessState.Current,
            ["ProjectionVersion"] = "tenant-sequence:1",
            ["OnProjectionRefreshRequested"] = EventCallback.Factory.Create(this, async () =>
            {
                await _reload(Interlocked.Increment(ref _reloads));
                // Parent read completion forwarding back into its child must not poll again.
                await NudgeAsync(renderingFlow.ShouldNotBeNull());
            }),
        };
        if (change)
        {
            values["Member"] = Original().Members[1];
            values["ProjectionEvidenceProvider"] = (Func<ChangeUserRole, Task<TenantDetailSnapshot?>>)(_ => _evidence(Interlocked.Increment(ref _evidenceReads)));
        }
        else
        {
            values["ProjectionEvidenceProvider"] = (Func<AddUserToTenant, Task<TenantDetailSnapshot?>>)(_ => _evidence(Interlocked.Increment(ref _evidenceReads)));
        }

        RenderFragment fragment = builder =>
        {
            builder.OpenComponent(0, change ? typeof(ChangeTenantMemberRoleFlow) : typeof(AddTenantMemberFlow));
            builder.AddMultipleAttributes(1, values);
            builder.CloseComponent();
        };
        _host = Render<CascadingValue<bool>>(p => p.Add(x => x.Value, true).Add(x => x.ChildContent, fragment));
        _cut = change ? _host.FindComponent<ChangeTenantMemberRoleFlow>() : _host.FindComponent<AddTenantMemberFlow>();
        _flow = _cut.Instance;
        renderingFlow = _flow;
    }

    private Task RequestAsync(bool projection, bool observeFailure = false)
    {
        if (!projection)
            return _cut.InvokeAsync(() => NudgeAsync(_flow));
        string id = _change ? "tenants-change-role-refresh" : "tenants-add-member-refresh";
        if (!observeFailure)
            return _cut.Find($"[data-testid='{id}']").ClickAsync(new MouseEventArgs());
        // FluentButton handles DOM-click faults itself. Invoke its actual callback to observe flow failures.
        FluentButton button = _cut.FindComponents<FluentButton>().Single(c => c.Instance.AdditionalAttributes?.TryGetValue("data-testid", out object? value) == true && Equals(value, id)).Instance;
        return _cut.InvokeAsync(() => button.OnClick.InvokeAsync(new MouseEventArgs()));
    }

    private static Task NudgeAsync(IComponent flow) => flow switch
    {
        AddTenantMemberFlow add => add.HandleAuthoritativeRefreshNudgeAsync(),
        ChangeTenantMemberRoleFlow change => change.HandleAuthoritativeRefreshNudgeAsync(),
        _ => throw new InvalidOperationException(),
    };
    private async Task<List<Task>> QueueAsync(string order, bool observeFailure = false)
    {
        List<Task> tasks = [];
        foreach (char request in order)
        {
            Task task = RequestAsync(request == 'P', observeFailure);
            tasks.Add(task);
            if (request == 'S')
                await task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        return tasks;
    }

    private void AssertCounts(int statuses, int reloads, int evidence)
    {
        _statusReads.ShouldBe(statuses);
        _reloads.ShouldBe(reloads);
        _evidenceReads.ShouldBe(evidence);
        _statusOverlapped.ShouldBe(0);
        DispatchCount().ShouldBe(1);
    }

    private int DispatchCount() => _gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name is nameof(ITenantCommandGateway.AddUserToTenantAsync) or nameof(ITenantCommandGateway.ChangeUserRoleAsync));
    private void AssertRetained(object original)
    {
        AssertIdentity(original);
        Tracker().HasPendingOwnership("tenant.alpha").ShouldBeTrue();
        Gate().IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
        Gate().TryAcquire(TenantCommandAggregateLock.ForTenant("tenant.beta"), this).ShouldBeTrue();
    }

    private void AssertIdentity(object original)
    {
        foreach (string property in new[]
        {
            "MessageId",
            "CorrelationId",
            "Intent",
            "BaselineProjectionVersion"
        }

        )
            Snapshot().GetType().GetProperty(property)!.GetValue(Snapshot()).ShouldBe(original.GetType().GetProperty(property)!.GetValue(original));
    }

    private object Snapshot() => _flow.GetType().GetProperty("Snapshot", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_flow)!;
    private TenantCommandLifecycleState State() => (TenantCommandLifecycleState)Snapshot().GetType().GetProperty("State")!.GetValue(Snapshot())!;
    private TenantAggregateCommandAdmissionGate Gate() => Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
    private TenantCommandAttemptTracker Tracker() => Services.GetRequiredService<TenantCommandAttemptTracker>();
    private TenantDetailSnapshot? Proof(bool matching) => TenantCommandProofFixtures.Proof(matching ? Original()with { Members = [new("owner", TenantRole.TenantOwner), new("target", TenantRole.TenantContributor)] } : Original(includeTarget: _change), matching ? "tenant-sequence:2" : "tenant-sequence:1");
    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static TenantCommandStatusResult Completed() => new(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true);
    private static TenantDetail Original(string tenant = "tenant.alpha", bool includeTarget = true) => new(tenant, "Alpha", "Description", TenantStatus.Active, includeTarget ? [new("owner", TenantRole.TenantOwner), new("target", TenantRole.TenantReader)] : [new("owner", TenantRole.TenantOwner)], new Dictionary<string, string>(), DateTimeOffset.UnixEpoch);
}
