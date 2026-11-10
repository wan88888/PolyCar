using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class RaceFlowTests
{
    private Scene scene;
    private LevelManager level;
    private CarController car;
    private RaceCourse course;
    private readonly Dictionary<string, (bool exists, int value)> saved = new Dictionary<string, (bool,int)>();
    private float oldBest;
    private bool hadBest;

    [SetUp]
    public void Setup()
    {
        foreach (string key in new[] {"PolyCar_TotalCoins","PolyCar_WelcomeReward","PolyCar_SelectedRoute","PolyCar_HighestUnlockedRoute","PolyCar_RouteCompleted_0","PolyCar_v2_BestRouteCoins_0","PolyCar_v2_BestRouteDrift_0"})
            saved[key] = (PlayerPrefs.HasKey(key),PlayerPrefs.GetInt(key));
        hadBest=PlayerPrefs.HasKey("PolyCar_v2_BestTime_0");oldBest=PlayerPrefs.GetFloat("PolyCar_v2_BestTime_0");
        scene=EditorSceneManager.NewPreviewScene();
        var vehicle=Make("Car");vehicle.AddComponent<Rigidbody>().useGravity=false;car=vehicle.AddComponent<CarController>();Call(car,"Awake");
        var root=Make("Course");course=root.AddComponent<RaceCourse>();
        var gate=Make("Gate");gate.transform.position=new Vector3(0,0,10);course.gates=new[]{gate.transform};
        level=Make("Level").AddComponent<LevelManager>();
        Set(level,"levels",new[]{new LevelManager.LevelDefinition{displayName="Test",root=root.transform,spawnPoint=Make("Spawn").transform,targetCoins=1}});
        Set(level,"gameManager",null);Set(level,"carGarage",null);
        level.SetPlayerCar(car);level.RestartCurrentLevel();
    }

    [TearDown]
    public void Teardown()
    {
        EditorSceneManager.ClosePreviewScene(scene);
        foreach(var pair in saved) {if(pair.Value.exists)PlayerPrefs.SetInt(pair.Key,pair.Value.value);else PlayerPrefs.DeleteKey(pair.Key);}
        if(hadBest)PlayerPrefs.SetFloat("PolyCar_v2_BestTime_0",oldBest);else PlayerPrefs.DeleteKey("PolyCar_v2_BestTime_0");
        PlayerPrefs.Save();Time.timeScale=1f;
    }
    private GameObject Make(string name){var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);return go;}
    private static void Set(object o,string field,object value)=>o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
    private static void Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,args);
    private void StartRace(){level.BeginRun();Call(level,"TickRun",3.1f);}

    [Test]
    public void CountdownBlocksControlsAndDoesNotConsumeRaceTime()
    {
        level.BeginRun();car.SetInput(1,1,0,false);
        Assert.That(car.ControlsEnabled,Is.False);
        Call(level,"TickRun",2f);Assert.That(level.Phase,Is.EqualTo(LevelManager.RunPhase.Countdown));
        Assert.That(level.ElapsedSeconds,Is.Zero);
        Call(level,"TickRun",1.1f);Assert.That(car.ControlsEnabled,Is.True);
        Assert.That(level.Phase,Is.EqualTo(LevelManager.RunPhase.Racing));
    }
    [Test]
    public void PauseDoesNotAdvanceCountdownOrRaceClock()
    {
        level.BeginRun();Call(level,"TickRun",0f);Assert.That(level.Phase,Is.EqualTo(LevelManager.RunPhase.Countdown));
        Call(level,"TickRun",3.1f);Call(level,"TickRun",0f);Assert.That(level.ElapsedSeconds,Is.Zero);
    }
    [Test]
    public void CheckpointRequiresForwardCrossingInsideWidth()
    {
        Transform gate=course.gates[0];
        Assert.That(RaceCourse.CrossedGate(new Vector3(0,0,9),new Vector3(0,0,11),gate,5),Is.True);
        Assert.That(RaceCourse.CrossedGate(new Vector3(0,0,11),new Vector3(0,0,9),gate,5),Is.False);
        Assert.That(RaceCourse.CrossedGate(new Vector3(8,0,9),new Vector3(8,0,11),gate,5),Is.False);
    }
    [Test]
    public void CoinsAreOptionalAndOnlyFinalOrderedGateCompletesRun()
    {
        var second=Make("Final gate");second.transform.position=new Vector3(0,0,20);course.gates=new[]{course.gates[0],second.transform};
        StartRace();car.transform.position=new Vector3(0,0,21);Call(level,"TickRun",1f);
        Assert.That(level.PassedGates,Is.EqualTo(1));Assert.That(level.LevelComplete,Is.False);
        car.transform.position=new Vector3(0,0,19);Call(level,"TickRun",1f);
        car.transform.position=new Vector3(0,0,21);Call(level,"TickRun",1f);
        Assert.That(level.LevelComplete,Is.True);Assert.That(level.CollectedCoins,Is.Zero);
    }
    [Test]
    public void BonusCoinsAreSettledExactlyOnceAtFinish()
    {
        int balance=SaveManager.TotalCoins;StartRace();level.AwardBonusCoins(7,"test");
        Assert.That(SaveManager.TotalCoins,Is.EqualTo(balance));
        car.transform.position=new Vector3(0,0,11);Call(level,"TickRun",1f);
        Assert.That(SaveManager.TotalCoins,Is.EqualTo(balance+17));
        Call(level,"TickRun",1f);Assert.That(SaveManager.TotalCoins,Is.EqualTo(balance+17));
    }
    [Test]
    public void RestartDiscardsUnbankedRewardsAndResetsProgress()
    {
        int balance=SaveManager.TotalCoins;StartRace();level.AwardBonusCoins(7,"test");level.RestartCurrentLevel();
        Assert.That(SaveManager.TotalCoins,Is.EqualTo(balance));Assert.That(level.Phase,Is.EqualTo(LevelManager.RunPhase.Ready));
        Assert.That(level.ElapsedSeconds,Is.Zero);Assert.That(level.PassedGates,Is.Zero);Assert.That(car.ControlsEnabled,Is.False);
    }
    [Test]
    public void RecoveryCostsTimeAndDoesNotAdvanceGate()
    {
        StartRace();car.transform.position=new Vector3(100,0,100);level.RecoverCar();
        Assert.That(level.Recoveries,Is.EqualTo(1));Assert.That(level.ElapsedSeconds,Is.EqualTo(3f));Assert.That(level.PassedGates,Is.Zero);
        Assert.That(car.GetComponent<Rigidbody>().position.x,Is.EqualTo(0).Within(0.01));
    }
    [TestCase(Key.R, true)]
    [TestCase(Key.N, true)]
    [TestCase(Key.N, false)]
    public void ResultShortcutsRespectNextRouteAvailability(Key key, bool hasNext)
    {
        if(hasNext)
        {
            var definitions=(LevelManager.LevelDefinition[])typeof(LevelManager).GetField("levels",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(level);
            Set(level,"levels",new[]{definitions[0],new LevelManager.LevelDefinition{displayName="Next",root=Make("Next course").transform,spawnPoint=Make("Next spawn").transform}});
        }
        LevelManager.LevelCompletionData completion=default;
        level.LevelCompleted+=data=>completion=data;
        StartRace();car.transform.position=new Vector3(0,0,11);Call(level,"TickRun",1f);
        var menu=Make("Menu").AddComponent<MainMenuUI>();
        var result=Make("Results");var hud=Make("HUD");hud.SetActive(false);
        var routes=Make("Routes");routes.SetActive(false);
        Set(menu,"levelManager",level);Set(menu,"completionPanel",result);Set(menu,"gameplayHudPanel",hud);Set(menu,"levelSelectPanel",routes);
        var button=Make("Next").AddComponent<Button>();Set(menu,"completionNextButton",button);
        Call(menu,"ShowCompletion",completion);
        menu.HandleShortcut(key);
        Assert.That(result.activeSelf,Is.False);
        if(key==Key.N&&!hasNext)
        {
            Assert.That(routes.activeSelf,Is.True);Assert.That(hud.activeSelf,Is.False);
            Assert.That(Time.timeScale,Is.Zero);Assert.That(level.CurrentLevelIndex,Is.Zero);
            Assert.That(level.Phase,Is.EqualTo(LevelManager.RunPhase.Finished));
        }
        else
        {
            Assert.That(Time.timeScale,Is.EqualTo(1f));Assert.That(hud.activeSelf,Is.True);
            Assert.That(level.Phase,Is.EqualTo(LevelManager.RunPhase.Countdown));
            Assert.That(level.CurrentLevelIndex,Is.EqualTo(key==Key.N?1:0));
        }
    }

    [Test]
    public void WelcomeRewardCannotBeClaimedTwice()
    {
        PlayerPrefs.DeleteKey("PolyCar_WelcomeReward");int balance=SaveManager.TotalCoins;
        SaveManager.ClaimWelcomeReward(12);SaveManager.ClaimWelcomeReward(12);
        Assert.That(SaveManager.TotalCoins,Is.EqualTo(balance+12));Assert.That(SaveManager.WelcomeRewardClaimed,Is.True);
    }
    [Test]
    public void GarageRejectsMidRaceSelection()
    {
        var garage=Make("Garage").AddComponent<CarGarage>();Set(garage,"levelManager",level);StartRace();
        Assert.That(garage.TrySelectOrUnlock(0,out string reason),Is.False);Assert.That(reason,Does.Contain("current run"));
    }
    [TestCase(80,1000,0,"GOLD")]
    [TestCase(95,1000,0,"SILVER")]
    [TestCase(80,1000,1,"SILVER")]
    [TestCase(80,100,0,"BRONZE")]
    public void MedalRequiresSpeedSkillAndCleanRecovery(int seconds,int score,int recovery,string expected)
    {
        Assert.That(RaceCourse.Medal(seconds,score,recovery,90,300,1000),Is.EqualTo(expected));
    }
    [Test]
    public void AirborneVehicleCannotAccelerateOrTurn()
    {
        car.gameObject.AddComponent<BoxCollider>();Call(car,"Awake");car.transform.position=Vector3.up*20;
        car.SetControlsEnabled(true);car.SetInput(1,1,0,false);Physics.SyncTransforms();Call(car,"FixedUpdate");
        scene.GetPhysicsScene().Simulate(0.02f);
        Assert.That(car.IsGrounded,Is.False);Assert.That(car.GetComponent<Rigidbody>().linearVelocity.magnitude,Is.LessThan(0.01f));
        Assert.That(car.GetComponent<Rigidbody>().angularVelocity.magnitude,Is.LessThan(0.01f));
    }
}
