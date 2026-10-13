using System;

namespace MoreRewards.Core;

/// <summary>
/// What the player asked for, held to what the game can take. At the defaults (the game's three rewards and its own
/// refresh cost) the mod changes nothing at all.
/// </summary>
public sealed record RewardSettings
{
    /// <summary>How many rewards the game itself offers at the end of a day.</summary>
    public const int GameOptionCount = 3;

    public const int MostOptions = 20;

    /// <summary>The setting's value for a cost that follows the number of rewards.</summary>
    public const int AutomaticRefreshCost = -1;

    public const int MostRefreshCost = 99;

    private RewardSettings(int optionCount, int? fixedRefreshCost)
    {
        OptionCount = optionCount;
        FixedRefreshCost = fixedRefreshCost;
    }

    public int OptionCount { get; }

    /// <summary>Null lets the cost follow the number of rewards; 0 makes every refresh free; anything above is the cost of every refresh.</summary>
    public int? FixedRefreshCost { get; }

    public bool AddsOptions => OptionCount > GameOptionCount;

    /// <summary>The automatic cost at three rewards is the game's own, so only a fixed cost or extra rewards change it.</summary>
    public bool ChangesCost => FixedRefreshCost != null || AddsOptions;

    /// <param name="refreshCost">Below 0 is automatic, as <see cref="AutomaticRefreshCost"/> is.</param>
    public static RewardSettings From(int optionCount, int refreshCost) =>
        new(Math.Clamp(optionCount, GameOptionCount, MostOptions), refreshCost < 0 ? null : Math.Min(refreshCost, MostRefreshCost));
}
