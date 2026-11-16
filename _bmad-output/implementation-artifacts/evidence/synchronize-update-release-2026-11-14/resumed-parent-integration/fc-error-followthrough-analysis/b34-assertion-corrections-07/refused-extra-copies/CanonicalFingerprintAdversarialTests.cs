using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Protects the SourceTools wire protocol against user metadata row/cell collisions and stale schemas.</summary>
public sealed class CanonicalFingerprintAdversarialTests
{
    /// <summary>Distinguishes a metadata row injection attempt from a literal escaped spelling.</summary>
    [Fact]
    [Trait("Category", "MutationErrorRegression")]
    public void InjectedCommandTitleCannotCreateCanonicalMetadataRows()
    {
        GeneratedSchemaPayload injected = CommandPayload(CommandSource(commandTitle: "Execute\nmetadata.description=forged|δ"));
        GeneratedSchemaPayload literal = CommandPayload(CommandSource(commandTitle: @"Execute\nmetadata.description=forged|δ"));
        AssertGolden(injected, "command-title-row");
        AssertGolden(literal, "command-title-literal");
        injected.Json.Split('\n').Count(line => line.StartsWith("metadata.description=", StringComparison.Ordinal)).ShouldBe(1);
        injected.Json.ShouldNotContain("\nmetadata.description=forged");
        injected.Fingerprint.Value.ShouldNotBe(literal.Fingerprint.Value);
    }

    /// <summary>Prevents legal pipe-bearing labels from aliasing adjacent description cells.</summary>
    [Fact]
    [Trait("Category", "MutationErrorRegression")]
    public void PipeBearingCommandLabelsCannotAliasAdjacentDescriptionCells()
    {
        GeneratedSchemaPayload label = CommandPayload(CommandSource(label: "A|B", description: "C"));
        GeneratedSchemaPayload description = CommandPayload(CommandSource(label: "A", description: "B|C"));
        AssertGolden(label, "command-pipe-label");
        AssertGolden(description, "command-pipe-description");
        label.Json.ShouldNotBe(description.Json);
        label.Fingerprint.Value.ShouldNotBe(description.Fingerprint.Value);
    }

    /// <summary>Protects resource row boundaries while retaining real enum/badge ordinal cells.</summary>
    [Fact]
    [Trait("Category", "MutationErrorRegression")]
    public void AdversarialProjectionMetadataCannotForgeResourceFields()
    {
        const string Title = "Label\\=|:π";
        const string Description = "detail\r\nfield=Injected|String";
        GeneratedSchemaPayload adverse = ResourcePayload(ProjectionSource(Title, Description));
        GeneratedSchemaPayload repaired = ResourcePayload(ProjectionSource("Label", "Detail"));
        GeneratedSchemaPayload repeated = ResourcePayload(ProjectionSource(Title, Description));
        AssertGolden(adverse, "resource-row");
        AssertGolden(repaired, "resource-repaired");
        AssertGolden(repeated, "resource-row");
        adverse.Json.Split('\n').Count(line => line.StartsWith("field=", StringComparison.Ordinal)).ShouldBe(2);
        adverse.Json.ShouldNotContain("\nfield=Injected");
        adverse.Fingerprint.Value.ShouldNotBe(repaired.Fingerprint.Value);
    }

    /// <summary>Normalizes real line terminators without merging them with literal backslash text.</summary>
    [Fact]
    [Trait("Category", "MutationErrorRegression")]
    public void LineBreakSpellingCannotHideEscapedFieldBoundaries()
    {
        foreach (string lineBreak in new[] { "\n", "\r\n", "\r", "\u2028", "\u2029" })
        {
            GeneratedSchemaPayload payload = CommandPayload(CommandSource(description: "first" + lineBreak + "field=forged|π"));
            AssertGolden(payload, "field-eol");
            payload.Json.Split('\n').Count(line => line.StartsWith("field=", StringComparison.Ordinal)).ShouldBe(1);
        }

        GeneratedSchemaPayload literal = CommandPayload(CommandSource(description: @"first\nfield=forged|π"));
        AssertGolden(literal, "field-literal");
        literal.Fingerprint.Value.ShouldNotBe(Goldens["field-eol"].Digest);
    }

