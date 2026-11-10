using UnityEngine;
using UnityEngine.UI;

/// <summary>Scalable rounded UI surface with a vertical gradient and a subtle raised edge.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RoundedPanel : MaskableGraphic
{
    public Color top = Color.white, bottom = Color.white, border = Color.white;
    public float radius = 24, borderWidth = 3, shadowDepth = 5;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        if(shadowDepth>0)Fill(mesh,new Rect(rect.x,rect.y-shadowDepth,rect.width,rect.height),radius,new Color(0.02f,0.12f,0.27f,0.32f),new Color(0.02f,0.12f,0.27f,0.32f));
        Fill(mesh,rect,radius,border,border);
        Rect inner=new Rect(rect.x+borderWidth,rect.y+borderWidth,Mathf.Max(0,rect.width-borderWidth*2),Mathf.Max(0,rect.height-borderWidth*2));
        Fill(mesh,inner,Mathf.Max(0,radius-borderWidth),top,bottom);
    }
    private void Fill(VertexHelper mesh,Rect rect,float rounding,Color upper,Color lower)
    {
        float r=Mathf.Min(rounding,Mathf.Min(rect.width,rect.height)/2);
        int start=mesh.currentVertCount;
        void Add(Vector2 p)=>mesh.AddVert(p,Color.Lerp(lower,upper,Mathf.InverseLerp(rect.yMin,rect.yMax,p.y))*color,Vector2.zero);
        Add(rect.center);
        for(int corner=0;corner<4;corner++)
        {
            Vector2 c=new Vector2(corner==0||corner==3?rect.xMax-r:rect.xMin+r,corner<2?rect.yMax-r:rect.yMin+r);
            for(int step=0;step<=10;step++)
            {
                float a=(corner*90+step*9)*Mathf.Deg2Rad;
                Add(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r);
            }
        }
        for(int i=1;i<=44;i++)mesh.AddTriangle(start,start+i,start+(i%44)+1);
    }
}
