using System.Globalization;

using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Configuration;
using Hexalith.Tenants.UI.Components.Tenants.Members;
using Hexalith.Tenants.UI.Components.Tenants.Metadata;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Configuration;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>Qualifies localized recovery explanations against the actions actually rendered by command flows.</summary>
public sealed class CommandRecoveryExplanationTests : FluentBunitContext
{
    /// <summary>Gets the supported surfaces, shipped cultures and callback/retry conditions.</summary>
    /// <returns>One gateway-driven rendered regression per combination.</returns>
    public static IEnumerable<object[]> Cases()
    {
        foreach (string kind in new[] { "metadata", "set", "remove-configuration", "remove-member" })
            foreach (string culture in new[] { "en", "fr" })
                foreach (string condition in new[] { "previewed", "rejected", "degraded", "exhausted", "untracked" })
                {
                    if (kind is not "metadata" || condition is not "previewed")
                    {
                        yield return [kind, culture, condition];
                    }
                }
    }

    /// <summary>Recovery text stays truthful with no optional destinations and when unchanged retries run out.</summary>
    /// <param name="kind">The command surface to exercise.</param>
    /// <param name="culture">The shipped resource culture.</param>
    /// <param name="condition">The gateway outcome and shared retry-budget condition.</param>
    [Theory]
    [MemberData(nameof(Cases))]
    public void RecoveryExplanationDoesNotPromiseAbsentActions(string kind, string culture, string condition)
    {
        ITenantCommandGateway gateway = RegisterGateway(culture, condition is "rejected", untracked: condition is "untracked");
        (IRenderedComponent<IComponent> cut, Func<TenantCommandLifecycleState> state, string prefix) = RenderFlow(kind);
        PrepareForm(kind, cut);
        if (condition is not "previewed")
        {
            cut.Find("form").Submit();
        }

        TenantCommandLifecycleState expected = condition switch
        {
            "previewed" => TenantCommandLifecycleState.Previewed,
            "rejected" => TenantCommandLifecycleState.Rejected,
            "untracked" => TenantCommandLifecycleState.UnableToVerify,
            _ => TenantCommandLifecycleState.Degraded,
        };
        cut.WaitForAssertion(() => state().ShouldBe(expected));
        string recoverySelector = $"[data-testid='tenants-{prefix}-recovery']";
        string explanation = cut.Find(recoverySelector).TextContent.Trim();
        explanation.ShouldBe(ExpectedExplanation(kind, culture, condition));
        AssertNoActionPromises(explanation);

        // Neither an audit destination nor an escalation destination has been supplied to these flows.
        cut.FindAll("[data-recovery-verb='inspectaudit'], [data-recovery-verb='escalate']").ShouldBeEmpty();
        if (condition is "previewed" or "rejected")
        {
            cut.FindAll("[data-testid='tenants-audit-availability']").ShouldBeEmpty();
            gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ITenantCommandGateway.GetStatusAsync))
                .ShouldBe(0);
        }
        else if (condition is "untracked")
        {
            if (kind is "set")
            {
                // This flow retains message identity and exposes reconciliation independently of correlation.
                cut.FindAll("[data-recovery-verb='refresh']").Count.ShouldBe(1);
            }
            else
            {
                cut.FindAll("[data-recovery-verb='refresh']").ShouldBeEmpty();
            }
            StatusLookupCount(gateway).ShouldBe(0);
        }
        else if (condition is "exhausted")
        {
            string refreshSelector = $"[data-testid='tenants-{prefix}-audit'] [data-recovery-verb='refresh']";
            int initialLookups = StatusLookupCount(gateway);
            for (int retry = 0; retry < TenantAuditAvailability.MaximumUnchangedRetries; retry++)
            {
                int expectedLookups = initialLookups + retry + 1;
                cut.Find(refreshSelector).Click();
                cut.WaitForAssertion(() => StatusLookupCount(gateway).ShouldBe(expectedLookups));
                state().ShouldBe(expected);
            }

            cut.WaitForAssertion(() => cut.FindAll(refreshSelector).ShouldBeEmpty());
            cut.Find("[data-testid='tenants-audit-availability-retry-limit']");
            cut.Find(recoverySelector).TextContent.Trim().ShouldBe(explanation);
            AssertNoActionPromises(cut.Find(recoverySelector).TextContent);
            cut.FindAll("[data-recovery-verb='inspectaudit'], [data-recovery-verb='escalate']").ShouldBeEmpty();
        }

        // Recovery lookup and presentation must never dispatch a second mutation.
        gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == DispatchName(kind))
            .ShouldBe(condition is "previewed" ? 0 : 1);
    }

    /// <summary>Unavailable preview evidence must not imply that a command was dispatched.</summary>
    /// <param name="kind">The configuration command surface.</param>
    /// <param name="culture">The shipped resource culture.</param>
    [Theory]
    [InlineData("set", "en")]
    [InlineData("set", "fr")]
    [InlineData("remove-configuration", "en")]
    [InlineData("remove-configuration", "fr")]
    public void BlockedPreviewDescribesEvidenceWithoutInventingACommandOutcome(string kind, string culture)
    {
        ITenantCommandGateway gateway = RegisterGateway(culture, rejected: false);
        (IRenderedComponent<IComponent> cut, Func<TenantCommandLifecycleState> state, string prefix) = RenderFlow(kind, blockedPreview: true);
        PrepareForm(kind, cut);
        cut.WaitForAssertion(() => state().ShouldBe(TenantCommandLifecycleState.UnableToVerify));
        string explanation = cut.Find($"[data-testid='tenants-{prefix}-recovery']").TextContent.Trim();
        explanation.ShouldBe(ExpectedExplanation(kind, culture, "untracked"));
        AssertNoActionPromises(explanation);
        cut.FindAll("[data-testid='tenants-audit-availability']").ShouldBeEmpty();
        gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == DispatchName(kind)).ShouldBe(0);
        StatusLookupCount(gateway).ShouldBe(0);
    }

    private static string ExpectedExplanation(string kind, string culture, string condition)
    {
        string operation = (culture, kind) switch
        {
            ("fr", "metadata") => "la mise à jour des métadonnées demandée",
            ("fr", "set") => "la modification de configuration demandée",
            ("fr", "remove-configuration") => "la suppression de la clé de configuration",
            ("fr", _) => "le retrait du membre",
            (_, "metadata") => "the requested metadata update",
            (_, "set") => "the requested configuration change",
            (_, "remove-configuration") => "removal of the configuration key",
            _ => "removal of the member",
        };
        string frenchPossessive = operation.StartsWith("le ", StringComparison.Ordinal)
            ? "du " + operation[3..] : "de " + operation;
        return (culture, condition) switch
        {
            ("fr", "previewed") => "Examinez la modification proposée et ses conséquences.",
            (_, "previewed") => "Review the proposed change and its consequences.",
            ("fr", "rejected") => $"La commande a été rejetée. Elle ne confirme pas {operation}.",
            (_, "rejected") => $"The command was rejected. It does not confirm {operation}.",
            ("fr", "untracked") => $"Les preuves disponibles n’établissent pas {operation}.",
            (_, "untracked") => $"The available evidence does not establish {operation}.",
            ("fr", _) => $"Le traitement de la commande est dégradé. La confirmation {frenchPossessive} reste à établir.",
            _ => $"Command processing is degraded. {char.ToUpperInvariant(operation[0])}{operation[1..]} has not been confirmed.",
        };
    }

    private static int StatusLookupCount(ITenantCommandGateway gateway)
        => gateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ITenantCommandGateway.GetStatusAsync));

    private static void AssertNoActionPromises(string explanation)
    {
        foreach (string promise in new[]
        {
            "retry", "refresh", "inspect", "escalate", "continue read-only", "request permission", "start correction",
            "réessay", "relancez", "actualisez", "inspectez", "escaladez", "continuez", "demandez", "démarrez",
        })
        {
            explanation.ShouldNotContain(promise, Case.Insensitive);
        }
    }

    private ITenantCommandGateway RegisterGateway(string culture, bool rejected, bool untracked = false)
    {
        Services.AddLocalization();
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(provider => new CultureBoundTenantsLocalizer(
            provider.GetRequiredService<IStringLocalizerFactory>().Create(typeof(TenantsResources)),
            CultureInfo.GetCultureInfo(culture)));
        Services.AddSingleton(new TenantSetConfigurationAttemptTracker());
        Services.AddSingleton(new TenantRemoveConfigurationAttemptTracker());
        var viewport = new TenantHighImpactViewportObservation();
        viewport.Observe(ViewportTier.Desktop);
        Services.AddSingleton(viewport);
        Services.AddSingleton(Substitute.For<ITenantQueryGateway>());

        ITenantCommandGateway gateway = Substitute.For<ITenantCommandGateway>();
        gateway.SupportsTrackedSetConfigurationDispatch.Returns(true);
        gateway.SupportsTrackedRemoveConfigurationDispatch.Returns(true);
        gateway.SupportsCommandStatusLookup.Returns(true);
        TenantCommandSubmissionResult Submission(string messageId)
            => rejected
                ? TenantCommandSubmissionResult.Rejected("The command was rejected.") with { MessageId = messageId }
                : TenantCommandSubmissionResult.Accepted(messageId, untracked ? string.Empty : "correlation-recovery");
        gateway.UpdateTenantAsync(Arg.Any<UpdateTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Submission(call.ArgAt<string>(1))));
        gateway.RemoveUserFromTenantAsync(Arg.Any<RemoveUserFromTenant>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Submission(call.ArgAt<string>(1))));
        gateway.SetTenantConfigurationTrackedAsync(Arg.Any<SetTenantConfiguration>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Submission(call.ArgAt<string>(1))));
        gateway.RemoveTenantConfigurationTrackedAsync(Arg.Any<RemoveTenantConfiguration>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Submission(call.ArgAt<string>(1))));
        gateway.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantCommandStatusResult(CommandStatus.PublishFailed, HasVerifiedCommandIdentity: true)));
        Services.AddSingleton(gateway);
        return gateway;
    }

    private (IRenderedComponent<IComponent> Cut, Func<TenantCommandLifecycleState> State, string Prefix) RenderFlow(string kind, bool blockedPreview = false)
    {
        TenantDetail detail = new("tenant.alpha", "Alpha", "Original", TenantStatus.Active,
            [new TenantMember("owner", TenantRole.TenantOwner), new TenantMember("reader", TenantRole.TenantReader)],
            new Dictionary<string, string>(), DateTimeOffset.UtcNow);
        TenantConfigurationManagementContext context = TenantConfigurationManagementContext.Available(
            detail.TenantId, TenantStatus.Active, false, ["billing"],
            [new TenantConfigurationSafeRow("billing", "billing.mode", "enabled")], TenantConfigurationAuthorityState.TenantOwner);
        switch (kind)
        {
            case "metadata":
                IRenderedComponent<EditTenantMetadataFlow> metadata = RenderScoped<EditTenantMetadataFlow>(p => p
                    .Add(x => x.Detail, detail).Add(x => x.SurfaceKind, TenantDetailSurfaceKind.Ready)
                    .Add(x => x.Freshness, ReadModelFreshnessState.Current).Add(x => x.Lifecycle, ProjectionLifecycleState.Current)
                    .Add(x => x.IsAuthorized, true).Add(x => x.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
                    .Add(x => x.AuthorizationReflectionProvider, _ => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
                    .Add(x => x.ProjectionVersion, "tenant-sequence:1"));
                return (metadata, () => metadata.Instance.Snapshot.State, "edit-metadata");
            case "set":
                IRenderedComponent<SetTenantConfigurationFlow> set = RenderScoped<SetTenantConfigurationFlow>(p => p
                    .Add(x => x.Context, context).Add(x => x.SurfaceKind, TenantDetailSurfaceKind.Ready)
                    .Add(x => x.Freshness, ReadModelFreshnessState.Current).Add(x => x.Lifecycle, ProjectionLifecycleState.Current)
                    .Add(x => x.PreviewEvidenceProvider, intent => Task.FromResult(blockedPreview ? TenantSetConfigurationPreview.Unavailable(intent) : TenantSetConfigurationPreview.Create(
                        intent, TenantStatus.Active, TenantSetConfigurationCurrentState.Different,
                        ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, "tenant-sequence:1", isAuthorized: true))));
                return (set, () => set.Instance.Snapshot.State, "config-set");
            case "remove-configuration":
                IRenderedComponent<RemoveTenantConfigurationFlow> removeConfiguration = RenderScoped<RemoveTenantConfigurationFlow>(p => p
                    .Add(x => x.Context, context).Add(x => x.TargetKey, "billing.mode")
                    .Add(x => x.SurfaceKind, TenantDetailSurfaceKind.Ready).Add(x => x.Freshness, ReadModelFreshnessState.Current)
                    .Add(x => x.Lifecycle, ProjectionLifecycleState.Current)
                    .Add(x => x.PreviewEvidenceProvider, intent => Task.FromResult(blockedPreview ? TenantRemoveConfigurationPreview.Unavailable(intent) : TenantRemoveConfigurationPreview.Create(
                        intent, TenantStatus.Active, TenantRemoveConfigurationCurrentState.Present,
                        ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, "tenant-sequence:1", isAuthorized: true))));
                return (removeConfiguration, () => removeConfiguration.Instance.Snapshot.State, "config-remove");
            default:
                IRenderedComponent<RemoveTenantMemberFlow> removeMember = RenderScoped<RemoveTenantMemberFlow>(p => p
                    .Add(x => x.Detail, detail).Add(x => x.Member, detail.Members[1])
                    .Add(x => x.SurfaceKind, TenantDetailSurfaceKind.Ready).Add(x => x.Freshness, ReadModelFreshnessState.Current)
                    .Add(x => x.IsAuthorized, true).Add(x => x.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
                    .Add(x => x.AuthorizationReflectionProvider, _ => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
                    .Add(x => x.ProjectionVersion, "tenant-sequence:1")
                    .Add(x => x.AuditProofCapabilityAvailable, true).Add(x => x.TargetPlatformStandingResolved, true));
                return (removeMember, () => removeMember.Instance.Snapshot.State, "remove-member");
        }
    }

    private IRenderedComponent<T> RenderScoped<T>(Action<ComponentParameterCollectionBuilder<T>> parameters)
        where T : class, IComponent
        => Render<CascadingValue<string>>(p => p.Add(x => x.Name, "CurrentTenantCallerScope").Add(x => x.Value, "caller-recovery")
            .AddChildContent<CascadingValue<bool>>(audit => audit.Add(x => x.Name, "AuditReadDenied").Add(x => x.Value, true)
                .AddChildContent(parameters))).FindComponent<T>();

    private static void PrepareForm(string kind, IRenderedComponent<IComponent> cut)
    {
        switch (kind)
        {
            case "metadata":
                cut.Find("[data-testid='tenants-edit-metadata-open']").Click();
                cut.Find("[data-testid='tenants-edit-metadata-name']").Change("Updated");
                break;
            case "set":
                cut.Find("[data-testid='tenants-config-set-open']").Click();
                cut.Find("[data-testid='tenants-config-set-key-suffix']").Change("mode");
                cut.Find("[data-testid='tenants-config-set-value']").Change("Updated");
                break;
            case "remove-configuration":
                cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.mode");
                break;
            default:
                cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("reader");
                break;
        }
    }

    private static string DispatchName(string kind)
        => kind switch
        {
            "metadata" => nameof(ITenantCommandGateway.UpdateTenantAsync),
            "set" => nameof(ITenantCommandGateway.SetTenantConfigurationTrackedAsync),
            "remove-configuration" => nameof(ITenantCommandGateway.RemoveTenantConfigurationTrackedAsync),
            _ => nameof(ITenantCommandGateway.RemoveUserFromTenantAsync),
        };
}
