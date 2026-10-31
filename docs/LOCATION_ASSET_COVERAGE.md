# Art coverage for all 236 locations

The target is **realistic fantasy 3D across every authored Location, L000–L235**, within a **$50 total asset budget**. The source contains 236 unique Location rows, including Stonebrook (`L000`). This is an exact inventory and an implementation plan; it is not a claim that the new art is imported, presented, or tested.

The complete per-location list is available in:

- [JSON coverage inventory](../distribution/location-asset-coverage.json): original ID/name/region/terrain/type, authored points of interest, reusable asset family, initial runtime mapping, existing prototype components, candidate components and explicit coverage/validation status.
- [CSV coverage inventory](../distribution/location-asset-coverage.csv): one row for every Location, suitable for filtering and reviewing missing landmarks.
- [Minimal visual-only adapter design](LOCATION_ART_ADAPTER_DESIGN.md): concrete code boundaries, quest visibility and navigation/authority invariants.
- [Environment acquisition plan within $50](LOCATION_ASSET_PLAN_UNDER_50_USD.md): compatible pack candidates, current price evidence and the acquisition/import limits.

## Source completeness

The canonical Location rows come from `Assets/Resources/Ascendance/Data/full_content_0850.json`; the JSON inventory records its SHA-256. The other Data resources contain Hero cards, the Hero roster and statuses, not another Location catalog. The initial graph maps a subset of these authored IDs. The later quest factory, the validator-only `DBG_` materializer and temporary World Event Location views all refer back to the same catalog; they do not add another authored Location ID set.

## What the existing project actually contains

`ProductionAssetResolver` resolves ten shared **Stonebrook prop slots**: House, Tree, Bush, Rock, Bridge, Well, Wagon, Crates, Brazier and Fence. Eighteen bundled OBJ variants support those slots. `StonebrookWorldBuilder` assembles a fixed village/forest/river backdrop and gives Locations a cylinder marker and a name label. It does not select an environment preset by authored Location ID, region or biome.

Stonebrook has a partial fixed village prototype, and the original River Crossing has a partial fixed river/bridge prototype. These remain separate from a dedicated, reviewed realistic art preset. This inventory records **0/236 dedicated realistic Location mappings implemented**, **0 biome presets implemented**, and **0 new Location visual/multiplayer checks passed**. Existing generic markers and reusable props are not counted as completed Location art.

The initial vertical-slice graph has 29 runtime nodes. Another 22 Stonebrook Valley Location rows are outside that initial graph; later-region Locations can be created by the quest-pool node factory. This inventory has not run every Location's gameplay creation path and does not certify their runtime availability.

## Reuse plan

The primary family below is derived from each row's actual `terrain`. Its separate region and authored points of interest remain required: a forest-labelled river crossing still needs its river, and Sunscar city architecture must not simply reuse a Crownlands town unchanged.

| Reusable component family | Locations |
|---|---:|
| Ruins and sacred architecture | 51 |
| Settlements and city districts | 44 |
| Temperate outdoors | 31 |
| Caves and mines | 22 |
| Living/blighted forest and spirit sites | 21 |
| Coast, docks and drowned world | 16 |
| Forges, magma, crystal and core machinery | 15 |
| Desert and solar sites | 10 |
| Crypts and underways | 8 |
| Roads and caravans | 6 |
| Fortresses and camps | 5 |
| Water crossings and marsh | 4 |
| Mountains and cliffs | 3 |
| **Total** | **236** |

These are shared component groups, not 236 separate Store purchases. Modular architecture, rocks, vegetation, terrain surfaces and water can be reused with distinct compositions, props, regional materials and lighting. Named landmarks still require their own assembly or model work. A pine-tree fallback alone does not complete Ancient Oak; a generic ruin does not complete a throne, Worldforge Core, living Rootheart, Leviathan Heart or Solar Crucible.

| Authored region / art variant | Locations |
|---|---:|
| Stonebrook Valley | 51 |
| Crownlands | 12 |
| Valewatch | 13 |
| Underways | 3 |
| Buried Valewatch | 9 |
| Ironspine Mountains | 37 |
| Verdant Reach | 37 |
| Storm Coast | 37 |
| Sunscar Expanse | 37 |

## Implementation boundary

1. Acquire the selected packages into private asset storage and inspect their actual meshes, materials, scripts, license terms and Unity `6000.6.5f1` compatibility. A Store compatibility claim is not a successful import test.
2. Create an explicit presentation-catalog entry for **every authored ID** using the inventory's original points of interest. Track generic biome fallback separately from a completed landmark composition.
3. Resolve presets through `AuthoredLocationId`, preserving the older runtime graph IDs used by saves. For example, runtime `L006` is **authored `L003` Woodland Clearing**, while authored Goblin Lookout `L006` is runtime `L011` in the initial graph.
4. Load art for the active quest and revealed Locations, then release or pool old quest visual roots. Do not instantiate 236 complete environments simultaneously.
5. Keep presentation children independent of authoritative movement, turn state and saved identity. Imported demo controllers, colliders and animation root motion must not redefine gameplay paths or attacks.
6. Check each preset's intended landmarks, scale, materials, lighting, LOD/memory and visibility. Test reveal, quest transition, save/reconnect and two-client consistency before changing that row's `NOT_IMPLEMENTED` / `NOT_RUN` statuses.

The source repository is public. Raw licensed Store packages belong in private storage; the public repository can retain integration code, asset references and acquisition/coverage metadata. Package ownership in the Store website's My Assets list does not itself transfer package files into this cloud workspace.

No assets were purchased, imported or deployed by this inventory task. The budget is a maximum, not a statement that every bespoke landmark is available as a ready-made realistic prefab for $50.
