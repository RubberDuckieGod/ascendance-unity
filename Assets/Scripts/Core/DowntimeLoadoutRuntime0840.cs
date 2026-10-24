using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    public sealed class DowntimeLoadoutEntry0840
    {
        public string Id;
        public string Label;
        public string Type;
        public string Slot;
        public string Hands;
        public string Rarity;
        public int Attack;
        public int Armor;
        public string Ability1;
        public string Ability2;
        public bool Equipped;
    }

    // 0.8.40 — dedicated Stonebrook Downtime loadout preparation.
    // Loadout changes are free Downtime services: they never consume the Hero's one Activity.
    public sealed partial class AscendanceGameManager
    {
        public bool CanManageDowntimeLoadout0840(string heroId)
        {
            return CanUseDowntimeActions0836
                && Heroes.TryGetValue(heroId??string.Empty,out var hero)
                && hero.Participating
                && !ParticipatingHeroes.Any(h=>h.Moving);
        }

        private DowntimeLoadoutEntry0840 LoadoutEntry0840(string id,bool equipped)
        {
            if(!RewardContent0826.Equipment.TryGetValue(id??string.Empty,out var d))return null;
            return new DowntimeLoadoutEntry0840
            {
                Id=d.Id,Label=RewardContent0826.EquipmentLabel(d.Id),Type=d.Type,Slot=d.Slot,Hands=d.Hands,Rarity=d.Rarity,
                Attack=d.Attack,Armor=d.Armor,Ability1=d.Ability1,Ability2=d.Ability2,Equipped=equipped
            };
        }

        public IReadOnlyList<DowntimeLoadoutEntry0840> DowntimeEquippedGear0840(string heroId)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var hero))return Array.Empty<DowntimeLoadoutEntry0840>();
            var result=new List<DowntimeLoadoutEntry0840>();
            foreach(var eq in hero.StartingEquipment)
                for(int i=0;i<Mathf.Max(1,eq.Copies);i++)
                {
                    var entry=LoadoutEntry0840(eq.Id,true);if(entry!=null)result.Add(entry);
                }
            return result;
        }

        public IReadOnlyList<DowntimeLoadoutEntry0840> DowntimeUnequippedGear0840(string heroId)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var hero))return Array.Empty<DowntimeLoadoutEntry0840>();
            return hero.AcquiredEquipment.Where(RewardContent0826.Equipment.ContainsKey).Select(id=>LoadoutEntry0840(id,false)).Where(x=>x!=null).ToList();
        }

        public string DowntimeLoadoutSummary0840(string heroId)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var hero))return "No Hero selected.";
            var equipped=DowntimeEquippedGear0840(heroId);
            int weapons=equipped.Count(x=>string.Equals(x.Slot,"Weapon",StringComparison.OrdinalIgnoreCase));
            int shields=equipped.Count(x=>string.Equals(x.Slot,"Shield",StringComparison.OrdinalIgnoreCase));
            int armor=equipped.Count(x=>string.Equals(x.Slot,"Armor",StringComparison.OrdinalIgnoreCase));
            int accessories=equipped.Count(x=>string.Equals(x.Slot,"Accessory",StringComparison.OrdinalIgnoreCase));
            return $"{hero.DisplayName}  •  ATTACK {hero.Attack}  •  ARMOR {hero.Armor}\nEquipped: Weapon {weapons}  •  Shield {shields}  •  Armor {armor}  •  Accessories {accessories}/{AccessoryCapacity(hero)}  •  Unequipped {hero.AcquiredEquipment.Count}";
        }

        public string DowntimeEquipmentComparison0840(string heroId,string equipmentId)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var hero)||!RewardContent0826.Equipment.TryGetValue(equipmentId??string.Empty,out var next))return string.Empty;
            var sameSlot=hero.StartingEquipment.Where(e=>string.Equals(e.Type,next.Slot,StringComparison.OrdinalIgnoreCase)||string.Equals(e.Type,next.Type,StringComparison.OrdinalIgnoreCase)).ToList();
            if(string.Equals(next.Slot,"Shield",StringComparison.OrdinalIgnoreCase))
            {
                var weapon=hero.StartingEquipment.FirstOrDefault(e=>string.Equals(e.Type,"Weapon",StringComparison.OrdinalIgnoreCase));
                if(weapon!=null&&WeaponOccupiesBothHands(weapon.Id))return $"BLOCKED • unequip {weapon.Name} first";
            }
            if(string.Equals(next.Slot,"Accessory",StringComparison.OrdinalIgnoreCase))
            {
                int count=sameSlot.Count;
                if(count>=AccessoryCapacity(hero))return "ACCESSORY SLOTS FULL • choose which Accessory to replace";
            }
            int projectedAttack=hero.Attack,projectedArmor=hero.Armor;
            foreach(var old in sameSlot){projectedAttack-=old.Attack*Mathf.Max(1,old.Copies);projectedArmor-=old.Armor*Mathf.Max(1,old.Copies);}
            projectedAttack+=next.Attack;projectedArmor+=next.Armor;
            if(string.Equals(next.Slot,"Weapon",StringComparison.OrdinalIgnoreCase)&&WeaponOccupiesBothHands(equipmentId))
            {
                foreach(var shield in hero.StartingEquipment.Where(e=>string.Equals(e.Type,"Shield",StringComparison.OrdinalIgnoreCase)))projectedArmor-=shield.Armor*Mathf.Max(1,shield.Copies);
            }
            string names=sameSlot.Count==0?"open slot":string.Join(" + ",sameSlot.Select(e=>e.Name));
            return $"vs {names} • TOTAL ATK {hero.Attack}→{Mathf.Max(0,projectedAttack)} • ARM {hero.Armor}→{Mathf.Max(0,projectedArmor)}";
        }

        public bool CanEquipDowntimeEquipment0840(string heroId,string equipmentId,int replaceAccessoryIndex=-1)
        {
            if(!CanManageDowntimeLoadout0840(heroId)||!Heroes.TryGetValue(heroId,out var hero)||!RewardContent0826.Equipment.TryGetValue(equipmentId??string.Empty,out var next))return false;
            if(!hero.AcquiredEquipment.Any(x=>string.Equals(x,equipmentId,StringComparison.OrdinalIgnoreCase)))return false;
            if(string.Equals(next.Slot,"Shield",StringComparison.OrdinalIgnoreCase))
            {
                var weapon=hero.StartingEquipment.FirstOrDefault(e=>string.Equals(e.Type,"Weapon",StringComparison.OrdinalIgnoreCase));
                if(weapon!=null&&WeaponOccupiesBothHands(weapon.Id))return false;
            }
            if(string.Equals(next.Slot,"Accessory",StringComparison.OrdinalIgnoreCase))
            {
                int count=hero.StartingEquipment.Count(e=>string.Equals(e.Type,"Accessory",StringComparison.OrdinalIgnoreCase));
                if(count>=AccessoryCapacity(hero)&&(replaceAccessoryIndex<0||replaceAccessoryIndex>=count))return false;
            }
            return true;
        }

        public bool EquipDowntimeEquipment0840(string heroId,string equipmentId,int replaceAccessoryIndex=-1)
        {
            if(!CanEquipDowntimeEquipment0840(heroId,equipmentId,replaceAccessoryIndex)||!Heroes.TryGetValue(heroId,out var hero))return false;
            bool ok=EquipAcquiredEquipment(hero,equipmentId,replaceAccessoryIndex);
            if(ok){AddLog($"DOWNTIME LOADOUT — {hero.DisplayName} prepares {RewardContent0826.EquipmentLabel(equipmentId)} for the next Quest. Downtime Activity remains {(CanTakeDowntimeActivity0839(hero.Id)?"available":"already used")}.");StateChanged?.Invoke();}
            return ok;
        }

        public bool CanUnequipDowntimeEquipment0840(string heroId,string equipmentId)
        {
            return CanManageDowntimeLoadout0840(heroId)
                && Heroes.TryGetValue(heroId??string.Empty,out var hero)
                && hero.StartingEquipment.Any(e=>string.Equals(e.Id,equipmentId,StringComparison.OrdinalIgnoreCase));
        }

        public bool UnequipDowntimeEquipment0840(string heroId,string equipmentId)
        {
            if(!CanUnequipDowntimeEquipment0840(heroId,equipmentId)||!Heroes.TryGetValue(heroId,out var hero))return false;
            var equipped=hero.StartingEquipment.FirstOrDefault(e=>string.Equals(e.Id,equipmentId,StringComparison.OrdinalIgnoreCase));
            if(equipped==null)return false;
            if(equipped.Copies>1)equipped.Copies--;
            else hero.StartingEquipment.Remove(equipped);
            hero.AcquiredEquipment.Add(equipmentId);
            RecalculateHeroEquipmentStats(hero);
            AddLog($"DOWNTIME LOADOUT — {hero.DisplayName} unequips {RewardContent0826.EquipmentLabel(equipmentId)}. It returns to personal inventory. Attack {hero.Attack} • Armor {hero.Armor}.");
            StateChanged?.Invoke();return true;
        }

        public bool DebugIsEquipmentEquipped0840(string heroId,string equipmentId)=>Heroes.TryGetValue(heroId??string.Empty,out var hero)&&hero.StartingEquipment.Any(e=>string.Equals(e.Id,equipmentId,StringComparison.OrdinalIgnoreCase));
    }
}
