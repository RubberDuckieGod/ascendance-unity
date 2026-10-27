#!/usr/bin/env python3
"""Prove native case-aligned upgrade/rollback retains newer synthetic progress.

Uses only verified local native builds, loopback clients, and a newly created
temporary XDG directory. Does not accept an existing save or remote endpoint.
This is native process evidence, not a systemd or Unity GUI acceptance test.
"""
import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import time
import uuid


ROOT = Path(__file__).resolve().parents[1]


def load(name, relative):
    spec = importlib.util.spec_from_file_location(name, ROOT / relative)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def run(old_build, old_manifest, new_build, new_manifest, output):
    native = load("case_upgrade_native", "tools/native_server_acceptance.py")
    release = load("case_upgrade_release", "Deploy/DigitalOcean/release.py")
    release.verify_artifact(old_build, release.read_json(old_manifest))
    release.verify_artifact(new_build, release.read_json(new_manifest))
    report = {
        "scope": "Verified compiled Linux servers with isolated synthetic saves and loopback clients",
        "noProductionConnection": True,
        "noExistingDataRead": True,
        "checks": [],
        "limitations": [
            "No systemd, DigitalOcean, Windows player, graphical, or audio acceptance is claimed.",
            "The malformed saved-room casing defect remains in the native runtime; this proves the supported exact active-room launch plan.",
        ],
    }

    def check(name, expected, actual):
        passed = expected == actual
        report["checks"].append({"name": name, "expected": expected, "actual": actual, "passed": passed})
        if not passed:
            raise AssertionError(name)

    started = time.monotonic()
    for drift in [False, True]:
        label = "case drift" if drift else "exact case"
        server = client = None
        with tempfile.TemporaryDirectory(prefix="asc-native-case-upgrade-") as temporary:
            root = Path(temporary)
            requested = "CASE-UPGRADE-" + uuid.uuid4().hex.upper()
            active = requested.lower() if drift else requested
            try:
                server = native.NativeServer(old_build, root, requested)
                client, initial = server.start()
                check(label + ": old native bootstrap succeeds", [True, "HELLO", "0.10.2c"],
                      [initial.get("Accepted"), initial.get("Code"), initial.get("ServerBuildVersion")])
                client.close(); client = None; server.stop()
                fixture = json.loads(server.save_path.read_text())
                fixture["RoomId"] = active
                fixture["Players"] = []
                body = fixture["DurableCampaign"]
                body["PartyGold"] = 111
                body.update({"TurnHeroId": "", "SelectedHeroId": "Aldric"})
                body["PendingRewardChoice"] = {
                    "SourceCardId": "SD082", "SourceCardName": "Synthetic case upgrade reward",
                    "HeroId": "Aldric", "Mode": "Gold25OrLoot1", "Prompt": "Choose synthetic reward", "RevealedCardIds": [],
                }
                body["PendingEncounterDeckControl"] = {
                    "SourceCardId": "SD041", "SourceCardName": "Synthetic case upgrade deck",
                    "Mode": "MayBottomTop", "RevealedCardIds": body["EncounterDeckIds"][:1],
                }
                requested_path = server.save_path
                requested_path.write_text(json.dumps(fixture))
                stale_hash = hashlib.sha256(requested_path.read_bytes()).hexdigest()
                active_path = requested_path.with_name(active + ".json")
                client, loaded = server.start()
                snapshot = loaded["Snapshot"]
                check(label + ": malformed fixture restores live authority spelling", active, snapshot["RoomId"])
                check(label + ": restored native gold sentinel", 111, snapshot["Campaign"]["PartyGold"])
                check(label + ": synthetic player joins real authority", True,
                      client.join("Aldric", uuid.uuid4().hex).get("Accepted"))
                chosen = client.command(native.Command.REWARD_CHOICE, OptionKey="GOLD")
                check(label + ": real native reward command accepted", [True, 136],
                      [chosen.get("Accepted"), chosen["Snapshot"]["Campaign"]["PartyGold"]])
                kept = client.command(native.Command.ENCOUNTER_DECK_CHOICE, OptionKey="KEEP")
                check(label + ": real native deck control clears pending encounter", [True, False],
                      [kept.get("Accepted"), kept["Snapshot"]["Transient"]["HasPendingEncounterDeckControl"]])
                snapshot = client.snapshot()
                saved = json.loads(active_path.read_text())
                check(label + ": current active file contains real awarded gold", 136, saved["DurableCampaign"]["PartyGold"])
                check(label + ": active file exact room/build/revisions match HELLO", True,
                      all(saved.get(name) == snapshot.get(name) for name in ["RoomId", "BuildVersion", "Revision", "GameplayRevision"]))
                if drift:
                    check(label + ": original CLI alias remains stale", 111,
                          json.loads(requested_path.read_text())["DurableCampaign"]["PartyGold"])
                    check(label + ": original stale file is byte-for-byte untouched", stale_hash,
                          hashlib.sha256(requested_path.read_bytes()).hexdigest())
                # No player/session data is retained in the public report. These
                # variables stay inside the disposable fixture for helper checks.
                client.close(); client = None; server.stop()
                after_stop = json.loads(active_path.read_text())
                check(label + ": graceful stop preserves active progress", [active, 136],
                      [after_stop["RoomId"], after_stop["DurableCampaign"]["PartyGold"]])
                server.build, server.room = new_build, active
                client, upgraded = server.start()
                check(label + ": new native build using exact active room preserves progress", [True, active, 136],
                      [upgraded.get("Accepted"), upgraded["Snapshot"]["RoomId"], upgraded["Snapshot"]["Campaign"]["PartyGold"]])
                upgraded_saved = json.loads(active_path.read_text())
                check(label + ": new native runtime persists to same exact active file", [active, 136],
                      [upgraded_saved["RoomId"], upgraded_saved["DurableCampaign"]["PartyGold"]])
                client.close(); client = None; server.stop()
                server.build, server.room = old_build, active
                client, rollback = server.start()
                check(label + ": old native rollback with exact active room preserves newest progress", [True, active, 136],
                      [rollback.get("Accepted"), rollback["Snapshot"]["RoomId"], rollback["Snapshot"]["Campaign"]["PartyGold"]])
                check(label + ": rollback still persists awarded gold", 136,
                      json.loads(active_path.read_text())["DurableCampaign"]["PartyGold"])
                if drift:
                    check(label + ": upgrade and rollback never rewrite stale alias", stale_hash,
                          hashlib.sha256(requested_path.read_bytes()).hexdigest())
                    check(label + ": stale alias never replaces current authoritative progress", [111, 136],
                          [json.loads(requested_path.read_text())["DurableCampaign"]["PartyGold"],
                           json.loads(active_path.read_text())["DurableCampaign"]["PartyGold"]])
            except Exception as error:
                report["error"] = {"scenario": label, "type": type(error).__name__, "check": str(error) if isinstance(error, AssertionError) else "Native fixture did not complete"}
                break
            finally:
                if client:
                    client.close()
                if server:
                    server.stop()
    report["passed"] = sum(check["passed"] for check in report["checks"])
    report["failed"] = sum(not check["passed"] for check in report["checks"])
    report["status"] = "PASS" if report["failed"] == 0 and "error" not in report else "FAIL"
    report["durationSeconds"] = round(time.monotonic() - started, 3)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2) + "\n")
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--old-build-dir", type=Path, required=True)
    parser.add_argument("--old-manifest", type=Path, required=True)
    parser.add_argument("--new-build-dir", type=Path, required=True)
    parser.add_argument("--new-manifest", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    result = run(args.old_build_dir.resolve(), args.old_manifest.resolve(), args.new_build_dir.resolve(),
                 args.new_manifest.resolve(), args.output.resolve())
    print(json.dumps({key: result.get(key) for key in ["status", "passed", "failed", "error"]}))
    return 0 if result["status"] == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
