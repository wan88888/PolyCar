using UnityEngine;
using UnityEngine.UI;

/// <summary>Resolution-independent menu symbols, with no dependency on font glyphs.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MenuIcon : MaskableGraphic
{
    public enum Kind { Coin, Settings, Home, Play, Pause, Replay, Next, Garage, Trophy, Routes, Close, Gift, Music, Sound, Check, Lock, ChevronLeft, ChevronRight, Info }
    public Kind icon;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect=GetPixelAdjustedRect();
        Vector2 center=rect.center;
        float radius=Mathf.Min(rect.width,rect.height)*0.46f;
        if(icon==Kind.Coin)
        {
            Disk(mesh,center,radius,new Color(1f,0.72f,0.20f));
            Disk(mesh,center,radius*0.78f,new Color(0.73f,0.41f,0.055f));
            Disk(mesh,center,radius*0.65f,new Color(1f,0.78f,0.28f));
            Quad(mesh,center+new Vector2(-radius*0.10f,-radius*0.40f),center+new Vector2(radius*0.10f,radius*0.40f),new Color(1f,0.94f,0.64f));
        }
        else if(icon==Kind.Settings)
        {
            for(int i=0;i<64;i++)
            {
                float a=i*Mathf.PI*2/64, b=(i+1)*Mathf.PI*2/64;
                float ra=(i%8>=2&&i%8<=5)?radius:radius*0.78f;
                int j=(i+1)%64;float rb=(j%8>=2&&j%8<=5)?radius:radius*0.78f;
                int first=mesh.currentVertCount;
                Add(mesh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*ra,color);
                Add(mesh,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*rb,color);
                Add(mesh,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius*0.35f,color);
                Add(mesh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*0.35f,color);
                mesh.AddTriangle(first,first+1,first+2);mesh.AddTriangle(first,first+2,first+3);
            }
        }
        else
        {
            Vector2 P(float x,float y)=>center+new Vector2(x,y)*radius;
            void L(float x,float y,float u,float v)=>Line(mesh,P(x,y),P(u,v),radius*0.13f,color);
            void Q(float x,float y,float u,float v)=>Quad(mesh,P(x,y),P(u,v),color);
            void T(Vector2 a,Vector2 b,Vector2 c){int n=mesh.currentVertCount;Add(mesh,a,color);Add(mesh,b,color);Add(mesh,c,color);mesh.AddTriangle(n,n+1,n+2);}
            void Arc(float start,float end,float r){for(int i=0;i<24;i++){float a=Mathf.Lerp(start,end,i/24f)*Mathf.Deg2Rad,b=Mathf.Lerp(start,end,(i+1)/24f)*Mathf.Deg2Rad;L(Mathf.Cos(a)*r,Mathf.Sin(a)*r,Mathf.Cos(b)*r,Mathf.Sin(b)*r);}}
            switch(icon)
            {
                case Kind.Info: Arc(0,360,0.85f);L(0,-0.48f,0,0.15f);Disk(mesh,P(0,0.48f),radius*0.10f,color);break;
                case Kind.ChevronLeft: L(0.4f,0.75f,-0.4f,0);L(-0.4f,0,0.4f,-0.75f);break;
                case Kind.ChevronRight: L(-0.4f,0.75f,0.4f,0);L(0.4f,0,-0.4f,-0.75f);break;
                case Kind.Home:
                    L(-0.9f,0.1f,0,0.85f);L(0,0.85f,0.9f,0.1f);L(-0.65f,0.2f,-0.65f,-0.75f);L(0.65f,0.2f,0.65f,-0.75f);L(-0.65f,-0.75f,0.65f,-0.75f);L(-0.2f,-0.75f,-0.2f,-0.15f);L(-0.2f,-0.15f,0.2f,-0.15f);L(0.2f,-0.15f,0.2f,-0.75f);break;
                case Kind.Play: T(P(-0.5f,-0.8f),P(0.85f,0),P(-0.5f,0.8f));break;
                case Kind.Pause: Q(-0.65f,-0.8f,-0.2f,0.8f);Q(0.2f,-0.8f,0.65f,0.8f);break;
                case Kind.Next: T(P(-0.7f,-0.8f),P(0.5f,0),P(-0.7f,0.8f));Q(0.65f,-0.8f,0.85f,0.8f);break;
                case Kind.Replay: Arc(45,325,0.72f);T(P(0.75f,0.7f),P(0.15f,0.9f),P(0.35f,0.3f));break;
                case Kind.Close: L(-0.65f,-0.65f,0.65f,0.65f);L(-0.65f,0.65f,0.65f,-0.65f);break;
                case Kind.Check: L(-0.8f,-0.05f,-0.2f,-0.6f);L(-0.2f,-0.6f,0.85f,0.65f);break;
                case Kind.Lock: Q(-0.65f,-0.75f,0.65f,0.15f);Arc(0,180,0.45f);L(-0.45f,0,-0.45f,0.3f);L(0.45f,0,0.45f,0.3f);Disk(mesh,P(0,-0.2f),radius*0.13f,new Color(0.08f,0.10f,0.13f));Quad(mesh,P(-0.06f,-0.48f),P(0.06f,-0.2f),new Color(0.08f,0.10f,0.13f));break;
                case Kind.Routes:
                    for(int y=0;y<2;y++)for(int x=0;x<2;x++)Q(-0.8f+x*0.95f,-0.8f+y*0.95f,-0.15f+x*0.95f,-0.15f+y*0.95f);break;
                case Kind.Garage:
                    L(-0.85f,0.15f,-0.5f,0.65f);L(-0.5f,0.65f,0.5f,0.65f);L(0.5f,0.65f,0.85f,0.15f);Q(-0.85f,-0.45f,0.85f,0.15f);Q(-0.65f,-0.72f,-0.35f,-0.45f);Q(0.35f,-0.72f,0.65f,-0.45f);break;
                case Kind.Trophy:
                    L(-0.5f,0.8f,0.5f,0.8f);L(-0.5f,0.8f,-0.4f,0);L(0.5f,0.8f,0.4f,0);L(-0.4f,0,0,-0.3f);L(0.4f,0,0,-0.3f);L(0,-0.3f,0,-0.75f);L(-0.5f,-0.8f,0.5f,-0.8f);L(-0.5f,0.55f,-0.85f,0.55f);L(-0.85f,0.55f,-0.75f,0.05f);L(-0.75f,0.05f,-0.4f,-0.05f);L(0.5f,0.55f,0.85f,0.55f);L(0.85f,0.55f,0.75f,0.05f);L(0.75f,0.05f,0.4f,-0.05f);break;
                case Kind.Gift:
                    L(-0.7f,0.25f,0.7f,0.25f);L(-0.7f,0.25f,-0.7f,-0.75f);L(0.7f,0.25f,0.7f,-0.75f);L(-0.7f,-0.75f,0.7f,-0.75f);L(0,0.65f,0,-0.75f);L(-0.8f,0.55f,0.8f,0.55f);L(0,0.55f,-0.45f,0.95f);L(0,0.55f,0.45f,0.95f);break;
                case Kind.Music:
                    L(-0.25f,-0.5f,-0.25f,0.65f);L(-0.25f,0.65f,0.65f,0.85f);L(0.65f,0.85f,0.65f,-0.3f);Disk(mesh,P(-0.48f,-0.5f),radius*0.27f,color);Disk(mesh,P(0.42f,-0.3f),radius*0.27f,color);break;
                case Kind.Sound:
                    Q(-0.85f,-0.3f,-0.45f,0.3f);T(P(-0.45f,-0.3f),P(0.1f,-0.7f),P(0.1f,0.7f));T(P(-0.45f,-0.3f),P(0.1f,0.7f),P(-0.45f,0.3f));Arc(-55,55,0.8f);break;
            }
        }

    }

    public static MenuIcon Attach(Transform parent,Kind kind,float size=42f)
    {
        var go=new GameObject("ActionIcon",typeof(RectTransform),typeof(MenuIcon));go.transform.SetParent(parent,false);
        var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(0.5f,0.5f);rect.sizeDelta=new Vector2(size,size);
        var graphic=go.GetComponent<MenuIcon>();graphic.icon=kind;graphic.color=new Color(0.9f,0.96f,1f);graphic.raycastTarget=false;return graphic;
    }
    private static void Line(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color tint)
    {
        Vector2 delta=(b-a).normalized;Vector2 side=new Vector2(-delta.y,delta.x)*width*0.5f;int first=mesh.currentVertCount;
        Add(mesh,a-side,tint);Add(mesh,a+side,tint);Add(mesh,b+side,tint);Add(mesh,b-side,tint);mesh.AddTriangle(first,first+1,first+2);mesh.AddTriangle(first,first+2,first+3);
    }
    private static void Add(VertexHelper mesh,Vector2 point,Color tint)=>mesh.AddVert(point,tint,Vector2.zero);
    private static void Disk(VertexHelper mesh,Vector2 center,float radius,Color tint)
    {
        int first=mesh.currentVertCount;Add(mesh,center,tint);
        for(int i=0;i<=48;i++)
        {
            float angle=i*Mathf.PI*2/48;
            Add(mesh,center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,tint);
            if(i>0)mesh.AddTriangle(first,first+i,first+i+1);
        }
    }
    private static void Quad(VertexHelper mesh,Vector2 min,Vector2 max,Color tint)
    {
        int first=mesh.currentVertCount;
        Add(mesh,min,tint);Add(mesh,new Vector2(min.x,max.y),tint);Add(mesh,max,tint);Add(mesh,new Vector2(max.x,min.y),tint);
        mesh.AddTriangle(first,first+1,first+2);mesh.AddTriangle(first,first+2,first+3);
    }
}
