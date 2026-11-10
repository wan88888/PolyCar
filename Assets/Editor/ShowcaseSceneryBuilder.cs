using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Authored harbor and works-yard compositions, keeping the existing race geometry.</summary>
public static class ShowcaseSceneryBuilder
{
    private static Material concrete, asphalt, ivory, amber, steel, rust, teal;
    private static RaceCourse course;
    private static Transform root;

    private static void Begin(Transform level, Transform scenery)
    {
        root = scenery; course = level.GetComponent<RaceCourse>();
        concrete = Surface("Showcase_Concrete", new Color(0.43f,0.48f,0.48f));
        asphalt = Surface("Showcase_ServiceAsphalt", new Color(0.13f,0.17f,0.20f));
        ivory = HarborArtBuilder.Material("Showcase_Ivory",new Color(0.89f,0.87f,0.73f));
        amber = HarborArtBuilder.Material("Showcase_Amber",new Color(0.96f,0.57f,0.16f));
        steel = HarborArtBuilder.Material("Showcase_Steel",new Color(0.075f,0.13f,0.18f),0.35f,0.35f);
        rust = HarborArtBuilder.Material("Showcase_Rust",new Color(0.54f,0.23f,0.13f));
        teal = HarborArtBuilder.Material("Showcase_Teal",new Color(0.12f,0.37f,0.40f));
    }

    public static void Harbor(Transform level, Transform scenery)
    {
        Begin(level,scenery);
        AddLook(level, false);
        var old = level.Find("Harbor district");
        foreach (var child in old.Cast<Transform>().ToArray())
            if(child.name!="Dock island" && child.name!="Sea" && !child.name.StartsWith("Crane") && child.name!="Hoist cable" && child.name!="Cargo hook") Object.DestroyImmediate(child.gameObject);
        old.Find("Dock island").GetComponent<Renderer>().sharedMaterial = concrete;
        var water = HarborArtBuilder.Material("Showcase_Water",new Color(0.035f,0.24f,0.32f),0.72f,0.15f);
        water.shader=Shader.Find("PolyCar/HarborWater");
        water.SetColor("_BaseColor",new Color(0.045f,0.24f,0.31f));
        EditorUtility.SetDirty(water);
        old.Find("Sea").GetComponent<Renderer>().sharedMaterial = water;
        // Cargo aprons and loading lanes break up the old undifferentiated concrete plane.
        Box("Container apron",new Vector3(-3,0.012f,17),new Vector3(120,0.018f,66),asphalt);
        for(int row=0;row<3;row++)
        for(int col=0;col<4;col++)
        {
            float x=-48+col*29, z=-2+row*20;
            var model=Prop("Industrial","shipping-container-"+(char)('a'+(row+col)%3),new Vector3(x,0.03f,z),90,18);
            if((row+col)%3==0)Prop("Industrial","shipping-container-b",new Vector3(x,ExternalArtBuilder.BoundsOf(model).max.y,z),90,18);
            MarkBay(new Vector3(x,0.035f,z),22,11);
        }
        for(int i=0;i<4;i++)
        {
            float x=-76+i*48;
            Box("Warehouse loading apron",new Vector3(x,0.012f,-91),new Vector3(43,0.018f,31),asphalt);
            var warehouse=Prop("Industrial",new[]{"building-b","building-g","building-h","building-j"}[i],new Vector3(x,0,-96),0,32);
            Label("Terminal "+(i+1),new Vector3(x,6,-78),0,0.48f,ivory.color);
            for(int bay=0;bay<3;bay++)MarkBay(new Vector3(x-10+bay*10,0.035f,-75),8,10);
            Lamp(new Vector3(x-19,0,-72),0,7);
        }
        // A recognisable working waterfront: quay walls, mooring gear, crane bracing and ship.
        Box("North quay wall",new Vector3(0,-1.4f,121),new Vector3(272,3,2),steel,true);
        Box("West quay wall",new Vector3(-136,-1.4f,5),new Vector3(2,3,232),steel,true);
        Box("East quay wall",new Vector3(136,-1.4f,5),new Vector3(2,3,232),steel,true);
        for(int i=0;i<28;i++)
        {
            float x=-129+i*9.5f;
            Box("Quay edge marking",new Vector3(x,0.035f,118.6f),new Vector3(4,0.025f,0.5f),i%2==0?amber:ivory);
            if(i%2==0){Cylinder("Mooring bollard",new Vector3(x,0.45f,116),0.36f,0.9f,steel);Cylinder("Bollard cap",new Vector3(x,0.88f,116),0.54f,0.14f,amber);}
        }
        for(int c=0;c<3;c++)
        {
            var p=new Vector3(-75+c*73,0,105);
            Beam("Crane diagonal",p+new Vector3(-9,2,0),p+new Vector3(9,26,0),0.34f,steel);
            Beam("Crane diagonal",p+new Vector3(9,2,0),p+new Vector3(-9,26,0),0.34f,steel);
            Box("Crane operator cab",p+new Vector3(5,26,5),new Vector3(4,3,4),teal);
            for(int rail=0;rail<2;rail++)Box("Crane rail",p+new Vector3(rail==0?-9:9,0.03f,0),new Vector3(0.14f,0.035f,26),steel);
        }
        CargoShip(new Vector3(5,-0.3f,151));
        Box("Opposite bank",new Vector3(4,-0.4f,260),new Vector3(470,0.8f,90),concrete);
        Box("Opposite seawall",new Vector3(4,-1,214),new Vector3(470,2.8f,2),steel);

        for(int i=0;i<12;i++)
        {
            float x=-180+i*33;
            Prop("Industrial","building-"+(char)('a'+i%15),new Vector3(x,0,235+(i%3)*12),180,24+i%3*7);
        }
        Prop("Industrial","water-tower",new Vector3(115,0,-85),0,23,true);
        for(int i=0;i<12;i++) Lamp(new Vector3(-125,0,-82+i*16),90,8);
        // Grounded staging area beside the starting straight, with clearly readable direction signs.
        Prop("Racing","pitsGarage",new Vector3(-63,0,-78),0,20);
        Prop("Racing","pitsOffice",new Vector3(-114,0,-70),20,12);
        Prop("Racing","tent",new Vector3(-96,0,-60),0,8);
        Label("Port / 01",new Vector3(-63,7,-68),0,0.64f,ivory.color);
        for(int i=0;i<16;i++)
        {
            float t=(i+0.35f)/16;
            HarborArtBuilder.Sample(course.centerLine,Length(course.centerLine)*t,out var p,out var f);
            var side=Vector3.Cross(Vector3.up,f);
            var pos=p+side*(course.gateHalfWidth+5.5f);
            if(ClearPoint(pos,2))Lamp(pos,Quaternion.LookRotation(f).eulerAngles.y,6);
        }
        FinishArt(level);
    }

