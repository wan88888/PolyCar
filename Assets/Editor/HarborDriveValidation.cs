using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Deterministic physics-only route following; not a substitute for human playtesting.</summary>
public static class HarborDriveValidation
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/DriftPrototype.unity");
        foreach(var coin in UnityEngine.Object.FindObjectsByType<CoinPickup>(FindObjectsSortMode.None)) coin.GetComponent<Collider>().isTrigger = true;
        var course=UnityEngine.Object.FindFirstObjectByType<RaceCourse>();
        var points=course.centerLine;
        var spawn=course.transform.Find("PlayerSpawnPoint");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vehicles/StarterCar.prefab");
        var car=UnityEngine.Object.Instantiate(prefab,spawn.position,spawn.rotation).GetComponent<CarController>();
        Call(car,"Awake");car.SetControlsEnabled(true);
        var body=car.GetComponent<Rigidbody>();
        Physics.simulationMode=SimulationMode.Script;Physics.SyncTransforms();
        int passed=0,nearest=0;float maxSpeed=0,slipSeconds=0;Vector3 previous=body.position;float elapsed=0;
        for(int step=0;step<7500;step++)
        {
            float best=float.MaxValue;int found=nearest;
            for(int j=0;j<9;j++)
            {
                int index=(nearest+j)%(points.Length-1);float distance=(points[index]-body.position).sqrMagnitude;
                if(distance<best){best=distance;found=index;}
            }
            nearest=found;
            Vector3 target=points[(nearest+5)%(points.Length-1)]-body.position;target.y=0;
            float angle=Vector3.SignedAngle(car.transform.forward,target,Vector3.up);
            float desired=Mathf.Abs(angle)>25f?10f:16f;
            car.SetInput(car.ForwardSpeed<desired?1f:0f,Mathf.Clamp(angle/20f,-1f,1f),car.ForwardSpeed>desired+2?0.3f:0f,false);
            Call(car,"FixedUpdate");Physics.Simulate(0.02f);
            // Explicitly sync Transform for an offline interpolated Rigidbody simulation.
            car.transform.SetPositionAndRotation(body.position,body.rotation);
            if(step%500==0)Debug.Log($"DRIVE_TRACE t={step*0.02f:F1} p={body.position} v={car.ForwardSpeed:F1} angle={angle:F1} near={nearest} grounded={car.IsGrounded} yaw={body.angularVelocity.y:F2}");
            elapsed+=0.02f;maxSpeed=Mathf.Max(maxSpeed,car.SpeedKmh);
            if(car.IsDrifting&&car.LateralSpeed>0.5f)slipSeconds+=0.02f;
            if(RaceCourse.CrossedGate(previous,body.position,course.gates[passed%course.gates.Length],course.gateHalfWidth))passed++;
            previous=body.position;
            if(passed==course.gates.Length*course.laps)break;
        }
        Debug.Log($"HARBOR_PHYSICS_RESULT gates={passed}/{course.gates.Length*course.laps} seconds={elapsed:F2} maxKmh={maxSpeed:F1} slipSeconds={slipSeconds:F2} position={body.position}");
        if(passed!=course.gates.Length*course.laps)throw new Exception("Automated physics route follower did not finish harbor course");
        Debug.Log("HARBOR_PHYSICS_OK");
    }
    private static void Call(object o,string method)=>o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
}
