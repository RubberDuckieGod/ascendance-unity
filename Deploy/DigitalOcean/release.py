#!/usr/bin/env python3
"""Local, fresh-install-only release support. No SSH, downloads, or game emulation."""

import argparse
import contextlib
import fcntl
import grp
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import pwd
import re
import shlex
import shutil
import stat
import subprocess
import sys
import uuid


class Refusal(Exception):
    pass


def require(ok, message):
    if not ok:
        raise Refusal(message)


def identifier(value):
    require(bool(re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._-]{0,63}", value)), "Invalid identifier.")
    return value


def strict_path(value, *, install=False):
    path = Path(value)
    require(path.is_absolute() and ".." not in path.parts, "Paths must be absolute without '..'.")
    require(path != Path("/"), "The filesystem root is not a valid target.")
    if install:
        require(bool(re.fullmatch(r"/[A-Za-z0-9_./-]+", str(path))), "Install paths need simple characters without spaces.")
    for part in [*reversed(path.parents), path]:
        require(not part.is_symlink(), "Symlink path components are refused.")
    return path


def digest(data):
    return hashlib.sha256(data).hexdigest()


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, "Duplicate manifest/configuration key.")
        result[key] = value
    return result


def read_json(path):
    return json.loads(regular_bytes(path), object_pairs_hook=unique_object)


def regular_bytes(path):
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
    with os.fdopen(fd, "rb") as source:
        require(stat.S_ISREG(os.fstat(source.fileno()).st_mode), "Managed metadata must be regular files, without symlinks.")
        return source.read()


def write_new(path, data, mode=0o644):
    fd = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, mode)
    with os.fdopen(fd, "wb") as output:
        os.fchmod(output.fileno(), mode)
        output.write(data)


@contextlib.contextmanager
def file_from_tree(root, relative):
    """Open every component relative to a directory FD without following links."""
    descriptors = [os.open(root, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW)]
    try:
        parts = PurePosixPath(relative).parts
        for part in parts[:-1]:
            descriptors.append(os.open(part, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW, dir_fd=descriptors[-1]))
        fd = os.open(parts[-1], os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK, dir_fd=descriptors[-1])
        with os.fdopen(fd, "rb") as source:
            require(stat.S_ISREG(os.fstat(source.fileno()).st_mode), "Artifact entries must be regular files.")
            yield source
    finally:
        for descriptor in reversed(descriptors):
            os.close(descriptor)


def tree_files(root):
    strict_path(str(root))
    require(root.is_dir(), "Build directory is missing.")
    files = []
    def fail_walk(error):
        raise Refusal("Unreadable artifact directory.") from error
    for directory, dirs, names in os.walk(root, followlinks=False, onerror=fail_walk):
        for name in dirs + names:
            path = Path(directory) / name
            mode = path.lstat().st_mode
            require(stat.S_ISDIR(mode) or stat.S_ISREG(mode), "Artifact symlinks and special files are refused.")
        files.extend((Path(directory) / name).relative_to(root).as_posix() for name in names)
    return sorted(files)


def artifact_manifest(root):
    files = tree_files(root)
    require("AscendanceServer.x86_64" in files and "UnityPlayer.so" in files, "Incomplete Linux artifact: executable or UnityPlayer.so missing.")
    data_files = [name for name in files if name.startswith("AscendanceServer_Data/")]
    require(bool(data_files), "Incomplete Linux artifact: data directory is empty or missing.")
    nonempty_data = False
    result = {}
    for relative in files:
        with file_from_tree(root, relative) as source:
            if relative in data_files and source.read(1):
                nonempty_data = True
            source.seek(0)
            if relative in ("AscendanceServer.x86_64", "UnityPlayer.so"):
                header = source.read(20)
                require(len(header) == 20 and header[:6] == b"\x7fELF\x02\x01" and header[18:20] == b"\x3e\x00", "Expected an x86-64 little-endian ELF executable/library.")
                source.seek(0)
            hasher = hashlib.sha256()
            for block in iter(lambda: source.read(1024 * 1024), b""):
                hasher.update(block)
            result[relative] = hasher.hexdigest()
    require(nonempty_data, "Incomplete Linux artifact: data files are empty.")
    return {"format": 1, "files": result}


def validate_manifest(manifest):
    require(isinstance(manifest, dict) and set(manifest) == {"format", "files"} and manifest["format"] == 1, "Unsupported artifact manifest.")
    require(isinstance(manifest["files"], dict) and manifest["files"], "Empty artifact manifest.")
    for name, checksum in manifest["files"].items():
        path = PurePosixPath(name)
        require(isinstance(name, str) and name and not path.is_absolute() and ".." not in path.parts and path.as_posix() == name and "\\" not in name, "Manifest contains a traversal or noncanonical path.")
        require(isinstance(checksum, str) and bool(re.fullmatch(r"[0-9a-f]{64}", checksum)), "Invalid SHA-256 in manifest.")


