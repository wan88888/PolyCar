using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Same starter car and follower for every route. No teleport, recovery or save edits.</summary>
public static class DifficultyValidation
{
    private static void Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
    public static void Run()=>Validate(false);
    public static void DriftRun()=>Validate(true);
    private static void Validate(bool drift)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/DriftPrototype.unity");
        var maps=GameObject.Find("LevelMaps").transform;
        GameObject.Find("Canvas").SetActive(false);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vehicles/StarterCar.prefab");
        var previousMode=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
        var rows=new List<string>{"route,width,obstacles,length,gates,passed,seconds,gold_seconds,drift_score,max_deviation,grounded_ratio"};
        int failed=0;
        try
        {
            for(int route=0;route<(drift?2:10);route++)
            {
                for(int i=0;i<maps.childCount;i++)maps.GetChild(i).gameObject.SetActive(i==route);
                var level=maps.GetChild(route);var course=level.GetComponent<RaceCourse>();var points=course.centerLine;
                foreach(var coin in level.GetComponentsInChildren<CoinPickup>())coin.GetComponent<Collider>().isTrigger=true;
                var cumulative=new float[points.Length];for(int i=1;i<points.Length;i++)cumulative[i]=cumulative[i-1]+Vector3.Distance(points[i-1],points[i]);
                float length=cumulative[cumulative.Length-1];bool closed=Vector3.Distance(points[0],points[points.Length-1])<0.1f;
                var spawn=level.Find("PlayerSpawnPoint");
                var car=Object.Instantiate(prefab,spawn.position,spawn.rotation).GetComponent<CarController>();
                Call(car,"Awake");car.SetControlsEnabled(true);var body=car.GetComponent<Rigidbody>();
                var scoreObject=new GameObject("Validation score");var score=scoreObject.AddComponent<GameManager>();
                typeof(GameManager).GetField("playerCar",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(score,car);
                Physics.SyncTransforms();
                int nearest=0,passed=0,grounded=0,steps=0;float seconds=0,maxDeviation=0;
                var previous=body.position;
                Vector3 Sample(float d){if(closed)d=Mathf.Repeat(d,length);else if(d>length)return points[points.Length-1]+(points[points.Length-1]-points[points.Length-2]).normalized*(d-length);HarborArtBuilder.Sample(points,d,out var p,out var f);return p;}
                for(int step=0;step<12000;step++)
                {
                    float best=float.MaxValue;int found=nearest;
                    for(int j=-3;j<24;j++)
                    {
                        int index=closed?(nearest+j+points.Length-1)%(points.Length-1):Mathf.Clamp(nearest+j,0,points.Length-1);
                        float distance=(points[index]-body.position).sqrMagnitude;
                        if(distance<best){best=distance;found=index;}
                    }
                    nearest=found;float progress=cumulative[nearest];
                    var ahead=Sample(progress+6)-Sample(progress);var later=Sample(progress+16)-Sample(progress+6);
                    float turn=Mathf.Abs(Vector3.SignedAngle(ahead,later,Vector3.up));
                    float targetSpeed=Mathf.Lerp(15,5,Mathf.InverseLerp(2,35,turn));
                    float lookAhead=Mathf.Lerp(4,7,Mathf.InverseLerp(4,16,car.ForwardSpeed));
                    var aim=Sample(progress+lookAhead)-body.position;aim.y=0;
                    float angle=Vector3.SignedAngle(car.transform.forward,aim,Vector3.up);
                    targetSpeed=Mathf.Min(targetSpeed,Mathf.Lerp(16,5,Mathf.InverseLerp(12,65,Mathf.Abs(angle))));
                    bool slide=drift&&car.ForwardSpeed>7&&turn>12&&turn<40&&(step%70)<12;
                    float desiredYaw=2*Mathf.Max(car.ForwardSpeed,2)*Mathf.Sin(angle*Mathf.Deg2Rad)/lookAhead;
                    float steer=Mathf.Clamp(desiredYaw*1.35f+(desiredYaw-body.angularVelocity.y)*0.65f,-1,1);
                    car.SetInput(car.ForwardSpeed<targetSpeed?1:0,steer,car.ForwardSpeed>targetSpeed+1.5f?0.25f:0,slide);
                    Call(car,"FixedUpdate");Physics.Simulate(0.02f);car.transform.SetPositionAndRotation(body.position,body.rotation);
                    Call(score,"UpdateDriftScoring",0.02f);
                    if(car.IsGrounded)grounded++;steps++;seconds+=0.02f;maxDeviation=Mathf.Max(maxDeviation,Mathf.Sqrt(best));
                    if(passed<course.gates.Length&&RaceCourse.CrossedGate(previous,body.position,course.gates[passed],course.gateHalfWidth))passed++;
                    previous=body.position;
                    if(passed==course.gates.Length)break;
                    if(step%2500==2499)Debug.Log($"BALANCE_TRACE route={route+1} time={seconds:F1} gates={passed} near={nearest} position={body.position}");
                }
                score.FlushActiveDrift();
                string row=string.Join(",",route+1,CourseDifficultyBuilder.Widths[route],level.Find("Obstacles").childCount,length.ToString("F1",CultureInfo.InvariantCulture),course.gates.Length,passed,seconds.ToString("F2",CultureInfo.InvariantCulture),course.goldTime,score.TotalDriftScore,maxDeviation.ToString("F2",CultureInfo.InvariantCulture),(grounded/(float)steps).ToString("F3",CultureInfo.InvariantCulture));
                rows.Add(row);Debug.Log("BALANCE_ROUTE "+row);
                if(passed!=course.gates.Length)failed++;
                Object.DestroyImmediate(car.gameObject);Object.DestroyImmediate(scoreObject);
            }
        }
        finally{Physics.simulationMode=previousMode;File.WriteAllLines(drift?"/tmp/polycar-balance-drift.csv":"/tmp/polycar-balance-driving.csv",rows);}
        if(failed>0)throw new Exception("Route follower failed on "+failed+" courses; inspect balance report");
        Debug.Log("BALANCE_ROUTES_OK mode="+(drift?"drift":"normal"));
    }
}
