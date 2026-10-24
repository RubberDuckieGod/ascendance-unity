# Ascendance Unity 0.8.13 — Downed / Dead / Revival Enforcement

Stable baseline: **0.8.12 Round Transition & Reaction Reserve Enforcement** (runtime verified).

## Rules now enforced

- A Hero reduced to 0 Health is classified immediately:
  - another non-Defeated Hero in the same Location → **Downed**;
  - no such Hero → **Dead**.
- That classification is sticky. A Downed Hero does not become Dead later because an ally leaves.
- Downed Heroes remain at 0 Health and cannot Start Turn, spend normal Actions, Move, Attack, Explore, play Hero Cards, use Potions, activate Hero-damage abilities, or perform Reactions.
- Becoming Downed no longer deletes unspent Actions. Those Actions are unusable while Downed, but remain subject to normal turn/round timing if the Hero is legally revived.
- Dead Heroes have no Action reserve and cannot be revived by normal healing or a Common Healing Potion.
- Common Healing Potion revival is a real 1-Action transaction: the acting Hero must be non-Defeated, taking their turn, have a general Action, carry a Potion, and share the target's Location.
- Common Healing Potion restores 4 Health. Healing a Downed Hero above 0 immediately clears Downed.
- Revival itself never grants Actions and never resets `TurnCompleted`.
- Potion targeting now supports any injured or Downed non-Dead Hero in the acting Hero's Location. During a turn, select the intended Hero portrait and press **Potion / Revive → Selected Hero** so the player chooses the target explicitly.
- Q001 now records failure when all participating Heroes are Defeated; one Downed Hero cannot fail the Quest while another non-Defeated Hero remains.
- Defeated Hero visuals no longer expose selection/hover/health-bar interaction as if the Hero were active.
- Save/load persists `QuestFailed` and repairs contradictory Dead/Downed/Health state.

## Runtime validation

Use a fresh Play Mode game with at least 3 Heroes (the default 4-Hero party is recommended), then run:

**Ascendance → Validation → Test 0.8.13 - Downed Dead Revival (Play Mode)**

The deterministic test covers Downed classification, sticky Downed state, blocked self-Potion use, 1-Action Potion revival, 4-Health recovery, no Action grant on revival, later normal Start Turn for an unstarted revived Hero, preservation of held Actions for a completed Downed Hero, no second normal turn after revival, Dead classification, Dead-heal rejection, and the all-Heroes-Defeated Quest failure gate.
