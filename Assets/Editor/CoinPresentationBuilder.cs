using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

/// <summary>Readable collectibles and HUD, with the existing pickup reach and value.</summary>
public static class CoinPresentationBuilder
{
    private const string PrefabPath="Assets/Prefabs/Pickups/Coin.prefab";
    private const string ScenePath="Assets/Scenes/DriftPrototype.unity";
    private const string Output="Review/Coins";

    public static void ConfigurePickup(GameObject coin)
    {
        coin.transform.localScale=new Vector3(1.2f,.22f,1.2f);
        // Match the pickup volume to the visible coin so visible contact collects it.
        var collider=coin.GetComponent<CapsuleCollider>();
        if(collider==null)throw new InvalidOperationException("Expected capsule pickup collider");
        collider.radius=.5f;collider.height=2f;collider.isTrigger=true;
    }

    [MenuItem("Tools/PolyCar/Enlarge Coins And Review")]
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        Capture("before",1280,720);
        var prefab=PrefabUtility.LoadPrefabContents(PrefabPath);
        try {ConfigurePickup(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,PrefabPath);}
        finally {PrefabUtility.UnloadPrefabContents(prefab);}
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var maps=GameObject.Find("LevelMaps").transform;
        var coins=maps.GetComponentsInChildren<CoinPickup>(true);
        foreach(var coin in coins)
        {
            ConfigurePickup(coin.gameObject);
            var collider=coin.GetComponent<CapsuleCollider>();
            if(coin.Value!=1 || !collider.isTrigger || Mathf.Abs(collider.radius*coin.transform.localScale.x-.6f)>.0001f)
                throw new InvalidOperationException("Coin reach/value changed");
            PrefabUtility.RecordPrefabInstancePropertyModifications(coin.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
        }
        GameplayPresentationBuilder.ApplyCoinSizing(GameObject.Find("Canvas").transform.Find("GameplayHUD"));
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Capture("after-16x9",1280,720);Capture("after-4x3",1024,768);
        File.WriteAllText(Path.Combine(Output,"review.md"),$"# 金币显示调整\n\n赛道金币视觉尺寸扩大为初始尺寸的 2.67 倍，胶囊触发范围与可见金币一致，{coins.Length} 枚金币的数量、位置、价值和收集规则不变。\n\n驾驶 HUD 金币图标由 56×56 调整为 68×68，数字字号由 34 调整为 38，面板加宽以留出间距（均为参考画布单位）。\n\n已检查 1280×720 和 1024×768 两种布局。\n\n[修改前](before.png) · [修改后 16:9](after-16x9.png) · [修改后 4:3](after-4x3.png)\n");
        Debug.Log($"COIN_PRESENTATION_OK coins={coins.Length} visualScale=2.67 pickupReachMatchesVisual=true ratios=2");
        EditorSceneManager.OpenScene(ScenePath);
        EnvironmentDeliveryBuilder.BuildPlayer();
    }

    private static void Capture(string name,int width,int height)
    {
        EditorSceneManager.OpenScene(ScenePath);
        var maps=GameObject.Find("LevelMaps").transform;
        for(int i=0;i<maps.childCount;i++)maps.GetChild(i).gameObject.SetActive(false);
        var level=maps.GetChild(0);level.gameObject.SetActive(true);
        var course=level.GetComponent<RaceCourse>();
        var coins=level.GetComponentsInChildren<CoinPickup>();
        float length=0;for(int i=1;i<course.centerLine.Length;i++)length+=Vector3.Distance(course.centerLine[i-1],course.centerLine[i]);
        HarborArtBuilder.Sample(course.centerLine,length/(coins.Length+1)-6,out var point,out var forward);
        var car=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vehicles/StarterCar.prefab"));
        car.transform.SetPositionAndRotation(point+Vector3.up*.24f,Quaternion.LookRotation(forward));
        var camera=Camera.main;camera.fieldOfView=64;
        camera.transform.position=point-forward*8.5f+Vector3.up*3.65f;
        camera.transform.LookAt(point+forward*4+Vector3.up*1.1f);
        level.GetComponent<SceneLook>().Apply(camera);
        var canvas=GameObject.Find("Canvas").GetComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        foreach(Transform child in canvas.transform)child.gameObject.SetActive(child.name=="GameplayHUD");
        var hud=canvas.transform.Find("GameplayHUD");
        hud.Find("LevelText").GetComponent<Text>().text="01 Harbor Loop";
        hud.Find("GuidanceText").GetComponent<Text>().text="00:04.2\n1/12";
        hud.Find("MessageText").GetComponent<Text>().text="";
        hud.Find("CoinText").GetComponent<Text>().text="0/10";
        hud.Find("SpeedText").GetComponent<Text>().text="48";
        hud.Find("DriftScoreText").GetComponent<Text>().text="0";
        hud.Find("DriftStateText").GetComponent<Text>().text="";
        var target=new RenderTexture(width,height,24){antiAliasing=4};var previous=RenderTexture.active;
        camera.targetTexture=target;
        try
        {
            canvas.GetComponent<CanvasScaler>().SendMessage("Update");Canvas.ForceUpdateCanvases();
            for(int i=0;i<4;i++)camera.Render();
            if(name.StartsWith("after"))
            {
                var text=hud.Find("CoinText").GetComponent<Text>();text.text="28/28";
                if(text.preferredWidth>text.rectTransform.rect.width)throw new InvalidOperationException("Coin count clipped");
                text.text="0/10";
            }
            RenderTexture.active=target;var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());Object.DestroyImmediate(image);
        }
        finally{camera.targetTexture=null;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);}
    }
}
