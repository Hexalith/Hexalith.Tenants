#!/usr/bin/env python3
"""Provision private material only after proving this CI job owns a fresh backend."""
import argparse
import base64
import datetime
import json
import os
from pathlib import Path
import secrets
import subprocess
import tempfile


def command(*arguments):
    return subprocess.check_output(arguments, text=True, timeout=30).strip()


def instant(value):
    return datetime.datetime.fromisoformat(value.replace("Z", "+00:00"))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--sdk-source-sha", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if os.environ.get("GITHUB_ACTIONS") != "true" or os.environ.get("GITHUB_JOB") != "development-catalog":
        raise ValueError("The source profile requires its declared disposable CI job.")
    root = Path(command("git", "rev-parse", "--show-toplevel"))
    for module in ("Hexalith.EventStore", "Hexalith.Builds"):
        relative = f"references/{module}"
        if command("git", "config", "--file", ".gitmodules", "--get", f"submodule.{module}.path") != relative:
            raise ValueError("A source prerequisite is not root-declared.")
        expected = command("git", "rev-parse", f"HEAD:{relative}")
        actual = command("git", "-C", str(root / relative), "rev-parse", "HEAD")
        if actual != expected or module == "Hexalith.EventStore" and actual != args.sdk_source_sha:
            raise ValueError("The actual source prerequisite differs from the selected gitlink.")
    if args.output.exists() or args.output.is_symlink():
        raise ValueError("Existing source qualification evidence must remain unchanged.")
    started = instant(os.environ["TENANTS_SOURCE_BACKEND_STARTED_AT"])
    before = Path(os.environ["TENANTS_SOURCE_BACKEND_BEFORE_PATH"])
    previous_volumes = json.loads(before.read_text(encoding="utf-8"))
    if not isinstance(previous_volumes, list) or any(not isinstance(name, str) for name in previous_volumes):
        raise ValueError("The pre-initialization backend inventory is invalid.")
    backend = json.loads(command("docker", "inspect", "dapr_redis"))[0]
    if not backend["State"]["Running"] or instant(backend["Created"]) < started:
        raise ValueError("The backend was not created by this job's initialization.")
    volumes = []
    for mount in backend["Mounts"]:
        if mount["Type"] != "volume":
            raise ValueError("A bind mount cannot establish a fresh business backend.")
        volume = json.loads(command("docker", "volume", "inspect", mount["Name"]))[0]
        if mount["Name"] in previous_volumes or volume["Driver"] != "local" or instant(volume["CreatedAt"]) < started.replace(microsecond=0):
            raise ValueError("A retained volume cannot receive newly provisioned owner keys.")
        volumes.append({"name": volume["Name"], "created": volume["CreatedAt"]})
    ports = backend["NetworkSettings"]["Ports"].get("6379/tcp") or []
    if not any(port["HostPort"] == "6379" and port["HostIp"] in ("0.0.0.0", "127.0.0.1") for port in ports):
        raise ValueError("The actual backend does not serve the selected localhost:6379 owner component.")
    if command("docker", "exec", backend["Id"], "redis-cli", "DBSIZE") != "0":
        raise ValueError("The business backend is not empty; no replacement owner material is provisioned.")
    runner_temp = Path(os.environ["RUNNER_TEMP"]).resolve(strict=True)
    private = Path(tempfile.mkdtemp(prefix="tenants-source-catalog-", dir=runner_temp))
    private.chmod(0o700)
    ring = base64.b64encode(secrets.token_bytes(32)).decode()
    signer = secrets.token_hex(32)
    for secret in (ring, signer):
        print("::add-mask::" + secret, flush=True)
    values = {
        "TENANTS_SOURCE_CATALOG_PROFILE": "true",
        # This separate behavior opt-in is emitted only after the fresh backend checks above.
        "TENANTS_SOURCE_WORKER_BACKED_RESPONSES_ENABLED": "true",
        # Exact owning categories only; do not enable HTTP/header or arbitrary transport logging.
        "Logging__LogLevel__Hexalith.EventStore.ServiceDefaults.Routing.TechnicalHttpRouteCatalogReceiver": "Debug",
        "Logging__LogLevel__Hexalith.EventStore.Server.Commands.CommandRouter": "Debug",
        "Logging__LogLevel__Hexalith.EventStore.Server.Pipeline.SubmitCommandHandler": "Debug",
        "TENANTS_SOURCE_CATALOG_SIGNING_KEY": signer,
        "Tenants__DevelopmentRouteCatalog__CustodyDirectory": str(private / "custody"),
        "Tenants__DevelopmentRouteCatalog__AuthorityComponentName": "routing-tenants-owner",
        "Tenants__DevelopmentRouteCatalog__AuthorityStateKey": "tenants-owner-authority",
        "Tenants__DevelopmentRouteCatalog__ActiveDigestKeyGeneration": "development-v1",
        "Tenants__DevelopmentRouteCatalog__DigestKeys__development-v1": ring,
        "Tenants__DevelopmentRouteCatalog__SecretMapGeneration": "development-v1",
        "EventStore__Authentication__Issuer": "hexalith-dev",
        "EventStore__Authentication__Audience": "hexalith-eventstore",
        "EventStore__Authentication__SigningKey": signer,
        "EventStore__Authentication__Subject": "11111111-1111-1111-1111-111111111111",
        "EventStore__Authentication__GlobalAdmin": "true",
        "EventStore__Authentication__Tenants__0": "system",
        "EventStore__Authentication__Domains__0": "tenants",
        "EventStore__Authentication__Domains__1": "global-administrators",
        "EventStore__Authentication__Permissions__0": "command:submit",
        "EventStore__Authentication__Permissions__1": "query:read",
    }
    with open(os.environ["GITHUB_ENV"], "a", encoding="utf-8") as output:
        for key, value in values.items():
            output.write(f"{key}={value}\n")
    evidence = {"sdkSource": args.sdk_source_sha,
                "sdkProducerVersion": os.environ.get("TENANTS_SOURCE_SDK_PRODUCER_VERSION"),
                "buildsSource": command("git", "rev-parse", "HEAD:references/Hexalith.Builds"),
                "backend": {"containerId": backend["Id"], "imageId": backend["Image"],
                            "created": backend["Created"], "volumes": volumes, "initialDatabaseSize": 0},
                "materialProvisionedWithFreshBackend": True}
    with args.output.open("x", encoding="utf-8") as output:
        output.write(json.dumps(evidence, indent=2) + "\n")


if __name__ == "__main__":
    main()
