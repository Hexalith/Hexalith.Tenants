using System.Globalization;
using System.Net;
using System.Text;
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
using Hexalith.Tenants.UI.Components.Tenants.Members;
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

    /// <summary>Unknown backend details and near-miss machine codes in both supported cultures.</summary>
    public static TheoryData<string, string, string[]> UnknownBackendDetails
    {
        get
        {
            string[] details =
            [
                "ordinary-private-detail-Canary", "password=Canary123", "person-canary@example.test",
                "%73%65%63%72%65%74-Canary", "％73％65％63％72％65％74-Canary", "<Canary-équipe>",
                "ConcurrencyConflict Canary", " ConcurrencyConflict", "concurrencyconflict",
                "ConcurrencyConflict ", "BackpressureExceeded Canary", " BackpressureExceeded",
                "BackpressureExceeded ", "backpressureexceeded", "DomainRejected Canary",
                " DomainRejected", "DomainRejected ", "domainrejected",
            ];
            TheoryData<string, string, string[]> cases = [];
            foreach (string culture in new[] { "en", "fr" })
            {
                foreach (string detail in details)
                {
                    cases.Add(culture, detail, UnsafeRepresentations(detail));
                }
            }

            return cases;
        }
    }

    /// <summary>D5 unsafe user identifiers, including repeated percent encodings, in both supported cultures.</summary>
    public static TheoryData<string, string, string[]> UnsafeUserIdentifiers
    {
        get
        {
            string[] identifiers =
            [
                "202.555.0100", "202/555/0100", "tel:+1-202-555-0100", " +1-202-555-0100",
                "person％40example.test", "person%2540example.test", "person%EF%BC%8540example.test",
                "user\u2503target", "user\u275Atarget", "user\u01C0target", "user\u23D0target",
                "user\u204Ftarget", "user\u061Btarget", "user%25C7%2580target", "ѕecret-canary",
                "tοken-canary", "ｓｅｃｒｅｔ-canary", "user%not-encoded",
                "user★target", "user§target", "user☃target", "user\U0001F680target",
            ];
            TheoryData<string, string, string[]> cases = [];
            foreach (string culture in new[] { "en", "fr" })
            {
                foreach (string identifier in identifiers)
                {
                    string encoded = string.Concat(Encoding.UTF8.GetBytes(identifier)
                        .Select(octet => "%" + octet.ToString("X2", CultureInfo.InvariantCulture)));
                    string nested = encoded.Replace("%", "%25", StringComparison.Ordinal);
                    string fullwidth = nested.Replace("%", "％", StringComparison.Ordinal);
                    foreach (string value in new[] { identifier, encoded, nested, fullwidth })
                    {
                        cases.Add(culture, value, UnsafeRepresentations(identifier, value));
                    }
                }
            }

            return cases;
        }
    }

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
        IRenderedComponent<EditTenantMetadataFlow> cut = SubmitMetadata();
        IStringLocalizer<TenantsResources> localizer = Services.GetRequiredService<IStringLocalizer<TenantsResources>>();
        string key = $"Tenants.Commands.Failure.{code}";
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-edit-metadata-safe-message']").TextContent.ShouldBe(localizer[key].Value));
        Live(cut).ShouldContain(localizer["Tenants.EditMetadata.State.Rejected"].Value);
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
    [MemberData(nameof(UnknownBackendDetails))]
    public async Task UnknownBackendDetailsCannotReachSupportObservers(string culture, string canary, string[] unsafeRepresentations)
    {
        ArgumentNullException.ThrowIfNull(unsafeRepresentations);
        UseCulture(culture);
        ConfigureCreate(Gateway(canary, CommandStatus.Rejected));
        string expectedRejection = culture == "fr" ? "La commande a été rejetée." : "The command was rejected.";
        string expectedDetail = culture == "fr"
            ? "Le statut de la commande contenait un détail d’assistance indisponible."
            : "The command status included an unavailable support detail.";
        string expectedCopied = culture == "fr" ? "Copié." : "Copied.";
        string expectedMetadataRejection = culture == "fr"
            ? "Commande de mise a jour des metadonnees rejetee."
            : "Metadata update command rejected.";
        AssertLocalizedResource("Tenants.Commands.Failure.DomainRejected", expectedRejection);
        AssertLocalizedResource("Tenants.Commands.Failure.UnavailableSupportDetail", expectedDetail);
        AssertLocalizedResource("Tenants.Copy.Feedback.Copied", expectedCopied);
        AssertLocalizedResource("Tenants.EditMetadata.State.Rejected", expectedMetadataRejection);
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        JSRuntimeInvocationHandler write = module.SetupVoid("writeText", _ => true).SetVoidResult();
        foreach (string representation in unsafeRepresentations)
        {
            ProveDisclosureObservers(representation, write);
        }
        int controlWrites = write.Invocations.Count;

        TenantCommandStatusResult processing = await Gateway(canary, CommandStatus.Processing)
            .GetStatusAsync(new(MessageId, CorrelationId, "tenant.alpha"));
        processing.SafeMessage.ShouldBe("The command status included an unavailable support detail.");
        processing.SafeMessageKey.ShouldBe("Tenants.Commands.Failure.UnavailableSupportDetail");
        processing.Status.ShouldBe(CommandStatus.Processing);
        processing.HasVerifiedCommandIdentity.ShouldBeTrue();
        processing.EventCount.ShouldBe(0);
        processing.IsPending.ShouldBeFalse();
        processing.IsRetryableFailure.ShouldBeFalse();
        processing.RejectionCode.ShouldBeNull();
        processing.CommittedEventSequence.ShouldBeNull();

        IRenderedComponent<CreateTenantFlow> cut = SubmitCreate();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected));
        cut.Find("[data-testid='tenants-create-safe-message']").TextContent.ShouldBe(expectedRejection);
        cut.Instance.Snapshot.SafeMessageKey.ShouldBe("Tenants.Commands.Failure.DomainRejected");
        AssertNoUnsafeRepresentations(Dom(cut), unsafeRepresentations);
        Live(cut).ShouldContain(expectedRejection);
        AssertNoUnsafeRepresentations(Live(cut), unsafeRepresentations);
        IRenderedComponent<EditTenantMetadataFlow> metadata = SubmitMetadata();
        metadata.WaitForAssertion(() => metadata.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected));
        metadata.Find("[data-testid='tenants-edit-metadata-safe-message']").TextContent.ShouldBe(expectedRejection);
        metadata.Instance.Snapshot.SafeMessageKey.ShouldBe("Tenants.Commands.Failure.DomainRejected");
        AssertNoUnsafeRepresentations(Dom(metadata), unsafeRepresentations);
        Live(metadata).ShouldContain(expectedMetadataRejection);
        AssertNoUnsafeRepresentations(Live(metadata), unsafeRepresentations);
        // Exclude only the three fixed resource keys asserted above; scan all remaining evidence without case sensitivity.
        AssertNoUnsafeRepresentations(JsonStrings(new
        {
            processing = processing with { SafeMessageKey = null },
            snapshot = cut.Instance.Snapshot with { SafeMessageKey = null },
            metadata = metadata.Instance.Snapshot with { SafeMessageKey = null },
        }), unsafeRepresentations);

        // Exercise a real allowed support copy in the same observer after the negative command render.
        IRenderedComponent<AuditEvidenceReceipt> receipt = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(SafeRow())));
        receipt.Find("[data-surface-testid='tenants-audit-receipt-copy']").Click();
        receipt.WaitForAssertion(() => write.Invocations.Count.ShouldBe(controlWrites + 1));
        receipt.WaitForAssertion(() => receipt.Find("[data-testid='tenants-audit-receipt-copy-feedback']").TextContent.ShouldBe(expectedCopied));
        Live(receipt).ShouldContain(expectedCopied);
        AssertNoUnsafeRepresentations(Dom(receipt), unsafeRepresentations);
        AssertNoUnsafeRepresentations(JsonStrings(receipt.Instance.Receipt!), unsafeRepresentations);
        AssertNoUnsafeRepresentations(Live(receipt), unsafeRepresentations);
        AssertNoUnsafeRepresentations(Clipboard(write, controlWrites), unsafeRepresentations);
    }

    [Theory]
    [MemberData(nameof(UnsafeUserIdentifiers))]
    public void D5UserShapesAreRedactedInMappedAndDirectAuditConsumers(string culture, string canary, string[] unsafeRepresentations)
    {
        ArgumentNullException.ThrowIfNull(unsafeRepresentations);
        UseCulture(culture);
        string expectedPartial = culture == "fr"
            ? "La preuve d’audit est partielle. Le reçu ne peut pas citer une preuve complète."
            : "Audit evidence is partial. The receipt cannot cite a complete proof.";
        string expectedBlocked = culture == "fr"
            ? "Le résumé du reçu ne peut pas être copié sans risque. Continuez en lecture seule ou contactez l’assistance."
            : "The receipt summary cannot be copied safely. Continue read-only or contact support.";
        string expectedCopied = culture == "fr" ? "Copié." : "Copied.";
        AssertLocalizedResource("Tenants.Audit.Receipt.State.Partial", expectedPartial);
        AssertLocalizedResource("Tenants.Audit.Receipt.CopyBlocked", expectedBlocked);
        AssertLocalizedResource("Tenants.Copy.Feedback.Copied", expectedCopied);
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        JSRuntimeInvocationHandler write = module.SetupVoid("writeText", _ => true).SetVoidResult();
        foreach (string representation in unsafeRepresentations)
        {
            ProveDisclosureObservers(representation, write);
        }
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
        AssertNoUnsafeRepresentations(JsonStrings(new { mapped, mappedReceipt, directReceipt }), unsafeRepresentations);
        mapped.Narrative.ShouldNotBeNull().UserId.ShouldBeNull();
        mappedReceipt.State.ShouldBe(TenantAuditReceiptState.Partial);
        directReceipt.State.ShouldBe(TenantAuditReceiptState.Partial);

        IRenderedComponent<AuditDataGrid> mappedGrid = Render<AuditDataGrid>(parameters => parameters
            .Add(component => component.Rows, new[] { mapped }));
        mappedGrid.WaitForAssertion(() => mappedGrid.Find("[data-testid='tenants-audit-row-actor']").TextContent.ShouldBeEmpty());
        mappedGrid.Find("[data-testid='tenants-audit-row-target']").TextContent.ShouldBe("tenant.alpha");
        AssertNoUnsafeRepresentations(Dom(mappedGrid), unsafeRepresentations);
        AssertNoUnsafeRepresentations(Live(mappedGrid), unsafeRepresentations);
        foreach (TenantAuditReceipt partial in new[] { mappedReceipt, directReceipt })
        {
            IRenderedComponent<AuditEvidenceReceipt> partialReceipt = Render<AuditEvidenceReceipt>(parameters => parameters
                .Add(component => component.Receipt, partial));
            partialReceipt.Find("[data-testid='tenants-audit-receipt-state']").TextContent.Trim().ShouldEndWith(expectedPartial);
            partialReceipt.Find("[data-testid='tenants-audit-receipt-actor'] dd").TextContent.ShouldBe("-");
            partialReceipt.Find("[data-testid='tenants-audit-receipt-target'] dd").TextContent
                .ShouldBe(partial == mappedReceipt ? "tenant.alpha" : "-");
            partialReceipt.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").ShouldBeEmpty();
            Live(partialReceipt).ShouldContain(expectedPartial);
            AssertNoUnsafeRepresentations(Dom(partialReceipt), unsafeRepresentations);
            AssertNoUnsafeRepresentations(Live(partialReceipt), unsafeRepresentations);
        }

        IRenderedComponent<AuditDataGrid> grid = Render<AuditDataGrid>(parameters => parameters
            .Add(component => component.Rows, new[] { direct }));
        grid.WaitForAssertion(() => grid.Find("[data-testid='tenants-audit-row-actor']").TextContent.ShouldBeEmpty());
        grid.Find("[data-testid='tenants-audit-row-target']").TextContent.ShouldBeEmpty();
        AssertNoUnsafeRepresentations(Dom(grid), unsafeRepresentations);
        AssertNoUnsafeRepresentations(Live(grid), unsafeRepresentations);
        grid.Find("[data-surface-testid='tenants-audit-copy-reference']").Click();
        grid.WaitForAssertion(() => write.Invocations.Count.ShouldBe(controlWrites + 1));
        grid.WaitForAssertion(() => grid.Find("[data-testid='tenants-audit-copy-reference-feedback']").TextContent.ShouldBe(expectedCopied));
        Live(grid).ShouldContain(expectedCopied);
        AssertNoUnsafeRepresentations(Live(grid), unsafeRepresentations);

        // A direct Ready receipt must not approve a user target under the less restrictive tenant policy.
        TenantAuditReceipt forged = TenantAuditReceipt.FromRow(SafeRow()) with { Target = canary, Actor = "actor-user" };
        IRenderedComponent<AuditEvidenceReceipt> receipt = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, forged));
        AssertNoUnsafeRepresentations(Dom(receipt), unsafeRepresentations);
        AssertNoUnsafeRepresentations(Live(receipt), unsafeRepresentations);
        receipt.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").ShouldBeEmpty();
        receipt.Find("[data-testid='tenants-audit-receipt-target'] dd").TextContent.ShouldBe("-");
        receipt.Find("[data-testid='tenants-audit-receipt-copy-blocked']").GetAttribute("aria-live").ShouldBe("assertive");
        receipt.Find("[data-testid='tenants-audit-receipt-copy-blocked']").TextContent.Trim().ShouldBe(expectedBlocked);
        Live(receipt).ShouldContain(expectedBlocked);
        TenantAuditReceipt directReady = new("actor-user", canary, "tenant.alpha", "UserAddedToTenant",
            direct.Timestamp, ReadModelFreshnessState.Current, "event-visible-1", null, TenantAuditReceiptState.Ready);
        IRenderedComponent<AuditEvidenceReceipt> untypedReceipt = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, directReady));
        AssertNoUnsafeRepresentations(Dom(untypedReceipt), unsafeRepresentations);
        AssertNoUnsafeRepresentations(Live(untypedReceipt), unsafeRepresentations);
        untypedReceipt.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").ShouldBeEmpty();
        untypedReceipt.Find("[data-testid='tenants-audit-receipt-target'] dd").TextContent.ShouldBe("-");
        untypedReceipt.Find("[data-testid='tenants-audit-receipt-copy-blocked']").TextContent.Trim().ShouldBe(expectedBlocked);
        Live(untypedReceipt).ShouldContain(expectedBlocked);
        IRenderedComponent<AuditDataGrid> untypedGrid = Render<AuditDataGrid>(parameters => parameters
            .Add(component => component.Rows, new[] { direct with { Narrative = new(), ReferenceContext = string.Empty } }));
        untypedGrid.WaitForAssertion(() => untypedGrid.Find("[data-testid='tenants-audit-row-target']").TextContent.ShouldBeEmpty());
        AssertNoUnsafeRepresentations(Dom(untypedGrid), unsafeRepresentations);
        AssertNoUnsafeRepresentations(Live(untypedGrid), unsafeRepresentations);
        write.Invocations.Count.ShouldBe(controlWrites + 1);
        Clipboard(write, controlWrites).ShouldBe("event-visible-1");
        AssertNoUnsafeRepresentations(Clipboard(write, controlWrites), unsafeRepresentations);
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


    /// <summary>Whole-message command keys are resolved from shipped resources on both observer surfaces.</summary>
    public static TheoryData<string, string, string> LocalizedCommandMessages
    {
        get
        {
            TheoryData<string, string, string> cases = [];
            foreach (string culture in new[] { "en", "fr" })
            {
                foreach (string code in new[] { "TenantAlreadyExists", "UserAlreadyInTenant", "UserNotInTenant", "RoleEscalation", "InsufficientPermissions", "TenantDisabled", "TenantNotFound", "ConfigurationLimitExceeded", "ConfigurationKeyNotFound", "TenantLifecycleStateAlreadySet", "GlobalAdministratorAlreadyExists", "LastGlobalAdministrator", "GlobalAdministratorNotFound" })
                {
                    cases.Add(culture, code, "Tenants.Commands.Rejection." + code);
                }

                cases.Add(culture, "pending", "Tenants.Commands.Status.Pending");
                cases.Add(culture, "transient", "Tenants.Commands.Status.RetryableFailure");
                cases.Add(culture, "request-rejected", "Tenants.Commands.Status.RequestRejected");
                cases.Add(culture, "malformed", "Tenants.Commands.Status.ResponseUnavailable");
                cases.Add(culture, "unknown", "Tenants.Commands.Status.ResponseUnavailable");
                cases.Add(culture, "mismatch", "Tenants.Commands.Status.TrackingMismatch");
                cases.Add(culture, "submission-invalid", "Tenants.Create.Submission.Invalid");
                cases.Add(culture, "submission-unavailable", "Tenants.Create.Submission.Unavailable");
                cases.Add(culture, "submission-failed", "Tenants.Create.Submission.Failed");
                cases.Add(culture, "unavailable", "Tenants.Create.Unavailable.GatewayConfiguration");
            }

            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(LocalizedCommandMessages))]
    public void SharedCommandMessagesReachDomAndLiveRegionsInTheSelectedCulture(string culture, string scenario, string expectedKey)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        UseCulture(culture);
        IEventStoreGatewayClient client = Substitute.For<IEventStoreGatewayClient>();
        if (scenario.StartsWith("submission-", StringComparison.Ordinal))
        {
            int code = scenario switch { "submission-invalid" => 400, "submission-unavailable" => 503, _ => 418 };
            client.SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
                .Returns<SubmitCommandResponse>(_ => throw new EventStoreGatewayException(code, "backend-private-Canary"));
        }
        else
        {
            client.SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
                .Returns(new SubmitCommandResponse(CorrelationId));
        }

        IUlidFactory ulids = Substitute.For<IUlidFactory>();
        ulids.NewUlid().Returns(MessageId);
        string body = JsonSerializer.Serialize(new
        {
            messageId = scenario == "mismatch" ? "backend-private-Canary" : MessageId,
            correlationId = CorrelationId, status = scenario == "unknown" ? "999" : nameof(CommandStatus.Rejected),
            statusCode = scenario == "unknown" ? 999 : (int)CommandStatus.Rejected,
            tenantId = "system", domain = "tenants", aggregateId = "tenant.alpha", eventCount = 0,
            rejectionEventType = scenario, failureReason = "backend-private-Canary",
        });
        if (scenario == "malformed")
        {
            body = "{ backend-private-Canary";
        }

        HttpStatusCode httpStatus = scenario switch
        {
            "pending" => HttpStatusCode.NotFound,
            "transient" => HttpStatusCode.ServiceUnavailable,
            "request-rejected" => HttpStatusCode.Forbidden,
            _ => HttpStatusCode.OK,
        };
        ITenantCommandGateway gateway = scenario == "unavailable" ? new UnavailableTenantCommandGateway()
            : new TenantCommandGateway(client, ulids, new HttpClient(new SupportSafetyCommandStatusHandler(body, httpStatus))
            { BaseAddress = new Uri("https://eventstore.example/") });
        ConfigureCreate(gateway);
        IRenderedComponent<CreateTenantFlow> cut = SubmitCreate();
        IStringLocalizer<TenantsResources> localizer = Services.GetRequiredService<IStringLocalizer<TenantsResources>>();
        string expected = localizer[expectedKey].Value;
        localizer[expectedKey].ResourceNotFound.ShouldBeFalse();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-create-safe-message']").TextContent.ShouldBe(expected));
        Live(cut).ShouldContain(expected);
        Dom(cut).ShouldNotContain(expectedKey);
        Live(cut).ShouldNotContain(expectedKey);
        Dom(cut).ShouldNotContain("backend-private-Canary");
        Live(cut).ShouldNotContain("backend-private-Canary");
        cut.Instance.Snapshot.HasCommandEventEvidence.ShouldBeFalse();
        cut.Instance.Snapshot.AuditState.ShouldNotBe(TenantCommandAuditState.AuditAvailable);
        if (culture == "fr")
        {
            CultureBoundTenantsLocalizer english = new(Services.GetRequiredService<IStringLocalizerFactory>()
                .Create(typeof(TenantsResources)), CultureInfo.GetCultureInfo("en"));
            expected.ShouldNotBe(english[expectedKey].Value);
            Dom(cut).ShouldNotContain(english[expectedKey].Value);
            Live(cut).ShouldNotContain(english[expectedKey].Value);
        }
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public void FreshStatusReplacesTheOldRenderedKeyAndProgressClearsIt(string culture)
    {
        UseCulture(culture);
        ITenantCommandGateway gateway = Substitute.For<ITenantCommandGateway>();
        gateway.SupportsCommandStatusLookup.Returns(true);
        gateway.CreateTenantAsync(Arg.Any<Hexalith.Tenants.Contracts.Commands.CreateTenant>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(TenantCommandSubmissionResult.Accepted(MessageId, CorrelationId));
        gateway.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>()).Returns(
            TenantCommandStatusResult.RetryableFailureWithKey("Tenants.Commands.Status.RetryableFailure"),
            new TenantCommandStatusResult(CommandStatus.PublishFailed, SafeMessage: "Fixed safe English compatibility fallback",
                SafeMessageKey: "Tenants.Commands.Failure.PublishFailed", HasVerifiedCommandIdentity: true),
            new TenantCommandStatusResult(CommandStatus.Processing, HasVerifiedCommandIdentity: true));
        ConfigureCreate(gateway);
        IRenderedComponent<CreateTenantFlow> cut = SubmitCreate();
        IStringLocalizer<TenantsResources> localizer = Services.GetRequiredService<IStringLocalizer<TenantsResources>>();
        string previous = localizer["Tenants.Commands.Status.RetryableFailure"].Value;
        string current = localizer["Tenants.Commands.Failure.PublishFailed"].Value;
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-create-safe-message']").TextContent.ShouldBe(previous));
        cut.Find("[data-testid='tenants-create-refresh']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-create-safe-message']").TextContent.ShouldBe(current));
        Live(cut).ShouldContain(current);
        Dom(cut).ShouldNotContain(previous);
        Dom(cut).ShouldNotContain("Fixed safe English compatibility fallback");
        cut.Find("[data-testid='tenants-create-refresh']").Click();
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='tenants-create-safe-message']").ShouldBeEmpty());
        Live(cut).ShouldNotContain(current);
        cut.Instance.Snapshot.SafeMessageKey.ShouldBeNull();
        cut.Instance.Snapshot.SafeMessage.ShouldBeNull();
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        cut.Instance.Snapshot.HasCommandEventEvidence.ShouldBeFalse();
        cut.Instance.Snapshot.MessageId.ShouldBe(MessageId);
        cut.Instance.Snapshot.CorrelationId.ShouldBe(CorrelationId);
    }


    [Theory]
    [InlineData("en", false)]
    [InlineData("fr", false)]
    [InlineData("en", true)]
    [InlineData("fr", true)]
    public void UnknownSnapshotStatusRendersItsFreshKeyOrLocalizedFallback(string culture, bool hasFreshKey)
    {
        UseCulture(culture);
        ITenantCommandGateway gateway = Substitute.For<ITenantCommandGateway>();
        gateway.SupportsCommandStatusLookup.Returns(true);
        gateway.CreateTenantAsync(Arg.Any<Hexalith.Tenants.Contracts.Commands.CreateTenant>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(TenantCommandSubmissionResult.Accepted(MessageId, CorrelationId));
        gateway.GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>()).Returns(
            new TenantCommandStatusResult((CommandStatus)999, SafeMessage: "backend-private-Canary", HasVerifiedCommandIdentity: true,
                SafeMessageKey: hasFreshKey ? "Tenants.Commands.Status.ResponseUnavailable" : null));
        ConfigureCreate(gateway);
        IRenderedComponent<CreateTenantFlow> cut = SubmitCreate();
        IStringLocalizer<TenantsResources> localizer = Services.GetRequiredService<IStringLocalizer<TenantsResources>>();
        string key = hasFreshKey ? "Tenants.Commands.Status.ResponseUnavailable" : "Tenants.Commands.Status.Unverified";
        string expected = localizer[key].Value;
        localizer[key].ResourceNotFound.ShouldBeFalse();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-create-safe-message']").TextContent.ShouldBe(expected));
        Live(cut).ShouldContain(expected);
        Dom(cut).ShouldNotContain(key);
        Dom(cut).ShouldNotContain("backend-private-Canary");
        Live(cut).ShouldNotContain("backend-private-Canary");
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        cut.Instance.Snapshot.HasCommandEventEvidence.ShouldBeFalse();
        cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditUnavailable);
    }

    [Theory]
    [InlineData("en", "The requested role was already applied.")]
    [InlineData("fr", "Le rôle demandé était déjà appliqué.")]
    public void ZeroEventRoleCompletionShowsLocalizedExplanationAndAnnouncesExactStateLabel(string culture, string expectedExplanation)
    {
        UseCulture(culture);
        ConfigureCreate(Gateway("", CommandStatus.Completed));
        Hexalith.Tenants.Contracts.Queries.TenantDetail detail = new("tenant.alpha", "Alpha", "Description",
            TenantStatus.Active, [new TenantMember("owner-user", TenantRole.TenantOwner), new TenantMember("reader-user", TenantRole.TenantReader)],
            new Dictionary<string, string>(), new DateTimeOffset(2026, 10, 13, 10, 0, 0, TimeSpan.Zero));
        IRenderedComponent<ChangeTenantMemberRoleFlow> cut = Render<ChangeTenantMemberRoleFlow>(parameters => parameters
            .Add(p => p.IsAuthorized, true)
            .Add(p => p.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(p => p.AuthorizationReflectionProvider, _ => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
            .Add(p => p.Detail, detail)
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionVersion, "tenant-sequence:1"));
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-change-role-new-role", nameof(TenantRole.TenantContributor));
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-change-role-safe-message']").TextContent.ShouldBe(expectedExplanation));
        IStringLocalizer<TenantsResources> localizer = Services.GetRequiredService<IStringLocalizer<TenantsResources>>();
        localizer["Tenants.ChangeRole.State.AlreadyApplied"].ResourceNotFound.ShouldBeFalse();
        string announcement = cut.Find("[data-testid='tenants-change-role-live-region']").TextContent.Trim();
        announcement.ShouldBe(localizer["Tenants.ChangeRole.State.AlreadyApplied"].Value);
        announcement.ShouldNotBe(expectedExplanation);
        Dom(cut).ShouldNotContain("Tenants.ChangeRole.Status.AlreadyApplied");
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        cut.Instance.Snapshot.HasCommandEventEvidence.ShouldBeFalse();
        cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.NotStarted);
        cut.Instance.Snapshot.MessageId.ShouldBe(MessageId);
        cut.Instance.Snapshot.CorrelationId.ShouldBe(CorrelationId);
    }


    /// <summary>Exercises operation-specific submissions and neutral status rejection copy in every member flow.</summary>
    [Theory]
    [InlineData("en", "add", true, "Tenants.AddMember.Rejection.TenantDisabled")]
    [InlineData("fr", "add", true, "Tenants.AddMember.Rejection.TenantDisabled")]
    [InlineData("en", "change", true, "Tenants.ChangeRole.Rejection.TenantDisabled")]
    [InlineData("fr", "change", true, "Tenants.ChangeRole.Rejection.TenantDisabled")]
    [InlineData("en", "remove", true, "Tenants.RemoveMember.Rejection.TenantDisabled")]
    [InlineData("fr", "remove", true, "Tenants.RemoveMember.Rejection.TenantDisabled")]
    [InlineData("en", "add", false, "Tenants.Commands.Rejection.TenantDisabled")]
    [InlineData("fr", "add", false, "Tenants.Commands.Rejection.TenantDisabled")]
    [InlineData("en", "change", false, "Tenants.Commands.Rejection.TenantDisabled")]
    [InlineData("fr", "change", false, "Tenants.Commands.Rejection.TenantDisabled")]
    [InlineData("en", "remove", false, "Tenants.Commands.Rejection.TenantDisabled")]
    [InlineData("fr", "remove", false, "Tenants.Commands.Rejection.TenantDisabled")]
    public void MemberFlowsRenderRealGatewayRejectionsAndAnnounceLocalizedState(
        string culture, string operation, bool submissionRejection, string expectedKey)
    {
        UseCulture(culture);
        IEventStoreGatewayClient client = Substitute.For<IEventStoreGatewayClient>();
        if (submissionRejection)
        {
            client.SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
                .Returns<SubmitCommandResponse>(_ => throw new EventStoreGatewayException(
                    (int)HttpStatusCode.Conflict, "TenantDisabled", detail: "backend-private-Canary"));
        }
        else
        {
            client.SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
                .Returns(new SubmitCommandResponse(CorrelationId));
        }

        IUlidFactory ulids = Substitute.For<IUlidFactory>();
        ulids.NewUlid().Returns(MessageId);
        string body = JsonSerializer.Serialize(new
        {
            messageId = MessageId, correlationId = CorrelationId,
            status = nameof(CommandStatus.Rejected), statusCode = (int)CommandStatus.Rejected,
            tenantId = "system", domain = "tenants", aggregateId = "tenant.alpha", eventCount = 0,
            rejectionEventType = "TenantDisabled", failureReason = "backend-private-Canary",
        });
        ConfigureCreate(new TenantCommandGateway(client, ulids,
            new HttpClient(new SupportSafetyCommandStatusHandler(body)) { BaseAddress = new Uri("https://eventstore.example/") }));
        Hexalith.Tenants.Contracts.Queries.TenantDetail detail = new("tenant.alpha", "Alpha", "Description",
            TenantStatus.Active, [new TenantMember("owner-user", TenantRole.TenantOwner), new TenantMember("reader-user", TenantRole.TenantReader)],
            new Dictionary<string, string>(), new DateTimeOffset(2026, 10, 13, 10, 0, 0, TimeSpan.Zero));
        if (operation == "add")
        {
            IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
                .Add(p => p.IsAuthorized, true)
                .Add(p => p.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
                .Add(p => p.AuthorizationReflectionProvider, _ => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
                .Add(p => p.Detail, detail)
                .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
                .Add(p => p.Freshness, ReadModelFreshnessState.Current)
                .Add(p => p.ProjectionVersion, "tenant-sequence:1"));
            cut.Find("[data-testid='tenants-add-member-user-id']").Change("new-user");
            FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantContributor));
            cut.Find("form").Submit();
            AssertLocalizedMemberRejection(cut, "add-member", "Tenants.AddMember.State.Rejected", culture, expectedKey);
            cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected);
            cut.Instance.Snapshot.HasCommandEventEvidence.ShouldBeFalse();
        }
        else if (operation == "change")
        {
            IRenderedComponent<ChangeTenantMemberRoleFlow> cut = Render<ChangeTenantMemberRoleFlow>(parameters => parameters
                .Add(p => p.IsAuthorized, true)
                .Add(p => p.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
                .Add(p => p.AuthorizationReflectionProvider, _ => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
                .Add(p => p.Detail, detail)
                .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
                .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
                .Add(p => p.Freshness, ReadModelFreshnessState.Current)
                .Add(p => p.ProjectionVersion, "tenant-sequence:1"));
            FluentSelectInterop.ChangeFluentSelect(cut, "tenants-change-role-new-role", nameof(TenantRole.TenantContributor));
            cut.Find("form").Submit();
            AssertLocalizedMemberRejection(cut, "change-role", "Tenants.ChangeRole.State.Rejected", culture, expectedKey);
            cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected);
            cut.Instance.Snapshot.HasCommandEventEvidence.ShouldBeFalse();
        }
        else
        {
            var viewport = new TenantHighImpactViewportObservation();
            viewport.Observe(Hexalith.FrontComposer.Shell.State.Navigation.ViewportTier.Desktop);
            Services.AddSingleton(viewport);
            Services.AddSingleton(Substitute.For<ITenantQueryGateway>());
            IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
                .Add(p => p.IsAuthorized, true)
                .Add(p => p.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
                .Add(p => p.AuthorizationReflectionProvider, _ => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
                .Add(p => p.AuditProofCapabilityAvailable, true)
                .Add(p => p.Detail, detail)
                .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
                .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
                .Add(p => p.Freshness, ReadModelFreshnessState.Current)
                .Add(p => p.ProjectionVersion, "tenant-sequence:1"));
            cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("reader-user");
            cut.Find("form").Submit();
            AssertLocalizedMemberRejection(cut, "remove-member", "Tenants.RemoveMember.State.Rejected", culture, expectedKey);
            cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected);
            cut.Instance.Snapshot.HasCommandEventEvidence.ShouldBeFalse();
        }
    }

    private void AssertLocalizedMemberRejection<T>(IRenderedComponent<T> cut, string prefix,
        string stateKey, string culture, string expectedKey) where T : IComponent
    {
        IStringLocalizer<TenantsResources> localizer = Services.GetRequiredService<IStringLocalizer<TenantsResources>>();
        localizer[expectedKey].ResourceNotFound.ShouldBeFalse();
        localizer[stateKey].ResourceNotFound.ShouldBeFalse();
        string expected = localizer[expectedKey].Value;
        cut.WaitForAssertion(() => cut.Find($"[data-testid='tenants-{prefix}-safe-message']").TextContent.ShouldBe(expected));
        cut.Find($"[data-testid='tenants-{prefix}-live-region']").TextContent.Trim().ShouldBe(localizer[stateKey].Value);
        Dom(cut).ShouldNotContain(expectedKey);
        Dom(cut).ShouldNotContain("backend-private-Canary");
        Live(cut).ShouldNotContain("backend-private-Canary");
        if (culture == "fr")
        {
            CultureBoundTenantsLocalizer english = new(Services.GetRequiredService<IStringLocalizerFactory>()
                .Create(typeof(TenantsResources)), CultureInfo.GetCultureInfo("en"));
            expected.ShouldNotBe(english[expectedKey].Value);
            Dom(cut).ShouldNotContain(english[expectedKey].Value);
            Live(cut).ShouldNotContain(english[stateKey].Value);
        }
    }

    private void ConfigureCreate(ITenantCommandGateway gateway)
    {
        IUlidFactory ulids = Substitute.For<IUlidFactory>();
        ulids.NewUlid().Returns(MessageId);
        Services.AddSingleton(ulids);
        Services.AddSingleton<ITenantCommandGateway>(gateway);
    }

    private IRenderedComponent<CreateTenantFlow> SubmitCreate()
    {
        IRenderedComponent<CreateTenantFlow> cut = Render<CreateTenantFlow>(parameters => parameters
            .Add(p => p.IsAuthorized, true)
            .Add(p => p.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(p => p.AuthorizationReflectionProvider, _ => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
            .Add(component => component.BaselineTenantAbsent, true));
        cut.InvokeAsync(() =>
        {
            cut.Find("[data-testid='tenants-create-tenant-id']").Change("tenant.alpha");
            cut.Find("[data-testid='tenants-create-name']").Change("Alpha");
            cut.Find("form").Submit();
        }).GetAwaiter().GetResult();
        return cut;
    }

    private IRenderedComponent<EditTenantMetadataFlow> SubmitMetadata()
    {
        Hexalith.Tenants.Contracts.Queries.TenantDetail detail = new("tenant.alpha", "Alpha", "Description",
            TenantStatus.Active, [new TenantMember("owner-user", TenantRole.TenantOwner)],
            new Dictionary<string, string>(), new DateTimeOffset(2026, 10, 13, 10, 0, 0, TimeSpan.Zero));
        IRenderedComponent<EditTenantMetadataFlow> cut = Render<CascadingValue<string>>(parameters => parameters
            .Add(component => component.Name, "CurrentTenantCallerScope")
            .Add(component => component.Value, "test-caller")
            .AddChildContent<EditTenantMetadataFlow>(child => child
            .Add(p => p.IsAuthorized, true)
            .Add(p => p.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(p => p.AuthorizationReflectionProvider, _ => Task.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized))
                .Add(component => component.Detail, detail)
                .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
                .Add(component => component.Freshness, ReadModelFreshnessState.Current)
                .Add(component => component.Lifecycle, ProjectionLifecycleState.Current)
                .Add(component => component.ProjectionVersion, "tenant-sequence:1")))
            .FindComponent<EditTenantMetadataFlow>();
        cut.Find("[data-testid='tenants-edit-metadata-open']").Click();
        cut.Find("[data-testid='tenants-edit-metadata-name']").Change("Alpha updated");
        cut.Find("form").Submit();
        return cut;
    }

    private void AssertLocalizedResource(string key, string expected)
    {
        IStringLocalizer<TenantsResources> localizer = Services.GetRequiredService<IStringLocalizer<TenantsResources>>();
        localizer[key].ResourceNotFound.ShouldBeFalse();
        localizer[key].Value.ShouldBe(expected);
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

    private static string[] UnsafeRepresentations(params string[] values)
    {
        HashSet<string> representations = new(StringComparer.Ordinal);
        foreach (string value in values)
        {
            string candidate = value;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                representations.Add(candidate);
                candidate = candidate.Normalize(NormalizationForm.FormKC);
                representations.Add(candidate);
                string decoded = Uri.UnescapeDataString(candidate);
                if (string.Equals(decoded, candidate, StringComparison.Ordinal))
                {
                    break;
                }

                candidate = decoded;
            }

            representations.Add(candidate);
        }

        return [.. representations];
    }

    private static void AssertNoUnsafeRepresentations(string observed, IEnumerable<string> unsafeRepresentations)
    {
        foreach (string representation in unsafeRepresentations)
        {
            observed.ShouldNotContain(representation, Case.Insensitive);
        }
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
