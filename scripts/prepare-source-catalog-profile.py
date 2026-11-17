#!/usr/bin/env python3
"""Provision a cold catalog profile on one unused database of the job-owned Redis.

Dapr initialization belongs to Builds. This consumer accepts only its completed,
current-job receipt, and never resets Redis or reuses a profile database.
"""
import argparse
import base64
import datetime
import hashlib
import json
import os
from pathlib import Path
import re
import secrets
import shutil
import stat
import subprocess
import tempfile
import xml.etree.ElementTree as ET


IDENTITY = {"repository": "GITHUB_REPOSITORY", "runId": "GITHUB_RUN_ID",
            "runAttempt": "GITHUB_RUN_ATTEMPT", "job": "GITHUB_JOB"}


def command(*arguments):
    # Docker inspect can contain credentials. Keep failed output out of diagnostics.
    result = subprocess.run(arguments, text=True, capture_output=True, timeout=30, check=False)
    if result.returncode:
        raise ValueError("A required catalog prerequisite inspection failed.")
    return result.stdout.strip()


def instant(value):
    if not isinstance(value, str) or not re.fullmatch(
            r"\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(?:\.\d+)?(?:Z|\+00:00)", value):
        raise ValueError("The backend timestamp is not UTC.")
    return datetime.datetime.fromisoformat(value.replace("Z", "+00:00"))


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate receipt fields are refused.")
        result[key] = value
    return result


def document(path, temporary):
    if (not path.is_absolute() or path.is_symlink() or path.resolve() != path
            or not path.is_relative_to(temporary)):
        raise ValueError("Catalog receipts must be real files beneath RUNNER_TEMP.")
    metadata = path.stat()
    if (not stat.S_ISREG(metadata.st_mode) or metadata.st_uid != os.getuid()
            or stat.S_IMODE(metadata.st_mode) & 0o077 or metadata.st_size > 1024 * 1024):
        raise ValueError("Catalog receipts require private current-user custody.")
    return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=unique_object)


def private_directory(value, temporary):
    """Bind recursive cleanup to one canonical directory created by this profile."""
    path = Path(value)
    if (not temporary.is_absolute() or temporary.is_symlink() or temporary.resolve() != temporary
            or not path.is_absolute() or path.is_symlink() or path.resolve() != path
            or path.parent != temporary or re.fullmatch(r"tenants-source-catalog-[A-Za-z0-9_-]+", path.name) is None):
        raise ValueError("Catalog cleanup requires its own private directory directly beneath RUNNER_TEMP.")
    metadata = path.stat()
    if (not stat.S_ISDIR(metadata.st_mode) or metadata.st_uid != os.getuid()
            or stat.S_IMODE(metadata.st_mode) & 0o077):
        raise ValueError("Catalog cleanup requires private current-user directory custody.")
    return path


def write_exclusive(path, value):
    descriptor = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600)
    with os.fdopen(descriptor, "w", encoding="utf-8") as output:
        json.dump(value, output, indent=2)
        output.write("\n")


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def job_identity():
    value = {key: os.environ.get(variable, "") for key, variable in IDENTITY.items()}
    if (os.environ.get("GITHUB_ACTIONS") != "true"
            or re.fullmatch(r"[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+", value["repository"]) is None
            or any(re.fullmatch(r"[1-9][0-9]*", value[key]) is None for key in ("runId", "runAttempt"))
            or re.fullmatch(r"[A-Za-z_][A-Za-z0-9_-]*", value["job"]) is None):
        raise ValueError("The catalog profile requires a supported shared CI job identity.")
    return value


