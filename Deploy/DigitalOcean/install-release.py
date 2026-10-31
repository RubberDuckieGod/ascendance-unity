#!/usr/bin/env python3
"""Fetch the pinned public release on its host and run the reviewed upgrader.

Run from an ordinary interactive root SSH session. Without --apply this only
stages and validates; the reviewed upgrader owns all canaries, maintenance
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


RELEASE = "unity-0.10.2h-online-campaigns-597f442-r2"
BUILD_VERSION = "0.10.2h"
SOURCE_REVISION = "597f44215ec2624ff0f215db80c6c487f05a84d7"
DEPLOYMENT_REVISION = "588afa0855b57382971a88803d28832b54a63d1d"
RELEASE_BASE = "https://github.com/RubberDuckieGod/ascendance-unity/releases/download/" + RELEASE + "/"
SOURCE_BASE = "https://raw.githubusercontent.com/RubberDuckieGod/ascendance-unity/" + DEPLOYMENT_REVISION + "/"
DESCRIPTOR_URL = RELEASE_BASE + "release.json"
DESCRIPTOR_SHA256 = "93bb60afdb608ed6a222e79e137207eff8b64356710041f4f8a772cb621bbf6f"
HELPER_PATHS = {
    "upgrade-existing.py": "Deploy/DigitalOcean/upgrade-existing.py",
    "inspect-existing.py": "Deploy/DigitalOcean/inspect-existing.py",
    "release.py": "Deploy/DigitalOcean/release.py",
    "native_server_acceptance.py": "tools/native_server_acceptance.py",
    "diagnose-existing.py": "Deploy/DigitalOcean/diagnose-existing.py",
    "save-schema-0102g.json": "Deploy/DigitalOcean/save-schema-0102g.json",
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
            data.get("buildVersion") == BUILD_VERSION and
            data.get("deploymentRevision") == DEPLOYMENT_REVISION and
            data.get("endpoint") == "159.65.100.206:7777", "Release identity differs from the reviewed server release")
    require(isinstance(data.get("helpers"), dict) and set(data["helpers"]) == set(HELPER_PATHS), "Unexpected release helpers")
    entries = []
    for name, relative in HELPER_PATHS.items():
        entries.append((name, data["helpers"][name], SOURCE_BASE + relative, 2 * 1024 * 1024))
    entries.extend([
        ("server.zip", data["server"]["archive"], RELEASE_BASE + "ascendance-linux-server-" + RELEASE + ".zip", 256 * 1024 * 1024),
        ("server-manifest.json", data["server"]["manifest"], RELEASE_BASE + "linux-server-manifest.json", 4 * 1024 * 1024),
        ("windows-client.zip", data["client"]["archive"], RELEASE_BASE + "ascendance-windows-" + RELEASE + ".zip", 256 * 1024 * 1024),
    ])
    for _, entry, expected_url, _ in entries:
        require(isinstance(entry, dict) and entry.get("url") == expected_url and
                isinstance(entry.get("sha256"), str) and re.fullmatch(r"[0-9a-f]{64}", entry["sha256"]),
                "A release download differs from its pinned source/repository")
    return entries


def inspect_current(root):
    """Run only the pinned read-only probe; never echo raw subprocess errors."""
    try:
        result = subprocess.run([sys.executable, str(root / "diagnose-existing.py")],
                                check=False, stdin=subprocess.DEVNULL, capture_output=True,
                                text=True, timeout=40)
        report = json.loads(result.stdout)
        require(isinstance(report, dict) and report.get("readOnly") is True and
                report.get("format") == "ascendance-readonly-hello-diagnostic-1",
                "Unexpected read-only diagnostic format")
        return report
    except Exception as error:
        return {"readOnly": True, "diagnosticAvailable": False, "error": type(error).__name__}


def identity_blockers(report):
    # An already secured listener cannot answer the plaintext probe. In that
    # case the upgrader performs its authenticated checks; do not guess.
    response = report.get("hello")
    if not isinstance(response, dict) or response.get("responseReceived") is not True:
        return []
    required = {"accepted": True, "code": "HELLO", "protocolVersion": 1,
                "requestIdMatches": True,
                "snapshotRoomMatchesExpected": True}
    failed = [name for name, expected in required.items()
              if type(response.get(name)) is not type(expected) or response[name] != expected]
    reviewed = report.get("sourceCompatibility")
    if not (isinstance(reviewed, dict) and reviewed.get("managedAssemblyReviewed") is True
            and reviewed.get("reportedBuildMatchesReviewedSource") is True
            and reviewed.get("targetBuildVersion") == BUILD_VERSION
            and reviewed.get("expectedSourceBuildVersion") in ("0.10.2c", "0.10.2g", "0.10.2h")):
        failed.append("reviewedSourceIdentity")
    elif response.get("serverBuildVersion") != reviewed["expectedSourceBuildVersion"]:
        failed.append("serverBuildVersion")
    return failed


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
    entries = descriptor(descriptor_path)
    for name, entry, _, limit in entries:
        if name not in HELPER_PATHS:
            continue
        progress("downloading and verifying " + name)
        download(entry["url"], entry["sha256"], root / name, limit)
    diagnosis = None
    if apply:
        progress("checking the running server identity before downloading the game archive or stopping the service")
        diagnosis = inspect_current(root)
        blockers = identity_blockers(diagnosis)
        if blockers:
            print(json.dumps({"status": "REFUSED", "phase": "read-only-preflight",
                              "productionServiceChanged": False,
                              "message": "Running server identity needs a targeted compatibility fix; activation was not started.",
                              "failedIdentityChecks": blockers, "diagnostic": diagnosis}, indent=2))
            return 1
    for name, entry, _, limit in entries:
        if name in HELPER_PATHS:
            continue
        progress("downloading and verifying " + name)
        download(entry["url"], entry["sha256"], root / name, limit)
    command = [sys.executable, str(root / "upgrade-existing.py"), "--descriptor", str(descriptor_path),
               "--archive", str(root / "server.zip"), "--manifest", str(root / "server-manifest.json")]
    if apply:
        command.append("--apply")
    progress("starting the reviewed upgrader with isolated canary, backup and recovery checks" if apply else
             "staging only; production activation was not requested")
    # The trusted upgrader emits only public JSON. On failure attach the pinned
    # diagnostic automatically, avoiding a second owner command. Raw stderr is
    # never displayed because subprocess errors can contain private values.
    result = subprocess.run(command, check=False, stdin=subprocess.DEVNULL,
                            capture_output=True, text=True)
    try:
        public = json.loads(result.stdout)
        require(isinstance(public, dict), "Unexpected deployment result format")
    except Exception:
        public = {"status": "REFUSED", "message": "Upgrader returned no recognized result; successful activation is not assumed."}
        if diagnosis is None:
            diagnosis = inspect_current(root)
        public["diagnostic"] = diagnosis
        print(json.dumps(public, indent=2))
        return result.returncode or 1
    if result.returncode != 0:
        public["diagnostic"] = diagnosis if diagnosis is not None else inspect_current(root)
    print(json.dumps(public, indent=2))
    return result.returncode


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
