#!/usr/bin/env python3
"""Qualify framework and version boundaries using the executable package validator."""
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
import zipfile

ROOT = Path(__file__).resolve().parents[2]


class NuGetDependencyRangeTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        (self.root / "src/obj").mkdir(parents=True)
        (self.root / "src/Fixture.csproj").write_text("<Project />")
        (self.root / "packages").mkdir()
        self.manifest = self.root / "release-packages.json"
        self.package_id = "Fixture"
        self.manifest.write_text(json.dumps({"packages": [{"id": self.package_id, "project": "src/Fixture.csproj"}]}))
        self.assets = {
            "projectFileDependencyGroups": {"net10.0": ["Fixture.Direct >= 1.0.0"]},
            "centralTransitiveDependencyGroups": {"net10.0": {"Fixture.Central": {"version": "[2.0.0, )"}}},
            "project": {"restore": {"projectPath": str(self.root / "src/Fixture.csproj"), "projectName": "Fixture", "projectUniqueName": str(self.root / "src/Fixture.csproj"),
                                     "projectStyle": "PackageReference", "originalTargetFrameworks": ["net10.0"]},
                        "frameworks": {"net10.0": {}}},
        }

    def run_validator(self, groups, accepted=True):
        (self.root / "src/obj/project.assets.json").write_text(json.dumps(self.assets))
        package = self.root / "packages/Fixture.1.2.3.nupkg"
        with zipfile.ZipFile(package, "w") as archive:
            archive.writestr("Fixture.nuspec", f'<package><metadata><id>{self.package_id}</id><version>1.2.3</version>'
                             '<readme>README.md</readme><license>MIT</license>' + groups + '</metadata></package>')
            archive.writestr("README.md", "fixture")
        result = subprocess.run(["python3", str(ROOT / "scripts/validate-nuget-packages.py"),
                                 str(package.parent), "--manifest", str(self.manifest)], text=True, capture_output=True)
        self.assertEqual(result.returncode == 0, accepted, result.stdout + result.stderr)
        return result

    @staticmethod
    def group(framework="net10.0", direct="1.0.0", central="2.0.0"):
        return f'<dependencies><group targetFramework="{framework}"><dependency id="Fixture.Direct" version="{direct}" />' \
               f'<dependency id="Fixture.Central" version="{central}" /></group></dependencies>'

    def test_equivalent_minimum_ranges_are_accepted(self):
        for minimum in ("1.0.0", ">= 1.0.0", "[1.0.0, )"):
            with self.subTest(minimum=minimum): self.run_validator(self.group(direct=minimum))
        self.run_validator(self.group(framework=".NETCoreApp10.0"))

    def test_equivalent_nuget_bound_versions_preserve_exact_range_semantics(self):
        for restored, observed in (("1.0", "1.0.0"), ("1.0.0.0", "1.0"),
                                   ("1.0.0-RC.1", "1.0.0-rc.1"), ("[1.0]", "[1.0.0.0]"),
                                   ("[1.0-RC.1, 2.0)", "[1.0.0-rc.1, 2.0.0.0)"),
                                   ("[1.0, 2.0.0.0)", "[1.0.0.0, 2.0)")):
            with self.subTest(restored=restored, observed=observed):
                self.assets["projectFileDependencyGroups"]["net10.0"] = ["Fixture.Direct " + restored]
                self.run_validator(self.group(direct=observed))
                self.run_validator(self.group(direct="9.0.0"), False)
        self.assets["projectFileDependencyGroups"]["net10.0"] = ["Fixture.Direct >= 1.0"]
        self.run_validator(self.group(direct="[1.0.0.0]"), False)
        self.run_validator(self.group(direct="1.0.0.1"), False)

    def test_supported_framework_long_and_short_names_are_equivalent(self):
        for short, long in (("netcoreapp3.1", ".NETCoreApp3.1"), ("netstandard2.0", ".NETStandard2.0"),
                             ("net10.0", ".NETCoreApp,Version=v10.0")):
            with self.subTest(short=short):
                for section in ("projectFileDependencyGroups", "centralTransitiveDependencyGroups"):
                    self.assets[section] = {short: next(iter(self.assets[section].values()))}
                self.assets["project"]["frameworks"] = {short: {}}
                self.assets["project"]["restore"]["originalTargetFrameworks"] = [short]
                self.run_validator(self.group(framework=long))
                self.run_validator(self.group(framework=short))
                self.run_validator(self.group(framework="net9.0"), False)

    def test_wrong_exact_missing_and_malformed_ranges_are_rejected(self):
        for version in ("9.0.0", "[1.0.0]", "(1.0.0,)", "", "1.*", "[1.0.0,", "(,)", "[2.0.0, 1.0.0]"):
            with self.subTest(version=version): self.run_validator(self.group(direct=version), False)
        self.run_validator(self.group(central="9.0.0"), False)

    def test_wrong_missing_and_duplicate_frameworks_are_rejected(self):
        self.run_validator(self.group(framework="net9.0"), False)
        self.run_validator(self.group().replace(' targetFramework="net10.0"', ''), False)
        group = self.group().removeprefix('<dependencies>').removesuffix('</dependencies>')
        self.run_validator('<dependencies>' + group + group + '</dependencies>', False)
        self.run_validator(self.group().replace('</group>', '<dependency id="fixture.direct" version="1.0.0" /></group>'), False)

    def test_framework_dependency_swaps_are_rejected(self):
        self.assets["projectFileDependencyGroups"]["net9.0"] = ["Fixture.Direct >= 3.0.0"]
        self.assets["centralTransitiveDependencyGroups"]["net9.0"] = {"Fixture.Central": {"version": "4.0.0"}}
        self.assets["project"]["frameworks"]["net9.0"] = {}
        self.assets["project"]["restore"]["originalTargetFrameworks"].append("net9.0")
        def groups(first, second):
            return '<dependencies>' + self.group("net10.0", *first)[14:-15] + self.group("net9.0", *second)[14:-15] + '</dependencies>'
        self.run_validator(groups(("1.0.0", "2.0.0"), ("3.0.0", "4.0.0")))
        self.run_validator(groups(("3.0.0", "4.0.0"), ("1.0.0", "2.0.0")), False)

    def test_technical_exceptions_obey_ordinary_range_checks(self):
        self.package_id = "Hexalith.Tenants.Server"
        self.assets["project"]["restore"]["projectName"] = self.package_id
        self.manifest.write_text(json.dumps({"packages": [{"id": self.package_id, "project": "src/Fixture.csproj"}]}))
        for dependency_id in ("Hexalith.EventStore.ServiceDefaults", "Hexalith.Commons.ServiceDefaults"):
            with self.subTest(dependency_id=dependency_id):
                self.assets["projectFileDependencyGroups"]["net10.0"] = [dependency_id + " >= 1.0.0"]
                self.assets["centralTransitiveDependencyGroups"]["net10.0"] = {}
                group = '<dependencies><group targetFramework="net10.0"><dependency id="' + dependency_id + '" version="{}" /></group></dependencies>'
                self.run_validator(group.format("1.0.0"))
                self.run_validator(group.format("9.0.0"), False)
                self.run_validator(group.format("1.0.0").replace('targetFramework="net10.0"', 'targetFramework="net9.0"'), False)

    def test_technical_defaults_are_refused_for_every_other_package_layer(self):
        for package_id in ("Hexalith.Tenants.Contracts", "Hexalith.Tenants.Client", "Hexalith.Tenants.Testing",
                           "Hexalith.Tenants.Aspire", "Unknown.Package"):
            for dependency_id in ("Hexalith.EventStore.ServiceDefaults", "Hexalith.Commons.ServiceDefaults",
                                  "Other.ServiceDefaults", "Hexalith.EventStore.ServiceDefaults.Extra"):
                with self.subTest(package=package_id, dependency=dependency_id):
                    self.package_id = package_id
                    self.assets["project"]["restore"]["projectName"] = package_id
                    self.manifest.write_text(json.dumps({"packages": [{"id": package_id, "project": "src/Fixture.csproj"}]}))
                    self.assets["projectFileDependencyGroups"]["net10.0"] = [dependency_id + " >= 1.0.0"]
                    self.assets["centralTransitiveDependencyGroups"]["net10.0"] = {}
                    group = f'<dependencies><group targetFramework="net10.0"><dependency id="{dependency_id}" version="1.0.0" /></group></dependencies>'
                    result = self.run_validator(group, False)
                    self.assertIn("forbidden projects", result.stdout + result.stderr)

    def test_unusable_central_evidence_is_rejected(self):
        for details in ({}, {"version": ""}, {"version": "1.*"}, {"version": None}):
            with self.subTest(details=details):
                self.assets["centralTransitiveDependencyGroups"]["net10.0"]["Fixture.Central"] = details
                self.run_validator(self.group(), False)

    def test_duplicate_direct_central_evidence_is_rejected(self):
        self.assets["centralTransitiveDependencyGroups"]["net10.0"]["Fixture.Direct"] = {"version": "1.0.0"}
        self.run_validator(self.group(), False)

    def test_dependency_free_package_remains_accepted(self):
        self.assets["centralTransitiveDependencyGroups"]["net10.0"] = {}
        self.assets["projectFileDependencyGroups"]["net10.0"] = []
        self.run_validator("")


if __name__ == "__main__": unittest.main()
