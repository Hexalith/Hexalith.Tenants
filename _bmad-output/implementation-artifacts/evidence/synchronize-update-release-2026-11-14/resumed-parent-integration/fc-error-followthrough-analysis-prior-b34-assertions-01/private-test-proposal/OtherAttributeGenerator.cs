using Hexalith.FrontComposer.SourceTools.Parsing;

using Microsoft.CodeAnalysis;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Invokes the public template parser through a legitimate nonmatching attribute provider.</summary>
internal sealed class OtherAttributeGenerator : IIncrementalGenerator
{
    /// <summary>Gets the template parse results delivered by the provider.</summary>
    internal List<ProjectionTemplateMarkerResult> Results { get; } = [];

    /// <summary>Registers the provider and collects its public parser results.</summary>
    /// <param name="context">The incremental generator initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ProjectionTemplateMarkerResult> parsed = context.SyntaxProvider.ForAttributeWithMetadataName(
            "System.ObsoleteAttribute",
            static (_, _) => true,
            static (attribute, cancellationToken) => ProjectionTemplateMarkerParser.Parse(attribute, cancellationToken));
        context.RegisterSourceOutput(parsed, (_, result) => Results.Add(result));
    }
}
