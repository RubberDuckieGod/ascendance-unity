#!/usr/bin/env python3
"""Reviewed, on-host upgrade of the recognized legacy Ascendance service.

Never opens firewall ports, joins a production room, prints access keys, clears
the old installation, relocates data, or automatically restores old save data.
The operator supplies a SHA-pinned release descriptor through the Windows
wrapper. All native canary traffic uses disposable local rooms and data.
"""
import argparse
import contextlib
import fcntl
import hashlib
import importlib.util
import ipaddress
import json
import os
from pathlib import Path
import pwd
import re
import secrets
import signal
import shutil
import socket
import ssl
import stat
import struct
import subprocess
import sys
import tarfile
import time
import uuid
import zipfile


HERE = Path(__file__).resolve().parent
ROOT = Path("/opt/ascendance-upgrades")
CONFIG = Path("/etc/ascendance-secure")
BACKUPS = Path("/var/backups/ascendance")
LEGACY = Path("/opt/ascendance")
SYSTEMD = Path("/etc/systemd/system")


class Refusal(Exception):
    pass


class RecoveryFailure(Refusal):
    pass


def require(ok, message):
    if not ok:
        raise Refusal(message)


def sibling(name):
    spec = importlib.util.spec_from_file_location(name.replace("-", "_"), HERE / (name + ".py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def canonical(value):
    return json.dumps(value, sort_keys=True, separators=(",", ":")).encode()


def digest(value):
    return hashlib.sha256(value).hexdigest()


def run(command, **kwargs):
    return subprocess.run(command, check=True, text=kwargs.pop("text", True), capture_output=True,
                          timeout=kwargs.pop("timeout", 30), **kwargs)


def secure_directory(path, mode=0o700, gid=0):
    require(path.is_absolute() and ".." not in path.parts and path != Path("/"), "Unsafe directory")
    for part in [*reversed(path.parents), path]:
        require(not part.is_symlink(), "A managed directory contains a symlink")
    path.mkdir(parents=True, exist_ok=True, mode=mode)
    require(path.is_dir() and path.stat().st_uid == 0, "Managed directory is not owned by root")
    require(not path.stat().st_mode & 0o022, "Managed directory is writable by another account")
    os.chmod(path, mode)
    os.chown(path, 0, gid)


def write_new(path, content, mode=0o600, gid=0):
    fd = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, mode)
    with os.fdopen(fd, "wb") as stream:
        os.fchmod(stream.fileno(), mode)
        os.fchown(stream.fileno(), 0, gid)
        stream.write(content)


def safe_tree(root):
    require(root.is_dir() and not root.is_symlink(), "Expected an existing regular directory")
    total = 0
    files = []
    for directory, directories, names in os.walk(root, followlinks=False):
        for name in directories + names:
            path = Path(directory) / name
            mode = path.lstat().st_mode
            require(stat.S_ISREG(mode) or stat.S_ISDIR(mode), "Backup tree contains a symlink or special file")
            if stat.S_ISREG(mode):
                files.append(path)
                total += path.stat().st_size
    return files, total


def file_proof(path):
    """Hash an already identified regular file without following a final link."""
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
    with os.fdopen(fd, "rb") as stream:
        before = os.fstat(stream.fileno())
        require(stat.S_ISREG(before.st_mode), "Expected a regular verification file")
        checksum = hashlib.sha256()
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            checksum.update(block)
        after = os.fstat(stream.fileno())
    identity = lambda info: (info.st_dev, info.st_ino, info.st_size, info.st_mtime_ns, info.st_ctime_ns, info.st_uid, info.st_gid, stat.S_IMODE(info.st_mode))
    require(identity(before) == identity(after), "A verification file changed while being read")
    require(identity(after) == identity(path.lstat()), "A verification file was replaced while being read")
    return {"sha256": checksum.hexdigest(), "size": after.st_size, "uid": after.st_uid,
            "gid": after.st_gid, "mode": stat.S_IMODE(after.st_mode),
            "device": after.st_dev, "inode": after.st_ino,
            "mtimeNs": after.st_mtime_ns, "ctimeNs": after.st_ctime_ns}


def tree_proof(root):
    """Rescan membership as well as contents; includes empty directories."""
    files, _ = safe_tree(root)
    directories = {}
    for directory, _, _ in os.walk(root, followlinks=False):
        path = Path(directory)
        info = path.lstat()
        require(stat.S_ISDIR(info.st_mode), "A verification directory changed identity")
        directories[str(path.relative_to(root))] = {"uid": info.st_uid, "gid": info.st_gid, "mode": stat.S_IMODE(info.st_mode)}
    return {"directories": directories,
            "files": {str(path.relative_to(root)): file_proof(path) for path in files}}


def tree_hashes(proof):
    return {name: entry["sha256"] for name, entry in proof["files"].items()}


def environment_proof(service, inspector):
    text = run(["systemctl", "show", service, "--property=EnvironmentFiles", "--value"]).stdout.strip()
    if not text:
        return []
    pattern = r"(/[^\s()\\*?\[\]]+) \(ignore_errors=(yes|no)\)"
    matches = list(re.finditer(pattern, text))
    require(matches and re.sub(pattern, "", text).strip() == "", "Environment file paths require review")
    result = []
    for match in matches:
        path = Path(match.group(1))
        require(path.is_absolute() and ".." not in path.parts and not inspector.path_details(path)["symlinkComponent"], "Environment file path identity requires review")
        entry = {"path": str(path), "optional": match.group(2) == "yes", "exists": path.exists()}
        if entry["exists"]:
            proof = file_proof(path)
            require(proof["uid"] == 0 and not proof["mode"] & 0o022, "Environment file ownership/permissions require review")
            entry["proof"] = proof
        else:
            require(entry["optional"], "Required environment file is missing")
        result.append(entry)
    return result


def plan_fingerprint(plan):
    stable = json.loads(json.dumps(plan))
    stable.get("serviceProperties", {}).pop("MainPID", None)
    stable.get("process", {}).pop("pid", None)
    return digest(canonical(stable))


def checked_plan(inspector, service):
    plan = inspector.inspect(service)
    require(not plan["warnings"], "Existing identity requires review: " + "; ".join(plan["warnings"]))
    process, properties = plan["process"], plan["serviceProperties"]
    require(properties["ActiveState"] == "active" and properties["SubState"] == "running", "Existing service is not running")
    require(Path(properties["WorkingDirectory"]) == LEGACY, "This helper only accepts the reviewed legacy working directory")
    require(properties["FragmentPath"] == str(SYSTEMD / (service + ".service")), "Unexpected systemd unit identity")
    executable = Path(process["executable"])
    require(executable.name == "AscendanceServer.x86_64", "The running process is not the expected Unity server")
    require(executable == LEGACY / executable.name or executable.is_relative_to(ROOT / "releases"), "Unrecognized running installation")
    require(process["workingDirectory"] == str(LEGACY), "Live working directory differs from the unit")
    require(properties["User"] and pwd.getpwnam(properties["User"]).pw_uid == process["uid"], "Service and live process users differ")
    require(re.fullmatch(r"[A-Za-z0-9_-]{1,128}", process["room"]) is not None, "Room identity requires manual argument review")
    data = Path(plan["persistence"]["expectedDataDirectory"]["path"])
    require(plan["persistence"]["expectedRoomFileExists"], "No verified persisted room exists")
    require(not plan["persistence"]["expectedDataDirectory"]["symlinkComponent"], "Persistence path includes a symlink")
    require(not data.is_relative_to(LEGACY) and not data.is_relative_to(ROOT), "Game data must be separate from executable releases")
    plan["upgradeVerification"] = {"code": tree_proof(executable.parent),
                                   "environmentFiles": environment_proof(service, inspector)}
    return plan


def require_endpoint_on_host(endpoint_ip):
    interfaces = json.loads(run(["ip", "-j", "-4", "address", "show", "scope", "global"]).stdout)
    addresses = {entry.get("local") for interface in interfaces for entry in interface.get("addr_info", []) if entry.get("family") == "inet"}
    require(endpoint_ip in addresses, "Release endpoint does not belong to this host; activation was not prepared")


def extract_build(archive, destination, manifest, release_tools):
    require(not destination.exists(), "Immutable release destination already exists")
    with zipfile.ZipFile(archive) as source:
        entries = source.infolist()
        require(0 < len(entries) <= 5000, "Invalid server archive entry count")
        require(sum(entry.file_size for entry in entries) <= 512 * 1024 * 1024, "Server archive exceeds size limit")
        names = set()
        for entry in entries:
            path = Path(entry.filename)
            require(not path.is_absolute() and ".." not in path.parts and "\\" not in entry.filename and ":" not in entry.filename, "Unsafe server archive path")
            require(path.parts and path.parts[0] == "LinuxServer", "Expected LinuxServer archive root")
            mode = (entry.external_attr >> 16) & 0o170000
            require(mode in (0, stat.S_IFDIR, stat.S_IFREG), "Server archive contains a link or special file")
            relative = Path(*path.parts[1:])
            if not relative.parts:
                continue
            require(str(relative) not in names, "Duplicate server archive path")
            names.add(str(relative))
        destination.mkdir(parents=True, mode=0o755)
        for entry in entries:
            relative = Path(*Path(entry.filename).parts[1:])
            if not relative.parts:
                continue
            target = destination / relative
            if entry.is_dir():
                target.mkdir(parents=True, exist_ok=True, mode=0o755)
            else:
                target.parent.mkdir(parents=True, exist_ok=True, mode=0o755)
                with source.open(entry) as incoming, target.open("xb") as output:
                    shutil.copyfileobj(incoming, output)
                os.chmod(target, 0o644)
    for directory, _, _ in os.walk(destination):
        os.chmod(directory, 0o755)
    release_tools.verify_artifact(destination, manifest)
    os.chmod(destination / "AscendanceServer.x86_64", 0o755)


def secure_configuration(service, process, endpoint_ip, inspector):
    gid = process["gid"]
    if CONFIG.exists():
        require(not inspector.path_details(CONFIG)["symlinkComponent"] and CONFIG.stat().st_uid == 0 and CONFIG.stat().st_gid == gid and stat.S_IMODE(CONFIG.stat().st_mode) == 0o750, "Secure configuration directory identity changed")
        require((CONFIG / "identity.json").is_file() and not (CONFIG / "identity.json").is_symlink(), "An unrecognized secure configuration already exists")
        identity = json.loads((CONFIG / "identity.json").read_text())
        require(identity == {"format": 1, "service": service, "uid": process["uid"], "gid": gid, "endpointIp": endpoint_ip}, "Secure configuration identity changed")
        for name in ["server.pfx", "pfx-password", "room-access-key", "server.crt"]:
            path = CONFIG / name
            require(path.is_file() and not path.is_symlink() and path.stat().st_uid == 0 and path.stat().st_gid == gid and stat.S_IMODE(path.stat().st_mode) == 0o640, "Private transport file ownership or permissions changed")
    else:
        secure_directory(CONFIG, 0o750, gid)
        try:
            write_new(CONFIG / "pfx-password", (secrets.token_hex(32) + "\n").encode(), 0o640, gid)
            write_new(CONFIG / "room-access-key", (secrets.token_hex(32) + "\n").encode(), 0o640, gid)
            run(["openssl", "req", "-x509", "-newkey", "rsa:3072", "-nodes", "-days", "365", "-subj", "/CN=Ascendance Multiplayer", "-addext", "subjectAltName=IP:" + endpoint_ip + ",IP:127.0.0.1,DNS:localhost", "-keyout", str(CONFIG / "temporary-key.pem"), "-out", str(CONFIG / "server.crt")])
            run(["openssl", "pkcs12", "-export", "-inkey", str(CONFIG / "temporary-key.pem"), "-in", str(CONFIG / "server.crt"), "-out", str(CONFIG / "server.pfx"), "-keypbe", "PBE-SHA1-3DES", "-certpbe", "PBE-SHA1-3DES", "-macalg", "sha1", "-passout", "file:" + str(CONFIG / "pfx-password")])
            (CONFIG / "temporary-key.pem").unlink()
            for name in ["server.pfx", "server.crt"]:
                os.chown(CONFIG / name, 0, gid)
                os.chmod(CONFIG / name, 0o640)
            write_new(CONFIG / "identity.json", canonical({"format": 1, "service": service, "uid": process["uid"], "gid": gid, "endpointIp": endpoint_ip}))
        except Exception:
            # An incomplete directory is kept private for inspection, not silently
            # erased/reinitialized on the next attempt.
            raise Refusal("Transport preparation failed; inspect the private configuration before retrying")
    key = (CONFIG / "room-access-key").read_text().strip()
    require(re.fullmatch(r"[A-Fa-f0-9]{64}", key) is not None, "Invalid private room key")
    certificate = run(["openssl", "x509", "-in", str(CONFIG / "server.crt"), "-outform", "DER"], text=False).stdout
    return {"pin": digest(certificate), "key": key,
            "environment": {"ASCENDANCE_TLS_PFX_PATH": str(CONFIG / "server.pfx"), "ASCENDANCE_TLS_PFX_PASSWORD_FILE": str(CONFIG / "pfx-password"), "ASCENDANCE_ROOM_ACCESS_KEY_FILE": str(CONFIG / "room-access-key")}}


def canary(release_path, manifest_path, descriptor, plan, security):
    helper = HERE / "native_server_acceptance.py"
    require(digest(helper.read_bytes()) == descriptor["helpers"][helper.name]["sha256"].lower(), "Canary helper checksum mismatch")
    installed_helper = release_path / "native_server_acceptance.py"
    if not installed_helper.exists():
        write_new(installed_helper, helper.read_bytes(), 0o644)
    else:
        require(digest(installed_helper.read_bytes()) == digest(helper.read_bytes()), "Existing canary helper differs")
    result_directory = release_path / ("canary-" + uuid.uuid4().hex)
    result_directory.mkdir(mode=0o700)
    process = plan["process"]
    os.chown(result_directory, process["uid"], process["gid"])
    report = result_directory / "result.json"
    command = ["python3", str(installed_helper), "--build-dir", str(release_path / "build"), "--manifest", str(manifest_path), "--output", str(report), "--source-revision", descriptor["sourceRevision"], "--tls-cert-sha256", security["pin"], "--access-key-file", str(CONFIG / "room-access-key")]
    if process["uid"] != 0:
        command = ["runuser", "-u", plan["serviceProperties"]["User"], "--", *command]
    environment = {"PATH": "/usr/local/bin:/usr/bin:/bin:/usr/sbin:/sbin", "LANG": "C.UTF-8", **security["environment"]}
    child = subprocess.Popen(command, env=environment, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, start_new_session=True)
    try:
        child.communicate(timeout=180)
    except subprocess.TimeoutExpired:
        with contextlib.suppress(ProcessLookupError):
            os.killpg(child.pid, signal.SIGTERM)
        try:
            child.communicate(timeout=10)
        except subprocess.TimeoutExpired:
            with contextlib.suppress(ProcessLookupError):
                os.killpg(child.pid, signal.SIGKILL)
            child.communicate()
        raise Refusal("Isolated canary exceeded its deadline; production was not stopped")
    require(child.returncode == 0 and report.is_file(), "Isolated native canary failed; production was not stopped")
    verdict = json.loads(report.read_text())
    require(verdict.get("status") == "PASS" and verdict.get("failed") == 0 and verdict.get("passed", 0) > 0 and verdict.get("nativeRoomSaveWasUnderIsolatedXdgDirectory") is True, "Canary did not prove successful isolated gameplay/persistence")
    return {"passed": verdict["passed"], "failed": 0, "reportPath": str(report)}


def connected_clients(port):
    output = run(["ss", "-Hnt", "state", "established", "( sport = :" + str(port) + " )"]).stdout
    return len([line for line in output.splitlines() if line.strip()])


def backup_tree(source, destination, inspector):
    before = tree_proof(source)
    shutil.copytree(source, destination, symlinks=False)
    after = tree_proof(source)
    require(before == after, "Data changed while backing up; production was not stopped")
    for directory, directories, names in os.walk(destination):
        os.chmod(directory, 0o700)
        for name in names:
            os.chmod(Path(directory) / name, 0o600)
    copied = tree_proof(destination)
    require(tree_hashes(copied) == tree_hashes(before) and copied["directories"].keys() == before["directories"].keys(), "Backup verification failed")
    return tree_hashes(before)


def verify_code_archive(path, proof):
    expected = tree_hashes(proof)
    found, directories = {}, set()
    with tarfile.open(path, "r:gz") as archive:
        for entry in archive:
            name = Path(entry.name)
            require(name.parts and name.parts[0] == "previous-code" and ".." not in name.parts and not name.is_absolute(), "Unexpected rollback archive path")
            relative = str(Path(*name.parts[1:]))
            if entry.isdir():
                require(relative not in directories and relative in proof["directories"], "Rollback archive directory differs from its manifest")
                directories.add(relative)
            else:
                require(entry.isfile() and relative not in found and relative in expected, "Rollback archive includes an unexpected file/link")
                checksum = hashlib.sha256()
                incoming = archive.extractfile(entry)
                for block in iter(lambda: incoming.read(1024 * 1024), b""):
                    checksum.update(block)
                found[relative] = checksum.hexdigest()
    require(found == expected and directories == proof["directories"].keys(), "Rollback code archive failed complete manifest verification")


def backup_existing(plan, release, inspector):
    secure_directory(BACKUPS)
    destination = BACKUPS / (time.strftime("%Y%m%dT%H%M%SZ", time.gmtime()) + "-" + release + "-" + uuid.uuid4().hex[:8])
    destination.mkdir(mode=0o700)
    code = Path(plan["process"]["executable"]).parent
    code_before = tree_proof(code)
    if "upgradeVerification" in plan:
        require(code_before == plan["upgradeVerification"]["code"], "Existing code changed before backup")
    _, code_bytes = safe_tree(code)
    data = Path(plan["persistence"]["expectedDataDirectory"]["path"])
    _, data_bytes = safe_tree(data)
    require(shutil.disk_usage(BACKUPS).free > code_bytes * 2 + data_bytes * 3 + 256 * 1024 * 1024, "Insufficient free disk for verified rollback backups")
    with tarfile.open(destination / "previous-code.tar.gz", "x:gz", dereference=False) as archive:
        archive.add(code, arcname="previous-code", recursive=True)
    os.chmod(destination / "previous-code.tar.gz", 0o600)
    require(tree_proof(code) == code_before, "Existing code changed while backing up")
    verify_code_archive(destination / "previous-code.tar.gz", code_before)
    write_new(destination / "previous-code-manifest.json", canonical(code_before))
    write_new(destination / "previous-code-archive.sha256", (file_proof(destination / "previous-code.tar.gz")["sha256"] + "\n").encode())
    configs = destination / "configuration"
    configs.mkdir(mode=0o700)
    paths = [Path(item["path"]) for item in plan["unitFileChecksums"]]
    environments = environment_proof(plan["service"], inspector)
    if "upgradeVerification" in plan:
        require(environments == plan["upgradeVerification"]["environmentFiles"], "Environment files changed before backup")
    paths.extend(Path(item["path"]) for item in environments if item["exists"])
    for path in dict.fromkeys(paths):
        require(not inspector.path_details(path)["symlinkComponent"], "Backup configuration includes a symlink")
        proof = file_proof(path)
        fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
        with os.fdopen(fd, "rb") as stream:
            require(stat.S_ISREG(os.fstat(stream.fileno()).st_mode), "Backup configuration is not a regular file")
            content = stream.read()
        require(digest(content) == proof["sha256"] and file_proof(path) == proof, "Configuration changed while backing up")
        target = configs / path.relative_to("/")
        target.parent.mkdir(parents=True, exist_ok=True, mode=0o700)
        write_new(target, content)
        require(file_proof(target)["sha256"] == proof["sha256"], "Configuration backup checksum differs")
    data = Path(plan["persistence"]["expectedDataDirectory"]["path"])
    room_file = data / "AscendanceServerRooms0100" / (plan["process"]["room"] + ".json")
    require(isinstance(json.loads(room_file.read_text()), dict), "Current room backup is not valid JSON")
    hashes = backup_tree(data, destination / "data-before-stop", inspector)
    if CONFIG.exists():
        backup_tree(CONFIG, destination / "private-transport-configuration", inspector)
    write_new(destination / "inspection.json", canonical(plan))
    write_new(destination / "data-before-stop-sha256.json", canonical(hashes))
    return destination


def quote_unit(value):
    require(not any(c in value for c in ["\0", "\n", "\r", "$", "%"]), "A unit argument requires manual escaping review")
    return '"' + value.replace("\\", "\\\\").replace('"', '\\"') + '"'


def override_text(release, plan, security):
    process = plan["process"]
    args = [str(release / "build" / "AscendanceServer.x86_64"), *process["booleanFlags"], "-room", process["room"], "-port", str(process["port"])]
    if process.get("logFile"):
        args.extend(["-logFile", process["logFile"]])
    config_path = str(Path(plan["persistence"]["expectedDataDirectory"]["path"]).parents[2])
    environment = {"XDG_CONFIG_HOME": config_path, **security["environment"]}
    return ("# Managed Ascendance immutable release override; base service remains unchanged.\n[Service]\nExecStart=\nExecStart=" + " ".join(quote_unit(arg) for arg in args) + "\n" + "".join("Environment=" + quote_unit(key + "=" + value) + "\n" for key, value in environment.items())).encode()


def frame_read(stream, size):
    data = b""
    while len(data) < size:
        chunk = stream.recv(size - len(data))
        if not chunk:
            raise Refusal("Secure health connection closed")
        data += chunk
    return data


def blank_authority(value, path=()):
    # JsonUtility materializes null reference fields as default DTOs, including
    # these authored initializers. This allowlist comes from the actual native
    # denied-HELLO DTO, not a production room. Every other nonblank leaf fails.
    defaults = {
        ("Campaign", "Version"): "0.10.2c",
        ("Campaign", "QuestId"): "Q001",
        ("Campaign", "CampaignProgress0833", "CurrentWorldEventId"): "WE001",
        ("Campaign", "CampaignProgress0833", "WorldEventDay"): 1,
        ("Campaign", "CampaignProgress0833", "TotalCampaignDay"): 1,
        ("Campaign", "SearchTravelSeverityRound0852"): -1,
        ("Campaign", "SearchGoblinSeverityRound0852"): -1,
        ("Campaign", "SearchConditionWardRound0852"): -1,
    }
    # Default CommandResult contains its own default Snapshot as well.
    if path and path[0] == "Snapshot":
        path = path[1:]
    if len(path) > 16:
        return False
    if isinstance(value, dict):
        return len(value) <= 500 and all(blank_authority(item, (*path, key)) for key, item in value.items())
    if isinstance(value, list):
        return len(value) == 0
    if value is None or value == "" or value is False or type(value) in (int, float) and value == 0:
        return True
    return path in defaults and type(value) is type(defaults[path]) and value == defaults[path]


def denied_without_authority(response):
    if response.get("Accepted") is not False or response.get("Code") != "ROOM_ACCESS_DENIED":
        return False
    # CommandResult can itself carry another Snapshot; allow only blank values
    # there and in Player, never session/reconnect proof or nested room state.
    for name in ["Snapshot", "Player", "CommandResult"]:
        value = response.get(name)
        if value is not None and (not isinstance(value, dict) or not blank_authority(value)):
            return False
    return True


def same_room(actual, expected):
    # Native routing uses OrdinalIgnoreCase. Limit this comparison to the same
    # ASCII room alphabet accepted by checked_plan; do not fold other identities.
    return all(isinstance(value, str) and re.fullmatch(r"[A-Za-z0-9_-]{1,128}", value)
               for value in (actual, expected)) and actual.lower() == expected.lower()


def public_hello_identity(response, request_id, room):
    response = response if isinstance(response, dict) else {}
    snapshot = response.get("Snapshot")
    snapshot = snapshot if isinstance(snapshot, dict) else {}
    def public_string(value, pattern):
        if value is None:
            return "<missing>"
        return value if isinstance(value, str) and re.fullmatch(pattern, value) else "<invalid>"
    protocol = response.get("ProtocolVersion")
    accepted = response.get("Accepted")
    return {
        "requestIdMatches": response.get("RequestId") == request_id,
        "accepted": accepted if type(accepted) is bool else "<missing-or-invalid>",
        "code": public_string(response.get("Code"), r"[A-Z0-9_]{1,64}"),
        "serverBuildVersion": public_string(response.get("ServerBuildVersion"), r"[0-9][0-9A-Za-z._+-]{0,63}"),
        "protocolVersion": protocol if type(protocol) is int and 0 <= protocol <= 65535 else "<missing-or-invalid>",
        "roomExactMatches": snapshot.get("RoomId") == room,
        "roomMatches": bool(same_room(snapshot.get("RoomId"), room)),
    }


def verify_hello_identity(response, request_id, room, label):
    metadata = public_hello_identity(response, request_id, room)
    valid = (metadata["requestIdMatches"] and metadata["accepted"] is True
             and metadata["code"] == "HELLO" and metadata["serverBuildVersion"] == "0.10.2c"
             and type(metadata["protocolVersion"]) is int and metadata["protocolVersion"] == 1
             and metadata["roomMatches"])
    # Public wire identity fields and booleans only: never include response
    # messages, snapshots, players, logs, reconnect proof or room access keys.
    require(valid, label + " verification failed: " + json.dumps(metadata, sort_keys=True))
    return response["Snapshot"]


def hello(port, room, pin, key, expect_denied=False):
    context = ssl.SSLContext(ssl.PROTOCOL_TLS_CLIENT)
    context.minimum_version = ssl.TLSVersion.TLSv1_2
    context.check_hostname = False
    context.verify_mode = ssl.CERT_NONE
    with socket.create_connection(("127.0.0.1", port), timeout=4) as raw:
        with context.wrap_socket(raw, server_hostname="127.0.0.1") as stream:
            stream.settimeout(8)
            require(digest(stream.getpeercert(binary_form=True)) == pin, "Active service certificate differs from the canary")
            request = {"ProtocolVersion": 1, "RequestId": uuid.uuid4().hex, "Operation": "HELLO", "RoomId": room}
            if key is not None:
                request["RoomAccessKey"] = key
            body = canonical(request)
            stream.sendall(struct.pack("!I", len(body)) + body)
            size = struct.unpack("!I", frame_read(stream, 4))[0]
            require(0 < size <= 8 * 1024 * 1024, "Invalid secure health response size")
            response = json.loads(frame_read(stream, size))
            if key is None or expect_denied:
                metadata = public_hello_identity(response, request["RequestId"], room)
                require(metadata["requestIdMatches"] and type(metadata["protocolVersion"]) is int and metadata["protocolVersion"] == 1,
                        "Secure HELLO envelope verification failed: " + json.dumps(metadata, sort_keys=True))
                require(denied_without_authority(response), "Unauthenticated access was not rejected without authority data")
            else:
                verify_hello_identity(response, request["RequestId"], room, "Read-only authenticated secure HELLO identity")
            return response.get("Snapshot")


def legacy_hello(port, room):
    # Read-only recovery verification of the previous, pre-TLS server. No JOIN,
    # command, player credential or save write is performed.
    with socket.create_connection(("127.0.0.1", port), timeout=4) as stream:
        stream.settimeout(8)
        request = {"ProtocolVersion": 1, "RequestId": uuid.uuid4().hex, "Operation": "HELLO", "RoomId": room}
        body = canonical(request)
        stream.sendall(struct.pack("!I", len(body)) + body)
        size = struct.unpack("!I", frame_read(stream, 4))[0]
        require(0 < size <= 8 * 1024 * 1024, "Invalid previous-service health response size")
        response = json.loads(frame_read(stream, size))
        return verify_hello_identity(response, request["RequestId"], room, "Read-only legacy HELLO identity")


def require_durable_phase(plan, security):
    process = plan["process"]
    if Path(process["executable"]).is_relative_to(ROOT / "releases"):
        snapshot = hello(process["port"], process["room"], security["pin"], security["key"])
    else:
        snapshot = legacy_hello(process["port"], process["room"])
    # Routing accepts ASCII case variants, but Linux persistence filenames do
    # not. Restarting a CLI/save-case mismatch can reload and overwrite stale
    # progress; health compatibility must never authorize that restart.
    require(snapshot.get("RoomId") == process["room"],
            "Live room/save filename capitalization differs; activation not started; keep service running until the active save path is verified.")
    transient = snapshot.get("Transient") or {}
    require(transient.get("IsResolving") is False and transient.get("HasPendingReactionWindow") is False and transient.get("MovingHeroIds") == [] and transient.get("HeroMovement") == [], "Game has an unresolved movement/reaction/resolution; finish it and save before activation")


def healthy(service, release, plan, security, inspector, timeout=60, expected_executable=None):
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        try:
            current = inspector.inspect(service)
            require(current["process"]["executable"] == (expected_executable or str(release / "build" / "AscendanceServer.x86_64")), "Service did not start the staged executable")
            expected_hash = plan["process"]["executableSha256"] if expected_executable else json.loads((release / "manifest.json").read_text())["files"]["AscendanceServer.x86_64"]
            require(current["process"]["executableSha256"] == expected_hash, "Running executable hash differs from the reviewed artifact")
            require(current["process"]["room"] == plan["process"]["room"] and current["process"]["port"] == plan["process"]["port"], "Service routing identity changed")
            require(current["persistence"]["expectedDataDirectory"]["path"] == plan["persistence"]["expectedDataDirectory"]["path"] and current["persistence"]["expectedRoomFileExists"], "Persistence location changed")
            if security is None:
                legacy_hello(plan["process"]["port"], plan["process"]["room"])
            else:
                hello(plan["process"]["port"], plan["process"]["room"], security["pin"], None)
                wrong_key = ("0" if security["key"][0] != "0" else "1") + security["key"][1:]
                hello(plan["process"]["port"], plan["process"]["room"], security["pin"], wrong_key, expect_denied=True)
                hello(plan["process"]["port"], plan["process"]["room"], security["pin"], security["key"])
            return
        except (OSError, ValueError, Refusal, subprocess.SubprocessError):
            time.sleep(1)
    raise Refusal("Secure service health did not pass before the deadline")


def activate(service, release_path, plan, security, inspector, expected, descriptor):
    require(plan_fingerprint(checked_plan(inspector, service)) == expected, "Service configuration changed during preparation")
    require(connected_clients(plan["process"]["port"]) == 0, "Players are still connected; save and close their games before activation")
    require_durable_phase(plan, security)
    override_dir = SYSTEMD / (service + ".service.d")
    require(not inspector.path_details(override_dir)["symlinkComponent"], "Override directory includes a symlink")
    if override_dir.exists():
        require(override_dir.is_dir() and override_dir.stat().st_uid == 0 and not override_dir.stat().st_mode & 0o022, "Override directory ownership/permissions changed")
    prepared_override = override_text(release_path, plan, security)
    backup = backup_existing(plan, descriptor["release"], inspector)
    override = override_dir / "80-ascendance-release.conf"
    previous = override.read_bytes() if override.is_file() and not override.is_symlink() else None
    require(not override.is_symlink(), "Override is a symlink")
    if previous is not None:
        require(override.stat().st_uid == 0 and not override.stat().st_mode & 0o022, "Existing release override ownership/permissions changed")
        require(previous.startswith(b"# Managed Ascendance immutable release override;"), "An unrecognized release override already exists")
    require(plan_fingerprint(checked_plan(inspector, service)) == expected, "Service configuration changed while backing up")
    require(connected_clients(plan["process"]["port"]) == 0, "A player connected while backing up; activation was not started")
    require_durable_phase(plan, security)
    stopped = False
    try:
        stopped = True
        run(["systemctl", "stop", service], timeout=45)
        require(run(["systemctl", "show", service, "--property=ActiveState", "--value"]).stdout.strip() in ["inactive", "failed"], "Service did not stop")
        data = Path(plan["persistence"]["expectedDataDirectory"]["path"])
        write_new(backup / "data-after-stop-sha256.json", canonical(backup_tree(data, backup / "data-after-stop", inspector)))
        if not override_dir.exists():
            secure_directory(override_dir, 0o755)
        temporary = override_dir / (".ascendance-" + uuid.uuid4().hex)
        write_new(temporary, prepared_override, 0o644)
        os.replace(temporary, override)
        run(["systemctl", "daemon-reload"])
        run(["systemctl", "start", service])
        healthy(service, release_path, plan, security, inspector)
        profile = {"format": 1, "endpoint": descriptor["endpoint"], "room": plan["process"]["room"], "certificateSha256": security["pin"], "accessKey": security["key"]}
        profile_path = CONFIG / "owner-connection.json"
        temporary = CONFIG / (".owner-" + uuid.uuid4().hex)
        write_new(temporary, canonical(profile))
        os.replace(temporary, profile_path)
        return {"status": "ACTIVATED_SECURE", "release": descriptor["release"], "sourceRevision": descriptor["sourceRevision"], "endpoint": descriptor["endpoint"], "room": plan["process"]["room"], "certificateSha256": security["pin"], "ownerProfilePath": str(profile_path), "backupDirectory": str(backup), "secureTransportVerified": True, "existingDataPathPreserved": True}
    except Exception:
        if stopped:
            with contextlib.suppress(Exception):
                run(["systemctl", "stop", service], timeout=45)
            try:
                if previous is None:
                    with contextlib.suppress(FileNotFoundError):
                        override.unlink()
                else:
                    replacement = override_dir / (".rollback-" + uuid.uuid4().hex)
                    write_new(replacement, previous, 0o644)
                    os.replace(replacement, override)
                run(["systemctl", "daemon-reload"])
                run(["systemctl", "start", service])
                healthy(service, release_path, plan, security if previous is not None else None, inspector, expected_executable=plan["process"]["executable"])
            except Exception:
                raise RecoveryFailure("Upgrade and previous-service recovery both failed. Inspect the private backup immediately: " + str(backup))
        raise Refusal("Upgrade did not pass; previous service configuration/code was restored. Save data was not automatically rolled back. Backup: " + str(backup))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--descriptor", type=Path, required=True)
    parser.add_argument("--archive", type=Path, required=True)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    try:
        require(os.geteuid() == 0, "Use the owner's root SSH session for deployment")
        os.umask(0o077)
        tools, inspector = sibling("release"), sibling("inspect-existing")
        descriptor = tools.read_json(args.descriptor)
        require(descriptor.get("format") == 1 and re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._-]{0,63}", descriptor.get("release", "")), "Invalid release descriptor")
        require(re.fullmatch(r"[0-9a-f]{40}", descriptor.get("sourceRevision", "")), "Missing reviewed source revision")
        endpoint_ip, endpoint_port = descriptor["endpoint"].rsplit(":", 1)
        require(ipaddress.ip_address(endpoint_ip).version == 4, "Expected an IPv4 game endpoint")
        service = "ascendance"
        plan = checked_plan(inspector, service)
        require(int(endpoint_port) == plan["process"]["port"], "The release cannot change the existing game port")
        require_endpoint_on_host(endpoint_ip)
        manifest = tools.read_json(args.manifest)
        tools.validate_manifest(manifest)
        require(digest(args.archive.read_bytes()) == descriptor["server"]["archive"]["sha256"].lower() and digest(args.manifest.read_bytes()) == descriptor["server"]["manifest"]["sha256"].lower(), "Uploaded artifact checksum differs from the reviewed descriptor")
        secure_directory(ROOT, 0o755)
        secure_directory(ROOT / "releases", 0o755)
        lock = os.open(ROOT / ".upgrade.lock", os.O_CREAT | os.O_RDWR | os.O_NOFOLLOW, 0o600)
        with os.fdopen(lock, "wb"):
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
            release_path = ROOT / "releases" / descriptor["release"]
            if not release_path.exists():
                release_path.mkdir(mode=0o755)
                os.chmod(release_path, 0o755)
                extract_build(args.archive, release_path / "build", manifest, tools)
                write_new(release_path / "manifest.json", canonical(manifest), 0o644)
                write_new(release_path / "source-revision", (descriptor["sourceRevision"] + "\n").encode(), 0o644)
            else:
                require((release_path / "source-revision").read_text().strip() == descriptor["sourceRevision"], "Existing immutable release has another source revision")
                tools.verify_artifact(release_path / "build", manifest)
            security = secure_configuration(service, plan["process"], endpoint_ip, inspector)
            verdict = canary(release_path, release_path / "manifest.json", descriptor, plan, security)
            if not args.apply:
                print(json.dumps({"status": "STAGED_SECURE", "release": descriptor["release"], "endpoint": descriptor["endpoint"], "room": plan["process"]["room"], "certificateSha256": security["pin"], "inspectionFingerprint": plan_fingerprint(plan), "canary": verdict, "productionServiceChanged": False}, indent=2))
            else:
                result = activate(service, release_path, plan, security, inspector, plan_fingerprint(plan), descriptor)
                result["canary"] = verdict
                print(json.dumps(result, indent=2))
        return 0
    except Exception as error:
        print(json.dumps({"status": "RECOVERY_FAILED" if isinstance(error, RecoveryFailure) else "REFUSED", "error": type(error).__name__, "message": str(error) if isinstance(error, Refusal) else "Deployment could not complete; inspect the staged release and private backup. No success is assumed."}, indent=2))
        return 1


if __name__ == "__main__":
    sys.exit(main())