def verify_artifact(root, manifest):
    validate_manifest(manifest)
    require(artifact_manifest(root) == manifest, "Artifact files or SHA-256 checksums do not match the manifest.")


def environment(config):
    values = {"ASCENDANCE_ROOT": config["root"], "ASCENDANCE_DATA_HOME": config["data_home"],
              "ASCENDANCE_LOG_DIR": config["log_dir"], "ASCENDANCE_ROOM": config["room"], "ASCENDANCE_PORT": str(config["port"])}
    return "".join(f"{key}={value}\n" for key, value in values.items()).encode()


def unit_text(config):
    return f"""# Managed by Ascendance release.py; identity and persistence are immutable.
[Unit]
Description=Ascendance dedicated server
After=network.target
[Service]
Type=simple
User={config['user']}
Group={config['group']}
WorkingDirectory={config['data_home']}
EnvironmentFile={config['config_dir']}/server.env
ExecStart={config['root']}/run-ascendance.sh
Restart=on-failure
RestartSec=5
TimeoutStopSec=30
UMask=0077
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths={config['data_home']} {config['log_dir']}
[Install]
WantedBy=multi-user.target
""".encode()


def service(args, *command):
    try:
        result = subprocess.run([args.service_command, *command], capture_output=True, text=True, timeout=30)
    except (OSError, subprocess.TimeoutExpired) as error:
        raise Refusal("Service command failed or timed out.") from error
    require(result.returncode == 0, "Service command failed: " + " ".join(command))
    return result.stdout.strip()


