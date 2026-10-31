"""Data-only migration input tests; no game process, save, network or deployment."""
import contextlib
import copy
import importlib.util
import io
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest import mock

ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location("native_migration_inputs", ROOT / "tools/native_g_h_migration.py")
MIGRATION = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MIGRATION)


class NativeMigrationInputs(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="asc-native-input-fixture-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.base = self.root / "base"
        (self.base / "AscendanceServer_Data").mkdir(parents=True)
        self.metadata = self.base / "AscendanceServer_Data/RuntimeInitializeOnLoads.json"
        self.data = {"root": [
            {"assemblyName": "Assembly-CSharp", "nameSpace": namespace,
             "className": class_name, "methodName": method, "loadTypes": 1, "isUnityClass": False}
            for namespace, class_name, method in sorted(MIGRATION.EXPECTED_INITIALIZERS)
        ]}
        self.metadata.write_text(json.dumps(self.data))

    def test_unknown_legacy_game_code_refused_before_copy_or_game_execution(self):
        legacy = self.root / "unknown.dll"
        legacy.write_bytes(b"synthetic foreign code; never executed")
        output = self.root / "output"
        with mock.patch.object(MIGRATION.release, "verify_artifact") as artifact, mock.patch.object(MIGRATION.shutil, "copytree") as copied:
            with self.assertRaisesRegex(ValueError, "SHA-256 differs"):
                MIGRATION.prepare_legacy_runtime(legacy, self.base, self.root / "manifest.json", output)
        artifact.assert_not_called()
        copied.assert_not_called()
        self.assertFalse(output.exists())

    def test_reviewed_five_initializers_are_required_and_bad_phases_are_not_accepted(self):
        MIGRATION.validate_initializer_contract(self.base)
        for change in [lambda data: data["root"].pop(),
                       lambda data: data["root"].append(copy.deepcopy(data["root"][0])),
                       lambda data: data["root"][0].update(className="UnknownGameSetup"),
                       lambda data: data["root"][0].update(loadTypes=True),
                       lambda data: data["root"][0].update(isUnityClass=True)]:
            data = copy.deepcopy(self.data)
            change(data)
            self.metadata.write_text(json.dumps(data))
            with self.subTest(change=change), self.assertRaises(ValueError):
                MIGRATION.validate_initializer_contract(self.base)

    def test_duplicate_runtime_metadata_fields_are_refused(self):
        self.metadata.write_text('{"root":[],"root":[]}')
        with self.assertRaisesRegex(ValueError, "Duplicate"):
            MIGRATION.validate_initializer_contract(self.base)

    def test_existing_output_and_broken_link_are_not_overwritten(self):
        output = self.root / "existing"
        output.mkdir()
        sentinel = output / "keep.txt"
        sentinel.write_text("keep existing review data")
        with self.assertRaisesRegex(ValueError, "must be new"):
            MIGRATION.ensure_new_output(output)
        self.assertEqual(sentinel.read_text(), "keep existing review data")
        if os.name == "posix":
            link = self.root / "link"
            link.symlink_to(self.root / "absent")
            with self.assertRaisesRegex(ValueError, "must be new"):
                MIGRATION.ensure_new_output(link)
            self.assertTrue(link.is_symlink())
        new = self.root / "new"
        MIGRATION.ensure_new_output(new)
        if os.name == "posix":
            self.assertEqual(new.stat().st_mode & 0o777, 0o700)

    def test_optional_legacy_build_requires_its_verified_manifest(self):
        args = ["--legacy-assembly", str(self.root / "game.dll"), "--candidate-build", str(self.base),
                "--manifest", str(self.root / "manifest.json"), "--output", str(self.root / "out"),
                "--legacy-base-build", str(self.base)]
        with mock.patch.object(MIGRATION, "run") as run, contextlib.redirect_stderr(io.StringIO()):
            with self.assertRaises(SystemExit) as exit_code:
                MIGRATION.main(args)
        self.assertEqual(exit_code.exception.code, 2)
        run.assert_not_called()

    def test_enemy_order_is_canonicalized_but_deck_order_and_selection_remain(self):
        original = {"Version": "0.10.2g", "SavedAtUtc": "synthetic timestamp", "SelectedEnemyId": "B",
                    "Enemies": [{"Id": "B", "Health": 2}, {"Id": "A", "Health": 3}],
                    "Heroes": [{"Id": "Aldric", "DeckIds": ["HC002", "HC001"]}]}
        before = copy.deepcopy(original)
        canonical = MIGRATION.canonical_campaign(original)
        self.assertEqual(original, before)
        self.assertEqual([enemy["Id"] for enemy in canonical["Enemies"]], ["A", "B"])
        self.assertEqual(canonical["Heroes"][0]["DeckIds"], ["HC002", "HC001"])
        self.assertEqual(canonical["SelectedEnemyId"], "B")
        changed = copy.deepcopy(original)
        changed["Heroes"][0]["DeckIds"].reverse()
        self.assertNotEqual(canonical, MIGRATION.canonical_campaign(changed))
        original["Enemies"] = [{"Id": "Enemy", "Health": 2}, {"Id": "ENEMY", "Health": 3}]
        with self.assertRaisesRegex(AssertionError, "not unique"):
            MIGRATION.canonical_campaign(original)


if __name__ == "__main__":
    unittest.main()
