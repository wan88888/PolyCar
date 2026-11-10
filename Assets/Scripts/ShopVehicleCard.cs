using UnityEngine;
using UnityEngine.UI;

public sealed class ShopVehicleCard : MonoBehaviour
{
    public Button purchaseButton, detailsButton;
    public GameObject stats, selectedBadge;
    public RectTransform vehicle;
    public Text actionLabel;
    public MenuIcon coin, detailIcon;
    public RoundedPanel actionSurface, frame;
    public bool Expanded => stats!=null && stats.activeSelf;
    private void Awake()=>Bind();
    private void OnEnable()=>SetExpanded(false);
    public void Bind()
    {
        purchaseButton.onClick.RemoveListener(Purchase);purchaseButton.onClick.AddListener(Purchase);
        detailsButton.onClick.RemoveListener(Toggle);detailsButton.onClick.AddListener(Toggle);
        SetExpanded(false);
    }
    private void Purchase()=>GetComponent<Button>().onClick.Invoke();
    public void Toggle()=>SetExpanded(!Expanded);
    public void SetExpanded(bool expanded)
    {
        if(stats==null)return;
        stats.SetActive(expanded);
        vehicle.anchoredPosition=expanded?new Vector2(-143,12):new Vector2(0,12);
        vehicle.sizeDelta=expanded?new Vector2(265,164):new Vector2(420,192);
        if(detailIcon!=null){detailIcon.icon=expanded?MenuIcon.Kind.Close:MenuIcon.Kind.Info;detailIcon.SetVerticesDirty();}
        var hint=detailsButton.GetComponent<IconButtonHint>();if(hint!=null)hint.description=expanded?"Hide performance":"Show performance";
    }
    public void Refresh(string name,int cost,bool unlocked,bool selected)
    {
        selectedBadge.SetActive(selected);
        coin.gameObject.SetActive(!unlocked);
        actionLabel.text=selected?"Equipped":unlocked?"Select":cost.ToString("N0");
        actionLabel.color=Color.white;
        actionLabel.rectTransform.anchoredPosition=new Vector2(unlocked?0:18,0);
        purchaseButton.interactable=!selected;
        actionSurface.top=selected?new Color(0.44f,0.57f,0.66f):new Color(0.39f,0.82f,0.19f);
        actionSurface.bottom=selected?new Color(0.28f,0.41f,0.53f):new Color(0.15f,0.57f,0.12f);
        actionSurface.SetVerticesDirty();
        frame.border=selected?new Color(1f,0.78f,0.25f):new Color(0.30f,0.66f,1f);
        frame.SetVerticesDirty();
        var hint=purchaseButton.GetComponent<IconButtonHint>();
        if(hint!=null)hint.description=selected?name+" equipped":unlocked?"Select "+name:$"Unlock {name} for {cost} coins";
    }
}
