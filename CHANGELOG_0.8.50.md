# Ascendance Unity 0.8.50 — Full Authored Content + Playtest Rules

Built from the runtime- and Windows-verified 0.8.49a baseline.

## Full v10.52 authored content catalog

- 16 Heroes / 323 Hero-card definitions remain loaded from the existing Hero database.
- 190 Loot definitions and all 254 physical Loot copies.
- 79 Loot rows flagged as Crafting Materials.
- 144 Equipment definitions, including their authored ability text.
- 100 Search-card definitions / 104 physical Search cards.
- 105 Encounter definitions. Stonebrook still uses its authored regional EN001–EN030 deck (35 physical cards); later-region Encounter definitions are available for later campaigns.
- 125 Enemy definitions.
- 236 Location definitions.
- 200 Crafting Recipe rows. The existing 20 recipes in the four currently implemented digital profession runtimes remain the executable subset until the remaining authored professions are implemented; no authored recipe row is discarded.
- 72 Quest rows and 24 World Event rows.
- 102 Status rows, 65 Keyword/reference rows, 13 World Region framework rows, and 58 Faction Reputation framework rows.

The imported content lives in `Assets/Resources/Ascendance/Data/full_content_0850.json` and is loaded through `FullContentCatalog0850`. Loading authored data is intentionally separate from claiming a mechanic is executable.

## Global playtest-rule fixes

- Search is unavailable while the acting Hero is Engaged with a living Enemy in the Hero's Location.
- Equipment with authored non-stat abilities is marked `[ABILITY]` in equipment labels.
- Equipment-management, Merchant, and Downtime Loadout surfaces show authored ability text.
- Successfully implemented Equipment triggers identify the item in the Adventure Log using `ITEM TRIGGER`.
- Older campaign saves reconcile newly added Search / Stonebrook Encounter physical card copies without erasing existing draw/discard history.

## Quest / exploration data alignment

- Quest Location pools now come from the authoritative v10.52 Quest rows rather than duplicated hard-coded pool lists.
- At Quest start only the scenario-required Starting Location is revealed. Normal Location cards remain hidden and enter visible play through Explore/reveal effects.
- Campaign I keeps its stable legacy runtime Location IDs for save compatibility while binding them to v10.52 authored Location IDs and metadata.

## Q001 — Defend Stonebrook corrections

- Q001 no longer starts with the Goblin Captain in Stonebrook.
- Opening force scales with party size:
  - 1 Hero: 1 Raider + 1 Archer
  - 2 Heroes: 2 Raiders + 1 Archer
  - 3 Heroes: 3 Raiders + 1 Archer
  - 4 Heroes: 3 Raiders + 2 Archers
- Every participating Hero begins Q001 with exactly 1 Common Healing Potion.
- Q001 begins with only Stonebrook revealed.
- The eight authored Q001 Exploration Locations are L001, L003, L006, L007, L017, L036, L043, and L047.
- The Goblin Captain enters when L006 Goblin Lookout is revealed, with the authored Scout and party-size scaling.
- Q001 reinforcement cadence uses the authored 1–4 Hero scaling.
- Reaching 5 Fire Tokens now resolves as a Q001 failure rather than only logging a warning.

## Validation

Use:

`Ascendance → Validation → Test 0.8.50 - Full Authored Content + Playtest Rules (Play Mode)`

0.8.50 is package/static validated until this validator is run successfully in Unity. 0.8.49a remains the latest fully runtime- and Windows-verified baseline until then.
