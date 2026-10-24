ASCENDANCE STONEBROOK ENVIRONMENT — 0.5.0

0.5.0 includes an in-project authored low-poly Stonebrook mesh kit:
  House_A / House_B / House_C
  Tree_A / Tree_B / Tree_C
  Bush_A / Bush_B
  Rock_A / Rock_B / Rock_C
  Bridge_A
  Well_A
  Wagon_A
  Crates_A / Crates_B
  Brazier_A
  Fence_A

The runtime selects variants deterministically so the village does not repeat the
same model everywhere.

OVERRIDE CONVENTION
You can replace the included art without changing code by adding exact production
assets at these Resource names:
  House.prefab
  Tree.prefab
  Bush.prefab
  Rock.prefab
  Bridge.prefab
  Well.prefab
  Wagon.prefab
  Crates.prefab
  Brazier.prefab
  Fence.prefab

Exact slots override the bundled _A/_B/_C variants.

AUTHORING CONVENTION
Use a ground-level pivot and real-world Unity scale (1 Unity unit = 1 meter).
Environment assets should face +Z when practical. The runtime NavMesh baker
collects MeshFilters from child objects automatically.

SURFACE TEXTURES
Textures/grass_albedo.png
Textures/dirt_albedo.png
Textures/stone_albedo.png
Textures/water_albedo.png

These are used by StonebrookSurfaceMaterials.cs and require no .mat assets.
