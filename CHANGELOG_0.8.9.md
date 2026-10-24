# 0.8.9 — Interpose Retarget Fix

- Fixed Interpose resolving its +1 Armor without actually redirecting the Enemy Attack.
- Retarget parsing now recognizes reaction text containing both `become the target` and `instead`, including `You become the target of that Attack instead.`
- Preserves 0.8.8 dynamic reaction revalidation after target changes.
- Preserves reset of protection attached only to the previous target when a pending Attack is redirected.
- Added a Play Mode helper that moves Garruk's existing Ignore the Pain and Feed the Rage cards into hand for a deterministic self-damage reaction test.
