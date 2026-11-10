using UnityEngine;
using UnityEngine.UI;

public sealed class MenuPageTab : MonoBehaviour
{
    public enum Page { Shop, Home, Rank }
    public Page destination;
    private void Awake()=>Bind();
    public void Bind()
    {
        var button=GetComponent<Button>();
        button.onClick.RemoveListener(Navigate);button.onClick.AddListener(Navigate);
    }
    private void Navigate()
    {
        var menu=FindFirstObjectByType<MainMenuUI>();if(menu==null)return;
        switch(destination){case Page.Shop:menu.ShowShop();break;case Page.Home:menu.ShowHome();break;case Page.Rank:menu.ShowRank();break;}
    }
}
