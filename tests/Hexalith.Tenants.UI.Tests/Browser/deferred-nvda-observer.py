"""Read-only settings observer, loaded only from the capture's private NVDA config."""

import json
from datetime import datetime, timezone
from pathlib import Path

import api
import config
import globalPluginHandler
import globalVars
import speech
import synthDriverHandler
import wx


class GlobalPlugin(globalPluginHandler.GlobalPlugin):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.stopped = False
        speech.speechCanceled.register(self.speech_cancelled)
        wx.CallLater(3000, self.capture_settings)
        wx.CallLater(100, self.capture_mode)

    def terminate(self):
        self.stopped = True
        speech.speechCanceled.unregister(self.speech_cancelled)
        super().terminate()

    def speech_cancelled(self, **kwargs):
        event = {"utc": datetime.now(timezone.utc).isoformat(), "kind": "nvda-speech-canceled"}
        with Path(globalVars.appArgs.configPath, "interruptions.jsonl").open("a", encoding="utf-8") as stream:
            stream.write(json.dumps(event) + "\n")

    def capture_mode(self):
        if self.stopped:
            return
        try:
            focus = api.getFocusObject()
            interceptor = getattr(focus, "treeInterceptor", None)
            mode = "not-applicable"
            if interceptor is not None and hasattr(interceptor, "passThrough"):
                mode = "focus" if interceptor.passThrough else "browse"
            application = getattr(getattr(focus, "appModule", None), "appName", None)
            state = {"observedUtc": datetime.now(timezone.utc).isoformat(), "mode": mode, "speechMode": speech.getState().speechMode.name,
                     "focusApplication": application if application in {"chrome", "firefox"} else "outside-browser"}
            target = Path(globalVars.appArgs.configPath, "effective-mode.json")
            temporary = target.with_suffix(".tmp")
            temporary.write_text(json.dumps(state), encoding="utf-8")
            temporary.replace(target)
        finally:
            # Let NVDA report the original exception. A transient publication
            # or focus-read error must not permanently stop future snapshots.
            # Repeated failures still fail the collector's freshness gate.
            wx.CallLater(100, self.capture_mode)

    def capture_settings(self):
        synth = synthDriverHandler.getSynth()
        settings = {
            "observedUtc": datetime.now(timezone.utc).isoformat(),
            "synthesizer": synth.name,
            "voice": getattr(synth, "voice", None),
            "language": getattr(synth, "language", None),
            "rate": getattr(synth, "rate", None),
            "speechMode": speech.getState().speechMode.name,
            "autoLanguageSwitching": config.conf["speech"]["autoLanguageSwitching"],
            "autoDialectSwitching": config.conf["speech"]["autoDialectSwitching"],
            "reportDynamicContentChanges": config.conf["presentation"]["reportDynamicContentChanges"],
            "autoPassThroughOnFocusChange": config.conf["virtualBuffers"]["autoPassThroughOnFocusChange"],
            "autoPassThroughOnCaretMove": config.conf["virtualBuffers"]["autoPassThroughOnCaretMove"],
            "useScreenLayout": config.conf["virtualBuffers"]["useScreenLayout"],
            "reportObjectDescriptions": config.conf["presentation"]["reportObjectDescriptions"],
            "reportHelpBalloons": config.conf["presentation"]["reportHelpBalloons"],
            "reportTooltips": config.conf["presentation"]["reportTooltips"],
        }
        Path(globalVars.appArgs.configPath, "effective-settings.json").write_text(
            json.dumps(settings, ensure_ascii=False, indent=2), encoding="utf-8"
        )
