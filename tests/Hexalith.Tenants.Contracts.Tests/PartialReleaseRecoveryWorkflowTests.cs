using System.Diagnostics;

using Shouldly;

using YamlDotNet.RepresentationModel;

namespace Hexalith.Tenants.Contracts.Tests;

/// <summary>
/// Qualifies recovery routing and offline destination controls in the mandatory contract lane.
/// </summary>
public class PartialReleaseRecoveryWorkflowTests
{
    /// <summary>
    /// The production scripts repair isolated destinations and reject conflicting evidence.
    /// </summary>
    [Fact]
    public async Task RecoveryPipelinePassesOfflineDestinationControls()
    {
        string repository = FindRepoRoot();
        var result = await RunAsync("python3", [Path.Combine(repository, "tests/scripts/test_partial_release_recovery.py")]);
        result.ExitCode.ShouldBe(0, result.Diagnostics);
        result.Diagnostics.ShouldContain("OK");
    }

    /// <summary>
    /// Both credential gates consume only the successful inventory step's explicit output.
    /// </summary>
    [Fact]
    public void RecoveryCredentialsDependOnValidatedMissingPackages()
    {
        YamlMappingNode[] steps = GetSteps();
        int validation = Array.FindIndex(steps, step => Value(step, "name") == "Validate partial release state");
        int guard = Array.FindIndex(steps, step => Value(step, "name") == "Require the Trusted Publishing account");
        int login = Array.FindIndex(steps, step => Value(step, "id") == "nuget-login");
        int publish = Array.FindIndex(steps, step => Value(step, "name") == "Publish missing release artifacts");

        validation.ShouldBeGreaterThanOrEqualTo(0);
        Value(steps[validation], "id").ShouldBe("recovery");
        Value(steps[validation], "run").ShouldContain("scripts/validate-partial-release-recovery.sh");
        guard.ShouldBeGreaterThan(validation);
        login.ShouldBeGreaterThan(guard);
        publish.ShouldBe(login + 1);
        Value(steps[guard], "if").ShouldBe("steps.recovery.outputs.needs-nuget == 'true'");
        Value(steps[login], "if").ShouldBe("steps.recovery.outputs.needs-nuget == 'true'");
        Value(steps[publish], "if").ShouldBeEmpty();
    }

    /// <summary>
    /// Package publication still rejects absent or whitespace-only Trusted Publishing accounts.
    /// </summary>
    /// <param name="account">The configured individual account.</param>
    /// <param name="exitCode">The required guard outcome.</param>
    [Theory]
    [InlineData("fixture-publisher", 0)]
    [InlineData("", 1)]
    [InlineData(" \t\r\n", 1)]
    public async Task RecoveryAccountGuardExecutesBeforeLogin(string account, int exitCode)
    {
        YamlMappingNode guard = GetSteps().Single(step => Value(step, "name") == "Require the Trusted Publishing account");
        var result = await RunAsync("bash", ["-c", Value(guard, "run")], account);
        result.ExitCode.ShouldBe(exitCode, result.Diagnostics);
        if (exitCode != 0)
        {
            result.Diagnostics.ShouldContain("NUGET_USER must name the individual nuget.org account");
        }
    }

    private static YamlMappingNode[] GetSteps()
    {
        using StringReader reader = new(File.ReadAllText(Path.Combine(FindRepoRoot(), ".github/workflows/recover-partial-release.yml")));
        YamlStream yaml = new();
        yaml.Load(reader);
        var root = (YamlMappingNode)yaml.Documents[0].RootNode;
        var jobs = (YamlMappingNode)root.Children[new YamlScalarNode("jobs")];
        var recover = (YamlMappingNode)jobs.Children[new YamlScalarNode("recover")];
        return [.. ((YamlSequenceNode)recover.Children[new YamlScalarNode("steps")]).Children.Cast<YamlMappingNode>()];
    }

    private static string Value(YamlMappingNode node, string key)
        => node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value) ? value.ToString() : string.Empty;

    private static async Task<(int ExitCode, string Diagnostics)> RunAsync(string executable, string[] arguments, string? account = null)
    {
        using Process process = new()
        {
            StartInfo = new ProcessStartInfo(executable)
            {
                WorkingDirectory = FindRepoRoot(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            },
        };
        foreach (string argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        if (account is not null)
        {
            process.StartInfo.Environment["NUGET_USER"] = account;
        }

        process.Start();
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        using CancellationTokenSource deadline = new(TimeSpan.FromMinutes(2));
        try
        {
            await process.WaitForExitAsync(deadline.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            throw;
        }

        return (process.ExitCode, await output + await error);
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Hexalith.Tenants.slnx")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
