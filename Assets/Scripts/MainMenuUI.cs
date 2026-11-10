using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MainMenuUI : MonoBehaviour
{
    private bool completionHasNextRoute;
    [Header("Panels")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject rankPanel;
    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private GameObject gameplayHudPanel;
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private GameObject settingsPopup;
    [SerializeField] private GameObject spinPopup;
    [SerializeField] private GameObject dailyPopup;

    [Header("Home")]
    [SerializeField] private Text coinBalanceText;
    [SerializeField] private Text homeLevelText;
    [SerializeField] private Button coinButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button spinButton;
    [SerializeField] private Button dailyButton;
    [SerializeField] private Button levelButton;
    [SerializeField] private Button gameplayHomeButton;
    [SerializeField] private Button homeTabButton;
    [SerializeField] private Button shopTabButton;
    [SerializeField] private Button rankTabButton;

    [Header("Level Select")]
    [SerializeField] private Button[] levelButtons;
    [SerializeField] private Text[] levelTitleTexts;
    [SerializeField] private Text[] levelMetaTexts;
    [SerializeField] private Text levelSelectStatusText;
    [SerializeField] private Button levelSelectBackButton;
    [SerializeField] private Button previousRoutePageButton;
    [SerializeField] private Button nextRoutePageButton;
    [SerializeField] private Text routePageText;
    private int routePage;
    private const int RoutesPerPage = 2;

    [Header("Completion")]
    [SerializeField] private Text completionTitleText;
    [SerializeField] private Text completionStatsText;
    [SerializeField] private Text completionBestText;
    [SerializeField] private Button completionNextButton;
    [SerializeField] private Button completionReplayButton;
    [SerializeField] private Button completionHomeButton;
    [SerializeField] private Button completionLevelSelectButton;

    [Header("Shop")]
    [SerializeField] private Button[] shopCarButtons;
    [SerializeField] private Text[] shopCarTitleTexts;
    [SerializeField] private Text[] shopCarCostTexts;
    [SerializeField] private Text shopStatusText;
    [SerializeField] private Button shopBackButton;

    [Header("Rank")]
    [SerializeField] private Text rankListText;
    [SerializeField] private Button rankBackButton;

    [Header("Popups")]
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle soundToggle;
    [SerializeField] private Text settingsStatusText;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Text spinStatusText;
    [SerializeField] private Button spinRewardButton;
    [SerializeField] private Button closeSpinButton;
    [SerializeField] private Text dailyStatusText;
    [SerializeField] private Button claimDailyButton;
    [SerializeField] private Button closeDailyButton;

    [Header("References")]
    [SerializeField] private CarGarage carGarage;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private CanvasGroup transitionFadeGroup;

    private GameObject pausePanel;
    private bool paused;
    private bool isGameplayActive;
    private Coroutine transitionRoutine;

    private void Awake()
    {
        if (carGarage == null)
        {
            carGarage = FindFirstObjectByType<CarGarage>();
        }

        if (levelManager == null)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }

        if (audioManager == null)
        {
            audioManager = AudioManager.Instance != null ? AudioManager.Instance : FindFirstObjectByType<AudioManager>();
        }

        if (cameraFollow == null)
        {
            cameraFollow = FindFirstObjectByType<CameraFollow>();
        }
    }

    private void Start()
    {
        CreatePausePanel();
        RegisterButtons();
        if (levelManager != null)
        {
            levelManager.LevelCompleted += ShowCompletion;
        }

        LoadSettings();
        ShowHome();
    }

    private void OnDestroy()
    {
        if (levelManager != null)
        {
            levelManager.LevelCompleted -= ShowCompletion;
        }
    }

    private void RegisterButtons()
    {
        AddClick(coinButton, ShowShop);
        AddClick(shopTabButton, ShowShop);
        AddClick(homeTabButton, ShowHome);
        AddClick(rankTabButton, ShowRank);
        AddClick(settingsButton, ShowSettingsPopup);
        AddClick(spinButton, ShowSpinPopup);
        AddClick(dailyButton, ShowDailyPopup);
        AddClick(levelButton, ShowLevelSelect);
        AddClick(gameplayHomeButton, TogglePause);
        AddClick(levelSelectBackButton, ShowHome);
        AddClick(previousRoutePageButton, () => ChangeRoutePage(-1));
        AddClick(nextRoutePageButton, () => ChangeRoutePage(1));
        AddClick(shopBackButton, ShowHome);
        AddClick(rankBackButton, ShowHome);
        AddClick(closeSettingsButton, ClosePopups);
        AddClick(closeSpinButton, ClosePopups);
        AddClick(closeDailyButton, ClosePopups);
        AddClick(spinRewardButton, ClaimSpinReward);
        AddClick(claimDailyButton, ClaimDailyReward);
        AddClick(completionNextButton, StartNextRouteFromCompletion);
        AddClick(completionReplayButton, ReplayRouteFromCompletion);
        AddClick(completionHomeButton, ShowHome);
        AddClick(completionLevelSelectButton, ShowLevelSelect);

        if (musicToggle != null)
        {
            musicToggle.onValueChanged.AddListener(SetMusicEnabled);
        }

        if (soundToggle != null)
        {
            soundToggle.onValueChanged.AddListener(SetSoundEnabled);
        }

        if (levelButtons != null)
        {
            for (int i = 0; i < levelButtons.Length; i++)
            {
                int capturedIndex = i;
                AddClick(levelButtons[i], () => TryStartLevel(capturedIndex));
            }
        }

        if (shopCarButtons == null)
        {
            return;
        }

        for (int i = 0; i < shopCarButtons.Length; i++)
        {
            int capturedIndex = i;
            AddClick(shopCarButtons[i], () => TrySelectOrUnlockCar(capturedIndex));
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.rKey.wasPressedThisFrame) HandleShortcut(Key.R);
        else if (keyboard.nKey.wasPressedThisFrame) HandleShortcut(Key.N);
        else if (keyboard.escapeKey.wasPressedThisFrame) HandleShortcut(Key.Escape);
    }

    // One command path for keyboard input and regression checks of menu/run transitions.
    public void HandleShortcut(Key key)
    {
        if (IsPanelOpen(completionPanel))
        {
            if (key == Key.R) { ReplayRouteFromCompletion(); return; }
            if (key == Key.N && completionNextButton != null && completionNextButton.interactable)
            { StartNextRouteFromCompletion(); return; }
        }
        if (isGameplayActive && !paused && key == Key.R) levelManager?.RecoverCar();
        if (key != Key.Escape) return;
        if (paused) { TogglePause(); return; }

        if (IsAnyPopupOpen())
        {
            ClosePopups();
            return;
        }

        if (IsPanelOpen(completionPanel))
        {
            ShowLevelSelect();
            return;
        }

        if (isGameplayActive)
        {
            TogglePause();
            return;
        }

        if (!IsPanelOpen(homePanel))
        {
            ShowHome();
        }
    }

    private static void Focus(Button button)
    {
        if(EventSystem.current!=null && button!=null && button.gameObject.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    public void ShowHome()
    {
        if (levelManager != null && levelManager.RunInProgress) levelManager.RestartCurrentLevel();
        paused = false;
        SetPanel(pausePanel, false);
        Time.timeScale = 0f;
        isGameplayActive = false;
        SetPanel(homePanel, true);
        SetPanel(shopPanel, false);
        SetPanel(rankPanel, false);
        SetPanel(levelSelectPanel, false);
        SetPanel(gameplayHudPanel, false);
        SetPanel(completionPanel, false);
        SetMenuCamera(true);
        ClosePopups();
        RefreshAll();
        PlayTransition();
        Focus(levelButton);
    }

    public void ShowShop()
    {
        Time.timeScale = 0f;
        isGameplayActive = false;
        SetPanel(homePanel, false);
        SetPanel(shopPanel, true);
        SetPanel(rankPanel, false);
        SetPanel(levelSelectPanel, false);
        SetPanel(gameplayHudPanel, false);
        SetPanel(completionPanel, false);
        SetMenuCamera(true);
        ClosePopups();
        RefreshAll();
        PlayTransition();
        Focus(shopPanel.transform.Find("BottomNav/ShopTabButton")?.GetComponent<Button>());
    }

    public void ShowRank()
    {
        Time.timeScale = 0f;
        isGameplayActive = false;
        SetPanel(homePanel, false);
        SetPanel(shopPanel, false);
        SetPanel(rankPanel, true);
        SetPanel(levelSelectPanel, false);
        SetPanel(gameplayHudPanel, false);
        SetPanel(completionPanel, false);
        SetMenuCamera(true);
        ClosePopups();
        RefreshAll();
        PlayTransition();
        var recordScroll = rankPanel.GetComponentInChildren<ScrollRect>();
        if (recordScroll != null) { recordScroll.StopMovement(); recordScroll.verticalNormalizedPosition = 1f; }
        Focus(rankPanel.transform.Find("BottomNav/RankTabButton")?.GetComponent<Button>());
    }

    public void ShowLevelSelect()
    {
        routePage = (levelManager != null ? levelManager.CurrentLevelIndex : 0) / RoutesPerPage;
        Time.timeScale = 0f;
        isGameplayActive = false;
        SetPanel(homePanel, false);
        SetPanel(shopPanel, false);
        SetPanel(rankPanel, false);
        SetPanel(levelSelectPanel, true);
        SetPanel(gameplayHudPanel, false);
        SetPanel(completionPanel, false);
        SetMenuCamera(true);
        ClosePopups();
        RefreshAll();
        PlayTransition();
        if(levelButtons != null && levelButtons.Length > 0) Focus(levelButtons[Mathf.Clamp(levelManager != null ? levelManager.CurrentLevelIndex : 0,0,levelButtons.Length-1)]);

        if (levelSelectStatusText != null)
        {
            levelSelectStatusText.text = string.Empty;
        }
    }

    public void StartGame()
    {
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
        Time.timeScale = 1f;
        paused = false;
        SetPanel(pausePanel, false);
        isGameplayActive = true;
        levelManager?.BeginRun();
        SetPanel(homePanel, false);
        SetPanel(shopPanel, false);
        SetPanel(rankPanel, false);
        SetPanel(levelSelectPanel, false);
        SetPanel(gameplayHudPanel, true);
        SetHudInteraction(true);
        SetPanel(completionPanel, false);
        SetMenuCamera(false);
        ClosePopups();
        PlayTransition();
    }

    private void ShowCompletion(LevelManager.LevelCompletionData data)
    {
        Time.timeScale = 0f;
        isGameplayActive = false;
        paused = false;
        SetPanel(pausePanel, false);
        SetPanel(homePanel, false);
        SetPanel(shopPanel, false);
        SetPanel(rankPanel, false);
        SetPanel(levelSelectPanel, false);
        SetPanel(gameplayHudPanel, false);
        SetHudInteraction(false);
        SetPanel(completionPanel, true);
        completionPanel.transform.SetAsLastSibling();
        SetMenuCamera(false);
        ClosePopups();
        completionHasNextRoute = data.HasNextRoute;
        var view = completionPanel.GetComponent<CompletionView>();
        if (view != null) view.Show(data);
        RefreshAll();
        PlayTransition();
        Focus(completionNextButton);
    }

    private void StartNextRouteFromCompletion()
    {
        if (!completionHasNextRoute) { ShowLevelSelect(); return; }
        if (levelManager != null)
        {
            levelManager.LoadNextLevel();
        }

        StartGame();
    }

    private void ReplayRouteFromCompletion()
    {
        if (levelManager != null)
        {
            levelManager.RestartCurrentLevel();
        }

        StartGame();
    }

    private void ShowSettingsPopup()
    {
        ClosePopups();
        SetPanel(settingsPopup, true);
        SetHomeInteraction(false);
        settingsPopup.transform.SetAsLastSibling();
        Focus(closeSettingsButton);
        if (settingsStatusText != null)
        {
            settingsStatusText.text = "Audio Settings";
        }
    }

    private void ShowSpinPopup()
    {
        ClosePopups();
        SetPanel(spinPopup, true);
        if (spinStatusText != null)
        {
            spinStatusText.text = SaveManager.WelcomeRewardClaimed ? "Welcome Reward Already Claimed" : "One Welcome Gift Per Save";
        }
    }

    private void ShowDailyPopup()
    {
        ClosePopups();
        SetPanel(dailyPopup, true);
        RefreshDailyPopup();
    }

    private void ClosePopups()
    {
        bool settingsWasOpen = IsPanelOpen(settingsPopup);
        SetPanel(settingsPopup, false);
        SetPanel(spinPopup, false);
        SetPanel(dailyPopup, false);
        SetHomeInteraction(true);
        if (settingsWasOpen) Focus(settingsButton);
    }

    private void SetHomeInteraction(bool enabled)
    {
        if (homePanel == null) return;
        var group = homePanel.GetComponent<CanvasGroup>();
        if (group == null) group = homePanel.AddComponent<CanvasGroup>();
        group.interactable = enabled;
        group.blocksRaycasts = enabled;
    }

    private bool IsAnyPopupOpen()
    {
        return IsPanelOpen(settingsPopup) || IsPanelOpen(spinPopup) || IsPanelOpen(dailyPopup);
    }

    private static bool IsPanelOpen(GameObject panel)
    {
        return panel != null && panel.activeSelf;
    }

    private void TrySelectOrUnlockCar(int index)
    {
        if (carGarage == null)
        {
            return;
        }

        carGarage.TrySelectOrUnlock(index, out string message);
        if (shopStatusText != null)
        {
            shopStatusText.text = UiCopy.Title(message);
        }

        RefreshAll();
    }

    private void TryStartLevel(int index)
    {
        if (levelManager == null)
        {
            return;
        }

        if (levelManager.TryLoadLevel(index, true, out string message))
        {
            StartGame();
            return;
        }

        if (levelSelectStatusText != null)
        {
            levelSelectStatusText.text = UiCopy.Title(message);
        }

        RefreshLevelSelect();
    }

    private void ClaimSpinReward()
    {
        if (SaveManager.WelcomeRewardClaimed)
        {
            if (spinStatusText != null)
            {
                spinStatusText.text = "Spin Reward Already Claimed";
            }

            return;
        }

        int reward = UnityEngine.Random.Range(GameBalance.SpinRewardMinCoins, GameBalance.SpinRewardMaxCoins + 1);
        SaveManager.ClaimWelcomeReward(reward);

        if (spinStatusText != null)
        {
            spinStatusText.text = $"You Won +{reward} coins";
        }

        RefreshAll();
    }

    private void ClaimDailyReward()
    {
        DateTime today = DateTime.Now;
        if (SaveManager.HasClaimedDailyReward(today))
        {
            RefreshDailyPopup();
            return;
        }

        SaveManager.SetDailyRewardClaimed(today);
        SaveManager.AddCoins(GameBalance.DailyRewardCoins);
        RefreshDailyPopup();
        RefreshAll();
    }

    private void RefreshDailyPopup()
    {
        bool claimedToday = SaveManager.HasClaimedDailyReward(DateTime.Now);
        if (dailyStatusText != null)
        {
            dailyStatusText.text = claimedToday ? "Daily Reward Claimed" : $"Claim Today's +{GameBalance.DailyRewardCoins} Coin Reward";
        }

        if (claimDailyButton != null)
        {
            claimDailyButton.interactable = !claimedToday;
        }
    }

    private void RefreshAll()
    {
        RefreshCoinText();
        RefreshHomeText();
        RefreshLevelSelect();
        RefreshShop();
        RefreshRank();
        RefreshDailyPopup();
    }

    private void RefreshCoinText()
    {
        if (coinBalanceText != null)
        {
            coinBalanceText.text = SaveManager.TotalCoins.ToString("N0");
        }
    }

    private void RefreshHomeText()
    {
        if (homeLevelText != null)
        {
            homeLevelText.text = "Play";
        }
    }

    private void ChangeRoutePage(int delta)
    {
        int count = levelManager != null ? levelManager.LevelCount : levelButtons?.Length ?? 0;
        routePage = Mathf.Clamp(routePage + delta, 0, Mathf.Max(0, (count - 1) / RoutesPerPage));
        if (levelSelectStatusText != null) levelSelectStatusText.text = string.Empty;
        RefreshLevelSelect();
    }

    private void RefreshLevelSelect()
    {
        if (levelButtons == null)
        {
            return;
        }

        int levelCount = Mathf.Min(levelManager != null ? levelManager.LevelCount : levelButtons.Length, levelButtons.Length);
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(levelCount / (float)RoutesPerPage));
        routePage = Mathf.Clamp(routePage, 0, pageCount - 1);
        if (previousRoutePageButton != null) previousRoutePageButton.interactable = routePage > 0;
        if (nextRoutePageButton != null) nextRoutePageButton.interactable = routePage < pageCount - 1;
        if (routePageText != null) routePageText.text = $"{routePage + 1} / {pageCount}";
        for (int i = 0; i < levelButtons.Length; i++)
        {
            bool valid = i < levelCount;
            Button button = levelButtons[i];
            SetPanel(button != null ? button.gameObject : null, valid && i / RoutesPerPage == routePage);
            if (!valid)
            {
                continue;
            }

            bool unlocked = levelManager == null || levelManager.IsLevelUnlocked(i);
            bool selected = levelManager != null && i == levelManager.CurrentLevelIndex;
            bool completed = SaveManager.IsRouteCompleted(i);
            SetLevelCardVisual(button, unlocked, selected);
            if(button!=null)
            {
                var symbol=button.transform.Find("ActionIcon")?.GetComponent<MenuIcon>();
                if(symbol!=null){symbol.icon=completed?MenuIcon.Kind.Check:unlocked?MenuIcon.Kind.Play:MenuIcon.Kind.Lock;symbol.SetVerticesDirty();}
                var shade=button.transform.Find("LockedShade");if(shade!=null)shade.gameObject.SetActive(!unlocked);
                var hint=button.GetComponent<IconButtonHint>();
                if(hint!=null)hint.description=unlocked?$"Start {(levelManager != null ? levelManager.GetLevelName(i) : "route " + (i + 1))}":$"Complete route {i} first";
            }


            if (levelTitleTexts != null && i < levelTitleTexts.Length && levelTitleTexts[i] != null)
            {
                string routeName = levelManager != null ? levelManager.GetLevelName(i) : $"Route {i + 1}";

                levelTitleTexts[i].text = SentenceCase(routeName);
                levelTitleTexts[i].color = Color.white;
            }

            if (levelMetaTexts != null && i < levelMetaTexts.Length && levelMetaTexts[i] != null)
            {

                var route = levelManager != null ? levelManager.GetCourse(i) : null;
                levelMetaTexts[i].text = unlocked
                    ? completed
                        ? $"Best {SaveManager.GetBestTime(i):0.0}s / {SaveManager.GetBestRouteDriftScore(i)} drift"
                        : route != null ? $"{route.difficulty}/10 · Gold {route.goldTime:0}s + {route.goldScore} drift" : "Finish to unlock the next route"
                    : $"Complete route {i} to unlock";
                levelMetaTexts[i].gameObject.SetActive(false);
                levelMetaTexts[i].color = unlocked ? new Color(0.45f, 0.95f, 1f) : new Color(0.68f, 0.72f, 0.76f);
            }
        }
    }

    private static string SentenceCase(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return UiCopy.Title(value);
    }

    private void RefreshShop()
    {
        if (carGarage == null || shopCarButtons == null)
        {
            return;
        }

        var wallet = shopPanel != null ? shopPanel.transform.Find("ShopWallet/Balance")?.GetComponent<Text>() : null;
        if (wallet != null) wallet.text = SaveManager.TotalCoins.ToString("N0");

        for (int i = 0; i < shopCarButtons.Length; i++)
        {
            bool valid = i < carGarage.CarCount;
            SetPanel(shopCarButtons[i] != null ? shopCarButtons[i].gameObject : null, valid);
            if (!valid)
            {
                continue;
            }

            bool unlocked = carGarage.IsCarUnlocked(i) || carGarage.GetCarUnlockCost(i) == 0;
            bool selected = i == carGarage.SelectedIndex;
            string carName = carGarage.GetCarDisplayName(i);
            int cost = carGarage.GetCarUnlockCost(i);

            var vehicleCard = shopCarButtons[i].GetComponent<ShopVehicleCard>();
            if (vehicleCard != null)
            {
                shopCarTitleTexts[i].text = carName;
                shopCarTitleTexts[i].color = new Color(0.07f, 0.23f, 0.40f);
                vehicleCard.Refresh(carName, cost, unlocked, selected);
                continue;
            }

            SetShopCardVisual(shopCarButtons[i], unlocked, selected);

            if (shopCarTitleTexts != null && i < shopCarTitleTexts.Length && shopCarTitleTexts[i] != null)
            {
                shopCarTitleTexts[i].text = carName;
                shopCarTitleTexts[i].color = selected ? new Color(1f, 0.74f, 0.22f) : Color.white;
            }

            if (shopCarCostTexts != null && i < shopCarCostTexts.Length && shopCarCostTexts[i] != null)
            {
                shopCarCostTexts[i].text = unlocked ? string.Empty : cost.ToString("N0");
                shopCarCostTexts[i].color = new Color(1f,0.77f,0.38f);
                var card=shopCarButtons[i];
                var symbol=card.transform.Find("ActionIcon")?.GetComponent<MenuIcon>();
                if(symbol!=null){symbol.icon=selected?MenuIcon.Kind.Check:unlocked?MenuIcon.Kind.Play:MenuIcon.Kind.Lock;symbol.SetVerticesDirty();}
                var currency=card.transform.Find("PriceCoin");if(currency!=null)currency.gameObject.SetActive(!unlocked);
                var hint=card.GetComponent<IconButtonHint>();
                if(hint!=null)hint.description=selected?$"{carName} selected":unlocked?$"Drive {carName}":$"Unlock for {cost} coins";

            }
        }
    }

    private void RefreshRank()
    {
        if (rankListText == null || rankPanel == null) return;
        int count = levelManager != null ? levelManager.LevelCount : 0;
        int completed = 0;
        var order = new List<int>();
        for (int i=0;i<count;i++) { order.Add(i); if(SaveManager.IsRouteCompleted(i))completed++; }
        order.Sort((a,b) =>
        {
            bool aHas=SaveManager.GetBestTime(a)>0, bHas=SaveManager.GetBestTime(b)>0;
            if(aHas!=bHas)return aHas?-1:1;
            if(!aHas)return a.CompareTo(b);
            int score=SaveManager.GetBestRouteDriftScore(b).CompareTo(SaveManager.GetBestRouteDriftScore(a));
            if(score!=0)return score;
            int time=SaveManager.GetBestTime(a).CompareTo(SaveManager.GetBestTime(b));
            return time!=0?time:a.CompareTo(b);
        });
        var content=rankPanel.transform.Find("RecordScroll/Viewport/Content")??rankPanel.transform;
        for(int position=0;position<10;position++)
        {
            var row=content.Find($"RecordRow_{position+1:00}");
            if(row==null)continue;
            row.gameObject.SetActive(position<count);
            if(position>=count)continue;
            int route=order[position];
            float best=SaveManager.GetBestTime(route);
            bool hasRecord=best>0f, done=SaveManager.IsRouteCompleted(route), available=levelManager.IsLevelUnlocked(route);
            row.Find("Route").GetComponent<Text>().text=SentenceCase(levelManager.GetLevelName(route));
            row.Find("Time").GetComponent<Text>().text=hasRecord?$"{(int)(best/60):00}:{best%60:00.00}":"—";
            row.Find("Drift").GetComponent<Text>().text=hasRecord?SaveManager.GetBestRouteDriftScore(route).ToString("N0"):"—";
            var status=row.Find("Status").GetComponent<Text>();
            status.text=hasRecord?"Completed":done?"No Record":available?"Ready":"Locked";
            status.color=new Color(0.83f,0.95f,1f);
            row.GetComponent<RouteRecordCard>()?.Refresh(route,position+1,hasRecord,available);
        }
        rankListText.text=$"{completed} / {count} Completed";
    }

    private static void SetShopCardVisual(Button button, bool unlocked, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Color baseColor = selected
            ? new Color(0.14f, 0.24f, 0.27f)
            : unlocked
                ? new Color(0.1f, 0.15f, 0.22f)
                : new Color(0.08f, 0.09f, 0.12f);

        if (button.targetGraphic is Image image)
        {
            image.color = baseColor;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f,1.15f,1.15f);
        colors.pressedColor = new Color(0.75f,0.75f,0.75f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
    }

    private static void SetLevelCardVisual(Button button, bool unlocked, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Color baseColor = selected
            ? new Color(1f, 0.80f, 0.30f)
            : unlocked
                ? new Color(0.40f, 0.80f, 1f)
                : new Color(0.35f, 0.55f, 0.70f);

        if (button.targetGraphic is Image image)
        {
            image.color = baseColor;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = unlocked ? new Color(1.15f,1.15f,1.15f) : Color.white;
        colors.pressedColor = unlocked ? new Color(0.75f,0.75f,0.75f) : Color.white;
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.5f,0.5f,0.5f);
        button.colors = colors;
        button.interactable = true;
    }

    private void LoadSettings()
    {
        bool musicEnabled = SaveManager.MusicEnabled;
        bool soundEnabled = SaveManager.SoundEnabled;

        if (musicToggle != null)
        {
            musicToggle.SetIsOnWithoutNotify(musicEnabled);
        }

        if (soundToggle != null)
        {
            soundToggle.SetIsOnWithoutNotify(soundEnabled);
        }

        if (audioManager == null)
        {
            audioManager = AudioManager.Instance != null ? AudioManager.Instance : FindFirstObjectByType<AudioManager>();
        }

        if (audioManager != null)
        {
            audioManager.SetMusicEnabled(musicEnabled);
            audioManager.SetSoundEnabled(soundEnabled);
        }
    }

    private void SetMusicEnabled(bool enabled)
    {
        SaveManager.SetMusicEnabled(enabled);
        if (audioManager == null)
        {
            audioManager = AudioManager.Instance != null ? AudioManager.Instance : FindFirstObjectByType<AudioManager>();
        }

        if (audioManager != null)
        {
            audioManager.SetMusicEnabled(enabled);
        }

        if (settingsStatusText != null)
        {
            settingsStatusText.text = enabled ? "Music On" : "Music Off";
        }
    }

    private void SetSoundEnabled(bool enabled)
    {
        SaveManager.SetSoundEnabled(enabled);
        if (audioManager == null)
        {
            audioManager = AudioManager.Instance != null ? AudioManager.Instance : FindFirstObjectByType<AudioManager>();
        }

        if (audioManager != null)
        {
            audioManager.SetSoundEnabled(enabled);
        }

        if (settingsStatusText != null)
        {
            settingsStatusText.text = enabled ? "Sound On" : "Sound Off";
        }
    }

    private void TogglePause()
    {
        if (!isGameplayActive) return;
        paused = !paused;
        Time.timeScale = paused ? 0f : 1f;
        SetPanel(pausePanel, paused);
        SetHudInteraction(!paused);
        if (paused) { pausePanel.transform.SetAsLastSibling(); Focus(pausePanel.transform.Find("Resume").GetComponent<Button>()); }
        else if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
    }

    private void CreatePausePanel()
    {
        pausePanel = PauseMenuView.Create(homePanel.transform.parent, TogglePause,
            () => { levelManager.RestartCurrentLevel(); StartGame(); }, ShowHome);
    }

    private void SetHudInteraction(bool enabled)
    {
        if (gameplayHudPanel == null) return;
        var group=gameplayHudPanel.GetComponent<CanvasGroup>();
        if(group==null)group=gameplayHudPanel.AddComponent<CanvasGroup>();
        group.interactable=enabled;group.blocksRaycasts=enabled;
    }

    private static void AddClick(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                action();
            });
        }
    }

    private void SetMenuCamera(bool active)
    {
        if (cameraFollow != null)
        {
            cameraFollow.SetMenuMode(active);
        }
    }

    private void PlayTransition()
    {
        if (transitionFadeGroup == null)
        {
            return;
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(FadeTransition());
    }

    private IEnumerator FadeTransition()
    {
        transitionFadeGroup.gameObject.SetActive(true);
        transitionFadeGroup.blocksRaycasts = true;
        transitionFadeGroup.alpha = 0.72f;

        const float duration = 0.22f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            transitionFadeGroup.alpha = Mathf.Lerp(0.72f, 0f, elapsed / duration);
            yield return null;
        }

        transitionFadeGroup.alpha = 0f;
        transitionFadeGroup.blocksRaycasts = false;
        transitionRoutine = null;
    }

    private static void SetPanel(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }
}
