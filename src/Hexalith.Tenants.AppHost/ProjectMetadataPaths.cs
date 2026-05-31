namespace Projects;

internal static class ProjectMetadataPaths {
    public static string GetProjectPath(params string[] path)
        => Path.Combine(GetRepositoryRoot(), Path.Combine(path));

    private static string GetRepositoryRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
