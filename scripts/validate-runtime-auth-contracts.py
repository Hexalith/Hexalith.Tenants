#!/usr/bin/env python3
"""Prove local Keycloak workload and app-channel contracts without business writes.

Capture a fresh `aspire describe --format json` to an owner-only file, then run:
    python3 scripts/validate-runtime-auth-contracts.py --state /tmp/aspire-state.json

The validator reads project credentials privately from their local Linux process
environments because Aspire describe redacts secrets. No credential, token, URL,
response body, or process environment is emitted. TLS verification stays enabled;
--ca-file can supply the trusted local development CA bundle when needed.
"""

from __future__ import annotations

import argparse
import base64
import json
import os
from pathlib import Path
import ssl
import stat
import sys
import urllib.error
import urllib.parse
import urllib.request
import uuid


class ValidationFailure(Exception):
    """Contain only a fixed, safe check name, never transport or secret data."""


class RejectRedirects(urllib.request.HTTPRedirectHandler):
    """Keep assertion and client credentials on the selected local endpoint."""

    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


def require(condition: bool, check: str, results: dict) -> None:
    results[check] = bool(condition)
    if not condition:
        raise ValidationFailure(check)


def resource(resources: list[dict], name: str, kind: str) -> dict:
    matches = [item for item in resources
               if item.get("displayName", item.get("name")) == name
               and item.get("resourceType") == kind]
    if len(matches) != 1:
        raise ValidationFailure("expected_local_resource")
    selected = matches[0]
    if selected.get("state") != "Running" or selected.get("healthStatus") != "Healthy":
        raise ValidationFailure("required_resources_healthy")
    return selected


def environment(project: dict) -> dict[str, str]:
    pid = project.get("properties", {}).get("executable.pid")
    if not isinstance(pid, int) or pid <= 0:
        raise ValidationFailure("project_process_available")
    raw = Path(f"/proc/{pid}/environ").read_bytes()
    values = dict(entry.decode().split("=", 1) for entry in raw.split(b"\0") if b"=" in entry)
    if values.get("ASPNETCORE_ENVIRONMENT") != "Development":
        raise ValidationFailure("development_topology_only")
    return values


def local_https(url: str) -> str:
    parsed = urllib.parse.urlsplit(url)
    if (parsed.scheme != "https" or parsed.hostname not in {"localhost", "127.0.0.1", "::1"}
            or parsed.username or parsed.password or parsed.query or parsed.fragment):
        raise ValidationFailure("verified_local_https_endpoint")
    return url.rstrip("/")


def endpoint(item: dict, name: str) -> str:
    matches = [url["url"] for url in item.get("urls", []) if url.get("name") == name]
    if len(matches) != 1:
        raise ValidationFailure("required_https_endpoint")
    return local_https(matches[0])


def request(opener, url: str, payload: bytes | None = None, headers: dict | None = None) -> tuple[int, dict]:
    local_https(url)
    req = urllib.request.Request(url, data=payload, headers=headers or {})
    return read_response(opener, req)


def invoke_eventstore_channel(opener, sidecar_url: str, notification: bytes,
                             api_token: str | None = None) -> tuple[int, dict]:
    """Use only the local sidecar API credential; the sidecar supplies APP_API_TOKEN."""
    parsed = urllib.parse.urlsplit(sidecar_url)
    if (parsed.scheme not in {"http", "https"} or parsed.hostname not in {"localhost", "127.0.0.1", "::1"}
            or parsed.username or parsed.password or parsed.query or parsed.fragment or parsed.path not in {"", "/"}):
        raise ValidationFailure("local_dapr_http_endpoint")
    headers = {"Content-Type": "application/json"}
    if api_token:
        headers["dapr-api-token"] = api_token
    url = sidecar_url.rstrip("/") + "/v1.0/invoke/eventstore/method/projections/changed"
    return read_response(opener, urllib.request.Request(url, data=notification, headers=headers))


def read_response(opener, req: urllib.request.Request) -> tuple[int, dict]:
    try:
        response = opener.open(req, timeout=30)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        body = response.read(1_048_577)
        if len(body) > 1_048_576:
            raise ValidationFailure("bounded_response")
        try:
            document = json.loads(body) if body else {}
        except (ValueError, UnicodeError):
            document = {}
        return response.code, document if isinstance(document, dict) else {}


