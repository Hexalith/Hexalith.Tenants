# Tenant configuration read policy

`Tenants:ConfigurationReadPolicy` controls positive configuration prefix grants and
the exact keys approved for display. Invalid policy withholds all configuration
reads, including administrator reads. Validation is cached until configuration
reload; every reload validates the complete effective declaration again.

New deployments can declare collection intent explicitly:

```json
{
  "Tenants": {
    "ConfigurationReadPolicy": {
      "CollectionShapes": {
        "PrefixGrants": "array",
        "DisplaySafe": "array"
      },
      "PrefixGrants": [],
      "DisplaySafe": []
    }
  }
}
```

When `CollectionShapes` is present, both names must have the exact value `array`.
Metadata names follow case-insensitive .NET configuration keys; tenant, subject,
prefix and display-safe key values retain their existing exact comparisons.
Missing, unknown, `scalar`, nested or additional discriminator declarations fail
closed. Observable scalar collection values, scalar/element conflicts and
non-indexed collection children also fail closed. Existing semantic validation
still applies to every grant and display-safe key. Declare the complete policy in
one effective provider shape; an override must not leave scalar parent values
alongside indexed elements from another provider.

The discriminator is deployment-owned metadata declaring intent, not proof of the
original JSON representation. .NET configuration flattens providers: an empty
array, empty scalar and some empty overrides can be observationally identical.
An explicit `array` declaration resolves intent in that ambiguous state; the
provider never reconstructs raw JSON from flattened keys.

Existing deployments without `CollectionShapes` retain legacy behavior. Valid
grant lists and the shipped legacy empty-array policy remain compatible. The
legacy empty-array/empty-scalar ambiguity remains unresolved and is deliberately
disclosed here. Shipped `appsettings.json` deliberately omits the discriminator so
upgrading does not opt layered existing deployments into stricter validation.
No existing configuration is migrated automatically. Opt in by
adding both discriminator values to your deployment configuration, then reload
and verify the resulting configuration availability.
