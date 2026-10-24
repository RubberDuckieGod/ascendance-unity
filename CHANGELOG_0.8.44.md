# Ascendance Unity 0.8.44 — Campaign Completion / Victory & Defeat End-State Flow

- Adds a dedicated Campaign I terminal screen for Q012 Victory or Defeat.
- Victory summary shows Defender of Stonebrook plus the already-resolved Q012 rewards; it never re-awards them.
- Defeat summary records Stonebrook Falls and grants no victory reward/title.
- Final summary shows Campaign Days, quest record, final Gold/Reputation/Morale, and per-Hero Level/XP/Kills/combat stats.
- Adds CAMPAIGN_END as a persistent Continue destination, including compatibility normalization for older terminal saves.
- Q012 Quest Results now routes to VIEW CAMPAIGN END instead of a dead Campaign Hub.
- End screen supports Save Final Campaign, Return to Main Menu, and Start New Campaign (direct party selection).
- Starting a fresh campaign from the end screen preserves the final save until the new campaign is actually saved.
- Q012/campaign reward and progression rules are unchanged.
