#!/usr/bin/env python3
"""Reconstruct four reviewed test assets and optionally fill one private draft.

Only the dedicated validation branch supplies binary chunks. Verification runs
before any GitHub write. This tool never publishes, deletes, deploys, or opens a
player profile, production save, private invitation or licensed asset intake.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import stat
import subprocess
import tempfile

import package_multiplayer_release as packaging
from native_combat_acceptance import extract_validated_client


ROOT = Path(__file__).resolve().parents[1]
REPOSITORY = "RubberDuckieGod/ascendance-unity"
RELEASE_ID = 430159712
RELEASE_TAG = "combat-validation-2bcf6ef"
SOURCE_REVISION = "2bcf6efe9a20b2692fc9dad10f7b9fb5f6dfa585"
PAYLOAD = "distribution/combat-native-2bcf6ef"
NAMES = {
    "ascendance-linux-server-" + RELEASE_TAG + ".zip": 256 * 1024 * 1024,
    "linux-server-manifest.json": 4 * 1024 * 1024,
    "ascendance-linux-client-" + RELEASE_TAG + ".zip": 512 * 1024 * 1024,
    "linux-client-manifest.json": 4 * 1024 * 1024,
}
MAX_CHUNK = 16 * 1024 * 1024


class Refusal(Exception):
    pass


def require(condition, message):
    if not condition:
        raise Refusal(message)


def unique(pairs):
    value = {}
    for name, item in pairs:
        require(name not in value, "Duplicate reviewed JSON field")
        value[name] = item
    return value


def document(data):
    return json.loads(data, object_pairs_hook=unique)


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def regular(path, maximum):
    path = Path(path)
    require(path.is_absolute() and path.is_relative_to(ROOT) and ".." not in path.parts,
            "Input must be inside the reviewed checkout")
    require(not ROOT.is_symlink() and ROOT.is_dir(), "Checkout root must be a regular directory")
    current = ROOT
    for part in path.relative_to(ROOT).parts:
        current /= part
        require(not current.is_symlink(), "Reviewed input includes a symlink")
    mode = path.lstat().st_mode
    require(stat.S_ISREG(mode) and 0 < path.stat().st_size <= maximum, "Reviewed input is not a bounded regular file")
    return path.read_bytes()


def load_inventory(path, expected_hash):
    require(isinstance(expected_hash, str) and re.fullmatch(r"[0-9a-f]{64}", expected_hash),
            "The parent must freeze the combat upload inventory checksum")
    data = regular(path, 64 * 1024)
    require(sha256(data) == expected_hash, "Combat upload inventory checksum differs")
    value = document(data)
    require(isinstance(value, dict) and set(value) == {"format", "repository", "releaseId", "releaseTag", "sourceRevision", "assets"},
            "Unrecognized reviewed combat upload inventory")
    require(value["format"] == "ascendance-reviewed-combat-upload-1" and value["repository"] == REPOSITORY
            and type(value["releaseId"]) is int and value["releaseId"] == RELEASE_ID
            and value["releaseTag"] == RELEASE_TAG and value["sourceRevision"] == SOURCE_REVISION,
            "Combat target differs from the exact reviewed private draft")
    require(isinstance(value["assets"], dict) and set(value["assets"]) == set(NAMES),
            "Exactly the four reviewed Linux test assets are required")
    referenced = set()
    for name, asset in value["assets"].items():
        require(isinstance(asset, dict) and set(asset) == {"bytes", "sha256", "chunks"}
                and type(asset["bytes"]) is int and 0 < asset["bytes"] <= NAMES[name]
                and isinstance(asset["sha256"], str) and re.fullmatch(r"[0-9a-f]{64}", asset["sha256"])
                and isinstance(asset["chunks"], list) and 0 < len(asset["chunks"]) <= 32,
                "Invalid reviewed asset size, hash or chunks")
        total = 0
        for chunk in asset["chunks"]:
            require(isinstance(chunk, dict) and set(chunk) == {"path", "bytes", "sha256"}, "Invalid reviewed chunk fields")
            relative = chunk["path"]
            require(isinstance(relative, str) and re.fullmatch(re.escape(PAYLOAD) + r"/asset[0-3]-part[0-9]{2}\.bin", relative)
                    and PurePosixPath(relative).as_posix() == relative and relative not in referenced,
                    "Invalid or repeated reviewed chunk path")
            require(type(chunk["bytes"]) is int and 0 < chunk["bytes"] <= MAX_CHUNK
                    and isinstance(chunk["sha256"], str) and re.fullmatch(r"[0-9a-f]{64}", chunk["sha256"]),
                    "Invalid reviewed chunk size or checksum")
            referenced.add(relative); total += chunk["bytes"]
        require(total == asset["bytes"], "Reviewed chunk sizes do not equal the asset size")
    payload = ROOT / PAYLOAD
    require(payload.is_dir() and not payload.is_symlink(), "Missing regular reviewed chunk directory")
    actual = {p.relative_to(ROOT).as_posix() for p in payload.rglob("*") if p.is_file() or p.is_symlink()}
    require(actual == referenced, "Reviewed chunk directory has missing or unreferenced files")
    return value


def validate_builds(output, revision):
    # Both production extraction validators enforce safe archive paths, complete
    # file manifests, per-file hashes, public-file guards and clean provenance.
    with tempfile.TemporaryDirectory(prefix="asc-combat-upload-verify-") as directory:
        work = Path(directory)
        server = packaging.extract_validated_server(output / ("ascendance-linux-server-" + RELEASE_TAG + ".zip"),
            output / "linux-server-manifest.json", work / "server", revision)
        client = extract_validated_client(output / ("ascendance-linux-client-" + RELEASE_TAG + ".zip"),
            output / "linux-client-manifest.json", work / "client", revision)
        proof = document((server / "AscendanceServer.provenance.json").read_bytes())
        require(proof.get("build", {}).get("executeMethod") ==
                "Ascendance.EditorTools.AscendanceDedicatedServerBuilder0100.BuildLinuxDedicatedServer0102",
                "Server was not built by the reviewed dedicated-server builder")
        # Access both return paths so neither extraction can be accidentally skipped.
        require(client.is_dir() and server.is_dir(), "Both native builds require full validation")


def reconstruct(inventory, output):
    output = Path(output).absolute()
    require(output == output.resolve() and output != ROOT and not output.is_relative_to(ROOT) and not output.exists(),
            "Asset reconstruction requires a new directory outside the source checkout")
    current = output
    while current != current.parent:
        require(not current.is_symlink(), "Output path includes a symlink")
        current = current.parent
    output.mkdir(mode=0o700, parents=True)
    for name, asset in inventory["assets"].items():
        digest = hashlib.sha256(); total = 0
        with (output / name).open("xb") as target:
            for chunk in asset["chunks"]:
                data = regular(ROOT / chunk["path"], MAX_CHUNK)
                require(len(data) == chunk["bytes"] and sha256(data) == chunk["sha256"], "Reviewed chunk bytes differ")
                target.write(data); digest.update(data); total += len(data)
        require(total == asset["bytes"] and digest.hexdigest() == asset["sha256"], "Reconstructed reviewed asset differs")
        packaging.check_public_file(name, (output / name).read_bytes() if name.endswith(".json") else b"")
    validate_builds(output, inventory["sourceRevision"])
    return output


def api(path):
    try:
        return document(subprocess.check_output(["gh", "api", path], text=True, stderr=subprocess.PIPE))
    except (subprocess.CalledProcessError, ValueError, OSError) as error:
        raise Refusal("GitHub could not verify the fixed private draft or tag") from error


def draft(inventory):
    value = api("repos/" + REPOSITORY + "/releases/" + str(RELEASE_ID))
    require(isinstance(value, dict) and value.get("id") == RELEASE_ID and value.get("draft") is True
            and value.get("prerelease") is True and value.get("tag_name") == RELEASE_TAG
            and value.get("target_commitish") == SOURCE_REVISION,
            "The exact reviewed target is not the expected private draft")
    assets = value.get("assets")
    require(isinstance(assets, list) and all(isinstance(a, dict) for a in assets), "Malformed private draft assets")
    names = [a.get("name") for a in assets]
    require(len(names) == len(set(names)) and set(names).issubset(NAMES), "Private draft contains unreviewed or duplicate assets")
    for asset in assets:
        expected = inventory["assets"][asset["name"]]
        require(asset.get("state") == "uploaded" and type(asset.get("size")) is int
                and asset["size"] == expected["bytes"] and asset.get("digest") == "sha256:" + expected["sha256"],
                "An existing private draft asset has different bytes; it will not be replaced")
    return value


def upload(inventory, output):
    require(bool(os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN")), "A workflow token is required for private draft upload")
    # Recheck the reconstructed files immediately before any write. A changed
    # output must not inherit the earlier extraction validation result.
    output = Path(output)
    require(output.is_absolute() and output == output.resolve() and output.is_dir()
            and not output.is_symlink() and not output.is_relative_to(ROOT),
            "Upload requires the verified directory outside the checkout")
    require({p.name for p in output.iterdir()} == set(NAMES), "Upload directory has unexpected or missing assets")
    for name, expected in inventory["assets"].items():
        path = output / name
        require(path.is_file() and not path.is_symlink() and path.stat().st_size == expected["bytes"]
                and sha256(path.read_bytes()) == expected["sha256"], "Verified output changed before upload")
    before = draft(inventory)
    ref = api("repos/" + REPOSITORY + "/git/ref/tags/" + RELEASE_TAG)
    require(ref.get("ref") == "refs/tags/" + RELEASE_TAG and ref.get("object", {}).get("type") == "commit"
            and ref["object"].get("sha") == SOURCE_REVISION,
            "The exact private draft tag does not point directly to the reviewed runtime")
    missing = sorted(set(NAMES) - {a["name"] for a in before["assets"]})
    if missing:
        try:
            subprocess.run(["gh", "release", "upload", RELEASE_TAG, "--repo", REPOSITORY,
                            *[str(output / name) for name in missing]], check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        except (subprocess.CalledProcessError, OSError) as error:
            raise Refusal("Reviewed private draft upload did not complete; no success is assumed") from error
    after = draft(inventory)
    require({a["name"] for a in after["assets"]} == set(NAMES), "Private draft upload is incomplete")
    return {"status": "UPLOADED_REVIEWED_PRIVATE_TEST_DRAFT", "releaseId": RELEASE_ID, "releaseTag": RELEASE_TAG,
            "sourceRevision": SOURCE_REVISION, "assets": len(NAMES), "draft": True, "published": False, "deployed": False}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--inventory", type=Path, required=True)
    parser.add_argument("--sha256", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--upload", action="store_true")
    args = parser.parse_args(argv)
    try:
        inventory = load_inventory(args.inventory.absolute(), args.sha256)
        output = reconstruct(inventory, args.output)
        result = upload(inventory, output) if args.upload else {
            "status": "VERIFIED_REVIEWED_NATIVE_COMBAT_ASSETS", "releaseId": RELEASE_ID,
            "sourceRevision": SOURCE_REVISION, "assets": len(NAMES), "published": False, "deployed": False}
    except (Refusal, packaging.Refusal, OSError, ValueError, TypeError) as error:
        parser.exit(1, "Reviewed combat assets refused: " + str(error) + "\n")
    print(json.dumps(result, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
