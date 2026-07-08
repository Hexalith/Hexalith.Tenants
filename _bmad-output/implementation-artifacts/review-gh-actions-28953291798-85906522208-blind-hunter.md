# Blind Hunter Review Prompt

Use the `bmad-review-adversarial-general` skill.

You get only the diff below. Do not inspect the repository, spec, docs, or conversation. Review for bugs, risky assumptions, hidden regressions, and missing tests. Return findings only when they are actionable, with severity and the exact diff location.

```diff
diff --git a/_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md b/_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md
new file mode 100644
index 0000000..2a1050a
--- /dev/null
+++ b/_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md
@@ -0,0 +1,63 @@
+---
+title: 'Fix GitHub Actions hosted UI route smoke flake from run 28953291798 job 85906522208'
+type: 'bugfix'
+created: '2026-07-08'
+status: 'in-progress'
+baseline_commit: '3d96d0aa89bf579d66d879f7d3d949a2d87d9b71'
+context:
+  - references/Hexalith.AI.Tools/hexalith-ux-instructions.md
+---
+
+<frozen-after-approval reason="human-owned intent - do not modify unless human renegotiates">
+
+## Intent
+
+**Problem:** The GitHub Actions `ci / aspire-tests` job `85906522208` in run `28953291798` failed only three `TenantsUiRouteSmokeTests` checks: `/tenants`, `/tenants/{id}`, and `/tenants/{id}/audit` returned HTTP 200 but the captured hosted HTML did not yet contain the expected unauthorized `data-testid` markers. The same full `Category!=Performance` Aspire suite passes locally after a fresh Release build, and the expected UI component markers still exist, so the evidence points to a hosted smoke-test readiness/timing gap rather than missing production UI markup.
+
+**Approach:** Harden the hosted UI smoke tests so route assertions wait briefly for a page-specific readiness marker before evaluating the existing unauthorized, navigation, support-safe, and no-data-leak assertions. Preserve the current fail-closed expectations; the retry only absorbs startup/first-paint timing where the UI endpoint is reachable before the tested route content is rendered.
+
+## Boundaries & Constraints
+
+**Always:** Keep the tests scoped to `Hexalith.Tenants.IntegrationTests` and use the existing `AspireTopologyFixture.TenantsUiClient`. Preserve every existing route-specific assertion that proves unauthorized state, route context, safe return links, and absence of sample tenant/raw payload/token content. Use `.slnx` only for restore/build and run the integration test project directly.
+
+**Ask First:** Halt before changing production UI components, route templates, authentication behavior, FrontComposer components, the shared EventStore integration-test fixture, AppHost topology, or GitHub workflow definitions. Halt before broadening this into a cross-suite retry abstraction outside the Tenants integration test project.
+
+**Never:** Do not weaken the tests to accept a generic shell-only HTML response as success. Do not remove unauthorized-state assertions, do not accept `404`, do not bypass the Aspire tests, do not make the UI reveal tenant/audit data when unauthenticated, and do not modify submodule contents.
+
+## I/O & Edge-Case Matrix
+
+| Scenario | Input / State | Expected Output / Behavior | Error Handling |
+|----------|--------------|---------------------------|----------------|
+| Hosted UI route first paint is late | `GET /tenants`, `/tenants/tenant.alpha`, or `/tenants/tenant.alpha/audit` initially returns HTTP 200 without the route-specific unauthorized marker | The smoke test retries the same route for a short bounded interval, then continues once the expected marker appears | If the marker never appears, the final failure includes the last HTTP status and markup assertion context |
+| Hosted UI renders expected unauthorized route | First or retried response contains the route shell and unauthorized marker | Existing assertions verify route controls/context, unauthorized copy, alert role, support-safe return context, and absence of leaked tenant/audit/token content | Any missing existing assertion still fails the test |
+| Hosted UI returns a non-success status | Route request returns a status other than HTTP 200 during the final attempt | The test fails with the same status expectation semantics as today | Redirect-only `/tenants/users` test remains unchanged because it intentionally expects HTTP 302 |
+
+</frozen-after-approval>
+
+## Code Map
+
+- `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs` -- owns the failing hosted UI route smoke assertions and should contain the bounded readiness helper used by the three affected HTTP 200 route checks.
+- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs` -- declares `tenants-ui` with `WaitForAliveness: false`, so tests cannot assume the fixture has waited for UI route content before the first request.
+- `references/Hexalith.EventStore/src/Hexalith.EventStore.Testing.Integration/AspireTopologyFixtureBase.cs` -- reference-only evidence: resource waiting proves Running/endpoint publication and optional `/alive`, not full route/dependency readiness; do not edit.
+- `_bmad-output/implementation-artifacts/spec-gh-actions-28944933021-85877006572.md` -- prior in-review CI remediation confirming the earlier generated API route failure is a separate issue and now build-and-test passes in run `28953291798`.
+
+## Tasks & Acceptance
+
+**Execution:**
+- [ ] `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs` -- add a private bounded helper that repeatedly requests a hosted UI route until HTTP 200 markup contains a caller-supplied readiness marker, then returns the final markup -- absorbs CI startup/first-paint timing without changing production behavior.
+- [ ] `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs` -- use the helper for `/tenants`, `/tenants/{id}`, and `/tenants/{id}/audit`, selecting the unauthorized marker as the readiness marker for each route -- keeps every existing content and support-safety assertion intact after readiness.
+- [ ] `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs` -- leave redirect and already-stable hosted UI smoke tests unchanged unless their current response contract requires the same HTTP 200 route-content readiness semantics -- avoids unnecessary churn.
+
+**Acceptance Criteria:**
+- Given the Aspire fixture has only created a `tenants-ui` HTTP client and not waited for UI route readiness, when a protected hosted UI route initially returns shell-only or early first-paint HTML, then the smoke test retries until the route-specific unauthorized marker appears or a bounded timeout expires.
+- Given `/tenants`, `/tenants/tenant.alpha`, and `/tenants/tenant.alpha/audit` render the expected unauthenticated fail-closed state, when the smoke tests complete, then they still assert the same route markers, unauthorized copy, alert roles, scoped context, safe links, and no tenant/audit/token leakage.
+- Given a route never renders the expected unauthorized marker, when the bounded helper exhausts its attempts, then the test fails instead of treating shell-only HTML as success.
+
+## Spec Change Log
+
+## Verification
+
+**Commands:**
+- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore` -- expected: Release build succeeds with zero warnings/errors.
+- `DiffEngine_Disabled=true dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-build --filter "FullyQualifiedName~TenantsUiRouteSmokeTests"` -- expected: six hosted UI smoke tests pass.
+- `DiffEngine_Disabled=true dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Performance"` -- expected: full non-performance Aspire suite passes.
diff --git a/tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs b/tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs
index af078fb..e618347 100644
--- a/tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs
+++ b/tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs
@@ -19,6 +19,11 @@ namespace Hexalith.Tenants.IntegrationTests;
 [DaprTestSerialization]
 [Trait("Category", "Integration")]
 public sealed class TenantsUiRouteSmokeTests : IDisposable {
+    private const string TenantsListUnauthorizedMarker = "data-testid=\"tenants-list-unauthorized\"";
+    private const string TenantsDetailUnauthorizedMarker = "data-testid=\"tenants-detail-unauthorized\"";
+    private const string TenantsAuditUnauthorizedMarker = "data-testid=\"tenants-audit-unauthorized\"";
+    private static readonly TimeSpan UiRouteReadinessTimeout = TimeSpan.FromSeconds(10);
+    private static readonly TimeSpan UiRouteReadinessDelay = TimeSpan.FromMilliseconds(250);
     private readonly IDisposable _daprTestLease;
     private readonly AspireTopologyFixture _fixture;
 
@@ -36,17 +41,13 @@ public sealed class TenantsUiRouteSmokeTests : IDisposable {
     public async Task Tenants_workspace_route_renders_unauthorized_state_in_hosted_ui() {
         _fixture.SkipIfUnavailable();
 
-        using HttpResponseMessage response = await _fixture.TenantsUiClient
-            .GetAsync("/tenants")
+        string markup = await GetHostedUiMarkupWhenReadyAsync("/tenants", TenantsListUnauthorizedMarker)
             .ConfigureAwait(false);
 
-        response.StatusCode.ShouldBe(HttpStatusCode.OK);
-
-        string markup = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
         markup.ShouldContain("data-testid=\"tenants-workspace\"");
         markup.ShouldContain("data-testid=\"tenants-list-search\"");
         markup.ShouldContain("data-testid=\"tenants-list-refresh\"");
-        markup.ShouldContain("data-testid=\"tenants-list-unauthorized\"");
+        markup.ShouldContain(TenantsListUnauthorizedMarker);
         markup.ShouldContain("Sign in required");
         markup.ShouldNotContain("sample tenant", Case.Insensitive);
         markup.ShouldNotContain("tenant-1", Case.Insensitive);
@@ -56,17 +57,15 @@ public sealed class TenantsUiRouteSmokeTests : IDisposable {
     public async Task Tenant_detail_route_renders_unauthorized_state_in_hosted_ui() {
         _fixture.SkipIfUnavailable();
 
-        using HttpResponseMessage response = await _fixture.TenantsUiClient
-            .GetAsync("/tenants/tenant.alpha?returnUrl=%2Ftenants%3Fsearch%3Dalpha")
+        string markup = await GetHostedUiMarkupWhenReadyAsync(
+                "/tenants/tenant.alpha?returnUrl=%2Ftenants%3Fsearch%3Dalpha",
+                TenantsDetailUnauthorizedMarker)
             .ConfigureAwait(false);
 
-        response.StatusCode.ShouldBe(HttpStatusCode.OK);
-
-        string markup = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
         markup.ShouldContain("data-testid=\"tenants-detail\"");
         markup.ShouldContain("data-testid=\"tenants-detail-back\"");
         markup.ShouldContain("href=\"/tenants?search=alpha\"");
-        markup.ShouldContain("data-testid=\"tenants-detail-unauthorized\"");
+        markup.ShouldContain(TenantsDetailUnauthorizedMarker);
         markup.ShouldContain("role=\"alert\"");
         markup.ShouldContain("Tenant detail unauthorized");
         markup.ShouldNotContain("data-testid=\"tenants-detail-identity\"");
@@ -77,13 +76,11 @@ public sealed class TenantsUiRouteSmokeTests : IDisposable {
     public async Task Tenant_audit_route_renders_scoped_context_and_unauthorized_state_in_hosted_ui() {
         _fixture.SkipIfUnavailable();
 
-        using HttpResponseMessage response = await _fixture.TenantsUiClient
-            .GetAsync("/tenants/tenant.alpha/audit?targetUserId=operator.support-01&source=member-row&returnUrl=%2Ftenants%3Fsearch%3Dalpha%26selected%3Dtenant.alpha&returnFocus=tenants-member-operator.support-01")
+        string markup = await GetHostedUiMarkupWhenReadyAsync(
+                "/tenants/tenant.alpha/audit?targetUserId=operator.support-01&source=member-row&returnUrl=%2Ftenants%3Fsearch%3Dalpha%26selected%3Dtenant.alpha&returnFocus=tenants-member-operator.support-01",
+                TenantsAuditUnauthorizedMarker)
             .ConfigureAwait(false);
 
-        response.StatusCode.ShouldBe(HttpStatusCode.OK);
-
-        string markup = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
         markup.ShouldContain("data-testid=\"tenants-audit-surface\"");
         markup.ShouldContain("data-testid=\"tenants-audit-context\"");
         markup.ShouldContain("operator.support-01");
@@ -92,7 +89,7 @@ public sealed class TenantsUiRouteSmokeTests : IDisposable {
         markup.ShouldContain("data-testid=\"tenants-audit-back\"");
         markup.ShouldContain("href=\"/tenants?search=alpha");
         markup.ShouldContain("selected=tenant.alpha");
-        markup.ShouldContain("data-testid=\"tenants-audit-unauthorized\"");
+        markup.ShouldContain(TenantsAuditUnauthorizedMarker);
         markup.ShouldContain("role=\"alert\"");
         markup.ShouldContain("You are not authorized to view tenant audit entries");
         markup.ShouldNotContain("data-testid=\"tenants-audit-row\"");
@@ -164,4 +161,39 @@ public sealed class TenantsUiRouteSmokeTests : IDisposable {
         markup.ShouldNotContain("/api/users", Case.Insensitive);
         markup.ShouldNotContain("access_token", Case.Insensitive);
     }
+
+    private async Task<string> GetHostedUiMarkupWhenReadyAsync(string requestUri, string readinessMarker) {
+        ArgumentException.ThrowIfNullOrWhiteSpace(requestUri);
+        ArgumentException.ThrowIfNullOrWhiteSpace(readinessMarker);
+
+        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(UiRouteReadinessTimeout);
+        HttpStatusCode? lastStatusCode = null;
+        string lastMarkup = string.Empty;
+
+        while (true) {
+            using HttpResponseMessage response = await _fixture.TenantsUiClient
+                .GetAsync(requestUri)
+                .ConfigureAwait(false);
+
+            lastStatusCode = response.StatusCode;
+            lastMarkup = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
+
+            if (response.StatusCode == HttpStatusCode.OK
+                && lastMarkup.Contains(readinessMarker, StringComparison.OrdinalIgnoreCase)) {
+                return lastMarkup;
+            }
+
+            if (DateTimeOffset.UtcNow >= deadline) {
+                break;
+            }
+
+            await Task.Delay(UiRouteReadinessDelay).ConfigureAwait(false);
+        }
+
+        lastStatusCode.ShouldBe(
+            HttpStatusCode.OK,
+            $"Hosted UI route '{requestUri}' did not return HTTP 200 before the readiness timeout.");
+        lastMarkup.ShouldContain(readinessMarker, Case.Insensitive);
+        return lastMarkup;
+    }
 }
diff --git a/_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md b/_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md
index 2a1050a..5c827c8 100644
--- a/_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md
+++ b/_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md
@@ -2,7 +2,7 @@
 title: 'Fix GitHub Actions hosted UI route smoke flake from run 28953291798 job 85906522208'
 type: 'bugfix'
 created: '2026-07-08'
-status: 'in-progress'
+status: 'in-review'
 baseline_commit: '3d96d0aa89bf579d66d879f7d3d949a2d87d9b71'
 context:
   - references/Hexalith.AI.Tools/hexalith-ux-instructions.md
@@ -44,9 +44,9 @@ context:
 ## Tasks & Acceptance
 
 **Execution:**
-- [ ] `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs` -- add a private bounded helper that repeatedly requests a hosted UI route until HTTP 200 markup contains a caller-supplied readiness marker, then returns the final markup -- absorbs CI startup/first-paint timing without changing production behavior.
-- [ ] `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs` -- use the helper for `/tenants`, `/tenants/{id}`, and `/tenants/{id}/audit`, selecting the unauthorized marker as the readiness marker for each route -- keeps every existing content and support-safety assertion intact after readiness.
-- [ ] `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs` -- leave redirect and already-stable hosted UI smoke tests unchanged unless their current response contract requires the same HTTP 200 route-content readiness semantics -- avoids unnecessary churn.
+- [x] `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs` -- add a private bounded helper that repeatedly requests a hosted UI route until HTTP 200 markup contains a caller-supplied readiness marker, then returns the final markup -- absorbs CI startup/first-paint timing without changing production behavior.
+- [x] `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs` -- use the helper for `/tenants`, `/tenants/{id}`, and `/tenants/{id}/audit`, selecting the unauthorized marker as the readiness marker for each route -- keeps every existing content and support-safety assertion intact after readiness.
+- [x] `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs` -- leave redirect and already-stable hosted UI smoke tests unchanged unless their current response contract requires the same HTTP 200 route-content readiness semantics -- avoids unnecessary churn.
 
 **Acceptance Criteria:**
 - Given the Aspire fixture has only created a `tenants-ui` HTTP client and not waited for UI route readiness, when a protected hosted UI route initially returns shell-only or early first-paint HTML, then the smoke test retries until the route-specific unauthorized marker appears or a bounded timeout expires.
```
