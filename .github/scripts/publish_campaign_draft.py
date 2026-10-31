#!/usr/bin/env python3
"""Verify frozen full client/server artifacts, then optionally upload one draft.

The workflow pins the inventory's SHA-256. This script never publishes a draft,
changes a server, resets a campaign, or reads a private invite.
"""
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
REPORTS = "full-campaign-validation-reports.zip"
HELPERS = {
    "upgrade-existing.py": "Deploy/DigitalOcean/upgrade-existing.py",
    "inspect-existing.py": "Deploy/DigitalOcean/inspect-existing.py",
    "release.py": "Deploy/DigitalOcean/release.py",
    "native_server_acceptance.py": "tools/native_server_acceptance.py",
    "diagnose-existing.py": "Deploy/DigitalOcean/diagnose-existing.py",
    "save-schema-0102g.json": "Deploy/DigitalOcean/save-schema-0102g.json",
}
OLD_H_DLL = "ee6b1db4ed7f78d72dcbccb436d5510d39a18f582b10cb72a059ed3411916ec2"


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
    result, folded = {}, set()
    with zipfile.ZipFile(path) as archive:
        entries = archive.infolist()
        require(0 < len(entries) <= 5000 and sum(item.file_size for item in entries) <= 512 * 1024 * 1024,
                "ZIP inventory or expanded size exceeds the reviewed boundary")
        for item in entries:
            name, pure = item.filename, PurePosixPath(item.filename)
            mode = (item.external_attr >> 16) & 0o170000
            require(not item.is_dir() and mode in (0, stat.S_IFREG) and not item.flag_bits & 1,
                    "ZIP contains a directory, link, special or encrypted file")
            require(not pure.is_absolute() and pure.as_posix() == name and ".." not in pure.parts
                    and "\\" not in name and ":" not in name and name not in result and name.casefold() not in folded,
                    "ZIP contains an unsafe or duplicate path")
            content = archive.read(item)
            public.check_public_file(name, content)
            result[name] = content
            folded.add(name.casefold())
    return result


def checks_passed(report, expected, label):
    checks = report.get("checks", [])
    require(type(expected) is int and expected > 0 and report.get("passed") == expected
            and report.get("failed") == 0 and len(checks) == expected
            and all(check.get("passed") is True and check.get("expected") == check.get("actual") for check in checks),
            label + " does not contain the complete reviewed successful semantic checks")


