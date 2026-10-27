using System.Text;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Contracts.Routing;
using Hexalith.Tenants.Server.Routing;
using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Routing;

public class TenantIntentDeclarationTests
{
    [Theory]
    [InlineData("create-tenant")]
    [InlineData("update-tenant")]
    [InlineData("disable-tenant")]
    [InlineData("enable-tenant")]
    [InlineData("add-user-to-tenant")]
    [InlineData("remove-user-from-tenant")]
    [InlineData("change-user-role")]
    [InlineData("set-tenant-configuration")]
    [InlineData("remove-tenant-configuration")]
    [InlineData("bootstrap-global-admin")]
    [InlineData("set-global-administrator")]
    [InlineData("remove-global-administrator")]
    public void EveryActualOperationHasEquivalentPayloadAndAliasSemantics(string operation)
    {
        IDeclaredIdempotencyIntentAdapter adapter = Adapter(operation);
        const string payload = "{\"TenantId\":\"ignored\",\"Name\":\" Acmé \",\"Description\":null,\"UserId\":\"User-A\",\"Role\":\"TenantOwner\",\"NewRole\":\"TenantReader\",\"Key\":\"Key-A\",\"Value\":\"\"}";
        IdempotencyIntentCommand command = Command(adapter, payload);
        IdempotencyCanonicalIntent expected = adapter.CreateIntent(command);
        string equivalent = "{\"value\":\"\",\"key\":\"Key-A\",\"newRole\":3,\"role\":\"tenantowner\",\"userId\":\"User-A\",\"description\":null,\"name\":\" Acmé \",\"tenantId\":\"another-ignored-target\"}";
        adapter.CreateIntent(command with { Payload = Encoding.UTF8.GetBytes(equivalent), Extensions = new Dictionary<string, string> { ["subject"] = "forged" } }).SemanticPayload.ShouldBe(expected.SemanticPayload);
        foreach (string alias in adapter.Declaration.Route.Execution!.Aliases)
            adapter.CreateIntent(command with { CommandType = "Other.Namespace." + alias + ", Other.Assembly" }).SemanticPayload.ShouldBe(expected.SemanticPayload);
        expected.CanonicalTarget.ShouldBe($"system:{adapter.Declaration.Route.Domain}:target-1");
        adapter.CreateIntent(command with { AggregateId = "target-2" }).CanonicalTarget.ShouldNotBe(expected.CanonicalTarget);
        adapter.RetentionTier.ShouldBe(IdempotencyReplayRetentionTier.Mutation);
        adapter.Declaration.Route.MessageIdVersion.ShouldBe(1);
        adapter.DescriptorVersion.ShouldBe(1);
        adapter.Declaration.Route.PlatformAdmission!.Namespace.ShouldBe("platform:" + adapter.Declaration.Route.Domain);
    }

    [Theory]
    [InlineData("create-tenant", "{\"name\":\"A\",\"description\":null}", "{\"name\":\"A\",\"description\":\"\"}")]
    [InlineData("update-tenant", "{\"name\":\"A\"}", "{\"name\":\"a\"}")]
    [InlineData("update-tenant", "{\"name\":\"é\"}", "{\"name\":\"é\"}")]
    [InlineData("add-user-to-tenant", "{\"userId\":\"A\",\"role\":1}", "{\"userId\":\"A\",\"role\":2}")]
    [InlineData("remove-user-from-tenant", "{\"userId\":\"A\"}", "{\"userId\":\"a\"}")]
    [InlineData("change-user-role", "{\"userId\":\"A\",\"newRole\":1}", "{\"userId\":\"A\",\"newRole\":3}")]
    [InlineData("set-tenant-configuration", "{\"key\":\"Key\",\"value\":\"\"}", "{\"key\":\"Key\",\"value\":\" \"}")]
    [InlineData("remove-tenant-configuration", "{\"key\":\"Key\"}", "{\"key\":\"key\"}")]
    [InlineData("add-user-to-tenant", "{\"userId\":\"A\",\"role\":1}", "{\"userId\":\"B\",\"role\":1}")]
    [InlineData("change-user-role", "{\"userId\":\"A\",\"newRole\":1}", "{\"userId\":\"B\",\"newRole\":1}")]
    [InlineData("set-tenant-configuration", "{\"key\":\"Key-A\",\"value\":\"same\"}", "{\"key\":\"Key-B\",\"value\":\"same\"}")]
    public void SemanticDistinctionsRemainDistinct(string operation, string first, string second)
    {
        IDeclaredIdempotencyIntentAdapter adapter = Adapter(operation);
        adapter.CreateIntent(Command(adapter, first)).SemanticPayload.ShouldNotBe(adapter.CreateIntent(Command(adapter, second)).SemanticPayload);
    }

    [Theory]
    [InlineData("bootstrap-global-admin")]
    [InlineData("set-global-administrator")]
    [InlineData("remove-global-administrator")]
    public void GlobalAdministratorIntentRequiresTrustedSubjectAndBindsIt(string operation)
    {
        IDeclaredIdempotencyIntentAdapter adapter = Adapter(operation);
        IdempotencyIntentCommand command = Command(adapter, "{\"userId\":\"actor-1\",\"authenticatedSubject\":\"spoof\"}");
        byte[] expected = adapter.CreateIntent(command).SemanticPayload;
        adapter.CreateIntent(command with { AuthenticatedSubject = "actor-2" }).SemanticPayload.ShouldNotBe(expected);
        adapter.CreateIntent(command with { Payload = "{\"userId\":\"actor-2\"}"u8.ToArray() }).SemanticPayload.ShouldNotBe(expected);
        Should.Throw<InvalidOperationException>(() => adapter.CreateIntent(command with { AuthenticatedSubject = null, Extensions = new Dictionary<string, string> { ["subject"] = "actor-1" } }));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"userId\":\"u\",\"role\":0}")]
    [InlineData("{\"userId\":\"u\",\"role\":999}")]
    public void InvalidOrDefaultRoleCannotBecomeCanonicalIntent(string json)
    {
        var adapter = Adapter("add-user-to-tenant");
        Should.Throw<InvalidOperationException>(() => adapter.CreateIntent(Command(adapter, json)));
    }

    [Fact]
    public void PublicAndInternalQueryKindsAndExactPermissionsRemainExplicit()
    {
        TenantCatalogDeclarations.Queries.Count.ShouldBe(8);
        TenantCatalogDeclarations.Queries.Count(q => q.Execution!.RequiresDelegatedContext).ShouldBe(2);
        TenantCatalogDeclarations.Queries.Where(q => q.Execution!.RequiresDelegatedContext).ShouldAllBe(q => q.Execution!.QueryKind == "computed" && q.ProjectionType == null);
        TenantCatalogDeclarations.Commands.Select(c => c.Route.PlatformAdmission!.RequiredHumanPermission).Order().ShouldBe(new[] {
            "tenants:create", "tenants:update", "tenants:disable", "tenants:enable", "tenants:add-user", "tenants:remove-user", "tenants:change-user-role",
            "tenants:set-configuration", "tenants:remove-configuration", "global-administrators:bootstrap", "global-administrators:set", "global-administrators:remove" }.Order());
    }

    private static IDeclaredIdempotencyIntentAdapter Adapter(string operation) => TenantIntentAdapters.Create().Single(adapter => adapter.CommandType == operation);
    private static IdempotencyIntentCommand Command(IDeclaredIdempotencyIntentAdapter adapter, string payload) => new(adapter.CommandType,
        "system", adapter.Declaration.Route.Domain, "target-1", Encoding.UTF8.GetBytes(payload), null) { AuthenticatedSubject = "actor-1" };
}
