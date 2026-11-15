"""Focused replay safeguards; never restore, build or run the SDK qualification graph."""

import importlib.util
import io
import json
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
from unittest.mock import patch

SPEC = importlib.util.spec_from_file_location("catalog_qualification", Path(__file__).with_name("qualify.py"))
DRIVER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(DRIVER)


class ReplaySafeguardTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="catalog-replay-controls-")
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name) / "source"
        self.root.mkdir()
        self.policy = b'{"sdk":{"version":"11.0.100","rollForward":"latestPatch"},"test":{"runner":"Microsoft.Testing.Platform"}}\n'
        (self.root / "global.json").write_bytes(self.policy)
        self.original = self.root / "evidence"
        self.original.mkdir()
        self.scratch = Path(temporary.name) / "scratch"
        self.scratch.mkdir()
        self.work = self.scratch / "Hexalith.Tenants"
        self.patches = patch.multiple(DRIVER, ROOT=self.root, ORIGINAL_EVIDENCE=self.original,
                                      SCRATCH=self.scratch, WORK=self.work)
        self.patches.start()
        self.addCleanup(self.patches.stop)

    def test_failed_and_successful_replays_preserve_original_receipts_and_each_other(self):
        originals = {"qualification-result.json": b'{"allCommandsPassed":true}\n',
                     "qualification-summary.json": b'{"dotnetTotal":671}\n',
                     "commands.json": b'[]\n', "qualification.log": b'original log\n'}
        for name, content in originals.items():
            (self.original / name).write_bytes(content)

        def fail():
            DRIVER.write("source-before.json", {"new": "partial replay"})
            raise RuntimeError("controlled early failure")

        with patch.object(DRIVER, "qualify", side_effect=fail), patch("sys.stdout", io.StringIO()), patch("sys.stderr", io.StringIO()):
            self.assertEqual(DRIVER.main(), 1)
        failed = DRIVER.EVIDENCE
        failed_bytes = (failed / "qualification-result.json").read_bytes()
        self.assertEqual(json.loads(failed_bytes)["status"], "failed")
        with patch.object(DRIVER, "qualify", return_value={"allCommandsPassed": True}), patch("sys.stdout", io.StringIO()):
            self.assertEqual(DRIVER.main(), 0)
        successful = DRIVER.EVIDENCE
        self.assertNotEqual(failed, successful)
        self.assertEqual((failed / "qualification-result.json").read_bytes(), failed_bytes)
        self.assertEqual(json.loads((successful / "qualification-result.json").read_text())["status"], "passed")
        self.assertFalse((successful / "qualification-summary.json").exists())
        self.assertEqual({name: (self.original / name).read_bytes() for name in originals}, originals)

    def test_interruption_leaves_its_own_explicitly_incomplete_result(self):
        with patch.object(DRIVER, "qualify", side_effect=KeyboardInterrupt), patch("sys.stdout", io.StringIO()):
            with self.assertRaises(KeyboardInterrupt):
                DRIVER.main()
        result = json.loads((DRIVER.EVIDENCE / "qualification-result.json").read_text())
        self.assertEqual(result, {"status": "incomplete", "allCommandsPassed": False})

    def test_measured_toolchain_and_consumer_parent_share_the_original_policy(self):
        calls = []

        def dotnet(arguments, *, cwd, **kwargs):
            self.assertEqual((Path(cwd) / "global.json").read_bytes(), self.policy)
            calls.append((arguments, cwd))
            return "11.0.101\n" if arguments == ["dotnet", "--version"] else "Measured SDK fixture details\n"

        def measure():
            self.work.mkdir()
            shutil.copy2(self.root / "global.json", self.work / "global.json")
            DRIVER.capture_toolchain()
            return {"allCommandsPassed": True}

        with patch.object(DRIVER, "qualify", side_effect=measure), patch.object(DRIVER.subprocess, "check_output", side_effect=dotnet), patch("sys.stdout", io.StringIO()):
            self.assertEqual(DRIVER.main(), 0)
        measured = json.loads((DRIVER.EVIDENCE / "toolchain.json").read_text())
        self.assertEqual(measured["selectedSdk"], {"repository": "11.0.101", "consumerParent": "11.0.101"})
        self.assertEqual((DRIVER.EVIDENCE / "global.json").read_bytes(), self.policy)
        self.assertEqual((DRIVER.EVIDENCE / "dotnet-info.log").read_text(), "Measured SDK fixture details\n")
        self.assertEqual(calls, [(["dotnet", "--version"], self.work), (["dotnet", "--version"], self.scratch),
                                 (["dotnet", "--info"], self.work)])

    def test_optional_submodules_do_not_inherit_the_ambient_repository_identity(self):
        paths = ("references/Active", "references/Uninitialized", "references/Missing", "references/Invalid")
        for name in (paths[0], paths[1], paths[3]):
            (self.root / name).mkdir(parents=True)

        def git(directory, *arguments):
            if directory == self.root:
                if arguments == ("ls-files", "--stage"):
                    return "\n".join("160000 " + "a" * 40 + " 0\t" + name for name in paths)
                return "parent identity"
            if arguments == ("rev-parse", "--show-toplevel"):
                if directory.name == "Invalid":
                    raise subprocess.CalledProcessError(128, "git")
                return str(directory if directory.name == "Active" else self.root)
            self.assertEqual(directory.name, "Active", "An uninitialized path must never be queried for HEAD/status.")
            return "active identity"

        with patch.object(DRIVER, "git", side_effect=git):
            modules = DRIVER.inputs()["submodules"]
        self.assertTrue(modules[0]["initialized"])
        self.assertEqual(modules[0]["head"], "active identity")
        for module in modules[1:]:
            self.assertFalse(module["initialized"])
            self.assertIsNone(module["head"])
            self.assertIsNone(module["status"])


if __name__ == "__main__":
    unittest.main()
