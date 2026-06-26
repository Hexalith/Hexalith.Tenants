using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Contracts.Queries;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Documentation;

public class EventContractReferenceDocumentationTests {
    private static readonly Regex JsonFenceRegex = new(
        "```json\\s*(?<json>.*?)\\s*```",
        RegexOptions.Compiled | RegexOptions.Singleline);

    [Fact]
    public void Event_contract_reference_lists_every_public_contract_type() {
        string reference = ReadReference();

        foreach (Type contractType in PublicContractTypes()) {
            reference.Contains(ContractDisplayName(contractType), StringComparison.Ordinal)
                .ShouldBeTrue($"{contractType.FullName} must be listed in docs/event-contract-reference.md.");
        }

        reference.ShouldContain("Hexalith.Tenants.Contracts");
        reference.ShouldContain("owning aggregate/domain");
        reference.ShouldContain("intended caller");
        reference.ShouldContain("intended consumer");
    }

    [Fact]
    public void Event_contract_reference_documents_every_public_contract_member_and_enum_value() {
        string reference = ReadReference();

        foreach (Type contractType in PublicContractTypes()) {
            foreach (PropertyInfo property in DocumentedProperties(contractType)) {
                ContainsDocumentedMember(reference, property.Name)
                    .ShouldBeTrue($"{contractType.FullName}.{property.Name} must be documented in docs/event-contract-reference.md.");
            }

            if (!contractType.IsEnum) {
                continue;
            }

            foreach (string enumName in Enum.GetNames(contractType)) {
                reference.Contains($"`{enumName}`", StringComparison.Ordinal)
                    .ShouldBeTrue($"{contractType.FullName}.{enumName} must be documented in docs/event-contract-reference.md.");
            }
        }
    }

    [Fact]
    public void Event_contract_reference_documents_current_DAPR_CloudEvents_and_ordering_guidance() {
        string reference = ReadReference();

        reference.ShouldContain("tenants.events");
        reference.ShouldContain("shared topic");
        reference.ShouldContain("CloudEvents 1.0");
        reference.ShouldContain("event type");
        reference.ShouldContain("at-least-once");
        reference.ShouldContain("idempotent");
        reference.ShouldContain("MessageId");
        reference.ShouldContain("SequenceNumber");
        reference.ShouldContain("aggregate-local");
        reference.ShouldContain("must not be treated as global ordering across services");
        reference.ShouldContain("idempotent-event-processing.md");
        reference.ShouldNotContain("per-event topic");
    }

    [Fact]
    public void Event_contract_reference_documents_payload_and_envelope_serialization_shape() {
        string reference = ReadReference();

        reference.ShouldContain("System.Text.Json");
        reference.ShouldContain("default `System.Text.Json` options");
        reference.ShouldContain("PascalCase");
        reference.ShouldContain("DateTimeOffset");
        reference.ShouldContain("+00:00");
        reference.ShouldContain("CloudEvents `id`");
        reference.ShouldContain("CloudEvents `source`");
        reference.ShouldContain("CloudEvents `type`");
        reference.ShouldContain("CloudEvents `specversion`");
        reference.ShouldContain("EventStore `MessageId`");
        reference.ShouldContain("EventStore `SequenceNumber`");
        reference.ShouldContain("EventStore `Timestamp`");
        reference.ShouldContain("EventStore `CorrelationId`");
        reference.ShouldContain("EventStore `CausationId`");
        reference.ShouldContain("EventStore `UserId`");
    }

