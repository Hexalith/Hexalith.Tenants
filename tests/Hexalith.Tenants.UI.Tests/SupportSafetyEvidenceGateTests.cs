using System.Text;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

/// <summary>
/// Mechanical gate over this test assembly's own sources. Two prose bans failed to remove
/// <c>ToString().ShouldNotContain(</c> support-safety assertions, so the ban is now enforced by a test that
/// fails naming file and line. Such an assertion runs against a hand-written fixed format string that emits
/// only enums, bools, and counts: it can never fail, therefore it certifies nothing, and offering it as
/// non-disclosure evidence lets a green suite claim safety it never observed.
/// </summary>
public sealed class SupportSafetyEvidenceGateTests
{
    // Assembled from fragments so this gate's own source cannot trip it.
    private const string BannedAssertion = "ToString()" + ".ShouldNotContain(";

    [Fact]
    public void No_test_offers_a_ToString_substring_check_as_support_safety_evidence()
    {
        IReadOnlyList<string> occurrences = FindBannedAssertions(TestProjectRoot());

        occurrences.ShouldBeEmpty(
            "Support-safety evidence must be placed on a surface where disclosure is actually possible "
            + "(rendered markup, the canonical URL, JS-interop invocations, or the log sink), with a control "
            + "case proving the assertion can fail. Remaining occurrences:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, occurrences));
    }

    [Fact]
    public void The_banned_assertion_scanner_reports_file_and_line_when_an_occurrence_exists()
    {
        // Proves the gate above can fail: the scanner is pointed at a directory that deliberately contains
        // one occurrence, and it must report that occurrence with its file name and 1-based line number.
        string directory = Path.Combine(Path.GetTempPath(), $"tenants-support-safety-gate-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(directory);
        try
        {
            string file = Path.Combine(directory, "PlantedTests.cs");
            File.WriteAllText(
                file,
                "public sealed class PlantedTests" + Environment.NewLine
                + "{" + Environment.NewLine
                + "    public void Planted() => snapshot." + BannedAssertion + "\"secret\");" + Environment.NewLine
                + "}" + Environment.NewLine,
                Encoding.UTF8);

            IReadOnlyList<string> occurrences = FindBannedAssertions(directory);

            string reported = occurrences.ShouldHaveSingleItem();
            reported.ShouldContain("PlantedTests.cs");
            reported.ShouldContain("line 3");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static IReadOnlyList<string> FindBannedAssertions(string root)
    {
        List<string> occurrences = [];
        foreach (string path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(root, path);
            if (relative.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || relative.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(path);
            for (int index = 0; index < lines.Length; index++)
            {
                if (lines[index].Contains(BannedAssertion, StringComparison.Ordinal)
                    && !lines[index].TrimStart().StartsWith("//", StringComparison.Ordinal))
                {
                    occurrences.Add($"{relative} line {index + 1}: {lines[index].Trim()}");
                }
            }
        }

        return occurrences;
    }

    private static string TestProjectRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "Hexalith.Tenants.UI.Tests.csproj")))
        {
            directory = directory.Parent;
        }

        return directory
            .ShouldNotBeNull("The UI test project root must be discoverable for the source scan.")
            .FullName;
    }
}
