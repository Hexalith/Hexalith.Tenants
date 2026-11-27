[Back to README](../README.md)

# Quickstart

Clone the repository, run the application with .NET Aspire, send your first tenant management command through the EventStore command gateway, and inspect the command outcome. This guide follows the same developer experience pattern as the [EventStore quickstart](../references/Hexalith.EventStore/docs/getting-started/quickstart.md).

## Prerequisites

Before you begin, verify that the following tools are installed and working. Run each check command and confirm the expected output.

### .NET SDK and runtime

```bash
dotnet --version
```

Expected: `11.0.100` or a later patch in the same feature band. The repository pins SDK `11.0.100` in [`global.json`](../global.json) with `rollForward: latestPatch`.

The current applications and consumers target `net11.0`. Verify `dotnet --list-runtimes` includes `Microsoft.NETCore.App 11.0.x` and `Microsoft.AspNetCore.App 11.0.x`. Retained archive compatibility checks keep their explicit original framework and may additionally require the .NET 10 runtimes.

If not installed, download from [https://dot.net](https://dot.net/download).

### PowerShell and the Aspire CLI

The AppHost build invokes PowerShell 7 (`pwsh`) to generate the platform version catalog. It also resolves the Aspire 17 CLI bundle for the dashboard and orchestrator. Check both from the shell that will run the build.

```bash
pwsh --version
aspire --version
```

Install [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) and the [Aspire CLI](https://aspire.dev/get-started/install-cli/) if needed. Run `aspire setup` to extract the bundle. The pinned SDK can also provision it with `dnx --yes aspire.cli@17.0.0 -- setup`. For an existing installation outside the default location, set `AspireCliBundlePath` to the extracted bundle directory.

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

### About platform scopes and `system` storage

Hexalith.Tenants stores its existing tenant histories in EventStore's shared `system` partition. The public catalog uses `platform:tenants` for tenant commands and cross-tenant reads and `platform:global-administrators` for administrator handoff and membership reads. Single-tenant reads use the canonical tenant ID in the route. The gateway maps authorized requests to retained storage after checking the catalog and the caller's permission.

Use this checkout with the coordinated EventStore `5.6.0` and Hexalith.McpCli.Abstractions `3.0.0` packages. The walkthrough requires an active routing catalog and local IdP credentials authorized for the operations below. The [development routing catalog profile](development-route-catalog.md#retained-backend-and-key-custody) describes the AppHost's opt-in and the custody settings to supply for your selected backend.

The shipped Keycloak realm's public password-grant client does not issue a UI-surface credential. Before starting a fresh walkthrough, arrange the approved local IdP mappings in [Get an Access Token](#get-an-access-token) and access to tokens from an authorized interactive client. AppHost generates the service administrator's login for each fresh realm unless you supply the secret parameters below.

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
catalog_packages=(-p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true
  -p:HexalithEventStoreVersion=5.6.0 -p:HexalithMcpCliVersion=3.0.0)
dotnet build Hexalith.Tenants.slnx --configuration Release "${catalog_packages[@]}"
```

## Run the Application

Load the catalog custody settings for your chosen backend as described in the [development routing catalog profile](development-route-catalog.md#retained-backend-and-key-custody).

The catalog profile uses the bound workload path to bootstrap the configured service administrator at startup with the fixed subject `11111111-1111-1111-1111-111111111111`. The shipped Keycloak realm imports legacy permissions and a `system` tenant grant. Before launch, configure the fixed subject's hosted bootstrap credential in the local realm import or the existing local IdP. Its access token needs a recognized global-administrator hint such as `global_admin=true`, exactly one `eventstore:permission=global-administrators:bootstrap` claim and a canonical tenant grant set. An empty grant set is valid for bootstrap. [Canonical claim contract](production-auth-claim-contract.md#current-7x-canonical-scope-contract)

For a fresh, non-persistent local realm, choose the hosted bootstrap credentials before launch. These existing shared parameters override generation. Keep the values in your local shell and out of tracked settings or transcripts. In the terminal that ran the build:

```bash
set +x
read -r -s -p "Choose local service administrator username: " Parameters__security_eventstore_client_username
printf '\n'
read -r -s -p "Choose local service administrator password: " Parameters__security_eventstore_client_password
printf '\n'
```

Bash cannot export variable names containing hyphens. Pass the exact Aspire parameter keys through `env` when starting AppHost (the values are the ones you entered):

```bash
env TENANTS_SOURCE_CATALOG_PROFILE=true \
    "Parameters__security-eventstore-client-username=$Parameters__security_eventstore_client_username" \
    "Parameters__security-eventstore-client-password=$Parameters__security_eventstore_client_password" \
    dotnet run --project src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj "${catalog_packages[@]}"
```

Keep this terminal running. Use a second terminal for the API commands.

With `KeycloakPersistent=true`, realm import does **not** update an existing realm/user. Changing these parameters will not rotate its stored credentials. Authenticate the existing configured subject with its current credentials, or reconcile the same subject through your IdP administration process, then verify membership below. Do not delete Keycloak/Redis state or change the fixed subject to force this walkthrough. If the configured account cannot authenticate, stop and repair that identity first.

The AppHost starts the EventStore command gateway, Tenants, tenants-api, tenants-ui, Keycloak and DAPR sidecars. It uses the Redis backend selected in the catalog profile. The profile can also include the [sample consumer](development-route-catalog.md).

> **Note:** The first run takes longer than usual because .NET restores NuGet packages and Docker pulls container images for Redis, Keycloak, and the DAPR sidecars.

Once the application starts, the terminal output includes the Aspire dashboard URL. Open it in your browser — the dashboard shows all running services and their endpoints.

Before sending a command, confirm the dashboard shows these local resources as running or healthy:

- `eventstore`: EventStore command gateway, including `POST /api/v1/commands`
- `tenants`: Tenants domain processor for `/process` and query endpoints
- `tenants-api`: Generated REST endpoints for administrator membership and tenant reads
- `tenants-ui`: Blazor InteractiveServer Tenants workspace composed through FrontComposer
- `security`: local Keycloak-backed identity provider for the token-based steps below
- DAPR sidecars for `eventstore`, `tenants`, and `tenants-api`

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

Obtain short-lived access tokens from your configured IdP and approved interactive client. Each token needs the configured issuer and audience, the exact IdP subject and a single issuer-signed `eventstore:surface=ui` claim for the administrator operations. Use the following exact `eventstore:permission` claims, with one occurrence per permission:

| Caller | Permissions for this walkthrough | Tenant grants |
| --- | --- | --- |
| Configured administrator | `global-administrators:read`, `global-administrators:set` | No tenant grant needed for handoff |
| Provisioned human administrator | `global-administrators:read`, `tenants:create`, `tenants:read`, and `tenants:add-user` for the optional follow-up | `my-first-tenant` for the detail and roster reads |

Include the applicable `eventstore:domain` claims (`global-administrators` and `tenants`). The human token used to submit `CreateTenant` also needs a recognized global-administrator hint such as `global_admin=true`. These claims are checked independently of persisted administrator authority. Emit only valid, distinct canonical tenant grants. A retained `eventstore:tenant=system` invalidates the public grant set. Keep the approved interactive client as the source of the UI-surface claim.

The Bash examples require `curl`, `jq` and `python3`. Find `eventstore` and **`tenants-api`** in the Aspire dashboard. EventStore serves commands and status. The generated REST reads use tenants-api. Define all shared helpers in the second terminal before checking membership. Each request has a two-second connection limit and a five-second total limit. Each status or projection polling phase has a 60-second budget shared across all requests and pages.

```bash
set +x
CONNECT_TIMEOUT=2
REQUEST_TIMEOUT=5
POLL_SECONDS=60

# Keep bearer values out of curl's process arguments and output.
platform_api() {
  local bearer="$1"
  shift
  [[ "$bearer" =~ ^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$ ]] || return 1
  printf 'header = "Authorization: Bearer %s"\n' "$bearer" |
    curl --connect-timeout "$CONNECT_TIMEOUT" --max-time "$REQUEST_TIMEOUT" --config - "$@"
}
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
      -H 'X-Hexalith-Scope: platform:global-administrators' \
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
  local bearer="$1" message_id="$2" scope="$3" correlation_id="${4:-}"
  local deadline=$((SECONDS + POLL_SECONDS)) body state budget
  while ((SECONDS < deadline)); do
    budget=$(request_budget "$deadline") || return 1
    body=$(platform_api "$bearer" --max-time "$budget" -fsS \
      -H "X-Hexalith-Scope: $scope" \
      "$EVENTSTORE_URL/api/v1/commands/status/$message_id") || return 1
    state=$(jq -er --arg id "$message_id" --arg correlation "$correlation_id" \
      'select(.messageId == $id and ($correlation == "" or .correlationId == $correlation)) | .status' \
      <<< "$body") || return 1
    case "$state" in
      Completed) printf '%s\n' "$body"; return 0 ;;
      Rejected|PublishFailed|TimedOut) printf '%s\n' "$body" >&2; return 1 ;;
      Received|Processing|EventsStored|EventsPublished) ;;
      *) return 1 ;;
    esac
    sleep 1
  done
  return 1
}
submit_tenant_command() {
  local message_id="$1" command_file="$2" body
  body=$(jq -ce --arg messageId "$message_id" '.messageId = $messageId' "$command_file") || return 1
  platform_api "$TOKEN" --fail-with-body -isS \
    -H 'X-Hexalith-Scope: platform:tenants' \
    -H 'Content-Type: application/json' --data-binary @/dev/fd/3 \
    "$EVENTSTORE_URL/api/v1/commands" 3<<< "$body"
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
  wait_command_completed "$ADMIN_TOKEN" "$HANDOFF_MESSAGE_ID" platform:global-administrators "$HANDOFF_CORRELATION_ID" >/dev/null || return 1
  wait_admin_membership "$ADMIN_TOKEN" "$HANDOFF_SUBJECT" || return 1
  HANDOFF_PROJECTED=true
}
resume_bootstrap() {
  BOOTSTRAP_PROJECTED=false
  [[ -n "${BOOTSTRAP_MESSAGE_ID:-}" && -n "${BOOTSTRAP_CORRELATION_ID:-}" ]] || return 1
  verify_configured_identity || return 1
  wait_command_completed "$ADMIN_TOKEN" "$BOOTSTRAP_MESSAGE_ID" platform:global-administrators "$BOOTSTRAP_CORRELATION_ID" >/dev/null || return 1
  wait_admin_membership "$ADMIN_TOKEN" "$ADMIN_SUBJECT" || return 1
  BOOTSTRAP_PROJECTED=true
}
```

The membership helper consumes the existing REST projection metadata: `X-Hexalith-Query-Provenance=ProjectionBacked`, current lifecycle (or the existing absent-lifecycle compatibility signal `X-Hexalith-Is-Stale=false`), no explicit degradation and a non-empty projection version. The producer may omit the optional `X-Hexalith-Is-Degraded` header for ordinary current projections; absence follows the existing client contract. It rejects stale/degraded/unknown/missing/contradictory evidence and duplicate metadata headers. All pages must report the same projection version. This establishes qualifying projected membership under the existing contract; it adds no bounded revocation guarantee.

Enter the configured administrator's issued access token without echoing it:

```bash
read -r -p "EventStore base URL: " EVENTSTORE_URL
read -r -p "Tenants API base URL: " TENANTS_API_URL
ADMIN_SUBJECT=11111111-1111-1111-1111-111111111111
authenticate_configured_admin() {
  read -r -s -p "Configured administrator access token: " ADMIN_TOKEN || return 1
  printf '\n'
  verify_configured_identity || { unset ADMIN_TOKEN; return 1; }
}
authenticate_configured_admin
```

A nonzero result stops progress. The API independently validates the token's signature and authority when you check membership.

### Verify configured administrator membership

```bash
if verify_configured_identity && has_admin_membership "$ADMIN_TOKEN" "$ADMIN_SUBJECT"; then
  printf 'Configured administrator membership verified.\n'