def verify_reports(reports, inventory, revision, windows_proof, upgrade_helper_sha):
    proofs = inventory.get("reports", {})
    require(proofs and set(reports) == set(proofs), "Public reports differ from the exact reviewed allowlist")
    for name, data in reports.items():
        require(name.endswith((".json", ".png")) and len(data) == proofs[name].get("bytes")
                and digest(data) == proofs[name].get("sha256"), "Unreviewed report or screenshot bytes")
        if name.endswith(".json"):
            document(data)
        else:
            require(data.startswith(b"\x89PNG\r\n\x1a\n"), "Public screenshot is not PNG")
    expected = inventory.get("acceptance", {})
    summary = document(reports["acceptance-summary.json"])
    require(summary.get("format") == "ascendance-full-roster-campaign-acceptance-1"
            and summary.get("runtimeSourceRevision") == revision and summary.get("runtimeBuildVersion") == "0.10.2h"
            and summary.get("serverDeploymentRequired") is True and summary.get("privateDataPublished") is False,
            "Acceptance summary runtime or deployment scope differs")
    require(summary.get("builds") == {"windowsShipping": "SUCCEEDED", "linuxDevelopment": "SUCCEEDED",
                                      "linuxServer": "SUCCEEDED", "cleanSource": True,
                                      "windowsDevelopmentDriversPresent": False}, "Clean matching builds are not proven")
    editor = document(reports["editor-gates.json"])
    gates = expected.get("editorGates", {})
    require(gates and editor.get("passed") is True and editor.get("errorCount") == 0 and editor.get("errors") == []
            and editor.get("buildVersion") == "0.10.2h" and editor.get("editorVersion") == "6000.6.5f1"
            and len(editor.get("gates", [])) == len(gates)
            and {gate.get("name") for gate in editor["gates"]} == set(gates)
            and all(gate.get("passed") is True and gate.get("expectedChecks") == gates[gate["name"]]
                    and gate.get("observedChecks") == gates[gate["name"]] for gate in editor["gates"]),
            "Editor gates are incomplete")
    require(summary.get("pythonHelperFixtures") == {"passed": expected.get("helperChecks"), "failed": 0, "skipped": 0}
            and type(expected.get("helperChecks")) is int and expected["helperChecks"] > 0,
            "Python helper fixture acceptance is incomplete")
    graphical = document(reports["native-full-campaign-tls.json"])
    checks_passed(graphical, expected.get("graphicalChecks"), "Actual graphical campaign acceptance")
    require(graphical.get("kind") == "actual-native-full-roster-online-campaign"
            and graphical.get("transport") == "pinned-TLS-and-room-key"
            and summary.get("screenshotsVisuallyReviewed") is True
            and type(summary.get("framesAdvancedBeforeCapture")) is int and summary["framesAdvancedBeforeCapture"] >= 15,
            "Actual graphical TLS and visual review were not proven")
    migration = document(reports["native-h-campaign-restore.json"])
    checks_passed(migration, expected.get("migrationChecks"), "Original-h copied campaign restore")
    require(migration.get("originalManagedAssemblySha256") == OLD_H_DLL
            and migration.get("sourceBuildVersion") == "0.10.2h" and migration.get("candidateSourceRevision") == revision
            and migration.get("gameplayCommandsSubmittedToCopiedRoom") == 0,
            "Copied campaign restoration is not from the exact deployed original h build")
    require(reports["windows-build-provenance.json"] == windows_proof,
            "Published Windows build proof differs from the shipped player")
    linux = document(reports["linux-development-build-provenance.json"])
    require(linux.get("schemaVersion") == 1 and linux.get("outcome") == "success" and linux.get("exitCode") == 0
            and linux.get("source") == {"vcs": "git", "revision": revision, "dirty": False}
            and linux.get("build", {}).get("target") == "StandaloneLinux64",
            "Graphical Linux acceptance player has wrong source or target")
    if inventory['release'].endswith('-r2'):
        require(expected.get('livePreflightChecks') == 17 and expected.get('directRestoreChecks') == 8,
                'Revised helper acceptance counts differ')
        live = document(reports['native-original-h-live-preflight.json'])
        checks_passed(live, 17, 'Actual original-h live preflight')
        require(live.get('kind') == 'actual-original-h-reviewed-live-preflight'
                and live.get('status') == 'PASS' and live.get('sourceAssemblySha256') == OLD_H_DLL
                and live.get('upgradeHelperSha256') == upgrade_helper_sha
                and live.get('noProductionConnection') is True and live.get('noExistingSaveRead') is True,
                'Revised helper proof does not bind this exact upgrader and isolated original h')
        direct = document(reports['native-original-h-direct-restoration.json'])
        checks_passed(direct, 8, 'Actual never-restarted original-h direct restore')
        require(direct.get('kind') == 'actual-never-restarted-source-h-direct-restore'
                and direct.get('status') == 'PASS' and direct.get('noProductionConnection') is True
                and direct.get('noExistingSaveRead') is True and direct.get('gameplayCommandsSubmittedToCopiedRoom') == 0,
                'Direct original-h restoration proof differs')


