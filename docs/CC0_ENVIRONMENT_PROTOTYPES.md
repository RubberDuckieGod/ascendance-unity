# Direct CC0 environment prototypes

Official **Poly Haven** downloads provide a free way to evaluate realistic
environment components without another Store account download. Poly Haven's
[license page](https://polyhaven.com/license) explicitly places its assets under
[CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/legalcode.en), including
commercial use and redistribution. [ambientCG](https://docs.ambientcg.com/license/)
also publishes its assets under CC0; no ambientCG files were acquired here.

Three Poly Haven model prototypes and their **1K PBR textures** were downloaded
from the official API/download hosts, with all published MD5 checksums verified
and SHA-256 hashes recorded. Total downloaded model/texture data: **50,574,905
bytes**. They are private evaluation files. The coordinator has since imported
them and built/reloaded **29 curated parts** with geometry, material and bounds
checks passing. Actual game presentation and location review remain separate.

| Model | Reported polygon count | Downloaded bytes | Potential use |
| --- | ---: | ---: | --- |
| [Rock Face 01](https://polyhaven.com/a/rock_face_01) | 20,174 | 4,164,274 | Cliff, cave entrance, rocky coast or desert composition component. |
| [Fern 02](https://polyhaven.com/a/fern_02) | 6,232 | 1,722,462 | Four fern clumps for woodland or marsh composition. |
| [Modular Fort 01](https://polyhaven.com/a/modular_fort_01) | 28,218 | 44,688,169 | Weathered stone fort sections, battlements, tower and arched-gate components. Its colonial architecture must be curated for Ascendance's regional styles. |

All three include **FBX** files, which Unity imports natively. No Blender install,
glTF importer or new Unity package was installed. The very high-detail Pine Tree
01 (about 17 million reported polygons, 648 MB FBX alone) and Coast Rocks 03
(about 1.5 million polygons) were rejected for this initial runtime evaluation.

Files and private paths are recorded in:

```text
/workspace/ascendance-asset-evaluation/cc0/prototype-manifest.json
```

The repository records source URLs, licenses, sizes and per-file hashes in
`distribution/cc0-environment-prototypes.json`. The full official CC0 legal code
and provider license page were archived beside the private prototypes.

## Additional material candidates

The following official texture sets were verified through the Poly Haven API.
Their 1K color, normal, roughness and AO maps have now been downloaded and
checksum-verified: **20 additional files, 10,035,272 bytes**. Optional specular
map metadata is recorded without downloading those maps:

- [Aerial Sand](https://polyhaven.com/a/aerial_sand): desert ground.
- [Snow 02](https://polyhaven.com/a/snow_02): snow surfaces.
- [Brown Mud](https://polyhaven.com/a/brown_mud): wet ground and marsh surfaces.
- [Castle Brick 01](https://polyhaven.com/a/castle_brick_01): masonry surfaces.
- [Rusty Metal](https://polyhaven.com/a/rusty_metal): weathered forge/metal props.

Texture sets need terrain or UV-mapped meshes. They do not create complete
locations, water behavior, lava animation or crystal translucency by themselves.

## Integration still required

The current game uses Unity 6000.6.5f1 with Built-in rendering. An isolated import
must verify FBX units/orientation, submeshes, UVs, materials and performance.
OpenGL normal maps need correct normal-map import settings. Roughness must be
converted to Standard-shader smoothness rather than assigned to the metallic
slot unchanged. Fern alpha requires cutout and appropriate backface treatment.
Fort pieces need individual prefab selection, collision and LOD/texture budgets.

The private Editor preparation tool `CC0AssetPreparation0103` prepares Standard
PBR materials, five ground profiles, normalized model parts and bundle inspection
metadata. The coordinator's Linux build/reload passed for 29 parts. An initial
render preview exposed backface culling on the open rock scan; its opaque
double-sided material was rebuilt, and a fresh render review is still required.
The private runtime manifest binds all 236 locations as **components-only**, with
authored and regional landmark gaps explicit. It requires actual presenter
validation before being reported as tested in the game.

The fern and open-rock preparation uses the owner's existing Knight `Standard
Double Sided` shader. Original Poly Haven files remain CC0, but a prepared
bundle containing that licensed shader must not be described as wholly CC0.

No Editor was launched by this asset research/preparation agent, and no game
server, campaign or production deployment was changed. These components can
reduce acquisition delays, but they do not establish completed art for all 236
authored locations. Regional compositions and signature landmarks remain
explicit work in `distribution/location-asset-coverage.json`.
