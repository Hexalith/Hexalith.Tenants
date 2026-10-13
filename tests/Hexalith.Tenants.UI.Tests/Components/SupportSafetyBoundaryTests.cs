using System.Globalization;
using System.Net;
using System.Text.Json;

using Bunit;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Shared;
using Hexalith.Tenants.UI.Components.Tenants;
using Hexalith.Tenants.UI.Components.Tenants.Audit;
using Hexalith.Tenants.UI.Components.Tenants.Metadata;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.Services.SupportSafety;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>Observes support boundaries through their DOM, live regions, JSON and actual clipboard calls.</summary>
public sealed class SupportSafetyBoundaryTests : FluentBunitContext
{
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string CorrelationId = "tracking-original";

    [Theory]
    [InlineData("en", "ConcurrencyConflict")]
    [InlineData("fr", "ConcurrencyConflict")]
    [InlineData("en", "BackpressureExceeded")]
    [InlineData("fr", "BackpressureExceeded")]
    [InlineData("en", "DomainRejected")]
    [InlineData("fr", "DomainRejected")]
    public void MetadataConsumerPrefersTheFreshLocalizedFailureKey(string culture, string code)
    {
        UseCulture(culture);
        ConfigureCreate(Gateway(code, CommandStatus.Rejected));
        Hexalith.Tenants.Contracts.Queries.TenantDetail detail = new("tenant.alpha", "Alpha", "Description",
            TenantStatus.Active, [new TenantMember("owner-user", TenantRole.TenantOwner)],
            new Dictionary<string, string>(), new DateTimeOffset(2026, 10, 13, 10, 0, 0, TimeSpan.Zero));
        IRenderedComponent<EditTenantMetadataFlow> cut = Render<CascadingValue<string>>(parameters => parameters
            .Add(component => component.Name, "CurrentTenantCallerScope")
            .Add(component => component.Value, "test-caller")
            .AddChildContent<EditTenantMetadataFlow>(child => child
                .Add(component => component.Detail, detail)
                .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
                .Add(component => component.Freshness, ReadModelFreshnessState.Current)
                .Add(component => component.Lifecycle, ProjectionLifecycleState.Current)
                .Add(component => component.ProjectionVersion, "tenant-sequence:1")))
            .FindComponent<EditTenantMetadataFlow>();
        cut.Find("[data-testid='tenants-edit-metadata-open']").Click();
        cut.Find("[data-testid='tenants-edit-metadata-name']").Change("Alpha updated");
        cut.Find("form").Submit();
        IStringLocalizer<TenantsResources> localizer = Services.GetRequiredService<IStringLocalizer<TenantsResources>>();
        string key = $"Tenants.Commands.Failure.{code}";
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-edit-metadata-safe-message']").TextContent.ShouldBe(localizer[key].Value));
        Live(cut).ShouldContain(localizer[key].Value);
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        cut.Instance.Snapshot.MessageId.ShouldBe(MessageId);
        cut.Instance.Snapshot.CorrelationId.ShouldBe(CorrelationId);
        cut.Instance.Snapshot.Intent.ShouldNotBeNull().Name.ShouldBe("Alpha updated");
    }

    [Theory]
    [InlineData("en", "ConcurrencyConflict", "The command could not complete because the current state changed. Retry status lookup.")]
    [InlineData("fr", "ConcurrencyConflict", "La commande n’a pas pu aboutir car l’état actuel a changé. Relancez la consultation du statut.")]
    [InlineData("en", "BackpressureExceeded", "The command could not complete because the service is busy. Retry status lookup later.")]
    [InlineData("fr", "BackpressureExceeded", "La commande n’a pas pu aboutir car le service est occupé. Relancez la consultation du statut plus tard.")]
    [InlineData("en", "DomainRejected", "The command was rejected.")]
    [InlineData("fr", "DomainRejected", "La commande a été rejetée.")]
    public async Task ExactFailureCodesRenderLocalizedWholeMessagesWithoutChangingEvidence(
        string culture, string code, string expected)
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            UseCulture(culture);
            TenantCommandGateway gateway = Gateway(code, CommandStatus.Rejected);
            TenantCommandStatusResult result = await gateway.GetStatusAsync(new(MessageId, CorrelationId, "tenant.alpha"));
            result.Status.ShouldBe(CommandStatus.Rejected);
            result.SafeMessageKey.ShouldBe($"Tenants.Commands.Failure.{code}");
            result.HasVerifiedCommandIdentity.ShouldBeTrue();
            result.EventCount.ShouldBe(0);
            result.IsPending.ShouldBeFalse();
            result.IsRetryableFailure.ShouldBeFalse();
            result.RejectionCode.ShouldBeNull();
            result.CommittedEventSequence.ShouldBeNull();

