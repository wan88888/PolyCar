using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shop art is rendered from the actual playable prefabs, never a recolored placeholder.</summary>
public static class ShopPreviewBuilder
{
    public static void BuildThumbnails()
    {
        const string folder = "Assets/UI/VehiclePreviews";
        Directory.CreateDirectory(folder);
        string[] names = { "StarterCar", "DriftCar", "RallyCar", "SpeedCar" };
        var panel = GameObject.Find("Canvas").transform.Find("ShopPanel");
        var studio = new GameObject("Temporary shop photo studio");
        studio.transform.position = new Vector3(0,-2000,0);
        var camera = new GameObject("Shop photo camera").AddComponent<Camera>();
        camera.transform.SetParent(studio.transform,false);
        camera.transform.localPosition = new Vector3(5.3f,3.1f,6.5f);
        camera.transform.LookAt(studio.transform.position + Vector3.up*0.5f);
        camera.orthographic = true; camera.orthographicSize = 1.48f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0,0,0,0);
        camera.cullingMask = 1 << 31; camera.farClipPlane = 25;
        camera.allowHDR = false; camera.enabled = false;
        var light = new GameObject("Shop photo key").AddComponent<Light>();
        light.transform.SetParent(studio.transform,false);
        light.type = LightType.Directional; light.intensity = 1.5f;
        light.color = new Color(1,0.94f,0.85f);
        light.transform.localRotation = Quaternion.Euler(40,-35,0); light.cullingMask = 1 << 31;
        var rt = new RenderTexture(780,468,24) { antiAliasing = 4 };
        camera.targetTexture = rt;
        var previous = RenderTexture.active;
        try
        {
            for(int i=0;i<names.Length;i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Vehicles/{names[i]}.prefab");
                var model = Object.Instantiate(prefab.transform.Find("LowPolyVisuals").gameObject,studio.transform);
                foreach(var child in model.GetComponentsInChildren<Transform>(true))child.gameObject.layer=31;
                var texture = new Texture2D(780,468,TextureFormat.RGBA32,false);
                try
                {
                    for(int frame=0;frame<3;frame++)camera.Render();
                    RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,780,468),0,0);texture.Apply();
                    string path=$"{folder}/{names[i]}.png";
                    File.WriteAllBytes(path,texture.EncodeToPNG());
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                    importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
                    importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
                    panel.Find($"ShopCarButton_{i+1:00}/ShopCarImage_{i+1:00}").GetComponent<Image>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
                finally { Object.DestroyImmediate(texture);Object.DestroyImmediate(model); }
            }
        }
        finally
        {
            RenderTexture.active=previous;camera.targetTexture=null;rt.Release();
            Object.DestroyImmediate(rt);Object.DestroyImmediate(studio);
        }
        Debug.Log("SHOP_PREVIEWS_RENDERED actualVehiclePrefabs=4");
    }
}
