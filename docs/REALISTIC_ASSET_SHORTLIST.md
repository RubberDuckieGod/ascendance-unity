# Ascendance realistic fantasy 3D asset shortlist

Verified against official Unity Asset Store product pages on 2026-10-31. Research only: no purchases, downloads of package contents, imports, pipeline changes, or production changes.

## Current project constraints

- Unity **6000.6.5f1**, **Built-in Render Pipeline** (`GraphicsSettings.m_CustomRenderPipeline: {fileID: 0}`; no URP/HDRP package in manifest).
- Heroes/goblins currently use articulated primitive meshes (`CharacterVisualRig.cs`); scenery combines generated primitives and a small Stonebrook OBJ set. Imported humanoids need an Animator/visual adapter, not just copying FBX files into Assets.
- 16 selectable heroes need distinct silhouettes and equipment. None of the shortlisted individual character products alone establishes faithful coverage of all 16.
- Keep server-owned movement, hit timing, HP, and turn state. Imported animation/root motion and demo character controllers must not take over multiplayer gameplay.
- Existing runtime interface is uGUI. Prefer sprite/UI prefab packs that can be adapted without replacing game command callbacks.
- Publisher compatibility claims are not an actual Unity6000.6.5f1 import/build test. Validate privately before promoting a pack.

## Free starter set

