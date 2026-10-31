# Online campaign creation validation

Validated runtime source: `597f44215ec2624ff0f215db80c6c487f05a84d7`.
Runtime build label remains `0.10.2h`, protocol `1`. This report covers online campaign creation, Hero selection, readiness, campaign slots and restoration. It does not certify every card's combat effect.

## Recorded results

| Verification | Expected | Actual |
| --- | --- | --- |
| Licensed Unity Editor gates | All required assertions pass with no Unity errors | **PASS: 250 checks, 0 errors**; includes 42 campaign checks and 32 lobby checks |
| Native server, plain transport | Real protocol/gameplay/persistence assertions pass | **PASS: 73 checks** |
| Native server, pinned TLS and private room key | Real protocol/gameplay/persistence assertions pass | **PASS: 80 checks** |
| Copied original h campaign restoration | Preserve original campaign and reconnect identities in the candidate server | **PASS: 34 checks** |
| Two graphical native clients and candidate server | Full campaign lifecycle, all 16 choices, strict gameplay-state comparisons | **PASS: 47 checks**, 0 failures, in the isolated [GitHub Actions run](https://github.com/RubberDuckieGod/ascendance-unity/actions/runs/41156884155) |
| Build provenance | Three builds identify the same clean runtime source | **PASS:** Linux server, Linux development client and Windows client identify the revision above; Windows development drivers absent |
| Deployment/helper regression tests | All selected tests pass without skips | **PASS: 233 tests**, 0 failures, 0 skips, including the revision-two installer safeguards |

The graphical CI result contains 47 expected/actual comparisons; every comparison is equal. Its `results.json` and these three screenshots are retained with that run:

- `campaign-full-sixteen-roster.png`
- `campaign-two-ready-nondefault-heroes.png`
- `campaign-fresh-two-nondefault-worlds.png`

The separate local graphical attempt, `native-campaign-tls-01`, **failed**: 38 checks passed, then reclaiming the original Aldric exceeded the unchanged 35-second world-entry deadline. The remaining nine checks were not reached. The client began entering Stonebrook during cleanup and recorded no managed errors. Heavy browser testing was running concurrently; resource contention is a possible explanation, not a proven cause. That attempt is retained as a failure and is not counted as 47 passing checks. The later isolated CI run completed all 47 checks with the same 35-second deadline and runtime, without relaxing assertions.

## Revision-two installer correction

The first owner installation **refused before activation** because the live campaign and persisted room differed. No successful production update is inferred from that attempt.

The exact original h binary was then tested without restarting it first. A legal Hero turn can persist its temporary command actor/target before restoring the live UI selection. That reproduced a mismatch in exactly `SelectedHeroId` and `SelectedEnemyId`, with identical gameplay and room revision. The previous copied-save test restarted the source process before comparison and did not expose this live-only case.

The replacement helper accepts those two presentation differences only for the exact reviewed original h assembly, with independently checked code identity and valid party/enemy references on both sides. Every other gameplay field and Enemy order must match. Revision, room creation/path and player ownership checks remain strict. Saved-copy and candidate restoration comparisons are unchanged; the helper does not force a save or reset a campaign.

| Additional check | Expected | Actual |
| --- | --- | --- |
| Original h live preflight | Accept only the demonstrated selection drift; reject altered Gold, Health, Level, XP, turn ownership, ordered cards/enemies or invalid references | **PASS: 17 actual native checks**; binds helper SHA `fea95a970ab54ca2f393f2ccdea9918108c89c2011764d22d564647aacc739bb` |
| Never-restarted source save copied to candidate | Restore that exact durable game and creation identity with no copied-room gameplay command | **PASS: 8 actual native checks** |
| Refusal diagnostics | Reveal only declared field paths and indexes, never values or credentials; leave the service unchanged | **PASS:** dedicated predicate/privacy fixtures and actual native diagnostic checks |

Revision two uses the same clean `597f442` server and Windows game binaries. Players who already installed that full-roster client do not need another game build. A revised installer still refuses any unexplained gameplay difference and reports safe field paths. These isolated reproductions do not claim the owner's private live data has been inspected or activation has completed.

## Gameplay and interaction expectations

| Mechanic | Expected behavior | Observed result |
| --- | --- | --- |
| Existing campaign join | Joining does not reset Gold, Round, board or progression | **PASS:** complete saved game unchanged; original fixture retained Gold 73, Round 4 |
| Explicit campaign creation | Confirmation precedes replacement and the current campaign is archived | **PASS:** confirmation alone changes nothing; original remains separately loadable |
| Full roster UI | Both independent clients see exactly the 16 authored Hero IDs with visible names/statuses/buttons | **PASS:** all 16 rendered within the actual client window |
| One Hero per player | One socket cannot reserve a second Hero with a raw JOIN | **PASS:** second JOIN refused with `DRAFT_PLAYER_ALREADY_BOUND`; intended Hero swap retains one identity |
| Claim concurrency/capacity | Duplicate claims fail; at most four distinct players claim Heroes | **PASS:** real socket refusals preserve existing owners; fifth claim refused |
| Readiness/start authority | No empty/unready start; only the creator starts the fully ready party | **PASS:** actual UI controls and server results agree |
| Draft restart | An unexpected disconnect reserves the Hero; draft state, creator identity and readiness survive process restart | **PASS:** both clients reclaimed their exact original draft identities after server/client restart |
| Fresh gameplay state | Only selected Heroes enter Q001, Round 1, with Gold 0, Level 1, XP 0 and no granted turn actions | **PASS:** actual Vaelis/Mira server state and both graphical worlds match |
| Opening cards/equipment | Five-card hand; exact 30-card authored personal multiset and exact starting gear/copies | **PASS:** all 16 fresh factories; actual Vaelis/Mira active server snapshots |
| Same-connection handoff | Ready lobby owners remain owners when the world opens | **PASS:** both Player IDs retained; no gameplay command submitted to create/start |
| Connected-player switching | Loading/creating cannot interrupt other connected players | **PASS:** server refusal leaves the complete previous game unchanged |
| Archive failure | A failed archive must not replace the active campaign or its durable file | **PASS:** deliberately occupied archive path refused; original file bytes/game unchanged |
| Original campaign load | Restore Health, Gold, Levels, XP, ordered cards, board and player credentials | **PASS:** complete game equality; Aldric returned with HP 5, Level 3, XP 260, Gold 73, Round 4 and the original Player ID |
| Enemy restoration | Preserve saved Enemy order and remove stale entries from the preceding campaign | **PASS:** strict saved-game equality across load and authority reconstruction |
| Active-campaign restart | Original campaign, both saved slots and creator management identity persist | **PASS:** actual server/player process restart |
| Connection UI recovery | Show network failure while disconnected; clear it after successful HELLO before explicit saved-Hero reclaim | **PASS:** real controller/server lifecycle; reclaim restores the original identity without changing gameplay |
| Private data boundaries | Public snapshots omit reconnect/creator credentials; temporary clients do not overwrite local campaigns | **PASS:** public-state checks and unchanged local save sentinels |
| Managed runtime errors | No managed diagnostic errors across any client process lifetime | **PASS:** errors from each process are accumulated before forced restarts and final assertions |

## All sixteen fresh Hero factories

Each row was tested through the actual fresh-campaign factory, selecting that Hero alone. Every row passed Level 1, XP 0, the authored maximum Health below, a five-card opening hand, the exact authored 30-card multiset, and authored starting equipment IDs/copy counts. The original game manager and its complete saved game remained unchanged after constructing all sixteen parties.

| Hero | Authored maximum Health | Factory result |
| --- | ---: | --- |
| Aldric | 12 | PASS |
| Lyra | 8 | PASS |
| Elara | 10 | PASS |
| Vaelis | 7 | PASS |
| Rowan | 9 | PASS |
| Seraphine | 11 | PASS |
| Thorne | 9 | PASS |
| Cassian | 9 | PASS |
| Kael | 9 | PASS |
| Mira | 9 | PASS |
| Malachar | 7 | PASS |
| Garruk | 12 | PASS |
| Veyra | 8 | PASS |
| Mordren | 8 | PASS |
| Lucien | 9 | PASS |
| Asha | 9 | PASS |

The independent graphical clients selected Vaelis and Mira. These graphical results do not mean sixteen graphical clients or every possible four-Hero party combination were run.

## Reproduction and limits

Editor entry point: `OnlineCampaignSetupRegressionTestTools0102.TestCampaignSetup` in a marked isolated validation project. The required campaign terminal is `Online Campaign Setup completed: 42 passed, 0 failed.`

The native flow uses `tools/native_campaign_setup.py` with candidate server/client paths, a private output directory, Xvfb and `--secure`. It drives production uGUI callbacks and actual sockets; it does not substitute a fake roster, claim response or game snapshot. It archives/restores generated test campaigns and uses independent disposable player profiles. No production campaign, account, invitation or credential was used.

The graphical run used Linux development players. Windows was built and its provenance/absence of development drivers verified; Windows gameplay was not executed by this run. The host lacked a usable audio output device and emitted FMOD initialization messages before managed diagnostic capture, so audio output is not certified. The 47-check campaign flow does not replace the separate legacy-server graphical compatibility run.

These validation results do not mean the DigitalOcean service has already been upgraded. Server installation and player instructions are in [Unity online campaigns](ONLINE_CAMPAIGNS_0102H.md).
