# Free realistic fantasy assets

The later owner request sets a **$50 total budget** and prioritizes **all location
environments**. The current environment basket and acquisition plan are in
[LOCATION_ASSET_PLAN_UNDER_50_USD.md](LOCATION_ASSET_PLAN_UNDER_50_USD.md). This
document retains the free character/motion candidates; paid characters are
deferred while environment coverage takes priority.

The owner requested **free assets only**, with a realistic fantasy 3D style. The
current game uses Unity **6000.6.5f1** and the **Built-in Render Pipeline**.

The following official Store listings were verified as free on 2026-10-31:

| Asset | Publisher | Intended use | Important limitation |
| --- | --- | --- | --- |
| [Modular Fantasy knight character](https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/modular-fantasy-knight-character-276754) | Anton Puzanov | First realistic armored Hero | One modular character; a Humanoid rig is provided, but a complete animation set and all 16 Hero identities are not established. |
| [Free Medieval Village Pack Vol.1](https://assetstore.unity.com/packages/3d/environments/fantasy/free-medieval-village-pack-vol-1-374316) | MyDreamGameStudio | Stonebrook buildings, carts and scenery | 35 meshes/prefabs; exteriors only. Screenshot trees, grass and skybox are not included. Built-in and URP are listed for Unity 6000.4. |
| [Conifers \[BOTD\]](https://assetstore.unity.com/packages/3d/vegetation/trees/conifers-botd-142076) | forst | Forest location scenery | Four conifers with LOD/wind. Custom shaders need testing in the current Editor. |
| [Magic Effects FREE](https://assetstore.unity.com/packages/vfx/particles/spells/magic-effects-free-247933) | Hovl Studio | Spell hits, status auras and ground markers | Effects must be mapped to actual authoritative gameplay events. |
| [FREE - 32 RPG Animations](https://assetstore.unity.com/packages/3d/animations/free-32-rpg-animations-215058) | Blink | Compact animation starter for imported rigs | Includes locomotion, one-/two-handed melee, bow shot, casting, hit, block, stun and death. No character models. Publisher states Humanoid and Generic use; retargeting to each actual model remains untested. |

The five rows above are the minimum starter acquisition set. All prices were
verified as zero; free animations do not require buying the full paid collections.

## Additional verified free candidates

For a more varied motion library, Kevin Iglesias offers four free sample packs,
each listing Standard/Built-in compatibility on Unity 6000.0.59f2 and masculine
and feminine variants. These are an optional alternative to the compact Blink
starter, not additional required purchases:

| Motion pack | Published contents | Remaining work |
| --- | --- | --- |
| [Human Basic Motions FREE](https://assetstore.unity.com/packages/3d/animations/human-basic-motions-free-154271) | 118 files: standing, walk/run/sprint, jump/fall, turns and talk | Humanoid retargeting, foot placement and in-place movement selection. |
| [Human Melee Animations FREE](https://assetstore.unity.com/packages/3d/animations/human-melee-animations-free-165785) | 94 files: combat idles, basic one-/two-handed/polearm/shield attacks, hit and death | Connect authoritative attack timing and validate weapon attachments. |
| [Human Archer Animations FREE](https://assetstore.unity.com/packages/3d/animations/human-archer-animations-free-335880) | 136 files: frontal bowshot, bow/arrow sheathing and movement | Match bow rig/string and authoritative projectile timing. |
| [Human Spellcasting Animations FREE](https://assetstore.unity.com/packages/3d/animations/human-spellcasting-animations-free-326043) | 90 files: five magic attacks, casting transitions and a hit reaction | Map individual spells to existing gameplay; animation does not establish mechanical coverage. |

Two further model packs are worth private evaluation, with explicit limitations:

- [Character Constructor (Medieval/Fantasy armor), Modular, Prototype](https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/character-constructor-medieval-fantasy-armor-modular-prototype-303401),
  **the disaster artist**: free, realistic-proportioned male/female presets and
  over 80 modular parts. Published screenshots show clothed/armored humans that
  could provide more visual variety than a single Knight. The compatibility
  table includes Built-in, but prepared setups target URP/HDRP; the publisher
  says Standard-pipeline materials must be assigned manually. No LODs. Actual
  Humanoid retargeting and all 16 Hero/civilian identities remain unverified.
- [Goblin demon creature modular unit Demo](https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/goblin-demon-creature-modular-unit-demo-322117),
  **charttyp**: free, one rigged hand-painted horned creature, eight armor pieces,
  one weapon and five animations; Built-in/URP listed for 2022.3. This is a
  **stylized fallback**, not a verified realistic green-Goblin set or complete
  Raider/Archer/Scout/Captain collection.

No coherent realistic free Goblin set or finished realistic free medieval
civilian set was established. Searches found usable but explicitly stylized
alternatives, such as
[Low Poly Medieval Peasants - Free](https://assetstore.unity.com/packages/3d/characters/humanoids/humans/low-poly-medieval-peasants-free-lowpoly-medieval-fantasy-series-122225)
(Polytope Studio; seven characters, Built-in/URP, no animations), and
[Goblin01](https://assetstore.unity.com/packages/3d/characters/goblin01-188119)
(DancingEyebrows; four animations, visibly cartoon proportions and no current
pipeline table). These do not satisfy the realistic target and are not in the
recommended acquisition set. The free stylized PBR Goblin #33104 is URP-only in
its current version, and the dark-fantasy enemy sample #327389 is 2D; both were
excluded from the current 3D Built-in plan.

Publisher pipeline compatibility claims are recorded in
`distribution/free-realistic-asset-acquisition.json`. They are not import or
gameplay test results. None of these packages has been downloaded, imported or
integrated into Ascendance yet. No purchases or production changes were made.

## Acquisition

No `.unitypackage` archives were found in the workspace or the usual Unity Asset
Store cache locations. The installed Unity CLI can inspect/import a local
archive; it does not offer a Store acquisition/download command. Its Store-capable
account sign-in was not available when checked. Existing Editor build licensing
is separate and was not modified.

The remaining owner action is to add the five starter products to **My Assets** in
the Unity Asset Store, then use **Unity Editor → Window → Package Manager → My
Assets → Download**. Provide the resulting `.unitypackage` archives, individually
or together in a ZIP. Do not include account credentials, browser sessions or
other Unity settings. An existing local cache is commonly at:

```text
%APPDATA%\Unity\Asset Store-5.x\
```

Depending on Unity version, use **Preferences → Package Manager → My Assets
cache** to find the actual cache location. The project can also be exported as a
package after importing only these four assets into a separate evaluation
project, if the cache cannot be found. Optional model/motion packs are listed
separately in the manifest and are not required to begin evaluation.

## Evaluation and integration

1. Store the archives in a private directory outside the public Git repository;
   compute SHA-256 checksums and inspect archive contents before importing.
2. Import into an isolated Built-in project on Unity 6000.6.5f1. Check import
   scripts, materials, texture memory, rigs, animations, missing references and
   third-party license notices. Keep the running combat validation project free
   from concurrent imports.
3. Add presentation-only prefab mappings keyed by existing Hero/enemy IDs, with
   fallback visuals for missing assets. A free realistic Goblin/NPC set and full
   coverage of all 16 Heroes remain gaps; do not represent them as completed.
4. Preserve server-owned movement, damage and turn logic. Imported demo
   controllers and root motion must not decide multiplayer gameplay.
5. Validate animation/hit timing and effects with two clients, then verify the
   Windows player and headless Linux server builds. No automatic deployment.

This source repository is public. Free pricing does not grant permission to
redistribute raw Asset Store source assets publicly. Keep the licensed source
packs in private storage; publish the integration code and acquisition manifest.
See [Unity Asset Store EULA, Appendix 1 §§2.2.1–2.2.1.1](https://unity.com/legal/as-terms).
