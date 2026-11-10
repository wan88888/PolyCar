using System;
using System.IO;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
public static class MenuPolishReview
{
 static object Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(obj,args);
 static void Capture(Canvas canvas,string name,int w,int h)
 {
  var camera=Camera.main;var target=new RenderTexture(w,h,24){antiAliasing=4};camera.targetTexture=target;
  canvas.GetComponent<CanvasScaler>().SendMessage("Update");Canvas.ForceUpdateCanvases();
  bool bottom=name.StartsWith("Rank-bottom");
  var scroll=canvas.transform.Find("RankPanel/RecordScroll").GetComponent<ScrollRect>();
  if(bottom){scroll.Rebuild(CanvasUpdate.PostLayout);scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();}
  for(int i=0;i<4;i++)camera.Render();
  if(bottom)
  {
   var last=scroll.content.Find("RecordRow_10") as RectTransform;
   var corners=new Vector3[4];last.GetWorldCorners(corners);
   Vector3 middle=scroll.viewport.InverseTransformPoint((corners[0]+corners[2])/2);
   if(!scroll.viewport.rect.Contains(middle))throw new Exception("Last record is not visible at bottom: "+middle+" viewport="+scroll.viewport.rect);
  }
  var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(w,h,TextureFormat.RGB24,false);
  texture.ReadPixels(new Rect(0,0,w,h),0,0);texture.Apply();File.WriteAllBytes("/tmp/polycar-polish-review/"+name+".png",texture.EncodeToPNG());
  RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(texture);Object.DestroyImmediate(target);
 }
 public static void Run()
 {
  MenuPolishBuilder.UpdateScene();
  HarborPreviewCapture.Run();
  var canvas=Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);canvas.gameObject.SetActive(true);
  var menu=Object.FindFirstObjectByType<MainMenuUI>();
  typeof(MainMenuUI).GetField("transitionFadeGroup",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(menu,null);
  Call(menu,"RegisterButtons");
  foreach(var card in canvas.GetComponentsInChildren<ShopVehicleCard>(true))card.Bind();
  foreach(var tab in canvas.GetComponentsInChildren<MenuPageTab>(true))tab.Bind();
  Directory.CreateDirectory("/tmp/polycar-polish-review");
  foreach(string name in new[]{"Home","Shop","Rank"})
  {
   Call(menu,"Show"+name);
   Capture(canvas,name+"-16x9",1280,720);Capture(canvas,name+"-4x3",1024,768);
  }
  menu.ShowShop();
  int money=SaveManager.TotalCoins;
  var first=canvas.transform.Find("ShopPanel/ShopCarButton_01").GetComponent<ShopVehicleCard>();
  var second=canvas.transform.Find("ShopPanel/ShopCarButton_02").GetComponent<ShopVehicleCard>();
  if(first.Expanded||second.Expanded)throw new Exception("Performance must start collapsed");
  second.detailsButton.onClick.Invoke();
  if(!second.Expanded||SaveManager.TotalCoins!=money)throw new Exception("Details toggle failed or changed wallet");
  Capture(canvas,"Shop-expanded-16x9",1280,720);Capture(canvas,"Shop-expanded-4x3",1024,768);
  second.detailsButton.onClick.Invoke();if(second.Expanded)throw new Exception("Performance did not collapse");
  menu.ShowHome();menu.ShowShop();if(second.Expanded)throw new Exception("Shop entry must collapse details");
  foreach(string from in new[]{"ShopPanel","RankPanel"})
  foreach(string target in new[]{"Shop","Home","Rank"})
  {
   if(from=="ShopPanel")menu.ShowShop();else menu.ShowRank();
   canvas.transform.Find(from+"/BottomNav/"+target+"TabButton").GetComponent<Button>().onClick.Invoke();
   if(!canvas.transform.Find(target+"Panel").gameObject.activeSelf)throw new Exception("Tab navigation failed: "+from+" -> "+target);
  }
  menu.ShowHome();var pulse=canvas.transform.Find("HomePanel/LevelButton").GetComponent<PlayButtonPulse>();
  Time.timeScale=0;pulse.Sample(0.7f);float high=pulse.transform.localScale.x;pulse.Sample(2.1f);float low=pulse.transform.localScale.x;
  if(high<=low||high>1.04f||low<0.99f)throw new Exception("Unscaled breathing range failed");
  pulse.transform.localScale=Vector3.one;
  menu.ShowRank();var scroll=canvas.transform.Find("RankPanel/RecordScroll").GetComponent<ScrollRect>();
  Capture(canvas,"Rank-before-scroll-16x9",1280,720);
  Canvas.ForceUpdateCanvases();scroll.Rebuild(CanvasUpdate.PostLayout);
  scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();Capture(canvas,"Rank-bottom-16x9",1280,720);
  if(scroll.content.anchoredPosition.y<=0)throw new Exception("Rank cannot scroll to last records");
  scroll.verticalNormalizedPosition=1;
  CheckSettingsAndCopy(canvas,menu);
  CheckPurchasesAndRank(canvas,menu);
  Debug.Log("MENU_POLISH_REVIEW_OK pages=3 ratios=2 detailsToggle=true noPurchaseOnDetails=true tabs=6 pulseUnscaled=true rankScroll=true");
 }
 static void CheckSettingsAndCopy(Canvas canvas,MainMenuUI menu)
 {
  var home=canvas.transform.Find("HomePanel");var shop=canvas.transform.Find("ShopPanel");var rank=canvas.transform.Find("RankPanel");
  if(home.Find("GameTitle").GetComponent<Text>().text!="Poly Drift")throw new Exception("Home title casing");
  string[] names={"Sport Sedan","Drift Hatch","Rally SUV","Track Racer"};
  for(int i=0;i<4;i++)if(shop.Find($"ShopCarButton_{i+1:00}/ShopCarTitle_{i+1:00}").GetComponent<Text>().text!=names[i])throw new Exception("Vehicle title casing");
  if(rank.Find("RecordDescription")!=null||rank.Find("RecordSummary").gameObject.activeSelf)throw new Exception("Redundant rank copy remains");
  foreach(var hint in canvas.GetComponentsInChildren<IconButtonHint>(true)){hint.OnPointerEnter(null);hint.OnSelect(null);}
  if(canvas.transform.Find("Icon hint")!=null)throw new Exception("Tooltip created");
  string[] keys={"PolyCar_MusicEnabled","PolyCar_SoundEnabled"};bool[] had={PlayerPrefs.HasKey(keys[0]),PlayerPrefs.HasKey(keys[1])};int[] saved={PlayerPrefs.GetInt(keys[0]),PlayerPrefs.GetInt(keys[1])};
  try
  {
   Call(Object.FindFirstObjectByType<AudioManager>(),"Awake");
   var popup=canvas.transform.Find("SettingsPopup");
   var music=popup.Find("MusicToggle").GetComponent<Toggle>();var sound=popup.Find("SoundToggle").GetComponent<Toggle>();
   foreach(var visual in popup.GetComponentsInChildren<AudioToggleVisual>(true))visual.Bind();
   menu.ShowHome();home.Find("SettingsButton").GetComponent<Button>().onClick.Invoke();
   if(!popup.gameObject.activeSelf||!home.gameObject.activeSelf)throw new Exception("Settings must overlay home");
   if(home.GetComponent<CanvasGroup>().interactable)throw new Exception("Settings must block underlying buttons");
   music.isOn=true;sound.isOn=true;
   Capture(canvas,"Settings-on-16x9",1280,720);Capture(canvas,"Settings-on-4x3",1024,768);
   music.isOn=false;sound.isOn=false;
   if(SaveManager.MusicEnabled||SaveManager.SoundEnabled||!music.GetComponent<AudioToggleVisual>().muteSlash.activeSelf||!sound.GetComponent<AudioToggleVisual>().muteSlash.activeSelf)throw new Exception("Mute state or persistence failed");
   Capture(canvas,"Settings-muted-16x9",1280,720);
   popup.Find("CloseButton").GetComponent<Button>().onClick.Invoke();if(popup.gameObject.activeSelf||!home.GetComponent<CanvasGroup>().interactable)throw new Exception("Settings close failed");
   Call(menu,"LoadSettings");home.Find("SettingsButton").GetComponent<Button>().onClick.Invoke();
   if(music.isOn||sound.isOn)throw new Exception("Settings not restored on reopen");
   music.isOn=true;sound.isOn=true;
   if(!SaveManager.MusicEnabled||!SaveManager.SoundEnabled)throw new Exception("Audio enable failed");
   menu.HandleShortcut(UnityEngine.InputSystem.Key.Escape);if(popup.gameObject.activeSelf)throw new Exception("Escape close failed");
   Debug.Log("SETTINGS_COPY_OK noHoverOrFocusHints=true titles=true rankCopyRemoved=true modal=true audioPersistence=true closeAndEscape=true");
  }
  finally
  {
   for(int i=0;i<2;i++)if(had[i])PlayerPrefs.SetInt(keys[i],saved[i]);else PlayerPrefs.DeleteKey(keys[i]);PlayerPrefs.Save();Call(menu,"LoadSettings");
  }
 }
 static void ClearPreviewCar(CarGarage garage)
 {
  var field=typeof(CarGarage).GetField("currentCar",BindingFlags.Instance|BindingFlags.NonPublic);
  var car=(GameObject)field.GetValue(garage);if(car!=null)Object.DestroyImmediate(car);field.SetValue(garage,null);
 }
 static void CheckPurchasesAndRank(Canvas canvas,MainMenuUI menu)
 {
  var keys=new System.Collections.Generic.Dictionary<string,(bool,int)>();
  string[] names={"PolyCar_TotalCoins","PolyCar_Unlocked_starter","PolyCar_Unlocked_drift","PolyCar_Unlocked_rally","PolyCar_Unlocked_speed","PolyCar_HighestUnlockedRoute","PolyCar_RouteCompleted_0","PolyCar_RouteCompleted_1","PolyCar_RouteCompleted_2","PolyCar_v2_BestRouteDrift_0","PolyCar_v2_BestRouteDrift_1","PolyCar_v2_BestRouteDrift_2"};
  foreach(var key in names)keys[key]=(PlayerPrefs.HasKey(key),PlayerPrefs.GetInt(key));
  bool hadSelected=PlayerPrefs.HasKey("PolyCar_SelectedCar");string selected=PlayerPrefs.GetString("PolyCar_SelectedCar");
  var times=new System.Collections.Generic.Dictionary<string,(bool,float)>();
  for(int i=0;i<3;i++){string key="PolyCar_v2_BestTime_"+i;times[key]=(PlayerPrefs.HasKey(key),PlayerPrefs.GetFloat(key));}
  try
  {
   var garage=Object.FindFirstObjectByType<CarGarage>();
   PlayerPrefs.SetInt("PolyCar_TotalCoins",0);PlayerPrefs.SetString("PolyCar_SelectedCar","starter");
   foreach(string id in new[]{"drift","rally","speed"})PlayerPrefs.SetInt("PolyCar_Unlocked_"+id,0);
   Call(garage,"Awake");menu.ShowShop();
   var shop=canvas.transform.Find("ShopPanel");
   var drift=shop.Find("ShopCarButton_02").GetComponent<ShopVehicleCard>();
   for(int i=1;i<4;i++)
   {
    shop.Find($"ShopCarButton_{i+1:00}").GetComponent<ShopVehicleCard>().purchaseButton.onClick.Invoke();
    if(shop.Find("ShopStatusText").GetComponent<Text>().text!="Insufficient Coins"||garage.IsCarUnlocked(i)||SaveManager.TotalCoins!=0)throw new Exception("Inconsistent insufficient coins message");
   }
   if(garage.IsCarUnlocked(1)||SaveManager.TotalCoins!=0||shop.Find("ShopStatusText").GetComponent<Text>().text!="Insufficient Coins")throw new Exception("Insufficient funds failed");
   Capture(canvas,"Shop-insufficient-funds",1280,720);
   PlayerPrefs.SetInt("PolyCar_TotalCoins",300);
   int balance=300;
   for(int i=1;i<4;i++)
   {
    ClearPreviewCar(garage);
    shop.Find($"ShopCarButton_{i+1:00}").GetComponent<ShopVehicleCard>().purchaseButton.onClick.Invoke();
    balance-=garage.GetCarUnlockCost(i);
    if(garage.SelectedIndex!=i||!garage.IsCarUnlocked(i)||SaveManager.TotalCoins!=balance)throw new Exception("Purchase failed: "+i);
    if(shop.Find("ShopWallet/Balance").GetComponent<Text>().text!=balance.ToString("N0"))throw new Exception("Wallet stale");
   }
   ClearPreviewCar(garage);drift.purchaseButton.onClick.Invoke();if(garage.SelectedIndex!=1||SaveManager.TotalCoins!=balance)throw new Exception("Owned car selection charged coins");
   for(int i=0;i<3;i++){PlayerPrefs.SetInt("PolyCar_RouteCompleted_"+i,1);PlayerPrefs.SetFloat("PolyCar_v2_BestTime_"+i,i==0?38:i==1?40:36);PlayerPrefs.SetInt("PolyCar_v2_BestRouteDrift_"+i,i==0?900:1500);}
   PlayerPrefs.SetInt("PolyCar_HighestUnlockedRoute",3);menu.ShowRank();
   var content=canvas.transform.Find("RankPanel/RecordScroll/Viewport/Content");
   string[] expected={"Route 03","Route 02","Route 01"};
   for(int i=0;i<3;i++)
   {
    var row=content.Find($"RecordRow_{i+1:00}").GetComponent<RouteRecordCard>();
    if(row.routeNumber.text!=expected[i]||!row.medal.gameObject.activeSelf||row.positionText.text!=(i+1).ToString()||row.thumbnail.sprite==null)throw new Exception("Ranking or medal mapping failed");
   }
   if(content.Find("RecordRow_04").GetComponent<RouteRecordCard>().medal.gameObject.activeSelf)throw new Exception("Unplayed route received a medal");
   Capture(canvas,"Rank-with-records-16x9",1280,720);Capture(canvas,"Rank-with-records-4x3",1024,768);
   Debug.Log("MENU_PURCHASE_RANK_OK insufficientFunds=true purchases=3 ownedSelectionFree=true rankSort=driftThenTime medalsOnlyForRecords=true");
  }
  finally
  {
   foreach(var pair in keys)if(pair.Value.Item1)PlayerPrefs.SetInt(pair.Key,pair.Value.Item2);else PlayerPrefs.DeleteKey(pair.Key);
   foreach(var pair in times)if(pair.Value.Item1)PlayerPrefs.SetFloat(pair.Key,pair.Value.Item2);else PlayerPrefs.DeleteKey(pair.Key);
   if(hadSelected)PlayerPrefs.SetString("PolyCar_SelectedCar",selected);else PlayerPrefs.DeleteKey("PolyCar_SelectedCar");
   PlayerPrefs.Save();
  }
 }

}