    public static void Industrial(Transform level, Transform scenery)
    {
        Begin(level,scenery);AddLook(level,true);
        // The wider learning course uses full-size city blocks instead of a miniature yard.
        Bounds bounds=new Bounds(course.centerLine[0],Vector3.zero);foreach(var point in course.centerLine)bounds.Encapsulate(point);
        Box("Works district foundation",new Vector3(bounds.center.x,-0.04f,bounds.center.z),new Vector3(bounds.size.x+120,0.10f,bounds.size.z+120),asphalt);
        Box("Service yard paving",new Vector3(bounds.center.x,0.018f,bounds.center.z),new Vector3(bounds.size.x+55,0.02f,bounds.size.z+55),concrete);
        for(int side=-1;side<=1;side+=2)
        for(int i=0;i<7;i++)
        {
            float x=bounds.min.x-10+i*(bounds.size.x+20)/6;
            float z=side<0?bounds.min.z-25:bounds.max.z+25;
            Prop("Industrial",new[]{"building-b","building-g","building-h","building-j"}[i%4],new Vector3(x,0,z),side<0?0:180,23);
            MarkBay(new Vector3(x,0.014f,z-side*16),8,10);
            Lamp(new Vector3(x,0,z-side*13),side<0?0:180,7);
        }
        for(int i=0;i<4;i++)
        {
            Prop("Industrial","shipping-container-"+(char)('a'+i%3),new Vector3(bounds.min.x-28,0,bounds.min.z+20+i*21),0,17);
            Prop("Industrial","building-"+(char)('k'+i),new Vector3(bounds.max.x+35,0,bounds.min.z+i*35),-90,22);
        }
        float north=bounds.max.z+49;
        Prop("Industrial","water-tower",new Vector3(bounds.min.x-20,0,north),0,24,true);
        for(int i=0;i<3;i++)
        {
            Prop("Industrial","detail-tank-large",new Vector3(18+i*16,0,north),0,12);
            Prop("Industrial","chimney-large",new Vector3(22+i*19,0,north+19),0,26+i*3,true);
            Box("Pipe rack support",new Vector3(-22+i*39,4,north-9),new Vector3(0.55f,8,0.55f),steel,true);
        }
        for(int i=0;i<3;i++)Beam("Works pipe",new Vector3(-25,7+i*0.6f,north-9),new Vector3(60,7+i*0.6f,north-9),0.4f,i==1?amber:rust);
        // Fill selected off-road parcels only; never move the road to accommodate scenery.
        int placed=0;
        for(float z=bounds.min.z+20;z<bounds.max.z;z+=35)
        for(float x=bounds.min.x+15;x<bounds.max.x;x+=38)
        {
            var pos=new Vector3(x,0,z);
            if(!ClearBounds(new Bounds(pos,new Vector3(21,15,21)),5))continue;
            Prop("Industrial",placed++%2==0?"building-d":"building-e",pos,180,19);
            if(ClearPoint(pos+Vector3.right*13,2))Lamp(pos+Vector3.right*13,0,6);
        }
        float length=Length(course.centerLine);
        for(int i=0;i<14;i++)
        {
            HarborArtBuilder.Sample(course.centerLine,length*(i+0.2f)/14,out var point,out var forward);
            var pos=point+Vector3.Cross(Vector3.up,forward)*(course.gateHalfWidth+5);
            if(ClearPoint(pos,2.5f))Lamp(pos,Quaternion.LookRotation(forward).eulerAngles.y,6);
        }
        Label("Foundry / 02",new Vector3(18,10,north-9),180,0.7f,ivory.color);
        foreach(var renderer in level.Find("Guidance").GetComponentsInChildren<Renderer>())renderer.sharedMaterial=ivory;
        foreach(var gate in course.gates)
        foreach(Transform child in gate)
        {
            if(child.name=="Gate beacon"){var p=child.localPosition;p.y=0.9f;child.localPosition=p;child.localScale=new Vector3(0.12f,1.35f,0.12f);}
            if(child.name=="Gate footing"){var p=child.localPosition;p.y=0.2f;child.localPosition=p;child.localScale=new Vector3(0.42f,0.4f,0.5f);}
        }
        FinishArt(level);
    }

