using System.Globalization;

using Bunit;
using Bunit.TestDoubles;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Testing;
using Hexalith.Tenants.UI.Composition;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class PageLayoutDeclarationTests : BunitContext
{
    public PageLayoutDeclarationTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        MarkupSanitizedOptions.ThrowOnUnsafe = false;

        _ = Services.AddLogging();
        _ = Services.AddFluentUIComponents();

        // Protected search paging is a required scoped circuit service; the workspace fails loudly without it.
        _ = Services.AddScoped<TenantSearchPagingState>();
        _ = Services.AddHexalithFrontComposerQuickstart();
        Services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        Services.Replace(ServiceDescriptor.Scoped<IUserContextAccessor>(_ => new TestUserContextAccessor()));
        Services.Replace(ServiceDescriptor.Scoped<IThemeService>(_ => Substitute.For<IThemeService>()));

        _ = AddAuthorization();
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton(Substitute.For<ITenantCommandGateway>());
        Services.AddSingleton(Substitute.For<ITenantsBffComposition>());

    }

    [Fact]
    public void Tenant_workspace_declares_full_width_layout_inside_frontcomposer_shell()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantListSnapshot.Empty(isAuthorizationScoped: true, ReadModelFreshnessState.Unknown)));
        Services.AddSingleton(gateway);
        EnsureStoreInitialized();

        IRenderedComponent<FrontComposerShell> cut = RenderPageInShell<TenantsWorkspace>();

        cut.WaitForAssertion(() =>
            cut.Find("#fc-main-content").GetAttribute("data-fc-page-layout").ShouldBe("full-width"));
        cut.Find("[data-testid='tenants-workspace']");
    }

    [Fact]
    public void User_lookup_declares_constrained_layout_inside_frontcomposer_shell()
    {
        Services.AddSingleton(Substitute.For<ITenantQueryGateway>());
        EnsureStoreInitialized();

        IRenderedComponent<FrontComposerShell> cut = RenderPageInShell<UserMembershipLookupPage>();

        cut.WaitForAssertion(() =>
            cut.Find("#fc-main-content").GetAttribute("data-fc-page-layout").ShouldBe("constrained"));
        cut.Find("[data-testid='tenants-user-lookup']");
    }

    private IRenderedComponent<FrontComposerShell> RenderPageInShell<TPage>()
        where TPage : IComponent
    {
        // The workspace only restores retained protected paging on an interactive render pass, and
        // SetRendererInfo initializes the service provider, so it runs after every registration.
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        return Render<FrontComposerShell>(parameters => parameters.Add(
            shell => shell.ChildContent,
            builder =>
            {
                builder.OpenComponent<TPage>(0);
                builder.CloseComponent();
            }));
    }

    private void EnsureStoreInitialized()
    {
        IStore store = Services.GetRequiredService<IStore>();
        store.InitializeAsync().GetAwaiter().GetResult();
    }

    private sealed class TestUserContextAccessor : IUserContextAccessor
    {
        public string? TenantId => "system";

        public string? UserId => "test-user";
    }

    /// <summary>
    /// Backed by the shipped <see cref="TenantsResources"/> bundle rather than echoing keys, so the
    /// suite-wide localizer-parity gate can observe this double instead of being opted out of by an empty
    /// enumeration, and so its indexer and its enumeration cannot disagree.
    /// </summary>
    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly System.Resources.ResourceManager Manager = new(typeof(TenantsResources));

        public LocalizedString this[string name]
            => new(name, Manager.GetString(name, CultureInfo.InvariantCulture) ?? name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(
                CultureInfo.InvariantCulture,
                Manager.GetString(name, CultureInfo.InvariantCulture) ?? name,
                arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            System.Resources.ResourceSet? set = Manager.GetResourceSet(
                CultureInfo.InvariantCulture,
                createIfNotExists: true,
                tryParents: true);
            if (set is null)
            {
                yield break;
            }

            foreach (System.Collections.DictionaryEntry entry in set)
            {
                if (entry.Key is string key && entry.Value is string value)
                {
                    yield return new LocalizedString(key, value);
                }
            }
        }
    }
}
