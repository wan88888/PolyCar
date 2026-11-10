using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class EnvironmentDeliveryBuilder
{
    public static void RebuildAndReview()
    {
        ExternalArtBuilder.BuildAll();
        EnvironmentValidation.Run();
        DifficultyValidation.Run();
    }

    public static void ReviewAndBuildPlayer()
    {
        EnvironmentValidation.Run();
        BuildPlayer();
    }

    public static void FinalizeWorldSigns()
    {
        var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/DriftPrototype.unity");
        foreach(var guid in AssetDatabase.FindAssets("Environment_NeonFont_ t:Material"))
        {
            var material=AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(AssetDatabase.GUIDToAssetPath(guid));
            material.shader=UnityEngine.Shader.Find("PolyCar/WorldSignText");
            EditorUtility.SetDirty(material);
        }
        foreach(var root in scene.GetRootGameObjects())
            foreach(var t in root.GetComponentsInChildren<UnityEngine.Transform>(true))
                if(t.name=="Neon roofline")UnityEngine.Object.DestroyImmediate(t.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        EnvironmentValidation.Run();
        RefreshVerifiedPreviews();
        BuildPlayer();
    }

    public static void RefreshVerifiedPreviews()
    {
        var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/DriftPrototype.unity");
        RoutePreviewBuilder.BuildThumbnails();
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    public static void BuildPlayer()
    {
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{"Assets/Scenes/DriftPrototype.unity"},
            locationPathName="Builds/PolyCar-Environments.app",
            target=BuildTarget.StandaloneOSX,
            options=BuildOptions.Development
        });
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Environment player build failed: "+report.summary.result);
        UnityEngine.Debug.Log("ENVIRONMENT_PLAYER_BUILD_OK "+report.summary.totalSize);
    }
}
