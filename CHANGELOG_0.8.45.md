# Ascendance Unity 0.8.45 — Multi-Campaign Framework & Valewatch Bootstrap

Built from runtime-verified 0.8.44.

- Adds explicit campaign identities C001 (The Goblin War) and C002 (Shadows Over Valewatch).
- Registers authored Campaign II structure: WE005–WE008 and Q013–Q024 from the current Campaign II source data.
- Campaign I Victory can continue into a dedicated Valewatch Campaign II launchpad.
- Campaign I is archived with Quest history, outcome, Campaign Days, and Defender of Stonebrook before active state moves to Campaign II.
- Continuation preserves post-Q012 Hero Level/XP, Equipment, personal inventory, profession XP, kill counts, Party Gold, General Reputation, and all other persistent Hero state.
- Adds CAMPAIGN_LAUNCHPAD save/resume routing and a Valewatch player-facing launchpad.
- Q013 is intentionally disabled until its live scenario/world runtime is implemented; no fake Quest start or Campaign Day spend is possible.
- Legacy saves without an explicit campaign ID normalize safely to Campaign I; WE005–WE008 can infer Campaign II for forward compatibility.
- Stonebrook Downtime is blocked while the party is at the Valewatch launchpad.
