using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests;

/// <summary>
/// Verifies that automated source provenance validation executes the required build and test commands.
/// </summary>
public class SourceReferenceWorkflowGovernanceTests
{
    private const string InitializeStep = "Initialize root-declared submodules";
    private const string BuildStep = "Build the source-reference integration graph";
    private const string VerifyStep = "Verify real-handler projection provenance";
    private const string IntegrationProject = "tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj";
    private const string ProvenanceMethod = "Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests.GlobalAdministratorsRealHandlerMetadataSurvivesRouterRestClientAndUiGateway";

    /// <summary>
    /// Checks the repository workflow against its automated source provenance contract.
    /// </summary>
    [Fact]
    public void Source_reference_workflow_executes_real_handler_projection_provenance()
    {
        WorkflowViolations(ReadWorkflow()).ShouldBeEmpty();
    }

    /// <summary>
    /// Accepts equivalent YAML scalar styles and harmless command whitespace.
    /// </summary>
    [Theory]
    [InlineData(ScalarStyle.Plain)]
    [InlineData(ScalarStyle.SingleQuoted)]
    [InlineData(ScalarStyle.DoubleQuoted)]
    [InlineData(ScalarStyle.Literal)]
    [InlineData(ScalarStyle.Folded)]
    public void Workflow_governance_accepts_equivalent_scalar_and_whitespace_formatting(ScalarStyle style)
    {
        YamlMappingNode workflow = ParseWorkflow(ReadWorkflow());
        foreach ((string name, string[] tokens) in ExpectedCommands())
        {
            FindStep(workflow, name).Children[new YamlScalarNode("run")] = new YamlScalarNode(string.Join("  ", tokens)) { Style = style };
        }

        WorkflowViolations(SerializeWorkflow(workflow)).ShouldBeEmpty();
    }

    /// <summary>
    /// Accepts explicit shell continuations that preserve the required single command.
    /// </summary>
    [Fact]
    public void Workflow_governance_accepts_explicit_shell_line_continuations()
    {
        YamlMappingNode workflow = ParseWorkflow(ReadWorkflow());
        foreach ((string name, string[] tokens) in ExpectedCommands())
        {
            FindStep(workflow, name).Children[new YamlScalarNode("run")] = new YamlScalarNode(string.Join(" \\\n  ", tokens)) { Style = ScalarStyle.Literal };
        }

        WorkflowViolations(SerializeWorkflow(workflow)).ShouldBeEmpty();
    }

