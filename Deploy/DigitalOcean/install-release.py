#!/usr/bin/env python3
"""Fetch the pinned public release on its host and run the reviewed upgrader.

Run from an ordinary interactive root SSH session. Without --apply this only
stages and validates; the unchanged upgrader owns all canaries, maintenance
guards, backups, activation and recovery. No player invite or key is printed.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import urllib.parse
import urllib.request


RELEASE = "unity-0.10.2c-multiplayer-eb51b22"
SOURCE_REVISION = "eb51b2292cf433a2cbe5dabb4863f1fef993161a"
RELEASE_BASE = "https://github.com/RubberDuckieGod/ascendance-unity/releases/download/" + RELEASE + "/"
SOURCE_BASE = "https://raw.githubusercontent.com/RubberDuckieGod/ascendance-unity/" + SOURCE_REVISION + "/"
DESCRIPTOR_URL = RELEASE_BASE + "release.json"
DESCRIPTOR_SHA256 = "91be6e70423791cd66bd89eada21e411301720694b05279c02bba541953b80bd"
HELPER_PATHS = {
    "upgrade-existing.py": "Deploy/DigitalOcean/upgrade-existing.py",
    "inspect-existing.py": "Deploy/DigitalOcean/inspect-existing.py",
    "release.py": "Deploy/DigitalOcean/release.py",
    "native_server_acceptance.py": "tools/native_server_acceptance.py",
}
NETWORK_HOSTS = {"github.com", "raw.githubusercontent.com", "release-assets.githubusercontent.com",
                 "objects.githubusercontent.com", "github-releases.githubusercontent.com"}


class Refusal(Exception):
    pass


def require(ok, message):
    if not ok:
        raise Refusal(message)


def progress(message):
    print("Ascendance: " + message, file=sys.stderr, flush=True)


def network_url(value):
    require(isinstance(value, str) and not any(ord(char) <= 32 for char in value), "Unsafe download URL")
    parsed = urllib.parse.urlsplit(value)
    require(parsed.scheme == "https" and parsed.hostname in NETWORK_HOSTS and
            parsed.port in (None, 443) and not parsed.username and not parsed.password and
            not parsed.fragment, "Downloads and redirects require an allowed HTTPS GitHub host")
    return value


class GithubHttpsRedirects(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, response, code, message, headers, new_url):
        network_url(new_url)
        return super().redirect_request(request, response, code, message, headers, new_url)


def download(url, checksum, destination, limit, opener=None):
    network_url(url)
    require(isinstance(checksum, str) and re.fullmatch(r"[0-9a-f]{64}", checksum), "Missing trusted download checksum")
    require(not destination.exists() and not destination.is_symlink(), "Download destination already exists")
    partial = destination.with_name(destination.name + ".partial")
    opener = opener or urllib.request.build_opener(GithubHttpsRedirects())
    request = urllib.request.Request(url, headers={"User-Agent": "Ascendance-reviewed-installer/1"})
    digest, count = hashlib.sha256(), 0
    try:
        with opener.open(request, timeout=120) as response, partial.open("xb") as output:
            for block in iter(lambda: response.read(1024 * 1024), b""):
                count += len(block)
                require(count <= limit, "Download exceeds the reviewed size limit")
                digest.update(block)
                output.write(block)
        require(digest.hexdigest() == checksum, "Download checksum mismatch; nothing was executed")
        partial.rename(destination)
    finally:
        if partial.is_file() and not partial.is_symlink():
            partial.unlink()


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, "Duplicate descriptor key")
        result[key] = value
    return result


def descriptor(path):
    data = json.loads(path.read_text(), object_pairs_hook=unique_object)
    require(isinstance(data, dict) and type(data.get("format")) is int and data["format"] == 1,
            "Unsupported release descriptor")
    require(data.get("release") == RELEASE and data.get("sourceRevision") == SOURCE_REVISION and
            data.get("endpoint") == "159.65.100.206:7777", "Release identity differs from the reviewed server release")
    require(isinstance(data.get("helpers"), dict) and set(data["helpers"]) == set(HELPER_PATHS), "Unexpected release helpers")
    entries = []
    for name, relative in HELPER_PATHS.items():
        entries.append((name, data["helpers"][name], SOURCE_BASE + relative, 2 * 1024 * 1024))
    entries.extend([
        ("server.zip", data["server"]["archive"], RELEASE_BASE + "ascendance-linux-server-" + RELEASE + ".zip", 256 * 1024 * 1024),
        ("server-manifest.json", data["server"]["manifest"], RELEASE_BASE + "linux-server-manifest.json", 4 * 1024 * 1024),
    ])
    for _, entry, expected_url, _ in entries:
        require(isinstance(entry, dict) and entry.get("url") == expected_url and
                isinstance(entry.get("sha256"), str) and re.fullmatch(r"[0-9a-f]{64}", entry["sha256"]),
                "A release download differs from its pinned source/repository")
    return entries


def prepare_and_run(apply=False):
    require(sys.platform.startswith("linux") and os.geteuid() == 0, "Run this installer in the owner's Linux root SSH session")
    os.umask(0o077)
    temporary_root = Path("/var/tmp")
    require(temporary_root.is_dir() and not temporary_root.is_symlink(), "Expected the regular /var/tmp directory")
    root = Path(tempfile.mkdtemp(prefix="ascendance-reviewed-install-", dir=temporary_root))
    os.chmod(root, 0o700)
    progress("keeping verified downloads privately at " + str(root))
    progress("verifying the fixed public release descriptor")
    descriptor_path = root / "release.json"
    download(DESCRIPTOR_URL, DESCRIPTOR_SHA256, descriptor_path, 64 * 1024)
    for name, entry, _, limit in descriptor(descriptor_path):
        progress("downloading and verifying " + name)
        download(entry["url"], entry["sha256"], root / name, limit)
    command = [sys.executable, str(root / "upgrade-existing.py"), "--descriptor", str(descriptor_path),
               "--archive", str(root / "server.zip"), "--manifest", str(root / "server-manifest.json")]
    if apply:
        command.append("--apply")
    progress("starting the reviewed upgrader with isolated canary, backup and recovery checks" if apply else
             "staging only; production activation was not requested")
    # The trusted upgrader emits its public result JSON directly to stdout.
    # No shell, SSH stdin pipe, account JOIN or private invite transfer is used.
    return subprocess.run(command, check=False, stdin=subprocess.DEVNULL).returncode


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apply", action="store_true", help="Activate only after the existing upgrader's required checks pass")
    args = parser.parse_args(argv)
    try:
        return prepare_and_run(args.apply)
    except Exception as error:
        message = str(error) if isinstance(error, Refusal) else "Bootstrap preparation failed; no successful activation is assumed."
        print(json.dumps({"status": "REFUSED", "error": type(error).__name__, "message": message}))
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
