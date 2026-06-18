using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

/// <summary>
/// Governance guard for the "domain UI uses Fluent v5 only" rule. In Fluent UI Blazor v5 the design
/// system only styles its own custom elements (<c>&lt;fluent-button&gt;</c>, <c>&lt;fluent-text-input&gt;</c>,
/// <c>&lt;fluent-select&gt;</c>, …); a raw <c>&lt;button&gt;</c> / <c>&lt;input&gt;</c> / <c>&lt;select&gt;</c> /
/// <c>&lt;textarea&gt;</c> is never upgraded and falls back to unstyled browser rendering, which also drops
/// the accessibility affordances (NFR6) that the Fluent components provide. This test fails the build if a
/// raw interactive HTML control is reintroduced into any <c>Hexalith.Tenants.UI</c> component, so the
/// completed Fluent conversion cannot silently regress.
/// <para>
/// This is the Tenants.UI slice of the project-wide UI component policy documented in FrontComposer
/// <c>architecture.md</c> §4.1 ("every UI page/component uses FrontComposer or Fluent v5 only"). Sibling
/// guards enforce the same rule on the other surfaces: <c>FluentConformanceTests</c> (FrontComposer Shell +
/// Counter.Web) and <c>AdminUiFluentConformanceTests</c> (EventStore Admin.UI). Tenants.UI declares no
/// carve-outs; the documented carve-outs on other surfaces are listed in architecture.md §4.1.
/// </para>
/// </summary>
public sealed class DomainUiFluentConformanceTests
{
    // Matches an opening tag for a raw interactive HTML control. The trailing character class anchors on a
    // real tag boundary (whitespace, self-close, or '>') so attributes like `inputmode=` and Fluent
    // components like <FluentButton> / <FluentTextInput> (capitalised) are not matched.
    private static readonly Regex RawInteractiveControl = new(
        "<(button|input|select|textarea)(\\s|/|>)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RawFormMarkup = new(
        "</?form(\\s|/|>)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Data surfaces should render through FluentDataGrid or FrontComposer grid primitives. Scanning
    // source keeps the guard focused on handwritten markup; FluentDataGrid can still render native
    // table semantics internally.
    private static readonly Regex RawTableMarkup = new(
        "<(table|thead|tbody|tr|td|th)(\\s|/|>)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex HardCodedCssColor = new(
        "#[0-9a-fA-F]{3,8}\\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex CssComment = new(
        "/\\*.*?\\*/",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    private static readonly Regex NativeControlCssSelector = new(
        "(^|[\\s,{>+~])(?:button|input|select|textarea)(?=[:.#\\s,{>+~\\[]|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);

    [Fact]
    [Trait("Category", "Governance")]
    public void Domain_ui_components_use_fluent_v5_only_with_no_raw_interactive_html_controls()
    {
        string componentsRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components");
        string[] razorFiles = Directory.GetFiles(componentsRoot, "*.razor", SearchOption.AllDirectories);

        // Guard against a broken path silently passing the scan.
        razorFiles.ShouldNotBeEmpty();

        List<string> offenders = [];
        foreach (string file in razorFiles)
        {
            MatchCollection matches = RawInteractiveControl.Matches(File.ReadAllText(file));
            if (matches.Count > 0)
            {
                string tags = string.Join(
                    ", ",
                    matches.Select(match => match.Groups[1].Value).Distinct(StringComparer.Ordinal));
                offenders.Add($"{Path.GetFileName(file)} ({tags})");
            }
        }

        offenders.ShouldBeEmpty(
            "Domain UI .razor components must use Fluent v5 components only (no raw <button>/<input>/<select>/"
            + $"<textarea>). Raw interactive controls found in: {string.Join("; ", offenders)}");
    }

    [Fact]
    [Trait("Category", "Governance")]
    public void Domain_ui_components_use_blazor_or_fluent_forms_with_no_raw_form_markup()
    {
        string componentsRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components");
        string[] razorFiles = Directory.GetFiles(componentsRoot, "*.razor", SearchOption.AllDirectories);

        // Guard against a broken path silently passing the scan.
        razorFiles.ShouldNotBeEmpty();

        List<string> offenders = [];
        foreach (string file in razorFiles)
        {
            MatchCollection matches = RawFormMarkup.Matches(File.ReadAllText(file));
            if (matches.Count > 0)
            {
                offenders.Add(Path.GetRelativePath(componentsRoot, file));
            }
        }

        offenders.ShouldBeEmpty(
            "Domain UI .razor components must use Blazor EditForm, Fluent, or FrontComposer form primitives "
            + $"instead of source-level raw <form> markup. Raw forms found in: {string.Join("; ", offenders)}");
    }

    [Fact]
    [Trait("Category", "Governance")]
    public void Domain_ui_components_use_fluent_grid_primitives_with_no_raw_table_markup()
    {
        string componentsRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components");
        string[] razorFiles = Directory.GetFiles(componentsRoot, "*.razor", SearchOption.AllDirectories);

        // Guard against a broken path silently passing the scan.
        razorFiles.ShouldNotBeEmpty();

        List<string> offenders = [];
        foreach (string file in razorFiles)
        {
            MatchCollection matches = RawTableMarkup.Matches(File.ReadAllText(file));
            if (matches.Count > 0)
            {
                string tags = string.Join(
                    ", ",
                    matches.Select(match => match.Groups[1].Value).Distinct(StringComparer.Ordinal));
                offenders.Add($"{Path.GetFileName(file)} ({tags})");
            }
        }

        offenders.ShouldBeEmpty(
            "Domain UI .razor components must use FluentDataGrid or FrontComposer grid primitives for data surfaces "
            + $"(no raw table markup). Raw table markup found in: {string.Join("; ", offenders)}");
    }

    [Fact]
    [Trait("Category", "Governance")]
    public void Multi_region_domain_pages_group_sibling_sections_with_fluent_accordions()
    {
        string componentsRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components");
        string[] accordionRequiredFiles =
        [
            Path.Combine(componentsRoot, "Pages", "GlobalAdministratorsPage.razor"),
            Path.Combine(componentsRoot, "Pages", "TenantAuditPage.razor"),
            Path.Combine(componentsRoot, "Pages", "TenantDetailPage.razor"),
            Path.Combine(componentsRoot, "Pages", "UserMembershipLookupPage.razor"),
            Path.Combine(componentsRoot, "Tenants", "TenantConfigurationView.razor"),
        ];

        List<string> offenders = [];
        foreach (string file in accordionRequiredFiles)
        {
            string content = File.ReadAllText(file);
            if (!content.Contains("<FluentAccordion", StringComparison.Ordinal)
                || !content.Contains("ExpandMode=\"AccordionExpandMode.Multi\"", StringComparison.Ordinal)
                || !content.Contains("Expanded=\"true\"", StringComparison.Ordinal))
            {
                offenders.Add(Path.GetRelativePath(componentsRoot, file));
            }
        }

        offenders.ShouldBeEmpty(
            "Multi-region domain pages must group sibling titled page regions with FluentAccordion, "
            + $"expanded by default. Missing accordion grouping in: {string.Join("; ", offenders)}");
    }

    [Fact]
    [Trait("Category", "Governance")]
    public void Domain_ui_component_css_does_not_own_semantic_colors_or_native_control_selectors()
    {
        string componentsRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components");
        string[] cssFiles = Directory.GetFiles(componentsRoot, "*.razor.css", SearchOption.AllDirectories);

        // Guard against a broken path silently passing the scan.
        cssFiles.ShouldNotBeEmpty();

        List<string> colorOffenders = [];
        List<string> selectorOffenders = [];
        foreach (string file in cssFiles)
        {
            string content = File.ReadAllText(file);
            string contentWithoutComments = CssComment.Replace(content, string.Empty);

            if (HardCodedCssColor.IsMatch(contentWithoutComments))
            {
                colorOffenders.Add(Path.GetRelativePath(componentsRoot, file));
            }

            if (NativeControlCssSelector.IsMatch(contentWithoutComments))
            {
                selectorOffenders.Add(Path.GetRelativePath(componentsRoot, file));
            }
        }

        colorOffenders.ShouldBeEmpty(
            "Domain UI component CSS must not hard-code semantic status/control colors. Use Fluent component "
            + $"roles, Fluent tokens, or system colors instead. Hard-coded colors found in: {string.Join("; ", colorOffenders)}");
        selectorOffenders.ShouldBeEmpty(
            "Domain UI component CSS must not style native button/input/select/textarea descendants. Use Fluent "
            + $"component parameters or wrapper layout classes instead. Native control selectors found in: {string.Join("; ", selectorOffenders)}");
    }

    private static string ProjectRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
