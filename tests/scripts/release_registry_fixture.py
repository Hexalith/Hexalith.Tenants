"""Isolated Registry HTTP transport; production OCI graph validation stays unchanged."""
import hashlib
import io
import json
import os
from pathlib import Path
import urllib.error
import urllib.parse
import urllib.request

INDEX = "application/vnd.oci.image.index.v1+json"
MANIFEST = "application/vnd.oci.image.manifest.v1+json"
CONFIG = "application/vnd.oci.image.config.v1+json"


def graph(state):
    """Build an immutable fixture graph and optionally corrupt one observation."""
    objects = {}
    def store(document, media_type):
        body = json.dumps(document, separators=(",", ":")).encode()
        digest = "sha256:" + hashlib.sha256(body).hexdigest()
        objects[digest] = (body, media_type, digest)
        return {"digest": digest, "size": len(body), "mediaType": media_type}
    children = []
    for architecture in ("amd64", "arm64"):
        source = state.get("oci_source_sha", "a" * 40)
        repository = "https://github.com/fixture/recovery"
        version = state.get("version", "1.2.3")
        labels = {
            "org.opencontainers.image.source": repository,
            "org.opencontainers.image.url": repository + "/releases/tag/v" + version,
            "org.opencontainers.image.documentation": repository + "/blob/" + source + "/README.md",
            "org.opencontainers.image.revision": source,
            "org.opencontainers.image.version": version,
        }
        mutation = state.get("oci_mutation")
        if mutation == "label" and architecture == "arm64":
            labels["org.opencontainers.image.version"] = "9.9.9"
        config = store({"os": "linux", "architecture": architecture, "config": {"Labels": labels},
                        "generation": state.get("oci_generation", 0)}, CONFIG)
        child = store({"schemaVersion": 2, "mediaType": MANIFEST, "config": config, "layers": []}, MANIFEST)
        child["platform"] = {"os": "linux", "architecture": architecture}
        if mutation == "platform" and architecture == "arm64": child["platform"]["architecture"] = "s390x"
        if mutation == "size" and architecture == "arm64": child["size"] += 1
        children.append(child)
    index = store({"schemaVersion": 2, "mediaType": INDEX, "manifests": children}, INDEX)
    return index["digest"], objects


class Response(io.BytesIO):
    def __init__(self, body, media_type, digest):
        super().__init__(body)
        self.headers = {"Content-Type": media_type, "Docker-Content-Digest": digest}


class FixtureOpener:
    def open(self, request, timeout=None):
        root = Path(os.environ["RECOVERY_FIXTURE"])
        state = json.loads((root / "destinations.json").read_text())
        url = urllib.parse.urlsplit(request.full_url)
        if url.netloc != "fixture.invalid" or not url.path.startswith("/v2/tenants/"):
            raise AssertionError("Unexpected network request: " + request.full_url)
        reference = urllib.parse.unquote(url.path.rsplit("/", 1)[1])
        with (root / "calls.jsonl").open("a") as log:
            log.write(json.dumps(["oci", reference, request.get_header("Accept")]) + "\n")
        if not state["container"] or state.get("oci_mutation") == "unresolved":
            raise urllib.error.URLError("isolated missing object")
        if state.get("oci_mutation") == "negotiation" or INDEX not in request.get_header("Accept") and "/manifests/" in url.path:
            raise urllib.error.URLError("isolated negotiation failure")
        if reference == state.get("version", "1.2.3"):
            if state.get("mutate_main_on_oci"):
                state["live_main_sha"] = "b" * 40
                (root / "destinations.json").write_text(json.dumps(state))
            if state.get("mutate_state_on_oci"):
                (root / "state/container-state").write_text("absent\n")
            if state.get("mutate_manifest_on_oci"):
                path = Path(os.environ["HEXALITH_RELEASE_PACKAGE_MANIFEST"])
                path.write_text(path.read_text() + "\n")
            if state.get("mutate_receipt_on_oci"):
                path = root / ".hexalith/release-evidence/1.2.3/tenants/oci-validation.json"
                path.write_text(path.read_text() + "\n")
        digest, objects = graph(state)
        if reference == state.get("version", "1.2.3"): reference = digest
        body, media_type, reported_digest = objects[reference]
        if state.get("oci_mutation") == "digest" and reference == digest: reported_digest = "sha256:" + "0" * 64
        return Response(body, media_type, reported_digest)


def install():
    """Replace only the network transport in temporary fixture subprocesses."""
    urllib.request.build_opener = lambda *args: FixtureOpener()


if __name__ == "__main__":
    # Metadata-only fixtures used by focused C# source/classification tests. Every
    # observed graph still goes through the real Builds validator before writes.
    import sys
    root, version = Path(sys.argv[1]), sys.argv[2]
    state = {"container": True, "version": version}
    (root / "destinations.json").write_text(json.dumps(state))
    (root / "sitecustomize.py").write_text("from release_registry_fixture import install\ninstall()\n")
    if sys.argv[3:] == ["no-receipt"]:
        sys.exit(0)
    directory = root / "state/oci-validated"
    directory.mkdir(parents=True, exist_ok=True)
    digest, _ = graph(state)
    (directory / "oci-validation.json").write_text(json.dumps({
        "result": "pass", "image": "fixture.invalid/tenants:" + version,
        "platforms": ["linux/amd64", "linux/arm64"], "index_digest": digest,
    }))
