"""Build the exact current-profile matrix without claiming AT acceptance.

Text occurrences describe queued output only. They include all focus/live output
and neither identify announcement causality nor establish audible completion.
"""
from datetime import datetime, timedelta
import hashlib
import json
from pathlib import Path
import re
import unicodedata
import zipfile


HERE = Path(__file__).resolve().parent
TRIALS = ["S0", "S1", "S2", "S3", "C1-lifecycle", "C1-availability", "C2-lifecycle", "C2-availability",
          "C3-events-lifecycle", "C3-events-availability", "C3-unavailable-lifecycle", "C3-unavailable-availability"]
TEXT = {
    "en": {
        "searchUnavailable": "Protected whole-set search is temporarily unavailable. You can continue browsing the authorized tenant list.",
        "searchRefreshed": "Protected search paging could not be restored. Search has restarted from the first page.",
        "listRefreshed": "The requested page was no longer available. The authorized first page has been refreshed.",
        "pendingLabel": "Audit pending",
        "pendingReason": "The command's events are stored, but its audit record is not readable yet. It normally appears shortly, and no proof is claimed until it does.",
        "unavailableLabel": "Audit unavailable",
        "unavailableReason": "The audit status could not be read or verified after the command was sent. This does not mean the record does not exist, and no proof is claimed.",
    },
    "fr": {
        "searchUnavailable": "La recherche protégée sur l'ensemble des locataires est temporairement indisponible. Vous pouvez continuer à parcourir la liste autorisée.",
        "searchRefreshed": "La pagination de recherche protégée n'a pas pu être restaurée. La recherche a redémarré depuis la première page.",
        "listRefreshed": "La page demandée n'était plus disponible. La première page autorisée a été actualisée.",
        "pendingLabel": "Audit en attente",
        "pendingReason": "Les événements de la commande sont enregistrés, mais son enregistrement d’audit n’est pas encore lisible. Il apparaît normalement sous peu, et aucune preuve n’est revendiquée d’ici là.",
        "unavailableLabel": "Audit indisponible",
        "unavailableReason": "Le statut d’audit n’a pas pu être lu ni vérifié après l’envoi de la commande. Cela ne signifie pas que l’enregistrement n’existe pas, et aucune preuve n’est revendiquée.",
    },
}


def normalize(value):
    return re.sub(r"\s+", " ", unicodedata.normalize("NFKC", value).replace("’", "'")).strip().casefold()


def utc(value):
    result = datetime.fromisoformat(value.replace("Z", "+00:00"))
    if result.tzinfo is None:
        raise ValueError("Timestamp must identify its UTC offset")
    return result


def one(events, kind, trial=None):
    matches = [item for item in events if item.get("kind") == kind and (trial is None or item.get("trial") == trial)]
    if len(matches) != 1:
        raise ValueError("Missing or duplicate timeline record: " + kind)
    return matches[0]


