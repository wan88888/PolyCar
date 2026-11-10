using UnityEngine;
using UnityEngine.UI;

public sealed class PlayButtonPulse : MonoBehaviour
{
    public CanvasGroup glow;
    private void Update()=>Sample(Time.unscaledTime);
    public void Sample(float time)
    {
        float wave=(Mathf.Sin(time*Mathf.PI*2/2.8f)+1)*0.5f;
        transform.localScale=Vector3.one*Mathf.Lerp(1f,1.035f,wave);
        if(glow!=null)glow.alpha=Mathf.Lerp(0.12f,0.32f,wave);
    }
    private void OnDisable(){transform.localScale=Vector3.one;if(glow!=null)glow.alpha=0.12f;}
}
