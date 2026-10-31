"""Required native art observations must fail closed independently of gameplay."""
import copy
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))
import native_combat_acceptance as combat


class NativeLocationArtObservationTests(unittest.TestCase):
    def fixture(self):
        return {"Art": {"PresenterInstalled": True, "BindingFailure": None, "ActiveLocations": 1,
            "SuppressedLegacyHouses": 4, "Locations": [{"RuntimeLocationId": "L000", "AuthoredLocationId": "L000",
                "RootActive": True, "PartsInstantiated": 4, "PartsMissing": 0, "Failures": [],
                "EnabledRenderers": 4, "VisibleRenderers": 2, "InteractionCenterClear": True,
                "DecorativeCollidersDisabled": True, "EffectiveCoverage": "components-only",
                "MissingLandmarks": ["Well", "Noticeboard"]}]}}

    def test_required_diagnostics_are_not_defaulted_into_a_pass(self):
        state = self.fixture()
        for key in ["PresenterInstalled", "ActiveLocations", "BindingFailure", "SuppressedLegacyHouses", "Locations"]:
            candidate = copy.deepcopy(state); del candidate["Art"][key]
            with self.subTest(key=key), self.assertRaises(KeyError):
                combat.location_art_observation(candidate)
        for key in state["Art"]["Locations"][0]:
            if key == "RuntimeLocationId": continue
            candidate = copy.deepcopy(state); del candidate["Art"]["Locations"][0][key]
            with self.subTest(key=key), self.assertRaises(KeyError):
                combat.location_art_observation(candidate)
        with self.assertRaises(KeyError): combat.location_art_observation({})

    def test_missing_roots_parts_renderers_failures_and_overlap_remain_observable(self):
        state = self.fixture()
        observed = combat.location_art_observation(state)
        self.assertTrue(observed["actualPartsPresent"])
        for key, value in [("RootActive", False), ("PartsInstantiated", 0), ("PartsMissing", 1), ("Failures", ["Missing model"])]:
            candidate = copy.deepcopy(state); candidate["Art"]["Locations"][0][key] = value
            with self.subTest(key=key): self.assertFalse(combat.location_art_observation(candidate)["actualPartsPresent"])
        for key, projected, value in [("EnabledRenderers", "enabledRenderersPresent", 0),
                                     ("InteractionCenterClear", "centersClear", False),
                                     ("DecorativeCollidersDisabled", "decorativeCollidersDisabled", False)]:
            candidate = copy.deepcopy(state); candidate["Art"]["Locations"][0][key] = value
            with self.subTest(key=key): self.assertFalse(combat.location_art_observation(candidate)[projected])
        candidate = copy.deepcopy(state); candidate["Art"]["Locations"] = []
        observed = combat.location_art_observation(candidate)
        self.assertEqual(observed["stonebrookRoots"], 0)
        self.assertFalse(observed["actualPartsPresent"])
        self.assertFalse(observed["centersClear"])

    def test_partial_components_do_not_erase_authored_landmark_gaps(self):
        observed = combat.location_art_observation(self.fixture())
        self.assertEqual(observed["suppressedLegacyHouses"], 4)
        self.assertEqual(observed["stonebrookVisibleRenderers"], 2)
        self.assertEqual(observed["coverage"], [{"authoredLocationId": "L000", "effectiveCoverage": "components-only",
                                                "missingLandmarks": ["Well", "Noticeboard"]}])


if __name__ == "__main__": unittest.main()
