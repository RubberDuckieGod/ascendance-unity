using UnityEngine;
using UnityEngine.UI;

namespace Ascendance
{
    /// <summary>
    /// 0.8.49 code-first Ascendance UI theme. Functional UI is composed entirely from
    /// UGUI primitives at runtime. The only image asset used by the Campaign II hub is
    /// the region background painting; frames, panels, rows, buttons, dividers and
    /// ornaments are code-built objects.
    /// </summary>
    public static class StudioFantasyUI0849
    {
        public const string BackgroundPath = "UI/AscendanceStudio0849/ValewatchBackground0849";

        public static readonly Color Gold = new(0.84f, 0.61f, 0.21f, 1f);
        public static readonly Color GoldBright = new(1.00f, 0.79f, 0.34f, 1f);
        public static readonly Color GoldDim = new(0.33f, 0.23f, 0.09f, 1f);
        public static readonly Color Ivory = new(0.94f, 0.91f, 0.84f, 1f);
        public static readonly Color Muted = new(0.67f, 0.71f, 0.76f, 1f);
        public static readonly Color Night = new(0.018f, 0.025f, 0.038f, 0.965f);
        public static readonly Color Night2 = new(0.031f, 0.041f, 0.060f, 0.965f);
        public static readonly Color Night3 = new(0.055f, 0.067f, 0.090f, 0.98f);
        public static readonly Color Blue = new(0.045f, 0.20f, 0.42f, 1f);
        public static readonly Color BlueHover = new(0.075f, 0.31f, 0.60f, 1f);
        public static readonly Color Brown = new(0.19f, 0.085f, 0.025f, 1f);
        public static readonly Color BrownHover = new(0.31f, 0.14f, 0.035f, 1f);
        public static readonly Color Locked = new(0.48f, 0.51f, 0.57f, 1f);
        public static readonly Color Green = new(0.40f, 0.80f, 0.48f, 1f);
        public static readonly Color Red = new(0.93f, 0.38f, 0.32f, 1f);

        public sealed class Frame
        {
            public GameObject Root;
            public Image Border;
            public Image InnerBorder;
            public Image Fill;
            public RectTransform Content;
        }

        public static GameObject Rect(Transform parent, string name, Vector2 min, Vector2 max, Color color, bool raycast = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>(); image.color = color; image.raycastTarget = raycast;
            return go;
        }

        public static RawImage Background(Transform parent)
        {
            var go = new GameObject("ValewatchBackground0849");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var raw = go.AddComponent<RawImage>(); raw.raycastTarget = false; raw.texture = Resources.Load<Texture2D>(BackgroundPath); raw.color = Color.white;
            return raw;
        }

        public static Frame FramedPanel(Transform parent, string name, Vector2 min, Vector2 max, bool strong = false, Color? fill = null)
        {
            var root = Rect(parent, name, min, max, new Color(0f,0f,0f,0f));
            var shadow = Rect(root.transform, "Shadow", new Vector2(0.008f,-0.010f), new Vector2(1.008f,0.990f), new Color(0f,0f,0f,0.55f));
            shadow.transform.SetAsFirstSibling();
            var borderGo = Rect(root.transform, "OuterGoldFrame", Vector2.zero, Vector2.one, strong ? GoldBright : Gold);
            var border = borderGo.GetComponent<Image>();
            var innerBorderGo = Rect(root.transform, "InnerDarkFrame", new Vector2(0.006f,0.010f), new Vector2(0.994f,0.990f), new Color(0.08f,0.065f,0.045f,1f));
            var innerBorder = innerBorderGo.GetComponent<Image>();
            var fillGo = Rect(root.transform, "PanelFill", new Vector2(0.010f,0.017f), new Vector2(0.990f,0.983f), fill ?? Night);
            var fillImage = fillGo.GetComponent<Image>();
            var topGlow = Rect(root.transform, "TopGoldHairline", new Vector2(0.018f,0.974f), new Vector2(0.982f,0.981f), new Color(GoldBright.r,GoldBright.g,GoldBright.b,strong?0.75f:0.38f));
            topGlow.GetComponent<Image>().raycastTarget=false;
            AddCornerMarks(root.transform, strong ? 0.030f : 0.022f);
            var contentGo = new GameObject("Content"); contentGo.transform.SetParent(root.transform,false);
            var content = contentGo.AddComponent<RectTransform>(); content.anchorMin = new Vector2(0.026f,0.035f); content.anchorMax = new Vector2(0.974f,0.965f); content.offsetMin = content.offsetMax = Vector2.zero;
            return new Frame{Root=root,Border=border,InnerBorder=innerBorder,Fill=fillImage,Content=content};
        }

