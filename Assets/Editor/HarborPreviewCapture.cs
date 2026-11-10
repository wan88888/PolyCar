using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Offline art/HUD review captures. These do not claim human playtesting.</summary>
public static class HarborPreviewCapture
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/DriftPrototype.unity");
        var canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        var maps = GameObject.Find("LevelMaps").transform;
        for (int i=0;i<maps.childCount;i++) maps.GetChild(i).gameObject.SetActive(i==0);
        var spawn = maps.GetChild(0).Find("PlayerSpawnPoint");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vehicles/StarterCar.prefab");
        var car=UnityEngine.Object.Instantiate(prefab,spawn.position,spawn.rotation);
        var camera=Camera.main;
        camera.transform.position=spawn.position-spawn.forward*8f+Vector3.up*4.2f;
        camera.transform.LookAt(spawn.position+spawn.forward*5f+Vector3.up*0.6f);
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1f;
        foreach(Transform child in canvas.transform)child.gameObject.SetActive(child.name=="GameplayHUD");
        var hud=canvas.transform.Find("GameplayHUD");
        hud.Find("GarageText").gameObject.SetActive(false);hud.Find("TotalCoinsText").gameObject.SetActive(false);
        hud.Find("LevelText").GetComponent<Text>().text="01 / HARBOR LOOP";
        hud.Find("GuidanceText").GetComponent<Text>().text="00.0s  /  BEST --\nGATE 01/24    42m   Ahead";
        hud.Find("MessageText").GetComponent<Text>().text="HARBOR SESSIONS\n2 laps / 24 gates / drift for medals";
        hud.Find("CoinText").GetComponent<Text>().text="PICKUPS 0/10";
        Capture(camera,canvas,"/tmp/polycar-harbor-drive.png",1280,720);
        Capture(camera,canvas,"/tmp/polycar-harbor-drive-4x3.png",1024,768);
        foreach(Transform child in canvas.transform)child.gameObject.SetActive(child.name=="HomePanel");
        var preview=canvas.GetComponentInChildren<GaragePreview>();
        Call(preview,"Start");Call(preview,"Update");
        var pc=(Camera)typeof(GaragePreview).GetField("previewCamera",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(preview);
        for(int i=0;i<4;i++)pc.Render();
        Call(UnityEngine.Object.FindFirstObjectByType<MainMenuUI>(),"RefreshHomeText");
        Capture(camera,canvas,"/tmp/polycar-harbor-home.png",1280,720);
        foreach(Transform child in canvas.transform)child.gameObject.SetActive(child.name=="ShopPanel");
        var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuUI>();
        Call(menu,"RefreshShop");
        Capture(camera,canvas,"/tmp/polycar-harbor-shop.png",1280,720);
        foreach(Transform child in canvas.transform)child.gameObject.SetActive(child.name=="RankPanel");
        Call(menu,"RefreshRank");
        Capture(camera,canvas,"/tmp/polycar-harbor-records.png",1280,720);
        canvas.gameObject.SetActive(false);
        camera.transform.position=new Vector3(-135,120,-140);camera.transform.LookAt(new Vector3(0,0,15));
        Capture(camera,null,"/tmp/polycar-harbor-overview.png",1280,720);
        Debug.Log("HARBOR_CAPTURE_OK");

    }
    private static void Call(object obj,string method)=>obj.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,null);
    private static void Capture(Camera camera,Canvas canvas,string path,int width,int height)
    {
        var target=new RenderTexture(width,height,24);target.antiAliasing=4;camera.targetTexture=target;
        if(canvas!=null)Canvas.ForceUpdateCanvases();
        for(int i=0;i<4;i++)camera.Render();
        RenderTexture.active=target;var tex=new Texture2D(width,height,TextureFormat.RGB24,false);
        tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(target);
    }
}