def verify():
    trusted_sha = os.environ.get("REVIEWED_INVENTORY_SHA256", "")
    require(re.fullmatch(r"[0-9a-f]{64}", trusted_sha), "The parent has not frozen the upload inventory")
    path = ROOT / "distribution/reviewed-campaign-upload.json"
    require(path.is_file() and not path.is_symlink(), "Missing regular reviewed upload inventory")
    raw = path.read_bytes()
    require(digest(raw) == trusted_sha, "Reviewed inventory checksum mismatch")
    inventory = document(raw)
    tag, revision, tooling = [inventory.get(key, "") for key in ["release", "sourceRevision", "deploymentRevision"]]
    require(inventory.get("format") == 1 and inventory.get("clientOnly") is False
            and re.fullmatch(r"unity-0\.10\.2h-online-campaigns-[0-9a-f]{7}(?:-r2)?", tag)
            and re.fullmatch(r"[0-9a-f]{40}", revision) and re.fullmatch(r"[0-9a-f]{40}", tooling)
            and tag in {"unity-0.10.2h-online-campaigns-" + revision[:7],
                        "unity-0.10.2h-online-campaigns-" + revision[:7] + "-r2"}, "Reviewed full release identity differs")
    release_id = inventory.get("releaseId")
    require(type(release_id) is int and release_id > 0, "Missing exact reviewed new draft ID")
    linux_name, windows_name = "ascendance-linux-server-" + tag + ".zip", "ascendance-windows-" + tag + ".zip"
    expected = {linux_name, windows_name, REPORTS, "CONNECTION_README.txt", "RuntimeSourceRevision.json", "SHA256SUMS",
                "linux-server-manifest.json", "windows-client-manifest.json", "release-package-manifest.json", "release.json"}
    require(set(inventory.get("assets", {})) == expected, "Reviewed inventory must contain exactly ten full-release assets")
    assets = ROOT / "distribution" / tag
    require(assets.is_dir() and not any(part.is_symlink() for part in [assets, *assets.parents])
            and {item.name for item in assets.iterdir()} == expected, "Frozen distribution file inventory changed")
    payload = {}
    public = support_module()
    for name in sorted(expected):
        file, proof = assets / name, inventory["assets"][name]
        require(file.is_file() and not file.is_symlink(), "Distribution asset must be a regular file")
        data = file.read_bytes()
        require(type(proof.get("bytes")) is int and 0 < proof["bytes"] < 95 * 1024 * 1024
                and len(data) == proof["bytes"] and digest(data) == proof.get("sha256"), "Frozen hash or size mismatch: " + name)
        public.check_public_file(name, data)
        payload[name] = data
    sums = {}
    for line in payload["SHA256SUMS"].decode().splitlines():
        match = re.fullmatch(r"([0-9a-f]{64})  ([A-Za-z0-9][A-Za-z0-9._-]{0,127})", line)
        require(match is not None and match[2] not in sums, "Noncanonical or duplicate checksum line")
        sums[match[2]] = match[1]
    require(set(sums) == expected - {"SHA256SUMS"} and all(digest(payload[n]) == sha for n, sha in sums.items()),
            "SHA256SUMS does not cover exactly the reviewed nine payloads")
    descriptor = document(payload["release.json"])
    require(descriptor.get("format") == 1 and descriptor.get("release") == tag
            and descriptor.get("sourceRevision") == revision and descriptor.get("deploymentRevision") == tooling
            and descriptor.get("buildVersion") == "0.10.2h" and descriptor.get("endpoint") == "159.65.100.206:7777"
            and descriptor.get("clientOnly", False) is False and set(descriptor.get("helpers", {})) == set(HELPERS),
            "Full server/client descriptor identity differs")
    base = "https://github.com/" + REPOSITORY + "/releases/download/" + tag + "/"
    for entry, name in [(descriptor["server"]["archive"], linux_name), (descriptor["server"]["manifest"], "linux-server-manifest.json"),
                        (descriptor["client"]["archive"], windows_name)]:
        require(entry.get("url") == base + name and entry.get("sha256") == digest(payload[name])
                and entry.get("sizeBytes") == len(payload[name]), "Archive/manifest descriptor differs from reviewed bytes")
    for name, relative in HELPERS.items():
        entry = descriptor["helpers"][name]
        blob = subprocess.check_output(["git", "show", tooling + ":" + relative], cwd=ROOT)
        require(entry.get("url") == "https://raw.githubusercontent.com/" + REPOSITORY + "/" + tooling + "/" + relative
                and entry.get("sha256") == digest(blob) and (ROOT / relative).read_bytes() == blob,
                "Descriptor or checked-out helper differs from its reviewed exact Git blob")
    package = document(payload["release-package-manifest.json"])
    packaged = expected - {REPORTS, "release.json", "SHA256SUMS", "release-package-manifest.json"}
    require(package.get("format") == 1 and package.get("sourceRevision") == revision
            and set(package.get("files", {})) == packaged
            and all(package["files"][name] == digest(payload[name]) for name in packaged), "Package manifest differs from packaged payloads")
    runtime = document(payload["RuntimeSourceRevision.json"])
    require(runtime.get("format") == 1 and runtime.get("release") == tag and runtime.get("RuntimeSourceRevision") == revision
            and set(runtime.get("builds", {})) == {"linux", "windows"}, "Runtime provenance identity differs")
    contents = {}
    for platform, name, prefix, manifest_name, proof_name, target in [
        ("linux", linux_name, "LinuxServer/", "linux-server-manifest.json", "AscendanceServer.provenance.json", "StandaloneLinux64"),
        ("windows", windows_name, "Windows/", "windows-client-manifest.json", "Ascendance.provenance.json", "StandaloneWindows64"),
    ]:
        files = zip_files(assets / name, public)
        manifest = document(payload[manifest_name])
        expected_files = {prefix + n: checksum for n, checksum in manifest.get("files", {}).items()}
        require(manifest.get("format") == 1 and set(files) == set(expected_files)
                and all(digest(files[n]) == checksum for n, checksum in expected_files.items()), "Complete archive file manifest differs")
        proof = document(files.get(prefix + proof_name, b"{}"))
        require(proof.get("outcome") == "success" and proof.get("exitCode") == 0
                and proof.get("build", {}).get("target") == target
                and proof.get("source", {}).get("revision") == revision and proof["source"].get("dirty") is False,
                "Archive is not built from the reviewed clean runtime")
        runtime_proof = runtime["builds"][platform]
        require(runtime_proof.get("sha256") == digest(files[prefix + proof_name])
                and runtime_proof.get("sourceRevision") == revision and runtime_proof.get("sourceDirty") is False
                and runtime_proof.get("fileManifestSha256") == digest(payload[manifest_name]), "Runtime proof differs from archive")
        contents[platform] = files
    windows = contents["windows"]
    require(windows.get("Windows/Ascendance.exe", b"").startswith(b"MZ")
            and windows.get("Windows/UnityPlayer.dll", b"").startswith(b"MZ")
            and any(n.startswith("Windows/Ascendance_Data/") for n in windows), "Complete Windows player is missing")
    require(digest(windows.get("Windows/Play-AscendanceMultiplayer.ps1", b"")) == inventory.get("launcherSha256")
            and windows.get("Windows/CONNECTION_README.txt") == payload["CONNECTION_README.txt"], "Reviewed launcher/instructions differ")
    verify_reports(zip_files(assets / REPORTS, public), inventory, revision, windows["Windows/Ascendance.provenance.json"],
                   descriptor['helpers']['upgrade-existing.py']['sha256'])
    print(json.dumps({"status": "VERIFIED_FROZEN_FULL_RELEASE", "release": tag, "sourceRevision": revision,
                      "releaseId": release_id, "assets": len(expected), "published": False, "deployed": False}))
    return inventory, assets


