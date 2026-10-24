using UnityEngine;

namespace Ascendance
{
    public sealed class HeroPieceAvatar : MonoBehaviour
    {
        public string PieceId;
        public static HeroPieceAvatar Create(HeroCreatedPiece piece)
        {
            if(piece==null||AscendanceGameManager.Instance==null||!AscendanceGameManager.Instance.Locations.TryGetValue(piece.LocationId,out var loc))return null;
            var go=GameObject.CreatePrimitive(piece.Type=="Spirit Totem"?PrimitiveType.Cylinder:PrimitiveType.Capsule);go.name=piece.Type+"_"+piece.PieceId;go.transform.position=loc.Position+Offset(piece.OwnerHeroId,piece.PieceId)+new Vector3(0,0.45f,0);go.transform.localScale=piece.Type=="Spirit Totem"?new Vector3(0.35f,0.55f,0.35f):new Vector3(0.34f,0.55f,0.34f);
            var renderer=go.GetComponent<Renderer>();if(renderer!=null){renderer.material=new Material(Shader.Find("Standard"));renderer.material.color=piece.Type=="Spirit Totem"?new Color(0.25f,0.75f,0.95f):new Color(0.78f,0.78f,0.68f);}
            var avatar=go.AddComponent<HeroPieceAvatar>();avatar.PieceId=piece.PieceId;WorldBillboardLabel.Create(go.transform,piece.Type,new Vector3(0,1.1f,0),Color.white,18);return avatar;
        }
        private static Vector3 Offset(string owner,string id){int hash=(owner+id).GetHashCode();float x=((hash&7)-3)*0.22f;float z=(((hash>>3)&7)-3)*0.22f;return new Vector3(x,0,z);}
    }
}
