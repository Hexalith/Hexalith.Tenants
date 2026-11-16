using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Protects public mapping, label, schema and equality behavior exposed by the Error inventory.</summary>
[Trait("Category", "MutationErrorRegression")]
public sealed class ErrorFollowthroughBehaviorTests
{
    /// <summary>Preserves each supported C# primitive alias and its public IR mapping.</summary>
    /// <param name="alias">The C# primitive alias.</param>
    /// <param name="expected">The supported public IR type.</param>
    [Theory]
    [InlineData("string", "String")]
    [InlineData("int", "Int32")]
    [InlineData("long", "Int64")]
    [InlineData("decimal", "Decimal")]
    [InlineData("double", "Double")]
    [InlineData("float", "Single")]
    [InlineData("bool", "Boolean")]
    public void PrimitiveAliasesRetainTheirDeclaredMapping(string alias, string expected)
        => FieldTypeMapper.MapType(alias, isEnum: false).ShouldBe(expected);

    /// <summary>Recognizes supported enumerable forms without accepting a lookalike type prefix.</summary>
    /// <param name="name">The candidate collection type name.</param>
    /// <param name="expected">Whether the collection shape is supported.</param>
    [Theory]
    [InlineData("System.Collections.Generic.IEnumerable", true)]
    [InlineData("System.Collections.Generic.IEnumerable<System.String>", true)]
    [InlineData("System.Collections.Generic.IEnumerableAlias", false)]
    public void EnumerableRecognitionRespectsTheGenericBoundary(string name, bool expected)
        => FieldTypeMapper.IsCollectionType(name).ShouldBe(expected);

    /// <summary>Keeps user-facing acronym, ASCII-boundary and digit labels readable.</summary>
    /// <param name="identifier">The source identifier shown to a user.</param>
    /// <param name="expected">The visible label.</param>
    [Theory]
    [InlineData("ABCd", "AB Cd")]
    [InlineData("ABc", "ABc")]
    [InlineData("XYZ", "XYZ")]
    [InlineData("AZName", "AZ Name")]
    [InlineData("AABx", "AA Bx")]
    [InlineData("XMLz", "XM Lz")]
    [InlineData("xA", "X A")]
    [InlineData("pAName", "P AName")]
    [InlineData("Order2Name", "Order2 Name")]
    public void LabelBoundariesPreserveVisibleWords(string identifier, string expected)
        => CamelCaseHumanizer.Humanize(identifier).ShouldBe(expected);

    /// <summary>Retains the renderer capability and bounds that a schema consumer negotiates.</summary>
    [Fact]
    public void MarkdownPayloadDescribesTheCompleteRendererContract()
    {
        GeneratedSchemaPayload payload = SchemaFingerprintTransform.CreateMarkdownRendererPayload("renderer", "Default", 64000, 4096);
        payload.Json.ShouldContain("family=MarkdownRendererContract\n");
        payload.Json.ShouldContain("contractId=renderer\n");
        payload.Json.ShouldContain("schemaVersion=frontcomposer.renderer.markdown.v1\n");
        payload.Json.ShouldContain("protocol=renderer\n");
        payload.Json.ShouldContain("boundedContext=\n");
        payload.Json.ShouldContain("fqn=\n");
        payload.Json.ShouldContain("metadata.bounds.maxCharacters=64000\n");
        payload.Json.ShouldContain("metadata.bounds.maxFieldCharacters=4096\n");
        payload.Json.ShouldContain("metadata.capability=bounded-markdown\\|sanitized-inert-text\n");
        payload.Json.ShouldContain("metadata.outputContentType=text/markdown\n");
        payload.Json.ShouldContain("metadata.renderStrategy=Default\n");

        GeneratedSchemaPayload changedId = SchemaFingerprintTransform.CreateMarkdownRendererPayload("other-renderer", "Default", 64000, 4096);
        GeneratedSchemaPayload changedStrategy = SchemaFingerprintTransform.CreateMarkdownRendererPayload("renderer", "ActionQueue", 64000, 4096);
        GeneratedSchemaPayload changedMaxCharacters = SchemaFingerprintTransform.CreateMarkdownRendererPayload("renderer", "Default", 64001, 4096);
        GeneratedSchemaPayload changedMaxFieldCharacters = SchemaFingerprintTransform.CreateMarkdownRendererPayload("renderer", "Default", 64000, 4097);
        payload.Fingerprint.Value.ShouldNotBe(changedId.Fingerprint.Value);
        payload.Fingerprint.Value.ShouldNotBe(changedStrategy.Fingerprint.Value);
        payload.Fingerprint.Value.ShouldNotBe(changedMaxCharacters.Fingerprint.Value);
        payload.Fingerprint.Value.ShouldNotBe(changedMaxFieldCharacters.Fingerprint.Value);
    }

