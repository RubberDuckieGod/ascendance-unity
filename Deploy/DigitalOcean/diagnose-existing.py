#!/usr/bin/env python3
"""Read-only public service metadata and one localhost HELLO compatibility check.

Does not join a room, submit commands, read saves, change services, or emit
player/campaign/session data, private keys, response messages or raw logs.
"""
import hashlib
import json
import os
from pathlib import Path
import re
import socket
import stat
import struct
import subprocess
import sys
import uuid


SERVICE = "ascendance"
BUILD = "0.10.2c"
KNOWN_PORT = 7777
PROPERTIES = ("ActiveState", "SubState", "User", "Group", "WorkingDirectory", "FragmentPath", "MainPID")
CANARY_ROOT = Path("/opt/ascendance-upgrades/releases/unity-0.10.2c-multiplayer-eb51b22")


def public_identifier(value, limit=128):
    return value if isinstance(value, str) and re.fullmatch(r"[A-Za-z0-9_.-]{1," + str(limit) + "}", value) else None


def public_integer(value):
    return value if type(value) is int and value >= 0 else None


def public_bool(value):
    return value if type(value) is bool else None


def known_port(value):
    try:
        number = int(value)
        return number if 1 <= number <= 65535 else None
    except (TypeError, ValueError):
        return None


def read_regular(path, limit):
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
    with os.fdopen(fd, "rb") as source:
        info = os.fstat(source.fileno())
        if not stat.S_ISREG(info.st_mode) or info.st_size > limit:
            raise ValueError("Unrecognized diagnostic metadata file")
        data = source.read(limit + 1)
        if len(data) > limit:
            raise ValueError("Diagnostic metadata exceeds limit")
        return data


def routing(argv, environment):
    values = {}
    for index, token in enumerate(argv[1:], 1):
        if token.lower() in ("-room", "-port") and index + 1 < len(argv):
            if token.lower() in values:
                raise ValueError("Ambiguous duplicate routing arguments")
            values[token.lower()] = argv[index + 1]
    cli_room = public_identifier(values.get("-room"))
    env_room = public_identifier(environment.get("ASCENDANCE_ROOM"))
    selected_room = cli_room if "-room" in values else env_room or "LOCAL-ROOM"
    selected_port = known_port(values.get("-port") if "-port" in values else environment.get("ASCENDANCE_PORT", KNOWN_PORT))
    return {"commandLineRoomProvided": "-room" in values, "commandLineRoom": cli_room,
            "environmentRoom": env_room, "selectedExpectedRoom": selected_room,
            "roomSource": "commandLine" if "-room" in values else "environment" if env_room else "default",
            "commandLinePortProvided": "-port" in values,
            "commandLinePort": known_port(values.get("-port")),
            "environmentPort": known_port(environment.get("ASCENDANCE_PORT")),
            "selectedExpectedPort": selected_port}


def public_file_hash(path):
    try:
        if any(component.is_symlink() for component in [path, *path.parents]):
            raise ValueError("Link in code metadata path")
        fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
        checksum, count = hashlib.sha256(), 0
        with os.fdopen(fd, "rb") as source:
            info = os.fstat(source.fileno())
            if not stat.S_ISREG(info.st_mode) or info.st_size > 512 * 1024 * 1024:
                raise ValueError("Unrecognized code metadata file")
            for block in iter(lambda: source.read(1024 * 1024), b""):
                count += len(block)
                if count > 512 * 1024 * 1024:
                    raise ValueError("Code metadata exceeds limit")
                checksum.update(block)
        return {"available": True, "path": str(path), "sha256": checksum.hexdigest(), "bytes": count}
    except Exception as error:
        return {"available": False, "path": str(path), "error": type(error).__name__}


def service_metadata(proc_root=Path("/proc"), run=subprocess.run):
    result = run(["systemctl", "show", SERVICE, "--no-pager", "--property=" + ",".join(PROPERTIES)],
                 check=True, capture_output=True, text=True, timeout=15)
    raw = dict(line.split("=", 1) for line in result.stdout.splitlines() if "=" in line)
    properties = {key: raw.get(key, "")[:512] for key in PROPERTIES}
    pid = int(properties["MainPID"])
    if pid <= 0:
        return {"properties": properties, "process": None}
    process = proc_root / str(pid)
    executable = os.readlink(process / "exe")
    # /proc/PID/exe is the deliberate kernel link to this process's image. Its
    # bytes are hashed, never emitted; other file reads refuse links.
    checksum = hashlib.sha256()
    with (process / "exe").open("rb") as image:
        image_info = os.fstat(image.fileno())
        if not stat.S_ISREG(image_info.st_mode) or image_info.st_size > 512 * 1024 * 1024:
            raise ValueError("Unexpected process executable")
        for block in iter(lambda: image.read(1024 * 1024), b""):
            checksum.update(block)
    argv = [part.decode("utf-8", "replace") for part in read_regular(process / "cmdline", 64 * 1024).split(b"\0") if part]
    environment = {}
    for entry in read_regular(process / "environ", 1024 * 1024).split(b"\0"):
        name, separator, value = entry.partition(b"=")
        if separator and name in (b"ASCENDANCE_ROOM", b"ASCENDANCE_PORT"):
            environment[name.decode("ascii")] = value.decode("utf-8", "replace")
    code = Path(executable).parent
    return {"properties": properties, "process": {"executablePath": executable[:512],
            "executableSha256": checksum.hexdigest(), "workingDirectory": os.readlink(process / "cwd")[:512],
            "managedAssembly": public_file_hash(code / "AscendanceServer_Data/Managed/Assembly-CSharp.dll"),
            "unityPlayer": public_file_hash(code / "UnityPlayer.so"),
            "routing": routing(argv, environment)}}


