# Create the environment's synthetic tenant

The Tenants manifest declares `create-synthetic-tenant` as an operator task with
`once-per-environment-creation` scope. Run
`module/tasks/create-synthetic-tenant.py` with Python 3 on Unix. The executable
takes no arguments. Set `HEXALITH_TASK_INVOCATION_PATH` to the path of the
runner's private UTF-8 JSON invocation file.

| Field | Value |
| --- | --- |
| `syntheticTenantId` | The canonical environment tenant ID, with 1 to 64 lowercase ASCII letters, digits, or hyphens. The first and last characters must be alphanumeric. `system` is reserved. |
| `gatewayUrl` | The base HTTP or HTTPS URL for the EventStore gateway or the runner's operator relay. |
| `commandTenant` | The command scope admitted by the gateway for Tenants, typically `platform:tenants`. |
| `headers` | A JSON object with the operator's authorization and catalog-binding header values supplied by the runner. |

The task submits `create-synthetic-tenant` to `POST /api/v1/commands` and
polls `GET /api/v1/commands/status/{messageId}` with the same credentials. It
allows up to 120 seconds for submission and completion. The gateway must
admit the internal command and resolve the caller as a verified global
administrator.

For a schema 3 gateway, point `gatewayUrl` at the runner's operator relay. The
relay is part of the future Builds task runner wiring. It forwards the exact
command and bound operator principal to EventStore's authenticated
`POST /api/v1/commands/bound/workload` endpoint with a workload assertion and a
signed command proof. For `platform:tenants`, the submission also needs the
bound operator's JWT with the `tenants:create` permission. The relay polls
EventStore's regular status endpoint with `X-Hexalith-Scope: platform:tenants`
and a scoped operator JWT with the `tenants:read` permission. The workload
status endpoint accepts public tenant IDs only.

Creation leaves the tenant active with the tenant identifier as its display
name. Rerunning the task for the same synthetic tenant produces no new event.
Global administrator tenant lists omit synthetic tenants, and members can see
their own synthetic tenant.

| Exit | Outcome |
| --- | --- |
| 0 | The command completed, including an idempotent rerun. |
| 1 | A timeout or a gateway failure prevented confirmation. |
| 2 | The invocation is missing or invalid. The task validates the tenant ID before sending a request. |
| 3 | The gateway refused authorization or the domain rejected the command. A real-tenant collision reports the tenant ID and `TenantAlreadyExistsRejection`. |

Keep the invocation file private. The task emits bounded diagnostics and does
not print credentials, headers, or gateway response bodies.