    /// <summary>Retains skill identity, negotiated metadata and canonical ordering of reference sets.</summary>
    [Fact]
    public void SkillPayloadRetainsMetadataAndCanonicalReferenceOrder()
    {
        GeneratedSchemaPayload payload = SchemaFingerprintTransform.CreateSkillCorpusResourcePayload("skill", "1.0", "frontcomposer://skill", "Skill title", "text/markdown", 7, ["Z", "A"], ["z.cs", "a.cs"]);
        payload.Json.ShouldContain("family=SkillCorpusResource\n");
        payload.Json.ShouldContain("contractId=skill\n");
        payload.Json.ShouldContain("schemaVersion=frontcomposer.skill-corpus.v1\n");
        payload.Json.ShouldContain("boundedContext=\n");
        payload.Json.ShouldContain("fqn=\n");
        payload.Json.ShouldContain("protocol=frontcomposer\\://skill\n");
        payload.Json.ShouldContain("metadata.contentType=text/markdown\n");
        payload.Json.ShouldContain("metadata.order=7\n");
        payload.Json.ShouldContain("metadata.publicApiReferences=A\\|Z\n");
        payload.Json.ShouldContain("metadata.samplePaths=a.cs\\|z.cs\n");
        payload.Json.ShouldContain("metadata.title=Skill title\n");
        payload.Json.ShouldContain("metadata.version=1.0\n");

        GeneratedSchemaPayload changedId = SchemaFingerprintTransform.CreateSkillCorpusResourcePayload("other-skill", "1.0", "frontcomposer://skill", "Skill title", "text/markdown", 7, ["Z", "A"], ["z.cs", "a.cs"]);
        GeneratedSchemaPayload changedResourceUri = SchemaFingerprintTransform.CreateSkillCorpusResourcePayload("skill", "1.0", "frontcomposer://other-skill", "Skill title", "text/markdown", 7, ["Z", "A"], ["z.cs", "a.cs"]);
        payload.Fingerprint.Value.ShouldNotBe(changedId.Fingerprint.Value);
        payload.Fingerprint.Value.ShouldNotBe(changedResourceUri.Fingerprint.Value);
    }

    /// <summary>Distinguishes nullable schema fields in both canonical material and negotiated fingerprint.</summary>
    [Fact]
    public void FieldNullabilityChangesTheNegotiatedSchemaContract()
    {
        McpCommandDescriptorModel Model(bool nullable) => new("test.Execute", "Tests.Command", "Tests", "Test", null, null,
            [new McpParameterDescriptorModel("Value", "String", "string", false, nullable, "Value", null, [], false)], []);
        GeneratedSchemaPayload nullable = SchemaFingerprintTransform.CreateCommandPayload(Model(true));
        GeneratedSchemaPayload required = SchemaFingerprintTransform.CreateCommandPayload(Model(false));
        nullable.Json.ShouldContain("|optional|nullable|");
        required.Json.ShouldContain("|optional|not-null|");
        nullable.Fingerprint.Value.ShouldNotBe(required.Fingerprint.Value);
    }

    /// <summary>Preserves generic equality comparison between absent and present projection results.</summary>
    [Fact]
    public void ProjectionResultComparisonIsReflexiveAndDistinguishesAnAbsentModel()
    {
        DomainModel model = new("Projection", "Tests", null, null, null, default);
        ParseResult present = new(model, default);
        ParseResult absent = new(null, default);
        EqualityComparer<ParseResult>.Default.Equals(present, present).ShouldBeTrue();
        EqualityComparer<ParseResult>.Default.Equals(present, absent).ShouldBeFalse();
        EqualityComparer<ParseResult>.Default.Equals(absent, present).ShouldBeFalse();
        present.Equals((ParseResult?)null).ShouldBeFalse();
    }

    /// <summary>Preserves diagnostic value equality and distinguishes every identity field.</summary>
    [Fact]
    public void DiagnosticComparisonIsReflexiveAndRejectsNull()
    {
        DiagnosticInfo diagnostic = new("HFC1005", "Invalid attribute", "Warning", "Projection.cs", 2, 3);
        DiagnosticInfo equal = new("HFC1005", "Invalid attribute", "Warning", "Projection.cs", 2, 3);
        EqualityComparer<DiagnosticInfo>.Default.Equals(diagnostic, diagnostic).ShouldBeTrue();
        EqualityComparer<DiagnosticInfo>.Default.Equals(diagnostic, equal).ShouldBeTrue();
        diagnostic.GetHashCode().ShouldBe(equal.GetHashCode());
        DiagnosticInfo[] different = [
            new("HFC1006", "Invalid attribute", "Warning", "Projection.cs", 2, 3),
            new("HFC1005", "Other attribute", "Warning", "Projection.cs", 2, 3),
            new("HFC1005", "Invalid attribute", "Error", "Projection.cs", 2, 3),
            new("HFC1005", "Invalid attribute", "Warning", "Other.cs", 2, 3),
            new("HFC1005", "Invalid attribute", "Warning", "Projection.cs", 3, 3),
            new("HFC1005", "Invalid attribute", "Warning", "Projection.cs", 2, 4),
        ];
        foreach (DiagnosticInfo changed in different)
        {
            EqualityComparer<DiagnosticInfo>.Default.Equals(diagnostic, changed).ShouldBeFalse();
        }
        diagnostic.Equals((DiagnosticInfo?)null).ShouldBeFalse();
    }

    /// <summary>Preserves boxed and operator equality of the public immutable array wrapper.</summary>
    [Fact]
    public void ArrayComparisonPreservesValueAndBoxedTypeBoundaries()
    {
        EquatableArray<string> first = new(ImmutableArray.Create("A"));
        EquatableArray<string> equal = new(ImmutableArray.Create("A"));
        EquatableArray<string> different = new(ImmutableArray.Create("B"));
        (first != equal).ShouldBeFalse();
        (first != different).ShouldBeTrue();
        first.Equals((object)equal).ShouldBeTrue();
        first.Equals((object)different).ShouldBeFalse();
        first.Equals((object)"A").ShouldBeFalse();
    }
}