def successful_capture(capture, fixture, invocation, timeline, attempt):
    """Failed retained attempts remain failures even if they contain trial rows."""
    try:
        build = fixture["build"]
        trials = [row["trial"] for row in fixture["nativeSpeechTrials"]]
        plan = one(timeline, "plan")["requiredTrials"]
        complete = one(timeline, "complete")["completedTrials"]
        source = fixture["sourceBeforeBuild"]
        engine = "chromium" if attempt["pairing"] == "NVDA/Chrome" else "firefox"
        hashes = [fixture[key] for key in ("localUiAssemblySha256", "localShellAssemblySha256", "fluentAssemblySha256", "fluentModuleSha256")]
        return (capture["passed"] is True and capture["completed"] is True and capture["runnerExitCode"] == 0
            and invocation["exitCode"] == 0 and capture["viewerBound"] is True and capture["sourceSettingsUnchanged"] is True
            and invocation["nvdaProcessesBefore"] == [] and invocation["nvdaProcessesAfter"] == []
            and fixture["manualSessionCompleted"] is True and fixture.get("passed") is not False
            and fixture["circuitFaults"] == [] and build["succeeded"] is True
            and build["reusedBuild"] is False and build["skippedForDevelopment"] is False
            and build["executable"] == "dotnet" and build["arguments"] == ["build", "src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj",
                "-c", "Debug", "-m:1", "-p:UseNuGetDeps=false", "-p:HexalithEventStoreVersion=3.128.4",
                "-p:HexalithMcpCliFromSource=false", "-p:NuGetAudit=false"]
            and fixture["sourceStableDuringBuild"] is True and fixture["sourceStableThroughCleanup"] is True
            and all(fixture[key] == source for key in ("sourceAfterBuild", "sourceAfterBrowser", "sourceAfterCleanup"))
            and source["sourceBaseRevision"] == fixture["sourceBaseRevision"] == attempt["sourceRootRevision"]
            and capture["engine"] == fixture["browserEngine"] == invocation["browserEngine"] == engine
            and all(isinstance(value, str) and re.fullmatch(r"[0-9a-f]{64}", value) for value in hashes)
            and bool(fixture["browserVersion"]) and bool(fixture["playwrightVersion"])
            and fixture["fluentPackageVersion"] == "5.0.1"
            and len(trials) > 0 and len(trials) == len(set(trials))
            and trials == capture["completedTrials"] == complete and set(trials) == set(plan) and len(plan) == len(trials)
            and not any(value for data in (capture, fixture, invocation) for key, value in data.items()
                        if key == "failure" or key.endswith("Blocker"))
            and utc(capture["startedUtc"]) <= utc(fixture["startedUtc"]) <= utc(fixture["finishedUtc"])
                <= utc(capture["finishedUtc"]) <= utc(invocation["endedUtc"]))
    except (KeyError, TypeError, ValueError):
        return False


def cancellation_segments(timeline, trial, trigger_time, end_time):
    """Keep setup/control notifications distinct from product-window events."""
    result = {"setup": [], "productWindow": [], "postWindow": [], "control": []}
    for item in timeline:
        if item.get("kind") != "nvda-speech-canceled":
            continue
        if item.get("trial") == trial + "-focus-control":
            result["control"].append(item)
        elif item.get("trial") == trial:
            stamp = utc(item["utc"])
            key = "setup" if stamp < trigger_time else "productWindow" if stamp <= end_time else "postWindow"
            result[key].append(item)
    return result