            ConfigureCreate(gateway);
            IRenderedComponent<CreateTenantFlow> cut = SubmitCreate();
            cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-create-safe-message']").TextContent.ShouldBe(expected));
            cut.Find("[data-testid='tenants-create-live-region']").TextContent.ShouldContain(expected);
            cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected);
            cut.Instance.Snapshot.MessageId.ShouldBe(MessageId);
            cut.Instance.Snapshot.CorrelationId.ShouldBe(CorrelationId);
            cut.Instance.Snapshot.Intent.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
            cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.NotStarted);
            cut.Instance.Snapshot.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
            cut.Instance.Snapshot.ShouldBe(cut.Instance.Snapshot.ApplyStatus(new(CommandStatus.Completed, HasVerifiedCommandIdentity: true)));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Theory]
    [InlineData("ordinary-private-detail-Canary")]
    [InlineData("password=Canary123")]
    [InlineData("person-canary@example.test")]
    [InlineData("%73%65%63%72%65%74-Canary")]
    [InlineData("％73％65％63％72％65％74-Canary")]
    [InlineData("ConcurrencyConflict Canary")]
    [InlineData(" ConcurrencyConflict")]
    [InlineData("concurrencyconflict")]
    [InlineData("<Canary-équipe>")]
    public async Task UnknownBackendDetailsCannotReachSupportObservers(string canary)
    {
        Services.AddLocalization();
        ConfigureCreate(Gateway(canary, CommandStatus.Rejected));
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        JSRuntimeInvocationHandler write = module.SetupVoid("writeText", _ => true).SetVoidResult();
        ProveDisclosureObservers(canary, write);
        int controlWrites = write.Invocations.Count;

        TenantCommandStatusResult processing = await Gateway(canary, CommandStatus.Processing)
            .GetStatusAsync(new(MessageId, CorrelationId, "tenant.alpha"));
        processing.SafeMessage.ShouldBe("The command status included an unavailable support detail.");
        processing.SafeMessageKey.ShouldBe("Tenants.Commands.Failure.UnavailableSupportDetail");
        processing.Status.ShouldBe(CommandStatus.Processing);
        processing.HasVerifiedCommandIdentity.ShouldBeTrue();

        IRenderedComponent<CreateTenantFlow> cut = SubmitCreate();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected));
        Dom(cut).ShouldNotContain(canary);
        Live(cut).ShouldNotContain(canary);
        JsonStrings(new { processing, snapshot = cut.Instance.Snapshot }).ShouldNotContain(canary);

        // Exercise a real allowed support copy in the same observer after the negative command render.
        IRenderedComponent<AuditEvidenceReceipt> receipt = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(SafeRow())));
        receipt.Find("[data-surface-testid='tenants-audit-receipt-copy']").Click();
        receipt.WaitForAssertion(() => write.Invocations.Count.ShouldBe(controlWrites + 1));
        Clipboard(write, controlWrites).ShouldNotContain(canary);
    }

    [Theory]
    [InlineData("202.555.0100")]
    [InlineData("202/555/0100")]
    [InlineData("tel:+1-202-555-0100")]
    [InlineData(" +1-202-555-0100")]
    [InlineData("person％40example.test")]
    [InlineData("person%2540example.test")]
    [InlineData("person%EF%BC%8540example.test")]
    [InlineData("user\u2503target")]
    [InlineData("user\u275Atarget")]
    [InlineData("user\u01C0target")]
    [InlineData("user\u23D0target")]
    [InlineData("user\u204Ftarget")]
    [InlineData("user\u061Btarget")]
    [InlineData("user%25C7%2580target")]
    [InlineData("ѕecret-canary")]
    [InlineData("tοken-canary")]
    [InlineData("ｓｅｃｒｅｔ-canary")]
    [InlineData("user%not-encoded")]
    public void D5UserShapesAreRedactedInMappedAndDirectAuditConsumers(string canary)
    {
        Services.AddLocalization();
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        JSRuntimeInvocationHandler write = module.SetupVoid("writeText", _ => true).SetVoidResult();
        ProveDisclosureObservers(canary, write);
        int controlWrites = write.Invocations.Count;

        TenantAuditEntry entry = Entry(canary, canary);
        TenantAuditRow mapped = TenantAuditRow.FromEntry(entry, ReadModelFreshnessState.Current);
        TenantAuditReceipt mappedReceipt = TenantAuditReceipt.FromRow(mapped);
        TenantAuditRow direct = SafeRow() with
        {
            ActorId = canary,
            Target = canary,
            Narrative = new(UserId: canary),
            ReferenceContext = $"userId: {canary}",
        };
        TenantAuditReceipt directReceipt = TenantAuditReceipt.FromRow(direct);
        JsonStrings(new { mapped, mappedReceipt, directReceipt }).ShouldNotContain(canary);
        mapped.Narrative.ShouldNotBeNull().UserId.ShouldBeNull();
        mappedReceipt.State.ShouldBe(TenantAuditReceiptState.Partial);
        directReceipt.State.ShouldBe(TenantAuditReceiptState.Partial);

        IRenderedComponent<AuditDataGrid> grid = Render<AuditDataGrid>(parameters => parameters
            .Add(component => component.Rows, new[] { direct }));
        grid.WaitForAssertion(() => grid.Find("[data-testid='tenants-audit-row-actor']").TextContent.ShouldBeEmpty());
        grid.Find("[data-testid='tenants-audit-row-target']").TextContent.ShouldBeEmpty();
        Dom(grid).ShouldNotContain(canary);
        Live(grid).ShouldNotContain(canary);
        grid.Find("[data-surface-testid='tenants-audit-copy-reference']").Click();
        grid.WaitForAssertion(() => write.Invocations.Count.ShouldBe(controlWrites + 1));

        // A direct Ready receipt must not approve a user target under the less restrictive tenant policy.
        TenantAuditReceipt forged = TenantAuditReceipt.FromRow(SafeRow()) with { Target = canary, Actor = "actor-user" };
        IRenderedComponent<AuditEvidenceReceipt> receipt = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, forged));
        Dom(receipt).ShouldNotContain(canary);
        Live(receipt).ShouldNotContain(canary);
        receipt.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").ShouldBeEmpty();
        receipt.Find("[data-testid='tenants-audit-receipt-copy-blocked']").GetAttribute("aria-live").ShouldBe("assertive");
        TenantAuditReceipt directReady = new("actor-user", canary, "tenant.alpha", "UserAddedToTenant",
            direct.Timestamp, ReadModelFreshnessState.Current, "event-visible-1", null, TenantAuditReceiptState.Ready);
        IRenderedComponent<AuditEvidenceReceipt> untypedReceipt = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, directReady));
        Dom(untypedReceipt).ShouldNotContain(canary);
        Live(untypedReceipt).ShouldNotContain(canary);
        untypedReceipt.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").ShouldBeEmpty();
        IRenderedComponent<AuditDataGrid> untypedGrid = Render<AuditDataGrid>(parameters => parameters
            .Add(component => component.Rows, new[] { direct with { Narrative = new(), ReferenceContext = string.Empty } }));
        untypedGrid.WaitForAssertion(() => untypedGrid.Find("[data-testid='tenants-audit-row-target']").TextContent.ShouldBeEmpty());
        Dom(untypedGrid).ShouldNotContain(canary);
        Clipboard(write, controlWrites).ShouldBe("event-visible-1");
        Clipboard(write, controlWrites).ShouldNotContain(canary);
    }

    [Theory]
    [InlineData("en", "  User/CaseSensitive.01  ", "tenant@alpha", "Event.Mixed-01")]
    [InlineData("fr", "équipe-α", "1234567", "+1-202-555-0100")]
    public void ApprovedLiteralsRemainExactInDomJsonAndClipboard(
        string culture, string user, string tenant, string reference)
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            UseCulture(culture);
            BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
            JSRuntimeInvocationHandler write = module.SetupVoid("writeText", _ => true).SetVoidResult();
            TenantAuditRow row = TenantAuditRow.FromEntry(Entry(user, user) with { TenantId = tenant, EventId = reference }, ReadModelFreshnessState.Current)
                with { Lifecycle = ProjectionLifecycleState.Current, Provenance = QueryResponseProvenance.ProjectionBacked };
            TenantAuditReceipt model = TenantAuditReceipt.FromRow(row);
            model.State.ShouldBe(TenantAuditReceiptState.Ready);
            model.Actor.ShouldBe(user);
            model.Target.ShouldBe(user);
            model.Scope.ShouldBe(tenant);
            model.AuditReference.ShouldBe(reference);
            JsonStrings(new { row, model }).ShouldContain(user);
            JsonStrings(new { row, model }).ShouldContain(tenant);
            IRenderedComponent<AuditEvidenceReceipt> receipt = Render<AuditEvidenceReceipt>(parameters => parameters
                .Add(component => component.Receipt, model));
            receipt.Find("[data-testid='tenants-audit-receipt-target'] dd").TextContent.ShouldBe(user);
            string summary = receipt.Find("[data-testid='tenants-audit-receipt-copy-value']").TextContent;
            summary.ShouldContain(user);
            summary.ShouldContain(tenant);
            summary.ShouldContain(reference);
            receipt.Find("[data-surface-testid='tenants-audit-receipt-copy']").Click();
            receipt.WaitForAssertion(() => write.Invocations.Count.ShouldBe(1));
            Clipboard(write).ShouldBe(summary);
            Live(receipt).ShouldNotContain(user);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void MalformedUnicodeFailsClosedWithoutExposingTheLiteralOrThrowing()
    {
        Services.AddLocalization();
        const string marker = "MalformedCanary";
        string value = marker + new string('\uD800', 1);
        TenantAuditRow row = TenantAuditRow.FromEntry(Entry(value, value), ReadModelFreshnessState.Current);
        TenantAuditReceipt receipt = TenantAuditReceipt.FromRow(row);
        JsonStrings(new { row, receipt }).ShouldNotContain(marker);
        IRenderedComponent<AuditDataGrid> grid = Render<AuditDataGrid>(parameters => parameters.Add(component => component.Rows, new[] { row }));
        Dom(grid).ShouldNotContain(marker);
        Live(grid).ShouldNotContain(marker);
        TenantAuditSupportSafety.SafeApprovedReference(value).ShouldBeNull();
    }

    private void ConfigureCreate(TenantCommandGateway gateway)
    {
        IUlidFactory ulids = Substitute.For<IUlidFactory>();
        ulids.NewUlid().Returns(MessageId);
        Services.AddSingleton(ulids);
        Services.AddSingleton<ITenantCommandGateway>(gateway);
    }

    private IRenderedComponent<CreateTenantFlow> SubmitCreate()
    {
        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(component => component.BaselineTenantAbsent, true));
        cut.InvokeAsync(() =>
        {
            cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
            cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
            cut.Find("form").Submit();
        }).GetAwaiter().GetResult();
        return cut;
    }

    private void UseCulture(string culture)
    {
        Services.AddLocalization();
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(provider => new CultureBoundTenantsLocalizer(
            provider.GetRequiredService<IStringLocalizerFactory>().Create(typeof(TenantsResources)),
            CultureInfo.GetCultureInfo(culture)));
    }

    private static TenantCommandGateway Gateway(string reason, CommandStatus status)
    {
        IEventStoreGatewayClient client = Substitute.For<IEventStoreGatewayClient>();
        client.SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SubmitCommandResponse(CorrelationId));
        IUlidFactory ulids = Substitute.For<IUlidFactory>();
        ulids.NewUlid().Returns(MessageId);
        string body = JsonSerializer.Serialize(new
        {
            messageId = MessageId,
            correlationId = CorrelationId,
            status = status.ToString(),
            statusCode = (int)status,
            tenantId = "system",
            domain = "tenants",
            aggregateId = "tenant.alpha",
            eventCount = 0,
            failureReason = reason,
        });
        return new(client, ulids, new HttpClient(new SupportSafetyCommandStatusHandler(body))
        {
            BaseAddress = new Uri("https://eventstore.example/"),
        });
    }

    private void ProveDisclosureObservers(string canary, JSRuntimeInvocationHandler write)
    {
        var disclosed = Render(builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "aria-live", "assertive");
            builder.AddContent(2, canary);
            builder.CloseElement();
        });
        Dom(disclosed).ShouldContain(canary);
        Live(disclosed).ShouldContain(canary);
        JsonStrings(new { detail = canary }).ShouldContain(canary);
        int before = write.Invocations.Count;
        IRenderedComponent<SupportSafeCopyButton> control = Render<SupportSafeCopyButton>(parameters => parameters
            .Add(component => component.Value, canary)
            .Add(component => component.ValueKind, SupportSafeCopyValueKind.ApprovedReference)
            .Add(component => component.IsApproved, true)
            .Add(component => component.AccessibleName, "Deliberate disclosure control")
            .Add(component => component.TestId, "tenants-disclosure-control"));
        control.Find("[data-surface-testid='tenants-disclosure-control']").Click();
        control.WaitForAssertion(() => write.Invocations.Count.ShouldBe(before + 1));
        Clipboard(write, before).ShouldContain(canary);
        disclosed.Dispose();
        control.Dispose();
    }

    private static string Dom<T>(IRenderedComponent<T> cut) where T : IComponent
        => WebUtility.HtmlDecode(cut.Markup);

    private static string Live<T>(IRenderedComponent<T> cut) where T : IComponent
        => string.Join("\n", cut.FindAll("[aria-live], [role='status'], [role='alert']").Select(element => element.TextContent));

    private static string Clipboard(JSRuntimeInvocationHandler write, int skip = 0)
        => string.Join("\n", write.Invocations.Skip(skip).Select(invocation => (string)invocation.Arguments[0]!));

    private static string JsonStrings(object evidence)
    {
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(evidence));
        return string.Join("\n", Strings(document.RootElement));
    }

    private static IEnumerable<string> Strings(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.String => [element.GetString()!],
            JsonValueKind.Object => element.EnumerateObject().SelectMany(property => Strings(property.Value)),
            JsonValueKind.Array => element.EnumerateArray().SelectMany(Strings),
            _ => [],
        };

    private static TenantAuditEntry Entry(string actor, string target)
        => new("event-visible-1", "UserAddedToTenant", AuditEventCategory.Access, actor,
            new DateTimeOffset(2026, 10, 13, 10, 0, 0, TimeSpan.Zero), "tenant.alpha",
            new Dictionary<string, string> { ["userId"] = target });

    private static TenantAuditRow SafeRow()
        => TenantAuditRow.FromEntry(Entry("actor-user", "target-user"), ReadModelFreshnessState.Current)
            with { Lifecycle = ProjectionLifecycleState.Current, Provenance = QueryResponseProvenance.ProjectionBacked };
}
