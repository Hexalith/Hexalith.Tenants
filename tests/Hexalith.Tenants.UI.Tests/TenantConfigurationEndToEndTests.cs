using System.Globalization;
using System.Security.Claims;
using System.Text;

using Bunit;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.Memories.Client.Rest;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Configuration;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.EventStore.Client.Projections;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

public sealed class TenantConfigurationEndToEndTests : BunitContext
{
    [Fact]
    public void Authenticated_circuit_policy_filters_raw_configuration_before_rendered_dom_and_accessibility_state()
    {
        const string tenantId = "tenant.alpha";
        const string subject = "operator-user";
        TenantDetail rawDetail = new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            TenantStatus.Active,
            [new TenantMember(subject, TenantRole.TenantReader)],
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["billing.mode"] = "visible-literal",
                ["billing.secret"] = "hidden-undefined-value",
                ["private.mode"] = "hidden-namespace-value",
            },
            DateTimeOffset.Parse("2026-07-22T08:00:00Z", CultureInfo.InvariantCulture));

        ClaimsIdentity identity = new(
            [
                new Claim("sub", subject),
                new Claim("global_admin", "false"),
            ],
            authenticationType: "test");
        IHttpContextAccessor httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity),
            },
        };
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        userContext.UserId.Returns(subject);
        userContext.TenantId.Returns("system");

        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(
                """
                {
                  "Tenants": {
                    "ConfigurationReadPolicy": {
                      "PrefixGrants": [
                        { "TenantId": "tenant.alpha", "Subject": "operator-user", "Prefix": "billing" }
                      ],
                      "DisplaySafe": ["billing.mode", "private.mode"]
                    }
                  }
                }
                """)))
            .Build();
        var circuitServicesAccessor = new CircuitServicesAccessor
        {
            Services = new ServiceCollection()
                .AddSingleton<AuthenticationStateProvider>(
                    new StubAuthenticationStateProvider(new ClaimsPrincipal(identity)))
                .BuildServiceProvider(),
        };
        TenantConfigurationPrincipalResolver principalResolver = new(circuitServicesAccessor, userContext);
        ITenantCommandGateway commandGateway = new UnavailableTenantCommandGateway();
        ITenantsBffComposition composition = new TenantsBffComposition(
            commandGateway,
            httpContextAccessor,
            principalResolver,
            new TenantConfigurationReadPolicyProvider(configuration));
        ITenantsRestQueryClient restQueryClient = Substitute.For<ITenantsRestQueryClient>();
        restQueryClient
            .GetTenantAsync(
                Arg.Any<GetTenantQuery>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new TenantsRestQueryResponse<TenantDetail>(
                rawDetail,
                new QueryResponseMetadata(ETag: "etag", IsStale: false)
                {
                    Provenance = QueryResponseProvenance.ProjectionBacked,
                    Lifecycle = ProjectionLifecycleState.Current,
                },
                TenantsRestQueryFailureKind.None,
                StatusCodes.Status200OK));
        ITenantQueryGateway queryGateway = new TenantQueryGateway(
            restQueryClient,
            userContext,
            new MemoriesClient(
                new HttpClient { BaseAddress = new Uri("https://memories.invalid") },
                Options.Create(new MemoriesClientOptions()),
                NullLogger<MemoriesClient>.Instance),
            new TenantSearchCursorCodec(new EphemeralDataProtectionProvider()),
            composition);

        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(queryGateway);
        Services.AddSingleton(composition);
        Services.AddSingleton(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new PassthroughLocalizer());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, tenantId));
        cut.WaitForElement("[data-testid='tenants-config-read-table']");

        string markup = cut.Markup;
        cut.Find("[data-testid='tenants-config-read-table']").TextContent.ShouldContain("billing.mode");
        cut.Find("[data-testid='tenants-config-read-table']").TextContent.ShouldContain("visible-literal");
        // aria-label is prohibited on role=code, so the approved literals must reach the accessibility tree
        // as text content. The absence assertions below are what actually prove the policy filtered.
        cut.Find("[data-testid='tenants-config-read-key']").TextContent.ShouldBe("billing.mode");
        cut.Find("[data-testid='tenants-config-read-value']").TextContent.ShouldBe("visible-literal");
        markup.ShouldNotContain("billing.secret", Case.Sensitive);
        markup.ShouldNotContain("hidden-undefined-value", Case.Sensitive);
        markup.ShouldNotContain("private.mode", Case.Sensitive);
        markup.ShouldNotContain("hidden-namespace-value", Case.Sensitive);
    }

    private sealed class StubAuthenticationStateProvider(ClaimsPrincipal principal) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(principal));
    }

    private sealed class PassthroughLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly IReadOnlyDictionary<string, string> Values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Tenants.Configuration.Title"] = "Visible configuration",
            ["Tenants.Configuration.Table.Caption"] = "Visible tenant configuration grouped by namespace",
        };

        public LocalizedString this[string name]
            => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(
                CultureInfo.CurrentCulture,
                Values.TryGetValue(name, out string? value) ? value : name,
                arguments));

        // Enumerated so the suite-wide localizer-parity gate can verify every stubbed value against the
        // shipped bundle; an empty enumeration would opt this double out of the gate entirely.
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(static entry => new LocalizedString(entry.Key, entry.Value));
    }
}
