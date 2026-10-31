"""Synthetic deployment predicates; actual original-h native proof is separate."""
import copy
import importlib.util
import json
import os
from pathlib import Path
import unittest
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("live_snapshot_upgrade", ROOT / "Deploy/DigitalOcean/upgrade-existing.py")
UPGRADE = importlib.util.module_from_spec(spec) if os.name == "posix" else None
if UPGRADE is not None:
    spec.loader.exec_module(UPGRADE)


@unittest.skipUnless(os.name == "posix", "Linux deployment safeguards")
class LiveSnapshotCompatibility(unittest.TestCase):
    def setUp(self):
        self.campaign = {"Version": "0.10.2h", "SavedAtUtc": "fixture-time", "QuestId": "Q001", "Round": 4,
            "PartyGold": 73, "TurnHeroId": "Aldric", "SelectedHeroId": "Aldric", "SelectedEnemyId": None,
            "PartyHeroIds": ["Garruk", "Aldric"], "PendingStateSchema0102f": 1,
            "HasPendingRewardChoice0102f": False, "HasPendingEncounterDeckControl0102f": False,
            "PendingRewardChoice": None, "PendingEncounterDeckControl": None,
            "Heroes": [{"Id": "Garruk", "Health": 9, "Level": 1, "XP": 0, "LocationId": "L000", "Actions": 0,
                        "HandIds": ["g1", "g2"], "DeckIds": ["g3", "g4"], "DiscardIds": ["g5", "g6"]},
                       {"Id": "Aldric", "Health": 5, "Level": 3, "XP": 260, "LocationId": "L000", "Actions": 3,
                        "HandIds": ["a1", "a2"], "DeckIds": ["a3", "a4"], "DiscardIds": ["a5", "a6"]}],
            "Enemies": [{"Id": "quest-enemy", "Health": 3, "LocationId": "L000", "ActivatedThisRound": False}],
            "EncounterDeckIds": ["EN001", "EN002"], "SearchDeckIds": ["SD001", "SD002"]}
        self.live = copy.deepcopy(self.campaign)
        self.live.update(SelectedHeroId="Garruk", SelectedEnemyId="quest-enemy")
        checksum = UPGRADE.SOURCE_H_SHA256
        self.plan = {"process": {"room": "SYNTHETIC", "wireBuildVersion": "0.10.2h"},
            "persistence": {"expectedDataDirectory": {"path": "/isolated/synthetic"}},
            "upgradeVerification": {"sourceIdentity": {"buildVersion": "0.10.2h", "managedAssemblySha256": checksum},
                "code": {"files": {"AscendanceServer_Data/Managed/Assembly-CSharp.dll": {"sha256": checksum}}}}}
        self.room = {"RoomId": "SYNTHETIC", "BuildVersion": "0.10.2h", "Revision": 7, "GameplayRevision": 3,
                     "CreatedAtUtc": "synthetic-creation", "UpdatedAtUtc": "synthetic-update", "Players": [],
                     "DurableCampaign": self.campaign}
        self.snapshot = {"RoomId": "SYNTHETIC", "BuildVersion": "0.10.2h", "Revision": 7, "Players": [],
                         "RoomCreatedAtUtc0102d": "synthetic-creation", "RoomStateOrigin0102d": "RESTORED_FROM_SERVER_DISK",
                         "PersistencePath0102d": str(UPGRADE.room_path(self.plan)), "Campaign": self.live}
        self.schema = json.loads((ROOT / "Deploy/DigitalOcean/save-schema-0102g.json").read_text())

    def test_reviewed_h_live_context_drift_is_accepted_without_mutating_inputs(self):
        before = copy.deepcopy((self.plan, self.campaign, self.live))
        self.assertFalse(UPGRADE.campaigns_equivalent(self.campaign, self.live))
        self.assertTrue(UPGRADE.live_campaigns_equivalent(self.plan, self.campaign, self.live))
        self.assertEqual((self.plan, self.campaign, self.live), before)

    def test_reviewed_h_without_presentation_drift_still_passes(self):
        self.assertTrue(UPGRADE.live_campaigns_equivalent(self.plan, self.campaign, copy.deepcopy(self.campaign)))

    def test_capture_returns_original_durable_room_and_does_not_force_save(self):
        before = copy.deepcopy((self.room, self.snapshot))
        with patch.object(UPGRADE, "read_private_room", return_value=self.room) as read:
            self.assertIs(UPGRADE.capture_migration_state(self.plan, self.snapshot, {}), self.room)
        read.assert_called_once_with(self.plan, {})
        self.assertEqual((self.room, self.snapshot), before)

    def test_new_equivalence_requires_exact_source_identity_and_actual_code_proof(self):
        paths = [("process", "wireBuildVersion"), ("upgradeVerification", "sourceIdentity", "buildVersion"),
                 ("upgradeVerification", "sourceIdentity", "managedAssemblySha256"),
                 ("upgradeVerification", "code", "files", "AscendanceServer_Data/Managed/Assembly-CSharp.dll", "sha256")]
        for path in paths:
            plan = copy.deepcopy(self.plan)
            parent = plan
            for field in path[:-1]: parent = parent[field]
            parent[path[-1]] = "unreviewed-fixture"
            with self.subTest(path=path):
                self.assertFalse(UPGRADE.live_campaigns_equivalent(plan, self.campaign, self.live))
        self.assertFalse(UPGRADE.live_campaigns_equivalent({}, self.campaign, self.live))

    def test_old_g_keeps_its_existing_strict_live_behavior(self):
        plan = copy.deepcopy(self.plan)
        plan["process"]["wireBuildVersion"] = "0.10.2g"
        plan["upgradeVerification"]["sourceIdentity"].update(buildVersion="0.10.2g", managedAssemblySha256=UPGRADE.SOURCE_G_SHA256)
        plan["upgradeVerification"]["code"]["files"]["AscendanceServer_Data/Managed/Assembly-CSharp.dll"]["sha256"] = UPGRADE.SOURCE_G_SHA256
        self.assertFalse(UPGRADE.live_campaigns_equivalent(plan, self.campaign, self.live))
        self.assertTrue(UPGRADE.live_campaigns_equivalent(plan, self.campaign, copy.deepcopy(self.campaign)))

    def test_gold_health_level_xp_turn_cards_and_every_other_gameplay_difference_refuses(self):
        mutations = [lambda c: c.update(PartyGold=74), lambda c: c.update(Round=5), lambda c: c.update(QuestId="Q002"),
            lambda c: c.update(TurnHeroId="Garruk"), lambda c: c["Heroes"][1].update(Health=6),
            lambda c: c["Heroes"][1].update(Level=2), lambda c: c["Heroes"][1].update(XP=261),
            lambda c: c["Heroes"][1].update(Actions=2), lambda c: c["Heroes"][1].update(LocationId="L001"),
            lambda c: c["Heroes"][1]["HandIds"].reverse(), lambda c: c["Heroes"][1]["DeckIds"].reverse(),
            lambda c: c["Heroes"][1]["DiscardIds"].reverse(), lambda c: c["EncounterDeckIds"].reverse(),
            lambda c: c["SearchDeckIds"].reverse(), lambda c: c["PartyHeroIds"].reverse(),
            lambda c: c["Enemies"][0].update(Health=2), lambda c: c["Enemies"][0].update(ActivatedThisRound=True),
            lambda c: c.update(HasPendingRewardChoice0102f=True,
                PendingRewardChoice={"SourceCardId": "SD082", "HeroId": "Aldric", "Mode": "Gold25OrLoot1"}),
            lambda c: c.update(HasPendingEncounterDeckControl0102f=True,
                PendingEncounterDeckControl={"SourceCardId": "SD041", "Mode": "MayBottomTop", "RevealedCardIds": ["EN001"]}),
            lambda c: c.update(UNREVIEWED_FIXTURE_FIELD=True)]
        for index, mutate in enumerate(mutations):
            live = copy.deepcopy(self.live); mutate(live)
            with self.subTest(mutation=index):
                self.assertFalse(UPGRADE.live_campaigns_equivalent(self.plan, self.campaign, live))
                snapshot = copy.deepcopy(self.snapshot); snapshot["Campaign"] = live
                with patch.object(UPGRADE, "read_private_room", return_value=self.room), self.assertRaisesRegex(UPGRADE.Refusal, "Live campaign"):
                    UPGRADE.capture_migration_state(self.plan, snapshot, {})

    def test_unknown_or_ambiguous_selected_references_refuse_the_new_exception(self):
        changes = [lambda c: c.update(SelectedHeroId="unknown-hero"), lambda c: c.update(SelectedHeroId=None),
                   lambda c: c.update(SelectedEnemyId="unknown-enemy"), lambda c: c.update(SelectedEnemyId=4),
                   lambda c: c["Heroes"].append(copy.deepcopy(c["Heroes"][0])),
                   lambda c: c["Enemies"].append(copy.deepcopy(c["Enemies"][0])),
                   lambda c: c.update(PartyHeroIds=["Garruk", "unknown-hero"])]
        for index, mutate in enumerate(changes):
            for side in ("durable", "live"):
                durable, live = copy.deepcopy(self.campaign), copy.deepcopy(self.live)
                mutate(durable if side == "durable" else live)
                with self.subTest(mutation=index, side=side):
                    self.assertFalse(UPGRADE.live_campaigns_equivalent(self.plan, durable, live))

    def test_enemy_order_remains_strict_even_without_a_presentation_difference(self):
        durable = copy.deepcopy(self.campaign)
        durable["Enemies"].append({"Id": "other-enemy", "Health": 4, "LocationId": "L001", "ActivatedThisRound": False})
        for selection_drift in (False, True):
            live = copy.deepcopy(durable); live["Enemies"].reverse()
            if selection_drift: live.update(SelectedHeroId="Garruk", SelectedEnemyId="quest-enemy")
            with self.subTest(selection_drift=selection_drift):
                self.assertTrue(UPGRADE.campaigns_equivalent(durable, {**live, "SelectedHeroId": durable["SelectedHeroId"], "SelectedEnemyId": durable["SelectedEnemyId"]}))
                self.assertFalse(UPGRADE.live_campaigns_equivalent(self.plan, durable, live))

    def test_mismatch_diagnostics_emit_only_declared_paths_and_indexes(self):
        live = copy.deepcopy(self.live)
        live["Heroes"][1]["Health"] = 999
        live["Heroes"][1]["HandIds"].reverse()
        live["private-arbitrary-fixture-value"] = "private-data-value"
        paths = UPGRADE.live_difference_paths(self.plan, self.campaign, live, self.schema)
        self.assertIn("Campaign.Heroes[1].Health", paths)
        self.assertIn("Campaign.Heroes[1].HandIds[0]", paths)
        text = json.dumps(paths)
        for value in ["private-arbitrary-fixture-value", "private-data-value", "999", "a1", "a2"]:
            self.assertNotIn(value, text)
        for path in paths:
            self.assertRegex(path, r"^Campaign(?:\.[A-Za-z][A-Za-z0-9_]*|\[[0-9]+\])*$")
        self.assertLessEqual(len(UPGRADE.live_difference_paths(self.plan, self.campaign, live, self.schema, limit=2)), 2)

    def test_capture_refusal_retains_safe_field_paths_without_values(self):
        snapshot = copy.deepcopy(self.snapshot); snapshot["Campaign"]["PartyGold"] = 1234567
        with patch.object(UPGRADE, "read_private_room", return_value=self.room), self.assertRaises(UPGRADE.LiveStateMismatch) as caught:
            UPGRADE.capture_migration_state(self.plan, snapshot, self.schema)
        self.assertIn("Campaign.PartyGold", caught.exception.difference_field_paths)
        self.assertNotIn("1234567", str(caught.exception))
        self.assertNotIn("1234567", json.dumps(caught.exception.difference_field_paths))

    def test_creation_path_revision_and_player_checks_still_refuse(self):
        for name, value in [("RoomId", "another-room"), ("BuildVersion", "0.10.2g"), ("Revision", 8),
                            ("RoomCreatedAtUtc0102d", "another-creation"), ("PersistencePath0102d", "/another/path"),
                            ("Players", [{"PlayerId": "unexpected-fixture-owner"}])]:
            snapshot = copy.deepcopy(self.snapshot); snapshot[name] = value
            with self.subTest(field=name), patch.object(UPGRADE, "read_private_room", return_value=self.room), self.assertRaises(UPGRADE.Refusal):
                UPGRADE.capture_migration_state(self.plan, snapshot, {})

    def test_copied_room_and_candidate_restore_keep_strict_selected_state_comparison(self):
        changed = copy.deepcopy(self.room); changed["DurableCampaign"] = self.live
        self.assertFalse(UPGRADE.rooms_equivalent(self.room, changed))
        with patch.object(UPGRADE, "read_private_room", return_value=changed), self.assertRaisesRegex(UPGRADE.Refusal, "Restored campaign"):
            UPGRADE.prove_restored_state(self.plan, self.snapshot, self.room, {}, "0.10.2h")


if __name__ == "__main__":
    unittest.main()
