#!/usr/bin/env python3
"""Read-only inspection of an existing Ascendance systemd process.

Only service metadata and the allowlisted HOME/XDG/game routing environment are
reported. Save contents, account/session records, other environment variables,
logs and unit contents are never emitted. This program changes no service,
firewall, game data, permissions or configuration.
"""

import argparse
import hashlib
import json
import os
from pathlib import Path
import pwd
import re
import stat
import subprocess
import sys


PROPERTIES = (
    "ActiveState", "SubState", "User", "Group", "WorkingDirectory",
    "FragmentPath", "DropInPaths", "MainPID", "LoadState",
)
ENVIRONMENT = ("HOME", "XDG_CONFIG_HOME", "ASCENDANCE_ROOM", "ASCENDANCE_PORT")
VALUE_FLAGS = ("-room", "-port", "-logfile")
BOOLEAN_FLAGS = ("-batchmode", "-nographics", "-ascendance-server")


def sha256_file(path):
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
    with os.fdopen(fd, "rb") as stream:
        if not stat.S_ISREG(os.fstat(stream.fileno()).st_mode):
            raise ValueError("Expected a regular file")
        result = hashlib.sha256()
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            result.update(block)
        return result.hexdigest()


def command_output(arguments):
    return subprocess.run(arguments, check=True, text=True, capture_output=True,
                          timeout=15).stdout


def route_flags(argv):
    values, present, unknown = {}, [], []
    index = 1
    while index < len(argv):
        name = argv[index].lower()
        if name in VALUE_FLAGS:
            if index + 1 >= len(argv):
                raise ValueError("A routing flag is missing its argument")
            if name in values:
                raise ValueError("Duplicate routing flags require operator review")
            values[name] = argv[index + 1]
            index += 2
        elif name in BOOLEAN_FLAGS:
            present.append(name)
            index += 1
        else:
            # Do not expose values of unknown arguments: they may be secrets.
            if name.startswith("-"):
                unknown.append(name.split("=", 1)[0])
            index += 1
    room = values.get("-room", "LOCAL-ROOM")
    if not room.strip() or len(room) > 256:
        raise ValueError("Invalid or oversized room identifier")
    return values, sorted(set(present)), sorted(set(unknown))


def path_details(path):
    result = {"path": str(path), "exists": path.exists(), "symlinkComponent": False}
    for component in [*reversed(path.parents), path]:
        if component.is_symlink():
            result["symlinkComponent"] = True
            break
    if path.exists():
        info = path.stat()
        result.update(uid=info.st_uid, gid=info.st_gid,
                      mode=oct(stat.S_IMODE(info.st_mode)))
    return result


