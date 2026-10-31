"""Synthetic upload guards; no Unity execution, GitHub access or deployment."""
import copy
import io
import json
import os
from pathlib import Path
import shutil
import sys
import tempfile
import unittest
from unittest.mock import patch
import zipfile

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))
import upload_reviewed_combat_assets as upload


def canonical(value):
    return (json.dumps(value, sort_keys=True) + "\n").encode()


class ReviewedCombatUploadTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="asc-reviewed-combat-upload-test-")
        self.addCleanup(temporary.cleanup)
        self.base = Path(temporary.name)
        self.root = self.base / "checkout"
        self.root.mkdir()
        root_patch = patch.object(upload, "ROOT", self.root)
        root_patch.start(); self.addCleanup(root_patch.stop)
        api_patch = patch.object(upload, "api", side_effect=AssertionError("Network is forbidden in fixtures"))
        self.api = api_patch.start(); self.addCleanup(api_patch.stop)
        run_patch = patch.object(upload.subprocess, "run", side_effect=AssertionError("Execution is forbidden in fixtures"))
        self.run = run_patch.start(); self.addCleanup(run_patch.stop)
        env_patch = patch.dict(os.environ, {"GH_TOKEN": "synthetic-test-token"})
        env_patch.start(); self.addCleanup(env_patch.stop)
        self.output_number = 0
        self.make_fixture()

    def make_fixture(self, mutate_server=None, mutate_client=None, mutate_manifest=None, extra_server=None):
        """Recompute even the frozen hashes so deeper validator failures matter."""
        elf = b"\x7fELF\x02\x01" + bytes(12) + b"\x3e\x00" + b"never executed synthetic fixture"
        assets = {}
        for kind, mutate, method in [
            ("Server", mutate_server, "Ascendance.EditorTools.AscendanceDedicatedServerBuilder0100.BuildLinuxDedicatedServer0102"),
            ("Client", mutate_client, "Ascendance.EditorTools.NativeClientAcceptanceBuilder0102.BuildLinuxClient"),
        ]:
            proof = {"outcome": "success", "exitCode": 0,
                     "build": {"target": "StandaloneLinux64", "executeMethod": method},
                     "source": {"revision": upload.SOURCE_REVISION, "dirty": False}}
            files = {"Ascendance" + kind + ".x86_64": elf, "UnityPlayer.so": elf,
                     "Ascendance" + kind + "_Data/globalgamemanagers": b"synthetic data; never a save",
                     "Ascendance" + kind + ".provenance.json": canonical(proof)}
            if mutate: mutate(files)
            manifest = {"format": 1, "files": {name: upload.sha256(data) for name, data in files.items()}}
            if mutate_manifest: mutate_manifest(kind, manifest)
            stream = io.BytesIO()
            with zipfile.ZipFile(stream, "w") as archive:
                for name, data in files.items(): upload.packaging.add_zip(archive, "Linux" + kind + "/" + name, data)
                if kind == "Server" and extra_server: upload.packaging.add_zip(archive, *extra_server)
            assets["ascendance-linux-" + kind.lower() + "-" + upload.RELEASE_TAG + ".zip"] = stream.getvalue()
            assets["linux-" + kind.lower() + "-manifest.json"] = canonical(manifest)
        self.bytes = assets
        payload = self.root / upload.PAYLOAD
        if payload.exists(): shutil.rmtree(payload)
        payload.mkdir(parents=True)
        inventory = {"format": "ascendance-reviewed-combat-upload-1", "repository": upload.REPOSITORY,
                     "releaseId": upload.RELEASE_ID, "releaseTag": upload.RELEASE_TAG,
                     "sourceRevision": upload.SOURCE_REVISION, "assets": {}}
        for index, name in enumerate(upload.NAMES):
            data = assets[name]
            relative = upload.PAYLOAD + "/asset" + str(index) + "-part00.bin"
            (self.root / relative).write_bytes(data)
            inventory["assets"][name] = {"bytes": len(data), "sha256": upload.sha256(data),
                "chunks": [{"path": relative, "bytes": len(data), "sha256": upload.sha256(data)}]}
        self.inventory = inventory
        self.write_inventory()

    def write_inventory(self):
        self.inventory_path = self.root / "distribution/reviewed-combat-upload.json"
        data = canonical(self.inventory)
        self.inventory_path.write_bytes(data)
        self.inventory_sha = upload.sha256(data)

    def load(self):
        return upload.load_inventory(self.inventory_path, self.inventory_sha)

    def reconstruct(self):
        self.output_number += 1
        return upload.reconstruct(self.load(), self.base / ("output-" + str(self.output_number)))

    def draft(self, names=None):
        if names is None: names = list(upload.NAMES)
        return {"id": upload.RELEASE_ID, "draft": True, "prerelease": True,
                "tag_name": upload.RELEASE_TAG, "target_commitish": upload.SOURCE_REVISION,
                "assets": [{"name": name, "state": "uploaded", "size": self.inventory["assets"][name]["bytes"],
                            "digest": "sha256:" + self.inventory["assets"][name]["sha256"]} for name in names]}

    def ref(self):
        return {"ref": "refs/tags/" + upload.RELEASE_TAG,
                "object": {"type": "commit", "sha": upload.SOURCE_REVISION}}

    def test_valid_complete_builds_reconstruct_without_network_or_execution(self):
        output = self.reconstruct()
        self.assertEqual({path.name: path.read_bytes() for path in output.iterdir()}, self.bytes)
        self.assertEqual(output.stat().st_mode & 0o777, 0o700)
        self.api.assert_not_called(); self.run.assert_not_called()

    def test_missing_or_wrong_inventory_freeze_refuses_before_network(self):
        for checksum in ["", "0" * 64, self.inventory_sha.upper(), "../anything"]:
            with self.subTest(checksum=checksum), self.assertRaises(upload.Refusal):
                upload.load_inventory(self.inventory_path, checksum)
        self.api.assert_not_called(); self.run.assert_not_called()

    def test_exact_target_refuses_other_repositories_releases_and_private_asset_intake(self):
        original = copy.deepcopy(self.inventory)
        for field, value in [("repository", "attacker/project"), ("releaseId", 430151477),
                             ("releaseId", str(upload.RELEASE_ID)), ("releaseTag", "other-tag"),
                             ("sourceRevision", "b" * 40), ("format", "unknown")]:
            self.inventory = {**original, field: value}; self.write_inventory()
            with self.subTest(field=field, value=value), self.assertRaisesRegex(upload.Refusal, "exact reviewed"):
                self.load()
        self.api.assert_not_called(); self.run.assert_not_called()

    def test_exact_four_asset_allowlist_refuses_missing_or_extra(self):
        original = copy.deepcopy(self.inventory)
        for missing in [True, False]:
            self.inventory = copy.deepcopy(original)
            if missing: self.inventory["assets"].pop(next(iter(upload.NAMES)))
            else: self.inventory["assets"]["release.json"] = {}
            self.write_inventory()
            with self.subTest(missing=missing), self.assertRaisesRegex(upload.Refusal, "four"):
                self.load()

    def test_duplicate_json_is_refused_even_with_matching_inventory_sha(self):
        data = self.inventory_path.read_bytes().replace(b'"assets":', b'"format":"duplicate","assets":', 1)
        self.inventory_path.write_bytes(data)
        with self.assertRaisesRegex(upload.Refusal, "Duplicate"):
            upload.load_inventory(self.inventory_path, upload.sha256(data))

    def test_inventory_cannot_escape_checkout_with_parent_path_components(self):
        path = self.root / ".." / self.root.name / self.inventory_path.relative_to(self.root)
        with self.assertRaisesRegex(upload.Refusal, "inside the reviewed checkout"):
            upload.load_inventory(path, self.inventory_sha)
        self.api.assert_not_called(); self.run.assert_not_called()

    def test_chunk_traversal_absolute_noncanonical_and_reuse_paths_are_refused(self):
        original = copy.deepcopy(self.inventory)
        first, second = list(upload.NAMES)[:2]
        reused = original["assets"][second]["chunks"][0]["path"]
        for relative in ["../escape", "/tmp/escape", upload.PAYLOAD + "/../escape.bin",
                         upload.PAYLOAD + "//asset0-part00.bin", reused]:
            self.inventory = copy.deepcopy(original)
            self.inventory["assets"][first]["chunks"][0]["path"] = relative
            self.write_inventory()
            with self.subTest(relative=relative), self.assertRaises(upload.Refusal): self.load()

    def test_missing_or_unreferenced_chunks_are_refused(self):
        payload = self.root / upload.PAYLOAD
        extra = payload / "unreviewed.bin"; extra.write_bytes(b"extra")
        with self.assertRaisesRegex(upload.Refusal, "unreferenced"): self.load()
        extra.unlink()
        next(payload.iterdir()).unlink()
        with self.assertRaisesRegex(upload.Refusal, "missing"): self.load()

    def test_chunk_tamper_and_truncation_refused_before_build_extraction(self):
        chunk = self.root / next(iter(self.inventory["assets"].values()))["chunks"][0]["path"]
        original = chunk.read_bytes()
        for changed in [bytes([original[0] ^ 1]) + original[1:], original[:-1]]:
            chunk.write_bytes(changed)
            with self.subTest(length=len(changed)), self.assertRaisesRegex(upload.Refusal, "chunk bytes"):
                self.reconstruct()
        self.api.assert_not_called(); self.run.assert_not_called()

    def test_chunk_file_and_ancestor_symlinks_refused(self):
        chunk = self.root / next(iter(self.inventory["assets"].values()))["chunks"][0]["path"]
        target = self.base / "outside.bin"; target.write_bytes(chunk.read_bytes())
        chunk.unlink(); chunk.symlink_to(target)
        with self.assertRaisesRegex(upload.Refusal, "symlink"): self.reconstruct()
        chunk.unlink(); chunk.write_bytes(target.read_bytes())
        payload = self.root / upload.PAYLOAD
        moved = self.base / "moved"; payload.rename(moved); payload.symlink_to(moved, target_is_directory=True)
        with self.assertRaisesRegex(upload.Refusal, "regular reviewed"): self.load()

    def test_output_cannot_overwrite_checkout_existing_data_or_follow_symlink(self):
        sentinel = self.base / "existing"; sentinel.mkdir(); (sentinel / "keep").write_text("keep")
        link = self.base / "link"; link.symlink_to(sentinel, target_is_directory=True)
        for output in [self.root / "generated", sentinel, link / "child",
                       self.base / ".." / self.base.name / "new-output"]:
            with self.subTest(output=output), self.assertRaises(upload.Refusal):
                upload.reconstruct(self.load(), output)
        self.assertEqual((sentinel / "keep").read_text(), "keep")

    def test_complete_manifest_mismatch_is_refused_even_when_upload_hashes_match(self):
        def mutate(kind, manifest):
            if kind == "Client": manifest["files"]["UnityPlayer.so"] = "0" * 64
        self.make_fixture(mutate_manifest=mutate)
        with self.assertRaisesRegex(Exception, "differ"): self.reconstruct()
        self.api.assert_not_called(); self.run.assert_not_called()

    def test_dirty_wrong_revision_and_wrong_builder_are_refused_in_both_native_builds(self):
        for kind in ["Server", "Client"]:
            for field, value in [("dirty", True), ("revision", "b" * 40), ("executeMethod", "Unreviewed.Builder")]:
                def mutate(files):
                    name = "Ascendance" + kind + ".provenance.json"
                    proof = json.loads(files[name])
                    proof["build" if field == "executeMethod" else "source"][field] = value
                    files[name] = canonical(proof)
                self.make_fixture(**{"mutate_" + kind.lower(): mutate})
                with self.subTest(kind=kind, field=field), self.assertRaisesRegex(Exception, "clean source|builder"):
                    self.reconstruct()
        self.api.assert_not_called(); self.run.assert_not_called()

    def test_private_key_invite_and_credential_json_are_refused_before_api(self):
        for name, data in [("server.pfx", b"synthetic private material"), ("Ascendance.invite.json", b"{}"),
                           ("settings.json", b'{"nested":{"accessKey":"synthetic-secret"}}')]:
            def mutate(files): files[name] = data
            self.make_fixture(mutate_client=mutate)
            with self.subTest(name=name), self.assertRaisesRegex(Exception, "Private"): self.reconstruct()
        self.api.assert_not_called(); self.run.assert_not_called()

    def test_archive_extra_or_traversing_file_is_refused_after_chunk_verification(self):
        for name in ["LinuxServer/extra", "LinuxServer/../escape", "/absolute", "OtherGame/data"]:
            self.make_fixture(extra_server=(name, b"never executed"))
            with self.subTest(name=name), self.assertRaisesRegex(Exception, "file set|ZIP path"):
                self.reconstruct()
        self.api.assert_not_called(); self.run.assert_not_called()

    def test_published_wrong_target_or_private_intake_draft_refuses_all_writes(self):
        output = self.reconstruct()
        original = self.draft([])
        for field, value in [("draft", False), ("prerelease", False), ("id", 430151477),
                             ("target_commitish", "b" * 40), ("tag_name", "other")]:
            self.api.side_effect = [{**original, field: value}]
            with self.subTest(field=field), self.assertRaisesRegex(upload.Refusal, "expected private draft"):
                upload.upload(self.inventory, output)
            self.run.assert_not_called()

    def test_absent_wrong_or_annotated_runtime_tag_refuses_writes(self):
        output = self.reconstruct()
        refs = [{}, {**self.ref(), "ref": "refs/tags/other"},
                {**self.ref(), "object": {"type": "tag", "sha": upload.SOURCE_REVISION}},
                {**self.ref(), "object": {"type": "commit", "sha": "b" * 40}}]
        for ref in refs:
            self.api.side_effect = [self.draft([]), ref]
            with self.subTest(ref=ref), self.assertRaisesRegex(upload.Refusal, "tag"):
                upload.upload(self.inventory, output)
            self.run.assert_not_called()
        self.api.side_effect = [self.draft([]), upload.Refusal("Tag missing")]
        with self.assertRaisesRegex(upload.Refusal, "missing"): upload.upload(self.inventory, output)
        self.run.assert_not_called()

    def test_existing_asset_wrong_digest_size_state_or_unreviewed_name_refuses_replacement(self):
        output = self.reconstruct()
        for field, value in [("digest", "sha256:" + "0" * 64), ("size", 0), ("state", "new"),
                             ("name", "private-intake.zip")]:
            candidate = self.draft()
            candidate["assets"][0][field] = value
            self.api.side_effect = [candidate]
            with self.subTest(field=field), self.assertRaises(upload.Refusal): upload.upload(self.inventory, output)
            self.run.assert_not_called()

    def test_duplicate_draft_asset_is_refused(self):
        output = self.reconstruct(); candidate = self.draft()
        candidate["assets"].append(candidate["assets"][0])
        self.api.side_effect = [candidate]
        with self.assertRaisesRegex(upload.Refusal, "duplicate"): upload.upload(self.inventory, output)
        self.run.assert_not_called()

    def test_upload_only_missing_asset_verifies_exact_tag_and_remains_unpublished(self):
        output = self.reconstruct()
        names = list(upload.NAMES)
        self.api.side_effect = [self.draft(names[:-1]), self.ref(), self.draft()]
        self.run.side_effect = None
        result = upload.upload(self.inventory, output)
        command = self.run.call_args.args[0]
        self.assertEqual(command, ["gh", "release", "upload", upload.RELEASE_TAG, "--repo", upload.REPOSITORY,
                                   str(output / names[-1])])
        self.assertTrue(self.run.call_args.kwargs["check"])
        self.assertNotIn("--clobber", command)
        self.assertEqual(self.api.call_args_list[1].args[0], "repos/" + upload.REPOSITORY + "/git/ref/tags/" + upload.RELEASE_TAG)
        self.assertEqual(result["assets"], 4)
        self.assertTrue(result["draft"]); self.assertFalse(result["published"]); self.assertFalse(result["deployed"])

    def test_already_complete_private_draft_does_no_writes(self):
        output = self.reconstruct()
        self.api.side_effect = [self.draft(), self.ref(), self.draft()]
        result = upload.upload(self.inventory, output)
        self.assertTrue(result["draft"]); self.run.assert_not_called()

    def test_failed_or_incomplete_upload_is_not_success(self):
        output = self.reconstruct()
        self.api.side_effect = [self.draft([]), self.ref(), self.draft([])]
        self.run.side_effect = None
        with self.assertRaisesRegex(upload.Refusal, "incomplete"): upload.upload(self.inventory, output)
        changed = self.draft(); changed["assets"][0]["digest"] = "sha256:" + "0" * 64
        self.api.side_effect = [self.draft([]), self.ref(), changed]
        with self.assertRaisesRegex(upload.Refusal, "different bytes"): upload.upload(self.inventory, output)

    def test_upload_rechecks_reconstructed_bytes_and_unexpected_files_before_api(self):
        output = self.reconstruct()
        name = next(iter(upload.NAMES)); (output / name).write_bytes(b"tampered after verification")
        with self.assertRaises(upload.Refusal): upload.upload(self.inventory, output)
        self.api.assert_not_called(); self.run.assert_not_called()
        (output / name).write_bytes(self.bytes[name]); (output / "unreviewed").write_bytes(b"extra")
        with self.assertRaises(upload.Refusal): upload.upload(self.inventory, output)
        self.api.assert_not_called(); self.run.assert_not_called()


if __name__ == "__main__":
    unittest.main()
