# Ascendance Unity 0.3.1 — Camera & HUD Readability Pass

Built on the stable 0.3.0 visual/combat prototype without changing Ascendance gameplay rules.

## Camera
- Default tactical camera moved substantially closer to the party.
- Camera pitch lowered from the distant board-view angle to a 34° CRPG presentation angle.
- Default distance reduced to 16.5 units.
- FOV tightened to 44° for stronger character/environment scale.
- Zoom range tightened to 9.5–31 units so tactical overview remains available without disappearing into the sky.
- `F` now centers the selected Hero **and restores a 13.5-unit character framing distance**.
- Hero selection still recenters the camera but preserves the player's current zoom.
- Camera focus now targets roughly chest height rather than the ground plane.
- Pan speed now scales more gently with zoom level.

## HUD/readability
- Canvas reference resolution changed to 1280×720 to keep text readable in a docked Unity Game view and scale cleanly at 1080p.
- Party, Quest, Target, Hero, Hand, and Controls panels rebalanced to protect the center gameplay view.
- Larger party/quest/target/Hero/card/button text.
- Card text increased from 13 to 16 design pixels.
- Combat log text increased and reduced to the latest 9 entries so lines remain legible.
- Interaction hint moved slightly upward so it no longer competes with the card hand.

## Preserved from 0.3.0 / 0.2.2
- Ascendance Location graph movement and Engagement rules.
- Runtime NavMesh physical movement and safe path-preview fallback.
- Hero selection, enemy targeting, hotkeys, and centralized input.
- Q001 objectives.
- Hero automatic-hit combat / Enemy 1d6 attack rule.
- Garruk Hero-card prototype behavior.
- Articulated Hero/Goblin proxies, combat feedback, health bars, floating damage, Stonebrook art pass.
