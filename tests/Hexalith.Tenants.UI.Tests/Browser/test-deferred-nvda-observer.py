"""Observer callback failures remain visible and do not disable future samples."""
import importlib.util
import json
from pathlib import Path
import sys
from types import SimpleNamespace
import unittest
from unittest.mock import Mock, patch


class ObserverRecoveryTests(unittest.TestCase):
    def setUp(self):
        self.clock = Mock()
        self.api = SimpleNamespace(getFocusObject=Mock(return_value=SimpleNamespace(
            treeInterceptor=SimpleNamespace(passThrough=False),
            appModule=SimpleNamespace(appName="firefox"))))
        dependencies = {
            "api": self.api, "config": SimpleNamespace(),
            "globalPluginHandler": SimpleNamespace(GlobalPlugin=object),
            "globalVars": SimpleNamespace(appArgs=SimpleNamespace(configPath="private-fixture")),
            "speech": SimpleNamespace(getState=lambda: SimpleNamespace(speechMode=SimpleNamespace(name="talk"))),
            "synthDriverHandler": SimpleNamespace(), "wx": SimpleNamespace(CallLater=self.clock),
        }
        spec = importlib.util.spec_from_file_location("observer_under_test", Path(__file__).with_name("deferred-nvda-observer.py"))
        self.module = importlib.util.module_from_spec(spec)
        with patch.dict(sys.modules, dependencies):
            spec.loader.exec_module(self.module)
        self.plugin = self.module.GlobalPlugin.__new__(self.module.GlobalPlugin)
        self.plugin.stopped = False

    def testPublicationErrorPropagatesAndNextCallbackPublishesFreshMode(self):
        target = Mock()
        temporary = target.with_suffix.return_value
        temporary.replace.side_effect = [PermissionError("simulated Windows sharing denial"), None]
        with patch.object(self.module, "Path", return_value=target):
            with self.assertRaises(PermissionError):
                self.plugin.capture_mode()
            self.clock.assert_called_once_with(100, self.plugin.capture_mode)
            self.plugin.capture_mode()
        self.assertEqual(self.clock.call_count, 2)
        self.assertEqual(temporary.replace.call_count, 2)
        first, second = [json.loads(call.args[0]) for call in temporary.write_text.call_args_list]
        self.assertGreaterEqual(second["observedUtc"], first["observedUtc"])
        self.assertEqual(second["mode"], "browse")
        self.assertEqual(second["speechMode"], "talk")
        self.assertEqual(second["focusApplication"], "firefox")

    def testFocusErrorPropagatesAndStillSchedulesNextCallback(self):
        self.api.getFocusObject.side_effect = RuntimeError("simulated destroyed focus object")
        with self.assertRaises(RuntimeError):
            self.plugin.capture_mode()
        self.clock.assert_called_once_with(100, self.plugin.capture_mode)

    def testStoppedObserverDoesNotReschedule(self):
        self.plugin.stopped = True
        self.plugin.capture_mode()
        self.clock.assert_not_called()
        self.api.getFocusObject.assert_not_called()


if __name__ == "__main__":
    unittest.main()