def window_controls(completed, timeline, speech, pairing):
    """Replay the unchanged temporal and raw positive/cancellation contracts."""
    try:
        trial = completed["trial"]
        culture = completed["culture"]
        control_trial = trial + "-focus-control"
        expected_name = "Search tenants" if culture == "en" else "Rechercher des locataires"
        arm, quiet, trigger, observe, end, control_start, control_end = [one(timeline, kind, trial)
            for kind in ("arm", "quiet-window", "trigger", "observe", "observation-end", "control-start", "control-end")]
        focus_result, request, response = [one(timeline, kind, control_trial)
            for kind in ("focus-control-result", "interruption-control", "interruption-control-result")]
        observation = completed["observation"]
        times = [utc(arm["utc"]), utc(quiet["utc"]), utc(trigger["utc"]), utc(observe["stateUtc"]),
                 utc(observe["utc"]), utc(observation["startedUtc"]), utc(observation["endedUtc"]),
                 utc(control_start["utc"]), utc(control_end["utc"]), utc(request["utc"]), utc(response["responseUtc"])]
        duration = times[6] - times[3]
        # The acknowledgement crosses the collector/Node clocks. The retained
        # protocol order binds observation-end -> control-start; their stamps
        # remain unchanged. No slack applies to duration, freshness or quiet.
        protocol = [arm, quiet, trigger, observe, end, control_start, control_end, focus_result, request, response]
        positions = [next(i for i, item in enumerate(timeline) if item is record) for record in protocol]
        ordered = (times[:7] == sorted(times[:7]) and times[7:] == sorted(times[7:])
                   and positions == sorted(positions) and times[6] <= times[8]
                   and times[7] >= times[3] + timedelta(seconds=30))
        if not (ordered and timedelta(seconds=30) <= duration <= timedelta(seconds=60)
                and end["observation"] == observation and observation["queuedQuietSeconds"] >= 5
                and quiet["queuedQuietSeconds"] >= 5):
            return False
        focus = control_end["focus"]
        if not (focus["documentHasFocus"] is True and focus["element"] == "input"
                and "tenants-list-search" in focus["ancestry"] and focus == completed["control"]["focus"] == completed["focus"]):
            return False
        samples = [row for row in speech if row["trial"] == control_trial and max(times[6], times[7]) <= utc(row["utc"]) <= times[9]]
        raw_text = " ".join(row["text"] for row in samples)
        if not (samples and normalize(expected_name) in normalize(raw_text)
                and len(samples) == completed["control"]["speechSamples"] == focus_result["speechSamples"]
                and completed["control"]["expectedName"] == focus_result["expectedName"] == expected_name
                and completed["control"]["verified"] is True and focus_result["verified"] is True):
            return False
        expected_app = "chrome" if pairing == "NVDA/Chrome" else "firefox"
        for sample in [row for row in speech if row["trial"] in (trial, control_trial)]:
            mode = sample["nvdaMode"]
            age = utc(sample["utc"]) - utc(mode["observedUtc"])
            if not (sample["browserForeground"] is True and timedelta(0) <= age <= timedelta(seconds=2)
                    and mode["speechMode"] == "talk" and mode["focusApplication"] == expected_app
                    and mode["mode"] in ("browse", "focus", "not-applicable")):
                return False
        interrupted = completed["interruption"]
        if not (interrupted["verified"] is True and response["verified"] is True
                and request["key"] == "Control" and response["status"] == "observed cancellation response"
                and utc(interrupted["requestedUtc"]) == utc(response["requestedUtc"]) == times[9]
                and utc(interrupted["responseUtc"]) == times[10]
                and timedelta(0) <= times[10] - times[9] <= timedelta(seconds=5)):
            return False
        return any(item.get("kind") == "nvda-speech-canceled" and item.get("trial") == control_trial
                   and utc(item["utc"]) == times[10] for item in timeline)
    except (KeyError, TypeError, ValueError):
        return False