    private static void FinishArt(Transform level)
    {
        foreach(var t in root.GetComponentsInChildren<Transform>()) t.gameObject.isStatic=true;
        Debug.Log($"SHOWCASE_SCENERY route={level.name} renderers={root.GetComponentsInChildren<Renderer>().Length}");
    }

    private static GameObject Prop(string pack,string name,Vector3 p,float yaw,float size,bool height=false)
    {
        var go=ExternalArtBuilder.Place(pack,name,root,p,Quaternion.Euler(0,yaw,0),size,height);
        var bounds=ExternalArtBuilder.BoundsOf(go);
        // Authored assets must leave enough room for the road, shoulders and chase camera.
        if(bounds.min.y<4.5f && !ClearBounds(bounds,1.5f))
        {
            Object.DestroyImmediate(go);
            throw new InvalidOperationException($"Scenery intrudes on road: {name} at {p}");
        }
        if(bounds.min.y<0.1f)
        {
            var collision=new GameObject("Scenery collision "+name,typeof(BoxCollider));collision.transform.SetParent(root,false);
            collision.transform.position=bounds.center;collision.GetComponent<BoxCollider>().size=bounds.size;
        }
        return go;
    }

    public static bool ClearBounds(Bounds b,RaceCourse route,float margin)
    {
        for(int i=1;i<route.centerLine.Length;i++)
        {
            float distance=Vector3.Distance(route.centerLine[i-1],route.centerLine[i]);
            for(float d=0;d<=distance+0.5f;d+=0.5f)
            {
                var p=Vector3.Lerp(route.centerLine[i-1],route.centerLine[i],Mathf.Min(d/distance,1));p.y=b.center.y;
                if(Vector3.Distance(p,b.ClosestPoint(p))<route.gateHalfWidth+margin)return false;
            }
        }
        return true;
    }
    private static bool ClearBounds(Bounds b,float margin)=>ClearBounds(b,course,margin);
    private static bool ClearPoint(Vector3 p,float radius)=>ClearBounds(new Bounds(p,new Vector3(radius*2,1,radius*2)),1.0f);
    private static float Length(Vector3[] points){float l=0;for(int i=1;i<points.Length;i++)l+=Vector3.Distance(points[i-1],points[i]);return l;}
    private static GameObject Box(string name,Vector3 p,Vector3 size,Material mat,bool solid=false)
    {
        if(solid && p.y-size.y/2<4.5f && !ClearBounds(new Bounds(p,size),1.5f))throw new InvalidOperationException("Solid scenery touches course: "+name);
        return HarborArtBuilder.Box(name,root,p,size,mat,solid);
    }
    private static void Beam(string name,Vector3 a,Vector3 b,float diameter,Material mat)
    {
        var go=Box(name,(a+b)*0.5f,new Vector3(diameter,diameter,Vector3.Distance(a,b)),mat);
        go.transform.rotation=Quaternion.LookRotation(b-a);
    }
    private static void Cylinder(string name,Vector3 p,float radius,float height,Material mat)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(root,false);go.transform.position=p;go.transform.localScale=new Vector3(radius*2,height/2,radius*2);go.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(go.GetComponent<Collider>());
    }
    private static void Lamp(Vector3 p,float yaw,float height)=>Prop("Racing","lightPostModern",p,yaw,height,true);
    private static void MarkBay(Vector3 p,float width,float length)
    {
        foreach(int side in new[]{-1,1})Box("Loading bay line",p+new Vector3(side*width/2,0,0),new Vector3(0.12f,0.012f,length),ivory);
        Box("Loading bay line",p+new Vector3(0,0,length/2),new Vector3(width,0.012f,0.12f),ivory);
    }
    private static void Drain(Vector3 p,float yaw)
    {
        var grate=Box("Drain surround",p,new Vector3(1.2f,0.014f,0.5f),steel);grate.transform.rotation=Quaternion.Euler(0,yaw,0);
        for(int i=0;i<7;i++)Box("Drain slot",p+new Vector3(-0.5f+i/6f,0.01f,0),new Vector3(0.045f,0.012f,0.38f),concrete);
    }
    private static void Label(string text,Vector3 p,float yaw,float size,Color tint)
    {
        var go=new GameObject(text);go.transform.SetParent(root,false);go.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));
        go.transform.rotation=Quaternion.Euler(0,yaw+180,0);
        var label=go.AddComponent<TextMesh>();label.text=text;label.fontSize=64;label.characterSize=size*0.18f;label.anchor=TextAnchor.MiddleCenter;label.color=tint;
        var back=Box("Sign backing",p+go.transform.forward*0.04f,new Vector3(text.Length*size*0.7f,size*1.5f,0.06f),steel);
        back.transform.rotation=go.transform.rotation;
    }
    private static Material Surface(string name,Color tint)
    {
        var mat=HarborArtBuilder.Material(name,tint);
        string path="Assets/Materials/"+name+"_Grain.asset";
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        bool fresh=texture==null;if(fresh)texture=new Texture2D(128,128,TextureFormat.RGB24,true);
        var random=new System.Random(42);
        var pixels=new Color[128*128];for(int i=0;i<pixels.Length;i++){float v=0.91f+(float)random.NextDouble()*0.09f;pixels[i]=new Color(v,v,v);}
        texture.SetPixels(pixels);texture.Apply();texture.wrapMode=TextureWrapMode.Repeat;texture.filterMode=FilterMode.Trilinear;
        if(fresh)AssetDatabase.CreateAsset(texture,path);else EditorUtility.SetDirty(texture);
        mat.SetTexture("_BaseMap",texture);mat.SetTextureScale("_BaseMap",new Vector2(24,24));EditorUtility.SetDirty(mat);return mat;
    }
    private static void AddLook(Transform level,bool works)
    {
        var look=level.gameObject.AddComponent<SceneLook>();
        string path="Assets/Materials/Showcase_Sky_"+(works?"Works":"Harbor")+".mat";
        var sky=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(sky==null){sky=new Material(Shader.Find("PolyCar/ShowcaseSky"));AssetDatabase.CreateAsset(sky,path);}
        sky.SetColor("_Zenith",works?new Color(0.16f,0.35f,0.52f):new Color(0.10f,0.38f,0.62f));
        sky.SetColor("_Horizon",works?new Color(0.76f,0.72f,0.59f):new Color(0.65f,0.78f,0.80f));
        EditorUtility.SetDirty(sky);
        look.sky=sky;look.fogColor=works?new Color(0.64f,0.67f,0.64f):new Color(0.57f,0.72f,0.77f);
        look.fogStart=works?95:210;look.fogEnd=works?340:700;
        look.sunColor=works?new Color(1,0.85f,0.67f):new Color(1,0.94f,0.83f);
    }

    private static void ContinuousRoad(Transform level)
    {
        var road=level.Find("Road");foreach(var child in road.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
        var pts=course.centerLine;
        var offsets=new Vector3[pts.Length];
        for(int i=0;i<pts.Length;i++)
        {
            var a=i==0?(pts[1]-pts[0]).normalized:(pts[i]-pts[i-1]).normalized;
            var b=i==pts.Length-1?a:(pts[i+1]-pts[i]).normalized;
            var normal=Vector3.Cross(Vector3.up,(a+b).normalized);
            offsets[i]=normal/Mathf.Max(0.55f,Vector3.Dot(normal,Vector3.Cross(Vector3.up,b)));
        }
        void Strip(string name,float left,float right,float y,Material mat,bool collision)
        {
            var vertices=new Vector3[pts.Length*2];var triangles=new int[(pts.Length-1)*6];var uv=new Vector2[vertices.Length];float distance=0;
            for(int i=0;i<pts.Length;i++)
            {
                if(i>0)distance+=Vector3.Distance(pts[i],pts[i-1]);
                vertices[2*i]=pts[i]+offsets[i]*left+Vector3.up*y;vertices[2*i+1]=pts[i]+offsets[i]*right+Vector3.up*y;
                uv[2*i]=new Vector2(0,distance/12);uv[2*i+1]=new Vector2(1,distance/12);
                if(i==pts.Length-1)continue;int k=i*6,v=i*2;
                triangles[k]=v;triangles[k+1]=v+2;triangles[k+2]=v+1;triangles[k+3]=v+1;triangles[k+4]=v+2;triangles[k+5]=v+3;
            }
            var mesh=new Mesh{name=name,vertices=vertices,triangles=triangles,uv=uv};mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path="Assets/Models/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);}
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(road,false);
            go.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);go.GetComponent<Renderer>().sharedMaterial=mat;
            if(collision)go.AddComponent<MeshCollider>().sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;
        }
        float half=course.gateHalfWidth-0.5f;
        Strip("Works_Shoulder",-half-0.7f,half+0.7f,0.005f,concrete,false);
        Strip("Works_Road",-half,half,0.04f,HarborArtBuilder.Material("Works_Asphalt",new Color(0.10f,0.14f,0.17f)),true);
        Strip("Works_LeftEdge",-half+0.08f,-half+0.2f,0.055f,ivory,false);
        Strip("Works_RightEdge",half-0.2f,half-0.08f,0.055f,ivory,false);
    }

    private static void CargoShip(Vector3 p)
    {
        // A tapered hull silhouette and stacked deck cargo, rather than another floating rectangular block.
        var outline=new[]{new Vector2(-56,-11),new Vector2(43,-11),new Vector2(57,-4),new Vector2(57,4),new Vector2(43,11),new Vector2(-56,11)};
        var verts=new List<Vector3>();foreach(var v in outline)verts.Add(p+new Vector3(v.x,-0.15f,v.y));foreach(var v in outline)verts.Add(p+new Vector3(v.x*0.97f,4,v.y*0.92f));
        var tris=new List<int>();for(int i=0;i<6;i++){int j=(i+1)%6;tris.AddRange(new[]{i,j,i+6,j,j+6,i+6});}
        for(int i=1;i<5;i++)tris.AddRange(new[]{6,6+i,7+i});
        for(int i=0;i<tris.Count;i+=3){int swap=tris[i+1];tris[i+1]=tris[i+2];tris[i+2]=swap;}
        var mesh=new Mesh{name="Harbor cargo hull",vertices=verts.ToArray(),triangles=tris.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();
        const string path="Assets/Models/Showcase_CargoHull.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);}
        var hull=new GameObject("Cargo vessel hull",typeof(MeshFilter),typeof(MeshRenderer));hull.transform.SetParent(root,false);hull.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);hull.GetComponent<Renderer>().sharedMaterial=teal;
        Box("Cargo vessel deck",p+new Vector3(-2,4.1f,0),new Vector3(98,0.3f,18),rust);
        for(int row=0;row<2;row++)for(int col=0;col<4;col++)Prop("Industrial","shipping-container-"+(char)('a'+(col+row)%3),p+new Vector3(-24+col*19,4.25f,-5+row*10),90,17);
        Box("Ship superstructure",p+new Vector3(-42,8,0),new Vector3(12,8,15),ivory);
        Box("Bridge windows",p+new Vector3(-42,10.5f,-7.6f),new Vector3(10,1.6f,0.2f),steel);
        Box("Bridge roof",p+new Vector3(-42,12.3f,0),new Vector3(14,0.35f,17),ivory);
        Cylinder("Ship funnel",p+new Vector3(-47,15,0),1.5f,6,amber);
        Beam("Mooring line",p+new Vector3(-46,4,-11),new Vector3(-55,0.8f,116),0.10f,steel);
        Beam("Mooring line",p+new Vector3(44,4,-11),new Vector3(62,0.8f,116),0.10f,steel);
    }
}
