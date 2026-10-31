#!/usr/bin/env python3
"""Verify real full-roster campaign creation, start, archive and restore.

Only generated loopback rooms and private temporary save/profile directories are
supported. The driver clicks production uGUI callbacks in independent players.
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
import traceback
import uuid

from native_server_acceptance import NativeServer, WireClient
from native_unity_clients import SecureWireClient, await_value, read_json, tls_fixture, unfocus_and_arrange
from native_lobby_clients import LobbyClient, choice

ROOT = Path(__file__).resolve().parents[1]


def saved_game(snapshot):
    game = copy.deepcopy(snapshot["Campaign"])
    game.pop("SavedAtUtc", None)
    return game


class CampaignClient(LobbyClient):
    def open(self):
        reply = self.request("open")
        if not reply["Accepted"]:
            raise AssertionError("The production JOIN ONLINE CAMPAIGN button was unavailable")
        return self.wait("online campaign roster", lambda s: s.get("LobbyVisible") and s.get("IsConnected")
                         and not s.get("IsBusy") and len(s.get("Choices", [])) >= 1)

    def click(self, operation, **fields):
        reply = self.request(operation, **fields)
        if not reply["Accepted"]:
            raise AssertionError(operation + " callback unavailable: " + str(reply.get("Code")))
        return reply

    def claim_draft(self, hero):
        self.choose(hero)
        self.click("claim")
        return self.wait("confirmed draft claim " + hero, lambda s: s.get("IsDraft") and not s.get("IsBusy")
                         and s.get("ClaimedHeroId") == hero and bool(s.get("PlayerId")))

    def menu(self):
        self.click("menu")
        return self.wait("normal return to menu", lambda s: s.get("Scene") == "MainMenu" and not s.get("GameReady"))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--server-build-dir", type=Path, required=True)
    parser.add_argument("--client-executable", type=Path, required=True)
    parser.add_argument("--xvfb", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--secure", action="store_true")
    args = parser.parse_args()
    output = args.output.resolve(); output.mkdir(mode=0o700)
    room = "NATIVE-LOBBY-CAMPAIGN-" + uuid.uuid4().hex
    roster = {row["id"]: row for row in json.loads((ROOT / "Assets/Resources/Ascendance/Data/hero_roster.json").read_text())["heroes"]}
    authored_cards = json.loads((ROOT / "Assets/Resources/Ascendance/Data/hero_cards.json").read_text())["cards"]
    server_environment, client_environment, certificate = {}, {}, None
    if args.secure:
        server_environment, client_environment, certificate = tls_fixture(output)
    factory = (lambda port, requested_room: SecureWireClient(port, requested_room, certificate, client_environment["ASCENDANCE_ROOM_ACCESS_KEY"])) if args.secure else WireClient
    server_root = output / "server"; server_root.mkdir()
    server = NativeServer(args.server_build_dir.resolve(), server_root, room, environment=server_environment, client_factory=factory)
    report = {"kind": "actual-native-full-roster-online-campaign", "transport": "pinned-TLS-and-room-key" if args.secure else "plain-loopback",
              "checks": [], "screenshots": [], "boundaries": [
                  "Generated private loopback room and independent disposable player profiles; no production saves or credentials.",
                  "Production uGUI button callbacks, actual native server, full-roster claims and same-connection world handoff.",
                  "Linux development players; this is not Windows gameplay or audio-output certification.",
                  "The host may emit FMOD initialization errors before managed diagnostic capture; logs are retained."]}
    clients = []; observer = None; xvfb = None
    def check(name, expected, actual):
        item = {"name": name, "expected": expected, "actual": actual, "passed": expected == actual}
        report["checks"].append(item)
        print(("PASS " if item["passed"] else "FAIL ") + name, flush=True)
        if not item["passed"]: raise AssertionError(name)
    def snapshot():
        reply = observer.exchange("SNAPSHOT")
        if not reply.get("Accepted"): raise AssertionError("Read-only observer snapshot refused")
        return reply["Snapshot"]
    def frames(players, lobby):
        markers = {player: player.state()["Frame"] for player in players}
        for player in players:
            player.wait("fresh rendered lobby" if lobby else "fresh rendered world",
                        lambda s, player=player: s.get("Frame", 0) >= markers[player] + 15
                        and (s.get("LobbyVisible") and not s.get("IsBusy") if lobby else s.get("GameReady") and s.get("HasHud")), 20)
    def capture(name):
        subprocess.run(["import", "-display", display, "-window", "root", str(output / name)], check=True,
                       env={**os.environ, "XAUTHORITY": str(authority)}, timeout=15, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
        report["screenshots"].append(name)
    try:
        observer, _ = server.start(); observer.close(); observer = None; server.stop()
        fixture = json.loads(server.save_path.read_text())
        game = fixture["DurableCampaign"]
        game["PartyHeroIds"] = ["Aldric", "Rowan"]; game["PartyGold"] = 73; game["Round"] = 4
        game["TurnHeroId"] = None; game["SelectedHeroId"] = "Aldric"
        for hero in game["Heroes"]:
            hero["Actions"] = 0; hero["TurnCompleted"] = False
            if hero["Id"] == "Aldric": hero.update(Level=3, XP=260, Health=5)
        fixture["Players"] = []
        server.save_path.write_text(json.dumps(fixture))
        observer, hello = server.start()
        check("Candidate server exposes campaign setup capability", True, bool(hello["Snapshot"].get("CampaignLobby", {}).get("Supported")))
        baseline = saved_game(snapshot())
        check("The existing progressed campaign is the initial active slot", [73, 4, ["Aldric", "Rowan"]],
              [baseline["PartyGold"], baseline["Round"], baseline["PartyHeroIds"]])
        number = next(i for i in range(191, 240) if not Path("/tmp/.X" + str(i) + "-lock").exists())
        display = ":" + str(number); authority = output / "Xauthority"
        def field(value): return struct.pack("!H", len(value)) + value
        authority.write_bytes(struct.pack("!H", 65535) + field(b"") + field(str(number).encode()) + field(b"MIT-MAGIC-COOKIE-1") + field(secrets.token_bytes(16)))
        authority.chmod(0o600)
        with (output / "xvfb.log").open("wb") as log:
            xvfb = subprocess.Popen([str(args.xvfb.resolve()), display, "-screen", "0", "1600x900x24", "-nolisten", "tcp", "-noreset", "-auth", str(authority)], stdout=log, stderr=subprocess.STDOUT)
        await_value("private X display", lambda: Path("/tmp/.X11-unix/X" + str(number)).exists(), timeout=10)
        a, b = [CampaignClient(args.client_executable.resolve(), output / name.lower(), name, server.port, room, display, authority,
                               security=client_environment) for name in ["Alice", "Bob"]]
        clients.extend([a, b])
        initial_a = a.start(); b.start()
        check("Main menu offers both a new online campaign and saved campaigns", [True, True],
              [any(x["Label"] == "NEW ONLINE CAMPAIGN" for x in initial_a["Buttons"]), any(x["Label"] == "SAVED CAMPAIGNS" for x in initial_a["Buttons"])])
        old_menu = a.open()
        check("Joining the old campaign still shows its saved party", ["Aldric", "Rowan"], sorted(x["HeroId"] for x in old_menu["Choices"]))
        old_owner = a.enter("Aldric"); original_id = old_owner["PlayerId"]
        check("Existing campaign join retains its progressed Gold and Round", [73, 4, "Aldric", 0],
              [old_owner["Gold"], old_owner["Round"], old_owner["ClaimedHeroId"], old_owner["CommandsSent"]])
        check("Joining does not reset the complete existing campaign", baseline, saved_game(snapshot()))
        a.menu()
        await_value("original socket disconnect", snapshot, lambda s: all(not p["Connected"] for p in s["Players"]))
        a.open(); a.click("new")
        confirm = a.wait("explicit new-campaign confirmation", lambda s: s.get("ConfirmationVisible"))
        check("New campaign requires an explicit confirmation before replacement", True, "FRESH ONLINE CAMPAIGN" in confirm["ConfirmationTitle"])
        check("The confirmation alone does not alter the old campaign", baseline, saved_game(snapshot()))
        a.click("confirm-new")
        draft_a = a.wait("new sixteen-Hero draft", lambda s: s.get("IsDraft") and not s.get("IsBusy") and len(s.get("Choices", [])) == 16)
        draft_b = b.open()
        check("The creator sees exactly all sixteen authored Hero IDs", sorted(roster), sorted(c["HeroId"] for c in draft_a["Choices"]))
        check("A friend independently sees all sixteen rather than the old four", sorted(roster), sorted(c["HeroId"] for c in draft_b["Choices"]))
        check("All sixteen Hero buttons have visible rendered rectangles", [16, True],
              [draft_a["HeroGridVisible"], all(c["VisibleRect"] and c["RenderedName"] and c["RenderedLabel"] for c in draft_a["Choices"])])
        check("The old campaign appears as a separately saved slot", True,
              any(s["Id"] == "original" and not s["IsActive"] for s in draft_a["CampaignLobby"]["Slots"]))
        draft_id = draft_a["CampaignLobby"]["CampaignId"]
        check("A fresh draft has no inherited player claims", 0, len(snapshot()["Players"]))
        unfocus_and_arrange(display, authority, [a.process.pid, b.process.pid]); frames([a, b], True)
        capture("campaign-full-sixteen-roster.png")
        chosen_a = a.claim_draft("Vaelis"); chosen_b = b.claim_draft("Mira")
        check("Each client claims one non-default Hero without entering gameplay", ["Vaelis", "Mira", False, False],
              [chosen_a["ClaimedHeroId"], chosen_b["ClaimedHeroId"], chosen_a["GameReady"], chosen_b["GameReady"]])
        draft_owner_a, draft_owner_b = chosen_a["PlayerId"], chosen_b["PlayerId"]
        check("Two claims create exactly two distinct owned Heroes", [2, ["Mira", "Vaelis"]],
              [len(snapshot()["Players"]), sorted(p["HeroId"] for p in snapshot()["Players"])])
        peer = b.wait("peer full-roster claim label", lambda s: not choice(s, "Vaelis")["CanChoose"])
        check("A claimed non-default Hero is disabled on the other computer", False, choice(peer, "Vaelis")["CanChoose"])
        premature = a.request("start")
        check("START is disabled before players mark Ready", [False, "BUTTON_UNAVAILABLE"], [premature["Accepted"], premature["Code"]])
        a.click("ready"); b.click("ready")
        ready_a = a.wait("creator ready START", lambda s: s.get("CanStart") and s.get("IsReady"))
        ready_b = b.wait("peer marked ready", lambda s: s.get("IsReady") and not s.get("IsBusy"))
        check("Only the creator can start the ready party", [True, False], [ready_a["CanStart"], ready_b["CanStart"]])
        check("Readiness has not submitted gameplay commands", [0, 0], [ready_a["CommandsSent"], ready_b["CommandsSent"]])
        frames([a, b], True); capture("campaign-two-ready-nondefault-heroes.png")
        # Simulate a lost client rather than intentionally leaving the draft:
        # normal Back/Quit releases the player's unstarted choice by design.
        for client in [a, b]:
            client.process.kill(); client.process.wait(timeout=10); client.process = None
        await_value("crashed draft clients remain reserved", snapshot, lambda s: len(s["Players"]) == 2 and all(not p["Connected"] for p in s["Players"]))
        observer.close(); observer = None; server.stop()
        observer, _ = server.start()
        resumed_draft = snapshot()
        check("Actual process restart retains the unstarted campaign and both ready reservations", [True, draft_id, [draft_owner_a, draft_owner_b], [True, True]],
              [resumed_draft["CampaignLobby"]["IsDraft"], resumed_draft["CampaignLobby"]["CampaignId"],
               [p["PlayerId"] for p in resumed_draft["Players"]], [p["Ready"] for p in resumed_draft["Players"]]])
        a.start(); b.start(); recovered_menu_a = a.open(); b.open()
        check("Creator computer retains campaign management ownership through draft restart", True, recovered_menu_a["CanCreate"])
        recovered_a = a.claim_draft("Vaelis"); recovered_b = b.claim_draft("Mira")
        check("Both computers reclaim their original draft identities and Ready state", [draft_owner_a, draft_owner_b, True, True],
              [recovered_a["PlayerId"], recovered_b["PlayerId"], recovered_a["IsReady"], recovered_b["IsReady"]])
        ready_a = a.wait("reconnected creator ready START", lambda s: s.get("CanStart") and s.get("IsReady"))
        check("Draft restart preserves the separate original campaign slot", True,
              any(s["Id"] == "original" and not s["IsActive"] for s in ready_a["CampaignLobby"]["Slots"]))
        a.click("start")
        started_a = a.wait("creator same-socket fresh world", lambda s: s.get("GameReady") and s.get("HasHud") and s.get("ClaimedHeroId") == "Vaelis", 40)
        started_b = b.wait("friend same-socket fresh world", lambda s: s.get("GameReady") and s.get("HasHud") and s.get("ClaimedHeroId") == "Mira", 40)
        fresh = snapshot(); heroes = {h["Id"]: h for h in fresh["Campaign"]["Heroes"]}
        check("Started party contains only the claimed full-roster Heroes", ["Mira", "Vaelis"], sorted(fresh["Campaign"]["PartyHeroIds"]))
        check("Both draft socket owners carry into gameplay unchanged", [draft_owner_a, draft_owner_b], [started_a["PlayerId"], started_b["PlayerId"]])
        check("A genuinely new campaign starts Q001 Round1 with fresh Gold", ["Q001", 1, 0],
              [fresh["Campaign"]["QuestId"], fresh["Campaign"]["Round"], fresh["Campaign"]["PartyGold"]])
        check("Selected Heroes start Level1 and zero XP", [[1, 0], [1, 0]], [[heroes[h]["Level"], heroes[h]["XP"]] for h in ["Vaelis", "Mira"]])
        check("Selected Heroes start at authored maximum Health", [roster[h]["maxHealth"] for h in ["Vaelis", "Mira"]], [heroes[h]["Health"] for h in ["Vaelis", "Mira"]])
        check("Each selected Hero has five cards, thirty total and no free actions", [[5, 30, 0], [5, 30, 0]],
              [[len(heroes[h]["HandIds"]), sum(len(heroes[h][z]) for z in ["HandIds", "DeckIds", "DiscardIds"]), heroes[h]["Actions"]] for h in ["Vaelis", "Mira"]])
        check("The active server gives each selected Hero the exact authored personal card multiset",
              [sorted(c["id"] for c in authored_cards if c["heroId"] == h for _ in range(max(1, c.get("copies", 0)))) for h in ["Vaelis", "Mira"]],
              [sorted(card for zone in ["HandIds", "DeckIds", "DiscardIds"] for card in heroes[h][zone]) for h in ["Vaelis", "Mira"]])
        check("The active server equips the exact authored starting gear and copies",
              [sorted(e["id"] for e in roster[h]["startingEquipment"] for _ in range(max(1, e.get("copies", 0)))) for h in ["Vaelis", "Mira"]],
              [sorted(heroes[h]["EquippedEquipmentIds"]) for h in ["Vaelis", "Mira"]])
        check("Both selected Heroes enter Stonebrook with starting equipment", True,
              all(heroes[h]["LocationId"] == "L000" and heroes[h]["EquippedEquipmentIds"] for h in ["Vaelis", "Mira"]))
        check("Creation and start require no gameplay action submission", [0, 0], [started_a["CommandsSent"], started_b["CommandsSent"]])
        unfocus_and_arrange(display, authority, [a.process.pid, b.process.pid]); frames([a, b], False)
        capture("campaign-fresh-two-nondefault-worlds.png")
        a.menu(); b.menu()
        await_value("new campaign players return to menu", snapshot, lambda s: all(not p["Connected"] for p in s["Players"]))
        a.click("saves")
        saved_menu = a.wait("loadable original campaign list", lambda s: s.get("CampaignListVisible") and s.get("CanCreate")
                            and any(x["Id"] == "original" for x in (s.get("CampaignLobby") or {}).get("Slots", [])))
        check("Saved campaign list retains original and new campaign", True,
              {"original", draft_id}.issubset({x["Id"] for x in saved_menu["CampaignLobby"]["Slots"]}))
        a.click("select-save", SlotId="original"); a.click("load-save")
        a.wait("saved load confirmation", lambda s: s.get("ConfirmationVisible"))
        a.click("confirm-load")
        a.wait("original party loaded", lambda s: s.get("LobbyVisible") and not s.get("IsBusy") and s.get("PartyHeroIds") == ["Aldric", "Rowan"])
        check("Loading restores the complete original progressed campaign", baseline, saved_game(snapshot()))
        restored_owner = a.enter("Aldric")
        check("Original player reconnects to original Hero session and progression", [original_id, "Aldric", 73, 4],
              [restored_owner["PlayerId"], restored_owner["ClaimedHeroId"], restored_owner["Gold"], restored_owner["Round"]])
        check("Original Hero retains Health, Level and XP", [5, 3, 260],
              [next(h for h in snapshot()["Campaign"]["Heroes"] if h["Id"] == "Aldric")[k] for k in ["Health", "Level", "XP"]])
        a.stop(); b.stop(); observer.close(); observer = None; server.stop()
        observer, _ = server.start()
        check("Actual process restart retains the active original campaign", baseline, saved_game(snapshot()))
        a.start(); a.click("saves")
        after_restart = a.wait("saved slots after actual server restart", lambda s: s.get("CampaignListVisible") and s.get("CanCreate")
                              and len((s.get("CampaignLobby") or {}).get("Slots", [])) >= 2)
        check("Server and player restart retain creator ownership and both slots", True,
              after_restart["CanCreate"] and {"original", draft_id}.issubset({s["Id"] for s in after_restart["CampaignLobby"]["Slots"]}))
        check("The read-only public snapshot contains no credential field values", False,
              any(key in json.dumps(snapshot()) for key in ['"CampaignOwnerToken0102"', '"ReconnectToken"']))
        for client in clients:
            check(client.hero + " local campaign save remains unchanged", True, client.local_save.read_bytes() == client.local_before)
            state = client.state() if client.process else read_json(client.output / "state.json")
            check(client.hero + " emits no managed diagnostic errors", [], state.get("Errors", []))
        report["passed"] = len(report["checks"]); report["failed"] = 0
    except Exception as error:
        report["error"] = type(error).__name__ + ": " + str(error)
        report["traceback"] = traceback.format_exc(); report["failed"] = 1
        report["passed"] = sum(x["passed"] for x in report["checks"])
        print(report["traceback"], flush=True)
    finally:
        for client in clients:
            try: client.stop()
            except Exception: pass
        if observer:
            try: observer.close()
            except Exception: pass
        try: server.stop()
        except Exception as error:
            report["shutdownError"] = str(error); report["failed"] = report.get("failed", 0) + 1
        if xvfb:
            xvfb.terminate()
            try: xvfb.wait(timeout=6)
            except subprocess.TimeoutExpired: xvfb.kill(); xvfb.wait()
        (output / "results.json").write_text(json.dumps(report, indent=2) + "\n")
    return 1 if report.get("failed") else 0


if __name__ == "__main__":
    raise SystemExit(main())
