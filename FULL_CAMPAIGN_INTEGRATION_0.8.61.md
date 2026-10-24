# Full Campaign End-to-End Integration — 0.8.61

0.8.61 is the integration milestone that connects the already-verified authored runtimes into one six-campaign progression path.

## Verified path under test
- Campaign I — The Goblin War — Q001–Q012 / WE001–WE004
- Campaign II — Shadows Over Valewatch — Q013–Q024 / WE005–WE008
- Campaign III — Heart of the Mountain — Q025–Q036 / WE009–WE012
- Campaign IV — The Withering Wilds — Q037–Q048 / WE013–WE016
- Campaign V — The Drowned Tide — Q049–Q060 / WE017–WE020
- Campaign VI — Crown of the Fallen Sun — Q061–Q072 / WE021–WE024

The validator does not use direct Quest alignment to skip campaign routing. Apart from the initial Q001 state supplied by the campaign reset helper, later Quests start through `BeginCampaignQuest0833`; World Events advance through `AdvanceWorldEvent0833`; campaign handoffs use `BeginValewatchContinuation0845` / `BeginNextCampaign0856`; and successful Quest resolution uses the real reward/result chain.

## Day-clock rule
A newly continued campaign is a pre-Quest state:
- World Event Day = 0
- Total Campaign Day = 0

Starting the first Quest consumes the first Campaign Day and changes both counters to 1. A retry starts a new Campaign Day as normal.

## Campaign archive titles
- C001 — Defender of Stonebrook
- C002 — Wardens of Valewatch
- C003 — Heartwardens of Ironhaven
- C004 — Wardens of the Verdant Reach
- C005 — Stormwardens of Ashenport
- C006 — Dawnbearers of Sunspire

## Save compatibility
Older saves may contain `AwaitingCampaignRuntime0845=true` from the period when later campaigns were framework-only. Restore now keeps that lock only when the referenced campaign is actually not runtime-playable. With all six authored campaigns live, the stale flag migrates away automatically.
