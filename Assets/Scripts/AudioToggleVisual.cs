using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public sealed class AudioToggleVisual : MonoBehaviour
{
    public RoundedPanel surface;
    public MenuIcon icon;
    public GameObject muteSlash;
    private void Awake()=>Bind();
    private void OnEnable()=>Refresh();
    public void Bind()
    {
        var toggle=GetComponent<Toggle>();toggle.onValueChanged.RemoveListener(OnChanged);toggle.onValueChanged.AddListener(OnChanged);Refresh();
    }
    private void OnChanged(bool value)=>Refresh();
    public void Refresh()
    {
        if(surface==null||icon==null||muteSlash==null)return;
        bool on=GetComponent<Toggle>().isOn;
        surface.top=on?new Color(0.32f,0.85f,1f):new Color(0.47f,0.60f,0.72f);
        surface.bottom=on?new Color(0.04f,0.36f,0.80f):new Color(0.23f,0.36f,0.51f);
        surface.SetVerticesDirty();icon.color=on?Color.white:new Color(0.78f,0.85f,0.92f);muteSlash.SetActive(!on);
    }
}
