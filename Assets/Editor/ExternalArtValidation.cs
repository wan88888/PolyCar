using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class ExternalArtValidation
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/DriftPrototype.unity");
        foreach(string name in new[]{"StarterCar","DriftCar","RallyCar","SpeedCar"})
        {
            var car=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vehicles/"+name+".prefab");
            var fx=car.GetComponent<CarPresentation>();
            if(fx==null||fx.wheels.Length!=4||fx.wheels.Any(w=>w==null))throw new Exception("Missing animated wheels: "+name);
            if(fx.rearContactPoints.Length!=2||fx.wheelSpinAxis!=Vector3.right)throw new Exception("Imported wheel axes/contact points not configured: "+name);
            if(car.GetComponentsInChildren<Collider>(true).Length!=1)throw new Exception("Art added unwanted vehicle colliders: "+name);
            if(car.GetComponentsInChildren<MeshFilter>(true).Count(m=>AssetDatabase.GetAssetPath(m.sharedMesh).StartsWith("Assets/ThirdParty/Kenney/"))<5)throw new Exception("Expected authored vehicle meshes: "+name);
            ValidateMaterials(car);
        }
        var maps=GameObject.Find("LevelMaps").transform;
        var camera=Camera.main;
        GameObject.Find("Canvas").SetActive(false);
        string output="/tmp/polycar-external-review";Directory.CreateDirectory(output);
        var rows=new List<string>{"route,imported_mesh_renderers,visible_triangles,materials,editor_render_ms"};
        var contactSheet=new Texture2D(1280,1800,TextureFormat.RGB24,false);
        for(int i=0;i<maps.childCount;i++)
        {
            for(int j=0;j<maps.childCount;j++)maps.GetChild(j).gameObject.SetActive(i==j);
            var level=maps.GetChild(i);var course=level.GetComponent<RaceCourse>();
            var filters=level.GetComponentsInChildren<MeshFilter>(true);
            int imported=filters.Count(m=>AssetDatabase.GetAssetPath(m.sharedMesh).StartsWith("Assets/ThirdParty/Kenney/"));
            if(imported<15)throw new Exception("Route has insufficient imported scenery: "+(i+1));
            ValidateMaterials(level.gameObject);
            long tris=filters.Where(m=>m.GetComponent<Renderer>()!=null&&m.GetComponent<Renderer>().enabled).Sum(m=>(long)m.sharedMesh.triangles.Length/3);
            if(tris>600000)throw new Exception("Unexpected route triangle budget: "+tris);
            var materialCount=level.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().Count();
            Bounds bounds=new Bounds(course.centerLine[0],Vector3.zero);foreach(var p in course.centerLine)bounds.Encapsulate(p);
            float extent=Mathf.Max(bounds.extents.x,bounds.extents.z)+15;
            camera.transform.position=bounds.center+new Vector3(-1f,1.1f,-1f)*extent;
            camera.transform.LookAt(bounds.center);
            camera.fieldOfView=56;
            var rt=new RenderTexture(640,360,24);rt.antiAliasing=4;camera.targetTexture=rt;
            for(int warm=0;warm<3;warm++)camera.Render();
            var clock=Stopwatch.StartNew();for(int frame=0;frame<8;frame++)camera.Render();clock.Stop();
            double milliseconds=clock.Elapsed.TotalMilliseconds/8;
            RenderTexture.active=rt;
            var image=new Texture2D(640,360,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,640,360),0,0);image.Apply();
            File.WriteAllBytes(output+"/route-"+(i+1).ToString("00")+".png",image.EncodeToPNG());
            contactSheet.SetPixels(i%2*640,(4-i/2)*360,640,360,image.GetPixels());
            rows.Add($"{i+1},{imported},{tris},{materialCount},{milliseconds:F2}");
            Debug.Log($"ROUTE_ART_CHECK route={i+1} imported={imported} triangles={tris} materials={materialCount} editorRenderMs={milliseconds:F2}");
            camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
        }
        contactSheet.Apply();File.WriteAllBytes(output+"/all-routes.png",contactSheet.EncodeToPNG());UnityEngine.Object.DestroyImmediate(contactSheet);
        File.WriteAllLines(output+"/asset-budget.csv",rows);
        Debug.Log("ALL_EXTERNAL_ART_VALIDATED routes=10 cars=4");
    }

    private static void ValidateMaterials(GameObject obj)
    {
        foreach(var renderer in obj.GetComponentsInChildren<Renderer>(true))
        {
            if(!renderer.enabled||renderer.GetComponent<TextMesh>()!=null)continue;
            foreach(var mat in renderer.sharedMaterials)
            {
                if(mat==null||mat.shader==null||mat.shader.name.Contains("InternalError"))throw new Exception("Broken material on "+obj.name+"/"+renderer.name);
                if(AssetDatabase.GetAssetPath(mat).Contains("/Imported/")&&mat.name.Contains("colormap")&&mat.GetTexture("_BaseMap")==null)
                    throw new Exception("Missing palette texture: "+mat.name);
            }
        }
    }
}
