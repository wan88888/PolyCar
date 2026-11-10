using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class EnvironmentLightingTests
{
    [Test]
    public void NightToDayExitRestoresSunAndAmbientAndBothCaptureCameras()
    {
        Light sun=null;foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if(light.type==LightType.Directional&&light.gameObject.layer!=31){sun=light;break;}
        GameObject createdSun=null;
        if(sun==null){createdSun=new GameObject("Lighting test sun",typeof(Light));sun=createdSun.GetComponent<Light>();sun.type=LightType.Directional;}
        var originalIntensity=sun.intensity;var originalRotation=sun.transform.rotation;var originalColor=sun.color;
        var originalAmbient=RenderSettings.ambientLight;var originalMode=RenderSettings.ambientMode;
        var go=new GameObject("Night lighting test");go.SetActive(false);var look=go.AddComponent<SceneLook>();
        var cameraA=new GameObject("Capture A",typeof(Camera));var cameraB=new GameObject("Capture B",typeof(Camera));
        var a=cameraA.GetComponent<Camera>();var b=cameraB.GetComponent<Camera>();a.clearFlags=CameraClearFlags.SolidColor;b.clearFlags=CameraClearFlags.Depth;
        try
        {
            look.sunIntensity=.2f;look.sunColor=Color.blue;look.overrideSunDirection=true;look.sunEuler=new Vector3(15,90,0);
            look.ambientColor=new Color(.1f,.12f,.2f);look.Apply(a);
            Assert.AreEqual(.2f,sun.intensity);Assert.AreEqual(Color.blue,sun.color);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(look.sunEuler),sun.transform.rotation),.01f);
            Assert.AreEqual(AmbientMode.Flat,RenderSettings.ambientMode);
            look.Apply(b);
            Assert.AreEqual(CameraClearFlags.SolidColor,a.clearFlags,"Switching capture cameras restores the earlier camera.");
            Assert.AreEqual(CameraClearFlags.Skybox,b.clearFlags);
            look.Restore();look.Restore();
            Assert.AreEqual(originalIntensity,sun.intensity);Assert.AreEqual(originalColor,sun.color);
            Assert.Less(Quaternion.Angle(originalRotation,sun.transform.rotation),.01f);
            Assert.AreEqual(originalMode,RenderSettings.ambientMode);Assert.AreEqual(originalAmbient,RenderSettings.ambientLight);
            Assert.AreEqual(CameraClearFlags.Depth,b.clearFlags);
        }
        finally
        {
            look.Restore();Object.DestroyImmediate(go);Object.DestroyImmediate(cameraA);Object.DestroyImmediate(cameraB);
            if(createdSun!=null)Object.DestroyImmediate(createdSun);
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    public void LoadingRoutesInEitherDirectionKeepsTheIncomingAtmosphere(int first)
    {
        const string selectedKey="PolyCar_SelectedRoute";
        bool hadSelected=PlayerPrefs.HasKey(selectedKey);int selected=PlayerPrefs.GetInt(selectedKey);
        Light sun=null;
        foreach(var candidate in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if(candidate.type==LightType.Directional && candidate.gameObject.layer!=31){sun=candidate;break;}
        GameObject createdSun=null;
        if(sun==null){createdSun=new GameObject("Route transition sun",typeof(Light));sun=createdSun.GetComponent<Light>();sun.type=LightType.Directional;}
        var managerObject=new GameObject("Route transition manager");managerObject.SetActive(false);
        var manager=managerObject.AddComponent<LevelManager>();
        var scoring=managerObject.AddComponent<GameManager>();
        var roots=new[]{new GameObject("Day route"),new GameObject("Night route")};
        var looks=new SceneLook[2];
        var skies=new Material[2];
        try
        {
            for(int i=0;i<2;i++)
            {
                roots[i].SetActive(false);looks[i]=roots[i].AddComponent<SceneLook>();
                skies[i]=new Material(Shader.Find("PolyCar/ShowcaseSky"));looks[i].sky=skies[i];
                looks[i].sunIntensity=i==0?1.7f:.18f;
                looks[i].sunColor=i==0?new Color(1,.9f,.7f):new Color(.3f,.4f,.8f);
                looks[i].ambientColor=i==0?new Color(.5f,.6f,.7f):new Color(.08f,.1f,.18f);
                looks[i].fogColor=i==0?new Color(.6f,.7f,.8f):new Color(.03f,.04f,.09f);
                looks[i].overrideSunDirection=true;looks[i].sunEuler=i==0?new Vector3(45,-30,0):new Vector3(20,120,0);
            }
            var fields=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(LevelManager).GetField("levels",fields).SetValue(manager,new[]{
                new LevelManager.LevelDefinition{displayName="Day",root=roots[0].transform},
                new LevelManager.LevelDefinition{displayName="Night",root=roots[1].transform}});
            typeof(LevelManager).GetField("gameManager",fields).SetValue(manager,scoring);
            typeof(LevelManager).GetField("carGarage",fields).SetValue(manager,null);
            var load=typeof(LevelManager).GetMethod("LoadLevel",fields);
            // Exercise actual LoadLevel, including the descending-index switch that used to clobber daytime lighting.
            foreach(int target in new[]{first,1-first,first,1-first,1-first})
            {
                load.Invoke(manager,new object[]{target,false});
                Assert.IsTrue(roots[target].activeSelf);Assert.IsFalse(roots[1-target].activeSelf);
                Assert.AreSame(skies[target],RenderSettings.skybox);
                Assert.AreEqual(looks[target].ambientColor,RenderSettings.ambientLight);
                Assert.AreEqual(AmbientMode.Flat,RenderSettings.ambientMode);
                Assert.AreEqual(looks[target].fogColor,RenderSettings.fogColor);
                Assert.AreEqual(looks[target].sunColor,sun.color);
                Assert.AreEqual(looks[target].sunIntensity,sun.intensity);
                Assert.Less(Quaternion.Angle(Quaternion.Euler(looks[target].sunEuler),sun.transform.rotation),.01f);
                Assert.AreEqual(target,manager.CurrentLevelIndex);
            }
        }
        finally
        {
            foreach(var look in looks)if(look!=null)look.Restore();
            foreach(var root in roots)Object.DestroyImmediate(root);
            foreach(var sky in skies)if(sky!=null)Object.DestroyImmediate(sky);
            Object.DestroyImmediate(managerObject);
            if(createdSun!=null)Object.DestroyImmediate(createdSun);
            if(hadSelected)PlayerPrefs.SetInt(selectedKey,selected);else PlayerPrefs.DeleteKey(selectedKey);
            PlayerPrefs.Save();
        }
    }
}
