"""Synthetic deadline/ownership tests; no real source/native checkout is mutated."""
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import Mock, patch
import uuid

HERE = Path(__file__).resolve().parent


def load(name):
    spec = importlib.util.spec_from_file_location(name, HERE / (name + ".py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class SetupGuardTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="tenants-setup-guards-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.source = self.root / "source"
        self.source.mkdir()
        self.destination = self.root / "tenants-five-resumed-owned"
        self.preparer = load("prepare-native")

    def testCommandTimeoutIsBoundedAndJournaled(self):
        execute = Mock(side_effect=subprocess.TimeoutExpired(["synthetic"], 0.25, output=b"partial"))
        output = self.root / "journal.json"
        job = self.preparer.Preparation(self.source, self.destination, output,
            overall_seconds=1, command_seconds=0.25, clock=lambda: 0, execute=execute)
        with self.assertRaises(TimeoutError):
            job.run(["synthetic"], self.source)
        self.assertEqual(execute.call_args.kwargs["timeout"], 0.25)
        journal = json.loads(output.read_bytes())
        self.assertTrue(journal["commands"][0]["timedOut"])
        self.assertEqual(journal["commands"][0]["stdout"], "partial")
        self.assertEqual(journal["failure"]["kind"], "command-timeout")
        self.assertFalse(journal["completed"])

    def testOverallDeadlineNarrowsCommandAndStopsNextCommand(self):
        clock = Mock(side_effect=[0, 0.8, 1.1, 1.2])
        execute = Mock(return_value=subprocess.CompletedProcess([], 0, b"", b""))
        output = self.root / "overall.json"
        job = self.preparer.Preparation(self.source, self.destination, output,
            overall_seconds=1, command_seconds=0.5, clock=clock, execute=execute)
        with self.assertRaises(TimeoutError):
            job.run(["synthetic"], self.source)
        self.assertAlmostEqual(execute.call_args.kwargs["timeout"], 0.2)
        with self.assertRaises(TimeoutError):
            job.run(["never-start"], self.source)
        self.assertEqual(execute.call_count, 1)
        self.assertEqual(json.loads(output.read_bytes())["failure"]["kind"], "overall-timeout")

    def testExpiredPreparationStartsNoSubprocess(self):
        execute = Mock()
        job = self.preparer.Preparation(self.source, self.destination, self.root / "expired.json",
            overall_seconds=1, clock=Mock(side_effect=[0, 2]), execute=execute)
        with self.assertRaises(TimeoutError):
            job.prepare()
        execute.assert_not_called()

    def testOnlyMatchingPreparerMarkerProvesOwnership(self):
        (self.destination / ".git").mkdir(parents=True)
        marker = self.preparer.establish_owner(self.source, self.destination, str(uuid.uuid4()))
        self.assertEqual(self.preparer.require_owned_destination(self.source, self.destination), marker)
        marker["source"] = str(self.root / "other")
        (self.destination / ".git" / self.preparer.OWNER_FILE).write_text(json.dumps(marker))
        with self.assertRaises(ValueError):
            self.preparer.require_owned_destination(self.source, self.destination)

    def testRefreshRejectsSourceHistoricalUnownedBeforeAnyMutation(self):
        refresh = load("refresh-native")
        historical = self.root / "tenants-five-remaining-20261030"
        historical.mkdir()
        self.destination.mkdir()
        for destination in (self.source, historical, self.destination, self.source / "tenants-five-resumed-nested"):
            with self.subTest(destination=destination), patch.object(sys, "argv", ["refresh", str(self.source), str(destination), str(self.root / "never.json")]), \
                 patch.object(refresh.subprocess, "run", side_effect=AssertionError("mutation reached")), \
                 patch.object(refresh.subprocess, "check_output", side_effect=AssertionError("Git reached")), \
                 patch.object(refresh.shutil, "copyfile", side_effect=AssertionError("copy reached")):
                with self.assertRaises(ValueError):
                    refresh.main()
        self.assertFalse((self.root / "never.json").exists())


if __name__ == "__main__":
    unittest.main()
