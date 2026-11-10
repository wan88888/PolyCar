using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class BugFixRegressionTests
{
    static object Call(object o,string method,params object[] args) => o.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance).Invoke(o,args);

    [TestCase(false)]
    [TestCase(true)]
    public void InterruptedObstacleFlashRestoresScaleColorAndCooldown(bool disable)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor",new Color(.2f,.4f,.6f));
        go.GetComponent<Renderer>().sharedMaterial=material;
        go.transform.localScale=new Vector3(2,3,4);
        var feedback=go.AddComponent<ObstacleFeedback>();
        try
        {
            Call(feedback,"Awake");
            var flash=(IEnumerator)Call(feedback,"FlashHit");flash.MoveNext();
            typeof(ObstacleFeedback).GetField("nextHitTime",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(feedback,999f);
            Assert.That(go.transform.localScale,Is.Not.EqualTo(new Vector3(2,3,4)));
            if(disable)Call(feedback,"OnDisable");else feedback.ResetFeedback();
            Assert.That(go.transform.localScale,Is.EqualTo(new Vector3(2,3,4)));
            var block=new MaterialPropertyBlock();go.GetComponent<Renderer>().GetPropertyBlock(block);
            Assert.That(Vector4.Distance(block.GetColor("_BaseColor"),material.GetColor("_BaseColor")),Is.LessThan(.00001f));
            Assert.That(typeof(ObstacleFeedback).GetField("nextHitTime",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(feedback),Is.EqualTo(0f));
        }
        finally{Object.DestroyImmediate(go);Object.DestroyImmediate(material);}
    }

    [TestCase(false,false)]
    [TestCase(false,true)]
    [TestCase(true,false)]
    [TestCase(true,true)]
    public void ReviewPreservesSelectedRouteIncludingFailureAndAbsentKey(bool exists,bool fail)
    {
        const string key="PolyCar_SelectedRoute";
        bool originalExists=PlayerPrefs.HasKey(key);int original=PlayerPrefs.GetInt(key);float originalScale=Time.timeScale;
        try
        {
            if(exists)PlayerPrefs.SetInt(key,7);else PlayerPrefs.DeleteKey(key);
            Action review=()=>InterfaceFeedbackReview.WithPreservedState(()=>
            {
                SaveManager.SetSelectedRoute(0);Time.timeScale=.5f;
                if(fail)throw new InvalidOperationException("Simulated review failure");
            });
            if(fail)Assert.Throws<InvalidOperationException>(()=>review());else review();
            Assert.That(PlayerPrefs.HasKey(key),Is.EqualTo(exists));
            if(exists)Assert.That(PlayerPrefs.GetInt(key),Is.EqualTo(7));
            Assert.That(Time.timeScale,Is.EqualTo(originalScale));
        }
        finally
        {
            if(originalExists)PlayerPrefs.SetInt(key,original);else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();Time.timeScale=originalScale;
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AtmosphereNeverSelectsPreviewOnlyDirectionalLight(bool invalidPreferredSun)
    {
        var oldSun=RenderSettings.sun;
        var existing=Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        var states=new bool[existing.Length];
        for(int i=0;i<existing.Length;i++){states[i]=existing[i].enabled;existing[i].enabled=false;}
        var studio=new GameObject("Preview-only light").AddComponent<Light>();studio.type=LightType.Directional;studio.cullingMask=1<<31;studio.intensity=2;
        var sun=new GameObject("World light").AddComponent<Light>();sun.type=LightType.Directional;sun.cullingMask=~(1<<31);sun.intensity=1;
        var root=new GameObject("Atmosphere");root.SetActive(false);var look=root.AddComponent<SceneLook>();
        try
        {
            RenderSettings.sun=invalidPreferredSun?studio:sun;
            look.sunIntensity=.3f;look.Apply();
            Assert.That(sun.intensity,Is.EqualTo(.3f));Assert.That(studio.intensity,Is.EqualTo(2));
            look.Restore();Assert.That(sun.intensity,Is.EqualTo(1));Assert.That(studio.intensity,Is.EqualTo(2));
        }
        finally
        {
            look.Restore();Object.DestroyImmediate(root);Object.DestroyImmediate(sun.gameObject);Object.DestroyImmediate(studio.gameObject);
            RenderSettings.sun=oldSun;
            for(int i=0;i<existing.Length;i++)if(existing[i]!=null)existing[i].enabled=states[i];
        }
    }
}
