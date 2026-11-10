using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Eight authored places with readable landmarks, shared low-poly materials and road-safe staging.</summary>
public static class ThemedSceneryBuilder
{
    private static Transform root;
    private static RaceCourse course;
    private static int route;
    private static Bounds bounds;
    private static Material stone, dark, ivory, timber, metal, amber, teal, red;
    private static int meshId;
    private static readonly Dictionary<Color,Material> lettering=new Dictionary<Color,Material>();

    public static void Build(Transform level,Transform scenery,int index)
    {
        root=scenery;course=level.GetComponent<RaceCourse>();route=index;meshId=0;
        bounds=new Bounds(course.centerLine[0],Vector3.zero);foreach(var p in course.centerLine)bounds.Encapsulate(p);
        stone=Mat("Stone",new Color(.49f,.50f,.46f));dark=Mat("Dark",new Color(.065f,.10f,.13f));
        ivory=Mat("Ivory",new Color(.88f,.85f,.71f));timber=Mat("Timber",new Color(.36f,.22f,.12f));
        metal=Mat("Steel",new Color(.23f,.30f,.32f));amber=Mat("SafetyAmber",new Color(.93f,.53f,.11f));
        teal=Mat("Teal",new Color(.08f,.42f,.45f));red=Mat("Terracotta",new Color(.62f,.24f,.13f));
        Atmosphere(level,index);
        switch(index)
        {
            case 2: Forest();break;
            case 3: Quarry();break;
            case 4: Sunset();break;
            case 5: Fishing();break;
            case 6: Canyon();break;
            case 7: Alpine();break;
            case 8: Refinery();break;
            case 9: Night();break;
        }
        foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;
        Debug.Log($"THEMED_ENVIRONMENT route={index+1} renderers={root.GetComponentsInChildren<Renderer>().Length} lights={root.GetComponentsInChildren<Light>().Length}");
    }

