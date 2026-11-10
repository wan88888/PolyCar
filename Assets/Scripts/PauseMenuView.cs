using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>Runtime pause modal sharing the Settings palette and surfaces.</summary>
public static class PauseMenuView
{
    public static GameObject Create(Transform parent,UnityAction resume,UnityAction retry,UnityAction home)
    {
        var root=Node("Pause",parent,Vector2.zero,Vector2.zero);
        var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
        var shade=root.AddComponent<Image>();shade.color=new Color(0.025f,0.10f,0.20f,0.72f);
        var card=Node("Dialog",root.transform,new Vector2(0,30),new Vector2(820,380));
        Surface(card,new Color(0.96f,0.99f,1),new Color(0.78f,0.91f,0.98f),new Color(1,0.78f,0.22f),44,7);
        var ribbon=Node("TitleRibbon",root.transform,new Vector2(0,190),new Vector2(870,116));
        Surface(ribbon,new Color(0.23f,0.74f,1),new Color(0.05f,0.40f,0.79f),new Color(1,0.82f,0.34f),24,4);
        Label("PauseTitle",root.transform,"Paused",new Vector2(0,190),new Vector2(700,100),52,Color.white);
        AddButton(root.transform,"Resume",MenuIcon.Kind.Play,new Vector2(-230,-20),156,true,resume);
        AddButton(root.transform,"Restart route",MenuIcon.Kind.Replay,new Vector2(0,-20),156,false,retry);
        AddButton(root.transform,"Home",MenuIcon.Kind.Home,new Vector2(230,-20),156,false,home);
        AddButton(root.transform,"CloseButton",MenuIcon.Kind.Close,new Vector2(400,286),82,false,resume);
        root.SetActive(false);return root;
    }
    private static GameObject Node(string name,Transform parent,Vector2 pos,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0.5f,0.5f);r.anchoredPosition=pos;r.sizeDelta=size;return go;
    }
    private static RoundedPanel Surface(GameObject go,Color top,Color bottom,Color border,float radius,float width)
    {
        var graphic=go.AddComponent<RoundedPanel>();graphic.top=top;graphic.bottom=bottom;graphic.border=border;graphic.radius=radius;graphic.borderWidth=width;graphic.raycastTarget=false;return graphic;
    }
    private static void Label(string name,Transform parent,string value,Vector2 pos,Vector2 size,int font,Color color)
    {
        var text=Node(name,parent,pos,size).AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=font;text.text=value;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;if(font>30)text.fontStyle=FontStyle.Bold;
    }
    private static void AddButton(Transform parent,string name,MenuIcon.Kind kind,Vector2 pos,float size,bool primary,UnityAction action)
    {
        var go=Node(name,parent,pos,new Vector2(size,size));
        var panel=Surface(go,primary?new Color(0.39f,0.87f,0.22f):new Color(0.32f,0.85f,1),primary?new Color(0.12f,0.59f,0.13f):new Color(0.04f,0.36f,0.8f),size<100?new Color(1,0.8f,0.25f):Color.white,size/2,5);panel.raycastTarget=true;panel.shadowDepth=8;
        var button=go.AddComponent<Button>();button.targetGraphic=panel;button.onClick.AddListener(action);
        MenuIcon.Attach(go.transform,kind,size*0.48f).color=Color.white;
        go.AddComponent<IconButtonHint>().description=UiCopy.Title(name);
    }
}
