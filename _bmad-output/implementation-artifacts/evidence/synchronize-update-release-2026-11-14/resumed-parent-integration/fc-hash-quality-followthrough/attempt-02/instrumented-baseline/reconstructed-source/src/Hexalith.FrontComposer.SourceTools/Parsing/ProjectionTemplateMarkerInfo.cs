namespace Hexalith.FrontComposer.SourceTools.Parsing;

/// <summary>
/// Story 6-2 T3 — incremental-cache-friendly result of parsing a single
/// <c>[ProjectionTemplate]</c> marker. Carries the validated descriptor metadata plus any
/// diagnostics that should be reported per marker.
/// </summary>
public sealed class ProjectionTemplateMarkerResult : IEquatable<ProjectionTemplateMarkerResult> {
    public ProjectionTemplateMarkerResult(
        ProjectionTemplateMarkerInfo? marker,
        EquatableArray<DiagnosticInfo> diagnostics) {
        Marker = marker;
        Diagnostics = diagnostics;
    }

    public ProjectionTemplateMarkerInfo? Marker { get; }

    public EquatableArray<DiagnosticInfo> Diagnostics { get; }

    public bool Equals(ProjectionTemplateMarkerResult? other)
        => other is not null
        && Equals(Marker, other.Marker)
        && Diagnostics.Equals(other.Diagnostics);

    public override bool Equals(object? obj) => Equals(obj as ProjectionTemplateMarkerResult);

    public override int GetHashCode() {
        PrivateHashActivity.Record(10);
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (Marker?.GetHashCode() ?? 0);
            hash = (hash * 31) + Diagnostics.GetHashCode();
            return hash;
        }
    }
}

/// <summary>
/// Story 6-2 T3 — value-equality IR captured for each <c>[ProjectionTemplate]</c> marker.
/// All fields are strings or value types so the type slots cleanly into Roslyn's incremental
/// generator cache.
/// </summary>
public sealed class ProjectionTemplateMarkerInfo : IEquatable<ProjectionTemplateMarkerInfo> {
    public ProjectionTemplateMarkerInfo(
        string templateTypeFullName,
        string templateNamespace,
        string templateTypeName,
        string projectionTypeFullName,
        string projectionNamespace,
        string projectionTypeName,
        string? role,
        int expectedContractVersion,
        string filePath,
        int line,
        int column) {
        TemplateTypeFullName = templateTypeFullName;
        TemplateNamespace = templateNamespace;
        TemplateTypeName = templateTypeName;
        ProjectionTypeFullName = projectionTypeFullName;
        ProjectionNamespace = projectionNamespace;
        ProjectionTypeName = projectionTypeName;
        Role = role;
        ExpectedContractVersion = expectedContractVersion;
        FilePath = filePath;
        Line = line;
        Column = column;
    }

    /// <summary>The template's fully qualified type name (namespace + type name).</summary>
    public string TemplateTypeFullName { get; }

    public string TemplateNamespace { get; }

    public string TemplateTypeName { get; }

    /// <summary>The projection's fully qualified type name (namespace + type name).</summary>
    public string ProjectionTypeFullName { get; }

    public string ProjectionNamespace { get; }

    public string ProjectionTypeName { get; }

    /// <summary>The optional <c>ProjectionRole</c> name; <see langword="null"/> when the
    /// marker did not specify a role filter (matches every role of the projection).</summary>
    public string? Role { get; }

    public int ExpectedContractVersion { get; }

    public string FilePath { get; }

    public int Line { get; }

    public int Column { get; }

    public bool Equals(ProjectionTemplateMarkerInfo? other)
        => other is not null
        && TemplateTypeFullName == other.TemplateTypeFullName
        && TemplateNamespace == other.TemplateNamespace
        && TemplateTypeName == other.TemplateTypeName
        && ProjectionTypeFullName == other.ProjectionTypeFullName
        && ProjectionNamespace == other.ProjectionNamespace
        && ProjectionTypeName == other.ProjectionTypeName
        && Role == other.Role
        && ExpectedContractVersion == other.ExpectedContractVersion
        && FilePath == other.FilePath
        && Line == other.Line
        && Column == other.Column;

    public override bool Equals(object? obj) => Equals(obj as ProjectionTemplateMarkerInfo);

    public override int GetHashCode() {
        PrivateHashActivity.Record(9);
        unchecked {
            int hash = 17;
            hash = (hash * 31) + (TemplateTypeFullName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (TemplateNamespace?.GetHashCode() ?? 0);
            hash = (hash * 31) + (TemplateTypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (ProjectionTypeFullName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (ProjectionNamespace?.GetHashCode() ?? 0);
            hash = (hash * 31) + (ProjectionTypeName?.GetHashCode() ?? 0);
            hash = (hash * 31) + (Role?.GetHashCode() ?? 0);
            hash = (hash * 31) + ExpectedContractVersion.GetHashCode();
            hash = (hash * 31) + (FilePath?.GetHashCode() ?? 0);
            hash = (hash * 31) + Line.GetHashCode();
            hash = (hash * 31) + Column.GetHashCode();
            return hash;
        }
    }
}
