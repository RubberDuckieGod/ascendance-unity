#!/usr/bin/env python3
"""Run a compiled native server against two independent, isolated wire clients.

This verifies the native server/protocol boundary, not Unity player presentation.
It never accepts a remote hostname or an existing campaign data directory.
"""
from __future__ import annotations

import argparse
import copy
import hashlib
import hmac
import functools
from enum import IntEnum
import ssl
import json
import os
from pathlib import Path
import signal
import socket
import struct
import subprocess
import tempfile
import time
import traceback
import uuid

MAX_FRAME = 8 * 1024 * 1024


class Command(IntEnum):
    # Protocol values from AscendanceOnlineModels0100.ServerCommandType0100.
    START_TURN = 0
    END_TURN = 1
    MOVE = 2
    BASIC_ATTACK = 3
    QUEST_PRIMARY = 7
    HEALING_POTION = 9
    REWARD_CHOICE = 12
    ENCOUNTER_DECK_CHOICE = 13


def read_exact(stream: socket.socket, size: int) -> bytes:
    chunks = bytearray()
    while len(chunks) < size:
        part = stream.recv(size - len(chunks))
        if not part:
            raise EOFError("Native server closed the test connection")
        chunks.extend(part)
    return bytes(chunks)


class WireClient:
    def __init__(self, port: int, room: str, tls_cert_sha256=None, access_key=None):
        raw = socket.create_connection(("127.0.0.1", port), timeout=5)
        raw.settimeout(10)
        try:
            if tls_cert_sha256:
                # The exact certificate pin is the trust anchor for this isolated
                # self-signed server. Verify it before sending credentials, then
                # independently check server purpose, time and loopback IP SAN.
                context = ssl.SSLContext(ssl.PROTOCOL_TLS_CLIENT)
                context.minimum_version = ssl.TLSVersion.TLSv1_2
                context.check_hostname = False
                context.verify_mode = ssl.CERT_NONE
                raw = context.wrap_socket(raw, server_hostname="127.0.0.1")
                certificate = raw.getpeercert(binary_form=True)
                fingerprint = hashlib.sha256(certificate).hexdigest()
                if not hmac.compare_digest(fingerprint, tls_cert_sha256.lower()):
                    raise ssl.SSLCertVerificationError("Native server certificate SHA-256 mismatch")
                with tempfile.TemporaryDirectory(prefix="asc-native-peer-cert-") as directory:
                    public = Path(directory) / "peer.pem"
                    public.write_text(ssl.DER_cert_to_PEM_cert(certificate))
                    verify = subprocess.run(
                        ["openssl", "verify", "-partial_chain", "-trusted", str(public),
                         "-purpose", "sslserver", "-verify_ip", "127.0.0.1", str(public)],
                        stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=5,
                    )
                    if verify.returncode:
                        raise ssl.SSLCertVerificationError("Pinned certificate failed time, purpose or loopback-IP verification")
        except BaseException:
            raw.close()
            raise
        self.socket = raw
        self.room = room
        self.player = None
        self.access_key = access_key

    def exchange(self, operation: str, **fields):
        request = {
            "ProtocolVersion": 1,
            "RequestId": uuid.uuid4().hex,
            "Operation": operation,
            "RoomId": self.room,
            **fields,
        }
        if self.access_key is not None and "RoomAccessKey" not in request:
            request["RoomAccessKey"] = self.access_key
        body = json.dumps(request, separators=(",", ":")).encode()
        self.socket.sendall(struct.pack("!I", len(body)) + body)
        size = struct.unpack("!I", read_exact(self.socket, 4))[0]
        if not 0 < size <= MAX_FRAME:
            raise ValueError("Invalid response frame length")
        response = json.loads(read_exact(self.socket, size))
        if response.get("RequestId") != request["RequestId"]:
            raise AssertionError("Response correlation does not match its request")
        return response

    def snapshot(self):
        response = self.exchange("SNAPSHOT")
        if response.get("Accepted") is not True:
            raise AssertionError("SNAPSHOT rejected: " + str(response.get("Code")))
        return response["Snapshot"]

    def join(self, hero: str, credential: str):
        response = self.exchange(
            "JOIN", HeroId=hero, DisplayName="Isolated native " + hero,
            ReconnectToken=credential,
        )
        if response.get("Accepted"):
            self.player = response["Player"]
        return response

    def reconnect(self, hero: str, credential: str):
        response = self.exchange("RECONNECT", HeroId=hero, ReconnectToken=credential)
        if response.get("Accepted"):
            self.player = response["Player"]
        return response

    def envelope(self, command_type: int, snapshot=None, **fields):
        snapshot = snapshot or self.snapshot()
        return {
            "CommandId": uuid.uuid4().hex,
            "RoomId": self.room,
            "PlayerId": self.player["PlayerId"],
            "ExpectedRevision": snapshot["Revision"],
            "HasExpectedGameplayRevision": True,
            "ExpectedGameplayRevision": snapshot["GameplayRevision"],
            "ExpectedServerInstanceId": snapshot["ServerInstanceId"],
            "Type": command_type,
            "ActorHeroId": self.player["HeroId"],
            **fields,
        }

    def command(self, command_type: int, **fields):
        return self.exchange("COMMAND", Command=self.envelope(command_type, **fields))

    def close(self):
        try:
            self.socket.shutdown(socket.SHUT_RDWR)
        except OSError:
            pass
        self.socket.close()


