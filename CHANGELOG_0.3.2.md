# Ascendance Unity 0.3.2 — HUD Containment / Responsive Combat Bar Fix

## Why this exists
0.3.1 improved the tactical camera, but its HUD readability pass increased font sizes without constraining text. In a docked/free-aspect Unity Game view, wrapped log/card/quest text could overflow the bounds of its panels and cover the 3D world.

## Fixes
- Keeps the successful 0.3.1 CRPG camera and F-focus behavior unchanged.
- Moves the CanvasScaler to a 1600×900 reference resolution as a middle ground between 0.3.0 and 0.3.1.
- Every HUD panel now has a RectMask2D containment mask.
- Runtime Text uses `VerticalWrapMode.Truncate` instead of unrestricted overflow.
- Important labels use constrained Best Fit sizing rather than blindly oversized fonts.
- Combat log reduced to the newest 6 entries in the on-screen panel.
- Q001 tracker copy is slightly condensed while preserving the same objectives/rules.
- Bottom Hero cards now show compact card name + AP cost only.
- Hovering a Hero card opens a dedicated rules tooltip above the combat bar.
- Card rules can no longer draw over Stonebrook or neighboring cards.
- Party/quest/target/hero/control typography retuned for docked Game windows.

## Editor note
The Unity Game tab's `Scale` control is an editor preview zoom. A value like 1.2× can crop the rendered game inside the dock. Use Fit/1× when judging the full HUD composition. This does not affect a standalone build.
