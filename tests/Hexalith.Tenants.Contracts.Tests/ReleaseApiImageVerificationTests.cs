using System.Diagnostics;
using System.Text.Json;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests;

/// <summary>Executes API release binding and refusal before invoking the OCI validator.</summary>
public class ReleaseApiImageVerificationTests
{
    /// <summary>Requires the same release version, source and immutable image as normal publication.</summary>
    /// <param name="scenario">The reviewed input or fresh observation mutation.</param>
    [Theory]
    [InlineData("valid")]
    [InlineData("missing-api-receipt")]
    [InlineData("wrong-image")]
    [InlineData("wrong-release-version")]
    [InlineData("wrong-release-source")]
    [InlineData("changed-digest")]
    public async Task ApiReadbackRequiresTheVerifiedNormalRelease(string scenario)
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip("Release readback fixtures require bash, jq and python3.");
        }

        string workspace = Path.Combine(Path.GetTempPath(), "tenants-api-release-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        try
        {
            string release = Path.Combine(workspace, ".hexalith/release-evidence/1.2.3");
            string receipt = Path.Combine(release, "tenants-api/oci-validation.json");
            string proof = Path.Combine(release, "destinations/verified.json");
            string marker = Path.Combine(release, "tenants-api-destination/verified.json");
            Directory.CreateDirectory(Path.GetDirectoryName(receipt)!);
            Directory.CreateDirectory(Path.GetDirectoryName(proof)!);
            Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
            await File.WriteAllTextAsync(marker, "stale");
            if (scenario != "missing-api-receipt")
            {
                await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(new
                {
                    result = "pass",
                    image = "registry.example/" + (scenario == "wrong-image" ? "tenants" : "tenants-api") + ":1.2.3",
                    platforms = new[] { "linux/amd64", "linux/arm64" },
                    index_digest = "sha256:" + new string('a', 64),
                    children = new[] { new { provenance_labels = new Dictionary<string, string>
                    { ["org.opencontainers.image.created"] = "2026-10-31T00:00:00Z" } } },
                }));
            }
            await File.WriteAllTextAsync(proof, JsonSerializer.Serialize(new
            {
                result = "pass", repository = "Hexalith/Hexalith.Tenants",
                version = scenario == "wrong-release-version" ? "1.2.4" : "1.2.3",
                source_sha = new string(scenario == "wrong-release-source" ? 'b' : 'a', 40),
            }));
            string validator = Path.Combine(workspace, "validator.py");
            await File.WriteAllTextAsync(validator, """
                import json, os, pathlib, sys
                args = sys.argv[1:]
                assert args[args.index('--image') + 1] == 'registry.example/tenants-api:1.2.3'
                assert args[args.index('--repository') + 1] == 'Hexalith/Hexalith.Tenants'
                assert args[args.index('--source-sha') + 1] == 'a' * 40
                assert args[args.index('--version') + 1] == '1.2.3'
                assert args[args.index('--created') + 1] == '2026-10-31T00:00:00Z'
                target = pathlib.Path(args[args.index('--evidence-directory') + 1])
                target.mkdir(parents=True, exist_ok=True)
                digest = ('b' if os.environ['SCENARIO'] == 'changed-digest' else 'a') * 64
                (target / 'oci-validation.json').write_text(json.dumps({'index_digest': 'sha256:' + digest}))
                """);
            var start = new ProcessStartInfo("bash")
            {
                WorkingDirectory = workspace, RedirectStandardOutput = true, RedirectStandardError = true,
            };
            start.ArgumentList.Add(Path.Combine(RepositoryRoot(), "scripts/verify-api-release-artifact.sh"));
            start.Environment["GITHUB_SHA"] = new string('a', 40);
            start.Environment["GITHUB_REPOSITORY"] = "Hexalith/Hexalith.Tenants";
            start.Environment["HEXALITH_ZOT_REGISTRY"] = "registry.example";
            start.Environment["HEXALITH_RELEASE_API_OCI_RECEIPT"] = receipt;
            start.Environment["HEXALITH_RELEASE_OCI_VALIDATOR"] = validator;
            start.Environment["SCENARIO"] = scenario;
            using var process = Process.Start(start)!;
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            Task<string> error = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) { process.Kill(entireProcessTree: true); throw; }
            string diagnostic = await output + await error;
            if (scenario == "valid")
            {
                process.ExitCode.ShouldBe(0, diagnostic);
                using var verified = JsonDocument.Parse(await File.ReadAllTextAsync(marker));
                verified.RootElement.GetProperty("version").GetString().ShouldBe("1.2.3");
                verified.RootElement.GetProperty("source_sha").GetString().ShouldBe(new string('a', 40));
            }
            else
            {
                process.ExitCode.ShouldNotBe(0, diagnostic);
                File.Exists(marker).ShouldBeFalse("A refused attempt must revoke its previous successful marker.");
                if (scenario != "changed-digest")
                    Directory.Exists(Path.Combine(release, "tenants-api-destination/oci")).ShouldBeFalse("Refusal must precede registry effects.");
            }
        }
        finally { Directory.Delete(workspace, recursive: true); }
    }

    private static string RepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.Tenants.slnx"))) return directory.FullName;
        throw new InvalidOperationException("The actual Tenants test repository is unavailable.");
    }
}
