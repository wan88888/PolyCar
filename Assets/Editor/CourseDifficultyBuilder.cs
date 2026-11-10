using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Ten explicit learning steps. Geometry and goals are authored together.</summary>
public static class CourseDifficultyBuilder
{
    public static readonly float[] Widths={12,11,10.5f,10,9.5f,9,8.5f,8,7.6f,7.2f};
    public static readonly int[] Obstacles={0,2,4,6,8,10,12,14,16,18};
    public static readonly int[] Silver={100,150,220,300,400,500,650,800,1000,1200};
    public static readonly int[] Gold={300,450,650,850,1100,1350,1600,1900,2200,2600};
    public static readonly float[] GoldSeconds={55,34,62,65,62,95,60,105,92,102};
    public static readonly string[] Skills={"Learn the line","Link gentle S bends","Brake for switchbacks","Vary corner entry","Rhythm through slalom","Hold a long slide","Read tight combinations","Control hairpin exits","Plan around obstacles","Combine every skill"};
    public static readonly string[] Names={"Harbor Loop","S-Curve Sprint","Switchback Climb","Quarry Figure Eight","Sunset Slalom","Dockside Spiral","Canyon Needles","Ridge Hairpins","Industrial Gauntlet","Midnight Pinball"};
    private static Vector3[] Points(params float[] xy){var p=new Vector3[xy.Length/2];for(int i=0;i<p.Length;i++)p[i]=new Vector3(xy[2*i],0,xy[2*i+1]);return p;}
    public static Vector3[] Route(int index)
    {
        if(index==0)return HarborArtBuilder.HarborRoute();
        Vector3[] knots=index switch
        {
            1=>Points(-100,-60,-45,-60,5,-30,-25,10,10,50,65,50,100,85),
            2=>Points(-90,-70,70,-70,90,-45,70,-20,-65,-20,-90,5,-65,30,80,30,100,60),
            3=>Points(-100,-70,-45,-85,10,-55,60,-85,100,-45,60,-5,10,-25,-45,15,-90,50,-40,85,20,55,85,80),
            4=>Points(-50,-150,-75,-100,40,-60,-40,-10,40,40,-40,90,20,140,80,140),
            5=>Points(-120,-100,20,-100,110,-45,110,60,30,110,-70,100,-105,30,-75,-35,10,-55,55,-15,40,40,-5,55,-40,20,-5,-10),
            6=>Points(-110,-130,-50,-100,-85,-50,-10,-15,-45,35,35,80,80,30,40,-15,105,-55,135,-110),
            7=>Points(-110,-100,80,-100,105,-75,80,-50,-75,-50,-100,-25,-75,0,80,0,105,25,80,50,-75,50,-100,75,-75,100,100,100),
            8=>Points(-115,-100,-50,-100,-20,-50,-85,-20,-95,45,-35,95,30,60,0,5,65,-35,115,0,130,65,65,115,0,100,55,35,135,-80),
            _=>Points(-120,-100,-65,-40,-110,25,-50,100,0,55,-35,0,15,-70,70,-40,35,20,90,85,130,25,80,-20,125,-100,25,-120,-40,-85,-120,-100)
        };
        var route=RoundCorners(knots, new[]{0f,32,30,30,30,30,30,30,32,34}[index]);
        if(index>=2)for(int i=0;i<route.Length;i++)route[i].y=CourseTerrainBuilder.HeightAt(index,route[i].x,route[i].z);
        return route;
    }
    // Quadratic fillets retain the intended turns but remove instantaneous heading changes.
    public static Vector3[] RoundCorners(Vector3[] knots,float cut)
    {
        bool closed=Vector3.Distance(knots[0],knots[knots.Length-1])<0.1f;
        int count=closed?knots.Length-1:knots.Length;
        var result=new List<Vector3>();
        void Line(Vector3 end){if(result.Count==0){result.Add(end);return;}var start=result[result.Count-1];int n=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(start,end)/1.5f));for(int j=1;j<=n;j++)result.Add(Vector3.Lerp(start,end,j/(float)n));}
        for(int i=0;i<count;i++)
        {
            if(!closed&&(i==0||i==count-1)){Line(knots[i]);continue;}
            var before=knots[(i+count-1)%count];var here=knots[i];var after=knots[(i+1)%count];
            float offset=Mathf.Min(cut,Vector3.Distance(before,here)*0.43f,Vector3.Distance(here,after)*0.43f);
            var start=here+(before-here).normalized*offset;var end=here+(after-here).normalized*offset;
            Line(start);int n=Mathf.Max(12,Mathf.CeilToInt(offset*2/1.2f));
            for(int j=1;j<=n;j++){float t=j/(float)n;result.Add((1-t)*(1-t)*start+2*(1-t)*t*here+t*t*end);}
        }
        if(closed)Line(result[0]);
        return result.ToArray();
    }
    public static void Configure(RaceCourse course,int index)
    {
        course.laps=1;course.goldTime=GoldSeconds[index];course.silverScore=Silver[index];course.goldScore=Gold[index];course.difficulty=index+1;course.skillFocus=Skills[index];
    }
    public static float[] ObstacleDistances(Vector3[] points,int count,int gates)
    {
        float length=0;for(int i=1;i<points.Length;i++)length+=Vector3.Distance(points[i-1],points[i]);
        var positions=new List<float>();
        for(int n=0;n<count;n++)
        {
            float desired=length*(n+1f)/(count+1f),best=0,cost=float.MaxValue;
            for(float d=24;d<length-20;d+=2)
            {
                bool occupied=false;foreach(float old in positions)if(Mathf.Abs(old-d)<15)occupied=true;
                if(occupied)continue;
                float gateDistance=float.MaxValue;for(int g=1;g<=gates;g++)gateDistance=Mathf.Min(gateDistance,Mathf.Abs(length*g/gates-d));
                if(gateDistance<7)continue;
                HarborArtBuilder.Sample(points,d-9,out var a,out var af);HarborArtBuilder.Sample(points,d+9,out var b,out var bf);
                float turn=Vector3.Angle(af,bf);float candidate=turn*3+Mathf.Abs(d-desired)*0.22f;
                if(candidate<cost){cost=candidate;best=d;}
            }
            if(best==0)throw new InvalidOperationException("Not enough visible obstacle slots");
            positions.Add(best);
        }
        positions.Sort();return positions.ToArray();
    }

    public static void BuildRoad(Transform parent,Vector3[] points,float width,int index)
    {
        var concrete=HarborArtBuilder.Material("Course_Shoulder",new Color(0.42f,0.46f,0.46f));
        var asphalt=HarborArtBuilder.Material("Course_Asphalt",new Color(0.10f,0.14f,0.17f));
        var paint=HarborArtBuilder.Material("Course_Edge",new Color(0.88f,0.85f,0.72f));
        var offsets=new Vector3[points.Length];
        bool closed=Vector3.Distance(points[0],points[points.Length-1])<0.1f;
        for(int i=0;i<points.Length;i++)
        {
            int prev=i==0?(closed?points.Length-2:0):i-1,next=i==points.Length-1?(closed?1:i):i+1;
            offsets[i]=Vector3.Cross(Vector3.up,points[next]-points[prev]).normalized;
        }
        void Strip(string suffix,float left,float right,float y,Material mat,bool collision,float? rightY=null,bool terrainLeft=false,bool terrainRight=false)
        {
            string name=$"Course_{index+1:00}_{suffix}";var v=new Vector3[points.Length*2];var tri=new int[(points.Length-1)*6];var uv=new Vector2[v.Length];float distance=0;
            for(int i=0;i<points.Length;i++)
            {
                if(i>0)distance+=Vector3.Distance(points[i-1],points[i]);
                v[2*i]=points[i]+offsets[i]*left+Vector3.up*y;v[2*i+1]=points[i]+offsets[i]*right+Vector3.up*(rightY??y);
                // Offset hairpin edges do not share their centerline's height; join the actual terrain triangle.
                if(terrainLeft)v[2*i].y=CourseTerrainBuilder.SurfaceHeight(index,v[2*i],points,width)+0.002f;
                if(terrainRight)v[2*i+1].y=CourseTerrainBuilder.SurfaceHeight(index,v[2*i+1],points,width)+0.002f;
                uv[2*i]=new Vector2(0,distance/12);uv[2*i+1]=new Vector2(1,distance/12);
                if(i==points.Length-1)continue;int k=i*6,n=i*2;tri[k]=n;tri[k+1]=n+2;tri[k+2]=n+1;tri[k+3]=n+1;tri[k+4]=n+2;tri[k+5]=n+3;
            }
            var mesh=new Mesh{name=name,vertices=v,triangles=tri,uv=uv};mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path="Assets/Models/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);}
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);go.GetComponent<Renderer>().sharedMaterial=mat;
            if(collision)go.AddComponent<MeshCollider>().sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;
        }
        // A 5 mm return lip matches the original off-road collider and cannot trap the chassis at crossings.
        float shoulderY=index>=8?0.038f:0.035f;
        Strip("Shoulder",-width/2-3,width/2+3,shoulderY,concrete,true);
        if(index>=2&&index<8)
        {
            // Explicit ramps eliminate the former 21.5 cm vertical lip beyond the shoulder.
            var verge=HarborArtBuilder.Material($"Terrain_{index+1:00}_Earth",CourseTerrainBuilder.EarthColor(index));
            Strip("LeftVergeRamp",-width/2-5,-width/2-3,-0.18f,verge,true,shoulderY,true,false);
            Strip("RightVergeRamp",width/2+3,width/2+5,shoulderY,verge,true,-0.18f,false,true);
        }
        Strip("Road",-width/2,width/2,0.04f,asphalt,true);
        Strip("LeftEdge",-width/2+0.12f,-width/2+0.25f,0.055f,paint,false);
        Strip("RightEdge",width/2-0.25f,width/2-0.12f,0.055f,paint,false);
        if(index>=2){CourseTerrainBuilder.Build(parent.parent,points,width,index);return;}
        Bounds bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);
        var ground=new GameObject("Off-road collision",typeof(BoxCollider));ground.transform.SetParent(parent,false);ground.transform.position=new Vector3(bounds.center.x,-0.265f,bounds.center.z);ground.GetComponent<BoxCollider>().size=new Vector3(bounds.size.x+120,0.6f,bounds.size.z+120);
    }
}
