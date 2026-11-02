using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Shouldly;

namespace Hexalith.Tenants.AppHost.Tests;

/// <summary>Qualifies the finite authority invocation budget on the actual run and publish resource models.</summary>
[Collection("LocalDaprIdentity")]
public sealed class SourceAuthorityInvocationBudgetTests
{
    /// <summary>Applies the EventStore-only default and respects environment input and command-line precedence.</summary>
    [Theory]
    [InlineData(false, null, null, "30")]
    [InlineData(true, null, null, "30")]
    [InlineData(false, null, "5", "5")]
    [InlineData(true, null, "5", "5")]
    [InlineData(false, null, "60", "60")]
    [InlineData(true, null, "60", "60")]
    [InlineData(false, null, "0", "0")]
    [InlineData(true, null, "0", "0")]
    [InlineData(false, null, "", "")]
    [InlineData(true, null, "", "")]
    [InlineData(false, "47", null, "47")]
    [InlineData(true, "47", null, "47")]
    [InlineData(false, "47", "5", "5")]
    [InlineData(true, "47", "5", "5")]
    public async Task DefaultAndExplicitSelectionsBelongOnlyToTheEventStoreHost(bool publish, string? environment, string? configured, string expected)
    {
        const string environmentKey = "EventStore__DomainServices__InvocationTimeoutSeconds";
        string? original = Environment.GetEnvironmentVariable(environmentKey);
        try
        {
            Environment.SetEnvironmentVariable(environmentKey, environment);
            List<string> args = ["--EnableKeycloak=true"];
            if (publish)
            {
                args.AddRange(["--publisher", "manifest", "--output-path",
                    Path.Combine(Path.GetTempPath(), $"tenants-invocation-model-{Guid.NewGuid():N}.json")]);
            }
            if (configured is not null)
            {
                args.Add("--EventStore:DomainServices:InvocationTimeoutSeconds=" + configured);
            }

            await using IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.Hexalith_Tenants_AppHost>([.. args]);
            builder.ExecutionContext.IsPublishMode.ShouldBe(publish);
            builder.Resources.OfType<ProjectResource>().Count(resource => resource.Name == "eventstore").ShouldBe(1);
            foreach (ProjectResource resource in builder.Resources.OfType<ProjectResource>())
            {
                var context = new EnvironmentCallbackContext(builder.ExecutionContext, resource, new Dictionary<string, object>());
                foreach (EnvironmentCallbackAnnotation annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
                {
                    await annotation.Callback(context);
                }

                if (resource.Name == "eventstore")
                {
                    context.EnvironmentVariables[environmentKey].ShouldBe(expected);
                }
                else
                {
                    context.EnvironmentVariables.ContainsKey(environmentKey).ShouldBeFalse(resource.Name);
                }
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentKey, original);
        }
    }
}
