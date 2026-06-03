using UnityEngine;
using UnityEngine.UI;

public sealed class GameManager : MonoBehaviour
{
    [SerializeField] private CarController playerCar;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private Text speedText;
    [SerializeField] private Text driftStateText;
    [SerializeField] private Text driftScoreText;
    [SerializeField] private Color normalColor = new Color(0.92f, 0.96f, 1f);
    [SerializeField] private Color driftColor = new Color(1f, 0.78f, 0.28f);

    [Header("Drift Score")]
    [SerializeField, Min(0f)] private float scoreRate = 4.4f;
    [SerializeField, Min(0f)] private float minCleanDriftSeconds = 1.1f;
    [SerializeField, Min(0)] private int minRewardScore = 120;
    [SerializeField, Min(1)] private int rewardScoreDivisor = 420;
    [SerializeField, Min(1f)] private float maxComboMultiplier = 5f;
    [SerializeField, Min(0f)] private float comboStep = 0.35f;

    private float currentDriftScore;
    private float totalDriftScore;
    private float comboMultiplier = 1f;
    private float driftTimer;
    private bool driftHadCollision;
    private bool wasDrifting;

    private void Awake()
    {
        if (playerCar == null)
        {
            playerCar = FindFirstObjectByType<CarController>();
        }

        if (levelManager == null)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }

        if (audioManager == null)
        {
            audioManager = AudioManager.Instance != null ? AudioManager.Instance : FindFirstObjectByType<AudioManager>();
        }
    }

    private void Update()
    {
        if (playerCar == null)
        {
            playerCar = FindFirstObjectByType<CarController>();
            if (playerCar == null)
            {
                UpdateDriftScoreText();
                return;
            }
        }

        if (audioManager == null)
        {
            audioManager = AudioManager.Instance != null ? AudioManager.Instance : FindFirstObjectByType<AudioManager>();
        }

        if (levelManager == null)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }

        if (audioManager != null)
        {
            audioManager.SetPlayerCar(playerCar);
        }

        UpdateDrivingHud();
        UpdateDriftScoring(Time.deltaTime);
    }

    public void SetPlayerCar(CarController car)
    {
        playerCar = car;
        if (audioManager == null)
        {
            audioManager = AudioManager.Instance != null ? AudioManager.Instance : FindFirstObjectByType<AudioManager>();
        }

        if (audioManager != null)
        {
            audioManager.SetPlayerCar(playerCar);
        }
    }

    public void ResetRunFeedback()
    {
        currentDriftScore = 0f;
        totalDriftScore = 0f;
        comboMultiplier = 1f;
        driftTimer = 0f;
        driftHadCollision = false;
        wasDrifting = false;
        UpdateDriftScoreText();
    }

    public void RegisterObstacleHit(int coinPenalty)
    {
        if (wasDrifting || currentDriftScore > 1f)
        {
            currentDriftScore = 0f;
            driftTimer = 0f;
            driftHadCollision = true;
            comboMultiplier = 1f;
            wasDrifting = false;
        }
        else
        {
            comboMultiplier = 1f;
        }

        if (levelManager != null)
        {
            levelManager.RegisterObstacleHit(coinPenalty);
        }

        if (audioManager != null)
        {
            audioManager.PlayCrash();
        }

        UpdateDriftScoreText();
    }

    private void UpdateDrivingHud()
    {
        if (playerCar == null)
        {
            return;
        }

        if (speedText != null)
        {
            speedText.text = $"{playerCar.SpeedKmh:0} km/h";
        }

        if (driftStateText != null)
        {
            driftStateText.text = playerCar.IsDrifting ? "Drift" : "Normal";
            driftStateText.color = playerCar.IsDrifting ? driftColor : normalColor;
        }
    }

    private void UpdateDriftScoring(float deltaTime)
    {
        bool isScoringDrift = playerCar != null && playerCar.IsDrifting && playerCar.SpeedKmh >= 18f;
        if (isScoringDrift)
        {
            if (!wasDrifting)
            {
                StartDriftScore();
            }

            float slipBonus = Mathf.Clamp(playerCar.LateralSpeed, 0.5f, 9f);
            float speedBonus = Mathf.InverseLerp(18f, 95f, playerCar.SpeedKmh) + 0.65f;
            currentDriftScore += slipBonus * speedBonus * scoreRate * comboMultiplier * deltaTime * 10f;
            driftTimer += deltaTime;
        }
        else if (wasDrifting)
        {
            FinishDriftScore();
        }

        wasDrifting = isScoringDrift;
        UpdateDriftScoreText();
    }

    private void StartDriftScore()
    {
        currentDriftScore = 0f;
        driftTimer = 0f;
        driftHadCollision = false;
    }

    private void FinishDriftScore()
    {
        int earnedScore = Mathf.RoundToInt(currentDriftScore);
        bool cleanDrift = !driftHadCollision && driftTimer >= minCleanDriftSeconds && earnedScore >= minRewardScore;
        if (cleanDrift)
        {
            totalDriftScore += earnedScore;
            int rewardCoins = Mathf.Clamp(earnedScore / rewardScoreDivisor, 1, 12);
            comboMultiplier = Mathf.Min(maxComboMultiplier, comboMultiplier + comboStep);

            if (levelManager != null)
            {
                levelManager.AwardBonusCoins(rewardCoins, $"Clean drift +{earnedScore} score");
            }

            if (audioManager != null)
            {
                audioManager.PlayDriftReward();
            }
        }
        else if (driftHadCollision)
        {
            comboMultiplier = 1f;
        }

        currentDriftScore = 0f;
        driftTimer = 0f;
        driftHadCollision = false;
    }

    private void UpdateDriftScoreText()
    {
        if (driftScoreText == null)
        {
            return;
        }

        int visibleScore = Mathf.RoundToInt(totalDriftScore + currentDriftScore);
        driftScoreText.text = $"Drift Score: {visibleScore}\nCombo x{comboMultiplier:0.0}";
        driftScoreText.color = currentDriftScore > 1f ? driftColor : normalColor;
    }
}
