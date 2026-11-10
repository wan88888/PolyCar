using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class InterfaceFeedbackReview
{
    const string Scene="Assets/Scenes/DriftPrototype.unity";
    const string Output="Review/InterfaceFeedback";
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,args);
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
    public static void Run()
    {
        WithPreservedState(RunReview);
    }

    public static void WithPreservedState(Action review)
    {
        const string key = "PolyCar_SelectedRoute";
        bool existed = PlayerPrefs.HasKey(key);
        int selected = PlayerPrefs.GetInt(key);
        float timeScale = Time.timeScale;
        try { review(); }
        finally
        {
            if (existed) PlayerPrefs.SetInt(key, selected);
            else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Time.timeScale = timeScale;
        }
    }

    private static void RunReview()
    {
        Directory.CreateDirectory(Output);
        var scene=EditorSceneManager.OpenScene(Scene);
        const string path="Assets/Prefabs/Pickups/Coin.prefab";
        var prefab=PrefabUtility.LoadPrefabContents(path);
        try{CoinPresentationBuilder.ConfigurePickup(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,path);}
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        var canvas=GameObject.Find("Canvas").GetComponent<Canvas>();
        foreach(var coin in GameObject.Find("LevelMaps").GetComponentsInChildren<CoinPickup>(true))
        {
            CoinPresentationBuilder.ConfigurePickup(coin.gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(coin.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(coin.GetComponent<Collider>());
        }
        typeof(MenuPolishBuilder).GetMethod("BuildRank",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{canvas.transform.Find("RankPanel")});
        GameplayPresentationBuilder.ApplyCoinSizing(canvas.transform.Find("GameplayHUD"));
        var pictures=new Sprite[10];
        for(int i=0;i<10;i++)pictures[i]=canvas.transform.Find($"LevelSelectPanel/LevelButton_{i+1:00}/Preview").GetComponent<Image>().sprite;
        foreach(var row in canvas.GetComponentsInChildren<RouteRecordCard>(true))row.routePictures=pictures;
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=Camera.main;canvas.planeDistance=1;
        foreach(Transform child in canvas.transform)child.gameObject.SetActive(child.name=="RankPanel");
        var menu=Object.FindFirstObjectByType<MainMenuUI>();Call(menu,"RefreshRank");
        foreach(var row in canvas.GetComponentsInChildren<RouteRecordCard>(true))
        {
            for(int i=0;i<10;i++)
            {
                row.Refresh(i,i+1,false,true);
                if(row.thumbnail.sprite!=pictures[i] || row.thumbnail.type!=Image.Type.Simple || !row.thumbnail.preserveAspect)throw new Exception("Rank preview mismatch");
            }
        }
        Call(menu,"RefreshRank");Capture(canvas,"rank",1280,720);Capture(canvas,"rank-4x3",1024,768);
        var scroll=canvas.transform.Find("RankPanel/RecordScroll").GetComponent<ScrollRect>();scroll.verticalNormalizedPosition=0;Capture(canvas,"rank-bottom",1280,720);
        foreach(Transform child in canvas.transform)child.gameObject.SetActive(child.name=="GameplayHUD");
        var level=Object.FindFirstObjectByType<LevelManager>();Call(level,"Awake");Call(level,"LoadLevel",0,true);
        var spawn=GameObject.Find("LevelMaps").transform.GetChild(0).Find("PlayerSpawnPoint");
        var car=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vehicles/StarterCar.prefab"),spawn.position,spawn.rotation).GetComponent<CarController>();
        Call(car,"Awake");level.SetPlayerCar(car);level.BeginRun();Call(level,"TickRun",3.1f);
        Camera.main.transform.position=spawn.position-spawn.forward*8+Vector3.up*4.2f;Camera.main.transform.LookAt(spawn.position+spawn.forward*5+Vector3.up*.6f);
        var coins=GameObject.Find("LevelMaps").transform.GetChild(0).GetComponentsInChildren<CoinPickup>();
        for(int i=0;i<2;i++)
        {
            Call(coins[i],"Awake");Set(coins[i],"levelManager",level);Call(coins[i],"OnTriggerEnter",car.GetComponentInChildren<Collider>());
            var count=canvas.transform.Find("GameplayHUD/CoinText").GetComponent<Text>().text;
            if(count!=$"{i+1}/10" || !coins[i].IsCollected)throw new Exception("Live coin counter failed: "+count);
        }
        Call(level,"SetMessage",string.Empty);Capture(canvas,"collected-two",1280,720);Capture(canvas,"collected-two-4x3",1024,768);
        var pause=PauseMenuView.Create(canvas.transform,()=>{},()=>{},()=>{});pause.SetActive(true);
        if(pause.transform.Find("Controls")!=null)throw new Exception("Pause instructions remain");
        Capture(canvas,"pause",1280,720);Capture(canvas,"pause-4x3",1024,768);
        Debug.Log("INTERFACE_FEEDBACK_OK rankPictures=10 fullFrame=true collectedCounter=2/10 pauseCopyRemoved=true ratios=2");
        EditorSceneManager.OpenScene(Scene);
        EnvironmentDeliveryBuilder.BuildPlayer();
    }
    static void Capture(Canvas canvas,string name,int w,int h)
    {
        var camera=Camera.main;var rt=new RenderTexture(w,h,24){antiAliasing=4};var previous=RenderTexture.active;
        camera.targetTexture=rt;canvas.GetComponent<CanvasScaler>().SendMessage("Update");Canvas.ForceUpdateCanvases();
        if(name.StartsWith("rank"))
        {
            var scroll=canvas.transform.Find("RankPanel/RecordScroll").GetComponent<ScrollRect>();
            scroll.Rebuild(CanvasUpdate.PostLayout);scroll.verticalNormalizedPosition=name=="rank-bottom"?0:1;Canvas.ForceUpdateCanvases();
        }
        for(int i=0;i<4;i++)camera.Render();
        RenderTexture.active=rt;var image=new Texture2D(w,h,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();
        File.WriteAllBytes($"{Output}/{name}.png",image.EncodeToPNG());Object.DestroyImmediate(image);
        camera.targetTexture=null;RenderTexture.active=previous;rt.Release();Object.DestroyImmediate(rt);
    }
}
