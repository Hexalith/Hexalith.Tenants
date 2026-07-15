# Final Merge Technology And Reality Recheck

Verdict: **fail — one high implementation-reality omission remains; the earlier critical findings are otherwise repaired.**

This focused recheck compared the updated spine and canonical architecture with the same local package, source, AppHost, REST/provenance, and UI-host evidence used by the first review. No web research was needed.

## Recheck Results

| Area | Result | Evidence |
| --- | --- | --- |
| Stack pins | Pass with a labeling note | Fluent `5.0.0-rc.4-26180.1`, EventStore `3.64.1`, Memories `2.5.0`, .NET `10.0.301`, Dapr `1.18.4`, Aspire `13.4.6`, xUnit `3.2.2`, and bUnit `2.8.4-preview` match current central pins/assets. |
| Read endpoint inventory | Pass | Both documents now list six reads, including `GET /api/global-administrators`. |
| AD-6 / AD-8 | Pass | Both documents accurately record the generic-gateway divergence, `HandlerComputed`/unknown-freshness consequence, platform REST provenance prerequisite, separate query/command service references, and BFF client split. |
| AD-13 | Pass | Both documents now assign the domain UI host to Tenants, orchestration to a platform/composing host, and mark the repository AppHost as transitional migration debt. This reconciles the architecture with the authoritative domain-module boundary. |
| AD-14 | Partial | The decision itself covers externalized configuration/secrets, shared health/telemetry, non-root SDK containers, external truth, DataProtection, session routing, cursor durability, and replica count. Its implementation-conformance account is incomplete. |

## Remaining High Finding

### AD-14 conformance omits the current health and telemetry gap

AD-14 requires the UI host to “consume shared health [and] telemetry.” The current `Hexalith.Tenants.UI` composition does not call `AddServiceDefaults`, register OpenTelemetry or health checks, or map shared/default health endpoints. Its project file also has no ServiceDefaults dependency. The container portion does conform: root `Directory.Build.targets` supplies the non-root `app` user, .NET 10 Alpine base, and port 8080; the UI also reads configuration rather than embedding production secrets.

Despite that reality, both documents describe the AD-14 implementation divergence only as the multi-replica DataProtection/session/cursor gate. Their claim that the reality check found exactly three active divergences therefore understates AD-14.

**Required documentation correction:** expand the AD-14 conformance/gap/handoff text in both artifacts to say that shared health endpoints and OpenTelemetry/ServiceDefaults integration are also unimplemented and must be supplied through the approved platform seam, without adding generic hosting infrastructure to the domain module. Keep the existing single-replica gate.

## Remaining Low Finding

### FrontComposer `3.1.1` is a package baseline, not the exact source dependency revision

`3.1.1` is the current central package pin, so the refreshed version ledger is materially correct. However, `Hexalith.Tenants.UI.csproj` references FrontComposer Contracts/Shell source projects unconditionally; the root gitlink is several commits beyond the `v3.1.1` tag, and the current submodule working tree is later still. Label the stack row “FrontComposer package baseline” or explicitly state that the UI's source revision is implementation state governed by the root gitlink. This is not a readiness blocker.

## Confirmed Repairs

- The previous false implication that a UI-only transport swap would satisfy AD-6 is gone.
- AD-8 is no longer claimed end-to-end conformant while freshness is normalized to `Unknown`.
- The canonical implementation sequence correctly orders platform provenance, composing-host references, BFF split, then direct six-route reads.
- AD-13 no longer treats a repository-owned AppHost as the target architecture.
- AD-14 supplies the missing operational ownership and single-replica safety invariant at the architecture level.

## Gate Conclusion

The architecture decision set is now coherent and reality-based. After recording the missing shared health/telemetry implementation divergence in both documents, this technology/reality lens can pass; the FrontComposer label can be cleaned up without blocking readiness.
