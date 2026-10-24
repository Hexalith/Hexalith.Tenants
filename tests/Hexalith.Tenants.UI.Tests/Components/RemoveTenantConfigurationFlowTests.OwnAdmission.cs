using Bunit;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.UI.Components.Tenants.Configuration;
using Hexalith.Tenants.UI.Services.Configuration;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>Qualifies retained command ownership and parent-render safety.</summary>
public sealed partial class RemoveTenantConfigurationFlowTests
{
    /// <summary>Allows only exact own-admission bookkeeping while preserving all removal safety checks.</summary>
    /// <param name="change">The evidence transition caused by the parent callback.</param>
    [Theory]
    [InlineData("own-admission")]
    [InlineData("authority")]
    [InlineData("version")]
    [InlineData("tenant")]
    [InlineData("support")]
    [InlineData("viewport")]
    [InlineData("surface-only")]
    [InlineData("busy-only")]
    [InlineData("confirmation")]
    [InlineData("transient-authority")]
    [InlineData("refused")]
    [InlineData("no-lease")]
    public void RemovalSubmitDistinguishesOwnAdmissionRenderFromChangedSafetyEvidence(string change)
    {
        var gateway = new StubTenantCommandGateway
        {
            Submission = TenantCommandSubmissionResult.Accepted("ignored", "correlation-owned-remove"),
            Status = new(CommandStatus.Received, HasVerifiedCommandIdentity: true),
        };
        RegisterServices(gateway);
        TenantConfigurationManagementContext context = Context(
            "tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" });
        TenantHighImpactActionEvidence original = Evidence();
        IRenderedComponent<RemoveTenantConfigurationFlow>? cut = null;
        cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Context, context)
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.Evidence, original)
            .Add(p => p.CommandActivityLease, active =>
            {
                if (!active) return Task.FromResult(true);
                IRenderedComponent<RemoveTenantConfigurationFlow> current = cut.ShouldNotBeNull();
                TenantHighImpactActionEvidence busy = original with { Admission = TenantHighImpactAdmissionEvidence.Busy };
                if (change == "transient-authority")
                {
                    current.Render(p => p.Add(v => v.Evidence, busy with { Authority = TenantHighImpactAuthorityEvidence.MissingPermission })
                        .Add(v => v.IsCommandSurfaceAvailable, false));
                }
                TenantHighImpactActionEvidence incoming = change switch
                {
                    "authority" => busy with { Authority = TenantHighImpactAuthorityEvidence.MissingPermission },
                    "version" => busy with { ProjectionVersion = "tenant-sequence:42" },
                    "support" => busy with { Support = TenantHighImpactSupportEvidence.Missing },
                    "viewport" => busy with { Viewport = TenantHighImpactViewportState.Unsafe },
                    "surface-only" => original,
                    _ => busy,
                };
                current.Render(p =>
                {
                    p.Add(v => v.Evidence, incoming)
                        .Add(v => v.IsCommandSurfaceAvailable, change == "busy-only");
                    if (change == "tenant")
                    {
                        p.Add(v => v.Context, Context("tenant.beta", new Dictionary<string, string> { ["billing.mode"] = "trial" }));
                    }
                });
                if (change == "confirmation")
                {
                    current.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.other");
                }
                return Task.FromResult(change != "refused");
            }));
        if (change == "no-lease")
        {
            cut.Render(p => p
                .Add(v => v.CommandActivityLease, (Func<bool, Task<bool>>?)null)
                .Add(v => v.OnCommandActivityChanged, active =>
                {
                    if (active)
                    {
                        cut.Render(v => v.Add(c => c.Evidence, original with { Admission = TenantHighImpactAdmissionEvidence.Busy })
                            .Add(c => c.IsCommandSurfaceAvailable, false));
                    }
                }));
        }
        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.mode");
        cut.Find("form").Submit();

        if (change == "own-admission")
        {
            cut.WaitForAssertion(() => gateway.RemoveConfigurationCallCount.ShouldBe(1));
            cut.Instance.Snapshot.MessageId.ShouldNotBeNullOrWhiteSpace();
            cut.Instance.Snapshot.Intent.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
            cut.Instance.Snapshot.RetainsAttempt.ShouldBeTrue();
        }
        else
        {
            cut.WaitForAssertion(() =>
            {
                gateway.RemoveConfigurationCallCount.ShouldBe(0);
                Services.GetRequiredService<TenantRemoveConfigurationAttemptTracker>()
                    .HasPendingOwnership("tenant.alpha").ShouldBeFalse();
            });
        }
    }
}
