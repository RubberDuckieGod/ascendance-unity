#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ascendance.Online;
using UnityEditor;
using UnityEngine;

namespace Ascendance.EditorTools
{
    // Presentation-only fixtures inspect the production HUD. Real transport ownership,
    // commands and round progression are exercised by the reaction authority gate.
    public static class CombatHudRegressionTestTools0102
    {
        private const string Prefix="[ASCENDANCE][COMBAT HUD]";
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;

        [MenuItem("Ascendance/Validation/Test Multiplayer Combat HUD (Play Mode)")]
        public static void TestHud()
        {
            AscendanceStoragePaths0102.RequireIsolatedValidationDirectory();
            var gm=AscendanceGameManager.Instance;
            var router=AscendanceClientCommandRouter0101.Instance;
            var hud=gm==null?null:gm.GetComponent<AscendanceHUD>();
            if(!Application.isPlaying||gm==null||router==null||hud==null||!router.IsLoopback||gm.IsResolving||gm.HasPendingReactionWindow)
                throw new InvalidOperationException("Use the idle isolated Stonebrook validation fixture.");
            var restore=AscendanceSaveSystem.CaptureForAuthority0100(gm);
            object transport=Field(router,"transport");
            object snapshot=Field(router,"lastSnapshot");
            object kind=Field(gm,"pendingReactionKind");
            object queued=Field(gm,"pendingReactionResolved");
            var priorOptions=gm.PendingReactionOptions.ToList();
            var tcp=new AscendanceTcpCommandTransport0102("127.0.0.1",1,"COMBAT-HUD-ONLY","UI fixture");
            int passed=0;
            void Check(bool ok,string label)
            {
                if(!ok)throw new InvalidOperationException(Prefix+" FAIL — "+label);
                passed++;Debug.Log(Prefix+" PASS — "+label);
            }
            void Refresh()=>typeof(AscendanceHUD).GetMethod("Refresh",Private).Invoke(hud,null);
            IEnumerable<UnityEngine.UI.Button> Reactions()=>((Transform)Field(hud,"reactionRoot")).GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.gameObject.activeSelf);
            UnityEngine.UI.Button Resolve()=>(UnityEngine.UI.Button)Field(hud,"resolveReactionButton");
            try
            {
                gm.SetParty(new[]{"Aldric","Rowan","Garruk","Elara"},"Aldric");
                foreach(var hero in gm.ParticipatingHeroes){hero.Health=hero.MaxHealth;hero.Dead=false;hero.Downed=false;hero.TurnCompleted=false;}
                Set(gm,"<TurnHeroId>k__BackingField",null);
                gm.PendingReactionOptions.Clear();
                gm.PendingReactionOptions.Add(new ReactionOption{HeroId="Aldric",CardId="UI_A",Label="Aldric shield"});
                gm.PendingReactionOptions.Add(new ReactionOption{HeroId="Rowan",CardId="UI_B",Label="Rowan evade"});
                Set(gm,"pendingReactionKind",ReactionWindowKind.BeforeEnemyAttack);
                Set(gm,"pendingReactionResolved",false);
                Refresh();
                Check(Reactions().Count()==2,"Solo HUD retains reactions for both locally controlled Heroes.");
                Check(Resolve().interactable,"An unresolved solo prompt offers Resolve / Decline.");

                // Populate only presentation metadata; no sockets, claims or commands are sent.
                Set(tcp,"heroId","Aldric");Set(router,"transport",tcp);
                var view=new ServerStateSnapshot0100{Players=new List<ServerPlayerView0100>{new(){HeroId="Aldric",Connected=true},new(){HeroId="Rowan",Connected=true}}};
                Set(router,"lastSnapshot",view);
                Refresh();
                Check(Reactions().Count()==1&&Reactions().Single().GetComponentInChildren<UnityEngine.UI.Text>().text=="Aldric shield",
                    "Online HUD renders only the connection owner's reaction cards.");
                gm.SelectHero("Rowan");Refresh();
                Check(Reactions().Count()==1&&Reactions().Single().GetComponentInChildren<UnityEngine.UI.Text>().text=="Aldric shield",
                    "Viewing another Hero does not offer that Hero's reaction cards.");
                gm.PendingReactionOptions.RemoveAll(o=>o.HeroId=="Aldric");Refresh();
                var reactionRoot=(Transform)Field(hud,"reactionRoot");
                Check(!Reactions().Any()&&reactionRoot.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.gameObject.activeSelf&&t.text.Contains("Other players may react")),
                    "A player without a legal reaction sees an explanation of the shared wait.");
                Check(Resolve().interactable,"A claimed player can still decline after consulting the other players.");
                Set(gm,"pendingReactionResolved",true);Refresh();
                Check(!Resolve().interactable&&Resolve().GetComponentInChildren<UnityEngine.UI.Text>().text=="RESOLVING...",
                    "A queued local response disables repeated resolution and displays progress.");
                Set(gm,"pendingReactionResolved",false);Set(tcp,"heroId",null);Refresh();
                Check(!Resolve().interactable&&!Reactions().Any(),"An unclaimed observer cannot press reaction controls.");

                Set(tcp,"heroId","Aldric");Set(gm,"pendingReactionKind",ReactionWindowKind.None);
                gm.Heroes["Aldric"].TurnCompleted=true;gm.Heroes["Rowan"].TurnCompleted=true;
                Refresh();
                string status=((UnityEngine.UI.Text)Field(hud,"actionText")).text;
                Check(status.Contains("Garruk")&&status.Contains("Elara")&&status.Contains("unclaimed"),
                    "A legacy four-Hero party names the unclaimed Heroes blocking Enemy Phase.");
                Check(!gm.Heroes["Garruk"].TurnCompleted&&!gm.Heroes["Elara"].TurnCompleted,
                    "Explaining the wait does not pass or remove the unclaimed Heroes.");
                view.Players.Add(new ServerPlayerView0100{HeroId="Garruk",Connected=false});
                gm.Heroes["Elara"].TurnCompleted=true;Refresh();
                status=((UnityEngine.UI.Text)Field(hud,"actionText")).text;
                Check(status.Contains("Garruk")&&status.Contains("disconnected"),"A claimed disconnected Hero is distinguished from an unclaimed party slot.");
            }
            finally
            {
                Set(router,"transport",transport);Set(router,"lastSnapshot",snapshot);
                Set(gm,"pendingReactionKind",kind);Set(gm,"pendingReactionResolved",queued);
                gm.PendingReactionOptions.Clear();gm.PendingReactionOptions.AddRange(priorOptions);
                gm.ApplySaveData(restore);tcp.Dispose();Refresh();
            }
            Debug.Log($"{Prefix} PASS — Combat HUD completed: {passed} passed, 0 failed.");
        }

        private static object Field(object obj,string name)=>obj.GetType().GetField(name,Private).GetValue(obj);
        private static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,Private).SetValue(obj,value);
    }
}
#endif
