using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class MainMenuUI : MonoBehaviour
{
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

    private bool spinUsedThisSession;
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
        AddClick(gameplayHomeButton, ShowHome);
        AddClick(levelSelectBackButton, ShowHome);
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
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
        {
            return;
        }

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
            ShowHome();
            return;
        }

        if (!IsPanelOpen(homePanel))
        {
            ShowHome();
        }
    }

    public void ShowHome()
    {
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
    }

    public void ShowLevelSelect()
    {
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

        if (levelSelectStatusText != null)
        {
            int routeNumber = levelManager != null ? levelManager.CurrentLevelIndex + 1 : 1;
            levelSelectStatusText.text = $"Choose a route. Current: Route {routeNumber}";
        }
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        isGameplayActive = true;
        SetPanel(homePanel, false);
        SetPanel(shopPanel, false);
        SetPanel(rankPanel, false);
        SetPanel(levelSelectPanel, false);
        SetPanel(gameplayHudPanel, true);
        SetPanel(completionPanel, false);
        SetMenuCamera(false);
        ClosePopups();
        PlayTransition();
    }

    private void ShowCompletion(LevelManager.LevelCompletionData data)
    {
        Time.timeScale = 0f;
        isGameplayActive = false;
        SetPanel(homePanel, false);
        SetPanel(shopPanel, false);
        SetPanel(rankPanel, false);
        SetPanel(levelSelectPanel, false);
        SetPanel(gameplayHudPanel, true);
        SetPanel(completionPanel, true);
        SetMenuCamera(false);
        ClosePopups();

        if (completionTitleText != null)
        {
            completionTitleText.text = $"ROUTE {data.RouteIndex + 1} COMPLETE";
        }

        if (completionStatsText != null)
        {
            completionStatsText.text =
                $"{data.RouteName}\n" +
                $"Collected Coins: {data.CollectedCoins}\n" +
                $"Drift Bonus: +{data.BonusCoins}\n" +
                $"Obstacle Penalty: -{data.PenaltyCoins}\n" +
                $"Drift Score: {data.DriftScore}\n" +
                $"Total Coins: {data.TotalCoins}";
        }

        if (completionBestText != null)
        {
            string firstText = data.FirstCompletion ? "First clear" : "Replay clear";
            string coinText = data.NewBestCoins ? "New coin best" : $"Best coins {data.BestCoins}";
            string driftText = data.NewBestDrift ? "New drift best" : $"Best drift {data.BestDriftScore}";
            completionBestText.text = $"{firstText} | {coinText} | {driftText}";
        }

        if (completionNextButton != null)
        {
            completionNextButton.interactable = data.HasNextRoute;
        }

        RefreshAll();
        PlayTransition();
    }

    private void StartNextRouteFromCompletion()
    {
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
        if (settingsStatusText != null)
        {
            settingsStatusText.text = "Audio settings";
        }
    }

    private void ShowSpinPopup()
    {
        ClosePopups();
        SetPanel(spinPopup, true);
        if (spinStatusText != null)
        {
            spinStatusText.text = spinUsedThisSession ? "Spin reward already claimed" : "Spin once for a coin bonus";
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
        SetPanel(settingsPopup, false);
        SetPanel(spinPopup, false);
        SetPanel(dailyPopup, false);
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
            shopStatusText.text = message;
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
            levelSelectStatusText.text = message;
        }

        RefreshLevelSelect();
    }

    private void ClaimSpinReward()
    {
        if (spinUsedThisSession)
        {
            if (spinStatusText != null)
            {
                spinStatusText.text = "Spin reward already claimed";
            }

            return;
        }

        spinUsedThisSession = true;
        int reward = UnityEngine.Random.Range(GameBalance.SpinRewardMinCoins, GameBalance.SpinRewardMaxCoins + 1);
        SaveManager.AddCoins(reward);

        if (spinStatusText != null)
        {
            spinStatusText.text = $"You won +{reward} coins";
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
            dailyStatusText.text = claimedToday ? "Daily reward claimed" : $"Claim today's +{GameBalance.DailyRewardCoins} coin reward";
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
            coinBalanceText.text = $"Coins {SaveManager.TotalCoins}";
        }
    }

    private void RefreshHomeText()
    {
        if (homeLevelText != null)
        {
            int totalRoutes = levelManager != null ? Mathf.Max(1, levelManager.LevelCount) : 10;
            int unlockedRoutes = levelManager != null
                ? Mathf.Min(totalRoutes, SaveManager.HighestUnlockedRouteIndex + 1)
                : 1;
            homeLevelText.text = $"LEVEL SELECT  {unlockedRoutes}/{totalRoutes}";
        }
    }

    private void RefreshLevelSelect()
    {
        if (levelButtons == null)
        {
            return;
        }

        int levelCount = levelManager != null ? levelManager.LevelCount : levelButtons.Length;
        for (int i = 0; i < levelButtons.Length; i++)
        {
            bool valid = i < levelCount;
            Button button = levelButtons[i];
            SetPanel(button != null ? button.gameObject : null, valid);
            if (!valid)
            {
                continue;
            }

            bool unlocked = levelManager == null || levelManager.IsLevelUnlocked(i);
            bool selected = levelManager != null && i == levelManager.CurrentLevelIndex;
            bool completed = SaveManager.IsRouteCompleted(i);
            SetLevelCardVisual(button, unlocked, selected);

            if (levelTitleTexts != null && i < levelTitleTexts.Length && levelTitleTexts[i] != null)
            {
                string routeName = levelManager != null ? levelManager.GetLevelName(i) : $"Route {i + 1}";
                string prefix = completed ? "Complete" : unlocked ? "Route" : "Locked";
                levelTitleTexts[i].text = unlocked ? $"{prefix} {i + 1}: {routeName}" : $"Route {i + 1}: Locked";
                levelTitleTexts[i].color = selected ? new Color(1f, 0.74f, 0.22f) : completed ? new Color(0.45f, 0.95f, 1f) : Color.white;
            }

            if (levelMetaTexts != null && i < levelMetaTexts.Length && levelMetaTexts[i] != null)
            {
                int targetCoins = levelManager != null ? levelManager.GetLevelTargetCoins(i) : 0;
                int bestCoins = SaveManager.GetBestRouteCoins(i);
                levelMetaTexts[i].text = unlocked
                    ? completed
                        ? $"Best {bestCoins} coins | Target {targetCoins}"
                        : $"{targetCoins} coins objective"
                    : $"Complete Route {i} to unlock";
                levelMetaTexts[i].color = unlocked ? new Color(0.45f, 0.95f, 1f) : new Color(0.68f, 0.72f, 0.76f);
            }
        }
    }

    private void RefreshShop()
    {
        if (carGarage == null || shopCarButtons == null)
        {
            return;
        }

        for (int i = 0; i < shopCarButtons.Length; i++)
        {
            bool valid = i < carGarage.CarCount;
            SetPanel(shopCarButtons[i] != null ? shopCarButtons[i].gameObject : null, valid);
            if (!valid)
            {
                continue;
            }

            bool unlocked = carGarage.IsCarUnlocked(i);
            bool selected = i == carGarage.SelectedIndex;
            string carName = carGarage.GetCarDisplayName(i);
            int cost = carGarage.GetCarUnlockCost(i);

            SetShopCardVisual(shopCarButtons[i], unlocked, selected);

            if (shopCarTitleTexts != null && i < shopCarTitleTexts.Length && shopCarTitleTexts[i] != null)
            {
                shopCarTitleTexts[i].text = carName;
                shopCarTitleTexts[i].color = selected ? new Color(1f, 0.74f, 0.22f) : Color.white;
            }

            if (shopCarCostTexts != null && i < shopCarCostTexts.Length && shopCarCostTexts[i] != null)
            {
                shopCarCostTexts[i].text = selected
                    ? "Selected"
                    : unlocked
                        ? "Tap to drive"
                        : $"Unlock {cost} coins";
                shopCarCostTexts[i].color = unlocked ? new Color(0.45f, 0.95f, 1f) : new Color(1f, 0.62f, 0.34f);
            }
        }
    }

    private void RefreshRank()
    {
        if (rankListText == null)
        {
            return;
        }

        int levelCount = levelManager != null ? levelManager.LevelCount : 10;
        int bestRouteIndex = SaveManager.GetBestCompletedRouteIndex(levelCount);
        int displayRoute = bestRouteIndex >= 0 ? bestRouteIndex + 1 : levelManager != null ? levelManager.CurrentLevelIndex + 1 : 1;
        int bestCoins = bestRouteIndex >= 0 ? SaveManager.GetBestRouteCoins(bestRouteIndex) : 0;
        int bestDrift = bestRouteIndex >= 0 ? SaveManager.GetBestRouteDriftScore(bestRouteIndex) : 0;
        int completedRoutes = SaveManager.GetCompletedRouteCount(levelCount);
        rankListText.text =
            $"01    YOU                         Route {displayRoute:00}   {bestCoins} coins   {bestDrift} drift\n\n" +
            "02    Drift Ace                   Route 08\n\n" +
            "03    Cone Cutter                 Route 06\n\n" +
            "04    Night Runner                Route 05\n\n" +
            $"05    Progress                    {completedRoutes}/{levelCount} routes";
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
        colors.normalColor = baseColor;
        colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.2f);
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
            ? new Color(0.16f, 0.26f, 0.28f)
            : unlocked
                ? new Color(0.09f, 0.14f, 0.2f)
                : new Color(0.075f, 0.08f, 0.095f);

        if (button.targetGraphic is Image image)
        {
            image.color = baseColor;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = unlocked ? Color.Lerp(baseColor, Color.white, 0.16f) : baseColor;
        colors.pressedColor = unlocked ? Color.Lerp(baseColor, Color.black, 0.2f) : baseColor;
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = baseColor;
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
            settingsStatusText.text = enabled ? "Music on" : "Music off";
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
            settingsStatusText.text = enabled ? "Sound on" : "Sound off";
        }
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
