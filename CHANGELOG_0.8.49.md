# Ascendance Unity 0.8.49 — Code-First Studio Campaign UI

Built from runtime-verified 0.8.48 behavior, replacing the modular cut-image skin with a production-style code-first UGUI composition.

## Code-first architecture
- Campaign II hub uses one region/environment painting only: `ValewatchBackground0849.png`.
- All functional and decorative interface pieces are live Unity objects created in code:
  - title frame
  - framed panels
  - section headers
  - corner ornaments
  - gold dividers/diamonds
  - four World Event cards
  - twelve Quest rows
  - selected-Quest identity/detail state
  - availability state
  - Save / Begin / Main Menu action buttons
- No sprite-sheet pieces, baked labels, transparent hotspots, or image-based button plates are used by the 0.8.49 hub.
- Functional `Image` objects intentionally use no sprites.

## Layout / readability
- Canvas reference resolution is explicitly 1920×1080 with 0.5 width/height matching.
- Dedicated safe-margin root keeps content away from screen edges.
- Larger quest/detail typography than 0.8.48.
- Left: Campaign Overview + Party/Continuation.
- Center: selected Quest detail and live status.
- Right: Campaign Progression.
- Bottom: three live actions.

## Campaign structure retained
- WE005: Q013 / Q014 / Q015
- WE006: Q016 / Q017 / Q018
- WE007: Q019 / Q020 / Q021
- WE008: Q022 / Q023 / Q024
- Optional/Main/Final roles do not reorder Quest IDs.
- Q013 remains launch-guarded until its live scenario runtime is implemented.

## Gameplay / persistence
No campaign progression, reward, save/load, continuation, quest-authority, combat, crafting, trading, loadout, or downtime rules changed.
