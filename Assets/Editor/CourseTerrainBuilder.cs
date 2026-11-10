using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Authored height fields and continuous road verges. Road profiles remain smooth and driveable.</summary>
public static class CourseTerrainBuilder
{
    // World-space fields keep intersecting route sections and closed circuits at the same height.
    public static float HeightAt(int index,float x,float z)
    {
        switch(index)
        {
            case 2:return 5f+(z+70)*0.075f+0.3f*Mathf.Sin(x/65f);
            case 3:return 7f+0.045f*(z+30)+1.1f*Mathf.Sin(x/80f);
            case 4:return 2.7f+0.35f*Mathf.Sin(z/75f)+0.15f*Mathf.Sin(x/60f);
            case 5:return 2.8f+0.22f*Mathf.Sin(x/80f)*Mathf.Cos(z/70f);
            case 6:return 7f+2.1f*Mathf.Sin(z/100f)+0.8f*Mathf.Cos(x/85f);
            case 7:return 8f+(z+100)*0.075f+0.25f*Mathf.Sin(x/90f);
            case 8:return 2.1f; // Engineered industrial slabs are level; relief lives outside the course.
            case 9:return 2.4f; // City pavement retains the validated flat driving surface.
            default:return 0;
        }
    }

    public static float DistanceToRoad(Vector3 position,Vector3[] route,out Vector3 nearest)
    {
        float best=float.MaxValue;nearest=route[0];
        for(int i=1;i<route.Length;i++)
        {
            var a=route[i-1];var b=route[i];var d=b-a;d.y=0;
            var delta=position-a;delta.y=0;
            float t=Mathf.Clamp01(Vector3.Dot(delta,d)/Mathf.Max(d.sqrMagnitude,0.00001f));
            var point=Vector3.Lerp(a,b,t);var separation=position-point;separation.y=0;
            if(separation.sqrMagnitude<best){best=separation.sqrMagnitude;nearest=point;}
        }
        return Mathf.Sqrt(best);
    }

