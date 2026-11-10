using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared vector wallpaper: blue/teal gradient and quiet motorsport motifs.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class RacingMenuBackground : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        Vector2 Point(float x, float y) => new Vector2(rect.xMin + x * rect.width, rect.yMin + y * rect.height);
        Color Shade(float x, float y)
        {
            Color bottom = new Color(0.10f, 0.43f, 0.73f);
            Color middle = new Color(0.23f, 0.72f, 0.78f);
            Color top = new Color(0.10f, 0.36f, 0.68f);
            Color shade = y < 0.48f ? Color.Lerp(bottom, middle, y / 0.48f) : Color.Lerp(middle, top, (y - 0.48f) / 0.52f);
            return Color.Lerp(shade, new Color(0.04f, 0.24f, 0.48f), Mathf.Pow(Mathf.Abs(x - 0.5f) * 2f, 2) * 0.18f);
        }
        for (int y = 0; y < 24; y++)
        for (int x = 0; x < 24; x++)
        {
            float l = x / 24f, r = (x + 1) / 24f, b = y / 24f, t = (y + 1) / 24f;
            int n = mesh.currentVertCount;
            mesh.AddVert(Point(l,b),Shade(l,b),Vector2.zero);
            mesh.AddVert(Point(l,t),Shade(l,t),Vector2.zero);
            mesh.AddVert(Point(r,t),Shade(r,t),Vector2.zero);
            mesh.AddVert(Point(r,b),Shade(r,b),Vector2.zero);
            mesh.AddTriangle(n,n+1,n+2); mesh.AddTriangle(n,n+2,n+3);
        }
        // Fixed canvas-space spacing keeps symbols round at every aspect ratio.
        float unit = rect.height / 1080f;
        float spacing = 245f * unit;
        int rows = Mathf.CeilToInt(rect.height / spacing) + 1;
        int columns = Mathf.CeilToInt(rect.width / spacing) + 1;
        for (int row = 0; row < rows; row++)
        for (int col = 0; col < columns; col++)
        {
            Vector2 center = new Vector2(rect.xMin + (col + (row % 2) * 0.5f) * spacing, rect.yMin + (row + 0.15f) * spacing);
            float angle = ((row + col) % 2 == 0 ? -24f : 18f) * Mathf.Deg2Rad;
            float size = (48f + (row + col) % 3 * 5f) * unit;
            float nx = (center.x - rect.xMin) / rect.width;
            Color ink = new Color(0.025f,0.24f,0.47f, Mathf.Lerp(0.055f,0.13f,Mathf.Abs(nx-0.5f)*2f));
            Vector2 P(float x,float y) => center + new Vector2(x*Mathf.Cos(angle)-y*Mathf.Sin(angle), x*Mathf.Sin(angle)+y*Mathf.Cos(angle))*size;
            void Quad(float l,float b,float r,float t) => Polygon(mesh,ink,P(l,b),P(l,t),P(r,t),P(r,b));
            void Line(float x,float y,float u,float v,float width)
            {
                Vector2 a=P(x,y), b=P(u,v), d=(b-a).normalized;
                Vector2 side=new Vector2(-d.y,d.x)*size*width*0.5f;
                Polygon(mesh,ink,a-side,a+side,b+side,b-side);
            }
            switch ((row * 2 + col) % 3)
            {
                case 0: // Top-view sports car, with window cutouts.
                    Quad(-0.52f,-0.9f,0.52f,-0.38f); Quad(-0.52f,0.32f,0.52f,1f);
                    Quad(-0.52f,-0.38f,-0.32f,0.32f); Quad(0.32f,-0.38f,0.52f,0.32f);
                    Quad(-0.32f,-0.08f,0.32f,0.13f);
                    Quad(-0.7f,-0.7f,-0.52f,-0.35f); Quad(0.52f,-0.7f,0.7f,-0.35f);
                    Quad(-0.7f,0.45f,-0.52f,0.8f); Quad(0.52f,0.45f,0.7f,0.8f);
                    break;
                case 1: // Tire and five wheel spokes.
                    for(int i=0;i<32;i++)
                    {
                        float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;
                        Polygon(mesh,ink,P(Mathf.Cos(a),Mathf.Sin(a)),P(Mathf.Cos(b),Mathf.Sin(b)),P(Mathf.Cos(b)*0.69f,Mathf.Sin(b)*0.69f),P(Mathf.Cos(a)*0.69f,Mathf.Sin(a)*0.69f));
                    }
                    for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Line(0,0,Mathf.Cos(a)*0.68f,Mathf.Sin(a)*0.68f,0.12f);}
                    break;
                default: // Checkered racing flag.
                    Line(-0.7f,-1,-0.7f,1,0.12f);
                    for(int y=0;y<3;y++)for(int x=0;x<4;x++)
                        if((x+y)%2==0)Quad(-0.65f+x*0.4f,-0.05f+y*0.35f,-0.25f+x*0.4f,0.3f+y*0.35f);
                    break;
            }
        }
    }

    private static void Polygon(VertexHelper mesh, Color color, params Vector2[] points)
    {
        int n=mesh.currentVertCount;
        foreach(var point in points)mesh.AddVert(point,color,Vector2.zero);
        for(int i=1;i<points.Length-1;i++)mesh.AddTriangle(n,n+i,n+i+1);
    }
}
