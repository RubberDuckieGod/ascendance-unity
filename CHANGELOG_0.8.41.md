# Ascendance Unity 0.8.41 — Quest Briefing & Ready-Up Flow

Built from the runtime-verified 0.8.40 Downtime Equipment Management & Loadout Preparation baseline.

## Added
- Dedicated pre-Quest briefing overlay between Campaign/Stonebrook quest selection and authoritative Quest launch.
- Authored Story, Victory Objective, Failure condition, and Reward text from `QuestContent0829`.
- Current World Event progress, Campaign Day pressure, and authoritative outcome/stakes preview.
- Participating-party roster with current HP, level, Attack/Armor, equipped slots, and unequipped count.
- Per-participating-Hero READY / NOT READY state.
- BEGIN QUEST remains disabled until every participating Hero is READY.
- Retry briefings use the same ready-up gate.

## Campaign safety
- Opening, canceling, or readying in a briefing does **not** advance World Event Day or Total Campaign Day.
- No Gold, materials, profession XP, loadout, or Downtime Activity state is changed by the briefing.
- Campaign Day increments only when the existing authoritative `BeginCampaignQuest0833` succeeds.
- Briefing readiness is transient and intentionally not saved as campaign progress.
- Successful authoritative launch clears all pending briefing/readiness state.

## UI routing
- Campaign quest cards now say `BRIEF Qxxx` / `REVIEW RETRY Qxxx` and open the briefing instead of immediately launching.
- `BACK` cancels the briefing and returns to the Campaign Hub.
- The briefing closes automatically once the Quest launches.

## Validation
Play Mode menu:
`Ascendance -> Validation -> Test 0.8.41 - Quest Briefing & Ready-Up (Play Mode)`
