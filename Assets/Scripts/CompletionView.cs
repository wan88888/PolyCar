using UnityEngine;
using UnityEngine.UI;

/// <summary>Presentation only: rewards are credited by LevelManager, never by this view.</summary>
public sealed class CompletionView : MonoBehaviour
{
    public Text heading, route, medalName, timeValue, driftValue, rewardValue, bestBadge, primaryLabel;
    public RoundedPanel medalSurface;
    public MenuIcon medalIcon, primaryIcon;
    public Button primaryButton, routesButton;

    public void Show(LevelManager.LevelCompletionData result)
    {
        heading.text=result.HasNextRoute?"Route Complete":"All Routes Complete";
        route.text=UiCopy.Title(result.RouteName);
        medalName.text=UiCopy.Title(result.Medal);
        float seconds=Mathf.Max(0,result.Seconds);
        timeValue.text=$"{(int)(seconds/60):00}:{seconds%60:00.00}";
        driftValue.text=Mathf.Max(0,result.DriftScore).ToString("N0");
        rewardValue.text=$"+{Mathf.Max(0,result.EarnedCoins):N0}";
        bestBadge.text=result.NewBestDrift?"New Drift Best":string.Empty;
        bestBadge.gameObject.SetActive(result.NewBestDrift);
        Color medal=result.Medal=="GOLD"?new Color(1,0.78f,0.22f):result.Medal=="SILVER"?new Color(0.70f,0.86f,0.97f):new Color(0.91f,0.57f,0.31f);
        medalSurface.top=Color.Lerp(medal,Color.white,0.3f);medalSurface.bottom=medal;medalSurface.SetVerticesDirty();
        medalIcon.color=new Color(0.14f,0.30f,0.43f);
        primaryLabel.text=result.HasNextRoute?"Next Route":"Routes";
        primaryIcon.icon=result.HasNextRoute?MenuIcon.Kind.Next:MenuIcon.Kind.Routes;primaryIcon.SetVerticesDirty();
        primaryButton.interactable=true;
        routesButton.gameObject.SetActive(result.HasNextRoute);
    }
}
