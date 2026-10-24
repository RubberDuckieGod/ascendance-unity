# Ascendance Unity 0.8.58 — Full Location Runtime Execution

Built from the runtime-verified **0.8.57a — Full World Event Runtime Execution** baseline.

## Full authored Location catalog

- All **236 authored Locations (L000-L235)** are available to the live Location runtime.
- Existing Stonebrook bespoke Location behavior remains authoritative where it already existed.
- Later-campaign Quest pools instantiate their authored Location rows with Reveal, First Visit, Ongoing, Search, terrain, connection, and regional Encounter metadata.
- The locked First Visit rule remains unchanged: a Location's one First Visit is not consumed when an Enemy is present; it remains pending until an enemy-free qualifying entry.

## Reveal / Search / First Visit execution

0.8.58 adds catalog-driven execution for authored Location effects including:

- Health recovery, cleanse, Gold, Supply, Loot/Item, Evidence/Lore, Inspired, Guarded, Hidden, Attack/Armor/Range bonuses, free Move, and effect-granted free Search.
- Authored choice prompts such as Heal-or-Cleanse, Attack-or-Armor, Gold-or-Search, Supply-or-Gold, Evidence-or-Encounter, and discard-or-damage.
- Exploration/Location deck top/bottom and top-2/top-3 reorder effects.
- Regional Encounter deck top/bottom, reorder, keep/discard, and discard-one effects.
- Dual-deck inspection without one pending choice overwriting the other.
- Draw-2/keep-1 Search rules and post-Search effects.
- Deferred post-Search execution so a Location's post-Search rule waits for the chosen Search card and any nested Search-card reward choice to fully resolve.
- Conditional no-random-Encounter First Visit rules and explicit authored fallback Encounter text.

## Regional Encounter routing

Location First Visit now routes to the authored regional Encounter ranges:

- Stonebrook / Campaign I — EN001-EN030
- Crownlands / Valewatch — EN031-EN045
- Ironspine Mountains — EN046-EN060
- Verdant Reach — EN061-EN075
- Storm Coast — EN076-EN090
- Sunscar Expanse — EN091-EN105

The Location runtime owns **which regional deck is drawn and when**. Individual Encounter-card consequences remain owned by the Encounter runtime; 0.8.58 does not invent missing Encounter-card semantics.

## Ongoing Location mechanics

Catalog-driven hooks now participate in the live movement, combat, healing, status, and turn pipelines, including:

- Difficult Terrain and conditional movement surcharges.
- Cave-In/Flood/Ascent/Sunfall-dependent movement costs.
- Cover, ranged Range/Attack modifiers, hero Armor, enemy Attack/Armor modifiers, and first-attack/first-recovery rules.
- no-healing / reduced-healing rules and conditional recovery bonuses.
- enter/start/end-activation triggers.
- local persistent Location markers.
- Location Actions such as clearing Cave-In, Temple Square Sanctuary, and Fallen Giant Tree climb.
- finale Locations that suppress random regional Encounters when authored.

Rules that depend on a resource not present in the current Hero model (for example printed Fatigue/Stamina recovery) retain their authored text and log the unresolved resource interaction rather than substituting an invented mechanic.

## Save / resume

Campaign saves now retain:

- per-Location `UsedKeys0858` once-per-Quest / once-per-Round state,
- later-region Encounter deck identity,
- regional Encounter deck order,
- regional Encounter discard order.

Fresh Quest starts clear scenario-scoped Location once-keys; mid-Quest save/resume restores them.

## Validator

The Play Mode validator performs **74 checks**. Coverage includes:

- complete L000-L235 catalog/ID coverage,
- representative Locations from every campaign region,
- Reveal, Search, First Visit, movement, healing, combat, Cover, and Location Actions,
- conditional Cave-In/Flood/Sunfall rules,
- regional Encounter deck separation,
- effect-granted Search exceptions,
- deferred/nested Search choice sequencing at L098/L104,
- final-route no-random-Encounter Locations,
- locked enemy-present First Visit behavior,
- save-facing Location once-keys and regional Encounter deck state,
- no Location `CONTENT PENDING` fallback.

## Unity validation

Enter Play Mode and run:

**Ascendance → Validation → Test 0.8.58 - Full Location Runtime Execution (Play Mode)**

Expected final line:

`[ASCENDANCE][0.8.58 TEST] PASS — Full Location Runtime Execution completed: 74 passed, 0 failed.`

0.8.58 is a **candidate** until this validator completes with 0 failed in Unity.
