"""Synthetic input guards only; these are not a native restoration verdict."""
import importlib.util
import json
import os
from pathlib import Path
import sys
import tempfile
from types import SimpleNamespace
import unittest
from unittest import mock


ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))
SPEC = importlib.util.spec_from_file_location("native_combat_migration_inputs", ROOT / "tools/native_combat_campaign_migration.py")
PROOF = importlib.util.module_from_spec(SPEC) if os.name == "posix" else None
if PROOF is not None:
    SPEC.loader.exec_module(PROOF)


@unittest.skipUnless(os.name == "posix", "Linux native migration input guards")
class NativeCombatMigrationInputs(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="asc-native-combat-input-")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.original, self.candidate = self.root / "original", self.root / "candidate"
        self.revision = "a" * 40
        for folder, revision in ((self.original, PROOF.ORIGINAL_REVISION), (self.candidate, self.revision)):
            folder.mkdir()
            (folder / "AscendanceServer.provenance.json").write_text(json.dumps({
                "source": {"vcs": "git", "revision": revision, "dirty": False}}))
        self.args = SimpleNamespace(original_build=self.original, original_manifest=self.root / "original-manifest.json",
                                    candidate_build=self.candidate, candidate_manifest=self.root / "candidate-manifest.json",
                                    expected_candidate_revision=self.revision)

    def fake_digest(self, path):
        return PROOF.ORIGINAL_ASSEMBLY if path.is_relative_to(self.original) else "b" * 64

    def verify(self):
        with mock.patch.object(PROOF.release, "read_json", side_effect=lambda path: json.loads(path.read_text()) if path.name.endswith("provenance.json") else {}), \
             mock.patch.object(PROOF.release, "verify_artifact"), mock.patch.object(PROOF, "digest", side_effect=self.fake_digest):
            return PROOF.verify_builds(self.args)

    def test_exact_original_and_different_clean_candidate_are_required(self):
        self.assertEqual(self.verify(), (self.original, self.candidate))
        for field, value in (("revision", "b" * 40), ("dirty", True), ("vcs", "unknown")):
            path = self.original / "AscendanceServer.provenance.json"
            source = {"vcs": "git", "revision": PROOF.ORIGINAL_REVISION, "dirty": False}
            source[field] = value
            path.write_text(json.dumps({"source": source}))
            with self.subTest(field=field), self.assertRaisesRegex(AssertionError, "exact reviewed clean"):
                self.verify()

    def test_unknown_original_managed_assembly_refuses_before_candidate_verification(self):
        with mock.patch.object(PROOF.release, "read_json", side_effect=lambda path: json.loads(path.read_text()) if path.name.endswith("provenance.json") else {}), \
             mock.patch.object(PROOF.release, "verify_artifact") as artifact, \
             mock.patch.object(PROOF, "digest", return_value="0" * 64), \
             self.assertRaisesRegex(AssertionError, "exact reviewed clean"):
            PROOF.verify_builds(self.args)
        self.assertEqual(artifact.call_count, 1)

    def test_candidate_cannot_be_old_source_missing_revision_or_dirty(self):
        for revision in (None, "", "short", PROOF.ORIGINAL_REVISION):
            self.args.expected_candidate_revision = revision
            with self.subTest(revision=revision), self.assertRaisesRegex(AssertionError, "different exact clean"):
                self.verify()
        self.args.expected_candidate_revision = self.revision
        (self.candidate / "AscendanceServer.provenance.json").write_text(json.dumps({
            "source": {"vcs": "git", "revision": self.revision, "dirty": True}}))
        with self.assertRaisesRegex(AssertionError, "candidate provenance"):
            self.verify()

    def test_candidate_must_actually_contain_a_new_game_assembly(self):
        with mock.patch.object(PROOF.release, "read_json", side_effect=lambda path: json.loads(path.read_text()) if path.name.endswith("provenance.json") else {}), \
             mock.patch.object(PROOF.release, "verify_artifact"), \
             mock.patch.object(PROOF, "digest", return_value=PROOF.ORIGINAL_ASSEMBLY), \
             self.assertRaisesRegex(AssertionError, "new reviewed game assembly"):
            PROOF.verify_builds(self.args)

    def test_private_tree_is_read_only_and_refuses_links(self):
        folder = self.root / "private-store"; folder.mkdir()
        game = folder / "room.json"; game.write_bytes(b"private synthetic fixture, never a real save")
        first = PROOF.private_tree(folder)
        self.assertEqual(PROOF.private_tree(folder), first)
        self.assertEqual(game.read_bytes(), b"private synthetic fixture, never a real save")
        if os.name == "posix":
            link = folder / "link"; link.symlink_to(game)
            with self.assertRaisesRegex(AssertionError, "contains a link"):
                PROOF.private_tree(folder)

    @unittest.skipUnless(os.name == "posix", "POSIX special-file guard")
    def test_private_tree_refuses_special_files_without_reading_them(self):
        folder = self.root / "private-store"; folder.mkdir()
        os.mkfifo(folder / "fifo")
        with self.assertRaisesRegex(AssertionError, "unsupported file"):
            PROOF.private_tree(folder)


if __name__ == "__main__":
    unittest.main()
