#!/usr/bin/env python3
"""Exercise normal-publication verification with real packages and OCI validation."""
import base64
import io
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import unittest
import zipfile

import test_partial_release_recovery as recovery


class ReleaseArtifactVerificationTests(unittest.TestCase):
    def setUp(self):
        self.fixture = recovery.PartialReleaseRecoveryTests()
        self.fixture.setUp()
        self.addCleanup(self.fixture.doCleanups)
        fixture = self.fixture
        fixture.destinations.update(packages=list(fixture.ids), container=True)
        fixture.release()
        directory = fixture.root / "nupkgs"
        directory.mkdir()
        package_bytes = {}
        for package_id in fixture.ids:
            content = io.BytesIO()
            with zipfile.ZipFile(content, "w") as archive:
                archive.writestr(package_id + ".nuspec", '<package><metadata><id>' + package_id + '</id>'
                                 '<version>1.2.3</version><license>MIT</license><readme>README.md</readme>'
                                 '</metadata></package>')
                archive.writestr("README.md", package_id)
            raw = content.getvalue()
            (directory / (package_id + ".1.2.3.nupkg")).write_bytes(raw)
            package_bytes[package_id] = base64.b64encode(raw).decode()
        fixture.destinations["package_content_base64"] = package_bytes
        for asset, package_id in zip(fixture.destinations["release"]["assets"], fixture.ids):
            asset["base64"] = package_bytes[package_id]
        fixture.save()
        result = subprocess.run(["bash", "-c", 'set -euo pipefail; version=1.2.3; repository=$GITHUB_REPOSITORY; '
                                 'source_sha=$GITHUB_SHA; source scripts/partial-release-recovery-common.sh; '
                                 'recovery_verify_container .hexalith/release-evidence/1.2.3/tenants'],
                                cwd=fixture.root, env=fixture.environment, capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

    def run_verifier(self, accepted=True):
        result = subprocess.run(["bash", str(recovery.ROOT / "scripts/verify-release-artifacts.sh")],
                                cwd=self.fixture.root, env=self.fixture.environment, capture_output=True, text=True)
        self.assertEqual(result.returncode == 0, accepted, result.stdout + result.stderr)
        self.fixture.assert_no_publication()
        return result

    def run_acquisition(self, budget=1_000_000, fixed_clock=""):
        shell = '''
          set -euo pipefail
          source "$1"
          fixture_clock="$4"
          if [ -n "$fixture_clock" ]; then
            recovery_monotonic_microseconds() { printf '%s\\n' "$fixture_clock"; }
          fi
          started="$(recovery_monotonic_microseconds)"
          recovery_download_nuget "$2" 1.2.3 acquisition.nupkg acquisition-readiness "$((started + $3))"
          printf '%s\\n' "$recovery_nuget_status"
        '''
        return subprocess.run(["bash", "-c", shell, "fixture",
                               str(recovery.ROOT / "scripts/partial-release-recovery-common.sh"),
                               self.fixture.ids[0], str(budget), fixed_clock], cwd=self.fixture.root,
                              env=self.fixture.environment, capture_output=True, text=True, timeout=5)

    def test_actual_prepared_version_and_all_destination_bytes_are_verified(self):
        self.run_verifier()
        evidence = self.fixture.root / ".hexalith/release-evidence/1.2.3/destinations"
        document = json.loads((evidence / "verified.json").read_text())
        self.assertEqual(document["version"], "1.2.3")
        self.assertEqual(document["source_sha"], self.fixture.environment["GITHUB_SHA"])
        self.assertEqual(len((evidence / "nuget-sha256").read_text().splitlines()), 5)
        self.assertEqual(len((evidence / "github-sha256").read_text().splitlines()), 5)
        observations = (evidence / "nuget-readiness").read_text().splitlines()
        self.assertEqual(len(observations), 5)
        self.assertTrue(all(line.startswith("200 ") and "https://api.nuget.org/v3-flatcontainer/" in line
                            for line in observations))
        self.assertFalse(any("https://www.nuget.org/api/v2/package/" in argument
                             for call in self.fixture.calls() for argument in call))

    def test_changed_nuget_bytes_reject_and_revoke_old_success(self):
        self.run_verifier()
        self.fixture.destinations["nuget_content"] = {self.fixture.ids[-1]: "unexpected"}
        self.fixture.save()
        self.run_verifier(False)
        self.assertFalse((self.fixture.root / ".hexalith/release-evidence/1.2.3/destinations/verified.json").exists())

    def test_missing_nuget_destination_is_rejected(self):
        self.fixture.destinations["packages"].pop()
        self.fixture.save()
        self.run_verifier(False)

    def test_missing_and_conflicting_github_assets_are_rejected(self):
        assets = self.fixture.destinations["release"]["assets"]
        removed = assets.pop()
        self.fixture.save()
        self.run_verifier(False)
        assets.append(removed)
        removed["base64"] = base64.b64encode(b"wrong bytes").decode()
        self.fixture.save()
        self.run_verifier(False)

    def test_missing_conflicting_and_ambiguous_publisher_receipts_are_rejected(self):
        receipt = self.fixture.root / ".hexalith/release-evidence/1.2.3/tenants/oci-validation.json"
        original = receipt.read_text()
        receipt.unlink()
        self.run_verifier(False)
        document = json.loads(original)
        document["image"] = "fixture.invalid/tenants:9.9.9"
        receipt.write_text(json.dumps(document))
        self.run_verifier(False)
        document = json.loads(original)
        document["index_digest"] = "sha256:" + "0" * 64
        receipt.write_text(json.dumps(document))
        self.run_verifier(False)
        other = self.fixture.root / ".hexalith/release-evidence/9.9.9/tenants"
        other.mkdir(parents=True)
        (other / "oci-validation.json").write_text(original)
        self.run_verifier(False)

    def nuget_archive(self, package_id, extra_entries=(), signature=True, replacement=None):
        reviewed = base64.b64decode(self.fixture.destinations["package_content_base64"][package_id])
        content = io.BytesIO()
        with zipfile.ZipFile(io.BytesIO(reviewed)) as source, zipfile.ZipFile(content, "w") as target:
            for entry in source.infolist(): target.writestr(entry, (replacement or {}).get(entry.filename, source.read(entry)))
            if signature: target.writestr(".signature.p7s", b"repository signature fixture")
            for name, data in extra_entries: target.writestr(name, data)
        self.fixture.destinations["package_content_base64"][package_id] = base64.b64encode(content.getvalue()).decode()
        self.fixture.save()
        return content.getvalue()

    def test_repository_signature_only_preserves_payload_but_retains_raw_hashes(self):
        package_id = self.fixture.ids[0]
        observed = self.nuget_archive(package_id)
        self.run_verifier()
        evidence = self.fixture.root / ".hexalith/release-evidence/1.2.3/destinations"
        self.assertEqual((evidence / "nuget" / (package_id + ".1.2.3.nupkg")).read_bytes(), observed)
        self.assertNotEqual((evidence / "nuget-sha256").read_text().split()[0],
                            (evidence / "reviewed/reviewed-sha256").read_text().split()[0])
        archive = evidence / "nuget" / (package_id + ".1.2.3.nupkg")
        self.assertIn(["dotnet", "nuget", "verify", "--all", str(archive)], self.fixture.calls())
        self.assertIn("native signature verifier fixture", archive.with_name(archive.name + ".signature-verification.log").read_text())

    def test_signed_destination_requires_successful_native_signature_verification(self):
        package_id = self.fixture.ids[0]
        self.nuget_archive(package_id)
        self.fixture.destinations["signature_verifier_exit"] = 1
        self.fixture.save()
        result = self.run_verifier(False)
        self.assertIn("Native NuGet signature verification failed", result.stderr)
        evidence = self.fixture.root / ".hexalith/release-evidence/1.2.3/destinations"
        self.assertFalse((evidence / "verified.json").exists())
        self.assertTrue((evidence / "nuget-sha256").read_text())
        log = evidence / "nuget" / (package_id + ".1.2.3.nupkg.signature-verification.log")
        self.assertIn("native signature verifier fixture", log.read_text())
        archive = evidence / "nuget" / (package_id + ".1.2.3.nupkg")
        same_bytes = subprocess.run([sys.executable, str(recovery.ROOT / "scripts/release-package-content.py"),
                                     "compare", str(archive), str(archive)], env=self.fixture.environment,
                                    capture_output=True, text=True)
        self.assertNotEqual(same_bytes.returncode, 0)
        self.assertIn("Native NuGet signature verification failed", same_bytes.stderr)

    def test_missing_native_signature_verifier_refuses_signed_archive(self):
        package_id = self.fixture.ids[0]
        signed = self.nuget_archive(package_id)
        destination = self.fixture.root / "signed.nupkg"
        destination.write_bytes(signed)
        reviewed = self.fixture.root / "nupkgs" / (package_id + ".1.2.3.nupkg")
        environment = dict(self.fixture.environment, PATH="")
        result = subprocess.run([sys.executable, str(recovery.ROOT / "scripts/release-package-content.py"),
                                 "compare", str(reviewed), str(destination)], env=environment,
                                capture_output=True, text=True)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("signature verifier is unavailable", result.stderr)
        self.assertIn("unavailable", destination.with_name(destination.name + ".signature-verification.log").read_text())

    def test_signature_cannot_hide_changed_or_ambiguous_payloads(self):
        package_id = self.fixture.ids[0]
        original = self.fixture.destinations["package_content_base64"][package_id]
        self.nuget_archive(package_id, replacement={"README.md": b"tampered bytes"})
        self.run_verifier(False)
        self.fixture.destinations["package_content_base64"][package_id] = original
        for entries in ((("unexpected.dll", b"tampered"),), (("README.md", b"tampered"),),
                        (("readme.md", b"ambiguous case"),), (("../outside", b"unsafe"),)):
            with self.subTest(entries=entries):
                package_id = self.fixture.ids[0]
                original = self.fixture.destinations["package_content_base64"][package_id]
                self.nuget_archive(package_id, entries)
                self.run_verifier(False)
                self.fixture.destinations["package_content_base64"][package_id] = original

    def test_reviewed_inputs_cannot_mutate_during_destination_reads(self):
        for mutation in ("mutate_package_on_download", "mutate_manifest_on_nuget", "mutate_receipt_on_oci"):
            with self.subTest(mutation=mutation):
                package = self.fixture.root / "nupkgs" / (self.fixture.ids[-1] + ".1.2.3.nupkg")
                manifest = self.fixture.manifest_path
                receipt = self.fixture.root / ".hexalith/release-evidence/1.2.3/tenants/oci-validation.json"
                originals = {path: path.read_bytes() for path in (package, manifest, receipt)}
                self.fixture.destinations[mutation] = True
                self.fixture.save()
                result = self.run_verifier(False)
                self.assertIn("changed", result.stderr)
                for path, raw in originals.items(): path.write_bytes(raw)
                del self.fixture.destinations[mutation]

    def test_nuget_and_github_cannot_match_different_mutable_reviewed_bytes(self):
        package_id = self.fixture.ids[0]
        self.fixture.destinations["mutate_package_on_nuget"] = True
        # A valid, differently signed package still has the original payload, but
        # replacing reviewed raw input during the read must revoke the attempt.
        self.nuget_archive(package_id)
        result = self.run_verifier(False)
        self.assertIn("changed", result.stderr)

    def test_overlapping_evidence_directory_refuses_before_modifying_reviewed_files(self):
        self.run_verifier()
        marker = self.fixture.root / ".hexalith/release-evidence/1.2.3/destinations/verified.json"
        prior_marker = marker.read_bytes()
        directory = self.fixture.root / "nupkgs"
        before = {path.name: path.read_bytes() for path in directory.iterdir()}
        self.fixture.environment["HEXALITH_RELEASE_DESTINATION_EVIDENCE_DIRECTORY"] = str(directory)
        result = self.run_verifier(False)
        self.assertIn("overlaps reviewed inputs", result.stderr)
        self.assertEqual({path.name: path.read_bytes() for path in directory.iterdir()}, before)
        self.assertEqual(marker.read_bytes(), prior_marker)

    def test_failed_receipt_or_version_admission_revokes_previous_success(self):
        self.run_verifier()
        marker = self.fixture.root / ".hexalith/release-evidence/1.2.3/destinations/verified.json"
        receipt = self.fixture.root / ".hexalith/release-evidence/1.2.3/tenants/oci-validation.json"
        raw = receipt.read_bytes()
        receipt.unlink()
        self.run_verifier(False)
        self.assertFalse(marker.exists())
        receipt.write_bytes(raw)
        self.run_verifier()
        package = self.fixture.root / "nupkgs" / (self.fixture.ids[0] + ".1.2.3.nupkg")
        package.write_bytes(b"unusable version evidence")
        self.run_verifier(False)
        self.assertFalse(marker.exists())

    def test_conflicting_github_bytes_do_not_replace_retained_nuget_bytes(self):
        package_id = self.fixture.ids[0]
        expected = base64.b64decode(self.fixture.destinations["package_content_base64"][package_id])
        self.fixture.destinations["release"]["assets"][0]["base64"] = base64.b64encode(b"conflicting github bytes").decode()
        self.fixture.save()
        self.run_verifier(False)
        evidence = self.fixture.root / ".hexalith/release-evidence/1.2.3/destinations"
        name = package_id + ".1.2.3.nupkg"
        self.assertEqual((evidence / "nuget" / name).read_bytes(), expected)
        self.assertEqual((evidence / "github" / name).read_bytes(), b"conflicting github bytes")

    def test_temporarily_unindexed_nuget_package_waits_until_available(self):
        self.fixture.environment["HEXALITH_RELEASE_NUGET_WAIT_SECONDS"] = "2"
        self.fixture.destinations["nuget_statuses"] = {self.fixture.ids[0]: [404, 404, 200]}
        self.fixture.save()
        self.run_verifier()
        observations = (self.fixture.root / ".hexalith/release-evidence/1.2.3/destinations/nuget-readiness").read_text()
        package_observations = [line.split() for line in observations.splitlines() if line.split()[1] == self.fixture.ids[0]]
        self.assertEqual([line[0] for line in package_observations], ["404", "404", "404", "404", "200"])
        self.assertEqual(["v3-flatcontainer" in line[2] for line in package_observations], [True, False, True, False, True])

    def test_nuget_readiness_exhaustion_is_bounded_and_retained(self):
        self.run_verifier()
        self.fixture.destinations["nuget_statuses"] = {self.fixture.ids[0]: [404]}
        self.fixture.destinations["packages"].remove(self.fixture.ids[0])
        self.fixture.save()
        result = self.run_verifier(False)
        self.assertIn("not ready within 0 seconds", result.stderr)
        evidence = self.fixture.root / ".hexalith/release-evidence/1.2.3/destinations"
        observations = (evidence / "nuget-readiness").read_text().splitlines()
        self.assertEqual([line.split()[0] for line in observations], ["404", "404"])
        self.assertIn("https://www.nuget.org/api/v2/package/", observations[1])
        self.assertFalse((evidence / "verified.json").exists())

    def test_v3_absence_falls_back_to_exact_signed_official_v2_bytes(self):
        package_id = self.fixture.ids[0]
        observed = self.nuget_archive(package_id)
        self.fixture.destinations["packages"].remove(package_id)
        self.fixture.destinations["nuget_v2_packages"] = [package_id]
        self.fixture.save()
        self.run_verifier()
        evidence = self.fixture.root / ".hexalith/release-evidence/1.2.3/destinations"
        self.assertEqual((evidence / "nuget" / (package_id + ".1.2.3.nupkg")).read_bytes(), observed)
        self.assertEqual((evidence / "nuget-readiness").read_text().splitlines()[:2], [
            f"404 {package_id} https://api.nuget.org/v3-flatcontainer/{package_id.lower()}/1.2.3/{package_id.lower()}.1.2.3.nupkg",
            f"200 {package_id} https://www.nuget.org/api/v2/package/{package_id}/1.2.3"])
        archive = evidence / "nuget" / (package_id + ".1.2.3.nupkg")
        self.assertIn(["dotnet", "nuget", "verify", "--all", str(archive)], self.fixture.calls())
        self.assertTrue((evidence / "verified.json").is_file())

    def test_wrong_v2_content_refuses_without_retry_or_old_success(self):
        self.run_verifier()
        package_id = self.fixture.ids[0]
        self.fixture.destinations["packages"].remove(package_id)
        self.fixture.destinations["nuget_v2_packages"] = [package_id]
        self.fixture.destinations["nuget_v2_content"] = {package_id: "wrong archive"}
        self.fixture.save()
        (self.fixture.root / "calls.jsonl").write_text("")
        result = self.run_verifier(False)
        self.assertIn("does not match the reviewed package", result.stderr)
        calls = [call for call in self.fixture.calls() if call[0] == "curl"]
        self.assertEqual(len(calls), 2)
        evidence = self.fixture.root / ".hexalith/release-evidence/1.2.3/destinations"
        self.assertEqual((evidence / "nuget" / (package_id + ".1.2.3.nupkg")).read_bytes(), b"wrong archive")
        self.assertFalse((evidence / "verified.json").exists())

    def test_v3_authentication_error_or_wrong_success_never_uses_v2(self):
        package_id = self.fixture.ids[0]
        self.fixture.destinations["nuget_v2_packages"] = [package_id]
        for status in (401, 200):
            with self.subTest(status=status):
                self.fixture.destinations["nuget_statuses"] = {package_id: [status]}
                self.fixture.destinations["nuget_content"] = {package_id: "wrong v3 content"}
                self.fixture.save()
                (self.fixture.root / "calls.jsonl").write_text("")
                result = self.run_verifier(False)
                expected = "unexpected status 401" if status == 401 else "does not match the reviewed package"
                self.assertIn(expected, result.stderr)
                calls = [call for call in self.fixture.calls() if call[0] == "curl"]
                self.assertEqual(len(calls), 1)
                self.assertIn("https://api.nuget.org/v3-flatcontainer/", calls[0][-1])
                self.assertFalse((self.fixture.root / ".hexalith/release-evidence/1.2.3/destinations/verified.json").exists())

    def test_fallback_requests_share_one_deadline_and_remaining_timeout(self):
        package_id = self.fixture.ids[0]
        self.fixture.environment["HEXALITH_RELEASE_NUGET_WAIT_SECONDS"] = "1"
        self.fixture.destinations["packages"].remove(package_id)
        self.fixture.destinations["nuget_v2_packages"] = [package_id]
        self.fixture.destinations["nuget_delays"] = {"v3": 0.35, "v2": 1}
        self.fixture.save()
        # Time only the controlled acquisition, excluding frozen-input admission
        # and the other destination checks performed by the full verifier.
        started = time.monotonic()
        acquisition = self.run_acquisition()
        self.assertEqual(acquisition.returncode, 0, acquisition.stderr)
        self.assertEqual(acquisition.stdout.strip(), "transport-failure")
        self.assertLess(time.monotonic() - started, 3)
        (self.fixture.root / "calls.jsonl").write_text("")
        result = self.run_verifier(False)
        self.assertIn("not ready within 1 seconds", result.stderr)
        calls = [call for call in self.fixture.calls() if call[0] == "curl"]
        self.assertEqual(len(calls), 2)
        timeouts = [float(call[call.index("--max-time") + 1]) for call in calls]
        self.assertGreater(timeouts[0], 0)
        self.assertLessEqual(timeouts[0], 1)
        self.assertGreater(timeouts[1], 0)
        self.assertLess(timeouts[1], timeouts[0] - 0.3)
        evidence = self.fixture.root / ".hexalith/release-evidence/1.2.3/destinations"
        self.assertEqual([line.split()[0] for line in (evidence / "nuget-readiness").read_text().splitlines()],
                         ["404", "transport-failure"])
        self.assertFalse((evidence / "verified.json").exists())

    def test_submillisecond_remaining_budget_never_starts_unlimited_curl(self):
        for remaining in (0, 1, 999, 1000):
            with self.subTest(remaining_microseconds=remaining):
                (self.fixture.root / "calls.jsonl").write_text("")
                result = self.run_acquisition(remaining, fixed_clock="0")
                self.assertEqual(result.returncode, 0, result.stderr)
                calls = [call for call in self.fixture.calls() if call[0] == "curl"]
                if remaining < 1000:
                    self.assertEqual(result.stdout.strip(), "deadline-exhausted")
                    self.assertEqual(calls, [])
                else:
                    self.assertEqual(result.stdout.strip(), "200")
                    self.assertEqual(len(calls), 1)
                    self.assertEqual(calls[0][calls[0].index("--max-time") + 1], "0.001000")

    def test_leading_zero_decimal_waits_still_verify_every_required_package(self):
        self.fixture.environment.update(HEXALITH_RELEASE_NUGET_WAIT_SECONDS="0010",
                                        HEXALITH_RELEASE_NUGET_WAIT_INTERVAL="08")
        self.fixture.destinations["nuget_statuses"] = {self.fixture.ids[0]: [404, 200]}
        self.fixture.save()
        # Record the parsed interval without spending eight seconds asleep.
        sleep = self.fixture.root / "bin/sleep"
        sleep.write_text(f"#!{sys.executable}\nfrom pathlib import Path\nimport sys\n"
                         "Path('sleep-interval').write_text(sys.argv[1])\n")
        sleep.chmod(0o755)
        self.run_verifier()
        self.assertEqual((self.fixture.root / "sleep-interval").read_text(), "8.000000")
        evidence = self.fixture.root / ".hexalith/release-evidence/1.2.3/destinations"
        self.assertEqual([line.split()[1] for line in (evidence / "nuget-sha256").read_text().splitlines()],
                         self.fixture.ids)
        calls = [call for call in self.fixture.calls() if call[0] == "curl"]
        first_timeout = float(calls[0][calls[0].index("--max-time") + 1])
        self.assertGreater(first_timeout, 8)
        self.assertLessEqual(first_timeout, 10)
        self.assertTrue((evidence / "verified.json").is_file())

    def test_leading_zero_interval_cannot_hide_failed_acquisition(self):
        self.run_verifier()
        self.fixture.environment.update(HEXALITH_RELEASE_NUGET_WAIT_SECONDS="0001",
                                        HEXALITH_RELEASE_NUGET_WAIT_INTERVAL="08")
        self.fixture.destinations["packages"].remove(self.fixture.ids[0])
        self.fixture.save()
        result = self.run_verifier(False)
        self.assertIn("not ready within 1 seconds", result.stderr)
        evidence = self.fixture.root / ".hexalith/release-evidence/1.2.3/destinations"
        self.assertEqual((evidence / "nuget-sha256").read_text(), "")
        self.assertFalse((evidence / "verified.json").exists())

    def test_leading_zero_waits_preserve_the_existing_public_maxima(self):
        for seconds, interval in (("01801", "00"), ("00", "061")):
            with self.subTest(seconds=seconds, interval=interval):
                self.fixture.environment.update(HEXALITH_RELEASE_NUGET_WAIT_SECONDS=seconds,
                                                HEXALITH_RELEASE_NUGET_WAIT_INTERVAL=interval)
                result = self.run_verifier(False)
                self.assertIn("exceeds its bounded limits", result.stderr)
                self.assertFalse(any(call[0] == "curl" for call in self.fixture.calls()))
                self.assertFalse((self.fixture.root / ".hexalith/release-evidence/1.2.3/destinations/verified.json").exists())

    def test_wait_budget_is_shared_across_the_entire_package_cohort(self):
        self.fixture.environment["HEXALITH_RELEASE_NUGET_WAIT_SECONDS"] = "1"
        clock = self.fixture.set_monotonic_clock([0, 0, 1_000_000, 1_000_000])
        result = self.run_verifier(False)
        self.assertIn(f"NuGet package {self.fixture.ids[1]} was not ready", result.stderr)
        self.assertEqual(json.loads(clock.read_text()), [])
        self.assertEqual(len([call for call in self.fixture.calls() if call[0] == "curl"]), 1)
        evidence = self.fixture.root / ".hexalith/release-evidence/1.2.3/destinations"
        self.assertEqual([line.split()[1] for line in (evidence / "nuget-sha256").read_text().splitlines()],
                         self.fixture.ids[:1])
        self.assertFalse((evidence / "verified.json").exists())

    def test_live_oci_digest_provenance_and_platform_drift_are_rejected(self):
        for key, value in (("oci_generation", 1), ("oci_mutation", "label"), ("oci_mutation", "platform"),
                           ("oci_mutation", "negotiation"), ("oci_source_sha", "b" * 40)):
            with self.subTest(key=key, value=value):
                self.fixture.destinations[key] = value
                self.fixture.save()
                self.run_verifier(False)
                del self.fixture.destinations[key]

    def test_wrong_source_tag_is_rejected(self):
        self.fixture.destinations["tag_sha"] = "b" * 40
        self.fixture.save()
        result = self.run_verifier(False)
        self.assertIn("Release tag does not resolve to the reviewed source", result.stderr)
        self.assertIn("[oci-registry-validator] pass", result.stdout)

    def test_extra_or_missing_reviewed_package_is_rejected(self):
        package = self.fixture.root / "nupkgs" / (self.fixture.ids[-1] + ".1.2.3.nupkg")
        package.rename(package.with_name("Unexpected.1.2.3.nupkg"))
        self.run_verifier(False)

    def test_workspace_escaping_destination_evidence_is_rejected(self):
        self.fixture.environment["HEXALITH_RELEASE_DESTINATION_EVIDENCE_DIRECTORY"] = "/tmp/outside-release-evidence"
        self.run_verifier(False)

    def test_symlinked_evidence_file_cannot_write_outside_workspace(self):
        with tempfile.TemporaryDirectory() as outside:
            target = Path(outside) / "preserved"
            target.write_text("preserved")
            evidence = self.fixture.root / "destinations"
            evidence.mkdir()
            (evidence / "nuget-sha256").symlink_to(target)
            self.fixture.environment["HEXALITH_RELEASE_DESTINATION_EVIDENCE_DIRECTORY"] = str(evidence)
            self.run_verifier(False)
            self.assertEqual(target.read_text(), "preserved")


if __name__ == "__main__": unittest.main()
