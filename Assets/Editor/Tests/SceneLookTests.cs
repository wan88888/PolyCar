using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SceneLookTests
{
    [Test]
    public void LevelAtmosphereRestoresSharedRenderStateAndCamera()
    {
        var originalSky=RenderSettings.skybox;var originalFog=RenderSettings.fogColor;
        var originalAmbient=RenderSettings.ambientLight;float originalStart=RenderSettings.fogStartDistance,originalEnd=RenderSettings.fogEndDistance;
        var go=new GameObject("Atmosphere test");go.SetActive(false);
        var cameraObject=new GameObject("Atmosphere camera");var camera=cameraObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;
        var look=go.AddComponent<SceneLook>();
        try
        {
            look.sky=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Showcase_Sky_Harbor.mat");
            Assert.NotNull(look.sky);
            look.Apply(camera);look.Apply(camera);
            Assert.AreEqual(look.sky,RenderSettings.skybox);
            Assert.AreEqual(CameraClearFlags.Skybox,camera.clearFlags);
            look.Restore();look.Restore();
            Assert.AreEqual(originalSky,RenderSettings.skybox);Assert.AreEqual(originalFog,RenderSettings.fogColor);
            Assert.AreEqual(originalAmbient,RenderSettings.ambientLight);
            Assert.AreEqual(originalStart,RenderSettings.fogStartDistance);Assert.AreEqual(originalEnd,RenderSettings.fogEndDistance);
            Assert.AreEqual(CameraClearFlags.SolidColor,camera.clearFlags);
        }
        finally{look.Restore();Object.DestroyImmediate(go);Object.DestroyImmediate(cameraObject);}
    }

    [Test]
    public void TwoShowcaseLevelsHaveIndependentAtmospheres()
    {
        var a=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Showcase_Sky_Harbor.mat");
        var b=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Showcase_Sky_Works.mat");
        Assert.NotNull(a);Assert.NotNull(b);Assert.AreNotEqual(a.GetColor("_Horizon"),b.GetColor("_Horizon"));
        Assert.IsTrue(a.shader.isSupported);Assert.IsTrue(b.shader.isSupported);
    }
}
