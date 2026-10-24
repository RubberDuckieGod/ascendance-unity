# Optional external art upgrade path

The 0.5.0 project already contains its own Stonebrook OBJ/MTL environment kit and needs no download to run.
For a later final-art pass, these CC0 libraries are good candidates:

- Kenney — Fantasy Town Kit (CC0): modular medieval/fantasy town pieces.
  https://kenney.nl/assets/fantasy-town-kit
- Quaternius — Medieval Village MegaKit (CC0): 300+ modular village pieces; standard interchange formats available.
  https://quaternius.itch.io/medieval-village-megakit
- Quaternius — RPG Character Pack (CC0): six rigged/animated fantasy characters.
  https://quaternius.com/packs/rpgcharacters.html

Exact Stonebrook Resource slots override the included _A/_B/_C meshes, so importing a final `House.prefab`, `Tree.prefab`, etc. does not require editing gameplay code.

Use **Ascendance > Production Art > Audit 0.5.0 Assets** in the Unity editor after importing rigged characters to verify the production slots and Animator presence.
