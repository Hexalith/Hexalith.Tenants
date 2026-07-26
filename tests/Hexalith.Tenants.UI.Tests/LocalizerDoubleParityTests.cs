using System.Globalization;
using System.Reflection;
using System.Resources;

using Hexalith.Tenants.UI.Resources;

using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

/// <summary>
/// Suite-wide gate: every <see cref="IStringLocalizer{TenantsResources}"/> double in this assembly must
/// return the exact shipped resource value for every key it stubs. A double that ships copy the product does
/// not, or that claims semantics the shipped copy contradicts, lets a green suite certify behavior that does
/// not exist. The gate is inescapable: a double that enumerates no keys is a failure, not a skip, and a
/// double whose indexer disagrees with its own <c>GetAllStrings</c> is a failure too, because otherwise a
/// double could return arbitrary copy through the indexer the components actually call while presenting an
/// empty or sanitized enumeration to this gate.
/// </summary>
public sealed class LocalizerDoubleParityTests
{
    [Fact]
    public void Every_localizer_double_returns_the_exact_shipped_resource_value_for_every_stubbed_key()
    {
        IReadOnlyList<Type> doubles = LocalizerDoubleTypes();

        doubles.ShouldNotBeEmpty("The parity gate must actually find the localizer doubles it guards.");

        RunParityGate(doubles);
    }

    [Fact]
    public void Localizer_double_parity_gate_can_fail()
    {
        // Guards the gate itself. Each control case drives the REAL gate and asserts that it throws, rather
        // than asserting that a divergent double happens to be excluded from it.
        _ = Should.Throw<ShouldAssertException>(
            () => RunParityGate([typeof(DivergentLocalizerDouble)]),
            "A double that ships copy the product does not must fail the parity gate.");

        _ = Should.Throw<ShouldAssertException>(
            () => RunParityGate([typeof(EmptyLocalizerDouble)]),
            "A double that enumerates no keys must fail the parity gate, never silently skip it.");

        _ = Should.Throw<ShouldAssertException>(
            () => RunParityGate([typeof(HiddenIndexerLocalizerDouble)]),
            "A double whose indexer disagrees with its own GetAllStrings must fail the parity gate.");

        _ = Should.Throw<ShouldAssertException>(
            () => RunParityGate([typeof(FrenchlessLocalizerDouble)]),
            "A key with no French value must fail the parity gate.");
    }

    /// <summary>
    /// Runs the real parity gate over the given doubles. Throws a Shouldly assertion failure describing every
    /// violation, so both the suite-wide gate and its own can-fail proof exercise exactly this code.
    /// </summary>
    private static void RunParityGate(IReadOnlyList<Type> doubles)
    {
        ResourceManager manager = new(typeof(TenantsResources));
        List<string> failures = [];
        int assertedKeys = 0;
        foreach (Type type in doubles)
        {
            if (Activator.CreateInstance(type, nonPublic: true) is not IStringLocalizer<TenantsResources> localizer)
            {
                failures.Add($"{type.FullName}: could not be constructed for parity inspection.");
                continue;
            }

            LocalizedString[] stubbedKeys = [.. localizer.GetAllStrings(includeParentCultures: false)];
            if (stubbedKeys.Length == 0)
            {
                // Failing rather than skipping: an empty enumeration is the one escape hatch a double could
                // use to opt out of the gate entirely while still feeding arbitrary copy to the components.
                failures.Add($"{type.FullName}: enumerates no keys, so its stubbed copy is unverifiable.");
                continue;
            }

            foreach (LocalizedString stubbed in stubbedKeys)
            {
                assertedKeys++;
                string indexed = localizer[stubbed.Name].Value;
                if (!string.Equals(indexed, stubbed.Value, StringComparison.Ordinal))
                {
                    failures.Add(
                        $"{type.FullName}: indexer returns \"{indexed}\" for '{stubbed.Name}' but GetAllStrings reports \"{stubbed.Value}\".");
                }

                string? shipped = manager.GetString(stubbed.Name, CultureInfo.InvariantCulture);
                if (shipped is null)
                {
                    failures.Add($"{type.FullName}: stubs '{stubbed.Name}', which TenantsResources.resx does not ship.");
                    continue;
                }

                if (!string.Equals(shipped, stubbed.Value, StringComparison.Ordinal))
                {
                    failures.Add(
                        $"{type.FullName}: '{stubbed.Name}' stubs \"{stubbed.Value}\" but the shipped value is \"{shipped}\".");
                }

                // Invariant parity alone proves EN only; it cannot observe a missing or fallback French
                // value, so every stubbed key is also resolved in an explicit `fr` culture that does not
                // fall back to its parents.
                if (!FrenchKeys.Value.Contains(stubbed.Name))
                {
                    failures.Add(
                        $"{type.FullName}: '{stubbed.Name}' has no French value in TenantsResources.fr.resx.");
                }
            }
        }

        assertedKeys.ShouldBeGreaterThan(0, "The parity gate must inspect at least one stubbed key.");
        failures.ShouldBeEmpty(string.Join(Environment.NewLine, failures));
    }

