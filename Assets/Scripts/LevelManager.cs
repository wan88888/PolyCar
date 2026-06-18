using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class LevelManager : MonoBehaviour
{
    public struct LevelCompletionData
    {
        public int RouteIndex;
        public string RouteName;
        public int CollectedCoins;
        public int BonusCoins;
        public int PenaltyCoins;
        public int EarnedCoins;
        public int DriftScore;
        public int BestCoins;
        public int BestDriftScore;
        public int TotalCoins;
        public bool FirstCompletion;
        public bool NewBestCoins;
        public bool NewBestDrift;
        public bool HasNextRoute;
    }

    [Serializable]
    public sealed class LevelDefinition
    {
        public string displayName = "Route";
        public Transform root;
        public Transform spawnPoint;
        [Min(1)] public int targetCoins = 10;
    }

    [Header("Levels")]
    [SerializeField] private LevelDefinition[] levels;
    [SerializeField] private int currentLevelIndex;

    [Header("UI")]
    [SerializeField] private Text levelText;
    [SerializeField] private Text coinText;
    [SerializeField] private Text totalCoinsText;
    [SerializeField] private Text messageText;
    [SerializeField] private Text guidanceText;

    [Header("References")]
    [SerializeField] private CarGarage carGarage;
    [SerializeField] private GameManager gameManager;

    [Header("Guidance")]
    [SerializeField] private Color guidanceColor = new Color(0.45f, 0.95f, 1f);
    [SerializeField] private Color guidanceCloseColor = new Color(1f, 0.74f, 0.22f);

    private int collectedCoins;
    private int activeTargetCoins = 10;
    private bool levelComplete;
    private int bonusCoinsThisRun;
    private int penaltyCoinsThisRun;
    private CoinPickup[] activeCoins = Array.Empty<CoinPickup>();
    private CoinPickup currentTargetCoin;
    private CarController playerCar;

    public int CollectedCoins => collectedCoins;
    public int TargetCoins => activeTargetCoins;
    public bool LevelComplete => levelComplete;
    public int CurrentLevelIndex => currentLevelIndex;
    public int LevelCount => levels != null ? levels.Length : 0;
    public event Action<LevelCompletionData> LevelCompleted;

    private void Awake()
    {
        if (carGarage == null)
        {
            carGarage = FindFirstObjectByType<CarGarage>();
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
    }

    private void Start()
    {
        SaveManager.UnlockRoute(0);

        int savedLevelIndex = Mathf.Clamp(SaveManager.GetSelectedRouteIndex(currentLevelIndex), 0, Mathf.Max(0, LevelCount - 1));
        if (!SaveManager.IsRouteUnlocked(savedLevelIndex))
        {
            savedLevelIndex = 0;
        }

        LoadLevel(savedLevelIndex, true);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.rKey.wasPressedThisFrame)
            {
                RestartCurrentLevel();
            }

            if (levelComplete && keyboard.nKey.wasPressedThisFrame)
            {
                LoadNextLevel();
            }
        }

        UpdateGuidance();
    }

    public void CollectCoin(CoinPickup coin)
    {
        if (levelComplete)
        {
            return;
        }

        collectedCoins += coin != null ? coin.Value : 1;
        if (collectedCoins >= activeTargetCoins)
        {
            ClearGuidanceTarget();
            CompleteLevel();
        }
        else
        {
            SelectNextTargetCoin();
        }

        UpdateUi();
    }

    public void RefreshTotalCoins()
    {
        UpdateUi();
    }

    public void AwardBonusCoins(int amount, string reason)
    {
        if (amount <= 0 || levelComplete)
        {
            return;
        }

        SaveManager.AddCoins(amount);
        bonusCoinsThisRun += amount;
        SetMessage($"{reason}\nBonus +{amount} coins");
        UpdateUi();
    }

    public void RegisterObstacleHit(int coinPenalty)
    {
        if (levelComplete)
        {
            return;
        }

        int paidPenalty = Mathf.Min(Mathf.Max(0, coinPenalty), SaveManager.TotalCoins);
        if (paidPenalty > 0)
        {
            SaveManager.TrySpendCoins(paidPenalty);
            penaltyCoinsThisRun += paidPenalty;
        }

        string penaltyText = paidPenalty > 0 ? $"Penalty -{paidPenalty} coins" : "No coins lost";
        SetMessage($"Obstacle hit\n{penaltyText}\nDrift combo broken");
        UpdateUi();
    }

    public void ShowMessage(string message)
    {
        SetMessage(message);
    }

    public void RestartCurrentLevel()
    {
        LoadLevel(currentLevelIndex, true);
    }

    public bool TryLoadLevel(int levelIndex, bool resetCar, out string message)
    {
        if (LevelCount == 0)
        {
            message = "No road levels configured";
            return false;
        }

        if (levelIndex < 0 || levelIndex >= LevelCount)
        {
            message = "Route unavailable";
            return false;
        }

        if (!SaveManager.IsRouteUnlocked(levelIndex))
        {
            message = $"Route {levelIndex + 1} is locked\nComplete Route {levelIndex} first";
            return false;
        }

        LoadLevel(levelIndex, resetCar);
        message = $"Route {levelIndex + 1}: {GetLevelName(levelIndex)}";
        return true;
    }

    public void LoadNextLevel()
    {
        if (LevelCount == 0)
        {
            return;
        }

        int nextLevelIndex = currentLevelIndex + 1;
        bool wrapped = nextLevelIndex >= LevelCount;
        if (wrapped)
        {
            nextLevelIndex = 0;
        }

        if (!SaveManager.IsRouteUnlocked(nextLevelIndex))
        {
            SetMessage($"Route {nextLevelIndex + 1} is locked\nComplete the current route first");
            return;
        }

        LoadLevel(nextLevelIndex, true);
        if (wrapped)
        {
            SetMessage("All routes complete\nLooping back to Route 1");
        }
    }

    public string GetLevelName(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= LevelCount)
        {
            return "Route";
        }

        string name = levels[levelIndex].displayName;
        return string.IsNullOrWhiteSpace(name) ? $"Route {levelIndex + 1}" : name;
    }

    public int GetLevelTargetCoins(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= LevelCount)
        {
            return 0;
        }

        return Mathf.Max(1, levels[levelIndex].targetCoins);
    }

    public bool IsLevelUnlocked(int levelIndex)
    {
        return SaveManager.IsRouteUnlocked(levelIndex);
    }

    private void LoadLevel(int levelIndex, bool resetCar)
    {
        if (LevelCount == 0)
        {
            ClearGuidanceTarget();
            activeCoins = Array.Empty<CoinPickup>();
            collectedCoins = 0;
            activeTargetCoins = 10;
            levelComplete = false;
            UpdateUi();
            SetMessage("No road levels configured");
            return;
        }

        ClearGuidanceTarget();
        currentLevelIndex = Mathf.Clamp(levelIndex, 0, LevelCount - 1);
        SaveManager.SetSelectedRoute(currentLevelIndex);

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].root != null)
            {
                levels[i].root.gameObject.SetActive(i == currentLevelIndex);
            }
        }

        LevelDefinition activeLevel = levels[currentLevelIndex];
        collectedCoins = 0;
        bonusCoinsThisRun = 0;
        penaltyCoinsThisRun = 0;
        levelComplete = false;
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null)
        {
            gameManager.ResetRunFeedback();
        }

        int coinCount = ResetCoins(activeLevel.root);
        activeTargetCoins = coinCount > 0
            ? Mathf.Clamp(activeLevel.targetCoins, 1, coinCount)
            : Mathf.Max(1, activeLevel.targetCoins);

        if (carGarage != null && activeLevel.spawnPoint != null)
        {
            carGarage.SetSpawnPoint(activeLevel.spawnPoint, resetCar);
        }

        SelectNextTargetCoin();
        UpdateUi();
        SetMessage($"{GetCurrentLevelName()}\nCollect the coins on the road");
    }

    private int ResetCoins(Transform levelRoot)
    {
        if (levelRoot == null)
        {
            return 0;
        }

        activeCoins = levelRoot.GetComponentsInChildren<CoinPickup>(true);
        Array.Sort(activeCoins, (left, right) => string.CompareOrdinal(left.name, right.name));
        for (int i = 0; i < activeCoins.Length; i++)
        {
            activeCoins[i].ResetPickup();
        }

        return activeCoins.Length;
    }

    private void CompleteLevel()
    {
        if (gameManager != null)
        {
            gameManager.FlushActiveDrift();
        }

        levelComplete = true;
        SaveManager.AddCoins(collectedCoins);
        SaveManager.UnlockRoute(currentLevelIndex);
        int driftScore = gameManager != null ? gameManager.TotalDriftScore : 0;
        int earnedCoins = Mathf.Max(0, collectedCoins + bonusCoinsThisRun - penaltyCoinsThisRun);
        SaveManager.RecordRouteResult(currentLevelIndex, earnedCoins, driftScore, out bool firstCompletion, out bool newBestCoins, out bool newBestDrift);

        int nextLevelIndex = currentLevelIndex + 1;
        bool unlockedNextRoute = nextLevelIndex < LevelCount;
        if (unlockedNextRoute)
        {
            SaveManager.UnlockRoute(nextLevelIndex);
        }

        string nextRouteMessage = unlockedNextRoute
            ? $"\nRoute {nextLevelIndex + 1} unlocked\nPress N for next route"
            : "\nAll routes complete";

        SetMessage($"{GetCurrentLevelName()} Complete\nReward +{collectedCoins} coins{nextRouteMessage}\nPress R to replay");
        SetGuidanceText("Route complete");
        LevelCompleted?.Invoke(new LevelCompletionData
        {
            RouteIndex = currentLevelIndex,
            RouteName = GetCurrentLevelName(),
            CollectedCoins = collectedCoins,
            BonusCoins = bonusCoinsThisRun,
            PenaltyCoins = penaltyCoinsThisRun,
            EarnedCoins = earnedCoins,
            DriftScore = driftScore,
            BestCoins = SaveManager.GetBestRouteCoins(currentLevelIndex),
            BestDriftScore = SaveManager.GetBestRouteDriftScore(currentLevelIndex),
            TotalCoins = SaveManager.TotalCoins,
            FirstCompletion = firstCompletion,
            NewBestCoins = newBestCoins,
            NewBestDrift = newBestDrift,
            HasNextRoute = unlockedNextRoute
        });
    }

    private void UpdateUi()
    {
        if (levelText != null)
        {
            levelText.text = LevelCount > 0
                ? $"Route {currentLevelIndex + 1}/{LevelCount}: {GetCurrentLevelName()}"
                : "Route: None";
        }

        if (coinText != null)
        {
            coinText.text = $"Coins: {Mathf.Min(collectedCoins, activeTargetCoins)} / {activeTargetCoins}";
        }

        if (totalCoinsText != null)
        {
            totalCoinsText.text = $"Total Coins: {SaveManager.TotalCoins}";
        }

        if (carGarage != null)
        {
            carGarage.RefreshUi();
        }
    }

    private string GetCurrentLevelName()
    {
        return GetLevelName(currentLevelIndex);
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }

    private void SelectNextTargetCoin()
    {
        ClearGuidanceTarget();
        if (levelComplete || activeCoins == null)
        {
            return;
        }

        for (int i = 0; i < activeCoins.Length; i++)
        {
            CoinPickup coin = activeCoins[i];
            if (coin == null || coin.IsCollected || !coin.gameObject.activeInHierarchy)
            {
                continue;
            }

            currentTargetCoin = coin;
            currentTargetCoin.SetHighlighted(true);
            return;
        }
    }

    private void ClearGuidanceTarget()
    {
        if (currentTargetCoin != null)
        {
            currentTargetCoin.SetHighlighted(false);
        }

        currentTargetCoin = null;
    }

    private void UpdateGuidance()
    {
        if (guidanceText == null)
        {
            return;
        }

        if (levelComplete)
        {
            SetGuidanceText("Route complete", guidanceCloseColor);
            return;
        }

        if (currentTargetCoin == null || currentTargetCoin.IsCollected || !currentTargetCoin.gameObject.activeInHierarchy)
        {
            SelectNextTargetCoin();
        }

        if (currentTargetCoin == null)
        {
            SetGuidanceText("Next coin: none");
            return;
        }

        if (playerCar == null)
        {
            playerCar = FindFirstObjectByType<CarController>();
        }

        if (playerCar == null)
        {
            SetGuidanceText("Next coin: ready");
            return;
        }

        Vector3 toTarget = currentTargetCoin.GuidancePosition - playerCar.transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        string direction = GetDirectionLabel(playerCar.transform.forward, toTarget);
        Color color = distance <= 12f ? guidanceCloseColor : guidanceColor;
        SetGuidanceText($"Next Coin: {distance:0}m | {direction}", color);
    }

    private static string GetDirectionLabel(Vector3 carForward, Vector3 toTarget)
    {
        carForward.y = 0f;
        if (carForward.sqrMagnitude < 0.01f || toTarget.sqrMagnitude < 0.01f)
        {
            return "Ahead";
        }

        float angle = Vector3.SignedAngle(carForward.normalized, toTarget.normalized, Vector3.up);
        float absoluteAngle = Mathf.Abs(angle);
        if (absoluteAngle < 20f)
        {
            return "Ahead";
        }

        if (absoluteAngle > 150f)
        {
            return "Behind";
        }

        if (angle > 70f)
        {
            return "Hard Right";
        }

        if (angle > 20f)
        {
            return "Right";
        }

        if (angle < -70f)
        {
            return "Hard Left";
        }

        return "Left";
    }

    private void SetGuidanceText(string message)
    {
        SetGuidanceText(message, guidanceColor);
    }

    private void SetGuidanceText(string message, Color color)
    {
        if (guidanceText != null)
        {
            guidanceText.text = message;
            guidanceText.color = color;
        }
    }

    public void SetPlayerCar(CarController car)
    {
        playerCar = car;
    }
}
