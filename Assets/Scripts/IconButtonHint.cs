using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Action metadata only. Menus deliberately show no hover or focus tooltip.</summary>
public sealed class IconButtonHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler, IPointerClickHandler, ISubmitHandler
{
    public string description;
    public void OnPointerEnter(PointerEventData e) { }
    public void OnPointerExit(PointerEventData e) { }
    public void OnSelect(BaseEventData e) { }
    public void OnDeselect(BaseEventData e) { }
    public void OnPointerClick(PointerEventData e) { }
    public void OnSubmit(BaseEventData e) { }
}
