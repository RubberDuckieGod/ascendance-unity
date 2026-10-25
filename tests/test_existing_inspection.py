"""Read-only inspector privacy and live-identity fixtures, without systemd or /proc."""
import importlib.util
import json
import os
from pathlib import Path
import tempfile
import unittest


SOURCE = Path(__file__).resolve().parents[1] / "Deploy" / "DigitalOcean" / "inspect-existing.py"
SPEC = importlib.util.spec_from_file_location("existing_inspection", SOURCE)
MODULE = None
if os.name == "posix":
    MODULE = importlib.util.module_from_spec(SPEC)
    SPEC.loader.exec_module(MODULE)


@unittest.skipUnless(os.name == "posix", "Linux /proc inspection fixtures")
class ExistingInspection(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="asc-inspection-")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.proc = self.root / "proc"
        self.process = self.proc / "123"
        self.process.mkdir(parents=True)
        self.game = self.root / "install"
        self.game.mkdir()
        self.binary = self.game / "AscendanceServer.x86_64"
        self.binary.write_bytes(b"fixture-not-executable")
        (self.process / "exe").symlink_to(self.binary)
        (self.process / "cwd").symlink_to(self.game)
        (self.process / "status").write_text(f"Uid:\t{os.getuid()}\t{os.getuid()}\t{os.getuid()}\t{os.getuid()}\nGid:\t{os.getgid()}\t{os.getgid()}\t{os.getgid()}\t{os.getgid()}\n")
        self.unit = self.root / "ascendance.service"
        self.unit.write_text("[Service]\nEnvironment=PRIVATE_VALUE=never-publish-unit-content\n")
        self.config = self.root / "external-config"
        self.data = self.config / "unity3d" / "Ascendance" / "Ascendance"
        self.room = self.data / "AscendanceServerRooms0100" / "ASC-PRIMARY.json"
        self.room.parent.mkdir(parents=True)
        self.room.write_text('{"privateSavedPlayer":"never-publish-save-content"}')
        self.arguments = [str(self.binary), "-batchmode", "-nographics", "-ascendance-server", "-room", "ASC-PRIMARY", "-port", "7777"]
        self.env = {"HOME": str(self.root / "home"), "XDG_CONFIG_HOME": str(self.config),
                    "CLOUD_TOKEN": "never-publish-environment-secret"}
        self.properties = {"MainPID": "123", "ActiveState": "active", "SubState": "running",
                           "User": "ascendance", "Group": "ascendance", "WorkingDirectory": str(self.game),
                           "FragmentPath": str(self.unit), "DropInPaths": "", "LoadState": "loaded"}

    def run_inspection(self):
        (self.process / "cmdline").write_bytes(b"\0".join(x.encode() for x in self.arguments) + b"\0")
        (self.process / "environ").write_bytes(b"\0".join((k + "=" + v).encode() for k, v in self.env.items()) + b"\0")
        self.called = []
        def run(args):
            self.called.append(args)
            return "\n".join(k + "=" + v for k, v in self.properties.items())
        return MODULE.inspect(proc_root=self.proc, run=run)

    def test_reads_actual_process_environment_and_retains_private_data(self):
        before = self.room.read_bytes(), self.unit.read_bytes(), self.binary.read_bytes()
        report = self.run_inspection()
        self.assertTrue(report["readOnly"])
        self.assertEqual(report["process"]["room"], "ASC-PRIMARY")
        self.assertEqual(report["process"]["port"], 7777)
        self.assertEqual(report["persistence"]["expectedDataDirectory"]["path"], str(self.data))
        self.assertTrue(report["persistence"]["expectedRoomFileExists"])
        self.assertEqual(report["warnings"], [])
        encoded = json.dumps(report)
        for secret in ["never-publish-unit-content", "never-publish-save-content", "never-publish-environment-secret", "CLOUD_TOKEN"]:
            self.assertNotIn(secret, encoded)
        self.assertEqual(before, (self.room.read_bytes(), self.unit.read_bytes(), self.binary.read_bytes()))
        self.assertEqual(len(self.called), 1)
        self.assertEqual(self.called[0][:3], ["systemctl", "show", "ascendance"])

    def test_unknown_flag_values_are_not_disclosed(self):
        self.arguments += ["-credential", "do-not-print-this", "-password=also-private"]
        report = self.run_inspection()
        self.assertEqual(report["process"]["unknownFlagNames"], ["-credential", "-password"])
        self.assertNotIn("do-not-print-this", json.dumps(report))
        self.assertNotIn("also-private", json.dumps(report))
        self.assertTrue(report["warnings"])

    def test_missing_expected_data_is_an_explicit_warning(self):
        self.room.unlink()
        report = self.run_inspection()
        self.assertFalse(report["persistence"]["expectedRoomFileExists"])
        self.assertTrue(any("actual data path" in warning for warning in report["warnings"]))

    def test_alternate_unity_identity_is_not_silently_selected(self):
        alternate = self.config / "unity3d" / "DefaultCompany" / "OtherProduct"
        (alternate / "AscendanceServerRooms0100").mkdir(parents=True)
        report = self.run_inspection()
        self.assertEqual(report["persistence"]["expectedDataDirectory"]["path"], str(self.data))
        self.assertEqual(report["persistence"]["otherServerDataDirectories"], [str(alternate)])
        self.assertTrue(any("Other Unity" in warning for warning in report["warnings"]))

    def test_inactive_service_refuses_process_inspection(self):
        self.properties["ActiveState"] = "inactive"
        with self.assertRaisesRegex(ValueError, "not running"):
            self.run_inspection()

    def test_invalid_service_argument_refuses_before_systemctl(self):
        with self.assertRaisesRegex(ValueError, "service name"):
            MODULE.inspect("ascendance; unwanted", proc_root=self.proc,
                           run=lambda args: self.fail("must not execute"))

    def test_duplicate_room_flags_refuse_ambiguous_identity(self):
        self.arguments += ["-room", "OTHER"]
        with self.assertRaisesRegex(ValueError, "Duplicate"):
            self.run_inspection()

    def test_default_room_follows_source_not_unread_environment_room(self):
        self.arguments = [str(self.binary), "-ascendance-server"]
        self.env["ASCENDANCE_ROOM"] = "IGNORED-BY-PLAYER-WITHOUT-FLAG"
        self.env["ASCENDANCE_PORT"] = "7788"
        report = self.run_inspection()
        self.assertEqual(report["process"]["room"], "LOCAL-ROOM")
        self.assertEqual(report["process"]["port"], 7788)


if __name__ == "__main__":
    unittest.main()
