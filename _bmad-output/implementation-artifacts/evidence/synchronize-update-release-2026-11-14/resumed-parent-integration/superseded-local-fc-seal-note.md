# Dependency source projection renewal, 2026-11-15

The committed source `38856d023dedd7c49da69b1cdb2fc54396f22f38` selects Platform
`f6f85032598865b9123f16364448bfd02c0ff5b0` and Builds
`73e60af8a1fcdf3f06726d5d1b6e1199cce778ee`. Current CI and Quality
correctly refuse the earlier owner pin. This maintenance regenerates exactly
four source projection fields from those committed objects.

| Field | Renewed value |
| --- | --- |
| `owner_commit` | `f6f85032598865b9123f16364448bfd02c0ff5b0` |
| `inventory_sha256` | `874d7ff434413ef9e11324fd4ecfae1bd9dda527f8cc405e89ffa5a0e5502fd0` |
| `builds_commit` | `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee` |
| `root_selectors_sha256` | `5632f8e8b96f16d6274597b9c38bdec0aa14fbfa824b7457f99235ab1d7eaeaf` |

The Platform inventory mapping, both canonical source files, Compile links,
inventory-only identities and consumer configuration retain their sealed hashes.
Platform remains evidence-only. Every evaluator authorization and executable
workflow byte stays unchanged. The current catalog profile also renews only
`Microsoft.CodeAnalysis.Workspaces.Common` from `5.9.0` to the selected `5.12.0`;
the unchanged exact-version validator previously refused this actual catalog.
Historical policy objects retain their original compiler requirement. No runtime
acceptance or publication is inferred.

The seal must land before a separate documentation-only activation push. That
successor must preserve all selectors, configuration and policy bytes and use
the landed seal as its immediate parent and actual event base. Exact-object
acquisition, graph/semantic checks, hosted CI and Quality remain required.

This renewal runs under the release spec’s delegated synchronization and CI
repair authorization. It records no invented human review or fallback approval.
