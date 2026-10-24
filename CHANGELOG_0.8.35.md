# Ascendance Unity 0.8.35 — Quest Results & Return-to-Campaign Flow

Built directly from the runtime-verified 0.8.34 Campaign Hub & Quest Selection UI baseline.

## Added
- Full-screen Quest Results overlay generated entirely at runtime; no scene wiring required.
- Automatic result presentation when a Quest resolves successfully or fails.
- Dedicated `QUEST COMPLETE`, `QUEST FAILED`, `CAMPAIGN I VICTORY`, and `CAMPAIGN I DEFEAT` states.
- Objective outcome panel using the authored Quest victory/failure text plus the live resolution line when available.
- Actual reward capture around the authoritative Quest reward chain: Hero XP deltas, Party Gold, General Reputation, Next-Quest Supply, Party Morale, and the exact Equipment cards granted by the live deck.
- Runtime reward-log capture so special authored consequences such as Q003's WE002 Scout reduction remain visible.
- Campaign consequence panel showing retry eligibility, newly unlocked Quests, World Event advancement availability, day pressure, missed Quests, and Campaign Victory/Defeat closure.
- `RETURN TO CAMPAIGN` routes directly back to the verified 0.8.34 Campaign Hub.
- `SAVE CAMPAIGN` on the Results screen.
- Quest result snapshot persistence in the real save schema and restore path.
- Starting a new Quest clears the previous result snapshot and closes the result overlay.
- 0.8.35 Play Mode validator under `Ascendance/Validation/Test 0.8.35 - Quest Results Flow (Play Mode)`.

## Architecture
`QuestResultsRuntime0835.cs` does not award rewards or choose campaign routes. It brackets the existing `ResolveQuestReward0829()` reward chain to record before/after deltas, then reads 0.8.33/0.8.34 campaign state for presentation. Quest starts, retries, and World Event advancement remain owned by the previously verified campaign runtime.

## Persistence
`QuestResultSaveData0835` is stored alongside existing campaign state. Saving after a resolved Quest preserves the result screen snapshot, including actual reward deltas and granted Equipment labels. The validator backs up and restores any pre-existing save file while exercising this path.

## Intentionally unchanged
- Q001–Q012 objective/combat rules.
- WE001–WE004 progression gates, day thresholds, chapter rewards, and chapter penalties.
- 0.8.34 Campaign Hub routing rules.
- Q010 print-locked Fortress Gate / two-Ballista setup.
