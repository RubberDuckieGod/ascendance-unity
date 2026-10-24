using System;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    /// <summary>
    /// Shared level-3 Hero ability consumers. Choice-heavy abilities use deterministic safe targets
    /// in the current vertical slice and log that choice rather than inventing new rules text.
    /// </summary>
    public static class HeroRulesRuntime
    {
        private static bool OncePerRound(HeroState hero, string key)
        {
            if (hero.UsedRoundAbilities.Contains(key)) return false;
            hero.UsedRoundAbilities.Add(key);
            return true;
        }

        private static bool OncePerTurn(HeroState hero, string key)
        {
            if (hero.UsedTurnAbilities.Contains(key)) return false;
            hero.UsedTurnAbilities.Add(key);
            return true;
        }

        public static void OnQuestStart(AscendanceGameManager gm, HeroState hero)
        {
            if (hero.Id == "Aldric" && !hero.HasStatus("Guarded")) gm.AddStatus(hero, "Guarded", 0, 1, "Battle Ready", true, hero);
        }

        public static int ModifyCardCost(AscendanceGameManager gm, HeroState hero, CardData card, int printedCost)
        {
            int cost = printedCost;
            if (hero.Id == "Vaelis" && Contains(card.Traits, "Spell") && !hero.UsedRoundAbilities.Contains("ArcaneMasteryCost"))
                cost = Mathf.Max(1, cost - 1);
            if (card.Kind == CardKind.Attack && hero.AttackCardCostReduction > 0)
                cost = Mathf.Max(1, cost - hero.AttackCardCostReduction);
            if (card.Kind == CardKind.Attack && hero.FreeAttackCardReady)
                cost = 0;
            return cost;
        }

        public static void OnCardPlayed(AscendanceGameManager gm, HeroState hero, CardData card)
        {
            if (hero.Id == "Vaelis" && Contains(card.Traits, "Spell") && OncePerRound(hero, "ArcaneMasteryCost"))
            {
                gm.DrawCards(hero, 1);
                hero.PendingDiscardCount += 1;
                gm.AddLog("HERO ABILITY — Improved Arcane Mastery: draw 1, then discard 1.");
            }

            if (hero.Id == "Elara" && EqualsText(card.Category, "Support") && OncePerRound(hero, "DivineGrace"))
            {
                var target = gm.ParticipatingHeroes.Where(h => !h.Dead && h.LocationId == hero.LocationId).OrderBy(h => (float)h.Health / Mathf.Max(1, h.MaxHealth)).FirstOrDefault();
                if (target != null)
                {
                    gm.HealHero(target, 2, "Improved Divine Grace", hero);
                    if (target.Id != hero.Id && OncePerRound(hero, "BlessedPresence")) gm.AddStatus(target, "Inspired", 1, 0, "Blessed Presence", true, hero);
                }
            }

            if (hero.Id == "Cassian" && EqualsText(card.Category, "Support") && OncePerRound(hero, "RousingVerse"))
            {
                var target = gm.ParticipatingHeroes.FirstOrDefault(h => h.Id != hero.Id && !h.Dead && h.LocationId == hero.LocationId);
                if (target != null)
                {
                    gm.AddStatus(target, "Inspired", 1, 0, "Improved Rousing Verse", true, hero, "gains Inspired until the end of their next turn");
                    gm.DrawCards(target, 2);
                    target.PendingDiscardCount += 1;
                }
            }
        }

        public static int BeforeAttack(AscendanceGameManager gm, HeroState hero, EnemyState enemy, CardData card)
        {
            int bonus = 0;
            if (hero.Id == "Lyra" && enemy != null && !enemy.ActivatedThisRound && OncePerTurn(hero, "Opportunist"))
            {
                gm.DrawCards(hero, 2); hero.PendingDiscardCount += 1; gm.AddLog("HERO ABILITY — Improved Opportunist: draw 2, then discard 1.");
            }
            if (hero.Id == "Garruk")
            {
                if (hero.BloodrageReady) { bonus += 2; hero.BloodrageReady = false; gm.AddLog("HERO ABILITY — Bloodrage consumed: +2 Attack."); }
                if (hero.Health * 2 <= hero.MaxHealth && hero.FirstAttackThisTurn) { bonus += 1; gm.AddLog("HERO ABILITY — Pain into Power: +1 Attack."); }
            }
            if (hero.Id == "Lucien")
            {
                if (hero.NextAttackBonus > 0) { bonus += hero.NextAttackBonus; hero.NextAttackBonus = 0; }
                if (hero.FirstAttackThisTurn && gm.Enemies.Values.Count(e => !e.Defeated && e.LocationId == hero.LocationId) == 1)
                { bonus += 2; gm.AddLog("HERO ABILITY — Single Combat: +2 Attack."); }
            }
            if (gm.HasPiece("Spirit Totem", null, hero.LocationId) && hero.FirstAttackThisTurn)
            { bonus += 1; gm.AddLog("HERO ABILITY — Totemic Presence: +1 Attack to the first Attack this Round."); }
            if (enemy != null && !string.IsNullOrEmpty(hero.TargetLockedEnemyId) && string.Equals(hero.TargetLockedEnemyId, enemy.Id, StringComparison.OrdinalIgnoreCase) && hero.TargetLockedAttackBonus > 0)
            { bonus += hero.TargetLockedAttackBonus; gm.AddLog($"TARGETED BONUS — +{hero.TargetLockedAttackBonus} Attack against {enemy.DisplayName}."); hero.TargetLockedEnemyId = null; hero.TargetLockedAttackBonus = 0; }
            if (enemy != null && !string.IsNullOrEmpty(hero.FocusEnemyId) && string.Equals(hero.FocusEnemyId, enemy.Id, StringComparison.OrdinalIgnoreCase) && hero.FocusAttackBonus > 0)
            { bonus += hero.FocusAttackBonus; }
            if (hero.NextAttackBonus > 0) { bonus += hero.NextAttackBonus; hero.NextAttackBonus = 0; }
            return bonus;
        }

        public static void AfterAttack(AscendanceGameManager gm, HeroState hero, EnemyState enemy, bool damaged)
        {
            hero.FirstAttackThisTurn = false;
            if (hero.Id == "Kael" && OncePerTurn(hero, "FlowingStep"))
            { hero.FreeMoveReady = true; hero.BonusAfterFreeMove = true; gm.AddLog("HERO ABILITY — Improved Flowing Step: free connected Move ready; first Attack after that Move gains +1 Attack."); }
            if (hero.Id == "Lucien" && damaged && OncePerRound(hero, "FancyFootwork"))
            { hero.FreeMoveReady = true; hero.FreeMoveIgnoresEngagement = true; gm.AddLog("HERO ABILITY — Fancy Footwork: free connected Move ready if no other Enemy prevents it."); }
            if (hero.Id == "Mordren" && gm.HasPiece("Bone Servant", hero.Id, hero.LocationId) && OncePerRound(hero, "GraveCommand") && enemy != null && !enemy.Defeated)
            {
                gm.DealDirectDamage(enemy, 1, "Grave Command", hero, null);
            }
        }

        public static void OnHeroDamaged(AscendanceGameManager gm, HeroState hero, int damage, string source)
        {
            if (damage <= 0) return;
            if (hero.Id == "Garruk" && OncePerRound(hero, "Bloodrage"))
            { hero.BloodrageReady = true; gm.AddLog("HERO ABILITY — Bloodrage: next Attack this Round gains +2 Attack."); }
        }

        public static void OnEnemyDefeated(AscendanceGameManager gm, HeroState hero, EnemyState enemy, CardData card)
        {
            hero.KillCount++;
            gm.RecordEnemyDefeat0855(enemy);
            if (hero.Id == "Garruk" && OncePerRound(hero, "RecklessMomentum")) { hero.FreeMoveReady = true; gm.AddLog("HERO ABILITY — Reckless Momentum: free connected Move ready."); }
            if (hero.Id == "Malachar" && card != null && (Contains(card.Traits, "Fire") || Contains(card.Traits, "Lightning")) && OncePerRound(hero, "ElementalMomentum")) gm.DrawCards(hero, 1);
            if (hero.Id == "Veyra" && enemy.Statuses.Count > 0 && OncePerRound(hero, "SoulSiphon")) gm.HealHero(hero, 1, "Soul Siphon", hero);
        }

        public static void OnMoveComplete(AscendanceGameManager gm, HeroState hero)
        {
            if (hero.Id == "Lucien" && OncePerRound(hero, "DuelistsTempo"))
            { hero.NextAttackBonus += 1; gm.AddLog("HERO ABILITY — Duelist's Tempo: next melee Attack this turn gains +1 Attack."); }
        }

        public static void OnStatusApplied(AscendanceGameManager gm, HeroState source, HeroState heroTarget, EnemyState enemyTarget, StatusInstance status)
        {
            if (source == null || status == null) return;
            if (source.Id == "Veyra" && enemyTarget != null && !status.Beneficial && OncePerRound(source, "MaleficHex"))
            { source.TargetLockedEnemyId = enemyTarget.Id; source.TargetLockedAttackBonus += 2; }
            if (source.Id == "Asha" && (heroTarget != null || enemyTarget != null) && OncePerRound(source, "SpiritGuide"))
            {
                var target = gm.ParticipatingHeroes.FirstOrDefault(h => !h.Dead && h.LocationId == source.LocationId);
                if (target != null) { gm.DrawCards(target, 1); target.PendingDiscardCount += 1; }
            }
            if (source.Id == "Asha" && heroTarget != null && status.Beneficial && OncePerRound(source, "AncestralAid")) gm.HealHero(heroTarget, 1, "Ancestral Aid", source);
            if (heroTarget != null && status.Beneficial)
            {
                foreach (var asha in gm.ParticipatingHeroes.Where(h => h.Id == "Asha" && !h.IsDefeated && h.LocationId == heroTarget.LocationId))
                    if (OncePerRound(asha, "AncestralAidGlobal")) gm.HealHero(heroTarget, 1, "Ancestral Aid", asha);
            }
            if (source.Id == "Seraphine" && heroTarget != null && heroTarget.Id != source.Id && status.Beneficial && OncePerRound(source, "Oathbound"))
            {
                if (source.HasStatus("Guarded")) gm.HealHero(source, 1, "Improved Oathbound", source);
                else gm.AddStatus(source, "Guarded", 0, 1, "Improved Oathbound", true, source);
            }
        }

        public static void OnHealOther(AscendanceGameManager gm, HeroState source, HeroState target)
        {
            if (source == null || target == null || source.Id == target.Id) return;
            if (source.Id == "Elara" && OncePerRound(source, "BlessedPresence")) gm.AddStatus(target, "Inspired", 1, 0, "Blessed Presence", true, source);
            if (source.Id == "Seraphine" && OncePerRound(source, "SacredDuty")) gm.AddStatus(target, "Guarded", 0, 1, "Sacred Duty", true, source);
        }

        public static void OnHeroEnteredLocation(AscendanceGameManager gm, HeroState movedHero)
        {
            foreach (var thorne in gm.ParticipatingHeroes.Where(h => h.Id == "Thorne" && !h.IsDefeated && h.LocationId == movedHero.LocationId))
                if (OncePerRound(thorne, "WildGrowth")) gm.AddStatus(movedHero, "Regenerating", 1, 0, "Improved Wild Growth", true, thorne, "gains Regenerating until the end of their next turn");
        }

        public static void OnAnyHeroHealed(AscendanceGameManager gm, HeroState target)
        {
            foreach (var seraphine in gm.ParticipatingHeroes.Where(h => h.Id == "Seraphine" && !h.IsDefeated && h.LocationId == target.LocationId && h.Id != target.Id))
            {
                if (!OncePerRound(seraphine, "Oathbound")) continue;
                if (seraphine.HasStatus("Guarded")) gm.HealHero(seraphine, 1, "Improved Oathbound", seraphine);
                else gm.AddStatus(seraphine, "Guarded", 0, 1, "Improved Oathbound", true, seraphine);
            }
        }

        public static void ResetRound(HeroState hero)
        {
            hero.UsedRoundAbilities.Clear();
            hero.UsedTurnAbilities.Clear();
            hero.FirstAttackThisTurn = true;
            hero.BloodrageReady = false;
        }

        public static void ResetTurn(HeroState hero)
        {
            hero.UsedTurnAbilities.Clear();
            hero.FirstAttackThisTurn = true;
            hero.TurnAttackBonus = 0;
        }

        private static bool Contains(string value, string token) => !string.IsNullOrEmpty(value) && value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        private static bool EqualsText(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
