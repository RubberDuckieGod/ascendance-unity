#!/usr/bin/env python3
"""Reject missing/incomplete Unity verdicts independently of the Editor exit code."""
import json
import sys
from pathlib import Path

EXPECTED = {"master": 54, "foundation": 24, "router": 29, "network": 27, "reliability": 22, "snapshot": 20, "lobby": 32, "campaign": 41}
TERMINALS = {
    "master": ("[ASCENDANCE][0.10.2h TEST]", "Master Save/Load + Full Regression"),
    "foundation": ("[ASCENDANCE][0.10.2h SERVER]", "Dedicated Server Authority Foundation"),
    "router": ("[ASCENDANCE][0.10.2h ROUTER]", "Client Command Routing"),
    "network": ("[ASCENDANCE][0.10.2h NETWORK]", "Real Network Transport"),
    "reliability": ("[ASCENDANCE][0.10.2h RELIABILITY]", "Network Reliability"),
    "lobby": ("[ASCENDANCE][ONLINE LOBBY]", "Online Hero Lobby"),
    "campaign": ("[ASCENDANCE][ONLINE CAMPAIGN]", "Online Campaign Setup"),
}


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"Duplicate JSON field: {key}")
        result[key] = value
    return result


def verify(path: Path) -> dict:
    result = json.loads(path.read_text(encoding="utf-8-sig"), object_pairs_hook=unique_object)
    if not isinstance(result, dict):
        raise ValueError("Validation verdict must be an object")
    if result.get("passed") is not True or type(result.get("errorCount")) is not int or result["errorCount"] != 0 or result.get("errors"):
        raise ValueError(f"Unity validation failed: {result.get('reason', 'no reason supplied')}")
    if result.get("editorVersion") != "6000.6.5f1" or result.get("buildVersion") != "0.10.2h":
        raise ValueError("Unexpected Unity Editor or game version in validation verdict")
    gates = result.get("gates", [])
    if not isinstance(gates, list) or any(not isinstance(g, dict) for g in gates):
        raise ValueError("Validation gates must be objects in an array")
    if len(gates) != len(EXPECTED) or {gate.get("name") for gate in gates} != set(EXPECTED):
        raise ValueError("Validation verdict is missing required gates or contains duplicate/unknown gates")
    for gate in gates:
        expected = EXPECTED[gate["name"]]
        count = gate.get("observedChecks")
        if gate.get("passed") is not True or type(count) is not int or count <= 0 or not gate.get("terminalMessage"):
            raise ValueError(f"Required gate did not complete successfully: {gate['name']}")
        if expected is not None and (count != expected or gate.get("expectedChecks") != expected):
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
    if len(sys.argv) != 2:
        print("Usage: verify_unity_validation.py RESULTS_JSON", file=sys.stderr)
        return 2
    try:
        result = verify(Path(sys.argv[1]))
    except (OSError, ValueError, TypeError, KeyError) as error:
        print(f"Unity validation result rejected: {error}", file=sys.stderr)
        return 1
    print("Unity runtime gates passed: " + ", ".join(f"{gate['name']}={gate['observedChecks']}" for gate in result["gates"]))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
