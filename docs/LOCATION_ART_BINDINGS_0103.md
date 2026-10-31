# Client location-art bindings

The adapter uses `StreamingAssets/AscendanceArt/location-art.json` plus the current platform's curated `.bundle` files. The server receives neither. An Editor/development-only `ASCENDANCE_ART_MANIFEST` absolute-path override supports private previews without placing licensed packages in the public project.

[The example](examples/location-art.components-only.json) is metadata only. Its all-zero hash and illustrative prefab name deliberately do not form a usable distribution. Replace the hash with the actual bundle SHA-256 and `assetName` with the exact lower-case path from `AssetBundle.GetAllAssetNames()`.

`format` is 1. `platform` is `StandaloneLinux64` or `StandaloneWindows64`. Bundles use unique IDs, leaf `.bundle` filenames and exact SHA-256 hashes. Profiles contain ordered parts with explicit local position, Euler rotation and positive scale. Locations bind canonical authored IDs, not the older runtime graph IDs. Missing IDs remain unbound; defining 236 catalog rows does not create 236 visual presets.

Coverage declarations are `unbound`, `components-only`, or `full-reviewed`. Full review requires explicit evidence in both the profile and Location binding, and all original authored points of interest must be included in the profile's landmark list. Omitted landmarks are computed from the canonical catalog even if a partial binding provides an empty `missingLandmarks` list. Runtime missing parts downgrade effective coverage.

Curated prefab assets must contain no custom/missing script components: the loader rejects these before instantiation so a demo script's `Awake` never runs. Clones enter an inactive private visual root, then colliders/physics/nav agents/obstacles and animation root motion are disabled before activation. Parts must leave a 1.9-unit interaction radius clear around the existing marker/pawn anchor; only flat ground dressing at or below 0.15 above the anchor may cross that center. Standardize prefab bounds in the private bundle builder, then position scenery around the reserved area. No automatic repositioning of gameplay objects or navigation rebake occurs.

Useful inspection APIs:

- `LocationArtCatalog0103.CountBindings(library)`: catalog, bound/unbound and declared partial/full counts. These are metadata declarations, not visual QA passes.
- `LocationArtPresenter0103.ActiveLocationCount` / `ActiveViews`: instantiated visible Location roots and their actual part counts/effective coverage.
- `LocationArtPresenter0103.BindingAttempts`: includes unbound attempts with missing files/parts, landmark gaps and failure reasons.
- `ReloadDevelopmentBindings(absolutePath)`: Editor/development preview reload after a private bundle build; absent from a shipping player.

A missing manifest uses the existing prototype scene quietly. Malformed paths, wrong platform, bundle hash mismatch or unsupported prefab scripts refuse the optional art while preserving game input and canonical state. This adapter does not certify the realism, completeness or performance of package contents.


An optional `replaceLegacy` array belongs to the **Location binding**, not its shared profile. Only L000 may request `stonebrook-house-0` through `stonebrook-house-3`, in the four `BuildVillage` call positions. Replacement hides only the captured house/roof/window Renderers after every requested profile part has loaded successfully and an enabled new model actually overlaps the original house footprint. It keeps the house colliders, navigation blocker, marker and actor objects unchanged. Missing art, non-overlap, reload and presenter teardown restore each Renderer's original enabled/hidden flag. Leave this array empty until the actual preview confirms the new house occupies the old footprint.

Framework gate: `LocationArtRegressionTestTools0103.TestFramework()` (46 checks). Its primitive fixtures exercise mapping, malformed-input refusal, visibility lifecycle, native physics/nav/demo isolation, clear-center handling, legacy renderer-only replacement/restoration, and canonical-state invariance. They are not asset-quality or all-location art coverage checks.

Required installed-art gate: `LocationArtRegressionTestTools0103.TestInstalledBundle()` (10 checks). It requires real private client bundles through the manifest, loads every declared part, tests overlapping library disposal, then instantiates current visible art and checks renderer/collider and canonical-state outcomes. Missing private assets fail this gate instead of being skipped.