    public static float GroundHeight(int index,Vector3 position,Vector3[] route,float width)
    {
        float distance=DistanceToRoad(position,route,out var nearest);
        // Engineered factory/city clear zones stay flush with pavement, like the validated original slab.
        // The broad apron also covers the largest drift excursion before terrain begins to undulate.
        if(index==8||index==9)
        {
            float apron=HeightAt(index,position.x,position.z)+0.035f;
            float far=HeightAt(index,position.x,position.z)+0.18f*Mathf.Sin(position.x*0.018f+0.6f)*Mathf.Cos(position.z*0.021f);
            return Mathf.Lerp(apron,far,Mathf.SmoothStep(0,1,Mathf.InverseLerp(width/2+15,width/2+35,distance)));
        }
        // Keep the 4 m grid's farthest triangle corner (5.66 m) outside the 5.5 m return corridor.
        float edge=width/2+12;
        // Flat cross-section beneath road/shoulders prevents raised terrain vertices snagging a wheel.
        float bank=Mathf.SmoothStep(0,1,Mathf.InverseLerp(edge,edge+19,distance));
        float h=HeightAt(index,position.x,position.z);
        float waves=Mathf.Sin(position.x*0.018f+0.6f)*Mathf.Cos(position.z*0.021f);
        if(index==2)h+=3+14*waves;
        else if(index==7)h+=6+22*waves;
        else if(index==3)h+=Mathf.Floor(Mathf.Max(0,distance-edge)/12f)*2.1f+1.2f*waves;
        else if(index==6)h+=8+19*(0.5f+0.5f*waves);
        else if(index==4||index==5)
        {
            float east=route[0].x;foreach(var p in route)east=Mathf.Max(east,p.x);
            // Beaches ease into the water east of the coastal roads.
            h=Mathf.Lerp(h+0.65f*waves,-2.8f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(east+5,east+65,position.x)));
        }
        else h+=0.18f*waves;
        return Mathf.Lerp(nearest.y-0.18f,h,bank);
    }

    /// <summary>Height of the actual authored 4 m grid triangle, for grounded props and verge seams.</summary>
    public static float SurfaceHeight(int index,Vector3 position,Vector3[] route,float width)
    {
        float x=Mathf.Floor(position.x/4)*4,z=Mathf.Floor(position.z/4)*4;
        float tx=(position.x-x)/4,tz=(position.z-z)/4;
        float H(float px,float pz)=>GroundHeight(index,new Vector3(px,0,pz),route,width);
        float a=H(x,z),b=H(x+4,z),c=H(x,z+4),d=H(x+4,z+4);
        return tx+tz<=1?a+(b-a)*tx+(c-a)*tz:d+(c-d)*(1-tx)+(b-d)*(1-tz);
    }

    public static Color EarthColor(int index)=>index switch
    {
        2=>new Color(0.23f,0.34f,0.22f),3=>new Color(0.48f,0.40f,0.29f),
        4=>new Color(0.74f,0.61f,0.40f),5=>new Color(0.59f,0.56f,0.42f),
        6=>new Color(0.58f,0.25f,0.13f),7=>new Color(0.32f,0.38f,0.32f),
        8=>new Color(0.29f,0.32f,0.33f),_=>new Color(0.18f,0.21f,0.27f)
    };

    public static void Build(Transform level,Vector3[] points,float width,int index)
    {
        var root=new GameObject("Terrain and road integration").transform;root.SetParent(level,false);
        Bounds bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);
        Color earth=EarthColor(index);
        var materials=new[]{HarborArtBuilder.Material($"Terrain_{index+1:00}_Earth",earth),HarborArtBuilder.Material($"Terrain_{index+1:00}_Light",earth*1.03f),HarborArtBuilder.Material($"Terrain_{index+1:00}_Dark",earth*0.97f)};
        const float step=4f,padding=115f;
        float minX=Mathf.Floor((bounds.min.x-padding)/step)*step,minZ=Mathf.Floor((bounds.min.z-padding)/step)*step;
        int nx=Mathf.CeilToInt((bounds.max.x+padding-minX)/step),nz=Mathf.CeilToInt((bounds.max.z+padding-minZ)/step);
        var field=new Vector3[(nx+1)*(nz+1)];
        for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
        {
            var p=new Vector3(minX+x*step,0,minZ+z*step);p.y=GroundHeight(index,p,points,width);field[z*(nx+1)+x]=p;
        }
        var vertices=new List<Vector3>();var triangles=new[]{new List<int>(),new List<int>(),new List<int>()};
        void Triangle(Vector3 a,Vector3 b,Vector3 c,int mat){int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);triangles[mat].Add(n);triangles[mat].Add(n+1);triangles[mat].Add(n+2);}
        for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
        {
            var a=field[z*(nx+1)+x];var b=field[z*(nx+1)+x+1];var c=field[(z+1)*(nx+1)+x];var d=field[(z+1)*(nx+1)+x+1];
            int mat=(x*13+z*7)%11<2?1:(x*7+z*3)%13<2?2:0;
            Triangle(a,c,b,mat);Triangle(b,c,d,mat);
        }
        string name=$"Course_{index+1:00}_Terrain";
        var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.subMeshCount=3;
        for(int i=0;i<3;i++)mesh.SetTriangles(triangles[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();
        SaveMesh(name,mesh);
        var terrain=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));terrain.transform.SetParent(root,false);
        terrain.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/"+name+".asset");terrain.GetComponent<MeshCollider>().sharedMesh=terrain.GetComponent<MeshFilter>().sharedMesh;terrain.GetComponent<Renderer>().sharedMaterials=materials;
        BuildVerge(root,points,width,index);
        if(index==2||index==7)BuildMountains(root,bounds,index,earth);
        foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;
    }

    private static void BuildVerge(Transform root,Vector3[] points,float width,int index)
    {
        var stone=HarborArtBuilder.Material($"Course_{index+1:00}_Wall",index==6?new Color(0.52f,0.24f,0.14f):new Color(0.39f,0.40f,0.37f));
        var metal=HarborArtBuilder.Material("Course_Guardrail",new Color(0.63f,0.67f,0.67f),0.25f,0.35f);
        var drain=HarborArtBuilder.Material("Course_Drain",new Color(0.16f,0.18f,0.19f));
        var reflector=HarborArtBuilder.Material("Course_Reflector",new Color(1,0.76f,0.34f));
        float length=0;for(int i=1;i<points.Length;i++)length+=Vector3.Distance(points[i-1],points[i]);
        bool rails=index==2||index==3||index==6||index==7;
        for(float distance=12;distance<length-9;distance+=6)
        {
            HarborArtBuilder.Sample(points,distance,out var p,out var f);var gradeForward=f;f.y=0;f.Normalize();var side=Vector3.Cross(Vector3.up,f);
            for(int s=-1;s<=1;s+=2)
            {
                var pos=p+side*s*(width/2+4.4f);
                // A neighbouring switchback must retain its own full escape corridor.
                if(DistanceToRoad(pos,points,out var closest)<width/2+3.8f)continue;
                if(rails)
                {
                    float ground=GroundHeight(index,pos,points,width);
                    var post=HarborArtBuilder.Box("Guardrail post",root,new Vector3(pos.x,(p.y+0.85f+ground)/2,pos.z),new Vector3(0.16f,p.y+0.85f-ground,0.16f),metal,true);
                    var beam=HarborArtBuilder.Box("Mountain guardrail",root,pos+Vector3.up*0.65f,new Vector3(0.15f,0.26f,5.8f),metal,true);beam.transform.rotation=Quaternion.LookRotation(gradeForward);
                    HarborArtBuilder.Box("Roadside reflector",root,pos+Vector3.up*0.92f,new Vector3(0.19f,0.16f,0.19f),reflector);
                    if(((int)distance/6)%4==0)
                    {
                        var wall=HarborArtBuilder.Box("Retaining wall",root,pos+side*s*0.7f-Vector3.up*0.3f,new Vector3(0.65f,2.6f,5.8f),stone,true);wall.transform.rotation=Quaternion.LookRotation(gradeForward);
                    }
                }
                else if(((int)distance/6)%4==0)
                {
                    var edge=p+side*s*(width/2+2.5f);
                    var cover=HarborArtBuilder.Box("Drainage channel",root,edge+Vector3.up*0.014f,new Vector3(0.55f,0.015f,3.6f),drain);cover.transform.rotation=Quaternion.LookRotation(f);
                    for(int g=-3;g<=3;g++){var grate=HarborArtBuilder.Box("Drain grate",root,edge+f*g*0.45f+Vector3.up*0.024f,new Vector3(0.5f,0.015f,0.055f),metal);grate.transform.rotation=Quaternion.LookRotation(f);}
                }
            }
        }
    }

    private static void BuildMountains(Transform root,Bounds bounds,int index,Color earth)
    {
        var rock=HarborArtBuilder.Material($"Course_{index+1:00}_DistantRock",earth*0.83f);
        var snow=HarborArtBuilder.Material("Alpine_Snow",new Color(0.83f,0.88f,0.88f));
        for(int i=0;i<9;i++)
        {
            float x=bounds.min.x-170+i*(bounds.size.x+340)/8;
            float z=bounds.max.z+130+Mathf.Sin(i*1.6f)*32;
            float floor=HeightAt(index,x,z)-8,height=(index==7?83:55)+(i%3)*17,radius=50+i%3*15;
            string name=$"Course_{index+1:00}_Mountain_{i}";
            var verts=new List<Vector3>();var tris=new List<int>();
            for(int face=0;face<6;face++)
            {
                float a=face*Mathf.PI/3,b=(face+1)*Mathf.PI/3;int v=verts.Count;
                verts.Add(new Vector3(x+Mathf.Cos(a)*radius,floor,z+Mathf.Sin(a)*radius));verts.Add(new Vector3(x+Mathf.Sin(i)*9,floor+height,z));verts.Add(new Vector3(x+Mathf.Cos(b)*radius,floor,z+Mathf.Sin(b)*radius));tris.Add(v);tris.Add(v+1);tris.Add(v+2);
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();SaveMesh(name,mesh);
            var go=new GameObject("Distant mountain silhouette",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/"+name+".asset");go.GetComponent<Renderer>().sharedMaterial=index==7&&i%3==0?snow:rock;
        }
    }

    private static void SaveMesh(string name,Mesh mesh)
    {
        string path="Assets/Models/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);}
    }
}
