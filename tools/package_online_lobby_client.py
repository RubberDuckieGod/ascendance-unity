#!/usr/bin/env python3
"""Package a reviewed Windows lobby client for the existing h server; never deploy."""
from __future__ import annotations

import argparse
import importlib.util
import json
from pathlib import Path
import re
import tempfile
import zipfile


ROOT = Path(__file__).resolve().parents[1]
_spec = importlib.util.spec_from_file_location("ascendance_public_packaging", ROOT / "tools/package_multiplayer_release.py")
support = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(support)

README = """Ascendance 0.10.2h — online Hero selection client

1. Extract the entire Windows folder. Keep Ascendance.exe, Ascendance_Data,
   UnityPlayer.dll and both Play-AscendanceMultiplayer launchers together.
   Choose a folder whose name does not contain square brackets.
2. Obtain Ascendance.invite.json privately from the owner and put it beside
   Play-AscendanceMultiplayer.cmd. The public archive contains no invitation.
3. Run Play-AscendanceMultiplayer.cmd and enter your own player name.
4. Choose JOIN ONLINE CAMPAIGN. Choose one available Hero from the server's
   existing party, or reconnect to your own reserved Hero using this PC's data.
   Choose JOIN CAMPAIGN to confirm that Hero before entering the campaign.
   Each player controls one different Hero; AVAILABLE, PLAYING and RESERVED
   labels display the server's claims. A reconnect is verified by the server.

This is a client-only update for the existing secure 0.10.2h server. It does not
deploy or reset a server, replace a campaign, or create a new party. Keep the
same private invitation and local player data to retain reconnect credentials.
Share invitations only privately with trusted players, never on public GitHub.
"""


def package(args):
    require = support.require
    tools = support.release_tools()
    require(bool(re.fullmatch(r"[0-9a-f]{40}", args.source_revision)), "Source must be an exact lowercase 40-character revision.")
    require(bool(re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._-]{0,63}", args.release)), "Invalid release identifier.")
    windows = tools.strict_path(str(args.windows_build.absolute()))
    output = tools.strict_path(str(args.output.absolute()))
    require(output.parent.is_dir() and not output.exists(), "Output requires an existing parent and absent destination.")
    require(output != windows and output not in windows.parents and windows not in output.parents,
            "Output must not overlap the Windows build.")
    files = support.snapshot(windows, tools)
    require(len({name.casefold() for name in files}) == len(files), "Windows paths collide by case.")
    require(all(name in files for name in ("Ascendance.exe", "UnityPlayer.dll")) and
            any(name.startswith("Ascendance_Data/") for name in files), "Windows player executable, library or data is missing.")
    require(sum(name.casefold().endswith("ascendance.exe") for name in files) == 1,
            "The client needs exactly one Ascendance.exe.")
    for name in ("Ascendance.exe", "UnityPlayer.dll"):
        with tools.file_from_tree(windows, name) as stream:
            require(stream.read(2) == b"MZ", "Windows executable/library is not a PE file.")
    proof = support.provenance(windows, "Ascendance.provenance.json", "StandaloneWindows64", args.source_revision, tools)
    document = tools.read_json(windows / "Ascendance.provenance.json")
    require(type(document.get("schemaVersion")) is int and document["schemaVersion"] == 1 and
            document.get("source", {}).get("vcs") == "git", "Windows provenance must use schema 1 and Git source identity.")
    extras = {name: tools.regular_bytes(ROOT / "tools" / name)
              for name in ("Play-AscendanceMultiplayer.ps1", "Play-AscendanceMultiplayer.cmd")}
    extras["CONNECTION_README.txt"] = README.encode("utf-8")
    require(not {name.casefold() for name in files}.intersection(name.casefold() for name in extras),
            "Build already contains package-owned launchers or instructions.")
    for name, content in extras.items():
        support.check_public_file(name, content)
    payload_files = {**files, **{name: support.digest(content) for name, content in extras.items()}}
    archive_name = "ascendance-windows-" + args.release + ".zip"
    with tempfile.TemporaryDirectory(prefix=".ascendance-lobby-client-", dir=output.parent) as temporary:
        stage = Path(temporary) / "package"
        stage.mkdir()
        with zipfile.ZipFile(stage / archive_name, "x", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
            for relative, expected in sorted(files.items()):
                with tools.file_from_tree(windows, relative) as stream:
                    content = stream.read()
                require(support.digest(content) == expected, "Build changed while packaging.")
                support.add_zip(archive, "Windows/" + relative, content)
            for relative, content in sorted(extras.items()):
                support.add_zip(archive, "Windows/" + relative, content)
        require(support.snapshot(windows, tools) == files, "Build file set changed during packaging.")
        manifest = support.canonical({"format": 1, "files": payload_files})
        (stage / "windows-client-manifest.json").write_bytes(manifest)
        proof["fileManifestSha256"] = support.digest(manifest)
        (stage / "RuntimeSourceRevision.json").write_bytes(support.canonical({
            "format": 1, "RuntimeSourceRevision": args.source_revision,
            "release": args.release, "clientOnly": True, "builds": {"windows": proof}}))
        (stage / "CONNECTION_README.txt").write_bytes(README.encode("utf-8"))
        descriptor = {
            "format": 1, "release": args.release, "sourceRevision": args.source_revision,
            "buildVersion": "0.10.2h", "endpoint": "159.65.100.206:7777", "clientOnly": True,
            "compatibleServer": {"buildVersion": "0.10.2h", "protocolVersion": 1,
                                 "sourceRevision": "55d25c03a3279a8396d5b8954e73180e08f7a0c5",
                                 "deploymentRequired": False},
            "client": {"archive": {
                "url": "https://github.com/RubberDuckieGod/ascendance-unity/releases/download/" + args.release + "/" + archive_name,
                "sha256": support.digest((stage / archive_name).read_bytes()),
                "sizeBytes": (stage / archive_name).stat().st_size}}}
        (stage / "release.json").write_bytes(support.canonical(descriptor))
        contents = {path.name: support.digest(path.read_bytes()) for path in sorted(stage.iterdir())}
        (stage / "release-package-manifest.json").write_bytes(support.canonical({
            "format": 1, "sourceRevision": args.source_revision, "clientOnly": True, "files": contents}))
        contents["release-package-manifest.json"] = support.digest((stage / "release-package-manifest.json").read_bytes())
        (stage / "SHA256SUMS").write_text("".join(checksum + "  " + name + "\n" for name, checksum in sorted(contents.items())))
        stage.rename(output)
    return {"output": str(output), "sourceRevision": args.source_revision, "clientOnly": True,
            "windowsFiles": len(payload_files), "artifacts": contents}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--windows-build", type=Path, required=True)
    parser.add_argument("--source-revision", required=True)
    parser.add_argument("--release", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        result = package(args)
    except Exception as error:
        parser.exit(1, "Client packaging refused: " + str(error) + "\n")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
