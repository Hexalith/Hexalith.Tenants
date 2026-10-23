#!/usr/bin/env python3
"""Freeze reviewed release inputs and compare NuGet payloads after repository signing."""
import argparse
import hashlib
import io
import json
from pathlib import Path, PurePosixPath
import stat
import subprocess
import sys
import zipfile


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def payload(data):
    """Read one unambiguous ZIP, retaining every entry except its root signature."""
    result = {}
    names = set()
    with zipfile.ZipFile(io.BytesIO(data)) as archive:
        for entry in archive.infolist():
            name = entry.filename
            path = PurePosixPath(name)
            key = name.casefold().rstrip("/")
            mode = entry.external_attr >> 16
            if (not name or "\\" in name or path.is_absolute() or ":" in name.split("/")[0]
                    or any(part in {"", ".", ".."} for part in name.rstrip("/").split("/"))
                    or key in names or entry.flag_bits & 1 or stat.S_IFMT(mode) not in (0, stat.S_IFREG, stat.S_IFDIR)):
                raise ValueError("Ambiguous or unsafe package archive entry")
            names.add(key)
            if name == ".signature.p7s":
                if entry.is_dir() or stat.S_IFMT(mode) not in (0, stat.S_IFREG) or entry.compress_type != zipfile.ZIP_STORED:
                    raise ValueError("Invalid package signature entry")
                continue
            if key == ".signature.p7s":
                raise ValueError("Ambiguous package signature entry")
            result[name] = archive.read(entry)
    return result


def compare(reviewed, destination, reviewed_sha256=None):
    original, observed = reviewed.read_bytes(), destination.read_bytes()
    if reviewed_sha256 is not None and sha256(original) != reviewed_sha256:
        raise ValueError("Reviewed package changed before its payload comparison")
    # Exact byte equality needs no signing normalization. ZIPs are still inspected
    # for ambiguity even when their complete byte hashes match.
    if original == observed and not zipfile.is_zipfile(io.BytesIO(original)):
        return
    original_payload, observed_payload = payload(original), payload(observed)
    if original_payload != observed_payload:
        raise ValueError("NuGet package does not match the reviewed package payload")
    # ZIP container differences are allowed only when NuGet.org added/updated its
    # root signature; other repacking is not repository signing evidence.
    with zipfile.ZipFile(io.BytesIO(observed)) as archive:
        signed = ".signature.p7s" in archive.namelist()
    if original != observed and not signed:
        raise ValueError("Changed package has no repository signing entry")
    if signed:
        log = destination.with_name(destination.name + ".signature-verification.log")
        with log.open("w") as diagnostics:
            try:
                result = subprocess.run(["dotnet", "nuget", "verify", "--all", str(destination)],
                                        stdout=diagnostics, stderr=subprocess.STDOUT, timeout=120, check=False)
            except (OSError, subprocess.TimeoutExpired) as error:
                diagnostics.write(f"Native signature verification unavailable: {error}\n")
                raise ValueError("Native NuGet signature verifier is unavailable or timed out") from error
        if result.returncode != 0:
            raise ValueError("Native NuGet signature verification failed; see retained diagnostics")
        if destination.read_bytes() != observed:
            raise ValueError("Destination package changed during native signature verification")


def freeze(manifest, receipt, directory):
    manifest, receipt = manifest.resolve(), receipt.resolve()
    manifest_bytes, receipt_bytes = manifest.read_bytes(), receipt.read_bytes()
    package_directory = Path("nupkgs").resolve()
    packages = sorted(package_directory.glob("*.nupkg"))
    document = {"manifest": {"path": str(manifest), "sha256": sha256(manifest_bytes)},
                "receipt": {"path": str(receipt), "sha256": sha256(receipt_bytes)}, "packages": {}}
    directory.mkdir(parents=True, exist_ok=True)
    (directory / "manifest.json").write_bytes(manifest_bytes)
    (directory / "receipt.json").write_bytes(receipt_bytes)
    hashes = []
    for package in packages:
        data = package.read_bytes()
        if zipfile.is_zipfile(io.BytesIO(data)):
            payload(data)
        document["packages"][package.name] = {"path": str(package), "sha256": sha256(data)}
        (directory / package.name).write_bytes(data)
        hashes.append(sha256(data) + " " + package.name + "\n")
    document["package_directory"] = str(package_directory)
    (directory / "reviewed-sha256").write_text("".join(hashes))
    (directory / "inputs.json").write_text(json.dumps(document, sort_keys=True))


def check(directory):
    document = json.loads((directory / "inputs.json").read_text())
    current_names = {path.name for path in Path(document["package_directory"]).glob("*.nupkg")}
    if current_names != set(document["packages"]):
        raise ValueError("Reviewed package inventory changed during destination verification")
    for item in [document["manifest"], document["receipt"], *document["packages"].values()]:
        if sha256(Path(item["path"]).read_bytes()) != item["sha256"]:
            raise ValueError("Reviewed package, manifest or receipt changed during destination verification")
    # The retained comparison inputs must still be the exact snapshot that was frozen.
    for name, item in document["packages"].items():
        if sha256((directory / name).read_bytes()) != item["sha256"]:
            raise ValueError("Frozen reviewed package changed during destination verification")
    for name in ("manifest", "receipt"):
        if sha256((directory / (name + ".json")).read_bytes()) != document[name]["sha256"]:
            raise ValueError("Frozen reviewed metadata changed during destination verification")
    expected_hashes = "".join(item["sha256"] + " " + name + "\n"
                              for name, item in sorted(document["packages"].items()))
    if (directory / "reviewed-sha256").read_text() != expected_hashes:
        raise ValueError("Frozen reviewed package hashes changed during destination verification")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest="operation", required=True)
    command = subparsers.add_parser("freeze")
    command.add_argument("manifest", type=Path)
    command.add_argument("receipt", type=Path)
    command.add_argument("directory", type=Path)
    command = subparsers.add_parser("check")
    command.add_argument("directory", type=Path)
    command = subparsers.add_parser("compare")
    command.add_argument("reviewed", type=Path)
    command.add_argument("destination", type=Path)
    command.add_argument("--reviewed-sha256")
    arguments = parser.parse_args()
    if arguments.operation == "freeze": freeze(arguments.manifest, arguments.receipt, arguments.directory)
    elif arguments.operation == "check": check(arguments.directory)
    else: compare(arguments.reviewed, arguments.destination, arguments.reviewed_sha256)


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, zipfile.BadZipFile, RuntimeError) as error:
        print(f"[release-package-content] {error}", file=sys.stderr)
        sys.exit(1)