def initialization(temporary, identity):
    receipt_path = Path(os.environ["HEXALITH_DAPR_INITIALIZATION_RECEIPT"])
    receipt = document(receipt_path, temporary)
    if (receipt_path.name != "hexalith-dapr-initialization.json"
            or receipt.get("schema") != "hexalith.dapr-initialization.v1"
            or receipt.get("status") != "complete" or receipt.get("identity") != identity):
        raise ValueError("A completed Dapr initialization receipt for this job is required.")
    started, completed = instant(receipt.get("startedAt")), instant(receipt.get("completedAt"))
    if completed < started or completed > datetime.datetime.now(datetime.timezone.utc):
        raise ValueError("The Dapr initialization interval is invalid.")
    before = receipt.get("preexistingVolumes")
    previous_containers = receipt.get("preexistingContainerIds")
    if (not isinstance(before, list) or any(not isinstance(name, str) for name in before)
            or len(set(before)) != len(before)
            or not isinstance(previous_containers, list)
            or any(not isinstance(name, str) for name in previous_containers)):
        raise ValueError("The pre-initialization backend inventory is invalid.")
    candidates = [item for item in receipt.get("createdContainers", [])
                  if isinstance(item, dict) and item.get("Name") == "/dapr_redis"]
    if len(candidates) != 1:
        raise ValueError("The initialization receipt does not identify one newly created Redis.")
    selected = candidates[0]
    backend = json.loads(command("docker", "inspect", "dapr_redis"))[0]
    if (backend.get("Id") != selected.get("Id") or backend["Id"] in previous_containers
            or not re.fullmatch(r"[0-9a-f]{64}", backend["Id"])
            or backend.get("Created") != selected.get("Created")
            or backend.get("Name") != "/dapr_redis" or selected.get("Running") is not True
            or not backend["State"]["Running"] or not started <= instant(backend["Created"]) <= completed
            or backend["Config"]["Image"] != selected.get("Image")):
        raise ValueError("The backend was not created by this job's initialization.")
    mounts = backend["Mounts"]
    recorded_mounts = selected.get("Mounts")
    if not mounts or not isinstance(recorded_mounts, list) or len(mounts) != len(recorded_mounts):
        raise ValueError("The actual backend mounts differ from initialization.")
    volumes = []
    for mount, recorded in zip(mounts, recorded_mounts):
        if mount.get("Type") != "volume":
            raise ValueError("A bind mount cannot establish a fresh business backend.")
        if {key: mount.get(key) for key in recorded} != recorded:
            raise ValueError("The actual backend mounts differ from initialization.")
        records = [item for item in receipt.get("mountedVolumes", []) if item.get("Name") == mount.get("Name")]
        volume = json.loads(command("docker", "volume", "inspect", mount["Name"]))[0]
        if (len(records) != 1 or any(volume.get(key) != value for key, value in records[0].items())
                or mount["Name"] in before or volume["Driver"] != "local" or volume.get("Scope") != "local"
                or instant(volume["CreatedAt"]) < started.replace(microsecond=0)
                or instant(volume["CreatedAt"]) > completed):
            raise ValueError("A retained or substituted volume cannot receive newly provisioned owner keys.")
        volumes.append({"name": volume["Name"], "created": volume["CreatedAt"]})
    ports = backend["NetworkSettings"]["Ports"].get("6379/tcp") or []
    if not any(port["HostPort"] == "6379" and port["HostIp"] in ("0.0.0.0", "127.0.0.1") for port in ports):
        raise ValueError("The actual backend does not serve the selected localhost:6379 owner component.")
    return receipt_path, backend, volumes


def redis_metadata(text, name):
    # These two checked-in component templates have literal Redis metadata. Reject
    # duplicates, YAML aliases/anchors and alternate value forms rather than guess.
    if re.search(r"^[^#\n]*[&*]", text, re.MULTILINE):
        raise ValueError("Component aliases and anchors cannot select the business backend.")
    entries = re.findall(r"(?m)^    - name: " + re.escape(name) + r"\s*\n      value: ([^\n]+)$", text)
    active = "\n".join(line.split("#", 1)[0] for line in text.splitlines() if not line.lstrip().startswith("#"))
    count = len(re.findall(r"\b" + re.escape(name) + r"\b", active))
    if count != len(entries) or count > 1:
        raise ValueError("Redis component metadata must be unique and literal.")
    return entries[0].strip('"') if entries else None


