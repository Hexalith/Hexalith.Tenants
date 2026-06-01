using System.Diagnostics;

using Hexalith.Tenants.Telemetry;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Telemetry;

[Collection("Telemetry")]
public class TenantActivitySourceTests : IDisposable {
    private readonly ActivityListener _listener;
    private readonly List<Activity> _activities = [];

    public TenantActivitySourceTests() {
        _listener = new ActivityListener {
            ShouldListenTo = source => source.Name == TenantActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _activities.Add(activity),
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() {
        _listener.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void SourceName_ShouldBeHexalithTenants() => TenantActivitySource.SourceName.ShouldBe("Hexalith.Tenants");

    [Fact]
    public void Instance_ShouldHaveCorrectSourceName() => TenantActivitySource.Instance.Name.ShouldBe("Hexalith.Tenants");

    [Fact]
    public void StartActivity_CommandProcess_ShouldCreateSpanWithCorrectName() {
        using Activity? activity = TenantActivitySource.Instance.StartActivity(
            TenantActivitySource.CommandProcess, ActivityKind.Internal);

        _ = activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("Tenants.Command.Process");
        _activities.ShouldContain(activity);
    }

    [Fact]
    public void StartActivity_QueryExecute_ShouldCreateSpanWithCorrectName() {
        using Activity? activity = TenantActivitySource.Instance.StartActivity(
            TenantActivitySource.QueryExecute, ActivityKind.Internal);

        _ = activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("Tenants.Projection.Query");
    }

    [Fact]
    public void StartActivity_ProjectionProject_ShouldCreateSpanWithCorrectName() {
        using Activity? activity = TenantActivitySource.Instance.StartActivity(
            TenantActivitySource.ProjectionProject, ActivityKind.Internal);

        _ = activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe("Tenants.Projection.Project");
    }

    [Fact]
    public void Activity_ShouldAcceptCommandTags() {
        using Activity? activity = TenantActivitySource.Instance.StartActivity(
            TenantActivitySource.CommandProcess, ActivityKind.Internal);

        _ = activity.ShouldNotBeNull();
        _ = activity.SetTag(TenantActivitySource.TagCommandType, "CreateTenant");
        _ = activity.SetTag(TenantActivitySource.TagTenantId, "tenant-1");
        _ = activity.SetTag(TenantActivitySource.TagSuccess, true);

        activity.GetTagItem(TenantActivitySource.TagCommandType).ShouldBe("CreateTenant");
        activity.GetTagItem(TenantActivitySource.TagTenantId).ShouldBe("tenant-1");
        activity.GetTagItem(TenantActivitySource.TagSuccess).ShouldBe(true);
    }

    [Fact]
    public void Activity_ShouldAcceptQueryTags() {
        using Activity? activity = TenantActivitySource.Instance.StartActivity(
            TenantActivitySource.QueryExecute, ActivityKind.Internal);

        _ = activity.ShouldNotBeNull();
        _ = activity.SetTag(TenantActivitySource.TagQueryType, "get-tenant");

        activity.GetTagItem(TenantActivitySource.TagQueryType).ShouldBe("get-tenant");
    }

    [Fact]
    public void Activity_ShouldAcceptProjectionDispatchTags() {
        using Activity? activity = TenantActivitySource.Instance.StartActivity(
            TenantActivitySource.ProjectionProject, ActivityKind.Internal);

        _ = activity.ShouldNotBeNull();
        _ = activity.SetTag(TenantActivitySource.TagStage, "projection-dispatch");
        _ = activity.SetTag(TenantActivitySource.TagTenantId, "system");
        _ = activity.SetTag(TenantActivitySource.TagDomain, "tenants");
        _ = activity.SetTag(TenantActivitySource.TagAggregateId, "tenant-1");
        _ = activity.SetTag(TenantActivitySource.TagProjectionType, "tenant");
        _ = activity.SetTag(TenantActivitySource.TagEventCount, 1);
        _ = activity.SetTag(TenantActivitySource.TagCausationIdStatus, "unavailable-from-projection-dto");
        _ = activity.SetTag(TenantActivitySource.TagOutcome, "completed");

        activity.GetTagItem(TenantActivitySource.TagStage).ShouldBe("projection-dispatch");
        activity.GetTagItem(TenantActivitySource.TagTenantId).ShouldBe("system");
        activity.GetTagItem(TenantActivitySource.TagDomain).ShouldBe("tenants");
        activity.GetTagItem(TenantActivitySource.TagAggregateId).ShouldBe("tenant-1");
        activity.GetTagItem(TenantActivitySource.TagProjectionType).ShouldBe("tenant");
        activity.GetTagItem(TenantActivitySource.TagEventCount).ShouldBe(1);
        activity.GetTagItem(TenantActivitySource.TagCausationIdStatus).ShouldBe("unavailable-from-projection-dto");
        activity.GetTagItem(TenantActivitySource.TagOutcome).ShouldBe("completed");
    }

    [Fact]
    public void Activity_ErrorStatus_ShouldBeSettable() {
        using Activity? activity = TenantActivitySource.Instance.StartActivity(
            TenantActivitySource.CommandProcess, ActivityKind.Internal);

        _ = activity.ShouldNotBeNull();
        _ = activity.SetStatus(ActivityStatusCode.Error, "Test error");

        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("Test error");
    }
}
