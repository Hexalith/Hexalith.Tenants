"""Exercise the actual AppHost metadata without building or starting resources."""
from contextlib import contextmanager
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


REPOSITORY = Path(__file__).resolve().parents[2]
APPHOST = Path("src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj")
CANONICAL_FILES = (
    "Hexalith.Platform.slnx", "DaprSelfHostedMtls.cs", "DaprSelfHostedMtlsSubscriber.cs",
)


class PlatformSourceRootTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.platform = next((candidate for candidate in (
            REPOSITORY / "references/Hexalith.Platform", REPOSITORY.parent / "Hexalith.Platform",
        ) if all((candidate / name).is_file() for name in CANONICAL_FILES)), None)
        if cls.platform is None:
            raise RuntimeError("Initialize the declared Platform root or supply its existing sibling checkout.")
        cls.builds = next((candidate for candidate in (
            REPOSITORY / "references/Hexalith.Builds", REPOSITORY.parent / "Hexalith.Builds",
        ) if (candidate / "Props/Directory.Packages.props").is_file()), None)
        if cls.builds is None:
            raise RuntimeError("The declared Builds package catalog is required.")

    def canonical(self, destination):
        destination.mkdir(parents=True, exist_ok=True)
        for name in CANONICAL_FILES:
            shutil.copyfile(self.platform / name, destination / name)
        return destination

    @contextmanager
    def layout(self, name):
        with tempfile.TemporaryDirectory(prefix="tenants-platform-metadata-") as temporary:
            base = Path(temporary)
            if name == "parent":
                tenants = base / "umbrella/Hexalith.Tenants"
                selected = base / "umbrella"
            elif name == "grandparent":
                tenants = base / "umbrella/references/Hexalith.Tenants"
                selected = base / "umbrella"
            else:
                tenants = base / "workspace/Hexalith.Tenants"
                selected = (tenants / "references/Hexalith.Platform" if name == "nested"
                            else tenants.parent / "Hexalith.Platform")
            (tenants / APPHOST.parent).mkdir(parents=True)
            for relative in (Path("Directory.Build.props"), Path("Directory.Packages.props"), APPHOST):
                shutil.copyfile(REPOSITORY / relative, tenants / relative)
            (tenants / "references").mkdir()
            # A read-only direct source projection, never nested initialization.
            (tenants / "references/Hexalith.Builds").symlink_to(self.builds, target_is_directory=True)
            yield tenants, self.canonical(selected), base

    def evaluate(self, tenants, configuration, explicit=None):
        command = [
            "dotnet", "msbuild", str(tenants / APPHOST), "-nologo",
            "-target:ValidateCanonicalPlatformSource",
            "-getProperty:HexalithPlatformRoot", "-getItem:Compile",
            "-p:Configuration=" + configuration,
            "-p:UseHexalithProjectReferences=" + ("true" if configuration == "Debug" else "false"),
        ]
        if explicit is not None:
            command.append("-p:HexalithPlatformRoot=" + str(explicit))
        environment = dict(os.environ)
        environment.pop("HexalithPlatformRoot", None)
        return subprocess.run(command, cwd=REPOSITORY, env=environment, text=True,
                              capture_output=True, timeout=60)

    def assert_selected(self, result, expected):
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        metadata = json.loads(result.stdout)
        self.assertEqual(Path(metadata["Properties"]["HexalithPlatformRoot"]).resolve(), expected.resolve())
        linked = {Path(item["FullPath"]).resolve() for item in metadata["Items"]["Compile"]
                  if Path(item["Identity"]).name.startswith("DaprSelfHostedMtls")}
        self.assertEqual(linked, {expected.resolve() / name for name in CANONICAL_FILES[1:]})

    def test_declared_nested_and_sibling_layouts_in_both_dependency_modes(self):
        for layout in ("nested", "sibling"):
            for configuration in ("Release", "Debug"):
                with self.subTest(layout=layout, configuration=configuration), self.layout(layout) as (tenants, platform, _):
                    self.assert_selected(self.evaluate(tenants, configuration), platform)

    def test_platform_umbrella_requires_and_accepts_its_explicit_owner_input(self):
        for layout in ("parent", "grandparent"):
            for configuration in ("Release", "Debug"):
                with self.subTest(layout=layout, configuration=configuration), self.layout(layout) as (tenants, platform, _):
                    self.assert_selected(self.evaluate(tenants, configuration, platform), platform)

    def test_ambient_parent_helpers_do_not_become_compile_inputs(self):
        for layout in ("parent", "grandparent"):
            for configuration in ("Release", "Debug"):
                with self.subTest(layout=layout, configuration=configuration), self.layout(layout) as (tenants, _, _):
                    result = self.evaluate(tenants, configuration)
                    self.assertNotEqual(result.returncode, 0)
                    self.assertIn("requires the canonical Platform checkout", result.stdout + result.stderr)

    def test_incomplete_canonical_checkout_refuses(self):
        for missing in CANONICAL_FILES:
            for configuration in ("Release", "Debug"):
                with self.subTest(missing=missing, configuration=configuration), self.layout("nested") as (tenants, platform, _):
                    (platform / missing).unlink()
                    result = self.evaluate(tenants, configuration, platform)
                    self.assertNotEqual(result.returncode, 0)
                    self.assertIn("canonical", result.stdout + result.stderr)

    def test_explicit_foreign_helpers_without_the_platform_checkout_refuse(self):
        for configuration in ("Release", "Debug"):
            with self.subTest(configuration=configuration), self.layout("sibling") as (tenants, _, base):
                foreign = self.canonical(base / "foreign-helpers")
                (foreign / CANONICAL_FILES[0]).unlink()
                result = self.evaluate(tenants, configuration, foreign)
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("requires the canonical Platform checkout", result.stdout + result.stderr)

    def test_absent_declared_sources_refuse(self):
        for configuration in ("Release", "Debug"):
            with self.subTest(configuration=configuration), self.layout("nested") as (tenants, platform, _):
                shutil.rmtree(platform)
                result = self.evaluate(tenants, configuration)
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("requires the canonical Platform checkout", result.stdout + result.stderr)

    def test_declared_nested_checkout_has_precedence_over_sibling(self):
        for configuration in ("Release", "Debug"):
            with self.subTest(configuration=configuration), self.layout("nested") as (tenants, platform, _):
                self.canonical(tenants.parent / "Hexalith.Platform")
                self.assert_selected(self.evaluate(tenants, configuration), platform)

    def test_explicit_owner_input_has_precedence_over_discovery(self):
        for configuration in ("Release", "Debug"):
            with self.subTest(configuration=configuration), self.layout("nested") as (tenants, _, base):
                explicit = self.canonical(base / "selected-platform")
                self.assert_selected(self.evaluate(tenants, configuration, explicit), explicit)


if __name__ == "__main__":
    unittest.main()
