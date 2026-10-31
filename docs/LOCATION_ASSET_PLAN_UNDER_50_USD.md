# Realistic location assets within a $50 total budget

The current priority is **assets for all locations**, using a realistic fantasy
3D direction. The owner set a **$50 USD total ceiling**. Environment components
take priority over paid characters. No purchases have occurred. The owner has
since provided the Village, Knight and Magic Effects archives; their filtered
Built-in evaluation is coordinated separately. Other Store packages below have
not been transferred. [Direct CC0 prototypes](CC0_ENVIRONMENT_PROTOTYPES.md) are
also available privately for evaluation without another Store download.

Ascendance has **236 authored locations**, covering nine regional architecture
styles. A component library can support those locations, but each still needs
its own composition and signature landmarks. Buying a forest or village pack
does not create all 236 scenes. The exact inventory and current missing mappings
are recorded in [LOCATION_ASSET_COVERAGE.md](LOCATION_ASSET_COVERAGE.md) and
`distribution/location-asset-coverage.json`.

## Proposed paid environment basket

Official Store pages were checked on 2026-10-31 with the currency set to USD:

| Product | Verified price before tax | Reusable contents |
| --- | --- | --- |
| [Medieval Fantasy Ruins - Dark Forest Environment](https://assetstore.unity.com/packages/3d/environments/fantasy/medieval-fantasy-ruins-dark-forest-environment-258405), ScansFactory | **$24.99 sale**; regular $49.99 | More than 300 meshes/prefabs: modular chapel/crypt architecture, doors, stairs, bridges, stonework, roots, trees and dead trees. Strong base for ruins, sacred sites, crypts and living/blighted woodland variants. |
| [Lost Temple Cave](https://assetstore.unity.com/packages/3d/environments/lost-temple-cave-209749), ScansFactory | **$15.00** | 36 photogrammetry meshes/prefabs: rocks, cliffs, cobblestones, altar and sculpture; eight closed cliff variants help compose caves. |
| **Total** | **$39.99 before tax** | **$10.01 remains for tax within the $50 ceiling.** |

These two packs share a publisher and photogrammetry approach. Both list
Built-in, URP and HDRP support for Unity 6, with configuration instructions
included. Their demo screenshots use HDRP; the current game uses **Built-in**.
The Built-in configuration must be evaluated in an isolated project, without
changing Ascendance's render pipeline.

Check ownership before buying: the owner already has assets in My Assets, but
the per-product inventory is unavailable here. Recheck the sale price and final
tax-inclusive checkout total. If that total exceeds $50, do not place the order;
the free components remain usable. This document is a proposed basket, not a
purchase receipt or completed acquisition.

## Free components that fill other themes

All listings below were verified as free. Village and Magic Effects source
archives are now available privately; current import results belong to the
isolated evaluation report. Other listed Store archives remain unavailable.

| Theme | Verified free product | What it supplies and what it does not |
| --- | --- | --- |
| Settlements, roads, caravans | [Free Medieval Village Pack Vol.1](https://assetstore.unity.com/packages/3d/environments/fantasy/free-medieval-village-pack-vol-1-374316) | 35 exterior meshes/prefabs, carts and village scenery. Does not supply complete interiors or every regional city style. |
| Fortresses, bridges, towers | [Medieval Castle - Modular](https://assetstore.unity.com/packages/3d/environments/fantasy/medieval-castle-modular-282498) | Castle, walls, towers, doors, houses and bridge segments; low-poly aerial-view construction pieces. Individual strongholds still need assembly. |
| Additional ruined architecture | [Altar Ruins Free](https://assetstore.unity.com/packages/3d/environments/fantasy/altar-ruins-free-109065) | 37 stone, arch, wall and stump models with PBR maps. Does not establish bespoke temple identities. |
| Grasslands, sand, snow, hills | [Terrain Asset Pack / Terrain Sample](https://assetstore.unity.com/packages/3d/environments/landscapes/terrain-asset-pack-terrain-sample-145808) | 42 heightmaps, 16 terrain layers and 20 vegetation prefabs. Vegetation requires Shader Graph. Preserve Unity Companion License and terrain-data ODbL notices. |
| Forest | [Conifers \[BOTD\]](https://assetstore.unity.com/packages/3d/vegetation/trees/conifers-botd-142076) | Four conifers with wind/LOD support; does not cover all deciduous or living-forest landmarks. |
| Rocks, cliffs, winter variants | [Rock and Boulders 2](https://assetstore.unity.com/packages/3d/props/exterior/rock-and-boulders-2-6947) | 24 rock/boulder prefabs with standard/winter materials. Older package; current shaders require verification. |
| Mountain horizons | [Winter Mountains and Stamps](https://assetstore.unity.com/packages/3d/environments/landscapes/winter-mountains-and-stamps-129245) | Six background mountain models and terrain stamps. Background art does not create playable mountain locations. |
| Mines and passages | [Mine](https://assetstore.unity.com/packages/3d/environments/dungeons/mine-92461) | Straight/turn/ramp tunnel segments, supports, ore, crate, lantern, pickaxe and ladder. Older 2017 package; materials and import compatibility need inspection. |
| Forges and workshops | [Blacksmith's Forge](https://assetstore.unity.com/packages/3d/environments/fantasy/blacksmith-s-forge-17785) | 74 meshes/prefabs, Built-in Standard materials. Dwarven/solar machinery and campaign landmarks need additional composition. |
| Crystal cores | [PBR Crystals Pack](https://assetstore.unity.com/packages/3d/props/exterior/pbr-crystals-pack-290970) | 16 crystal meshes and 96 color/cluster prefabs; Built-in and URP scenes. Does not create a complete crystal biome. |
| Coast, docks, flooded areas | [Old Sea Port - Environment](https://assetstore.unity.com/packages/3d/environments/old-sea-port-environment-36897) | More than 40 prefabs. Exact contents need inspection and medieval/fantasy curation; modern props cannot be counted as faithful coastal coverage. |
| Water | [Water Shaders V2.x](https://assetstore.unity.com/packages/vfx/shaders/water-shaders-v2-x-149916) | Built-in-only water with reflection/shore foam. Older shaders need Unity 6000.6 testing; this is rendering, not swimming or gameplay behavior. |
| Desert/solar architecture | [Egyptian Palace / Ancient Egypt Lite](https://assetstore.unity.com/packages/3d/environments/egyptian-palace-ancient-egypt-lite-free-3d-assets-311814) | 55 temple, obelisk, pillar, palm, platform and decorative meshes. A low-poly style compromise; it does not reproduce Sunscar's unique monuments. |
| Blight, rituals, magical locations | [Magic Effects FREE](https://assetstore.unity.com/packages/vfx/particles/spells/magic-effects-free-247933) | Auras, ground circles and particles. Visual material/effect variants need to follow actual game state. |

The pack list provides **candidate components**. It is not an imported asset
catalog, completed location mapping or pass result. Region-specific cities,
living roots/hearts, drowned-world and whalespine landmarks, solar machinery,
colossi, thrones, prisons and objective placement remain explicit art tasks.

## Getting owned packages into this environment

The supported Unity process is **Editor → Window → Package Manager → My Assets
→ select package → Download**. Unity CLI supports inspecting/importing/exporting
local `.unitypackage` files; it has no supported My Assets acquisition/download
command. Signing the CLI in again would not provide that missing command, so no
new sign-in request was initiated.

After downloading in a separate empty evaluation project, provide the
`.unitypackage` files (or one ZIP containing them). The usual Windows cache is:

```text
%APPDATA%\Unity\Asset Store-5.x
```

Unity documents the [download procedure](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-ui-import.html)
and [asset cache location](https://docs.unity3d.com/6000.0/Documentation/Manual/AssetStorePackages.html).
The cache location can be customized in Preferences → Package Manager.
Keep credentials and session files out of uploads.

Source archives will stay private outside the public game repository. Before
integration, inspect package contents and scripts, hash each archive, test
materials/rigs/performance in Unity 6000.6.5f1, then map components to individual
location IDs. Do not import a Store demo's gameplay controllers, project settings
or replacement render pipeline into the active multiplayer game.