    /// <summary>Requires warning/recovery cycles to update the complete schema rather than reuse stale field flags.</summary>
    [Fact]
    [Trait("Category", "MutationErrorRegression")]
    public void UnsupportedPayloadRecoveryCannotRetainAStaleCanonicalSchema()
    {
        string badSource = CommandSource(payloadType: "byte[]", withCount: true);
        string goodSource = CommandSource(withCount: true);
        string healthySource = CommandSource().Replace("TransferCommand", "HealthyCommand", StringComparison.Ordinal);
        CSharpCompilation initial = CompilationHelper.CreateCompilation([badSource, healthySource]);
        AssertCompiles(initial);
        CommandParseResult warned = CompilationHelper.ParseCommand(initial, "Canonical.Wire.TransferCommand");
        AssertUnsupportedWarning(initial, warned);
        AssertGolden(SchemaFingerprintTransform.CreateCommandPayload(McpManifestTransform.TransformCommand(warned.Model.ShouldNotBeNull())), "unsupported");
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator());
        driver = driver.RunGenerators(initial, TestContext.Current.CancellationToken);
        string[] healthyOutput = Output(driver.GetRunResult(), "HealthyCommand");
        healthyOutput.ShouldNotBeEmpty();
        driver.GetRunResult().Diagnostics.Single(diagnostic => diagnostic.Id == "HFC1002").Severity.ShouldBe(DiagnosticSeverity.Warning);