    [Fact]
    public void Event_contract_reference_json_examples_are_valid_and_use_contract_enum_serialization() {
        MatchCollection matches = JsonFenceRegex.Matches(ReadReference());
        matches.Count.ShouldBeGreaterThan(0);

        foreach (Match match in matches) {
            string json = match.Groups["json"].Value;
            json.ShouldNotContain("<");
            using JsonDocument document = JsonDocument.Parse(json);
            document.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
        }

        JsonElement userAdded = FindJsonObject("Role");
        userAdded.GetProperty("Role").GetString().ShouldBe("TenantContributor");
        userAdded.Deserialize<UserAddedToTenant>().ShouldNotBeNull().Role.ShouldBe(TenantRole.TenantContributor);

        JsonElement userRoleChanged = FindJsonObject("OldRole");
        userRoleChanged.Deserialize<UserRoleChanged>().ShouldNotBeNull().NewRole.ShouldBe(TenantRole.TenantOwner);

        JsonElement lifecycleRejection = FindJsonObject("RequestedStatus");
        TenantLifecycleStateAlreadySetRejection rejection = lifecycleRejection.Deserialize<TenantLifecycleStateAlreadySetRejection>().ShouldNotBeNull();
        rejection.CurrentStatus.ShouldBe(TenantStatus.Disabled);
        rejection.RequestedStatus.ShouldBe(TenantStatus.Disabled);
    }

    [Fact]
    public void Event_contract_reference_covers_drift_prone_contract_details() {
        string reference = ReadReference();

        reference.ShouldContain("TenantLifecycleStateAlreadySetRejection");
        reference.ShouldContain("ConfigurationKeyNotFoundRejection");
        reference.ShouldContain("GlobalAdministratorAlreadyExistsRejection");
        reference.ShouldContain("GlobalAdministratorNotFoundRejection");
        reference.ShouldContain("TenantUpdated");
        reference.ShouldContain("UpdatedAt");
        reference.ShouldContain("GlobalAdministratorSet");
        reference.ShouldContain("ActorUserId");
        reference.ShouldContain("SetAt");
        reference.ShouldContain("GlobalAdministratorRemoved");
        reference.ShouldContain("RemovedAt");
        reference.ShouldContain("PaginatedResult<TenantSummary>");
        reference.ShouldContain("TenantDetail");
        reference.ShouldContain("PaginatedResult<TenantMember>");
        reference.ShouldContain("PaginatedResult<UserTenantMembership>");
        reference.ShouldContain("PaginatedResult<TenantAuditEntry>");
        reference.ShouldContain("PaginatedResult<GlobalAdministratorSummary>");
    }

    [Fact]
    public void Event_contract_reference_matches_source_backed_authorization_and_rejection_outcomes() {
        string reference = ReadReference();

        reference.ShouldContain("| `DisableTenant` | `Hexalith.Tenants.Contracts` | `TenantAggregate` / `tenants` | `TenantId` | Trusted global administrator | `TenantDisabled`; `TenantNotFoundRejection`, `TenantLifecycleStateAlreadySetRejection`, `InsufficientPermissionsRejection` |");
        reference.ShouldContain("| `EnableTenant` | `Hexalith.Tenants.Contracts` | `TenantAggregate` / `tenants` | `TenantId` | Trusted global administrator | `TenantEnabled`; `TenantNotFoundRejection`, `TenantLifecycleStateAlreadySetRejection`, `InsufficientPermissionsRejection` |");
        reference.ShouldContain("| `SetTenantConfiguration` | `Hexalith.Tenants.Contracts` | `TenantAggregate` / `tenants` | `TenantId`, `Key`, `Value` | Tenant owner or trusted global administrator |");
        reference.ShouldContain("| `RemoveTenantConfiguration` | `Hexalith.Tenants.Contracts` | `TenantAggregate` / `tenants` | `TenantId`, `Key` | Tenant owner or trusted global administrator |");
        reference.ShouldContain("Requires a tenant owner or trusted global administrator; tenant contributors cannot change tenant configuration.");
        reference.ShouldContain("Requires a tenant owner or trusted global administrator; tenant contributors cannot remove tenant configuration.");
        reference.ShouldNotContain("| `SetTenantConfiguration` | `Hexalith.Tenants.Contracts` | `TenantAggregate` / `tenants` | `TenantId`, `Key`, `Value` | Tenant contributor/owner or trusted global administrator |");
        reference.ShouldNotContain("| `RemoveTenantConfiguration` | `Hexalith.Tenants.Contracts` | `TenantAggregate` / `tenants` | `TenantId`, `Key` | Tenant contributor/owner or trusted global administrator |");
    }

