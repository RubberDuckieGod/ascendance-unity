using System;
using System.Collections.Generic;
using System.Linq;

namespace Ascendance
{
    // 0.8.41 — transient pre-Quest briefing / ready-up gate.
    // This layer never advances Campaign Day or initializes a Quest until ConfirmBriefedQuest0841 succeeds.
    public sealed partial class AscendanceGameManager
    {
        private string pendingQuestBriefingId0841;
        private readonly HashSet<string> questReadyHeroIds0841=new(StringComparer.OrdinalIgnoreCase);

        public string PendingQuestBriefingId0841 => pendingQuestBriefingId0841??string.Empty;
        public bool IsQuestBriefingOpen0841 => !string.IsNullOrEmpty(pendingQuestBriefingId0841);
        public int ReadyHeroCount0841 => ParticipatingHeroes.Count(h=>questReadyHeroIds0841.Contains(h.Id));
        public int RequiredReadyHeroCount0841 => ParticipatingHeroes.Count();
        public bool AllParticipatingHeroesReady0841 => RequiredReadyHeroCount0841>0&&ReadyHeroCount0841==RequiredReadyHeroCount0841;

        public bool OpenQuestBriefing0841(string questId)
        {
            SyncCampaignProgress0833();
            if(!CanBeginCampaignQuest0833(questId)||!QuestContent0829.Definitions.ContainsKey(questId??string.Empty))return false;
            pendingQuestBriefingId0841=questId;
            questReadyHeroIds0841.Clear();
            AddLog($"QUEST BRIEFING — {questId} {QuestContent0829.Definitions[questId].Name} selected. Campaign state is unchanged until the party confirms BEGIN QUEST.");
            StateChanged?.Invoke();
            return true;
        }

        public bool IsHeroReadyForQuest0841(string heroId)
        {
            return !string.IsNullOrEmpty(heroId)&&questReadyHeroIds0841.Contains(heroId);
        }

        public bool SetHeroQuestReady0841(string heroId,bool ready)
        {
            if(!IsQuestBriefingOpen0841||!Heroes.TryGetValue(heroId??string.Empty,out var hero)||!hero.Participating)return false;
            if(ready)questReadyHeroIds0841.Add(hero.Id);else questReadyHeroIds0841.Remove(hero.Id);
            AddLog($"QUEST READY — {hero.DisplayName} is {(ready?"READY":"NOT READY")} for {pendingQuestBriefingId0841}. ({ReadyHeroCount0841}/{RequiredReadyHeroCount0841})");
            StateChanged?.Invoke();
            return true;
        }

        public bool ToggleHeroQuestReady0841(string heroId)=>SetHeroQuestReady0841(heroId,!IsHeroReadyForQuest0841(heroId));

        public bool CanConfirmBriefedQuest0841
        {
            get
            {
                if(!IsQuestBriefingOpen0841||!AllParticipatingHeroesReady0841)return false;
                return CanBeginCampaignQuest0833(pendingQuestBriefingId0841);
            }
        }

        public bool ConfirmBriefedQuest0841()
        {
            if(!CanConfirmBriefedQuest0841)return false;
            string questId=pendingQuestBriefingId0841;
            bool began=BeginCampaignQuest0833(questId);
            if(!began)return false;
            // BeginCampaignQuest0833 clears the transient briefing state after authoritative launch.
            return true;
        }

        public void CancelQuestBriefing0841()
        {
            ClearQuestBriefing0841(true);
        }

        private void ClearQuestBriefing0841(bool notify)
        {
            bool changed=IsQuestBriefingOpen0841||questReadyHeroIds0841.Count>0;
            pendingQuestBriefingId0841=null;
            questReadyHeroIds0841.Clear();
            if(changed&&notify)StateChanged?.Invoke();
        }

        public QuestDefinition0829 PendingQuestDefinition0841
            => QuestContent0829.Definitions.TryGetValue(pendingQuestBriefingId0841??string.Empty,out var d)?d:null;

        public string QuestBriefingWorldEventText0841
        {
            get
            {
                if(!IsQuestBriefingOpen0841)return string.Empty;
                return $"{CurrentWorldEventId0833}  •  {CurrentWorldEventName0833}\n{CurrentWorldEventProgressLabel0834}\n{CurrentWorldEventOutcomePreview0834}";
            }
        }

        public string QuestBriefingPartySummary0841(string heroId)
        {
            if(!Heroes.TryGetValue(heroId??string.Empty,out var hero)||!hero.Participating)return string.Empty;
            string ready=IsHeroReadyForQuest0841(hero.Id)?"READY":"NOT READY";
            return $"{hero.DisplayName}  •  {ready}\nHP {hero.Health}/{hero.MaxHealth}  •  Lv {hero.Level}  •  {DowntimeLoadoutSummary0840(hero.Id).Replace("\n","  •  ")}";
        }

        public bool DebugBriefingReadyContains0841(string heroId)=>questReadyHeroIds0841.Contains(heroId??string.Empty);
    }
}
