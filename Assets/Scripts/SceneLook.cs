using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Level-owned atmosphere; every shared lighting property is restored on exit.</summary>
public sealed class SceneLook : MonoBehaviour
{
    public Material sky;
    public Color fogColor = new Color(0.57f,0.72f,0.77f);
    public float fogStart = 210f, fogEnd = 700f;
    public Color sunColor = new Color(1,0.94f,0.83f);
    public Color ambientColor = new Color(0.53f,0.60f,0.65f);
    public float sunIntensity = 1.7f;
    public bool overrideSunDirection;
    public Vector3 sunEuler = new Vector3(45,-30,0);
    private bool applied, oldFog;
    private float oldStart, oldEnd, oldSunIntensity;
    private FogMode oldMode;
    private AmbientMode oldAmbientMode;
    private Color oldFogColor, oldSunColor, oldAmbient;
    private Quaternion oldSunRotation;
    private Material oldSky;
    private Camera cameraUsed;
    private CameraClearFlags oldClear;
    private Light sunlight;

    private void OnEnable() => Apply();
    private void OnDisable() => Restore();

    public void Apply(Camera view = null)
    {
        if (!applied)
        {
            applied=true;
            oldSky=RenderSettings.skybox;oldFog=RenderSettings.fog;oldMode=RenderSettings.fogMode;
            oldStart=RenderSettings.fogStartDistance;oldEnd=RenderSettings.fogEndDistance;oldFogColor=RenderSettings.fogColor;
            oldAmbient=RenderSettings.ambientLight;oldAmbientMode=RenderSettings.ambientMode;
            sunlight = IsWorldSun(RenderSettings.sun) ? RenderSettings.sun : null;
            if (sunlight == null)
                foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.InstanceID))
                    if(IsWorldSun(light)){sunlight=light;break;}
            if(sunlight!=null){oldSunColor=sunlight.color;oldSunIntensity=sunlight.intensity;oldSunRotation=sunlight.transform.rotation;}
        }
        // Captures may provide another camera after OnEnable applied the same environment.
        var requested=view!=null?view:Camera.main;
        if(requested!=cameraUsed)
        {
            if(cameraUsed!=null)cameraUsed.clearFlags=oldClear;
            cameraUsed=requested;
            if(cameraUsed!=null)oldClear=cameraUsed.clearFlags;
        }
        if(cameraUsed!=null)cameraUsed.clearFlags=CameraClearFlags.Skybox;
        if(sunlight!=null)
        {
            sunlight.color=sunColor;sunlight.intensity=sunIntensity;
            if(overrideSunDirection)sunlight.transform.rotation=Quaternion.Euler(sunEuler);
        }
        RenderSettings.skybox=sky;
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
        RenderSettings.fogStartDistance=fogStart;RenderSettings.fogEndDistance=fogEnd;RenderSettings.fogColor=fogColor;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=ambientColor;
    }
    public void Restore()
    {
        if(!applied)return;
        RenderSettings.skybox=oldSky;RenderSettings.fog=oldFog;RenderSettings.fogMode=oldMode;
        RenderSettings.fogStartDistance=oldStart;RenderSettings.fogEndDistance=oldEnd;RenderSettings.fogColor=oldFogColor;
        RenderSettings.ambientMode=oldAmbientMode;RenderSettings.ambientLight=oldAmbient;
        if(cameraUsed!=null)cameraUsed.clearFlags=oldClear;
        if(sunlight!=null){sunlight.color=oldSunColor;sunlight.intensity=oldSunIntensity;sunlight.transform.rotation=oldSunRotation;}
        cameraUsed=null;sunlight=null;applied=false;
    }

    private static bool IsWorldSun(Light light) => light != null && light.isActiveAndEnabled
        && light.type == LightType.Directional && light.gameObject.layer != 31
        && (light.cullingMask & ~(1 << 31)) != 0;
}
