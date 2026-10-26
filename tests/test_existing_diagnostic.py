"""Read-only diagnostic redaction and wire fixtures; no real service/game calls."""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import struct
import tempfile
from types import SimpleNamespace
import unittest
from unittest import mock


ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location("diagnose_existing", ROOT / "Deploy/DigitalOcean/diagnose-existing.py")
DIAGNOSTIC = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(DIAGNOSTIC)


class ExistingDiagnostic(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="asc-public-diagnostic-fixture-")
        self.addCleanup(self.temporary.cleanup)
        self.folder = Path(self.temporary.name)

    def test_routing_distinguishes_explicit_room_argument_from_environment_fallback(self):
        environment = {"ASCENDANCE_ROOM": "ASC-PRIMARY", "ASCENDANCE_PORT": "7777"}
        explicit = DIAGNOSTIC.routing(["server", "-room", "REAL-CLI-ROOM", "-port", "7777"], environment)
        self.assertTrue(explicit["commandLineRoomProvided"])
        self.assertEqual(explicit["commandLineRoom"], "REAL-CLI-ROOM")
        self.assertEqual(explicit["environmentRoom"], "ASC-PRIMARY")
        self.assertEqual(explicit["selectedExpectedRoom"], "REAL-CLI-ROOM")
        fallback = DIAGNOSTIC.routing(["server"], environment)
        self.assertFalse(fallback["commandLineRoomProvided"])
        self.assertEqual(fallback["roomSource"], "environment")
        self.assertEqual(fallback["selectedExpectedRoom"], "ASC-PRIMARY")
        self.assertEqual(DIAGNOSTIC.routing(["server"], {})["selectedExpectedRoom"], "LOCAL-ROOM")

    def test_hello_summary_retains_each_identity_predicate_but_redacts_private_state(self):
        request = {"RequestId": "safe-correlation", "RoomId": "ASC-PRIMARY"}
        response = {"Accepted": True, "Code": "HELLO", "ProtocolVersion": 1,
                    "ServerBuildVersion": "other-build", "RequestId": "wrong-correlation",
                    "Message": "PRIVATE-RESPONSE-MESSAGE", "Player": {"ReconnectToken": "PRIVATE-TOKEN"},
                    "Snapshot": {"RoomId": "DIFFERENT-ROOM", "Players": [{"DisplayName": "PRIVATE-NAME"}],
                                 "Campaign": {"PartyGold": 777, "Heroes": ["PRIVATE-HERO"]},
                                 "Transient": {"IsResolving": True, "HasPendingReactionWindow": False,
                                               "MovingHeroIds": ["PRIVATE-HERO"],
                                               "ReactionOptions": ["PRIVATE-CARD"], "RecentLog": ["PRIVATE-LOG"]}}}
        result = DIAGNOSTIC.summarize_hello(response, request)
        encoded = json.dumps(result)
        self.assertNotIn("PRIVATE", encoded)
        self.assertNotIn("777", encoded)
        self.assertEqual(result["snapshotRoomId"], "DIFFERENT-ROOM")
        self.assertFalse(result["snapshotRoomMatchesExpected"])
        self.assertFalse(result["buildMatchesExpected"])
        self.assertFalse(result["requestIdMatches"])
        self.assertEqual(result["transient"]["MovingHeroIdsCount"], 1)
        self.assertTrue(result["transient"]["IsResolving"])

    def test_one_minimal_hello_request_has_no_player_auth_or_mutating_operation(self):
        class Stream:
            def __enter__(self): return self
            def __exit__(self, *args): pass
            def settimeout(self, timeout): pass
            def sendall(self, data):
                self.request = json.loads(data[4:])
                response = {"Accepted": True, "Code": "HELLO", "ProtocolVersion": 1,
                            "RequestId": self.request["RequestId"], "ServerBuildVersion": "0.10.2c",
                            "Snapshot": {"RoomId": self.request["RoomId"]}}
                body = json.dumps(response).encode()
                self.data = struct.pack("!I", len(body)) + body
            def recv(self, size):
                chunk, self.data = self.data[:size], self.data[size:]
                return chunk
        stream = Stream()
        with mock.patch.object(DIAGNOSTIC.socket, "create_connection", return_value=stream) as connect:
            result = DIAGNOSTIC.hello("ASC-PRIMARY", 7777)
        connect.assert_called_once_with(("127.0.0.1", 7777), timeout=4)
        self.assertEqual(set(stream.request), {"ProtocolVersion", "RequestId", "Operation", "RoomId"})
        self.assertEqual(stream.request["Operation"], "HELLO")
        self.assertTrue(result["requestIdMatches"])
        self.assertTrue(result["buildMatchesExpected"])

    def test_case_only_room_difference_is_visible_without_claiming_an_exact_match(self):
        response = {"Accepted": True, "Code": "HELLO", "Snapshot": {"RoomId": "asc-primary"}}
        result = DIAGNOSTIC.summarize_hello(response, {"RequestId": "public-id", "RoomId": "ASC-PRIMARY"})
        self.assertEqual(result["expectedRoomId"], "ASC-PRIMARY")
        self.assertEqual(result["snapshotRoomId"], "asc-primary")
        self.assertFalse(result["snapshotRoomMatchesExpected"])
        self.assertTrue(result["snapshotRoomMatchesAsciiCaseInsensitive"])

    def test_unrecognized_port_never_opens_a_connection(self):
        with mock.patch.object(DIAGNOSTIC.socket, "create_connection") as connect:
            self.assertFalse(DIAGNOSTIC.hello("ASC-PRIMARY", 22)["responseReceived"])
            connect.assert_not_called()

    @unittest.skipUnless(os.name == "posix", "Linux proc metadata fixture uses symlinks")
    def test_process_metadata_hashes_actual_game_assembly_without_exposing_arguments_or_environment(self):
        code = self.folder / "code"
        (code / "AscendanceServer_Data/Managed").mkdir(parents=True)
        binary = code / "AscendanceServer.x86_64"
        binary.write_bytes(b"public synthetic Unity stub")
        assembly = code / "AscendanceServer_Data/Managed/Assembly-CSharp.dll"
        assembly.write_bytes(b"PRIVATE-ASSEMBLY-CONTENT")
        (code / "UnityPlayer.so").write_bytes(b"public synthetic player library")
        proc = self.folder / "proc"
        process = proc / "123"
        process.mkdir(parents=True)
        (process / "exe").symlink_to(binary)
        (process / "cwd").symlink_to(code, target_is_directory=True)
        (process / "cmdline").write_bytes(b"\0".join([b"server", b"-room", b"ASC-PRIMARY", b"-port", b"7777", b"-secret", b"PRIVATE-ARGUMENT"]) + b"\0")
        (process / "environ").write_bytes(b"ASCENDANCE_ROOM=OTHER-ENV-ROOM\0ASCENDANCE_PORT=7777\0PRIVATE_KEY=PRIVATE-ENVIRONMENT\0")
        properties = "ActiveState=active\nSubState=running\nMainPID=123\nUser=ascendance\nExecStart=PRIVATE-SYSTEMD-COMMAND\n"
        calls = []
        def run(arguments, **kwargs):
            calls.append(arguments)
            return SimpleNamespace(stdout=properties)
        result = DIAGNOSTIC.service_metadata(proc, run)
        encoded = json.dumps(result)
        self.assertNotIn("PRIVATE", encoded)
        self.assertNotIn("ExecStart", encoded)
        self.assertFalse(any("ExecStart" in item for item in calls[0]))
        self.assertEqual(result["process"]["managedAssembly"]["sha256"], hashlib.sha256(assembly.read_bytes()).hexdigest())
        self.assertEqual(result["process"]["routing"]["roomSource"], "commandLine")
        self.assertTrue(result["process"]["routing"]["commandLinePortProvided"])
        self.assertEqual(result["process"]["routing"]["selectedExpectedPort"], 7777)

    def test_latest_canary_emits_only_public_verdict_counts_and_identity(self):
        for directory, passed in [("canary-older", 70), ("canary-newer", 80)]:
            path = self.folder / directory / "result.json"
            path.parent.mkdir()
            path.write_text(json.dumps({"status": "PASS", "passed": passed, "failed": 0,
                                        "sourceRevision": "a" * 40,
                                        "nativeRoomSaveWasUnderIsolatedXdgDirectory": True,
                                        "checks": [{"actual": "PRIVATE-GAMEPLAY-FIXTURE"}],
                                        "logs": ["PRIVATE-LOG-CONTENTS"], "accessKey": "PRIVATE-KEY"}))
            os.utime(path, (100 if passed == 70 else 200, 100 if passed == 70 else 200))
        result = DIAGNOSTIC.latest_canary(self.folder)
        self.assertEqual(result["passed"], 80)
        self.assertEqual(result["failed"], 0)
        self.assertTrue(result["isolatedNativeRoomSave"])
        self.assertNotIn("PRIVATE", json.dumps(result))

    @unittest.skipUnless(os.name == "posix", "Symlink refusal uses Linux filesystem semantics")
    def test_symlinked_canary_result_is_not_read(self):
        private = self.folder / "private-file"
        private.write_text('{"status":"PASS","passed":999}')
        canary = self.folder / "canary-linked"
        canary.mkdir()
        (canary / "result.json").symlink_to(private)
        self.assertEqual(DIAGNOSTIC.latest_canary(self.folder), {"available": False})


if __name__ == "__main__":
    unittest.main()
