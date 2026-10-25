using System;

namespace MoreRewards.Core;

/// <summary>When the shop's price rise goes back to the markup alone.</summary>
public enum ShopReset
{
    Day,
    Week,
    Never,
}

/// <summary>How the shop shows a reward the player cannot buy now.</summary>
public enum ShopUnavailable
{
    Grey,
    Hide,
}

/// <summary>
/// What the player set for the end-of-day screen, held to what the screen can take: at least one card is never locked,
/// so a refresh always has a slot to fill.
/// </summary>
public sealed record ScreenSettings
{
    public const int GameMaxLocked = 1;
    public const int MostBanCost = 99;
    public const int MostUnbanRefund = 100;
    public const int MostCashOut = 1000;
    public const int MostShopMarkup = 200;
    public const int MostShopIncrease = 100;
    public const int MostShopPerDay = 99;

    public static readonly ScreenSettings Game = From(new ScreenInput());

    public int OptionCount { get; private init; }

    /// <summary>How many cards can be locked at once; above the game's 1, the mod keeps the others.</summary>
    public int MaxLocked { get; private init; }

    public bool NoRepeatOnRefresh { get; private init; }
    public bool BanButton { get; private init; }
    public int BanCost { get; private init; }
    public bool BanWholeLine { get; private init; }

    /// <summary>The percent of a ban's cost paid back when it is removed.</summary>
    public int UnbanRefund { get; private init; }

    public bool Labels { get; private init; }

    /// <summary>Cash per survival point left when the player leaves the screen; 0 is off.</summary>
    public int CashOut { get; private init; }

    public bool ShopEnabled { get; private init; }
    public bool ShopHidesUnavailable { get; private init; }
    public int ShopMarkup { get; private init; }
    public int ShopIncrease { get; private init; }
    public ShopReset ShopReset { get; private init; }

    /// <summary>Shop purchases allowed a day; 0 is no limit.</summary>
    public int ShopPerDay { get; private init; }

    public bool ShopLocked { get; private init; }

    public bool LocksMore => MaxLocked > GameMaxLocked;

    public static ScreenSettings From(ScreenInput input)
    {
        var optionCount = Math.Clamp(input.OptionCount, RewardSettings.GameOptionCount, RewardSettings.MostOptions);

        return new ScreenSettings
        {
            OptionCount = optionCount,
            MaxLocked = Math.Clamp(input.MaxLocked, GameMaxLocked, optionCount - 1),
            NoRepeatOnRefresh = input.NoRepeatOnRefresh,
            BanButton = input.BanButton,
            BanCost = Math.Clamp(input.BanCost, 0, MostBanCost),
            BanWholeLine = input.BanWholeLine,
            UnbanRefund = Math.Clamp(input.UnbanRefund, 0, MostUnbanRefund),
            Labels = input.Labels,
            CashOut = Math.Clamp(input.CashOut, 0, MostCashOut),
            ShopEnabled = input.ShopEnabled,
            ShopHidesUnavailable = input.ShopHidesUnavailable,
            ShopMarkup = Math.Clamp(input.ShopMarkup, 0, MostShopMarkup),
            ShopIncrease = Math.Clamp(input.ShopIncrease, 0, MostShopIncrease),
            ShopReset = input.ShopReset,
            ShopPerDay = Math.Clamp(input.ShopPerDay, 0, MostShopPerDay),
            ShopLocked = input.ShopLocked,
        };
    }
}

/// <summary>The settings as the config file holds them, before <see cref="ScreenSettings.From"/> checks them. Defaults leave the screen as the game has it.</summary>
public sealed record ScreenInput
{
    public int OptionCount { get; init; } = RewardSettings.GameOptionCount;
    public int MaxLocked { get; init; } = ScreenSettings.GameMaxLocked;
    public bool NoRepeatOnRefresh { get; init; }
    public bool BanButton { get; init; }
    public int BanCost { get; init; }
    public bool BanWholeLine { get; init; }
    public int UnbanRefund { get; init; }
    public bool Labels { get; init; }
    public int CashOut { get; init; }
    public bool ShopEnabled { get; init; }
    public bool ShopHidesUnavailable { get; init; }
    public int ShopMarkup { get; init; }
    public int ShopIncrease { get; init; }
    public ShopReset ShopReset { get; init; }
    public int ShopPerDay { get; init; }
    public bool ShopLocked { get; init; }
}
