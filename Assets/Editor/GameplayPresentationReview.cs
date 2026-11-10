using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class GameplayPresentationReview
{
    private static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(o,args);
    private static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
    private static void Capture(Canvas canvas,string name,int w,int h)=>typeof(MenuPolishReview).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{canvas,name,w,h});
    public static void Run()
    {
        MenuPolishReview.Run();
        bool hadRoute=PlayerPrefs.HasKey("PolyCar_SelectedRoute");int selectedRoute=PlayerPrefs.GetInt("PolyCar_SelectedRoute");
        try
        {
            HarborPreviewCapture.Run();
            var canvas=Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);canvas.gameObject.SetActive(true);
            var menu=Object.FindFirstObjectByType<MainMenuUI>();var level=Object.FindFirstObjectByType<LevelManager>();var scoring=Object.FindFirstObjectByType<GameManager>();
            Set(menu,"transitionFadeGroup",null);Call(menu,"CreatePausePanel");Call(menu,"RegisterButtons");
            menu.ShowLevelSelect();
            for(int i=1;i<=10;i++)
            {
                var card=canvas.transform.Find($"LevelSelectPanel/LevelButton_{i:00}");
                if(card.Find($"LevelMeta_{i:00}").gameObject.activeSelf||card.Find("NumberBadge").gameObject.activeSelf)throw new Exception("Route metadata still visible");
                var title=card.Find($"LevelTitle_{i:00}").GetComponent<Text>();if(title.text!=UiCopy.Title(title.text))throw new Exception("Route title casing failed");
            }
            Capture(canvas,"Routes-16x9",1280,720);Capture(canvas,"Routes-4x3",1024,768);
            Call(menu,"ChangeRoutePage",4);Capture(canvas,"Routes-last-page",1280,720);
            if(!canvas.transform.Find("LevelSelectPanel/LevelButton_10").gameObject.activeSelf)throw new Exception("Last route page missing");
            Call(level,"Awake");Call(level,"LoadLevel",0,true);
            foreach(var existing in Object.FindObjectsByType<CarController>(FindObjectsSortMode.None))if(existing.gameObject.layer!=31)Object.DestroyImmediate(existing.gameObject);
            var spawn=GameObject.Find("LevelMaps").transform.GetChild(0).Find("PlayerSpawnPoint");
            var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vehicles/StarterCar.prefab");
            var car=Object.Instantiate(prefab,spawn.position,spawn.rotation).GetComponent<CarController>();
            Call(car,"Awake");level.SetPlayerCar(car);scoring.SetPlayerCar(car);
            Camera.main.transform.position=spawn.position-spawn.forward*8+Vector3.up*4.2f;
            Camera.main.transform.LookAt(spawn.position+spawn.forward*5+Vector3.up*0.6f);
            Physics.SyncTransforms();
            Call(menu,"StartGame");Call(level,"TickRun",1.1f);Call(level,"UpdateGuidance");Call(scoring,"UpdateDrivingHud");Call(scoring,"UpdateDriftScoreText");
            var hud=canvas.transform.Find("GameplayHUD");
            if(hud.Find("MessageText").GetComponent<Text>().text!="2")throw new Exception("Countdown is not concise");
            Capture(canvas,"Countdown-16x9",1280,720);
            Call(level,"TickRun",2.1f);Call(level,"SetMessage",string.Empty);Call(level,"UpdateUi");Call(level,"UpdateGuidance");
            if(level.Phase!=LevelManager.RunPhase.Racing||!car.ControlsEnabled)throw new Exception("Countdown flow changed");
            Set(scoring,"totalDriftScore",1234f);Set(scoring,"comboMultiplier",1.7f);Call(scoring,"UpdateDriftScoreText");
            car.GetComponent<Rigidbody>().linearVelocity=car.transform.forward*15;Call(scoring,"UpdateDrivingHud");
            foreach(string name in new[]{"Controls","GarageText","TotalCoinsText"})if(hud.Find(name).gameObject.activeSelf)throw new Exception("Legacy HUD copy remains");
            if(hud.Find("GuidanceText").GetComponent<Text>().text.Contains("BEST")||hud.Find("DriftStateText").GetComponent<Text>().text=="Normal")throw new Exception("Legacy HUD strings remain");
            if(hud.Find("SpeedText").GetComponent<Text>().text!="54")throw new Exception("Speed numeric display failed");
            foreach(var text in hud.GetComponentsInChildren<Text>())if(text.text!=UiCopy.Title(text.text))throw new Exception("HUD casing: "+text.name);
            Capture(canvas,"DrivingHud-16x9",1280,720);Capture(canvas,"DrivingHud-4x3",1024,768);
            hud.Find("GameplayHomeButton").GetComponent<Button>().onClick.Invoke();
            var pause=canvas.transform.Find("Pause");
            if(!pause.gameObject.activeSelf||Time.timeScale!=0||hud.GetComponent<CanvasGroup>().interactable)throw new Exception("Pause modal failed");
            Capture(canvas,"Pause-16x9",1280,720);Capture(canvas,"Pause-4x3",1024,768);
            pause.Find("Resume").GetComponent<Button>().onClick.Invoke();if(pause.gameObject.activeSelf||Time.timeScale!=1||!hud.GetComponent<CanvasGroup>().interactable)throw new Exception("Resume failed");
            hud.Find("GameplayHomeButton").GetComponent<Button>().onClick.Invoke();pause.Find("Restart route").GetComponent<Button>().onClick.Invoke();
            if(level.Phase!=LevelManager.RunPhase.Countdown||pause.gameObject.activeSelf)throw new Exception("Restart failed");
            hud.Find("GameplayHomeButton").GetComponent<Button>().onClick.Invoke();pause.Find("CloseButton").GetComponent<Button>().onClick.Invoke();
            if(pause.gameObject.activeSelf||Time.timeScale!=1)throw new Exception("Pause close failed");
            hud.Find("GameplayHomeButton").GetComponent<Button>().onClick.Invoke();pause.Find("Home").GetComponent<Button>().onClick.Invoke();
            if(!canvas.transform.Find("HomePanel").gameObject.activeSelf||pause.gameObject.activeSelf)throw new Exception("Pause home failed");
            Debug.Log("GAMEPLAY_PRESENTATION_OK routesMinimal=true titleCase=true pages=5 hudMinimal=true countdown=true pauseResumeRestartCloseHome=true ratios=2");
        }
        finally{if(hadRoute)PlayerPrefs.SetInt("PolyCar_SelectedRoute",selectedRoute);else PlayerPrefs.DeleteKey("PolyCar_SelectedRoute");PlayerPrefs.Save();Time.timeScale=1;}
    }
}