else
  printf 'Membership unresolved; keep this shell open and follow explicit bootstrap recovery.\n'
fi
```

This check preserves the authenticated shell and every recovery helper on missing membership. A `403`, absent membership, invalid metadata, timeout or unavailable projection leaves bootstrap unresolved. Stop before granting authority and follow the read-only steps in [explicit recovery](production-auth-readiness.md#explicit-bootstrap-recovery). `202 Accepted`, `GlobalAdminAlreadyBootstrappedRejection`, resource health and UI badges alone do not prove this subject is an administrator.

### Grant a provisioned human administrator access

Provision a separate local realm user with the claims above and obtain the exact stable subject from the IdP. Confirm that this is the intended recipient of administrator authority before running the grant.

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
    '{messageId:$messageId,correlationId:$correlationId,tenant:"platform:global-administrators",domain:"global-administrators",aggregateId:"global-administrators",commandType:"SetGlobalAdministrator",payload:{UserId:$subject}}') || return 1
  HANDOFF_HTTP=$(platform_api "$ADMIN_TOKEN" -sS -o /dev/null -w '%{http_code}' \
    -H 'X-Hexalith-Scope: platform:global-administrators' \
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
  read -r -s -p "Provisioned human access token: " TOKEN || return 1
  printf '\n'
  if [[ "$(token_subject "$TOKEN")" != "$HANDOFF_SUBJECT" ]] ||
    ! has_admin_membership "$TOKEN" "$HANDOFF_SUBJECT"; then
    unset TOKEN
    return 1
  fi
}
authenticate_human
```

