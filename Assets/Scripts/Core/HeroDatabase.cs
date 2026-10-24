using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    [Serializable] internal sealed class HeroDatabaseRoot { public List<HeroDefinition> heroes = new(); }
    [Serializable] internal sealed class HeroCardDatabaseRoot { public List<HeroCardDefinition> cards = new(); }
    [Serializable] internal sealed class StatusDatabaseRoot { public List<StatusDefinition> statuses = new(); }

    [Serializable]
    public sealed class HeroDefinition
    {
        public string id;
        public string heroCardId;
        public string displayName;
        public string className;
        public string species;
        public string primaryRole;
        public string secondaryRole;
        public string traits;
        public int level;
        public int maxHealth;
        public int baseAttack;
        public int baseArmor;
        public int attack;
        public int armor;
        public string startingEquipmentText;
        public List<EquipmentDefinition> startingEquipment = new();
        public string innateAbility;
        public string coreMechanic;
        public List<AbilityJsonDefinition> abilities = new();
    }

    [Serializable]
    public sealed class EquipmentDefinition
    {
        public string id;
        public string name;
        public string type;
        public int copies = 1;
        public int attack;
        public int armor;
        public string ability1;
        public string ability2;
    }

    [Serializable]
    public sealed class AbilityJsonDefinition
    {
        public int level;
        public int slot;
        public string name;
        public string text;
    }

    [Serializable]
    public sealed class HeroCardDefinition
    {
        public string id;
        public string heroId;
        public string heroCardId;
        public int level;
        public int copies;
        public string name;
        public string category;
        public int cost;
        public string speed;
        public string targetRange;
        public string target;
        public string traits;
        public string keywords;
        public string rules;
        public string flavor;

        public CardData ToCardData() => new CardData
        {
            Id = id, HeroId = heroId, HeroCardId = heroCardId, Level = level, Copies = copies,
            Name = name, Category = category, Cost = cost, Speed = speed, TargetRange = targetRange,
            Target = target, Traits = traits, Keywords = keywords, Rules = rules, Flavor = flavor
        };
    }

    [Serializable]
    public sealed class StatusDefinition
    {
        public string id;
        public string name;
        public string type;
        public string category;
        public string effect;
        public string duration;
        public string removal;
        public string keywords;
    }

    public static class HeroDatabase
    {
        private static bool loaded;
        private static readonly Dictionary<string, HeroDefinition> heroes = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, HeroCardDefinition> cards = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<HeroCardDefinition>> cardsByHero = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, StatusDefinition> statuses = new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyCollection<HeroDefinition> Heroes { get { EnsureLoaded(); return heroes.Values; } }
        public static int CardDefinitionCount { get { EnsureLoaded(); return cards.Count; } }

        public static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            LoadHeroes();
            LoadCards();
            LoadStatuses();
        }

        private static void LoadHeroes()
        {
            var asset = Resources.Load<TextAsset>("Ascendance/Data/hero_roster");
            if (asset == null) throw new InvalidOperationException("Missing Resources/Ascendance/Data/hero_roster.json");
            var root = JsonUtility.FromJson<HeroDatabaseRoot>(asset.text);
            if (root?.heroes == null) throw new InvalidOperationException("Invalid hero_roster.json");
            foreach (var hero in root.heroes)
                if (hero != null && !string.IsNullOrEmpty(hero.id)) heroes[hero.id] = hero;
        }

        private static void LoadCards()
        {
            var asset = Resources.Load<TextAsset>("Ascendance/Data/hero_cards");
            if (asset == null) throw new InvalidOperationException("Missing Resources/Ascendance/Data/hero_cards.json");
            var root = JsonUtility.FromJson<HeroCardDatabaseRoot>(asset.text);
            if (root?.cards == null) throw new InvalidOperationException("Invalid hero_cards.json");
            foreach (var card in root.cards)
            {
                if (card == null || string.IsNullOrEmpty(card.id)) continue;
                cards[card.id] = card;
                if (!cardsByHero.TryGetValue(card.heroId, out var list)) cardsByHero[card.heroId] = list = new List<HeroCardDefinition>();
                list.Add(card);
            }
        }

        private static void LoadStatuses()
        {
            var asset = Resources.Load<TextAsset>("Ascendance/Data/status_effects");
            if (asset == null) return;
            var root = JsonUtility.FromJson<StatusDatabaseRoot>(asset.text);
            if (root?.statuses == null) return;
            foreach (var status in root.statuses)
                if (status != null && !string.IsNullOrEmpty(status.name)) statuses[status.name] = status;
        }

        public static HeroDefinition GetHero(string id)
        {
            EnsureLoaded();
            heroes.TryGetValue(id ?? string.Empty, out var hero);
            return hero;
        }

        public static CardData GetCard(string id)
        {
            EnsureLoaded();
            return cards.TryGetValue(id ?? string.Empty, out var def) ? def.ToCardData() : null;
        }

        public static List<string> BuildPhysicalDeck(string heroId)
        {
            EnsureLoaded();
            var result = new List<string>();
            if (!cardsByHero.TryGetValue(heroId ?? string.Empty, out var defs)) return result;
            foreach (var def in defs.OrderBy(c => c.id))
                for (int i = 0; i < Mathf.Max(1, def.copies); i++) result.Add(def.id);
            return result;
        }

        public static StatusDefinition GetStatus(string name)
        {
            EnsureLoaded();
            statuses.TryGetValue(name ?? string.Empty, out var status);
            return status;
        }

        public static string[] DefaultParty => new[] { "Garruk", "Rowan", "Aldric", "Elara" };
    }
}