        private static void AddCornerMarks(Transform parent, float size)
        {
            Corner(parent, "TL", new Vector2(0.008f,0.992f), new Vector2(1f,-1f), size);
            Corner(parent, "TR", new Vector2(0.992f,0.992f), new Vector2(-1f,-1f), size);
            Corner(parent, "BL", new Vector2(0.008f,0.008f), new Vector2(1f,1f), size);
            Corner(parent, "BR", new Vector2(0.992f,0.008f), new Vector2(-1f,1f), size);
        }

        private static void Corner(Transform parent, string name, Vector2 anchor, Vector2 dir, float size)
        {
            var holder = new GameObject("Corner_"+name); holder.transform.SetParent(parent,false);
            var rt = holder.AddComponent<RectTransform>(); rt.anchorMin=rt.anchorMax=anchor; rt.sizeDelta=new Vector2(1f,1f); rt.anchoredPosition=Vector2.zero;
            var h=Rect(holder.transform,"H",Vector2.zero,Vector2.zero,GoldBright); var hrt=h.GetComponent<RectTransform>(); hrt.anchorMin=hrt.anchorMax=Vector2.zero; hrt.sizeDelta=new Vector2(size*900f,2f); hrt.pivot=new Vector2(dir.x>0?0f:1f,0.5f); hrt.anchoredPosition=Vector2.zero;
            var v=Rect(holder.transform,"V",Vector2.zero,Vector2.zero,GoldBright); var vrt=v.GetComponent<RectTransform>(); vrt.anchorMin=vrt.anchorMax=Vector2.zero; vrt.sizeDelta=new Vector2(2f,size*520f); vrt.pivot=new Vector2(0.5f,dir.y>0?0f:1f); vrt.anchoredPosition=Vector2.zero;
            var d=Rect(holder.transform,"Diamond",Vector2.zero,Vector2.zero,GoldBright); var drt=d.GetComponent<RectTransform>(); drt.anchorMin=drt.anchorMax=Vector2.zero; drt.sizeDelta=new Vector2(8f,8f); drt.anchoredPosition=Vector2.zero; drt.localRotation=Quaternion.Euler(0,0,45);
        }

        public static Text Text(Transform parent, string name, string value, int size, TextAnchor anchor, Vector2 min, Vector2 max, Color color, FontStyle style = FontStyle.Normal, bool shadow = true)
        {
            var t = ShellUIFactory.TextRect(parent, value, size, anchor, min, max, color);
            t.gameObject.name = name; t.fontStyle=style; t.raycastTarget=false;
            if(shadow){var s=t.gameObject.AddComponent<Shadow>();s.effectColor=new Color(0f,0f,0f,0.88f);s.effectDistance=new Vector2(1.3f,-1.3f);}
            return t;
        }

        public static void Fit(Text t, int min, int max){if(t==null)return;t.resizeTextForBestFit=true;t.resizeTextMinSize=min;t.resizeTextMaxSize=max;}

        public static void Divider(Transform parent, float y, float x0=0.02f, float x1=0.98f, float alpha=0.62f)
        {
            Rect(parent,"DividerShadow",new Vector2(x0,y-0.002f),new Vector2(x1,y),new Color(0f,0f,0f,0.9f));
            Rect(parent,"DividerGold",new Vector2(x0,y),new Vector2(x1,y+0.003f),new Color(Gold.r,Gold.g,Gold.b,alpha));
            Diamond(parent,new Vector2((x0+x1)*0.5f,y+0.0015f),0.008f,new Color(GoldBright.r,GoldBright.g,GoldBright.b,alpha));
        }

