#!/usr/bin/env python3
"""Prove original-h campaign preservation with actual isolated native players.

The only saves accepted by this driver are generated here through the reviewed
original binary. It never accepts an existing save directory or remote address.
Only results.json is suitable for publication; private fixtures remain private.
"""
from __future__ import annotations

import argparse
import copy
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import shutil
import socket
import time
import uuid

from native_server_acceptance import Command, NativeServer, WireClient, hero
from native_unity_clients import tls_fixture


ROOT = Path(__file__).resolve().parents[1]
ORIGINAL_REVISION = "55d25c03a3279a8396d5b8954e73180e08f7a0c5"
ORIGINAL_ASSEMBLY = "ee6b1db4ed7f78d72dcbccb436d5510d39a18f582b10cb72a059ed3411916ec2"
EMPTY_METADATA = ("CampaignId0102", "CampaignName0102", "CampaignOwnerToken0102",
                  "CampaignCreatorPlayerId0102", "LastCampaignActionId0102", "LastCampaignActionCode0102")


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


release = module("native_h_migration_release", ROOT / "Deploy/DigitalOcean/release.py")
upgrade = module("native_h_migration_upgrade", ROOT / "Deploy/DigitalOcean/upgrade-existing.py")


def require(condition, label):
    if not condition:
        raise AssertionError(label)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path):
    return release.read_json(path)


def exact_campaign(value):
    """Use reviewed optional-state equivalences, retaining actual enemy order."""
    normalized = upgrade.semantic_campaign(value)
    normalized["Enemies"] = copy.deepcopy(value["Enemies"])
    return normalized


def private_identity(players):
    value = copy.deepcopy(players)
    for player in value:
        player.pop("Connected", None)
        player.pop("LastSeenUtc", None)
        if player.get("ReadyForCampaign0102", False) is False:
            player.pop("ReadyForCampaign0102", None)
    return value


def listener_addresses(server):
    inodes = set()
    for entry in (Path("/proc") / str(server.process.pid) / "fd").iterdir():
        try:
            target = os.readlink(entry)
        except FileNotFoundError:
            continue
        if target.startswith("socket:["):
            inodes.add(target[8:-1])
    result = []
    for line in Path("/proc/net/tcp").read_text().splitlines()[1:]:
        fields = line.split()
        address, port = fields[1].split(":")
        if fields[3] == "0A" and int(port, 16) == server.port and fields[9] in inodes:
            result.append(socket.inet_ntoa(bytes.fromhex(address)[::-1]))
    return sorted(result)