    /// <summary>
    /// Resolves the explicit <c>fr</c> resource set without falling back to its parent cultures, so a key
    /// present only in the neutral bundle is observed as a missing French value rather than an English one.
    /// </summary>
    private static readonly Lazy<HashSet<string>> FrenchKeys = new(static () =>
    {
        ResourceManager manager = new(typeof(TenantsResources));
        ResourceSet? french = manager.GetResourceSet(
            CultureInfo.GetCultureInfo("fr"),
            createIfNotExists: true,
            tryParents: false);
        HashSet<string> keys = new(StringComparer.Ordinal);
        if (french is null)
        {
            return keys;
        }

        foreach (System.Collections.DictionaryEntry entry in french)
        {
            if (entry.Key is string key)
            {
                _ = keys.Add(key);
            }
        }

        return keys;
    });

    private static IReadOnlyList<Type> LocalizerDoubleTypes()
        => [.. typeof(LocalizerDoubleParityTests).Assembly
            .GetTypes()
            .Where(static type => !type.IsAbstract
                && !type.IsInterface
                && !type.IsGenericTypeDefinition
                && !ControlDoubles.Contains(type)
                && typeof(IStringLocalizer<TenantsResources>).IsAssignableFrom(type)
                && type.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    binder: null,
                    Type.EmptyTypes,
                    modifiers: null) is not null)
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)];

    /// <summary>The deliberately broken doubles that exist only to drive the gate to failure.</summary>
    private static readonly IReadOnlyList<Type> ControlDoubles =
    [
        typeof(DivergentLocalizerDouble),
        typeof(EmptyLocalizerDouble),
        typeof(HiddenIndexerLocalizerDouble),
        typeof(FrenchlessLocalizerDouble),
    ];

    /// <summary>A deliberately divergent double used only to prove the parity gate can fail.</summary>
    private sealed class DivergentLocalizerDouble : IStringLocalizer<TenantsResources>
    {
        private const string Key = "Tenants.List.Title";

        public LocalizedString this[string name] => new(name, "Copy the product does not ship");

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => [new LocalizedString(Key, "Copy the product does not ship")];
    }

    /// <summary>A double that enumerates nothing, used only to prove the gate fails instead of skipping.</summary>
    private sealed class EmptyLocalizerDouble : IStringLocalizer<TenantsResources>
    {
        public LocalizedString this[string name] => new(name, "Copy the product does not ship");

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }

    /// <summary>A double whose indexer contradicts its own enumeration.</summary>
    private sealed class HiddenIndexerLocalizerDouble : IStringLocalizer<TenantsResources>
    {
        private const string Key = "Tenants.List.Title";

        public LocalizedString this[string name] => new(name, "Copy the product does not ship");

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => [new LocalizedString(Key, new ResourceManager(typeof(TenantsResources))
                .GetString(Key, CultureInfo.InvariantCulture)!)];
    }

    /// <summary>A double stubbing a key that no French bundle ships.</summary>
    private sealed class FrenchlessLocalizerDouble : IStringLocalizer<TenantsResources>
    {
        private const string Key = "Tenants.List.State.KeyThatDoesNotExist.Title";

        public LocalizedString this[string name] => new(name, "Unshipped copy");

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => [new LocalizedString(Key, "Unshipped copy")];
    }
}
