# 0.8.17 Live Specialized Conditions

This build focuses on conditions actually granted by current playable Hero cards rather than implementing every dormant definition in `status_effects.json`.

Validated live conditions:
- Hidden — Camouflage; removed when the Hero attacks.
- Taunted — Holy Challenge; next legal Enemy attack is forced toward the source Hero if able.
- Knocked Down — Sweeping Leg; first Hero Action is spent standing, or the current one-action Enemy model spends its activation standing.
- Shocked — Shock Cartridge; prevents the next additional Action gain and is otherwise round-expiring.
- Burning — Explosive Charge and other unconditional Burning sources; 1 damage at end of activation, persistent until removed.
- Meteor Shard Burning — one-shot 1 damage at start of next Enemy activation, then removed after its Arcane prerequisite applies it.
- Bleeding — Briar Storm; 1 damage after an Attack is performed, persistent until removed.
- Nature's Blessing — removes Bleeding or Burning from its healed Hero when present.

Dormant status definitions remain data-authored for later content that actually grants them. They were not fabricated into current gameplay merely because their names exist in the database.