def run(args):
    original, candidate = args.original_build.resolve(), args.candidate_build.resolve()
    for build, manifest in [(original, args.original_manifest), (candidate, args.candidate_manifest)]:
        release.verify_artifact(build, read(manifest))
    original_provenance = read(original / "AscendanceServer.provenance.json")
    candidate_provenance = read(candidate / "AscendanceServer.provenance.json")
    require(original_provenance["source"] == {"vcs": "git", "revision": ORIGINAL_REVISION, "dirty": False},
            "The original build is not the reviewed clean h source")
    require(digest(original / "AscendanceServer_Data/Managed/Assembly-CSharp.dll") == ORIGINAL_ASSEMBLY,
            "The original h managed assembly differs from the independently validated release")
    require(re.fullmatch("[0-9a-f]{40}", args.expected_candidate_revision) is not None
            and candidate_provenance["source"] == {"vcs": "git", "revision": args.expected_candidate_revision, "dirty": False},
            "Candidate provenance is not the requested clean source revision")
    schema_path = ROOT / "Deploy/DigitalOcean/save-schema-0102g.json"
    schema = read(schema_path)
    require(schema["rootType"] == "Ascendance.Online.PersistedServerRoom0100"
            and len(schema["types"]) == 24 and sum(len(fields) for fields in schema["types"].values()) == 423,
            "The reviewed full-room schema identity differs")
    output = args.output.absolute()
    require(not output.exists() and not output.is_symlink() and output.parent.is_dir(), "Output directory must be new")
    output.mkdir(mode=0o700)
    private = output / "private"; private.mkdir(mode=0o700)
    report = {"kind": "actual-original-h-to-full-roster-campaign-preservation", "status": "RUNNING",
              "sourceBuildVersion": "0.10.2h", "candidateBuildVersion": "0.10.2h",
              "originalSourceRevision": ORIGINAL_REVISION, "candidateSourceRevision": args.expected_candidate_revision,
              "originalManagedAssemblySha256": ORIGINAL_ASSEMBLY,
              "candidateManagedAssemblySha256": digest(candidate / "AscendanceServer_Data/Managed/Assembly-CSharp.dll"),
              "originalManifestSha256": digest(args.original_manifest), "candidateManifestSha256": digest(args.candidate_manifest),
              "schemaSha256": digest(schema_path), "transport": "pinned-TLS-and-room-key" if args.secure else "plain-loopback",
              "noProductionConnection": True, "noExistingSaveRead": True,
              "gameplayCommandsSubmittedToCopiedRoom": 0, "checks": [], "limitations": [
                  "Generated isolated native Linux campaign; no production save, service, firewall or Windows gameplay is touched.",
                  "Read-only copied-room campaign proof precedes explicit reconnect verification; copied-room gameplay commands remain zero."]}
    servers, clients, counts = [], [], {"original": 0, "candidate": 0}
    started = time.monotonic()

    def check(name, expected, actual):
        item = {"name": name, "expected": expected, "actual": actual, "passed": expected == actual}
        report["checks"].append(item)
        print(("PASS " if item["passed"] else "FAIL ") + name, flush=True)
        require(item["passed"], name)

    try:
        environment, access, pin = {"ASCENDANCE_LISTEN_LOOPBACK": "1"}, None, None
        if args.secure:
            tls_environment, client_environment, _ = tls_fixture(private)
            environment.update(tls_environment)
            access, pin = client_environment["ASCENDANCE_ROOM_ACCESS_KEY"], client_environment["ASCENDANCE_CERT_SHA256"]

        def factory(phase):
            class CountedClient(WireClient):
                def exchange(self, operation, **fields):
                    if operation.upper() == "COMMAND": counts[phase] += 1
                    return super().exchange(operation, **fields)
            return lambda port, room: CountedClient(port, room, tls_cert_sha256=pin, access_key=access)

        room = "H-MIGRATION-" + uuid.uuid4().hex
        source_root, target_root = private / "original", private / "candidate"
        source_root.mkdir(mode=0o700); target_root.mkdir(mode=0o700)
        source = NativeServer(original, source_root, room, environment=environment, client_factory=factory("original"))
        target = NativeServer(candidate, target_root, room, environment=environment, client_factory=factory("candidate"))
        servers.extend([source, target])

        def start(server):
            client, hello = server.start(); clients.append(client)
            check(("Original" if server is source else "Candidate") + " HELLO is accepted", True,
                  hello.get("Accepted") is True and hello.get("Code") == "HELLO" and hello.get("ServerBuildVersion") == "0.10.2h")
            check(("Original" if server is source else "Candidate") + " actual listener is loopback only", ["127.0.0.1"], listener_addresses(server))
            return client, hello["Snapshot"]

        def close(client):
            client.close(); clients.remove(client)

        def plan(server):
            stat = server.save_path.stat()
            return {"process": {"room": room, "uid": stat.st_uid, "gid": stat.st_gid, "wireBuildVersion": "0.10.2h"},
                    "persistence": {"expectedDataDirectory": {"path": str(server.save_path.parent.parent)}}}

        def saved(server):
            return upgrade.read_private_room(plan(server), schema, "0.10.2h")

        client, initial = start(source)
        check("Generated original h starts with the authored four-Hero party", ["Garruk", "Rowan", "Aldric", "Elara"], initial["Campaign"]["PartyHeroIds"])
        credentials, identities = {}, {}
        for hero_id in ("Aldric", "Rowan"):
            credentials[hero_id] = uuid.uuid4().hex
            joined = client.join(hero_id, credentials[hero_id])
            require(joined.get("Accepted") is True, "Original Hero claim failed")
            identities[hero_id] = joined["Player"]["PlayerId"]
            begun = client.command(Command.START_TURN)
            check("Original " + hero_id + " real StartTurn grants three Actions", [True, 3],
                  [begun.get("Accepted"), hero(begun["Snapshot"], hero_id)["Actions"]])
            ended = client.command(Command.END_TURN)
            check("Original " + hero_id + " real EndTurn becomes durable progression", [True, True],
                  [ended.get("Accepted"), hero(ended["Snapshot"], hero_id)["TurnCompleted"]])
        close(client); source.stop()
        baseline = saved(source)
        check("Actual original gameplay commands were submitted", 4, counts["original"])
        client, restarted = start(source)
        check("Original h itself restores its generated campaign", "RESTORED_FROM_SERVER_DISK", restarted.get("RoomStateOrigin0102d"))
        for hero_id in ("Aldric", "Rowan"):
            returned = client.reconnect(hero_id, credentials[hero_id])
            check("Original " + hero_id + " session survives its own restart", True,
                  returned.get("Accepted") is True and returned["Player"]["PlayerId"] == identities[hero_id])
        close(client); source.stop()
        baseline = saved(source)
        source_bytes = source.save_path.read_bytes()
        target.save_path.parent.mkdir(parents=True, mode=0o700)
        target.save_path.write_bytes(source_bytes); target.save_path.chmod(0o600)
        check("Copied input is byte-for-byte the actual stopped original h save", True, target.save_path.read_bytes() == source_bytes)

        client, restored = start(target)
        current = saved(target)
        check("Candidate proves restoration from its exact private copied path", True,
              restored.get("RoomStateOrigin0102d") == "RESTORED_FROM_SERVER_DISK"
              and restored.get("PersistencePath0102d") == str(target.save_path))
        check("Original room creation identity and revisions are unchanged", True,
              restored.get("RoomCreatedAtUtc0102d") == baseline["CreatedAtUtc"]
              and current["CreatedAtUtc"] == baseline["CreatedAtUtc"]
              and restored["Revision"] == baseline["Revision"] and current["GameplayRevision"] == baseline.get("GameplayRevision", 0))
        check("Reviewed full-room deployment comparison accepts actual h restoration", True, upgrade.rooms_equivalent(baseline, current))
        check("Entire saved gameplay including ordered enemies, hands, decks and discards is preserved", True,
              exact_campaign(baseline["DurableCampaign"]) == exact_campaign(current["DurableCampaign"])
              == exact_campaign(restored["Campaign"]))
        check("Every private reconnect credential and original player identity remains exact", True,
              private_identity(baseline["Players"]) == private_identity(current["Players"]))
        check("HELLO does not create a campaign owner, slot identity or readiness", True,
              all(current.get(name) in (None, "") for name in EMPTY_METADATA)
              and current.get("CampaignDraft0102", False) is False
              and all(player.get("ReadyForCampaign0102", False) is False for player in current["Players"]))
        check("Legacy h remains an active campaign with a separately visible original slot", True,
              restored["CampaignLobby"]["Supported"] is True and not restored["CampaignLobby"]["IsDraft"]
              and any(slot["Id"] == "original" and slot["IsActive"] for slot in restored["CampaignLobby"]["Slots"]))
        public = json.dumps(restored, separators=(",", ":"))
        check("Public copied-room snapshot contains no private reconnect credentials", True,
              all(credential not in public for credential in credentials.values()))
        check("Copied-room read-only proof submitted no gameplay commands", 0, counts["candidate"])
        close(client); target.stop()
        check("Read-only copied-room shutdown preserves full durable room", True, upgrade.rooms_equivalent(baseline, saved(target)))
        client, restarted = start(target)
        check("Candidate restart preserves exact saved gameplay order", True,
              exact_campaign(baseline["DurableCampaign"]) == exact_campaign(restarted["Campaign"]))
        for hero_id in ("Aldric", "Rowan"):
            returned = client.reconnect(hero_id, credentials[hero_id])
            check("Candidate accepts original " + hero_id + " private reconnect identity", True,
                  returned.get("Accepted") is True and returned["Player"]["PlayerId"] == identities[hero_id])
        close(client); target.stop()
        check("Verified reconnects preserve complete ordered gameplay and private identities", True,
              exact_campaign(baseline["DurableCampaign"]) == exact_campaign(saved(target)["DurableCampaign"])
              and private_identity(baseline["Players"]) == private_identity(saved(target)["Players"]))
        check("All candidate operations submitted zero gameplay commands", 0, counts["candidate"])
        check("Original private source save remains byte-for-byte unchanged by candidate proof", True, source.save_path.read_bytes() == source_bytes)
        report["status"] = "PASS"
    except Exception as error:
        report["status"] = "FAIL"
        report["error"] = {"type": type(error).__name__, "check": str(error) if isinstance(error, AssertionError) else "Native migration proof did not complete; inspect private logs"}
    finally:
        for client in clients:
            try: client.close()
            except OSError: pass
        for server in servers:
            try: server.stop()
            except Exception: report["status"] = "FAIL"
        report["gameplayCommandsSubmittedToCopiedRoom"] = counts["candidate"]
        report["passed"] = sum(item["passed"] for item in report["checks"])
        report["failed"] = sum(not item["passed"] for item in report["checks"])
        report["durationSeconds"] = round(time.monotonic() - started, 3)
        if report["status"] == "PASS": shutil.rmtree(private)
        destination = output / "results.json"
        destination.write_text(json.dumps(report, indent=2) + "\n"); destination.chmod(0o600)
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--original-build", type=Path, required=True)
    parser.add_argument("--original-manifest", type=Path, required=True)
    parser.add_argument("--candidate-build", type=Path, required=True)
    parser.add_argument("--candidate-manifest", type=Path, required=True)
    parser.add_argument("--expected-candidate-revision", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--secure", action="store_true")
    try:
        report = run(parser.parse_args())
    except Exception as error:
        report = {"status": "REFUSED", "passed": 0, "failed": 0,
                  "error": {"type": type(error).__name__, "check": "Native migration preparation failed; no preservation proof is claimed"}}
    print(json.dumps({name: report.get(name) for name in ("status", "passed", "failed", "error")}))
    return 0 if report.get("status") == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
