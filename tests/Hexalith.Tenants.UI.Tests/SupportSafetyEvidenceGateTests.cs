using System.Text;
using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

/// <summary>
/// Mechanical gate over this test assembly's own sources. Two prose bans failed to remove
/// <c>ToString().ShouldNotContain(</c> support-safety assertions, so the ban is now enforced by a test that
/// fails naming file and line. Such an assertion runs against a hand-written fixed format string that emits
/// only enums, bools, and counts: it can never fail, therefore it certifies nothing, and offering it as
/// non-disclosure evidence lets a green suite claim safety it never observed.
/// </summary>
public sealed partial class SupportSafetyEvidenceGateTests
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
                || relative.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal)

                // This gate necessarily names the pattern it bans. Its own ability to fail is proven by
                // the planted-file test below, not by scanning its own source.
                || string.Equals(Path.GetFileName(path), "SupportSafetyEvidenceGateTests.cs", StringComparison.Ordinal))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(path);
            // Matching the literal spelling alone was trivially evaded four times inside this very
            // directory: "(x.ToString() ?? string.Empty).ShouldNotContain(" and "var t = x.ToString();"
            // followed by "t.ShouldNotContain(" both slipped through while being exactly the pattern this
            // gate exists to ban. Two rules are applied instead of one literal.
            Dictionary<string, int> stringifiedLocals = new(StringComparer.Ordinal);
            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index];
                if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                // Rule 1 -- one statement stringifies and asserts absence, however it is spelled.
                if (line.Contains("ToString()", StringComparison.Ordinal)
                    && line.Contains("ShouldNotContain", StringComparison.Ordinal))
                {
                    occurrences.Add($"{relative} line {index + 1}: {line.Trim()}");
                    continue;
                }

                // Rule 2 -- the value is parked in a local first, then asserted on a later line.
                Match assignment = StringifiedLocalPattern().Match(line);
                if (assignment.Success)
                {
                    stringifiedLocals[assignment.Groups["name"].Value] = index + 1;
                    continue;
                }

                foreach ((string name, int declaredAt) in stringifiedLocals)
                {
                    if (line.Contains($"{name}.ShouldNotContain", StringComparison.Ordinal))
                    {
                        occurrences.Add(
                            $"{relative} line {index + 1}: {line.Trim()} "
                            + $"(stringified at line {declaredAt})");
                    }
                }
            }
        }

        return occurrences;
    }

    [GeneratedRegex(@"^\s*(?:var|string)\s+(?<name>\w+)\s*=.*\bToString\(\)")]
    private static partial Regex StringifiedLocalPattern();

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
