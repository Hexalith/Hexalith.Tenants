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
/// <remarks>
/// The scan is statement-based, not line-based. A line-based scan was evaded by every spelling a formatter
/// or a nullable annotation produces: <c>string? text = x.ToString();</c> (the <c>?</c> defeated a
/// <c>string\s+</c> pattern), an assignment to an already-declared local, a fluent chain wrapped across two
/// physical lines, and <c>$"{x}".ShouldNotContain(</c>, which stringifies with no literal
/// <c>ToString()</c> in sight. Each rule below carries its own planted-failure test, so deleting any one
/// rule turns a test red rather than silently halving the gate.
/// </remarks>
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

    // The third argument pins WHICH rule fired: the "stringified at line N" marker is emitted only by
    // Rule 2. Without it a row could silently be caught by Rule 1 and leave Rule 2 unproven -- which is
    // exactly how the previous single planted case left half the scanner self-certifying.
    [Theory]

    // Rule 1, the plain spelling.
    [InlineData("    public void Planted() => snapshot.@BANNED@\"secret\");", 3, 0)]

    // Rule 1 must survive the formatter: the chain is one statement across two physical lines, and is
    // reported at the line the statement starts on.
    [InlineData("    public void Planted() => snapshot.ToString()\n            .ShouldNotContain(\"secret\");", 3, 0)]

    // Rule 1, coalesced subject -- the spelling that evaded the literal match four times.
    [InlineData("    public void Planted() => (snapshot.ToString() ?? string.Empty).ShouldNotContain(\"secret\");", 3, 0)]

    // Rule 1, absence asserted without ShouldNotContain at all.
    [InlineData("    public void Planted() => snapshot.ToString().Contains(\"secret\").ShouldBeFalse();", 3, 0)]

    // Rule 2, nullable declaration -- "string?" defeated the previous "string\\s+" pattern outright. The
    // assertion is a separate statement from the assignment, so Rule 1 cannot reach it and only Rule 2 can.
    [InlineData("    public void Planted()\n    {\n        string? text = snapshot.ToString();\n        text.ShouldNotContain(\"secret\");\n    }", 6, 5)]

    // Rule 2, assignment to an already-declared local, with no declaration keyword to match on.
    [InlineData("    public void Planted()\n    {\n        string text;\n        text = snapshot.ToString();\n        text.ShouldNotContain(\"secret\");\n    }", 7, 6)]

    // Rule 3, interpolation -- stringifies with no literal ToString() anywhere in the statement, so neither
    // Rule 1 nor Rule 2 can reach it.
    [InlineData("    public void Planted() => $\"{snapshot}\".ShouldNotContain(\"secret\");", 3, 0)]
    public void The_banned_assertion_scanner_reports_file_and_line_for_every_evadable_spelling(
        string plantedBody,
        int expectedLine,
        int stringifiedAtLine)
    {
        ArgumentNullException.ThrowIfNull(plantedBody);

        // Proves each rule can fail: the scanner is pointed at a directory that deliberately contains one
        // occurrence, and it must report it with file name and 1-based line number. Deleting any single rule
        // from FindBannedAssertions turns exactly the corresponding row red.
        string directory = Path.Combine(Path.GetTempPath(), $"tenants-support-safety-gate-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(directory);
        try
        {
            string file = Path.Combine(directory, "PlantedTests.cs");
            File.WriteAllText(
                file,
                "public sealed class PlantedTests" + Environment.NewLine
                + "{" + Environment.NewLine
                + plantedBody.Replace("@BANNED@", BannedAssertion, StringComparison.Ordinal)
                    .Replace("\n", Environment.NewLine, StringComparison.Ordinal) + Environment.NewLine
                + "}" + Environment.NewLine,
                Encoding.UTF8);

            IReadOnlyList<string> occurrences = FindBannedAssertions(directory);

            string reported = occurrences.ShouldHaveSingleItem();
            reported.ShouldContain("PlantedTests.cs");
            reported.ShouldContain($"line {expectedLine}");
            if (stringifiedAtLine > 0)
            {
                reported.ShouldContain($"stringified at line {stringifiedAtLine}");
            }
            else
            {
                reported.ShouldNotContain("stringified at");
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void The_banned_assertion_scanner_does_not_flag_an_absence_check_on_a_real_surface()
    {
        // The control case. Without it the gate could be "hardened" into matching everything, which would be
        // just as useless in the other direction: legitimate evidence on rendered markup must stay legal.
        string directory = Path.Combine(Path.GetTempPath(), $"tenants-support-safety-gate-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(
                Path.Combine(directory, "CleanTests.cs"),
                "public sealed class CleanTests" + Environment.NewLine
                + "{" + Environment.NewLine
                + "    public void Clean() => cut.Markup.ShouldNotContain(\"secret\");" + Environment.NewLine
                + "}" + Environment.NewLine,
                Encoding.UTF8);

            FindBannedAssertions(directory).ShouldBeEmpty();
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

                // This gate necessarily names the patterns it bans. Its own ability to fail is proven by the
                // planted-file theory above, not by scanning its own source. Matched on the path relative to
                // the scan root, not the bare file name, so an unrelated file that merely shares the name
                // cannot exempt itself.
                || string.Equals(relative, "SupportSafetyEvidenceGateTests.cs", StringComparison.Ordinal))
            {
                continue;
            }

            // Scoped per file: a local stringified in one file cannot implicate a same-named local elsewhere.
            Dictionary<string, int> stringifiedLocals = new(StringComparer.Ordinal);
            foreach ((string statement, int line) in Statements(File.ReadAllLines(path)))
            {
                // Rule 1 -- the statement stringifies and asserts absence, however either half is spelled.
                if (statement.Contains("ToString()", StringComparison.Ordinal) && AssertsAbsence(statement))
                {
                    occurrences.Add($"{relative} line {line}: {Summarize(statement)}");
                    continue;
                }

                // Rule 3 -- interpolation stringifies with no literal ToString() to match on. Only an
                // interpolated *subject* counts; an interpolated assertion message is legitimate.
                if (InterpolatedSubjectPattern().IsMatch(statement))
                {
                    occurrences.Add($"{relative} line {line}: {Summarize(statement)}");
                    continue;
                }

                // Rule 2 -- the value is parked in a local, then asserted. Both halves are inside one
                // statement here only when written as a single expression; the split form is caught by
                // tracking the local across statements below.
                Match assignment = StringifiedLocalPattern().Match(statement);
                if (assignment.Success)
                {
                    string name = assignment.Groups["name"].Value;
                    if (statement.Contains($"{name}.ShouldNotContain", StringComparison.Ordinal))
                    {
                        occurrences.Add($"{relative} line {line}: {Summarize(statement)}");
                    }
                    else
                    {
                        stringifiedLocals[name] = line;
                    }

                    continue;
                }

                foreach ((string name, int declaredAt) in stringifiedLocals)
                {
                    if (statement.Contains($"{name}.ShouldNotContain", StringComparison.Ordinal))
                    {
                        occurrences.Add(
                            $"{relative} line {line}: {Summarize(statement)} (stringified at line {declaredAt})");
                    }
                }
            }
        }

        return occurrences;
    }

    /// <summary>
    /// Splits source into logical statements, each tagged with the 1-based line it starts on. Physical lines
    /// are joined until the statement terminates, so a fluent chain wrapped by a formatter is matched as the
    /// single statement it is rather than slipping between two half-matches.
    /// </summary>
    private static IEnumerable<(string Statement, int Line)> Statements(string[] lines)
    {
        StringBuilder buffer = new();
        int startLine = 0;
        for (int index = 0; index < lines.Length; index++)
        {
            string trimmed = lines[index].Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            if (buffer.Length == 0)
            {
                startLine = index + 1;
            }
            else
            {
                _ = buffer.Append(' ');
            }

            _ = buffer.Append(trimmed);
            if (trimmed[^1] is ';' or '{' or '}')
            {
                yield return (buffer.ToString(), startLine);
                _ = buffer.Clear();
            }
        }

        if (buffer.Length > 0)
        {
            yield return (buffer.ToString(), startLine);
        }
    }

    // Absence can be asserted without ShouldNotContain: Contains(...).ShouldBeFalse() is the same
    // non-evidence wearing a different assertion.
    private static bool AssertsAbsence(string statement)
        => statement.Contains("ShouldNotContain", StringComparison.Ordinal)
            || (statement.Contains("Contains(", StringComparison.Ordinal)
                && statement.Contains("ShouldBeFalse", StringComparison.Ordinal));

    private static string Summarize(string statement)
        => statement.Length <= 200 ? statement : statement[..200] + "...";

    // Matches "var x = ...ToString()", "string? x = ...", and a bare "x = ...ToString()" with no declaration
    // keyword at all. The negative lookahead keeps "==" comparisons out.
    [GeneratedRegex(@"(?:^|[{;]\s*)(?:(?:var|string\??|object\??)\s+)?(?<name>\w+)\s*=(?!=)[^;]*\bToString\(\)")]
    private static partial Regex StringifiedLocalPattern();

    // An interpolated string that is itself the assertion subject.
    [GeneratedRegex(@"\$""[^""]*\{[^}]+\}[^""]*""\s*\.\s*(?:ShouldNotContain|Contains)")]
    private static partial Regex InterpolatedSubjectPattern();

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
