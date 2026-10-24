using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ascendance
{
    // 0.8.57 — Full authored World Event execution for WE001-WE024.
    // Campaign I's existing 0.8.33 rules remain authoritative; this layer completes the
    // authored active modifiers, Campaign II-VI chapter rewards/penalties, campaign meters,
    // route markers, finale state, and save-facing deterministic state.
    public sealed partial class AscendanceGameManager
    {
        private const string WE0857Prefix = "WE0857:";

        public int Suspicion0857 => Mathf.Max(0, CampaignProgress0833?.Suspicion0857 ?? 0);
        public int Instability0857 => Mathf.Clamp(CampaignProgress0833?.Instability0857 ?? 0, 0, 8);
        public int Blight0857 => Mathf.Clamp(CampaignProgress0833?.Blight0857 ?? 0, 0, 8);
        public int Doomtide0857 => Mathf.Clamp(CampaignProgress0833?.Doomtide0857 ?? 0, 0, 8);
        public int Sunfall0857 => Mathf.Clamp(CampaignProgress0833?.Sunfall0857 ?? 0, 0, 8);
        public IReadOnlyList<string> WorldEventFlags0857 => CampaignProgress0833?.WorldEventFlags0857 ?? (IReadOnlyList<string>)Array.Empty<string>();
        public string CurrentWorldEventPressureLabel0857
        {
            get
            {
                EnsureWorldEventCampaignDefaults0857();
                return CurrentCampaignId0845 switch
                {
                    CampaignContent0845.CampaignII when CurrentWorldEventId0833 == "WE006" => $"SUSPICION {Suspicion0857}/3",
                    CampaignContent0845.CampaignIII => $"INSTABILITY {Instability0857}/8",
                    CampaignContent0845.CampaignIV => $"BLIGHT {Blight0857}/8",
                    CampaignContent0845.CampaignV => $"DOOMTIDE {Doomtide0857}/8",
                    CampaignContent0845.CampaignVI => $"SUNFALL {Sunfall0857}/8",
                    _ => string.Empty
                };
            }
        }

        public string CurrentWorldEventAuthoredPreview0857
        {
            get
            {
                var row = FullContentCatalog0850.Root.worldEvents.FirstOrDefault(x => x != null && string.Equals(x.Card_ID, CurrentWorldEventId0833, StringComparison.OrdinalIgnoreCase));
                if (row == null) return string.Empty;
                string pressure = CurrentWorldEventPressureLabel0857;
                string effect = string.IsNullOrWhiteSpace(row.Campaign_Effects) ? row.World_Event_Modifier : row.Campaign_Effects;
                string reward = row.Campaign_Rewards;
                string penalty = row.Campaign_Penalties;
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(pressure)) parts.Add(pressure);
                if (!string.IsNullOrWhiteSpace(effect)) parts.Add("ACTIVE — " + effect);
                if (!string.IsNullOrWhiteSpace(reward)) parts.Add("REWARD — " + reward);
                if (!string.IsNullOrWhiteSpace(penalty)) parts.Add("RISK — " + penalty);
                return string.Join("  •  ", parts);
            }
        }

        private bool HasWEFlag0857(string flag) => CampaignProgress0833?.WorldEventFlags0857?.Contains(flag, StringComparer.OrdinalIgnoreCase) ?? false;
        private void AddWEFlag0857(string flag)
        {
            if (CampaignProgress0833 == null || string.IsNullOrWhiteSpace(flag)) return;
            CampaignProgress0833.WorldEventFlags0857 ??= new List<string>();
            if (CampaignProgress0833.WorldEventFlags0857.Contains(flag, StringComparer.OrdinalIgnoreCase)) return;
            CampaignProgress0833.WorldEventFlags0857.Add(flag);
            AddQuestCampaignFlag0856(flag);
        }
        private bool HasWEKey0857(string key) => CampaignProgress0833?.WorldEventOnceKeys0857?.Contains(key, StringComparer.OrdinalIgnoreCase) ?? false;
        private bool UseWEKey0857(string key)
        {
            if (CampaignProgress0833 == null || string.IsNullOrWhiteSpace(key)) return false;
            CampaignProgress0833.WorldEventOnceKeys0857 ??= new List<string>();
            if (CampaignProgress0833.WorldEventOnceKeys0857.Contains(key, StringComparer.OrdinalIgnoreCase)) return false;
            CampaignProgress0833.WorldEventOnceKeys0857.Add(key);
            return true;
        }

        private void EnsureWorldEventCampaignDefaults0857()
        {
            if (CampaignProgress0833 == null || CampaignProgress0833.WorldEventStateInitialized0857) return;
            CampaignProgress0833.WorldEventStateInitialized0857 = true;
            switch (CurrentCampaignId0845)
            {
                case CampaignContent0845.CampaignIII: CampaignProgress0833.Instability0857 = 2; break;
                case CampaignContent0845.CampaignIV: CampaignProgress0833.Blight0857 = 2; break;
                case CampaignContent0845.CampaignV: CampaignProgress0833.Doomtide0857 = 2; break;
                case CampaignContent0845.CampaignVI: CampaignProgress0833.Sunfall0857 = 2; break;
            }
        }

        public void InitializeWorldEventCampaignState0857(string campaignId)
        {
            if (CampaignProgress0833 == null) return;
            CampaignProgress0833.WorldEventStateInitialized0857 = true;
            CampaignProgress0833.Suspicion0857 = 0;
            CampaignProgress0833.Instability0857 = 0;
            CampaignProgress0833.Blight0857 = 0;
            CampaignProgress0833.Doomtide0857 = 0;
            CampaignProgress0833.Sunfall0857 = 0;
            CampaignProgress0833.WorldEventFlags0857 = new List<string>();
            CampaignProgress0833.WorldEventOnceKeys0857 = new List<string>();
            switch (CampaignContent0845.NormalizeCampaignId0845(campaignId, CampaignProgress0833.CurrentWorldEventId))
            {
                case CampaignContent0845.CampaignIII: CampaignProgress0833.Instability0857 = 2; break;
                case CampaignContent0845.CampaignIV: CampaignProgress0833.Blight0857 = 2; break;
                case CampaignContent0845.CampaignV: CampaignProgress0833.Doomtide0857 = 2; break;
                case CampaignContent0845.CampaignVI: CampaignProgress0833.Sunfall0857 = 2; break;
            }
        }

        private void ChangeMeter0857(string meter, int delta, string source)
        {
            if (CampaignProgress0833 == null || delta == 0) return;
            int before, after;
            switch ((meter ?? string.Empty).ToUpperInvariant())
            {
                case "SUSPICION": before = CampaignProgress0833.Suspicion0857; after = Mathf.Clamp(before + delta, 0, 3); CampaignProgress0833.Suspicion0857 = after; break;
                case "INSTABILITY": before = CampaignProgress0833.Instability0857; after = Mathf.Clamp(before + delta, 0, 8); CampaignProgress0833.Instability0857 = after; break;
                case "BLIGHT": before = CampaignProgress0833.Blight0857; after = Mathf.Clamp(before + delta, 0, 8); CampaignProgress0833.Blight0857 = after; break;
                case "DOOMTIDE": before = CampaignProgress0833.Doomtide0857; after = Mathf.Clamp(before + delta, 0, 8); CampaignProgress0833.Doomtide0857 = after; break;
                case "SUNFALL": before = CampaignProgress0833.Sunfall0857; after = Mathf.Clamp(before + delta, 0, 8); CampaignProgress0833.Sunfall0857 = after; break;
                default: return;
            }
            AddLog($"WORLD EVENT METER — {meter} {before} → {after} ({source}).");
        }

        private static int WEIndex0857(string id)
        {
            var m = Regex.Match(id ?? string.Empty, @"^WE(\d{3})$", RegexOptions.IgnoreCase);
            return m.Success && int.TryParse(m.Groups[1].Value, out int n) ? n : 0;
        }

        public bool IsWorldEventExecutable0857(string id)
        {
            int n = WEIndex0857(id);
            if (n < 1 || n > 24 || !CampaignContent0833.WorldEvents.ContainsKey(id ?? string.Empty)) return false;
            return FullContentCatalog0850.Root.worldEvents.Any(w => w != null && string.Equals(w.Card_ID, id, StringComparison.OrdinalIgnoreCase));
        }

        private int CompletedCount0857(WorldEventDefinition0833 w)
        {
            if (w == null) return 0;
            return new[] { w.Main1, w.Main2, w.Optional }.Count(q => !string.IsNullOrWhiteSpace(q) && Completed0833(q));
        }
        private int RequiredCompletedCount0857(WorldEventDefinition0833 w)
        {
            if (w == null) return 0;
            return new[] { w.Main1, w.Main2 }.Count(q => !string.IsNullOrWhiteSpace(q) && Completed0833(q));
        }
        private void AwardChapterXp0857(int xp, string source)
        {
            if (xp <= 0) return;
            foreach (var h in ParticipatingHeroes) h.XP += xp;
            AddLog($"WORLD EVENT REWARD — {source}: +{xp} XP to each Hero.");
        }

        // Returns true when 0.8.57 owns resolution. WE001-WE003 remain on the proven 0.8.33 resolver.
        private bool ApplyWorldEventResolution0857(WorldEventDefinition0833 w)
        {
            EnsureWorldEventCampaignDefaults0857();
            if (w == null || WEIndex0857(w.Id) <= 3) return false;
            int complete = CompletedCount0857(w), required = RequiredCompletedCount0857(w);
            bool all = complete >= 3;
            bool shortRequired = required < 2;
            var receiver = SelectedHero ?? ParticipatingHeroes.FirstOrDefault();

            switch (w.Id)
            {
                case "WE005":
                    if (all) { AwardChapterXp0857(50, w.Id); AddFactionReputation0856("Crown Watch", 1, "WE005 all-quests reward"); AddWEFlag0857("Witness Testimony"); }
                    else if (shortRequired) { CampaignProgress0833.Suspicion0857 = Mathf.Max(1, CampaignProgress0833.Suspicion0857); AddWEFlag0857("Q016 +1 E035 Masked Acolyte"); AddGeneralReputation(-1, "WE005 incomplete chapter penalty"); }
                    break;
                case "WE006":
                    if (all) { AwardChapterXp0857(75, w.Id); AddGeneralReputation(1, "WE006 all-quests reward"); AddWEFlag0857("Quiet Crown Supply Network Destroyed"); AddWEFlag0857("Q021 cancel first Ritual Focus enemy spawn"); }
                    else if (shortRequired) { AddWEFlag0857("Q019 +1 E040 Gravebound Soldier"); AddWEFlag0857("WE007 first Underground Encounter +1 Severity"); }
                    break;
                case "WE007":
                    if (all) { AwardChapterXp0857(100, w.Id); AddFactionReputation0856("Archive of Seven", 1, "WE007 all-quests reward"); AddWEFlag0857("Royal Seal Recovered"); AddWEFlag0857("Q024 disable one Crown Ward"); }
                    else if (shortRequired) { AddWEFlag0857("Q023 +1 E045 Veilspawn"); AddWEFlag0857("Q024 +1 Crown Ward"); }
                    break;
                case "WE009":
                    if (all) { AwardChapterXp0857(75, w.Id); AddFactionReputation0856("Deepdelver Guild", 1, "WE009 all-quests reward"); ChangeMeter0857("Instability", -1, "WE009 all-quests reward"); AddWEFlag0857("Survey Network Restored"); }
                    else if (shortRequired) { ChangeMeter0857("Instability", 1, "WE009 incomplete chapter penalty"); AddWEFlag0857("Liftworks Damaged"); AddWEFlag0857("Q028 +1 blocked-route token"); }
                    break;
                case "WE010":
                    if (all) { AwardChapterXp0857(100, w.Id); AddFactionReputation0856("Stone-Singer Circle", 1, "WE010 all-quests reward"); ChangeMeter0857("Instability", -1, "WE010 all-quests reward"); AddWEFlag0857("Emberdeep Alliance"); }
                    else if (shortRequired) { ChangeMeter0857("Instability", 1, "WE010 incomplete chapter penalty"); AddWEFlag0857("Q031 +1 E062 Forge Golem"); }
                    break;
                case "WE011":
                    if (all) { AwardChapterXp0857(125, w.Id); AddFactionReputation0856("Ironhaven Forge Council", 1, "WE011 all-quests reward"); ChangeMeter0857("Instability", -1, "WE011 all-quests reward"); AddWEFlag0857("Heartstone Harmonized"); }
                    else if (shortRequired) { ChangeMeter0857("Instability", 1, "WE011 incomplete chapter penalty"); AddWEFlag0857("Q035 +1 E063 Heartstone Adept"); }
                    break;
                case "WE013":
                    if (all) { AwardChapterXp0857(100, w.Id); AddFactionReputation0856("Lantern Apothecary", 1, "WE013 all-quests reward"); ChangeMeter0857("Blight", -1, "WE013 all-quests reward"); AddWEFlag0857("First Cure Distilled"); }
                    else if (shortRequired) { ChangeMeter0857("Blight", 1, "WE013 incomplete chapter penalty"); AddWEFlag0857("Briar Gate Withered"); AddWEFlag0857("Q040 +1 Blight Patch"); }
                    break;
                case "WE014":
                    if (all) { AwardChapterXp0857(125, w.Id); AddFactionReputation0856("Rootwardens", 1, "WE014 all-quests reward"); ChangeMeter0857("Blight", -1, "WE014 all-quests reward"); AddWEFlag0857("Spirit Anchors Restored"); }
                    else if (shortRequired) { ChangeMeter0857("Blight", 1, "WE014 incomplete chapter penalty"); AddWEFlag0857("Q043 +1 E080 Hollow Warden"); }
                    break;
                case "WE015":
                    if (all) { AwardChapterXp0857(150, w.Id); AddFactionReputation0856("Eldergrove Circle", 1, "WE015 all-quests reward"); ChangeMeter0857("Blight", -1, "WE015 all-quests reward"); AddWEFlag0857("Heartseed Covenant Restored"); }
                    else if (shortRequired) { ChangeMeter0857("Blight", 1, "WE015 incomplete chapter penalty"); AddWEFlag0857("Q047 +1 E082 Briar Colossus"); }
                    break;
                case "WE017":
                    if (all) { AwardChapterXp0857(125, w.Id); AddFactionReputation0856("Ashenport Admiralty", 1, "WE017 all-quests reward"); ChangeMeter0857("Doomtide", -1, "WE017 all-quests reward"); AddWEFlag0857("Harbor Channels Secured"); }
                    else if (shortRequired) { ChangeMeter0857("Doomtide", 1, "WE017 incomplete chapter penalty"); AddWEFlag0857("Stormgate Flooded"); AddWEFlag0857("Q052 +1 Flood Marker"); }
                    break;
                case "WE018":
                    if (all) { AwardChapterXp0857(150, w.Id); AddFactionReputation0856("Beacon Order", 1, "WE018 all-quests reward"); ChangeMeter0857("Doomtide", -1, "WE018 all-quests reward"); AddWEFlag0857("Drowned Choir Silenced"); }
                    else if (shortRequired) { ChangeMeter0857("Doomtide", 1, "WE018 incomplete chapter penalty"); AddWEFlag0857("Q055 +1 E104 Tempest Herald"); }
                    break;
                case "WE019":
                    if (all) { AwardChapterXp0857(175, w.Id); AddFactionReputation0856("Tidewarden Order", 1, "WE019 all-quests reward"); ChangeMeter0857("Doomtide", -1, "WE019 all-quests reward"); AddWEFlag0857("Beacon Network Restored"); }
                    else if (shortRequired) { ChangeMeter0857("Doomtide", 1, "WE019 incomplete chapter penalty"); AddWEFlag0857("Q059 +1 E103 Chainwarden Golem"); }
                    break;
                case "WE021":
                    if (all) { AwardChapterXp0857(150, w.Id); AddFactionReputation0856("Dune Wardens", 1, "WE021 all-quests reward"); ChangeMeter0857("Sunfall", -1, "WE021 all-quests reward"); AddWEFlag0857("Eastern Roads Secured"); }
                    else if (shortRequired) { ChangeMeter0857("Sunfall", 1, "WE021 incomplete chapter penalty"); AddWEFlag0857("Caravan Gate Lost"); AddWEFlag0857("Q064 +1 Mirage Marker"); }
                    break;
                case "WE022":
                    if (all) { AwardChapterXp0857(175, w.Id); AddFactionReputation0856("Sunspire Concord", 1, "WE022 all-quests reward"); ChangeMeter0857("Sunfall", -1, "WE022 all-quests reward"); AddWEFlag0857("Oasis Covenant Restored"); }
                    else if (shortRequired) { ChangeMeter0857("Sunfall", 1, "WE022 incomplete chapter penalty"); AddWEFlag0857("Q067 +1 E117 Sunforged Golem"); }
                    break;
                case "WE023":
                    if (all) { AwardChapterXp0857(200, w.Id); AddFactionReputation0856("Order of First Dawn", 1, "WE023 all-quests reward"); ChangeMeter0857("Sunfall", -1, "WE023 all-quests reward"); AddWEFlag0857("Dawn Beacon Restored"); }
                    else if (shortRequired) { ChangeMeter0857("Sunfall", 1, "WE023 incomplete chapter penalty"); AddWEFlag0857("Q071 +1 E122 Black Sun Knight"); }
                    break;
            }
            AddLog($"WORLD EVENT RESOLUTION — {w.Id} {w.Name}: {complete}/3 authored Quests completed; required {required}/2.");
            return true;
        }

        private void ResolveFinalWorldEvent0857(bool victory)
        {
            if (CampaignProgress0833 == null || !CampaignContent0833.WorldEvents.TryGetValue(CurrentWorldEventId0833, out var w) || !string.IsNullOrEmpty(w.NextId)) return;
            if (!CampaignProgress0833.CompletedWorldEventIds.Contains(w.Id, StringComparer.OrdinalIgnoreCase) && victory) CampaignProgress0833.CompletedWorldEventIds.Add(w.Id);
            string finalMeter = CurrentCampaignId0845 switch
            {
                CampaignContent0845.CampaignIII => $"Final Instability {Instability0857}",
                CampaignContent0845.CampaignIV => $"Final Blight {Blight0857}",
                CampaignContent0845.CampaignV => $"Final Doomtide {Doomtide0857}",
                CampaignContent0845.CampaignVI => $"Final Sunfall {Sunfall0857}",
                _ => string.Empty
            };
            if (victory)
            {
                string unlock = w.Id switch { "WE004" => "Defenders of Stonebrook", "WE008" => "Wardens of Valewatch", "WE012" => "Heartwardens of Ironhaven", "WE016" => "Wardens of the Verdant Reach", "WE020" => "Stormwardens of Ashenport", "WE024" => "Dawnbearers of Sunspire", _ => null };
                if (!string.IsNullOrEmpty(unlock)) AddWEFlag0857("UNLOCK — " + unlock);
                if (!string.IsNullOrEmpty(finalMeter)) AddWEFlag0857(finalMeter);
                AddWEFlag0857($"{w.Id} VICTORY — {w.Name}");
            }
            else
            {
                string loss = w.Id switch { "WE004" => "Stonebrook Falls", "WE008" => "Valewatch Fallen", "WE012" => "Ironhaven Broken", "WE016" => "Verdant Reach Withered", "WE020" => "Ashenport Drowned", "WE024" => "Black Sun Ascendant", _ => null };
                if (!string.IsNullOrEmpty(loss)) AddWEFlag0857(loss);
                if (!string.IsNullOrEmpty(finalMeter)) AddWEFlag0857(finalMeter);
            }
        }

        public bool WorldEventRandomTravelSuppressed0857(LocationState location)
        {
            string id = CurrentWorldEventId0833;
            if (id == "WE004" || id == "WE008") return true;
            string authored = location?.AuthoredLocationId ?? location?.Id ?? string.Empty;
            if (id == "WE012") return new[] { "L122", "L123", "L124" }.Contains(authored, StringComparer.OrdinalIgnoreCase);
            if (id == "WE016") return new[] { "L159", "L160", "L161" }.Contains(authored, StringComparer.OrdinalIgnoreCase);
            if (id == "WE020") return new[] { "L196", "L197", "L198" }.Contains(authored, StringComparer.OrdinalIgnoreCase);
            if (id == "WE024") return new[] { "L233", "L234", "L235" }.Contains(authored, StringComparer.OrdinalIgnoreCase);
            return false;
        }
        public bool WorldEventVoluntaryRetreatBlocked0857 => new[] { "WE004", "WE008", "WE012", "WE016", "WE020", "WE024" }.Contains(CurrentWorldEventId0833, StringComparer.OrdinalIgnoreCase);

        private static bool LocationText0857(LocationState l, params string[] needles)
        {
            if (l == null) return false;
            string text = $"{l.Name} {l.Terrain} {l.Traits} {l.Keywords}";
            return needles.Any(n => text.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        private static string EnemyText0857(EnemyState e)
        {
            if (e == null) return string.Empty;
            if (SearchEncounterContent0820.EnemyTemplates.TryGetValue(e.TemplateId ?? string.Empty, out var t)) return $"{t.Name} {t.EnemyType} {t.Role} {t.Difficulty} {t.Traits} {t.Keywords}";
            return e.DisplayName ?? string.Empty;
        }
        private static bool EnemyTextHas0857(EnemyState e, params string[] needles)
        {
            string text = EnemyText0857(e); return needles.Any(n => text.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private bool PartyHasLoot0857(string lootId) => ParticipatingHeroes.Any(h => h.LootInventory.Contains(lootId ?? string.Empty, StringComparer.OrdinalIgnoreCase));

        private void ApplyWorldEventEnemySpawn0857(EnemyState enemy, string source, bool questSpawn)
        {
            if (enemy == null || CampaignProgress0833 == null) return;
            EnsureWorldEventCampaignDefaults0857();
            string we = CurrentWorldEventId0833;
            if (we == "WE002" && EnemyTextHas0857(enemy, "Leader")) { enemy.MaxHealth++; enemy.Health++; AddLog($"WORLD EVENT — WE002 Leader pressure gives {enemy.DisplayName} +1 Health."); }
            if (we == "WE003" && EnemyTextHas0857(enemy, "Archer")) { enemy.Attack++; AddLog($"WORLD EVENT — WE003 Siege gives {enemy.DisplayName} +1 Attack (Archer)."); }
            if (we == "WE008" && SearchEncounterContent0820.IsEliteEnemyTemplate(enemy.TemplateId) && EnemyTextHas0857(enemy, "Cult", "Undead", "Veil")) { enemy.MaxHealth++; enemy.Health++; AddLog($"WORLD EVENT — WE008 Final Crisis gives {enemy.DisplayName} +1 Health."); }
            if (!questSpawn || enemy.IsBoss) return;

            string meter = null; string[] traits = null; int value = 0;
            if (we == "WE009") { meter = "INSTABILITY"; value = Instability0857; traits = new[] { "Construct", "Deep Creature" }; }
            else if (we == "WE013") { meter = "BLIGHT"; value = Blight0857; traits = new[] { "Blighted", "Plant", "Spirit" }; }
            else if (we == "WE017") { meter = "DOOMTIDE"; value = Doomtide0857; traits = new[] { "Drowned", "Sea" }; }
            else if (we == "WE021") { meter = "SUNFALL"; value = Sunfall0857; traits = new[] { "Solar", "Construct", "Undead" }; }
            if (value < 6 || traits == null || !EnemyTextHas0857(enemy, traits)) return;
            string group = Regex.Replace(source ?? "setup", @"\s+", " ").Trim();
            string chosen = $"{WE0857Prefix}{CurrentQuestId0829}:HIGH_GROUP";
            string groupKey = chosen + ":" + group;
            var keys = CampaignProgress0833.WorldEventOnceKeys0857;
            bool any = keys.Any(x => x.StartsWith(chosen + ":", StringComparison.OrdinalIgnoreCase));
            if (!any) keys.Add(groupKey);
            if (!keys.Contains(groupKey, StringComparer.OrdinalIgnoreCase)) return;
            enemy.MaxHealth++; enemy.Health++;
            AddLog($"WORLD EVENT — {meter} {value}: first eligible scripted group gains +1 Health; {enemy.DisplayName} enters at {enemy.Health}/{enemy.MaxHealth}.");
        }

        private int WorldEventEnemyMovementBonus0857(EnemyState enemy)
        {
            if (enemy == null) return 0;
            if (CurrentWorldEventId0833 == "WE001" && IsGoblinEnemy(enemy) && !enemy.ActivatedThisRound) return 1;
            return 0;
        }

        private bool WorldEventEnemyIgnoresDifficultTerrain0857(EnemyState enemy)
        {
            return enemy != null && CurrentWorldEventId0833 == "WE007" && EnemyTextHas0857(enemy, "Undead");
        }

        private int ConsumeWorldEventEncounterSeverity0857(EncounterCardDefinition card, LocationState location, int fixedSeverity)
        {
            if (fixedSeverity > 0 || card == null || location == null || CampaignProgress0833 == null) return 0;
            EnsureWorldEventCampaignDefaults0857();
            int mod = 0; string we = CurrentWorldEventId0833;
            if (we == "WE007" && LocationText0857(location, "Underground", "Crypt", "Catacomb", "Ancient", "Buried City")) mod++;
            if (we == "WE007" && HasWEFlag0857("WE007 first Underground Encounter +1 Severity") && LocationText0857(location, "Underground") && UseWEKey0857($"{WE0857Prefix}WE007:PenaltyEncounter")) mod++;

            string text = $"{card.Name} {card.EncounterType} {card.Terrain} {card.Trigger} {card.Severity12} {card.Severity34} {card.Severity56} {card.Severity7Plus}";
            bool Match(params string[] words) => words.Any(x => text.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);
            string key = null;
            if (we == "WE009" && Instability0857 >= 3 && Match("Cave-In", "structural", "Collapse")) key = $"{WE0857Prefix}{CurrentQuestId0829}:STRUCTURAL";
            else if (we == "WE013" && Blight0857 >= 3 && Match("Blight", "Poison", "Spore")) key = $"{WE0857Prefix}{CurrentQuestId0829}:BLIGHT_HAZARD";
            else if (we == "WE017" && Doomtide0857 >= 3 && Match("Flood", "Current", "Storm")) key = $"{WE0857Prefix}{CurrentQuestId0829}:TIDE_HAZARD";
            else if (we == "WE019" && Doomtide0857 >= 3 && Match("Storm", "Tempest", "Lightning")) key = $"{WE0857Prefix}{CurrentQuestId0829}:R{Round}:STORM_HAZARD";
            else if (we == "WE021" && Sunfall0857 >= 3 && Match("Heat", "Mirage", "Solar")) key = $"{WE0857Prefix}{CurrentQuestId0829}:SOLAR_HAZARD";
            else if (we == "WE023" && Sunfall0857 >= 3 && Match("Heat", "Solar")) key = $"{WE0857Prefix}{CurrentQuestId0829}:R{Round}:ECLIPSE_HAZARD";
            if (!string.IsNullOrEmpty(key) && UseWEKey0857(key)) mod++;
            if (mod > 0) AddLog($"WORLD EVENT — {we} modifies rolled Travel Encounter Severity by +{mod} at {location.Name}.");
            return mod;
        }

        private void ApplyWorldEventEncounterSetup0857(LocationState location, HeroState hero, EncounterRuntimeState encounter)
        {
            if (encounter == null || location == null || CampaignProgress0833 == null) return;
            if (CurrentWorldEventId0833 == "WE005" && encounter.SpawnedEnemyIds.Count > 0 && LocationText0857(location, "Urban", "Settlement", "City") && UseWEKey0857($"{WE0857Prefix}{CurrentQuestId0829}:CURFEW"))
            {
                SpawnEncounterEnemy("E033", location, encounter);
                AddLog("WORLD EVENT — WE005 Curfew adds 1 E033 Street Cutthroat to the first enemy-producing Urban Travel Encounter this Quest.");
            }
            if (CurrentWorldEventId0833 == "WE002" && encounter.SpawnedEnemyIds.Count > 0)
            {
                var normal = encounter.SpawnedEnemyIds.Select(id => Enemies.TryGetValue(id, out var e) ? e : null).FirstOrDefault(e => e != null && IsGoblinEnemy(e) && !SearchEncounterContent0820.IsEliteEnemyTemplate(e.TemplateId));
                if (normal != null && SearchEncounterContent0820.EnemyTemplates.ContainsKey("E006"))
                {
                    string loc = normal.LocationId; string oldId = normal.Id; encounter.SpawnedEnemyIds.Remove(oldId); Enemies.Remove(oldId); if (EnemyAvatars.TryGetValue(oldId, out var av) && av != null) { EnemyAvatars.Remove(oldId); UnityEngine.Object.Destroy(av.gameObject); }
                    var elite = SpawnEncounterEnemy("E006", Locations[loc], encounter);
                    if (elite != null) AddLog("WORLD EVENT — WE002 replaces one normal Goblin with an Elite Goblin (E006) for this Travel Encounter.");
                }
            }
        }

        private void OnWorldEventQuestGoalProgress0857(QuestGoalState0856 goal)
        {
            if (goal == null) return;
            string text = goal.Label ?? string.Empty;

            // WE006 — Suspicion grows from the first Evidence / Ledger / Suspect discovery each Quest.
            if (CurrentWorldEventId0833 == "WE006" &&
                (text.IndexOf("Evidence", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("Ledger", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("Suspect", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                string key = $"{WE0857Prefix}{CurrentQuestId0829}:SUSPICION_OBJECTIVE";
                if (UseWEKey0857(key))
                {
                    if (HasWEFlag0857("Witness Testimony") && UseWEKey0857($"{WE0857Prefix}{CurrentQuestId0829}:WITNESS_MITIGATION")) AddLog("WORLD EVENT — Witness Testimony prevents the first Suspicion marker this Quest.");
                    else
                    {
                        ChangeMeter0857("Suspicion", 1, $"{CurrentQuestId0829} objective discovery");
                        if (Suspicion0857 >= 3) AddWEFlag0857("WE006 Veiled Agent pending");
                    }
                }
            }

            // WE007 / Q021 — destroying each Ritual Focus causes the authored reinforcement sequence.
            // A fully completed WE006 cancels the first of these spawns. Solo uses the authored
            // first-and-third Focus scaling instead of spawning on the second Focus.
            if (CurrentWorldEventId0833 == "WE007" && string.Equals(CurrentQuestId0829, "Q021", StringComparison.OrdinalIgnoreCase) &&
                text.IndexOf("Ritual Focus", StringComparison.OrdinalIgnoreCase) >= 0 && goal.Current >= 1 && goal.Current <= 3)
            {
                int focus = goal.Current;
                if (PartySize == 1 && focus == 2) return;
                string once = $"{WE0857Prefix}Q021:RITUAL_FOCUS_{focus}";
                if (!UseWEKey0857(once)) return;
                if (HasWEFlag0857("Q021 cancel first Ritual Focus enemy spawn") && UseWEKey0857($"{WE0857Prefix}Q021:SUPPLY_NETWORK_CANCEL"))
                {
                    AddLog("WORLD EVENT AID — Quiet Crown Supply Network Destroyed cancels the first Q021 Ritual Focus reinforcement.");
                    return;
                }
                string enemyId = focus == 1 ? "E040" : focus == 2 ? "E041" : "E045";
                string spawnLocation = "L086";
                if (Locations.TryGetValue("L086", out var cathedral))
                    spawnLocation = cathedral.Connections.Where(id => Locations.ContainsKey(id)).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).FirstOrDefault() ?? "L086";
                if (Locations.ContainsKey(spawnLocation))
                {
                    SpawnQuestEnemy0829(enemyId, spawnLocation, $"Q021 Ritual Focus {focus} destroyed");
                    AddLog($"QUEST RULE — Q021 Ritual Focus {focus} spawns {enemyId} in connected Location {spawnLocation}.");
                }
            }
        }

        private void TryInjectWorldEventQuestSpawn0857(string locationId, string source)
        {
            if (CurrentWorldEventId0833 != "WE006" || !HasWEFlag0857("WE006 Veiled Agent pending")) return;
            string key = $"{WE0857Prefix}{CurrentQuestId0829}:VEILED_AGENT";
            if (!UseWEKey0857(key)) return;
            CampaignProgress0833.WorldEventFlags0857.RemoveAll(x => string.Equals(x, "WE006 Veiled Agent pending", StringComparison.OrdinalIgnoreCase));
            SpawnQuestEnemy0829("E036", locationId, "WE006 Suspicion threshold");
            AddLog("WORLD EVENT — Suspicion 3: E036 Veiled Agent joins the next scripted enemy placement.");
        }

        private void OnWorldEventQuestStarted0857(string questId)
        {
            if (CampaignProgress0833 == null || string.IsNullOrWhiteSpace(questId)) return;
            EnsureWorldEventCampaignDefaults0857();

            // Quest-scoped World Event state must survive save/load mid-Quest, but a fresh attempt
            // (including a retry of the same Quest) starts with clean once-per-Quest/Round keys and
            // without route markers left behind by the previous scenario.
            CampaignProgress0833.WorldEventOnceKeys0857?.RemoveAll(k => k != null && k.StartsWith($"{WE0857Prefix}{questId}:", StringComparison.OrdinalIgnoreCase));
            foreach (var location in Locations.Values)
                location.TemporaryTags.RemoveAll(t => t != null && !string.IsNullOrWhiteSpace(t.Tag) && t.Tag.StartsWith(WE0857Prefix + "MARKER:", StringComparison.OrdinalIgnoreCase));

            string SpawnFlag(string q, string flag, string enemy)
            {
                if (!string.Equals(questId, q, StringComparison.OrdinalIgnoreCase) || !HasWEFlag0857(flag)) return null;
                return enemy;
            }
            string enemyId = SpawnFlag("Q016", "Q016 +1 E035 Masked Acolyte", "E035") ?? SpawnFlag("Q019", "Q019 +1 E040 Gravebound Soldier", "E040") ?? SpawnFlag("Q023", "Q023 +1 E045 Veilspawn", "E045") ?? SpawnFlag("Q031", "Q031 +1 E062 Forge Golem", "E062") ?? SpawnFlag("Q035", "Q035 +1 E063 Heartstone Adept", "E063") ?? SpawnFlag("Q043", "Q043 +1 E080 Hollow Warden", "E080") ?? SpawnFlag("Q047", "Q047 +1 E082 Briar Colossus", "E082") ?? SpawnFlag("Q055", "Q055 +1 E104 Tempest Herald", "E104") ?? SpawnFlag("Q059", "Q059 +1 E103 Chainwarden Golem", "E103") ?? SpawnFlag("Q067", "Q067 +1 E117 Sunforged Golem", "E117") ?? SpawnFlag("Q071", "Q071 +1 E122 Black Sun Knight", "E122");
            if (!string.IsNullOrEmpty(enemyId))
            {
                string loc = FullContentCatalog0850.StartingLocationId(questId); if (!Locations.ContainsKey(loc)) loc = ParticipatingHeroes.FirstOrDefault()?.LocationId;
                if (!string.IsNullOrEmpty(loc) && Locations.ContainsKey(loc)) SpawnQuestEnemy0829(enemyId, loc, "prior World Event incomplete-chapter penalty");
            }
            if (CurrentWorldEventId0833 == "WE003" && NextQuestSupply0829 > 0) { NextQuestSupply0829 = Mathf.Max(0, NextQuestSupply0829 - 1); AddLog("WORLD EVENT — WE003 Siege consumes 1 banked Supply at Quest start."); }

            // WE007 prior-chapter outcomes directly alter Q024's authored Crown Ward setup.
            // The Quest parser creates the printed three-Ward objective first; this adjusts only
            // that target and leaves the bespoke boss-phase rules untouched.
            if (string.Equals(questId, "Q024", StringComparison.OrdinalIgnoreCase) && QuestRuntime0856 != null)
            {
                var wards = QuestRuntime0856.Goals.FirstOrDefault(g => g.Kind == "Interact" && (g.Label ?? string.Empty).IndexOf("Crown Ward", StringComparison.OrdinalIgnoreCase) >= 0);
                if (wards != null)
                {
                    int printed = wards.Target;
                    bool seal = HasWEFlag0857("Royal Seal Recovered") || PartyHasLoot0857("LT164");
                    bool extra = HasWEFlag0857("Q024 +1 Crown Ward");
                    wards.Target = Mathf.Max(1, printed - (seal ? 1 : 0) + (extra ? 1 : 0));
                    if (seal) AddLog("WORLD EVENT AID — Royal Seal removes 1 Crown Ward from Q024 setup.");
                    if (extra) AddLog("WORLD EVENT PENALTY — Q024 begins with 1 additional Crown Ward.");
                    if (wards.Current >= wards.Target) wards.Complete = true;
                }
            }

            if ((CurrentWorldEventId0833 == "WE009" && Instability0857 >= 8) || (CurrentWorldEventId0833 == "WE013" && Blight0857 >= 8) || (CurrentWorldEventId0833 == "WE017" && Doomtide0857 >= 8) || (CurrentWorldEventId0833 == "WE021" && Sunfall0857 >= 8))
                AddLog($"WORLD EVENT CRISIS — {CurrentWorldEventId0833} campaign meter is at 8; Quest-specific crisis text remains authoritative.");
        }

        private static string MarkerTag0857(string kind) => WE0857Prefix + "MARKER:" + kind.ToUpperInvariant();
        private bool HasMarker0857(LocationState l, string kind) => l != null && l.TemporaryTags.Any(t => string.Equals(t.Tag, MarkerTag0857(kind), StringComparison.OrdinalIgnoreCase));
        private void PlaceMarker0857(LocationState l, string kind, string source)
        {
            if (l == null || HasMarker0857(l, kind)) return;
            l.TemporaryTags.Add(new LocationTagInstance { Tag = MarkerTag0857(kind), Source = source, ExpiryTiming = StatusExpiryTiming.Persistent });
            AddLog($"WORLD EVENT MARKER — {kind} placed at {l.Name} ({source}).");
        }

        private void OnWorldEventLocationRevealed0857(LocationState location, HeroState hero)
        {
            if (location == null || CampaignProgress0833 == null) return;
            string we = CurrentWorldEventId0833;
            int count = 0; string kind = null; bool eligible = false;
            if (we == "WE010") { kind = "Fracture"; eligible = LocationText0857(location, "Deep Road", "Bridge", "Road"); if (Instability0857 >= 3) count = Instability0857 >= 6 ? 2 : 1; }
            else if (we == "WE014") { kind = "Blight Patch"; eligible = LocationText0857(location, "Forest", "Grove"); if (Blight0857 >= 3) count = Blight0857 >= 6 ? 2 : 1; }
            else if (we == "WE018") { kind = "Flood"; eligible = LocationText0857(location, "Coast", "Dock", "Cavern"); if (Doomtide0857 >= 3) count = Doomtide0857 >= 6 ? 2 : 1; }
            else if (we == "WE022") { kind = "Mirage"; eligible = LocationText0857(location, "Desert", "Ruin"); if (Sunfall0857 >= 3) count = Sunfall0857 >= 6 ? 2 : 1; }

            if (eligible && kind == "Fracture" && count > 0 && HasWEFlag0857("Survey Network Restored") && UseWEKey0857($"{WE0857Prefix}{CurrentQuestId0829}:SURVEY_CANCEL"))
            {
                AddLog("WORLD EVENT — Survey Network Restored cancels the first Fracture Token this Quest.");
                count = 0;
            }

            if (eligible && count > 0 && kind != null)
            {
                int already = Locations.Values.Count(l => HasMarker0857(l, kind) && l.TemporaryTags.Any(t => string.Equals(t.Source, CurrentQuestId0829, StringComparison.OrdinalIgnoreCase)));
                if (already < count) PlaceMarker0857(location, kind, CurrentQuestId0829);
            }

            TryPlacePenaltyMarker0857("Q028", "Q028 +1 blocked-route token", "Fracture", location, LocationText0857(location, "Deep Road", "Bridge", "Road"));
            TryPlacePenaltyMarker0857("Q040", "Q040 +1 Blight Patch", "Blight Patch", location, LocationText0857(location, "Forest", "Grove"));
            TryPlacePenaltyMarker0857("Q052", "Q052 +1 Flood Marker", "Flood", location, LocationText0857(location, "Coast", "Dock", "Cavern"));
            TryPlacePenaltyMarker0857("Q064", "Q064 +1 Mirage Marker", "Mirage", location, LocationText0857(location, "Desert", "Ruin"));
        }

        private void TryPlacePenaltyMarker0857(string quest, string flag, string kind, LocationState location, bool eligible)
        {
            if (!eligible || !string.Equals(CurrentQuestId0829, quest, StringComparison.OrdinalIgnoreCase) || !HasWEFlag0857(flag)) return;
            string key = $"{WE0857Prefix}{quest}:PENALTY_MARKER";
            if (!UseWEKey0857(key)) return;
            PlaceMarker0857(location, kind, flag);
        }

        private int WorldEventMoveSurcharge0857(HeroState hero, string destinationId)
        {
            if (hero == null || !Locations.TryGetValue(hero.LocationId, out var from) || !Locations.TryGetValue(destinationId ?? string.Empty, out var to)) return 0;
            int extra = 0;
            foreach (var kind in new[] { "Fracture", "Flood" }) { if (HasMarker0857(from, kind)) extra++; if (HasMarker0857(to, kind)) extra++; }
            if (HasMarker0857(to, "Mirage") && !hero.UsedRoundAbilities.Contains($"{WE0857Prefix}MIRAGE:{to.Id}")) extra++;
            return extra;
        }
        private void ConsumeWorldEventMoveSurcharge0857(HeroState hero, string destinationId)
        {
            if (hero == null || !Locations.TryGetValue(destinationId ?? string.Empty, out var to)) return;
            if (HasMarker0857(to, "Mirage")) hero.UsedRoundAbilities.Add($"{WE0857Prefix}MIRAGE:{to.Id}");
        }

        private bool CanWorldEventLocationAction0857(HeroState hero)
        {
            if (hero == null || GeneralActions(hero) < 1 || !Locations.TryGetValue(hero.LocationId, out var l)) return false;
            return !Enemies.Values.Any(e => !e.Defeated && e.LocationId == hero.LocationId) && new[] { "Fracture", "Blight Patch", "Flood", "Mirage" }.Any(k => HasMarker0857(l, k));
        }
        private string WorldEventLocationActionLabel0857(HeroState hero)
        {
            if (hero == null || !Locations.TryGetValue(hero.LocationId, out var l)) return "Quest Utility";
            if (HasMarker0857(l, "Fracture")) return "Clear Fracture";
            if (HasMarker0857(l, "Blight Patch")) return "Cleanse Blight";
            if (HasMarker0857(l, "Flood")) return "Clear Flood";
            if (HasMarker0857(l, "Mirage")) return "Dispel Mirage";
            return "Quest Utility";
        }
        private bool ResolveWorldEventLocationAction0857(HeroState hero)
        {
            if (!CanWorldEventLocationAction0857(hero) || !Locations.TryGetValue(hero.LocationId, out var l)) return false;
            string kind = new[] { "Fracture", "Blight Patch", "Flood", "Mirage" }.First(k => HasMarker0857(l, k));
            hero.Actions--; l.TemporaryTags.RemoveAll(t => string.Equals(t.Tag, MarkerTag0857(kind), StringComparison.OrdinalIgnoreCase));
            AddLog($"WORLD EVENT ACTION — {hero.DisplayName} spends 1 Action to clear {kind} at {l.Name}."); StateChanged?.Invoke(); return true;
        }

        private int ModifyWorldEventHealing0857(HeroState target, int amount)
        {
            if (target == null || amount <= 0 || !Locations.TryGetValue(target.LocationId, out var l)) return amount;
            if (HasMarker0857(l, "Blight Patch") && UseWEKey0857($"{WE0857Prefix}{CurrentQuestId0829}:R{Round}:{target.Id}:BLIGHT_HEAL")) { amount = Mathf.Max(0, amount - 1); AddLog("WORLD EVENT — Blight Patch suppresses 1 Health recovery (first recovery here this Round)."); }
            if (CurrentWorldEventId0833 == "WE023" && Sunfall0857 >= 6 && LocationText0857(l, "Desert", "Solar") && UseWEKey0857($"{WE0857Prefix}{CurrentQuestId0829}:R{Round}:{target.Id}:ECLIPSE_HEAL"))
            {
                if (PartyHasLoot0857("LT188") && UseWEKey0857($"{WE0857Prefix}{CurrentQuestId0829}:R{Round}:FIRST_DAWN_PRISM")) AddLog("WORLD EVENT AID — LT188 First Dawn Prism cancels this Round's Eclipse Pressure recovery reduction.");
                else { amount = Mathf.Max(0, amount - 1); AddLog("WORLD EVENT — Eclipse Pressure reduces this Hero's first Desert/Solar recovery this Round by 1."); }
            }
            return amount;
        }

        private void ResolveWorldEventBeneficialStatus0857(HeroState target, StatusInstance status, int forcedRoll = 0)
        {
            if (target == null || status == null || !status.Beneficial || CurrentWorldEventId0833 != "WE015" || Blight0857 < 6 || !Locations.TryGetValue(target.LocationId, out var l) || !LocationText0857(l, "Ancient Forest", "Spirit", "Hollow", "Ancient")) return;
            string key = $"{WE0857Prefix}{CurrentQuestId0829}:R{Round}:{target.Id}:HOLLOW_STATUS"; if (!UseWEKey0857(key)) return;
            int roll = forcedRoll >= 1 && forcedRoll <= 6 ? forcedRoll : UnityEngine.Random.Range(1, 7);
            if (roll <= 2 && PartyHasLoot0857("LT176") && UseWEKey0857($"{WE0857Prefix}{CurrentQuestId0829}:R{Round}:HEARTSEED_WARD")) { AddLog($"WORLD EVENT AID — LT176 Heartseed Ward preserves {target.DisplayName}'s {status.Name} from Hollow Pressure."); return; }
            if (roll <= 2) { target.Statuses.Remove(status); AddLog($"WORLD EVENT — Hollow Pressure rolls {roll}; {target.DisplayName}'s {status.Name} is consumed immediately after resolving."); }
            else AddLog($"WORLD EVENT — Hollow Pressure rolls {roll}; {target.DisplayName}'s {status.Name} remains.");
        }

        private void OnWorldEventHeroEnteredLocation0857(HeroState hero, LocationState location, int forcedRoll = 0)
        {
            if (hero == null || location == null) return;
            if (CurrentWorldEventId0833 == "WE011" && Instability0857 >= 3 && LocationText0857(location, "Forge", "Magma") && UseWEKey0857($"{WE0857Prefix}{CurrentQuestId0829}:R{Round}:{hero.Id}:FORGE_HEAT"))
            {
                bool guarded = hero.HasStatus("Guarded"); if (Instability0857 >= 6 || !guarded) ApplyHeroDamageNow(hero, 1, "WE011 Forge Heat", false, null); else AddLog("WORLD EVENT — Guarded prevents WE011 Forge Heat damage.");
            }
            if (CurrentWorldEventId0833 == "WE019" && Doomtide0857 >= 6 && LocationText0857(location, "Coast", "Sea") && UseWEKey0857($"{WE0857Prefix}{CurrentQuestId0829}:R{Round}:{hero.Id}:TEMPEST_ENTRY"))
            {
                if (PartyHasLoot0857("LT182") && UseWEKey0857($"{WE0857Prefix}{CurrentQuestId0829}:R{Round}:BEACON_LENS")) { AddLog("WORLD EVENT AID — LT182 Beacon Lens suppresses this Round's Tempest Pressure entry roll."); return; }
                int roll = forcedRoll >= 1 && forcedRoll <= 6 ? forcedRoll : UnityEngine.Random.Range(1, 7);
                if (roll <= 2 && !hero.HasStatus("Guarded")) ApplyHeroDamageNow(hero, 1, "WE019 Tempest Pressure", false, null);
                AddLog($"WORLD EVENT — WE019 exposed-route roll {roll}{(hero.HasStatus("Guarded") ? " (Guarded)" : string.Empty)}.");
            }
        }

#if UNITY_EDITOR
        public int DebugWorldEventMeter0857(string name) => (name ?? string.Empty).ToUpperInvariant() switch { "SUSPICION" => Suspicion0857, "INSTABILITY" => Instability0857, "BLIGHT" => Blight0857, "DOOMTIDE" => Doomtide0857, "SUNFALL" => Sunfall0857, _ => -1 };
        public void DebugSetWorldEventMeter0857(string name, int value) { EnsureWorldEventCampaignDefaults0857(); int current = DebugWorldEventMeter0857(name); if (current >= 0) ChangeMeter0857(name, value - current, "0.8.57 validator"); }
        public void DebugSetWorldEvent0857(string id)
        {
            if (CampaignProgress0833 == null) InitializeCampaignProgress0833();
            CampaignProgress0833.CurrentWorldEventId = id; CampaignProgress0833.CampaignId0845 = CampaignContent0845.NormalizeCampaignId0845(null, id); CampaignProgress0833.CampaignCompleted = false; CampaignProgress0833.CampaignDefeated = false; CampaignProgress0833.WorldEventStateInitialized0857 = true;
        }
        public bool DebugHasWorldEventFlag0857(string text) => CampaignProgress0833?.WorldEventFlags0857?.Any(x => x.IndexOf(text ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0) ?? false;
        public void DebugAddWorldEventFlag0857(string flag) => AddWEFlag0857(flag);
        public int DebugWorldEventMoveSurcharge0857(string heroId, string destinationId) => Heroes.TryGetValue(heroId ?? string.Empty, out var h) ? WorldEventMoveSurcharge0857(h, destinationId) : -1;
        public bool DebugWorldEventTravelSuppressed0857(string locationId)
        {
            if (Locations.TryGetValue(locationId ?? string.Empty, out var l)) return WorldEventRandomTravelSuppressed0857(l);
            if (FullContentCatalog0850.TryGetLocation(locationId, out var authored)) return WorldEventRandomTravelSuppressed0857(new LocationState { Id = authored.id, AuthoredLocationId = authored.id, Name = authored.name, Terrain = authored.terrain, Traits = authored.traits, Keywords = authored.keywords });
            return false;
        }
        public bool DebugPlaceWorldEventMarker0857(string locationId, string kind) { if (!Locations.TryGetValue(locationId ?? string.Empty, out var l)) return false; PlaceMarker0857(l, kind, "0.8.57 validator"); return true; }
        public bool DebugHasWorldEventMarker0857(string locationId, string kind) => Locations.TryGetValue(locationId ?? string.Empty, out var l) && HasMarker0857(l, kind);
        public void DebugApplyWorldEventResolution0857(string id, bool main1, bool main2, bool optional)
        {
            DebugSetWorldEvent0857(id); var w = CampaignContent0833.WorldEvents[id];
            foreach (var q in new[] { w.Main1, w.Main2, w.Optional }) CampaignProgress0833.CompletedQuestIds.RemoveAll(x => string.Equals(x, q, StringComparison.OrdinalIgnoreCase));
            if (main1 && !string.IsNullOrEmpty(w.Main1)) CampaignProgress0833.CompletedQuestIds.Add(w.Main1); if (main2 && !string.IsNullOrEmpty(w.Main2)) CampaignProgress0833.CompletedQuestIds.Add(w.Main2); if (optional && !string.IsNullOrEmpty(w.Optional)) CampaignProgress0833.CompletedQuestIds.Add(w.Optional);
            ApplyWorldEventResolution0857(w);
        }
        public void DebugResolveWorldEventStatus0857(string heroId, string status, int forcedRoll)
        {
            if (!Heroes.TryGetValue(heroId ?? string.Empty, out var h)) return; var s = new StatusInstance { Name = status, Beneficial = true, Source = "0.8.57 validator" }; h.Statuses.Add(s); ResolveWorldEventBeneficialStatus0857(h, s, forcedRoll);
        }
        public void DebugWorldEventHeroEnter0857(string heroId, string locationId, int forcedRoll = 1) { if (Heroes.TryGetValue(heroId ?? string.Empty, out var h) && Locations.TryGetValue(locationId ?? string.Empty, out var l)) { h.LocationId = locationId; OnWorldEventHeroEnteredLocation0857(h, l, forcedRoll); } }
        public int DebugModifyWorldEventHealing0857(string heroId, int amount) => Heroes.TryGetValue(heroId ?? string.Empty, out var h) ? ModifyWorldEventHealing0857(h, amount) : amount;
        public int DebugWorldEventEnemyMovementBonus0857(string worldEventId, string templateId, bool activated = false)
        {
            if (!SearchEncounterContent0820.EnemyTemplates.TryGetValue(templateId ?? string.Empty, out var t)) return int.MinValue;
            DebugSetWorldEvent0857(worldEventId);
            return WorldEventEnemyMovementBonus0857(new EnemyState { TemplateId = t.Id, DisplayName = t.Name, ActivatedThisRound = activated });
        }
        public bool DebugWorldEventEnemyIgnoresDifficultTerrain0857(string templateId)
        {
            if (!SearchEncounterContent0820.EnemyTemplates.TryGetValue(templateId ?? string.Empty, out var t)) return false;
            return WorldEventEnemyIgnoresDifficultTerrain0857(new EnemyState { TemplateId = t.Id, DisplayName = t.Name });
        }
        public int DebugWorldEventEnemyHealthDelta0857(string worldEventId, string templateId, bool questSpawn = true, string source = "validator group")
        {
            if (!SearchEncounterContent0820.EnemyTemplates.TryGetValue(templateId ?? string.Empty, out var t)) return int.MinValue;
            DebugSetWorldEvent0857(worldEventId);
            var e = new EnemyState { Id = "DBG_WE0857", TemplateId = t.Id, DisplayName = t.Name, MaxHealth = t.Health, Health = t.Health, Armor = t.Armor, Attack = t.Attack, IsBoss = t.Boss };
            ApplyWorldEventEnemySpawn0857(e, source, questSpawn);
            return e.MaxHealth - t.Health;
        }
        public int DebugWorldEventEnemyAttackDelta0857(string worldEventId, string templateId)
        {
            if (!SearchEncounterContent0820.EnemyTemplates.TryGetValue(templateId ?? string.Empty, out var t)) return int.MinValue;
            DebugSetWorldEvent0857(worldEventId);
            var e = new EnemyState { Id = "DBG_WE0857", TemplateId = t.Id, DisplayName = t.Name, MaxHealth = t.Health, Health = t.Health, Armor = t.Armor, Attack = t.Attack, IsBoss = t.Boss };
            ApplyWorldEventEnemySpawn0857(e, "validator group", true);
            return e.Attack - t.Attack;
        }
        public int DebugWorldEventEncounterSeverityModifier0857(string worldEventId, string cardId, string locationId, int fixedSeverity = 0)
        {
            if (!SearchEncounterContent0820.EncounterCards.TryGetValue(cardId ?? string.Empty, out var card)) return int.MinValue;
            DebugSetWorldEvent0857(worldEventId);
            LocationState location = null;
            if (!string.IsNullOrWhiteSpace(locationId)) Locations.TryGetValue(locationId, out location);
            if (location == null && FullContentCatalog0850.TryGetLocation(locationId, out var authored)) location = new LocationState { Id = authored.id, AuthoredLocationId = authored.id, Name = authored.name, Terrain = authored.terrain, Traits = authored.traits, Keywords = authored.keywords };
            return location == null ? int.MinValue : ConsumeWorldEventEncounterSeverity0857(card, location, fixedSeverity);
        }
        public bool DebugRevealWorldEventLocation0857(string locationId, string heroId = null)
        {
            if (!Locations.TryGetValue(locationId ?? string.Empty, out var location)) return false;
            Heroes.TryGetValue(heroId ?? string.Empty, out var hero);
            OnWorldEventLocationRevealed0857(location, hero);
            return true;
        }
        public void DebugResolveFinalWorldEvent0857(bool victory) => ResolveFinalWorldEvent0857(victory);
        public void DebugWorldEventGoalProgress0857(string label, int current = 1, int target = 1) => OnWorldEventQuestGoalProgress0857(new QuestGoalState0856 { Key = "DBG", Label = label, Kind = "Interact", Current = current, Target = Mathf.Max(1, target), Complete = current >= target });
#endif
    }
}
