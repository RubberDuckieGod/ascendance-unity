#!/usr/bin/env python3
"""Exercise the real online Hero menu in isolated development Unity players.

The server path is independent so a candidate client can be checked against the
unchanged deployed h server. The only supported endpoint is generated loopback.
"""
from __future__ import annotations
import argparse
import copy
import json
import os
from pathlib import Path
import secrets
import struct
import subprocess
import time
import traceback
import uuid

from native_server_acceptance import NativeServer, WireClient
from native_unity_clients import Client, await_value, read_json, unfocus_and_arrange


class LobbyClient(Client):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        (self.output / ".ascendance-native-lobby").write_text("isolated-native-lobby-0102\n")
        self.local_save = self.output / "config/unity3d/Ascendance/Ascendance/campaign_q001.json"
        self.local_save.parent.mkdir(parents=True)
        self.local_save.write_text("private-local-save-sentinel-" + uuid.uuid4().hex)
        self.local_before = self.local_save.read_bytes()

    def start(self):
        self.runs += 1
        (self.output / "state.json").unlink(missing_ok=True)
        inherited = {k: v for k, v in os.environ.items() if k not in {
            "DIGITALOCEAN_ACCESS_TOKEN", "GH_TOKEN", "GITHUB_TOKEN", "OPENAI_API_KEY",
            "UNITY_SERVICE_ACCOUNT_ID", "UNITY_SERVICE_ACCOUNT_SECRET",
            "ASCENDANCE_TLS", "ASCENDANCE_CERT_SHA256", "ASCENDANCE_ROOM_ACCESS_KEY",
            "ASCENDANCE_TLS_PFX_PATH", "ASCENDANCE_TLS_PFX_PASSWORD_FILE", "ASCENDANCE_ROOM_ACCESS_KEY_FILE",
        }}
        self.process = subprocess.Popen(
            [str(self.executable), "-ascendance-connect", "127.0.0.1:" + str(self.port),
             "-room", self.room, "-player", "Lobby " + self.hero,
             "-asc-lobby-output", str(self.output),
             "-screen-width", "800", "-screen-height", "600", "-screen-fullscreen", "0",
             "-force-glcore", "-logFile", str(self.output / ("lobby-" + str(self.runs) + ".log"))],
            cwd=self.executable.parent,
            env={**inherited, "DISPLAY": self.display, "XAUTHORITY": str(self.authority),
                 "ASCENDANCE_NATIVE_LOBBY_ACCEPTANCE": "1", "XDG_CONFIG_HOME": str(self.output / "config"),
                 "XDG_DATA_HOME": str(self.output / "data"), "XDG_CACHE_HOME": str(self.output / "cache"),
                 "TMPDIR": str(self.output / "tmp"), "LIBGL_ALWAYS_SOFTWARE": "1"},
            stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
        return self.wait("real main menu", lambda s: s.get("Scene") == "MainMenu" and any(b["Name"] == "Button_JOIN_ONLINE_CAMPAIGN" for b in s.get("Buttons", [])), 50)

    def open(self):
        reply = self.request("open")
        if not reply["Accepted"]: raise AssertionError(reply)
        return self.wait("live online Hero roster", lambda s: s.get("LobbyVisible") and s.get("IsConnected") and len(s.get("Choices", [])) == 2)

    def choose(self, hero):
        reply = self.request("select", HeroId=hero)
        if not reply["Accepted"]: raise AssertionError(reply)
        return self.wait("selected " + hero, lambda s: s.get("SelectedHeroId") == hero)

    def enter(self, hero):
        self.choose(hero)
        reply = self.request("claim")
        if not reply["Accepted"]: raise AssertionError(reply)
        return self.wait("claimed " + hero + " before gameplay", lambda s: s.get("GameReady") and s.get("HasHud") and s.get("ClaimedHeroId") == hero and bool(s.get("PlayerId")), 35)


def canonical(snapshot):
    campaign = copy.deepcopy(snapshot["Campaign"])
    campaign.pop("SavedAtUtc", None)
    return {"campaign": campaign, "transient": snapshot["Transient"], "gameplayRevision": snapshot["GameplayRevision"]}


def choice(state, hero):
    return next(row for row in state["Choices"] if row["HeroId"] == hero)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--server-build-dir", type=Path, required=True)
    parser.add_argument("--client-executable", type=Path, required=True)
    parser.add_argument("--xvfb", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    output = args.output.resolve(); output.mkdir(mode=0o700)
    room = "NATIVE-LOBBY-" + uuid.uuid4().hex
    server_root = output / "server"; server_root.mkdir()
    server = NativeServer(args.server_build_dir.resolve(), server_root, room)
    report = {"kind": "actual-native-online-hero-lobby", "checks": [], "screenshots": [],
              "boundaries": ["Fresh isolated loopback fixture; no production accounts or saves.",
                             "Actual uGUI button callbacks, production lobby model and transport, native server and independent graphical players.",
                             "No audio-output certification; native host FMOD initialization can precede managed error capture."]}
    observer = None; xvfb = None; clients = []
    def check(label, expected, actual):
        row = {"name": label, "expected": expected, "actual": actual, "passed": expected == actual}
        report["checks"].append(row)
        print(("PASS " if row["passed"] else "FAIL ") + label, flush=True)
        if not row["passed"]: raise AssertionError(label)
    def snapshot():
        response = observer.exchange("SNAPSHOT")
        if not response["Accepted"]: raise AssertionError("Read-only observer snapshot rejected")
        return response["Snapshot"]
    def capture(name):
        subprocess.run(["import", "-display", display, "-window", "root", str(output / name)],
                       env={**os.environ, "XAUTHORITY": str(authority)}, check=True, timeout=15,
                       stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
        report["screenshots"].append(name)
    try:
        observer, _ = server.start(); observer.close(); observer = None; server.stop()
        fixture = json.loads(server.save_path.read_text())
        fixture["DurableCampaign"]["PartyHeroIds"] = ["Aldric", "Rowan"]
        fixture["DurableCampaign"]["PartyGold"] = 37
        server.save_path.write_text(json.dumps(fixture))
        observer, hello = server.start()
        check("Unchanged native h server accepts candidate client protocol", "0.10.2h", hello["ServerBuildVersion"])
        baseline = canonical(snapshot())
        display_number = next(n for n in range(191, 240) if not Path("/tmp/.X" + str(n) + "-lock").exists())
        display = ":" + str(display_number); authority = output / "Xauthority"
        def field(data): return struct.pack("!H", len(data)) + data
        authority.write_bytes(struct.pack("!H", 65535) + field(b"") + field(str(display_number).encode()) + field(b"MIT-MAGIC-COOKIE-1") + field(secrets.token_bytes(16)))
        authority.chmod(0o600)
        with (output / "xvfb.log").open("wb") as log:
            xvfb = subprocess.Popen([str(args.xvfb.resolve()), display, "-screen", "0", "1600x900x24", "-nolisten", "tcp", "-noreset", "-auth", str(authority)], stdout=log, stderr=subprocess.STDOUT)
        await_value("private X display", lambda: Path("/tmp/.X11-unix/X" + str(display_number)).exists(), timeout=10)
        a, b = [LobbyClient(args.client_executable.resolve(), output / name.lower(), name, server.port, room, display, authority) for name in ["Alice", "Bob"]]
        clients.extend([a, b])
        for client in [a, b]:
            initial = client.start()
            check(client.hero + " sees JOIN ONLINE CAMPAIGN instead of solo party setup", True, any(x["Label"] == "JOIN ONLINE CAMPAIGN" for x in initial["Buttons"]))
            check(client.hero + " starts without a game session", [False, None], [initial["GameReady"], initial.get("PlayerId") or None])
            state = client.open()
            check(client.hero + " sees exactly the existing server party", ["Aldric", "Rowan"], sorted(x["HeroId"] for x in state["Choices"]))
            check(client.hero + " sees available rendered Hero statuses", True, all(x["CanChoose"] and x["RenderedLabel"] for x in state["Choices"]))
            check(client.hero + " does not show remove-party controls", False, any("REMOVE SELECTED HERO" in (x["Label"] or "") for x in state["Buttons"]))
        check("Opening both real menus claims no player", 0, len(snapshot()["Players"]))
        check("Opening both real menus leaves campaign unchanged", baseline, canonical(snapshot()))
        unfocus_and_arrange(display, authority, [a.process.pid, b.process.pid])
        capture("online-lobby-before-claims.png")
        a.choose("Aldric"); a.choose("Rowan")
        selected = a.choose("Aldric")
        check("Selecting another online Hero replaces the single selected Hero", "Aldric", selected["SelectedHeroId"])
        state_a = a.enter("Aldric")
        a_id = state_a["PlayerId"]
        check("First UI claim owns only the selected Hero before gameplay", ["Aldric", 0], [state_a["ClaimedHeroId"], state_a["CommandsSent"]])
        check("UI claim does not alter the full campaign", baseline, canonical(snapshot()))
        state_b = b.wait("peer claim appears in lobby", lambda s: not choice(s, "Aldric")["CanChoose"] and choice(s, "Aldric")["ClaimedBy"] == "Lobby Alice")
        check("Peer Hero is visibly claimed and cannot be selected", [False, "Lobby Alice"], [choice(state_b, "Aldric")["CanChoose"], choice(state_b, "Aldric")["ClaimedBy"]])
        check("Claimed Hero status is rendered in actual uGUI", True, bool(choice(state_b, "Aldric")["RenderedLabel"]))
        first_turn = a.request("turn")
        check("Claimed owner starts its own first turn", [True, "Aldric"], [first_turn["Accepted"], first_turn["State"]["TurnHeroId"]])
        active_campaign = canonical(snapshot())
        state_b = b.enter("Rowan")
        check("Friend claims selected Rowan while Aldric's turn is active", ["Rowan", 0], [state_b["ClaimedHeroId"], state_b["CommandsSent"]])
        check("Friend claim during active turn preserves the full campaign", active_campaign, canonical(snapshot()))
        check("Friends have distinct confirmed player sessions", True, a_id != state_b["PlayerId"])
        b.wait("active Aldric turn replicated", lambda s: s.get("TurnHeroId") == "Aldric")
        wrong_turn = b.request("turn")
        check("Friend cannot end another Hero's turn", [False, "OTHER_HERO_TURN"], [wrong_turn["Accepted"], wrong_turn["Code"]])
        check("Wrong-turn rejection sends no gameplay command and keeps Rowan ownership", [0, "Rowan"], [wrong_turn["State"]["CommandsSent"], wrong_turn["State"]["ClaimedHeroId"]])
        check("Wrong-turn click cannot mutate the active campaign", active_campaign, canonical(snapshot()))
        end_a = a.request("turn")
        check("Aldric owner ends its own turn", True, end_a["Accepted"])
        b.wait("no active turn before own start", lambda s: not s.get("TurnHeroId"))
        own_b = b.request("turn")
        check("Friend starts its own Rowan turn", [True, "Rowan", "Rowan"], [own_b["Accepted"], own_b["State"]["TurnHeroId"], own_b["State"]["ClaimedHeroId"]])
        stable = canonical(snapshot())
        a.stop()
        await_value("disconnected Hero remains reserved", snapshot, lambda s: any(p["HeroId"] == "Aldric" and not p["Connected"] for p in s["Players"]))
        c = LobbyClient(args.client_executable.resolve(), output / "newcomputer", "NewComputer", server.port, room, display, authority); clients.append(c)
        c.start(); sc = c.open()
        check("New computer cannot take disconnected owner's reserved Hero", [False, False], [choice(sc, "Aldric")["CanChoose"], choice(sc, "Aldric")["HasSavedCredential"]])
        check("New computer sees reservation owner", "Lobby Alice", choice(sc, "Aldric")["ClaimedBy"])
        back = c.request("back")
        check("Actual Back control cancels the unclaimed lobby", True, back["Accepted"])
        c.wait("lobby closed", lambda s: not s.get("LobbyVisible"))
        check("Canceling a lobby creates no extra session", 2, len(snapshot()["Players"]))
        c.stop()
        a.start(); restored_menu = a.open()
        check("Original computer sees its saved reconnect option", [True, True], [choice(restored_menu, "Aldric")["CanChoose"], choice(restored_menu, "Aldric")["HasSavedCredential"]])
        restored = a.enter("Aldric")
        check("Menu reconnect retains the exact owner session", [a_id, "Aldric", 0], [restored["PlayerId"], restored["ClaimedHeroId"], restored["CommandsSent"]])
        check("Return through lobby during a peer turn preserves campaign", stable, canonical(snapshot()))
        check("Restored server campaign Gold remains authoritative", 37, restored["Gold"])
        for client in [a, b, c]:
            check(client.hero + " local save bytes are unchanged", True, client.local_save.read_bytes() == client.local_before)
            state = client.state() if client.process else read_json(client.output / "state.json")
            check(client.hero + " emits no managed Unity errors", [], state.get("Errors", []))
        unfocus_and_arrange(display, authority, [a.process.pid, b.process.pid])
        capture("online-lobby-world-two-owners.png")
        report["passed"] = len(report["checks"]); report["failed"] = 0
    except Exception as error:
        report["error"] = type(error).__name__ + ": " + str(error)
        report["traceback"] = traceback.format_exc()
        report["passed"] = sum(row["passed"] for row in report["checks"]); report["failed"] = 1
        print(report["traceback"], flush=True)
    finally:
        for client in clients:
            try: client.stop()
            except Exception: pass
        if observer:
            try: observer.close()
            except Exception: pass
        try: server.stop()
        except Exception as error: report["shutdownError"] = str(error); report["failed"] = report.get("failed", 0) + 1
        if xvfb:
            xvfb.terminate()
            try: xvfb.wait(timeout=6)
            except subprocess.TimeoutExpired: xvfb.kill(); xvfb.wait()
        (output / "results.json").write_text(json.dumps(report, indent=2) + "\n")
    return 1 if report.get("failed") else 0


if __name__ == "__main__":
    raise SystemExit(main())
