#!/usr/bin/env python3
"""Prove 597f442 campaign/slot preservation with isolated pinned-TLS servers.

Only this driver's freshly generated campaign is copied. There is no remote
address or existing-save input. Publish results.json only; private fixtures can
contain disposable owner credentials, reconnect tokens and TLS private keys.
The older 55d25c0 migration driver and its evidence remain separate.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import stat
import time
import uuid

import native_h_campaign_migration as original_h
from native_server_acceptance import Command, NativeServer, WireClient, hero
from native_unity_clients import tls_fixture


ROOT = Path(__file__).resolve().parents[1]
ORIGINAL_REVISION = "597f44215ec2624ff0f215db80c6c487f05a84d7"
ORIGINAL_ASSEMBLY = "ed12446483b665df9b7c81562e7e602f30723ec38bbc4f8c5722f9353836c95b"
ASSEMBLY_PATH = "AscendanceServer_Data/Managed/Assembly-CSharp.dll"
release, upgrade = original_h.release, original_h.upgrade


def require(condition, label):
    if not condition:
        raise AssertionError(label)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def verify_builds(args):
    original, candidate = args.original_build.resolve(), args.candidate_build.resolve()
    release.verify_artifact(original, release.read_json(args.original_manifest))
    original_provenance = release.read_json(original / "AscendanceServer.provenance.json")
    require(original_provenance.get("source") == {"vcs": "git", "revision": ORIGINAL_REVISION, "dirty": False}
            and digest(original / ASSEMBLY_PATH) == ORIGINAL_ASSEMBLY,
            "The source is not the exact reviewed clean 597f442 server")
    require(re.fullmatch(r"[0-9a-f]{40}", args.expected_candidate_revision or "") is not None
            and args.expected_candidate_revision != ORIGINAL_REVISION,
            "The candidate must identify a different exact clean source revision")
    release.verify_artifact(candidate, release.read_json(args.candidate_manifest))
    candidate_provenance = release.read_json(candidate / "AscendanceServer.provenance.json")
    require(candidate_provenance.get("source") == {
                "vcs": "git", "revision": args.expected_candidate_revision, "dirty": False},
            "The candidate provenance is not the requested clean source revision")
    require(digest(candidate / ASSEMBLY_PATH) != ORIGINAL_ASSEMBLY,
            "The candidate must contain the new reviewed game assembly")
    return original, candidate


def private_tree(root):
    """Hash a generated private persistence tree, refusing links/special files."""
    require(root.is_dir() and not root.is_symlink(), "Private persistence root is not a regular directory")
    result = {}
    for path in sorted(root.rglob("*")):
        mode = path.lstat().st_mode
        require(not stat.S_ISLNK(mode), "Private persistence contains a link")
        if stat.S_ISDIR(mode):
            continue
        require(stat.S_ISREG(mode) and path.stat().st_size <= 32 * 1024 * 1024,
                "Private persistence contains an unsupported file")
        result[str(path.relative_to(root))] = digest(path)
    return result


def archive_tree(server):
    root = server.save_path.parent / "CampaignSlots"
    return private_tree(root) if root.is_dir() else {}


def plan(server):
    info = server.save_path.stat()
    checksum = digest(server.build / ASSEMBLY_PATH)
    return {
        "process": {"room": server.room, "uid": info.st_uid, "gid": info.st_gid,
                    "wireBuildVersion": "0.10.2h"},
        "persistence": {"expectedDataDirectory": {"path": str(server.save_path.parent.parent)}},
        "upgradeVerification": {
            "sourceIdentity": {"buildVersion": "0.10.2h", "transport": "secure",
                               "managedAssemblySha256": checksum},
            "code": {"files": {ASSEMBLY_PATH: {"sha256": checksum}}}},
    }


def prepare_source(server, factory, schema, check, clients):
    """Generate real gameplay and reproduce the reviewed pre-stop selection bug."""
    owner, credentials, identities = uuid.uuid4().hex, {}, {}
    a, hello = server.start(); clients.append(a)
    check("Original 597 authenticated HELLO identifies h", True,
          hello.get("Accepted") is True and hello.get("ServerBuildVersion") == "0.10.2h")
    check("Original 597 listener is loopback only", ["127.0.0.1"], original_h.listener_addresses(server))
    created = a.exchange("CREATE_CAMPAIGN", CampaignOwnerToken=owner)
    check("Original creates an actual owned campaign draft", True,
          created.get("Accepted") is True and created.get("Code") == "CAMPAIGN_CREATED")
    joined = []
    for client, hero_id in ((a, "Aldric"), (factory(server.port, server.room), "Rowan")):
        if client is not a:
            clients.append(client)
        credentials[hero_id] = uuid.uuid4().hex
        fields = {"HeroId": hero_id, "DisplayName": "Private migration " + hero_id,
                  "ReconnectToken": credentials[hero_id]}
        if hero_id == "Aldric": fields["CampaignOwnerToken"] = owner
        result = client.exchange("JOIN", **fields)
        require(result.get("Accepted") is True, "Original campaign Hero claim failed")
        client.player = result["Player"]
        identities[hero_id] = result["Player"]["PlayerId"]
        ready = client.exchange("SET_READY", PlayerId=client.player["PlayerId"], Ready=True)
        check("Original " + hero_id + " genuinely claims and readies its own Hero", True,
              ready.get("Accepted") is True and ready.get("Code") == "READY_CHANGED")
        joined.append(client)
    started = a.exchange("START_CAMPAIGN", CampaignOwnerToken=owner)
    check("Original starts the selected two-Hero campaign", [True, ["Aldric", "Rowan"]],
          [started.get("Accepted"), started.get("Snapshot", {}).get("Campaign", {}).get("PartyHeroIds")])
    for client, hero_id in zip(joined, ("Aldric", "Rowan")):
        begun = client.command(Command.START_TURN)
        check("Original " + hero_id + " gameplay StartTurn is accepted", [True, 3],
              [begun.get("Accepted"), hero(begun["Snapshot"], hero_id)["Actions"]])
        rescued = client.command(Command.QUEST_PRIMARY)
        check("Original " + hero_id + " spends one actual Action to rescue a Villager", [True, 2],
              [rescued.get("Accepted"), hero(rescued["Snapshot"], hero_id)["Actions"]])
        if hero_id == "Aldric":
            ended = client.command(Command.END_TURN)
            check("Original first Hero really completes its turn", [True, True],
                  [ended.get("Accepted"), hero(ended["Snapshot"], hero_id)["TurnCompleted"]])
    snapshot = a.snapshot()
    durable = upgrade.read_private_room(plan(server), schema, "0.10.2h")
    check("Original generated state retains two actual rescues and the active second turn", [2, "Rowan", 2],
          [snapshot["Campaign"]["VillagersRescued"], snapshot["Transient"]["TurnHeroId"],
           hero(snapshot, "Rowan")["Actions"]])
    check("Original 597 reproduces only the reviewed live presentation selection drift", True,
          not upgrade.campaigns_equivalent(durable["DurableCampaign"], snapshot["Campaign"])
          and upgrade.live_campaigns_equivalent(plan(server), durable["DurableCampaign"], snapshot["Campaign"]))
    check("Actual original 597 read-only preflight accepts its exact durable room", True,
          upgrade.capture_migration_state(plan(server), snapshot, schema) == durable)
    check("Original saved campaign predates NPC object materialization", True,
          not any(obj.get("Type") == "Villager" for obj in durable["DurableCampaign"]["QuestRuntime0829"]["Objects"]))
    for client in joined:
        client.close(); clients.remove(client)
    server.stop()
    baseline = upgrade.read_private_room(plan(server), schema, "0.10.2h")
    check("Original campaign owner and creation metadata are populated", True,
          baseline.get("CampaignOwnerToken0102") == owner
          and bool(baseline.get("CampaignId0102")) and bool(baseline.get("CampaignName0102"))
          and baseline.get("CampaignCreatorPlayerId0102") == identities["Aldric"]
          and baseline.get("CampaignDraft0102") is False)
    check("Original saved-slot archive is present", True, bool(archive_tree(server)))
    return {"room": baseline, "owner": owner, "credentials": credentials, "identities": identities}


def run(args):
    original, candidate = verify_builds(args)
    schema_path = ROOT / "Deploy/DigitalOcean/save-schema-0102g.json"
    schema = release.read_json(schema_path)
    require(schema.get("rootType") == "Ascendance.Online.PersistedServerRoom0100"
            and len(schema.get("types", {})) == 24
            and sum(len(fields) for fields in schema["types"].values()) == 423,
            "The reviewed full-room save schema has changed")
    output = args.output.absolute()
    require(not output.exists() and not output.is_symlink() and output.parent.is_dir(),
            "Output directory must be new")
    require(not any(parent.is_symlink() for parent in output.parents), "Output parent contains a link")
    output.mkdir(mode=0o700)
    private = output / "private"; private.mkdir(mode=0o700)
    report = {
        "kind": "actual-597f442-to-combat-campaign-preservation", "status": "RUNNING",
        "sourceBuildVersion": "0.10.2h", "candidateBuildVersion": "0.10.2h",
        "originalSourceRevision": ORIGINAL_REVISION, "candidateSourceRevision": args.expected_candidate_revision,
        "originalManagedAssemblySha256": ORIGINAL_ASSEMBLY,
        "candidateManagedAssemblySha256": digest(candidate / ASSEMBLY_PATH),
        "originalManifestSha256": digest(args.original_manifest), "candidateManifestSha256": digest(args.candidate_manifest),
        "schemaSha256": digest(schema_path), "transport": "pinned-TLS-and-room-key",
        "noProductionConnection": True, "noExistingSaveRead": True,
        "gameplayCommandsSubmittedToCopiedRoom": 0, "checks": [], "limitations": [
            "Generated isolated native Linux campaign; no production save, service, firewall or Windows player is touched.",
            "Copied-room proof uses HELLO only before the separately counted reconnect checks; no candidate gameplay commands.",
            "Only results.json is suitable for publication. Private fixtures may hold disposable campaign and TLS credentials."]}
    clients, servers, counts = [], [], {"original": 0, "candidate": 0, "candidateReconnect": 0}
    started_at = time.monotonic()

    def check(name, expected, actual):
        item = {"name": name, "expected": expected, "actual": actual, "passed": expected == actual}
        report["checks"].append(item)
        print(("PASS " if item["passed"] else "FAIL ") + name, flush=True)
        require(item["passed"], name)

    try:
        environment, client_security, _ = tls_fixture(private)
        environment["ASCENDANCE_LISTEN_LOOPBACK"] = "1"
        pin, access = client_security["ASCENDANCE_CERT_SHA256"], client_security["ASCENDANCE_ROOM_ACCESS_KEY"]

        def factory(phase):
            class CountedClient(WireClient):
                def exchange(self, operation, **fields):
                    if operation.upper() == "COMMAND": counts[phase] += 1
                    if phase == "candidate" and operation.upper() == "RECONNECT": counts["candidateReconnect"] += 1
                    return super().exchange(operation, **fields)
            return lambda port, room: CountedClient(port, room, tls_cert_sha256=pin, access_key=access)

        room_id = "COMBAT-MIGRATION-" + uuid.uuid4().hex
        source_root, target_root = private / "original", private / "candidate"
        source_root.mkdir(mode=0o700); target_root.mkdir(mode=0o700)
        source = NativeServer(original, source_root, room_id, environment=environment, client_factory=factory("original"))
        target = NativeServer(candidate, target_root, room_id, environment=environment, client_factory=factory("candidate"))
        servers.extend((source, target))
        prepared = prepare_source(source, factory("original"), schema, check, clients)
        baseline = prepared["room"]
        source_files, source_archives = private_tree(source.save_path.parent), archive_tree(source)
        check("Original executed five genuine gameplay commands", 5, counts["original"])
        target.save_path.parent.parent.mkdir(parents=True, exist_ok=True, mode=0o700)
        shutil.copytree(source.save_path.parent, target.save_path.parent)
        check("The entire copied room and campaign-slot tree is byte-identical", True,
              source_files == private_tree(target.save_path.parent))

        for cycle in (1, 2):
            client, hello = target.start(); clients.append(client)
            snapshot = hello.get("Snapshot", {})
            check("Candidate HELLO cycle " + str(cycle) + " identifies authenticated h", True,
                  hello.get("Accepted") is True and hello.get("ServerBuildVersion") == "0.10.2h")
            check("Candidate listener cycle " + str(cycle) + " is loopback only", ["127.0.0.1"], original_h.listener_addresses(target))
            current = upgrade.read_private_room(plan(target), schema, "0.10.2h")
            upgrade.prove_restored_state(plan(target), snapshot, baseline, schema, "0.10.2h")
            check("Candidate cycle " + str(cycle) + " passes strict private-copy restore comparison", True,
                  upgrade.rooms_equivalent(baseline, current))
            check("Candidate cycle " + str(cycle) + " preserves every ordered gameplay field", True,
                  original_h.exact_campaign(baseline["DurableCampaign"]) == original_h.exact_campaign(current["DurableCampaign"])
                  == original_h.exact_campaign(snapshot["Campaign"]))
            check("Candidate cycle " + str(cycle) + " does not materialize legacy NPC objects on HELLO", True,
                  current["DurableCampaign"]["QuestRuntime0829"]["Objects"] == baseline["DurableCampaign"]["QuestRuntime0829"]["Objects"])
            check("Candidate cycle " + str(cycle) + " preserves archived campaigns byte-for-byte", True,
                  archive_tree(target) == source_archives)
            check("Candidate cycle " + str(cycle) + " exposes original owned slot metadata", True,
                  snapshot["CampaignLobby"]["CampaignId"] == baseline["CampaignId0102"]
                  and snapshot["CampaignLobby"]["CreatorPlayerId"] == baseline["CampaignCreatorPlayerId0102"]
                  and any(slot["Id"] == "original" and not slot["IsActive"] for slot in snapshot["CampaignLobby"]["Slots"]))
            public = json.dumps(snapshot, separators=(",", ":"))
            check("Candidate cycle " + str(cycle) + " never exposes private owner or reconnect proofs", True,
                  all(secret not in public for secret in [prepared["owner"], *prepared["credentials"].values()]))
            check("Copied-room read-only cycle " + str(cycle) + " issued zero gameplay commands", 0, counts["candidate"])
            check("Copied-room read-only cycle " + str(cycle) + " issued no reconnect operations", 0, counts["candidateReconnect"])
            client.close(); clients.remove(client); target.stop()
        check("Read-only copied-room shutdown keeps the durable room strict", True,
              upgrade.rooms_equivalent(baseline, upgrade.read_private_room(plan(target), schema, "0.10.2h")))
        client, hello = target.start(); clients.append(client)
        for hero_id in ("Aldric", "Rowan"):
            result = client.reconnect(hero_id, prepared["credentials"][hero_id])
            check("Candidate accepts the original " + hero_id + " reconnect identity", True,
                  result.get("Accepted") is True and result["Player"]["PlayerId"] == prepared["identities"][hero_id])
        client.close(); clients.remove(client); target.stop()
        current = upgrade.read_private_room(plan(target), schema, "0.10.2h")
        check("Explicit reconnects preserve ordered gameplay and durable private identity", True,
              original_h.exact_campaign(baseline["DurableCampaign"]) == original_h.exact_campaign(current["DurableCampaign"])
              and original_h.private_identity(baseline["Players"]) == original_h.private_identity(current["Players"]))
        check("Explicit reconnects preserve original campaign owner and slot metadata", True,
              all(current.get(key) == baseline.get(key) for key in (
                  "CampaignId0102", "CampaignName0102", "CampaignOwnerToken0102", "CampaignCreatorPlayerId0102",
                  "CampaignDraft0102", "LastCampaignActionId0102", "LastCampaignActionCode0102"))
              and archive_tree(target) == source_archives)
        check("Candidate executed no gameplay commands in any phase", 0, counts["candidate"])
        check("Candidate used exactly two explicit reconnect operations after read-only proof", 2, counts["candidateReconnect"])
        check("Candidate did not change any file in the original private persistence tree", True,
              private_tree(source.save_path.parent) == source_files)
        report["status"] = "PASS"
    except Exception as error:
        report["status"] = "FAIL"
        report["error"] = {"type": type(error).__name__, "check": str(error) if isinstance(error, AssertionError)
                           else "Native campaign preservation did not complete; inspect private fixture logs"}
    finally:
        for client in clients:
            try: client.close()
            except OSError: pass
        for server in servers:
            try: server.stop()
            except Exception: report["status"] = "FAIL"
        report["gameplayCommandsSubmittedToCopiedRoom"] = counts["candidate"]
        report["originalGameplayCommands"] = counts["original"]
        report["candidateReconnectOperations"] = counts["candidateReconnect"]
        report["passed"] = sum(item["passed"] for item in report["checks"])
        report["failed"] = sum(not item["passed"] for item in report["checks"])
        report["durationSeconds"] = round(time.monotonic() - started_at, 3)
        if report["status"] == "PASS": shutil.rmtree(private)
        result_path = output / "results.json"
        result_path.write_text(json.dumps(report, indent=2) + "\n"); result_path.chmod(0o600)
    return report


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--original-build", type=Path, required=True)
    parser.add_argument("--original-manifest", type=Path, required=True)
    parser.add_argument("--candidate-build", type=Path, required=True)
    parser.add_argument("--candidate-manifest", type=Path, required=True)
    parser.add_argument("--expected-candidate-revision", required=True)
    parser.add_argument("--output", type=Path, required=True)
    try:
        report = run(parser.parse_args(argv))
    except Exception as error:
        report = {"status": "REFUSED", "passed": 0, "failed": 0,
                  "error": {"type": type(error).__name__, "check": "Native migration preparation failed; no preservation proof is claimed"}}
    print(json.dumps({key: report.get(key) for key in ("status", "passed", "failed", "error")}))
    return 0 if report.get("status") == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
