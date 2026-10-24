# Full Quest Runtime — 0.8.56

## Runtime ownership

### Q001–Q012
These remain on the proven specialized Quest runtimes from 0.8.29–0.8.32. 0.8.56 does not replace or simplify their authored bespoke mechanics.

### Q013–Q072
These use `QuestRuntime0856`, which turns the authored v10.52 Quest rows into executable runtime state.

The generalized layer uses only authored information available in the Quest/Location/Enemy/Loot catalogs. Where a campaign-follow-on sentence records a future consequence but does not define a numeric gameplay modifier, 0.8.56 persists that sentence as World State instead of inventing a bonus or penalty.

## Executable goal types
- **Interact** — Wards, Nodes, Anchors, Seals, Caches, Survivors, facilities, Sites, and other explicit objective tokens.
- **Search** — Evidence and other authored Search-driven objectives.
- **Enemy** — required E### or named enemies/bosses.
- **Reach** — authored destination/escape/escort Locations.
- **Clear** — destination-clearing clauses such as “defeat all enemies there/in L###.”
- **Loot** — explicit LT### or printed-name Quest Item requirements.
- **Hold** — authored “through end Round N” objectives.
- **Choice** — finale choices such as Seal/Harmonize/Shatter, Purge/Seal/Reconcile, Bind/Banish/Bargain, and Restore/Shatter/Bear the Crown.

## Setup and phase handling
Enemy setup is parsed from authored setup text. Immediate setup enemies spawn when the Quest begins. Enemies authored to appear when a Location is revealed/accessed are queued against that Location. Enemies authored as “out of play” or spawned after objective progress remain delayed until their prerequisite path is available. This preserves later-campaign boss staging without pulling final bosses into Round 1.

## Campaign map and persistence
Later Quest Locations are built from the authoritative Location catalog and remain saveable through the existing `LocationState` persistence. Generalized Quest state additionally persists:
- current goal progress/completion,
- pressure value/limit,
- delayed enemy IDs,
- reveal-triggered spawns,
- final-choice state,
- campaign/follow-on flags,
- faction Reputation.

## Six campaign ranges
- Campaign I — Q001–Q012 — WE001–WE004
- Campaign II — Q013–Q024 — WE005–WE008
- Campaign III — Q025–Q036 — WE009–WE012
- Campaign IV — Q037–Q048 — WE013–WE016
- Campaign V — Q049–Q060 — WE017–WE020
- Campaign VI — Q061–Q072 — WE021–WE024

## Validator coverage
The 0.8.56 Play Mode validator checks the complete 72-Quest registry, 60 generalized blueprints, six campaign mappings/finales, representative later-campaign dynamic maps, delayed/reveal enemy setup, pressure failure, faction Reputation, named Quest Item mapping, clear objectives, finale choices, reward/world-state flags, save restoration, and regressions to Search, Encounter, Loot/Equipment, and Crafting catalogs.
