"""Synthetic 597f442 deployment guards, not native migration execution proof."""
import copy
import importlib.util
import os
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location(
    "combat_upgrade_existing_predicates", ROOT / "tests/test_live_snapshot_compatibility.py")
predicates = importlib.util.module_from_spec(spec)
spec.loader.exec_module(predicates)
UPGRADE = predicates.UPGRADE
CURRENT_597_ASSEMBLY = "ed12446483b665df9b7c81562e7e602f30723ec38bbc4f8c5722f9353836c95b"
ASSEMBLY_PATH = "AscendanceServer_Data/Managed/Assembly-CSharp.dll"


@unittest.skipUnless(os.name == "posix", "Linux deployment safeguards")
class CurrentCampaignUpgradeCompatibility(predicates.LiveSnapshotCompatibility):
    """Run all existing strict comparison tests against the added exact source."""

    def setUp(self):
        super().setUp()
        verification = self.plan["upgradeVerification"]
        verification["sourceIdentity"].update(
            managedAssemblySha256=CURRENT_597_ASSEMBLY, transport="secure")
        verification["code"]["files"][ASSEMBLY_PATH]["sha256"] = CURRENT_597_ASSEMBLY

    def test_current_build_identity_accepts_only_its_exact_reviewed_hash(self):
        code = {"files": {ASSEMBLY_PATH: {"sha256": CURRENT_597_ASSEMBLY}}}
        self.assertEqual(UPGRADE.reviewed_build_identity(code, modern=True), {
            "buildVersion": "0.10.2h", "managedAssemblySha256": CURRENT_597_ASSEMBLY,
            "transport": "secure"})
        code["files"][ASSEMBLY_PATH]["sha256"] = "0" + CURRENT_597_ASSEMBLY[1:]
        with self.assertRaisesRegex(UPGRADE.Refusal, "not a reviewed legacy source"):
            UPGRADE.reviewed_build_identity(code, modern=True)

    def test_current_context_drift_requires_secure_transport_identity(self):
        for transport in (None, "plain", "unknown"):
            plan = copy.deepcopy(self.plan)
            plan["upgradeVerification"]["sourceIdentity"]["transport"] = transport
            with self.subTest(transport=transport):
                self.assertFalse(UPGRADE.live_campaigns_equivalent(plan, self.campaign, self.live))

    def test_reviewed_hashes_cannot_be_mixed_between_identity_and_code(self):
        for field in ("identity", "code"):
            plan = copy.deepcopy(self.plan)
            if field == "identity":
                plan["upgradeVerification"]["sourceIdentity"]["managedAssemblySha256"] = UPGRADE.SOURCE_H_SHA256
            else:
                plan["upgradeVerification"]["code"]["files"][ASSEMBLY_PATH]["sha256"] = UPGRADE.SOURCE_H_SHA256
            with self.subTest(field=field):
                self.assertFalse(UPGRADE.live_campaigns_equivalent(plan, self.campaign, self.live))

    def test_both_current_campaign_versions_must_match_the_verified_process(self):
        for side in ("expected", "actual"):
            for version in (None, "0.10.2g", "0.10.2c", "future"):
                expected, actual = copy.deepcopy(self.campaign), copy.deepcopy(self.live)
                (expected if side == "expected" else actual)["Version"] = version
                with self.subTest(side=side, version=version):
                    self.assertFalse(UPGRADE.live_campaigns_equivalent(self.plan, expected, actual))

    def test_quest_object_materialization_is_not_a_read_only_restore_equivalence(self):
        for objects in ([], [{"ObjectId": "Q001_VILLAGER_1", "Type": "Villager", "Destroyed": False}],
                        [{"ObjectId": "Q001_VILLAGER_1", "Type": "Villager", "Destroyed": True}]):
            live = copy.deepcopy(self.live)
            live["QuestRuntime0829"] = {"Objects": objects}
            with self.subTest(objects=objects):
                self.assertFalse(UPGRADE.live_campaigns_equivalent(self.plan, self.campaign, live))


if __name__ == "__main__":
    unittest.main()
