using UnityEngine;
using UnityEngine.UI;

/// <summary>Colored low-poly illustrations matching the game's vehicles.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MenuTabEmblem : MaskableGraphic
{
    public MenuPageTab.Page page;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();Rect rect=GetPixelAdjustedRect();float s=Mathf.Min(rect.width,rect.height)/2.2f;Vector2 center=rect.center;
        Vector2 P(float x,float y)=>center+new Vector2(x,y)*s;
        void Poly(Color c,params Vector2[] p){int n=mesh.currentVertCount;foreach(var v in p)mesh.AddVert(v,c*color,Vector2.zero);for(int i=1;i<p.Length-1;i++)mesh.AddTriangle(n,n+i,n+i+1);}
        void Q(float x,float y,float u,float v,Color c)=>Poly(c,P(x,y),P(x,v),P(u,v),P(u,y));
        Color blue=new Color(0.08f,0.40f,0.75f),light=new Color(0.37f,0.83f,1),cream=new Color(1,0.94f,0.70f),gold=new Color(1,0.67f,0.13f),orange=new Color(0.98f,0.37f,0.13f),dark=new Color(0.035f,0.17f,0.31f);
        Q(-0.88f,-0.93f,0.92f,-0.77f,new Color(0.015f,0.09f,0.19f,0.38f));
        if(page==MenuPageTab.Page.Rank)
        {
            Q(-0.96f,-0.79f,0.96f,-0.62f,blue);Q(-0.96f,-0.64f,0.96f,-0.54f,light);
            Q(-0.83f,-0.55f,-0.30f,0.22f,new Color(0.49f,0.75f,0.90f));Q(-0.83f,0.22f,-0.30f,0.32f,new Color(0.78f,0.95f,1));
            Q(0.34f,-0.55f,0.84f,0.06f,new Color(0.95f,0.45f,0.21f));Q(0.34f,0.06f,0.84f,0.16f,new Color(1,0.72f,0.38f));
            Q(-0.28f,-0.55f,0.31f,0.64f,gold);Q(-0.28f,0.64f,0.31f,0.76f,cream);Q(0.21f,-0.55f,0.31f,0.64f,new Color(0.93f,0.48f,0.05f));
            Q(-0.035f,-0.26f,0.075f,0.39f,cream);Q(-0.14f,0.26f,0.075f,0.39f,cream);
        }
        else
        {
            Q(-0.77f,-0.76f,0.78f,0.44f,cream);Q(0.6f,-0.76f,0.78f,0.44f,new Color(0.89f,0.73f,0.35f));
            Q(-0.88f,-0.82f,0.89f,-0.65f,gold);Q(-0.88f,-0.71f,0.89f,-0.62f,cream);
            if(page==MenuPageTab.Page.Home)
            {
                Poly(orange,P(-1,0.38f),P(0,1.07f),P(1,0.38f),P(0.85f,0.20f),P(0,0.79f),P(-0.85f,0.20f));
                Poly(gold,P(-1,0.38f),P(0,1.07f),P(0,0.93f),P(-0.88f,0.28f));
                Q(-0.30f,-0.62f,0.29f,0.12f,blue);Q(-0.21f,-0.62f,0.20f,0.02f,light);Q(0.12f,-0.33f,0.18f,-0.23f,cream);
                Q(-0.16f,0.31f,0.16f,0.63f,blue);Q(-0.11f,0.36f,0.11f,0.58f,light);
            }
            else
            {
                Q(-0.6f,-0.58f,0.6f,0.15f,blue);Q(-0.51f,-0.49f,0.51f,0.08f,dark);
                for(int i=0;i<5;i++)Poly(i%2==0?orange:cream,P(-0.94f+i*0.376f,0.22f),P(-0.76f+i*0.304f,0.76f),P(-0.456f+i*0.304f,0.76f),P(-0.564f+i*0.376f,0.22f));
                Q(-0.43f,-0.4f,0.43f,-0.13f,light);Poly(light,P(-0.32f,-0.13f),P(-0.19f,0.02f),P(0.22f,0.02f),P(0.35f,-0.13f));
                Q(-0.34f,-0.48f,-0.18f,-0.36f,dark);Q(0.18f,-0.48f,0.34f,-0.36f,dark);
            }
        }
    }
}
