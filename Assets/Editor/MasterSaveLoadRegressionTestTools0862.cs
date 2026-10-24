#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Ascendance.EditorTools
{
    public static class MasterSaveLoadRegressionTestTools0862
    {
        [MenuItem("Ascendance/Validation/Test 0.10.2c - Master Save Load + Full Regression (Play Mode)")]
        public static void Test0862()
        {
            if (!Application.isPlaying) { Debug.LogError("[ASCENDANCE][0.10.2c TEST] Enter Play Mode first."); return; }
            var gm = AscendanceGameManager.Instance;
            if (gm == null) { Debug.LogError("[ASCENDANCE][0.10.2c TEST] No AscendanceGameManager found."); return; }
            gm.StartCoroutine(Run(gm));
        }

        private static IEnumerator Run(AscendanceGameManager gm)
        {
            int pass = 0, fail = 0;
            void Check(bool ok, string label)
            {
                if (ok) { pass++; Debug.Log("[ASCENDANCE][0.10.2c TEST] PASS — " + label); }
                else { fail++; Debug.LogError("[ASCENDANCE][0.10.2c TEST] FAIL — " + label); }
            }

            string path = AscendanceSaveSystem.SavePath;
            bool hadSave = File.Exists(path);
            byte[] previousSave = hadSave ? File.ReadAllBytes(path) : null;

            try
            {
                Check(AscendanceGameManager.BuildVersion == "0.10.2c", "Build identifies the Master Save/Load + Full Regression milestone.");

                var world = UnityEngine.Object.FindAnyObjectByType<StonebrookWorldBuilder>();
                int waitFrames = 0;
                while (world != null && !world.PresentationSpawnReady0862 && waitFrames++ < 300) yield return null;
                Check(world != null && world.PresentationSpawnReady0862, "Stonebrook defers runtime presentation repair until navigation setup has completed.");
                Check(NavMesh.GetSettingsCount() == 0 || (world != null && world.NavigationReady0862), "Runtime NavMesh reaches ready state when NavMesh agent settings are available.");

                int invalidAgents = gm.HeroAvatars.Values
                    .Where(a => a != null)
                    .Select(a => a.GetComponent<NavMeshAgent>())
                    .Count(a => a != null && a.enabled && !a.isOnNavMesh);
                Check(invalidAgents == 0, "No spawned Hero owns an enabled NavMeshAgent that is detached from the live NavMesh.");

                // Representative authoritative mid-Quest state covering persistent Hero, Enemy,
                // Location, inventory, profession, status, resource, and created-piece state.
                gm.SetParty(new[] { "Garruk", "Aldric", "Elara" }, "Garruk", true);
                gm.DebugResetCampaignProgress0833();
                yield return null;
                QuestResultsUI0835.Instance?.Close();
                DowntimeUI0836.Instance?.Close();
                CampaignHubUI0834.Instance?.Close();
                QuestBriefingUI0841.Instance?.CloseForResume0842();
                gm.CancelQuestBriefing0841();

                gm.DebugSetPartyGold(432);
                gm.DebugSetGeneralReputation(2);

                var g = gm.Heroes["Garruk"];
                g.Health = Mathf.Max(1, g.MaxHealth - 3);
                g.XP = 123;
                g.Level = 2;
                g.HealingPotions = 2;
                g.KillCount = 7;
                g.BlacksmithingXp0837 = 41;
                g.AlchemyXp0837 = 17;
                g.NextAttackBonus = 2;
                g.NextArmorIgnore = 1;
                g.FreeMoveReady = true;
                g.FreeMoveIgnoresEngagement = true;
                g.FreeSearchLocationId = "L000";
                g.UsedRoundAbilities.Add("0862_ROUND_USED");
                g.UsedTurnAbilities.Add("0862_TURN_USED");
                g.UsedQuestCards.Add("0862_QUEST_USED");
                g.KeptSearchEffects0852.Add("SD027");
                var expectedHandIds = g.Hand.Select(c => c.Id).ToList();
                var expectedDeckIds = g.Deck.ToList();
                var expectedDiscardIds = g.Discard.ToList();
                var expectedEquippedIds = g.StartingEquipment.SelectMany(e => Enumerable.Repeat(e.Id, Mathf.Max(1, e.Copies))).ToList();
                g.Statuses.Add(new StatusInstance
                {
                    Id = "0862_HERO_STATUS",
                    Name = "Regression Ward",
                    RemainingTurns = 1,
                    RemainingRounds = 1,
                    Source = "0.10.2c regression",
                    Beneficial = true,
                    ExpiryTiming = StatusExpiryTiming.EndOfRound,
                    RemainingExpiryTriggers = 1,
                    SourceHeroId = "Garruk",
                    AppliedRound = gm.Round
                });

                string lootId = gm.LootDeck.FirstOrDefault();
                if (!string.IsNullOrEmpty(lootId) && !g.LootInventory.Contains(lootId)) g.LootInventory.Add(lootId);
                string equipmentId = gm.EquipmentDeck.FirstOrDefault();
                if (!string.IsNullOrEmpty(equipmentId) && !g.AcquiredEquipment.Contains(equipmentId)) g.AcquiredEquipment.Add(equipmentId);

                var l000 = gm.Locations["L000"];
                l000.SearchRequests = 2;
                l000.TravelEncounterRequests = 3;
                l000.TemporaryTags.Add(new LocationTagInstance
                {
                    Tag = "0862 Regression Tag",
                    Source = "0.10.2c regression",
                    SourceHeroId = "Garruk",
                    ExpiryTiming = StatusExpiryTiming.EndOfRound,
                    RemainingExpiryTriggers = 1
                });
                l000.UsedKeys0858.Add("0862_LOCATION_KEY");

                var enemy = gm.Enemies.Values.FirstOrDefault(e => e != null && !e.Defeated);
                string enemyId = enemy?.Id;
                if (enemy != null)
                {
                    enemy.Health = Mathf.Max(1, enemy.MaxHealth - 1);
                    enemy.ActivatedThisRound = true;
                    enemy.UsedKeys0859.Add("0862_ENEMY_KEY");
                    enemy.MovementStepsThisActivation0859 = 2;
                    enemy.FocusHeroId0859 = "Garruk";
                    enemy.Statuses.Add(new StatusInstance
                    {
                        Id = "0862_ENEMY_STATUS",
                        Name = "Regression Mark",
                        RemainingTurns = 1,
                        RemainingRounds = 1,
                        Source = "0.10.2c regression",
                        Beneficial = false,
                        ExpiryTiming = StatusExpiryTiming.EndOfRound,
                        RemainingExpiryTriggers = 1,
                        SourceHeroId = "Garruk",
                        AppliedRound = gm.Round
                    });
                }

                gm.HeroCreatedPieces.RemoveAll(p => p != null && p.PieceId == "0862_TEST_PIECE");
                gm.HeroCreatedPieces.Add(new HeroCreatedPiece
                {
                    PieceId = "0862_TEST_PIECE",
                    Type = "Regression Totem",
                    OwnerHeroId = "Garruk",
                    LocationId = "L000",
                    Health = 4
                });

                Check(CampaignResumeRuntime0842.DetermineCaptureSurface0842(gm) == CampaignResumeRuntime0842.MidQuest, "Live gameplay identifies MID_QUEST as the authoritative resume surface.");
                Check(AscendanceSaveSystem.SaveCurrent(gm, out _), "Representative mid-Quest state saves through the authoritative save system.");
                var midSave = AscendanceSaveSystem.Load();
                Check(midSave != null && midSave.Version == "0.10.2c" && midSave.ResumeSurface0842 == CampaignResumeRuntime0842.MidQuest, "Save JSON records 0.10.2c and the MID_QUEST resume surface.");
                Check(midSave != null && midSave.PartyGold == 432 && midSave.GeneralReputation == 2, "Party Gold and General Reputation persist into the save payload.");

                var savedHero = midSave?.Heroes?.FirstOrDefault(h => h.Id == "Garruk");
                Check(savedHero != null && savedHero.Health == g.Health && savedHero.XP == 123 && savedHero.Level == 2 && savedHero.HealingPotions == 2 && savedHero.KillCount == 7, "Hero Health, Level/XP, consumables, and persistent kill count serialize together.");
                Check(savedHero != null && savedHero.BlacksmithingXp0837 == 41 && savedHero.AlchemyXp0837 == 17 && savedHero.NextAttackBonus == 2 && savedHero.NextArmorIgnore == 1 && savedHero.FreeMoveReady, "Profession and active Hero runtime modifiers serialize together.");
                Check(savedHero != null && savedHero.Statuses.Any(s => s.Name == "Regression Ward") && savedHero.UsedRoundAbilities.Contains("0862_ROUND_USED") && savedHero.UsedTurnAbilities.Contains("0862_TURN_USED") && savedHero.UsedQuestCards.Contains("0862_QUEST_USED"), "Hero statuses and once-per-round/turn/Quest usage survive capture.");
                Check(savedHero != null && savedHero.HandIds.SequenceEqual(expectedHandIds) && savedHero.DeckIds.SequenceEqual(expectedDeckIds) && savedHero.DiscardIds.SequenceEqual(expectedDiscardIds), "Hero hand, deck order, and discard pile survive capture exactly.");
                Check(savedHero != null && savedHero.EquippedEquipmentIds.SequenceEqual(expectedEquippedIds), "Equipped loadout survives capture with authored copy counts intact.");
                Check(savedHero != null && (string.IsNullOrEmpty(lootId) || savedHero.LootInventoryIds.Contains(lootId)) && (string.IsNullOrEmpty(equipmentId) || savedHero.AcquiredEquipmentIds.Contains(equipmentId)) && savedHero.KeptSearchEffects0852.Contains("SD027"), "Loot, acquired Equipment, and kept Search effects survive capture.");

                var savedLocation = midSave?.Locations?.FirstOrDefault(l => l.Id == "L000");
                Check(savedLocation != null && savedLocation.SearchRequests == 2 && savedLocation.TravelEncounterRequests == 3 && savedLocation.TemporaryTags.Any(t => t.Tag == "0862 Regression Tag") && savedLocation.UsedKeys0858.Contains("0862_LOCATION_KEY"), "Location counters, temporary tags, and authored once-keys survive capture.");

                var savedEnemy = midSave?.Enemies?.FirstOrDefault(e => e.Id == enemyId);
                Check(enemy == null || (savedEnemy != null && savedEnemy.Health == enemy.Health && savedEnemy.ActivatedThisRound && savedEnemy.UsedKeys0859.Contains("0862_ENEMY_KEY") && savedEnemy.MovementStepsThisActivation0859 == 2 && savedEnemy.FocusHeroId0859 == "Garruk" && savedEnemy.Statuses.Any(s => s.Name == "Regression Mark")), "Enemy mutable state, statuses, once-keys, movement, and focus survive capture.");
                Check(midSave != null && midSave.HeroCreatedPieces.Any(p => p.PieceId == "0862_TEST_PIECE" && p.LocationId == "L000" && p.Health == 4), "Hero-created persistent pieces survive capture.");

                // Deliberately destroy the representative runtime state, then prove ApplySaveData
                // reconstructs the saved snapshot rather than accidentally preserving memory.
                gm.DebugSetPartyGold(0);
                gm.DebugSetGeneralReputation(0);
                g.Health = g.MaxHealth;
                g.XP = 0;
                g.Level = 1;
                g.HealingPotions = 0;
                g.KillCount = 0;
                g.BlacksmithingXp0837 = 0;
                g.AlchemyXp0837 = 0;
                g.NextAttackBonus = 0;
                g.NextArmorIgnore = 0;
                g.FreeMoveReady = false;
                g.FreeMoveIgnoresEngagement = false;
                g.FreeSearchLocationId = null;
                g.Statuses.Clear();
                g.Hand.Clear();
                g.Deck.Clear();
                g.Discard.Clear();
                g.StartingEquipment.Clear();
                g.UsedRoundAbilities.Clear();
                g.UsedTurnAbilities.Clear();
                g.UsedQuestCards.Clear();
                g.LootInventory.Clear();
                g.AcquiredEquipment.Clear();
                g.KeptSearchEffects0852.Clear();
                l000.SearchRequests = 0;
                l000.TravelEncounterRequests = 0;
                l000.TemporaryTags.Clear();
                l000.UsedKeys0858.Clear();
                if (enemy != null)
                {
                    enemy.Health = enemy.MaxHealth;
                    enemy.ActivatedThisRound = false;
                    enemy.UsedKeys0859.Clear();
                    enemy.MovementStepsThisActivation0859 = 0;
                    enemy.FocusHeroId0859 = null;
                    enemy.Statuses.Clear();
                }
                gm.HeroCreatedPieces.RemoveAll(p => p != null && p.PieceId == "0862_TEST_PIECE");

                if (midSave != null) gm.ApplySaveData(midSave);
                yield return null;

                g = gm.Heroes["Garruk"];
                l000 = gm.Locations["L000"];
                var restoredEnemy = !string.IsNullOrEmpty(enemyId) && gm.Enemies.TryGetValue(enemyId, out var foundEnemy) ? foundEnemy : null;
                Check(gm.PartyGold == 432 && gm.GeneralReputation == 2, "ApplySaveData restores Party Gold and Reputation after destructive mutation.");
                Check(g.Health == savedHero?.Health && g.XP == 123 && g.Level == 2 && g.HealingPotions == 2 && g.KillCount == 7, "ApplySaveData restores Hero progression and consumable state after destructive mutation.");
                Check(g.BlacksmithingXp0837 == 41 && g.AlchemyXp0837 == 17 && g.NextAttackBonus == 2 && g.NextArmorIgnore == 1 && g.FreeMoveReady && g.FreeMoveIgnoresEngagement && g.FreeSearchLocationId == "L000", "ApplySaveData restores Hero profession and active modifier state.");
                Check(g.Statuses.Any(s => s.Name == "Regression Ward") && g.UsedRoundAbilities.Contains("0862_ROUND_USED") && g.UsedTurnAbilities.Contains("0862_TURN_USED") && g.UsedQuestCards.Contains("0862_QUEST_USED"), "ApplySaveData restores Hero statuses and usage locks.");
                Check(g.Hand.Select(c => c.Id).SequenceEqual(expectedHandIds) && g.Deck.SequenceEqual(expectedDeckIds) && g.Discard.SequenceEqual(expectedDiscardIds), "ApplySaveData restores Hero hand, deck order, and discard pile after destructive mutation.");
                Check(g.StartingEquipment.SelectMany(e => Enumerable.Repeat(e.Id, Mathf.Max(1, e.Copies))).SequenceEqual(expectedEquippedIds), "ApplySaveData restores the equipped loadout after destructive mutation.");
                Check((string.IsNullOrEmpty(lootId) || g.LootInventory.Contains(lootId)) && (string.IsNullOrEmpty(equipmentId) || g.AcquiredEquipment.Contains(equipmentId)) && g.KeptSearchEffects0852.Contains("SD027"), "ApplySaveData restores Loot, Equipment, and kept Search state.");
                Check(l000.SearchRequests == 2 && l000.TravelEncounterRequests == 3 && l000.TemporaryTags.Any(t => t.Tag == "0862 Regression Tag") && l000.UsedKeys0858.Contains("0862_LOCATION_KEY"), "ApplySaveData restores Location counters, tags, and once-keys.");
                Check(enemy == null || (restoredEnemy != null && restoredEnemy.ActivatedThisRound && restoredEnemy.UsedKeys0859.Contains("0862_ENEMY_KEY") && restoredEnemy.MovementStepsThisActivation0859 == 2 && restoredEnemy.FocusHeroId0859 == "Garruk" && restoredEnemy.Statuses.Any(s => s.Name == "Regression Mark")), "ApplySaveData restores Enemy state without leaking the destructive mutation.");
                Check(gm.HeroCreatedPieces.Any(p => p.PieceId == "0862_TEST_PIECE" && p.LocationId == "L000" && p.Health == 4), "ApplySaveData restores Hero-created persistent pieces.");

                string Fingerprint()
                {
                    var hero = gm.Heroes["Garruk"];
                    var location = gm.Locations["L000"];
                    var e = !string.IsNullOrEmpty(enemyId) && gm.Enemies.TryGetValue(enemyId, out var ee) ? ee : null;
                    return string.Join("|",
                        gm.CurrentCampaignId0845, gm.CurrentQuestId0829, gm.WorldEventDay0833, gm.TotalCampaignDay0833,
                        gm.PartyGold, gm.GeneralReputation, hero.Level, hero.XP, hero.Health, hero.HealingPotions, hero.KillCount,
                        hero.BlacksmithingXp0837, hero.AlchemyXp0837, hero.NextAttackBonus, hero.NextArmorIgnore,
                        hero.Statuses.Count, hero.LootInventory.Count, hero.AcquiredEquipment.Count, hero.KeptSearchEffects0852.Count,
                        location.SearchRequests, location.TravelEncounterRequests, location.TemporaryTags.Count, location.UsedKeys0858.Count,
                        e?.Health ?? -1, e?.Statuses.Count ?? -1, e?.UsedKeys0859.Count ?? -1,
                        gm.HeroCreatedPieces.Count);
                }

                string stableFingerprint = Fingerprint();
                bool cyclesStable = true;
                for (int i = 0; i < 3; i++)
                {
                    if (!AscendanceSaveSystem.SaveCurrent(gm, out _)) { cyclesStable = false; break; }
                    var cycleSave = AscendanceSaveSystem.Load();
                    if (cycleSave == null) { cyclesStable = false; break; }
                    gm.ApplySaveData(cycleSave);
                    yield return null;
                    if (Fingerprint() != stableFingerprint) { cyclesStable = false; break; }
                }
                Check(cyclesStable, "Three consecutive save/load cycles are idempotent across representative campaign state.");

                // Surface regression: MID_QUEST -> RESULTS -> DOWNTIME -> CAMPAIGN_HUB.
                var resume = UnityEngine.Object.FindAnyObjectByType<CampaignResumeController0842>();
                Check(resume != null, "Persistent resume controller remains installed for 0.10.2c.");
                Check(AscendanceSaveSystem.SaveCurrent(gm, out _) && AscendanceSaveSystem.Load()?.ResumeSurface0842 == CampaignResumeRuntime0842.MidQuest, "MID_QUEST surface still persists after master state restoration.");

                gm.DebugResolveCampaignQuest0833(true);
                yield return null;
                Check(gm.QuestComplete && QuestResultsUI0835.Instance != null && QuestResultsUI0835.Instance.IsOpen, "Quest resolution still opens the Results surface.");
                Check(AscendanceSaveSystem.SaveCurrent(gm, out _) && AscendanceSaveSystem.Load()?.ResumeSurface0842 == CampaignResumeRuntime0842.Results, "RESULTS surface persists through save/load.");

                QuestResultsUI0835.Instance?.ReturnToCampaign();
                yield return null;
                Check(DowntimeUI0836.Instance != null && DowntimeUI0836.Instance.IsOpen && gm.IsDowntimeAvailable0836, "Results-to-Downtime flow remains live after persistence regression.");
                Check(AscendanceSaveSystem.SaveCurrent(gm, out _) && AscendanceSaveSystem.Load()?.ResumeSurface0842 == CampaignResumeRuntime0842.Downtime, "DOWNTIME surface persists through save/load.");

                DowntimeUI0836.Instance?.ChooseNextQuest();
                yield return null;
                Check(CampaignHubUI0834.Instance != null && CampaignHubUI0834.Instance.IsOpen, "Downtime-to-Campaign Hub flow remains live.");
                Check(AscendanceSaveSystem.SaveCurrent(gm, out _) && AscendanceSaveSystem.Load()?.ResumeSurface0842 == CampaignResumeRuntime0842.CampaignHub, "CAMPAIGN_HUB surface persists through save/load.");

                // Terminal campaign boundary and Campaign II handoff regression. This intentionally
                // uses the same authoritative debug route already used by the verified end-state tests.
                QuestResultsUI0835.Instance?.Close();
                DowntimeUI0836.Instance?.Close();
                CampaignHubUI0834.Instance?.Close();
                gm.DebugResetCampaignProgress0833();
                gm.DebugStartQuest0829("Q012");
                gm.DebugResolveCampaignQuest0833(true);
                yield return null;
                Check(gm.CampaignCompleted0833 && gm.CanContinueToValewatch0845, "Campaign I terminal Victory remains reachable before persistence handoff validation.");
                QuestResultsUI0835.Instance?.ReturnToCampaign();
                yield return null;
                Check(CampaignResumeRuntime0842.DetermineCaptureSurface0842(gm) == CampaignResumeRuntime0842.CampaignEnd, "Terminal Campaign I Victory identifies CAMPAIGN_END as its resume surface.");
                Check(AscendanceSaveSystem.SaveCurrent(gm, out _) && AscendanceSaveSystem.Load()?.ResumeSurface0842 == CampaignResumeRuntime0842.CampaignEnd, "CAMPAIGN_END survives authoritative save capture.");

                bool continued = gm.BeginValewatchContinuation0845();
                yield return null;
                Check(continued && gm.CurrentCampaignId0845 == CampaignContent0845.CampaignII && gm.WorldEventDay0833 == 0 && gm.TotalCampaignDay0833 == 0, "Campaign I -> II handoff starts Campaign II at Day 0 before its first Quest.");
                Check(gm.CampaignArchive0845.Count == 1 && gm.CampaignArchive0845[0].CampaignId == CampaignContent0845.CampaignI && gm.CampaignArchive0845[0].Outcome == "VICTORY", "Campaign I archive exists before Campaign II save/load.");
                Check(AscendanceSaveSystem.SaveCurrent(gm, out _), "Campaign II Day-0 handoff saves successfully.");
                var dayZeroSave = AscendanceSaveSystem.Load();
                if (dayZeroSave != null) gm.ApplySaveData(dayZeroSave);
                yield return null;
                Check(dayZeroSave != null && gm.CurrentCampaignId0845 == CampaignContent0845.CampaignII && gm.WorldEventDay0833 == 0 && gm.TotalCampaignDay0833 == 0 && gm.CampaignArchive0845.Count == 1, "Campaign II Day-0 identity and prior-campaign archive survive save/load.");

                int beforeWorldDay = gm.WorldEventDay0833;
                int beforeTotalDay = gm.TotalCampaignDay0833;
                bool beganQ013 = gm.BeginCampaignQuest0833("Q013");
                yield return null;
                Check(beganQ013 && gm.CurrentQuestId0829 == "Q013" && gm.WorldEventDay0833 == beforeWorldDay + 1 && gm.TotalCampaignDay0833 == beforeTotalDay + 1, "Campaign II first Quest consumes Day 1 only when Q013 actually begins.");
                Check(AscendanceSaveSystem.SaveCurrent(gm, out _), "Campaign II Day-1 active Quest saves successfully.");
                var dayOneSave = AscendanceSaveSystem.Load();
                if (dayOneSave != null) gm.ApplySaveData(dayOneSave);
                yield return null;
                Check(dayOneSave != null && gm.CurrentCampaignId0845 == CampaignContent0845.CampaignII && gm.CurrentQuestId0829 == "Q013" && gm.WorldEventDay0833 == 1 && gm.TotalCampaignDay0833 == 1 && gm.CampaignArchive0845.Count == 1, "Campaign II active Q013 Day-1 state survives save/load with archive intact.");

                // One-click inherited full regression gate. 0.10.2c explicitly re-baselines
                // the live runtime before every inherited validator. Without this isolation the
                // prior Q013 save/load scenario can leave a reward choice and Q013 enemies alive,
                // contaminating validators that were authored against a fresh Q001 runtime.
                bool nestedPass = false;

                yield return PrepareNestedBaseline0862a(gm);
                yield return RunNestedValidator(FullLocationRuntimeTestTools0858.Test0858, "Full Location Runtime Execution completed:", ok => nestedPass = ok);
                Check(nestedPass, "Inherited 0.8.58c Full Location Runtime Execution suite passes under the 0.10.2c baseline.");

                nestedPass = false;
                yield return PrepareNestedBaseline0862a(gm);
                yield return RunNestedValidator(FullEnemyRuntimeTestTools0859.Test0859, "Full Enemy Runtime Execution completed:", ok => nestedPass = ok);
                Check(nestedPass, "Inherited 0.8.59b Full Enemy Runtime Execution suite passes under the 0.10.2c baseline.");

                nestedPass = false;
                yield return PrepareNestedBaseline0862a(gm);
                Check(gm.EnemyAvatars.Count == gm.Enemies.Count && gm.EnemyAvatars.Keys.All(id => gm.Enemies.ContainsKey(id)),
                    "Nested reset prunes stale Enemy presentation registrations before the next regression suite.");
                yield return RunNestedValidator(FullHeroRuntimeTestTools0860.Test0860, "Full Hero & Hero Card Runtime Execution completed:", ok => nestedPass = ok);
                Check(nestedPass, "Inherited 0.8.60c Full Hero & Hero Card Runtime Execution suite passes under the 0.10.2c baseline.");

                nestedPass = false;
                yield return PrepareNestedBaseline0862a(gm);
                yield return RunNestedValidator(FullCampaignIntegrationTestTools0861.Test0861, "Full Campaign End-to-End Integration completed:", ok => nestedPass = ok);
                Check(nestedPass, "Inherited 0.8.61 Full Campaign End-to-End Integration suite passes under the 0.10.2c baseline.");

                // Corrupt-save handling remains part of the master gate.
                File.WriteAllText(path, "{ 0.10.2c intentionally corrupt regression payload");
                Check(AscendanceSaveSystem.HasSave && !AscendanceSaveSystem.HasValidSave, "Corrupt save files are not accepted as valid Continue state.");
                Check(!AscendanceSaveSystem.TryLoad(out _, out var corruptMessage) && !string.IsNullOrWhiteSpace(corruptMessage), "Corrupt save parsing fails cleanly with a player-facing message.");
            }
            finally
            {
                try
                {
                    if (hadSave && previousSave != null)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        File.WriteAllBytes(path, previousSave);
                    }
                    else if (File.Exists(path)) File.Delete(path);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    fail++;
                }
            }

            bool restoredOriginalSave = File.Exists(path) == hadSave;
            if (restoredOriginalSave && hadSave && previousSave != null)
                restoredOriginalSave = previousSave.SequenceEqual(File.ReadAllBytes(path));
            Check(restoredOriginalSave, "Validator restores the exact pre-test campaign save file bytes.");
            if (fail == 0) Debug.Log($"[ASCENDANCE][0.10.2c TEST] PASS — Master Save/Load + Full Regression completed: {pass} passed, 0 failed.");
            else Debug.LogError($"[ASCENDANCE][0.10.2c TEST] FAIL — Master Save/Load + Full Regression completed: {pass} passed, {fail} failed.");
        }

        private static IEnumerator PrepareNestedBaseline0862a(AscendanceGameManager gm)
        {
            if (gm == null) yield break;

            QuestResultsUI0835.Instance?.Close();
            DowntimeUI0836.Instance?.Close();
            CampaignHubUI0834.Instance?.Close();
            QuestBriefingUI0841.Instance?.CloseForResume0842();
            gm.CancelQuestBriefing0841();

            // DebugResetCampaignProgress0833 routes through StartQuest0829("Q001").
            // That authoritative reset clears pending reward/deck-control/content state
            // and rebuilds the known-good Q001 opening enemy setup.
            gm.DebugResetCampaignProgress0833();
            gm.ClearPendingContentInstruction();
            yield return null;
        }

        private static IEnumerator RunNestedValidator(Action start, string completionMarker, Action<bool> completed)
        {
            bool finished = false;
            bool passed = false;
            void OnLog(string condition, string stackTrace, LogType type)
            {
                if (string.IsNullOrEmpty(condition) || !condition.Contains(completionMarker)) return;
                finished = true;
                passed = condition.Contains(" PASS — ") && condition.Contains("0 failed");
            }

            Application.logMessageReceived += OnLog;
            try
            {
                start?.Invoke();
                int frames = 0;
                while (!finished && frames++ < 12000) yield return null;
                completed?.Invoke(finished && passed);
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
            }
        }
    }
}
#endif
