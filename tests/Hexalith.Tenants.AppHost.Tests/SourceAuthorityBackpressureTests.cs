using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Shouldly;

namespace Hexalith.Tenants.AppHost.Tests;

/// <summary>Qualifies bounded source-authority backpressure without changing permit ceilings or operator selections.</summary>
public sealed class SourceAuthorityBackpressureTests
{
    [Theory]
    [InlineData(false, null, "64")]
    [InlineData(true, null, "64")]
    [InlineData(false, "0", "0")]
    [InlineData(true, "0", "0")]
    [InlineData(false, "7", "7")]
    [InlineData(true, "7", "7")]
    public async Task QueueDefaultAndExplicitOverridesPreserveTheExistingRatePolicy(bool publish, string? configured, string expected)
    {
        List<string> args = ["--EnableKeycloak=true"];
        if (publish) args.AddRange(["--publisher", "manifest", "--output-path", Path.Combine(Path.GetTempPath(), $"tenants-queue-model-{Guid.NewGuid():N}.json")]);
        if (configured is not null) args.Add("--EventStore:RateLimiting:QueueLimit=" + configured);
        await using IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Hexalith_Tenants_AppHost>([.. args]);
        IResource eventStore = builder.Resources.Single(resource => resource.Name == "eventstore");
        var context = new EnvironmentCallbackContext(builder.ExecutionContext, eventStore, new Dictionary<string, object>());
        foreach (EnvironmentCallbackAnnotation annotation in eventStore.Annotations.OfType<EnvironmentCallbackAnnotation>())
            await annotation.Callback(context);
        context.EnvironmentVariables["EventStore__RateLimiting__QueueLimit"].ShouldBe(expected);
        foreach (string key in new[] { "PermitLimit", "WindowSeconds", "ConsumerPermitLimit", "ConsumerWindowSeconds" })
            context.EnvironmentVariables.ContainsKey("EventStore__RateLimiting__" + key).ShouldBeFalse();
    }
}
