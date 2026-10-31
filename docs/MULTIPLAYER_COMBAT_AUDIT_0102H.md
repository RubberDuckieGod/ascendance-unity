# Multiplayer combat and Q001 villagers

This follow-up addresses reported Goblins not attacking Heroes or Villagers. It does not certify every Unity mechanic or imply that the website and Unity have complete rules parity.

## Reproduced before the change

Actual isolated native servers built from `597f44215ec2624ff0f215db80c6c487f05a84d7` demonstrated:

| Trigger | Expected | Observed defect |
|---|---|---|
| Q001 Goblin activates beside an unrescued Villager | Attack one Villager before a Hero | No civilian target or loss implementation existed. |
| Decline a pre-attack reaction | Accepted response followed by enemy damage | Authority returned `RULE_REJECTED` although the coroutine subsequently resolved the attack. The queued response was absent from the authority fingerprint. |
| Aldric's player views Rowan, then declines | Respond as the connection's owned Hero | Router sent the viewed Hero and ownership validation could reject the response, leaving combat waiting. |
| Two claimed Heroes finish turns in a saved four-Hero party | Explain which party members remain | Two unclaimed Heroes remained unfinished, so Enemy Phase never started; the HUD did not clearly identify that wait. |

Enemies did damage Heroes after reactions were resolved in the baseline: Rowan's Health changed from 9 to 0 and the Round advanced from 1 to 2. Merely opening two clients or executing commands had missed the failures above; previous graphical acceptance stopped before the second Hero ended their turn.

## Website rules selected by the owner

The owner explicitly chose website behavior over the conflicting printed all-four-lost failure condition:

- Q001 starts with four civilian tokens in Stonebrook.
- An able Goblin in Stonebrook attacks one available Villager instead of a Hero. Rescued or lost tokens cannot be attacked again. Non-Goblin targeting remains ordinary Hero targeting.
- Three Villagers lost fails the quest because rescuing two is impossible. The enemy phase stops after that failure.
- Escorting an available Villager costs one Action. The bonus is +15 Gold only when all four are rescued, matching the website's reward implementation.
- No Villager Health or Armor values were invented. A token loss represents the attack, matching the website.

Quest rules take priority over ordinary Hero target selection. Existing incapacitation rules still run before the Villager attack. This change does not introduce new cross-location civilian pursuit rules.

## Persistence and ownership

Villager states reuse the existing persistent Quest object type. New campaigns create four objects; legacy campaigns with only the rescued counter present a read-only four-token view. The first legitimate rescue or Villager attack materializes those objects. Loading, inspecting, or displaying an older save does not itself add fields or mutate its canonical data.

The reaction fix fingerprints the actual queued-resolution flag and rejects repeated responses while that flag is queued. It does not accept arbitrary commands that made no change. Online reaction routing uses the connection's owned Hero, independently of the inspected portrait. Solo control remains available for the whole party.

Unclaimed/disconnected Heroes are named in the HUD. They are not silently passed, removed, or assigned to another player. Use an appropriately sized new online campaign when the desired party has fewer Heroes; existing campaign ownership remains protected.

## Verification status

The complete Editor run from clean source `2bcf6efe9a20b2692fc9dad10f7b9fb5f6dfa585` passed **359 checks with zero errors**: master 54, authority 24, routing 30, network 27, reliability 22, snapshots 20, lobby 32, campaigns 42, reactions 30, combat HUD 11 and Villagers 67. The 67 Villager checks include nonlethal intermediate-tile Rooted movement, lethal pursuit and ranged-retreat traps, and rewards at 75/90 Gold.

Linux Dedicated Server, Linux development client and Windows shipping client all compiled successfully from that exact clean source. Development acceptance-driver symbols are absent from the Windows shipping assembly. Windows player execution has not been performed in this Linux environment.

The actual current `597f442` to candidate `2bcf6ef` private campaign preservation proof passed **47/47**. Two authenticated read-only restore cycles preserved every ordered gameplay field, archived campaign bytes, owner/slot metadata and room identity/revisions. Both original reconnect identities were then accepted. The candidate received **zero gameplay commands** throughout this preservation proof; no production campaign was read.

Actual two-GUI-client combat acceptance passed **92/92 semantic checks** over pinned TLS. Each rescue was verified against its accepted command response, the server and both independently advancing replicas. Both players completed their turns; legitimate declines while inspecting another Hero were accepted; enemies dealt real Hero damage; the game returned to Round 2; server/player restart and reconnect preserved the outcome. A separate fresh campaign lost exactly three unrescued Villagers, failed Q001 without attacking Heroes, and remained quiescent with no extra attacks or spawns.

The existing full-16-Hero online campaign graphical regression also passed **47/47** on the same compiled runtime. This covers roster selection, authored starting decks/gear, readiness, new/archived campaigns, progression restoration and restart, not every Hero card's complete mechanics certification. The plain native server regression passed **73/73**.

Two earlier GUI attempts remain failed harness runs: one waited for an extra reaction after the phase had ended; another read an older periodic replica file immediately after a successful rescue. Accepted responses and persisted authority proved the game effects were correct. The runner now requires fresh response/revision-bound state, exact server effects and both replicas before advancing. These corrections do not relax the gameplay assertions or count failed attempts as passes.

Linux graphical tests use software rendering on Xvfb. Screenshots were inspected; the development console shows missing-device FMOD output because this environment has no audio device. Audio functionality and final Windows visual quality have not been certified.

New validation gates cover civilian conservation, legal attacks and incapacitation, rescue bounds, failure/reward timing, save/load and legacy read-only behavior, reaction acknowledgment/idempotency/ownership, complete two-player rounds, and the actual HUD controls. Native graphical acceptance uses two independent isolated clients and checks gameplay state, not just process startup. Unexecuted cases are not passes.

Known limits: the authored Stonebrook Exalted civilian-loss prevention perk has not been certified by this follow-up; neither have all other factions, all 16 Heroes' complete card interactions, all quests, or every website/Unity difference. Realistic Asset Store pack selection is a separate presentation task. Windows runtime behavior requires its own execution evidence; a successful Linux graphical run alone is not a Windows player test.

Production campaign data is not used by these fixtures. Deployment, when performed, must retain the current campaign and use the existing backup, private-copy restore, TLS and canary checks.
