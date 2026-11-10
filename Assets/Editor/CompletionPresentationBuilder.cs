using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class CompletionPresentationBuilder
{
    private static readonly Color Ink=new Color(0.07f,0.23f,0.40f);
    private static void Layout(Transform t,Vector2 position,Vector2 size)
    {
        var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0.5f,0.5f);r.anchoredPosition=position;r.sizeDelta=size;
    }
    private static GameObject Node(string name,Transform parent,Vector2 position,Vector2 size)
    {
        var old=parent.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);Layout(go.transform,position,size);return go;
    }
    private static RoundedPanel Surface(GameObject go,Color top,Color bottom,Color border,float radius=28,float line=3)
    {
        var old=go.GetComponent<Image>();if(old!=null)Object.DestroyImmediate(old);
        var panel=go.GetComponent<RoundedPanel>();if(panel==null)panel=go.AddComponent<RoundedPanel>();
        panel.top=top;panel.bottom=bottom;panel.border=border;panel.radius=radius;panel.borderWidth=line;panel.raycastTarget=false;return panel;
    }
    private static Text Label(string name,Transform parent,string value,Vector2 position,Vector2 size,int font,Color color,bool bold=false)
    {
        var text=Node(name,parent,position,size).AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text=value;text.fontSize=font;text.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
        text.resizeTextForBestFit=true;text.resizeTextMaxSize=font;text.resizeTextMinSize=Mathf.Min(font,22);return text;
    }
    private static void StyleButton(Button button,Vector2 position,Vector2 size,bool primary)
    {
        Layout(button.transform,position,size);
        var surface=Surface(button.gameObject,primary?new Color(0.39f,0.87f,0.22f):new Color(0.30f,0.78f,1),primary?new Color(0.12f,0.59f,0.13f):new Color(0.055f,0.38f,0.77f),primary?new Color(1,0.91f,0.49f):Color.white,primary?30:42,4);
        surface.raycastTarget=true;button.targetGraphic=surface;
        var icon=button.transform.Find("ActionIcon").GetComponent<MenuIcon>();Layout(icon.transform,primary?new Vector2(-158,0):Vector2.zero,new Vector2(primary?44:40,primary?44:40));icon.color=Color.white;
    }
    public static void Apply(Transform canvas)
    {
        var root=canvas.Find("CompletionPanel");root.GetComponent<Image>().color=new Color(0.025f,0.10f,0.20f,0.72f);
        var card=root.Find("CompletionCard");Layout(card,new Vector2(0,-8),new Vector2(1000,770));
        Surface(card.gameObject,new Color(0.97f,0.99f,1),new Color(0.77f,0.90f,0.98f),new Color(1,0.78f,0.22f),44,6);
        foreach(string name in new[]{"CompletionAccent","CompletionStatsText","CompletionBestText"})card.Find(name).gameObject.SetActive(false);
        var ribbon=Node("TitleRibbon",card,new Vector2(0,330),new Vector2(1050,112));
        Surface(ribbon,new Color(0.23f,0.74f,1),new Color(0.05f,0.40f,0.79f),new Color(1,0.82f,0.34f),24,4);
        var heading=card.Find("CompletionTitleText").GetComponent<Text>();Layout(heading.transform,new Vector2(0,330),new Vector2(920,98));heading.fontSize=48;heading.fontStyle=FontStyle.Bold;heading.color=Color.white;heading.alignment=TextAnchor.MiddleCenter;heading.transform.SetAsLastSibling();
        var route=Label("RouteName",card,"Harbor Loop",new Vector2(0,245),new Vector2(900,45),27,Ink);
        var medal=Node("Medal",card,new Vector2(0,177),new Vector2(300,74));var medalSurface=Surface(medal,new Color(1,0.90f,0.57f),new Color(1,0.75f,0.20f),Color.white,32,3);
        var medalIcon=MenuIcon.Attach(medal.transform,MenuIcon.Kind.Trophy,46);medalIcon.rectTransform.anchoredPosition=new Vector2(-96,0);
        var medalName=Label("MedalName",medal.transform,"Gold",new Vector2(28,0),new Vector2(190,60),32,Ink,true);
        Text time=null,drift=null,best=null;
        for(int i=0;i<2;i++)
        {
            var stat=Node(i==0?"TimeResult":"DriftResult",card,new Vector2(i==0?-218:218,50),new Vector2(408,152));
            Surface(stat,new Color(0.77f,0.92f,0.99f),new Color(0.91f,0.97f,1),Color.white,24,3).shadowDepth=0;
            Label("Caption",stat.transform,i==0?"Time":"Drift Score",new Vector2(0,45),new Vector2(360,32),23,Ink);
            var value=Label("Value",stat.transform,i==0?"00:00.00":"0",new Vector2(0,-2),new Vector2(370,62),50,Ink,true);
            if(i==0)time=value;else{drift=value;best=Label("NewBest",stat.transform,"New Drift Best",new Vector2(0,-51),new Vector2(360,30),19,new Color(0.06f,0.47f,0.39f),true);}
        }
        var reward=Node("Reward",card,new Vector2(0,-130),new Vector2(520,104));Surface(reward,new Color(1,0.96f,0.72f),new Color(1,0.87f,0.40f),new Color(1,0.74f,0.22f),28,3).shadowDepth=0;
        Label("Caption",reward.transform,"Reward",new Vector2(0,31),new Vector2(400,30),22,Ink);
        var coin=MenuIcon.Attach(reward.transform,MenuIcon.Kind.Coin,48);coin.rectTransform.anchoredPosition=new Vector2(-75,-17);
        var rewardValue=Label("Value",reward.transform,"+0",new Vector2(40,-17),new Vector2(210,60),44,Ink,true);
        var next=card.Find("CompletionNextButton").GetComponent<Button>();StyleButton(next,new Vector2(0,-268),new Vector2(460,104),true);
        var primaryLabel=Label("PrimaryLabel",next.transform,"Next Route",new Vector2(35,0),new Vector2(335,76),38,Color.white,true);
        if(next.GetComponent<PlayButtonPulse>()==null)next.gameObject.AddComponent<PlayButtonPulse>();
        var retry=card.Find("CompletionReplayButton").GetComponent<Button>();StyleButton(retry,new Vector2(-350,-268),new Vector2(84,84),false);
        var routes=card.Find("CompletionLevelSelectButton").GetComponent<Button>();StyleButton(routes,new Vector2(350,-268),new Vector2(84,84),false);
        StyleButton(card.Find("CompletionHomeButton").GetComponent<Button>(),new Vector2(470,428),new Vector2(80,80),false);
        var view=root.GetComponent<CompletionView>();if(view==null)view=root.gameObject.AddComponent<CompletionView>();
        view.heading=heading;view.route=route;view.medalName=medalName;view.timeValue=time;view.driftValue=drift;view.rewardValue=rewardValue;view.bestBadge=best;view.medalSurface=medalSurface;view.medalIcon=medalIcon;view.primaryIcon=next.transform.Find("ActionIcon").GetComponent<MenuIcon>();view.primaryLabel=primaryLabel;view.primaryButton=next;view.routesButton=routes;
    }
}
