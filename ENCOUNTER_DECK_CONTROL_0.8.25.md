# 0.8.25 Encounter Deck Control

Deck-control effects operate directly on `EncounterDeck`, preserving physical copies and exact order. The UI pauses normal Hero actions until the choice resolves.

Modes:
- `MayBottomTop` — EN003 reward.
- `ReorderTop2` — EN022 reward.
- `BottomOneTop2` — EN028 Severity 3–4.
- `ReorderTop3` — EN028 Severity 5–6.

The revealed snapshot and live deck order are both saved. If the live top changes unexpectedly before a choice resolves, the stale choice is canceled rather than being applied to different cards.

EN027's mandatory Severity 7 follow-up uses a fixed Severity 3 value, which selects the authored 3–4 band exactly and bypasses Skip/queued Severity modifiers because the card explicitly says the additional Encounter must be resolved. Other ongoing effects such as War Drums still apply normally to that Encounter.
