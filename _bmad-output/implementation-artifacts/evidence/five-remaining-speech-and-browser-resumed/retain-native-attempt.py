"""Retain a reviewed fixture packet without newline conversion or private paths."""
import hashlib
import json
from pathlib import Path
import sys
import zipfile


HERE = Path(__file__).resolve().parent
directory, label, pairing, inventory = sys.argv[1:]
directory = Path(directory)
capture_raw = (directory / "capture.json").read_bytes()
capture = json.loads(capture_raw)
fixture = json.loads((directory / "fixture.json").read_bytes())
speech = json.loads((directory / "speech.json").read_bytes())
if any(sample.get("browserForeground") is not True for sample in speech):
    raise ValueError("Review nonforeground samples before retention")
old = json.dumps(capture["privateLogs"], ensure_ascii=False).encode()
new = json.dumps("[private native logs retained outside the repository]").encode()
if capture_raw.count(old) != 1:
    raise ValueError("Private log path is not uniquely identifiable")
sanitized = capture_raw.replace(old, new)
if {**json.loads(sanitized), "privateLogs": capture["privateLogs"]} != capture:
    raise ValueError("Unexpected metadata change")
members = {}
for name in ("fixture.json", "speech.json", "timeline.json", "speech.jsonl"):
    if (directory / name).exists():
        members[name] = (directory / name).read_bytes()
members["capture.json"] = sanitized
members["invocation.json"] = Path(str(directory) + "-invocation.json").read_bytes()
members["retention.json"] = (json.dumps({"removedFieldValue": "privateLogs user-profile path only",
    "rawCaptureSha256": hashlib.sha256(capture_raw).hexdigest(),
    "retainedCaptureSha256": hashlib.sha256(sanitized).hexdigest(),
    "speechAndTimelineBytesUnchanged": True,
    "collectorOriginalOutputSanitizedFlagPreserved": capture["outputSanitized"],
    "manualInspection": "Retained Speech Viewer samples contain fixture product/control speech; no authentication or unrelated desktop speech retained.",
    "audibleCompletion": "Not established by Speech Viewer"}, indent=2) + "\n").encode()
packet = HERE / "native-attempts" / (label + ".zip")
packet.parent.mkdir(exist_ok=True)
if packet.exists():
    raise ValueError("Never overwrite a retained attempt")
with zipfile.ZipFile(packet, "w", zipfile.ZIP_DEFLATED) as archive:
    for name, data in members.items():
        archive.writestr(name, data)
path = HERE / "native-attempt-index.json"
index = json.loads(path.read_bytes()) if path.exists() else {"attempts": []}
index["attempts"].append({"label": label, "pairing": pairing,
    "packet": packet.relative_to(HERE).as_posix(), "packetSha256": hashlib.sha256(packet.read_bytes()).hexdigest(),
    "sourceRootRevision": fixture["sourceBeforeBuild"]["sourceBaseRevision"], "dependencyInventory": inventory,
    "collectorSha256": capture["collectorSha256"], "observerSha256": capture["observerSha256"],
    "startedUtc": capture["startedUtc"], "finishedUtc": capture["finishedUtc"],
    "failure": capture.get("failure"), "collectorPassed": capture["passed"],
    "protocolCompleted": capture["completed"], "speechSamples": len(speech),
    "completedTrials": [trial["trial"] for trial in fixture.get("nativeSpeechTrials", [])],
    "sourceSettingsUnchanged": capture["sourceSettingsUnchanged"],
    "retainedMembers": [{"path": name, "bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()}
                        for name, data in members.items()]})
path.write_text(json.dumps(index, indent=2) + "\n")
print(label, "retained", len(speech), "speech samples; complete controls:", index["attempts"][-1]["completedTrials"])
