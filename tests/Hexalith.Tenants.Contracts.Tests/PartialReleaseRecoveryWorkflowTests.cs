using System.Diagnostics;
using System.Text.Json;

using Shouldly;

using YamlDotNet.RepresentationModel;

namespace Hexalith.Tenants.Contracts.Tests;

/// <summary>
/// Qualifies release credential boundaries and offline recovery controls in the mandatory contract lane.
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
            timeout: TimeSpan.FromMinutes(5));
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
        int preparation = Array.FindIndex(steps, step => Value(step, "name") == "Prepare recovery packages");
        int guard = Array.FindIndex(steps, step => Value(step, "name") == "Require the Trusted Publishing account");
        int login = Array.FindIndex(steps, step => Value(step, "id") == "nuget-login");
        int publish = Array.FindIndex(steps, step => Value(step, "name") == "Publish missing release artifacts");

        validation.ShouldBeGreaterThanOrEqualTo(0);
        Value(steps[validation], "id").ShouldBe("recovery");
        Value(steps[validation], "run").ShouldContain("scripts/validate-partial-release-recovery.sh");
        preparation.ShouldBeGreaterThan(validation);
        AssertPreparationBindings(steps[preparation]);
        guard.ShouldBeGreaterThan(preparation);
        login.ShouldBeGreaterThan(guard);
        publish.ShouldBe(login + 1);
        Value(steps[guard], "if").ShouldBe("steps.recovery.outputs.needs-nuget == 'true'");
        Value(steps[login], "if").ShouldBe("steps.recovery.outputs.needs-nuget == 'true'");
        Value(steps[publish], "if").ShouldBeEmpty();
    }

    /// <summary>
    /// Removing or replacing any required preparation input fails the workflow binding contract.
    /// </summary>
    /// <param name="binding">The required environment binding.</param>
    /// <param name="remove">Whether to remove the binding instead of replacing its value.</param>
    [Theory]
    [InlineData("RECOVERY_VERSION", true)]
    [InlineData("RECOVERY_VERSION", false)]
    [InlineData("GITHUB_SHA", true)]
    [InlineData("GITHUB_SHA", false)]
    [InlineData("GH_TOKEN", true)]
    [InlineData("GH_TOKEN", false)]
    public void MissingOrChangedPreparationBindingMutationIsRejected(string binding, bool remove)
    {
        YamlMappingNode preparation = GetSteps().Single(step => Value(step, "name") == "Prepare recovery packages");
        AssertPreparationBindings(preparation);
        var environment = (YamlMappingNode)preparation.Children[new YamlScalarNode("env")];
        if (remove)
        {
            environment.Children.Remove(new YamlScalarNode(binding)).ShouldBeTrue();
        }
        else
        {
            environment.Children[new YamlScalarNode(binding)] = new YamlScalarNode("changed-binding");
        }

        _ = Should.Throw<ShouldAssertException>(() => AssertPreparationBindings(preparation));
    }

    /// <summary>
    /// Failed preparation cannot be made successful for the following credential gates.
    /// </summary>
    [Fact]
    public void ContinuedPreparationFailureMutationIsRejected()
    {
        YamlMappingNode preparation = GetSteps().Single(step => Value(step, "name") == "Prepare recovery packages");
        AssertPreparationBindings(preparation);
        preparation.Children[new YamlScalarNode("continue-on-error")] = new YamlScalarNode("true");

        _ = Should.Throw<ShouldAssertException>(() => AssertPreparationBindings(preparation));
    }

    /// <summary>
    /// Both workflows execute their actual account guard against configured and unavailable accounts.
    /// </summary>
    /// <param name="workflow">The owning publication workflow.</param>
    /// <param name="job">The publication job.</param>
    /// <param name="account">The configured individual account, or null for an unset variable.</param>
    /// <param name="exitCode">The required guard outcome.</param>
    [Theory]
    [InlineData("release.yml", "release", "fixture-publisher", 0)]
    [InlineData("release.yml", "release", null, 1)]
    [InlineData("release.yml", "release", "", 1)]
    [InlineData("release.yml", "release", " \t\r\n", 1)]
    [InlineData("recover-partial-release.yml", "recover", "fixture-publisher", 0)]
    [InlineData("recover-partial-release.yml", "recover", null, 1)]
    [InlineData("recover-partial-release.yml", "recover", "", 1)]
    [InlineData("recover-partial-release.yml", "recover", " \t\r\n", 1)]
    public async Task AccountGuardExecutesBeforeLogin(string workflow, string job, string? account, int exitCode)
    {
        YamlMappingNode guard = GetSteps(workflow, job).Single(step => Value(step, "name") == "Require the Trusted Publishing account");
        var result = await RunAsync("bash", ["-c", Value(guard, "run")], account);
        result.ExitCode.ShouldBe(exitCode, result.Diagnostics);
        if (exitCode != 0)
        {
            result.Diagnostics.ShouldContain(account is null
                ? "NUGET_USER" : "NUGET_USER must name the individual nuget.org account");
        }
    }

    /// <summary>
    /// Credential gates run under their exact supported conditions and publish only the login output.
    /// </summary>
    /// <param name="workflow">The owning publication workflow.</param>
    /// <param name="job">The publication job.</param>
    /// <param name="publication">The credential-bearing publication step.</param>
    [Theory]
    [InlineData("release.yml", "release", "Semantic Release")]
    [InlineData("recover-partial-release.yml", "recover", "Publish missing release artifacts")]
    public void CredentialHandoffUsesRequiredConditions(string workflow, string job, string publication)
        => AssertCredentialHandoff(GetSteps(workflow, job), job, publication);

    /// <summary>
    /// Adding a disabling condition to either real login or account guard fails the handoff contract.
    /// </summary>
    /// <param name="workflow">The owning publication workflow.</param>
    /// <param name="job">The publication job.</param>
    /// <param name="publication">The credential-bearing publication step.</param>
    /// <param name="stepName">The credential gate being disabled.</param>
    [Theory]
    [InlineData("release.yml", "release", "Semantic Release", "Exchange GitHub OIDC token for a temporary NuGet key")]
    [InlineData("release.yml", "release", "Semantic Release", "Require the Trusted Publishing account")]
    [InlineData("recover-partial-release.yml", "recover", "Publish missing release artifacts", "Exchange GitHub OIDC token for a temporary NuGet key")]
    [InlineData("recover-partial-release.yml", "recover", "Publish missing release artifacts", "Require the Trusted Publishing account")]
    public void DisabledCredentialGateMutationIsRejected(string workflow, string job, string publication, string stepName)
    {
        YamlMappingNode[] steps = GetSteps(workflow, job);
        AssertCredentialHandoff(steps, job, publication);
        YamlMappingNode step = steps.Single(candidate => Value(candidate, "name") == stepName);
        step.Children[new YamlScalarNode("if")] = new YamlScalarNode("${{ false }}");

        _ = Should.Throw<ShouldAssertException>(() => AssertCredentialHandoff(steps, job, publication));
    }

    /// <summary>
    /// Replacing the real publication binding with an empty value fails the output handoff contract.
    /// </summary>
    /// <param name="workflow">The owning publication workflow.</param>
    /// <param name="job">The publication job.</param>
    /// <param name="publication">The credential-bearing publication step.</param>
    [Theory]
    [InlineData("release.yml", "release", "Semantic Release")]
    [InlineData("recover-partial-release.yml", "recover", "Publish missing release artifacts")]
    public void EmptyCredentialBindingMutationIsRejected(string workflow, string job, string publication)
    {
        YamlMappingNode[] steps = GetSteps(workflow, job);
        AssertCredentialHandoff(steps, job, publication);
        var environment = (YamlMappingNode)steps.Single(step => Value(step, "name") == publication)
            .Children[new YamlScalarNode("env")];
        environment.Children[new YamlScalarNode("NUGET_API_KEY")] = new YamlScalarNode(string.Empty);

        _ = Should.Throw<ShouldAssertException>(() => AssertCredentialHandoff(steps, job, publication));
    }

    /// <summary>
    /// Skipped login or unusable login output cannot reach any recovery destination or build client.
    /// </summary>
    /// <param name="loginOutput">The absent or unusable output supplied by the login step.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public async Task MissingLoginOutputStopsRecoveryBeforeAnyClientRuns(string? loginOutput)
    {
        YamlMappingNode publication = GetSteps().Single(step => Value(step, "name") == "Publish missing release artifacts");
        await AssertUnavailableLoginOutputStopsClientsAsync(Value(publication, "run"), loginOutput,
            "NUGET_API_KEY is required for missing packages.");
    }

    /// <summary>
    /// Normal release verification refuses unusable login output before build or destination clients run.
    /// </summary>
    /// <param name="loginOutput">The absent or unusable output supplied by the login step.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public async Task MissingLoginOutputStopsNormalReleaseBeforeAnyClientRuns(string? loginOutput)
    {
        using JsonDocument release = JsonDocument.Parse(File.ReadAllText(Path.Combine(FindRepoRoot(), ".releaserc.json")));
        JsonElement execution = release.RootElement.GetProperty("plugins").EnumerateArray()
            .Single(plugin => plugin.ValueKind == JsonValueKind.Array && plugin[0].GetString() == "@semantic-release/exec")[1];
        string verification = execution.GetProperty("verifyReleaseCmd").GetString()!
            .Replace("${nextRelease.version}", "1.2.3", StringComparison.Ordinal);
        await AssertUnavailableLoginOutputStopsClientsAsync(verification, loginOutput,
            "NUGET_API_KEY is required before publishing NuGet packages.");
    }

    /// <summary>
    /// Every external action in both release workflows uses an immutable lowercase commit identity.
    /// </summary>
    /// <param name="workflow">The publication workflow.</param>
    [Theory]
    [InlineData("release.yml")]
    [InlineData("recover-partial-release.yml")]
    public void ExternalReleaseActionsUseImmutableRevisions(string workflow)
    {
        YamlMappingNode jobs = GetJobs(workflow);
        string workflowSource = File.ReadAllText(Path.Combine(FindRepoRoot(), ".github/workflows", workflow));
        using JsonDocument evidence = JsonDocument.Parse(File.ReadAllText(Path.Combine(FindRepoRoot(),
            "_bmad-output/implementation-artifacts/dw-552-553-557-561-566-action-pins.json")));
        var pins = evidence.RootElement.EnumerateArray().ToDictionary(
            pin => pin.GetProperty("action").GetString()!,
            pin => (Commit: pin.GetProperty("commit").GetString()!, Version: pin.GetProperty("version").GetString()!),
            StringComparer.Ordinal);
        pins.Keys.Order(StringComparer.Ordinal).ToArray().ShouldBe(new[]
        {
            "NuGet/login", "actions/cache", "actions/checkout", "actions/setup-dotnet", "actions/setup-node", "actions/upload-artifact",
        });
        foreach (JsonElement pin in evidence.RootElement.EnumerateArray())
        {
            string commit = pin.GetProperty("commit").GetString()!;
            string version = pin.GetProperty("version").GetString()!;
            pin.GetProperty("upstream_refs").EnumerateArray().Select(reference => reference.GetString())
                .ShouldContain($"{commit}\trefs/tags/{version}");
        }

        int externalActions = 0;
        foreach (YamlMappingNode job in jobs.Children.Values.Cast<YamlMappingNode>())
        {
            if (!job.Children.TryGetValue(new YamlScalarNode("steps"), out YamlNode? steps))
            {
                continue;
            }

            foreach (YamlMappingNode step in ((YamlSequenceNode)steps).Children.Cast<YamlMappingNode>())
            {
                string action = Value(step, "uses");
                if (action.Length == 0 || action.StartsWith("./", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] identity = action.Split('@');
                identity.Length.ShouldBe(2, $"{workflow}: {action}");
                identity[1].Length.ShouldBe(40, $"{workflow}: {action}");
                identity[1].All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f')
                    .ShouldBeTrue($"{workflow}: {action}");
                if (pins.TryGetValue(identity[0], out var pin))
                {
                    identity[1].ShouldBe(pin.Commit, $"{workflow}: {action} must match the reviewed upstream revision.");
                    string[] declarations = workflowSource.Split('\n').Select(line => line.Trim())
                        .Select(line => line.StartsWith("- ", StringComparison.Ordinal) ? line[2..] : line)
                        .Where(line => line.StartsWith($"uses: {identity[0]}@", StringComparison.Ordinal)).ToArray();
                    declarations.ShouldNotBeEmpty();
                    foreach (string declaration in declarations)
                    {
                        declaration.ShouldBe($"uses: {identity[0]}@{pin.Commit} # {pin.Version}",
                            $"{workflow}: preserve the reviewed version comment on every action declaration.");
                    }
                }

                externalActions++;
            }
        }

        externalActions.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Normal publication validates reviewed bytes and immutable OCI evidence without writes.
    /// </summary>
    [Fact]
    public async Task NormalPublicationPassesOfflineArtifactControls()
    {
        var result = await RunAsync("python3", [Path.Combine(FindRepoRoot(), "tests/scripts/test_release_artifact_verification.py")]);
        result.ExitCode.ShouldBe(0, result.Diagnostics);
        result.Diagnostics.ShouldContain("OK");
    }

    /// <summary>
    /// Verification must run immediately after publication and before unconditional evidence upload.
    /// </summary>
    [Fact]
    public void NormalPublicationVerifiesDestinationsBeforeRetainingEvidence()
    {
        YamlMappingNode[] steps = GetSteps("release.yml", "release");
        int publish = Array.FindIndex(steps, step => Value(step, "name") == "Semantic Release");
        int verify = Array.FindIndex(steps, step => Value(step, "name") == "Verify published destination content");
        int upload = Array.FindIndex(steps, step => Value(step, "name") == "Upload complete release evidence");
        verify.ShouldBe(publish + 1);
        upload.ShouldBe(verify + 1);
        AssertDestinationVerificationBindings(steps[verify]);
        Value(steps[upload], "if").ShouldBe("${{ always() }}");
        MappingValue(steps[upload], "with", "path").ShouldBe(".hexalith/release-evidence/**");
    }

    /// <summary>
    /// Required destination verification cannot be bypassed or rebound by workflow edits.
    /// </summary>
    /// <param name="field">The binding to mutate.</param>
    [Theory]
    [InlineData("run")]
    [InlineData("if")]
    [InlineData("continue-on-error")]
    [InlineData("shell")]
    [InlineData("GH_TOKEN")]
    [InlineData("HEXALITH_ZOT_USERNAME")]
    [InlineData("HEXALITH_ZOT_API_KEY")]
    [InlineData("HEXALITH_RELEASE_OCI_VALIDATOR")]
    public void ChangedDestinationVerificationBindingIsRejected(string field)
    {
        YamlMappingNode verify = GetSteps("release.yml", "release").Single(step => Value(step, "name") == "Verify published destination content");
        AssertDestinationVerificationBindings(verify);
        YamlMappingNode target = field is "run" or "if" or "continue-on-error" or "shell"
            ? verify : (YamlMappingNode)verify.Children[new YamlScalarNode("env")];
        target.Children[new YamlScalarNode(field)] = new YamlScalarNode("changed");
        _ = Should.Throw<ShouldAssertException>(() => AssertDestinationVerificationBindings(verify));
    }

    /// <summary>
    /// Closure verification precedes preparation and remains in the standalone publishing workflow.
    /// </summary>
    [Fact]
    public void StandaloneProvenancePrecedesSensitivePreparation()
    {
        foreach (var identity in new[]
        {
            (Workflow: "release.yml", Job: "prepare-release", Sensitive: "Build and pack release candidate"),
            (Workflow: "recover-partial-release.yml", Job: "recover", Sensitive: "Prepare recovery packages"),
        })
        {
            YamlMappingNode[] steps = GetSteps(identity.Workflow, identity.Job);
            int provenance = Array.FindIndex(steps, step => Value(step, "uses") == "./.hexalith/builds-execution/Github/governed-provenance");
            int preparation = Array.FindIndex(steps, step => Value(step, "name") == identity.Sensitive);
            provenance.ShouldBeGreaterThan(0);
            provenance.ShouldBeLessThan(preparation);
            MappingValue(steps[provenance], "with", "workflow-mode").ShouldBe("standalone");
            MappingValue(steps[provenance], "with", "workflow-path").ShouldBe(".github/workflows/" + identity.Workflow);
            Value(steps[provenance], "if").ShouldBeEmpty();
            Value(steps[provenance], "continue-on-error").ShouldBeEmpty();
        }
    }

    /// <summary>
    /// The actual final publication step and bootstrap refuse drift and changing required gates.
    /// </summary>
    [Fact]
    public async Task ShippedWorkflowAdmissionAndBootstrapPassExecutableControls()
    {
        var result = await RunAsync("python3", [Path.Combine(FindRepoRoot(), "tests/scripts/test_release_workflow_governance.py")]);
        result.ExitCode.ShouldBe(0, result.Diagnostics);
        result.Diagnostics.ShouldContain("OK");
    }

    /// <summary>
    /// Credential-free candidate preparation can read Actions qualification through the scoped GitHub token.
    /// </summary>
    [Fact]
    public void CandidatePreparationCanReadRequiredWorkflowGates()
    {
        var job = (YamlMappingNode)GetJobs("release.yml").Children[new YamlScalarNode("prepare-release")];
        MappingValue(job, "permissions", "actions").ShouldBe("read");
        YamlMappingNode build = GetSteps("release.yml", "prepare-release")
            .Single(step => Value(step, "name") == "Build and pack release candidate");
        MappingValue(build, "env", "GH_TOKEN").ShouldBe("${{ github.token }}");
        MappingValue(build, "env", "NUGET_API_KEY").ShouldBeEmpty();
        MappingValue(build, "env", "HEXALITH_ZOT_API_KEY").ShouldBeEmpty();
    }

    private static void AssertDestinationVerificationBindings(YamlMappingNode verification)
    {
        Value(verification, "run").ShouldBe("bash scripts/verify-release-artifacts.sh");
        Value(verification, "shell").ShouldBe("bash");
        Value(verification, "if").ShouldBeEmpty();
        (Value(verification, "continue-on-error") is "" or "false").ShouldBeTrue();
        MappingValue(verification, "env", "GH_TOKEN").ShouldBe("${{ github.token }}");
        MappingValue(verification, "env", "HEXALITH_ZOT_USERNAME").ShouldBe("${{ secrets.HEXALITH_ZOT_USERNAME }}");
        MappingValue(verification, "env", "HEXALITH_ZOT_API_KEY").ShouldBe("${{ secrets.HEXALITH_ZOT_API_KEY }}");
        MappingValue(verification, "env", "HEXALITH_RELEASE_OCI_VALIDATOR").ShouldBe(".hexalith/release/oci_registry_validator.py");
    }

    private static void AssertPreparationBindings(YamlMappingNode preparation)
    {
        Value(preparation, "run").ShouldBe("bash scripts/prepare-partial-release.sh \"$RECOVERY_VERSION\"");
        Value(preparation, "if").ShouldBeEmpty();
        (Value(preparation, "continue-on-error") is "" or "false")
            .ShouldBeTrue("Failed preparation must prevent the following credential steps.");
        MappingValue(preparation, "env", "RECOVERY_VERSION").ShouldBe("${{ inputs.version }}");
        MappingValue(preparation, "env", "GITHUB_SHA").ShouldBe("${{ inputs.source_sha }}");
        MappingValue(preparation, "env", "GH_TOKEN").ShouldBe("${{ github.token }}");
        MappingValue(preparation, "env", "NUGET_API_KEY").ShouldBeEmpty();
        MappingValue(preparation, "env", "HEXALITH_ZOT_API_KEY").ShouldBeEmpty();
        MappingValue(preparation, "env", "HEXALITH_ZOT_USERNAME").ShouldBeEmpty();
    }

    private static async Task AssertUnavailableLoginOutputStopsClientsAsync(
        string command,
        string? loginOutput,
        string expectedDiagnostic)
    {
        string temporary = Path.Combine(Path.GetTempPath(), $"tenants-login-handoff-{Guid.NewGuid():N}");
        string state = Path.Combine(temporary, "state");
        string clients = Path.Combine(temporary, "bin");
        string calls = Path.Combine(temporary, "calls");
        Directory.CreateDirectory(state);
        Directory.CreateDirectory(clients);
        try
        {
            const string source = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            await File.WriteAllTextAsync(Path.Combine(state, "source-sha"), source + "\n");
            await File.WriteAllTextAsync(Path.Combine(state, "version"), "1.2.3\n");
            await File.WriteAllTextAsync(Path.Combine(state, "missing-packages"), "Fixture.Package\n");
            foreach (string client in new[] { "gh", "curl", "dotnet", "docker", "python3", "npm", "git" })
            {
                string path = Path.Combine(clients, client);
                await File.WriteAllTextAsync(path, "#!/bin/bash\nprintf '%s\\n' \"$0\" >> \"$HANDOFF_CALLS\"\nexit 97\n");
                var permissions = await RunAsync("chmod", ["+x", path]);
                permissions.ExitCode.ShouldBe(0, permissions.Diagnostics);
            }

            Dictionary<string, string?> environment = new(StringComparer.Ordinal)
            {
                ["PATH"] = clients + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH"),
                ["HANDOFF_CALLS"] = calls,
                ["HEXALITH_RECOVERY_STATE_DIRECTORY"] = state,
                ["RECOVERY_VERSION"] = "1.2.3",
                ["GITHUB_SHA"] = source,
                ["NUGET_API_KEY"] = loginOutput,
            };
            var result = await RunAsync("bash", ["-c", command], environment: environment);
            result.ExitCode.ShouldNotBe(0, result.Diagnostics);
            result.Diagnostics.ShouldContain(expectedDiagnostic);
            File.Exists(calls).ShouldBeFalse(result.Diagnostics);
        }
        finally
        {
            Directory.Delete(temporary, recursive: true);
        }
    }

    private static void AssertCredentialHandoff(YamlMappingNode[] steps, string job, string publication)
    {
        YamlMappingNode guard = steps.Single(step => Value(step, "name") == "Require the Trusted Publishing account");
        YamlMappingNode login = steps.Single(step => Value(step, "id") == "nuget-login");
        YamlMappingNode publish = steps.Single(step => Value(step, "name") == publication);
        string condition = job == "recover" ? "steps.recovery.outputs.needs-nuget == 'true'" : string.Empty;
        Value(guard, "if").ShouldBe(condition);
        Value(login, "if").ShouldBe(condition);
        Value(publish, "if").ShouldBeEmpty();
        MappingValue(guard, "env", "NUGET_USER").ShouldBe("${{ vars.NUGET_USER }}");
        MappingValue(login, "with", "user").ShouldBe("${{ vars.NUGET_USER }}");
        MappingValue(publish, "env", "NUGET_API_KEY").ShouldBe("${{ steps.nuget-login.outputs.NUGET_API_KEY }}");
        Array.IndexOf(steps, login).ShouldBeGreaterThan(Array.IndexOf(steps, guard));
        Array.IndexOf(steps, publish).ShouldBe(Array.IndexOf(steps, login) + 1);
    }

    private static YamlMappingNode[] GetSteps(string workflow = "recover-partial-release.yml", string job = "recover")
    {
        var mapping = (YamlMappingNode)GetJobs(workflow).Children[new YamlScalarNode(job)];
        return [.. ((YamlSequenceNode)mapping.Children[new YamlScalarNode("steps")]).Children.Cast<YamlMappingNode>()];
    }

    private static YamlMappingNode GetJobs(string workflow)
    {
        using StringReader reader = new(File.ReadAllText(Path.Combine(FindRepoRoot(), ".github/workflows", workflow)));
        YamlStream yaml = new();
        yaml.Load(reader);
        var root = (YamlMappingNode)yaml.Documents[0].RootNode;
        return (YamlMappingNode)root.Children[new YamlScalarNode("jobs")];
    }

    private static string MappingValue(YamlMappingNode node, string mapping, string key)
        => node.Children.TryGetValue(new YamlScalarNode(mapping), out YamlNode? value)
            ? Value((YamlMappingNode)value, key) : string.Empty;

    private static string Value(YamlMappingNode node, string key)
        => node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value) ? value.ToString() : string.Empty;

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
