using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.Tests.Components;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

public sealed partial class TenantsWorkspaceTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task WorkspaceReadSafetyDiagnosticsCannotHideRetryOrFatalEvidence(bool resolutionFails, bool fatal)
    {
        ITenantQueryGateway reads = FiniteWorkspaceReads();
        int calls = 0;
        reads.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++calls == 2 ? Task.FromException<TenantListSnapshot>(ReadSafetyFailure("unexpected"))
                : Task.FromResult(TenantListSnapshot.Empty(true, ReadModelFreshnessState.Current)));
        Exception diagnosticFailure = fatal
            ? new InvalidOperationException("diagnostic-wrapper", new OutOfMemoryException("original-fatal"))
            : new ObjectDisposedException("private-diagnostic-provider");
        Services.AddSingleton<ILogger<TenantsWorkspace>>(_ => resolutionFails
            ? throw diagnosticFailure : new WorkspaceReadSafetyLogger(diagnosticFailure));
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition());
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        Func<Task> refresh = () => cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        if (fatal)
        {
            Exception observed = await Should.ThrowAsync<Exception>(refresh);
            observed.ShouldBeSameAs(diagnosticFailure);
            return;
        }
        await refresh();
        cut.WaitForElement("[data-testid='tenants-list-error']");
        cut.Markup.ShouldNotContain("private-diagnostic-provider");
        await cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='tenants-list-error']").ShouldBeEmpty());
        calls.ShouldBe(3);
    }

    [Theory]
    [InlineData("disposed", "provider-disposed")]
    [InlineData("null-reference", "programming-failure")]
    [InlineData("null-argument", "invalid-argument")]
    [InlineData("empty-argument", "invalid-argument")]
    [InlineData("cancelled", "provider-cancelled")]
    [InlineData("unexpected", "read-failed")]
    public async Task WorkspaceReadSafetyContainsNonfatalFailuresAndRetryRecovers(string failure, string category)
    {
        ITenantQueryGateway reads = FiniteWorkspaceReads();
        int calls = 0;
        reads.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++calls == 1
                ? Task.FromException<TenantListSnapshot>(ReadSafetyFailure(failure))
                : Task.FromResult(TenantListSnapshot.Empty(true, ReadModelFreshnessState.Current)));
        var logger = new WorkspaceReadSafetyLogger();
        Services.AddSingleton<ILogger<TenantsWorkspace>>(logger);
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition());

        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        cut.WaitForElement("[data-testid='tenants-list-error']");
        cut.Markup.ShouldNotContain("private-cursor-query-principal");
        logger.Messages.ShouldHaveSingleItem().ShouldContain(category);
        logger.Messages.ShouldAllBe(message => !message.Contains("private-cursor-query-principal", StringComparison.Ordinal));
        logger.Exceptions.ShouldHaveSingleItem().ShouldBeNull();
        await cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='tenants-list-error']").ShouldBeEmpty());
        calls.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WorkspaceReadSafetyRetiresFailuresAfterReplacementOrDisposal(bool dispose)
    {
        var pending = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        ITenantQueryGateway reads = FiniteWorkspaceReads();
        int calls = 0;
        reads.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++calls == 2 ? pending.Task
                : Task.FromResult(TenantListSnapshot.Empty(true, ReadModelFreshnessState.Current)));
        var logger = new WorkspaceReadSafetyLogger();
        Services.AddSingleton<ILogger<TenantsWorkspace>>(logger);
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition());
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        Task oldLoad = cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => calls.ShouldBe(2));
        if (dispose)
        {
            await cut.InvokeAsync(() => ((IDisposable)cut.Instance).Dispose());
        }
        else
        {
            await cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        }
        TenantListSnapshot before = PrivateField<TenantListSnapshot>(cut.Instance, "_snapshot");
        pending.SetException(ReadSafetyFailure("disposed"));
        await oldLoad.WaitAsync(TimeSpan.FromSeconds(5));
        PrivateField<TenantListSnapshot>(cut.Instance, "_snapshot").ShouldBeSameAs(before);
        logger.Messages.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("memory")]
    [InlineData("access")]
    [InlineData("stack")]
    [InlineData("aggregate")]
    [InlineData("wrapper")]
    [InlineData("nested-wrapper")]
    public async Task WorkspaceReadSafetyPropagatesFatalFailures(string failure)
    {
        ITenantQueryGateway reads = FiniteWorkspaceReads();
        int calls = 0;
        Exception fatal = failure switch
        {
            "memory" => new OutOfMemoryException(),
            "access" => new AccessViolationException(),
            "aggregate" => new AggregateException(new InvalidOperationException(), new OutOfMemoryException()),
            "wrapper" => new InvalidOperationException("outer", new OutOfMemoryException()),
            "nested-wrapper" => new ArgumentException("outer", new InvalidOperationException("inner", new AccessViolationException())),
            _ => new StackOverflowException(),
        };
        reads.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++calls == 1 ? Task.FromResult(TenantListSnapshot.Empty(true, ReadModelFreshnessState.Current))
                : Task.FromException<TenantListSnapshot>(fatal));
        RegisterFiniteWorkspaceServices(reads, new StubTenantsBffComposition());
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        Exception observed = await Should.ThrowAsync<Exception>(() =>
            cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs()));
        observed.ShouldBeSameAs(fatal);
    }

    private static Exception ReadSafetyFailure(string failure)
        => failure switch
        {
            "disposed" => new ObjectDisposedException("private-cursor-query-principal"),
            "null-reference" => new NullReferenceException("private-cursor-query-principal"),
            "null-argument" => new ArgumentNullException("private-cursor-query-principal"),
            "empty-argument" => new ArgumentException("private-cursor-query-principal"),
            "cancelled" => new OperationCanceledException("private-cursor-query-principal"),
            _ => new InvalidOperationException("private-cursor-query-principal"),
        };
}