| Need | Official product / publisher | Why it fits | Limit |
|---|---|---|---|
| First realistic armored hero | [Modular Fantasy knight character](https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/modular-fantasy-knight-character-276754), Anton Puzanov | FREE; modular PBR knight; Humanoid rig; ~10.9K triangles; Standard/URP/HDRP listed for2021.3 | Does not provide all heroes or a complete animation set. Current Editor validation needed. |
| Village/buildings/cart | [Free Medieval Village Pack Vol.1](https://assetstore.unity.com/packages/3d/environments/fantasy/free-medieval-village-pack-vol-1-374316), MyDreamGameStudio | FREE; realistic weathered timber/stone;35 meshes/prefabs; Built-in and URP on6000.4 | Exteriors only. Trees/grass/skybox in screenshots are not included. |
| Trees | [Conifers [BOTD]](https://assetstore.unity.com/packages/3d/vegetation/trees/conifers-botd-142076), forst | FREE;4 realistic conifers; LOD/wind; Built-in/URP supported | Custom vegetation shaders require visual/performance testing on current Editor. |
| Readable spell/status effects | [Magic Effects FREE](https://assetstore.unity.com/packages/vfx/particles/spells/magic-effects-free-247933), Hovl Studio | FREE; circles, auras, hits, AoE, shields, smoke; Built-in/URP/HDRP stated | Map each effect to a real gameplay event; do not infer mechanics from visual effects. |
| Optional animated character prototype | [FREE - Modular Character - Fantasy RPG Human Male](https://assetstore.unity.com/packages/3d/characters/humanoids/humans/free-modular-character-fantasy-rpg-human-male-228952), Blink | FREE; body/starter armor;32 Humanoid animations | More stylized than the realistic target; one male base only. Useful retargeting prototype, not final roster choice. |

A fully coherent realistic roster plus realistic goblins has not been established from the free set. Do not describe the free set as a complete replacement.

## Paid upgrade priorities

Prices below are the **EUR prices rendered by the official store during verification**, excluding tax; they can change and are not spending authorization.

| Priority | Product / publisher | Observed price | Practical fit / remaining validation |
|---|---|---|---|
|1| [Medieval Civilians & Soldiers](https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/medieval-civilians-soldiers-244651), The Tales Factory |€165.60| Modular guards, peasants, monks, priests, merchants, armor, bows, equipment. Standard Built-in materials; URP/HDRP packages included. Strong multi-role NPC/hero base, but female and fantasy-specific identities need separate coverage. |
|2| [Forest Environment - Dynamic Nature](https://assetstore.unity.com/packages/3d/vegetation/forest-environment-dynamic-nature-150668), NatureManufacture |€55.20| Scanned realistic environment; LOD; explicit Unity6.6 Built-in/URP/HDRP support in description. Use selected assets around existing board locations, not the full demo world. |
|3| [GUI Pro - Fantasy RPG](https://assetstore.unity.com/packages/2d/gui/gui-pro-fantasy-rpg-170168), LAYERLAB |€36.79|1200+PNG sources;300+prefabs; native uGUI. No game code or animation. Reskin current working command UI and test small-screen/reaction layouts. |
|4| [RPG Character Mecanim Animation Pack](https://assetstore.unity.com/packages/3d/animations/rpg-character-mecanim-animation-pack-63772), Explosive |€110.40|1437 Humanoid animations, all pipelines. Use clips with existing authority/locomotion; do not import its optional physics controller as gameplay authority. |
|5 budget goblin| [Goblin Warrior PBR](https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/goblin-warrior-pbr-33088), organicpolygons |€13.80| Rigged Mecanim goblin with Standard-shader PBR maps; base+armor pieces. Older Unity5-era package, no current compatibility table or included animation guarantee. Treat as an evaluation candidate, not a verified drop-in. |
|5 premium goblins| [Goblin Bundle](https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/goblin-bundle-399854), Art Hiraeth |€253.92| Warrior, Archer, Berserker, Bomber, Shaman; Built-in/URP/HDRP table for6000.5.7. **No animations, no LODs**, ~37K–46Ktris each and4K textures. Requires retargeting/LODs/texture budget work. |
|Budget villager| [Medieval Villager Man Realistic](https://assetstore.unity.com/packages/3d/characters/humanoids/humans/medieval-villager-man-realistic-355004), Hayq Studios |€14.71| Realistic rigged Humanoid NPC, Standard Built-in listed for2021.3. One appearance,42Ktris/4K textures; requires optimization and current Editor test. |

## Coherent InfinityPBR alternative — package-version compatibility caveat

[Human Character - Fantasy RPG](https://assetstore.unity.com/packages/3d/characters/humanoids/humans/human-character-fantasy-rpg-60016) (€69.00), [Goblin Character Fantasy RPG](https://assetstore.unity.com/packages/3d/characters/creatures/goblin-character-fantasy-rpg-fantasy-rpg-166272) (€46.00), and [Medieval Fantasy Town Village Environment](https://assetstore.unity.com/packages/3d/environments/fantasy/medieval-fantasy-town-village-environment-for-rpg-fps-107464) (€59.80) share publisher **Magic Pig Games / InfinityPBR** and a realistic PBR direction. Character packages include animations and modular wardrobes.

However, product descriptions say **Unity6.0 packages ship Built-in, Unity6.3 packages ship URP**, and the current6.6 compatibility tables omit Built-in. Obtain/validate the appropriate Built-in package version before buying this route, or plan an explicit separately-tested URP migration. Do not casually switch Ascendance's rendering pipeline to make a store demo work.

## Suggested integration order

1. Get a free realistic knight + one village prefab + one magic marker into an isolated asset-evaluation project on6000.6.5f1. Check rig, materials, texture memory, import scripts, and licenses.
2. Add a presentation-only prefab catalog to Ascendance with stable hero/enemy IDs and primitive fallback. Keep network/save schemas and gameplay authority independent of models.
3. Map all16 heroes to distinct outfits/weapons/silhouettes, plus Raider/Archer/Scout/Captain and4civilian tokens. List gaps explicitly.
4. Wire animation/VFX to authoritative start/impact/end events. Verify two clients see the same attacks, villagers, losses, snares, statuses and deaths. No gameplay decisions from animation events alone.
5. Verify Windows player visuals/performance and headless Linux build. Keep large visual assets out of server bundles where practical.

## Source storage / licensing

The game source repository is public. Standard Asset Store licenses permit distribution embedded in a substantial game, not redistribution of raw third-party asset packages as a public asset library. Keep licensed source packs in private storage/private repo (or ignored external asset directories) and commit the integration code, asset manifest, GUID references and acquisition instructions. Free price does not itself grant public-source redistribution rights. See [Unity Asset Store EULA, Appendix1 §§2.2.1–2.2.1.1](https://unity.com/legal/as-terms).
