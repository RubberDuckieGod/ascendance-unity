"""Fail-closed native combat report projections; no game, saved data or network."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import sys
import tempfile
import unittest
import zipfile

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))
SPEC = importlib.util.spec_from_file_location("native_combat_acceptance", ROOT / "tools/native_combat_acceptance.py")
COMBAT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(COMBAT)


class NativeCombatObservationTests(unittest.TestCase):
    def fixture(self):
        hero = {"Id": "Aldric", "Health": 8, "Actions": 0, "LocationId": "L000", "TurnCompleted": False,
                "Downed": False, "Dead": False}
        objects = [{"ObjectId": "Q001_VILLAGER_" + str(i), "Type": "Villager", "LocationId": "L000",
                    "Rescued": i == 1, "Destroyed": i == 2} for i in range(1, 5)]
        campaign = {"Round": 2, "QuestFailed": False, "QuestComplete": False, "VillagersRescued": 1,
                    "PartyHeroIds": ["Aldric"], "Heroes": [hero, {**hero, "Id": "Rowan", "Health": 9}],
                    "QuestRuntime0829": {"Objects": objects},
                    "Enemies": [{"Id": "GoblinA", "Health": 3, "Armor": 1, "LocationId": "L000"}]}
        snapshot = {"Campaign": campaign, "Transient": {"HasPendingReactionWindow": False, "IsResolving": False}}
        state = {"Round": 2, "QuestFailed": False, "QuestComplete": False, "VillagersRescued": 1,
                 "VillagersAvailable": 2, "VillagersLost": 1, "Heroes": [hero], "Enemies": campaign["Enemies"],
                 "QuestObjects": [{"Id": obj["ObjectId"], **{k: obj[k] for k in ["Type", "LocationId", "Rescued", "Destroyed"]}}
                                  for obj in objects], "PendingReaction": False, "IsResolving": False}
        return snapshot, state

    def test_authority_projection_matches_exact_observed_party_and_tokens(self):
        snapshot, state = self.fixture()
        self.assertEqual(COMBAT.native_campaign_view(snapshot), COMBAT.combat_view(state))
        self.assertEqual(COMBAT.native_campaign_view(snapshot)["villagers"], [1, 2, 1])

    def test_peer_damage_and_villager_loss_cannot_be_hidden_by_projection(self):
        snapshot, state = self.fixture()
        expected = COMBAT.native_campaign_view(snapshot)
        for mutate in [lambda s: s["Heroes"][0].update(Health=9),
                       lambda s: s["QuestObjects"][2].update(Destroyed=True),
                       lambda s: s.update(VillagersLost=2),
                       lambda s: s.update(QuestFailed=True),
                       lambda s: s.update(PendingReaction=True),
                       lambda s: s.update(IsResolving=True)]:
            candidate = copy.deepcopy(state)
            mutate(candidate)
            self.assertNotEqual(expected, COMBAT.combat_view(candidate))

    def test_missing_gameplay_observations_raise_instead_of_becoming_default_passes(self):
        snapshot, state = self.fixture()
        for name in ["QuestObjects", "PendingReaction", "VillagersLost", "IsResolving", "Heroes"]:
            broken = copy.deepcopy(state)
            del broken[name]
            with self.subTest(name=name), self.assertRaises(KeyError):
                COMBAT.combat_view(broken)
        del snapshot["Campaign"]["QuestRuntime0829"]
        with self.assertRaises(KeyError): COMBAT.native_campaign_view(snapshot)

    def test_absent_disabled_and_hidden_hud_controls_are_not_clickable(self):
        self.assertFalse(COMBAT.button_ready({}, "ResolveReaction"))
        for active, interactable in [(False, True), (True, False), (False, False)]:
            self.assertFalse(COMBAT.button_ready({"Buttons": [{"Name": "ResolveReaction", "Active": active,
                                                               "Interactable": interactable}]}, "ResolveReaction"))
        self.assertTrue(COMBAT.button_ready({"Buttons": [{"Name": "ResolveReaction", "Active": True,
                                                          "Interactable": True}]}, "ResolveReaction"))

    def phase_pair(self):
        state = {"ServerInstanceId": "isolated", "GameplayRevision": 10, "Round": 1, "QuestFailed": False,
                 "PendingReaction": True, "IsResolving": False,
                 "Buttons": [{"Name": "ResolveReaction", "Active": True, "Interactable": True}]}
        snapshot = {"ServerInstanceId": "isolated", "GameplayRevision": 10,
                    "Campaign": {"Round": 1, "QuestFailed": False},
                    "Transient": {"HasPendingReactionWindow": True, "IsResolving": False}}
        return state, snapshot

    def test_queued_old_reaction_is_not_submitted_again(self):
        state, snapshot = self.phase_pair()
        self.assertEqual(COMBAT.phase_action(state, snapshot, 10), "wait")
        snapshot["GameplayRevision"] = 11
        self.assertEqual(COMBAT.phase_action(state, snapshot, 10), "wait")  # Replica still behind.
        state["GameplayRevision"] = 11
        self.assertEqual(COMBAT.phase_action(state, snapshot, 10), "react")

    def test_completed_phase_wins_over_stale_reaction_or_missing_button(self):
        state, snapshot = self.phase_pair()
        state.update(Round=2, PendingReaction=False, Buttons=[])
        self.assertEqual(COMBAT.phase_action(state, snapshot, 10), "complete")
        state, snapshot = self.phase_pair()
        snapshot["Campaign"]["Round"] = 2
        snapshot["Transient"]["HasPendingReactionWindow"] = False
        self.assertEqual(COMBAT.phase_action(state, snapshot, 10), "complete")

    def test_new_prompt_requires_matching_instance_interactive_button_and_waiting_authority(self):
        state, snapshot = self.phase_pair()
        self.assertEqual(COMBAT.phase_action(state, snapshot, 9), "react")
        for mutate in [lambda s: s.update(ServerInstanceId="old"), lambda s: s.update(IsResolving=True),
                       lambda s: s.update(Buttons=[])]:
            candidate = copy.deepcopy(state); mutate(candidate)
            self.assertEqual(COMBAT.phase_action(candidate, snapshot, 9), "wait")


class ReviewedNativeClientExtractionTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="asc-reviewed-native-client-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.revision = "a" * 40
        elf = b"\x7fELF\x02\x01" + b"\x00" * 12 + b"\x3e\x00"
        self.files = {"AscendanceClient.x86_64": elf, "UnityPlayer.so": elf,
                      "AscendanceClient_Data/Managed/Assembly-CSharp.dll": b"synthetic data only; never executed",
                      "AscendanceClient.provenance.json": json.dumps({"outcome": "success", "exitCode": 0,
                          "build": {"target": "StandaloneLinux64", "executeMethod": "Ascendance.EditorTools.NativeClientAcceptanceBuilder0102.BuildLinuxClient"},
                          "source": {"revision": self.revision, "dirty": False}}).encode()}

    def archive(self, extra=None, mutate=None):
        files = copy.deepcopy(self.files)
        if mutate: mutate(files)
        archive, manifest = self.root / "client.zip", self.root / "manifest.json"
        with zipfile.ZipFile(archive, "w") as output:
            for name, content in files.items(): output.writestr("LinuxClient/" + name, content)
            if extra: output.writestr(*extra)
        manifest.write_text(json.dumps({"format": 1, "files": {name: hashlib.sha256(data).hexdigest() for name, data in files.items()}}))
        return archive, manifest

    def test_only_complete_clean_reviewed_native_client_can_be_extracted(self):
        archive, manifest = self.archive()
        destination = self.root / "new"
        self.assertEqual(COMBAT.extract_validated_client(archive, manifest, destination, self.revision), destination)
        self.assertEqual((destination / "AscendanceClient.x86_64").read_bytes(), self.files["AscendanceClient.x86_64"])

    def test_traversal_or_extra_payload_is_refused_before_extracting(self):
        for path in ["LinuxClient/../escape", "/absolute", "OtherGame/file", "LinuxClient/unreviewed.txt"]:
            archive, manifest = self.archive((path, b"unreviewed"))
            destination = self.root / "new"
            with self.subTest(path=path), self.assertRaises(Exception):
                COMBAT.extract_validated_client(archive, manifest, destination, self.revision)
            self.assertFalse(destination.exists())

    def test_mismatched_file_hash_never_becomes_an_executable_pass(self):
        archive, manifest = self.archive()
        data = json.loads(manifest.read_text())
        data["files"]["UnityPlayer.so"] = "0" * 64
        manifest.write_text(json.dumps(data))
        with self.assertRaisesRegex(Exception, "differ"):
            COMBAT.extract_validated_client(archive, manifest, self.root / "new", self.revision)

    def test_dirty_or_wrong_source_build_provenance_is_refused(self):
        for name, value in [("dirty", True), ("revision", "b" * 40)]:
            def mutate(files):
                proof = json.loads(files["AscendanceClient.provenance.json"])
                proof["source"][name] = value
                files["AscendanceClient.provenance.json"] = json.dumps(proof).encode()
            archive, manifest = self.archive(mutate=mutate)
            with self.subTest(name=name), self.assertRaisesRegex(Exception, "clean source"):
                COMBAT.extract_validated_client(archive, manifest, self.root / name, self.revision)

    def test_existing_output_is_preserved(self):
        archive, manifest = self.archive()
        destination = self.root / "existing"
        destination.mkdir()
        sentinel = destination / "keep.txt"; sentinel.write_text("keep")
        with self.assertRaisesRegex(Exception, "absent"):
            COMBAT.extract_validated_client(archive, manifest, destination, self.revision)
        self.assertEqual(sentinel.read_text(), "keep")


if __name__ == "__main__":
    unittest.main()