def upload(inventory, assets):
    release_id, tag, revision = inventory["releaseId"], inventory["release"], inventory["sourceRevision"]
    expected = set(inventory["assets"])
    def target():
        release = document(subprocess.check_output(["gh", "api", "repos/" + REPOSITORY + "/releases/" + str(release_id)], text=True))
        require(release.get("id") == release_id and release.get("tag_name") == tag and release.get("target_commitish") == revision
                and release.get("draft") is True and release.get("prerelease") is True, "Target is not the exact reviewed new draft")
        for asset in release.get("assets", []):
            name = asset.get("name")
            require(name in expected and asset.get("size") == inventory["assets"][name]["bytes"]
                    and asset.get("digest") == "sha256:" + inventory["assets"][name]["sha256"], "Existing draft asset differs")
        return release
    existing = target()
    ref = document(subprocess.check_output(["gh", "api", "repos/" + REPOSITORY + "/git/ref/tags/" + tag], text=True))
    require(ref.get("ref") == "refs/tags/" + tag and ref.get("object", {}).get("type") == "commit"
            and ref["object"].get("sha") == revision, "The exact release tag does not point directly at the reviewed runtime")
    missing = sorted(expected - {asset["name"] for asset in existing.get("assets", [])})
    if missing:
        subprocess.run(["gh", "release", "upload", tag, "--repo", REPOSITORY, *[str(assets / name) for name in missing]], check=True)
    complete = target()
    require(len(complete.get("assets", [])) == len(expected) and {a["name"] for a in complete["assets"]} == expected,
            "Uploaded draft inventory differs")
    print(json.dumps({"status": "UPLOADED_REVIEWED_DRAFT", "releaseId": release_id,
                      "tag": tag, "draft": True, "published": False, "deployed": False}))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--upload", action="store_true")
    parser.add_argument("--prepare-native", type=Path)
    args = parser.parse_args()
    require(not (args.upload and args.prepare_native), "Choose native preparation or upload")
    inventory, assets = verify()
    if args.prepare_native:
        destination = args.prepare_native.absolute()
        require(not destination.exists(), "Native staging destination must be absent")
        destination.mkdir(mode=0o700)
        archive = assets / ("ascendance-linux-server-" + inventory["release"] + ".zip")
        support_module().extract_validated_server(archive, assets / "linux-server-manifest.json", destination / "build", inventory["sourceRevision"])
        (destination / "linux-server-manifest.json").write_bytes((assets / "linux-server-manifest.json").read_bytes())
        (destination / "source-revision.txt").write_text(inventory["sourceRevision"])
    if args.upload:
        upload(inventory, assets)


if __name__ == "__main__":
    main()