    private static IEnumerable<Type> PublicContractTypes() {
        Assembly assembly = typeof(CreateTenant).Assembly;

        Type[] contractTypes = [.. assembly
            .GetTypes()
            .Where(static type => type.IsPublic
                && type.Namespace is not null
                && type.Namespace.StartsWith("Hexalith.Tenants.Contracts.", StringComparison.Ordinal)
                && (type.Namespace.EndsWith(".Commands", StringComparison.Ordinal)
                    || type.Namespace.EndsWith(".Events", StringComparison.Ordinal)
                    || type.Namespace.EndsWith(".Events.Rejections", StringComparison.Ordinal)
                    || type.Namespace.EndsWith(".Queries", StringComparison.Ordinal)
                    || type.Namespace.EndsWith(".Enums", StringComparison.Ordinal)))
            .Where(static type => type != typeof(IEventPayload))
            .Where(static type => type != typeof(IRejectionEvent))
            .Where(static type => type != typeof(IQueryContract))
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)];

        contractTypes.Count(static type => type.Namespace == typeof(CreateTenant).Namespace).ShouldBe(12);
        contractTypes.Count(static type => type.Namespace == typeof(TenantCreated).Namespace).ShouldBe(11);
        contractTypes.Count(static type => type.Namespace == typeof(TenantNotFoundRejection).Namespace).ShouldBe(14);
        contractTypes.Count(static type => typeof(IQueryContract).IsAssignableFrom(type)).ShouldBe(6);
        contractTypes.ShouldContain(typeof(PaginatedResult<>));
        contractTypes.Count(static type => type.IsEnum).ShouldBe(3);

        return contractTypes;
    }

    private static IEnumerable<PropertyInfo> DocumentedProperties(Type contractType)
        => contractType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(static property => property.GetMethod is { IsPublic: true })
            .OrderBy(static property => property.Name, StringComparer.Ordinal);

    private static bool ContainsDocumentedMember(string reference, string memberName)
        => reference.Contains($"`{memberName}`", StringComparison.Ordinal)
            || reference.Contains($"`{memberName}?`", StringComparison.Ordinal);

    private static string ContractDisplayName(Type contractType)
        => contractType.IsGenericType
            ? $"{contractType.Name[..contractType.Name.IndexOf('`', StringComparison.Ordinal)]}<T>"
            : contractType.Name;

    private static JsonElement FindJsonObject(string propertyName) {
        foreach (Match match in JsonFenceRegex.Matches(ReadReference())) {
            using JsonDocument document = JsonDocument.Parse(match.Groups["json"].Value);
            JsonElement root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(propertyName, out _)) {
                return root.Clone();
            }
        }

        throw new InvalidOperationException($"No JSON example contains property {propertyName}.");
    }

    private static string ReadReference()
        => File.ReadAllText(RepositoryPath("docs", "event-contract-reference.md"));

    private static string RepositoryPath(params string[] segments) {
        string repoRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string direct = Path.GetFullPath(Path.Combine(
            new[] { repoRoot }.Concat(segments).ToArray()));
        if (File.Exists(direct) || Directory.Exists(direct)) {
            return direct;
        }

        // A dependent module (e.g. Hexalith.EventStore) is a nested submodule of this repository
        // that may be left uninitialized when this repository is itself a submodule of a parent
        // that checks the dependency out as a sibling checkout. Fall back to that sibling.
        if (segments.Length > 0 && segments[0].StartsWith("Hexalith.", StringComparison.Ordinal)) {
            string sibling = Path.GetFullPath(Path.Combine(
                new[] { repoRoot, ".." }.Concat(segments).ToArray()));
            if (File.Exists(sibling) || Directory.Exists(sibling)) {
                return sibling;
            }
        }

        return direct;
    }
}
