# Edge Case Hunter Review Prompt

Use the `bmad-review-edge-case-hunter` skill.

You may inspect the repository. Review the diff below for unhandled branches, race conditions, timing holes, retry edge cases, false positives, false negatives, and test isolation problems. Return only actionable findings with severity and file/line references.

```diff
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
```

Relevant paths to inspect:

- `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs`
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Testing.Integration/AspireTopologyFixtureBase.cs`
