#!/usr/bin/env python3
"""Package validated, clean-source Unity builds locally; never upload or deploy."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
from pathlib import Path, PurePosixPath
import re
import shutil
import stat
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
README = """Ascendance multiplayer — Windows

1. Extract the complete Windows ZIP into a folder whose path has no square
   brackets. Keep Ascendance.exe, Ascendance_Data,
   UnityPlayer.dll and both Play-AscendanceMultiplayer files together.
2. Keep your existing private Ascendance.invite.json, or get it from the server
   owner. Put it beside
   Play-AscendanceMultiplayer.cmd, or enter its full path when prompted.
3. Double-click Play-AscendanceMultiplayer.cmd and enter your player name.
4. Choose JOIN ONLINE CAMPAIGN. For an existing campaign, choose an available
   Hero from that saved party. Every friend uses the same invite and their own
   player name and Hero.
5. To begin a separate adventure, the campaign creator chooses NEW ONLINE
   CAMPAIGN and confirms. The previous campaign is saved in its own slot.
   The new draft shows all 16 Heroes. Each of 1-4 players claims a different
   Hero and chooses READY. The creator then chooses START CAMPAIGN.
6. Use SAVED CAMPAIGNS to restore a previous adventure. Have other players
   return to the menu before switching campaigns, then join the selected one.

