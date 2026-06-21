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

    private static readonly Regex PageRootMainWrapper = new(
        "<main(\\s|>)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex DirectPageTitle = new(
        "<PageTitle(\\s|>)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex DirectRouteHeading = new(
        "<h1(\\s|>)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // The page-root layout classes are derived per-file from each page's outer Class="..." attribute
    // (see Domain_page_css_does_not_own_page_root_layout) so a newly added page is covered automatically
    // instead of relying on a hardcoded class allowlist that silently drifts.
    private static readonly Regex PageRootClassAttribute = new(
        "Class=\"([a-zA-Z][\\w-]*)\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PageRootLayoutDeclaration = new(
        "\\b(display\\s*:\\s*grid|grid-template(?:-columns|-rows)?\\s*:|padding(?:-inline|-block)?(?:-start|-end)?\\s*:|max-width\\s*:|max-inline-size\\s*:|inline-size\\s*:|block-size\\s*:)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    // ──────────────────────────────────────────────────────────────────────────────────────
    // 2026-06-18 structural-HTML + styling-token conformance sweep (proposal §5.3 a/b/c).
    // These three guards are additive — they extend the existing nine guards to cover
    // layout-only <div>/<span> wrappers, text-styling spans/paragraphs, and component-local
    // styling CSS, which the controls/forms/tables/page-layout guards do not catch.
    // ──────────────────────────────────────────────────────────────────────────────────────

    // (b) Layout expressed via an inline style= attribute on a raw element must move to
    // FluentStack/FluentGrid. Scans .razor source (not rendered HTML), so FluentStack — which
    // emits its flex style at render time but carries no source-level style= — is not matched.
    // 2026-06-19 (AC2): widened beyond the original flex/grid/gap/grid-template/flex-direction set
    // to also catch inline spacing (margin/padding), sizing/measure (width/height/block/inline size
    // including min/max variants), and alignment (justify-content/align-items). Both quote styles and
    // whitespace around the equals sign are scanned so small markup variations cannot bypass the guard.
    private static readonly Regex InlineLayoutStyle = new(
        "<\\w+[^>]*\\bstyle\\s*=\\s*(?:\"[^\"]*|'[^']*)\\b("
        + "display\\s*:\\s*(?:flex|grid)|gap\\s*:|grid-template|flex-direction\\s*:|"
        + "margin[\\w-]*\\s*:|padding[\\w-]*\\s*:|(?:min-|max-)?(?:width|height|inline-size|block-size)\\s*:|"
        + "justify-content\\s*:|align-items\\s*:)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    // (a) Component CSS that owns layout/spacing/typography. The directive pushes flex/grid
    // layout to FluentStack/FluentGrid and text sizing/weight to Fluent typography; whatever
    // genuinely cannot be expressed by a Fluent primitive must be documented per rule.
    // 2026-06-19 (AC1): the original "(?!0)" zero-skip also let compact non-zero spacing such as
    // "margin:0.5rem" through (any value beginning with the digit 0). The skip now matches a real
    // zero value only — one or more zero tokens (with optional unit) plus optional !important,
    // ending the declaration ("; "/"}"/end-of-body) — so "margin:0", "margin:0 0 0 0" and
    // "padding:0px" stay skipped while "margin:0.5rem"/"padding:0.5rem" are flagged.
    // Leading \s* absorbs any whitespace the preceding "\s*:\s*" backtracks away, so a multi-token
    // zero value ("0 0 0 0") cannot slip past by leaving a leading space before this lookahead.
    private const string ZeroValueSkip = "(?!\\s*(?:0[a-z%]*\\s*)+(?:!important\\s*)?(?:[;}]|$))";

    private static readonly Regex StylingOwnershipDeclaration = new(
        "\\b(display\\s*:\\s*(?:flex|grid|inline-flex|inline-grid)|gap\\s*:|grid-template|"
        + "margin(?:-inline|-block|-top|-right|-bottom|-left)?\\s*:\\s*" + ZeroValueSkip + "|"
        + "padding(?:-inline|-block|-top|-right|-bottom|-left)?\\s*:\\s*" + ZeroValueSkip + "|"
        + "font-size\\s*:|font-weight\\s*:|line-height\\s*:)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    // A per-rule documented opt-out marker: "/* fc-css-exception: <reason> */" placed on (or
    // immediately before) the rule it exempts. The reason is captured so the guard can assert
    // every exception is justified.
    private static readonly Regex FcCssException = new(
        "/\\*\\s*fc-css-exception:\\s*(?<reason>[^*]+?)\\s*\\*/",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    // Innermost "selector { body }" rule extraction. Plain CSS rule bodies contain no nested
    // braces, so this matches each declaration block individually; the "prelude" group captures
    // the selector plus any comment (including an fc-css-exception marker) since the prior rule.
    private static readonly Regex CssRule = new(
        "(?<prelude>[^{}]*)\\{(?<body>[^{}]*)\\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    // 2026-06-19 (AC3): raw layout-wrapper tags inside Razor (@* ... *@) or HTML (<!-- ... -->)
    // comments are commented-out markup, not rendered DOM, and must not inflate the <div>/<span>
    // budget. Comments are stripped before the tags are counted.
    private static readonly Regex RazorOrHtmlComment = new(
        "@\\*.*?\\*@|<!--.*?-->",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    private static readonly Regex DivSpanTag = new(
        "<(div|span)(\\s|/|>)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Raw structural/semantic HTML kept as the documented "no Fluent v5 equivalent" fallback.
    // Each kept tag MUST carry a rationale here (the governance allowlist). <main> is shell-owned
    // and already guarded by Domain_pages_do_not_reintroduce_page_root_layout_wrappers.
    private static readonly IReadOnlyDictionary<string, string> StructuralHtmlAllowlist =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["header"] = "semantic banner/region landmark for screen-reader navigation; Fluent v5 has no landmark-emitting layout primitive",
            ["section"] = "semantic titled region landmark grouping a page area; Fluent v5 has no landmark-emitting equivalent",
            ["nav"] = "semantic navigation landmark for breadcrumbs and link groups; Fluent v5 has no landmark equivalent",
            ["dl"] = "description list grouping name/value detail pairs; Fluent v5 has no definition-list primitive",
            ["dt"] = "description term; the semantic partner of <dd> inside a <dl>; no Fluent equivalent exists",
            ["dd"] = "description detail; the semantic partner of <dt> inside a <dl>; no Fluent equivalent exists",
            ["ul"] = "unordered list semantics for reason/consequence collections; Fluent v5 has no list primitive",
            ["ol"] = "ordered list semantics for sequenced steps; Fluent v5 has no list primitive",
            ["li"] = "list item; the semantic child of <ul>/<ol>; no Fluent equivalent exists",
            ["a"] = "in-content navigation anchor / router link; FluentAnchor styles button-like actions, not inline navigation links",
        };

    // Post-migration ceiling for raw layout-wrapper tags (<div> + <span>) across Components/**.
    // Pre-sweep baseline = 245 (138 <div> + 107 <span>). After migrating layout-only wrappers to
    // FluentStack/FluentGrid the achieved floor is recorded here. This ratchets DOWN as migration
    // proceeds; it may only be RAISED with an explicit code-review note. Keeping it as a budget
    // (rather than a brittle zero-rule) makes the "minimize raw layout markup" rule regression-proof
    // while honoring the kept a11y/nav structural tags in StructuralHtmlAllowlist.
    private const int DivSpanLayoutBudgetBaseline = 245;
    private const int DivSpanLayoutBudgetCeiling = 220;

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
    public void Domain_page_components_declare_frontcomposer_page_layout_modes()
    {
        string pagesRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components", "Pages");
        (string FileName, string ExpectedMode, string Rationale)[] expectedDeclarations =
        [
            ("TenantsWorkspace.razor", "FcPageLayoutMode.FullWidth", "tenant list is a dense DataGrid-first operational surface"),
            ("MyTenantsPage.razor", "FcPageLayoutMode.FullWidth", "membership list is a dense DataGrid-first operational surface"),
            ("GlobalAdministratorsPage.razor", "FcPageLayoutMode.FullWidth", "administrator governance keeps the DataGrid directly visible"),
            ("TenantAuditPage.razor", "FcPageLayoutMode.FullWidth", "audit review keeps the audit DataGrid directly visible"),
            ("TenantDetailPage.razor", "FcPageLayoutMode.Constrained", "tenant detail is a readable detail and command composition page"),
            ("UserMembershipLookupPage.razor", "FcPageLayoutMode.Constrained", "lookup form and status copy need readable measure"),
        ];

        List<string> offenders = [];
        foreach ((string fileName, string expectedMode, string rationale) in expectedDeclarations)
        {
            string content = File.ReadAllText(Path.Combine(pagesRoot, fileName));
            if (!DeclaresLayoutMeasure(content) || !DeclaresLayoutMode(content, expectedMode))
            {
                offenders.Add($"{fileName} must declare {expectedMode} ({rationale})");
            }
        }

        // Catch a newly added route page that declares no FcPageLayout measure at all (the explicit
        // expectations above only cover the known pages).
        foreach (string razorFile in Directory.GetFiles(pagesRoot, "*.razor", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(razorFile);
            if (content.Contains("@page", StringComparison.Ordinal)
                && !DeclaresLayoutMeasure(content))
            {
                offenders.Add($"{Path.GetFileName(razorFile)} is a route page but declares no FcPageLayout measure");
            }
        }

        offenders.ShouldBeEmpty(
            "Tenants pages must declare FC-LYT measure through FcPageLayout instead of local page-layout wrappers. "
            + string.Join("; ", offenders));
    }

    [Fact]
    [Trait("Category", "Governance")]
    public void Domain_route_pages_declare_frontcomposer_page_headers()
    {
        string pagesRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components", "Pages");
        string[] razorFiles = Directory.GetFiles(pagesRoot, "*.razor", SearchOption.AllDirectories);

        razorFiles.ShouldNotBeEmpty();

        List<string> offenders = [];
        foreach (string file in razorFiles)
        {
            string content = File.ReadAllText(file);
            if (!content.Contains("@page", StringComparison.Ordinal))
            {
                continue;
            }

            string fileName = Path.GetFileName(file);
            if (!DeclaresFrontComposerHeader(content))
            {
                offenders.Add($"{fileName} must declare route title/header through FcPageHeader");
            }

            if (DirectPageTitle.IsMatch(content))
            {
                offenders.Add($"{fileName} declares PageTitle directly");
            }

            if (DirectRouteHeading.IsMatch(content))
            {
                offenders.Add($"{fileName} declares raw route-level h1 heading markup");
            }
        }

        offenders.ShouldBeEmpty(
            "Route-level browser titles and visible page headers belong to FrontComposer FcPageHeader. "
            + "Tenants pages supply localized strings/fragments only: "
            + string.Join("; ", offenders));
    }

    [Fact]
    [Trait("Category", "Governance")]
    public void Domain_pages_do_not_reintroduce_page_root_layout_wrappers()
    {
        string pagesRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components", "Pages");
        string[] razorFiles = Directory.GetFiles(pagesRoot, "*.razor", SearchOption.AllDirectories);

        razorFiles.ShouldNotBeEmpty();

        List<string> offenders = [];
        foreach (string file in razorFiles)
        {
            string content = File.ReadAllText(file);
            if (PageRootMainWrapper.IsMatch(content))
            {
                offenders.Add(Path.GetFileName(file));
            }
        }

        offenders.ShouldBeEmpty(
            "FrontComposerShell owns the shell/content container and FcPageLayout owns page measure. "
            + $"Do not add Tenants-owned page-root <main> layout wrappers: {string.Join("; ", offenders)}");
    }

    [Fact]
    [Trait("Category", "Governance")]
    public void Domain_page_css_does_not_own_page_root_layout()
    {
        string pagesRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components", "Pages");
        string[] cssFiles = Directory.GetFiles(pagesRoot, "*.razor.css", SearchOption.TopDirectoryOnly);
        string[] razorFiles = Directory.GetFiles(pagesRoot, "*.razor", SearchOption.TopDirectoryOnly);

        cssFiles.ShouldNotBeEmpty();

        // Derive the monitored page-root class names from each page's outer Class="..." so a newly
        // added page (with a new root class) is covered without editing this guard.
        HashSet<string> rootClasses = [];
        foreach (string razorFile in razorFiles)
        {
            Match classMatch = PageRootClassAttribute.Match(File.ReadAllText(razorFile));
            if (classMatch.Success)
            {
                rootClasses.Add(classMatch.Groups[1].Value);
            }
        }

        rootClasses.ShouldNotBeEmpty("Expected to derive at least one page-root layout class from the page components.");

        // Match the page-root class as a complete selector — standalone (".root {") or as a member of a
        // grouped selector list (".root, .root__x { ... }") — so root-layout ownership cannot hide behind
        // grouping. The (?=[,{]) lookahead keeps BEM children (".root__x") and descendant ancestors
        // (".root h2") out of the match.
        Regex pageRootSelector = new(
            "\\.(" + string.Join("|", rootClasses.Select(Regex.Escape)) + ")\\s*(?=[,{])[^{}]*\\{(?<body>.*?)\\}",
            RegexOptions.CultureInvariant | RegexOptions.Singleline);

        List<string> offenders = [];
        foreach (string file in cssFiles)
        {
            string content = CssComment.Replace(File.ReadAllText(file), string.Empty);
            foreach (Match match in pageRootSelector.Matches(content))
            {
                if (PageRootLayoutDeclaration.IsMatch(match.Groups["body"].Value))
                {
                    offenders.Add($"{Path.GetFileName(file)} ({match.Groups[1].Value})");
                }
            }
        }

        offenders.ShouldBeEmpty(
            "Page-root layout belongs to FrontComposer/Fluent primitives. Page CSS may keep component-specific "
            + $"exceptions, but must not set root display/grid/padding/max-width (incl. logical properties): {string.Join("; ", offenders)}");
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

    [Fact]
    [Trait("Category", "Governance")]
    public void Domain_ui_components_do_not_carry_inline_layout_styles()
    {
        string componentsRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components");
        string[] razorFiles = Directory.GetFiles(componentsRoot, "*.razor", SearchOption.AllDirectories);

        // Guard against a broken path silently passing the scan.
        razorFiles.ShouldNotBeEmpty();

        List<string> offenders = [];
        foreach (string file in razorFiles)
        {
            if (InlineLayoutStyle.IsMatch(File.ReadAllText(file)))
            {
                offenders.Add(Path.GetRelativePath(componentsRoot, file));
            }
        }

        offenders.ShouldBeEmpty(
            "Domain UI .razor components must express layout through FluentStack/FluentGrid, not inline "
            + $"layout style= attributes (display:flex/grid, gap, grid-template, flex-direction). Found in: {string.Join("; ", offenders)}");
    }

    [Fact]
    [Trait("Category", "Governance")]
    public void Domain_ui_component_css_does_not_own_layout_spacing_or_typography()
    {
        string componentsRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components");
        string[] cssFiles = Directory.GetFiles(componentsRoot, "*.razor.css", SearchOption.AllDirectories);

        // Guard against a broken path silently passing the scan.
        cssFiles.ShouldNotBeEmpty();

        List<string> reasonOffenders = [];
        List<string> ownershipOffenders = [];
        foreach (string file in cssFiles)
        {
            string raw = File.ReadAllText(file);
            string relative = Path.GetRelativePath(componentsRoot, file);

            // Every documented exception must carry a meaningful reason (no empty / token opt-outs).
            foreach (Match marker in FcCssException.Matches(raw))
            {
                string reason = marker.Groups["reason"].Value.Trim();
                if (reason.Length < 12 || !reason.Contains(' ', StringComparison.Ordinal))
                {
                    reasonOffenders.Add($"{relative} (\"{reason}\")");
                }
            }

            foreach (string offender in StylingOwnershipOffenders(raw))
            {
                ownershipOffenders.Add($"{relative} :: {offender}");
            }
        }

        reasonOffenders.ShouldBeEmpty(
            "Every /* fc-css-exception: ... */ marker must document a real reason (>= 12 chars, multi-word). "
            + $"Under-documented exceptions: {string.Join("; ", reasonOffenders)}");

        ownershipOffenders.ShouldBeEmpty(
            "Domain UI component CSS must express layout/spacing/typography through Fluent primitives + design "
            + "tokens. A rule may keep a declaration only via an immediately-preceding "
            + "'/* fc-css-exception: <reason> */' marker or inside @media (forced-colors). "
            + $"Unmarked ownership found in: {string.Join("; ", ownershipOffenders)}");
    }

    // Returns the styling-ownership offenders in a CSS document: every rule that owns
    // layout/spacing/typography without an immediately-preceding "/* fc-css-exception: <reason> */"
    // marker. @media (forced-colors) blocks are removed first (always-allowed high-contrast support).
    // 2026-06-19 (AC4): fc-css-exception scoping is intentionally kept RULE-level — each marker
    // exempts only the single rule it immediately precedes (its prelude), never the following rule,
    // because component rules here are small single-purpose blocks whose marker reason already names
    // the declarations it covers.
    // 2026-06-19 (AC5): the :focus-visible blanket exemption was removed. Focus-ring affordances
    // (outline/outline-offset/outline-color) are not tracked ownership declarations, so genuine focus
    // rules still pass; a :focus-visible rule that owns layout/spacing/typography is now flagged
    // unless it carries an fc-css-exception marker.
    private static IReadOnlyList<string> StylingOwnershipOffenders(string css)
    {
        List<string> offenders = [];
        string scannable = RemoveForcedColorsMediaBlocks(css);
        foreach (Match rule in CssRule.Matches(scannable))
        {
            string prelude = rule.Groups["prelude"].Value;
            string body = rule.Groups["body"].Value;

            if (FcCssException.IsMatch(prelude))
            {
                continue;
            }

            Match owned = StylingOwnershipDeclaration.Match(body);
            if (owned.Success)
            {
                offenders.Add($"'{prelude.Trim()}' owns '{owned.Value.Trim()}'");
            }
        }

        return offenders;
    }

    [Fact]
    [Trait("Category", "Governance")]
    public void Domain_ui_structural_html_allowlist_is_documented_and_div_span_budget_holds()
    {
        // (1) Every kept raw structural/semantic tag is documented with a rationale.
        List<string> undocumented = [];
        foreach ((string tag, string rationale) in StructuralHtmlAllowlist)
        {
            if (string.IsNullOrWhiteSpace(rationale) || rationale.Trim().Length < 12)
            {
                undocumented.Add(tag);
            }
        }

        undocumented.ShouldBeEmpty(
            "Each kept raw structural/semantic tag must carry a rationale in StructuralHtmlAllowlist "
            + $"(no Fluent v5 equivalent). Missing/short rationale for: {string.Join(", ", undocumented)}");

        // (2) The raw layout-wrapper budget (<div> + <span>) must not exceed the ratcheted ceiling.
        string componentsRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components");
        string[] razorFiles = Directory.GetFiles(componentsRoot, "*.razor", SearchOption.AllDirectories);
        razorFiles.ShouldNotBeEmpty();

        int total = razorFiles.Sum(file => CountLayoutWrappers(File.ReadAllText(file)));

        total.ShouldBeLessThanOrEqualTo(
            DivSpanLayoutBudgetCeiling,
            $"Raw <div>+<span> layout wrappers across Components/** ({total}) exceed the ratcheted ceiling "
            + $"({DivSpanLayoutBudgetCeiling}; pre-sweep baseline {DivSpanLayoutBudgetBaseline}). Migrate layout-only "
            + "wrappers to FluentStack/FluentGrid, or — only with a code-review note — raise the ceiling.");
    }

    // ──────────────────────────────────────────────────────────────────────────────────────
    // 2026-06-19 governance-guard hardening (cc-2026-06-19-domain-ui-governance-and-accessibility).
    // These unit tests pin the guard *logic* (the private static regex/helpers above) so the closed
    // §5.3 bypasses cannot silently reopen. They exercise the same detection the file scanners use,
    // on crafted CSS/markup fragments.
    // ──────────────────────────────────────────────────────────────────────────────────────

    [Theory]
    [Trait("Category", "Governance")]
    [InlineData("margin: 0.5rem")]
    [InlineData("margin:0.5rem")]
    [InlineData("padding: 0.5rem")]
    [InlineData("padding:0.5rem")]
    [InlineData("padding-inline: 0.25rem")]
    [InlineData("margin-block: 0.5rem")]
    [InlineData("padding: 0.15rem 0.4rem")]
    public void Styling_ownership_guard_flags_compact_non_zero_spacing(string declaration)
        => StylingOwnershipDeclaration.IsMatch(declaration).ShouldBeTrue(
            $"compact non-zero spacing '{declaration}' must be treated as layout ownership (AC1).");

    [Theory]
    [Trait("Category", "Governance")]
    [InlineData("margin: 0;")]
    [InlineData("margin:0")]
    [InlineData("padding: 0px;")]
    [InlineData("margin: 0 0 0 0;")]
    [InlineData("padding: 0 0;")]
    [InlineData("margin: 0 !important;")]
    public void Styling_ownership_guard_still_skips_genuine_zero_resets(string declaration)
        => StylingOwnershipDeclaration.IsMatch(declaration).ShouldBeFalse(
            $"a genuine zero reset '{declaration}' must remain skipped (AC1).");

    [Theory]
    [Trait("Category", "Governance")]
    [InlineData("<div style=\"margin: 8px\">")]
    [InlineData("<span style=\"padding-inline: 4px\">")]
    [InlineData("<div style=\"width: 50%\">")]
    [InlineData("<div style=\"height: 10rem\">")]
    [InlineData("<div style=\"inline-size: 12rem\">")]
    [InlineData("<div style=\"block-size: 12rem\">")]
    [InlineData("<div style=\"min-width: 12rem\">")]
    [InlineData("<div style=\"max-inline-size: 24rem\">")]
    [InlineData("<div style=\"justify-content: center\">")]
    [InlineData("<div style=\"align-items: center\">")]
    [InlineData("<div style='margin: 8px'>")]
    [InlineData("<div style = \"margin: 8px\">")]
    public void Inline_layout_style_guard_flags_spacing_sizing_and_alignment(string markup)
        => InlineLayoutStyle.IsMatch(markup).ShouldBeTrue(
            $"inline layout/spacing/sizing style '{markup}' must be flagged (AC2).");

    [Theory]
    [Trait("Category", "Governance")]
    [InlineData("<div style=\"color: red\">")]
    [InlineData("<div style=\"background: Canvas\">")]
    [InlineData("<FluentStack Orientation=\"Orientation.Vertical\" VerticalGap=\"16px\">")]
    public void Inline_layout_style_guard_allows_non_layout_markup(string markup)
        => InlineLayoutStyle.IsMatch(markup).ShouldBeFalse(
            $"non-layout markup '{markup}' must not be flagged (AC2).");

    [Fact]
    [Trait("Category", "Governance")]
    public void Div_span_budget_excludes_commented_out_tags()
    {
        // Razor (@* *@) and HTML (<!-- -->) comments hold commented-out markup, not rendered DOM (AC3).
        CountLayoutWrappers("@* <div></div><span></span> *@").ShouldBe(0);
        CountLayoutWrappers("<!-- <div> <span> -->").ShouldBe(0);
        CountLayoutWrappers("@* <div> *@\n<div data-testid=\"x\"></div>").ShouldBe(1);

        // Real tags still count, including ones that follow a comment on the same line.
        CountLayoutWrappers("<div></div><span></span>").ShouldBe(2);
    }

    [Fact]
    [Trait("Category", "Governance")]
    public void Fc_css_exception_marker_is_rule_scoped_and_does_not_leak_to_the_next_rule()
    {
        // AC4 decision: fc-css-exception scoping stays RULE-level. The marker exempts only the rule it
        // immediately precedes; the following rule is still scanned.
        const string css =
            "/* fc-css-exception: documented layout ownership rationale */\n"
            + ".exempt { display: flex; }\n"
            + ".scanned { display: flex; }";

        IReadOnlyList<string> offenders = StylingOwnershipOffenders(css);

        offenders.ShouldHaveSingleItem();
        offenders[0].ShouldContain(".scanned");
        offenders[0].ShouldNotContain(".exempt");
    }

    [Fact]
    [Trait("Category", "Governance")]
    public void Focus_visible_rules_pass_on_affordances_but_are_flagged_for_layout_ownership()
    {
        // AC5 decision: the :focus-visible blanket exemption is removed. Outline affordances are not
        // tracked ownership declarations, so genuine focus rings still pass...
        StylingOwnershipOffenders(".a:focus-visible { outline: 2px solid Highlight; outline-offset: 3px; }")
            .ShouldBeEmpty();

        // ...but a :focus-visible rule that owns spacing is now flagged unless it is documented.
        StylingOwnershipOffenders(".a:focus-visible { padding: 2rem; }")
            .ShouldHaveSingleItem();
        StylingOwnershipOffenders(
            "/* fc-css-exception: documented focus padding rationale */\n.a:focus-visible { padding: 2rem; }")
            .ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "Governance")]
    public void Forced_colors_block_removal_is_stable_with_braces_in_comments_and_strings()
    {
        // AC6: a stray brace inside a comment or string must not prematurely close the forced-colors
        // block and leak its tail (a spacing declaration) back into the scannable CSS.
        const string withComment =
            "@media (forced-colors: active) {\n  /* a } brace */\n  .x { margin: 5rem; }\n}\n.real { color: red; }";
        string remaining = RemoveForcedColorsMediaBlocks(withComment);
        remaining.ShouldNotContain("margin: 5rem");
        remaining.ShouldContain(".real");
        StylingOwnershipOffenders(withComment).ShouldBeEmpty();

        const string withString =
            "@media (forced-colors: active) {\n  .y::after { content: \"}\"; padding: 9rem; }\n}\n.tail { color: red; }";
        StylingOwnershipOffenders(withString).ShouldBeEmpty();
    }

    // Removes every "@media (... forced-colors ...) { ... }" block (brace-matched) so the
    // high-contrast exception is not scanned by the styling-ownership guard.
    // 2026-06-19 (AC6): the brace matcher now skips CSS comments (/* ... */) and quoted strings so a
    // stray '{' or '}' inside a comment or string cannot desynchronize the depth counter and leak the
    // tail of a forced-colors block back into the scannable CSS.
    private static string RemoveForcedColorsMediaBlocks(string css)
    {
        while (true)
        {
            Match media = Regex.Match(
                css,
                "@media[^{]*forced-colors[^{]*\\{",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!media.Success)
            {
                return css;
            }

            int blockStart = media.Index;
            int cursor = media.Index + media.Length; // first char after the opening '{'
            int depth = 1;
            while (cursor < css.Length && depth > 0)
            {
                char current = css[cursor];

                // Skip a /* ... */ comment wholesale so braces inside it are not counted.
                if (current == '/' && cursor + 1 < css.Length && css[cursor + 1] == '*')
                {
                    int commentEnd = css.IndexOf("*/", cursor + 2, StringComparison.Ordinal);
                    cursor = commentEnd < 0 ? css.Length : commentEnd + 2;
                    continue;
                }

                // Skip a quoted string wholesale (honoring backslash escapes) for the same reason.
                if (current is '"' or '\'')
                {
                    cursor++;
                    while (cursor < css.Length && css[cursor] != current)
                    {
                        cursor += css[cursor] == '\\' ? 2 : 1;
                    }

                    cursor++; // step past the closing quote (or past end-of-string)
                    continue;
                }

                if (current == '{')
                {
                    depth++;
                }
                else if (current == '}')
                {
                    depth--;
                }

                cursor++;
            }

            css = css.Remove(blockStart, Math.Min(cursor, css.Length) - blockStart);
        }
    }

    // Counts raw <div>/<span> layout-wrapper tags after stripping Razor and HTML comments, so
    // commented-out markup never inflates the layout-wrapper budget (AC3).
    private static int CountLayoutWrappers(string razor)
        => DivSpanTag.Matches(RazorOrHtmlComment.Replace(razor, string.Empty)).Count;

    // cc-2026-06-21: the route-level browser title + visible header may be declared either directly via
    // FcPageHeader or through the FrontComposer aggregate page wrappers (FcAggregateListPage /
    // FcAggregateDetailPage), which compose FcPageHeader internally. Both are FrontComposer-owned; this
    // does NOT relax the raw <PageTitle>/<h1> bans, which are still enforced separately.
    private static bool DeclaresFrontComposerHeader(string content)
        => content.Contains("<FcPageHeader", StringComparison.Ordinal)
            || content.Contains("<FcAggregateListPage", StringComparison.Ordinal)
            || content.Contains("<FcAggregateDetailPage", StringComparison.Ordinal);

    // The FC-LYT measure may be declared directly via FcPageLayout or forwarded through an aggregate page
    // wrapper, which owns the FcPageLayout element and exposes the measure as its LayoutMode parameter.
    private static bool DeclaresLayoutMeasure(string content)
        => content.Contains("<FcPageLayout", StringComparison.Ordinal)
            || content.Contains("<FcAggregateListPage", StringComparison.Ordinal)
            || content.Contains("<FcAggregateDetailPage", StringComparison.Ordinal);

    private static bool DeclaresLayoutMode(string content, string expectedMode)
        => content.Contains($"Mode=\"{expectedMode}\"", StringComparison.Ordinal)
            || content.Contains($"LayoutMode=\"{expectedMode}\"", StringComparison.Ordinal);

    private static string ProjectRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
