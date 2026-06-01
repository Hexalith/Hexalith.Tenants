using System.Reflection;
using System.Runtime.CompilerServices;

using Hexalith.Tenants.Actors;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Architecture;

/// <summary>
/// Story 7.5 AC3 — architecture assertion that the Tenants host carries no authoritative,
/// instance-local tenant state.
/// </summary>
/// <remarks>
/// <para>
/// Authoritative tenant state lives in EventStore (events/snapshots) and the DAPR state store
/// (projections). A writable (non-<c>readonly</c>) static field is the classic carrier of mutable
/// process-local state that would make behavior depend on which replica served a request, so the
/// <c>Hexalith.Tenants</c> host assembly must contain none in its own (non compiler-generated) types.
/// </para>
/// <para>
/// In-process state that DOES exist is non-authoritative and allowed: static <c>readonly</c>
/// <see cref="System.Text.Json.JsonSerializerOptions"/> / source-generated logger delegates
/// (configuration-only), the per-actor-lifetime ETag payload cache and orphan-log dedup set inside
/// <c>TenantsProjectionActor</c> (rebuilt from the durable state store on activation), and the
/// client-side <c>InMemoryTenantProjectionStore</c> / reflection cache in the separate
/// <c>Hexalith.Tenants.Client</c> consumer assembly. None hold authoritative tenant state.
/// </para>
/// </remarks>
public class StatelessHostStateTests {
    [Fact]
    public void TenantsHostAssembly_HasNoWritableStaticFields_HoldingInstanceLocalState() {
        Assembly hostAssembly = typeof(TenantsProjectionActor).Assembly;

        List<string> writableStaticFields = hostAssembly
            .GetTypes()
            // Exclude compiler/source-generated types (e.g. the AddOpenApi XML-comment cache and lambda
            // display classes). Their names carry '<' and they hold framework/doc state, not tenant state.
            .Where(type => !IsCompilerGenerated(type) && !type.Name.Contains('<', StringComparison.Ordinal))
            .SelectMany(type => type.GetFields(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Where(field => !field.IsLiteral && !field.IsInitOnly)
            .Where(field => !IsCompilerGenerated(field) && !field.Name.Contains('<', StringComparison.Ordinal))
            .Select(field => $"{field.DeclaringType?.FullName}.{field.Name}")
            .ToList();

        writableStaticFields.ShouldBeEmpty(
            "Tenants host types must not hold authoritative tenant state in writable static fields; "
            + "state belongs to EventStore and the DAPR state store. Offenders: "
            + string.Join(", ", writableStaticFields));
    }

    private static bool IsCompilerGenerated(MemberInfo member)
        => member.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);
}
