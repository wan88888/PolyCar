using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class MenuPolishBuilder
{
    private static readonly Color Ink=new Color(0.07f,0.23f,0.40f);
    private static readonly Color Blue=new Color(0.14f,0.48f,0.86f);
    private static Font Font=>Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    private static RectTransform Rect(Transform item,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size)
    {
        var r=(RectTransform)item;r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=pos;r.sizeDelta=size;return r;
    }
    private static GameObject Node(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);Rect(go.transform,anchor,pivot,pos,size);return go;
    }
    private static void Remove(Transform root,string name){var t=root.Find(name);if(t!=null)Object.DestroyImmediate(t.gameObject);}
    private static RoundedPanel Surface(GameObject go,Color top,Color bottom,Color border,float radius=24,float line=3)
    {
        var image=go.GetComponent<Image>();if(image!=null)Object.DestroyImmediate(image);
        var surface=go.GetComponent<RoundedPanel>()??go.AddComponent<RoundedPanel>();
        surface.top=top;surface.bottom=bottom;surface.border=border;surface.radius=radius;surface.borderWidth=line;surface.raycastTarget=false;return surface;
    }
    private static Text Text(string name,Transform parent,string value,Vector2 anchor,Vector2 pos,Vector2 size,int font,Color color,TextAnchor alignment=TextAnchor.MiddleCenter)
    {
        var go=Node(name,parent,anchor,anchor,pos,size);var text=go.AddComponent<Text>();text.font=Font;text.text=value;text.fontSize=font;text.color=color;text.alignment=alignment;text.raycastTarget=false;return text;
    }
    private static Button Button(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,Color top,Color bottom)
    {
        var go=Node(name,parent,anchor,anchor,pos,size);var surface=Surface(go,top,bottom,new Color(0.78f,0.94f,1));surface.raycastTarget=true;
        var button=go.AddComponent<Button>();button.targetGraphic=surface;
        var colors=button.colors;colors.highlightedColor=new Color(1.08f,1.08f,1.08f);colors.pressedColor=new Color(0.8f,0.88f,0.96f);colors.disabledColor=Color.white;button.colors=colors;
        go.AddComponent<IconButtonHint>();return button;
    }
    private static void Center(RectTransform rect,Vector2 position,Vector2 size)=>Rect(rect,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),position,size);
    public static void Apply(Transform canvas)
    {
        BuildShop(canvas.Find("ShopPanel"));
        BuildRank(canvas.Find("RankPanel"));
        BuildNav(canvas.Find("HomePanel"),MenuPageTab.Page.Home);
        BuildNav(canvas.Find("ShopPanel"),MenuPageTab.Page.Shop);
        BuildNav(canvas.Find("RankPanel"),MenuPageTab.Page.Rank);
        BuildPlay(canvas.Find("HomePanel/LevelButton").GetComponent<Button>());
        BuildHeaderAndSettings(canvas);
        GameplayPresentationBuilder.Apply(canvas);
        CompletionPresentationBuilder.Apply(canvas);
    }
    private static void BuildShop(Transform shop)
    {
        shop.Find("ShopBackButton").gameObject.SetActive(false);
        Rect(shop.Find("ShopStatusText"),new Vector2(0.5f,0),new Vector2(0.5f,0),new Vector2(0,194),new Vector2(1450,36));
        for(int i=0;i<4;i++)
        {
            var card=shop.Find($"ShopCarButton_{i+1:00}");
            Rect(card,new Vector2(0.5f,1),new Vector2(0.5f,1),new Vector2(i%2==0?-325:325,-164-(i/2)*350),new Vector2(600,326));
            card.GetComponent<Button>().enabled=false;
            card.GetComponent<IconButtonHint>().enabled=false;
            var frame=Surface(card.gameObject,new Color(0.96f,0.99f,1),new Color(0.80f,0.90f,0.97f),new Color(0.3f,0.66f,1),30,5);
            foreach(string old in new[]{"ShopAccent_"+(i+1).ToString("00"),"ActionIcon","PriceCoin"})if(card.Find(old)!=null)card.Find(old).gameObject.SetActive(false);
            card.Find($"ShopCarCost_{i+1:00}").gameObject.SetActive(false);
            var title=card.Find($"ShopCarTitle_{i+1:00}").GetComponent<Text>();
            Rect(title.transform,new Vector2(0.5f,1),new Vector2(0.5f,1),new Vector2(0,-15),new Vector2(440,43));title.alignment=TextAnchor.MiddleCenter;title.fontSize=31;title.fontStyle=FontStyle.Bold;title.color=Ink;
            var picture=card.Find($"ShopCarImage_{i+1:00}").GetComponent<Image>();Center(picture.rectTransform,new Vector2(0,12),new Vector2(420,192));
            var oldStats=card.Find("Performance");
            if(oldStats!=null){while(oldStats.childCount>0)oldStats.GetChild(0).SetParent(card,false);Object.DestroyImmediate(oldStats.gameObject);}
            var stats=Node("Performance",card,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(138,0),new Vector2(250,160));
            string[] names={"Speed","Drift","Grip"};
            for(int j=0;j<3;j++)
            {
                var label=card.Find(names[j]+"_Label");var track=card.Find(names[j]+"_Track");var fill=card.Find(names[j]+"_Fill");
                float fraction=((RectTransform)fill).sizeDelta.x/((RectTransform)track).sizeDelta.x;
                foreach(var t in new[]{label,track,fill})t.SetParent(stats.transform,false);
                Rect(label,new Vector2(0,1),new Vector2(0,1),new Vector2(0,-j*49),new Vector2(230,26));label.GetComponent<Text>().fontSize=20;label.GetComponent<Text>().color=Ink;
                Rect(track,new Vector2(0,1),new Vector2(0,1),new Vector2(0,-30-j*49),new Vector2(230,11));track.GetComponent<Image>().color=new Color(0.63f,0.77f,0.86f);
                Rect(fill,new Vector2(0,1),new Vector2(0,1),new Vector2(0,-30-j*49),new Vector2(230*fraction,11));fill.GetComponent<Image>().color=new Color(0.08f,0.56f,0.74f);
            }
            foreach(string child in new[]{"Purchase","Details","EquippedBadge"})Remove(card,child);
            var purchase=Button("Purchase",card,new Vector2(0.5f,0),new Vector2(0,17),new Vector2(220,63),new Color(0.39f,0.82f,0.19f),new Color(0.15f,0.57f,0.12f));
            var action=Text("Action",purchase.transform,"Equipped",new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(180,48),28,Color.white);action.fontStyle=FontStyle.Bold;
            var coin=MenuIcon.Attach(purchase.transform,MenuIcon.Kind.Coin,34);coin.rectTransform.anchoredPosition=new Vector2(-64,0);
            var details=Button("Details",card,new Vector2(1,1),new Vector2(-15,-14),new Vector2(48,48),new Color(0.84f,0.94f,1),new Color(0.65f,0.85f,0.96f));
            var info=MenuIcon.Attach(details.transform,MenuIcon.Kind.Info,28);info.color=Ink;
            var badge=Node("EquippedBadge",card,new Vector2(0,1),new Vector2(0,1),new Vector2(14,-14),new Vector2(44,44));
            Surface(badge,new Color(0.49f,0.91f,0.19f),new Color(0.20f,0.62f,0.07f),new Color(0.85f,1,0.62f),22,2);
            MenuIcon.Attach(badge.transform,MenuIcon.Kind.Check,28);
            var view=card.GetComponent<ShopVehicleCard>()??card.gameObject.AddComponent<ShopVehicleCard>();
            view.purchaseButton=purchase;view.detailsButton=details;view.stats=stats;view.selectedBadge=badge;view.vehicle=picture.rectTransform;view.actionLabel=action;view.coin=coin;view.detailIcon=info;view.actionSurface=purchase.GetComponent<RoundedPanel>();view.frame=frame;view.SetExpanded(false);
        }
    }
    private static void BuildNav(Transform page,MenuPageTab.Page selected)
    {
        var nav=page.Find("BottomNav");
        if(nav==null)nav=Node("BottomNav",page,new Vector2(0.5f,0),new Vector2(0.5f,0),Vector2.zero,new Vector2(1920,144)).transform;
        var r=(RectTransform)nav;r.anchorMin=new Vector2(0,0);r.anchorMax=new Vector2(1,0);r.pivot=new Vector2(0.5f,0);r.sizeDelta=new Vector2(0,144);r.anchoredPosition=Vector2.zero;
        Surface(nav.gameObject,new Color(0.12f,0.41f,0.73f),new Color(0.055f,0.24f,0.48f),new Color(0.32f,0.75f,0.97f),0,3).shadowDepth=0;
        foreach(MenuPageTab.Page target in new[]{MenuPageTab.Page.Shop,MenuPageTab.Page.Home,MenuPageTab.Page.Rank})
        {
            string name=target+"TabButton";
            var item=nav.Find(name);Button button;
            if(item==null)button=Button(name,nav,new Vector2(0.5f,0),Vector2.zero,new Vector2(260,138),Blue,Blue);else button=item.GetComponent<Button>();
            bool active=target==selected;float x=((int)target-1)*280;
            Rect(button.transform,new Vector2(0.5f,0),new Vector2(0.5f,0),new Vector2(x,0),new Vector2(260,active?182:138));
            var surface=Surface(button.gameObject,active?new Color(0.18f,0.65f,0.94f):new Color(0.10f,0.36f,0.66f),new Color(0.06f,0.29f,0.57f),active?new Color(1,0.78f,0.26f):new Color(0.25f,0.58f,0.83f),active?28:12,active?4:1);surface.raycastTarget=true;button.targetGraphic=surface;
            foreach(string old in new[]{"ActionIcon","Label","Emblem","Caption"})Remove(button.transform,old);
            var emblem=Node("Emblem",button.transform,new Vector2(0.5f,1),new Vector2(0.5f,1),new Vector2(0,active?-10:-18),new Vector2(active?112:94,active?112:94)).AddComponent<MenuTabEmblem>();emblem.page=target;emblem.raycastTarget=false;
            var caption=Text("Caption",button.transform,target.ToString(),new Vector2(0.5f,0),new Vector2(0,10),new Vector2(240,45),33,Color.white);caption.fontStyle=FontStyle.Bold;caption.gameObject.SetActive(active);
            var hint=button.GetComponent<IconButtonHint>()??button.gameObject.AddComponent<IconButtonHint>();hint.description=target.ToString();
            if(page.name!="HomePanel"){var link=button.GetComponent<MenuPageTab>()??button.gameObject.AddComponent<MenuPageTab>();link.destination=target;}
        }
        nav.SetAsLastSibling();
    }
    private static void BuildPlay(Button play)
    {
        Rect(play.transform,new Vector2(0.5f,0),new Vector2(0.5f,0),new Vector2(0,232),new Vector2(350,108));
        var surface=Surface(play.gameObject,new Color(0.39f,0.87f,0.22f),new Color(0.12f,0.59f,0.13f),new Color(1,0.93f,0.57f),30,4);surface.raycastTarget=true;play.targetGraphic=surface;
        Remove(play.transform,"PlayLabel");Remove(play.transform,"PulseGlow");Remove(play.transform.parent,"PulseGlow");
        var icon=play.transform.Find("ActionIcon").GetComponent<MenuIcon>();Center(icon.rectTransform,new Vector2(-82,0),new Vector2(42,42));icon.color=Color.white;
        var label=Text("PlayLabel",play.transform,"Play",new Vector2(0.5f,0.5f),new Vector2(28,0),new Vector2(180,78),48,Color.white);label.fontStyle=FontStyle.Bold;
        var glow=Node("PulseGlow",play.transform,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(374,132));glow.transform.SetAsFirstSibling();
        Surface(glow,new Color(0.85f,1,0.45f),new Color(0.85f,1,0.45f),new Color(0.85f,1,0.45f),36,0).shadowDepth=0;
        var group=glow.AddComponent<CanvasGroup>();group.alpha=0.12f;group.blocksRaycasts=false;group.interactable=false;
        // A child renders above its parent, so keep the glow behind the button in its own sibling.
        glow.transform.SetParent(play.transform.parent,false);Rect(glow.transform,new Vector2(0.5f,0),new Vector2(0.5f,0),new Vector2(0,220),new Vector2(374,132));glow.transform.SetSiblingIndex(play.transform.GetSiblingIndex());
        var pulse=play.GetComponent<PlayButtonPulse>()??play.gameObject.AddComponent<PlayButtonPulse>();pulse.glow=group;
    }
    private static void BuildRank(Transform rank)
    {
        rank.Find("RankBackButton").gameObject.SetActive(false);rank.Find("RecordsHeader").gameObject.SetActive(false);
        var title=rank.Find("RankTitle").GetComponent<Text>();title.text="Rank";title.alignment=TextAnchor.MiddleCenter;Rect(title.transform,new Vector2(0.5f,1),new Vector2(0.5f,1),new Vector2(0,-30),new Vector2(800,65));
        Rect(rank.Find("RecordSummary"),new Vector2(1,1),new Vector2(1,1),new Vector2(-160,-142),new Vector2(540,40));rank.Find("RecordSummary").GetComponent<Text>().alignment=TextAnchor.MiddleRight;
        Remove(rank,"RecordDescription");rank.Find("RecordSummary").gameObject.SetActive(false);
        var rows=new Transform[10];for(int i=0;i<10;i++){rows[i]=rank.Find($"RecordRow_{i+1:00}")??rank.Find($"RecordScroll/Viewport/Content/RecordRow_{i+1:00}");if(rows[i]!=null)rows[i].SetParent(rank,false);}
        Remove(rank,"RecordScroll");
        var scroll=Node("RecordScroll",rank,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),Vector2.zero,Vector2.zero);
        var sr=(RectTransform)scroll.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.offsetMin=new Vector2(150,200);sr.offsetMax=new Vector2(-150,-150);
        var viewport=Node("Viewport",scroll.transform,Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero);var vr=(RectTransform)viewport.transform;vr.anchorMax=Vector2.one;vr.offsetMin=Vector2.zero;vr.offsetMax=new Vector2(-26,0);viewport.AddComponent<RectMask2D>();var hit=viewport.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
        var content=Node("Content",viewport.transform,new Vector2(0.5f,1),new Vector2(0.5f,1),Vector2.zero,new Vector2(0,1300));var cr=(RectTransform)content.transform;cr.anchorMin=new Vector2(0,1);cr.anchorMax=new Vector2(1,1);
        var scrollRect=scroll.AddComponent<ScrollRect>();scrollRect.viewport=vr;scrollRect.content=cr;scrollRect.horizontal=false;scrollRect.movementType=ScrollRect.MovementType.Clamped;scrollRect.scrollSensitivity=35;
        var pictures=new Sprite[10];for(int i=0;i<10;i++)pictures[i]=AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/UI/RoutePreviews/Route_{i+1:00}.png");
        for(int i=0;i<10;i++)
        {
            var row=rows[i];row.SetParent(content.transform,false);
            var rr=Rect(row,new Vector2(0.5f,1),new Vector2(0.5f,1),new Vector2(0,-i*130-4),new Vector2(0,114));rr.anchorMin=new Vector2(0,1);rr.anchorMax=new Vector2(1,1);rr.sizeDelta=new Vector2(-10,114);
            var surface=Surface(row.gameObject,new Color(0.23f,0.77f,0.92f),new Color(0.10f,0.53f,0.78f),new Color(0.51f,0.87f,1),24,3);surface.raycastTarget=true;
            foreach(string child in new[]{"Badge","Portrait","RouteNumber","ScorePill","TimeCaption"})Remove(row,child);
            var badge=Node("Badge",row,new Vector2(0,0.5f),new Vector2(0,0.5f),new Vector2(18,0),new Vector2(76,80));var badgeSurface=Surface(badge,Blue,Blue,new Color(0.70f,0.93f,1),20,2);
            var medal=MenuIcon.Attach(badge.transform,MenuIcon.Kind.Trophy,48);medal.color=Ink;
            var place=Text("Position",badge.transform,"—",new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(68,58),32,Color.white);
            var portrait=Node("Portrait",row,new Vector2(0,0.5f),new Vector2(0,0.5f),new Vector2(112,0),new Vector2(192,108));var photo=portrait.AddComponent<Image>();photo.preserveAspect=true;photo.raycastTarget=false;
            var route=row.Find("Route").GetComponent<Text>();Rect(route.transform,new Vector2(0,0.5f),new Vector2(0,0.5f),new Vector2(326,17),new Vector2(548,44));route.fontSize=30;route.fontStyle=FontStyle.Bold;route.color=Color.white;
            var number=Text("RouteNumber",row,"Route 01",new Vector2(0,0.5f),new Vector2(328,-24),new Vector2(270,32),19,new Color(0.79f,0.95f,1),TextAnchor.MiddleLeft);
            var status=row.Find("Status").GetComponent<Text>();Rect(status.transform,new Vector2(0,0.5f),new Vector2(0,0.5f),new Vector2(502,-24),new Vector2(250,32));status.fontSize=19;
            var time=row.Find("Time").GetComponent<Text>();Rect(time.transform,new Vector2(1,0.5f),new Vector2(1,0.5f),new Vector2(-300,-14),new Vector2(250,42));time.alignment=TextAnchor.MiddleCenter;time.fontSize=27;time.color=Color.white;
            Text("TimeCaption",row,"Best time",new Vector2(1,0.5f),new Vector2(-300,25),new Vector2(250,26),18,new Color(0.79f,0.95f,1));
            var pill=Node("ScorePill",row,new Vector2(1,0.5f),new Vector2(1,0.5f),new Vector2(-20,0),new Vector2(250,76));Surface(pill,new Color(0.10f,0.39f,0.58f),new Color(0.055f,0.28f,0.47f),new Color(0.45f,0.81f,0.94f),32,2);
            var trophy=MenuIcon.Attach(pill.transform,MenuIcon.Kind.Trophy,34);trophy.color=new Color(1,0.82f,0.30f);trophy.rectTransform.anchoredPosition=new Vector2(-82,0);
            pill.transform.SetAsFirstSibling();
            var drift=row.Find("Drift").GetComponent<Text>();Rect(drift.transform,new Vector2(1,0.5f),new Vector2(1,0.5f),new Vector2(-35,8),new Vector2(170,40));drift.alignment=TextAnchor.MiddleCenter;drift.fontSize=30;drift.fontStyle=FontStyle.Bold;drift.color=Color.white;
            Text("DriftCaption",pill.transform,"Drift",new Vector2(0.5f,0.5f),new Vector2(25,-21),new Vector2(150,26),16,new Color(0.75f,0.91f,1));
            var view=row.GetComponent<RouteRecordCard>()??row.gameObject.AddComponent<RouteRecordCard>();view.thumbnail=photo;view.positionText=place;view.routeNumber=number;view.medal=medal;view.badge=badgeSurface;view.surface=surface;view.routePictures=pictures;
        }
        var track=Node("Scrollbar",scroll.transform,new Vector2(1,0.5f),new Vector2(1,0.5f),Vector2.zero,new Vector2(12,0));var tr=(RectTransform)track.transform;tr.anchorMin=new Vector2(1,0);tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(-12,0);tr.offsetMax=Vector2.zero;
        var trackSurface=Surface(track,new Color(0.1f,0.38f,0.56f,0.45f),new Color(0.1f,0.38f,0.56f,0.45f),Color.clear,6,0);
        var handle=Node("Handle",track.transform,Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero);var hr=(RectTransform)handle.transform;hr.anchorMax=Vector2.one;hr.offsetMin=hr.offsetMax=Vector2.zero;
        var hg=Surface(handle,new Color(0.77f,0.94f,1),new Color(0.39f,0.78f,0.96f),Color.clear,6,0);hg.raycastTarget=true;
        var bar=track.AddComponent<Scrollbar>();bar.handleRect=hr;bar.targetGraphic=hg;bar.direction=Scrollbar.Direction.BottomToTop;scrollRect.verticalScrollbar=bar;
    }
    private static void BuildHeaderAndSettings(Transform canvas)
    {
        canvas.Find("HomePanel/GameTitle").GetComponent<Text>().text="Poly Drift";
        foreach(string path in new[]{"HomePanel/CoinButton","ShopPanel/ShopWallet"})
        {
            var wallet=canvas.Find(path);
            var surface=Surface(wallet.gameObject,new Color(0.99f,0.99f,0.91f),new Color(0.83f,0.94f,0.96f),new Color(1f,0.80f,0.29f),32,3);
            wallet.Find("Balance").GetComponent<Text>().color=Ink;
            var button=wallet.GetComponent<Button>();if(button!=null){button.targetGraphic=surface;surface.raycastTarget=true;}
        }
        var settings=canvas.Find("HomePanel/SettingsButton").GetComponent<Button>();
        Rect(settings.transform,new Vector2(1,1),new Vector2(1,1),new Vector2(-42,-24),new Vector2(78,78));
        var settingsSurface=Surface(settings.gameObject,new Color(0.30f,0.78f,1),new Color(0.055f,0.38f,0.77f),new Color(0.85f,0.97f,1),24,3);settingsSurface.raycastTarget=true;settings.targetGraphic=settingsSurface;
        var popup=canvas.Find("SettingsPopup");popup.GetComponent<Image>().color=new Color(0.025f,0.10f,0.20f,0.72f);
        var dialog=popup.Find("Dialog");Rect(dialog,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(0,-15),new Vector2(740,490));
        Surface(dialog.gameObject,new Color(0.96f,0.99f,1),new Color(0.78f,0.91f,0.98f),new Color(1,0.78f,0.22f),44,7);
        Remove(dialog,"Inset");var inset=Node("Inset",dialog,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(0,-35),new Vector2(640,290));
        Surface(inset,new Color(0.62f,0.84f,0.94f),new Color(0.81f,0.94f,0.99f),Color.white,32,3).shadowDepth=0;
        Remove(popup,"TitleRibbon");var ribbon=Node("TitleRibbon",popup,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(0,190),new Vector2(790,116));
        Surface(ribbon,new Color(0.23f,0.74f,1),new Color(0.05f,0.40f,0.79f),new Color(1,0.82f,0.34f),24,4);
        var title=popup.Find("Title").GetComponent<Text>();Rect(title.transform,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(0,190),new Vector2(620,100));title.fontSize=52;title.fontStyle=FontStyle.Bold;title.color=Color.white;title.transform.SetAsLastSibling();
        var close=popup.Find("CloseButton").GetComponent<Button>();Rect(close.transform,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(360,286),new Vector2(82,82));
        var closeSurface=Surface(close.gameObject,new Color(0.35f,0.84f,1),new Color(0.05f,0.37f,0.78f),new Color(1,0.80f,0.25f),41,4);closeSurface.raycastTarget=true;close.targetGraphic=closeSurface;close.transform.SetAsLastSibling();
        foreach(string name in new[]{"MusicToggle","SoundToggle"})
        {
            var toggle=popup.Find(name).GetComponent<Toggle>();
            Rect(toggle.transform,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(name=="MusicToggle"?-145:145,-50),new Vector2(166,166));
            foreach(string child in new[]{"ToggleTrack","Checkmark","AudioSetting","MuteSlash"})Remove(toggle.transform,child);
            var surface=Surface(toggle.gameObject,new Color(0.32f,0.85f,1),new Color(0.04f,0.36f,0.8f),Color.white,83,6);surface.raycastTarget=true;surface.shadowDepth=9;toggle.targetGraphic=surface;toggle.graphic=null;
            var icon=toggle.transform.Find("Audio icon").GetComponent<MenuIcon>();Center(icon.rectTransform,Vector2.zero,new Vector2(86,86));icon.color=Color.white;
            var slash=Node("MuteSlash",toggle.transform,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(114,10));slash.transform.localRotation=Quaternion.Euler(0,0,-45);var slashImage=slash.AddComponent<Image>();slashImage.color=new Color(1,0.34f,0.29f);slashImage.raycastTarget=false;
            var visual=toggle.GetComponent<AudioToggleVisual>()??toggle.gameObject.AddComponent<AudioToggleVisual>();visual.surface=surface;visual.icon=icon;visual.muteSlash=slash;visual.Refresh();
        }
        popup.Find("SettingsStatusText").gameObject.SetActive(false);
        foreach(Transform child in canvas)if(child.name=="Icon hint")child.gameObject.SetActive(false);
    }

    [MenuItem("Tools/PolyCar/Polish menu cards and navigation")]
    public static void UpdateScene()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/DriftPrototype.unity");Apply(Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include).transform);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("MENU_POLISH_BUILT");
    }
}
