using UnityEngine;

namespace Ascendance
{
    public sealed class EncounterObjectAvatar : MonoBehaviour
    {
        public string ObjectId;

        public static EncounterObjectAvatar Create(EncounterObjectState obj)
        {
            var gm=AscendanceGameManager.Instance;
            if(obj==null||gm==null||!gm.Locations.TryGetValue(obj.LocationId,out var loc))return null;
            var existing=Object.FindObjectsByType<EncounterObjectAvatar>();
            foreach(var x in existing)if(x!=null&&x.ObjectId==obj.ObjectId)return x;
            PrimitiveType primitive=obj.Type=="Fire Token"?PrimitiveType.Sphere:obj.Type=="Web"?PrimitiveType.Cylinder:PrimitiveType.Cylinder;
            var go=GameObject.CreatePrimitive(primitive);go.name=$"EncounterObject_{obj.ObjectId}_{obj.Type}";
            go.transform.position=loc.Position+Offset(obj.ObjectId)+new Vector3(0,0.28f,0);
            if(obj.Type=="Fire Token")go.transform.localScale=new Vector3(0.38f,0.58f,0.38f);
            else if(obj.Type=="Web")go.transform.localScale=new Vector3(0.9f,0.05f,0.9f);
            else if(obj.Type=="Snare")go.transform.localScale=new Vector3(0.55f,0.04f,0.55f);
            else go.transform.localScale=new Vector3(0.4f,0.6f,0.4f);
            var renderer=go.GetComponent<Renderer>();if(renderer!=null){renderer.material=new Material(Shader.Find("Standard"));renderer.material.color=ColorFor(obj.Type);}
            var avatar=go.AddComponent<EncounterObjectAvatar>();avatar.ObjectId=obj.ObjectId;
            WorldBillboardLabel.Create(go.transform,obj.DisplayName,new Vector3(0,0.9f,0),Color.white,15);
            return avatar;
        }

        private static Color ColorFor(string type)
        {
            if(type=="Fire Token")return new Color(0.95f,0.25f,0.05f);
            if(type=="Web")return new Color(0.82f,0.82f,0.88f);
            if(type=="Snare")return new Color(0.42f,0.23f,0.08f);
            return new Color(0.5f,0.5f,0.5f);
        }

        private static Vector3 Offset(string id)
        {
            int hash=id.GetHashCode();float x=((hash&7)-3)*0.25f;float z=(((hash>>3)&7)-3)*0.25f;return new Vector3(x,0,z);
        }
    }
}