    /// <summary>
    /// Qualifies rejection of missing, misplaced, commented or failure-ignoring commands.
    /// </summary>
    [Theory]
    [InlineData("commented-floor")]
    [InlineData("commented-skips")]
    [InlineData("floor-in-unrelated-step")]
    [InlineData("test-configuration")]
    [InlineData("build-configuration")]
    [InlineData("test-source-mode")]
    [InlineData("build-source-mode")]
    [InlineData("missing-no-build")]
    [InlineData("wrong-method")]
    [InlineData("three-case-floor")]
    [InlineData("missing-fail-skips")]
    [InlineData("fail-skips-off")]
    [InlineData("bare-fail-skips")]
    [InlineData("ignored-test-failure")]
    [InlineData("ignored-build-failure")]
    [InlineData("extra-command")]
    [InlineData("literal-newlines")]
    [InlineData("commented-initialization")]
    [InlineData("echoed-initialization")]
    [InlineData("missing-initialization")]
    [InlineData("missing-build")]
    [InlineData("missing-verification")]
    [InlineData("duplicate-verification")]
    [InlineData("initialization-after-build")]
    [InlineData("non-scalar-command")]
    public void Workflow_governance_rejects_ineffective_commands(string mutation)
    {
        string original = ReadWorkflow();
        WorkflowViolations(original).ShouldBeEmpty();
        YamlMappingNode workflow = ParseWorkflow(original);
        YamlMappingNode initialize = FindStep(workflow, InitializeStep);
        YamlMappingNode build = FindStep(workflow, BuildStep);
        YamlMappingNode verify = FindStep(workflow, VerifyStep);
        YamlSequenceNode steps = WorkflowSteps(workflow);

        switch (mutation)
        {
            case "commented-floor":
                ReplaceCommand(verify, "--minimum-expected-tests 4", "# --minimum-expected-tests 4");
                break;
            case "commented-skips":
                ReplaceCommand(verify, "--fail-skips on", "# --fail-skips on");
                break;
            case "floor-in-unrelated-step":
                ReplaceCommand(verify, "--minimum-expected-tests 4", string.Empty);
                steps.Add(new YamlMappingNode { { "name", "Unrelated flags" }, { "run", "echo --minimum-expected-tests 4" } });
                break;
            case "test-configuration":
                ReplaceCommand(verify, "--configuration Debug", "--configuration Release");
                break;
            case "build-configuration":
                ReplaceCommand(build, "--configuration Debug", "--configuration Release");
                break;
            case "test-source-mode":
                ReplaceCommand(verify, "UseHexalithProjectReferences=true", "UseHexalithProjectReferences=false");
                break;
            case "build-source-mode":
                ReplaceCommand(build, "UseHexalithProjectReferences=true", "UseHexalithProjectReferences=false");
                break;
            case "missing-no-build":
                ReplaceCommand(verify, "--no-build", string.Empty);
                break;
            case "wrong-method":
                ReplaceCommand(verify, ProvenanceMethod, "UnrelatedTest.Method");
                break;
            case "three-case-floor":
                ReplaceCommand(verify, "--minimum-expected-tests 4", "--minimum-expected-tests 3");
                break;
            case "missing-fail-skips":
                ReplaceCommand(verify, "--fail-skips on", string.Empty);
                break;
            case "fail-skips-off":
                ReplaceCommand(verify, "--fail-skips on", "--fail-skips off");
                break;
            case "bare-fail-skips":
                ReplaceCommand(verify, "--fail-skips on", "--fail-skips");
                break;
            case "ignored-test-failure":
                ReplaceCommand(verify, "--fail-skips on", "--fail-skips on || true");
                break;
            case "ignored-build-failure":
                ReplaceCommand(build, "--no-incremental", "--no-incremental || true");
                break;
            case "extra-command":
                ReplaceCommand(verify, "--fail-skips on", "--fail-skips on\necho ignored");
                break;
            case "literal-newlines":
                ReplaceCommand(verify, " --configuration", "\n--configuration");
                break;
            case "commented-initialization":
                ReplaceCommand(initialize, "git -c", "# git -c");
                break;
            case "echoed-initialization":
                ReplaceCommand(initialize, "git -c", "echo git -c");
                break;
            case "missing-initialization":
                steps.Children.Remove(initialize);
                break;
            case "missing-build":
                steps.Children.Remove(build);
                break;
            case "missing-verification":
                steps.Children.Remove(verify);
                break;
            case "duplicate-verification":
                steps.Add(FindStep(ParseWorkflow(original), VerifyStep));
                break;
            case "initialization-after-build":
                steps.Children.Remove(initialize);
                steps.Add(initialize);
                break;
            case "non-scalar-command":
                verify.Children[new YamlScalarNode("run")] = new YamlSequenceNode("dotnet", "test");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mutation), mutation, "Unknown mutation.");
        }

        WorkflowViolations(SerializeWorkflow(workflow)).ShouldNotBeEmpty($"The actual validator must reject {mutation}.");
    }

    /// <summary>
    /// Qualifies rejection of execution overrides that bypass required command failures.
    /// </summary>
    [Theory]
    [InlineData("job", "if", "false")]
    [InlineData("job", "continue-on-error", "true")]
    [InlineData(InitializeStep, "if", "false")]
    [InlineData(BuildStep, "if", "false")]
    [InlineData(VerifyStep, "if", "false")]
    [InlineData(InitializeStep, "continue-on-error", "true")]
    [InlineData(BuildStep, "continue-on-error", "true")]
    [InlineData(VerifyStep, "continue-on-error", "true")]
    [InlineData(VerifyStep, "shell", "echo {0}")]
    [InlineData(BuildStep, "working-directory", "unrelated")]
    [InlineData(InitializeStep, "uses", "unrelated/action@main")]
    public void Workflow_governance_rejects_conditional_or_overridden_execution(string target, string key, string value)
    {
        YamlMappingNode workflow = ParseWorkflow(ReadWorkflow());
        YamlMappingNode node = target == "job" ? ProvenanceJob(workflow) : FindStep(workflow, target);
        node.Children[new YamlScalarNode(key)] = new YamlScalarNode(value);

        WorkflowViolations(SerializeWorkflow(workflow)).ShouldNotBeEmpty();
    }

    /// <summary>
    /// Rejects workflow and job shell defaults that redirect the checked commands.
    /// </summary>
    [Theory]
    [InlineData("workflow")]
    [InlineData("job")]
    public void Workflow_governance_rejects_overridden_shell_defaults(string target)
    {
        YamlMappingNode workflow = ParseWorkflow(ReadWorkflow());
        YamlMappingNode node = target == "workflow" ? workflow : ProvenanceJob(workflow);
        node.Children[new YamlScalarNode("defaults")] = new YamlMappingNode
        {
            { "run", new YamlMappingNode { { "shell", "echo {0}" } } },
        };

        WorkflowViolations(SerializeWorkflow(workflow)).ShouldNotBeEmpty();
    }

    /// <summary>
    /// Qualifies rejection of missing, duplicate and malformed workflow structure.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("jobs: [")]
    [InlineData("jobs: {}\njobs: {}")]
    [InlineData("jobs: {}\n---\njobs: {}")]
    [InlineData("jobs: [projection-provenance]")]
    [InlineData("jobs: {projection-provenance: {steps: disabled}}")]
    [InlineData("jobs:\n  projection-provenance:\n    steps:\n      - name: Verify real-handler projection provenance\n        run: dotnet test\n        run: echo ignored")]
    public void Workflow_governance_rejects_missing_duplicate_or_malformed_yaml(string workflow)
    {
        WorkflowViolations(workflow).ShouldNotBeEmpty();
    }

    /// <summary>
    /// Allows a manual entry point alongside the required main push and pull request events.
    /// </summary>
    [Fact]
    public void Workflow_governance_accepts_additional_manual_trigger()
    {
        YamlMappingNode workflow = ParseWorkflow(ReadWorkflow());
        ((YamlMappingNode)Value(workflow, "on")!).Add("workflow_dispatch", new YamlMappingNode());

        WorkflowViolations(SerializeWorkflow(workflow)).ShouldBeEmpty();
    }

    /// <summary>
    /// Rejects trigger changes that suppress required automated main validation.
    /// </summary>
    [Theory]
    [InlineData("manual-only")]
    [InlineData("missing-push")]
    [InlineData("missing-pull_request")]
    [InlineData("wrong-main-push")]
    [InlineData("wrong-main-pull_request")]
    [InlineData("scalar-events")]
    public void Workflow_governance_rejects_missing_automated_main_triggers(string mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        YamlMappingNode workflow = ParseWorkflow(ReadWorkflow());
        YamlMappingNode triggers = (YamlMappingNode)Value(workflow, "on")!;
        if (mutation == "manual-only")
        {
            workflow.Children[new YamlScalarNode("on")] = new YamlMappingNode { { "workflow_dispatch", new YamlMappingNode() } };
        }
        else if (mutation == "scalar-events")
        {
            workflow.Children[new YamlScalarNode("on")] = new YamlScalarNode("workflow_dispatch");
        }
        else if (mutation.StartsWith("missing-", StringComparison.Ordinal))
        {
            triggers.Children.Remove(new YamlScalarNode(mutation["missing-".Length..]));
        }
        else
        {
            YamlMappingNode eventTrigger = (YamlMappingNode)Value(triggers, mutation["wrong-main-".Length..])!;
            eventTrigger.Children[new YamlScalarNode("branches")] = new YamlSequenceNode("release");
        }

        WorkflowViolations(SerializeWorkflow(workflow)).ShouldNotBeEmpty();
    }

    /// <summary>
    /// Rejects event filters that can prevent the required main validation from running.
    /// </summary>
    [Theory]
    [InlineData("push", "paths")]
    [InlineData("pull_request", "paths")]
    [InlineData("push", "paths-ignore")]
    [InlineData("pull_request", "types")]
    [InlineData("push", "branches-ignore")]
    [InlineData("pull_request", "branches-ignore")]
    [InlineData("push", "tags")]
    public void Workflow_governance_rejects_suppressing_event_filters(string eventName, string filter)
    {
        YamlMappingNode workflow = ParseWorkflow(ReadWorkflow());
        YamlMappingNode triggers = (YamlMappingNode)Value(workflow, "on")!;
        YamlMappingNode eventTrigger = (YamlMappingNode)Value(triggers, eventName)!;
        eventTrigger.Add(filter, new YamlSequenceNode("unrelated"));

        WorkflowViolations(SerializeWorkflow(workflow)).ShouldNotBeEmpty();
    }

    /// <summary>
    /// Rejects a dependency on a skipped preflight job, in either supported needs syntax.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Workflow_governance_rejects_job_dependencies_on_skipped_preflight(bool sequence)
    {
        YamlMappingNode workflow = ParseWorkflow(ReadWorkflow());
        ((YamlMappingNode)Value(workflow, "jobs")!).Add("preflight", new YamlMappingNode
        {
            { "if", "false" },
            { "runs-on", "ubuntu-latest" },
            { "steps", new YamlSequenceNode(new YamlMappingNode { { "run", "echo skipped" } }) },
        });
        ProvenanceJob(workflow).Add("needs", sequence ? new YamlSequenceNode("preflight") : new YamlScalarNode("preflight"));

        WorkflowViolations(SerializeWorkflow(workflow)).ShouldNotBeEmpty();
    }

    private static (string Name, string[] Tokens)[] ExpectedCommands()
        =>
        [
            (InitializeStep, ["git", "-c", "submodule.recurse=false", "submodule", "update", "--init"]),
            (BuildStep, ["dotnet", "build", IntegrationProject, "--configuration", "Debug", "-p:UseHexalithProjectReferences=true", "-warnaserror", "-m:1", "-nr:false", "--no-incremental"]),
            (VerifyStep, ["dotnet", "test", IntegrationProject, "--configuration", "Debug", "--no-build", "-p:UseHexalithProjectReferences=true", "--minimum-expected-tests", "4", "--filter-method", ProvenanceMethod, "--fail-skips", "on"]),
        ];

    private static string[] WorkflowViolations(string text)
    {
        List<string> violations = [];
        try
        {
            YamlMappingNode workflow = ParseWorkflow(text);
            YamlMappingNode job = ProvenanceJob(workflow);
            RequireAutomatedMainTriggers(workflow, violations);
            RejectExecutionOverrides(workflow, "workflow", violations);
            RejectExecutionOverrides(job, "projection-provenance job", violations);
            if (Value(job, "needs") is not null)
            {
                violations.Add("The projection-provenance job must run independently of other jobs.");
            }

            YamlSequenceNode steps = WorkflowSteps(workflow);
            if (steps.Children.Any(step => step is not YamlMappingNode))
            {
                throw new InvalidOperationException("Every job step must be a YAML mapping.");
            }

            int previousIndex = -1;
            foreach ((string name, string[] expected) in ExpectedCommands())
            {
                YamlMappingNode step = FindStep(workflow, name);
                int index = steps.Children.IndexOf(step);
                if (index <= previousIndex)
                {
                    violations.Add($"{name}: initialization, build and verification must execute in that order.");
                }

                previousIndex = index;
                RejectExecutionOverrides(step, name, violations);
                if (Value(step, "uses") is not null)
                {
                    violations.Add($"{name}: the checked command must be the executable run step.");
                }

                if (Value(step, "run") is not YamlScalarNode { Value: { } command })
                {
                    violations.Add($"{name}: missing scalar run command.");
                    continue;
                }

                // YAML folding has already happened. A remaining shell newline separates commands;
                // only an explicit backslash continuation can join it into this one command.
                string normalized = command.Replace("\\\r\n", " ", StringComparison.Ordinal)
                    .Replace("\\\n", " ", StringComparison.Ordinal).Trim();
                string[] actual = normalized.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
                {
                    violations.Add($"{name}: run must execute exactly: {string.Join(' ', expected)}");
                }
            }
        }
        catch (Exception exception) when (exception is YamlException or ArgumentException or InvalidOperationException)
        {
            violations.Add($"Invalid source-reference workflow: {exception.Message}");
        }

        return [.. violations];
    }

    private static void RequireAutomatedMainTriggers(YamlMappingNode workflow, List<string> violations)
    {
        if (Value(workflow, "on") is not YamlMappingNode triggers)
        {
            violations.Add("The workflow must declare main push and pull_request event mappings.");
            return;
        }

        foreach (string eventName in new[] { "push", "pull_request" })
        {
            if (Value(triggers, eventName) is not YamlMappingNode eventTrigger
                || eventTrigger.Children.Count != 1
                || Value(eventTrigger, "branches") is not YamlSequenceNode branches
                || branches.Children.Count != 1
                || branches.Children[0] is not YamlScalarNode { Value: "main" })
            {
                violations.Add($"The {eventName} trigger must select main with no event filters that suppress validation.");
            }
        }
    }

    private static void RejectExecutionOverrides(YamlMappingNode node, string context, List<string> violations)
    {
        foreach (string key in new[] { "if", "continue-on-error", "defaults", "shell", "working-directory" })
        {
            if (Value(node, key) is not null)
            {
                violations.Add($"{context}: {key} can disable, redirect or ignore the required command.");
            }
        }
    }

    private static YamlMappingNode ParseWorkflow(string text)
    {
        YamlStream stream = new();
        using StringReader reader = new(text);
        stream.Load(reader);
        return stream.Documents.Count == 1 && stream.Documents[0].RootNode is YamlMappingNode root
            ? root
            : throw new InvalidOperationException("Expected exactly one YAML mapping document.");
    }

    private static YamlMappingNode ProvenanceJob(YamlMappingNode workflow)
        => Value(workflow, "jobs") is YamlMappingNode jobs && Value(jobs, "projection-provenance") is YamlMappingNode job
            ? job
            : throw new InvalidOperationException("Missing projection-provenance job mapping.");

    private static YamlSequenceNode WorkflowSteps(YamlMappingNode workflow)
        => Value(ProvenanceJob(workflow), "steps") as YamlSequenceNode
            ?? throw new InvalidOperationException("Missing projection-provenance steps sequence.");

    private static YamlMappingNode FindStep(YamlMappingNode workflow, string name)
        => WorkflowSteps(workflow).Children.OfType<YamlMappingNode>()
            .Single(step => Value(step, "name") is YamlScalarNode scalar && scalar.Value == name);

    private static YamlNode? Value(YamlMappingNode node, string key)
        => node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value) ? value : null;

    private static void ReplaceCommand(YamlMappingNode step, string oldValue, string newValue)
    {
        string command = ((YamlScalarNode)Value(step, "run")!).Value!;
        command.Contains(oldValue, StringComparison.Ordinal).ShouldBeTrue("A mutation must actually change the checked command.");
        step.Children[new YamlScalarNode("run")] = new YamlScalarNode(command.Replace(oldValue, newValue, StringComparison.Ordinal));
    }

    private static string SerializeWorkflow(YamlMappingNode workflow)
    {
        using StringWriter writer = new();
        new YamlStream(new YamlDocument(workflow)).Save(writer, assignAnchors: false);
        return writer.ToString();
    }

    private static string ReadWorkflow()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            string path = Path.Combine(current.FullName, ".github", "workflows", "source-reference.yml");
            if (File.Exists(Path.Combine(current.FullName, "Hexalith.Tenants.slnx")) && File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository source-reference workflow.");
    }
}