def hero(snapshot, hero_id):
    return next(h for h in snapshot["Campaign"]["Heroes"] if h["Id"] == hero_id)


def enemy_health(snapshot, enemy_id):
    enemy = next((e for e in snapshot["Campaign"]["Enemies"] if e["Id"] == enemy_id), None)
    return enemy["Health"] if enemy else 0


def durable_view(snapshot):
    campaign = snapshot["Campaign"]
    fields = [
        "Health", "Actions", "LocationId", "HealingPotions", "TurnCompleted",
        "DeckIds", "HandIds", "DiscardIds", "Level", "XP",
        "LootInventoryIds", "EquippedEquipmentIds", "Statuses",
    ]
    return {
        "heroes": {h: {k: hero(snapshot, h).get(k) for k in fields} for h in ["Aldric", "Rowan"]},
        "turn": snapshot["Transient"]["TurnHeroId"],
        "round": campaign["Round"],
        "gold": campaign["PartyGold"],
        "villagers": campaign["VillagersRescued"],
        "enemies": [{k: e.get(k) for k in ["Id", "Health", "LocationId"]} for e in campaign["Enemies"]],
    }


class NativeServer:
    def __init__(self, build: Path, root: Path, room: str, environment=None, client_factory=WireClient):
        self.build, self.root, self.room = build, root, room
        self.environment = dict(environment or {})
        self.client_factory = client_factory
        self.config = root / "config"
        self.config.mkdir()
        for name in ["cache", "data", "tmp"]:
            (root / name).mkdir()
        with socket.socket() as available:
            available.bind(("127.0.0.1", 0))
            self.port = available.getsockname()[1]
        self.process = None
        self.logs = []

    @property
    def save_path(self):
        return self.config / "unity3d/Ascendance/Ascendance/AscendanceServerRooms0100" / (self.room + ".json")

    def start(self):
        log = self.root / ("server-" + str(len(self.logs) + 1) + ".log")
        self.logs.append(log)
        self.process = subprocess.Popen(
            [str(self.build / "AscendanceServer.x86_64"), "-batchmode", "-nographics",
             "-ascendance-server", "-room", self.room, "-port", str(self.port), "-logFile", str(log)],
            cwd=self.build,
            env={**{k: v for k, v in os.environ.items() if not k.startswith(("ASCENDANCE_TLS_", "ASCENDANCE_ROOM_ACCESS_KEY"))},
                 **self.environment, "XDG_CONFIG_HOME": str(self.config),
                 "XDG_CACHE_HOME": str(self.root / "cache"),
                 "XDG_DATA_HOME": str(self.root / "data"), "TMPDIR": str(self.root / "tmp")},
            stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT,
        )
        deadline = time.monotonic() + 20
        while time.monotonic() < deadline:
            if self.process.poll() is not None:
                raise RuntimeError("Native server exited before readiness: " + str(self.process.returncode))
            try:
                client = self.client_factory(self.port, self.room)
                hello = client.exchange("HELLO")
                if hello.get("Accepted") and hello.get("Code") == "HELLO":
                    return client, hello
                client.close()
            except OSError:
                time.sleep(0.1)
        raise TimeoutError("Native HELLO did not become ready")

    def stop(self):
        if self.process and self.process.poll() is None:
            self.process.send_signal(signal.SIGTERM)
            try:
                self.process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                self.process.kill()
                self.process.wait()
                raise RuntimeError("Native server did not stop within ten seconds")
        self.process = None


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build-dir", type=Path, required=True)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--source-revision", help="Reviewed source commit recorded with this exact build")
    parser.add_argument("--tls-cert-sha256", help="Exact SHA-256 of the server DER certificate; no insecure fallback")
    parser.add_argument("--access-key-file", type=Path, help="Private test room access-key file; its contents are never reported")
    args = parser.parse_args()
    build = args.build_dir.resolve()
    tls_environment = {}
    access_key = None
    if bool(args.tls_cert_sha256) != bool(args.access_key_file):
        parser.error("TLS acceptance requires both a certificate pin and an access-key file")
    if args.tls_cert_sha256:
        if len(args.tls_cert_sha256) != 64 or any(c not in "0123456789abcdefABCDEF" for c in args.tls_cert_sha256):
            parser.error("Certificate SHA-256 must contain exactly 64 hexadecimal characters")
        access_key = args.access_key_file.read_text().strip()
        if len(access_key) != 64 or any(c not in "0123456789abcdefABCDEF" for c in access_key):
            parser.error("Room access-key file must contain a 64-character hexadecimal key")
        for name in ["ASCENDANCE_TLS_PFX_PATH", "ASCENDANCE_TLS_PFX_PASSWORD_FILE"]:
            if not os.environ.get(name):
                parser.error("Missing TLS server configuration variable: " + name)
        tls_environment = {k: v for k, v in os.environ.items() if k.startswith(("ASCENDANCE_TLS_", "ASCENDANCE_ROOM_ACCESS_KEY"))}
        tls_environment["ASCENDANCE_ROOM_ACCESS_KEY_FILE"] = str(args.access_key_file.resolve())
    wire = functools.partial(WireClient, tls_cert_sha256=args.tls_cert_sha256, access_key=access_key)
    manifest = json.loads(args.manifest.read_text())["files"]
    checks = []
    report = {
        "scope": "Compiled native Linux server plus two independent Python TCP clients; not Unity player/UI acceptance",
        "buildVersion": "0.10.2c", "sourceRevision": args.source_revision or "UNKNOWN_NOT_PROVEN",
        "transport": "TLS with exact DER pin, time and IP SAN verification" if args.tls_cert_sha256 else "Plain TCP with loopback test clients on an isolated high port",
        "authentication": "Private room-access-key file" if args.tls_cert_sha256 else "Legacy no room key",
        "checks": checks, "limitations": ["Independent Unity player rendering/input was not exercised."],
    }

    def check(name, expected, actual):
        passed = expected == actual
        checks.append({"name": name, "expected": expected, "actual": actual, "passed": passed})
        if not passed:
            raise AssertionError(name + ": expected " + repr(expected) + ", actual " + repr(actual))

    def accepted(name, result):
        check(name, {"Accepted": True}, {"Accepted": result.get("Accepted")} if result.get("Accepted") else {"Accepted": result.get("Accepted"), "Code": result.get("Code"), "Transient": result.get("Snapshot", {}).get("Transient")})
        return result["Snapshot"]

    started = time.monotonic()
    clients = []
    server = None
    temp = tempfile.TemporaryDirectory(prefix="asc-native-acceptance-")
    root = Path(temp.name)
    try:
        actual_files = {str(p.relative_to(build)): p for p in build.rglob("*") if p.is_file()}
        check("Build has exactly the manifest's complete file set", sorted(manifest), sorted(actual_files))
        bad = [name for name, path in actual_files.items() if path.is_symlink() or hashlib.sha256(path.read_bytes()).hexdigest() != manifest[name]]
        check("All emitted build files match reviewed SHA-256 manifest", [], bad)
        room = "NATIVE-ACCEPTANCE-" + uuid.uuid4().hex
        server = NativeServer(build, root, room, environment=tls_environment, client_factory=wire)
        bootstrap, initial = server.start(); clients.append(bootstrap)
        check("Compiled server performs protocol HELLO", [True, "HELLO", "0.10.2c", 1], [initial.get("Accepted"), initial.get("Code"), initial.get("ServerBuildVersion"), initial.get("ProtocolVersion")])
        check("Default native server party is authored four-Hero party", ["Garruk", "Rowan", "Aldric", "Elara"], initial["Snapshot"]["Campaign"]["PartyHeroIds"])
        check("Protocol mismatch is rejected", "PROTOCOL_MISMATCH", bootstrap.exchange("HELLO", ProtocolVersion=99).get("Code"))
        check("Wrong room is rejected", "ROOM_MISMATCH", bootstrap.exchange("HELLO", RoomId=room + "-WRONG").get("Code"))
        if args.tls_cert_sha256:
            for label, key in [("missing", None), ("incorrect", "0" * 64 if access_key != "0" * 64 else "1" * 64)]:
                untrusted = WireClient(server.port, room, tls_cert_sha256=args.tls_cert_sha256, access_key=key)
                try:
                    denied = untrusted.exchange("HELLO")
                finally:
                    untrusted.close()
                snapshot = denied.get("Snapshot") or {}
                # JsonUtility may serialize a null response reference as an
                # empty default DTO. Assert no actual room/Hero/session state.
                exposed = bool(snapshot.get("RoomId") or snapshot.get("Players") or (snapshot.get("Campaign") or {}).get("Heroes"))
                check("TLS request with " + label + " room key is rejected without authority data", [False, "ROOM_ACCESS_DENIED", False], [denied.get("Accepted"), denied.get("Code"), exposed])
            wrong_pin = ("1" if args.tls_cert_sha256[0] == "0" else "0") + args.tls_cert_sha256[1:]
            rejected = False
            try:
                stranger_pin = WireClient(server.port, room, tls_cert_sha256=wrong_pin, access_key=access_key)
                stranger_pin.close()
            except ssl.SSLCertVerificationError:
                rejected = True
            check("Wrong certificate pin is rejected before any credential request", True, rejected)
            plaintext_rejected = False
            plaintext = None
            try:
                plaintext = WireClient(server.port, room)
                plaintext.socket.settimeout(3)
                reply = plaintext.exchange("HELLO")
                plaintext_rejected = reply.get("Accepted") is not True
            except (OSError, EOFError, ValueError):
                plaintext_rejected = True
            finally:
                if plaintext: plaintext.close()
            check("Plaintext cannot obtain a protocol HELLO from the TLS listener", True, plaintext_rejected)
            bounded_waits = {}
            for label, open_connection in [
                ("unfinished TLS handshake", lambda: socket.create_connection(("127.0.0.1", server.port), timeout=5)),
                ("TLS connection without first room-authenticated request", lambda: wire(server.port, room).socket),
            ]:
                pending = open_connection()
                waiting_started = time.monotonic()
                pending.settimeout(7)
                closed = False
                try:
                    while time.monotonic() - waiting_started < 7:
                        if not pending.recv(1):
                            closed = True
                            break
                except TimeoutError:
                    pass  # A client read timeout is not evidence of server closure.
                except (ConnectionResetError, BrokenPipeError, ssl.SSLEOFError):
                    closed = True
                finally:
                    pending.close()
                elapsed = time.monotonic() - waiting_started
                bounded_waits[label] = elapsed
                check("Native server closes " + label + " within seven seconds", [True, True], [closed, elapsed < 7])
            report["unauthenticatedClosureSeconds"] = bounded_waits
            check("Security rejections leave the authenticated native listener healthy", [True, 0], [bootstrap.exchange("HELLO").get("Accepted"), len(bootstrap.snapshot()["Players"])])
        accepted("Fresh unmodified native party starts Aldric's real turn", bootstrap.join("Aldric", uuid.uuid4().hex))
        started_native = accepted("Fresh native StartTurn succeeds before save/restart", bootstrap.command(Command.START_TURN))
        check("Fresh native StartTurn grants three Actions", 3, hero(started_native, "Aldric")["Actions"])
        accepted("Fresh native EndTurn succeeds before restart", bootstrap.command(Command.END_TURN))
        bootstrap.close(); clients.clear(); server.stop()
        check("Native room save is created only in isolated XDG directory", True, server.save_path.is_file() and server.save_path.resolve().is_relative_to(root))

        # Restart the native player's UNMODIFIED save, before constructing any
        # synthetic gameplay fixture. JsonUtility used to create blank pending
        # choices here, permanently blocking the next Hero's StartTurn.
        restored, restored_hello = server.start(); clients.append(restored)
        transient = restored_hello["Snapshot"]["Transient"]
        check("Unmodified native save restart has no phantom pending choices", [False, False], [transient["HasPendingRewardChoice"], transient["HasPendingEncounterDeckControl"]])
        accepted("Rowan joins the unchanged restarted room", restored.join("Rowan", uuid.uuid4().hex))
        resumed = accepted("Next Hero starts after actual untouched native save/restart", restored.command(Command.START_TURN))
        check("Restored native StartTurn grants correct Actions", [3, "Rowan"], [hero(resumed, "Rowan")["Actions"], resumed["Transient"]["TurnHeroId"]])
        accepted("Rowan ends restored native turn", restored.command(Command.END_TURN))
        restored.close(); clients.clear(); server.stop()

        # Preserve partial and extension data: only entirely default optional
        # records may disappear. These cases use actual native restore/snapshot,
        # not a mirror of the normalization implementation in Python.
        pristine = json.loads(server.save_path.read_text())
        pristine["Players"] = []
        pristine["DurableCampaign"].update({"TurnHeroId": "", "SelectedHeroId": "Aldric"})
        for h in pristine["DurableCampaign"]["Heroes"]:
            h.update({"Actions": 0, "TurnCompleted": False})
        reward_fields = ["SourceCardId", "SourceCardName", "HeroId", "Mode", "Prompt", "RevealedCardIds"]
        deck_fields = ["SourceCardId", "SourceCardName", "Mode", "RevealedCardIds"]
        for kind, fields, flag in [("PendingRewardChoice", reward_fields, "HasPendingRewardChoice"), ("PendingEncounterDeckControl", deck_fields, "HasPendingEncounterDeckControl")]:
            for field in fields:
                saved = copy.deepcopy(pristine)
                body = saved["DurableCampaign"]
                body["PendingRewardChoice"] = None
                body["PendingEncounterDeckControl"] = None
                value = ["EN001"] if field == "RevealedCardIds" else "ISOLATED-EXTENSION-DATA"
                body[kind] = {field: value}
                server.save_path.write_text(json.dumps(saved))
                pending, pending_hello = server.start(); clients.append(pending)
                snap = pending_hello["Snapshot"]
                check("Native restore preserves " + kind + " with only " + field, [True, value], [snap["Transient"][flag], snap["Campaign"][kind][field]])
                pending.close(); clients.clear(); server.stop()

        # Known real choice modes must also remain usable after restore.
        saved = copy.deepcopy(pristine)
        body = saved["DurableCampaign"]
        body["PendingRewardChoice"] = {"SourceCardId": "SD082", "SourceCardName": "Isolated native reward", "HeroId": "Aldric", "Mode": "Gold25OrLoot1", "Prompt": "Choose the printed reward", "RevealedCardIds": []}
        body["PendingEncounterDeckControl"] = {"SourceCardId": "SD041", "SourceCardName": "Isolated native deck inspection", "Mode": "MayBottomTop", "RevealedCardIds": body["EncounterDeckIds"][:1]}
        server.save_path.write_text(json.dumps(saved))
        pending, pending_hello = server.start(); clients.append(pending)
        snap = pending_hello["Snapshot"]
        check("Legitimate restored reward/deck choices retain real options", [True, True, ["GOLD", "LOOT"], ["KEEP", "BOTTOM0"]], [snap["Transient"]["HasPendingRewardChoice"], snap["Transient"]["HasPendingEncounterDeckControl"], [o.split("|", 1)[0] for o in snap["Transient"]["RewardOptions"]], [o.split("|", 1)[0] for o in snap["Transient"]["EncounterDeckControlOptions"]]])
        accepted("Actor joins while legitimate choices are pending", pending.join("Aldric", uuid.uuid4().hex))
        gold_before = snap["Campaign"]["PartyGold"]
        chosen = accepted("Actual restored Gold reward resolves", pending.command(Command.REWARD_CHOICE, OptionKey="GOLD"))
        check("Restored reward grants exactly 25 Gold and clears its prompt", [gold_before + 25, False, True], [chosen["Campaign"]["PartyGold"], chosen["Transient"]["HasPendingRewardChoice"], chosen["Transient"]["HasPendingEncounterDeckControl"]])
        order = chosen["Campaign"]["EncounterDeckIds"]
        kept = accepted("Actual restored Encounter KEEP option resolves", pending.command(Command.ENCOUNTER_DECK_CHOICE, OptionKey="KEEP"))
        check("Restored KEEP preserves deck order and clears deck prompt", [order, False], [kept["Campaign"]["EncounterDeckIds"], kept["Transient"]["HasPendingEncounterDeckControl"]])
        pending.close(); clients.clear(); server.stop()

        # This is an authored synthetic restored scenario, never an existing save.
        fixture = copy.deepcopy(pristine)
        campaign = fixture["DurableCampaign"]
        campaign["PartyHeroIds"] = ["Aldric", "Rowan"]
        campaign.update({"SelectedHeroId": "Aldric", "SelectedEnemyId": "", "TurnHeroId": "", "ActiveHeroId": "", "FireTokens": 1, "VillagersRescued": 0})
        victim = next(e for e in campaign["Enemies"] if e["TemplateId"] == "E001")
        victim.update({"Health": 6, "MaxHealth": 6, "Armor": 1, "LocationId": "L000", "Statuses": []})
        campaign["Enemies"] = [victim]
        enemy_id = victim["Id"]
        campaign["QuestRuntime0829"]["QuestOwnedEnemyIds"] = [enemy_id]
        for h in campaign["Heroes"]:
            if h["Id"] in ["Aldric", "Rowan"]:
                h.update({"LocationId": "L000", "Actions": 0, "TurnCompleted": False, "HealingPotions": 1})
            if h["Id"] == "Rowan":
                h["Health"] = 4
        destination = next(loc for loc in campaign["Locations"] if loc["Id"] == "L001")
        destination.update({"Revealed": True, "RevealResolved": True, "FirstVisitResolved": True, "RevealEffectDeferred": False})
        server.save_path.write_text(json.dumps(fixture))

        a, hello = server.start(); clients.append(a)
        b = wire(server.port, room); clients.append(b)
        token_a, token_b = uuid.uuid4().hex, uuid.uuid4().hex
        first = accepted("Aldric joins native server", a.join("Aldric", token_a))
        start_command = a.envelope(Command.START_TURN, first)
        accepted("Rowan joins through independent socket", b.join("Rowan", token_b))
        a_id, b_id = a.player["PlayerId"], b.player["PlayerId"]
        snap = accepted("Presence-only revision does not reject Aldric's legal StartTurn", a.exchange("COMMAND", Command=start_command))
        check("StartTurn grants exactly three Actions and correct owner", [3, "Aldric"], [hero(snap, "Aldric")["Actions"], snap["Transient"]["TurnHeroId"]])
        check("Concurrent Rowan turn is rejected by rules", "RULE_REJECTED", b.command(Command.START_TURN).get("Code"))
        check("Foreign Hero actor is rejected", "HERO_OWNERSHIP", a.command(Command.END_TURN, ActorHeroId="Rowan").get("Code"))
        stranger = wire(server.port, room); clients.append(stranger)
        forged = b.envelope(Command.END_TURN)
        check("Unjoined socket cannot use a disclosed PlayerId", "PLAYER_NOT_BOUND", stranger.exchange("COMMAND", Command=forged).get("Code"))
        check("Claimed Hero cannot be stolen by another credential", "HERO_CLAIMED", stranger.join("Aldric", uuid.uuid4().hex).get("Code"))
        rejoin = a.join("Aldric", token_a)
        check("Repeated JOIN retains same session and exactly two players", [True, a_id, 2], [rejoin.get("Accepted"), rejoin.get("Player", {}).get("PlayerId"), len(rejoin["Snapshot"]["Players"])])

        attack = a.envelope(Command.BASIC_ATTACK, TargetId=enemy_id)
        snap = accepted("First actual BasicAttack commits", a.exchange("COMMAND", Command=attack))
        check("Iron Longsword 4 Attack against 1 Armor deals exactly three", [2, 3], [hero(snap, "Aldric")["Actions"], enemy_health(snap, enemy_id)])
        peer = b.snapshot()
        check("Peer observes actual first attack Health/Action totals", [2, 3], [hero(peer, "Aldric")["Actions"], enemy_health(peer, enemy_id)])
        duplicate = a.exchange("COMMAND", Command=attack)
        check("Immediate duplicate attack executes once", [True, True, 2, 3], [duplicate.get("Accepted"), duplicate.get("CommandResult", {}).get("Duplicate"), hero(duplicate["Snapshot"], "Aldric")["Actions"], enemy_health(duplicate["Snapshot"], enemy_id)])
        snap = accepted("Second BasicAttack commits", a.command(Command.BASIC_ATTACK, TargetId=enemy_id))
        check("Second attack defeats Raider once for one Action", [1, 0, 1], [hero(snap, "Aldric")["Actions"], enemy_health(snap, enemy_id), hero(snap, "Aldric")["KillCount"]])
        duplicate = a.exchange("COMMAND", Command=attack)
        check("Old duplicate returns fresh current state without another kill", [True, 1, 0, 1], [duplicate.get("CommandResult", {}).get("Duplicate"), hero(duplicate["Snapshot"], "Aldric")["Actions"], enemy_health(duplicate["Snapshot"], enemy_id), hero(duplicate["Snapshot"], "Aldric")["KillCount"]])
        snap = accepted("Actual native movement is accepted", a.command(Command.MOVE, TargetId="L001"))
        deadline = time.monotonic() + 5
        while (snap["Transient"]["MovingHeroIds"] or hero(snap, "Aldric")["LocationId"] != "L001") and time.monotonic() < deadline:
            time.sleep(.05); snap = a.snapshot()
        check("Connected revealed move spends one Action and completes", [0, "L001", []], [hero(snap, "Aldric")["Actions"], hero(snap, "Aldric")["LocationId"], snap["Transient"]["MovingHeroIds"]])
        snap = accepted("Aldric ends his own turn", a.command(Command.END_TURN))
        check("EndTurn marks completed and releases owner", [True, "", 0], [hero(snap, "Aldric")["TurnCompleted"], snap["Transient"]["TurnHeroId"], hero(snap, "Aldric")["Actions"]])
        snap = accepted("Rowan starts next native Hero turn", b.command(Command.START_TURN))
        check("Rowan receives three Actions", [3, "Rowan"], [hero(snap, "Rowan")["Actions"], snap["Transient"]["TurnHeroId"]])
        snap = accepted("Actual Healing Potion commits", b.command(Command.HEALING_POTION, TargetId="Rowan"))
        check("Potion recovers printed four Health, consumes one and pays one Action", [8, 0, 2], [hero(snap, "Rowan")["Health"], hero(snap, "Rowan")["HealingPotions"], hero(snap, "Rowan")["Actions"]])
        check("Peer converges to all persistent gameplay values", durable_view(snap), durable_view(a.snapshot()))
        check("Genuinely stale gameplay command is rejected", "STALE_REVISION", a.exchange("COMMAND", Command=a.envelope(Command.START_TURN, first)).get("Code"))

        idle_before = durable_view(a.snapshot())
        idle_started = time.monotonic()
        time.sleep(36)
        idle_elapsed = time.monotonic() - idle_started
        check("Two truly idle native sockets survive more than 35 seconds", [True, True, True], [idle_elapsed >= 36, a.exchange("PING").get("Accepted"), b.exchange("PING").get("Accepted")])
        check("Idle session keeps exact gameplay state", idle_before, durable_view(b.snapshot()))
        report["idleSeconds"] = idle_elapsed

        replacement = wire(server.port, room); clients.append(replacement)
        recon = replacement.reconnect("Rowan", token_b)
        check("Reconnect retains the same Hero session", [True, b_id, "Rowan"], [recon.get("Accepted"), recon.get("Player", {}).get("PlayerId"), recon.get("Player", {}).get("HeroId")])
        check("Older socket loses command ownership after reconnect", "PLAYER_NOT_BOUND", b.command(Command.END_TURN).get("Code"))
        b.close(); clients.remove(b)
        time.sleep(.15)
        view = replacement.snapshot()
        check("Closing old socket does not disconnect replacement lease", True, next(p["Connected"] for p in view["Players"] if p["PlayerId"] == b_id))
        previous_instance = view["ServerInstanceId"]
        old_uncertain_command = replacement.envelope(Command.QUEST_PRIMARY, view)
        before_restart = durable_view(view)
        for client in clients: client.close()
        clients.clear(); time.sleep(.1); server.stop()
        disk = json.loads(server.save_path.read_text())
        check("Completed native gameplay is actually durable on disk", [8, 2, 0, "L001"], [next(h["Health"] for h in disk["DurableCampaign"]["Heroes"] if h["Id"] == "Rowan"), next(h["Actions"] for h in disk["DurableCampaign"]["Heroes"] if h["Id"] == "Rowan"), next(h["HealingPotions"] for h in disk["DurableCampaign"]["Heroes"] if h["Id"] == "Rowan"), next(h["LocationId"] for h in disk["DurableCampaign"]["Heroes"] if h["Id"] == "Aldric")])
        a2, restarted = server.start(); clients.append(a2)
        b2 = wire(server.port, room); clients.append(b2)
        check("Restart creates a fresh server-process identity", True, restarted["Snapshot"]["ServerInstanceId"] != previous_instance)
        check("Restart restores exact Hero decks/Health/Actions/inventory/movement", before_restart, durable_view(restarted["Snapshot"]))
        check("Restart requires fresh reconnect proof for disconnected players", [False, False], [p["Connected"] for p in restarted["Snapshot"]["Players"]])
        ra = a2.reconnect("Aldric", token_a); rb = b2.reconnect("Rowan", token_b)
        check("Both native sessions reconnect across actual process restart", [True, a_id, True, b_id], [ra.get("Accepted"), ra.get("Player", {}).get("PlayerId"), rb.get("Accepted"), rb.get("Player", {}).get("PlayerId")])
        stale = b2.exchange("COMMAND", Command=old_uncertain_command)
        check("Uncertain previous-process command cannot replay", "SERVER_RESTARTED", stale.get("Code"))
        check("Rejected replay preserves post-restart Actions and objective", [2, 0], [hero(b2.snapshot(), "Rowan")["Actions"], b2.snapshot()["Campaign"]["VillagersRescued"]])
        snap = accepted("A new post-restart objective command commits", b2.command(Command.QUEST_PRIMARY))
        check("Real rescue after restart spends one Action and rescues one Villager", [1, 1, 8], [hero(snap, "Rowan")["Actions"], snap["Campaign"]["VillagersRescued"], hero(snap, "Rowan")["Health"]])
        check("Both independent clients converge after restart gameplay", durable_view(snap), durable_view(a2.snapshot()))
        report["status"] = "PASS"
    except Exception as error:
        report["status"] = "FAIL"
        report["error"] = type(error).__name__ + ": " + str(error)
        if not any(not c["passed"] for c in checks):
            checks.append({"name": "Native scenario reaches every required acceptance step",
                           "expected": "No unexpected native startup or protocol exception",
                           "actual": report["error"], "passed": False})
        report["remainingScenarios"] = "Not executed after the first failure; not passing."
        traceback.print_exc()
    finally:
        for client in clients:
            try: client.close()
            except OSError: pass
        if server:
            try: server.stop()
            except Exception as error:
                report["status"] = "FAIL"; report["shutdownError"] = str(error)
            output = args.output.resolve(); output.parent.mkdir(parents=True, exist_ok=True)
            logs = output.parent / (output.stem + "-logs"); logs.mkdir(exist_ok=True)
            report["logs"] = []
            for source in server.logs:
                if source.exists():
                    destination = logs / source.name
                    destination.write_bytes(source.read_bytes())
                    report["logs"].append(str(destination))
            report["isolatedDataDirectory"] = str(root)
            report["nativeRoomSaveWasUnderIsolatedXdgDirectory"] = server.save_path.is_file() and server.save_path.is_relative_to(root)
        report["durationSeconds"] = time.monotonic() - started
        report["passed"] = sum(c["passed"] for c in checks)
        report["failed"] = sum(not c["passed"] for c in checks)
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(report, indent=2) + "\n")
        temp.cleanup()
    print(json.dumps({k: report.get(k) for k in ["status", "passed", "failed", "durationSeconds", "error"]}))
    return 0 if report.get("status") == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