def exact_read(stream, size):
    output = b""
    while len(output) < size:
        chunk = stream.recv(size - len(output))
        if not chunk:
            raise ValueError("Read-only HELLO connection closed")
        output += chunk
    return output


def summarize_hello(response, request):
    snapshot = response.get("Snapshot") if isinstance(response.get("Snapshot"), dict) else {}
    transient = snapshot.get("Transient") if isinstance(snapshot.get("Transient"), dict) else {}
    result = {"responseReceived": True, "accepted": public_bool(response.get("Accepted")),
              "code": public_identifier(response.get("Code"), 64),
              "protocolVersion": public_integer(response.get("ProtocolVersion")),
              "serverBuildVersion": public_identifier(response.get("ServerBuildVersion"), 40),
              "requestIdMatches": response.get("RequestId") == request["RequestId"],
              "expectedRoomId": public_identifier(request.get("RoomId")),
              "snapshotRoomId": public_identifier(snapshot.get("RoomId")),
              "snapshotRoomMatchesExpected": snapshot.get("RoomId") == request["RoomId"],
              "snapshotRoomMatchesAsciiCaseInsensitive": isinstance(snapshot.get("RoomId"), str) and
                  public_identifier(snapshot["RoomId"]) is not None and
                  snapshot["RoomId"].lower() == request["RoomId"].lower(),
              "buildMatchesExpected": response.get("ServerBuildVersion") == BUILD,
              "transient": {}}
    for name in ["IsResolving", "HasPendingReactionWindow", "HasPendingRewardChoice", "HasPendingEncounterDeckControl"]:
        result["transient"][name] = public_bool(transient.get(name))
    for name in ["MovingHeroIds", "HeroMovement", "ReactionOptions", "RewardOptions", "EncounterDeckControlOptions"]:
        value = transient.get(name)
        result["transient"][name + "Count"] = len(value) if isinstance(value, list) else None
    return result


def hello(room, port):
    if port != KNOWN_PORT or not public_identifier(room):
        return {"responseReceived": False, "error": "Routing metadata differs from the recognized local game endpoint"}
    request = {"ProtocolVersion": 1, "RequestId": uuid.uuid4().hex, "Operation": "HELLO", "RoomId": room}
    try:
        with socket.create_connection(("127.0.0.1", port), timeout=4) as stream:
            stream.settimeout(8)
            body = json.dumps(request, separators=(",", ":")).encode()
            stream.sendall(struct.pack("!I", len(body)) + body)
            size = struct.unpack("!I", exact_read(stream, 4))[0]
            if not 0 < size <= 8 * 1024 * 1024:
                raise ValueError("Unrecognized HELLO frame size")
            response = json.loads(exact_read(stream, size))
            if not isinstance(response, dict):
                raise ValueError("Unrecognized HELLO response shape")
            return summarize_hello(response, request)
    except Exception as error:
        # Do not echo raw response data, exception URLs, messages or private state.
        return {"responseReceived": False, "error": type(error).__name__}


def latest_canary(root=CANARY_ROOT):
    candidates = []
    if root.is_dir():
        for path in root.glob("canary-*/result.json"):
            if any(part.is_symlink() for part in [path, *path.parents]):
                continue
            if path.is_file():
                candidates.append(path)
    if not candidates:
        return {"available": False}
    path = max(candidates, key=lambda candidate: candidate.stat().st_mtime_ns)
    try:
        result = json.loads(read_regular(path, 4 * 1024 * 1024))
        return {"available": True, "path": str(path),
                "status": result.get("status") if result.get("status") in ("PASS", "FAIL") else None,
                "passed": public_integer(result.get("passed")), "failed": public_integer(result.get("failed")),
                "sourceRevision": result.get("sourceRevision") if isinstance(result.get("sourceRevision"), str) and re.fullmatch(r"[0-9a-f]{40}", result["sourceRevision"]) else None,
                "isolatedNativeRoomSave": public_bool(result.get("nativeRoomSaveWasUnderIsolatedXdgDirectory"))}
    except Exception as error:
        return {"available": False, "error": type(error).__name__}


def main():
    if not sys.platform.startswith("linux") or os.geteuid() != 0:
        print(json.dumps({"readOnly": True, "error": "Use the owner's Linux root SSH session"}))
        return 1
    report = {"format": "ascendance-readonly-hello-diagnostic-1", "readOnly": True,
              "expectedBuild": BUILD, "expectedKnownPort": KNOWN_PORT}
    try:
        report["service"] = service_metadata()
        process = report["service"].get("process")
        if process:
            route = process["routing"]
            report["hello"] = hello(route["selectedExpectedRoom"], route["selectedExpectedPort"])
        else:
            report["hello"] = {"responseReceived": False, "error": "Service has no main process"}
        report["latestIsolatedCanary"] = latest_canary()
        print(json.dumps(report, indent=2))
        return 0
    except Exception as error:
        report["error"] = type(error).__name__
        print(json.dumps(report, indent=2))
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
