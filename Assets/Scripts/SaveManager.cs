using UnityEngine;

public static class SaveManager
{
    private const string TotalCoinsKey = "PolyCar_TotalCoins";
    private const string SelectedCarKey = "PolyCar_SelectedCar";
    private const string HighestUnlockedRouteKey = "PolyCar_HighestUnlockedRoute";
    private const string SelectedRouteKey = "PolyCar_SelectedRoute";
    private const string UnlockedPrefix = "PolyCar_Unlocked_";

    public static int TotalCoins => PlayerPrefs.GetInt(TotalCoinsKey, 0);
    public static int HighestUnlockedRouteIndex => Mathf.Max(0, PlayerPrefs.GetInt(HighestUnlockedRouteKey, 0));

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
}
