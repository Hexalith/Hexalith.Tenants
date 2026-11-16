using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Hexalith.FrontComposer.SourceTools.Parsing;

/// <summary>
/// Formats Roslyn types as deterministic source syntax while retaining any required reference aliases.
/// </summary>
internal static class SourceTypeNameFormatter {
    private static readonly SymbolDisplayFormat FunctionPointerDisplayFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
            | SymbolDisplayMiscellaneousOptions.ExpandNullable);

    /// <summary>
    /// Formats <paramref name="typeSymbol"/> and returns the aliases needed to use the result.
    /// </summary>
    internal static string Format(
        ITypeSymbol typeSymbol,
        Compilation compilation,
        out EquatableArray<string> requiredExternAliases) {
        if (typeSymbol is null) {
            throw new ArgumentNullException(nameof(typeSymbol));
        }

        if (compilation is null) {
            throw new ArgumentNullException(nameof(compilation));
        }

        SortedSet<string> aliases = new(StringComparer.Ordinal);
        string result = FormatCore(typeSymbol, compilation, aliases, includeNullableAnnotation: false);
        requiredExternAliases = new EquatableArray<string>(ImmutableArray.CreateRange(aliases));
        return result;
    }

    private static string FormatCore(
        ITypeSymbol typeSymbol,
        Compilation compilation,
        SortedSet<string> aliases,
        bool includeNullableAnnotation = true) {
        switch (typeSymbol) {
            case IArrayTypeSymbol arrayType:
                return FormatArrayType(arrayType, compilation, aliases);

            case IPointerTypeSymbol pointerType:
                return FormatCore(pointerType.PointedAtType, compilation, aliases) + "*";

            case IFunctionPointerTypeSymbol functionPointerType:
                CollectFunctionPointerAliases(functionPointerType, compilation, aliases);
                return functionPointerType.ToDisplayString(FunctionPointerDisplayFormat);

            case IDynamicTypeSymbol:
                return AppendNullableAnnotation(typeSymbol, "global::System.Object", includeNullableAnnotation);

            case ITypeParameterSymbol typeParameter:
                return AppendNullableAnnotation(typeParameter, EscapeIdentifier(typeParameter.Name), includeNullableAnnotation);

            case INamedTypeSymbol namedType:
                return FormatNamedType(namedType, compilation, aliases, includeNullableAnnotation);

            default:
                return typeSymbol.ToDisplayString(FunctionPointerDisplayFormat);
        }
    }

    private static string FormatArrayType(
        IArrayTypeSymbol arrayType,
        Compilation compilation,
        SortedSet<string> aliases) {
        List<(int Rank, bool IsNullable)> layers = [];
        ITypeSymbol elementType = arrayType;
        while (elementType is IArrayTypeSymbol currentArray) {
            layers.Add((currentArray.Rank, currentArray.NullableAnnotation == NullableAnnotation.Annotated));
            elementType = currentArray.ElementType;
        }

        StringBuilder builder = new(FormatCore(elementType, compilation, aliases));
        for (int i = 0; i < layers.Count; i++) {
            int rank = layers[i].Rank;
            // C# writes the outer array's nullable suffix after all nested rank specifiers,
            // so nullable annotations occupy the reverse positions of Roslyn's outer-first layers.
            bool isNullable = layers[layers.Count - i - 1].IsNullable;
            _ = builder.Append('[').Append(',', rank - 1).Append(']');
            if (isNullable) {
                _ = builder.Append('?');
            }
        }

        return builder.ToString();
    }

    private static string AppendNullableAnnotation(
        ITypeSymbol typeSymbol,
        string sourceTypeName,
        bool includeNullableAnnotation)
        => includeNullableAnnotation
            && typeSymbol.NullableAnnotation == NullableAnnotation.Annotated
            && (typeSymbol.IsReferenceType || typeSymbol is ITypeParameterSymbol)
                ? sourceTypeName + "?"
                : sourceTypeName;

    private static string FormatNamedType(
        INamedTypeSymbol namedType,
        Compilation compilation,
        SortedSet<string> aliases,
        bool includeNullableAnnotation) {
        StringBuilder builder = new();
        if (namedType.ContainingType is not null) {
            _ = builder.Append(FormatNamedType(
                namedType.ContainingType,
                compilation,
                aliases,
                includeNullableAnnotation: false)).Append('.');
        }
        else {
            _ = builder.Append(GetRootQualifier(namedType.ContainingAssembly, compilation, aliases));
            AppendNamespace(builder, namedType.ContainingNamespace);
        }

        _ = builder.Append(EscapeIdentifier(namedType.Name));
        if (namedType.Arity > 0) {
            _ = builder.Append('<');
            int firstOwnTypeArgument = namedType.TypeArguments.Length - namedType.Arity;
            for (int i = firstOwnTypeArgument; i < namedType.TypeArguments.Length; i++) {
                if (i > firstOwnTypeArgument) {
                    _ = builder.Append(", ");
                }

                _ = builder.Append(FormatCore(namedType.TypeArguments[i], compilation, aliases));
            }

            _ = builder.Append('>');
        }

        return AppendNullableAnnotation(namedType, builder.ToString(), includeNullableAnnotation);
    }

    private static string GetRootQualifier(
        IAssemblySymbol? assembly,
        Compilation compilation,
        SortedSet<string> requiredAliases) {
        if (assembly is null || SymbolEqualityComparer.Default.Equals(assembly, compilation.Assembly)) {
            return "global::";
        }

        SortedSet<string> aliases = new(StringComparer.Ordinal);
        bool globallyReachable = false;
        foreach (MetadataReference reference in compilation.References) {
            ISymbol? referencedSymbol = compilation.GetAssemblyOrModuleSymbol(reference);
            IAssemblySymbol? referencedAssembly = referencedSymbol switch {
                IAssemblySymbol assemblySymbol => assemblySymbol,
                IModuleSymbol moduleSymbol => moduleSymbol.ContainingAssembly,
                _ => null,
            };
            if (!SymbolEqualityComparer.Default.Equals(referencedAssembly, assembly)) {
                continue;
            }

            ImmutableArray<string> referenceAliases = reference.Properties.Aliases;
            if (referenceAliases.IsDefaultOrEmpty) {
                globallyReachable = true;
                continue;
            }

            foreach (string alias in referenceAliases) {
                if (string.Equals(alias, "global", StringComparison.Ordinal)) {
                    globallyReachable = true;
                }
                else {
                    _ = aliases.Add(alias);
                }
            }
        }

        if (globallyReachable || aliases.Count == 0) {
            return "global::";
        }

        string selectedAlias = aliases.Max!;
        _ = requiredAliases.Add(selectedAlias);
        return EscapeIdentifier(selectedAlias) + "::";
    }

    private static void AppendNamespace(StringBuilder builder, INamespaceSymbol? namespaceSymbol) {
        if (namespaceSymbol is null || namespaceSymbol.IsGlobalNamespace) {
            return;
        }

        Stack<string> parts = new();
        INamespaceSymbol? current = namespaceSymbol;
        while (current is not null && !current.IsGlobalNamespace) {
            parts.Push(EscapeIdentifier(current.Name));
            current = current.ContainingNamespace;
        }

        while (parts.Count > 0) {
            _ = builder.Append(parts.Pop()).Append('.');
        }
    }

    private static void CollectFunctionPointerAliases(
        IFunctionPointerTypeSymbol functionPointerType,
        Compilation compilation,
        SortedSet<string> aliases) {
        _ = FormatCore(functionPointerType.Signature.ReturnType, compilation, aliases);
        foreach (IParameterSymbol parameter in functionPointerType.Signature.Parameters) {
            _ = FormatCore(parameter.Type, compilation, aliases);
        }
    }

    internal static string EscapeIdentifier(string identifier)
        => SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None
            || SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None
                ? "@" + identifier
                : identifier;
}
