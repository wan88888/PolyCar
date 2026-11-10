using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Adapts licensed source models to the existing game without changing its chassis or course geometry.</summary>
public static class ExternalArtBuilder
{
    public static bool SampleOnly;
    public static bool Available => File.Exists("Assets/ThirdParty/Kenney/Sources.json");
    private const string Root = "Assets/ThirdParty/Kenney/";
    private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

    [MenuItem("Tools/PolyCar/Build External Art Sample")]
    public static void BuildSample()
    {
        SampleOnly = true;
        try { LowPolyDriftPrototypeBuilder.Build(); }
        finally { SampleOnly = false; }
    }

    [MenuItem("Tools/PolyCar/Build All Routes With External Art")]
    public static void BuildAll() { SampleOnly = false; LowPolyDriftPrototypeBuilder.Build(); }

    public static void ApplyToLoadedScene()
    {
        Materials.Clear();
        Directory.CreateDirectory("Assets/Materials/Imported"); AssetDatabase.Refresh();
        string[] names = {"StarterCar","DriftCar","RallyCar","SpeedCar"};
        string[] models = {"sedan-sports","hatchback-sports","suv","race"};
        for(int i=0;i<(SampleOnly?1:4);i++) ReplaceCar(names[i],models[i]);
        var maps = GameObject.Find("LevelMaps").transform;
        for(int i=0;i<(SampleOnly?1:maps.childCount);i++) DecorateCourse(maps.GetChild(i),i);
        AssetDatabase.SaveAssets();
        Debug.Log($"EXTERNAL_ART_APPLIED cars={(SampleOnly?1:4)} routes={(SampleOnly?1:maps.childCount)}");
    }

    public static GameObject Place(string pack,string asset,Transform parent,Vector3 position,Quaternion rotation,float size,bool height=false)
    {
        string path=Root+pack+"/Models/"+asset+".fbx";
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(source==null)throw new InvalidOperationException("Missing licensed model: "+path);
        var wrapper=new GameObject("Kenney_"+pack+"_"+asset); wrapper.transform.SetParent(parent,false);
        var model=UnityEngine.Object.Instantiate(source,wrapper.transform);
        model.name=asset;
        // Source files have different pivots and unit conventions. Normalize in a wrapper, never edit the source mesh.
        Bounds bounds=BoundsOf(model);
        float dimension=height?bounds.size.y:Mathf.Max(bounds.size.x,bounds.size.z);
        float scale=size/Mathf.Max(dimension,0.001f);
        model.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        wrapper.transform.localScale=Vector3.one*scale;
        wrapper.transform.SetPositionAndRotation(position,rotation);
        foreach(var collider in model.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(collider);
        foreach(var renderer in model.GetComponentsInChildren<Renderer>())
        {
            var mats=renderer.sharedMaterials;
            for(int i=0;i<mats.Length;i++)mats[i]=ConvertMaterial(pack,mats[i]);
            renderer.sharedMaterials=mats;
        }
        return wrapper;
    }

    public static Bounds BoundsOf(GameObject obj)
    {
        var renderers=obj.GetComponentsInChildren<Renderer>(true);
        if(renderers.Length==0)throw new InvalidOperationException("Model has no renderers: "+obj.name);
        Bounds bounds=renderers[0].bounds;
        foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static Material ConvertMaterial(string pack,Material source)
    {
        string key=pack+"_"+source.name.Replace(" ","_");
        if(Materials.TryGetValue(key,out Material cached))return cached;
        string path="Assets/Materials/Imported/"+key+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        Color color=source.HasProperty("_Color")?source.color:Color.white;
        if (pack == "Nature")
        {
            string part=source.name.ToLowerInvariant();
            if(part.Contains("leaf")) color=part.Contains("dark")?new Color(0.12f,0.31f,0.22f):new Color(0.24f,0.46f,0.30f);
            else if(part.Contains("wood")) color=new Color(0.38f,0.25f,0.16f);
            else if(part.Contains("grass")) color=new Color(0.31f,0.42f,0.26f);
            else if(part.Contains("dirt")) color=new Color(0.48f,0.38f,0.28f);
        }
        Texture texture=source.mainTexture;
        // Keep each kit's atlas explicit; both vehicle and building packs call their texture 'colormap'.
        if(pack=="Cars"||pack=="Industrial")texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+pack+"/Textures/colormap.png");
        else if(texture!=null)texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+pack+"/Textures/"+texture.name+".png");
        mat.SetColor("_BaseColor",color); mat.SetTexture("_BaseMap",texture);
        mat.SetFloat("_Smoothness",pack=="Cars"?0.45f:source.name.Contains("specular")?0.5f:0.16f);
        mat.SetFloat("_Metallic",0f);
        EditorUtility.SetDirty(mat);Materials[key]=mat;return mat;
    }

