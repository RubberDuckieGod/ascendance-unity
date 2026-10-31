"""Deployment safeguards with synthetic artifacts/services; no real activation."""
import copy
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import stat
import shutil
import socket
import ssl
import struct
import subprocess
import tempfile
import threading
from types import SimpleNamespace
import unittest
from unittest import mock
import zipfile


ROOT = Path(__file__).resolve().parents[1]


def load(name):
    spec = importlib.util.spec_from_file_location(name.replace("-", "_"), ROOT / "Deploy" / "DigitalOcean" / (name + ".py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


UPGRADE = load("upgrade-existing") if os.name == "posix" else None
RELEASE = load("release") if os.name == "posix" else None
INSPECTOR = load("inspect-existing") if os.name == "posix" else None


class OneFrameServer:
    """Real loopback HELLO fixture, not compiled Unity/server gameplay proof."""
    def __init__(self, snapshot, build="0.10.2c"):
        self.snapshot, self.build, self.request, self.errors = snapshot, build, None, []

    def __enter__(self):
        self.listener = socket.socket()
        self.listener.settimeout(4)
        self.listener.bind(("127.0.0.1", 0))
        self.listener.listen(1)
        self.port = self.listener.getsockname()[1]
        self.worker = threading.Thread(target=self.serve, daemon=True)
        self.worker.start()
        return self

    def serve(self):
        try:
            with self.listener.accept()[0] as stream:
                stream.settimeout(4)
                size = struct.unpack("!I", UPGRADE.frame_read(stream, 4))[0]
                if not 0 < size < 65536:
                    raise ValueError("Unexpected fixture request size")
                self.request = json.loads(UPGRADE.frame_read(stream, size))
                response = {"ProtocolVersion": 1, "RequestId": self.request["RequestId"],
                            "Accepted": True, "Code": "HELLO", "ServerBuildVersion": self.build,
                            "Snapshot": self.snapshot}
                body = json.dumps(response).encode()
                stream.sendall(struct.pack("!I", len(body)) + body)
        except Exception as error:
            self.errors.append(type(error).__name__)

    def __exit__(self, *args):
        self.worker.join(5)
        self.listener.close()
        if self.worker.is_alive() or self.errors:
            raise AssertionError("Single-frame loopback fixture did not finish cleanly")


@unittest.skipUnless(os.name == "posix", "Linux deployment safeguards")
class UpgradeSafety(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="asc-upgrade-fixture-")
        self.addCleanup(self.temp.cleanup)
        self.folder = Path(self.temp.name)
        self.legacy = self.folder / "legacy"
        self.release_root = self.folder / "releases-root"
        self.config = self.folder / "private-transport"
        self.backups = self.folder / "backups"
        self.systemd = self.folder / "systemd"
        self.data = self.folder / "data" / "config" / "unity3d" / "Ascendance" / "Ascendance"
        for path in [self.legacy, self.release_root, self.backups, self.systemd, self.data]:
            path.mkdir(parents=True)
        self.room_file = self.data / "AscendanceServerRooms0100" / "ASC-PRIMARY.json"
        self.room_file.parent.mkdir()
        self.room_file.write_text('{"original":"durable game data"}')
        self.unit = self.systemd / "ascendance.service"
        self.unit.write_text("[Service]\nUser=ascendance\nWorkingDirectory=" + str(self.legacy) + "\n")
        self.binary = self.legacy / "AscendanceServer.x86_64"
        self.binary.write_bytes(b"original-code-never-executed")
        self.release = self.release_root / "releases" / "test-release"
        (self.release / "build").mkdir(parents=True)
        (self.release / "build" / "AscendanceServer.x86_64").write_bytes(b"new-code-never-executed")
        self.plan = {"service": "ascendance", "warnings": [],
                     "serviceProperties": {"MainPID": "123", "ActiveState": "active", "SubState": "running", "User": "ascendance", "WorkingDirectory": str(self.legacy), "FragmentPath": str(self.unit)},
                     "process": {"pid": 123, "uid": os.getuid(), "gid": os.getgid(), "executable": str(self.binary), "executableSha256": hashlib.sha256(self.binary.read_bytes()).hexdigest(), "workingDirectory": str(self.legacy), "environment": {"HOME": str(self.folder / "home")}, "room": "ASC-PRIMARY", "port": 7777, "booleanFlags": ["-ascendance-server", "-batchmode", "-nographics"], "logFile": "/var/log/ascendance/server.log"},
                     "persistence": {"expectedDataDirectory": {"path": str(self.data), "symlinkComponent": False}, "expectedRoomFileExists": True},
                     "unitFileChecksums": [{"path": str(self.unit), "sha256": hashlib.sha256(self.unit.read_bytes()).hexdigest()}]}
        self.security = {"pin": "a" * 64, "key": "b" * 64,
                         "environment": {"ASCENDANCE_TLS_PFX_PATH": str(self.config / "server.pfx"), "ASCENDANCE_TLS_PFX_PASSWORD_FILE": str(self.config / "password"), "ASCENDANCE_ROOM_ACCESS_KEY_FILE": str(self.config / "access-key")}}
        self.descriptor = {"release": "test-release", "sourceRevision": "a" * 40, "endpoint": "159.65.100.206:7777"}
        self.addCleanup(mock.patch.stopall)
        mock.patch.multiple(UPGRADE, ROOT=self.release_root, LEGACY=self.legacy, CONFIG=self.config, BACKUPS=self.backups, SYSTEMD=self.systemd).start()
        # Ownership operations are simulated: these fixture tests run unprivileged
        # and do not claim that systemd or on-host activation has occurred.
        mock.patch.object(UPGRADE.os, "fchown", lambda *args: None).start()
        self.actual_durable_phase = UPGRADE.require_durable_phase
        mock.patch.object(UPGRADE, "require_durable_phase", return_value=None).start()

    def activate(self, **changes):
        plan = copy.deepcopy(self.plan)
        plan.update(changes)
        return UPGRADE.activate("ascendance", self.release, plan, self.security, INSPECTOR, UPGRADE.plan_fingerprint(plan), self.descriptor)

    def mock_plan(self):
        return mock.patch.object(UPGRADE, "checked_plan", side_effect=lambda *args: copy.deepcopy(self.plan))

    def fixture_backup(self, *args):
        target = self.backups / "verified-backup"
        target.mkdir()
        return target

    def fake_directory(self, path, mode=0o700, gid=0):
        path.mkdir(parents=True, exist_ok=True)
        path.chmod(mode)

    def test_fingerprint_ignores_process_restart_but_detects_config_or_room_changes(self):
        changed = copy.deepcopy(self.plan)
        changed["process"]["pid"] = 999
        changed["serviceProperties"]["MainPID"] = "999"
        self.assertEqual(UPGRADE.plan_fingerprint(self.plan), UPGRADE.plan_fingerprint(changed))
        changed["process"]["room"] = "DIFFERENT"
        self.assertNotEqual(UPGRADE.plan_fingerprint(self.plan), UPGRADE.plan_fingerprint(changed))

    def test_unit_override_preserves_live_routing_data_and_log_without_secret_or_home_override(self):
        body = UPGRADE.override_text(self.release, self.plan, self.security).decode()
        self.assertIn('"-room" "ASC-PRIMARY" "-port" "7777"', body)
        self.assertIn('"-logFile" "/var/log/ascendance/server.log"', body)
        self.assertIn('Environment="XDG_CONFIG_HOME=' + str(self.data.parents[2]) + '"', body)
        self.assertNotIn("HOME=", body.replace("XDG_CONFIG_HOME=", ""))
        self.assertNotIn(self.security["key"], body)
        self.assertEqual(self.unit.read_text().splitlines()[0], "[Service]")

    def test_unit_argument_metacharacters_refuse_before_activation(self):
        self.plan["process"]["room"] = "ROOM$UNSAFE"
        with self.mock_plan(), mock.patch.object(UPGRADE, "connected_clients", return_value=0), mock.patch.object(UPGRADE, "run") as system:
            with self.assertRaisesRegex(UPGRADE.Refusal, "escaping"):
                self.activate()
            system.assert_not_called()

    def test_connected_players_prevent_backup_and_stop(self):
        with self.mock_plan(), mock.patch.object(UPGRADE, "connected_clients", return_value=1), mock.patch.object(UPGRADE, "backup_existing") as backup, mock.patch.object(UPGRADE, "run") as system:
            with self.assertRaisesRegex(UPGRADE.Refusal, "Players are still connected"):
                self.activate()
            backup.assert_not_called()
            system.assert_not_called()

    def test_failed_backup_never_stops_existing_game(self):
        with self.mock_plan(), mock.patch.object(UPGRADE, "connected_clients", return_value=0), mock.patch.object(UPGRADE, "backup_existing", side_effect=UPGRADE.Refusal("failed backup")), mock.patch.object(UPGRADE, "run") as system:
            with self.assertRaisesRegex(UPGRADE.Refusal, "failed backup"):
                self.activate()
            system.assert_not_called()

    def test_a_player_connecting_during_backup_refuses_before_stop(self):
        with self.mock_plan(), mock.patch.object(UPGRADE, "connected_clients", side_effect=[0, 1]), mock.patch.object(UPGRADE, "backup_existing", side_effect=self.fixture_backup), mock.patch.object(UPGRADE, "run") as system:
            with self.assertRaisesRegex(UPGRADE.Refusal, "player connected"):
                self.activate()
            system.assert_not_called()

    def test_failure_restores_previous_code_configuration_but_never_replaces_saves(self):
        commands = []
        starts = 0
        original = self.room_file.read_bytes()
        def system(args, **kwargs):
            nonlocal starts
            commands.append(args)
            if args[1] == "start":
                starts += 1
                if starts == 1:
                    self.room_file.write_bytes(b"new-server-state-retained-for-manual-review")
            return SimpleNamespace(stdout="inactive\n" if "--property=ActiveState" in args else "")
        with self.mock_plan(), mock.patch.object(UPGRADE, "connected_clients", return_value=0), mock.patch.object(UPGRADE, "backup_existing", side_effect=self.fixture_backup), mock.patch.object(UPGRADE, "secure_directory", side_effect=self.fake_directory), mock.patch.object(UPGRADE, "run", side_effect=system), mock.patch.object(UPGRADE, "healthy", side_effect=[UPGRADE.Refusal("new health failed"), None]) as health:
            with self.assertRaisesRegex(UPGRADE.Refusal, "Save data was not automatically rolled back"):
                self.activate()
        self.assertEqual(starts, 2)
        self.assertEqual(self.binary.read_bytes(), b"original-code-never-executed")
        self.assertEqual(self.room_file.read_bytes(), b"new-server-state-retained-for-manual-review")
        self.assertEqual((self.backups / "verified-backup" / "data-after-stop" / "AscendanceServerRooms0100" / "ASC-PRIMARY.json").read_bytes(), original)
        self.assertFalse((self.systemd / "ascendance.service.d" / "80-ascendance-release.conf").exists())
        self.assertEqual(health.call_args_list[1].kwargs["expected_executable"], str(self.binary))
        self.assertIsNone(health.call_args_list[1].args[3])  # Original server was plaintext.

    def test_recovery_failure_is_distinct_from_successful_rollback(self):
        def system(args, **kwargs):
            return SimpleNamespace(stdout="inactive\n" if "--property=ActiveState" in args else "")
        with self.mock_plan(), mock.patch.object(UPGRADE, "connected_clients", return_value=0), mock.patch.object(UPGRADE, "backup_existing", side_effect=self.fixture_backup), mock.patch.object(UPGRADE, "secure_directory", side_effect=self.fake_directory), mock.patch.object(UPGRADE, "run", side_effect=system), mock.patch.object(UPGRADE, "healthy", side_effect=UPGRADE.Refusal("both health checks fail")):
            with self.assertRaisesRegex(UPGRADE.RecoveryFailure, "both failed"):
                self.activate()

    def test_backup_copy_is_private_and_hash_verified(self):
        target = self.folder / "private-copy"
        hashes = UPGRADE.backup_tree(self.data, target, INSPECTOR)
        self.assertEqual(hashes["AscendanceServerRooms0100/ASC-PRIMARY.json"], hashlib.sha256(self.room_file.read_bytes()).hexdigest())
        self.assertEqual(stat.S_IMODE(target.stat().st_mode), 0o700)
        self.assertEqual(stat.S_IMODE((target / "AscendanceServerRooms0100" / "ASC-PRIMARY.json").stat().st_mode), 0o600)

    def test_real_backup_keeps_code_unit_data_and_transport_files_private(self):
        self.config.mkdir()
        (self.config / "private-key-fixture").write_text("synthetic-private-transport-value")
        before = self.room_file.read_bytes(), self.unit.read_bytes(), self.binary.read_bytes()
        with mock.patch.object(UPGRADE, "secure_directory", side_effect=self.fake_directory), mock.patch.object(UPGRADE, "run", return_value=SimpleNamespace(stdout="")):
            backup = UPGRADE.backup_existing(self.plan, "reviewed-release", INSPECTOR)
        import tarfile
        with tarfile.open(backup / "previous-code.tar.gz") as archive:
            self.assertEqual(archive.extractfile("previous-code/AscendanceServer.x86_64").read(), self.binary.read_bytes())
        self.assertEqual((backup / "data-before-stop" / "AscendanceServerRooms0100" / "ASC-PRIMARY.json").read_bytes(), self.room_file.read_bytes())
        self.assertEqual((backup / "configuration" / self.unit.relative_to("/")).read_bytes(), self.unit.read_bytes())
        self.assertEqual((backup / "private-transport-configuration" / "private-key-fixture").read_text(), "synthetic-private-transport-value")
        self.assertEqual(stat.S_IMODE(backup.stat().st_mode), 0o700)
        self.assertEqual(before, (self.room_file.read_bytes(), self.unit.read_bytes(), self.binary.read_bytes()))

    def test_backup_refuses_a_source_that_changes_during_copy(self):
        copytree = UPGRADE.shutil.copytree
        def mutate(source, destination, *args, **kwargs):
            result = copytree(source, destination, *args, **kwargs)
            if Path(source) == self.data:
                self.room_file.write_text("changed-during-copy")
            return result
        with mock.patch.object(UPGRADE.shutil, "copytree", side_effect=mutate):
            with self.assertRaisesRegex(UPGRADE.Refusal, "Data changed"):
                UPGRADE.backup_tree(self.data, self.folder / "inconsistent-copy", INSPECTOR)

    def test_backup_refuses_new_file_membership_during_copy(self):
        copytree = UPGRADE.shutil.copytree
        def mutate(source, destination, *args, **kwargs):
            result = copytree(source, destination, *args, **kwargs)
            if Path(source) == self.data:
                (self.data / "new-save-during-copy.json").write_text("{}")
            return result
        with mock.patch.object(UPGRADE.shutil, "copytree", side_effect=mutate):
            with self.assertRaisesRegex(UPGRADE.Refusal, "Data changed"):
                UPGRADE.backup_tree(self.data, self.folder / "incomplete-copy", INSPECTOR)

    def test_full_old_code_manifest_detects_changed_assembly_with_same_executable(self):
        assembly = self.legacy / "AscendanceServer_Data" / "Managed" / "Assembly-CSharp.dll"
        assembly.parent.mkdir(parents=True)
        assembly.write_bytes(b"old-gameplay-code")
        original = UPGRADE.tree_proof(self.legacy)
        unchanged_binary = self.binary.read_bytes()
        assembly.write_bytes(b"changed-gameplay-code")
        self.assertNotEqual(original, UPGRADE.tree_proof(self.legacy))
        self.assertEqual(self.binary.read_bytes(), unchanged_binary)

    def test_corrupted_rollback_code_archive_cannot_pass_manifest_verification(self):
        import tarfile
        proof = UPGRADE.tree_proof(self.legacy)
        archive_path = self.folder / "altered-code.tar.gz"
        self.binary.write_bytes(b"corrupted-backup-code")
        with tarfile.open(archive_path, "w:gz") as archive:
            archive.add(self.legacy, arcname="previous-code")
        with self.assertRaisesRegex(UPGRADE.Refusal, "manifest verification"):
            UPGRADE.verify_code_archive(archive_path, proof)

    def test_environment_file_proof_detects_private_value_drift_without_disclosing_values(self):
        environment = self.folder / "private.env"
        environment.write_text("SYNTHETIC_SECRET=first-private-value\n")
        environment.chmod(0o600)
        with mock.patch.object(UPGRADE, "run", return_value=SimpleNamespace(stdout=str(environment) + " (ignore_errors=no)\n")):
            # Fixture file ownership is the test runner's; production requires root.
            original_proof = UPGRADE.file_proof
            def proof(path):
                value = original_proof(path)
                value["uid"] = 0
                return value
            with mock.patch.object(UPGRADE, "file_proof", side_effect=proof):
                first = UPGRADE.environment_proof("ascendance", INSPECTOR)
                environment.write_text("SYNTHETIC_SECRET=second-private-value\n")
                second = UPGRADE.environment_proof("ascendance", INSPECTOR)
        self.assertNotEqual(first, second)
        self.assertNotIn("private-value", json.dumps(first) + json.dumps(second))

    def test_environment_file_symlink_is_refused_before_content_read(self):
        target = self.folder / "private.env"
        target.write_text("SYNTHETIC_SECRET=private-value\n")
        link = self.folder / "unsafe.env"
        link.symlink_to(target)
        with mock.patch.object(UPGRADE, "run", return_value=SimpleNamespace(stdout=str(link) + " (ignore_errors=no)\n")), mock.patch.object(UPGRADE, "file_proof") as proof:
            with self.assertRaisesRegex(UPGRADE.Refusal, "path identity"):
                UPGRADE.environment_proof("ascendance", INSPECTOR)
            proof.assert_not_called()

    def test_endpoint_must_match_the_actual_droplet_interface_before_staging(self):
        addresses = [{"addr_info": [{"family": "inet", "local": "159.65.100.206"},
                                    {"family": "inet", "local": "10.46.0.5"}]}]
        with mock.patch.object(UPGRADE, "run", return_value=SimpleNamespace(stdout=json.dumps(addresses))):
            UPGRADE.require_endpoint_on_host("159.65.100.206")
            with self.assertRaisesRegex(UPGRADE.Refusal, "does not belong"):
                UPGRADE.require_endpoint_on_host("203.0.113.9")

    def test_actual_native_default_denial_dto_is_accepted_without_authority_data(self):
        response = json.loads((ROOT / "tests/fixtures/native-denied-default-response.json").read_text())
        self.assertTrue(response["Snapshot"])
        self.assertTrue(UPGRADE.denied_without_authority(response))

    def test_populated_authority_fields_in_any_denial_dto_are_rejected(self):
        blank = json.loads((ROOT / "tests/fixtures/native-denied-default-response.json").read_text())
        for route, value in [
            (("Snapshot", "RoomId"), "PRIVATE-ROOM"),
            (("Snapshot", "ServerInstanceId"), "instance"),
            (("Snapshot", "Revision"), 1),
            (("Snapshot", "Players"), [{"PlayerId": "session"}]),
            (("Snapshot", "Campaign", "PartyGold"), 20),
            (("Snapshot", "Campaign", "Heroes"), [{"Id": "Aldric"}]),
            (("Snapshot", "Transient", "RecentLog"), ["private log"]),
            (("CommandResult", "Snapshot", "Campaign", "PartyGold"), 20),
            (("Player", "ReconnectToken"), "private reconnect proof")]:
            response = copy.deepcopy(blank)
            parent = response
            for name in route[:-1]: parent = parent[name]
            parent[route[-1]] = value
            with self.subTest(route=route):
                self.assertFalse(UPGRADE.denied_without_authority(response))

    def test_secure_health_checks_missing_and_actual_incorrect_keys_before_authenticated_hello(self):
        inspector = SimpleNamespace(inspect=lambda service: copy.deepcopy(self.plan))
        with mock.patch.object(UPGRADE, "hello") as hello:
            UPGRADE.healthy("ascendance", self.release, self.plan, self.security, inspector, timeout=1, expected_executable=str(self.binary))
        self.assertEqual(len(hello.call_args_list), 3)
        self.assertIsNone(hello.call_args_list[0].args[3])
        wrong = hello.call_args_list[1]
        self.assertRegex(wrong.args[3], r"^[0-9a-f]{64}$")
        self.assertNotEqual(wrong.args[3], self.security["key"])
        self.assertTrue(wrong.kwargs["expect_denied"])
        self.assertEqual(hello.call_args_list[2].args[3], self.security["key"])

    def test_read_only_restart_guard_blocks_transient_resolution_but_preserves_persisted_choices(self):
        base = {"RoomId": self.plan["process"]["room"], "Transient": {"IsResolving": False, "HasPendingReactionWindow": False,
                              "MovingHeroIds": [], "HeroMovement": [],
                              "HasPendingRewardChoice": True, "HasPendingEncounterDeckControl": True}}
        with mock.patch.object(UPGRADE, "legacy_hello", return_value=base):
            self.actual_durable_phase(self.plan, self.security)
        for name, value in [("IsResolving", True), ("HasPendingReactionWindow", True), ("MovingHeroIds", ["Aldric"]), ("HeroMovement", [{"HeroId": "Aldric"}])]:
            snapshot = copy.deepcopy(base)
            snapshot["Transient"][name] = value
            with self.subTest(name=name), mock.patch.object(UPGRADE, "legacy_hello", return_value=snapshot):
                with self.assertRaisesRegex(UPGRADE.Refusal, "unresolved"):
                    self.actual_durable_phase(self.plan, self.security)

    def original_legacy_snapshot(self):
        # Original main 7f3c598 DTO has MovingHeroIds but no HeroMovement. Its
        # moving-ID predicate is identical to AscendanceSaveSystem.CanSave.
        return {"RoomId": self.plan["process"]["room"],
                "Transient": {"IsResolving": False, "HasPendingReactionWindow": False,
                              "MovingHeroIds": [], "HasPendingRewardChoice": True,
                              "HasPendingEncounterDeckControl": True}}

    def test_original_c_plain_hello_with_absent_detail_list_proves_durable_phase(self):
        snapshot = self.original_legacy_snapshot()
        stream = HealthWireCompatibility.Stream({"Snapshot": snapshot})
        with mock.patch.object(UPGRADE.socket, "create_connection", return_value=stream):
            self.actual_durable_phase(self.plan, self.security)
        self.assertEqual(stream.request["Operation"], "HELLO")
        self.assertEqual(stream.request["RoomId"], "ASC-PRIMARY")

    def test_original_dto_real_loopback_maintenance_enforces_build_and_moving_ids(self):
        for case in ["clear-original-c", "unknown-g", "missing-moving-ids"]:
            snapshot = self.original_legacy_snapshot()
            if case == "missing-moving-ids":
                del snapshot["Transient"]["MovingHeroIds"]
            build = "0.10.2g" if case == "unknown-g" else "0.10.2c"
            with self.subTest(case=case), OneFrameServer(snapshot, build) as peer:
                plan = copy.deepcopy(self.plan)
                plan["process"]["port"] = peer.port
                if case == "clear-original-c":
                    self.actual_durable_phase(plan, self.security)
                else:
                    message = "serverBuildVersion" if case == "unknown-g" else "unresolved"
                    with self.assertRaisesRegex(UPGRADE.Refusal, message):
                        self.actual_durable_phase(plan, self.security)
            self.assertEqual(peer.request["Operation"], "HELLO")
            self.assertEqual(peer.request["RoomId"], "ASC-PRIMARY")

    def test_original_c_missing_detail_does_not_waive_resolution_or_reaction_flags(self):
        for name in ["IsResolving", "HasPendingReactionWindow"]:
            for value in [True, None, 0, "false", "absent"]:
                snapshot = self.original_legacy_snapshot()
                if value == "absent":
                    del snapshot["Transient"][name]
                else:
                    snapshot["Transient"][name] = value
                with self.subTest(name=name, value=value), mock.patch.object(UPGRADE, "legacy_hello", return_value=snapshot):
                    with self.assertRaisesRegex(UPGRADE.Refusal, "unresolved"):
                        self.actual_durable_phase(self.plan, self.security)

    def test_original_c_missing_detail_requires_unambiguous_empty_moving_ids(self):
        for value in [None, {}, False, "", ["Aldric"], "absent"]:
            snapshot = self.original_legacy_snapshot()
            if value == "absent":
                del snapshot["Transient"]["MovingHeroIds"]
            else:
                snapshot["Transient"]["MovingHeroIds"] = value
            with self.subTest(value=value), mock.patch.object(UPGRADE, "legacy_hello", return_value=snapshot):
                with self.assertRaisesRegex(UPGRADE.Refusal, "unresolved"):
                    self.actual_durable_phase(self.plan, self.security)

    def test_present_legacy_detail_list_cannot_be_null_malformed_or_moving(self):
        for value in [None, {}, False, "", [{"HeroId": "Aldric"}]]:
            snapshot = self.original_legacy_snapshot()
            snapshot["Transient"]["HeroMovement"] = value
            with self.subTest(value=value), mock.patch.object(UPGRADE, "legacy_hello", return_value=snapshot):
                with self.assertRaisesRegex(UPGRADE.Refusal, "unresolved"):
                    self.actual_durable_phase(self.plan, self.security)

    def test_secure_target_cannot_omit_newer_movement_detail_list(self):
        plan = copy.deepcopy(self.plan)
        plan["process"]["executable"] = str(self.release / "build" / "AscendanceServer.x86_64")
        with mock.patch.object(UPGRADE, "hello", return_value=self.original_legacy_snapshot()) as secure, mock.patch.object(UPGRADE, "legacy_hello") as plain:
            with self.assertRaisesRegex(UPGRADE.Refusal, "unresolved"):
                self.actual_durable_phase(plan, self.security)
            secure.assert_called_once()
            plain.assert_not_called()

    def test_original_c_missing_detail_still_requires_exact_save_filename_identity(self):
        snapshot = self.original_legacy_snapshot()
        snapshot["RoomId"] = "asc-primary"
        with mock.patch.object(UPGRADE, "legacy_hello", return_value=snapshot):
            with self.assertRaisesRegex(UPGRADE.Refusal, "capitalization differs"):
                self.actual_durable_phase(self.plan, self.security)

    def test_original_c_movement_started_during_backup_still_prevents_service_stop(self):
        clear = self.original_legacy_snapshot()
        moving = copy.deepcopy(clear)
        moving["Transient"]["MovingHeroIds"] = ["Aldric"]
        with self.mock_plan(), mock.patch.object(UPGRADE, "require_durable_phase", side_effect=self.actual_durable_phase), mock.patch.object(UPGRADE, "legacy_hello", side_effect=[clear, moving]) as health, mock.patch.object(UPGRADE, "connected_clients", return_value=0), mock.patch.object(UPGRADE, "backup_existing", side_effect=self.fixture_backup) as backup, mock.patch.object(UPGRADE, "run") as system:
            with self.assertRaisesRegex(UPGRADE.Refusal, "unresolved"):
                self.activate()
            self.assertEqual(health.call_count, 2)
            backup.assert_called_once()
            system.assert_not_called()

    def test_original_detail_omission_does_not_allow_unknown_legacy_build(self):
        for version in ["0.10.2g", "0.10.2b"]:
            stream = HealthWireCompatibility.Stream({"ServerBuildVersion": version,
                                                   "Snapshot": self.original_legacy_snapshot()})
            with self.subTest(version=version), mock.patch.object(UPGRADE.socket, "create_connection", return_value=stream):
                with self.assertRaisesRegex(UPGRADE.Refusal, "serverBuildVersion"):
                    self.actual_durable_phase(self.plan, self.security)

    def reviewed_schema(self):
        return json.loads((ROOT / "Deploy/DigitalOcean/save-schema-0102g.json").read_text())

    def enable_g_migration(self):
        room = {"RoomId": "ASC-PRIMARY", "BuildVersion": "0.10.2g", "Revision": 77,
                "CreatedAtUtc": "2025-01-02T03:04:05Z", "UpdatedAtUtc": "2025-01-03T03:04:05Z",
                "Players": [{"PlayerId": "synthetic-player", "DisplayName": "PRIVATE-NAME",
                             "HeroId": "Aldric", "ReconnectToken": "PRIVATE-TOKEN", "Connected": False,
                             "JoinedAtUtc": "2025-01-02T03:05:00Z", "LastSeenUtc": "2025-01-03T03:05:00Z"}],
                "DurableCampaign": {"Version": "0.10.2g", "SavedAtUtc": "2025-01-03T03:04:05Z",
                                    "QuestId": "Q001", "PartyGold": 136,
                                    "Heroes": [{"Id": "Aldric", "Health": 7, "Level": 1}],
                                    "Enemies": [{"Id": "enemy-b", "Health": 3}, {"Id": "enemy-a", "Health": 4}],
                                    "PendingStateSchema0102f": 1, "HasPendingRewardChoice0102f": False,
                                    "HasPendingEncounterDeckControl0102f": False,
                                    "PendingRewardChoice": {"SourceCardId": "SD082", "Mode": "Gold25OrLoot1"},
                                    "PendingEncounterDeckControl": None}}
        self.room_file.write_text(json.dumps(room))
        self.room_file.chmod(0o600)
        self.plan["process"]["wireBuildVersion"] = "0.10.2g"
        self.plan["upgradeVerification"] = {"code": UPGRADE.tree_proof(self.legacy),
                                               "sourceIdentity": {"buildVersion": "0.10.2g", "transport": "plain",
                                                                  "managedAssemblySha256": UPGRADE.SOURCE_G_SHA256}}
        self.descriptor["buildVersion"] = "0.10.2h"
        self.descriptor["helpers"] = {UPGRADE.SCHEMA_NAME: {
            "sha256": hashlib.sha256((ROOT / "Deploy/DigitalOcean/save-schema-0102g.json").read_bytes()).hexdigest()}}
        self.security["buildVersion"] = "0.10.2h"
        return room

    def snapshot_for_room(self, room, restored=True, path=None):
        return {"RoomId": room["RoomId"], "BuildVersion": room["BuildVersion"], "Revision": room["Revision"],
                "Campaign": copy.deepcopy(room["DurableCampaign"]),
                "Players": [{key: player.get(key) for key in ("PlayerId", "DisplayName", "HeroId", "Connected")}
                            for player in room["Players"]],
                "Transient": {"IsResolving": False, "HasPendingReactionWindow": False, "MovingHeroIds": []},
                "RoomStateOrigin0102d": "RESTORED_FROM_SERVER_DISK" if restored else "FRESH_SERVER_PROCESS_STATE",
                "RoomCreatedAtUtc0102d": room["CreatedAtUtc"],
                "PersistencePath0102d": str(path or self.room_file)}

    def test_g_build_is_accepted_only_for_exact_reviewed_managed_code(self):
        code = {"files": {"AscendanceServer_Data/Managed/Assembly-CSharp.dll": {"sha256": UPGRADE.SOURCE_G_SHA256}}}
        identity = UPGRADE.reviewed_build_identity(code)
        self.assertEqual(identity["buildVersion"], "0.10.2g")
        code["files"]["AscendanceServer_Data/Managed/Assembly-CSharp.dll"]["sha256"] = "e" * 64
        with self.assertRaisesRegex(UPGRADE.Refusal, "not a reviewed legacy source"):
            UPGRADE.reviewed_build_identity(code)

    def test_deployed_h_upgrade_requires_its_exact_reviewed_managed_assembly(self):
        code = {"files": {"AscendanceServer_Data/Managed/Assembly-CSharp.dll": {"sha256": UPGRADE.SOURCE_H_SHA256}}}
        identity = UPGRADE.reviewed_build_identity(code, modern=True)
        self.assertEqual(identity, {"buildVersion": "0.10.2h", "managedAssemblySha256": UPGRADE.SOURCE_H_SHA256,
                                    "transport": "secure"})
        code["files"]["AscendanceServer_Data/Managed/Assembly-CSharp.dll"]["sha256"] = "e" * 64
        with self.assertRaisesRegex(UPGRADE.Refusal, "not a reviewed legacy source"):
            UPGRADE.reviewed_build_identity(code, modern=True)

    def test_modern_source_code_must_match_a_reviewed_or_exact_pinned_target_assembly(self):
        checksum = "d" * 64
        code = {"files": {"AscendanceServer_Data/Managed/Assembly-CSharp.dll": {"sha256": checksum}}}
        with self.assertRaisesRegex(UPGRADE.Refusal, "not a reviewed legacy source"):
            UPGRADE.reviewed_build_identity(code, modern=True)
        with self.assertRaisesRegex(UPGRADE.Refusal, "not a reviewed legacy source"):
            UPGRADE.reviewed_build_identity(code, modern=True, target_checksum="e" * 64)
        identity = UPGRADE.reviewed_build_identity(code, modern=True, target_checksum=checksum)
        self.assertEqual(identity["buildVersion"], "0.10.2h")
        self.assertEqual(identity["transport"], "secure")

    def test_reviewed_g_uses_g_wire_contract_instead_of_target_h_version(self):
        room = self.enable_g_migration()
        with OneFrameServer(self.snapshot_for_room(room), build="0.10.2g") as peer:
            plan = copy.deepcopy(self.plan)
            plan["process"]["port"] = peer.port
            self.actual_durable_phase(plan, self.security)
            self.assertEqual(peer.request["Operation"], "HELLO")
        with OneFrameServer(self.snapshot_for_room(room), build="0.10.2c") as peer:
            plan["process"]["port"] = peer.port
            with self.assertRaisesRegex(UPGRADE.Refusal, "serverBuildVersion"):
                self.actual_durable_phase(plan, self.security)

    def test_pinned_schema_checksum_cannot_be_replaced(self):
        self.enable_g_migration()
        descriptor = copy.deepcopy(self.descriptor)
        descriptor["helpers"][UPGRADE.SCHEMA_NAME]["sha256"] = "0" * 64
        with self.assertRaisesRegex(UPGRADE.Refusal, "checksum differs"):
            UPGRADE.load_save_schema(descriptor)

    def test_unknown_nested_field_or_mutated_scalar_refuses_before_backup_or_stop(self):
        for mutation in [lambda room: room["DurableCampaign"]["Heroes"][0].update(PRIVATE_UNKNOWN_FIELD=1),
                         lambda room: room["DurableCampaign"]["Heroes"][0].update(Health="7"),
                         lambda room: room["DurableCampaign"].update(PendingStateSchema0102f=2)]:
            room = self.enable_g_migration()
            mutation(room)
            self.room_file.write_text(json.dumps(room))
            with self.subTest(mutation=mutation), self.mock_plan(), mock.patch.object(UPGRADE, "connected_clients", return_value=0), mock.patch.object(UPGRADE, "require_durable_phase", return_value=self.snapshot_for_room(room)), mock.patch.object(UPGRADE, "backup_existing") as backup, mock.patch.object(UPGRADE, "private_restore_probe") as probe, mock.patch.object(UPGRADE, "run") as system:
                with self.assertRaises(UPGRADE.Refusal) as error:
                    self.activate()
                self.assertNotIn("PRIVATE", str(error.exception))
                backup.assert_not_called()
                probe.assert_not_called()
                system.assert_not_called()

    def test_live_and_disk_gold_mismatch_prevents_copy_probe_or_service_stop(self):
        room = self.enable_g_migration()
        snapshot = self.snapshot_for_room(room)
        snapshot["Campaign"]["PartyGold"] = 135
        with self.mock_plan(), mock.patch.object(UPGRADE, "connected_clients", return_value=0), mock.patch.object(UPGRADE, "require_durable_phase", return_value=snapshot), mock.patch.object(UPGRADE, "backup_existing") as backup, mock.patch.object(UPGRADE, "run") as system:
            with self.assertRaisesRegex(UPGRADE.Refusal, "Live campaign"):
                self.activate()
            backup.assert_not_called()
            system.assert_not_called()

    def test_live_g_wrong_persistence_path_or_creation_identity_prevents_backup_and_stop(self):
        for field, value in [("PersistencePath0102d", "/PRIVATE-DIFFERENT-UNITY-IDENTITY/ASC-PRIMARY.json"),
                             ("RoomCreatedAtUtc0102d", "PRIVATE-DIFFERENT-CREATION")]:
            room = self.enable_g_migration()
            snapshot = self.snapshot_for_room(room)
            snapshot[field] = value
            with self.subTest(field=field), self.mock_plan(), mock.patch.object(UPGRADE, "connected_clients", return_value=0), mock.patch.object(UPGRADE, "require_durable_phase", return_value=snapshot), mock.patch.object(UPGRADE, "backup_existing") as backup, mock.patch.object(UPGRADE, "private_restore_probe") as probe, mock.patch.object(UPGRADE, "run") as system:
                with self.assertRaisesRegex(UPGRADE.Refusal, "persistence path or room creation identity differs") as error:
                    self.activate()
                self.assertNotIn("PRIVATE", str(error.exception))
                backup.assert_not_called()
                probe.assert_not_called()
                system.assert_not_called()

    def test_valid_fresh_g_process_may_identify_its_existing_exact_persisted_room(self):
        room = self.enable_g_migration()
        snapshot = self.snapshot_for_room(room, restored=False)
        verified = UPGRADE.capture_migration_state(self.plan, snapshot, self.reviewed_schema())
        self.assertEqual(verified["CreatedAtUtc"], room["CreatedAtUtc"])
        self.assertEqual(verified["DurableCampaign"]["PartyGold"], 136)

    def test_failed_private_copy_restore_does_not_stop_production(self):
        room = self.enable_g_migration()
        before = self.room_file.read_bytes()
        with self.mock_plan(), mock.patch.object(UPGRADE, "connected_clients", return_value=0), mock.patch.object(UPGRADE, "require_durable_phase", return_value=self.snapshot_for_room(room)), mock.patch.object(UPGRADE, "backup_existing", side_effect=self.fixture_backup), mock.patch.object(UPGRADE, "private_restore_probe", side_effect=UPGRADE.Refusal("Private copy lost progress")) as probe, mock.patch.object(UPGRADE, "run") as system:
            with self.assertRaisesRegex(UPGRADE.Refusal, "Private copy lost progress"):
                self.activate()
            probe.assert_called_once()
            system.assert_not_called()
        self.assertEqual(self.room_file.read_bytes(), before)

    def test_movement_after_private_copy_inspection_still_prevents_stop(self):
        room = self.enable_g_migration()
        clear = self.snapshot_for_room(room)
        moving = copy.deepcopy(clear)
        moving["Transient"]["MovingHeroIds"] = ["Aldric"]
        with self.mock_plan(), mock.patch.object(UPGRADE, "connected_clients", return_value=0), mock.patch.object(UPGRADE, "require_durable_phase", side_effect=self.actual_durable_phase), mock.patch.object(UPGRADE, "legacy_hello", side_effect=[clear, clear, moving]), mock.patch.object(UPGRADE, "backup_existing", side_effect=self.fixture_backup), mock.patch.object(UPGRADE, "private_restore_probe", return_value={"verified": True}), mock.patch.object(UPGRADE, "run") as system:
            with self.assertRaisesRegex(UPGRADE.Refusal, "unresolved"):
                self.activate()
            system.assert_not_called()

    def test_semantic_migration_retains_gold_player_credentials_and_ordered_cards(self):
        room = self.enable_g_migration()
        expected = UPGRADE.semantic_room(room)
        after = copy.deepcopy(room)
        after.update(BuildVersion="0.10.2h", UpdatedAtUtc="new-reviewed-time", GameplayRevision=0)
        after["DurableCampaign"].update(Version="0.10.2h", SavedAtUtc="new-reviewed-time", PendingRewardChoice=None)
        after["DurableCampaign"]["Enemies"].reverse()
        self.assertEqual(UPGRADE.semantic_room(after), expected)
        for change in [lambda current: current["DurableCampaign"].update(PartyGold=135),
                       lambda current: current["Players"][0].update(ReconnectToken="DIFFERENT-PRIVATE-TOKEN"),
                       lambda current: current["DurableCampaign"]["Heroes"][0].update(Health=6)]:
            changed = copy.deepcopy(after)
            change(changed)
            self.assertNotEqual(UPGRADE.semantic_room(changed), expected)
        before = copy.deepcopy(room)
        before["DurableCampaign"]["Heroes"][0]["HandIds"] = ["CARD-A", "CARD-B"]
        after = copy.deepcopy(before)
        after["DurableCampaign"]["Heroes"][0]["HandIds"].reverse()
        self.assertNotEqual(UPGRADE.semantic_room(before), UPGRADE.semantic_room(after))

    def test_campaign_slot_loading_allows_only_declared_empty_defaults(self):
        room = self.enable_g_migration()
        room.update(BuildVersion="0.10.2h")
        room["DurableCampaign"]["Version"] = "0.10.2h"
        restored = copy.deepcopy(room)
        string_fields = ("CampaignId0102", "CampaignName0102", "CampaignOwnerToken0102",
                         "CampaignCreatorPlayerId0102", "LastCampaignActionId0102", "LastCampaignActionCode0102")
        for value in (None, ""):
            for field in string_fields:
                restored[field] = value
            restored["CampaignDraft0102"] = False
            restored["Players"][0]["ReadyForCampaign0102"] = False
            self.assertTrue(UPGRADE.rooms_equivalent(room, restored))
        for field in string_fields:
            changed = copy.deepcopy(restored)
            changed[field] = "NONDEFAULT-PRIVATE-METADATA"
            with self.subTest(field=field):
                self.assertFalse(UPGRADE.rooms_equivalent(room, changed))
        for change in (lambda value: value.update(CampaignDraft0102=True),
                       lambda value: value["Players"][0].update(ReadyForCampaign0102=True)):
            changed = copy.deepcopy(restored)
            change(changed)
            self.assertFalse(UPGRADE.rooms_equivalent(room, changed))

    def test_current_h_room_proves_private_restoration_without_g_source(self):
        room = self.enable_g_migration()
        room["BuildVersion"] = room["DurableCampaign"]["Version"] = "0.10.2h"
        self.plan["process"]["wireBuildVersion"] = "0.10.2h"
        self.room_file.write_text(json.dumps(room))
        schema = self.reviewed_schema()
        self.assertEqual(UPGRADE.capture_migration_state(self.plan, self.snapshot_for_room(room), schema), room)
        restored = copy.deepcopy(room)
        restored.update(CampaignId0102="", CampaignName0102=None, CampaignDraft0102=False,
                        CampaignOwnerToken0102=None, CampaignCreatorPlayerId0102="",
                        LastCampaignActionId0102="", LastCampaignActionCode0102="")
        restored["Players"][0]["ReadyForCampaign0102"] = False
        self.room_file.write_text(json.dumps(restored))
        UPGRADE.prove_restored_state(self.plan, self.snapshot_for_room(restored), room, schema, "0.10.2h")
        restored["CampaignOwnerToken0102"] = "NEW-PRIVATE-OWNER"
        self.room_file.write_text(json.dumps(restored))
        with self.assertRaisesRegex(UPGRADE.Refusal, "durable progress differs"):
            UPGRADE.prove_restored_state(self.plan, self.snapshot_for_room(restored), room, schema, "0.10.2h")

    def test_campaign_slot_schema_refuses_bad_types_and_unknown_fields(self):
        room = self.enable_g_migration()
        schema = self.reviewed_schema()
        for change in (lambda value: value.update(CampaignDraft0102="false"),
                       lambda value: value.update(CampaignOwnerToken0102=[]),
                       lambda value: value["Players"][0].update(ReadyForCampaign0102=1),
                       lambda value: value.update(UnknownCampaignOwner0102="PRIVATE")):
            changed = copy.deepcopy(room)
            change(changed)
            with self.subTest(change=change), self.assertRaises(UPGRADE.Refusal):
                UPGRADE.validate_serialized_value(changed, schema["rootType"], schema)

    def test_fresh_room_or_lost_gold_cannot_pass_post_start_restore_proof(self):
        expected = self.enable_g_migration()
        current = copy.deepcopy(expected)
        current["BuildVersion"] = current["DurableCampaign"]["Version"] = "0.10.2h"
        self.room_file.write_text(json.dumps(current))
        schema = self.reviewed_schema()
        UPGRADE.prove_restored_state(self.plan, self.snapshot_for_room(current), expected, schema, "0.10.2h")
        with self.assertRaisesRegex(UPGRADE.Refusal, "did not prove restoration"):
            UPGRADE.prove_restored_state(self.plan, self.snapshot_for_room(current, restored=False), expected, schema, "0.10.2h")
        current["DurableCampaign"]["PartyGold"] = 135
        self.room_file.write_text(json.dumps(current))
        with self.assertRaisesRegex(UPGRADE.Refusal, "durable progress differs"):
            UPGRADE.prove_restored_state(self.plan, self.snapshot_for_room(current), expected, schema, "0.10.2h")

    def test_false_presence_flag_semantics_do_not_erase_true_pending_reward(self):
        room = self.enable_g_migration()
        absent = UPGRADE.semantic_campaign(room["DurableCampaign"])
        room["DurableCampaign"]["HasPendingRewardChoice0102f"] = True
        present = UPGRADE.semantic_campaign(room["DurableCampaign"])
        self.assertIsNone(absent["PendingRewardChoice"])
        self.assertEqual(present["PendingRewardChoice"]["Mode"], "Gold25OrLoot1")
        self.assertNotEqual(absent, present)

    def test_enemy_reordering_does_not_allow_duplicate_identity_loss(self):
        room = self.enable_g_migration()
        room["DurableCampaign"]["Enemies"].append({"Id": "ENEMY-A", "Health": 4})
        with self.assertRaisesRegex(UPGRADE.Refusal, "duplicate enemy"):
            UPGRADE.semantic_room(room)

    def test_only_empty_enemy_selection_may_restore_to_first_living_local_enemy(self):
        expected = self.enable_g_migration()
        campaign = expected["DurableCampaign"]
        campaign["SelectedHeroId"] = "Aldric"
        campaign["SelectedEnemyId"] = None
        campaign["Heroes"][0]["LocationId"] = "L000"
        campaign["Enemies"] = [{"Id": "dead", "LocationId": "L000", "Health": 0},
                               {"Id": "elsewhere", "LocationId": "L001", "Health": 8},
                               {"Id": "first-local", "LocationId": "L000", "Health": 3},
                               {"Id": "second-local", "LocationId": "L000", "Health": 5}]
        restored = copy.deepcopy(expected)
        restored["DurableCampaign"]["SelectedEnemyId"] = "first-local"
        self.assertTrue(UPGRADE.rooms_equivalent(expected, restored))
        for wrong in ["dead", "elsewhere", "second-local", "missing-enemy"]:
            changed = copy.deepcopy(restored)
            changed["DurableCampaign"]["SelectedEnemyId"] = wrong
            with self.subTest(selection=wrong):
                self.assertFalse(UPGRADE.rooms_equivalent(expected, changed))
        expected["DurableCampaign"]["SelectedEnemyId"] = "second-local"
        self.assertFalse(UPGRADE.rooms_equivalent(expected, restored))
        restored["DurableCampaign"]["SelectedEnemyId"] = "second-local"
        self.assertTrue(UPGRADE.rooms_equivalent(expected, restored))

    def test_private_copy_probe_never_joins_or_changes_original_room(self):
        room = self.enable_g_migration()
        before = self.room_file.read_bytes()
        launches = []
        stopped = []
        def launch(command, **kwargs):
            launches.append((command, kwargs))
            return SimpleNamespace(pid=123456, poll=lambda: None)
        def system(command, **kwargs):
            port = command[-1].split(":", 1)[1].split()[0]
            return SimpleNamespace(stdout="LISTEN 0 128 127.0.0.1:" + port + " 0.0.0.0:*\n")
        def hello(port, requested_room, pin, key, **kwargs):
            environment = launches[0][1]["env"]
            path = Path(environment["XDG_CONFIG_HOME"]) / "unity3d/Ascendance/Ascendance/AscendanceServerRooms0100/ASC-PRIMARY.json"
            copied = json.loads(path.read_text())
            copied["BuildVersion"] = copied["DurableCampaign"]["Version"] = "0.10.2h"
            path.write_text(json.dumps(copied))
            self.assertEqual(kwargs["expected_build"], "0.10.2h")
            return self.snapshot_for_room(copied, path=path)
        with mock.patch.object(UPGRADE.subprocess, "Popen", side_effect=launch), mock.patch.object(UPGRADE, "run", side_effect=system), mock.patch.object(UPGRADE, "hello", side_effect=hello) as health, mock.patch.object(UPGRADE, "stop_private_probe", side_effect=lambda child: stopped.append(child.pid)):
            result = UPGRADE.private_restore_probe("ascendance", self.release, self.plan, self.security, room, self.reviewed_schema())
        self.assertTrue(result["verified"])
        self.assertEqual(result["gameplayCommandsSubmitted"], 0)
        self.assertEqual(health.call_count, 1)
        self.assertEqual(stopped, [123456])
        command, kwargs = launches[0]
        self.assertEqual(kwargs["env"]["ASCENDANCE_LISTEN_LOOPBACK"], "1")
        self.assertNotIn("HOME", kwargs["env"])
        self.assertNotIn(self.security["key"], " ".join(command))
        self.assertEqual(self.room_file.read_bytes(), before)

    def test_private_copy_probe_rejects_public_listener_before_any_room_query(self):
        room = self.enable_g_migration()
        def system(command, **kwargs):
            port = command[-1].split(":", 1)[1].split()[0]
            return SimpleNamespace(stdout="LISTEN 0 128 0.0.0.0:" + port + " 0.0.0.0:*\n")
        child = SimpleNamespace(pid=123456, poll=lambda: None)
        with mock.patch.object(UPGRADE.subprocess, "Popen", return_value=child), mock.patch.object(UPGRADE, "run", side_effect=system), mock.patch.object(UPGRADE, "hello") as health, mock.patch.object(UPGRADE, "stop_private_probe") as stop:
            with self.assertRaisesRegex(UPGRADE.Refusal, "loopback-only"):
                UPGRADE.private_restore_probe("ascendance", self.release, self.plan, self.security, room, self.reviewed_schema())
            health.assert_not_called()
            stop.assert_called_once_with(child)

    def test_g_recovery_health_and_h_target_health_expect_different_builds(self):
        self.enable_g_migration()
        inspector = SimpleNamespace(inspect=lambda service: copy.deepcopy(self.plan))
        with mock.patch.object(UPGRADE, "legacy_hello", return_value={}) as legacy:
            UPGRADE.healthy("ascendance", self.release, self.plan, None, inspector,
                            timeout=1, expected_executable=str(self.binary))
        legacy.assert_called_once_with(7777, "ASC-PRIMARY", expected_build="0.10.2g")
        current = copy.deepcopy(self.plan)
        current["process"]["executable"] = str(self.release / "build/AscendanceServer.x86_64")
        current["process"]["executableSha256"] = hashlib.sha256((self.release / "build/AscendanceServer.x86_64").read_bytes()).hexdigest()
        (self.release / "manifest.json").write_text(json.dumps({"files": {
            "AscendanceServer.x86_64": current["process"]["executableSha256"]}}))
        inspector = SimpleNamespace(inspect=lambda service: copy.deepcopy(current))
        with mock.patch.object(UPGRADE, "hello", return_value={}) as secure:
            UPGRADE.healthy("ascendance", self.release, self.plan, self.security, inspector, timeout=1)
        self.assertEqual(len(secure.call_args_list), 3)
        self.assertTrue(all(call.kwargs["expected_build"] == "0.10.2h" for call in secure.call_args_list))

    def test_g_rollback_retains_current_progress_after_target_transport_failure(self):
        room = self.enable_g_migration()
        starts = []
        def system(command, **kwargs):
            if command[1] == "start":
                starts.append(command)
                current = json.loads(self.room_file.read_text())
                current["BuildVersion"] = current["DurableCampaign"]["Version"] = "0.10.2h" if len(starts) == 1 else "0.10.2g"
                current.setdefault("GameplayRevision", 0)
                self.room_file.write_text(json.dumps(current))
            return SimpleNamespace(stdout="inactive\n" if "--property=ActiveState" in command else "")
        with self.mock_plan(), mock.patch.object(UPGRADE, "connected_clients", return_value=0), mock.patch.object(UPGRADE, "require_durable_phase", return_value=self.snapshot_for_room(room)), mock.patch.object(UPGRADE, "backup_existing", side_effect=self.fixture_backup), mock.patch.object(UPGRADE, "private_restore_probe", return_value={"verified": True}), mock.patch.object(UPGRADE, "secure_directory", side_effect=self.fake_directory), mock.patch.object(UPGRADE, "run", side_effect=system), mock.patch.object(UPGRADE, "healthy", side_effect=[UPGRADE.Refusal("Target transport unavailable"), None]) as health:
            with self.assertRaisesRegex(UPGRADE.Refusal, "previous service configuration/code was restored"):
                self.activate()
        self.assertEqual(len(starts), 2)
        recovery = health.call_args_list[1]
        self.assertIsNone(recovery.args[3])
        self.assertEqual(recovery.args[2]["process"]["wireBuildVersion"], "0.10.2g")
        self.assertEqual(recovery.kwargs["expected_executable"], str(self.binary))
        self.assertEqual(recovery.kwargs["expected_room"]["DurableCampaign"]["PartyGold"], 136)
        self.assertEqual(json.loads(self.room_file.read_text())["DurableCampaign"]["PartyGold"], 136)

    def test_lost_target_progress_blocks_unsafe_legacy_restart_and_keeps_verified_backup(self):
        room = self.enable_g_migration()
        starts = []
        def system(command, **kwargs):
            if command[1] == "start":
                starts.append(command)
                current = json.loads(self.room_file.read_text())
                current["BuildVersion"] = current["DurableCampaign"]["Version"] = "0.10.2h"
                current["DurableCampaign"]["PartyGold"] = 135
                self.room_file.write_text(json.dumps(current))
            return SimpleNamespace(stdout="inactive\n" if "--property=ActiveState" in command else "")
        with self.mock_plan(), mock.patch.object(UPGRADE, "connected_clients", return_value=0), mock.patch.object(UPGRADE, "require_durable_phase", return_value=self.snapshot_for_room(room)), mock.patch.object(UPGRADE, "backup_existing", side_effect=self.fixture_backup), mock.patch.object(UPGRADE, "private_restore_probe", return_value={"verified": True}), mock.patch.object(UPGRADE, "secure_directory", side_effect=self.fake_directory), mock.patch.object(UPGRADE, "run", side_effect=system), mock.patch.object(UPGRADE, "healthy", side_effect=UPGRADE.Refusal("Target campaign lost progress")):
            with self.assertRaisesRegex(UPGRADE.RecoveryFailure, "both failed") as error:
                self.activate()
        self.assertNotIn("PRIVATE", str(error.exception))
        self.assertEqual(len(starts), 1)  # Never let old g load/rewrite an unverified new save.
        self.assertEqual(json.loads(self.room_file.read_text())["DurableCampaign"]["PartyGold"], 135)
        backup = self.backups / "verified-backup/data-after-stop/AscendanceServerRooms0100/ASC-PRIMARY.json"
        self.assertEqual(json.loads(backup.read_text())["DurableCampaign"]["PartyGold"], 136)

    def test_pre_stop_guard_separates_exact_filename_identity_from_plain_and_tls_health(self):
        exact = {"RoomId": "ASC-PRIMARY", "Transient": {"IsResolving": False, "HasPendingReactionWindow": False,
                                                         "MovingHeroIds": [], "HeroMovement": []}}
        drift = copy.deepcopy(exact)
        drift["RoomId"] = "asc-primary"
        for transport in ["plain", "tls"]:
            plan = copy.deepcopy(self.plan)
            if transport == "tls":
                plan["process"]["executable"] = str(self.release / "build" / "AscendanceServer.x86_64")
            for snapshot in [exact, drift]:
                with self.subTest(transport=transport, exact=snapshot is exact), mock.patch.object(UPGRADE, "legacy_hello", return_value=snapshot), mock.patch.object(UPGRADE, "hello", return_value=snapshot):
                    if snapshot is exact:
                        self.actual_durable_phase(plan, self.security)
                    else:
                        with self.assertRaisesRegex(UPGRADE.Refusal, "capitalization differs; activation not started"):
                            self.actual_durable_phase(plan, self.security)

    def test_live_room_case_drift_refuses_before_any_backup_or_stop(self):
        drift = {"RoomId": "asc-primary", "Transient": {"IsResolving": False, "HasPendingReactionWindow": False,
                                                         "MovingHeroIds": [], "HeroMovement": []}}
        with self.mock_plan(), mock.patch.object(UPGRADE, "require_durable_phase", side_effect=self.actual_durable_phase), mock.patch.object(UPGRADE, "legacy_hello", return_value=drift), mock.patch.object(UPGRADE, "connected_clients", return_value=0), mock.patch.object(UPGRADE, "backup_existing") as backup, mock.patch.object(UPGRADE, "run") as system:
            with self.assertRaisesRegex(UPGRADE.Refusal, "keep service running until the active save path is verified"):
                self.activate()
            backup.assert_not_called()
            system.assert_not_called()

    def test_room_case_drift_at_second_phase_check_still_refuses_before_stop(self):
        exact = {"RoomId": "ASC-PRIMARY", "Transient": {"IsResolving": False, "HasPendingReactionWindow": False,
                                                         "MovingHeroIds": [], "HeroMovement": []}}
        drift = copy.deepcopy(exact)
        drift["RoomId"] = "asc-primary"
        with self.mock_plan(), mock.patch.object(UPGRADE, "require_durable_phase", side_effect=self.actual_durable_phase), mock.patch.object(UPGRADE, "legacy_hello", side_effect=[exact, drift]) as health, mock.patch.object(UPGRADE, "connected_clients", return_value=0), mock.patch.object(UPGRADE, "backup_existing", side_effect=self.fixture_backup) as backup, mock.patch.object(UPGRADE, "run") as system:
            with self.assertRaisesRegex(UPGRADE.Refusal, "capitalization differs; activation not started"):
                self.activate()
            self.assertEqual(health.call_count, 2)
            backup.assert_called_once()
            system.assert_not_called()

    def test_archive_traversal_is_rejected_before_writing_build(self):
        archive = self.folder / "unsafe.zip"
        with zipfile.ZipFile(archive, "w") as output:
            output.writestr("LinuxServer/../../outside", "bad")
        destination = self.folder / "build"
        with self.assertRaisesRegex(UPGRADE.Refusal, "Unsafe server archive path"):
            UPGRADE.extract_build(archive, destination, {}, RELEASE)
        self.assertFalse(destination.exists())
        self.assertFalse((self.folder.parent / "outside").exists())

    def test_artifact_directories_allow_service_read_under_restrictive_umask(self):
        archive = self.folder / "synthetic.zip"
        elf = b"\x7fELF\x02\x01" + b"\0" * 12 + b"\x3e\x00" + b"synthetic-not-executable"
        files = {"AscendanceServer.x86_64": elf, "UnityPlayer.so": elf, "AscendanceServer_Data/file": b"data"}
        manifest = {"format": 1, "files": {name: hashlib.sha256(data).hexdigest() for name, data in files.items()}}
        with zipfile.ZipFile(archive, "w") as output:
            for name, data in files.items():
                output.writestr("LinuxServer/" + name, data)
        previous = os.umask(0o077)
        try:
            destination = self.folder / "extracted"
            UPGRADE.extract_build(archive, destination, manifest, RELEASE)
        finally:
            os.umask(previous)
        self.assertEqual(stat.S_IMODE(destination.stat().st_mode), 0o755)
        self.assertEqual(stat.S_IMODE((destination / "AscendanceServer_Data").stat().st_mode), 0o755)
        self.assertEqual(stat.S_IMODE((destination / "AscendanceServer.x86_64").stat().st_mode), 0o755)

    @unittest.skipUnless(shutil.which("openssl"), "OpenSSL is required for real certificate fixture")
    def test_generated_transport_files_use_real_certificate_and_compatible_private_pfx(self):
        def directory(path, mode=0o700, gid=0):
            path.mkdir(parents=True, mode=mode)
            path.chmod(mode)
        with mock.patch.object(UPGRADE, "secure_directory", side_effect=directory), mock.patch.object(UPGRADE.os, "chown", lambda *args: None):
            security = UPGRADE.secure_configuration("ascendance", self.plan["process"], "159.65.100.206", INSPECTOR)
        cert_bytes = ssl.PEM_cert_to_DER_cert((self.config / "server.crt").read_text())
        self.assertEqual(security["pin"], hashlib.sha256(cert_bytes).hexdigest())
        self.assertEqual(len(security["key"]), 64)
        for name in ["server.pfx", "pfx-password", "room-access-key", "server.crt"]:
            self.assertEqual(stat.S_IMODE((self.config / name).stat().st_mode), 0o640)
        self.assertFalse((self.config / "temporary-key.pem").exists())
        result = subprocess.run(["openssl", "pkcs12", "-in", str(self.config / "server.pfx"), "-passin", "file:" + str(self.config / "pfx-password"), "-noout"], capture_output=True)
        self.assertEqual(result.returncode, 0, "The generated PFX must be readable with its private password file")
        san = subprocess.check_output(["openssl", "x509", "-in", str(self.config / "server.crt"), "-noout", "-ext", "subjectAltName"], text=True)
        self.assertIn("IP Address:159.65.100.206", san)
        self.assertIn("IP Address:127.0.0.1", san)
        self.assertIn("DNS:localhost", san)

    def test_wrong_certificate_pin_prevents_sending_any_access_key(self):
        class Stream:
            sent = []
            def __enter__(self): return self
            def __exit__(self, *args): pass
            def settimeout(self, value): pass
            def getpeercert(self, binary_form=False): return b"wrong certificate"
            def sendall(self, data): self.sent.append(data)
        stream = Stream()
        context = SimpleNamespace(wrap_socket=lambda *args, **kwargs: stream)
        with mock.patch.object(UPGRADE.socket, "create_connection", return_value=stream), mock.patch.object(UPGRADE.ssl, "SSLContext", return_value=context):
            with self.assertRaisesRegex(UPGRADE.Refusal, "certificate differs"):
                UPGRADE.hello(7777, "ASC-PRIMARY", "a" * 64, "b" * 64)
        self.assertEqual(stream.sent, [])


@unittest.skipUnless(os.name == "posix", "Linux deployment wire compatibility")
class HealthWireCompatibility(unittest.TestCase):
    # Actual framed request/response bytes exercise the production helper; no
    # service, network connection, account or deployment is involved.
    class Stream:
        def __init__(self, overrides=None, actual_room="ASC-PRIMARY"):
            self.overrides = overrides or {}
            self.actual_room = actual_room
            self.pending = b""
            self.request = None
        def __enter__(self): return self
        def __exit__(self, *args): pass
        def settimeout(self, value): pass
        def getpeercert(self, binary_form=False): return b"public fixture certificate"
        def sendall(self, frame):
            self.request = json.loads(frame[4:])
            response = {"ProtocolVersion": 1, "RequestId": self.request["RequestId"],
                        "Accepted": True, "Code": "HELLO", "ServerBuildVersion": "0.10.2c",
                        "Message": "PRIVATE-RESPONSE-MESSAGE",
                        "Player": {"ReconnectToken": "PRIVATE-RECONNECT-PROOF"},
                        "Snapshot": {"RoomId": self.actual_room,
                                     "Campaign": {"PrivateFixture": "PRIVATE-CAMPAIGN-DATA"},
                                     "Transient": {"RecentLog": ["PRIVATE-PLAYER-LOG"]}}}
            response.update(self.overrides)
            body = json.dumps(response).encode()
            self.pending = struct.pack("!I", len(body)) + body
        def recv(self, size):
            part, self.pending = self.pending[:size], self.pending[size:]
            return part

    def invoke(self, transport, overrides=None, room="asc-primary", actual_room="ASC-PRIMARY", expect_denied=False):
        stream = self.Stream(overrides, actual_room)
        context = SimpleNamespace(wrap_socket=lambda *args, **kwargs: stream)
        with mock.patch.object(UPGRADE.socket, "create_connection", return_value=stream), mock.patch.object(UPGRADE.ssl, "SSLContext", return_value=context):
            if transport == "plain":
                result = UPGRADE.legacy_hello(17777, room)
            else:
                result = UPGRADE.hello(17777, room, hashlib.sha256(stream.getpeercert()).hexdigest(), "PRIVATE-ACCESS-KEY", expect_denied=expect_denied)
        return result, stream.request

    def test_case_variants_match_native_ascii_room_routing_for_plain_and_tls(self):
        for transport in ["plain", "tls"]:
            with self.subTest(transport=transport):
                snapshot, request = self.invoke(transport)
                self.assertEqual(snapshot["RoomId"], "ASC-PRIMARY")
                self.assertEqual(request["RoomId"], "asc-primary")
                self.assertEqual(request["Operation"], "HELLO")
                self.assertNotIn("ClientBuildVersion", request)
                self.assertNotIn("ReconnectToken", request)

    def test_wire_identity_failures_keep_all_guards_and_print_public_metadata_only(self):
        cases = [("correlation", {"RequestId": "wrong-correlation"}, "requestIdMatches"),
                 ("wrong-room", {"Snapshot": {"RoomId": "OTHER-ROOM"}}, "roomMatches"),
                 ("wrong-build", {"ServerBuildVersion": "0.10.2b"}, "serverBuildVersion"),
                 ("wrong-protocol", {"ProtocolVersion": 2}, "protocolVersion"),
                 ("missing-protocol", {"ProtocolVersion": None}, "protocolVersion"),
                 ("boolean-protocol", {"ProtocolVersion": True}, "protocolVersion"),
                 ("rejected", {"Accepted": False}, "accepted"),
                 ("wrong-code", {"Code": "PONG"}, "code"),
                 ("invalid-code", {"Code": "DO-NOT-ECHO-THIS\n"}, "<invalid>")]
        for transport in ["plain", "tls"]:
            for name, overrides, expected_label in cases:
                with self.subTest(transport=transport, case=name):
                    with self.assertRaises(UPGRADE.Refusal) as caught:
                        self.invoke(transport, overrides)
                    error = str(caught.exception)
                    self.assertIn(expected_label, error)
                    for private in ["PRIVATE-RESPONSE-MESSAGE", "PRIVATE-RECONNECT-PROOF", "PRIVATE-CAMPAIGN-DATA", "PRIVATE-PLAYER-LOG", "PRIVATE-ACCESS-KEY", "DO-NOT-ECHO-THIS"]:
                        self.assertNotIn(private, error)

    def test_nonascii_or_padded_room_ids_are_not_normalized_into_matches(self):
        for room in ["ASC-PRIMAR\u0131", " ASC-PRIMARY ", "ASC-PRIMARY/OTHER"]:
            for transport in ["plain", "tls"]:
                with self.subTest(room=room, transport=transport):
                    with self.assertRaisesRegex(UPGRADE.Refusal, "roomMatches"):
                        self.invoke(transport, actual_room=room)

    def test_denied_tls_response_still_requires_protocol_correlation_and_no_state(self):
        denied = {"Accepted": False, "Code": "ROOM_ACCESS_DENIED", "Player": None, "Snapshot": None, "CommandResult": None}
        self.assertIsNone(self.invoke("tls", denied, expect_denied=True)[0])
        for failure in [{"ProtocolVersion": 2}, {"RequestId": "wrong"}, {"Player": {"ReconnectToken": "PRIVATE-RECONNECT-PROOF"}}]:
            with self.subTest(failure=list(failure)):
                with self.assertRaises(UPGRADE.Refusal) as caught:
                    self.invoke("tls", {**denied, **failure}, expect_denied=True)
                self.assertNotIn("PRIVATE-RECONNECT-PROOF", str(caught.exception))


if __name__ == "__main__":
    unittest.main()
