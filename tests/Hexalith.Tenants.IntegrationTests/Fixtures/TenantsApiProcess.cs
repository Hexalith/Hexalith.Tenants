extern alias TenantsApi;

using System.Diagnostics;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

internal static class TenantsApiProcess
{
    internal static ProcessStartInfo CreateStartInfo(string directory, string environment)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = directory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        string[] isolatedPrefixes = ["EventStore__", "DAPR_", "Authentication__", "Jwt__", "Oidc__", "ASPNETCORE_", "OTEL_"];
        foreach (string name in start.Environment.Keys.Where(name =>
            isolatedPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))).ToArray())
        {
            start.Environment.Remove(name);
        }

        start.Environment["DOTNET_ENVIRONMENT"] = environment;
        string testAssembly = typeof(TenantsApiProcess).Assembly.Location;
        foreach (string argument in new[]
        {
            "exec", "--runtimeconfig", Path.ChangeExtension(testAssembly, ".runtimeconfig.json"),
            "--depsfile", Path.ChangeExtension(testAssembly, ".deps.json"), typeof(TenantsApi::Program).Assembly.Location,
            "--urls=http://127.0.0.1:0", "--environment=" + environment,
            "--DAPR_HTTP_ENDPOINT=http://127.0.0.1:1", "--EventStore:BaseUrl=http://127.0.0.1:1",
        })
        {
            start.ArgumentList.Add(argument);
        }
        return start;
    }

    internal static Process Start(ProcessStartInfo start, out Task<string> output, out Task<string> error)
    {
        Process process = Process.Start(start) ?? throw new InvalidOperationException("The API test host did not start.");
        output = process.StandardOutput.ReadToEndAsync();
        error = process.StandardError.ReadToEndAsync();
        return process;
    }

    internal static async Task StopAsync(Process process, Task<string> output, Task<string> error)
    {
        if (!process.HasExited) process.Kill(entireProcessTree: true);
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        await Task.WhenAll(output, error).WaitAsync(TimeSpan.FromSeconds(5));
    }
}
