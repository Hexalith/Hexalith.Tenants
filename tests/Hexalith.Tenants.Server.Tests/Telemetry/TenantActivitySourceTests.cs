using System.Diagnostics;

using Hexalith.Tenants.CommandApi.Telemetry;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Telemetry;

[Collection("Telemetry")]
public class TenantActivitySourceTests : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly List<Activity> _activities = [];

    public TenantActivitySourceTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TenantActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _activities.Add(activity),
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void SourceName_ShouldBeHexalithTenants()
    {
        TenantActivitySource.SourceName.ShouldBe("Hexalith.Tenants");
    }

    [Fact]
    public void Instance_ShouldHaveCorrectSourceName()
    {
        TenantActivitySource.Instance.Name.ShouldBe("Hexalith.Tenants");
    }

    [Fact]
    public void StartActivity_CommandProcess_ShouldCreateSpanWithCorrectName()
    {
        using Activity? activity = TenantActivitySource.Instance.StartActivity(
            TenantActivitySource.CommandProcess, ActivityKind.Internal);

        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("Tenants.Command.Process");
        _activities.ShouldContain(activity);
    }

    [Fact]
    public void StartActivity_QueryExecute_ShouldCreateSpanWithCorrectName()
    {
        using Activity? activity = TenantActivitySource.Instance.StartActivity(
            TenantActivitySource.QueryExecute, ActivityKind.Internal);

        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("Tenants.Projection.Query");
    }

    [Fact]
    public void Activity_ShouldAcceptCommandTags()
    {
        using Activity? activity = TenantActivitySource.Instance.StartActivity(
            TenantActivitySource.CommandProcess, ActivityKind.Internal);

        activity.ShouldNotBeNull();
        activity.SetTag(TenantActivitySource.TagCommandType, "CreateTenant");
        activity.SetTag(TenantActivitySource.TagTenantId, "tenant-1");
        activity.SetTag(TenantActivitySource.TagSuccess, true);

        activity.GetTagItem(TenantActivitySource.TagCommandType).ShouldBe("CreateTenant");
        activity.GetTagItem(TenantActivitySource.TagTenantId).ShouldBe("tenant-1");
        activity.GetTagItem(TenantActivitySource.TagSuccess).ShouldBe(true);
    }

    [Fact]
    public void Activity_ShouldAcceptQueryTags()
    {
        using Activity? activity = TenantActivitySource.Instance.StartActivity(
            TenantActivitySource.QueryExecute, ActivityKind.Internal);

        activity.ShouldNotBeNull();
        activity.SetTag(TenantActivitySource.TagQueryType, "get-tenant");

        activity.GetTagItem(TenantActivitySource.TagQueryType).ShouldBe("get-tenant");
    }

    [Fact]
    public void Activity_ErrorStatus_ShouldBeSettable()
    {
        using Activity? activity = TenantActivitySource.Instance.StartActivity(
            TenantActivitySource.CommandProcess, ActivityKind.Internal);

        activity.ShouldNotBeNull();
        activity.SetStatus(ActivityStatusCode.Error, "Test error");

        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("Test error");
    }
}
