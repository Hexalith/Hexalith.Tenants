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

        AssertParityGatePasses(doubles);
    }

    [Fact]
    public void Localizer_double_parity_gate_can_fail()
    {
        // Guards the gate itself. Each control drives the REAL gate and pins BOTH that the gate rejects the
        // double and which rule rejected it. Asserting only that the gate throws let a control pass on any
        // failure at all -- and one of them was doing exactly that: the double meant to prove the French
        // rule was being rejected by the neutral-bundle rule instead, so the French rule had no proof.
        AssertGateRejects<DivergentLocalizerDouble>("but the shipped value is");
        AssertGateRejects<EmptyLocalizerDouble>("enumerates no keys");
        AssertGateRejects<HiddenIndexerLocalizerDouble>("indexer returns");
        AssertGateRejects<FrenchlessLocalizerDouble>("has no French value");
        AssertGateRejects<UnconstructableLocalizerDouble>("could not be constructed");
    }

    [Fact]
    public void Localizer_double_discovery_does_not_require_a_parameterless_constructor()
    {
        // Discovery previously filtered on a parameterless constructor, so a double taking one argument was
        // never inspected and the gate's own "could not be constructed" failure was unreachable. Discovery
        // must now find such a type, and the gate must fail on it rather than pass over it.
        LocalizerDoubleTypes(excludeControls: false).ShouldContain(typeof(UnconstructableLocalizerDouble));
        LocalizerDoubleTypes().ShouldNotContain(typeof(UnconstructableLocalizerDouble));
    }

    /// <summary>
    /// Asserts the real parity gate passes over the given doubles, reporting every violation it found.
    /// </summary>
    private static void AssertParityGatePasses(IReadOnlyList<Type> doubles)
    {
        ParityGateResult result = RunParityGate(doubles);

        // Failures are reported before the coverage guard: a double that enumerates nothing, or that cannot
        // be constructed, also leaves AssertedKeys at zero, and reporting the coverage guard first replaced
        // its specific diagnosis with a generic one.
        result.Failures.ShouldBeEmpty(string.Join(Environment.NewLine, result.Failures));
        result.AssertedKeys.ShouldBeGreaterThan(0, "The parity gate must inspect at least one stubbed key.");
    }

    /// <summary>
    /// Drives the real gate over one deliberately broken double and pins both that the gate rejects it and
    /// which rule rejected it.
    /// </summary>
    /// <typeparam name="TDouble">The deliberately broken double.</typeparam>
    /// <param name="expectedRule">Text from the failure message the planted defect must produce.</param>
    private static void AssertGateRejects<TDouble>(string expectedRule)
        where TDouble : IStringLocalizer<TenantsResources>
    {
        IReadOnlyList<string> failures = RunParityGate([typeof(TDouble)]).Failures;
        failures.ShouldContain(
            failure => failure.Contains(expectedRule, StringComparison.Ordinal),
            $"{typeof(TDouble).Name} must be rejected by the '{expectedRule}' rule, but the gate reported: "
                + string.Join(" | ", failures));
        _ = Should.Throw<ShouldAssertException>(() => AssertParityGatePasses([typeof(TDouble)]));
    }

    /// <summary>The outcome of one parity-gate run: every violation found, and how many keys were inspected.</summary>
    /// <param name="Failures">Every violation, one message per rule per key.</param>
    /// <param name="AssertedKeys">The number of stubbed keys inspected.</param>
    private sealed record ParityGateResult(IReadOnlyList<string> Failures, int AssertedKeys);

    /// <summary>
    /// Runs the real parity gate over the given doubles and returns what it found, so both the suite-wide
    /// gate and its own can-fail proof exercise exactly this code and can each assert on the exact rule.
    /// Returning the findings rather than asserting inline is deliberate: Shouldly elides long collection
    /// and string renderings, and the elided text is precisely what names the violated rule.
    /// </summary>
    private static ParityGateResult RunParityGate(IReadOnlyList<Type> doubles)
    {
        ResourceManager manager = new(typeof(TenantsResources));
        List<string> failures = [];
        int assertedKeys = 0;
        foreach (Type type in doubles)
        {
            object? instance;
            try
            {
                instance = Activator.CreateInstance(type, nonPublic: true);
            }
            catch (Exception exception) when (exception is MissingMethodException or TargetInvocationException)
            {
                // A double the gate cannot construct is a failure, not a skip. Discovery used to require a
                // parameterless constructor, so any double that took one argument was never inspected at
                // all -- the quietest way there is to opt out of a gate that exists to be inescapable.
                instance = null;
            }

            if (instance is not IStringLocalizer<TenantsResources> localizer)
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

                // Invariant parity alone proves EN only; it cannot observe a missing or fallback French
                // value, so every stubbed key is also resolved in an explicit `fr` culture that does not
                // fall back to its parents. Checked BEFORE the neutral-bundle verdict: a key missing from
                // the neutral bundle used to short-circuit this check, so the gate under-reported, and the
                // control double meant to exercise this rule was in fact being rejected by the other one.
                if (!FrenchKeys.Value.Contains(stubbed.Name))
                {
                    failures.Add(
                        $"{type.FullName}: '{stubbed.Name}' has no French value in TenantsResources.fr.resx.");
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
            }
        }

        return new ParityGateResult(failures, assertedKeys);
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

    /// <summary>
    /// Every localizer double in this assembly. Constructability is deliberately NOT a discovery filter:
    /// filtering on it silently excluded exactly the doubles the gate could not verify, turning the gate's
    /// own construction failure into dead code. A double that cannot be constructed is now discovered and
    /// reported as a failure.
    /// </summary>
    private static IReadOnlyList<Type> LocalizerDoubleTypes(bool excludeControls = true)
        => [.. typeof(LocalizerDoubleParityTests).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract
                && !type.IsInterface
                && !type.IsGenericTypeDefinition
                && (!excludeControls || !ControlDoubles.Contains(type))
                && typeof(IStringLocalizer<TenantsResources>).IsAssignableFrom(type))
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)];

    /// <summary>The deliberately broken doubles that exist only to drive the gate to failure.</summary>
    private static readonly IReadOnlyList<Type> ControlDoubles =
    [
        typeof(DivergentLocalizerDouble),
        typeof(EmptyLocalizerDouble),
        typeof(HiddenIndexerLocalizerDouble),
        typeof(FrenchlessLocalizerDouble),
        typeof(UnconstructableLocalizerDouble),
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

        // The literal shipped value, not a ResourceManager read-back. Reading the value from the same
        // ResourceManager the gate compares it against made that half of the control tautological: it agreed
        // with the shipped bundle by construction, whatever the bundle said. Written out, this double
        // reaches the gate with a value that is genuinely correct, so the only defect it plants -- and the
        // only reason the gate may reject it -- is the indexer disagreeing with its own enumeration.
        private const string ShippedValue = "Tenants";

        public LocalizedString this[string name] => new(name, "Copy the product does not ship");

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => [new LocalizedString(Key, ShippedValue)];
    }

    /// <summary>A double with no parameterless constructor, used to prove discovery cannot drop one.</summary>
    private sealed class UnconstructableLocalizerDouble(string copy) : IStringLocalizer<TenantsResources>
    {
        public LocalizedString this[string name] => new(name, copy);

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => [new LocalizedString("Tenants.List.Title", copy)];
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
