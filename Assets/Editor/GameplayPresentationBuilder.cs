using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class GameplayPresentationBuilder
{
    private static void Layout(Transform t,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size)
    {
        var r=(RectTransform)t;r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=pos;r.sizeDelta=size;
    }
    private static RoundedPanel Surface(GameObject go,Color top,Color bottom,float radius=22,float border=2)
    {
        var image=go.GetComponent<Image>();if(image!=null)Object.DestroyImmediate(image);
        var panel=go.GetComponent<RoundedPanel>();if(panel==null)panel=go.AddComponent<RoundedPanel>();
        panel.top=top;panel.bottom=bottom;panel.border=new Color(0.66f,0.90f,1f,0.85f);panel.radius=radius;panel.borderWidth=border;panel.raycastTarget=false;panel.shadowDepth=3;return panel;
    }
    private static GameObject Node(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size)
    {
        var old=parent.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);Layout(go.transform,anchor,anchor,pos,size);return go;
    }
    private static void ButtonStyle(Button button)
    {
        var panel=Surface(button.gameObject,new Color(0.30f,0.78f,1),new Color(0.055f,0.38f,0.77f),24,3);panel.raycastTarget=true;button.targetGraphic=panel;
    }
    private static void TextLayout(Transform root,string name,Vector2 anchor,Vector2 pos,Vector2 size,int font,TextAnchor alignment)
    {
        var text=root.Find(name).GetComponent<Text>();Layout(text.transform,anchor,anchor,pos,size);text.fontSize=font;text.alignment=alignment;text.raycastTarget=false;text.color=Color.white;
    }
    private static void Label(Transform parent,string name,string value,Vector2 anchor,Vector2 pos,Vector2 size,int font)
    {
        var text=Node(name,parent,anchor,pos,size).AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text=value;text.fontSize=font;text.color=new Color(0.76f,0.91f,1);text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;
    }
    public static void Apply(Transform canvas)
    {
        var routes=canvas.Find("LevelSelectPanel");
        var wallpaper=routes.Find("Racing wallpaper");
        if(wallpaper==null)
        {
            var go=new GameObject("Racing wallpaper",typeof(RectTransform),typeof(RacingMenuBackground));go.transform.SetParent(routes,false);go.transform.SetAsFirstSibling();
            var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;go.GetComponent<RacingMenuBackground>().raycastTarget=false;
        }
        routes.Find("LevelSelectTopRail").GetComponent<Image>().color=new Color(0.04f,0.19f,0.36f,0.3f);
        var home=routes.Find("LevelSelectBackButton").GetComponent<Button>();Layout(home.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(42,-24),new Vector2(78,78));ButtonStyle(home);
        foreach(string name in new[]{"PreviousPage","NextPage"})ButtonStyle(routes.Find(name).GetComponent<Button>());
        for(int i=1;i<=10;i++)
        {
            var card=routes.Find($"LevelButton_{i:00}");Layout(card,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(i%2==1?-410:410,-5),new Vector2(780,516));
            foreach(string child in new[]{"Caption","NumberBadge","ActionIcon",$"LevelMeta_{i:00}"})card.Find(child).gameObject.SetActive(false);
            var ribbon=card.Find("NameRibbon");Layout(ribbon,new Vector2(0,0),new Vector2(0,0),new Vector2(6,6),new Vector2(768,72));Surface(ribbon.gameObject,new Color(0.16f,0.60f,0.85f),new Color(0.05f,0.35f,0.64f),0,0).shadowDepth=0;
            TextLayout(card,$"LevelTitle_{i:00}",new Vector2(0.5f,0),new Vector2(0,12),new Vector2(730,62),34,TextAnchor.MiddleCenter);
            card.Find($"LevelTitle_{i:00}").GetComponent<Text>().fontStyle=FontStyle.Bold;
        }
        var hud=canvas.Find("GameplayHUD");
        foreach(string name in new[]{"Controls","GarageText","TotalCoinsText"})hud.Find(name).gameObject.SetActive(false);
        Panel(hud,"RoutePanel",new Vector2(0,1),new Vector2(28,-28),new Vector2(430,76));
        Panel(hud,"RunPanel",new Vector2(0.5f,1),new Vector2(0,-22),new Vector2(530,110));
        Panel(hud,"SpeedPanel",new Vector2(0,0),new Vector2(28,28),new Vector2(260,128));
        Panel(hud,"DriftPanel",new Vector2(1,0),new Vector2(-28,28),new Vector2(340,128));
        TextLayout(hud,"LevelText",new Vector2(0,1),new Vector2(48,-40),new Vector2(390,52),25,TextAnchor.MiddleLeft);
        TextLayout(hud,"GuidanceText",new Vector2(0.5f,1),new Vector2(0,-34),new Vector2(490,86),28,TextAnchor.MiddleCenter);
        ApplyCoinSizing(hud);
        TextLayout(hud,"SpeedText",new Vector2(0,0),new Vector2(50,67),new Vector2(155,78),60,TextAnchor.MiddleLeft);
        Label(hud,"SpeedUnit","km/h",new Vector2(0,0),new Vector2(201,79),new Vector2(80,36),20);
        TextLayout(hud,"DriftStateText",new Vector2(0,0),new Vector2(52,38),new Vector2(210,32),21,TextAnchor.MiddleLeft);
        TextLayout(hud,"DriftScoreText",new Vector2(1,0),new Vector2(-48,48),new Vector2(300,54),34,TextAnchor.MiddleRight);
        Label(hud,"ScoreLabel","Drift",new Vector2(1,0),new Vector2(-46,109),new Vector2(298,30),21);
        TextLayout(hud,"MessageText",new Vector2(0.5f,1),new Vector2(0,-158),new Vector2(900,100),28,TextAnchor.MiddleCenter);
        var pause=hud.Find("GameplayHomeButton").GetComponent<Button>();Layout(pause.transform,new Vector2(1,1),new Vector2(1,1),new Vector2(-28,-28),new Vector2(80,80));ButtonStyle(pause);
        foreach(var text in canvas.GetComponentsInChildren<Text>(true))text.text=UiCopy.Title(text.text);
        foreach(var label in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(label.text=="FINISH / LAP")label.text="Finish";
    }
    public static void ApplyCoinSizing(Transform hud)
    {
        Panel(hud,"PickupPanel",new Vector2(1,1),new Vector2(-142,-28),new Vector2(230,84));
        TextLayout(hud,"CoinText",new Vector2(1,1),new Vector2(-158,-36),new Vector2(120,60),38,TextAnchor.MiddleRight);
        var coin=Node("PickupIcon",hud,new Vector2(1,1),new Vector2(-296,-36),new Vector2(68,68));
        var icon=coin.AddComponent<MenuIcon>();icon.icon=MenuIcon.Kind.Coin;icon.raycastTarget=false;
    }

    private static void Panel(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size)
    {
        var go=Node(name,parent,anchor,pos,size);var panel=Surface(go,new Color(0.04f,0.21f,0.33f,0.78f),new Color(0.025f,0.12f,0.23f,0.70f),20,1);panel.border=new Color(0.42f,0.76f,0.92f,0.35f);panel.shadowDepth=0;go.transform.SetAsFirstSibling();
    }
}
