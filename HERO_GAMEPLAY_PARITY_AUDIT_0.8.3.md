# Ascendance Unity 0.8.3 — Hero Gameplay Parity Audit

Baseline: **Unity 0.8.2 Hero System Lifecycle Fix**, checked against the stabilized browser **3.36.11 Hero Card Functionality Audit / 3.36.13 automatic-hit baseline** and the current v3.1 rules.

## Authoritative-data checks

- **16/16 authored Heroes** are present.
- **323/323 Hero-card definitions** are present.
- Every Hero expands to an individual **30-card physical deck**.
- The complete **56-card 3.36.11 explicit repair set** is present in the Unity data.
- The nine current `Limit 1 per Quest/campaign` Hero cards are identified by the Unity runtime and their used state now persists through Save → Continue.
- Default adventure setup remains **5 cards in hand** per participating Hero.

## 0.8.3 executable repairs

### Turn / Action economy
- Each participating non-defeated Hero now completes a distinct Hero turn before the World Phase.
- **End Turn** preserves unused general Actions for legal Reactions for the rest of the Round.
- Mana Burn's granted Action is tracked as **Attack-card-only**; it cannot pay for movement, Basic Attack, Potions, or a non-Attack card.
- All remaining Actions are reset only after World/End resolution when the next Round begins.

### Shared attack/modifier pipeline
- Basic and Hero-card Attacks consume the same next-Attack/target-locked modifier pipeline.
- Battle Roar applies to Basic and card Attacks until Garruk ends his turn.
- Bloodrage is armed from damage once per Round and consumed by the next legal Attack.
- Challenge the Strongest / Conduit / Harvest Despair / Taunting Salute bonuses remain locked to their chosen Enemy and cannot be stolen by attacking a different target.
- Avatar of Rage applies +3 Attack while active and enforces its 1-Health floor.
- Crane Stance grants its connected-Location next-Attack range.
- Perfect Form reduces Attack-card Action cost (minimum 1), adds its Attack modifier, and draws after Attack-card resolution.
- Master's Hunt arms the next Attack card as free only when its marked-target Attack defeats that Enemy.

### Reaction timing
- Pre-Attack reactions are filtered by printed target/range/trigger and predicted damage/lethality.
- Elara lethal-save paths, Garruk damage prevention/floor, Soul Shield-style reduction, and target-redirection are supported in the current Enemy Attack window.
- Bone Shield / Dead Interpose are no longer offered unless the required Bone Servant actually exists in the correct Location.
- Kael Counter Strike is offered only after an Enemy Attack against Kael deals 0 damage.
- Garruk Blood for Blood is offered only after an Enemy in his Location actually damages him.
- Cassian Standing Ovation opens after a Hero in Cassian's Location defeats an Enemy.
- `Limit 1 per Quest/campaign` Reaction use is persisted.

### Summons / pieces
- Bone Servant and Spirit Totem placement validates maximums before spending the card or Actions.
- Servant Assault / Bone Legion validate that a legal Servant + Enemy state exists before payment.
- Grave Recall / Totem Recall validate a legal piece before payment.
- Hero-created pieces continue to persist through Save → Continue.

### Card-flow / persistence
- Draw, Hand, Discard, empty-deck reshuffle, required discard, and per-Hero switching remain authoritative Unity state.
- Save data now also preserves turn-completion state, target/focus modifiers, Perfect Form state, free-Attack-card state, temporary range, Attack-card-only Actions, and used Quest/campaign-limit cards.

## Deliberate migration boundaries found by the audit

This audit **does not claim all 323 specialized printed outcomes are fully migrated yet**. The card records are authoritative, and the shared engine now covers the high-risk Q001-executable paths above, but these remaining systems should be migrated before calling Hero cards full effect-level parity:

1. **Non-Attack incoming-damage Reaction windows** — Encounter/Search/Condition/direct damage still needs one generalized interruptable damage transaction so cards such as Ignore the Pain / Too Angry to Fall can react to every printed non-Attack source, not only current Enemy Attacks.
2. **Enemy non-Attack abilities / Spells** — Counter Melody / Spell Reflection-style windows require the full Enemy ability engine, which Q001's current enemies do not yet exercise.
3. **Forced movement and optional board-choice UI** — cards such as Shield Charge, Rampage follow-up movement/attack, Master Duelist out-and-back movement, pushes, and multi-location choices need the full movement/choice layer rather than deterministic shortcuts.
4. **Phase-aware status duration tracking** — some statuses say “until end of next turn” or “until end of next activation.” The current generic status timer is still coarser than those printed timings.
5. **Systems not migrated yet** — Equipment, full Search/Explore decks, full Location tags, advanced Encounter effects, and some card interactions depending on them remain intentionally deferred rather than invented.

## In-Editor audit command

Run:

**Ascendance → Validation → Run 0.8.3 Hero Parity Audit**

The command checks the 16-Hero roster, 323 definition count, 30-card deck expansion for every Hero, all 56 browser repair-set IDs, and current Quest/campaign-limit card records. It reports remaining migration boundaries separately from data failures.

## Recommended live regression

For the next Play Mode pass, prioritize:

1. Garruk — Battle Roar → Basic Attack; then take damage → Bloodrage → different target / correct target consumption.
2. Kael — Crane Stance ranged Basic Attack; Counter Strike after a 0-damage Enemy Attack; Perfect Form + Attack card.
3. Rowan — Scout Ahead and Master's Hunt marked-target kill → free next Attack card.
4. Mordren — attempt summon at cap (must not spend card/Actions), then Servant Assault / Bone Legion.
5. Asha — attempt Totem at cap (must not spend), Totem Recall, ally attacking in Totem Location.
6. Down/Revive — Down a Hero, revive with same-Location ally + Common Healing Potion.
7. Save mid-Round with changed hands/modifiers/pieces, Continue, verify state is unchanged.
8. End each of four Hero turns separately and verify World Phase waits for the fourth completed turn.