        public static void Diamond(Transform parent, Vector2 center, float size, Color color)
        {
            var go=Rect(parent,"Diamond",center-new Vector2(size,size),center+new Vector2(size,size),color);go.GetComponent<RectTransform>().localRotation=Quaternion.Euler(0,0,45);
        }

        public static Text SectionHeader(Transform parent, string text, int size=22)
        {
            var t=Text(parent,"SectionHeader",text,size,TextAnchor.MiddleLeft,new Vector2(0.035f,0.86f),new Vector2(0.965f,0.98f),GoldBright,FontStyle.Bold);
            Fit(t,14,size); Divider(parent,0.845f,0.035f,0.965f,0.55f); return t;
        }

        public static GameObject Chip(Transform parent,string name,string label,Vector2 min,Vector2 max,Color accent)
        {
            var outer=Rect(parent,name,min,max,accent);
            Rect(outer.transform,"ChipFill",new Vector2(0.012f,0.09f),new Vector2(0.988f,0.91f),new Color(0.02f,0.028f,0.042f,0.98f));
            var txt=Text(outer.transform,"Label",label,14,TextAnchor.MiddleCenter,new Vector2(0.04f,0.08f),new Vector2(0.96f,0.92f),Ivory,FontStyle.Bold);
            Fit(txt,9,14); return outer;
        }

        public static Button Button(Transform parent,string name,string label,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action,bool primary=false)
        {
            var root=Rect(parent,name,min,max,primary?GoldBright:Gold,true);
            var inner=Rect(root.transform,"ButtonFill",new Vector2(0.010f,0.075f),new Vector2(0.990f,0.925f),primary?Blue:Brown,true);
            var top=Rect(root.transform,"TopHighlight",new Vector2(0.025f,0.895f),new Vector2(0.975f,0.925f),new Color(1f,0.86f,0.48f,0.28f)); top.GetComponent<Image>().raycastTarget=false;
            AddCornerMarks(root.transform,0.018f);
            var button=root.AddComponent<Button>(); button.targetGraphic=inner.GetComponent<Image>();
            var c=button.colors;c.normalColor=Color.white;c.highlightedColor=primary?new Color(1.18f,1.18f,1.18f,1f):new Color(1.24f,1.12f,0.92f,1f);c.pressedColor=new Color(0.72f,0.72f,0.72f,1f);c.selectedColor=c.highlightedColor;c.disabledColor=new Color(0.34f,0.34f,0.34f,0.76f);c.colorMultiplier=1f;button.colors=c;
            if(action!=null)button.onClick.AddListener(action);
            var txt=Text(root.transform,"Label",label,25,TextAnchor.MiddleCenter,new Vector2(0.07f,0.10f),new Vector2(0.93f,0.90f),Ivory,FontStyle.Bold);Fit(txt,14,25);
            return button;
        }

        public static Button QuestRow(Transform parent,string name,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action,out Image fill,out Image border)
        {
            var root=Rect(parent,name,min,max,GoldDim,true); border=root.GetComponent<Image>();
            var inner=Rect(root.transform,"RowFill",new Vector2(0.006f,0.075f),new Vector2(0.994f,0.925f),new Color(0.026f,0.034f,0.050f,0.985f),true); fill=inner.GetComponent<Image>();
            var button=root.AddComponent<Button>();button.targetGraphic=fill;
            var c=button.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1.65f,1.65f,1.65f,1f);c.pressedColor=new Color(0.75f,0.75f,0.75f,1f);c.selectedColor=c.highlightedColor;c.disabledColor=new Color(0.35f,0.35f,0.35f,0.8f);button.colors=c;
            if(action!=null)button.onClick.AddListener(action);
            Diamond(root.transform,new Vector2(0.032f,0.50f),0.024f,GoldDim);
            return button;
        }

        public static void SetQuestRowSelected(Image fill, Image border, bool selected)
        {
            if(fill!=null)fill.color=selected?new Color(0.075f,0.095f,0.135f,0.995f):new Color(0.026f,0.034f,0.050f,0.985f);
            if(border!=null)border.color=selected?GoldBright:GoldDim;
        }
    }
}