def inspect(service="ascendance", proc_root=Path("/proc"), run=command_output):
    if not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9_.-]{0,63}", service):
        raise ValueError("Invalid service name")
    text = run(["systemctl", "show", service, "--no-pager",
                "--property=" + ",".join(PROPERTIES)])
    properties = dict(line.split("=", 1) for line in text.splitlines() if "=" in line)
    public = {key: properties.get(key, "") for key in PROPERTIES if key != "DropInPaths"}
    result = {"format": "ascendance-existing-inspection-1", "service": service,
              "readOnly": True, "serviceProperties": public, "warnings": []}
    pid = int(properties.get("MainPID", "0"))
    if pid <= 0 or properties.get("ActiveState") != "active":
        raise ValueError("The expected service is not running; no process inspected")
    process = proc_root / str(pid)
    status = dict(line.split(":", 1) for line in (process / "status").read_text().splitlines() if ":" in line)
    uid = int(status["Uid"].split()[1])
    gid = int(status["Gid"].split()[1])
    executable = os.readlink(process / "exe")
    cwd = os.readlink(process / "cwd")
    args = [part.decode("utf-8", "replace") for part in (process / "cmdline").read_bytes().split(b"\0") if part]
    if not args:
        raise ValueError("Running process has no readable command line")
    flags, boolean_flags, unknown_flags = route_flags(args)
    # Decode only named values; never serialize the process's full environment.
    environment = {}
    for entry in (process / "environ").read_bytes().split(b"\0"):
        name, separator, value = entry.partition(b"=")
        if separator and name.decode("ascii", "ignore") in ENVIRONMENT:
            environment[name.decode("ascii")] = value.decode("utf-8", "replace")
    home = environment.get("HOME") or pwd.getpwuid(uid).pw_dir
    config = environment.get("XDG_CONFIG_HOME") or str(Path(home) / ".config")
    if not Path(config).is_absolute() or config == "/":
        raise ValueError("The live process has an unsafe/unrecognized config path")
    port = int(flags.get("-port", environment.get("ASCENDANCE_PORT", "7777")))
    if not 1 <= port <= 65535:
        raise ValueError("The live process has an invalid port")
    room = flags.get("-room", "LOCAL-ROOM")
    safe_room = "".join(c for c in room if c.isalnum() or c in "-_") or "room"
    expected = Path(config) / "unity3d" / "Ascendance" / "Ascendance"
    saves = expected / "AscendanceServerRooms0100"
    result["process"] = {"pid": pid, "uid": uid, "gid": gid,
                         "executable": executable, "workingDirectory": cwd,
                         "environment": environment, "room": room, "port": port,
                         "booleanFlags": boolean_flags, "unknownFlagNames": unknown_flags,
                         "logFile": flags.get("-logfile")}
    if executable.endswith(" (deleted)"):
        result["warnings"].append("The running executable was deleted; restore a complete rollback artifact before updating")
    else:
        result["process"]["executableSha256"] = sha256_file(Path(executable))
    result["persistence"] = {"derivation": "Linux Unity HOME/XDG_CONFIG_HOME + unity3d/Ascendance/Ascendance",
                             "expectedDataDirectory": path_details(expected),
                             "roomDirectory": path_details(saves),
                             "expectedRoomFileExists": (saves / (safe_room + ".json")).is_file()}
    # Other identities are reported as directory paths only, never save contents.
    alternatives = []
    unity = Path(config) / "unity3d"
    if unity.is_dir() and not unity.is_symlink():
        for company in sorted(unity.iterdir())[:64]:
            if company.is_symlink() or not company.is_dir():
                continue
            for product in sorted(company.iterdir())[:64]:
                if product.is_symlink() or not product.is_dir() or product == expected:
                    continue
                if (product / "AscendanceServerRooms0100").is_dir():
                    alternatives.append(str(product))
    result["persistence"]["otherServerDataDirectories"] = alternatives
    unit_files = [properties.get("FragmentPath", "")]
    dropins = properties.get("DropInPaths", "")
    if "\\" in dropins:
        result["warnings"].append("Escaped systemd drop-in paths require operator review")
    else:
        unit_files.extend(dropins.split())
    result["unitFileChecksums"] = [
        {"path": name, "sha256": sha256_file(Path(name))}
        for name in unit_files if name
    ]
    if unknown_flags:
        result["warnings"].append("Unknown process flags require review; their values were intentionally not reported")
    if not result["persistence"]["expectedRoomFileExists"]:
        result["warnings"].append("The expected persisted room was not found; do not activate an update until its actual data path is verified")
    if alternatives:
        result["warnings"].append("Other Unity server data identities exist; review the running identity before updating")
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--service", default="ascendance")
    args = parser.parse_args()
    try:
        print(json.dumps(inspect(args.service), indent=2))
        return 0
    except Exception as error:
        # Avoid leaking subprocess stderr/unit contents or raw process data.
        print(json.dumps({"format": "ascendance-existing-inspection-1", "readOnly": True,
                          "error": type(error).__name__, "message": str(error) if isinstance(error, ValueError) else "Inspection could not complete; verify root access and the service identity"}, indent=2))
        return 1


if __name__ == "__main__":
    sys.exit(main())
