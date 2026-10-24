# Ascendance Unity 0.8.52a — Full Search Deck Execution

Built from runtime-verified 0.8.51 (50/0).

## Scope
This smaller milestone executes the complete authored Search deck without claiming the separate Campaign I Location execution pass.

- SD001–SD019 retain their existing specialized runtime behavior.
- SD020–SD100 now have explicit runtime resolution paths.
- Search choices use the existing reward-choice UI.
- Search effects may reveal/inspect Exploration cards, manipulate Encounter cards, grant/cleanse statuses, heal/damage, grant Gold/Loot/Equipment, modify Travel Encounter severity, or create retained utility cards as authored.
- SD027 Forgotten Key and SD089 Goblin War Plans persist as retained Search utilities.
- SD036 Explore lock, SD043 Prepared Position, SD054 no-Move, SD058 Rubble, SD059 no-Healing, and SD092 Shamanic Markings alter live gameplay checks. Prepared Position is consumed only by the first damaging Enemy Attack, not by incidental/hazard damage.
- Search-triggered Travel Encounter post-resolution rewards are carried on the encounter state and persist through save/load.
- SD078 Frightened Refugee resolves its delayed Gold reward when the escort reaches Stonebrook.
- SD098 Beacon Instructions reduces the Q003 Beacon Action cost by 1 for the round.
- Effect-granted Search/Explore behavior from verified 0.8.51 remains intact.

## Deferred to 0.8.52b
Campaign I Location-card-owned Reveal, First Visit, ongoing, combat, and post-Encounter effects are not considered verified by this milestone.