def components(root, private, database):
    source = root / "src/Hexalith.Tenants.AppHost/DaprComponents"
    target = private / "components"
    target.mkdir(mode=0o700)
    hashes = {}
    for original in sorted([*source.glob("*.yaml"), source / "secrets.json"]):
        if original.is_symlink() or not original.is_file():
            raise ValueError("The complete component set must contain regular checked-in files.")
        text = original.read_text(encoding="utf-8")
        if original.name in ("statestore.yaml", "pubsub.yaml", "configstore.yaml"):
            if redis_metadata(text, "redisDB") is not None or redis_metadata(text, "redisHost") != "localhost:6379":
                raise ValueError("The default Redis component backend differs from its qualified profile.")
            text = text.replace("    - name: redisHost\n", f'    - name: redisDB\n      value: "{database}"\n    - name: redisHost\n')
            if redis_metadata(text, "redisDB") != str(database):
                raise ValueError("The isolated Redis database was not selected canonically.")
        if original.name == "secretstore.memories.yaml":
            text = text.replace('value: "DaprComponents/secrets.json"', "value: " + json.dumps(str(target / "secrets.json")))
        destination = target / original.name
        destination.write_text(text, encoding="utf-8")
        destination.chmod(0o600)
        hashes[original.name] = digest(destination)
    if not {"statestore.yaml", "pubsub.yaml"} <= hashes.keys():
        raise ValueError("The complete state and pub/sub component set is required.")
    return target, hashes


def package_producer(root, version):
    """Bind the catalog package producer independently of executable source gitlinks."""
    assets = json.loads((root / "src/Hexalith.Tenants.AppHost/obj/project.assets.json").read_text(encoding="utf-8"),
                        object_pairs_hook=unique_object)
    package = "Hexalith.EventStore.Aspire/" + version
    entry = assets["libraries"].get(package)
    if (not isinstance(entry, dict) or entry.get("type") != "package"
            or not isinstance(entry.get("sha512"), str) or not entry["sha512"]):
        raise ValueError("The catalog SDK must be the exact restored published package.")
    relative = Path(entry["path"])
    if relative.is_absolute() or ".." in relative.parts:
        raise ValueError("The restored catalog package path is invalid.")
    candidates = [Path(folder) / relative / "hexalith.eventstore.aspire.nuspec" for folder in assets["packageFolders"]]
    candidates = [path for path in candidates if path.is_file() and not path.is_symlink()]
    if len(candidates) != 1:
        raise ValueError("The restored catalog package producer is ambiguous or absent.")
    repository = ET.parse(candidates[0]).getroot().find("{*}metadata/{*}repository")
    if (repository is None or repository.get("url", "").removesuffix(".git").rstrip("/") != "https://github.com/Hexalith/Hexalith.EventStore"
            or re.fullmatch(r"[0-9a-f]{40}", repository.get("commit", "")) is None):
        raise ValueError("The restored catalog package has no exact producer revision.")
    return {"id": "Hexalith.EventStore.Aspire", "version": version, "sha512": entry["sha512"],
            "revision": repository.get("commit")}


def active_processes(directory):
    """Do not release the next profile while any private-component consumer survives."""
    selected = []
    for path in Path("/proc").glob("[0-9]*/cmdline"):
        try:
            if int(path.parent.name) == os.getpid():
                continue
            if (str(directory).encode() in path.read_bytes()
                    or b"TMPDIR=" + os.fsencode(directory) + b"\0" in (path.parent / "environ").read_bytes()):
                selected.append(int(path.parent.name))
        except (FileNotFoundError, ProcessLookupError, PermissionError):
            continue
    return selected


