[Back to README](../README.md)

# Quickstart

Clone the repository, run the application with .NET Aspire, send your first tenant management command through the EventStore command gateway, and inspect the command outcome. This guide follows the same developer experience pattern as the [EventStore quickstart](../references/Hexalith.EventStore/docs/getting-started/quickstart.md).

## Prerequisites

Before you begin, verify that the following tools are installed and working. Run each check command and confirm the expected output.

### .NET 10 SDK

```bash
dotnet --version
```

Expected: `10.0.402` or a later `10.0.xxx` patch version. The repository pins SDK `10.0.402` in [`global.json`](../global.json) with `rollForward: latestPatch`.

If not installed, download from [https://dot.net](https://dot.net/download).

### DAPR CLI and Runtime

```bash
dapr --version
```

Expected: CLI version and runtime version both present.

```bash
dapr init
```

> **Note:** Run `dapr init` (full init, not `--slim`) for this local quickstart. Full init provides Redis, actor placement, and scheduler. `dapr init --slim` excludes those local services; use slim mode only when you provide placement, scheduler, `statestore`, and `pubsub` separately. Deployment-specific DAPR details live in [`deploy/dapr`](../deploy/dapr/README.md).

The existing local tests expect Redis on `localhost:6379`, placement on `50005` on Linux or `6050` on Windows, and scheduler on `50006` on Linux or `6060` on Windows.

If not installed, follow the [DAPR Getting Started guide](https://docs.dapr.io/getting-started/).

### Docker

```bash
docker info
```

Expected: Docker daemon information (Engine version, etc.).

Docker Desktop must be running. The Aspire AppHost launches containers for Redis, Keycloak, and DAPR sidecars.

> **Tip:** Allocate at least 4 GB of memory to Docker Desktop. The full topology (EventStore command gateway + Tenants + Sample + DAPR sidecars + Redis + Keycloak) can exceed lower memory limits.

If not installed, download Docker Desktop from [https://docs.docker.com/get-started/get-docker/](https://docs.docker.com/get-started/get-docker/).

### Root-Declared Submodules

Only initialize the root-declared submodules under `references/` used by this repository:

```bash
git submodule update --init references/Hexalith.EventStore references/Hexalith.Commons references/Hexalith.AI.Tools references/Hexalith.Builds references/Hexalith.FrontComposer references/Hexalith.PolymorphicSerializations references/Hexalith.Memories references/Hexalith.McpCli references/Hexalith.Platform
git submodule status references/Hexalith.EventStore references/Hexalith.Commons references/Hexalith.AI.Tools references/Hexalith.Builds references/Hexalith.FrontComposer references/Hexalith.PolymorphicSerializations references/Hexalith.Memories references/Hexalith.McpCli references/Hexalith.Platform
```

Expected: each line starts with a commit hash or a leading space. A leading `-` means the submodule is not initialized; rerun the command above.

> **Do not use recursive initialization.** Do not run `git submodule update --init --recursive`; nested submodules are intentionally left alone for this repository.

### About the `system` Tenant

Hexalith.Tenants operates as a platform-level service within EventStore's multi-tenant model. All tenant management commands run under the `system` tenant context — this is a platform tenant that manages other tenants, not a user-facing tenant.

For local development, the Aspire AppHost topology handles the `system` tenant configuration automatically. By default it starts a local Keycloak realm. AppHost generates the service administrator credentials for each run unless you supply the secret parameters below, and wires them into the realm and services. Do not extract generated credentials from resource environments or logs. You do not need to manually deploy EventStore or configure JWT tenant claims.

## Clone and Build

Clone the repository, then initialize the root-declared submodules:

```bash
git clone https://github.com/Hexalith/Hexalith.Tenants.git
cd Hexalith.Tenants
git submodule update --init references/Hexalith.EventStore references/Hexalith.Commons references/Hexalith.AI.Tools references/Hexalith.Builds references/Hexalith.FrontComposer references/Hexalith.PolymorphicSerializations references/Hexalith.Memories references/Hexalith.McpCli references/Hexalith.Platform
```

Do not add `--recursive`.

> **Windows users:** The repository contains submodule paths such as `Hexalith.Tenants/references/Hexalith.EventStore/src/...`. If the build fails with path-too-long errors, run `git config --system core.longpaths true` and re-clone.

Verify the build:

```bash
dotnet build Hexalith.Tenants.slnx --configuration Release
```

## Run the Application

For the manual administrator handoff, choose credentials before starting a **fresh, non-persistent local realm**. These existing shared parameters override generation; keep the values in your local shell, never in tracked settings or transcripts. In the terminal that will launch AppHost:

```bash
set +x
read -r -s -p "Choose local service administrator username: " Parameters__security_eventstore_client_username
printf '\n'
read -r -s -p "Choose local service administrator password: " Parameters__security_eventstore_client_password
printf '\n'
```

Bash cannot export variable names containing hyphens. Pass the exact Aspire parameter keys through `env` when starting AppHost (the values are the ones you entered):

```bash
env "Parameters__security-eventstore-client-username=$Parameters__security_eventstore_client_username" \
    "Parameters__security-eventstore-client-password=$Parameters__security_eventstore_client_password" \
    dotnet run --project src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
```

Keep this terminal running. Use a second terminal for the remaining steps and enter the same chosen credentials when prompted. The parameter values are secrets; do not record the expanded invocation or enable shell tracing.

With `KeycloakPersistent=true`, realm import does **not** update an existing realm/user. Changing these parameters will not rotate its stored credentials. Authenticate the existing configured subject with its current credentials, or reconcile the same subject through your IdP administration process, then verify membership below. Do not delete Keycloak/Redis state or change the fixed subject to force this walkthrough. If the configured account cannot authenticate, stop and repair that identity first.

The single AppHost launch above starts the EventStore command gateway, Tenants, tenants-api, tenants-ui, Keycloak, Redis, DAPR sidecars and the sample consuming service.

> **Note:** The first run takes longer than usual because .NET restores NuGet packages and Docker pulls container images for Redis, Keycloak, and the DAPR sidecars.

Once the application starts, the terminal output includes the Aspire dashboard URL. Open it in your browser — the dashboard shows all running services and their endpoints.

Before sending a command, confirm the dashboard shows these local resources as running or healthy:

- `eventstore`: EventStore command gateway, including `POST /api/v1/commands`
- `tenants`: Tenants domain processor for `/process` and query endpoints
- `tenants-ui`: Blazor InteractiveServer Tenants workspace composed through FrontComposer
- `security`: local Keycloak-backed identity provider for the token-based steps below
- `redis`: local state store backing DAPR actor and projection state
- `sample`: consuming service subscribed to tenant events
- DAPR sidecars for `eventstore`, `tenants`, and `sample`

If `eventstore` or `tenants` is missing or unhealthy, do not submit the first command yet. Check the AppHost resource details first; common causes are Docker not running, DAPR not initialized, port conflicts from old sidecars, or uninitialized submodules.

### UI authorization and notification deadline

The UI uses `Tenants:Ui:AuthorizationAndSubscriptionTimeout` for authorization
resolution and optional projection subscription setup/cleanup. It defaults to
`00:00:10`; override it in host configuration or with
`Tenants__Ui__AuthorizationAndSubscriptionTimeout`. Supply a positive finite
TimeSpan within .NET timer limits. Invalid values are rejected.

Authorization expiry leaves authority indeterminate and recovery available.
Subscription expiry consumes one of three failed setup attempts; an explicit
refresh reopens that budget. Reads continue independently. If a provider ignores
cancellation, the coordinator retains that exact scope until late setup and cleanup
finish, preventing old cleanup from removing a replacement subscription. Other
scopes remain usable. Route/caller cancellation is separate from ordinary load
cancellation, so paging and refresh preserve established subscriptions.

### UI membership projection wait

The add-member, change-role and removal flows use
`Tenants:Ui:MembershipProjectionTimeout`, default `00:05:00`. Override it in host
configuration or with `Tenants__Ui__MembershipProjectionTimeout`. Supply a positive
finite invariant TimeSpan no greater than `49.17:02:47.294` (the .NET timer limit).
Invalid values are rejected. The circuit-scoped tracker reads this setting when
it is created; changing configuration does not change an existing tracker's bound.

Each retained membership attempt starts one monotonic deadline when it first
reaches `ProjectionPending`. Elapsed time includes held status/projection operations
and detached renderers. Refreshes, concurrent or coalesced requests, notifications,
status recovery, Continue read-only, replacement and remount never restart it.
Accepted-only and ambiguous delivery attempts have separate lifecycle behavior.

Expiry presents `UnableToVerify` with status recovery and support-safe guidance.
It does not cancel the command, prove rejection or failure, or release its aggregate
admission. Continue read-only and surface disposal detach the renderer while
preserving the unresolved dispatched attempt and admission. Recovery queries the
same attempt; only qualifying authoritative projection evidence can confirm it,
including evidence arriving after expiry. Terminal evidence stops the timer and
releases its admission. Circuit/tracker disposal stops timers and observers without
inventing an outcome or releasing unresolved admission.

## Get an Access Token

The default AppHost bootstraps its generated service administrator at startup, using the fixed subject `11111111-1111-1111-1111-111111111111`. Running resources do not prove bootstrap succeeded. First authenticate that configured subject using the operator-selected credentials above, then verify authoritative projected membership. Never invent a token or submit bootstrap as a probe.

The following Bash examples require `curl`, `jq` and `python3`. Find `security`, `eventstore`, and **`tenants-api`** URLs in the Aspire dashboard. Commands/status belong to EventStore; generated REST reads belong to tenants-api. First define **all shared helpers** in the second terminal, before authentication or membership checks. Every actual token/API request has a two-second connection limit and five-second request limit. Each status/projection polling phase has a 60-second elapsed budget, shared across all pages, and each request uses at most its remaining budget. A stalled response returns unresolved; page/attempt limits alone are insufficient.

```bash
set +x
CONNECT_TIMEOUT=2
REQUEST_TIMEOUT=5
POLL_SECONDS=60

# Keep bearer values out of curl's process arguments and output.
platform_api() {
  local bearer="$1"
  shift
  printf 'header = "Authorization: Bearer %s"\n' "$bearer" |
    curl --connect-timeout "$CONNECT_TIMEOUT" --max-time "$REQUEST_TIMEOUT" --config - "$@"
}
request_token() (
  set -o pipefail
  curl --connect-timeout "$CONNECT_TIMEOUT" --max-time "$REQUEST_TIMEOUT" -fsS -X POST \
    "$SECURITY_URL/realms/hexalith/protocol/openid-connect/token" \
    -d "grant_type=password" -d "client_id=hexalith-eventstore" \
    --data-urlencode "username@/dev/fd/3" --data-urlencode "password@/dev/fd/4" \
    3< <(printf '%s' "$LOCAL_PLATFORM_USERNAME") 4< <(printf '%s' "$LOCAL_PLATFORM_PASSWORD") |
    jq -er .access_token
)
# Local inspection is a consistency check; the API validates the token itself.
token_subject() {
  TOKEN_TO_INSPECT="$1" python3 - <<'PYTHON'
import base64, json, os, sys
try:
    payload = os.environ["TOKEN_TO_INSPECT"].split(".")[1]
    subject = json.loads(base64.urlsafe_b64decode(payload + "=" * (-len(payload) % 4)))["sub"]
    if not isinstance(subject, str) or not subject.strip():
        raise ValueError()
    print(subject)
except (ValueError, KeyError, IndexError, TypeError):
    sys.exit(1)
PYTHON
}
verify_configured_identity() {
  [[ -n "${ADMIN_TOKEN:-}" && -n "${ADMIN_SUBJECT:-}" ]] &&
    [[ "$(token_subject "$ADMIN_TOKEN")" == "$ADMIN_SUBJECT" ]]
}
new_ulid() {
  python3 - <<'PYTHON'
import secrets, time
alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"
value = (int(time.time() * 1000) << 80) | secrets.randbits(80)
print("".join(alphabet[(value >> (5 * i)) & 31] for i in range(25, -1, -1)))
PYTHON
}
request_budget() {
  local remaining=$(( $1 - SECONDS ))
  ((remaining > 0)) || return 1
  if ((remaining < REQUEST_TIMEOUT)); then printf '%s' "$remaining"; else printf '%s' "$REQUEST_TIMEOUT"; fi
}
projection_version() {
  python3 - "$1" <<'PYTHON'
from email.parser import Parser
from pathlib import Path
import re, sys
try:
    block = re.split(r"\r?\n\r?\n", Path(sys.argv[1]).read_text().strip())[-1]
    headers = Parser().parsestr("\n".join(block.splitlines()[1:]))
    def one(name, required=True):
        values = headers.get_all(name, [])
        if len(values) != 1:
            if not values and not required:
                return None
            raise ValueError()
        return values[0].strip()
    if one("X-Hexalith-Query-Provenance") != "ProjectionBacked":
        raise ValueError()
    lifecycle = one("X-Hexalith-Projection-Lifecycle", False)
    stale = one("X-Hexalith-Is-Stale", False)
    if lifecycle is not None and lifecycle != "Current":
        raise ValueError()
    if stale is not None and stale.casefold() != "false":
        raise ValueError()
    if lifecycle is None and stale is None:
        raise ValueError()
    degraded = one("X-Hexalith-Is-Degraded", False)
    if degraded is not None and degraded.casefold() != "false":
        raise ValueError()
    version = one("X-Hexalith-Projection-Version")
    if not version or len(version) > 4096 or any(ord(c) < 32 or ord(c) == 127 for c in version):
        raise ValueError()
    print(version)
except (ValueError, OSError):
    sys.exit(1)
PYTHON
}
has_admin_membership() {
  local bearer="$1" subject="$2" deadline="${3:-$((SECONDS + POLL_SECONDS))}"
  local page cursor="" body found=false version="" next_version headers budget
  for ((page=0; page<100; page++)); do
    budget=$(request_budget "$deadline") || return 1
    headers=$(mktemp) || return 1
    body=$(platform_api "$bearer" --max-time "$budget" -fsS --dump-header "$headers" --get \
      "$TENANTS_API_URL/api/global-administrators" --data-urlencode "pageSize=100" \
      --data-urlencode "cursor=$cursor") || { rm -f "$headers"; return 1; }
    next_version=$(projection_version "$headers") || { rm -f "$headers"; return 1; }
    rm -f "$headers"
    [[ -z "$version" || "$version" == "$next_version" ]] || return 1
    version="$next_version"
    jq -e '(.items | type) == "array" and (.hasMore | type) == "boolean"' >/dev/null <<< "$body" || return 1
    if jq -e --arg subject "$subject" '.items | any(.userId == $subject)' >/dev/null <<< "$body"; then found=true; fi
    if jq -e '.hasMore == false' >/dev/null <<< "$body"; then [[ "$found" == true ]]; return; fi
    cursor=$(jq -er '.cursor | select(type == "string" and length > 0)' <<< "$body") || return 1
  done
  return 1
}
wait_command_completed() {
  local bearer="$1" correlation="$2" deadline=$((SECONDS + POLL_SECONDS)) body state budget
  while ((SECONDS < deadline)); do
    budget=$(request_budget "$deadline") || return 1
    body=$(platform_api "$bearer" --max-time "$budget" -fsS \
      "$EVENTSTORE_URL/api/v1/commands/status/$correlation") || return 1
    state=$(jq -er '.status' <<< "$body") || return 1
    case "$state" in
      Completed) return 0 ;;
      Rejected|PublishFailed|TimedOut) return 1 ;;
      Received|Processing|EventsStored|EventsPublished) ;;
      *) return 1 ;;
    esac
    sleep 1
  done
  return 1
}
wait_admin_membership() {
  local bearer="$1" subject="$2" deadline=$((SECONDS + POLL_SECONDS))
  while ((SECONDS < deadline)); do
    has_admin_membership "$bearer" "$subject" "$deadline" && return 0
    ((SECONDS < deadline)) && sleep 1
  done
  return 1
}
resume_handoff() {
  HANDOFF_PROJECTED=false
  [[ -n "${HANDOFF_MESSAGE_ID:-}" && -n "${HANDOFF_CORRELATION_ID:-}" && -n "${HANDOFF_SUBJECT:-}" ]] || return 1
  verify_configured_identity && has_admin_membership "$ADMIN_TOKEN" "$ADMIN_SUBJECT" || return 1
  wait_command_completed "$ADMIN_TOKEN" "$HANDOFF_CORRELATION_ID" || return 1
  wait_admin_membership "$ADMIN_TOKEN" "$HANDOFF_SUBJECT" || return 1
  HANDOFF_PROJECTED=true
}
resume_bootstrap() {
  BOOTSTRAP_PROJECTED=false
  [[ -n "${BOOTSTRAP_MESSAGE_ID:-}" && -n "${BOOTSTRAP_CORRELATION_ID:-}" ]] || return 1
  verify_configured_identity || return 1
  wait_command_completed "$ADMIN_TOKEN" "$BOOTSTRAP_CORRELATION_ID" || return 1
  wait_admin_membership "$ADMIN_TOKEN" "$ADMIN_SUBJECT" || return 1
  BOOTSTRAP_PROJECTED=true
}
```

The membership helper consumes the existing REST projection metadata: `X-Hexalith-Query-Provenance=ProjectionBacked`, current lifecycle (or the existing absent-lifecycle compatibility signal `X-Hexalith-Is-Stale=false`), no explicit degradation and a non-empty projection version. The producer may omit the optional `X-Hexalith-Is-Degraded` header for ordinary current projections; absence follows the existing client contract. It rejects stale/degraded/unknown/missing/contradictory evidence and duplicate metadata headers. All pages must report the same projection version. This establishes qualifying projected membership under the existing contract; it adds no bounded revocation guarantee.

Authenticate the configured administrator without printing credentials or tokens:

```bash
read -r -p "Security base URL: " SECURITY_URL
read -r -p "EventStore base URL: " EVENTSTORE_URL
read -r -p "Tenants API base URL: " TENANTS_API_URL
ADMIN_SUBJECT=11111111-1111-1111-1111-111111111111
authenticate_configured_admin() {
  read -r -s -p "Chosen local service administrator username: " LOCAL_PLATFORM_USERNAME
  printf '\n'
  read -r -s -p "Chosen local service administrator password: " LOCAL_PLATFORM_PASSWORD
  printf '\n'
  ADMIN_TOKEN=$(request_token)
  local result=$?
  unset LOCAL_PLATFORM_PASSWORD LOCAL_PLATFORM_USERNAME
  ((result == 0)) && verify_configured_identity
}
authenticate_configured_admin
```

A nonzero result stops progress; keep the shell open. The realm import at `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json` defines the public `hexalith-eventstore` client, direct access grants and claim mappings.

### Verify configured administrator membership

```bash
if verify_configured_identity && has_admin_membership "$ADMIN_TOKEN" "$ADMIN_SUBJECT"; then
  printf 'Configured administrator membership verified.\n'
else
  printf 'Membership unresolved; keep this shell open and follow explicit bootstrap recovery.\n'
fi
```

This check preserves the authenticated shell and every recovery helper on missing membership. A `403`, absent membership, invalid metadata, timeout or unavailable projection is **not** proof bootstrap never ran. Stop before granting authority and follow [explicit recovery](production-auth-readiness.md#explicit-bootstrap-recovery). `202 Accepted`, `GlobalAdminAlreadyBootstrappedRejection`, resource health and UI badges alone do not prove this subject is an administrator.

### Grant a provisioned human administrator access

Through your local IdP administration process, provision a separate local realm user with a stable subject and obtain that exact subject from the IdP. It needs `eventstore:tenant=system`, `eventstore:domain=global-administrators` and `eventstore:domain=tenants`, plus command and query permissions (for example `command:submit` and `query:read`). For UI administrator reflection, also map `global_admin=true` or a supported administrator role. These claims do not grant persisted Tenants membership by themselves.

The mutation rechecks the configured identity and qualifying administrator membership. Any retained IDs block replacement **before** changing the subject or IDs. A successful dispatch is followed by status and projection checks through `resume_handoff`:

```bash
grant_human_access() {
  [[ -z "${HANDOFF_MESSAGE_ID:-}" && -z "${HANDOFF_CORRELATION_ID:-}" ]] || return 1
  verify_configured_identity && has_admin_membership "$ADMIN_TOKEN" "$ADMIN_SUBJECT" || return 1
  HANDOFF_PROJECTED=false
  read -r -p "Provisioned human's exact IdP subject: " HUMAN_SUBJECT
  [[ -n "$HUMAN_SUBJECT" ]] || return 1
  HANDOFF_SUBJECT="$HUMAN_SUBJECT"
  HANDOFF_MESSAGE_ID=$(new_ulid)
  HANDOFF_CORRELATION_ID=$(new_ulid)
  HANDOFF_BODY=$(jq -nc --arg messageId "$HANDOFF_MESSAGE_ID" --arg correlationId "$HANDOFF_CORRELATION_ID" \
    --arg subject "$HANDOFF_SUBJECT" \
    '{messageId:$messageId,correlationId:$correlationId,tenant:"system",domain:"global-administrators",aggregateId:"global-administrators",commandType:"SetGlobalAdministrator",payload:{UserId:$subject}}')
  HANDOFF_HTTP=$(platform_api "$ADMIN_TOKEN" -sS -o /dev/null -w '%{http_code}' \
    -H 'Content-Type: application/json' --data-binary @/dev/fd/3 \
    "$EVENTSTORE_URL/api/v1/commands" 3<<< "$HANDOFF_BODY") || return 1
  [[ "$HANDOFF_HTTP" == 202 ]] || return 1
  resume_handoff
}
grant_human_access
```

A nonzero result stops the handoff and preserves its IDs/target in this shell. Recover status and projection with those **same** retained handles, without another submission:

```bash
resume_handoff
```

`PublishFailed` means events were persisted but publication failed. It stops unresolved with retained IDs; persisted events forbid blind replacement. Use the existing EventStore operator publication-recovery policy, then resume status/projection verification. `Rejected`, `TimedOut`, unknown status, HTTP failure or timeout also stop unresolved. An already-existing-human rejection requires deliberate membership investigation rather than a new grant. These instructions do not redefine platform publication recovery or allow automatically clearing/replacing retained handles.

Only a successful handoff permits the following executable human-authentication block. Its first gate runs before any prompt or request:

```bash
authenticate_human() {
  [[ "${HANDOFF_PROJECTED:-false}" == true && -n "${HANDOFF_SUBJECT:-}" ]] || return 1
  read -r -p "Local platform username: " LOCAL_PLATFORM_USERNAME
  read -r -s -p "Local platform password: " LOCAL_PLATFORM_PASSWORD
  printf '\n'
  TOKEN=$(request_token)
  local result=$?
  unset LOCAL_PLATFORM_PASSWORD LOCAL_PLATFORM_USERNAME
  ((result == 0)) || return 1
  [[ "$(token_subject "$TOKEN")" == "$HANDOFF_SUBJECT" ]] || return 1
  has_admin_membership "$TOKEN" "$HANDOFF_SUBJECT"
}
authenticate_human
```

Use `TOKEN` for the remaining API walkthrough only after this function succeeds, or sign into tenants-ui as that human. Keep tokens in this local shell and clear them when finished. The UI recognizes exact `system` membership among valid multivalued tenant claims such as the shipped administrator's `system`, `tenant-a`, `tenant-b`; malformed values or ambiguous identities fail closed.

`EnableKeycloak=false` is outside this token walkthrough. The tracked `references/Hexalith.EventStore/src/Hexalith.EventStore/appsettings.Development.json` does not provide a static signing key or audience for a handwritten HMAC token, so disabling Keycloak does not make the old development-token command valid. For production IdP mappings, see [Production Auth Claim Contract](production-auth-claim-contract.md).

> **Production note:** Production deployments use OIDC authority-based JWT validation. Before release, run the [Production Auth Readiness](production-auth-readiness.md) checklist and smoke tests.

## Validate Before the First Command

Do these checks before you submit your first tenant command. They catch the common local setup failures while the system is still idle.

### Verify the EventStore command gateway

Find the `eventstore` service URL in the Aspire dashboard, then confirm the OpenAPI document exposes the command gateway route:

```bash
curl --connect-timeout 2 --max-time 5 -fsS "{eventstore-url}/swagger/v1/swagger.json" | rg '"/api/v1/commands"'
```

Expected: a match for `/api/v1/commands`. If the request fails, the EventStore resource is not reachable yet; check AppHost health, Docker, DAPR initialization, and port conflicts before continuing. Do not switch to a Tenants-specific command URL; tenant commands are submitted through the EventStore-owned `POST /api/v1/commands` route.

### Verify the token and `system` tenant claim

Use the token from the previous step to call the status endpoint with a correlation ID that has not been used:

```bash
curl --connect-timeout 2 --max-time 5 -i -H "Authorization: Bearer $TOKEN" \
  "{eventstore-url}/api/v1/commands/status/01JQK000000000000000009999"
```

Expected: `404 Not Found` with a problem-details body saying no command status exists for that correlation ID. That proves the gateway accepted the token and searched the authorized tenant scope.

Fix these outcomes before submitting a tenant command:

| Result | Meaning | Fix |
| ------ | ------- | --- |
| `401 Unauthorized` | Missing, expired, malformed, wrong issuer, wrong audience, or wrong signing key token | Re-acquire the local Keycloak token and confirm the issuer, audience, and signing algorithm match the AppHost OIDC configuration. |
| `403 Forbidden` with no tenant authorization claims | Token lacks an effective `eventstore:tenant=system` claim | Use your provisioned local platform account with an effective `eventstore:tenant=system` claim. |
| `403 Forbidden` with tenant mismatch | Token tenant is not exactly `system` for the platform command path | Use the local platform administrator token; do not submit tenant-management commands under `tenant-a`, `tenant-b`, or differently cased `System`. |
| Connection failure | EventStore is not reachable | Wait for the AppHost resource, then check Docker, DAPR full init, and AppHost resource details. |

## Send Your First Commands

### Open Swagger UI

Find the `eventstore` service in the Aspire dashboard and open its URL. Append `/swagger` to the URL to open the Swagger UI.

1. Click the **Authorize** button at the top of the page
2. In the **Value** field, paste the token you generated — do not include the `Bearer` prefix, Swagger adds it automatically
3. Click **Authorize**, then **Close**

### Step 1: Confirm administrator handoff

The membership checks above replace the former bootstrap-write probe. Confirm they succeeded for both the configured administrator and the provisioned human. Do not submit `BootstrapGlobalAdmin` to test access; it is a one-time state-changing command, and its target must equal the authenticated actor.

### Step 2: Create Your First Tenant

Now create a tenant. In the same **POST /api/v1/commands** endpoint, submit:

```json
{
    "messageId": "01JQK7YQ7YQ7YQ7YQ7YQ7YQ7Y2",
    "tenant": "system",
    "domain": "tenants",
    "aggregateId": "my-first-tenant",
    "commandType": "CreateTenant",
    "payload": {
        "TenantId": "my-first-tenant",
        "Name": "My First Tenant",
        "Description": "Created via quickstart guide"
    }
}
```

> **Important:** `aggregateId` and `payload.TenantId` must match — the aggregate ID is the managed tenant ID per the identity scheme (`system:tenants:{aggregateId}`). If they don't match, the command will be rejected with a validation error.

Click **Execute**. The API returns `202 Accepted`. The response body contains a correlation ID:

```json
{ "correlationId": "01JQK7YQ7YQ7YQ7YQ7YQ7YQ7Y2" }
```

The `Location` header points to the status polling endpoint (`/api/v1/commands/status/{correlationId}`). You can poll this endpoint until you see a terminal status:

```json
{
    "correlationId": "01JQK7YQ7YQ7YQ7YQ7YQ7YQ7Y2",
    "status": "Completed",
    "statusCode": 4,
    "timestamp": "2026-03-19T12:00:01Z",
    "aggregateId": "my-first-tenant",
    "eventCount": 1,
    "rejectionEventType": null,
    "failureReason": null
}
```

`status: "Completed"` with `eventCount: 1` confirms the `TenantCreated` event was stored and published. A `status: "Rejected"` response means a business rule rejected the command; check `rejectionEventType` for the reason and use the corrective actions below. Infrastructure failures use `failureReason` instead.

### Verify the Event

Verify the tenant was created by querying the read model. Expand the **GET /api/tenants/{tenantId}** endpoint, enter `my-first-tenant` as the tenant ID, and execute.

The response should contain the tenant details including the name and description you provided.

> **Note:** If the query returns 404, retry after the projection catches up. Projections are eventually consistent — the read model processes events asynchronously. If 404 persists across retries, check the Aspire dashboard for service errors and verify the command reached `status: "Completed"` via the status endpoint.

You can also check the command status via the URL in the `Location` header from the previous response, or query it directly: `GET /api/v1/commands/status/{correlationId}`.

### Running the Quickstart Again

If you've run this before:

- Reauthenticate the configured administrator and verify projected membership before repeating the human handoff. `GlobalAdminAlreadyBootstrappedRejection` alone does not establish membership for the configured subject.
- **CreateTenant** with the same ID may reach `status: "Rejected"` with `rejectionEventType` ending in `TenantAlreadyExistsRejection`. Use a different `aggregateId` and matching `payload.TenantId`, e.g., `my-second-tenant`.

### Try More Commands

Create a multi-step workflow — add a user to your new tenant:

**1. Add a user to the tenant:**

```json
{
    "messageId": "01JQK7YQ7YQ7YQ7YQ7YQ7YQ7Y3",
    "tenant": "system",
    "domain": "tenants",
    "aggregateId": "my-first-tenant",
    "commandType": "AddUserToTenant",
    "payload": {
        "TenantId": "my-first-tenant",
        "UserId": "jane-doe",
        "Role": "TenantContributor"
    }
}
```

> **Roles:** Use the enum names `TenantOwner`, `TenantContributor`, or `TenantReader`. `Unknown` is the fail-closed sentinel and is rejected by the aggregate.

**2. Verify the user was added:**

Expand `GET /api/tenants/{tenantId}/users`, enter `my-first-tenant`, and execute. The response lists users and their roles in the tenant.

## Next Steps

> **Note:** Everything below is optional follow-up — you've already completed the core quickstart by creating your first tenant.

### Consume Tenant Events in Your Service

Install the NuGet packages for event-driven integration:

```bash
dotnet add package Hexalith.Tenants.Contracts
dotnet add package Hexalith.Tenants.Client
```

The sample consuming service shows the complete pattern: package references,
DI registration, typed tenant event handlers, DAPR subscription mapping, local
projection updates, access checks, configuration reads, and production adaptation
boundaries.

Use the [Sample Consuming Service Walkthrough](sample-consuming-service-walkthrough.md)
as the source-backed guide for copying the pattern from
[`samples/Hexalith.Tenants.Sample/`](../samples/Hexalith.Tenants.Sample/).

For event envelope fields, delivery semantics, and ordering limits, see [Event Contract Reference](event-contract-reference.md). For event handling patterns and idempotent processing, see [Idempotent Event Processing](idempotent-event-processing.md).

### Test Tenant Isolation Without Infrastructure

Install the Testing package in your consuming service test project when you need fast tenant setup and event replay without DAPR, Docker, Aspire, HTTP, or a live EventStore:

```bash
dotnet add package Hexalith.Tenants.Testing
```

Use `TenantIsolationTestHelpers` to create independent tenant contexts, replay only the events for the tenant under test, simulate duplicate delivery, and assert local grant/revoke behavior in your own projection:

```csharp
InMemoryTenantService tenants = TenantIsolationTestHelpers.CreateServiceWithTenants(
    new Dictionary<string, IReadOnlyDictionary<string, TenantRole>> {
        ["tenant-a"] = new Dictionary<string, TenantRole> {
            ["shared-user"] = TenantRole.TenantOwner,
            ["reader"] = TenantRole.TenantReader,
        },
        ["tenant-b"] = new Dictionary<string, TenantRole> {
            ["shared-user"] = TenantRole.TenantReader,
        },
    });

IReadOnlyList<IEventPayload> tenantAEvents = TenantIsolationTestHelpers.GetTenantEvents(tenants, "tenant-a");
IReadOnlyList<IEventPayload> duplicateTenantAEvents = TenantIsolationTestHelpers.DuplicateDelivery(tenantAEvents);

consumerProjection.ApplyEvents(duplicateTenantAEvents);
consumerProjection.IsAuthorized("tenant-a", "shared-user", TenantRole.TenantOwner).ShouldBeTrue();
consumerProjection.IsAuthorized("tenant-b", "shared-user", TenantRole.TenantOwner).ShouldBeFalse();

_ = TenantIsolationTestHelpers.RemoveUser(tenants, "tenant-a", "reader");
consumerProjection.ApplyEvents(TenantIsolationTestHelpers.GetTenantEvents(tenants, "tenant-a"));
consumerProjection.IsAuthorized("tenant-a", "reader", TenantRole.TenantReader).ShouldBeFalse();
```

`Hexalith.Tenants.Testing` provides aggregate-level fake parity: command validation, successful event production, and state transitions execute through the same aggregate logic used by the service. Consuming services are still responsible for testing their own projection-level and query-level isolation, including deduplication behavior and tenant-scoped reads. Keep idempotency assertions aligned with the guidance in [Idempotent Event Processing](idempotent-event-processing.md) rather than duplicating those patterns in each test.

### Run Local Test Projects

Run test projects individually through the repository-selected Microsoft.Testing.Platform path:

```bash
dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release
```

## Troubleshooting

### AppHost Startup Failures

**Port conflict — DAPR sidecar port 3500 already in use**

Stop other DAPR instances and retry:

```bash
dapr stop --all
```

Or change the port in the AppHost configuration.

**Docker resource limits**

The topology (EventStore command gateway + Tenants + Sample + DAPR sidecars + Redis + Keycloak) can exceed default Docker Desktop memory allocation. Increase Docker memory to 4 GB or more in Docker Desktop Settings > Resources.

**DAPR not initialized**

If you see DAPR-related errors, ensure you've run the full initialization:

```bash
dapr init
```

Use `dapr init` (not `--slim`) — the Aspire topology requires Redis, placement, and scheduler. Slim self-hosted mode (`dapr init --slim`) is for operators who provide placement, scheduler, and the `statestore`/`pubsub` components themselves before actor flows start.

Expected local ports used by existing tests:

| Dependency | Linux | Windows |
| ---------- | ----- | ------- |
| Redis | `localhost:6379` | `localhost:6379` |
| Placement | `50005` | `6050` |
| Scheduler | `50006` | `6060` |

### DAPR Configuration Triage

| Symptom | Likely issue | Action |
| ------- | ------------ | ------ |
| Actor startup fails before command processing | missing placement | Confirm full `dapr init` ran or provide placement for slim mode |
| Actor reminder/scheduler errors appear | missing scheduler | Confirm scheduler is reachable on the expected port |
| State calls report a missing component | missing state store or wrong component name | Confirm the component is named `statestore` and scoped to the calling AppId |
| Event publishing/subscription fails | missing pub/sub or wrong component name | Confirm the component is named `pubsub` and scoped to `eventstore` and subscriber AppIds |
| Tenants `/process` or `/project` invocation fails | wrong AppId or denied service invocation | Confirm EventStore uses AppId `eventstore`, Tenants uses AppId `tenants`, and the receiver access-control template allows the route |
| Component exists but a sidecar cannot use it | wrong component scope | Add only the required AppId to the component `scopes` list |
| Sidecar logs show access-control denial | denied service invocation | Inspect the called sidecar's DAPR `Configuration`, not the caller's |

Production DAPR templates and additional triage guidance live in [`deploy/dapr`](../deploy/dapr/README.md).

**Build fails on Windows with path-too-long**

Enable long paths and re-clone:

```bash
git config --system core.longpaths true
```

### Common Errors

| Error                            | Meaning                      | Action                                       |
| -------------------------------- | ---------------------------- | -------------------------------------------- |
| `GlobalAdminAlreadyBootstrappedRejection` | Bootstrap already ran        | Safe to proceed — the admin exists           |
| `TenantAlreadyExistsRejection`            | Tenant ID already used       | Use a different `aggregateId` and `TenantId` |
| `401 Unauthorized`               | JWT token expired or invalid | Re-generate the token using the script above |
| `403 Forbidden`                  | Token lacks the effective `eventstore:tenant=system` authorization | Confirm the token has `tenants: ["system"]` locally or a production mapping that normalizes to `eventstore:tenant=system` |