def token_claims(token: str) -> dict:
    parts = token.split(".")
    if len(parts) != 3:
        raise ValidationFailure("issued_workload_jwt")
    # Claim inspection alone is not authentication. The protected production route below
    # independently verifies the actual issuer signature, audience, caller and operation.
    payload = parts[1] + "=" * (-len(parts[1]) % 4)
    return json.loads(base64.urlsafe_b64decode(payload))


def validate(state: dict, context: ssl.SSLContext, results: dict) -> None:
    resources = state["resources"]
    eventstore = resource(resources, "eventstore", "Project")
    tenants = resource(resources, "tenants", "Project")
    resource(resources, "security", "Container")
    eventstore_env = environment(eventstore)
    tenants_env = environment(tenants)
    client_id = eventstore_env["Authentication__WorkloadIssuer__ClientId"]
    secret = eventstore_env["Authentication__WorkloadIssuer__ClientSecret"]
    eventstore_channel = eventstore_env["APP_API_TOKEN"]
    tenants_channel = tenants_env["APP_API_TOKEN"]
    require(bool(secret and eventstore_channel and tenants_channel), "credentials_configured", results)
    require(eventstore_channel != tenants_channel, "app_channel_tokens_isolated", results)
    issuer = local_https(tenants_env["Authentication__JwtBearer__Authority"])
    require(issuer == local_https(eventstore_env["Authentication__JwtBearer__Authority"]),
            "shared_keycloak_issuer", results)
    eventstore_url = endpoint(eventstore, "https")
    tenants_url = endpoint(tenants, "https")
    opener = urllib.request.build_opener(
        urllib.request.ProxyHandler({}), urllib.request.HTTPSHandler(context=context), RejectRedirects())

    def exchange(scope: str, client_secret: str = secret) -> tuple[int, dict]:
        form = urllib.parse.urlencode({
            "grant_type": "client_credentials", "client_id": client_id,
            "client_secret": client_secret, "scope": scope,
        }).encode()
        return request(opener, issuer + "/protocol/openid-connect/token", form,
                       {"Content-Type": "application/x-www-form-urlencoded"})

    scope = "eventstore-audience.tenants eventstore-operation.domain-service.project"
    status, issued = exchange(scope)
    results["workload_token_http_status"] = status
    require(status == 200 and bool(issued.get("access_token")), "client_credentials_grant", results)
    assertion = issued["access_token"]
    claims = token_claims(assertion)
    audiences = claims.get("aud", [])
    audiences = [audiences] if isinstance(audiences, str) else audiences
    operations = claims.get("eventstore:operation", [])
    operations = [operations] if isinstance(operations, str) else operations
    require(claims.get("azp") == client_id == "eventstore", "issued_workload_caller", results)
    require("tenants" in audiences and "sample" not in audiences, "issued_tenants_audience", results)
    require(operations == ["domain-service:project"], "issued_project_operation", results)

    status, denied = exchange(scope, "invalid-local-validator-secret")
    results["invalid_client_secret_http_status"] = status
    require(status in {400, 401} and not denied.get("access_token"), "invalid_client_secret_rejected", results)

    # TenantProjectionHandler returns its default result for an empty event prefix before
    # any state-store access. Never dispatch a command or project the global singleton here.
    projection = json.dumps({"tenantId": "system", "domain": "tenants",
                             "aggregateId": "runtime-contract-" + uuid.uuid4().hex, "events": []}).encode()

    def project(check: str, token: str | None, channel: str | None,
                allowed: bool = False, bearer: bool = False) -> None:
        headers = {"Content-Type": "application/json"}
        if channel is not None:
            headers["dapr-api-token"] = channel
        if token is not None:
            headers["Authorization" if bearer else "X-Hexalith-Workload-Assertion"] = (
                "Bearer " + token if bearer else token)
        code, body = request(opener, tenants_url + "/project", projection, headers)
        results[check + "_http_status"] = code
        require((code == 200 and body.get("projectionType") == "tenants") if allowed
                else code in {401, 403}, check, results)

    project("project_authorized", assertion, tenants_channel, allowed=True)
    project("project_missing_channel_rejected", assertion, None)
    project("project_wrong_channel_rejected", assertion, "invalid-local-validator-channel")
    project("project_other_app_channel_rejected", assertion, eventstore_channel)
    project("project_missing_assertion_rejected", None, tenants_channel)
    project("project_bearer_cannot_replace_assertion", assertion, tenants_channel, bearer=True)

    for check, requested_scope, expected_audience, expected_operations in (
        ("project_missing_operation_rejected", "eventstore-audience.tenants", "tenants", []),
        ("project_wrong_operation_rejected", "eventstore-audience.tenants eventstore-operation.domain-service.query",
         "tenants", ["domain-service:query"]),
        ("project_wrong_audience_rejected", "eventstore-audience.sample eventstore-operation.domain-service.project",
         "sample", ["domain-service:project"]),
    ):
        code, issued_control = exchange(requested_scope)
        require(code == 200 and bool(issued_control.get("access_token")), check + "_token_issued", results)
        control_claims = token_claims(issued_control["access_token"])
        control_audiences = control_claims.get("aud", [])
        control_audiences = [control_audiences] if isinstance(control_audiences, str) else control_audiences
        control_operations = control_claims.get("eventstore:operation", [])
        control_operations = [control_operations] if isinstance(control_operations, str) else control_operations
        require(control_claims.get("azp") == client_id, check + "_caller", results)
        require(control_audiences == [expected_audience], check + "_audience", results)
        require(control_operations == expected_operations, check + "_operations", results)
        project(check, issued_control["access_token"], tenants_channel)

    # /dapr/subscribe is anonymous in EventStore, so it cannot prove channel protection.
    # This protected callback returns 400 on empty identity before provenance verification,
    # actor access, freshness mutation, or broadcasting. 400 proves channel admission only.
    notification = json.dumps({"tenantId": "", "projectionType": ""}).encode()
    for check, channel, expected in (
        ("eventstore_channel_admitted", eventstore_channel, {400}),
        ("eventstore_missing_channel_rejected", None, {401, 403}),
        ("eventstore_wrong_channel_rejected", "invalid-local-validator-channel", {401, 403}),
        ("eventstore_other_app_channel_rejected", tenants_channel, {401, 403}),
    ):
        headers = {"Content-Type": "application/json"}
        if channel is not None:
            headers["dapr-api-token"] = channel
        code, _ = request(opener, eventstore_url + "/projections/changed", notification, headers)
        results[check + "_http_status"] = code
        require(code in expected, check, results)

    # Invoke the app through its own sidecar without supplying APP_API_TOKEN ourselves.
    # DAPR_API_TOKEN, when configured, authenticates this request to the sidecar only;
    # the sidecar must independently deliver the app-channel token on the final hop.
    code, _ = invoke_eventstore_channel(opener, eventstore_env["DAPR_HTTP_ENDPOINT"], notification,
                                        eventstore_env.get("DAPR_API_TOKEN"))
    results["eventstore_sidecar_channel_delivered_http_status"] = code
    require(code == 400, "eventstore_sidecar_channel_delivered", results)
    results["empty_tenant_projection_only"] = True
    results["tls_verified"] = True


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--state", type=Path, required=True, help="Private JSON from fresh aspire describe")
    parser.add_argument("--ca-file", type=Path, help="Trusted local TLS CA bundle; verification stays enabled")
    args = parser.parse_args()
    results: dict = {"validator": "tenants-runtime-auth-contracts/v1", "passed": False}
    try:
        descriptor = os.open(args.state, os.O_RDONLY | os.O_NOFOLLOW | os.O_CLOEXEC | os.O_NONBLOCK)
        with os.fdopen(descriptor) as state_file:
            file_stat = os.fstat(state_file.fileno())
            if (not stat.S_ISREG(file_stat.st_mode) or file_stat.st_uid != os.getuid()
                    or file_stat.st_mode & 0o077):
                raise ValidationFailure("state_file_owner_only")
            state = json.load(state_file)
        context = ssl.create_default_context(cafile=str(args.ca_file) if args.ca_file else None)
        validate(state, context, results)
        results["passed"] = True
    except ValidationFailure as error:
        results["failed_check"] = str(error)
    except Exception as error:
        # Exception messages can contain URLs, headers, or the secret-bearing token response.
        results["error"] = "local configuration or transport failure"
        results["exception_type"] = type(error).__name__
    print(json.dumps(results, indent=2, sort_keys=True))
    return 0 if results["passed"] else 1


if __name__ == "__main__":
    sys.exit(main())