Use `TOKEN` for the remaining API walkthrough after this function succeeds. Keep tokens in this local shell and clear them with `unset ADMIN_TOKEN TOKEN` when finished.

This walkthrough uses IdP-issued tokens. For production mappings, see [Production Auth Claim Contract](production-auth-claim-contract.md).

> **Production note:** Production deployments use OIDC authority-based JWT validation. Before release, run the [Production Auth Readiness](production-auth-readiness.md) checklist and smoke tests.

## Validate Before the First Command

Do these checks before you submit your first tenant command. They catch the common local setup failures while the system is still idle.

### Verify the EventStore command gateway

Confirm the authenticated OpenAPI document exposes the command gateway route when OpenAPI is enabled:

```bash
platform_api "$TOKEN" -fsS "$EVENTSTORE_URL/openapi/v1.json" |
  jq -e '.paths["/api/v1/commands"].post != null'
```

Expected output is `true`. Submit tenant commands through EventStore's `POST /api/v1/commands` route.

### Verify tenant command status access

Use a fresh ID to check status access without submitting a command:

```bash
STATUS_PROBE_ID=$(new_ulid)
platform_api "$TOKEN" -isS -H 'X-Hexalith-Scope: platform:tenants' \
  "$EVENTSTORE_URL/api/v1/commands/status/$STATUS_PROBE_ID"
```

