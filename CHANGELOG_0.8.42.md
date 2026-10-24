# Ascendance Unity 0.8.42 — Persistent Campaign Resume & Main Menu Continue Flow

Built directly from the runtime-verified 0.8.41 Quest Briefing & Ready-Up baseline.

## Player-facing Continue flow
- Main Menu now exposes **CONTINUE CAMPAIGN** only when the campaign JSON parses as a valid save.
- Continue shows a live summary with resume location, Quest, World Event, Campaign Day, party, and save timestamp.
- Corrupt / incomplete saves are rejected instead of silently falling through toward a fresh Stonebrook session.
- The exact save object validated by Continue is carried across the MainMenu → Stonebrook scene transition and applied once.

## Persistent resume surface
0.8.42 adds `ResumeSurface0842` to campaign save data. It records one of:
- `MID_QUEST`
- `RESULTS`
- `DOWNTIME`
- `CAMPAIGN_HUB`

A small resume controller applies the authoritative campaign save first, then restores the intended player-facing surface after Stonebrook UI components initialize.

### Important transient-state rule
Quest Briefing / READY state remains intentionally transient. Saving while the briefing is open records `CAMPAIGN_HUB`; Continue returns to quest selection with all READY flags cleared so the party must brief and ready again.

## Resume behavior
- Mid-Quest: no between-Quest overlays are reopened; control returns to restored gameplay.
- Quest Results: reopens the resolved Quest results.
- Downtime: opens Stonebrook Downtime directly and suppresses the legacy resolved-Quest Results auto-open.
- Campaign Hub: reopens quest selection directly.
- A resolved Quest with Results already dismissed defaults a newly-created save to Downtime.
- Pre-0.8.42 saves remain compatible: resolved saves fall back to Results; active saves fall back to Mid-Quest.

## Safe autosave
When the runtime is saveable, Ascendance now autosaves on:
- Quit to Main Menu
- Application quit
- Application/mobile pause

The existing save safety gate is preserved: movement, Reaction windows, and active resolution block saving, leaving the previous good save untouched.

## Regression scope
No combat, campaign progression, Quest rules, crafting, trading, profession, activity, merchant, loadout, or ready-up rules were changed. The only save-schema addition is `ResumeSurface0842`.
