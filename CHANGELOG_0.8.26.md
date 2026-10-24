# Ascendance Unity 0.8.26 — Loot, Equipment & Reputation Integration

Built from the runtime-verified 0.8.25a baseline.

## New runtime systems
- Imported the current v10.52 Loot Deck: 190 authored Loot definitions / 254 physical copies.
- Imported the current v10.52 Equipment catalog: 144 authored Equipment cards.
- Added persistent Loot Deck / Loot Discard and Equipment Deck / Equipment Discard state.
- Added per-Hero Loot Inventory and Acquired Equipment Inventory. Starting Equipment remains separate/equipped.
- Equipment rewards enter Inventory unequipped; 0.8.26 does not silently replace equipped gear or apply its stats.
- Added Campaign I General Reputation, clamped -5 to +6 per the Faction Reputation rules.
- Added persistent player-facing reward-choice state and HUD modal.

## Campaign I Encounter integration
- EN004: 1 Loot draw.
- EN005: real 25 Gold or 1 Item choice.
- EN007: conditional Item reward is a real Loot draw when all Fire Tokens are removed.
- EN008: Severity 1-2 gains 1 Item; Severity 3-4 has a real pay 10 Gold for +1 General Reputation choice; escort combat victory awards its Reputation + 15 Gold.
- EN009: Severity 3-4 draws 1 Loot; 5-6 draws Loot + Item; 7+ draws Loot + Equipment before the Saboteur fight.
- EN012: 1 Loot draw; Severity 5+ adds a second Item draw.
- EN013: real 1 Item or 20 Gold choice.
- EN014: real 30 Gold or 1 Loot choice after a fought/cleared encounter.
- EN017: Item/Equipment bands are real; Severity 7 tracks the exact Equipment and discards it if both Elite Goblins leave play undefeated.
- EN019: Campaign I legacy Reputation resolves as General Reputation for immediate/rescue bands that the current runtime can determine.
- EN024: +1 General Reputation if all Fire Tokens are removed.
- EN025: 35 Gold + 1 Loot draw.
- EN027: Item/Equipment bands are real; Severity 7 still chains the mandatory fixed Severity 3-4 follow-up.

## Persistence / UI
- Reward deck order/discards, General Reputation, Hero inventories, pending reward choice, and EN017 reversible reward tracking save/resume.
- HUD shows General Reputation and selected Hero Loot/Equipment inventory counts/previews.
- Reward choices block normal Hero actions and pause World/priority resolution until resolved, with Reaction and Encounter Deck Control retaining higher UI priority.

## Cleanup
- Non-applicable EN002/EN013 severity-gated consequence text no longer appears as CONTENT PENDING.

## Still intentionally pending
- Equipping/replacing acquired Equipment and applying acquired Equipment abilities/stats.
- Search Deck Loot/Equipment choice migration (SD001-SD020) beyond the existing Search runtime.
- Merchant buy/sell stock and discounts.
- Encounter choices requiring escort/avoidance/merchant/reputation subsystems not yet represented by deterministic current state.