def build():
    refresh = json.loads((HERE / "native-refresh.json").read_bytes())
    root_revision = next(row for row in refresh["repositories"] if row["path"] == ".")["after"]["revision"]
    rows = {(pairing, trial + "-" + culture): {"pairing": pairing, "culture": culture, "trial": trial + "-" + culture,
             "sourceRootRevision": root_revision, "protocolComplete": False, "independentlyAssessed": False,
             "acceptedForClosure": False, "status": "not captured in a completed current-profile window"}
            for pairing in ("NVDA/Chrome", "NVDA/Firefox") for culture in ("en", "fr") for trial in TRIALS}
    inspections = []
    index = json.loads((HERE / "native-attempt-index.json").read_bytes())
    for attempt in index["attempts"]:
        if attempt["sourceRootRevision"] != root_revision:
            continue
        with zipfile.ZipFile(HERE / attempt["packet"]) as archive:
            fixture = json.loads(archive.read("fixture.json"))
            timeline = json.loads(archive.read("timeline.json"))
            speech = json.loads(archive.read("speech.json"))
            capture = json.loads(archive.read("capture.json"))
            invocation = json.loads(archive.read("invocation.json"))
        capture_valid = successful_capture(capture, fixture, invocation, timeline, attempt)
        for completed in fixture.get("nativeSpeechTrials", []):
            trial = completed["trial"]
            row = rows[(attempt["pairing"], trial)]
            trigger = next(item for item in timeline if item.get("kind") == "trigger" and item.get("trial") == trial)
            event = next(item for item in timeline if item.get("kind") == "observe" and item.get("trial") == trial)
            trigger_time = utc(trigger["utc"])
            state_time = utc(event["stateUtc"])
            receipt_time = utc(completed["observation"]["startedUtc"])
            end_time = utc(completed["observation"]["endedUtc"])
            duration = utc(completed["observation"]["endedUtc"]) - utc(event["stateUtc"])
            valid = capture_valid and window_controls(completed, timeline, speech, attempt["pairing"])
            if trial.startswith("C"):
                state = event["fixture"]
                expected_status = "events" if trial.startswith("C1-") or trial.startswith("C3-events-") else "unavailable"
                valid = valid and state["simulatedCommands"] == 1 and state["unexpectedCommands"] == 0 \
                    and state["projection"] == "hold" and state["status"] == expected_status
            samples = [sample for sample in speech if sample["trial"] == trial]
            queued = " ".join(sample["text"] for sample in samples)
            pre_trigger = [sample for sample in samples if utc(sample["utc"]) < trigger_time]
            post_trigger = [sample for sample in samples if trigger_time <= utc(sample["utc"]) <= end_time]
            post_trigger_text = " ".join(sample["text"] for sample in post_trigger)
            pre_trigger_text = " ".join(sample["text"] for sample in pre_trigger)
            occurrences = {name: normalize(post_trigger_text).count(normalize(text)) for name, text in TEXT[row["culture"]].items()}
            row.update(protocolComplete=bool(valid), status="complete controls; independent speech assessment pending" if valid
                       else "failed/incomplete evidence; not an announcement omission", packet=attempt["packet"],
                       triggerUtc=trigger["utc"], matchingStateUtc=event["stateUtc"],
                       collectorReceiptStartedUtc=completed["observation"]["startedUtc"],
                       observationEndedUtc=completed["observation"]["endedUtc"],
                       stateToEndMilliseconds=duration.total_seconds() * 1000, speechSamples=len(samples),
                       positiveControlVerified=completed["control"]["verified"],
                       cancellationResponseVerified=completed["interruption"]["verified"])
            inspections.append({"pairing": attempt["pairing"], "culture": row["culture"], "trial": trial,
                "packet": attempt["packet"], "speechMember": "speech.json", "timelineMember": "timeline.json",
                "queuedTextSha256": hashlib.sha256(queued.encode()).hexdigest(),
                "triggerUtc": trigger["utc"], "matchingStateUtc": event["stateUtc"],
                "collectorReceiptStartedUtc": completed["observation"]["startedUtc"],
                "observationEndedUtc": completed["observation"]["endedUtc"],
                "postTriggerQueuedTextOccurrences": occurrences,
                "preTriggerQueuedTextOccurrences": {name: normalize(pre_trigger_text).count(normalize(text))
                    for name, text in TEXT[row["culture"]].items()},
                "sampleArrivalSegments": {"preTrigger": len(pre_trigger),
                    "triggerBeforeMatchedState": sum(trigger_time <= utc(sample["utc"]) < state_time for sample in samples),
                    "matchedStateBeforeCollectorReceipt": sum(state_time <= utc(sample["utc"]) < receipt_time for sample in samples),
                    "collectorReceiptThroughEnd": sum(receipt_time <= utc(sample["utc"]) <= end_time for sample in samples),
                    "afterEnd": sum(utc(sample["utc"]) > end_time for sample in samples)},
                "productWindowCancellationNotifications": len(cancellation_segments(timeline, trial, trigger_time, end_time)["productWindow"]),
                "cancellationSegments": cancellation_segments(timeline, trial, trigger_time, end_time),
                "focusAtMatchingState": event["focus"], "fixtureAtMatchingState": event["fixture"],
                "interpretation": "Provisional literal occurrence inventory, including focus and live output; no announcement-cause attribution, no audible-completion claim and no independent acceptance."})
    matrix = {"profile": "frozen c2da profile at refreshed run start", "sourceRootRevision": root_revision,
              "sourceInventory": "native-refresh.json", "requiredWindows": 48,
              "completedWindowControls": sum(row["protocolComplete"] for row in rows.values()),
              "independentlyAcceptedWindows": 0, "windows": list(rows.values()),
              "earlierSourceAttemptsCountTowardThisMatrix": False}
    inspection = {"method": "See build-native-matrix.py; complete raw queues and state/focus timelines remain in hash-bound ZIP members. Literal counts are not independent AT assessment.", "windows": inspections}
    return matrix, inspection


