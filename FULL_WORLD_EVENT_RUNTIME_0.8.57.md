# Full World Event Runtime — 0.8.57

## Runtime ownership

World Events now own campaign-scale pressure and chapter consequences; Quest runtimes continue to own individual Quest objectives and victory/failure rules. This prevents the World Event layer from duplicating the 0.8.56 Quest engine while still allowing prior chapter outcomes to alter later Quest setup.

Campaign I's proven WE001–WE003 chapter-resolution logic remains on the existing 0.8.33 resolver. 0.8.57 adds active combat/travel modifiers there and owns resolution for the authored Campaign II–VI non-final chapters. Final campaign outcomes are recorded by the 0.8.57 finale layer.

## Campaign meters
- **Suspicion** — Campaign II, 0–3.
- **Instability** — Campaign III, 0–8, begins at 2.
- **Blight** — Campaign IV, 0–8, begins at 2.
- **Doomtide** — Campaign V, 0–8, begins at 2.
- **Sunfall** — Campaign VI, 0–8, begins at 2.

Meters, chapter flags, and deterministic once-keys are part of `CampaignProgressState0833` and `CampaignProgressSaveData0833`.

## Campaign II carryover execution
- WE005 Curfew can add E033 to the first qualifying Urban enemy Travel Encounter.
- WE006 objective discoveries raise Suspicion; Q015 Witness Testimony cancels the first qualifying increase each Quest.
- Suspicion 3 queues E036 for the next scripted enemy placement.
- Completing all WE006 Quests records the Supply Network reward and cancels the first Q021 Ritual Focus reinforcement.
- Q021 Ritual Focus destruction uses its authored reinforcement order E040 → E041 → E045 (solo skips the second Focus reinforcement).
- WE007 rolled Underground/Crypt/Catacomb/Ancient/Buried City Travel severity gains +1; fixed severity remains fixed.
- WE007 Undead treat Difficult Terrain as normal terrain.
- Royal Seal Recovered / LT164 removes one Crown Ward from Q024's printed setup; the failed-WE007 penalty adds one Crown Ward.

## Campaign III–VI pressure execution
0.8.57 implements the authored chapter-meter consequences through the live movement, encounter, enemy, healing, status, and Quest-start pipelines:
- first qualifying hazard/Encounter severity increases,
- first qualifying scripted enemy group Health increases,
- Fracture / Blight Patch / Flood / Mirage route markers,
- route movement surcharges and 1-Action clearing,
- Forge Heat / Tempest entry damage,
- Blight Patch and Eclipse recovery suppression,
- Hollow Pressure beneficial-Status loss,
- prior-chapter enemy/marker penalties,
- optional-Quest aids and chapter reward flags.

The Heartseed Ward, Beacon Lens, and First Dawn Prism each suppress their authored World Event pressure once per Round when available.

## Finale restrictions/state
- WE004 and WE008 suppress random Travel globally during their finales.
- WE012, WE016, WE020, and WE024 suppress random Travel only in the authored finale Location ranges.
- Finale events block voluntary Retreat state for the six campaign finales.
- Victory/defeat records campaign-specific unlock/loss flags and final Instability/Blight/Doomtide/Sunfall where applicable.

## Fresh Quest vs. save/load state
World Event route markers and once-keys are scenario-scoped. They are serialized so a mid-Quest save resumes exactly, but a fresh Quest start/retry clears prior scenario markers and that Quest's spent once-keys before applying current World Event setup.

## Player-facing Campaign Hub
For WE005–WE024, the Campaign Hub preview now exposes the active authored World Event modifier, reward/risk text, and pressure meter. Existing bespoke WE001–WE003 preview text remains unchanged.

## Validator coverage
The 0.8.57 Play Mode validator performs **58 checks**, covering:
- all 24 World Event definitions/IDs and six campaign arcs,
- authored 5/6/6/finale cadence,
- Campaign I combat/travel modifiers,
- Campaign II Suspicion, Curfew, Supply Network, Ritual Focus, Royal Seal, and finale rules,
- Campaign III Instability / Fractures / Forge Heat,
- Campaign IV Blight / Blight Patches / Hollow Pressure / Heartseed Ward,
- Campaign V Doomtide / Flood / Tempest Pressure / Beacon Lens,
- Campaign VI Sunfall / Mirage / Eclipse Pressure / First Dawn Prism,
- later-campaign Campaign Hub preview,
- finale travel suppression and final world-state flags,
- save-facing World Event fields,
- Quest-scope marker cleanup,
- absence of World Event `CONTENT PENDING` fallbacks.

## 0.8.57a parser hotfix

The first Unity 0.8.57 validator run completed 56 passed / 2 failed. Both failures traced to generalized Quest objective count inference reading numeric portions of authored IDs as counts (for example `L087`). The 0.8.57a hotfix corrects that parser and also de-duplicates overlapping Interact objective labels discovered during the audit (Q040, Q062, Q072). The expanded 0.8.57a validator contains 62 checks.
