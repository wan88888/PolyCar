using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

/// <summary>Offline completion fixtures; never award currency or record fake results.</summary>
public static class CompletionPresentationReview
{
    private static object Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(obj,args);
    private static void Capture(Canvas canvas,string name,int w,int h)=>typeof(MenuPolishReview).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{canvas,name,w,h});
    private static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void Run()
    {
        string[] keys={"PolyCar_SelectedRoute","PolyCar_HighestUnlockedRoute"};
        bool[] exists=Array.ConvertAll(keys,PlayerPrefs.HasKey);int[] values=Array.ConvertAll(keys,k=>PlayerPrefs.GetInt(k));
        int wallet=SaveManager.TotalCoins;
        try
        {
            MenuPolishBuilder.UpdateScene();HarborPreviewCapture.Run();
            Directory.CreateDirectory("/tmp/polycar-polish-review");
            var canvas=Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);canvas.gameObject.SetActive(true);
            var menu=Object.FindFirstObjectByType<MainMenuUI>();var level=Object.FindFirstObjectByType<LevelManager>();
            typeof(MainMenuUI).GetField("transitionFadeGroup",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(menu,null);
            Call(menu,"CreatePausePanel");Call(menu,"RegisterButtons");Call(level,"Awake");Call(level,"LoadLevel",0,true);
            var spawn=GameObject.Find("LevelMaps").transform.GetChild(0).Find("PlayerSpawnPoint");
            Camera.main.transform.position=spawn.position-spawn.forward*8+Vector3.up*4.2f;
            Camera.main.transform.LookAt(spawn.position+spawn.forward*5+Vector3.up*0.6f);
            var data=new LevelManager.LevelCompletionData{RouteName="Harbor Loop",Medal="GOLD",Seconds=83.42f,DriftScore=3280,EarnedCoins=48,CollectedCoins=12,BonusCoins=40,PenaltyCoins=4,NewBestDrift=true,HasNextRoute=true};
            Call(menu,"ShowCompletion",data);
            var view=canvas.GetComponentInChildren<CompletionView>(true);var card=view.transform.Find("CompletionCard");
            Require(view.timeValue.text=="01:23.42"&&view.driftValue.text=="3,280"&&view.rewardValue.text=="+48","Result formatting/net reward failed");
            Require(view.bestBadge.gameObject.activeSelf&&!canvas.transform.Find("GameplayHUD").gameObject.activeSelf,"Completion visibility failed");
            Capture(canvas,"Completion-Gold-16x9",1280,720);Capture(canvas,"Completion-Gold-4x3",1024,768);
            data.Medal="SILVER";data.NewBestDrift=false;data.EarnedCoins=0;Call(menu,"ShowCompletion",data);
            Require(!view.bestBadge.gameObject.activeSelf&&view.rewardValue.text=="+0"&&view.medalName.text=="Silver","Repeat/zero result failed");
            Capture(canvas,"Completion-Silver",1280,720);
            SaveManager.UnlockRoute(1);view.primaryButton.onClick.Invoke();
            Require(level.CurrentLevelIndex==1&&level.Phase==LevelManager.RunPhase.Countdown&&!view.gameObject.activeSelf,"Next route failed");
            Call(menu,"ShowCompletion",data);card.Find("CompletionReplayButton").GetComponent<Button>().onClick.Invoke();
            Require(level.CurrentLevelIndex==1&&level.Phase==LevelManager.RunPhase.Countdown,"Retry failed");
            Call(menu,"ShowCompletion",data);view.routesButton.onClick.Invoke();Require(canvas.transform.Find("LevelSelectPanel").gameObject.activeSelf&&!view.gameObject.activeSelf,"Routes failed");
            Call(menu,"ShowCompletion",data);card.Find("CompletionHomeButton").GetComponent<Button>().onClick.Invoke();Require(canvas.transform.Find("HomePanel").gameObject.activeSelf&&!view.gameObject.activeSelf,"Home failed");
            Call(level,"LoadLevel",9,true);data.RouteName=level.GetLevelName(9);data.Medal="BRONZE";data.HasNextRoute=false;data.DriftScore=900;data.EarnedCoins=25;Call(menu,"ShowCompletion",data);
            Require(view.primaryLabel.text=="Routes"&&!view.routesButton.gameObject.activeSelf&&view.heading.text=="All Routes Complete","Final route display failed");
            Capture(canvas,"Completion-Final-16x9",1280,720);Capture(canvas,"Completion-Final-4x3",1024,768);
            view.primaryButton.onClick.Invoke();Require(level.CurrentLevelIndex==9&&canvas.transform.Find("LevelSelectPanel").gameObject.activeSelf,"Final route must return to routes without wrapping");
            Require(SaveManager.TotalCoins==wallet,"Completion presentation changed wallet");
            Debug.Log("COMPLETION_PRESENTATION_OK medals=3 ratios=2 netReward=true zeroReward=true newBest=true nextRetryRoutesHome=true finalRouteReturns=true noDuplicateReward=true");
        }
        finally{for(int i=0;i<keys.Length;i++){if(exists[i])PlayerPrefs.SetInt(keys[i],values[i]);else PlayerPrefs.DeleteKey(keys[i]);}PlayerPrefs.Save();Time.timeScale=1;}
    }
}