    private static Material Mat(string name,Color color)=>HarborArtBuilder.Material("Environment_"+name,color);
    private static Material Glow(string name,Color color,float intensity)
    {
        var mat=Mat(name,color);mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*intensity);EditorUtility.SetDirty(mat);return mat;
    }
    private static float TerrainSurface(Vector3 p)
    {
        // Match the authored four-metre grid triangles exactly, including banks between samples.
        float x=Mathf.Floor(p.x/4)*4,z=Mathf.Floor(p.z/4)*4,tx=(p.x-x)/4,tz=(p.z-z)/4;
        float H(float px,float pz)=>CourseTerrainBuilder.GroundHeight(route,new Vector3(px,0,pz),course.centerLine,CourseDifficultyBuilder.Widths[route]);
        float a=H(x,z),b=H(x+4,z),c=H(x,z+4),d=H(x+4,z+4);
        return tx+tz<=1?a+(b-a)*tx+(c-a)*tz:d+(c-d)*(1-tx)+(b-d)*(1-tz);
    }
    private static Vector3 Ground(Vector3 p){p.y=TerrainSurface(p);return p;}
    private static float RoadDistance(Vector3 p)
    {
        float best=float.MaxValue;p.y=0;
        for(int i=1;i<course.centerLine.Length;i++)
        {
            var a=course.centerLine[i-1];var b=course.centerLine[i];a.y=b.y=0;var d=b-a;
            best=Mathf.Min(best,Vector3.Distance(p,a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/Mathf.Max(d.sqrMagnitude,.001f))));
        }
        return best;
    }
    private static bool Clear(Vector3 p,float radius)=>RoadDistance(p)>course.gateHalfWidth+5+radius;
    private static GameObject Box(string name,Vector3 center,Vector3 size,Material mat,bool collision=true)
    {
        var go=HarborArtBuilder.Box(name,root,center,size,mat,collision);return go;
    }
    private static GameObject Beam(string name,Vector3 a,Vector3 b,float width,Material mat,bool collision=false)
    {
        var go=Box(name,(a+b)*.5f,new Vector3(width,width,Vector3.Distance(a,b)),mat,collision);go.transform.rotation=Quaternion.LookRotation(b-a);return go;
    }
    private static GameObject Cylinder(string name,Vector3 bottom,float radius,float height,Material mat,bool collision=true)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(root,false);
        go.transform.position=bottom+Vector3.up*height*.5f;go.transform.localScale=new Vector3(radius*2,height*.5f,radius*2);go.GetComponent<Renderer>().sharedMaterial=mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        if(collision){var c=go.AddComponent<BoxCollider>();c.size=new Vector3(1,2,1);}
        return go;
    }
    private static GameObject Prop(string pack,string name,Vector3 p,float size,float yaw=0,bool byHeight=true)
    {
        // Actual model bounds are checked, so wider imported models cannot overhang the driving corridor.
        var go=ExternalArtBuilder.Place(pack,name,root,Ground(p),Quaternion.Euler(0,yaw,0),size,byHeight);
        var b=ExternalArtBuilder.BoundsOf(go);
        go.transform.position+=Vector3.up*(TerrainSurface(b.center)-b.min.y);b=ExternalArtBuilder.BoundsOf(go);
        if(!Clear(b.center,Mathf.Sqrt(b.extents.x*b.extents.x+b.extents.z*b.extents.z))) {Object.DestroyImmediate(go);return null;}
        var c=new GameObject("Scenery collision "+name,typeof(BoxCollider));c.transform.SetParent(root,false);c.transform.position=b.center;c.GetComponent<BoxCollider>().size=b.size;
        return go;
    }
    private static void Label(string text,Vector3 p,float yaw,Material backing,Color tint,float width=12)
    {
        var board=Box(text+" sign",p,new Vector3(width,2.4f,.2f),backing,false);board.transform.rotation=Quaternion.Euler(0,yaw,0);
        var go=new GameObject(text);go.transform.SetParent(board.transform,false);go.transform.localPosition=new Vector3(0,0,-.56f);go.transform.localRotation=Quaternion.identity;go.transform.localScale=new Vector3(1/width,1/2.4f,5);
        var label=go.AddComponent<TextMesh>();label.text=text;label.fontSize=64;label.characterSize=.4f;label.anchor=TextAnchor.MiddleCenter;label.color=tint;
        if(route==9)
        {
            if(!lettering.TryGetValue(tint,out var fontMaterial))
            {
                string path="Assets/Materials/Environment_NeonFont_"+ColorUtility.ToHtmlStringRGB(tint)+".mat";
                fontMaterial=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(fontMaterial==null){fontMaterial=new Material(Shader.Find("PolyCar/WorldSignText"));AssetDatabase.CreateAsset(fontMaterial,path);}
                fontMaterial.mainTexture=label.font.material.mainTexture;fontMaterial.color=Color.white;EditorUtility.SetDirty(fontMaterial);lettering[tint]=fontMaterial;
            }
            label.GetComponent<Renderer>().sharedMaterial=fontMaterial;
        }
    }
    private static void Trees(string species,int count,float height,float extra=0)
    {
        float length=0;for(int i=1;i<course.centerLine.Length;i++)length+=Vector3.Distance(course.centerLine[i-1],course.centerLine[i]);
        for(int i=0;i<count;i++)
        {
            HarborArtBuilder.Sample(course.centerLine,length*(i+.37f)/count,out var p,out var forward);
            var side=Vector3.Cross(Vector3.up,forward).normalized*(i%2==0?1:-1);
            p+=side*(course.gateHalfWidth+15+extra+(i%4)*8);
            Prop("Nature",species,p,height*(.77f+(i%5)*.09f),i*137.5f);
        }
    }
    private static void Forest()
    {
        Trees("tree_pineTallA",85,13);Trees("tree_pineTallB",57,16,25);
        var p=Ground(new Vector3(bounds.min.x-34,0,bounds.center.z+4));
        Cabin("Forest lookout",p,new Vector3(14,7,10),timber,red);
        var tower=p+new Vector3(19,0,5);tower=Ground(tower);
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box("Lookout timber leg",tower+new Vector3(x*2,6,z*2),new Vector3(.5f,12,.5f),timber);
        Box("Lookout platform",tower+Vector3.up*12,new Vector3(6,.5f,6),timber);
        Cabin("Ranger lookout cabin",tower+Vector3.up*12.3f,new Vector3(5,3.5f,5),ivory,red);
        Label("PINE RIDGE",p+new Vector3(0,5.8f,-5.2f),0,dark,ivory.color,12);
        for(int i=0;i<14;i++)Prop("Nature","rock_largeA",new Vector3(bounds.min.x-20-i%3*8,0,bounds.min.z+i*10),4+i%3,31*i);
    }
    private static void Quarry()
    {
        var chalk=Mat("QuarryLimestone",new Color(.66f,.61f,.48f));
        for(int tier=0;tier<4;tier++)
        {
            float z=bounds.max.z+25+tier*13;var p=Ground(new Vector3(bounds.center.x,0,z));
            Box("Quarry stepped rock bench "+tier,p+Vector3.up*(tier*7+3.5f),new Vector3(bounds.size.x+70-tier*15,7,16),chalk);
            for(int i=0;i<9;i++)Rock("Quarry cliff face",Ground(new Vector3(bounds.min.x-25+i*31,0,z+6))+Vector3.up*(tier*7),new Vector3(17,8+i%3*3,10),chalk,6);
        }
        for(int i=0;i<22;i++)Prop("Nature",i%2==0?"rock_largeB":"rock_largeC",new Vector3(bounds.max.x+24+i%4*8,0,bounds.min.z+8+i/4*20),3+i%4,41*i);
        var e=Ground(new Vector3(bounds.min.x-26,0,bounds.center.z));Excavator(e);
        var hopper=Ground(new Vector3(bounds.max.x+32,0,bounds.center.z));
        Box("Crusher hopper",hopper+Vector3.up*5,new Vector3(10,6,8),amber);
        for(int i=0;i<3;i++)Beam("Conveyor frame",hopper+new Vector3(0,3+i*.5f,0),hopper+new Vector3(0,1+i*.5f,-25),.35f,metal,true);
        var belt=Box("Stone conveyor belt",hopper+new Vector3(0,3,-12),new Vector3(3,.3f,26),dark);belt.transform.rotation=Quaternion.Euler(-9,0,0);
        Label("QUARRY 04",e+new Vector3(0,8,7),180,dark,amber.color,12);
    }
    private static void Excavator(Vector3 p)
    {
        Box("Quarry excavator",p+Vector3.up*2,new Vector3(8,2,5),amber);
        for(int s=-1;s<=1;s+=2)Box("Excavator track",p+new Vector3(0,.75f,s*3),new Vector3(9,1.5f,1.5f),dark);
        Box("Excavator glazed cab",p+new Vector3(-2,4,0),new Vector3(3,3,3.8f),teal);
        Beam("Excavator boom",p+new Vector3(2,3,0),p+new Vector3(7,10,0),1.2f,amber,true);
        Beam("Excavator stick",p+new Vector3(7,10,0),p+new Vector3(13,3,0),.9f,amber,true);
        Box("Excavator bucket",p+new Vector3(13,2,0),new Vector3(3,2,4),metal);
        Beam("Hydraulic ram",p+new Vector3(3,4,1),p+new Vector3(6,8,1),.25f,metal);
    }
    private static void Water(bool sunset)
    {
        var water=Mat(sunset?"SunsetWater":"VillageWater",sunset?new Color(.20f,.36f,.39f):new Color(.035f,.30f,.43f));
        water.shader=Shader.Find("PolyCar/HarborWater");EditorUtility.SetDirty(water);
        Box("Open sea",new Vector3(bounds.max.x+480,-1.15f,bounds.center.z),new Vector3(900,.12f,1600),water,false);
        var foam=Mat("SeaFoam",new Color(.76f,.85f,.79f));
        for(int i=0;i<14;i++)
        {
            var p=new Vector3(bounds.max.x+47+i%3*12,-1.06f,bounds.min.z-70+i*31);
            Box("Shorebreak",p,new Vector3(.5f,.025f,18+i%4*4),foam,false);
        }
    }
    private static void Sunset()
    {
        Water(true);Trees("tree_palm",56,12,5);
        var p=Ground(new Vector3(bounds.max.x+22,0,bounds.center.z));
        Pavilion("Sunset pavilion",p,14,10,teal);
        for(int i=0;i<8;i++)
        {
            var q=Ground(new Vector3(bounds.max.x+23+i%2*10,0,bounds.min.z+30+i*26));
            Cylinder("Beach parasol pole",q,.1f,3,ivory,false);Roof("Striped beach umbrella",q+Vector3.up*3,5,5,1,i%2==0?amber:teal);
            Box("Beach lounger",q+new Vector3(2,.45f,2),new Vector3(1.1f,.3f,2.2f),ivory);
        }
        Label("SUNSET BAY",p+new Vector3(0,4,-5.2f),0,teal,ivory.color,12);
    }
    private static void Fishing()
    {
        Water(false);Trees("tree_palm",30,10,12);
        for(int i=0;i<9;i++)
        {
            var p=Ground(new Vector3(bounds.max.x+20+(i%2)*14,0,bounds.min.z+10+i*24));
            Cabin("Waterfront fisher house",p,new Vector3(11,5+i%3*1.5f,10),i%3==0?teal:i%3==1?ivory:Mat("VillageOchre",new Color(.84f,.58f,.25f)),red);
        }
        float east=bounds.max.x;var pier=new Vector3(east+49,1,bounds.center.z);
        Box("Fishing pier",pier,new Vector3(65,.5f,7),timber);
        for(int i=0;i<9;i++)for(int s=-1;s<=1;s+=2)Cylinder("Pier piling",pier+new Vector3(-30+i*7,-5,s*2.6f),.4f,5.3f,timber);
        for(int i=0;i<18;i++)Box("Pier deck plank",pier+new Vector3(-31+i*3.6f,.27f,0),new Vector3(3.4f,.08f,7),timber,false);
        for(int i=0;i<4;i++)Boat(new Vector3(east+45+i*15,-.7f,bounds.center.z+(i%2==0?12:-14)),i%2==0?teal:red);
        var lighthouse=new Vector3(east+34,Ground(new Vector3(east+34,0,bounds.max.z+30)).y,bounds.max.z+30);
        Cylinder("Lighthouse base",lighthouse,4,3,stone);Cylinder("Village lighthouse",lighthouse+Vector3.up*3,2.6f,15,ivory);
        Cylinder("Lighthouse red band",lighthouse+Vector3.up*12,2.64f,2,red,false);
        Cylinder("Lighthouse lantern",lighthouse+Vector3.up*18,3.1f,2.5f,Glow("LighthouseGlass",new Color(1,.83f,.4f),1.5f),false);
        Roof("Lighthouse cap",lighthouse+Vector3.up*20.5f,7,7,2,red);
    }
    private static void Boat(Vector3 p,Material paint)
    {
        // Pointed bow and narrower keel make the fishing boats legible from the driving camera.
        var v=new[]{new Vector3(-3,0,-7),new Vector3(3,0,-7),new Vector3(3,0,3),new Vector3(0,0,8),new Vector3(-3,0,3),new Vector3(-2,-1.6f,-6),new Vector3(2,-1.6f,-6),new Vector3(0,-1.6f,6)};
        int[] t={0,4,1,1,4,2,2,4,3,0,1,5,1,6,5,1,2,6,2,7,6,2,3,7,3,4,7,4,5,7,4,0,5,5,6,7};
        MeshObject("Fishing boat hull",p,v,t,paint,true);
        Box("Fishing boat cabin",p+new Vector3(0,1.4f,-2),new Vector3(3.4f,2.8f,4),ivory);
        Box("Fishing boat windshield",p+new Vector3(0,1.8f,.05f),new Vector3(2.7f,1.2f,.08f),teal,false);
        Cylinder("Fishing mast",p+new Vector3(0,0,2),.12f,8,metal,false);
        Beam("Fishing boom",p+new Vector3(0,6,2),p+new Vector3(4,5,2),.15f,metal);
        Box("Fishing net crate",p+new Vector3(0,.6f,4),new Vector3(2,1.2f,2),timber);
    }
    private static void Canyon()
    {
        var sandstone=Mat("CanyonRust",new Color(.63f,.26f,.13f));var pale=Mat("CanyonStrata",new Color(.79f,.42f,.23f));
        for(int i=0;i<22;i++)
        {
            var p=new Vector3(i%2==0?bounds.min.x-25-i%3*9:bounds.max.x+25+i%3*9,0,bounds.min.z-20+i/2*27);
            Rock("Canyon butte",Ground(p),new Vector3(21+i%3*5,30+i%5*7,21),sandstone,7);
            Rock("Canyon pale cap",Ground(p)+Vector3.up*(26+i%5*7),new Vector3(16+i%3*4,5,17),pale,7);
        }
        float len=0;for(int i=1;i<course.centerLine.Length;i++)len+=Vector3.Distance(course.centerLine[i-1],course.centerLine[i]);
        for(int i=0;i<35;i++)
        {
            HarborArtBuilder.Sample(course.centerLine,len*(i+.3f)/35,out var p,out var f);
            p+=Vector3.Cross(Vector3.up,f).normalized*(i%2==0?1:-1)*(course.gateHalfWidth+15);
            if(Clear(p,5))Rock("Canyon roadside spur",Ground(p),new Vector3(6,9+i%4*3,7),sandstone,6);
        }
        Trees("cactus_tall",38,5,4);
        var outcrop=Ground(new Vector3(bounds.center.x,0,bounds.max.z+38));
        Label("RED CANYON",outcrop+Vector3.up*5,0,sandstone,ivory.color,16);
    }
    private static void Alpine()
    {
        var slate=Mat("AlpineSlate",new Color(.33f,.40f,.45f));var snow=Mat("AlpineSnow",new Color(.83f,.89f,.88f));
        for(int i=0;i<9;i++)
        {
            var p=Ground(new Vector3(bounds.min.x-100+i*51,0,bounds.max.z+100+(i%2)*28));
            float height=65+i%3*22;
            Rock("Alpine summit",p,new Vector3(72,height,65),slate,7);
            Rock("Snow cap",p+Vector3.up*(height*.70f),new Vector3(21,height*.30f+1,20),snow,7);
        }
        Trees("tree_pineTallB",66,11,10);
        var lodge=Ground(new Vector3(bounds.min.x-29,0,bounds.center.z));Cabin("Alpine lodge",lodge,new Vector3(18,7,13),ivory,timber);
        Label("RIDGE PASS",lodge+new Vector3(0,5.8f,-6.6f),0,timber,ivory.color,14);
        for(int i=0;i<10;i++)Prop("Nature","rock_largeC",new Vector3(bounds.max.x+24,0,bounds.min.z+i*22),6+i%3*2,i*63);
    }
    private static void Refinery()
    {
        for(int i=0;i<6;i++)
        {
            var p=Ground(new Vector3(bounds.min.x-38-i%2*26,0,bounds.min.z+20+i/2*58));
            Cylinder("Refinery tanks",p,10,15+i%2*3,ivory);
            Cylinder("Tank top rim",p+Vector3.up*(15+i%2*3),10.2f,.5f,metal,false);
            Beam("Tank ladder",p+new Vector3(-10.3f,.2f,0),p+new Vector3(-10.3f,17,0),.25f,metal);
            for(int r=0;r<9;r++)Beam("Tank ladder rung",p+new Vector3(-10.3f,1+r*1.8f,-.5f),p+new Vector3(-10.3f,1+r*1.8f,.5f),.12f,metal);
        }
        for(int i=0;i<9;i++)
        {
            var p=new Vector3(bounds.min.x-15+i*34,0,bounds.max.z+34);
            Prop("Industrial",i%3==0?"chimney-large":"detail-tank-large",p,i%3==0?38:18);
        }
        for(int i=0;i<13;i++)Prop("Industrial","building-"+(char)('a'+i%15),new Vector3(bounds.max.x+32+i%2*24,0,bounds.min.z-20+i*20),14+i%3*3,90);
        float z=bounds.max.z+20;var a=Ground(new Vector3(bounds.min.x-25,0,z));var b=Ground(new Vector3(bounds.max.x+35,0,z));float y=Mathf.Max(a.y,b.y)+10;
        for(int i=0;i<3;i++)
        {
            Beam("Large refinery pipeline",new Vector3(a.x,y+i*1.1f,z),new Vector3(b.x,y+i*1.1f,z),.8f,i%2==0?amber:metal,true);
            for(float x=a.x;x<=b.x;x+=25)Box("Pipe rack support",Ground(new Vector3(x,0,z))+Vector3.up*5,new Vector3(.6f,10,.6f),metal);
        }
        for(int i=0;i<3;i++)Prop("Industrial","shipping-container-c",new Vector3(bounds.max.x+30,0,bounds.min.z-25-i*10),13,0,false);
        Label("FOUNDRY 09",new Vector3(bounds.center.x,y+5,z),0,metal,amber.color,18);
    }
    private static void Night()
    {
        var violet=Glow("NeonViolet",new Color(.55f,.14f,1),2.8f);var cyan=Glow("NeonCyan",new Color(.08f,.83f,1),2.5f);var warm=Glow("WarmWindow",new Color(1,.64f,.26f),1.3f);
        for(int i=0;i<26;i++)
        {
            var p=new Vector3(i%2==0?bounds.min.x-25-i%3*15:bounds.max.x+27+i%3*15,0,bounds.min.z-25+i/2*22);
            NightBuilding(p,i,cyan,violet,warm);
        }
        int lot=30;
        foreach(var p in new[]{new Vector3(-65,0,55),new Vector3(-55,0,-15),new Vector3(-65,0,-90),new Vector3(38,0,70),new Vector3(85,0,12),new Vector3(17,0,-15),new Vector3(70,0,-80),new Vector3(-10,0,88)})
            if(Clear(p,12))NightBuilding(p,lot++,cyan,violet,warm);
        NightStreets(cyan,violet,warm);
    }
    private static void NightBuilding(Vector3 p,int i,Material cyan,Material violet,Material warm)
    {
            var model=Prop("Industrial","building-"+(char)('a'+i%15),p,12+i%4*4,i%2==0?90:-90);
            if(model==null)return;var b=ExternalArtBuilder.BoundsOf(model);var facade=new Vector3(b.center.x,b.center.y,b.min.z-.12f);
            for(int floor=0;floor<3;floor++)for(int col=0;col<4;col++)
                Box("Lit street window",new Vector3(b.min.x+1.5f+col*(b.size.x-3)/3,b.min.y+2+floor*2.4f,b.min.z-.1f),new Vector3(1.1f,1.2f,.1f),(i+col)%4==0?cyan:warm,false);
            if(i%4==0)Label(new[]{"ARCADE","NIGHT RUN","DRIFT CLUB","GARAGE"}[i/4%4],facade+Vector3.up*2,0,dark,i%2==0?cyan.color:violet.color,Mathf.Min(b.size.x,14));
        }
    private static void NightStreets(Material cyan,Material violet,Material warm)
    {
        float length=0;for(int i=1;i<course.centerLine.Length;i++)length+=Vector3.Distance(course.centerLine[i-1],course.centerLine[i]);
        int placed=0;
        for(int i=0;i<32;i++)
        {
            HarborArtBuilder.Sample(course.centerLine,length*(i+.15f)/32,out var p,out var f);
            var side=Vector3.Cross(Vector3.up,f).normalized*(i%2==0?1:-1);p+=side*(course.gateHalfWidth+7);
            if(!Clear(p,.6f))continue;p=Ground(p);
            Cylinder("Neon avenue lamp post",p,.15f,7,metal);
            Beam("Streetlight arm",p+Vector3.up*6.8f,p+Vector3.up*6.8f-side*3,.18f,metal);
            Box("Emissive street luminaire",p+Vector3.up*6.8f-side*3,new Vector3(1.4f,.18f,.7f),warm,false);
            if(placed++%2==0)
            {
                var lamp=new GameObject("Road pool light",typeof(Light));lamp.transform.SetParent(root,false);lamp.transform.position=p+Vector3.up*6.5f-side*3;
                var l=lamp.GetComponent<Light>();l.type=LightType.Point;l.color=i%4==0?new Color(.32f,.65f,1):new Color(1,.68f,.36f);l.intensity=90;l.range=26;l.shadows=LightShadows.None;l.renderMode=LightRenderMode.Auto;
            }
        }
        var landmark=Ground(new Vector3(bounds.center.x,0,bounds.max.z+35));
        Box("Neon avenue",landmark+Vector3.up*12,new Vector3(32,24,16),dark);
        for(int i=0;i<5;i++)Box("Tower neon band",landmark+new Vector3(0,4+i*4,-8.1f),new Vector3(32,.28f,.2f),i%2==0?cyan:violet,false);
        Label("MIDNIGHT",landmark+new Vector3(0,22,-8.25f),0,dark,cyan.color,25);
    }

    private static void Cabin(string name,Vector3 p,Vector3 size,Material walls,Material roof)
    {
        Box(name,p+Vector3.up*size.y*.5f,size,walls);
        Roof(name+" roof",p+Vector3.up*size.y,size.x+1.5f,size.z+1.5f,size.y*.4f,roof);
        Box("Cabin door",p+new Vector3(0,1.8f,-size.z*.5f-.07f),new Vector3(1.8f,3.6f,.12f),timber,false);
        foreach(int s in new[]{-1,1})Box("Cabin window",p+new Vector3(s*size.x*.3f,size.y*.55f,-size.z*.5f-.08f),new Vector3(size.x*.2f,1.8f,.15f),teal,false);
        // Solid skirt buries walls into the slope instead of leaving an unsupported corner.
        Box("Building foundation",p-Vector3.up*2,new Vector3(size.x+.3f,4,size.z+.3f),stone);
    }
    private static void Pavilion(string name,Vector3 p,float width,float depth,Material roof)
    {
        Box(name,p+Vector3.up*.2f,new Vector3(width,.4f,depth),timber);
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Box("Pavilion post",p+new Vector3(x*(width/2-.6f),2.5f,z*(depth/2-.6f)),new Vector3(.35f,5,.35f),ivory);
        Roof(name+" roof",p+Vector3.up*5,width+1,depth+1,2.3f,roof);
    }
    private static void Roof(string name,Vector3 p,float width,float depth,float height,Material mat)
    {
        var v=new[]{new Vector3(-width/2,0,-depth/2),new Vector3(width/2,0,-depth/2),new Vector3(width/2,0,depth/2),new Vector3(-width/2,0,depth/2),new Vector3(0,height,-depth/2),new Vector3(0,height,depth/2)};
        MeshObject(name,p,v,new[]{0,4,1,3,2,5,0,3,5,0,5,4,1,4,5,1,5,2},mat,false);
    }
    private static void Rock(string name,Vector3 p,Vector3 size,Material mat,int sides)
    {
        var v=new List<Vector3>();
        for(int ring=0;ring<3;ring++)for(int i=0;i<sides;i++)
        {
            float a=(i+.11f*ring)*Mathf.PI*2/sides,r=ring==0?.5f:ring==1?.44f:.12f;
            v.Add(new Vector3(Mathf.Cos(a)*size.x*r,ring==0?-1:ring==1?size.y*.72f:size.y,Mathf.Sin(a)*size.z*r));
        }
        v.Add(Vector3.up*size.y*1.03f);var tri=new List<int>();
        for(int r=0;r<2;r++)for(int i=0;i<sides;i++){int a=r*sides+i,b=r*sides+(i+1)%sides;tri.AddRange(new[]{a,a+sides,b,b,a+sides,b+sides});}
        for(int i=0;i<sides;i++)tri.AddRange(new[]{sides*2+i,sides*3,sides*2+(i+1)%sides});
        MeshObject(name,p,v.ToArray(),tri.ToArray(),mat,true);
    }
    private static GameObject MeshObject(string name,Vector3 p,Vector3[] source,int[] sourceTriangles,Material mat,bool collider)
    {
        // Split vertices for flat normals: every rock, roof and hull stays in the same low-poly language.
        var vertices=new Vector3[sourceTriangles.Length];var tri=new int[vertices.Length];
        for(int i=0;i<vertices.Length;i++){vertices[i]=source[sourceTriangles[i]];tri[i]=i;}
        var mesh=new Mesh{name=name,vertices=vertices,triangles=tri};mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=$"Assets/Models/Theme_{route+1:00}_{meshId++:000}.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing==null){AssetDatabase.CreateAsset(mesh,path);existing=mesh;}else{EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);}
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.transform.position=p;go.GetComponent<MeshFilter>().sharedMesh=existing;go.GetComponent<Renderer>().sharedMaterial=mat;
        if(collider)go.AddComponent<MeshCollider>().sharedMesh=existing;return go;
    }

    private static void Atmosphere(Transform level,int index)
    {
        Color[] zenith={new Color(.13f,.31f,.42f),new Color(.30f,.43f,.50f),new Color(.28f,.13f,.32f),new Color(.10f,.43f,.62f),new Color(.29f,.49f,.64f),new Color(.09f,.28f,.48f),new Color(.24f,.31f,.35f),new Color(.007f,.012f,.045f)};
        Color[] horizon={new Color(.65f,.76f,.65f),new Color(.82f,.76f,.59f),new Color(1,.49f,.26f),new Color(.68f,.83f,.85f),new Color(.88f,.64f,.41f),new Color(.73f,.84f,.91f),new Color(.68f,.63f,.49f),new Color(.075f,.09f,.19f)};
        Color[] sun={new Color(1,.93f,.72f),new Color(1,.91f,.72f),new Color(1,.48f,.24f),new Color(1,.95f,.81f),new Color(1,.87f,.67f),new Color(.89f,.95f,1),new Color(1,.79f,.55f),new Color(.38f,.51f,.85f)};
        float[] intensity={1.1f,1.35f,1.15f,1.2f,1.25f,1.2f,.95f,.34f};
        float[] pitch={35,55,12,42,58,32,28,35};
        var look=level.gameObject.AddComponent<SceneLook>();look.Restore();int k=index-2;
        var path=$"Assets/Materials/ThemeSky_{index+1:00}.mat";var sky=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(sky==null){sky=new Material(Shader.Find("PolyCar/ThemeSky"));AssetDatabase.CreateAsset(sky,path);}
        sky.SetColor("_Zenith",zenith[k]);sky.SetColor("_Horizon",horizon[k]);sky.SetColor("_CloudColor",index==9?new Color(.08f,.10f,.20f):index==4?new Color(1,.64f,.43f):new Color(.93f,.93f,.88f));
        sky.SetFloat("_CloudStrength",index==9?.12f:.35f);sky.SetFloat("_Stars",index==9?.8f:0);
        var rotation=Quaternion.Euler(pitch[k],-40+k*17,0);sky.SetVector("_SunDirection",-(rotation*Vector3.forward));sky.SetColor("_SunColor",sun[k]);EditorUtility.SetDirty(sky);
        look.sky=sky;look.sunColor=sun[k];look.sunIntensity=intensity[k];look.overrideSunDirection=true;look.sunEuler=rotation.eulerAngles;
        look.ambientColor=index==9?new Color(.22f,.26f,.37f):index==4?new Color(.50f,.34f,.38f):new Color(.48f,.53f,.55f);
        look.fogColor=horizon[k];look.fogStart=index==9?120:index==2?140:230;look.fogEnd=index==9?430:index==2?530:850;
    }
}