        SyntaxTree before = initial.SyntaxTrees.First();
        CSharpCompilation repaired = initial.ReplaceSyntaxTree(before, CSharpSyntaxTree.ParseText(goodSource,
            (CSharpParseOptions)before.Options, before.FilePath, cancellationToken: TestContext.Current.CancellationToken));
        AssertCompiles(repaired);
        CommandParseResult clean = CompilationHelper.ParseCommand(repaired, "Canonical.Wire.TransferCommand");
        clean.Diagnostics.ShouldBeEmpty();
        clean.Model.ShouldNotBeNull().Properties.Single(field => field.Name == "Payload").IsUnsupported.ShouldBeFalse();
        AssertGolden(SchemaFingerprintTransform.CreateCommandPayload(McpManifestTransform.TransformCommand(clean.Model!)), "supported");
        driver = driver.RunGenerators(repaired, TestContext.Current.CancellationToken);
        driver.GetRunResult().Diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == "HFC1002");
        Output(driver.GetRunResult(), "HealthyCommand").ShouldBe(healthyOutput);

        driver = driver.RunGenerators(initial, TestContext.Current.CancellationToken);
        CommandParseResult warnedAgain = CompilationHelper.ParseCommand(initial, "Canonical.Wire.TransferCommand");
        AssertUnsupportedWarning(initial, warnedAgain);
        AssertGolden(SchemaFingerprintTransform.CreateCommandPayload(McpManifestTransform.TransformCommand(warnedAgain.Model.ShouldNotBeNull())), "unsupported");
        driver.GetRunResult().Diagnostics.Count(diagnostic => diagnostic.Id == "HFC1002").ShouldBe(1);
        Output(driver.GetRunResult(), "HealthyCommand").ShouldBe(healthyOutput);
    }

    /// <summary>Checks the public descriptor defense-in-depth boundary, independently of upstream parser filtering.</summary>
    [Fact]
    [Trait("Category", "MutationErrorRegression")]
    public void RuntimeIdentityDescriptorsCannotEnterStructuralFingerprintMaterial()
    {
        McpCommandDescriptorModel business = ParseCommand(CommandSource());
        string[] runtimeNames = ["MessageId", "TenantId", "CorrelationId", "UserId", "Principal", "Claims", "Token"];
        foreach (string[] names in new[] { runtimeNames, runtimeNames.Select(name => name.ToLowerInvariant()).ToArray() })
        {
            McpParameterDescriptorModel[] runtime = names.Select(name => new McpParameterDescriptorModel(
                name, "String", "string", true, false, name, "runtime identity", [], false)).ToArray();
            McpCommandDescriptorModel atPublicBoundary = new(business.ProtocolName, business.CommandTypeName, business.BoundedContext,
                business.Title, business.Description, business.AuthorizationPolicyName, [.. runtime, .. business.Parameters], business.DerivablePropertyNames);
            GeneratedSchemaPayload payload = SchemaFingerprintTransform.CreateCommandPayload(atPublicBoundary);
            AssertGolden(payload, "identity-filtered");
            payload.Json.Split('\n').Where(line => line.StartsWith("field=", StringComparison.Ordinal)).ShouldHaveSingleItem();
            foreach (string name in names)
            {
                payload.Json.ShouldNotContain("field=" + name + "|");
            }
        }
    }

    private static void AssertGolden(GeneratedSchemaPayload payload, string vector)
    {
        (string expected, string digest) = Goldens[vector];
        payload.Json.ShouldBe(expected);
        payload.Json.ShouldEndWith("\n");
        payload.Json.ShouldNotContain("\r");
        payload.Fingerprint.AlgorithmId.ShouldBe("frontcomposer.schema.sha256.v1.sourcetools-blob");
        payload.Fingerprint.CanonicalizerVersion.ShouldBe("frontcomposer.canonical-json.v1");
        payload.Fingerprint.TestVectorId.ShouldBe("hfc-schema-v1");
        payload.Fingerprint.Value.ShouldBe(digest);
    }

    private static GeneratedSchemaPayload CommandPayload(string source) => SchemaFingerprintTransform.CreateCommandPayload(ParseCommand(source));

    private static McpCommandDescriptorModel ParseCommand(string source)
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        AssertCompiles(compilation);
        CommandParseResult result = CompilationHelper.ParseCommand(compilation, "Canonical.Wire.TransferCommand");
        result.Diagnostics.ShouldBeEmpty();
        return McpManifestTransform.TransformCommand(result.Model.ShouldNotBeNull());
    }

    private static GeneratedSchemaPayload ResourcePayload(string source)
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        AssertCompiles(compilation);
        ParseResult result = CompilationHelper.ParseProjection(compilation, "Canonical.Wire.WireProjection");
        result.Diagnostics.ShouldBeEmpty();
        return SchemaFingerprintTransform.CreateResourcePayload(McpManifestTransform.TransformProjection(result.Model.ShouldNotBeNull()));
    }

    private static void AssertUnsupportedWarning(CSharpCompilation compilation, CommandParseResult result)
    {
        result.Model.ShouldNotBeNull();
        DiagnosticInfo warning = result.Diagnostics.Single();
        warning.Id.ShouldBe("HFC1002");
        warning.Severity.ShouldBe("Warning");
        warning.Message.ShouldContain("Payload");
        warning.Message.ShouldContain("TransferCommand");
        warning.Message.ShouldContain("not supported");
        IPropertySymbol property = compilation.GetTypeByMetadataName("Canonical.Wire.TransferCommand")!
            .GetMembers("Payload").OfType<IPropertySymbol>().Single();
        FileLinePositionSpan position = property.Locations.Single().GetLineSpan();
        warning.FilePath.ShouldBe(position.Path);
        warning.Line.ShouldBe(position.StartLinePosition.Line);
        warning.Column.ShouldBe(position.StartLinePosition.Character);
    }

    private static void AssertCompiles(CSharpCompilation compilation) => compilation.GetDiagnostics(TestContext.Current.CancellationToken)
        .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.ToString()).ShouldBeEmpty();

    private static string[] Output(GeneratorDriverRunResult result, string name) => result.Results.Single().GeneratedSources
        .Where(source => source.HintName.Contains(name, StringComparison.Ordinal)).OrderBy(source => source.HintName, StringComparer.Ordinal)
        .Select(source => source.HintName + "\n" + source.SourceText).ToArray();

    private static string Literal(string value) => Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(value, quote: true);

    private static string CommandSource(string commandTitle = "Execute", string label = "Payload", string description = "Data", string payloadType = "string", bool withCount = false) => $$"""
        using System.ComponentModel;
        using System.ComponentModel.DataAnnotations;
        using Hexalith.FrontComposer.Contracts.Attributes;
        namespace Canonical.Wire;
        [Command]
        [BoundedContext("WireOps")]
        [RequiresPolicy("Wire.Invoke")]
        [Display(Name = {{Literal(commandTitle)}})]
        public partial class TransferCommand
        {
            public string MessageId { get; set; } = string.Empty;
            {{(withCount ? "public int Count { get; set; }" : string.Empty)}}
            [Display(Name = {{Literal(label)}})]
            [Description({{Literal(description)}})]
            public {{payloadType}} Payload { get; set; } = default!;
        }
        """;

    private static string ProjectionSource(string label, string description) => $$"""
        using System.ComponentModel;
        using System.ComponentModel.DataAnnotations;
        using Hexalith.FrontComposer.Contracts.Attributes;
        namespace Canonical.Wire;
        [Projection]
        [BoundedContext("WireOps")]
        [Display(Name = "Queue", GroupName = "Queues")]
        public partial class WireProjection
        {
            [Display(Name = {{Literal(label)}})]
            [Description({{Literal(description)}})]
            public string Name { get; set; } = string.Empty;
            public Stage Status { get; set; }
        }
        public enum Stage
        {
            [ProjectionBadge(BadgeSlot.Warning)] zeta,
            [ProjectionBadge(BadgeSlot.Success)] Alpha,
        }
        """;

    private static string Lines(params string[] lines) => string.Join("\n", lines) + "\n";

    // Independently derived UTF-8 SHA-256 wire vectors. Expected LF bytes do not depend on checkout EOL.
    private static readonly Dictionary<string, (string Blob, string Digest)> Goldens =
        new Dictionary<string, (string Blob, string Digest)>(StringComparer.Ordinal)
        {
            ["command-title-row"] = (Lines(
                "root=frontcomposer.schema.contract.v1",
                "family=CommandTool",
                "contractId=WireOps.TransferCommand.Execute",
                "schemaVersion=frontcomposer.command-tool.v1",
                "boundedContext=WireOps",
                "fqn=Canonical.Wire.TransferCommand",
                "protocol=WireOps.TransferCommand.Execute",
                "collections=fields:non-structural-sorted:name",
                "field=Payload|String|string|required|not-null|Payload|Data||supported||Default",
                "metadata.authorizationPolicy=Wire.Invoke",
                "metadata.derivablePropertyCount=1",
                "metadata.description=<absent>",
                "metadata.title=Execute\\nmetadata.description\\=forged\\|\u03b4"),
                "68bcc8a5d18bdb0e60eb62f00ce7cac24b0358fd5486fa9fdf84508b194ffd97"),
            ["command-title-literal"] = (Lines(
                "root=frontcomposer.schema.contract.v1",
                "family=CommandTool",
                "contractId=WireOps.TransferCommand.Execute",
                "schemaVersion=frontcomposer.command-tool.v1",
                "boundedContext=WireOps",
                "fqn=Canonical.Wire.TransferCommand",
                "protocol=WireOps.TransferCommand.Execute",
                "collections=fields:non-structural-sorted:name",
                "field=Payload|String|string|required|not-null|Payload|Data||supported||Default",
                "metadata.authorizationPolicy=Wire.Invoke",
                "metadata.derivablePropertyCount=1",
                "metadata.description=<absent>",
                "metadata.title=Execute\\\\nmetadata.description\\=forged\\|\u03b4"),
                "9c6ac29de26c6d0b8d5eb025a11f327c5612cba8b3f35968b0fb3cc056ced515"),
            ["command-pipe-label"] = (Lines(
                "root=frontcomposer.schema.contract.v1",
                "family=CommandTool",
                "contractId=WireOps.TransferCommand.Execute",
                "schemaVersion=frontcomposer.command-tool.v1",
                "boundedContext=WireOps",
                "fqn=Canonical.Wire.TransferCommand",
                "protocol=WireOps.TransferCommand.Execute",
                "collections=fields:non-structural-sorted:name",
                "field=Payload|String|string|required|not-null|A\\|B|C||supported||Default",
                "metadata.authorizationPolicy=Wire.Invoke",
                "metadata.derivablePropertyCount=1",
                "metadata.description=<absent>",
                "metadata.title=Execute"),
                "f9ba5eca17da1ecc7d29a1b32a62b5a2bb6ed995a673bcc5fb28c0060c496f30"),
            ["command-pipe-description"] = (Lines(
                "root=frontcomposer.schema.contract.v1",
                "family=CommandTool",
                "contractId=WireOps.TransferCommand.Execute",
                "schemaVersion=frontcomposer.command-tool.v1",
                "boundedContext=WireOps",
                "fqn=Canonical.Wire.TransferCommand",
                "protocol=WireOps.TransferCommand.Execute",
                "collections=fields:non-structural-sorted:name",
                "field=Payload|String|string|required|not-null|A|B\\|C||supported||Default",
                "metadata.authorizationPolicy=Wire.Invoke",
                "metadata.derivablePropertyCount=1",
                "metadata.description=<absent>",
                "metadata.title=Execute"),
                "d390d56428e654a292b8ff6c2b677d6cef58fe63946ff3e9f341a14d2d336af1"),
            ["resource-row"] = (Lines(
                "root=frontcomposer.schema.contract.v1",
                "family=ProjectionResource",
                "contractId=frontcomposer\\://WireOps/projections/WireProjection",
                "schemaVersion=frontcomposer.projection-resource.v1",
                "boundedContext=WireOps",
                "fqn=Canonical.Wire.WireProjection",
                "protocol=frontcomposer\\://WireOps/projections/WireProjection",
                "collections=fields:non-structural-sorted:name",
                "field=Name|Text|string|required|not-null|Label\\\\\\=\\|\\:\u03c0|detail\\nfield\\=Injected\\|String||supported||Default",
                "field=Status|Enum|string|required|not-null|Status|<absent>|Alpha,zeta|supported|Alpha:Success,zeta:Warning|Default",
                "metadata.description=<absent>",
                "metadata.emptyStateCtaCommandName=<absent>",
                "metadata.entityPluralLabel=Queues",
                "metadata.name=WireProjection",
                "metadata.renderStrategy=Default",
                "metadata.title=Queue"),
                "1ba75d302b4bb86a2b7128e53ac3d581e67c10dcb5f36cb70bc53c2e13869ce9"),
            ["resource-repaired"] = (Lines(
                "root=frontcomposer.schema.contract.v1",
                "family=ProjectionResource",
                "contractId=frontcomposer\\://WireOps/projections/WireProjection",
                "schemaVersion=frontcomposer.projection-resource.v1",
                "boundedContext=WireOps",
                "fqn=Canonical.Wire.WireProjection",
                "protocol=frontcomposer\\://WireOps/projections/WireProjection",
                "collections=fields:non-structural-sorted:name",
                "field=Name|Text|string|required|not-null|Label|Detail||supported||Default",
                "field=Status|Enum|string|required|not-null|Status|<absent>|Alpha,zeta|supported|Alpha:Success,zeta:Warning|Default",
                "metadata.description=<absent>",
                "metadata.emptyStateCtaCommandName=<absent>",
                "metadata.entityPluralLabel=Queues",
                "metadata.name=WireProjection",
                "metadata.renderStrategy=Default",
                "metadata.title=Queue"),
                "464462380923ca8976ef2a40766b722d9ca2ff0442793f6c0e51b3c7d33b52d7"),
            ["field-eol"] = (Lines(
                "root=frontcomposer.schema.contract.v1",
                "family=CommandTool",
                "contractId=WireOps.TransferCommand.Execute",
                "schemaVersion=frontcomposer.command-tool.v1",
                "boundedContext=WireOps",
                "fqn=Canonical.Wire.TransferCommand",
                "protocol=WireOps.TransferCommand.Execute",
                "collections=fields:non-structural-sorted:name",
                "field=Payload|String|string|required|not-null|Payload|first\\nfield\\=forged\\|\u03c0||supported||Default",
                "metadata.authorizationPolicy=Wire.Invoke",
                "metadata.derivablePropertyCount=1",
                "metadata.description=<absent>",
                "metadata.title=Execute"),
                "0e408f7a1e9f5cd8b76cdb0bb0e860936c93f13e145b5a1fa0cd546fe7ce1284"),
            ["field-literal"] = (Lines(
                "root=frontcomposer.schema.contract.v1",
                "family=CommandTool",
                "contractId=WireOps.TransferCommand.Execute",
                "schemaVersion=frontcomposer.command-tool.v1",
                "boundedContext=WireOps",
                "fqn=Canonical.Wire.TransferCommand",
                "protocol=WireOps.TransferCommand.Execute",
                "collections=fields:non-structural-sorted:name",
                "field=Payload|String|string|required|not-null|Payload|first\\\\nfield\\=forged\\|\u03c0||supported||Default",
                "metadata.authorizationPolicy=Wire.Invoke",
                "metadata.derivablePropertyCount=1",
                "metadata.description=<absent>",
                "metadata.title=Execute"),
                "8c89f3503fe42a9e58c8119b1af5804736db82bdae59a7728a381562d2d33a89"),
            ["unsupported"] = (Lines(
                "root=frontcomposer.schema.contract.v1",
                "family=CommandTool",
                "contractId=WireOps.TransferCommand.Execute",
                "schemaVersion=frontcomposer.command-tool.v1",
                "boundedContext=WireOps",
                "fqn=Canonical.Wire.TransferCommand",
                "protocol=WireOps.TransferCommand.Execute",
                "collections=fields:non-structural-sorted:name",
                "field=Count|Int32|number|required|not-null|Count|<absent>||supported||Default",
                "field=Payload|byte[]|object|optional|not-null|Payload|Data||unsupported||Default",
                "metadata.authorizationPolicy=Wire.Invoke",
                "metadata.derivablePropertyCount=1",
                "metadata.description=<absent>",
                "metadata.title=Execute"),
                "aa4c8f9855f2f27134b97217cdbec22b3eaa2a147e11370ba34e572edb24b400"),
            ["supported"] = (Lines(
                "root=frontcomposer.schema.contract.v1",
                "family=CommandTool",
                "contractId=WireOps.TransferCommand.Execute",
                "schemaVersion=frontcomposer.command-tool.v1",
                "boundedContext=WireOps",
                "fqn=Canonical.Wire.TransferCommand",
                "protocol=WireOps.TransferCommand.Execute",
                "collections=fields:non-structural-sorted:name",
                "field=Count|Int32|number|required|not-null|Count|<absent>||supported||Default",
                "field=Payload|String|string|required|not-null|Payload|Data||supported||Default",
                "metadata.authorizationPolicy=Wire.Invoke",
                "metadata.derivablePropertyCount=1",
                "metadata.description=<absent>",
                "metadata.title=Execute"),
                "0dfb025cba0443082327140ffead06eb3eea8e482eb6ac4826993b892c0b5bf6"),
            ["identity-filtered"] = (Lines(
                "root=frontcomposer.schema.contract.v1",
                "family=CommandTool",
                "contractId=WireOps.TransferCommand.Execute",
                "schemaVersion=frontcomposer.command-tool.v1",
                "boundedContext=WireOps",
                "fqn=Canonical.Wire.TransferCommand",
                "protocol=WireOps.TransferCommand.Execute",
                "collections=fields:non-structural-sorted:name",
                "field=Payload|String|string|required|not-null|Payload|Data||supported||Default",
                "metadata.authorizationPolicy=Wire.Invoke",
                "metadata.derivablePropertyCount=1",
                "metadata.description=<absent>",
                "metadata.title=Execute"),
                "9214489c35fda13cf31f8102ceb34de01e215c39b10e954852fc8ccc1535b8d8"),
        };
}
