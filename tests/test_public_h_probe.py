import copy
import importlib.util
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location("public_h_probe", ROOT / "tools/verify_public_h_server.py")
PROBE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PROBE)


class PublicHProbe(unittest.TestCase):
    def response(self):
        return {"ProtocolVersion": 1, "RequestId": "fixture", "ServerBuildVersion": "0.10.2h",
                "Accepted": False, "Code": "ROOM_ACCESS_DENIED", "Snapshot": None,
                "Player": None, "CommandResult": None}

    def test_missing_key_denial_requires_all_protocol_and_privacy_guards(self):
        PROBE.verify_response(self.response(), "fixture")
        for name, value in [("ProtocolVersion", True), ("RequestId", "other"),
                            ("ServerBuildVersion", "0.10.2g"), ("Accepted", True),
                            ("Code", "HELLO"), ("Snapshot", {"RoomId": "PRIVATE"}),
                            ("Player", {"ReconnectToken": "PRIVATE"})]:
            data = self.response();data[name] = value
            with self.subTest(name=name), self.assertRaises(PROBE.Refusal) as error:
                PROBE.verify_response(data, "fixture")
            self.assertNotIn("PRIVATE", str(error.exception))

    def test_duplicate_response_fields_fail_instead_of_hiding_authority(self):
        with self.assertRaises(PROBE.Refusal):
            json.loads('{"Accepted":true,"Accepted":false}', object_pairs_hook=PROBE.unique_object)

    def test_wrong_certificate_is_refused_before_any_decode_or_request(self):
        with self.assertRaisesRegex(PROBE.Refusal, "activation pin"):
            PROBE.verify_certificate(b"untrusted certificate")


if __name__ == "__main__":
    unittest.main()
