using System.Text.RegularExpressions;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

public sealed class CommandFlowGuardConformanceTests
{
    // Both release mechanisms must be policed. The flows moved from the EventCallback to the Func-based
    // CommandActivityLease, and this guard matched only the retired path -- so it kept passing while no
    // longer guarding anything the live code does.
    private static readonly Regex DirectParentLockRelease = new(
        "OnCommandActivityChanged\\.InvokeAsync\\(\\s*false\\s*\\)"
        + "|CommandActivityLease(\\.Invoke)?\\(\\s*false\\s*\\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    [Trait("Category", "Governance")]
    public void Command_flows_do_not_release_page_activity_directly()
    {
        string componentsRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components", "Tenants");
        string[] razorFiles = Directory.GetFiles(componentsRoot, "*Flow.razor", SearchOption.AllDirectories);

        razorFiles.ShouldNotBeEmpty();

        List<string> offenders = [];
        foreach (string file in razorFiles)
        {
            string contents = File.ReadAllText(file);
            if (DirectParentLockRelease.IsMatch(contents))
            {
                offenders.Add(Path.GetRelativePath(ProjectRoot(), file));
            }
        }

        offenders.ShouldBeEmpty(
            "Command flows must route page-level command activity through TenantCommandFlowGuard so Accepted "
            + "and ProjectionPending work keeps sibling command surfaces locked until projection truth or a terminal state. "
            + "This applies to the CommandActivityLease delegate as well as the OnCommandActivityChanged callback.");
    }

    private static string ProjectRoot()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            if (File.Exists(Path.Combine(directory, "Hexalith.Tenants.slnx")))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        return Directory.GetCurrentDirectory();
    }
}
