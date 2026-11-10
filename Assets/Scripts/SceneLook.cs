using UnityEngine;

/// <summary>Level-owned atmosphere. Restores shared render state when leaving the level.</summary>
public sealed class SceneLook : MonoBehaviour
{
    public Material sky;
    public Color fogColor = new Color(0.57f,0.72f,0.77f);
    public float fogStart = 210f, fogEnd = 700f;
    public Color sunColor = new Color(1,0.94f,0.83f);
    private bool applied, oldFog;
    private float oldStart, oldEnd;
    private FogMode oldMode;
    private Color oldFogColor, oldSunColor, oldAmbient;
    private Material oldSky;
    private Camera cameraUsed;
    private CameraClearFlags oldClear;
    private Light sunlight;

    private void OnEnable() => Apply();
    private void OnDisable() => Restore();

    public void Apply(Camera view = null)
    {
        if (applied) return;
        applied=true;
        oldSky=RenderSettings.skybox;oldFog=RenderSettings.fog;oldMode=RenderSettings.fogMode;
        oldStart=RenderSettings.fogStartDistance;oldEnd=RenderSettings.fogEndDistance;oldFogColor=RenderSettings.fogColor;
        oldAmbient=RenderSettings.ambientLight;
        cameraUsed=view!=null?view:Camera.main;
        if(cameraUsed!=null){oldClear=cameraUsed.clearFlags;cameraUsed.clearFlags=CameraClearFlags.Skybox;}
        foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if(light.type==LightType.Directional && light.gameObject.layer!=31){sunlight=light;break;}
        if(sunlight!=null){oldSunColor=sunlight.color;sunlight.color=sunColor;}
        RenderSettings.skybox=sky;
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
        RenderSettings.fogStartDistance=fogStart;RenderSettings.fogEndDistance=fogEnd;RenderSettings.fogColor=fogColor;
        RenderSettings.ambientLight=new Color(0.53f,0.60f,0.65f);
    }
    public void Restore()
    {
        if(!applied)return;
        RenderSettings.skybox=oldSky;RenderSettings.fog=oldFog;RenderSettings.fogMode=oldMode;
        RenderSettings.fogStartDistance=oldStart;RenderSettings.fogEndDistance=oldEnd;RenderSettings.fogColor=oldFogColor;
        RenderSettings.ambientLight=oldAmbient;
        if(cameraUsed!=null)cameraUsed.clearFlags=oldClear;
        if(sunlight!=null)sunlight.color=oldSunColor;
        applied=false;
    }
}
