using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class RacingMenuThemeBuilder
{
    public static void Apply(Transform canvas)
    {
        foreach(string pageName in new[]{"HomePanel","ShopPanel","RankPanel","LevelSelectPanel"})
        {
            Transform page=canvas.Find(pageName);
            if(page==null)continue;
            var existing=page.Find("Racing wallpaper");
            if(existing!=null)Object.DestroyImmediate(existing.gameObject);
            var go=new GameObject("Racing wallpaper",typeof(RectTransform),typeof(RacingMenuBackground));
            go.transform.SetParent(page,false);go.transform.SetAsFirstSibling();
            var rect=(RectTransform)go.transform;
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            go.GetComponent<RacingMenuBackground>().raycastTarget=false;
            var rail=page.Find(pageName.Replace("Panel","TopRail"));
            if(rail!=null)rail.GetComponent<Image>().color=new Color(0.04f,0.19f,0.36f,0.3f);
        }
        SetText(canvas,"RankPanel/RecordSummary",new Color(0.04f,0.18f,0.30f));
        SetText(canvas,"ShopPanel/ShopStatusText",new Color(0.04f,0.18f,0.30f));
        var nav=canvas.Find("HomePanel/BottomNav");
        if(nav!=null && nav.GetComponent<Image>()!=null)nav.GetComponent<Image>().color=new Color(0.04f,0.20f,0.35f,0.88f);
    }
    private static void SetText(Transform root,string path,Color color)
    {
        var item=root.Find(path);if(item!=null)item.GetComponent<Text>().color=color;
    }
    [MenuItem("Tools/PolyCar/Apply racing menu background")]
    public static void UpdateScene()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/DriftPrototype.unity");
        Apply(Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include).transform);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("RACING_MENU_THEME_OK pages=Home,Shop,Rank");
    }
}
