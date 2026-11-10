using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class LevelManager : MonoBehaviour
{
    public struct LevelCompletionData
    {
        public float Seconds;
        public float BestSeconds;
        public string Medal;
        public int Recoveries;
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

    public enum RunPhase { Ready, Countdown, Racing, Finished }
    public RunPhase Phase { get; private set; }
    public float ElapsedSeconds { get; private set; }
    public int PassedGates { get; private set; }
    public int Recoveries { get; private set; }
    public bool RunInProgress => Phase == RunPhase.Countdown || Phase == RunPhase.Racing;
    private RaceCourse course;
    private float countdown;
    private float messageUntil;
    private float overturnedTime;
    private Vector3 previousPosition;
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
    public RaceCourse GetCourse(int index) => levels != null && index >= 0 && index < levels.Length && levels[index].root != null ? levels[index].root.GetComponent<RaceCourse>() : null;
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
        TickRun(Time.deltaTime);
        UpdateGuidance();
    }

    public void CollectCoin(CoinPickup coin)
    {
        if (levelComplete || Phase != RunPhase.Racing)
        {
            return;
        }

        collectedCoins += coin != null ? coin.Value : 1;
        if (course == null && collectedCoins >= activeTargetCoins)
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
        if (amount <= 0 || levelComplete || Phase != RunPhase.Racing)
        {
            return;
        }

        bonusCoinsThisRun += amount;
        SetMessage($"+{amount} Coins");
        UpdateUi();
    }

    public void RegisterObstacleHit(int coinPenalty)
    {
        if (levelComplete)
        {
            return;
        }

        if (Phase != RunPhase.Racing) return;
        int paidPenalty = Mathf.Max(0, coinPenalty);
        penaltyCoinsThisRun += paidPenalty;

        SetMessage(paidPenalty > 0 ? $"Collision · -{paidPenalty} Coins" : "Combo Lost");
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

        // Release the previous level's shared lighting before the incoming level captures it.
        // A single ascending SetActive loop would let a later night level restore over an earlier day level.
        for (int i = 0; i < levels.Length; i++)
        {
            if (i != currentLevelIndex && levels[i].root != null)
            {
                levels[i].root.gameObject.SetActive(false);
                // Explicit calls also keep editor-driven level changes correct; runtime callbacks are idempotent.
                levels[i].root.GetComponent<SceneLook>()?.Restore();
            }
        }

        LevelDefinition activeLevel = levels[currentLevelIndex];
        if (activeLevel.root != null)
        {
            activeLevel.root.gameObject.SetActive(true);
            activeLevel.root.GetComponent<SceneLook>()?.Apply();
        }
        collectedCoins = 0;
        bonusCoinsThisRun = 0;
        penaltyCoinsThisRun = 0;
        levelComplete = false;
        Phase = RunPhase.Ready;
        ElapsedSeconds = 0f;
        PassedGates = 0;
        Recoveries = 0;
        overturnedTime = 0f;
        course = activeLevel.root != null ? activeLevel.root.GetComponent<RaceCourse>() : null;
        if (playerCar != null) playerCar.SetControlsEnabled(false);
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null)
        {
            gameManager.ResetRunFeedback();
        }

        int coinCount = ResetCoins(activeLevel.root);
        if (activeLevel.root != null)
            foreach (var obstacle in activeLevel.root.GetComponentsInChildren<ObstacleFeedback>(true))
                obstacle.ResetFeedback();
        activeTargetCoins = coinCount > 0
            ? Mathf.Clamp(activeLevel.targetCoins, 1, coinCount)
            : Mathf.Max(1, activeLevel.targetCoins);

        if (carGarage != null && activeLevel.spawnPoint != null)
        {
            carGarage.SetSpawnPoint(activeLevel.spawnPoint, resetCar);
        }

        SelectNextTargetCoin();
        UpdateUi();
        SetMessage(string.Empty);
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
        Phase = RunPhase.Finished;
        if (playerCar != null) playerCar.SetControlsEnabled(false);
        SaveManager.UnlockRoute(currentLevelIndex);
        int driftScore = gameManager != null ? gameManager.TotalDriftScore : 0;
        int earnedCoins = Mathf.Max(0, collectedCoins + bonusCoinsThisRun + 10 - penaltyCoinsThisRun);
        SaveManager.AddCoins(earnedCoins);
        SaveManager.RecordTime(currentLevelIndex, ElapsedSeconds);
        string medal = course == null ? "BRONZE" : RaceCourse.Medal(ElapsedSeconds, driftScore, Recoveries, course.goldTime, course.silverScore, course.goldScore);
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
            Seconds = ElapsedSeconds,
            BestSeconds = SaveManager.GetBestTime(currentLevelIndex),
            Medal = medal,
            Recoveries = Recoveries,
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
                ? $"{currentLevelIndex + 1:00}  {UiCopy.Title(GetCurrentLevelName())}"
                : "Route: None";
        }

        if (coinText != null)
        {
            coinText.text = $"{collectedCoins}/{activeTargetCoins}";
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
            messageText.text = UiCopy.Title(message);
            messageText.fontSize = Phase == RunPhase.Countdown ? 76 : 28;
            messageUntil = Time.time + 1.7f;
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

        if (course != null && course.gates.Length > 0)
        {
            int total = course.gates.Length * course.laps;
            Transform gate = course.gates[PassedGates % course.gates.Length];
            float gateDistance = playerCar != null ? Vector3.Distance(playerCar.transform.position, gate.position) : 0f;
            string gateDirection = playerCar != null ? GetDirectionLabel(playerCar.transform.forward, gate.position - playerCar.transform.position) : "Ahead";
            SetGuidanceText($"{(int)(ElapsedSeconds / 60):00}:{ElapsedSeconds % 60:00.0}\nGate {Mathf.Min(PassedGates + 1, total)}/{total} · {gateDistance:0}m {gateDirection}");
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
            guidanceText.text = UiCopy.Title(message);
            guidanceText.color = color;
        }
    }

    public void SetPlayerCar(CarController car)
    {
        playerCar = car;
        if (playerCar != null) playerCar.SetControlsEnabled(Phase == RunPhase.Racing);
    }

    public void BeginRun()
    {
        if (RunInProgress) return;
        if (levelComplete) RestartCurrentLevel();
        Phase = RunPhase.Countdown;
        countdown = 3f;
        if (course != null) course.HighlightGate(0);
        if (playerCar != null) playerCar.SetControlsEnabled(false);
        SetMessage("3");
    }

    private void TickRun(float deltaTime)
    {
        if (playerCar == null || deltaTime <= 0f) return;
        if (Phase == RunPhase.Countdown)
        {
            countdown -= deltaTime;
            SetMessage(countdown > 0f
                ? $"{Mathf.CeilToInt(countdown)}"
                : "Go!");
            if (countdown <= 0f)
            {
                Phase = RunPhase.Racing;
                previousPosition = playerCar.transform.position;
                playerCar.SetControlsEnabled(true);
            }
            return;
        }
        if (Phase != RunPhase.Racing) return;
        ElapsedSeconds += deltaTime;
        if (messageText != null && Time.time > messageUntil) messageText.text = string.Empty;
        Vector3 position = playerCar.transform.position;
        overturnedTime = Vector3.Dot(playerCar.transform.up, Vector3.up) < 0.25f ? overturnedTime + deltaTime : 0f;
        if (position.y < -4f || overturnedTime > 2f)
        {
            RecoverCar();
            return;
        }
        if (course != null && course.gates.Length > 0)
        {
            Transform gate = course.gates[PassedGates % course.gates.Length];
            if (RaceCourse.CrossedGate(previousPosition, position, gate, course.gateHalfWidth))
            {
                PassedGates++;
                course.HighlightGate(PassedGates % course.gates.Length);
                AudioManager.Instance?.PlayCoin();
                if (PassedGates >= course.gates.Length * course.laps) CompleteLevel();
                // Gate progress updates in the compact HUD; no repeated center-screen toast.
            }
        }
        previousPosition = position;
    }

    public void RecoverCar()
    {
        if (Phase != RunPhase.Racing || playerCar == null) return;
        Transform recovery = course != null && PassedGates > 0
            ? course.gates[(PassedGates - 1) % course.gates.Length] : levels[currentLevelIndex].spawnPoint;
        Vector3 position = recovery.position + Vector3.up * 0.6f;
        if (PassedGates > 0) position += recovery.forward * 3f;
        playerCar.ResetCar(position, recovery.rotation);
        previousPosition = position;
        overturnedTime = 0f;
        Recoveries++;
        ElapsedSeconds += 3f;
        gameManager?.RegisterObstacleHit(0);
        SetMessage("Recovered · +3s");
    }
}
