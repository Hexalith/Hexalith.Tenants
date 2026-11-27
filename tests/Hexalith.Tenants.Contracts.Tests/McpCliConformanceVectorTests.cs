using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Hexalith.McpCli.Abstractions;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.Contracts.Queries;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests;

/// <summary>
/// Validates the module-owned McpCli v1 conformance vector for <see cref="RefreshTenantAccess"/>
/// against the decorated <c>Hexalith.Tenants.Contracts</c> types, canonical scope rules, and v1 schema structure.
/// </summary>
public sealed class McpCliConformanceVectorTests
{
    private static readonly Regex PackageIdPattern = new(
        @"\A[A-Za-z0-9][A-Za-z0-9_.-]*\z",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PackageVersionPattern = new(
        @"\A[0-9]+\.[0-9]+\.[0-9]+(?:\.[0-9]+)?(?:-[0-9A-Za-z.-]+)?\z",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex UlidPattern = new(
        @"\A[0-7][0-9A-HJKMNP-TV-Z]{25}\z",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex JsonPointerPattern = new(
        @"\A(?:/(?:[^~/]|~0|~1)*)*\z",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> ValidAssertionOperators = new(StringComparer.Ordinal)
    {
        "equals",
        "exists",
        "arrayLength",
        "contains",
    };

    private static readonly string[] ExpectedVectorFiles =
    [
        "add-user-to-tenant.json",
        "change-user-role.json",
        "create-tenant.json",
        "disable-tenant.json",
        "enable-tenant.json",
        "get-tenant-audit.json",
        "get-tenant-users.json",
        "get-tenant.json",
        "get-user-tenants.json",
        "list-tenants.json",
        "refresh-tenant-access.json",
        "remove-tenant-configuration.json",
        "remove-user-from-tenant.json",
        "set-tenant-configuration.json",
        "update-tenant.json",
    ];

    // Mirrors the McpCli payload contract: exact CLR names, unknown members rejected, enums by name only.
    private static readonly JsonSerializerOptions McpCliPayloadOptions = new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false) },
    };

    private static readonly JsonSerializerOptions WebResponseOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    [Fact]
    public void RefreshTenantAccess_vector_matches_contract_routing_canonical_scopes_and_v1_schema_strictness()
    {
        string vectorPath = ResolveVectorPath("refresh-tenant-access.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(vectorPath));
        JsonElement root = document.RootElement;

        AssertExactPropertyNames(
            root,
            "formatVersion",
            "package",
            "operation",
            "kind",
            "prerequisites",
            "invocation",
            "postconditions");

        Assembly contractsAssembly = typeof(RefreshTenantAccess).Assembly;
        HexalithModuleAttribute module = contractsAssembly.GetCustomAttribute<HexalithModuleAttribute>().ShouldNotBeNull();
        HexalithCommandAttribute refreshAttribute = typeof(RefreshTenantAccess)
            .GetCustomAttribute<HexalithCommandAttribute>(inherit: false)
            .ShouldNotBeNull();
        HexalithCommandAttribute createAttribute = typeof(CreateTenant)
            .GetCustomAttribute<HexalithCommandAttribute>(inherit: false)
            .ShouldNotBeNull();
        HexalithQueryAttribute getTenantAttribute = typeof(GetTenantQuery)
            .GetCustomAttribute<HexalithQueryAttribute>(inherit: false)
            .ShouldNotBeNull();

        string expectedOperation = $"{module.Name}.{RefreshTenantAccess.CommandType}";
        root.GetProperty("formatVersion").GetInt32().ShouldBe(1);

        JsonElement package = root.GetProperty("package");
        AssertExactPropertyNames(package, "id", "version");
        string packageId = package.GetProperty("id").GetString().ShouldNotBeNull();
        PackageIdPattern.IsMatch(packageId).ShouldBeTrue();
        packageId.ShouldBe(contractsAssembly.GetName().Name);
        string packageVersion = package.GetProperty("version").GetString().ShouldNotBeNull();
        PackageVersionPattern.IsMatch(packageVersion).ShouldBeTrue(
            $"package.version '{packageVersion}' must satisfy McpCli v1 schema SemVer pattern.");

        root.GetProperty("operation").GetString().ShouldBe(expectedOperation);
        root.GetProperty("kind").GetString().ShouldBe("command");

        // Validate invocation against RefreshTenantAccess contract, canonical platform scope, and schema.json.
        JsonElement invocation = root.GetProperty("invocation");
        AssertStepPropertyNames(invocation);
        invocation.GetProperty("operation").GetString().ShouldBe(expectedOperation);
        invocation.GetProperty("kind").GetString().ShouldBe("command");

        RefreshTenantAccess inputCommand = DeserializeStrict<RefreshTenantAccess>(invocation.GetProperty("payload"));
        inputCommand.TenantId.ShouldNotBeNullOrWhiteSpace();
        inputCommand.AggregateId.ShouldBe(inputCommand.TenantId);

        JsonElement invocationEnvelope = invocation.GetProperty("envelope");
        AssertExactPropertyNames(invocationEnvelope, "tenant", "idempotencyKey");
        refreshAttribute.PlatformScope.ShouldBe(TenantPlatformScopes.Tenants);
        string expectedCommandScope = TenantPlatformScopes.Tenants;
        invocationEnvelope.GetProperty("tenant").GetString().ShouldBe(expectedCommandScope);
        string idempotencyKey = invocationEnvelope.GetProperty("idempotencyKey").GetString().ShouldNotBeNull();
        UlidPattern.IsMatch(idempotencyKey).ShouldBeTrue("RefreshTenantAccess vector must supply a canonical ULID idempotencyKey.");

        JsonElement invocationGateway = invocation.GetProperty("expectedGateway");
        AssertExactPropertyNames(invocationGateway, "method", "path", "body", "maskGenerated");
        invocationGateway.GetProperty("method").GetString().ShouldBe("POST");
        invocationGateway.GetProperty("path").GetString().ShouldBe("/api/v1/commands");
        GetStringArray(invocationGateway.GetProperty("maskGenerated")).ShouldBe(["/messageId", "/correlationId"]);

        JsonElement invocationBody = invocationGateway.GetProperty("body");
        AssertExactPropertyNames(
            invocationBody,
            "messageId",
            "tenant",
            "domain",
            "aggregateId",
            "commandType",
            "payload",
            "correlationId",
            "idempotencyKey");
        invocationBody.GetProperty("messageId").GetString().ShouldBe("<generated>");
        invocationBody.GetProperty("correlationId").GetString().ShouldBe("<generated>");
        invocationBody.GetProperty("tenant").GetString().ShouldBe(expectedCommandScope);
        invocationBody.GetProperty("domain").GetString().ShouldBe(RefreshTenantAccess.Domain);
        invocationBody.GetProperty("aggregateId").GetString().ShouldBe(inputCommand.AggregateId);
        invocationBody.GetProperty("commandType").GetString().ShouldBe(RefreshTenantAccess.CommandType);
        invocationBody.GetProperty("idempotencyKey").GetString().ShouldBe(idempotencyKey);
        DeserializeStrict<RefreshTenantAccess>(invocationBody.GetProperty("payload")).ShouldBe(inputCommand);

        JsonElement invocationResponse = invocation.GetProperty("scriptedResponse");
        AssertExactPropertyNames(invocationResponse, "statusCode", "body", "echoRequestFields");
        invocationResponse.GetProperty("statusCode").GetInt32().ShouldBe(202);
        AssertEmptyJsonObject(invocationResponse.GetProperty("body"));
        GetStringArray(invocationResponse.GetProperty("echoRequestFields")).ShouldBe(["correlationId", "messageId"]);

        Dictionary<string, JsonElement> invocationAssertions = ReadStrictEqualsAssertions(
            invocation.GetProperty("assertions"),
            expectedCount: 5);
        invocationAssertions["/operation"].GetString().ShouldBe(expectedOperation);
        invocationAssertions["/status"].GetString().ShouldBe("accepted");
        invocationAssertions["/tenant"].GetString().ShouldBe(expectedCommandScope);
        invocationAssertions["/aggregateId"].GetString().ShouldBe(inputCommand.AggregateId);
        invocationAssertions["/idempotencyKey"].GetString().ShouldBe(idempotencyKey);

        // Validate prerequisite step against CreateTenant contract, canonical platform scope, and schema.json.
        JsonElement prerequisites = root.GetProperty("prerequisites");
        prerequisites.ValueKind.ShouldBe(JsonValueKind.Array);
        prerequisites.GetArrayLength().ShouldBe(1);
        JsonElement createStep = prerequisites[0];
        AssertStepPropertyNames(createStep);
        string expectedCreateOperation = $"{module.Name}.{CreateTenant.CommandType}";
        createStep.GetProperty("operation").GetString().ShouldBe(expectedCreateOperation);
        createStep.GetProperty("kind").GetString().ShouldBe("command");

        CreateTenant createCommand = DeserializeStrict<CreateTenant>(createStep.GetProperty("payload"));
        createCommand.TenantId.ShouldBe(inputCommand.TenantId);
        createCommand.AggregateId.ShouldBe(inputCommand.AggregateId);

        JsonElement createEnvelope = createStep.GetProperty("envelope");
        AssertExactPropertyNames(createEnvelope, "tenant");
        createAttribute.PlatformScope.ShouldBe(TenantPlatformScopes.Tenants);
        string expectedCreateScope = TenantPlatformScopes.Tenants;
        createEnvelope.GetProperty("tenant").GetString().ShouldBe(expectedCreateScope);

        JsonElement createGateway = createStep.GetProperty("expectedGateway");
        AssertExactPropertyNames(createGateway, "method", "path", "body", "maskGenerated");
        createGateway.GetProperty("method").GetString().ShouldBe("POST");
        createGateway.GetProperty("path").GetString().ShouldBe("/api/v1/commands");
        GetStringArray(createGateway.GetProperty("maskGenerated")).ShouldBe(["/messageId", "/correlationId"]);

        JsonElement createBody = createGateway.GetProperty("body");
        AssertExactPropertyNames(
            createBody,
            "messageId",
            "tenant",
            "domain",
            "aggregateId",
            "commandType",
            "payload",
            "correlationId");
        createBody.GetProperty("messageId").GetString().ShouldBe("<generated>");
        createBody.GetProperty("correlationId").GetString().ShouldBe("<generated>");
        createBody.GetProperty("tenant").GetString().ShouldBe(expectedCreateScope);
        createBody.GetProperty("domain").GetString().ShouldBe(CreateTenant.Domain);
        createBody.GetProperty("aggregateId").GetString().ShouldBe(createCommand.AggregateId);
        createBody.GetProperty("commandType").GetString().ShouldBe(CreateTenant.CommandType);
        DeserializeStrict<CreateTenant>(createBody.GetProperty("payload")).ShouldBe(createCommand);

        JsonElement createResponse = createStep.GetProperty("scriptedResponse");
        AssertExactPropertyNames(createResponse, "statusCode", "body", "echoRequestFields");
        createResponse.GetProperty("statusCode").GetInt32().ShouldBe(202);
        AssertEmptyJsonObject(createResponse.GetProperty("body"));
        GetStringArray(createResponse.GetProperty("echoRequestFields")).ShouldBe(["correlationId", "messageId"]);

        Dictionary<string, JsonElement> createAssertions = ReadStrictEqualsAssertions(
            createStep.GetProperty("assertions"),
            expectedCount: 4);
        createAssertions["/operation"].GetString().ShouldBe(expectedCreateOperation);
        createAssertions["/status"].GetString().ShouldBe("accepted");
        createAssertions["/tenant"].GetString().ShouldBe(expectedCreateScope);
        createAssertions["/aggregateId"].GetString().ShouldBe(createCommand.AggregateId);

        // Validate postcondition step against GetTenantQuery contract, canonical single-tenant scope, and schema.json.
        JsonElement postconditions = root.GetProperty("postconditions");
        postconditions.ValueKind.ShouldBe(JsonValueKind.Array);
        postconditions.GetArrayLength().ShouldBe(1);
        JsonElement queryStep = postconditions[0];
        AssertStepPropertyNames(queryStep);
        string expectedQueryOperation = $"{module.Name}.{GetTenantQuery.QueryType}";
        queryStep.GetProperty("operation").GetString().ShouldBe(expectedQueryOperation);
        queryStep.GetProperty("kind").GetString().ShouldBe("query");
        getTenantAttribute.PlatformScope.ShouldBeNull();
        getTenantAttribute.AggregateIdProperty.ShouldBe(nameof(GetTenantQuery.TenantId));

        GetTenantQuery inputQuery = DeserializeStrict<GetTenantQuery>(queryStep.GetProperty("payload"));
        inputQuery.TenantId.ShouldBe(inputCommand.TenantId);

        JsonElement queryEnvelope = queryStep.GetProperty("envelope");
        AssertExactPropertyNames(queryEnvelope, "tenant");
        string postconditionScope = queryEnvelope.GetProperty("tenant").GetString().ShouldNotBeNull();
        postconditionScope.ShouldBe(inputCommand.TenantId);
        postconditionScope.ShouldNotBe("system");
        postconditionScope.ShouldNotBe(TenantPlatformScopes.Tenants);

        JsonElement queryGateway = queryStep.GetProperty("expectedGateway");
        AssertExactPropertyNames(queryGateway, "method", "path", "body", "maskGenerated");
        queryGateway.GetProperty("method").GetString().ShouldBe("POST");
        queryGateway.GetProperty("path").GetString().ShouldBe("/api/v1/queries");
        GetStringArray(queryGateway.GetProperty("maskGenerated")).ShouldBeEmpty();

        JsonElement queryBody = queryGateway.GetProperty("body");
        AssertExactPropertyNames(
            queryBody,
            "tenant",
            "domain",
            "aggregateId",
            "queryType",
            "projectionType",
            "payload");
        queryBody.GetProperty("tenant").GetString().ShouldBe(inputCommand.TenantId);
        queryBody.GetProperty("domain").GetString().ShouldBe(GetTenantQuery.Domain);
        queryBody.GetProperty("aggregateId").GetString().ShouldBe(inputQuery.TenantId);
        queryBody.GetProperty("queryType").GetString().ShouldBe(GetTenantQuery.QueryType);
        queryBody.GetProperty("projectionType").GetString().ShouldBe(GetTenantQuery.ProjectionType);
        DeserializeStrict<GetTenantQuery>(queryBody.GetProperty("payload")).TenantId.ShouldBe(inputQuery.TenantId);

        JsonElement queryResponse = queryStep.GetProperty("scriptedResponse");
        AssertExactPropertyNames(queryResponse, "statusCode", "body", "echoRequestFields");
        queryResponse.GetProperty("statusCode").GetInt32().ShouldBe(200);
        GetStringArray(queryResponse.GetProperty("echoRequestFields")).ShouldBeEmpty();
        JsonElement queryResponseBody = queryResponse.GetProperty("body");
        AssertExactPropertyNames(queryResponseBody, "correlationId", "success", "payload");
        UlidPattern.IsMatch(queryResponseBody.GetProperty("correlationId").GetString().ShouldNotBeNull()).ShouldBeTrue();
        queryResponseBody.GetProperty("success").GetBoolean().ShouldBeTrue();
        TenantDetail detail = JsonSerializer.Deserialize<TenantDetail>(
            queryResponseBody.GetProperty("payload").GetRawText(),
            WebResponseOptions).ShouldNotBeNull();
        detail.TenantId.ShouldBe(inputCommand.TenantId);
        detail.Name.ShouldBe(createCommand.Name);
        detail.Description.ShouldBe(createCommand.Description);
        detail.Status.ShouldBe(TenantStatus.Active);

        Dictionary<string, JsonElement> queryAssertions = ReadStrictEqualsAssertions(
            queryStep.GetProperty("assertions"),
            expectedCount: 5);
        queryAssertions["/operation"].GetString().ShouldBe(expectedQueryOperation);
        queryAssertions["/tenant"].GetString().ShouldBe(inputCommand.TenantId);
        queryAssertions["/document/tenantId"].GetString().ShouldBe(inputCommand.TenantId);
        queryAssertions["/document/name"].GetString().ShouldBe(createCommand.Name);
        queryAssertions["/document/status"].GetString().ShouldBe(nameof(TenantStatus.Active));
    }

    [Fact]
    public void RefreshTenantAccess_vector_coexists_with_all_fourteen_frozen_story_4_8_vectors()
    {
        string vectorDirectory = Path.GetDirectoryName(ResolveVectorPath("refresh-tenant-access.json"))!;
        string[] vectorFiles = Directory.GetFiles(vectorDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(static name => name is not null)
            .Select(static name => name!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        vectorFiles.ShouldBe(ExpectedVectorFiles.Order(StringComparer.Ordinal));
    }

    private static void AssertStepPropertyNames(JsonElement step)
        => AssertExactPropertyNames(
            step,
            "operation",
            "kind",
            "payload",
            "envelope",
            "expectedGateway",
            "scriptedResponse",
            "assertions");

    private static void AssertExactPropertyNames(JsonElement element, params string[] expectedNames)
    {
        element.ValueKind.ShouldBe(JsonValueKind.Object);
        string[] actualNames = element.EnumerateObject()
            .Select(static property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        actualNames.ShouldBe(expectedNames.Order(StringComparer.Ordinal));
    }

    private static void AssertEmptyJsonObject(JsonElement element)
    {
        element.ValueKind.ShouldBe(JsonValueKind.Object);
        element.EnumerateObject().Count().ShouldBe(0);
    }

    private static T DeserializeStrict<T>(JsonElement element)
        where T : class
        => JsonSerializer.Deserialize<T>(element.GetRawText(), McpCliPayloadOptions).ShouldNotBeNull();

    private static string[] GetStringArray(JsonElement array)
    {
        array.ValueKind.ShouldBe(JsonValueKind.Array);
        return array.EnumerateArray().Select(static item => item.GetString().ShouldNotBeNull()).ToArray();
    }

    private static Dictionary<string, JsonElement> ReadStrictEqualsAssertions(JsonElement assertions, int expectedCount)
    {
        assertions.ValueKind.ShouldBe(JsonValueKind.Array);
        assertions.GetArrayLength().ShouldBe(expectedCount);

        Dictionary<string, JsonElement> values = new(StringComparer.Ordinal);
        foreach (JsonElement assertion in assertions.EnumerateArray())
        {
            AssertExactPropertyNames(assertion, "path", "operator", "value");
            string op = assertion.GetProperty("operator").GetString().ShouldNotBeNull();
            ValidAssertionOperators.ShouldContain(op);
            op.ShouldBe("equals");

            string path = assertion.GetProperty("path").GetString().ShouldNotBeNull();
            JsonPointerPattern.IsMatch(path).ShouldBeTrue($"Assertion path '{path}' must be a valid JSON Pointer.");
            values.ContainsKey(path).ShouldBeFalse($"Duplicate assertion path '{path}'.");
            values[path] = assertion.GetProperty("value").Clone();
        }

        return values;
    }

    private static string ResolveVectorPath(string fileName)
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "tests", "conformance-vectors", "mcpcli-v1", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Could not locate McpCli conformance vector '{fileName}'.");
    }
}
