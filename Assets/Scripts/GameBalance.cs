public static class GameBalance
{
    public const int DailyRewardCoins = 20;
    public const int SpinRewardMinCoins = 8;
    public const int SpinRewardMaxCoins = 25;

    public const int LightObstaclePenaltyCoins = 1;
    public const int HeavyObstaclePenaltyCoins = 2;

    private static readonly int[] CarUnlockCosts = { 0, 30, 75, 140 };

    public static int GetCarUnlockCost(int carIndex)
    {
        return carIndex >= 0 && carIndex < CarUnlockCosts.Length
            ? CarUnlockCosts[carIndex]
            : 160 + carIndex * 40;
    }
}
