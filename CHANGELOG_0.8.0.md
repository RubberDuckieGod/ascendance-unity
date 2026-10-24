# Ascendance Unity 0.8.0 — Full Hero System Migration

Built from the confirmed-working 0.7.5 standalone/game-shell baseline.

## Authoritative data migration
- Imports all 16 Level-3 Heroes from `Ascendance_v10.50_Automatic_Hit_Equipment_Alignment.xlsx`.
- Imports all 323 authored Hero-card definitions.
- Expands `Copies` to exactly 30 physical cards per Hero (480 cards across the 16 personal decks).
- Imports Level-3 active Hero abilities from the stabilized 3.36.x ability data.
- Imports the authored Status Effects table.
- Uses Base Hero stats plus the printed starting-equipment Attack/Armor bonuses.

## Party / Hero Select
- Hero Select now exposes the full 16-Hero roster.
- Choose 1–4 Heroes; default remains Garruk / Rowan / Aldric / Elara.
- Clicking a selected Hero makes that Hero the initial controlled Hero.
- `REMOVE CONTROLLED HERO` removes that Hero from the party when more than one Hero is selected.
- Stonebrook spawns only the selected party and uses PartyIndex formation offsets, so any combination of four Heroes can spawn without stacking.

## Deck / hand engine
- Every Hero starts with their own shuffled 30-card Hero Deck.
- Every Hero draws a 5-card opening hand.
- Runtime Deck / Hand / Discard piles are separate.
- When the Hero Deck is empty, the Discard Pile reshuffles and drawing continues.
- At the new Round, each living Hero draws their current Draw Value (prototype default: 1 until per-Hero Draw Value data is authored).
- Draw/discard requirements are interactive: when a Hero must discard, click a card in their hand to discard it.
- Card hover shows authored category, speed, effective Action cost, and full rules text.

## Card resolution
- Removed the Garruk-only card restriction.
- All 323 authored cards are present in Unity and can enter hands through their correct decks.
- Shared automatic-hit Attack-card resolver handles Attack bonuses, Armor ignore, multi-target `each Enemy in your Location`, half-Health clauses, Conditions, draw/discard, healing, next-Attack bonuses, and free-Move grants where the authored text is structurally resolvable.
- Preserves the verified Garruk Battle Roar / Bloodrage / Groundbreaker / Avatar of Rage behavior.
- Choice-heavy or subsystem-specific text that depends on a not-yet-migrated subsystem remains visible as authored rules instead of inventing incompatible rules.

## Reactions
- Reaction cards cannot be played as normal Actions.
- Enemy Attacks now open a Reaction Window when eligible pre-Attack Reaction cards are present.
- Reaction UI supports Armor increases, damage reduction, defeat prevention, target redirection, Stealth, and Attack cancellation patterns.
- Improved Shield Wall is exposed as an Aldric Hero-ability reaction option.
- Saved Actions remain available for Reactions.

## Hero abilities
Level-1 through Level-3 abilities are loaded for every Level-3 Hero. Runtime consumers now cover the current Q001-relevant portions of:
- Aldric — Improved Shield Wall / Battle Ready
- Lyra — Improved Opportunist
- Elara — Improved Divine Grace / Blessed Presence
- Vaelis — Improved Arcane Mastery
- Rowan — Improved Trailblazer / Pathfinder hook
- Seraphine — Improved Oathbound / Sacred Duty
- Thorne — Improved Wild Growth
- Cassian — Improved Rousing Verse
- Kael — Improved Flowing Step
- Mira — Improved Field Engineer
- Malachar — Arcane Overload / Controlled Burn / Elemental Momentum
- Garruk — Bloodrage / Reckless Momentum / Pain into Power
- Veyra — Malefic Hex / Lingering Curse hook / Soul Siphon
- Mordren — Raise Servant / Grave Command / Death Harvest support
- Lucien — Duelist's Tempo / Fancy Footwork / Single Combat
- Asha — Spirit Guide / Totemic Presence / Ancestral Aid

## Health / Downed / Revive
- Every new Hero starts with one Common Healing Potion.
- `Potion / Revive` spends 1 Action.
- If a Downed ally occupies the same Location, the Potion is consumed to revive that Hero at 4 Health.
- Otherwise it heals the selected Hero for up to 4 Health.
- At 0 Health a Hero becomes Downed only if a non-Defeated allied Hero shares the Location; otherwise that Hero becomes Dead.
- Mira's Field Engineer can preserve a consumed Potion on a 4–6 and performs its draw/discard rider.

## Conditions
- Hero and Enemy Condition state is now persistent runtime data.
- Guarded / Inspired / Weakened / Vulnerable feed the combat calculations.
- Burning and Regenerating resolve during the current Round cleanup implementation.
- Authored Conditions remain visible and saveable even where a later subsystem will add additional timing consumers.

## Hero-created pieces
- Persistent `HeroCreatedPiece` state supports Bone Servants and Spirit Totems.
- Piece state includes type, owner, Location, ID, and Health.
- Pieces save/load with the campaign and render as world objects in the current asset-free presentation.

## Kill count and persistence
- Kill counts are tracked per Hero.
- Save data now preserves party membership, Level/XP, Health, Actions, potions, kill count, deck order, hand, discard, Conditions, pending combat modifiers, ability-use state, enemies, Locations, and Hero-created pieces.
- 0.7.x saves are migrated to a fresh 30-card deck / 5-card hand and one starting Potion when the old save has no deck fields.

## Known migration boundary
0.8.0 is the Hero-system migration, not the Equipment/Inventory, Search/Explore-deck, or full campaign-content migration. Cards that explicitly depend on those later systems keep their authored text and data, but the missing subsystem is not fabricated here. That is intentionally reserved for the following milestones.
