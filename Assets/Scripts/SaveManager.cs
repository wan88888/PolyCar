using System;
using UnityEngine;

public static class SaveManager
{
    private const string TotalCoinsKey = "PolyCar_TotalCoins";
    private const string SelectedCarKey = "PolyCar_SelectedCar";
    private const string HighestUnlockedRouteKey = "PolyCar_HighestUnlockedRoute";
    private const string SelectedRouteKey = "PolyCar_SelectedRoute";
    private const string MusicEnabledKey = "PolyCar_MusicEnabled";
    private const string SoundEnabledKey = "PolyCar_SoundEnabled";
    private const string LastDailyClaimKey = "PolyCar_LastDailyClaim";
    private const string UnlockedPrefix = "PolyCar_Unlocked_";
    private const string RouteCompletedPrefix = "PolyCar_RouteCompleted_";
    private const string BestRouteCoinsPrefix = "PolyCar_BestRouteCoins_";
    private const string BestRouteDriftPrefix = "PolyCar_BestRouteDrift_";

    public static int TotalCoins => PlayerPrefs.GetInt(TotalCoinsKey, 0);
    public static int HighestUnlockedRouteIndex => Mathf.Max(0, PlayerPrefs.GetInt(HighestUnlockedRouteKey, 0));
    public static bool MusicEnabled => PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
    public static bool SoundEnabled => PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;

    public static void EnsureCarUnlocked(string carId)
    {
        if (!IsCarUnlocked(carId))
        {
            UnlockCar(carId);
        }
    }

    public static bool IsCarUnlocked(string carId)
    {
        return PlayerPrefs.GetInt(UnlockedPrefix + carId, 0) == 1;
    }

    public static void UnlockCar(string carId)
    {
        PlayerPrefs.SetInt(UnlockedPrefix + carId, 1);
        PlayerPrefs.Save();
    }

    public static bool TrySpendCoins(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (TotalCoins < amount)
        {
            return false;
        }

        PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins - amount);
        PlayerPrefs.Save();
        return true;
    }

    public static void AddCoins(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins + amount);
        PlayerPrefs.Save();
    }

    public static string GetSelectedCar(string fallbackCarId)
    {
        string selectedCar = PlayerPrefs.GetString(SelectedCarKey, fallbackCarId);
        return string.IsNullOrWhiteSpace(selectedCar) ? fallbackCarId : selectedCar;
    }

    public static void SetSelectedCar(string carId)
    {
        PlayerPrefs.SetString(SelectedCarKey, carId);
        PlayerPrefs.Save();
    }

    public static int GetSelectedRouteIndex(int fallbackRouteIndex)
    {
        int selectedRoute = PlayerPrefs.GetInt(SelectedRouteKey, fallbackRouteIndex);
        return Mathf.Max(0, selectedRoute);
    }

    public static void SetSelectedRoute(int routeIndex)
    {
        PlayerPrefs.SetInt(SelectedRouteKey, Mathf.Max(0, routeIndex));
        PlayerPrefs.Save();
    }

    public static bool IsRouteUnlocked(int routeIndex)
    {
        return routeIndex <= HighestUnlockedRouteIndex;
    }

    public static void UnlockRoute(int routeIndex)
    {
        routeIndex = Mathf.Max(0, routeIndex);
        if (routeIndex <= HighestUnlockedRouteIndex)
        {
            return;
        }

        PlayerPrefs.SetInt(HighestUnlockedRouteKey, routeIndex);
        PlayerPrefs.Save();
    }

    public static bool IsRouteCompleted(int routeIndex)
    {
        return PlayerPrefs.GetInt(RouteCompletedPrefix + Mathf.Max(0, routeIndex), 0) == 1;
    }

    public static int GetBestRouteCoins(int routeIndex)
    {
        return PlayerPrefs.GetInt(BestRouteCoinsPrefix + Mathf.Max(0, routeIndex), 0);
    }

    public static int GetBestRouteDriftScore(int routeIndex)
    {
        return PlayerPrefs.GetInt(BestRouteDriftPrefix + Mathf.Max(0, routeIndex), 0);
    }

    public static void RecordRouteResult(int routeIndex, int earnedCoins, int driftScore, out bool firstCompletion, out bool newBestCoins, out bool newBestDrift)
    {
        routeIndex = Mathf.Max(0, routeIndex);
        earnedCoins = Mathf.Max(0, earnedCoins);
        driftScore = Mathf.Max(0, driftScore);

        firstCompletion = !IsRouteCompleted(routeIndex);
        newBestCoins = earnedCoins > GetBestRouteCoins(routeIndex);
        newBestDrift = driftScore > GetBestRouteDriftScore(routeIndex);

        PlayerPrefs.SetInt(RouteCompletedPrefix + routeIndex, 1);
        if (newBestCoins)
        {
            PlayerPrefs.SetInt(BestRouteCoinsPrefix + routeIndex, earnedCoins);
        }

        if (newBestDrift)
        {
            PlayerPrefs.SetInt(BestRouteDriftPrefix + routeIndex, driftScore);
        }

        PlayerPrefs.Save();
    }

    public static int GetCompletedRouteCount(int levelCount)
    {
        int completed = 0;
        for (int i = 0; i < levelCount; i++)
        {
            if (IsRouteCompleted(i))
            {
                completed++;
            }
        }

        return completed;
    }

    public static int GetBestCompletedRouteIndex(int levelCount)
    {
        for (int i = levelCount - 1; i >= 0; i--)
        {
            if (IsRouteCompleted(i))
            {
                return i;
            }
        }

        return -1;
    }

    public static bool HasClaimedDailyReward(DateTime date)
    {
        return PlayerPrefs.GetString(LastDailyClaimKey, string.Empty) == date.ToString("yyyyMMdd");
    }

    public static void SetDailyRewardClaimed(DateTime date)
    {
        PlayerPrefs.SetString(LastDailyClaimKey, date.ToString("yyyyMMdd"));
        PlayerPrefs.Save();
    }

    public static void SetMusicEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void SetSoundEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(SoundEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}