    private static void ReplaceCar(string name,string modelName)
    {
        string path="Assets/Prefabs/Vehicles/"+name+".prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var visual=root.transform.Find("LowPolyVisuals");
            foreach(Transform child in visual.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var model=Place("Cars",modelName,visual,root.transform.position+Vector3.down*0.2f,Quaternion.identity,3.5f);
            // Restore local origin after fitting inside the isolated prefab editing scene.
            model.transform.localPosition=Vector3.down*0.2f;
            var fx=root.GetComponent<CarPresentation>();
            var pivots=new List<Transform>();
            foreach(string wheelName in new[]{"wheel-front-left","wheel-front-right","wheel-back-left","wheel-back-right"})
            {
                var wheel=model.GetComponentsInChildren<Transform>().First(t=>t.name==wheelName);
                Bounds bounds=BoundsOf(wheel.gameObject);
                var pivot=new GameObject(wheelName+"_Pivot").transform;pivot.SetParent(visual,false);pivot.position=bounds.center;
                wheel.SetParent(pivot,true);pivots.Add(pivot);
            }
            fx.visualRoot=visual;fx.wheels=pivots.ToArray();fx.wheelSpinAxis=Vector3.right;
            fx.rearContactPoints=new[]{root.transform.InverseTransformPoint(pivots[2].position),root.transform.InverseTransformPoint(pivots[3].position)};
            for(int i=0;i<2;i++)fx.rearContactPoints[i].y=-0.18f;
            var lamps=new List<Renderer>();
            foreach(int side in new[]{-1,1})
            {
                var lamp=HarborArtBuilder.Box("Brake light",visual,new Vector3(side*0.6f,0.43f,-1.76f),new Vector3(0.24f,0.075f,0.025f),HarborArtBuilder.Material("Car_Taillamp",new Color(0.65f,0.025f,0.015f),0.5f));
                lamps.Add(lamp.GetComponent<Renderer>());
            }
            fx.brakeLights=lamps.ToArray();
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void DecorateCourse(Transform level,int index)
    {
        var course=level.GetComponent<RaceCourse>();
        var root=new GameObject("Imported scenery").transform;root.SetParent(level,false);
        if(index==0) ShowcaseSceneryBuilder.Harbor(level, root);
        else if(index==1) ShowcaseSceneryBuilder.Industrial(level, root);
        else ThemedSceneryBuilder.Build(level,root,index);
        ReplaceObstacleVisuals(level);

        for(int gateIndex=0;gateIndex<course.gates.Length;gateIndex++)
        {
            var gate=course.gates[gateIndex];
            // Checkpoints use their existing roadside beacons. Only the lap line needs an arch.
            if(gateIndex!=course.gates.Length-1)continue;
            var arch = new GameObject("Slim finish gantry").transform;
            arch.SetParent(root,false); arch.SetPositionAndRotation(gate.position,gate.rotation);
            float halfSpan = course.gateHalfWidth + 2.2f;
            var steel = HarborArtBuilder.Material("Harbor_Steel",new Color(0.055f,0.10f,0.14f),0.3f,0.5f);
            var ivory = HarborArtBuilder.Material("Harbor_Ivory",new Color(0.9f,0.87f,0.72f));
            foreach(int side in new[]{-1,1})
                HarborArtBuilder.Box("Slender support",arch,new Vector3(side*halfSpan,3f,0),new Vector3(0.22f,6f,0.22f),steel);
            HarborArtBuilder.Box("Finish header",arch,new Vector3(0,6f,0),new Vector3(halfSpan*2f+0.22f,0.38f,0.18f),ivory);
        }
        if(index==0)
        {
            foreach(var rail in level.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Safety barrier").ToArray())
            {
                var visual=rail.GetComponent<Renderer>();if(visual!=null)visual.enabled=false;
                Place("Racing","barrierWhite",root,rail.position-Vector3.up*0.5f,rail.rotation*Quaternion.Euler(0,90,0),2.4f);
            }
        }
        // A low-cost shared-material scenery budget; no new collision geometry on the racing line.
        Debug.Log($"ROUTE_ART {index+1} meshes={root.GetComponentsInChildren<MeshFilter>(true).Length}");
    }

    private static void ReplaceHarbor(Transform level,Transform root)
    {
        var old=level.Find("Harbor district");
        if(old!=null)
            foreach(var child in old.Cast<Transform>().ToArray())
                if(child.name!="Dock island"&&child.name!="Sea"&&!child.name.StartsWith("Crane")&&child.name!="Hoist cable"&&child.name!="Cargo hook")UnityEngine.Object.DestroyImmediate(child.gameObject);
        for(int i=0;i<12;i++)
        {
            var pos=new Vector3(-48+(i%4)*29,0,(i/4)*18);
            Place("Industrial","shipping-container-"+(char)('a'+i%3),root,pos,Quaternion.Euler(0,90,0),12);
            if(i%3==0)Place("Industrial","shipping-container-b",root,pos+Vector3.up*4.5f,Quaternion.Euler(0,90,0),12);
        }
        for(int i=0;i<5;i++)
            Place("Industrial","building-"+new[]{"b","g","h","j","m"}[i],root,new Vector3(-72+i*36,0,-88),Quaternion.Euler(0,180,0),26);
        Place("Industrial","water-tower",root,new Vector3(119,0,-70),Quaternion.identity,22,true);
        Place("Industrial","building-o",root,new Vector3(-116,0,65),Quaternion.Euler(0,90,0),23);
        for(int i=0;i<5;i++)Place("Industrial","building-"+(char)('a'+i),root,new Vector3(-85+i*43,0,150),Quaternion.Euler(0,180,0),34);
    }

    private static void PopulateTheme(Transform root,RaceCourse course,int index)
    {
        Bounds bounds=new Bounds(course.centerLine[0],Vector3.zero);foreach(var point in course.centerLine)bounds.Encapsulate(point);
        bool mountain=index==2||index==3||index==6||index==7;
        bool coast=index==4||index==5;
        var groundMaterial=HarborArtBuilder.Material("Course_Ground_"+index,mountain?new Color(0.22f,0.29f,0.25f):coast?new Color(0.5f,0.46f,0.31f):new Color(0.23f,0.29f,0.32f));
        HarborArtBuilder.Box("District ground",root,new Vector3(bounds.center.x,-0.04f,bounds.center.z),new Vector3(bounds.size.x+70,0.1f,bounds.size.z+70),groundMaterial);
        int placed=0;
        for(int z=-2;z<=2;z++)for(int x=-2;x<=2;x++)
        {
            if(x==0&&z==0)continue;
            Vector3 pos=bounds.center+new Vector3(x*(bounds.extents.x/2f+8f),0,z*(bounds.extents.z/2f+8f));pos.y=0;
            float radius=mountain||coast?4f:7f;
            if(DistanceToRoad(pos,course.centerLine)<course.gateHalfWidth+radius+2f)continue;
            if(mountain)
            {
                Place("Nature",placed%3==0?"rock_tallA":placed%2==0?"tree_pineTallA":"tree_pineTallB",root,pos,Quaternion.Euler(0,(placed*71)%360,0),placed%3==0?7:10,true);
                Place("Nature","rock_largeB",root,pos+new Vector3(2,0,3),Quaternion.identity,4);
            }
            else if(coast)
            {
                Place("Nature","tree_palm",root,pos,Quaternion.Euler(0,(placed*73)%360,0),8,true);
                Place("Nature","plant_bush",root,pos+new Vector3(2,0,2),Quaternion.identity,2.4f);
            }
            else Place("Industrial","building-"+(char)('a'+(placed+index)%15),root,pos,Quaternion.LookRotation(pos-bounds.center),12);
            placed++;
        }
        if(index==3||index==6)
            for(int i=0;i<7;i++)Place("Nature","rock_tallB",root,new Vector3(bounds.min.x-20+i*7,0,bounds.max.z+22),Quaternion.Euler(0,i*43,0),14+i%3*3,true);
    }

    private static float DistanceToRoad(Vector3 p,Vector3[] route)
    {
        float best=float.MaxValue;
        for(int i=1;i<route.Length;i++)
        {
            Vector3 d=route[i]-route[i-1];float t=Mathf.Clamp01(Vector3.Dot(p-route[i-1],d)/Mathf.Max(d.sqrMagnitude,0.001f));
            best=Mathf.Min(best,Vector3.Distance(p,route[i-1]+d*t));
        }
        return best;
    }

    private static void AddRaceFacilities(Transform root,RaceCourse course,int index)
    {
        float length=0;for(int i=1;i<course.centerLine.Length;i++)length+=Vector3.Distance(course.centerLine[i-1],course.centerLine[i]);
        for(int i=0;i<(index==0?20:6);i++)
        {
            HarborArtBuilder.Sample(course.centerLine,length*i/(index==0?20f:6f),out Vector3 point,out Vector3 forward);
            Vector3 side=Vector3.Cross(Vector3.up,forward);Quaternion rotation=Quaternion.LookRotation(forward);
            Vector3 pos=point+side*(course.gateHalfWidth+3f);
            if(DistanceToRoad(pos,course.centerLine)>course.gateHalfWidth+1)
                Place("Racing","lightPostModern",root,pos,rotation,6,true);
        }
        // Choose clear locations outside the course bounds rather than placing buildings in hairpins.
        Bounds bounds=new Bounds(course.centerLine[0],Vector3.zero);foreach(var point in course.centerLine)bounds.Encapsulate(point);
        float offset=index==0?18:10;
        Place("Racing","grandStandCovered",root,new Vector3(bounds.max.x+offset,0,bounds.center.z),Quaternion.Euler(0,90,0),index==0?22:12);
        Place("Racing","pitsGarage",root,new Vector3(bounds.min.x-offset,0,bounds.center.z),Quaternion.Euler(0,-90,0),index==0?20:10);
        Place("Racing","billboard",root,new Vector3(bounds.center.x,0,bounds.min.z-offset),Quaternion.Euler(0,180,0),index==0?12:8);
    }

    private static void ReplaceObstacleVisuals(Transform level)
    {
        var obstacles=level.Find("Obstacles");if(obstacles==null)return;
        foreach(var obstacle in obstacles.Cast<Transform>().ToArray())
        {
            var renderers=obstacle.GetComponentsInChildren<Renderer>();if(renderers.Length==0)continue;
            Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            string model=obstacle.name.StartsWith("Cone")?"pylon":obstacle.name.StartsWith("Tire")?"barrierWall":"barrierRed";
            foreach(var renderer in renderers)renderer.enabled=false;
            // Existing collider and penalties are retained. Fit the replacement to the existing footprint.
            float size=Mathf.Max(bounds.size.x,bounds.size.z);
            var view=Place("Racing",model,level,new Vector3(bounds.center.x,bounds.min.y,bounds.center.z),obstacle.rotation,size);
            view.transform.SetParent(obstacle,true);
        }
    }
}