The owner must first deploy and verify the matching Linux server. This download
contains no private invite or server credentials. Send the invite privately;
never attach it to a public release. If the owner rotates it, obtain a fresh copy.
The matching server update is required for the new campaign setup. Updating
the software alone keeps the existing campaign active. Keep your local player
data to retain your reconnect and campaign-creator identity.
"""
PRIVATE_EXTENSIONS = {".pfx", ".p12", ".pem", ".key", ".cer", ".crt", ".der", ".ulf", ".lic", ".env", ".keystore", ".jks"}
PRIVATE_NAMES = {"authorized_keys", "known_hosts", ".env", "credentials", "credentials.json", "room-access-key", "pfx-password"}
PRIVATE_DIRECTORIES = {".ssh", ".aws", ".codex", "ascendanceserverrooms0100"}
PRIVATE_FIELDS = {"accesskey", "roomaccesskey", "reconnecttoken", "password", "passwd", "privatekey", "clientsecret", "refreshtoken", "campaignownertoken0102"}


class Refusal(Exception):
    pass


def require(condition, message):
    if not condition:
        raise Refusal(message)


def digest(data):
    return hashlib.sha256(data).hexdigest()


def canonical(value):
    return (json.dumps(value, indent=2, sort_keys=True) + "\n").encode()


def release_tools():
    spec = importlib.util.spec_from_file_location("ascendance_release_packaging_support", ROOT / "Deploy/DigitalOcean/release.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def unique_json(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, "Duplicate key in build JSON is refused.")
        result[key] = value
    return result


def private_json(value):
    if isinstance(value, dict):
        return any((str(key).lower().replace("_", "") in PRIVATE_FIELDS and item not in (None, "", False))
                   or private_json(item) for key, item in value.items())
    if isinstance(value, list):
        return any(private_json(item) for item in value)
    return False


def check_public_file(relative, content):
    path = Path(relative)
    require(not PurePosixPath(relative).is_absolute() and ".." not in path.parts and "\\" not in relative and ":" not in relative,
            "Noncanonical or unsafe archive path in build payload.")
    require(not any(part.lower() in PRIVATE_DIRECTORIES for part in path.parts), "Private data directory found in build payload.")
    require(path.suffix.lower() not in PRIVATE_EXTENSIONS and path.name.lower() not in PRIVATE_NAMES
            and "invite" not in path.name.lower(), "Private credential/invite file found in build payload.")
    if path.suffix.lower() == ".json":
        try:
            parsed = json.loads(content, object_pairs_hook=unique_json)
        except (ValueError, UnicodeError):
            parsed = None
        require(not private_json(parsed), "Private credential fields found in build JSON.")
    if path.suffix.lower() in {".json", ".txt", ".xml", ".yaml", ".yml", ".ps1", ".cmd", ".conf", ".config"}:
        require(not re.search(br"-----BEGIN (?:RSA |EC |OPENSSH |ENCRYPTED )?PRIVATE KEY-----", content), "Private key material found in text payload.")


def snapshot(build, tools):
    result = {}
    for relative in tools.tree_files(build):
        with tools.file_from_tree(build, relative) as stream:
            content = stream.read()
        check_public_file(relative, content)
        result[relative] = digest(content)
    return result


def provenance(build, filename, target, revision, tools):
    path = build / filename
    require(filename in tools.tree_files(build), "Unity build provenance is missing.")
    document = tools.read_json(path)
    require(document.get("outcome") == "success" and document.get("exitCode") == 0, "Unity build provenance does not report success.")
    require(document.get("build", {}).get("target") == target, "Unity build provenance target differs from the package.")
    source = document.get("source", {})
    require(source.get("revision") == revision and source.get("dirty") is False, "Build requires the exact reviewed clean source revision; preliminary dirty builds are refused.")
    return {"file": filename, "sha256": digest(tools.regular_bytes(path)), "sourceRevision": source["revision"], "sourceDirty": source["dirty"], "target": target}


def add_zip(archive, name, content, executable=False):
    info = zipfile.ZipInfo(name, (1980, 1, 1, 0, 0, 0))
    info.create_system = 3
    info.external_attr = (stat.S_IFREG | (0o755 if executable else 0o644)) << 16
    info.compress_type = zipfile.ZIP_DEFLATED
    archive.writestr(info, content)


def package(args):
    tools = release_tools()
    require(bool(re.fullmatch(r"[0-9a-f]{40}", args.source_revision)), "Source revision must be an exact lowercase 40-character commit SHA.")
    require(bool(re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._-]{0,63}", args.release)), "Invalid release identifier.")
    linux = tools.strict_path(str(args.linux_build.absolute()))
    windows = tools.strict_path(str(args.windows_build.absolute()))
    output = tools.strict_path(str(args.output.absolute()))
    require(output.parent.is_dir() and not output.exists(), "Output requires an existing parent and an absent destination.")
    require(all(output != build and output not in build.parents and build not in output.parents for build in [linux, windows]), "Output and build directories must not overlap.")
    manifest_path = tools.strict_path(str(args.linux_manifest.absolute()))
    linux_manifest = tools.read_json(manifest_path)
    tools.verify_artifact(linux, linux_manifest)
    linux_files = snapshot(linux, tools)
    require(linux_files == linux_manifest["files"], "Linux file set changed after validation.")
    windows_files = snapshot(windows, tools)
    require(len({name.casefold() for name in windows_files}) == len(windows_files), "Windows build has case-colliding paths.")
    require(all(name in windows_files for name in ["Ascendance.exe", "UnityPlayer.dll"])
            and any(name.startswith("Ascendance_Data/") for name in windows_files), "Incomplete Windows player build.")
    require(sum(name.lower().endswith("ascendance.exe") for name in windows_files) == 1, "Windows package needs exactly one game executable.")
    for name in ["Ascendance.exe", "UnityPlayer.dll"]:
        with tools.file_from_tree(windows, name) as stream:
            require(stream.read(2) == b"MZ", "Expected Windows PE executable/library.")
    proofs = {
        "linux": provenance(linux, "AscendanceServer.provenance.json", "StandaloneLinux64", args.source_revision, tools),
        "windows": provenance(windows, "Ascendance.provenance.json", "StandaloneWindows64", args.source_revision, tools),
    }
    extras = {name: tools.regular_bytes(ROOT / "tools" / name) for name in ["Play-AscendanceMultiplayer.ps1", "Play-AscendanceMultiplayer.cmd"]}
    extras["CONNECTION_README.txt"] = README.encode()
    require(not {n.casefold() for n in extras}.intersection(n.casefold() for n in windows_files), "Build already contains package-owned launcher/readme files.")
    for name, content in extras.items():
        check_public_file(name, content)
    windows_package = {**windows_files, **{name: digest(data) for name, data in extras.items()}}
    names = {"linuxArchive": "ascendance-linux-server-" + args.release + ".zip", "windowsArchive": "ascendance-windows-" + args.release + ".zip"}
    with tempfile.TemporaryDirectory(prefix=".ascendance-public-package-", dir=output.parent) as temporary:
        stage = Path(temporary) / "package"
        stage.mkdir()
        for build, files, prefix, archive_name, additions in [
            (linux, linux_files, "LinuxServer/", names["linuxArchive"], {}),
            (windows, windows_files, "Windows/", names["windowsArchive"], extras),
        ]:
            with zipfile.ZipFile(stage / archive_name, "x", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
                for relative, expected in sorted(files.items()):
                    with tools.file_from_tree(build, relative) as stream:
                        content = stream.read()
                    require(digest(content) == expected, "Build changed while packaging; no release was written.")
                    add_zip(archive, prefix + relative, content, relative == "AscendanceServer.x86_64")
                for relative, content in sorted(additions.items()):
                    add_zip(archive, prefix + relative, content)
        require(snapshot(linux, tools) == linux_files and snapshot(windows, tools) == windows_files, "Build file set changed during packaging.")
        (stage / "linux-server-manifest.json").write_bytes(tools.regular_bytes(manifest_path))
        (stage / "windows-client-manifest.json").write_bytes(canonical({"format": 1, "files": windows_package}))
        (stage / "CONNECTION_README.txt").write_text(README)
        proofs["linux"]["fileManifestSha256"] = digest((stage / "linux-server-manifest.json").read_bytes())
        proofs["windows"]["fileManifestSha256"] = digest((stage / "windows-client-manifest.json").read_bytes())
        (stage / "RuntimeSourceRevision.json").write_bytes(canonical({"format": 1, "RuntimeSourceRevision": args.source_revision, "release": args.release, "builds": proofs}))
        contents = {path.name: digest(path.read_bytes()) for path in sorted(stage.iterdir())}
        (stage / "release-package-manifest.json").write_bytes(canonical({"format": 1, "sourceRevision": args.source_revision, "files": contents}))
        contents["release-package-manifest.json"] = digest((stage / "release-package-manifest.json").read_bytes())
        (stage / "SHA256SUMS").write_text("".join(checksum + "  " + name + "\n" for name, checksum in sorted(contents.items())))
        stage.rename(output)
    return {"output": str(output), "sourceRevision": args.source_revision, "linuxFiles": len(linux_files), "windowsFiles": len(windows_package), "artifacts": contents}


def extract_validated_server(archive_path, manifest_path, destination, source_revision):
    """Validate an untrusted public archive before any native binary is launched."""
    tools = release_tools()
    require(re.fullmatch(r"[0-9a-f]{40}", source_revision), "Invalid reviewed source revision.")
    destination = tools.strict_path(str(Path(destination).absolute()))
    require(not destination.exists(), "Server extraction destination must be absent.")
    manifest = tools.read_json(Path(manifest_path))
    tools.validate_manifest(manifest)
    with zipfile.ZipFile(archive_path) as archive:
        entries = archive.infolist()
        require(0 < len(entries) <= 5000 and sum(e.file_size for e in entries) <= 512 * 1024 * 1024, "Invalid server ZIP size or entry count.")
        names = set()
        for entry in entries:
            name = entry.filename
            path = PurePosixPath(name)
            require(not path.is_absolute() and ".." not in path.parts and "\\" not in name and ":" not in name
                    and path.as_posix() == name.rstrip("/") and path.parts and path.parts[0] == "LinuxServer", "Unsafe or unexpected server ZIP path.")
            mode = (entry.external_attr >> 16) & 0o170000
            require(mode in (0, stat.S_IFREG, stat.S_IFDIR), "Server ZIP contains a symlink or special file.")
            require(name not in names, "Duplicate server ZIP path.")
            names.add(name)
        files = {str(PurePosixPath(e.filename).relative_to("LinuxServer")) for e in entries if not e.is_dir()}
        require(files == set(manifest["files"]), "Server ZIP file set differs from its reviewed manifest.")
        destination.mkdir(parents=True)
        for entry in entries:
            if entry.is_dir():
                continue
            relative = PurePosixPath(entry.filename).relative_to("LinuxServer")
            target = destination / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            with archive.open(entry) as incoming, target.open("xb") as outgoing:
                shutil.copyfileobj(incoming, outgoing)
    tools.verify_artifact(destination, manifest)
    provenance(destination, "AscendanceServer.provenance.json", "StandaloneLinux64", source_revision, tools)
    snapshot(destination, tools)
    (destination / "AscendanceServer.x86_64").chmod(0o755)
    return destination


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--linux-build", type=Path, required=True)
    parser.add_argument("--linux-manifest", type=Path, required=True)
    parser.add_argument("--windows-build", type=Path, required=True)
    parser.add_argument("--source-revision", required=True)
    parser.add_argument("--release", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        result = package(args)
    except Exception as error:
        parser.exit(1, "Packaging refused: " + str(error) + "\n")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
