using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Deterministic authoring for the harbor vertical slice; assets remain editable.</summary>
public static class HarborArtBuilder
{
    public static Material Material(string name, Color color, float smoothness = 0.15f, float metallic = 0f)
    {
        string path = "Assets/Materials/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color); mat.SetColor("_Color", color);
        mat.SetFloat("_Smoothness", smoothness); mat.SetFloat("_Metallic", metallic);
        EditorUtility.SetDirty(mat); return mat;
    }

    public static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material mat, bool collider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent,false);
        go.transform.localPosition = position; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    public static Vector3[] HarborRoute()
    {
        var knots = new[] { new Vector3(-70,0,-40), new Vector3(0,0,-58), new Vector3(78,0,-38), new Vector3(110,0,18), new Vector3(65,0,70), new Vector3(-12,0,80), new Vector3(-93,0,36) };
        var points = new List<Vector3>();
        for (int i = 0; i < knots.Length; i++)
        {
            Vector3 a = knots[(i + knots.Length - 1) % knots.Length], b = knots[i], c = knots[(i+1)%knots.Length], d = knots[(i+2)%knots.Length];
            for (int j = 0; j < 20; j++)
            {
                float t = j / 20f;
                points.Add(0.5f * (2*b + (c-a)*t + (2*a-5*b+4*c-d)*t*t + (-a+3*b-3*c+d)*t*t*t));
            }
        }
        points.Add(points[0]); return points.ToArray();
    }

    public static void Road(Transform parent, Vector3[] points, float width)
    {
        var asphalt = Material("Harbor_Asphalt",new Color(0.105f,0.14f,0.17f));
        var edge = Material("Harbor_Concrete",new Color(0.56f,0.61f,0.61f));
        var paint = Material("Harbor_Ivory",new Color(0.9f,0.87f,0.72f));
        var amber = Material("Harbor_Amber",new Color(1f,0.53f,0.12f));
        Ribbon("Harbor_Shoulder", parent, points, width+2.6f, 0.015f, edge, false);
        Ribbon("Harbor_Road", parent, points, width, 0.04f, asphalt, true);
        for (int i = 0; i < points.Length-1; i++)
        {
            Vector3 forward = (points[i+1]-points[i]).normalized;
            Vector3 right = Vector3.Cross(Vector3.up,forward);
            var rotation = Quaternion.LookRotation(forward);
            for (int side = -1; side <= 1; side += 2)
            {
                var curb = Box("Alternating curb",parent,(points[i]+points[i+1])*0.5f+right*(width/2f+0.38f)*side+Vector3.up*0.08f,new Vector3(0.7f,0.1f,Vector3.Distance(points[i],points[i+1])+0.05f),i%2==0 ? paint:amber);
                curb.transform.rotation = rotation;
                if (i % 4 == 0)
                {
                    var rail = Box("Safety barrier",parent,points[i]+right*(width/2f+1.6f)*side+Vector3.up*0.5f,new Vector3(0.3f,1f,2.4f),edge,true);
                    rail.transform.rotation = rotation;
                    rail.AddComponent<ObstacleFeedback>().Configure(1,new Color(1,0.6f,0.2f),1f);
                }
            }
            if (i%2==0)
            {
                var line = Box("Center dash",parent,points[i]+Vector3.up*0.06f,new Vector3(0.12f,0.015f,1.5f),paint);
                line.transform.rotation = rotation;
            }
        }
    }

    private static void Ribbon(string name, Transform parent, Vector3[] points, float width, float height, Material material, bool collision)
    {
        var vertices = new Vector3[points.Length*2]; var triangles = new int[(points.Length-1)*6];
        for (int i=0;i<points.Length;i++)
        {
            int p = i == 0 ? points.Length-2 : i-1, n = i == points.Length-1 ? 1 : i+1;
            Vector3 right = Vector3.Cross(Vector3.up,(points[n]-points[p]).normalized);
            vertices[i*2] = points[i]-right*width/2f+Vector3.up*height;
            vertices[i*2+1] = points[i]+right*width/2f+Vector3.up*height;
            if (i==points.Length-1) continue;
            int k=i*6,v=i*2; triangles[k]=v;triangles[k+1]=v+2;triangles[k+2]=v+1;triangles[k+3]=v+1;triangles[k+4]=v+2;triangles[k+5]=v+3;
        }
        var mesh = new Mesh {name=name,vertices=vertices,triangles=triangles}; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        SaveMesh(name,mesh);
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
        go.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/"+name+".asset");go.GetComponent<MeshRenderer>().sharedMaterial=material;
        if(collision) go.AddComponent<MeshCollider>().sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;
    }

    private static void SaveMesh(string name, Mesh mesh)
    {
        if(!AssetDatabase.IsValidFolder("Assets/Models")) AssetDatabase.CreateFolder("Assets","Models");
        string path="Assets/Models/"+name+".asset";
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old==null) AssetDatabase.CreateAsset(mesh,path);
        else {EditorUtility.CopySerialized(mesh,old); Object.DestroyImmediate(mesh);}
    }

    public static void Course(Transform parent, Vector3[] points, float width, int gateCount, bool harbor)
    {
        var course=parent.gameObject.AddComponent<RaceCourse>();course.gateHalfWidth=width/2f+0.5f;
        course.centerLine=points;course.goldTime=harbor?90f:60f;course.gates=new Transform[gateCount];
        // Two laps make the harbor a 60–90 second learning challenge at its tuned speed.
        course.laps=harbor?2:1;
        var cyan=Material("Harbor_Gate",new Color(0.18f,0.83f,0.83f),0.4f);
        var dark=Material("Harbor_Steel",new Color(0.055f,0.10f,0.14f),0.3f,0.5f);
        float length=0;for(int i=1;i<points.Length;i++)length+=Vector3.Distance(points[i-1],points[i]);
        for(int g=0;g<gateCount;g++)
        {
            float distance=length*(g+1)/gateCount;
            Sample(points,distance,out Vector3 pos,out Vector3 dir);
            var root=new GameObject("Gate_"+(g+1).ToString("00"));root.transform.SetParent(parent,false);root.transform.position=pos;root.transform.rotation=Quaternion.LookRotation(dir);
            course.gates[g]=root.transform;
            foreach(int side in new[]{-1,1})
            {
                Box("Gate footing",root.transform,new Vector3(side*(width/2f+0.8f),0.4f,0),new Vector3(1.1f,0.8f,1.3f),dark);
                Box("Gate beacon",root.transform,new Vector3(side*(width/2f+0.8f),2f,0),new Vector3(0.22f,3.5f,0.22f),cyan);
            }
            Box("Gate stripe",root.transform,new Vector3(0,0.075f,0),new Vector3(width,0.025f,0.32f),cyan);
            Label(root.transform, g==gateCount-1 ? "Finish" : (g+1).ToString("00"), new Vector3(0,4,0), 0.45f,cyan.color);
        }
    }

    public static void Sample(Vector3[] points,float distance,out Vector3 position,out Vector3 forward)
    {
        for(int i=1;i<points.Length;i++)
        {
            float length=Vector3.Distance(points[i-1],points[i]);
            if(distance<=length || i==points.Length-1){position=Vector3.Lerp(points[i-1],points[i],Mathf.Clamp01(distance/length));forward=(points[i]-points[i-1]).normalized;return;}
            distance-=length;
        }
        position=points[0];forward=Vector3.forward;
    }

    public static void Scenery(Transform parent)
    {
        var root=new GameObject("Harbor district");root.transform.SetParent(parent,false);
        var concrete=Material("Harbor_Dock",new Color(0.28f,0.35f,0.37f));
        var water=Material("Harbor_Water",new Color(0.07f,0.37f,0.46f),0.8f,0.25f);
        var orange=Material("Harbor_Crane",new Color(0.94f,0.43f,0.16f),0.3f,0.25f);
        var white=Material("Harbor_Pearl",new Color(0.79f,0.82f,0.78f));
        var dark=Material("Harbor_Steel",new Color(0.055f,0.10f,0.14f),0.3f,0.5f);
        Box("Dock island",root.transform,new Vector3(0,-0.35f,5),new Vector3(270,0.7f,230),concrete,true);
        Box("Sea",root.transform,new Vector3(0,-0.7f,0),new Vector3(1200,0.1f,1200),water);
        var colors=new[]{new Color(0.12f,0.43f,0.48f),new Color(0.79f,0.29f,0.16f),new Color(0.67f,0.66f,0.45f)};
        for(int i=0;i<18;i++)
        {
            float x=-48+(i%6)*18,z=-4+(i/6)*17;
            var mat=Material("Harbor_Container_"+(i%3),colors[i%3]);
            int stacks=i%3==0?2:1;
            for(int j=0;j<stacks;j++)
            {
                Vector3 pos=new Vector3(x,1.6f+j*3.2f,z);
                Box("Cargo container",root.transform,pos,new Vector3(12,3.1f,5.5f),mat,true);
                for(int rib=0;rib<12;rib++)Box("Container rib",root.transform,pos+new Vector3(-5.5f+rib,0,-2.8f),new Vector3(0.075f,2.8f,0.08f),dark);
                Label(root.transform,"POLY / CARGO",pos+new Vector3(0,0,-2.88f),0.24f,Color.white);
            }
        }
        for(int c=0;c<3;c++)
        {
            Vector3 p=new Vector3(-75+c*73,0,105);
            for(int side=-1;side<=1;side+=2)Box("Crane tower",root.transform,p+new Vector3(side*9,14,0),new Vector3(1.6f,28,1.8f),orange,true);
            Box("Crane bridge",root.transform,p+new Vector3(0,28,0),new Vector3(23,2,3),orange);
            Box("Crane boom",root.transform,p+new Vector3(0,28,13),new Vector3(2,1.4f,44),orange);
            Box("Hoist cable",root.transform,p+new Vector3(0,19,29),new Vector3(0.1f,18,0.1f),dark);
            Box("Cargo hook",root.transform,p+new Vector3(0,10,29),new Vector3(3,0.5f,2),dark);
        }
        for(int i=0;i<9;i++)
        {
            Vector3 p=new Vector3(-125,0,-70+i*20);
            Box("Quay lamp",root.transform,p+Vector3.up*5,new Vector3(0.2f,10,0.2f),dark);
            Box("Lamp cap",root.transform,p+new Vector3(1,10,0),new Vector3(2.4f,0.2f,0.6f),white);
        }
        Box("Quayside warehouse",root.transform,new Vector3(12,5,-90),new Vector3(62,10,20),white,true);
        Box("Warehouse roof",root.transform,new Vector3(12,10.3f,-90),new Vector3(65,0.6f,23),dark);
        for(int i=0;i<6;i++)
        {
            Box("Loading bay",root.transform,new Vector3(-13+i*10,2.6f,-79.9f),new Vector3(6,5,0.15f),dark);
            Box("Loading canopy",root.transform,new Vector3(-13+i*10,5.5f,-77.8f),new Vector3(7,0.2f,4),orange);
        }
        for(int i=0;i<12;i++)
            Box("Distant skyline",root.transform,new Vector3(-190+i*35,12+(i%4)*4,220),new Vector3(22,24+(i%4)*8,20),concrete);
        Label(root.transform,"HARBOR / 01",new Vector3(-20,8,-20),1.2f,white.color);
    }

    private static void Label(Transform parent,string text,Vector3 position,float size,Color color)
    {
        var go=new GameObject(text);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.identity;
        var label=go.AddComponent<TextMesh>();label.text=text;label.characterSize=size * 0.1f;label.fontSize=64;label.anchor=TextAnchor.MiddleCenter;label.color=color;
    }

    public static void Car(Transform root,string carName,Material paint,Material glass,Material tire,Vector3 dimensions)
    {
        var trim=Material("Car_Trim",new Color(0.045f,0.055f,0.065f),0.3f);
        var rim=Material("Car_Alloy",new Color(0.65f,0.72f,0.75f),0.65f,0.8f);
        var white=Material("Car_Headlamp",new Color(0.88f,0.97f,1f),0.8f);
        var red=Material("Car_Taillamp",new Color(0.6f,0.025f,0.015f),0.5f);
        float w=dimensions.x/2f,l=dimensions.z/2f;
        Profile(carName+"_Body",root,new[]{new Vector4(-l,w*0.85f,-0.03f,0.33f),new Vector4(-l+0.22f,w, -0.07f,0.5f),new Vector4(l-0.35f,w,-0.07f,0.43f),new Vector4(l,w*0.78f,0.05f,0.3f)},paint);
        float roof=carName.Contains("Rally")?1.03f:carName.Contains("Speed")?0.75f:0.92f;
        Profile(carName+"_Cabin",root,new[]{new Vector4(-1.05f,w*0.76f,0.43f,0.48f),new Vector4(-0.7f,w*0.65f,0.44f,roof),new Vector4(0.22f,w*0.64f,0.44f,roof),new Vector4(0.78f,w*0.76f,0.42f,0.46f)},glass);
        Box("Roof",root,new Vector3(0,roof+0.015f,-0.24f),new Vector3(w*1.3f,0.055f,0.95f),paint);
        Box("Front splitter",root,new Vector3(0,0.015f,l-0.05f),new Vector3(w*1.8f,0.1f,0.35f),trim);
        Box("Rear bumper",root,new Vector3(0,0.06f,-l),new Vector3(w*1.8f,0.16f,0.15f),trim);
        Box("Front grille",root,new Vector3(0,0.2f,l),new Vector3(w,0.15f,0.03f),trim);
        var tail=new List<Renderer>();var wheels=new List<Transform>();
        foreach(int side in new[]{-1,1})
        {
            Box("Headlight",root,new Vector3(side*w*0.64f,0.31f,l-0.015f),new Vector3(0.32f,0.12f,0.065f),white);
            tail.Add(Box("Brake light",root,new Vector3(side*w*0.63f,0.28f,-l-0.02f),new Vector3(0.35f,0.12f,0.055f),red).GetComponent<Renderer>());
            Box("Side sill",root,new Vector3(side*w,0.06f,0),new Vector3(0.08f,0.12f,l*1.5f),trim);
            Box("Mirror",root,new Vector3(side*(w+0.07f),0.61f,0.42f),new Vector3(0.22f,0.12f,0.22f),paint);
        }
        for(int i=0;i<4;i++)
        {
            int side=i%2==0?-1:1;
            var wheel=GameObject.CreatePrimitive(PrimitiveType.Cylinder);wheel.name="Wheel_"+i;wheel.transform.SetParent(root,false);
            wheel.transform.localPosition=new Vector3(side*(w+0.01f),-0.01f,i<2?1.02f:-1.08f);wheel.transform.localRotation=Quaternion.Euler(0,0,90);wheel.transform.localScale=new Vector3(0.52f,0.16f,0.52f);wheel.GetComponent<Renderer>().sharedMaterial=tire;Object.DestroyImmediate(wheel.GetComponent<Collider>());
            var hub=GameObject.CreatePrimitive(PrimitiveType.Cylinder);hub.name="Alloy rim";hub.transform.SetParent(wheel.transform,false);hub.transform.localPosition=new Vector3(0,-side*1.02f,0);hub.transform.localScale=new Vector3(0.66f,0.06f,0.66f);hub.GetComponent<Renderer>().sharedMaterial=rim;Object.DestroyImmediate(hub.GetComponent<Collider>());
            wheels.Add(wheel.transform);
        }
        if(!carName.Contains("Starter"))
        {
            Box("Wing",root,new Vector3(0,0.73f,-l+0.15f),new Vector3(w*2.05f,0.08f,0.35f),paint);
            foreach(int side in new[]{-1,1})Box("Wing mount",root,new Vector3(side*0.5f,0.55f,-l+0.15f),new Vector3(0.06f,0.3f,0.1f),trim);
        }
        Box("Hood stripe",root,new Vector3(0,0.445f,1.05f),new Vector3(0.25f,0.015f,0.55f),white);
        var fx=root.parent.gameObject.AddComponent<CarPresentation>();fx.visualRoot=root;fx.wheels=wheels.ToArray();fx.brakeLights=tail.ToArray();
        fx.trailMaterial=EffectMaterial("Tire_Marks",false);fx.smokeMaterial=EffectMaterial("Tire_Smoke",true);
    }

    private static Material EffectMaterial(string name,bool smoke)
    {
        string path="Assets/Materials/"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(mat,path);}
        mat.shader = Shader.Find(smoke ? "Sprites/Default" : "PolyCar/TireMarks");
        if(smoke)
        {
            string texPath="Assets/Materials/SmokeFalloff.asset";
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if(tex==null)
            {
                tex=new Texture2D(32,32,TextureFormat.RGBA32,false);
                for(int y=0;y<32;y++)for(int x=0;x<32;x++){float a=Mathf.Clamp01(1-Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/15.5f);tex.SetPixel(x,y,new Color(1,1,1,a*a));}
                tex.Apply();AssetDatabase.CreateAsset(tex,texPath);
            }
            mat.mainTexture=tex;
        }
        EditorUtility.SetDirty(mat);return mat;
    }

    private static void Profile(string name,Transform parent,Vector4[] slices,Material material)
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();
        // An eight-sided section gives a deliberate chamfer instead of a primitive box.
        var rings=new List<Vector3[]>();
        foreach(var s in slices)
        {
            float z=s.x,w=s.y,b=s.z,t=s.w,c=0.08f;
            rings.Add(new[]{new Vector3(-w+c,b,z),new Vector3(w-c,b,z),new Vector3(w,b+c,z),new Vector3(w,t-c,z),new Vector3(w-c,t,z),new Vector3(-w+c,t,z),new Vector3(-w,t-c,z),new Vector3(-w,b+c,z)});
        }
        void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 d){int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
        for(int i=0;i<rings.Count-1;i++)for(int j=0;j<8;j++)Face(rings[i][j],rings[i][(j+1)%8],rings[i+1][(j+1)%8],rings[i+1][j]);
        // Caps use a center fan.
        for(int end=0;end<2;end++)
        {
            var ring=rings[end==0?0:rings.Count-1];Vector3 center=Vector3.zero;foreach(var v in ring)center+=v;center/=8;
            for(int j=0;j<8;j++){int n=vertices.Count;vertices.AddRange(new[]{center,ring[end==0?(j+1)%8:j],ring[end==0?j:(j+1)%8]});triangles.AddRange(new[]{n,n+1,n+2});}
        }
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();SaveMesh(name,mesh);
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/"+name+".asset");go.GetComponent<MeshRenderer>().sharedMaterial=material;
    }
}
