using System.Diagnostics;

using Shouldly;


namespace Hexalith.Tenants.Contracts.Tests;

/// <summary>
/// Qualifies shipped package and destination recovery tooling with offline product controls.
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
        var result = await RunAsync("python3", [Path.Combine(repository, "tests/scripts/test_partial_release_recovery.py")],
            timeout: TimeSpan.FromMinutes(15));
        result.ExitCode.ShouldBe(0, result.Diagnostics);
        result.Diagnostics.ShouldContain("OK");
    }

    /// <summary>
    /// Normal publication validates reviewed bytes and immutable OCI evidence without writes.
    /// </summary>
    [Fact]
    public async Task NormalPublicationPassesOfflineArtifactControls()
    {
        var result = await RunAsync("python3", [Path.Combine(FindRepoRoot(), "tests/scripts/test_release_artifact_verification.py")],
            timeout: TimeSpan.FromMinutes(15));
        result.ExitCode.ShouldBe(0, result.Diagnostics);
        result.Diagnostics.ShouldContain("OK");
    }

    private static async Task<(int ExitCode, string Diagnostics)> RunAsync(
        string executable,
        string[] arguments,
        string? account = null,
        IReadOnlyDictionary<string, string?>? environment = null,
        TimeSpan? timeout = null)
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

        process.StartInfo.Environment.Remove("NUGET_USER");
        process.StartInfo.Environment.Remove("NUGET_API_KEY");
        if (account is not null)
        {
            process.StartInfo.Environment["NUGET_USER"] = account;
        }

        if (environment is not null)
        {
            foreach (var entry in environment)
            {
                if (entry.Value is null)
                {
                    process.StartInfo.Environment.Remove(entry.Key);
                }
                else
                {
                    process.StartInfo.Environment[entry.Key] = entry.Value;
                }
            }
        }

        process.Start();
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        using CancellationTokenSource deadline = new(timeout ?? TimeSpan.FromMinutes(2));
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
