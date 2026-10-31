# Minimal location-art adapter

Design only. No presentation code, package imports, scene edits, build changes or deployments were performed by this document. The target is an explicit visual entry for all **236 authored Locations**, using the [coverage inventory](LOCATION_ASSET_COVERAGE.md) and the [environment acquisition plan](LOCATION_ASSET_PLAN_UNDER_50_USD.md).

## Existing boundaries

- `AscendanceGameManager.BuildLocations()` creates the original 29-node vertical-slice graph. Those runtime IDs remain stable for saves and differ from authored content IDs.
- `QuestRuntime0856.EnsureQuestRuntimeLocations0856()` creates additional quest-pool nodes for Q013+ and asks the world builder for missing markers. Catalog presence is separate from runtime creation. The 22 Stonebrook catalog rows outside the initial graph have not been certified as reachable gameplay nodes by this art inventory.
- `StonebrookWorldBuilder.SpawnLocationMarkers()` and `EnsureRuntimeLocationSpawns0856()` create the same cylinder/name marker. They do not bind terrain or landmarks to a Location.
- `ProductionAssetResolver` loads shared Stonebrook props by slot. It remains useful as a prototype fallback and does not have to be replaced for characters or existing scenery.
- The world builder bakes its explicit geometry lists once, using `Bounds(Vector3.zero, new Vector3(78f,14f,78f))`. Imported location visuals should not enter those lists or expand that bake as an incidental art change.
- `CRPGInputController` uses the closest physics raycast hit for Hero, Enemy and Location selection. Decorative colliders can therefore break clicks even when they do not change the NavMesh.

## Small implementation

Keep the adapter in new presentation files, with one installation hook in the existing world builder after its initial markers are ready. Use a separate art branch/worktree after the combat build is frozen.

| Proposed piece | Responsibility |
|---|---|
| `LocationArtCatalog` | Read-only, authored-ID-indexed metadata. One entry for each L000–L235 with family, regional palette, ordered visual parts, local transforms, bounds, origin/package ID and explicit fallback/landmark completion status. |
| `LocationArtResolver` | Resolve a curated private package prefab or client art-bundle asset. Use current prototype Resources props only as a declared fallback. Missing bindings remain missing; do not change a coverage record to passing because a generic tree loaded. |
| `LocationArtPresenter` | Own client-only visual roots, reconcile them with revealed active Locations, pool/remove obsolete roots, and anchor them at existing `LocationState.Position`. It receives its manager explicitly rather than following temporarily swapped test singletons. |
| Editor intake/audit tool | Inspect imported package contents, disable gameplay/demo components, select materials/prefabs, build curated bindings and check all 236 IDs for missing assets/landmarks. Package inspection precedes execution of imported demo/editor scripts. |

The installation hook can create the presenter after `SpawnLocationMarkers()`. The presenter's initial reconciliation handles current state; subscribing to the manager's existing state-change event handles reveals and quest transitions. `EnsureRuntimeLocationSpawns0856()` may request reconciliation after it adds new markers. No new server command, save field, room protocol or gameplay-location construction is needed for this adapter.

## Stable identity and layout

Resolve a node's canonical key as its **`AuthoredLocationId`**. Do not key art by a colliding legacy runtime ID. For example:

| Authored content | Initial runtime node |
|---|---|
| L003 Woodland Clearing | L006 |
| L004 River Crossing | L001 |
| L006 Goblin Lookout | L011 |

Each rendered root keeps both identities, such as `LocationArt_L003_L006`, for inspection only. Its world position comes from the existing runtime state; the catalog describes local decorative offsets. It never writes `LocationState.Position`, `Connections`, reveal flags, quests, Hero/Enemy positions or saved data.

The minimal visual scope is a **landmark cluster/diorama around each existing board anchor**. Reserve the marker and pawn formation area for interaction. Bound each composition against adjacent node spacing; full-scale buildings/castles require deliberate presentation scale and camera review. Full continuous region worlds with different movement coordinates are a separate feature, not a consequence of importing meshes.

Choose visual variants with a fixed, platform-independent hash of authored ID and part ID. Do not use gameplay `UnityEngine.Random` or platform-specific `string.GetHashCode()` to select art. Both clients should choose the same composition without introducing new network authority.

## Active quest and visibility

A read-only reconciliation should:

1. Obtain the current quest's existing authored location pool and starting location.
2. Match existing runtime nodes by authored ID. Include a revealed occupied/objective node if needed so the visual representation never hides the location of a visible Hero or quest object merely because of an incomplete pool list.
3. Instantiate art only for those revealed nodes; leave the existing marker's gameplay visibility logic authoritative.
4. Pool or remove roots from the previous quest and disable inactive art effects. Do not destroy game markers or remove entries from `gm.Locations`.
5. Dispose event subscriptions and roots on scene teardown.

This uses the current graph; it does not materialize missing content rows to make an art coverage count look complete. Gameplay availability for the 22 additional Stonebrook catalog rows needs its own explicit verification.

## Navigation, clicks and authority

Curated visual instances should have colliders, Rigidbody, NavMeshAgent/NavMeshObstacle, root motion and imported demo controllers disabled or removed. Preserve only reviewed visual components such as Renderers, safe material/wind effects, particles and lights. Do not attach the existing gameplay marker/controller to an arbitrary Store demo prefab.

The adapter does not call the NavMesh baker, add geometry to `walkableGeometry` / `obstacleGeometry`, change the bake bounds, or create an alternate movement controller. Use a dedicated decorative layer excluded from gameplay raycasts as additional protection. Test click selection against the actual input controller, rather than assuming a collider-free model is correctly placed.

The fixed Stonebrook backdrop cannot be hidden by disabling `Stonebrook_World`: that root also owns markers, characters and other live presentation. Initially preserve it as the declared prototype fallback. Replacing its visible backdrop later requires separating decorative Renderers from the navigation substrate and checking that any retained invisible obstacle geometry agrees with the visual design. Do not rebake a new region's full mesh layout silently under existing saves.

## Client asset delivery

Raw licensed source packages remain in private storage. Curate only the required prefabs and dependencies into a private import project/folder; leave Store demo scenes, controllers and unused high-resolution variants out of shipping outputs.

A Resources-based resolver is the smallest prototype integration, but blindly putting every Store model under Resources can enlarge both clients and the dedicated-server build. Measure the actual build result before selecting it. For the larger environment packs, Unity's built-in AssetBundle pipeline can package curated art separately for each client platform without adding an Addressables dependency: build the bundles outside the shared player source tree, then include the client bundles and their hash manifest in the complete game client package. The server neither loads nor needs these visual bundles. Treat that bundle path as an explicit packaging change and validate it; it is not implemented here.

## Completion evidence

For every ID, separately record:

- Package/prefab binding exists, with inspected source and matching hash/GUID provenance.
- Generic biome fallback versus complete authored points-of-interest/landmark composition.
- Successful target-Editor import, materials and scale.
- Screenshot review and performance/LOD/memory observations.
- Reveal/hide, quest transition, save/reconnect and two-client visual consistency.
- Unchanged canonical gameplay state and save identity when only art is loaded or unloaded.

A faithful 236-entry catalog can reuse a modest set of component packs. It still requires per-location composition and testing; purchasing two packs or instantiating one prefab per biome does not establish all-location coverage.
