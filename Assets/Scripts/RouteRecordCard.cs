using UnityEngine;
using UnityEngine.UI;

public sealed class RouteRecordCard : MonoBehaviour
{
    public Image thumbnail;
    public Text positionText, routeNumber;
    public MenuIcon medal;
    public RoundedPanel badge, surface;
    public Sprite[] routePictures;
    public void Refresh(int route,int place,bool completed,bool available)
    {
        if(routePictures!=null&&route<routePictures.Length)thumbnail.sprite=routePictures[route];
        routeNumber.text=$"Route {route+1:00}";
        positionText.text=completed?place.ToString():"—";
        medal.gameObject.SetActive(completed&&place<=3);
        positionText.gameObject.SetActive(true);
        bool podium=completed&&place<=3;
        positionText.rectTransform.anchoredPosition=podium?new Vector2(0,-26):Vector2.zero;
        positionText.rectTransform.sizeDelta=podium?new Vector2(68,24):new Vector2(68,58);
        positionText.fontSize=podium?18:32;
        medal.rectTransform.anchoredPosition=new Vector2(0,10);
        badge.top=completed?place==1?new Color(1,0.79f,0.19f):place==2?new Color(0.78f,0.90f,1):place==3?new Color(0.93f,0.57f,0.30f):new Color(0.12f,0.60f,0.73f):new Color(0.31f,0.54f,0.66f);
        badge.bottom=Color.Lerp(badge.top,new Color(0.06f,0.26f,0.42f),0.25f);badge.SetVerticesDirty();
        surface.top=completed?new Color(0.23f,0.77f,0.92f):available?new Color(0.30f,0.69f,0.82f):new Color(0.33f,0.57f,0.71f);
        surface.bottom=completed?new Color(0.10f,0.53f,0.78f):new Color(0.18f,0.40f,0.59f);surface.SetVerticesDirty();
    }
}