def private_containers(directory):
    """Inspect only identities; no raw Docker configuration crosses private custody."""
    identifiers = command("docker", "ps", "--all", "--quiet", "--no-trunc").splitlines()
    if len(set(identifiers)) != len(identifiers) or any(re.fullmatch(r"[0-9a-f]{64}", item) is None for item in identifiers):
        raise ValueError("The topology shutdown container inventory is invalid.")
    if not identifiers:
        return []
    records = json.loads(command("docker", "inspect", *identifiers))
    if (not isinstance(records, list) or any(not isinstance(item, dict) for item in records)
            or len(records) != len(identifiers) or {item.get("Id") for item in records} != set(identifiers)):
        raise ValueError("The topology shutdown container identities changed.")
    owned = []
    for record in records:
        mounts = record.get("Mounts")
        if not isinstance(mounts, list) or any(not isinstance(mount, dict) for mount in mounts):
            raise ValueError("The topology shutdown mount inventory is invalid.")
        if any(isinstance(mount.get("Source"), str) and Path(mount["Source"]).is_absolute()
               and Path(os.path.abspath(mount["Source"])).is_relative_to(directory) for mount in mounts):
            owned.append(record["Id"])
    return owned


def finish(path, temporary, identity):
    receipt = document(path, temporary)
    private = private_directory(receipt["privateDirectory"], temporary)
    active_path = temporary / "tenants-catalog-active.json"
    active = document(active_path, temporary)
    if (receipt.get("schema") != "tenants.catalog-profile.v1" or receipt.get("identity") != identity
            or active != {"receipt": str(path), "sha256": digest(path)}):
        raise ValueError("The active catalog profile identity changed.")
    shutdown = document(private / "shutdown.json", temporary)
    if shutdown != {"provisioningSha256": digest(path), "disposed": True}:
        raise ValueError("The topology did not complete its awaited shutdown.")
    for name, expected in receipt["components"].items():
        if digest(private / "components" / name) != expected:
            raise ValueError("The private component set changed during qualification.")
    if active_processes(private):
        raise ValueError("The previous profile still has running component consumers.")
    if private_containers(private):
        raise ValueError("The previous profile still has private-custody container resources.")
    # Database reservations and receipts remain until job cleanup. A completed
    # profile's populated state is never reset or reused by another profile.
    active_path.unlink()
    shutil.rmtree(private_directory(private, temporary))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sdk-source-sha")
    parser.add_argument("--profile")
    parser.add_argument("--redis-db")
    parser.add_argument("--worker-backed-responses", choices=("true", "false"), default="false")
    parser.add_argument("--output", type=Path)
    parser.add_argument("--finish", type=Path)
    args = parser.parse_args()
    identity = job_identity()
    temporary = Path(os.environ["RUNNER_TEMP"])
    if not temporary.is_absolute() or temporary.resolve() != temporary or not temporary.is_dir():
        raise ValueError("The catalog profile requires a real absolute RUNNER_TEMP.")
    if args.finish:
        finish(args.finish, temporary, identity)
        return
    if (not args.output or not args.sdk_source_sha or not re.fullmatch(r"[0-9a-f]{40}", args.sdk_source_sha)
            or not args.profile or not re.fullmatch(r"[a-z][a-z0-9-]{0,63}", args.profile)
            or not args.redis_db or not re.fullmatch(r"[1-9]|1[0-5]", args.redis_db)):
        raise ValueError("A named profile and a canonical unused Redis database 1 through 15 are required; DB0 is reserved.")
    producer = os.environ.get("TENANTS_SOURCE_SDK_PRODUCER_VERSION", "")
    if not re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+(?:[.+-][0-9A-Za-z.-]+)?", producer):
        raise ValueError("The exact selected package producer identity is required.")
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
    if (not args.output.is_absolute() or args.output.parent.resolve() != args.output.parent
            or not args.output.is_relative_to(temporary)):
        raise ValueError("Profile evidence must be beneath RUNNER_TEMP.")
    producer_package = package_producer(root, producer)
    receipt_path, backend, volumes = initialization(temporary, identity)
    active = temporary / "tenants-catalog-active.json"
    reservation = temporary / f"tenants-catalog-database-{args.redis_db}.json"
    profile_reservation = temporary / f"tenants-catalog-profile-{args.profile}.json"
    if any(path.exists() or path.is_symlink() for path in (active, reservation, profile_reservation)):
        raise ValueError("A profile is still active or the database/profile was already consumed.")
    if command("docker", "exec", backend["Id"], "redis-cli", "-n", args.redis_db, "DBSIZE") != "0":
        raise ValueError("The selected business database is not empty; no replacement owner material is provisioned.")
    server_identity = re.findall(r"(?m)^run_id:([0-9a-f]{40})\r?$", command(
        "docker", "exec", backend["Id"], "redis-cli", "--raw", "INFO", "server"))
    if len(server_identity) != 1:
        raise ValueError("The actual Redis server identity is unavailable.")
    # Reservation is exclusive and retained even on interruption, so a failed
    # startup cannot accidentally make a used business database available again.
    write_exclusive(reservation, {"identity": identity, "profile": args.profile,
                                  "database": int(args.redis_db), "initializationSha256": digest(receipt_path)})
    write_exclusive(profile_reservation, {"database": int(args.redis_db)})
    private = Path(tempfile.mkdtemp(prefix="tenants-source-catalog-", dir=temporary))
    private.chmod(0o700)
    try:
        selected_components, hashes = components(root, private, args.redis_db)
        evidence = {"schema": "tenants.catalog-profile.v1", "identity": identity,
                    "profile": args.profile, "database": int(args.redis_db),
                    "endpoint": "127.0.0.1:6379", "privateDirectory": str(private),
                    "initializationReceipt": str(receipt_path), "initializationSha256": digest(receipt_path),
                    "sdkSource": args.sdk_source_sha, "sdkProducerVersion": producer, "sdkPackageProducer": producer_package,
                    "buildsSource": command("git", "rev-parse", "HEAD:references/Hexalith.Builds"),
                    "components": hashes,
                    "backend": {"containerId": backend["Id"], "imageId": backend["Image"],
                                "created": backend["Created"], "volumes": volumes, "initialDatabaseSize": 0, "redisRunId": server_identity[0]},
                    "materialProvisionedWithFreshBackend": True}
        write_exclusive(args.output, evidence)
        write_exclusive(active, {"receipt": str(args.output), "sha256": digest(args.output)})
        ring = base64.b64encode(secrets.token_bytes(32)).decode()
        signer = secrets.token_hex(32)
        values = {
            "TENANTS_SOURCE_CATALOG_PROFILE": "true",
            "TENANTS_SOURCE_WORKER_BACKED_RESPONSES_ENABLED": args.worker_backed_responses,
            "TENANTS_SOURCE_CATALOG_REDIS_DATABASE": args.redis_db,
            "TENANTS_SOURCE_CATALOG_REDIS_ENDPOINT": "127.0.0.1:6379",
            "TENANTS_SOURCE_CATALOG_PROVISIONING_RECEIPT": str(args.output),
            "TENANTS_SOURCE_CATALOG_PROVISIONING_SHA256": digest(args.output),
            "TENANTS_SOURCE_CATALOG_SHUTDOWN_PATH": str(private / "shutdown.json"),
            "Dapr__Mtls__CertificateDirectory": str(private / "dapr-certificates"),
            "TENANTS_SOURCE_SDK_SHA": args.sdk_source_sha,
            "TENANTS_SOURCE_SDK_PRODUCER_REVISION": producer_package["revision"],
            "Dapr__StateStoreComponentPath": str(selected_components / "statestore.yaml"),
            "Dapr__PubSubComponentPath": str(selected_components / "pubsub.yaml"),
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
        for category in ("Hexalith.EventStore.ServiceDefaults.Routing.TechnicalHttpRouteCatalogReceiver",
                         "Hexalith.EventStore.Server.Commands.CommandRouter", "Hexalith.EventStore.Server.Pipeline.SubmitCommandHandler"):
            values["Logging__LogLevel__" + category] = "Debug"
        # Keys are masked before they cross the environment boundary. No raw
        # diagnostics, private custody, or test results become public artifacts.
        for secret in (ring, signer):
            print("::add-mask::" + secret, flush=True)
        with open(os.environ["GITHUB_ENV"], "a", encoding="utf-8") as output:
            for key, value in values.items():
                output.write(f"{key}={value}\n")
    except BaseException:
        shutil.rmtree(private_directory(private, temporary))
        raise


if __name__ == "__main__":
    main()
