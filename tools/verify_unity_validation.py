#!/usr/bin/env python3
"""Reject missing/incomplete Unity verdicts independently of the Editor exit code."""
import json
import argparse
import hashlib
import os
import re
import sys
from pathlib import Path

EXPECTED = {"master": 54, "foundation": 24, "router": 30, "network": 27, "reliability": 22, "snapshot": 20, "lobby": 32, "campaign": 42, "reaction": 30, "combatHud": 11, "villagers": 67, "locationArt": 47}
OPTIONAL = {"installedArt": 10, "allLocationArt": 236}
TERMINALS = {
    "master": ("[ASCENDANCE][0.10.2h TEST]", "Master Save/Load + Full Regression"),
    "foundation": ("[ASCENDANCE][0.10.2h SERVER]", "Dedicated Server Authority Foundation"),
    "router": ("[ASCENDANCE][0.10.2h ROUTER]", "Client Command Routing"),
    "network": ("[ASCENDANCE][0.10.2h NETWORK]", "Real Network Transport"),
    "reliability": ("[ASCENDANCE][0.10.2h RELIABILITY]", "Network Reliability"),
    "lobby": ("[ASCENDANCE][ONLINE LOBBY]", "Online Hero Lobby"),
    "campaign": ("[ASCENDANCE][ONLINE CAMPAIGN]", "Online Campaign Setup"),
    "reaction": ("[ASCENDANCE][REACTION AUTHORITY]", "Reaction Authority"),
    "combatHud": ("[ASCENDANCE][COMBAT HUD]", "Combat HUD"),
    "villagers": ("[ASCENDANCE][Q001 VILLAGERS]", "Villager gameplay regression"),
    "locationArt": ("[ASCENDANCE][LOCATION ART]", "Location Art Framework"),
    "installedArt": ("[ASCENDANCE][LOCATION ART]", "Installed Location Art Bundle"),
    "allLocationArt": ("[ASCENDANCE][LOCATION ART]", "All Location Art Bindings"),
}


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"Duplicate JSON field: {key}")
        result[key] = value
    return result


def manifest_hash(path) -> str:
    if not path:
        raise ValueError("Strict installed-art verification requires ASCENDANCE_ART_MANIFEST or --art-manifest")
    manifest = Path(path)
    if not manifest.is_absolute() or manifest.is_symlink() or any(parent.is_symlink() for parent in manifest.parents):
        raise ValueError("Art manifest must be an absolute regular path without symbolic links")
    if not manifest.is_file() or not 0 < manifest.stat().st_size <= 4 * 1024 * 1024:
        raise ValueError("Actual art manifest is missing or has invalid size")
    return hashlib.sha256(manifest.read_bytes()).hexdigest()


def verify(path: Path, require_installed_art=False, require_all_location_art=False, art_manifest=None, expected_art_manifest_sha256=None) -> dict:
    require_installed_art = bool(require_installed_art or require_all_location_art)
    result = json.loads(path.read_text(encoding="utf-8-sig"), object_pairs_hook=unique_object)
    if not isinstance(result, dict):
        raise ValueError("Validation verdict must be an object")
    if result.get("passed") is not True or type(result.get("errorCount")) is not int or result["errorCount"] != 0 or result.get("errors"):
        raise ValueError(f"Unity validation failed: {result.get('reason', 'no reason supplied')}")
    if result.get("editorVersion") != "6000.6.5f1" or result.get("buildVersion") != "0.10.2h":
        raise ValueError("Unexpected Unity Editor or game version in validation verdict")
    if (type(result.get("requireInstalledArt")) is not bool or type(result.get("requireAllLocationArt")) is not bool
            or result["requireInstalledArt"] != require_installed_art or result["requireAllLocationArt"] != bool(require_all_location_art)):
        raise ValueError("Unity verdict art mode differs from the explicitly requested validation")
    expected_gates = dict(EXPECTED)
    if require_installed_art:
        expected_gates["installedArt"] = OPTIONAL["installedArt"]
        actual_hash = manifest_hash(art_manifest or os.environ.get("ASCENDANCE_ART_MANIFEST"))
        if type(result.get("artManifestSha256")) is not str or not re.fullmatch(r"[0-9a-f]{64}", result["artManifestSha256"]) or result["artManifestSha256"] != actual_hash:
            raise ValueError("Unity verdict differs from the actual required art manifest bytes")
        if expected_art_manifest_sha256 is not None and expected_art_manifest_sha256 != actual_hash:
            raise ValueError("Required art manifest changed after the validation inputs were frozen")
    elif result.get("artManifestSha256") != "":
        raise ValueError("Baseline validation cannot claim an installed-art manifest proof")
    if require_all_location_art:
        expected_gates["allLocationArt"] = OPTIONAL["allLocationArt"]
    gates = result.get("gates", [])
    if not isinstance(gates, list) or any(not isinstance(g, dict) for g in gates):
        raise ValueError("Validation gates must be objects in an array")
    if len(gates) != len(expected_gates) or {gate.get("name") for gate in gates} != set(expected_gates):
        raise ValueError("Validation verdict is missing required gates or contains duplicate/unknown gates")
    for gate in gates:
        expected = expected_gates[gate["name"]]
        count = gate.get("observedChecks")
        if gate.get("passed") is not True or type(count) is not int or count <= 0 or not gate.get("terminalMessage"):
            raise ValueError(f"Required gate did not complete successfully: {gate['name']}")
        if expected is not None and (count != expected or type(gate.get("expectedChecks")) is not int or gate.get("expectedChecks") != expected):
            raise ValueError(f"Unexpected check count for {gate['name']}: {count}, expected {expected}")
        if gate['name'] == 'snapshot':
            terminal = f"[ASCENDANCE TEST 0.10.2 SNAPSHOT] PASS — {count} checks."
        else:
            prefix, label = TERMINALS[gate['name']]
            terminal = f"{prefix} PASS — {label} completed: {count} passed, 0 failed."
        if gate['terminalMessage'].strip() != terminal:
            raise ValueError(f"Required terminal verdict is invalid: {gate['name']}")
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("results", type=Path)
    parser.add_argument("--require-installed-art", action="store_true")
    parser.add_argument("--require-all-location-art", action="store_true")
    parser.add_argument("--art-manifest", type=Path)
    parser.add_argument("--expected-art-manifest-sha256")
    args = parser.parse_args()
    try:
        result = verify(args.results, args.require_installed_art, args.require_all_location_art, args.art_manifest, args.expected_art_manifest_sha256)
    except (OSError, ValueError, TypeError, KeyError) as error:
        print(f"Unity validation result rejected: {error}", file=sys.stderr)
        return 1
    print("Unity runtime gates passed: " + ", ".join(f"{gate['name']}={gate['observedChecks']}" for gate in result["gates"]))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