Expect `404 Not Found` with problem type `https://hexalith.io/problems/command-status-not-found`. The response confirms a status lookup under `platform:tenants`. The catalog and domain still authorize each command separately.

Fix these outcomes before submitting a tenant command:

| Result | Meaning | Fix |
| ------ | ------- | --- |
| `401 Unauthorized` | Missing, expired, malformed, wrong issuer, wrong audience, or wrong signing key token | Obtain a fresh token from the approved interactive client and confirm it matches the AppHost OIDC configuration. |
| `403 Forbidden` | The selected scope or principal is unauthorized | Confirm that you sent the single `platform:tenants` scope header, the token contains exactly one `tenants:read` permission, and all tenant grants are valid and distinct. |
| Connection failure | EventStore is not reachable | Wait for the AppHost resource, then check Docker, DAPR full init, and AppHost resource details. |

## Send Your First Commands

### Step 1: Confirm administrator handoff

Confirm the membership checks succeeded for both subjects. The bound hosted bootstrap is a one-time state-changing operation.

### Step 2: Create Your First Tenant

Save this request as `create-tenant.json`:

```json
{
    "messageId": "01JQK7YQ7YQ7YQ7YQ7YQ7YQ7Y2",
    "tenant": "platform:tenants",
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

Set `aggregateId` and `payload.TenantId` to the same canonical managed tenant ID. The submit helper replaces the illustrative message ID and sends the matching `X-Hexalith-Scope: platform:tenants` header.

Generate and retain a fresh message ID for this attempt:

```bash
CREATE_MESSAGE_ID=$(new_ulid)
submit_tenant_command "$CREATE_MESSAGE_ID" create-tenant.json
```

The API returns `202 Accepted` with the message and correlation IDs. The `Location` header identifies the message at the status polling endpoint. Poll `GET /api/v1/commands/status/{messageId}` on EventStore with the same scope header and the retained ID:

```bash
wait_command_completed "$TOKEN" "$CREATE_MESSAGE_ID" platform:tenants
```

`status: "Completed"` (status code `4`) with `eventCount: 1` confirms that the `TenantCreated` event was stored and published. For `status: "Rejected"`, check `rejectionEventType` for a business-rule rejection or `failureReason` for infrastructure failure. If submission or polling leaves the outcome unresolved, use the same message ID for recovery before issuing a replacement.

### Verify the Event

Verify the tenant through the generated `GET /api/tenants/{tenantId}` endpoint on tenants-api. The route supplies the canonical tenant. Send the authorized bearer with the matching tenant grant and omit scope headers.

```bash
platform_api "$TOKEN" -fsS "$TENANTS_API_URL/api/tenants/my-first-tenant"
```

The response should contain the tenant ID, name, and description you submitted. If the projection is still catching up, retry the read. If it remains unavailable, check the command status and AppHost resource health before proceeding.

### Running the Quickstart Again

If you've run this before:

- Reauthenticate the configured administrator and verify projected membership before repeating the human handoff. `GlobalAdminAlreadyBootstrappedRejection` alone does not establish membership for the configured subject.
- **CreateTenant** with the same tenant ID may reach `status: "Rejected"` with `rejectionEventType` ending in `TenantAlreadyExistsRejection`. Use a different `aggregateId` and matching `payload.TenantId`. Update the read URL and the caller's tenant grant to that same ID.

### Try More Commands

Create a multi-step workflow — add a user to your new tenant:

Save this request as `add-user.json`. Use the exact IdP subject of the user to add in `payload.UserId`.

```json
{
    "messageId": "01JQK7YQ7YQ7YQ7YQ7YQ7YQ7Y3",
    "tenant": "platform:tenants",
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

Submit with a new message ID, then check the status in `platform:tenants`:

```bash
ADD_USER_MESSAGE_ID=$(new_ulid)
submit_tenant_command "$ADD_USER_MESSAGE_ID" add-user.json
wait_command_completed "$TOKEN" "$ADD_USER_MESSAGE_ID" platform:tenants
```

After completion, the generated `GET /api/tenants/{tenantId}/users` route lists the user's subject and role:

```bash
platform_api "$TOKEN" -fsS "$TENANTS_API_URL/api/tenants/my-first-tenant/users"
```

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
| `GlobalAdminAlreadyBootstrappedRejection` | An administrator was already bootstrapped | Verify projected membership for the configured subject before proceeding |
| `TenantAlreadyExistsRejection`            | Tenant ID already used       | Use a different `aggregateId` and `TenantId` |
| `401 Unauthorized`               | Access token expired or invalid | Obtain a fresh token from the approved interactive client and enter it again in [Get an Access Token](#get-an-access-token) |
| `403 Forbidden`                  | Scope, catalog permission, authenticated surface, or domain authority was refused | Check the operation's scope and exact claims above, then verify persisted authority and the applicable tenant grant |
