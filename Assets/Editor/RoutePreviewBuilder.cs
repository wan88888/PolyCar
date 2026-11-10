using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Static route photographs rendered from the playable scenes; no runtime cameras.</summary>
public static class RoutePreviewBuilder
{
    private const string Folder = "Assets/UI/RoutePreviews";

    public static Sprite CardFrame()
    {
        Directory.CreateDirectory(Folder);
        string path = Folder + "/CardFrame.png";
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
        {
            var point = new Vector2(x + 0.5f, y + 0.5f);
            var nearest = new Vector2(Mathf.Clamp(point.x, 20, 44), Mathf.Clamp(point.y, 20, 44));
            texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(20 - Vector2.Distance(point, nearest))));
        }
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        ImportSprite(path, new Vector4(22, 22, 22, 22));
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    public static void BuildThumbnails()
    {
        Directory.CreateDirectory(Folder);
        var maps = GameObject.Find("LevelMaps").transform;
        var panel = GameObject.Find("Canvas").transform.Find("LevelSelectPanel");
        var active = new bool[maps.childCount];
        for (int i = 0; i < maps.childCount; i++) active[i] = maps.GetChild(i).gameObject.activeSelf;
        var camera = new GameObject("Temporary route photo camera").AddComponent<Camera>();
        camera.enabled = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.28f, 0.47f, 0.59f);
        camera.cullingMask = ~(1 << 5 | 1 << 31);
        camera.fieldOfView = 50;
        camera.farClipPlane = 1200;
        camera.allowHDR = false;
        var target = new RenderTexture(960, 540, 24) { antiAliasing = 4 };
        camera.targetTexture = target;
        var previous = RenderTexture.active;
        bool previousFog = RenderSettings.fog;
        RenderSettings.fog = false;
        try
        {
            for (int i = 0; i < maps.childCount; i++)
            {
                for (int j = 0; j < maps.childCount; j++) maps.GetChild(j).gameObject.SetActive(i == j);
                var look = maps.GetChild(i).GetComponent<SceneLook>();
                if(look!=null)look.Apply(camera);
                RenderSettings.fog=false;
                var course = maps.GetChild(i).GetComponent<RaceCourse>();
                var bounds = new Bounds(course.centerLine[0], Vector3.zero);
                foreach (var point in course.centerLine) bounds.Encapsulate(point);
                float extent = Mathf.Max(bounds.extents.x, bounds.extents.z) + 10;
                camera.transform.position = bounds.center + new Vector3(-1.05f, 1.25f, -1.05f) * extent;
                camera.transform.LookAt(bounds.center);
                var texture = new Texture2D(960, 540, TextureFormat.RGB24, false);
                try
                {
                    for (int frame = 0; frame < 3; frame++) camera.Render();
                    RenderTexture.active = target;
                    texture.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); texture.Apply();
                    string path = $"{Folder}/Route_{i + 1:00}.png";
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                    ImportSprite(path, Vector4.zero);
                    panel.Find($"LevelButton_{i + 1:00}/Preview").GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
                finally { Object.DestroyImmediate(texture); if(look!=null)look.Restore(); }
            }
        }
        finally
        {
            for (int i = 0; i < maps.childCount; i++) maps.GetChild(i).gameObject.SetActive(active[i]);
            RenderTexture.active = previous;
            RenderSettings.fog = previousFog;
            camera.targetTexture = null;
            target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(camera.gameObject);
        }
        Debug.Log("ROUTE_PREVIEWS_RENDERED actualCourses=" + maps.childCount);
    }

    private static void ImportSprite(string path, Vector4 border)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = border;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
}
