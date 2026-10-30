"""Focused replay regressions using copies of retained evidence, never captures."""
import copy
from datetime import timedelta
import importlib.util
import json
from pathlib import Path
import unittest
import zipfile

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("matrix", HERE / "build-native-matrix.py")
matrix = importlib.util.module_from_spec(spec)
spec.loader.exec_module(matrix)


class MatrixReplayTests(unittest.TestCase):
    def setUp(self):
        with zipfile.ZipFile(HERE / "native-attempts/current-chrome-matrix-1.zip") as archive:
            self.capture, self.fixture, self.invocation, self.timeline, self.speech = [json.loads(archive.read(name))
                for name in ("capture.json", "fixture.json", "invocation.json", "timeline.json", "speech.json")]
        self.row = next(row for row in self.fixture["nativeSpeechTrials"] if row["trial"] == "C2-lifecycle-en")
        self.attempt = {"pairing": "NVDA/Chrome", "sourceRootRevision": self.fixture["sourceBaseRevision"]}

    def valid(self):
        return matrix.window_controls(self.row, self.timeline, self.speech, "NVDA/Chrome")

    def change_duration(self, seconds):
        trial = self.row["trial"]
        observe = matrix.one(self.timeline, "observe", trial)
        old_end = matrix.utc(self.row["observation"]["endedUtc"])
        new_end = matrix.utc(observe["stateUtc"]) + timedelta(seconds=seconds)
        delta = new_end - old_end
        self.row["observation"]["endedUtc"] = new_end.isoformat()
        matrix.one(self.timeline, "observation-end", trial)["observation"]["endedUtc"] = new_end.isoformat()
        if delta <= timedelta(0):
            # Keep controls after the original >=30-second window, so a
            # 29-second rejection specifically protects the duration floor.
            return
        # Shift the entire subsequent control interval, preserving its raw
        # speech/mode/cancellation bindings. The duration gate is isolated.
        for item in self.timeline:
            if item.get("trial") in (trial, trial + "-focus-control"):
                for key in ("utc", "requestedUtc", "responseUtc"):
                    if key in item and matrix.utc(item[key]) >= old_end:
                        item[key] = (matrix.utc(item[key]) + delta).isoformat()
        for key in ("requestedUtc", "responseUtc"):
            self.row["interruption"][key] = (matrix.utc(self.row["interruption"][key]) + delta).isoformat()
        for item in self.speech:
            if item["trial"] == trial + "-focus-control":
                item["utc"] = (matrix.utc(item["utc"]) + delta).isoformat()
                item["nvdaMode"]["observedUtc"] = (matrix.utc(item["nvdaMode"]["observedUtc"]) + delta).isoformat()

    def testThirtySecondsAcceptedAndTwentyNineRejected(self):
        self.change_duration(30)
        self.assertTrue(self.valid())
        self.change_duration(29)
        self.assertFalse(self.valid())

    def testOverSixtySecondsRejectedWithOtherwiseOrderedControls(self):
        self.change_duration(60.001)
        self.assertFalse(self.valid())

    def testReversedProtocolAndInconsistentEndRejected(self):
        end = matrix.one(self.timeline, "observation-end", self.row["trial"])
        control = matrix.one(self.timeline, "control-start", self.row["trial"])
        i, j = self.timeline.index(end), self.timeline.index(control)
        self.timeline[i], self.timeline[j] = self.timeline[j], self.timeline[i]
        self.assertFalse(self.valid())
        self.timeline[i], self.timeline[j] = self.timeline[j], self.timeline[i]
        end["observation"]["endedUtc"] = "2026-10-30T18:00:00Z"
        self.assertFalse(self.valid())

    def testRetainedCrossClockTransitionAndSameMillisecondAreValid(self):
        self.assertTrue(self.valid())
        start = matrix.one(self.timeline, "control-start", self.row["trial"])
        end = matrix.utc(self.row["observation"]["endedUtc"])
        start["utc"] = end.replace(microsecond=end.microsecond // 1000 * 1000).isoformat()
        self.assertTrue(self.valid())
        start["utc"] = matrix.one(self.timeline, "trigger", self.row["trial"])["utc"]
        self.assertFalse(self.valid())

    def testSetupCancellationIsNotProductInterruption(self):
        observe = matrix.one(self.timeline, "observe", self.row["trial"])
        trigger = matrix.one(self.timeline, "trigger", self.row["trial"])
        groups = matrix.cancellation_segments(self.timeline, self.row["trial"], matrix.utc(trigger["utc"]),
                                               matrix.utc(self.row["observation"]["endedUtc"]))
        self.assertEqual(len(groups["setup"]), 1)
        self.assertEqual(len(groups["productWindow"]), 0)
        self.assertGreater(len(groups["control"]), 0)
        self.assertLess(matrix.utc(groups["setup"][0]["utc"]), matrix.utc(observe["stateUtc"]))

    def testRawSearchSpeechAndCancellationAreRequired(self):
        self.assertTrue(self.valid())
        self.speech = [row for row in self.speech if row["trial"] != self.row["trial"] + "-focus-control"]
        self.assertFalse(self.valid())

    def testMatchingPostRequestCancellationIsRequired(self):
        request = matrix.utc(self.row["interruption"]["requestedUtc"])
        self.timeline = [row for row in self.timeline if not (row.get("kind") == "nvda-speech-canceled"
            and row.get("trial") == self.row["trial"] + "-focus-control" and matrix.utc(row["utc"]) >= request)]
        self.assertFalse(self.valid())

    def testFailedCaptureRunnerBuildRuntimeAndFailureAreRejected(self):
        self.assertTrue(matrix.successful_capture(self.capture, self.fixture, self.invocation, self.timeline, self.attempt))
        changes = [("capture", "passed", False), ("capture", "completed", False), ("capture", "runnerExitCode", 1),
                   ("invocation", "exitCode", 1), ("fixture", "sourceStableDuringBuild", False),
                   ("fixture", "failure", {"label": "failed"})]
        for target, key, value in changes:
            with self.subTest(target=target, key=key):
                data = copy.deepcopy(getattr(self, target)); data[key] = value
                args = {name: data if name == target else getattr(self, name)
                        for name in ("capture", "fixture", "invocation", "timeline")}
                self.assertFalse(matrix.successful_capture(**args, attempt=self.attempt))
        self.fixture["build"]["succeeded"] = False
        self.assertFalse(matrix.successful_capture(self.capture, self.fixture, self.invocation, self.timeline, self.attempt))


if __name__ == "__main__":
    unittest.main()