def runtime_summary():
    """Derive runtime/count/cleanup facts from complete retained packets."""
    root = next(row for row in json.loads((HERE / "native-refresh.json").read_bytes())["repositories"]
                if row["path"] == ".")["after"]["revision"]
    pairings = []
    for attempt in json.loads((HERE / "native-attempt-index.json").read_bytes())["attempts"]:
        if attempt["sourceRootRevision"] != root:
            continue
        packet = HERE / attempt["packet"]
        with zipfile.ZipFile(packet) as archive:
            capture, fixture, invocation, timeline, speech = [json.loads(archive.read(name))
                for name in ("capture.json", "fixture.json", "invocation.json", "timeline.json", "speech.json")]
        successful = successful_capture(capture, fixture, invocation, timeline, attempt)
        pairings.append({"pairing": attempt["pairing"], "packet": attempt["packet"],
            "packetSha256": hashlib.sha256(packet.read_bytes()).hexdigest(),
            "browserVersion": fixture["browserVersion"], "browserEngine": fixture["browserEngine"],
            "playwrightVersion": fixture["playwrightVersion"], "nvdaVersion": capture["nvdaVersion"],
            "osVersion": capture["osVersion"], "sourceRootRevision": fixture["sourceBeforeBuild"]["sourceBaseRevision"],
            "fluentPackageVersion": fixture["fluentPackageVersion"], "fluentModuleSha256": fixture["fluentModuleSha256"],
            "uiAssemblySha256": fixture["localUiAssemblySha256"], "shellAssemblySha256": fixture["localShellAssemblySha256"],
            "fluentAssemblySha256": fixture["fluentAssemblySha256"],
            "completeWindows": sum(successful and window_controls(row, timeline, speech, attempt["pairing"])
                                   for row in fixture.get("nativeSpeechTrials", [])),
            "speechSamples": len(speech), "sourceSettingsUnchanged": capture["sourceSettingsUnchanged"],
            "sourceSettingsSha256": capture["sourceSettingsSha256"], "postRunObservedUtc": invocation["endedUtc"],
            "nvdaProcessesAfter": invocation["nvdaProcessesAfter"], "collectorPassed": capture["passed"],
            "freshBuildArguments": fixture["build"]["arguments"]})
    return {"pairings": pairings,
        "allRequiredWindowsCollected": len(pairings) == 2 and {r["pairing"] for r in pairings} == {"NVDA/Chrome", "NVDA/Firefox"}
            and sum(r["completeWindows"] for r in pairings) == 48,
        "independentAcceptancePending": True,
        "assessmentScope": "Pre-assessment runtime facts; final semantic disposition is in independent-at-assessment.json.",
        **{name: len({r[field] for r in pairings}) == 1 for name, field in
           (("uiAssemblyEqualAcrossPairings", "uiAssemblySha256"), ("shellAssemblyEqualAcrossPairings", "shellAssemblySha256"),
            ("fluentModuleEqualAcrossPairings", "fluentModuleSha256"))}}


def main():
    matrix, inspection = build()
    (HERE / "native-matrix.json").write_text(json.dumps(matrix, indent=2) + "\n")
    (HERE / "speech-inspection.json").write_text(json.dumps(inspection, indent=2) + "\n")
    (HERE / "native-runtime-summary.json").write_text(json.dumps(runtime_summary(), indent=2) + "\n")
    print(json.dumps({"completedCurrentProfileWindows": matrix["completedWindowControls"], "requiredWindows": 48,
                      "independentlyAccepted": 0}, indent=2))


if __name__ == "__main__":
    main()
