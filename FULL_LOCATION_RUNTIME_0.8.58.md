# Full Location Runtime — 0.8.58

## Runtime ownership

0.8.58 makes authored Locations active gameplay objects rather than catalog-only records. Location rules are layered onto the already-verified Quest, World Event, Search, Encounter, Loot/Equipment, and Crafting runtimes instead of replacing them.

Existing Stonebrook-specific code remains authoritative for its proven bespoke effects. `LocationRuntime0858.cs` provides the generalized execution layer for the full L000-L235 catalog and later-campaign locations.

## Location lifecycle

The live Location lifecycle is now:

1. Quest runtime instantiates the authored Location pool.
2. Reveal resolves the Location's authored Reveal effect.
3. Entry checks Location entry triggers.
4. First Visit remains pending while an Enemy occupies the Location; once eligible, it resolves exactly once.
5. First Visit draws from the Location's regional Encounter deck when authored.
6. Search obeys the Location's printed restriction/limit and any effect-granted Search exception.
7. Post-Search rules resolve only after Search-card choices and nested reward choices finish.
8. Ongoing rules feed movement, combat, healing, status, and turn-start/end pipelines.
9. Per-Quest/per-Round Location state is captured in campaign saves.

## Regional Encounter ownership

Regional First Visit routing is based on the authored Location region/deck metadata. A separate later-region Encounter deck/discard is maintained so a Sunscar, Storm Coast, Verdant, Ironspine, or Valewatch Location cannot consume Campaign I's Stonebrook Travel deck.

0.8.58 validates Location-trigger routing and regional deck state. Encounter-card effect execution remains the responsibility of the Encounter runtime.

## Choice sequencing safeguard

A Location Search can itself open a draw-2/keep-1 prompt, and the selected Search card can open another reward prompt. Some Locations also have an authored post-Search deck effect. 0.8.58 serializes that chain instead of allowing prompts to overwrite one another:

**Search choice → chosen Search card → nested Search-card choice (if any) → Location post-Search effect**.

L098 Survey Chamber and L104 Abandoned Clan Hall are explicit validator regressions for this behavior.

## Persistent Location state

`LocationState.UsedKeys0858` stores deterministic once-per-Quest / once-per-Round rule consumption. Local marker state uses existing persistent Location tags. Campaign saves additionally retain the active regional Encounter deck key, deck order, and discard order.

## Non-invented rules

When authored text depends on a system that does not yet exist as a real gameplay resource, the Location runtime does not silently replace it with another mechanic. For example, Fatigue/Stamina recovery is preserved/logged until a real Fatigue/Stamina resource exists. Quest-specific objectives remain owned by the Quest runtime, and World Event pressure/markers remain owned by the World Event runtime.

## Play Mode validation

Run:

**Ascendance → Validation → Test 0.8.58 - Full Location Runtime Execution (Play Mode)**

Expected:

`[ASCENDANCE][0.8.58 TEST] PASS — Full Location Runtime Execution completed: 74 passed, 0 failed.`
