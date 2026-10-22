using System;

namespace MoreRewards.Core;

/// <summary>
/// The shop lets the player pick any reward, so it costs more than the card would: the markup on top of the card's
/// price, plus the rise for each buy since the last reset. A reward bought before its needs are met costs double.
/// </summary>
public static class ShopPrice
{
    /// <param name="cardCost">What the card costs on the rewards screen, any discount already taken off.</param>
    /// <param name="isEarly">The reward's needs are not met yet, which only the shop's early buys allow.</param>
    public static int Of(int cardCost, ScreenSettings settings, int buys, bool isEarly)
    {
        var price = (int)Math.Ceiling(Math.Max(0, cardCost) * (100 + Rise(settings, buys)) / 100.0);

        return isEarly ? price * 2 : price;
    }

    /// <summary>How far the shop's prices are above the card prices, in percent, after <paramref name="buys"/> buys.</summary>
    public static int Rise(ScreenSettings settings, int buys) => settings.ShopMarkup + settings.ShopIncrease * Math.Max(0, buys);

    /// <summary>The game's own price for the card: the cost, less the discount the player has earned, rounded.</summary>
    public static int CardCost(int cost, float discountRatio)
    {
        if (discountRatio <= 0f) return cost;

        return Math.Max(0, (int)((1f - discountRatio) * cost + 0.5f));
    }

    /// <summary>Buys left today, or null when there is no daily limit.</summary>
    public static int? LeftToday(ScreenSettings settings, ShopTally tally) =>
        settings.ShopPerDay > 0 ? Math.Max(0, settings.ShopPerDay - tally.Today) : null;
}
