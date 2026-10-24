using System.Linq;
using UnityEngine;

namespace Ascendance
{
    // 0.8.29 lightweight world presentation for persistent Quest-owned pieces (first use: Q002 Wagons).
    public sealed class QuestObjectAvatar0829 : MonoBehaviour
    {
        public string ObjectId;
        private Renderer body;

        public static QuestObjectAvatar0829 Create(QuestObjectState0829 obj)
        {
            var gm=AscendanceGameManager.Instance;if(obj==null||gm==null||obj.Rescued||obj.Destroyed||!gm.Locations.TryGetValue(obj.LocationId,out var loc))return null;
            foreach(var existing in Object.FindObjectsByType<QuestObjectAvatar0829>())if(existing!=null&&existing.ObjectId==obj.ObjectId)return existing;
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=$"QuestObject_{obj.ObjectId}_{obj.Type}";go.transform.position=loc.Position+Offset(obj.ObjectId)+new Vector3(0,0.35f,0);go.transform.localScale=(obj.Type=="Wagon"||obj.Type=="SupplyWagon")?new Vector3(1.15f,0.55f,0.75f):obj.Type=="Beacon"?new Vector3(0.45f,1.5f,0.45f):obj.Type=="SupplyCrate"?new Vector3(0.75f,0.55f,0.75f):obj.Type=="Prisoner"||obj.Type=="CaptainRoland"?new Vector3(0.42f,1.05f,0.42f):obj.Type=="SiegeEngine"?new Vector3(1.45f,0.9f,1.15f):obj.Type=="Ballista"?new Vector3(1.1f,0.7f,1.0f):obj.Type=="FortressGate"?new Vector3(1.8f,1.6f,0.55f):obj.Type=="Banner"?new Vector3(0.25f,1.6f,0.55f):new Vector3(0.6f,0.6f,0.6f);
            var r=go.GetComponent<Renderer>();if(r!=null){r.material=new Material(Shader.Find("Standard"));r.material.color=(obj.Type=="Wagon"||obj.Type=="SupplyWagon")?new Color(0.42f,0.23f,0.08f):obj.Type=="Beacon"?new Color(0.55f,0.42f,0.12f):obj.Type=="SupplyCrate"?new Color(0.36f,0.20f,0.07f):obj.Type=="Prisoner"?new Color(0.74f,0.68f,0.54f):obj.Type=="CaptainRoland"?new Color(0.20f,0.42f,0.72f):obj.Type=="SiegeEngine"?new Color(0.30f,0.18f,0.10f):obj.Type=="Ballista"?new Color(0.30f,0.24f,0.16f):obj.Type=="FortressGate"?new Color(0.22f,0.16f,0.10f):obj.Type=="Banner"?new Color(0.55f,0.08f,0.08f):new Color(0.45f,0.45f,0.45f);}
            var avatar=go.AddComponent<QuestObjectAvatar0829>();avatar.ObjectId=obj.ObjectId;avatar.body=r;WorldBillboardLabel.Create(go.transform,obj.DisplayName,new Vector3(0,1.0f,0),Color.white,15);return avatar;
        }

        private void Update()
        {
            var gm=AscendanceGameManager.Instance;if(gm==null)return;var obj=gm.QuestObjects0829.FirstOrDefault(o=>o.ObjectId==ObjectId);
            if(obj==null||obj.Rescued||obj.Destroyed){Destroy(gameObject);return;}if(!gm.Locations.TryGetValue(obj.LocationId,out var loc))return;
            transform.position=loc.Position+Offset(obj.ObjectId)+new Vector3(0,0.35f,0);if(body!=null){if(obj.Type=="Wagon")body.material.color=new Color(0.42f+0.05f*Mathf.Clamp(obj.Supply,0,4),0.23f,0.08f);else if(obj.Type=="SupplyWagon")body.material.color=obj.Supply>=2?new Color(0.28f,0.20f,0.16f):new Color(0.46f,0.28f,0.10f);else if(obj.Type=="Beacon")body.material.color=obj.Resolved?new Color(0.95f,0.72f,0.18f):new Color(0.35f,0.30f,0.22f);else if(obj.Type=="Prisoner")body.material.color=obj.Resolved?new Color(0.40f,0.72f,0.45f):new Color(0.74f,0.68f,0.54f);else if(obj.Type=="SiegeEngine")body.material.color=obj.Health<=Mathf.Max(1,obj.MaxHealth/2)?new Color(0.48f,0.16f,0.08f):new Color(0.30f,0.18f,0.10f);else if(obj.Type=="Ballista")body.material.color=obj.Health<=Mathf.Max(1,obj.MaxHealth/2)?new Color(0.55f,0.18f,0.08f):new Color(0.30f,0.24f,0.16f);else if(obj.Type=="FortressGate")body.material.color=obj.Health<=Mathf.Max(1,obj.MaxHealth/2)?new Color(0.48f,0.13f,0.08f):new Color(0.22f,0.16f,0.10f);}
        }
        private static Vector3 Offset(string id){int hash=(id??"").GetHashCode();return new Vector3(((hash&3)-1.5f)*0.65f,0,(((hash>>2)&3)-1.5f)*0.45f);}
    }
}