def initialize(args):
    root = strict_path(args.root, install=True)
    external = [strict_path(value, install=True) for value in (args.config_dir, args.data_home, args.log_dir)]
    paths = [root, *external]
    for index, path in enumerate(paths):
        require(not path.exists(), "Fresh installs require absent install/config/data/log paths; existing layouts need a reviewed migration.")
        require(path.parent.is_dir(), "Parent directories must already exist.")
        for other in paths[index + 1:]:
            require(path != other and path not in other.parents and other not in path.parents, "Install/config/data/log paths must not overlap.")
    unit_dir = strict_path(args.unit_dir, install=True)
    require(unit_dir.is_dir(), "Systemd unit directory must exist.")
    name = identifier(args.service_name) + ".service"
    unit = unit_dir / name
    require(not unit.exists() and not unit.is_symlink(), "An existing service unit is unmanaged; migration is required.")
    require(service(args, "show", "--property=LoadState", "--value", name) == "not-found", "An existing or unknown service is refused.")
    account = pwd.getpwnam(args.user)
    group = grp.getgrgid(account.pw_gid)
    require(bool(re.fullmatch(r"[A-Za-z_][A-Za-z0-9_-]*", account.pw_name)), "Unsupported service account name.")
    require(bool(re.fullmatch(r"[A-Za-z_][A-Za-z0-9_-]*", group.gr_name)), "Unsupported service group name.")
    config = {"format": 1, "root": str(root), "config_dir": str(external[0]), "data_home": str(external[1]),
              "log_dir": str(external[2]), "unit": str(unit), "service": name, "user": account.pw_name,
              "group": group.gr_name, "uid": account.pw_uid, "gid": account.pw_gid,
              "room": identifier(args.room), "port": args.port}
    require(bool(re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9_-]{0,63}", args.room)), "Room IDs may contain only letters, digits, underscores and hyphens.")
    require(1 <= args.port <= 65535, "Invalid TCP port.")
    runner = Path(__file__).with_name("run-ascendance.sh").read_bytes()
    config["runner_sha256"] = digest(runner)
    # No service is started or enabled. Failure leaves files for inspection, never removes data.
    root.mkdir(mode=0o755)
    root.chmod(0o755)
    (root / "releases").mkdir(mode=0o755)
    (root / "releases").chmod(0o755)
    for index, path in enumerate(external):
        path.mkdir(mode=0o750)
        path.chmod(0o750)
        owner = os.getuid() if index == 0 else account.pw_uid
        if (owner, account.pw_gid) != (os.getuid(), os.getgid()):
            os.chown(path, owner, account.pw_gid)
    write_new(external[0] / "server.env", environment(config), 0o640)
    if account.pw_gid != os.getgid():
        os.chown(external[0] / "server.env", -1, account.pw_gid)
    write_new(root / "run-ascendance.sh", runner, 0o755)
    write_new(unit, unit_text(config))
    write_new(root / "managed.json", (json.dumps(config, indent=2) + "\n").encode())
    print("Fresh layout prepared. Service has not been started or enabled.")


@contextlib.contextmanager
def managed(args):
    root = strict_path(args.root, install=True)
    require(root.is_dir(), "Managed install root is missing.")
    fd = os.open(root / ".release.lock", os.O_CREAT | os.O_RDWR | os.O_NOFOLLOW | os.O_NONBLOCK, 0o600)
    try:
        require(stat.S_ISREG(os.fstat(fd).st_mode), "Managed lock must be a regular file.")
        fcntl.flock(fd, fcntl.LOCK_EX | fcntl.LOCK_NB)
        config = read_json(root / "managed.json")
        fields = {"format", "root", "config_dir", "data_home", "log_dir", "unit", "service", "user", "group", "uid", "gid", "room", "port", "runner_sha256"}
        require(isinstance(config, dict) and set(config) == fields, "Unsupported managed metadata schema.")
        require(config["format"] == 1 and config["root"] == str(root), "Unmanaged or moved install root.")
        require(all(isinstance(config[key], str) for key in fields - {"format", "uid", "gid", "port"}), "Invalid managed metadata field types.")
        require(all(type(config[key]) is int for key in ("format", "uid", "gid", "port")) and 1 <= config["port"] <= 65535, "Invalid managed identity or port.")
        require(bool(re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9_-]{0,63}", config["room"])), "Invalid managed room identity.")
        require(config["service"] == Path(config["unit"]).name and config["service"].endswith(".service"), "Invalid managed service identity.")
        identifier(config["service"][:-8])
        for key in ("config_dir", "data_home", "log_dir", "unit"):
            strict_path(config[key], install=True)
        require(all(Path(config[key]).is_dir() for key in ("config_dir", "data_home", "log_dir")), "External configuration/data/log directories are missing.")
        account = pwd.getpwnam(config["user"])
        require((account.pw_uid, account.pw_gid, grp.getgrgid(account.pw_gid).gr_name) == (config["uid"], config["gid"], config["group"]), "Service account identity has changed.")
        require(regular_bytes(Path(config["unit"])) == unit_text(config), "Service unit has changed or is unmanaged.")
        require(regular_bytes(Path(config["config_dir"]) / "server.env") == environment(config), "Configuration/identity changes require a reviewed migration.")
        require(digest(regular_bytes(root / "run-ascendance.sh")) == config["runner_sha256"], "Managed runner has changed.")
        require((root / "releases").is_dir() and not (root / "releases").is_symlink(), "Unmanaged releases directory.")
        yield root, config
    finally:
        os.close(fd)


def stage(args):
    build = strict_path(args.build_dir)
    manifest = read_json(strict_path(args.manifest))
    verify_artifact(build, manifest)
    with managed(args) as (root, _):
        destination = root / "releases" / identifier(args.release)
        require(not destination.exists() and not destination.is_symlink(), "Release already exists; releases are never overwritten.")
        temporary = root / "releases" / (".staging-" + uuid.uuid4().hex)
        temporary.mkdir()
        temporary.chmod(0o755)
        try:
            output = temporary / "build"
            output.mkdir()
            for relative in manifest["files"]:
                target = output / relative
                target.parent.mkdir(parents=True, exist_ok=True)
                with file_from_tree(build, relative) as source, target.open("xb") as copied:
                    shutil.copyfileobj(source, copied)
                    executable = bool(os.fstat(source.fileno()).st_mode & 0o111)
                target.chmod(0o755 if executable else 0o644)
            (output / "AscendanceServer.x86_64").chmod(0o755)
            for directory, _, _ in os.walk(output):
                Path(directory).chmod(0o755)
            verify_artifact(output, manifest)
            write_new(temporary / "manifest.json", (json.dumps(manifest, indent=2) + "\n").encode())
            temporary.rename(destination)
        except BaseException:
            shutil.rmtree(temporary)
            raise
    print("Release staged and integrity checked. Service unchanged.")


def current_release(root):
    current = root / "current"
    if not current.is_symlink():
        require(not current.exists(), "Existing current path is unmanaged.")
        return None
    target = os.readlink(current)
    parts = PurePosixPath(target).parts
    require(len(parts) == 3 and parts[0] == "releases" and parts[2] == "build", "Unmanaged current symlink.")
    release = identifier(parts[1])
    strict_path(str(root / target))
    require((root / target).is_dir(), "Current release is missing.")
    return release


def point_current(root, release):
    if release is None:
        (root / "current").unlink(missing_ok=True)
        return
    temporary = root / (".current-" + uuid.uuid4().hex)
    temporary.symlink_to("releases/" + release + "/build")
    os.replace(temporary, root / "current")


def health(args, root, config, release):
    command = shlex.split(args.health_command)
    require(bool(command) and Path(command[0]).is_absolute(), "Health command must name an absolute executable; shell syntax is not supported.")
    env = dict(os.environ, ASCENDANCE_RELEASE=release, ASCENDANCE_ROOT=str(root), ASCENDANCE_ROOM=config["room"], ASCENDANCE_PORT=str(config["port"]))
    try:
        result = subprocess.run(command, env=env, capture_output=True, timeout=args.health_timeout)
    except (OSError, subprocess.TimeoutExpired) as error:
        raise Refusal("Operator health command failed or timed out.") from error
    require(result.returncode == 0, "Operator health command failed.")


def activate(args):
    # Validate before any service command or symlink switch.
    require(0 < args.health_timeout <= 600, "Health timeout must be between 0 and 600 seconds.")
    require(bool(shlex.split(args.health_command)) and Path(shlex.split(args.health_command)[0]).is_absolute(), "Health command must use an absolute executable.")
    with managed(args) as (root, config):
        release = identifier(args.release)
        location = strict_path(str(root / "releases" / release))
        verify_artifact(location / "build", read_json(location / "manifest.json"))
        previous = current_release(root)
        require(previous != release, "Release is already current.")
        if previous is not None:
            old = root / "releases" / previous
            verify_artifact(old / "build", read_json(old / "manifest.json"))
        service(args, "daemon-reload")
        require(service(args, "show", "--property=FragmentPath", "--value", config["service"]) == config["unit"], "Loaded service is unmanaged.")
        require(not service(args, "show", "--property=DropInPaths", "--value", config["service"]), "Service overrides require a reviewed migration.")
        point_current(root, release)
        try:
            service(args, "restart", config["service"])
            health(args, root, config, release)
        except (Refusal, KeyboardInterrupt) as original:
            point_current(root, previous)
            try:
                service(args, "restart" if previous else "stop", config["service"])
                if previous:
                    health(args, root, config, previous)
            except (Refusal, KeyboardInterrupt) as rollback:
                raise Refusal(f"Activation failed ({original}); previous link restored, but recovery failed ({rollback}). Inspect the service.") from original
            raise Refusal(f"Activation failed ({original}); previous release restored and recovery completed.") from original
    print("Release activated; operator health command passed. Multiplayer validation is only as strong as that command.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="action", required=True)
    manifest = commands.add_parser("manifest", help="Record artifact structure and SHA-256; does not prove game correctness.")
    manifest.add_argument("--build-dir", required=True)
    manifest.add_argument("--output", required=True)
    init = commands.add_parser("init", help="Prepare a fresh local install only; never starts/enables a service.")
    for option in ("config-dir", "data-home", "log-dir", "unit-dir", "user", "room"):
        init.add_argument("--" + option, required=True)
    init.add_argument("--service-name", default="ascendance")
    init.add_argument("--port", type=int, default=7777)
    stage_parser = commands.add_parser("stage")
    stage_parser.add_argument("--build-dir", required=True)
    stage_parser.add_argument("--manifest", required=True)
    activation = commands.add_parser("activate")
    activation.add_argument("--health-command", required=True)
    activation.add_argument("--health-timeout", type=float, default=60)
    status = commands.add_parser("status", help="Local metadata only; does not probe networking or gameplay.")
    for command in (init, stage_parser, activation, status):
        command.add_argument("--root", required=True)
        command.add_argument("--service-command", default="/usr/bin/systemctl", help="Local service executable; override for isolated tests.")
    for command in (stage_parser, activation):
        command.add_argument("--release", required=True)
    args = parser.parse_args()
    try:
        if args.action == "manifest":
            build = strict_path(args.build_dir)
            output = strict_path(args.output)
            require(build != output and build not in output.parents, "Manifest must be written outside the build tree.")
            write_new(output, (json.dumps(artifact_manifest(build), indent=2) + "\n").encode())
            print("Artifact manifest written. Structural/integrity checks do not prove runtime completeness.")
        elif args.action == "init":
            initialize(args)
        elif args.action == "stage":
            stage(args)
        elif args.action == "activate":
            activate(args)
        else:
            with managed(args) as (root, _):
                print("Managed layout; current release: " + (current_release(root) or "none") + ". No runtime validation performed.")
    except (Refusal, OSError, ValueError, KeyError, TypeError) as error:
        print("REFUSED: " + str(error), file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
