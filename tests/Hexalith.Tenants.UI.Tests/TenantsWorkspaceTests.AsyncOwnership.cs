using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Components.Tenants;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

public sealed partial class TenantsWorkspaceTests
{
    [Fact]
    public async Task SynchronousCancellationCompletionCannotApplyTheSupersededList()
    {
        TenantListSnapshot original = TenantListSnapshot.Empty(true, ReadModelFreshnessState.Current);
        TenantListSnapshot obsolete = original with { Notice = TenantListReason.ListRefreshed };
        // Deliberately allow inline continuations: the old read completes inside Cancel(), before
        // BeginLoad returns to the new read. An asynchronous TCS would miss this exact boundary.
        var oldRead = new TaskCompletionSource<TenantListSnapshot>();
        var newRead = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        ITenantQueryGateway reads = FiniteWorkspaceReads();
        IRenderedComponent<TenantsWorkspace>? cut = null;
        TenantListSnapshot? confirmedInsideCancellation = null;
        long oldLoadVersion = 0;
        long versionInsideCancellation = 0;
        CancellationTokenRegistration registration = default;
        Task? oldRefresh = null;
        int calls = 0;
        reads.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                switch (++calls)
                {
                    case 1: return Task.FromResult(original);
                    case 2:
                        oldLoadVersion = PrivateField<long>(cut!.Instance, "_loadVersion");
                        registration = call.ArgAt<CancellationToken>(2).Register(() =>
                        {
                            versionInsideCancellation = PrivateField<long>(cut!.Instance, "_loadVersion");
                            oldRead.SetResult(obsolete);
                            confirmedInsideCancellation = PrivateField<TenantListSnapshot>(cut!.Instance, "_lastConfirmedSnapshot");
                        });
                        return oldRead.Task;
                    default: return newRead.Task;
                }
            });
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition());
        cut = RenderWorkspace();
        cut.WaitForAssertion(() => calls.ShouldBe(1));
        oldRefresh = cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => calls.ShouldBe(2));

        Task replacing = cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        try
        {
            cut.WaitForAssertion(() => calls.ShouldBe(3));
            versionInsideCancellation.ShouldBeGreaterThan(oldLoadVersion,
                "replacement must retire the prior generation before its synchronous cancellation callback");
            await oldRefresh.WaitAsync(TimeSpan.FromSeconds(5));
            confirmedInsideCancellation.ShouldBeSameAs(original);
            PrivateField<TenantListSnapshot>(cut.Instance, "_lastConfirmedSnapshot").ShouldBeSameAs(original);
            cut.FindAll("[data-testid='tenants-list-paging-notice']").ShouldBeEmpty();
        }
        finally
        {
            newRead.TrySetResult(original);
            await Task.WhenAll(oldRefresh, replacing).WaitAsync(TimeSpan.FromSeconds(5));
            registration.Dispose();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProductionCreateHostWithholdsDismissalDuringHeldProjectionProof(bool proofFails)
    {
        Services.AddSingleton<AuthenticationStateProvider>(new MutableAuthenticationStateProvider(AdministratorPrincipal()));
        TenantListSnapshot original = TenantListSnapshot.Empty(true, ReadModelFreshnessState.Current) with
        {
            CallerScope = TenantQueryGateway.AuditCallerScope("operator.alpha"),
        };
        var proof = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        ITenantQueryGateway reads = FiniteWorkspaceReads();
        int listCalls = 0;
        reads.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++listCalls == 3 ? proof.Task : Task.FromResult(original));
        reads.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(TenantDetailSnapshot.NotFound(call.ArgAt<TenantDetailRequest>(0).TenantId)));
        ITenantCommandGateway commands = Substitute.For<ITenantCommandGateway>();
        commands.CreateTenantAsync(Arg.Any<CreateTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(TenantCommandSubmissionResult.Accepted(call.ArgAt<string>(1), "host-proof-correlation")));
        commands.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true)));
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized), commands);
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        IRenderedComponent<CreateTenantFlow> create = cut.FindComponent<CreateTenantFlow>();
        cut.WaitForAssertion(() => create.Instance.IsAuthorized.ShouldBeTrue());
        SetAcceptanceCreateIntent(create);

        Task submit = create.Find("form").SubmitAsync();
        try
        {
            cut.WaitForAssertion(() => listCalls.ShouldBe(3));
            cut.FindComponent<CreateTenantFlow>().Instance.ShouldBeSameAs(create.Instance);
            create.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
            create.FindAll("[data-recovery-verb='continuereadonly']").ShouldBeEmpty();
            if (proofFails) proof.SetException(new InvalidOperationException("held host proof failure"));
            else proof.SetResult(original);
            await submit.WaitAsync(TimeSpan.FromSeconds(5));
            // The real host keeps audit pending while projection proof is unresolved, so this
            // dismissal path remains unavailable. The callback-free flow tests qualify dismissal
            // when its shared recovery control does offer it.
            create.FindAll("[data-recovery-verb='continuereadonly']").ShouldBeEmpty();
            create.Instance.Snapshot.State.ShouldBe(proofFails
                ? TenantCommandLifecycleState.UnableToVerify : TenantCommandLifecycleState.ProjectionPending);
            create.Instance.Snapshot.Intent!.TenantId.ShouldBe("retained-create");
            create.Instance.Snapshot.MessageId.ShouldNotBeNullOrWhiteSpace();
            Services.GetRequiredService<TenantAggregateCommandAdmissionGate>()
                .IsLocked(TenantCommandAggregateLock.ForTenant("retained-create")).ShouldBeTrue();
            cut.Markup.ShouldNotContain("held host proof failure");
            _ = commands.Received(1).CreateTenantAsync(Arg.Any<CreateTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            proof.TrySetResult(original);
            await submit.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}
