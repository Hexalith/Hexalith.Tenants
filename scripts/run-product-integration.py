#!/usr/bin/env python3
"""Run the complete integration project once, with separately isolated cold profiles."""
import importlib.util
import json
import os
from pathlib import Path
import re
import select
import shutil
import signal
import subprocess
import sys
import time


ROOT = Path(__file__).resolve().parents[1]
PROJECT = "tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj"
# The ordinary tier excludes exactly these five classes and Performance. Each
# cold profile uses one distinct nonzero database; DB0 remains the ordinary tier.
PROFILES = (
    ("rest", "AuthenticatedTenantsRouteTests", 1, 8, False, False),
    ("persisted", "AspireTopologyTests", 2, 7, True, True),
    ("anonymous-ui", "TenantsUiRouteSmokeTests", 3, 6, False, False),
    ("authenticated-ui", "AuthenticatedTenantsUiRouteTests", 4, 3, False, False),
    ("catalog-owner", "SourceCatalogOwnerProfileTests", 5, 1, True, False),
)
SPEC = importlib.util.spec_from_file_location("catalog_profile", ROOT / "scripts/prepare-source-catalog-profile.py")
PROFILE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PROFILE)


def stop_private_consumers(private):
    """Terminate and await only consumers inheriting this profile's private workspace."""
    descriptors = []
    for pid in PROFILE.active_processes(private):
        descriptor = None
        try:
            descriptor = os.pidfd_open(pid)
            if pid not in PROFILE.active_processes(private):
                os.close(descriptor)
                continue
            signal.pidfd_send_signal(descriptor, signal.SIGTERM)
            descriptors.append(descriptor)
        except ProcessLookupError:
            if descriptor is not None:
                os.close(descriptor)
            continue
    for descriptor in descriptors:
        try:
            if not select.select([descriptor], [], [], 30)[0]:
                signal.pidfd_send_signal(descriptor, signal.SIGKILL)
                if not select.select([descriptor], [], [], 30)[0]:
                    raise ValueError("A private topology consumer did not stop.")
        finally:
            os.close(descriptor)
    if PROFILE.active_processes(private):
        raise ValueError("Private topology cleanup is incomplete.")
    for identifier in PROFILE.private_containers(private):
        PROFILE.command("docker", "stop", "--time", "15", identifier)
        PROFILE.command("docker", "rm", identifier)
    if PROFILE.private_containers(private):
        raise ValueError("Private topology container cleanup is incomplete.")


def run_profile(profile, class_name, database, minimum, workers, sample, *, temporary, output, producer, source):
    receipt = output / (profile + ".json")
    env_path = temporary / ("tenants-profile-environment-" + profile)
    descriptor = os.open(env_path, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600)
    os.close(descriptor)
    environment = {**os.environ, "GITHUB_ENV": str(env_path), "TENANTS_SOURCE_SDK_PRODUCER_VERSION": producer}
    private = None
    try:
        subprocess.run([sys.executable, "scripts/prepare-source-catalog-profile.py", "--profile", profile,
                        "--redis-db", str(database), "--worker-backed-responses", str(workers).lower(),
                        "--sdk-source-sha", source, "--output", str(receipt)], cwd=ROOT, env=environment, check=True)
        values = dict(line.split("=", 1) for line in env_path.read_text(encoding="utf-8").splitlines())
        provisioning = json.loads(receipt.read_text(encoding="utf-8"))
        private = PROFILE.private_directory(provisioning["privateDirectory"], temporary)
        result_path = private / "catalog-result.json"
        child = {**environment, **values, "TMPDIR": str(private), "TMP": str(private), "TEMP": str(private),
                 "TENANTS_LIVE_REQUIRE_COLD_BOOTSTRAP": "true", "TENANTS_SOURCE_CATALOG_RESULT": str(result_path),
                 "TENANTS_SOURCE_CATALOG_INCLUDE_SAMPLE": str(sample).lower(),
                 "EventStore__Projections__WorkersEnabled": str(workers).lower()}
        print("Cold product profile: " + profile, flush=True)
        with (private / "test.log").open("x", encoding="utf-8") as log:
            (private / "test.log").chmod(0o600)
            process = subprocess.Popen(["dotnet", "test", "--project", PROJECT, "--configuration", "Release",
                "--no-build", "--no-restore", "--stop-on-fail", "off", "--filter-class",
                "Hexalith.Tenants.IntegrationTests." + class_name, "--minimum-expected-tests", str(minimum),
                "--fail-skips", "on", "--results-directory", "TestResults/authenticated-routes/" + profile,
                "--report-xunit-trx", "--report-xunit-trx-filename", profile + ".trx"],
                cwd=ROOT, env=child, stdout=log, stderr=subprocess.STDOUT, start_new_session=True)
            try:
                status = process.wait(timeout=2400)
            except BaseException:
                os.killpg(process.pid, signal.SIGTERM)
                try: process.wait(timeout=30)
                except subprocess.TimeoutExpired:
                    os.killpg(process.pid, signal.SIGKILL)
                    process.wait()
                raise
        # Only the existing support-safe result shapes cross private custody.
        if result_path.is_file() and not result_path.is_symlink():
            PROFILE.write_exclusive(output / (profile + "-catalog-result.json"), json.loads(result_path.read_text()))
        for index, result in enumerate(sorted(private.glob("tenants-live-routes-*/result.json"))):
            if result.is_file() and not result.is_symlink():
                PROFILE.write_exclusive(output / (profile + "-browser-" + str(index) + ".json"), json.loads(result.read_text()))
        PROFILE.finish(receipt, temporary, PROFILE.job_identity())
        private = None
        if status:
            raise ValueError("A cold product profile failed: " + profile)
    finally:
        env_path.unlink(missing_ok=True)
        if private is not None:
            # This cleanup never creates a successful shutdown receipt. A failed
            # fixture still blocks later profiles and the job remains failed.
            private = PROFILE.private_directory(private, temporary)
            stop_private_consumers(private)
            shutil.rmtree(PROFILE.private_directory(private, temporary))


def main():
    if os.environ.get("TENANTS_SOURCE_CATALOG_PROFILE") == "true":
        raise ValueError("The ordinary integration tier cannot inherit a cold catalog profile.")
    subprocess.run(["bash", "scripts/run-aspire-integration-tier.sh"], cwd=ROOT, check=True)
    temporary = Path(os.environ["RUNNER_TEMP"]).resolve(strict=True)
    output = temporary / "tenants-product-profiles"
    output.mkdir(mode=0o700)
    source = PROFILE.command("git", "rev-parse", "HEAD:references/Hexalith.EventStore")
    producer = PROFILE.command("dotnet", "msbuild", "src/Hexalith.Tenants/Hexalith.Tenants.csproj", "-nologo",
        "-getProperty:HexalithEventStoreVersion", "-p:Configuration=Release", "-p:UseHexalithProjectReferences=false",
        "-p:UseNuGetDeps=true", "-p:HexalithEventStoreFromSource=false")
    if producer != "5.1.1":
        raise ValueError("The catalog profile did not select its exact qualified SDK.")
    for values in PROFILES:
        run_profile(*values, temporary=temporary, output=output, producer=producer, source=source)


if __name__ == "__main__":
    try:
        main()
    except Exception:
        print("Tenants product integration failed; only support-safe results are retained and private topology material is cleaned.", file=sys.stderr)
        raise SystemExit(1)
