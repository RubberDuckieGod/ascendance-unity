"""Fixture tests for the handoff gate, independent of unavailable Unity assets."""

import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


TOOL = Path(__file__).resolve().parents[1] / "tools" / "project_intake.py"
SPEC = importlib.util.spec_from_file_location("project_intake", TOOL)
intake = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(intake)


class IntakeTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.write("ProjectSettings/ProjectVersion.txt", "m_EditorVersion: 6000.6.5f1\nm_EditorVersionWithRevision: 6000.6.5f1 (3ff58d469c8a)\n")
        self.write("ProjectSettings/ProjectSettings.asset", "PlayerSettings:\n  companyName: Ascendance\n  productName: Ascendance\n  bundleVersion: 0.10.2h\n")
        self.write("ProjectSettings/EditorSettings.asset", "EditorSettings:\n  m_SerializationMode: 2\n")
        self.write("ProjectSettings/VersionControlSettings.asset", "VersionControlSettings:\n  m_Mode: Visible Meta Files\n")
        self.write("ProjectSettings/EditorBuildSettings.asset", """EditorBuildSettings:
  m_Scenes:
  - enabled: 1
    path: Assets/Scenes/MainMenu.unity
    guid: 7a1e1c2f1b2e4a49a86759007a000701
  - enabled: 1
    path: Assets/Scenes/Stonebrook.unity
    guid: 7a1e1c2f1b2e4a49a86759007a000702
  m_configObjects: {}
""")
        self.json("Packages/manifest.json", {"dependencies": {"com.unity.ugui": "2.6.0"}})
        self.json("Packages/packages-lock.json", {"dependencies": {
            "com.unity.ugui": {"version": "2.6.0", "source": "builtin", "depth": 0,
                               "dependencies": {"com.unity.modules.ui": "1.0.0"}},
            "com.unity.modules.ui": {"version": "1.0.0", "source": "builtin", "depth": 1, "dependencies": {}},
        }})
        # Executing this historical verifier would leave a marker and fail.
        self.write("tests/verify_historical.py", "from pathlib import Path\nPath(__file__).with_suffix('.executed').touch()\nraise RuntimeError('must never execute')\n")

    def write(self, rel, text):
        path = self.root / rel
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")

    def json(self, rel, value):
        self.write(rel, json.dumps(value))

    def check(self, report, section, check_id):
        return next(c for c in report["sections"][section]["checks"] if c["id"] == check_id)

    def test_metadata_only_passes_without_certifying_missing_source_or_runtime(self):
        report = intake.inspect_project(self.root, "metadata-only")
        self.assertEqual(report["gate_status"], "PASS")
        self.assertEqual(report["sections"]["metadata"]["status"], "PASS")
        self.assertEqual(report["sections"]["source"]["status"], "BLOCKED")
        self.assertEqual(report["sections"]["runtime"]["status"], "BLOCKED")
        self.assertFalse(report["project_code_executed"])
        self.assertFalse((self.root / "tests/verify_historical.executed").exists())

    def test_required_source_fails_and_cli_writes_report_on_failure(self):
        target = self.root / "failure.json"
        result = subprocess.run([sys.executable, str(TOOL), "--require-source", "--project-root", str(self.root),
                                 "--report", str(target)], capture_output=True, text=True, check=False)
        self.assertEqual(result.returncode, 1, result.stderr)
        report = json.loads(target.read_text())
        self.assertEqual(report["gate_status"], "FAIL")
        self.assertEqual(report["sections"]["metadata"]["status"], "PASS")
        self.assertEqual(self.check(report, "source", "file:Assets/Scenes/MainMenu.unity")["status"], "BLOCKED")
        self.assertFalse(report["sections"]["runtime"]["executed"])

    def test_invalid_manifest_json_fails_metadata_gate(self):
        self.write("Packages/manifest.json", '{"dependencies":')
        report = intake.inspect_project(self.root, "metadata-only")
        self.assertEqual(report["gate_status"], "FAIL")
        self.assertEqual(self.check(report, "metadata", "json:Packages/manifest.json")["status"], "FAIL")

    def test_missing_metadata_is_failure_not_missing_source_exception(self):
        (self.root / "ProjectSettings/ProjectVersion.txt").unlink()
        report = intake.inspect_project(self.root, "metadata-only")
        self.assertEqual(report["sections"]["metadata"]["status"], "FAIL")
        self.assertEqual(report["sections"]["source"]["status"], "BLOCKED")

    def test_lock_must_resolve_transitive_dependencies(self):
        rel = "Packages/packages-lock.json"
        lock = json.loads((self.root / rel).read_text())
        del lock["dependencies"]["com.unity.modules.ui"]
        self.json(rel, lock)
        report = intake.inspect_project(self.root, "metadata-only")
        check = self.check(report, "metadata", "package-lock-consistency")
        self.assertEqual(check["status"], "FAIL")
        self.assertIn("com.unity.modules.ui", check["detail"])

    def test_direct_package_version_must_match_lock(self):
        self.json("Packages/manifest.json", {"dependencies": {"com.unity.ugui": "9.0.0"}})
        report = intake.inspect_project(self.root, "metadata-only")
        self.assertEqual(self.check(report, "metadata", "package-lock-consistency")["status"], "FAIL")

    def test_duplicate_json_keys_are_rejected(self):
        self.write("Packages/manifest.json", '{"dependencies":{"com.unity.ugui":"2.6.0","com.unity.ugui":"9.0.0"}}')
        report = intake.inspect_project(self.root, "metadata-only")
        self.assertIn("duplicate JSON key", self.check(report, "metadata", "json:Packages/manifest.json")["detail"])

    def test_scene_meta_guid_mismatch_fails_even_when_scene_exists(self):
        self.write("Assets/Scenes/MainMenu.unity", "%YAML 1.1\n--- !u!1 &1\nGameObject: {}\n")
        self.write("Assets/Scenes/MainMenu.unity.meta", "fileFormatVersion: 2\nguid: 11111111111111111111111111111111\n")
        report = intake.inspect_project(self.root)
        self.assertEqual(report["sections"]["metadata"]["status"], "PASS")
        self.assertEqual(self.check(report, "source", "scene-guid:Assets/Scenes/MainMenu.unity")["status"], "FAIL")
        self.assertEqual(report["gate_status"], "FAIL")

    def test_build_settings_cannot_silently_replace_original_scene_guid(self):
        rel = "ProjectSettings/EditorBuildSettings.asset"
        self.write(rel, (self.root / rel).read_text().replace("7a1e1c2f1b2e4a49a86759007a000701", "11111111111111111111111111111111"))
        report = intake.inspect_project(self.root, "metadata-only")
        self.assertEqual(self.check(report, "metadata", "build-scene-settings")["status"], "FAIL")

    def test_syntax_error_in_legacy_verifier_is_reported_without_execution(self):
        self.write("tests/verify_bad.py", "def broken(:\n")
        report = intake.inspect_project(self.root, "metadata-only")
        self.assertEqual(self.check(report, "metadata", "syntax:tests/verify_bad.py")["status"], "FAIL")
        self.assertFalse((self.root / "tests/verify_historical.executed").exists())

    def test_editor_revision_mismatch_fails_metadata(self):
        self.write("ProjectSettings/ProjectVersion.txt", "m_EditorVersion: 6000.6.5f1\nm_EditorVersionWithRevision: 6000.6.4f1 (3ff58d469c8a)\n")
        report = intake.inspect_project(self.root, "metadata-only")
        self.assertEqual(self.check(report, "metadata", "editor-version")["status"], "FAIL")

    def test_empty_player_version_cannot_consume_following_setting(self):
        self.write("ProjectSettings/ProjectSettings.asset", "PlayerSettings:\n  companyName: Ascendance\n  productName: Ascendance\n  bundleVersion:\n  otherSetting: 123\n")
        report = intake.inspect_project(self.root, "metadata-only")
        self.assertEqual(self.check(report, "metadata", "player-identity")["status"], "FAIL")

    def test_nonstandard_json_numbers_are_rejected(self):
        self.write("Packages/manifest.json", '{"dependencies":{"com.unity.ugui":NaN}}')
        report = intake.inspect_project(self.root, "metadata-only")
        self.assertIn("nonstandard JSON constant", self.check(report, "metadata", "json:Packages/manifest.json")["detail"])

    def test_truncated_authored_catalog_is_not_certified_as_valid_source(self):
        self.json("Assets/Resources/Ascendance/Data/hero_roster.json", {"heroes": []})
        report = intake.inspect_project(self.root)
        check = self.check(report, "source", "json:Assets/Resources/Ascendance/Data/hero_roster.json")
        self.assertEqual(check["status"], "FAIL")
        self.assertIn("16 entries", check["detail"])
        self.assertEqual(report["sections"]["runtime"]["status"], "BLOCKED")


if __name__ == "__main__":
    unittest.main()
