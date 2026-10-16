using System.Globalization;
using System.Reflection;

using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Members;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>Exercises the retained membership deadline through real Fluent controls and held operations.</summary>
public sealed class MembershipProjectionWaitTests : FluentBunitContext
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);
    private readonly ManualTimeProvider _clock = new();
    private readonly List<(string Method, object Intent, string MessageId)> _dispatches = [];
    private readonly List<CancellationToken> _authorityTokens = [];
    private IRenderedComponent<CascadingValue<bool>>? _host;
    private IRenderedComponent<IComponent> _cut = null!;
    private IComponent _flow = null!;
    private ITenantCommandGateway _gateway = null!;
    private string _kind = string.Empty;
    private int _statusReads;
    private int _parentReads;
    private int _evidenceReads;
    private int _auditReads;
    private int _activeStatus;
    private int _statusOverlap;
    private Func<int, Task<TenantCommandStatusResult>> _status = _ => Task.FromResult(Completed());
    private Func<int, Task> _parent = _ => Task.CompletedTask;
    private Func<int, object, Task<TenantDetailSnapshot?>> _evidence = (_, intent) => Task.FromResult(Proof(intent, "unchanged"));
    private Func<object, string, Task<TenantCommandSubmissionResult>> _delivery = (_, message) => Task.FromResult(TenantCommandSubmissionResult.Accepted(message, "correlation-original"));

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public async Task ExactBoundaryExpiresWithoutAnyIoCancellationOrAdmissionRelease(string kind)
    {
        await StartAsync(kind);
        object original = Snapshot();
        await AdvanceAsync(Bound - TimeSpan.FromTicks(1));
        State().ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        Expired(Snapshot()).ShouldBeFalse();
        await AdvanceAsync(TimeSpan.FromTicks(1));
        State().ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        Expired(Snapshot()).ShouldBeTrue();
        Property<string>(Snapshot(), "SafeMessageKey").ShouldBe("Tenants.Membership.ProjectionWait.Expired");
        AssertCounts(1, 1, 1);
        AssertUnresolved(original);
        Property<TenantCommandAuditState>(Snapshot(), "AuditState").ShouldBe(Property<TenantCommandAuditState>(original, "AuditState"));
        _cut.FindAll($"[data-testid='{Prefix}-continue-read-only']").Count.ShouldBe(1);
        _cut.Markup.ShouldContain("Retry status lookup, continue read-only, or contact support");
        _cut.Markup.ShouldContain("This does not mean the command failed.");
        _cut.Markup.ShouldNotContain(Property<string>(original, "MessageId")!);
        _cut.Markup.ShouldNotContain("correlation-original");
        _authorityTokens.ShouldAllBe(token => !token.IsCancellationRequested);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public async Task FrenchExpiryAnnouncesUncertaintyAndKeepsBothRecoveryActionsReachable(string kind)
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            await StartAsync(kind);
            object original = Snapshot();
            await AdvanceAsync(Bound);
            State().ShouldBe(TenantCommandLifecycleState.UnableToVerify);
            string guidance = _cut.Find($"[data-testid='{Prefix}-safe-message']").TextContent;
            guidance.ShouldContain("Réessayez la recherche du statut, continuez en lecture seule ou contactez le support");
            guidance.ShouldContain("Cela ne signifie pas que la commande a échoué.");
            guidance.ShouldNotContain("Retry status lookup");
            _cut.Find($"[data-testid='{Prefix}-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
            _cut.Find($"[data-testid='{Prefix}-live-region']").GetAttribute("aria-atomic").ShouldBe("true");
            _cut.Markup.ShouldNotContain(Property<string>(original, "MessageId")!);
            _cut.Markup.ShouldNotContain("correlation-original");
            await RequestAsync(projection: true);
            State().ShouldBe(TenantCommandLifecycleState.UnableToVerify);
            AssertCounts(2, 2, 2);
            AssertUnresolved(original);
            await ClickAsync($"{Prefix}-continue-read-only");
            State().ShouldBe(TenantCommandLifecycleState.Idle);
            AssertIdentity(original, Retained("tenant.alpha"));
            AssertPendingAdmission("tenant.alpha");
            AssertCounts(2, 2, 2);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public async Task ExpiredMembershipWaitEndsAtProjectionConfirmationWhileRemovalAuditKeepsItsOwnLifetime()
    {
        await StartAsync("remove");
        object original = Snapshot();
        await AdvanceAsync(Bound);
        var entered = Signal();
        var release = Signal();
        CancellationToken auditToken = default;
        ITenantQueryGateway queries = Services.GetRequiredService<ITenantQueryGateway>();
        queries.GetTenantAuditAsync(Arg.Any<TenantAuditRequest>(), Arg.Any<TenantAuditSnapshot?>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            Interlocked.Increment(ref _auditReads);
            auditToken = call.ArgAt<CancellationToken>(2);
            entered.SetResult();
            await release.Task;
            return TenantAuditSnapshot.Unavailable(call.ArgAt<TenantAuditRequest>(0));
        });
        _evidence = (_, intent) => Task.FromResult(Proof(intent, "qualified"));
        Task refresh = RequestAsync(projection: true);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        TenantRemoveMemberCommandSnapshot confirmed = (TenantRemoveMemberCommandSnapshot)Snapshot();
        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.CommittedEventSequence.ShouldBe(2);
        confirmed.ProjectionProofCallerScope.ShouldBe("test-caller");
        confirmed.AuditState.ShouldBe(TenantCommandAuditStates.AwaitingMatchedProof);
        AssertIdentity(original, confirmed);
        _clock.PendingTimerCount.ShouldBe(0);
        auditToken.CanBeCanceled.ShouldBeTrue();
        auditToken.IsCancellationRequested.ShouldBeFalse();
        AssertCounts(2, 2, 2);
        _auditReads.ShouldBe(1);
        AssertTerminalAdmission();
        await AdvanceAsync(Bound * 2);
        State().ShouldBe(TenantCommandLifecycleState.Confirmed);
        ((TenantRemoveMemberCommandSnapshot)Snapshot()).AuditState.ShouldBe(TenantCommandAuditStates.AwaitingMatchedProof);
        auditToken.IsCancellationRequested.ShouldBeFalse();
        _clock.PendingTimerCount.ShouldBe(0);
        AssertCounts(2, 2, 2);
        _auditReads.ShouldBe(1);
        refresh.IsCompleted.ShouldBeFalse();
        release.SetResult();
        await refresh.WaitAsync(TimeSpan.FromSeconds(5));
        TenantRemoveMemberCommandSnapshot unavailableAudit = (TenantRemoveMemberCommandSnapshot)Snapshot();
        unavailableAudit.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        unavailableAudit.AuditState.ShouldBe(TenantCommandAuditState.AuditUnavailable);
        unavailableAudit.CommittedEventSequence.ShouldBe(2);
        unavailableAudit.ProjectionProofCallerScope.ShouldBe("test-caller");
        AssertIdentity(original, unavailableAudit);
        AssertCounts(2, 2, 2);
        _auditReads.ShouldBe(1);
        AssertTerminalAdmission();
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public async Task MixedCoalescedRequestsPreserveTheOriginalDeadlineAndProjectionIntent(string kind)
    {
        await StartAsync(kind);
        object original = Snapshot();
        await AdvanceAsync(Bound / 2);
        var entered = Signal();
        var release = Signal();
        _status = async read =>
        {
            if (read == 2)
            {
                entered.SetResult();
                await release.Task;
            }
            return Completed();
        };
        Task owner = RequestAsync(projection: false);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task manual = RequestAsync(projection: true);
        await RequestAsync(projection: false);
        await RequestAsync(projection: false);
        AssertCounts(2, 1, 1);
        await AdvanceAsync(Bound / 2);
        State().ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        manual.IsCompleted.ShouldBeFalse();
        AssertUnresolved(original);
        release.SetResult();
        await Task.WhenAll(owner, manual).WaitAsync(TimeSpan.FromSeconds(5));
        AssertCounts(3, 2, 3);
        State().ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        Expired(Snapshot()).ShouldBeTrue();
        await RequestAsync(projection: true);
        AssertCounts(4, 3, 4);
        State().ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        AssertUnresolved(original);
    }

    [Theory]
    [InlineData("add", "status", false)]
    [InlineData("add", "status", true)]
    [InlineData("add", "parent", false)]
    [InlineData("add", "parent", true)]
    [InlineData("add", "evidence", false)]
    [InlineData("add", "evidence", true)]
    [InlineData("change", "status", false)]
    [InlineData("change", "status", true)]
    [InlineData("change", "parent", false)]
    [InlineData("change", "parent", true)]
    [InlineData("change", "evidence", false)]
    [InlineData("change", "evidence", true)]
    [InlineData("remove", "status", false)]
    [InlineData("remove", "status", true)]
    [InlineData("remove", "parent", false)]
    [InlineData("remove", "parent", true)]
    [InlineData("remove", "evidence", false)]
    [InlineData("remove", "evidence", true)]
    public async Task HeldQualifyingEvidenceConfirmsBeforeOrAfterExpiry(string kind, string heldStage, bool expire)
    {
        await StartAsync(kind);
        object original = Snapshot();
        var entered = Signal();
        var release = Signal();
        _status = async _ =>
        {
            if (heldStage == "status")
            {
                entered.SetResult();
                await release.Task;
            }
            return Completed();
        };
        _parent = async _ =>
        {
            if (heldStage == "parent")
            {
                entered.SetResult();
                await release.Task;
            }
        };
        _evidence = async (_, intent) =>
        {
            if (heldStage == "evidence")
            {
                entered.SetResult();
                await release.Task;
            }
            return Proof(intent, "qualified");
        };
        Task refresh = RequestAsync(projection: true);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await AdvanceAsync(expire ? Bound : Bound - TimeSpan.FromTicks(1));
        State().ShouldBe(expire ? TenantCommandLifecycleState.UnableToVerify : TenantCommandLifecycleState.ProjectionPending);
        AssertUnresolved(original);
        release.SetResult();
        await refresh.WaitAsync(TimeSpan.FromSeconds(5));
        State().ShouldBe(TenantCommandLifecycleState.Confirmed);
        AssertIdentity(original, Snapshot());
        AssertCounts(2, 2, 2);
        _auditReads.ShouldBe(kind == "remove" ? 1 : 0);
        AssertTerminalAdmission();
        _clock.PendingTimerCount.ShouldBe(0);
        await AdvanceAsync(Bound * 2);
        State().ShouldBe(TenantCommandLifecycleState.Confirmed);
        AssertCounts(2, 2, 2);
    }

    [Theory]
    [InlineData("add", "unavailable")]
    [InlineData("add", "wrong-tenant")]
    [InlineData("add", "wrong-postcondition")]
    [InlineData("add", "unchanged")]
    [InlineData("add", "regressed")]
    [InlineData("add", "stale")]
    [InlineData("change", "unavailable")]
    [InlineData("change", "wrong-tenant")]
    [InlineData("change", "wrong-postcondition")]
    [InlineData("change", "unchanged")]
    [InlineData("change", "regressed")]
    [InlineData("change", "stale")]
    [InlineData("remove", "unavailable")]
    [InlineData("remove", "wrong-tenant")]
    [InlineData("remove", "wrong-postcondition")]
    [InlineData("remove", "unchanged")]
    [InlineData("remove", "regressed")]
    [InlineData("remove", "stale")]
    public async Task UnqualifiedRecoveryNeverRenewsPendingOrClaimsConfirmation(string kind, string proofKind)
    {
        await StartAsync(kind);
        object original = Snapshot();
        await AdvanceAsync(Bound);
        _evidence = (_, intent) => Task.FromResult(Proof(intent, proofKind));
        await RequestAsync(projection: true);
        State().ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        Expired(Snapshot()).ShouldBeTrue();
        AssertCounts(2, 2, 2);
        AssertUnresolved(original);
        _auditReads.ShouldBe(0);
        _clock.PendingTimerCount.ShouldBe(0);
        _evidence = (_, intent) => Task.FromResult(Proof(intent, "qualified"));
        await RequestAsync(projection: true);
        State().ShouldBe(TenantCommandLifecycleState.Confirmed);
        AssertCounts(3, 3, 3);
        AssertIdentity(original, Snapshot());
        AssertTerminalAdmission();
    }

    [Theory]
    [InlineData("add", "unknown")]
    [InlineData("add", "pending")]
    [InlineData("add", "retryable")]
    [InlineData("add", "throw")]
    [InlineData("change", "unknown")]
    [InlineData("change", "pending")]
    [InlineData("change", "retryable")]
    [InlineData("change", "throw")]
    [InlineData("remove", "unknown")]
    [InlineData("remove", "pending")]
    [InlineData("remove", "retryable")]
    [InlineData("remove", "throw")]
    public async Task StatusFailureConsumesElapsedTimeAndExplicitRecoveryUsesTheSameAttempt(string kind, string failure)
    {
        await StartAsync(kind);
        object original = Snapshot();
        await AdvanceAsync(Bound / 2);
        _status = _ => failure switch
        {
            "unknown" => Task.FromResult(TenantCommandStatusResult.Unknown("Status is unavailable.")),
            "pending" => Task.FromResult(TenantCommandStatusResult.Pending("Status is pending.")),
            "retryable" => Task.FromResult(TenantCommandStatusResult.RetryableFailure("Status is unavailable.")),
            _ => Task.FromException<TenantCommandStatusResult>(new InvalidOperationException("Held status failed.")),
        };
        if (failure == "throw")
        {
            await Should.ThrowAsync<InvalidOperationException>(() => RequestAsync(projection: true));
        }
        else
        {
            await RequestAsync(projection: true);
        }
        await AdvanceAsync(Bound / 2);
        State().ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        if (failure != "unknown")
        {
            Expired(Snapshot()).ShouldBeTrue();
        }
        AssertCounts(2, failure == "throw" ? 1 : 2, failure == "throw" ? 1 : 2);
        AssertUnresolved(original);
        _status = _ => Task.FromResult(Completed());
        _evidence = (_, intent) => Task.FromResult(Proof(intent, "qualified"));
        await RequestAsync(projection: true);
        State().ShouldBe(TenantCommandLifecycleState.Confirmed);
        AssertCounts(3, failure == "throw" ? 2 : 3, failure == "throw" ? 2 : 3);
        AssertIdentity(original, Snapshot());
        AssertTerminalAdmission();
    }

    [Theory]
    [InlineData("add", "pending", true)]
    [InlineData("add", "retryable", true)]
    [InlineData("add", "unknown", false)]
    [InlineData("add", "unverified", false)]
    [InlineData("add", "malformed", false)]
    [InlineData("change", "pending", true)]
    [InlineData("change", "retryable", true)]
    [InlineData("change", "unknown", false)]
    [InlineData("change", "unverified", false)]
    [InlineData("change", "malformed", false)]
    [InlineData("remove", "pending", true)]
    [InlineData("remove", "retryable", true)]
    [InlineData("remove", "unknown", false)]
    [InlineData("remove", "unverified", false)]
    [InlineData("remove", "malformed", false)]
    public async Task PostExpiryTransientStatusAllowsQualifiedProofWhileUnverifiedStatusRemainsABarrier(string kind, string statusKind, bool canConfirm)
    {
        await StartAsync(kind);
        object original = Snapshot();
        await AdvanceAsync(Bound);
        _status = _ => Task.FromResult(statusKind switch
        {
            "pending" => TenantCommandStatusResult.Pending("Status is pending."),
            "retryable" => TenantCommandStatusResult.RetryableFailure("Status is temporarily unavailable."),
            "unknown" => TenantCommandStatusResult.Unknown("Status could not be verified."),
            "unverified" => new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
            _ => new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1, IsPending: true, HasVerifiedCommandIdentity: true),
        });
        _evidence = (_, intent) => Task.FromResult(Proof(intent, "qualified"));
        await RequestAsync(projection: true);
        State().ShouldBe(canConfirm ? TenantCommandLifecycleState.Confirmed : TenantCommandLifecycleState.UnableToVerify);
        AssertCounts(2, 2, 2);
        AssertIdentity(original, Snapshot());
        _auditReads.ShouldBe(canConfirm && kind == "remove" ? 1 : 0);
        if (canConfirm)
        {
            AssertTerminalAdmission();
        }
        else
        {
            AssertUnresolved(original);
            _status = _ => Task.FromResult(Completed());
            await RequestAsync(projection: true);
            State().ShouldBe(TenantCommandLifecycleState.Confirmed);
            AssertCounts(3, 3, 3);
            AssertIdentity(original, Snapshot());
            AssertTerminalAdmission();
        }
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public async Task ContinueReadOnlyRetainsExpiredOwnershipAndAcceptsLateTerminalStatus(string kind)
    {
        await StartAsync(kind);
        object original = Snapshot();
        await AdvanceAsync(Bound);
        var entered = Signal();
        var release = Signal();
        _status = async _ =>
        {
            entered.SetResult();
            await release.Task;
            return new(CommandStatus.Rejected, HasVerifiedCommandIdentity: true);
        };
        Task refresh = RequestAsync(projection: true);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await ClickAsync($"{Prefix}-continue-read-only");
        State().ShouldBe(TenantCommandLifecycleState.Idle);
        AssertIdentity(original, Retained("tenant.alpha"));
        Expired(Retained("tenant.alpha")).ShouldBeTrue();
        AssertPendingAdmission("tenant.alpha");
        await AdvanceAsync(Bound);
        AssertCounts(2, 1, 1);
        release.SetResult();
        await refresh.WaitAsync(TimeSpan.FromSeconds(5));
        State().ShouldBe(TenantCommandLifecycleState.Idle);
        Property<TenantCommandLifecycleState>(Retained("tenant.alpha"), "State").ShouldBe(TenantCommandLifecycleState.Rejected);
        AssertCounts(2, 1, 1);
        AssertTerminalAdmission();
        await UnmountAsync();
        MountFlow();
        State().ShouldBe(TenantCommandLifecycleState.Rejected);
        AssertIdentity(original, Snapshot());
    }

    [Theory]
    [InlineData("add", "parent", false)]
    [InlineData("add", "evidence", false)]
    [InlineData("add", "evidence", true)]
    [InlineData("change", "parent", false)]
    [InlineData("change", "evidence", false)]
    [InlineData("change", "evidence", true)]
    [InlineData("remove", "parent", false)]
    [InlineData("remove", "evidence", false)]
    [InlineData("remove", "evidence", true)]
    public async Task ContinueReadOnlyIsReachableDuringTheInitialHeldProjectionReconciliation(string kind, string heldStage, bool qualifyingLateProof)
    {
        var entered = Signal();
        var release = Signal();
        _parent = async _ =>
        {
            if (heldStage == "parent")
            {
                entered.SetResult();
                await release.Task;
            }
        };
        _evidence = async (_, intent) =>
        {
            if (heldStage == "evidence")
            {
                entered.SetResult();
                await release.Task;
            }
            return Proof(intent, qualifyingLateProof ? "qualified" : "unchanged");
        };
        Task submission = StartAsync(kind, assertInitialState: false);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        object original = Snapshot();
        State().ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        submission.IsCompleted.ShouldBeFalse();
        await AdvanceAsync(Bound);
        State().ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        ((bool)_flow.GetType().GetField("_isSubmitting", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_flow)!).ShouldBeTrue();
        _cut.FindAll($"[data-testid='{Prefix}-continue-read-only']").Count.ShouldBe(1);
        await ClickAsync($"{Prefix}-continue-read-only");
        submission.IsCompleted.ShouldBeFalse();
        State().ShouldBe(TenantCommandLifecycleState.Idle);
        AssertIdentity(original, Retained("tenant.alpha"));
        Expired(Retained("tenant.alpha")).ShouldBeTrue();
        AssertPendingAdmission("tenant.alpha");
        AssertCounts(1, 1, heldStage == "evidence" ? 1 : 0);
        release.SetResult();
        await submission.WaitAsync(TimeSpan.FromSeconds(5));
        State().ShouldBe(TenantCommandLifecycleState.Idle);
        AssertIdentity(original, Retained("tenant.alpha"));
        Property<TenantCommandLifecycleState>(Retained("tenant.alpha"), "State").ShouldBe(qualifyingLateProof ? TenantCommandLifecycleState.Confirmed : TenantCommandLifecycleState.UnableToVerify);
        AssertCounts(1, 1, heldStage == "evidence" ? 1 : 0);
        _auditReads.ShouldBe(0);
        if (qualifyingLateProof)
        {
            AssertTerminalAdmission();
        }
        else
        {
            AssertPendingAdmission("tenant.alpha");
        }
    }

    [Theory]
    [InlineData("add", false)]
    [InlineData("add", true)]
    [InlineData("change", false)]
    [InlineData("change", true)]
    [InlineData("remove", false)]
    [InlineData("remove", true)]
    public async Task RealUnmountAndRemountPreserveTheOriginalDeadline(string kind, bool detachedAtExpiry)
    {
        await StartAsync(kind);
        object original = Snapshot();
        await AdvanceAsync(Bound / 2);
        IComponent originalComponent = _flow;
        await UnmountAsync();
        if (!detachedAtExpiry)
        {
            MountFlow();
            ReferenceEquals(_flow, originalComponent).ShouldBeFalse();
            State().ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        }
        await AdvanceAsync(Bound / 2);
        Property<TenantCommandLifecycleState>(Retained("tenant.alpha"), "State").ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        if (detachedAtExpiry)
        {
            MountFlow();
            ReferenceEquals(_flow, originalComponent).ShouldBeFalse();
        }
        State().ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        AssertUnresolved(original);
        AssertCounts(1, 1, 1);
        _evidence = (_, intent) => Task.FromResult(Proof(intent, "qualified"));
        await RequestAsync(projection: true);
        State().ShouldBe(TenantCommandLifecycleState.Confirmed);
        AssertCounts(2, 2, 2);
        AssertTerminalAdmission();
    }

    [Theory]
    [InlineData("add", "status", false)]
    [InlineData("add", "status", true)]
    [InlineData("add", "evidence", false)]
    [InlineData("add", "evidence", true)]
    [InlineData("change", "status", false)]
    [InlineData("change", "status", true)]
    [InlineData("change", "evidence", false)]
    [InlineData("change", "evidence", true)]
    [InlineData("remove", "status", false)]
    [InlineData("remove", "status", true)]
    [InlineData("remove", "evidence", false)]
    [InlineData("remove", "evidence", true)]
    public async Task DetachedLateOutcomesSettleTheRetainedAttemptWithoutMutatingForeignAdmission(string kind, string heldStage, bool remountBeforeCompletion)
    {
        await StartAsync(kind);
        object original = Snapshot();
        var entered = Signal();
        var release = Signal();
        _status = async _ =>
        {
            if (heldStage == "status")
            {
                entered.SetResult();
                await release.Task;
                return new(CommandStatus.Rejected, HasVerifiedCommandIdentity: true);
            }
            return Completed();
        };
        _evidence = async (_, intent) =>
        {
            entered.SetResult();
            await release.Task;
            return Proof(intent, "qualified");
        };
        Task refresh = RequestAsync(projection: true);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await UnmountAsync();
        await AdvanceAsync(Bound);
        Expired(Retained("tenant.alpha")).ShouldBeTrue();
        AssertPendingAdmission("tenant.alpha");
        if (remountBeforeCompletion)
        {
            MountFlow();
            State().ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        }
        release.SetResult();
        await refresh.WaitAsync(TimeSpan.FromSeconds(5));
        TenantCommandLifecycleState terminal = heldStage == "status" ? TenantCommandLifecycleState.Rejected : TenantCommandLifecycleState.Confirmed;
        Property<TenantCommandLifecycleState>(Retained("tenant.alpha"), "State").ShouldBe(terminal);
        if (!remountBeforeCompletion)
        {
            MountFlow();
        }
        _cut.WaitForAssertion(() => State().ShouldBe(terminal));
        AssertIdentity(original, Snapshot());
        AssertCounts(2, heldStage == "status" ? 1 : 2, heldStage == "status" ? 1 : 2);
        AssertTerminalAdmission();
        object foreignOwner = new();
        Gate().TryAcquireLease(TenantCommandAggregateLock.ForTenant("tenant.alpha"), foreignOwner, out TenantAggregateCommandLease? foreignLease).ShouldBeTrue();
        await UnmountAsync();
        await AdvanceAsync(Bound);
        Gate().IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
        foreignLease!.TryAbandonBeforeDispatch(foreignOwner).ShouldBeTrue();
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public async Task SameInstanceTenantReplacementKeepsIndependentOriginalDeadlines(string kind)
    {
        await StartAsync(kind);
        object original = Snapshot();
        IComponent originalComponent = _flow;
        await AdvanceAsync(Bound / 2);
        MountFlow("tenant.beta");
        ReferenceEquals(_flow, originalComponent).ShouldBeTrue();
        State().ShouldBe(kind == "remove" ? TenantCommandLifecycleState.Previewed : TenantCommandLifecycleState.Idle);
        await SubmitAsync();
        object replacement = Snapshot();
        Property<string>(replacement, "MessageId").ShouldNotBe(Property<string>(original, "MessageId"));
        await AdvanceAsync(Bound / 2);
        State().ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        Expired(Snapshot()).ShouldBeFalse();
        Property<TenantCommandLifecycleState>(Retained("tenant.alpha"), "State").ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        AssertIdentity(original, Retained("tenant.alpha"));
        AssertPendingAdmission("tenant.alpha");
        AssertPendingAdmission("tenant.beta");
        AssertCounts(2, 2, 2, dispatches: 2);
        MountFlow();
        ReferenceEquals(_flow, originalComponent).ShouldBeTrue();
        State().ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        AssertIdentity(original, Snapshot());
        await AdvanceAsync(Bound / 2);
        Property<TenantCommandLifecycleState>(Retained("tenant.beta"), "State").ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        AssertIdentity(replacement, Retained("tenant.beta"));
        AssertCounts(2, 2, 2, dispatches: 2);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public async Task AcceptedOnlyTimeIsOutsideTheProjectionBoundAndFirstPendingStartsItOnce(string kind)
    {
        _status = _ => Task.FromResult(new TenantCommandStatusResult(CommandStatus.Received, HasVerifiedCommandIdentity: true));
        await StartAsync(kind, TenantCommandLifecycleState.Accepted);
        object original = Snapshot();
        await AdvanceAsync(Bound * 2);
        State().ShouldBe(TenantCommandLifecycleState.Accepted);
        Expired(Snapshot()).ShouldBeFalse();
        _clock.PendingTimerCount.ShouldBe(0);
        _status = _ => Task.FromResult(Completed());
        await RequestAsync(projection: true);
        await AdvanceAsync(Bound - TimeSpan.FromTicks(1));
        State().ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        await RequestAsync(projection: false);
        await AdvanceAsync(TimeSpan.FromTicks(1));
        State().ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        AssertUnresolved(original);
        AssertCounts(3, 2, 3);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public async Task AmbiguousDispatchedDeliveryIsOutsideTheProjectionBound(string kind)
    {
        _delivery = (_, message) => Task.FromResult(TenantCommandSubmissionResult.Failed("Delivery is uncertain.") with { MessageId = message, IsAmbiguousFailure = true });
        await StartAsync(kind, TenantCommandLifecycleState.RequestSent);
        object original = Snapshot();
        await AdvanceAsync(Bound * 2);
        State().ShouldBe(TenantCommandLifecycleState.RequestSent);
        Expired(Snapshot()).ShouldBeFalse();
        _clock.PendingTimerCount.ShouldBe(0);
        await RequestAsync(projection: false);
        AssertCounts(0, 0, 0);
        AssertUnresolved(original);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("change")]
    [InlineData("remove")]
    public async Task TrackerDisposalStopsItsTimerWithoutInventingAnOutcomeOrReleasingAdmission(string kind)
    {
        await StartAsync(kind);
        object original = Snapshot();
        _clock.PendingTimerCount.ShouldBe(1);
        ((IDisposable)Tracker()).Dispose();
        _clock.PendingTimerCount.ShouldBe(0);
        await AdvanceAsync(Bound * 2);
        State().ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        AssertIdentity(original, Snapshot());
        Gate().IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
        AssertCounts(1, 1, 1);
    }

    private async Task StartAsync(string kind, TenantCommandLifecycleState expected = TenantCommandLifecycleState.ProjectionPending, bool assertInitialState = true)
    {
        _kind = kind;
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tenants:Ui:MembershipProjectionTimeout"] = "00:00:10",
        }).Build();
        Services.AddSingleton(configuration);
        Services.AddSingleton<TimeProvider>(_clock);
        Services.AddSingleton(provider => new TenantCommandAttemptTracker(provider.GetRequiredService<TenantAggregateCommandAdmissionGate>(), configuration, _clock));
        Services.AddLocalization();
        RenderTree.Add<CascadingValue<string>>(parameters => parameters.Add(component => component.Name, "CurrentTenantCallerScope").Add(component => component.Value, "test-caller"));
        var viewport = new TenantHighImpactViewportObservation();
        viewport.Observe(Hexalith.FrontComposer.Shell.State.Navigation.ViewportTier.Desktop);
        Services.AddSingleton(viewport);
        _gateway = Substitute.For<ITenantCommandGateway>();
        _gateway.AddUserToTenantAsync(Arg.Any<AddUserToTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call => DispatchAsync(nameof(ITenantCommandGateway.AddUserToTenantAsync), call.ArgAt<AddUserToTenant>(0), call.ArgAt<string>(1), call.ArgAt<CancellationToken>(2)));
        _gateway.ChangeUserRoleAsync(Arg.Any<ChangeUserRole>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call => DispatchAsync(nameof(ITenantCommandGateway.ChangeUserRoleAsync), call.ArgAt<ChangeUserRole>(0), call.ArgAt<string>(1), call.ArgAt<CancellationToken>(2)));
        _gateway.RemoveUserFromTenantAsync(Arg.Any<RemoveUserFromTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call => DispatchAsync(nameof(ITenantCommandGateway.RemoveUserFromTenantAsync), call.ArgAt<RemoveUserFromTenant>(0), call.ArgAt<string>(1), call.ArgAt<CancellationToken>(2)));
        _gateway.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            TenantCommandTrackingHandle handle = call.ArgAt<TenantCommandTrackingHandle>(0);
            (string _, object intent, string _) = _dispatches.Single(dispatch => dispatch.MessageId == handle.MessageId);
            handle.CorrelationId.ShouldBe("correlation-original");
            handle.AggregateId.ShouldBe(TenantId(intent));
            handle.ExpectedDomain.ShouldBe(TenantIdentity.Domain);
            call.ArgAt<CancellationToken>(1).ShouldBe(CancellationToken.None);
            if (Interlocked.Increment(ref _activeStatus) > 1)
            {
                Interlocked.Exchange(ref _statusOverlap, 1);
            }
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
        ITenantQueryGateway queries = Substitute.For<ITenantQueryGateway>();
        queries.GetTenantAuditAsync(Arg.Any<TenantAuditRequest>(), Arg.Any<TenantAuditSnapshot?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            Interlocked.Increment(ref _auditReads);
            return Task.FromResult(TenantAuditSnapshot.Unavailable(call.ArgAt<TenantAuditRequest>(0)));
        });
        Services.AddSingleton(queries);
        MountFlow();
        await SubmitAsync();
        if (assertInitialState)
        {
            State().ShouldBe(expected);
            AssertCounts(expected == TenantCommandLifecycleState.RequestSent ? 0 : 1, expected == TenantCommandLifecycleState.RequestSent ? 0 : 1, expected == TenantCommandLifecycleState.RequestSent ? 0 : 1);
        }
    }

    private Task<TenantCommandSubmissionResult> DispatchAsync(string method, object intent, string message, CancellationToken cancellationToken)
    {
        cancellationToken.ShouldBe(CancellationToken.None);
        message.ShouldNotBeNullOrWhiteSpace();
        _dispatches.Add((method, intent, message));
        return _delivery(intent, message);
    }

    private void MountFlow(string tenant = "tenant.alpha")
    {
        IComponent? renderingFlow = null;
        TenantDetail detail = Original(tenant, _kind != "add");
        var parameters = new Dictionary<string, object>
        {
            ["Detail"] = detail,
            ["IsAuthorized"] = true,
            ["AuthorizationReflection"] = TenantLifecycleAuthorizationReflectionState.Authorized,
            ["AuthorizationReflectionProvider"] = (Func<CancellationToken, Task<TenantLifecycleAuthorizationReflectionState>>)(token =>
            {
                _authorityTokens.Add(token);
                return Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized);
            }),
            ["SurfaceKind"] = TenantDetailSurfaceKind.Ready,
            ["Freshness"] = ReadModelFreshnessState.Current,
            ["ProjectionVersion"] = "tenant-sequence:1",
            ["OnProjectionRefreshRequested"] = EventCallback.Factory.Create(this, async () =>
            {
                await _parent(Interlocked.Increment(ref _parentReads));
                await NudgeAsync(renderingFlow.ShouldNotBeNull());
            }),
        };
        switch (_kind)
        {
            case "add":
                parameters["ProjectionEvidenceProvider"] = (Func<AddUserToTenant, Task<TenantDetailSnapshot?>>)(intent => _evidence(Interlocked.Increment(ref _evidenceReads), intent));
                break;
            case "change":
                parameters["Member"] = detail.Members[1];
                parameters["ProjectionEvidenceProvider"] = (Func<ChangeUserRole, Task<TenantDetailSnapshot?>>)(intent => _evidence(Interlocked.Increment(ref _evidenceReads), intent));
                break;
            default:
                parameters["Member"] = detail.Members[1];
                parameters["AuditProofCapabilityAvailable"] = true;
                parameters["TargetPlatformStandingResolved"] = true;
                parameters["ProjectionEvidenceProvider"] = (Func<RemoveUserFromTenant, Task<TenantDetailSnapshot?>>)(intent => _evidence(Interlocked.Increment(ref _evidenceReads), intent));
                break;
        }
        RenderFragment content = builder =>
        {
            builder.OpenComponent(0, _kind switch
            {
                "add" => typeof(AddTenantMemberFlow),
                "change" => typeof(ChangeTenantMemberRoleFlow),
                _ => typeof(RemoveTenantMemberFlow),
            });
            builder.AddMultipleAttributes(1, parameters);
            builder.CloseComponent();
        };
        if (_host is null)
        {
            _host = Render<CascadingValue<bool>>(builder => builder.Add(component => component.Value, true).Add(component => component.ChildContent, content));
        }
        else
        {
            _host.Render(builder => builder.Add(component => component.Value, true).Add(component => component.ChildContent, content));
        }
        _cut = _kind switch
        {
            "add" => _host.FindComponent<AddTenantMemberFlow>(),
            "change" => _host.FindComponent<ChangeTenantMemberRoleFlow>(),
            _ => _host.FindComponent<RemoveTenantMemberFlow>(),
        };
        _flow = _cut.Instance;
        renderingFlow = _flow;
    }

    private async Task SubmitAsync()
    {
        if (_kind == "add")
        {
            _cut.Find("[data-testid='tenants-add-member-user-id']").Change("target");
            FluentSelectInterop.ChangeFluentSelect(_cut, "tenants-add-member-role", nameof(TenantRole.TenantContributor));
        }
        else if (_kind == "change")
        {
            FluentSelectInterop.ChangeFluentSelect(_cut, "tenants-change-role-new-role", nameof(TenantRole.TenantContributor));
        }
        else
        {
            _cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("target");
        }
        await _cut.Find("form").SubmitAsync();
    }

    private Task RequestAsync(bool projection)
        => projection ? ClickAsync($"{Prefix}-refresh") : _cut.InvokeAsync(() => NudgeAsync(_flow));

    private Task ClickAsync(string testId)
    {
        FluentButton button = _cut.FindComponents<FluentButton>().Single(rendered => rendered.Instance.AdditionalAttributes?.TryGetValue("data-testid", out object? value) is true && Equals(value, testId)).Instance;
        button.Disabled.ShouldBeFalse();
        _cut.Find($"[data-testid='{testId}']").HasAttribute("disabled").ShouldBeFalse();
        return _cut.InvokeAsync(() => button.OnClick.InvokeAsync(new MouseEventArgs()));
    }

    private Task UnmountAsync()
    {
        IRenderedComponent<CascadingValue<bool>> host = _host.ShouldNotBeNull();
        return host.InvokeAsync(() => host.Render(parameters => parameters.Add(component => component.Value, true).Add(component => component.ChildContent, (RenderFragment)(_ => { }))));
    }

    private async Task AdvanceAsync(TimeSpan duration)
    {
        _clock.Advance(duration);
        if (_host is not null)
        {
            await _host.InvokeAsync(() => { });
        }
    }

    private void AssertCounts(int status, int parent, int evidence, int dispatches = 1)
    {
        _statusReads.ShouldBe(status);
        _parentReads.ShouldBe(parent);
        _evidenceReads.ShouldBe(evidence);
        _dispatches.Count.ShouldBe(dispatches);
        _gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name is nameof(ITenantCommandGateway.AddUserToTenantAsync) or nameof(ITenantCommandGateway.ChangeUserRoleAsync) or nameof(ITenantCommandGateway.RemoveUserFromTenantAsync)).ShouldBe(dispatches);
        _dispatches.ShouldAllBe(dispatch => dispatch.Method == (_kind == "add" ? nameof(ITenantCommandGateway.AddUserToTenantAsync) : _kind == "change" ? nameof(ITenantCommandGateway.ChangeUserRoleAsync) : nameof(ITenantCommandGateway.RemoveUserFromTenantAsync)));
        _statusOverlap.ShouldBe(0);
    }

    private void AssertUnresolved(object original)
    {
        AssertIdentity(original, Snapshot());
        AssertPendingAdmission("tenant.alpha");
        object unrelatedOwner = new();
        Gate().TryAcquireLease(TenantCommandAggregateLock.ForTenant("tenant.unrelated"), unrelatedOwner, out TenantAggregateCommandLease? unrelatedLease).ShouldBeTrue();
        unrelatedLease!.TryAbandonBeforeDispatch(unrelatedOwner).ShouldBeTrue();
    }

    private void AssertPendingAdmission(string tenant)
    {
        Tracker().HasPendingOwnership(tenant).ShouldBeTrue();
        Gate().IsLocked(TenantCommandAggregateLock.ForTenant(tenant)).ShouldBeTrue();
        Gate().TryAcquireLease(TenantCommandAggregateLock.ForTenant(tenant), new object(), out _).ShouldBeFalse();
    }

    private void AssertTerminalAdmission()
    {
        Tracker().HasPendingOwnership("tenant.alpha").ShouldBeFalse();
        Gate().IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
    }

    private static void AssertIdentity(object expected, object actual)
    {
        foreach (string property in new[] { "Intent", "MessageId", "CorrelationId", "BaselineProjectionVersion", "BaselinePostconditionMet" })
        {
            actual.GetType().GetProperty(property)!.GetValue(actual).ShouldBe(expected.GetType().GetProperty(property)!.GetValue(expected));
        }
        if (Property<bool>(expected, "HasCommandEventEvidence"))
        {
            Property<bool>(actual, "HasCommandEventEvidence").ShouldBeTrue();
        }
    }

    private string Prefix => _kind == "add" ? "tenants-add-member" : _kind == "change" ? "tenants-change-role" : "tenants-remove-member";
    private object Snapshot() => _flow.GetType().GetProperty("Snapshot", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_flow)!;
    private TenantCommandLifecycleState State() => Property<TenantCommandLifecycleState>(Snapshot(), "State");
    private static bool Expired(object snapshot) => Property<bool>(snapshot, "ProjectionWaitExpired");
    private static T? Property<T>(object snapshot, string property) => (T?)snapshot.GetType().GetProperty(property)!.GetValue(snapshot);
    private TenantAggregateCommandAdmissionGate Gate() => Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
    private TenantCommandAttemptTracker Tracker() => Services.GetRequiredService<TenantCommandAttemptTracker>();
    private object Retained(string tenant) => _kind switch
    {
        "add" => Tracker().Find<TenantAddMemberCommandSnapshot>(tenant)!,
        "change" => Tracker().Find<TenantChangeRoleCommandSnapshot>(tenant)!,
        _ => Tracker().Find<TenantRemoveMemberCommandSnapshot>(tenant)!,
    };

    private static Task NudgeAsync(IComponent flow) => flow switch
    {
        AddTenantMemberFlow add => add.HandleAuthoritativeRefreshNudgeAsync(),
        ChangeTenantMemberRoleFlow change => change.HandleAuthoritativeRefreshNudgeAsync(),
        RemoveTenantMemberFlow remove => remove.HandleAuthoritativeRefreshNudgeAsync(),
        _ => throw new InvalidOperationException("Unexpected membership surface."),
    };

    private static TenantCommandStatusResult Completed() => new(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true) { CommittedEventSequence = 2 };
    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static string TenantId(object intent) => (string)intent.GetType().GetProperty("TenantId")!.GetValue(intent)!;
    private static TenantDetail Original(string tenant, bool includeTarget) => new(tenant, "Alpha", "Description", TenantStatus.Active,
        includeTarget ? [new("owner", TenantRole.TenantOwner), new("target", TenantRole.TenantReader)] : [new("owner", TenantRole.TenantOwner)], new Dictionary<string, string>(), DateTimeOffset.UnixEpoch);

    private static TenantDetailSnapshot? Proof(object intent, string kind)
    {
        if (kind == "unavailable")
        {
            return null;
        }
        TenantDetail detail = Original(kind == "wrong-tenant" ? "tenant.foreign" : TenantId(intent), includeTarget: true);
        if (intent is RemoveUserFromTenant)
        {
            if (kind != "wrong-postcondition")
            {
                detail = detail with { Members = [new("owner", TenantRole.TenantOwner)] };
            }
        }
        else if (kind != "wrong-postcondition")
        {
            detail = detail with { Members = [new("owner", TenantRole.TenantOwner), new("target", TenantRole.TenantContributor)] };
        }
        string version = kind == "unchanged" ? "tenant-sequence:1" : kind == "regressed" ? "tenant-sequence:0" : "tenant-sequence:2";
        return kind == "stale"
            ? TenantDetailSnapshot.Ready(detail, "etag-proof", ReadModelFreshnessState.Stale, ProjectionLifecycleState.Current, version).WithCallerScope("test-caller")
            : TenantCommandProofFixtures.Proof(detail, version);
    }
}
