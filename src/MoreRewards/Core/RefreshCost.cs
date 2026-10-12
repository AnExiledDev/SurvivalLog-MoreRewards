namespace MoreRewards.Core;

/// <summary>
/// The price of refreshing the end-of-day rewards. The automatic cost is the game's own cost minus one, plus half the
/// number of rewards rounded down. With the game's cost of 2 that is 2 for 3 rewards, 3 for 4 or 5, 4 for 6 or 7, and
/// 6 for 10: the more rewards a refresh rolls, the more it costs, and three rewards cost what the game charges.
/// </summary>
public static class RefreshCost
{
    public static int Auto(int gameCost, int optionCount) => gameCost <= 0 ? gameCost : gameCost - 1 + optionCount / 2;

    /// <summary>A fixed cost the player set wins; otherwise the cost follows the number of rewards.</summary>
    public static int Target(int gameCost, RewardSettings settings) =>
        settings.FixedRefreshCost > 0 ? settings.FixedRefreshCost : Auto(gameCost, settings.OptionCount);
}
