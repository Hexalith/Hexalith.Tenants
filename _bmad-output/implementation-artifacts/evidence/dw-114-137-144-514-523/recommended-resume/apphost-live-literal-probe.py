"""Qualify all six literal reads and three denial controls on a running Tenants AppHost."""
from pathlib import Path
import argparse
import base64
import hashlib
import hmac
import json
import os
import re
import subprocess
import time
import urllib.error
import urllib.parse
import urllib.request

EXPECTED_CHECKS = ("list", "detail", "users", "audit", "memberships", "administrators", "denied caller", "denied verb", "missing bearer")


def checkout_root(explicit=None):
    if explicit:
        return Path(explicit).resolve()
    for candidate in (Path.cwd(), *Path(__file__).resolve().parents):
        if (candidate / "Hexalith.Tenants.slnx").is_file():
            return candidate
    raise RuntimeError("Pass --repo pointing to the Tenants checkout.")


def demo_token(root):
    fixture = root / "tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs"
    key = re.search(r'DemoSigningKey = "([^"]+)"', fixture.read_text()).group(1)
    def b64(value):
        return base64.urlsafe_b64encode(value).decode().rstrip("=")
    claims = {"iss": "hexalith-dev", "aud": "hexalith-eventstore", "exp": int(time.time()) + 600,
              "sub": "11111111-1111-1111-1111-111111111111", "tenants": '["system"]',
              "domains": '["global-administrators","tenants"]', "permissions": '["command:submit","query:read"]',
              "roles": '["GlobalAdministrator"]'}
    unsigned = b64(b'{"alg":"HS256","typ":"JWT"}') + "." + b64(json.dumps(claims, separators=(",", ":")).encode())
    return unsigned + "." + b64(hmac.new(key.encode(), unsigned.encode(), hashlib.sha256).digest())


def qualify(state, token, tenant_id=None):
    def endpoint(name):
        resource = next(r for r in state["resources"] if r.get("displayName", r.get("name")) == name)
        return next(u["url"] for u in resource["urls"] if u["name"] == "http")

    def read(base, path, method="GET", auth=True):
        headers = {"dapr-app-id": "tenants-api"}
        if auth:
            headers["Authorization"] = "Bearer " + token
        request = urllib.request.Request(base + path, method=method, headers=headers)
        try:
            with urllib.request.urlopen(request, timeout=20) as response:
                return response.status, response.read()
        except urllib.error.HTTPError as error:
            return error.code, error.read()

    results = []
    def check(label, base, path, expected=200, method="GET", auth=True):
        try:
            code, body = read(base, path, method, auth)
            results.append({"case": label, "status": code, "expected": expected, "pass": code == expected})
            return code, body
        except Exception as error:
            results.append({"case": label, "status": None, "expected": expected, "pass": False,
                            "reason": type(error).__name__})
            return None, b""

    caller = endpoint("tenants-ui-dapr-cli")
    code, body = check("list", caller, "/api/tenants?pageSize=1")
    if not tenant_id and code == 200:
        rows = json.loads(body).get("items", [])
        tenant_id = rows[0]["tenantId"] if rows else None
    subject = "11111111-1111-1111-1111-111111111111"
    paths = {"detail": f"/api/tenants/{urllib.parse.quote(tenant_id or '', safe='')}",
             "users": f"/api/tenants/{urllib.parse.quote(tenant_id or '', safe='')}/users?pageSize=1",
             "audit": f"/api/tenants/{urllib.parse.quote(tenant_id or '', safe='')}/audit?pageSize=1",
             "memberships": f"/api/users/{subject}/tenants?pageSize=1",
             "administrators": "/api/global-administrators?pageSize=1"}
    for label, path in paths.items():
        if tenant_id:
            check(label, caller, path)
        else:
            results.append({"case": label, "status": None, "expected": 200, "pass": False,
                            "reason": "Seed a supported tenant or pass --tenant-id; all six reads are required."})
    check("denied caller", endpoint("eventstore-dapr-cli"), "/api/tenants", expected=403)
    check("denied verb", caller, "/api/tenants", expected=403, method="POST")
    check("missing bearer", caller, "/api/tenants", expected=401, auth=False)
    return {"localTopology": "ordinary Tenants AppHost, shared Platform mTLS bootstrap",
            "routeMode": "literal", "results": results,
            "allPass": tuple(row["case"] for row in results) == EXPECTED_CHECKS and all(row["pass"] for row in results)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", help="Tenants checkout (otherwise discovered from this script or cwd)")
    parser.add_argument("--state", type=Path, help="Explicit Aspire describe JSON; otherwise collect fresh state")
    parser.add_argument("--tenant-id", help="Existing supported literal tenant identifier")
    parser.add_argument("--token-env", help="Environment variable containing an access token; otherwise use the local fixture token")
    parser.add_argument("--output", type=Path, default=Path(__file__).with_name("apphost-live-literal-results.json"))
    args = parser.parse_args()
    root = checkout_root(args.repo)
    state = json.loads(args.state.read_text() if args.state else subprocess.check_output(
        ["aspire", "describe", "--apphost", str(root / "src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj"), "--format", "Json"], text=True, cwd=root))
    token = os.environ[args.token_env] if args.token_env else demo_token(root)
    report = qualify(state, token, args.tenant_id)
    args.output.write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))
    return 0 if report["allPass"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
