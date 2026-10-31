# Free realistic fantasy assets

The owner requested **free assets only**, with a realistic fantasy 3D style. The
current game uses Unity **6000.6.5f1** and the **Built-in Render Pipeline**.

The following official Store listings were verified as free on 2026-10-31:

| Asset | Publisher | Intended use | Important limitation |
| --- | --- | --- | --- |
| [Modular Fantasy knight character](https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/modular-fantasy-knight-character-276754) | Anton Puzanov | First realistic armored Hero | One modular character; a Humanoid rig is provided, but a complete animation set and all 16 Hero identities are not established. |
| [Free Medieval Village Pack Vol.1](https://assetstore.unity.com/packages/3d/environments/fantasy/free-medieval-village-pack-vol-1-374316) | MyDreamGameStudio | Stonebrook buildings, carts and scenery | 35 meshes/prefabs; exteriors only. Screenshot trees, grass and skybox are not included. Built-in and URP are listed for Unity 6000.4. |
| [Conifers \[BOTD\]](https://assetstore.unity.com/packages/3d/vegetation/trees/conifers-botd-142076) | forst | Forest location scenery | Four conifers with LOD/wind. Custom shaders need testing in the current Editor. |
| [Magic Effects FREE](https://assetstore.unity.com/packages/vfx/particles/spells/magic-effects-free-247933) | Hovl Studio | Spell hits, status auras and ground markers | Effects must be mapped to actual authoritative gameplay events. |

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

The remaining owner action is to add the four free products to **My Assets** in
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
project, if the cache cannot be found.

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
