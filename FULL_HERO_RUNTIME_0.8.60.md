# Full Hero & Hero Card Runtime — 0.8.60

## Runtime ownership

0.8.60 closes the remaining execution gaps across the complete authored Hero layer: **16 Heroes**, **323 Hero-card definitions**, **30-card physical decks**, and **49 Reaction definitions**.

The milestone sits on top of the existing verified turn, reaction, status, movement, equipment, Search/Explore, Enemy, Location, Quest, and World Event systems. Existing Hero-specific rules remain authoritative; 0.8.60 supplies shared helpers and narrow card-specific clauses only where the authored text requires them.

## Card execution

The runtime now explicitly supports:
- conditional Attack bonuses and conditional Armor ignore,
- post-Armor bonus damage,
- cards-played / Attack-cards-played turn chains,
- multi-target and chained Attacks,
- deterministic legal movement and forced movement,
- ally healing/cleanse/Inspire effects,
- temporary Attack/Armor/card-cost statuses,
- Arcane / Consecrated Location interactions,
- Hero-created pieces and piece-triggered effects,
- entry Reactions and forced-movement prevention,
- targeted Enemy-ability reflection,
- special defeat and post-Attack follow-ups.

## Representative authored mechanics

The live layer includes, among others:
- Aldric: Hold Fast, Shield Escort, Veteran’s Resolve, Heroic Charge, Brace counter follow-up.
- Lyra: Backstab/Stealth condition, Smoke Bomb/Blink-style escape handling, Vanish Without a Trace.
- Elara: typed anti-Undead/Cursed bonuses, healing/cleanse/support chains, Light of Ascendance.
- Vaelis: Arcane Intellect/Infusion, Teleport/Dimension Door/Ethereal Passage, Arcane Ward, Time Stop, Spell Reflection.
- Rowan: Marked interactions, Overwatch, Trailblazer, Arrow Storm.
- Seraphine: Smite Evil condition, Guardian’s Charge, Beacon of Hope.
- Thorne: Rooted interactions, Protective Roots, Woodland Stride, Avatar of Nature.
- Cassian: afflicted-target bonuses, Rousing Verse, Grand Finale.
- Kael: card-chain Flying Kick and third-Attack Hundred Blows timing.
- Mira: Turret, Mechanical Trap, Field Workshop, Automatic Countermeasure, Master Engineer, Grand Invention.
- Malachar: Volatile Magic/Cataclysm self-damage and area Attack sequencing.
- Garruk: Bloodied/Executioner conditional damage and existing rage systems.
- Veyra: afflicted-target curse bonuses and Damnation.
- Mordren: Bone Servant sacrifice, movement, harvest, and Death Unbound.
- Lucien: isolation bonuses, Master Duelist footwork, Perfect Finale.
- Asha: Totemic Strike, Spirit Walk, Call of the Ancestors.

## Persistence

No new incompatible save format is required. The runtime uses already-persistent state for:
- Hero Statuses,
- hand/discard contents,
- UsedTurnAbilities card-chain markers,
- Hero-created pieces and their health/location.

## Validation

Run in Play Mode:

**Ascendance → Validation → Test 0.8.60 - Full Hero & Hero Card Runtime Execution (Play Mode)**

Expected:

`[ASCENDANCE][0.8.60 TEST] PASS — Full Hero & Hero Card Runtime Execution completed: 85 passed, 0 failed.`
