#!/usr/bin/env python3
"""Verify frozen public client artifacts; optionally upload to one reviewed draft."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path, PurePosixPath
import re
import stat
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parents[2]
REPOSITORY = "RubberDuckieGod/ascendance-unity"
TAG = "unity-0.10.2h-online-lobby-8c143b7"
REVISION = "8c143b7e6a7b5ec3c151b7692dbc7ed57cf48860"
SERVER_REVISION = "55d25c03a3279a8396d5b8954e73180e08f7a0c5"
LAUNCHER_SHA256 = "f0fcafc81b03a9b879011c8e8307c8822387d78764e4dd141abbc8aa55b37ca6"
ARCHIVE = "ascendance-windows-" + TAG + ".zip"
REPORTS = "online-lobby-validation-reports.zip"
EXPECTED = {ARCHIVE, REPORTS, "CONNECTION_README.txt", "RuntimeSourceRevision.json", "SHA256SUMS",
            "windows-client-manifest.json", "release-package-manifest.json", "release.json"}
MAX_FILE_BYTES = 95 * 1024 * 1024


def require(condition, message):
    if not condition:
        raise SystemExit(message)


def digest(data):
    return hashlib.sha256(data).hexdigest()


def unique(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, "Duplicate JSON field is refused")
        result[key] = value
    return result


def document(data):
    return json.loads(data, object_pairs_hook=unique)


def support_module():
    spec = importlib.util.spec_from_file_location("ascendance_public_packaging", ROOT / "tools/package_multiplayer_release.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def zip_files(path, public):
    result = {}
    with zipfile.ZipFile(path) as archive:
        infos = archive.infolist()
        require(len(infos) <= 5000 and sum(item.file_size for item in infos) <= 512 * 1024 * 1024,
                "ZIP inventory or expanded size exceeds the reviewed client boundary")
        folded = set()
        for item in infos:
            name = item.filename
            pure = PurePosixPath(name)
            mode = (item.external_attr >> 16) & 0o170000
            require(not item.is_dir() and mode in (0, stat.S_IFREG) and not item.flag_bits & 1,
                    "ZIP contains a directory, link, special or encrypted file")
            require(not pure.is_absolute() and ".." not in pure.parts and "\\" not in name and ":" not in name
                    and name not in result and name.casefold() not in folded, "ZIP contains an unsafe or duplicate path")
            content = archive.read(item)
            public.check_public_file(name, content)
            result[name] = content
            folded.add(name.casefold())
    return result


def verify():
    trusted_sha = os.environ.get("REVIEWED_INVENTORY_SHA256", "")
    require(re.fullmatch(r"[0-9a-f]{64}", trusted_sha), "The parent has not frozen the upload inventory")
    inventory_path = ROOT / "distribution" / "reviewed-online-lobby-upload.json"
    require(inventory_path.is_file() and not inventory_path.is_symlink(), "Missing regular reviewed upload inventory")
    inventory_bytes = inventory_path.read_bytes()
    require(digest(inventory_bytes) == trusted_sha, "Reviewed inventory checksum mismatch")
    inventory = document(inventory_bytes)
    require(inventory.get("format") == 1 and inventory.get("release") == TAG
            and inventory.get("sourceRevision") == REVISION and inventory.get("clientOnly") is True,
            "Reviewed inventory identity changed")
    release_id = inventory.get("releaseId")
    require(type(release_id) is int and release_id > 0, "Missing the exact reviewed new draft ID")
    require(set(inventory.get("assets", {})) == EXPECTED, "Reviewed upload inventory must contain exactly eight assets")
    assets = ROOT / "distribution" / TAG
    require(assets.is_dir() and not any(part.is_symlink() for part in [assets, *assets.parents]), "Distribution directory must be regular")
    require({item.name for item in assets.iterdir()} == EXPECTED, "Frozen distribution file inventory changed")
    payload = {}
    for name in sorted(EXPECTED):
        path = assets / name
        require(path.is_file() and not path.is_symlink(), "Distribution asset must be a regular file")
        data = path.read_bytes()
        proof = inventory["assets"][name]
        require(type(proof.get("bytes")) is int and 0 < proof["bytes"] <= MAX_FILE_BYTES
                and len(data) == proof["bytes"] and digest(data) == proof.get("sha256"), "Frozen asset hash or size mismatch: " + name)
        payload[name] = data
    sums = {}
    for line in payload["SHA256SUMS"].decode("utf-8").splitlines():
        match = re.fullmatch(r"([0-9a-f]{64})  ([A-Za-z0-9][A-Za-z0-9._-]{0,127})", line)
        require(match is not None and match[2] not in sums, "Noncanonical or duplicate checksum line")
        sums[match[2]] = match[1]
    require(set(sums) == EXPECTED - {"SHA256SUMS"}, "SHA256SUMS must cover exactly seven payloads")
    require(all(digest(payload[name]) == checksum for name, checksum in sums.items()), "Payload checksum mismatch")
    public = support_module()
    for name, data in payload.items():
        public.check_public_file(name, data)
    descriptor = document(payload["release.json"])
    require(descriptor.get("format") == 1 and descriptor.get("clientOnly") is True
            and descriptor.get("release") == TAG and descriptor.get("sourceRevision") == REVISION
            and descriptor.get("buildVersion") == "0.10.2h" and descriptor.get("endpoint") == "159.65.100.206:7777"
            and "server" not in descriptor and "helpers" not in descriptor,
            "Client-only descriptor identity changed")
    compatible = descriptor.get("compatibleServer", {})
    require(compatible == {"buildVersion": "0.10.2h", "protocolVersion": 1,
                           "sourceRevision": SERVER_REVISION, "deploymentRequired": False}, "Existing-server compatibility boundary changed")
    archive = descriptor.get("client", {}).get("archive", {})
    require(archive.get("url") == "https://github.com/" + REPOSITORY + "/releases/download/" + TAG + "/" + ARCHIVE
            and archive.get("sha256") == digest(payload[ARCHIVE]) and archive.get("sizeBytes") == len(payload[ARCHIVE]),
            "Client archive descriptor differs from reviewed bytes")
    package_manifest = document(payload["release-package-manifest.json"])
    packaged = EXPECTED - {REPORTS, "SHA256SUMS", "release-package-manifest.json"}
    require(package_manifest.get("format") == 1 and package_manifest.get("clientOnly") is True
            and package_manifest.get("sourceRevision") == REVISION
            and set(package_manifest.get("files", {})) == packaged
            and all(package_manifest["files"][name] == digest(payload[name]) for name in packaged),
            "Original client package manifest differs from frozen package payloads")
    runtime = document(payload["RuntimeSourceRevision.json"])
    require(runtime.get("clientOnly") is True and runtime.get("release") == TAG
            and runtime.get("RuntimeSourceRevision") == REVISION, "Runtime provenance identity changed")
    manifest = document(payload["windows-client-manifest.json"])
    contents = zip_files(assets / ARCHIVE, public)
    require(all(name.startswith("Windows/") for name in contents), "Client ZIP must contain only the complete Windows folder")
    expected_files = {"Windows/" + name: checksum for name, checksum in manifest.get("files", {}).items()}
    require(manifest.get("format") == 1 and set(contents) == set(expected_files)
            and all(digest(contents[name]) == checksum for name, checksum in expected_files.items()),
            "Client archive and complete per-file manifest differ")
    require(contents.get("Windows/Ascendance.exe", b"").startswith(b"MZ")
            and contents.get("Windows/UnityPlayer.dll", b"").startswith(b"MZ")
            and any(name.startswith("Windows/Ascendance_Data/") for name in contents), "Complete Windows player files are missing")
    require(digest(contents.get("Windows/Play-AscendanceMultiplayer.ps1", b"")) == LAUNCHER_SHA256,
            "Reviewed path-safe online launcher changed")
    proof = document(contents.get("Windows/Ascendance.provenance.json", b"{}"))
    require(proof.get("schemaVersion") == 1 and proof.get("outcome") == "success" and proof.get("exitCode") == 0
            and proof.get("build", {}).get("target") == "StandaloneWindows64"
            and proof.get("source", {}).get("vcs") == "git" and proof["source"].get("revision") == REVISION
            and proof["source"].get("dirty") is False, "Windows build is not the reviewed clean runtime")
    reports = zip_files(assets / REPORTS, public)
    report_proofs = inventory.get("reports", {})
    require(report_proofs and set(reports) == set(report_proofs), "Public reports differ from the exact parent-reviewed allowlist")
    for name, data in reports.items():
        require(name.endswith((".json", ".png")) and len(data) == report_proofs[name].get("bytes")
                and digest(data) == report_proofs[name].get("sha256"), "Unreviewed report or screenshot bytes")
        if name.endswith(".json"):
            document(data)
        else:
            require(data.startswith(b"\x89PNG\r\n\x1a\n"), "Public screenshot is not PNG")
    summary = document(reports["acceptance-summary.json"])
    require(summary.get("format") == "ascendance-online-hero-lobby-acceptance-1"
            and summary.get("runtimeSourceRevision") == REVISION and summary.get("runtimeBuildVersion") == "0.10.2h"
            and summary.get("clientOnly") is True and summary.get("serverDeploymentRequired") is False
            and summary.get("compatibleServerSourceRevision") == SERVER_REVISION and summary.get("privateDataPublished") is False,
            "Acceptance summary runtime or deployment scope differs")
    require(summary.get("editor") == {"passed": 207, "failed": 0, "errors": 0, "lobbyChecks": 31}
            and summary.get("pythonHelperFixtures") == {"passed": 196, "failed": 0, "skipped": 0},
            "Incomplete Editor or helper acceptance summary")
    graphical = summary.get("actualGraphicalClients", {})
    require(graphical.get("passed") == 41 and graphical.get("failed") == 0
            and graphical.get("transport") == "pinned-TLS-and-room-key"
            and graphical.get("screenshotsVisuallyReviewed") is True
            and type(graphical.get("framesAdvancedBeforeCapture")) is int and graphical["framesAdvancedBeforeCapture"] >= 15,
            "Actual graphical or visual-review acceptance boundary failed")
    require(summary.get("builds") == {"windowsShipping": "SUCCEEDED", "linuxDevelopment": "SUCCEEDED",
                                       "cleanSource": True, "windowsDevelopmentDriversPresent": False},
            "Acceptance does not certify clean shipping Windows and development Linux builds")
    editor = document(reports["editor-gates.json"])
    expected_gates = {"master": 54, "foundation": 24, "router": 29, "network": 27,
                      "reliability": 22, "snapshot": 20, "lobby": 31}
    require(editor.get("passed") is True and editor.get("errorCount") == 0 and editor.get("errors") == []
            and editor.get("editorVersion") == "6000.6.5f1" and editor.get("buildVersion") == "0.10.2h"
            and len(editor.get("gates", [])) == len(expected_gates)
            and {gate.get("name") for gate in editor["gates"]} == set(expected_gates), "Editor gates are incomplete")
    require(all(gate.get("passed") is True and type(gate.get("observedChecks")) is int
                and gate["observedChecks"] == expected_gates[gate["name"]]
                and gate.get("expectedChecks") == expected_gates[gate["name"]] for gate in editor["gates"]),
            "Editor gate count or pass flag differs")
    native = document(reports["native-two-client-tls.json"])
    require(native.get("kind") == "actual-native-online-hero-lobby" and native.get("transport") == "pinned-TLS-and-room-key"
            and native.get("passed") == 41 and native.get("failed") == 0 and len(native.get("checks", [])) == 41
            and all(check.get("passed") is True and check.get("expected") == check.get("actual") for check in native["checks"]),
            "Actual two-client TLS semantic acceptance failed")
    require(reports["windows-build-provenance.json"] == contents["Windows/Ascendance.provenance.json"],
            "Published Windows build proof differs from the shipped player")
    linux = document(reports["linux-development-build-provenance.json"])
    require(linux.get("schemaVersion") == 1 and linux.get("outcome") == "success" and linux.get("exitCode") == 0
            and linux.get("source") == {"vcs": "git", "revision": REVISION, "dirty": False}
            and linux.get("build", {}).get("target") == "StandaloneLinux64",
            "Graphical Linux acceptance player has wrong source or target")
    print(json.dumps({"status": "VERIFIED_FROZEN_CLIENT_ONLY_ASSETS", "release": TAG,
                      "sourceRevision": REVISION, "releaseId": release_id, "assets": len(EXPECTED),
                      "windowsFiles": len(contents), "publicReports": len(reports), "deployed": False}))
    return inventory, assets


def upload(inventory, assets):
    release_id = inventory["releaseId"]
    def target():
        result = subprocess.check_output(["gh", "api", "repos/" + REPOSITORY + "/releases/" + str(release_id)], text=True)
        release = document(result)
        require(release.get("id") == release_id and release.get("tag_name") == TAG
                and release.get("target_commitish") == REVISION and release.get("draft") is True
                and release.get("prerelease") is True, "Target is not the exact reviewed new prerelease draft")
        for asset in release.get("assets", []):
            name = asset.get("name")
            require(name in EXPECTED and asset.get("size") == inventory["assets"][name]["bytes"]
                    and asset.get("digest") == "sha256:" + inventory["assets"][name]["sha256"],
                    "Existing draft contains an unreviewed or mismatched asset")
        return release
    existing = target()
    existing_names = {asset["name"] for asset in existing.get("assets", [])}
    missing = sorted(EXPECTED - existing_names)
    if missing:
        subprocess.run(["gh", "release", "upload", TAG, "--repo", REPOSITORY,
                        *[str(assets / name) for name in missing]], check=True)
    uploaded = target()
    require(len(uploaded.get("assets", [])) == len(EXPECTED)
            and {asset["name"] for asset in uploaded["assets"]} == EXPECTED, "Uploaded draft inventory differs")
    print(json.dumps({"status": "UPLOADED_REVIEWED_DRAFT", "releaseId": release_id, "tag": TAG,
                      "assetCount": len(EXPECTED), "draft": True, "published": False, "deployed": False}))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--upload", action="store_true", help="Upload to the exact reviewed draft; never publish or deploy")
    args = parser.parse_args()
    reviewed, directory = verify()
    if args.upload:
        upload(reviewed, directory)
