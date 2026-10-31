#!/usr/bin/env python3
"""Exercise complete enemy phases through two independent native Unity HUDs.

This accepts build paths only, starts fresh loopback-only servers, and creates
campaigns using the production online lobby. It never opens an existing save,
accepts a remote address, or adds gameplay mutation hooks to shipping builds.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import secrets
import shutil
import stat
import struct
import subprocess
import time
import traceback
import uuid
import zipfile

from native_server_acceptance import NativeServer, WireClient
from native_unity_clients import Client, SecureWireClient, await_value, body, tls_fixture, unfocus_and_arrange


def extract_validated_client(archive_path, manifest_path, destination, source_revision):
    """Refuse unreviewed/incomplete/traversing development players before execution."""
    import package_multiplayer_release as packaging
    tools = packaging.release_tools()
    require = packaging.require
    require(re.fullmatch(r"[0-9a-f]{40}", source_revision), "Invalid reviewed client source revision.")
    destination = tools.strict_path(str(Path(destination).absolute()))
    require(not destination.exists(), "Client extraction destination must be absent.")
    manifest = tools.read_json(Path(manifest_path))
    tools.validate_manifest(manifest)
    with zipfile.ZipFile(archive_path) as archive:
        entries = archive.infolist()
        require(0 < len(entries) <= 5000 and sum(e.file_size for e in entries) <= 1024 * 1024 * 1024,
                "Invalid client ZIP size or entry count.")
        names = set()
        for entry in entries:
            name, path = entry.filename, PurePosixPath(entry.filename)
            require(not path.is_absolute() and ".." not in path.parts and "\\" not in name and ":" not in name
                    and path.as_posix() == name.rstrip("/") and path.parts and path.parts[0] == "LinuxClient",
                    "Unsafe or unexpected client ZIP path.")
            mode = (entry.external_attr >> 16) & 0o170000
            require(mode in (0, stat.S_IFREG, stat.S_IFDIR) and not entry.flag_bits & 1,
                    "Client ZIP contains a link, special or encrypted entry.")
            require(name not in names, "Duplicate client ZIP path.")
            names.add(name)
        files = {str(PurePosixPath(e.filename).relative_to("LinuxClient")) for e in entries if not e.is_dir()}
        require(files == set(manifest["files"]), "Client ZIP file set differs from its reviewed manifest.")
        require({"AscendanceClient.x86_64", "UnityPlayer.so", "AscendanceClient.provenance.json"}.issubset(files)
                and any(name.startswith("AscendanceClient_Data/") for name in files), "Incomplete native client payload.")
        destination.mkdir(parents=True)
        for entry in entries:
            if entry.is_dir(): continue
            target = destination / PurePosixPath(entry.filename).relative_to("LinuxClient")
            target.parent.mkdir(parents=True, exist_ok=True)
            with archive.open(entry) as incoming, target.open("xb") as outgoing:
                shutil.copyfileobj(incoming, outgoing)
    require(packaging.snapshot(destination, tools) == manifest["files"], "Client files differ from the reviewed complete manifest.")
    packaging.provenance(destination, "AscendanceClient.provenance.json", "StandaloneLinux64", source_revision, tools)
    proof = tools.read_json(destination / "AscendanceClient.provenance.json")
    require(proof.get("build", {}).get("executeMethod") == "Ascendance.EditorTools.NativeClientAcceptanceBuilder0102.BuildLinuxClient",
            "Client was not built using the reviewed development acceptance builder.")
    for name in ["AscendanceClient.x86_64", "UnityPlayer.so"]:
        with (destination / name).open("rb") as source: header = source.read(20)
        require(len(header) == 20 and header[:6] == b"\x7fELF\x02\x01" and header[18:20] == b"\x3e\x00",
                "Expected an x86-64 little-endian native client executable/library.")
    (destination / "AscendanceClient.x86_64").chmod(0o755)
    return destination


def button_ready(state, name):
    return any(b.get("Name") == name and b.get("Active") and b.get("Interactable")
               for b in state.get("Buttons", []))


def phase_complete(state):
    return not state["IsResolving"] and not state["PendingReaction"] and (state["QuestFailed"] or state["Round"] >= 2)


def phase_action(state, snapshot, last_response_revision):
    """Distinguish a fresh interactive prompt from a queued/stale old prompt."""
    if state["ServerInstanceId"] != snapshot["ServerInstanceId"]:
        return "wait"
    transient, campaign = snapshot["Transient"], snapshot["Campaign"]
    authority_complete = (not transient["IsResolving"] and not transient["HasPendingReactionWindow"]
                          and (campaign["QuestFailed"] or campaign["Round"] >= 2))
    if authority_complete or phase_complete(state):
        return "complete"
    # The command acknowledgement records queued resolution before the coroutine
    # resumes. Its unchanged-looking reaction must not receive another decline.
    if (snapshot["GameplayRevision"] > last_response_revision and state["GameplayRevision"] >= snapshot["GameplayRevision"]
            and transient["HasPendingReactionWindow"] and not transient["IsResolving"]
            and state["PendingReaction"] and not state["IsResolving"] and button_ready(state, "ResolveReaction")):
        return "react"
    return "wait"


def combat_view(state):
    """Canonical outcome fields; selections, frames and presence are not gameplay."""
    return {
        "round": state["Round"], "failed": state["QuestFailed"], "complete": state["QuestComplete"],
        "villagers": [state["VillagersRescued"], state["VillagersAvailable"], state["VillagersLost"]],
        "heroes": sorted(({k: h[k] for k in ["Id", "Health", "Actions", "LocationId", "TurnCompleted", "Downed", "Dead"]}
                          for h in state["Heroes"]), key=lambda h: h["Id"]),
        "objects": sorted(state["QuestObjects"], key=lambda o: o["Id"]),
        "enemies": sorted(state["Enemies"], key=lambda e: e["Id"]),
        "pendingReaction": state["PendingReaction"], "isResolving": state["IsResolving"],
    }


def native_campaign_view(snapshot):
    campaign = snapshot["Campaign"]
    objects = campaign["QuestRuntime0829"]["Objects"]
    villagers = [o for o in objects if o["Type"] == "Villager"]
    party = set(campaign["PartyHeroIds"])
    return {
        "round": campaign["Round"], "failed": campaign["QuestFailed"], "complete": campaign["QuestComplete"],
        "villagers": [campaign["VillagersRescued"], sum(not o["Rescued"] and not o["Destroyed"] for o in villagers),
                      sum(o["Destroyed"] for o in villagers)],
        "heroes": sorted(({k: h[k] for k in ["Id", "Health", "Actions", "LocationId", "TurnCompleted", "Downed", "Dead"]}
                          for h in campaign["Heroes"] if h["Id"] in party), key=lambda h: h["Id"]),
        "objects": sorted(({"Id": o["ObjectId"], "Type": o["Type"], "LocationId": o["LocationId"],
                            "Rescued": o["Rescued"], "Destroyed": o["Destroyed"]} for o in objects), key=lambda o: o["Id"]),
        "enemies": sorted(({k: e[k] for k in ["Id", "Health", "Armor", "LocationId"]} for e in campaign["Enemies"]), key=lambda e: e["Id"]),
        "pendingReaction": snapshot["Transient"]["HasPendingReactionWindow"], "isResolving": snapshot["Transient"]["IsResolving"],
    }


class Acceptance:
    def __init__(self, args):
        self.args = args
        self.output = args.output.resolve()
        self.output.mkdir(mode=0o700)  # New/private output only; never reuse prior game data.
        self.report = {"kind": "native-two-Unity-HUD-complete-enemy-phases", "sourceRevision": args.source_revision,
                       "transport": "pinned TLS and private invitation" if args.tls else "loopback TCP",
                       "noProductionConnection": True, "checks": [], "scenarios": [],
                       "boundaries": ["Actual Linux development players; Windows shipping binary is not executed by this suite.",
                                      "Combat controls invoke the real HUD Button.onClick including production ownership wrappers.",
                                      "Temporary fresh campaigns only; existing player saves are not loaded or edited.",
                                      "Dice and dealt starting hands use the production game RNG."]}
        self.clients, self.observer, self.server, self.xvfb = [], None, None, None

    def check(self, name, expected, actual):
        passed = expected == actual
        self.report["checks"].append({"name": name, "expected": expected, "actual": actual, "passed": passed})
        print(("PASS " if passed else "FAIL ") + name, flush=True)
        if not passed:
            raise AssertionError(name + ": " + json.dumps({"expected": expected, "actual": actual}, default=str))

    def request(self, client, operation, **fields):
        response = client.request(operation, **fields)
        self.check(client.hero + " " + operation + " " + fields.get("TargetId", "") + " accepted", True, response["Accepted"])
        return response["State"]

    def click(self, client, name):
        client.wait("real HUD " + name + " enabled", lambda s: button_ready(s, name))
        return self.request(client, "hud-button", TargetId=name)

    def synced(self, label):
        actual = native_campaign_view(self.observer.snapshot())
        for client in self.clients:
            client.wait(label + " authoritative outcome", lambda s: combat_view(s) == actual)
            self.check(label + " " + client.hero + " replica equals native authority", actual, combat_view(client.state()))
        return actual

    def private_display(self):
        number = next(n for n in range(191, 240) if not Path("/tmp/.X" + str(n) + "-lock").exists())
        self.display = ":" + str(number)
        self.authority = self.output / "Xauthority"
        def field(value): return struct.pack("!H", len(value)) + value
        self.authority.write_bytes(struct.pack("!H", 65535) + field(b"") + field(str(number).encode())
                                   + field(b"MIT-MAGIC-COOKIE-1") + field(secrets.token_bytes(16)))
        self.authority.chmod(0o600)
        with (self.output / "xvfb.log").open("wb") as log:
            self.xvfb = subprocess.Popen([str(self.args.xvfb.resolve()), self.display, "-screen", "0", "1600x900x24",
                                         "-nolisten", "tcp", "-noreset", "-auth", str(self.authority)], stdout=log, stderr=subprocess.STDOUT)
        await_value("private X display ready", lambda: Path("/tmp/.X11-unix/X" + str(number)).exists(), timeout=10)

    def start_campaign(self, directory):
        room = "NATIVE-ACCEPTANCE-" + uuid.uuid4().hex
        directory.mkdir(mode=0o700)
        server_root = directory / "server"; server_root.mkdir()
        server_security = {"ASCENDANCE_LISTEN_LOOPBACK": "1"}
        client_security, certificate = {}, None
        if self.args.tls:
            tls_server, client_security, certificate = tls_fixture(directory)
            server_security.update(tls_server)
        factory = (lambda port, room: SecureWireClient(port, room, certificate, client_security["ASCENDANCE_ROOM_ACCESS_KEY"])) if self.args.tls else WireClient
        self.server = NativeServer(self.args.server_build_dir.resolve(), server_root, room,
                                   environment=server_security, client_factory=factory)
        self.observer, initial = self.server.start()
        self.check("Native test server protocol/build", [True, 1, "0.10.2h"],
                   [initial["Accepted"], initial["ProtocolVersion"], initial["ServerBuildVersion"]])
        for index, name in enumerate(["Aldric", "Rowan"]):
            client = Client(self.args.client_executable.resolve(), directory / name.lower(), name, self.server.port,
                            room, self.display, self.authority, security=client_security, expected_avatar_count=None)
            self.clients.append(client)
            state = client.start()
            self.check(name + " uses an independent native graphical player", [False, True, True],
                       [state["Dedicated"], state["HasHud"], state["Connected"]])
            if index == 0:
                self.request(client, "campaign-create")
        for client in self.clients:
            self.request(client, "campaign-ready")
        self.request(self.clients[0], "campaign-start")
        for client in self.clients:
            client.wait("new online campaign selected party", lambda s: s.get("AvatarCount") == 2
                        and sorted(h["Id"] for h in s.get("Heroes", [])) == ["Aldric", "Rowan"]
                        and s.get("VillagersAvailable") == 4 and s.get("Round") == 1)
        self.check("Two clients plus native server are separate OS processes", 3,
                   len({self.server.process.pid, *(c.process.pid for c in self.clients)}))
        self.check("Native clients own separate authenticated Hero sessions", True,
                   self.clients[0].state()["PlayerId"] != self.clients[1].state()["PlayerId"])
        unfocus_and_arrange(self.display, self.authority, [c.process.pid for c in self.clients])
        self.synced("fresh campaign")

    def finish_phase(self, before_health, expect_failure=False):
        a, b = self.clients
        responses = 0
        last_response_revision = -1
        deadline = time.monotonic() + 60

        def wait_for_prompt_or_completion(responder):
            def observe():
                snapshot = self.observer.snapshot()
                state = responder.state()
                return {"action": phase_action(state, snapshot, last_response_revision),
                        "clientRound": state["Round"], "clientReaction": state["PendingReaction"],
                        "clientResolving": state["IsResolving"], "clientGameplayRevision": state["GameplayRevision"],
                        "authorityRound": snapshot["Campaign"]["Round"], "authorityGameplayRevision": snapshot["GameplayRevision"]}
            return await_value("fresh real reaction button or completed enemy phase", observe,
                               lambda observation: observation["action"] != "wait", timeout=max(.1, deadline-time.monotonic()))["action"]

        while time.monotonic() < deadline:
            responder = self.clients[responses % 2]
            if wait_for_prompt_or_completion(responder) == "complete":
                break
            # Exercise the exact bug: the responding player inspects their
            # friend's Hero, then clicks the actual shared decline button.
            other = b.hero if responder is a else a.hero
            self.request(responder, "select-view", TargetId=other)
            self.check(responder.hero + " inspects other Hero during reaction", other, responder.state()["SelectedHeroId"])
            # Snapshot polling and a queued resolution can finish the entire
            # phase while the UI request is in flight. Observe completion as a
            # valid outcome, rather than waiting for a button that has vanished.
            if wait_for_prompt_or_completion(responder) == "complete":
                break
            response_state = self.request(responder, "hud-button", TargetId="ResolveReaction")
            last_response_revision = response_state["GameplayRevision"]
            responses += 1
        final = self.synced("complete enemy phase")
        self.check("Enemy phase reaches an explicit terminal state", True,
                   not final["pendingReaction"] and not final["isResolving"] and (final["round"] >= 2 or final["failed"]))
        if expect_failure:
            self.check("Three unprotected villagers are lost and Q001 fails", [True, 1, [0, 1, 3]],
                       [final["failed"], final["round"], final["villagers"]])
            self.check("Goblins consume their attacks on villagers instead of Heroes", before_health,
                       {h["Id"]: h["Health"] for h in a.state()["Heroes"]})
        else:
            self.check("Rescued villagers remain safe while Goblins attack Heroes", [4, 0, 0], final["villagers"])
            self.check("Two-player enemy phase returns to Round 2 Hero Phase", [2, False], [final["round"], final["failed"]])
            self.check("Enemy attacks cause actual authoritative Hero damage", True,
                       any(h["Health"] < before_health[h["Id"]] for h in a.state()["Heroes"]))
            self.check("At least one genuine reaction uses the other-Hero HUD inspection path", True, responses > 0)
        return final, responses

    def close_campaign(self):
        failures = []
        for client in self.clients:
            try: client.stop()
            except Exception as error: failures.append("Client shutdown: " + str(error))
        self.clients = []
        if self.observer:
            try: self.observer.close()
            except Exception as error: failures.append("Observer shutdown: " + str(error))
            self.observer = None
        if self.server:
            try: self.server.stop()
            except Exception as error: failures.append("Server shutdown: " + str(error))
            self.server = None
        if failures: raise RuntimeError("; ".join(failures))

    def run(self):
        try:
            self.private_display()
            self.start_campaign(self.output / "rescued-fallback")
            a, b = self.clients
            before_health = {h["Id"]: h["Health"] for h in a.state()["Heroes"]}
            for client in self.clients:
                self.click(client, "EndTurn")  # START TURN on the same real HUD control.
                for _ in range(2): self.click(client, "Rescue")
                self.check(client.hero + " spends two Actions rescuing two actual tokens", 1, body(client.state(), client.hero)["Actions"])
                self.click(client, "EndTurn")
            final, reactions = self.finish_phase(before_health)
            self.report["scenarios"].append({"name": "all villagers rescued, complete enemy phase and other-Hero reaction decline",
                                             "outcome": final, "acceptedHudReactionResponses": reactions})
            # Restart both the authoritative server and one GUI process. The
            # remaining client uses the production transport reconnect path.
            previous_instance = a.state()["ServerInstanceId"]
            a_id = a.state()["PlayerId"]
            self.observer.close(); self.observer = None
            self.server.stop()
            self.observer, hello = self.server.start()
            a.stop(); a.start()
            self.request(b, "reconnect")
            for client in self.clients:
                client.wait("post-combat server restart identity", lambda s: s.get("ServerInstanceId") == hello["Snapshot"]["ServerInstanceId"])
            self.check("Server restart uses a new authority identity", True, previous_instance != hello["Snapshot"]["ServerInstanceId"])
            self.check("Restarted graphical client keeps its authenticated Hero", a_id, a.state()["PlayerId"])
            self.check("Persisted combat outcome survives server and player restart", final, self.synced("post-combat restart"))
            self.close_campaign()

            self.start_campaign(self.output / "unprotected-loss")
            before_health = {h["Id"]: h["Health"] for h in self.clients[0].state()["Heroes"]}
            for client in self.clients:
                self.click(client, "EndTurn")
                self.click(client, "EndTurn")
            final, reactions = self.finish_phase(before_health, expect_failure=True)
            self.report["scenarios"].append({"name": "unprotected villagers consume three goblin attacks and fail Q001",
                                             "outcome": final, "acceptedHudReactionResponses": reactions})
            before = self.synced("failed quest quiescent")
            time.sleep(1)
            self.check("Failed quest does not keep spawning or attacking", before, self.synced("failed quest remains quiescent"))
            for client in self.clients:
                self.check(client.hero + " reports no managed runtime errors", [], client.state()["Errors"])
            self.report["passed"] = True
        except Exception:
            self.report["passed"] = False
            self.report["error"] = traceback.format_exc()
            for client in self.clients:
                try: self.report.setdefault("failureStates", {})[client.hero] = client.state()
                except Exception: pass
            print(self.report["error"], flush=True)
        finally:
            try: self.close_campaign()
            except Exception as error:
                self.report["passed"] = False; self.report["cleanupError"] = str(error)
            if self.xvfb and self.xvfb.poll() is None:
                self.xvfb.terminate()
                try: self.xvfb.wait(timeout=8)
                except subprocess.TimeoutExpired:
                    self.xvfb.kill(); self.xvfb.wait(); self.report["passed"] = False
                    self.report["displayCleanupError"] = "Private X display required forced termination"
            self.report["passedChecks"] = sum(c["passed"] for c in self.report["checks"])
            self.report["failedChecks"] = sum(not c["passed"] for c in self.report["checks"])
            (self.output / "results.json").write_text(json.dumps(self.report, indent=2) + "\n")
        return 0 if self.report.get("passed") else 1


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--server-build-dir", type=Path, required=True)
    parser.add_argument("--client-executable", type=Path, required=True)
    parser.add_argument("--xvfb", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--tls", action="store_true")
    parser.add_argument("--source-revision", required=True)
    args = parser.parse_args(argv)
    if len(args.source_revision) != 40 or any(c not in "0123456789abcdefABCDEF" for c in args.source_revision):
        parser.error("--source-revision must be the exact 40-character reviewed source commit")
    return Acceptance(args).run()


if __name__ == "__main__":
    raise SystemExit(main())
