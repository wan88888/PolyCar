using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class BugFixRuntimeValidation
{
    static int stage;
    static double deadline;
    static CameraFollow follow;
    static ObstacleFeedback obstacle;
    static Vector3 obstacleScale;
    const string Output="Review/BugAudit/fixed-runtime.txt";
    static BugFixRuntimeValidation(){EditorApplication.update+=Tick;}
    static object Get(object o,string n)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance).Invoke(o,a);
    static void Log(string s){File.AppendAllText(Output,s+"\n");Debug.Log("BUG_AUDIT "+s);}
    public static void Run()
    {
        Directory.CreateDirectory("Review/BugAudit");File.WriteAllText(Output,"");
        EditorSceneManager.OpenScene("Assets/Scenes/DriftPrototype.unity");
        SessionState.SetBool("PolyCarBugFixFailed",false);SessionState.SetBool("PolyCarBugFix",true);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!SessionState.GetBool("PolyCarBugFix",false)||!EditorApplication.isPlaying)return;
        try
        {
            if(stage==0){deadline=EditorApplication.timeSinceStartup+3;stage++;return;}
            if(EditorApplication.timeSinceStartup<deadline)return;
            if(stage==1)
            {
                var level=Object.FindFirstObjectByType<LevelManager>();
                int selected=level.CurrentLevelIndex;
                for(int i=0;i<10;i++)
                {
                    Call(level,"LoadLevel",i,true);
                    var look=level.GetCourse(i).GetComponent<SceneLook>();
                    var sun=(Light)Get(look,"sunlight");
                    if(sun.gameObject.layer==31 || (sun.cullingMask & ~(1<<31))==0)throw new Exception("Preview light selected");
                    Log($"route={i+1} sunlight={sun.name} layer={sun.gameObject.layer} cullingMask={sun.cullingMask} intensity={sun.intensity}");
                }
                Call(level,"LoadLevel",selected,true);
                var car=Object.FindFirstObjectByType<CarGarage>();
                var player=((GameObject)Get(car,"currentCar")).GetComponent<CarController>();
                follow=Camera.main.GetComponent<CameraFollow>();follow.SetTarget(player.transform);follow.SetMenuMode(false);
                follow.transform.position=player.transform.position-player.transform.forward*(float)Get(follow,"followDistance")+Vector3.up*(float)Get(follow,"followHeight");
                Time.timeScale=0;
                obstacle=Object.FindFirstObjectByType<ObstacleFeedback>();
                if(obstacle==null){Call(level,"LoadLevel",1,true);obstacle=Object.FindFirstObjectByType<ObstacleFeedback>();Call(level,"LoadLevel",selected,true);}
                // Use an isolated primitive so the hit lifecycle does not alter race progress.
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Audit obstacle";obstacle=go.AddComponent<ObstacleFeedback>();
                obstacleScale=go.transform.localScale;
                Call(obstacle,"HandleHit",player.GetComponent<Collider>());
                Log("obstacle after hit scale="+go.transform.localScale);
                go.SetActive(false);
                stage++;deadline=EditorApplication.timeSinceStartup+.3;
            }
            else if(stage==2)
            {
                var velocity=(Vector3)Get(follow,"followVelocity");Log("paused camera smoothing velocity="+velocity+" finite="+(!float.IsNaN(velocity.x)));
                follow.SetMenuMode(true);
                obstacle.gameObject.SetActive(true);Time.timeScale=1;
                stage++;deadline=EditorApplication.timeSinceStartup+.3;
            }
            else
            {
                Log("obstacle after reenable scale="+obstacle.transform.localScale+" expected="+obstacleScale);
                if(obstacle.transform.localScale!=obstacleScale)throw new Exception("Obstacle remains scaled");
                Log("RUNTIME_FIXES_OK obstacleRestored=true routes=10");
                Finish();
            }
        }
        catch(Exception e){Log(e.ToString());SessionState.SetBool("PolyCarBugFixFailed",true);Finish();}
    }
    static void Finish()
    {
        SessionState.SetBool("PolyCarBugFix",false);Time.timeScale=1;EditorApplication.isPlaying=false;
        EditorApplication.delayCall+=()=>EditorApplication.Exit(SessionState.GetBool("PolyCarBugFixFailed",false)?1:0);
    }
}
