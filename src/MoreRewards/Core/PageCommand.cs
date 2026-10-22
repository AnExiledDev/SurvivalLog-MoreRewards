using System;

namespace MoreRewards.Core;

/// <summary>What the rewards page asked the plugin to do.</summary>
public enum PageVerb
{
    /// <summary>Lock or unlock the card <see cref="PageCommand.Id"/> at <see cref="PageCommand.Slot"/>.</summary>
    Pin,

    /// <summary>Not interested: swap the card out, and keep it away for the rest of the day.</summary>
    Skip,

    /// <summary>Swap the card out and keep it away for the rest of the run.</summary>
    Ban,

    /// <summary>The shop was opened and wants its list.</summary>
    Shop,

    /// <summary>Buy <see cref="PageCommand.Id"/> from the shop.</summary>
    ShopBuy,

    /// <summary>The player is leaving the screen: turn the points left into cash.</summary>
    CashOut,
}

public readonly record struct PageCommand(PageVerb Verb, int Id, int Slot);

/// <summary>
/// Reads the messages the rewards page posts to the game, "slmod|morerewards|verb|id|slot". Anything else, or a
/// message with a bad number, is not ours or not usable and reads as null.
/// </summary>
public static class PageCommands
{
    public const string Prefix = "slmod|morerewards|";

    public static PageCommand? Read(string? message)
    {
        if (message == null || !message.StartsWith(Prefix, StringComparison.Ordinal)) return null;

        var parts = message.Substring(Prefix.Length).Split('|');

        return parts[0] switch
        {
            "pin" => WithCard(PageVerb.Pin, parts),
            "skip" => WithCard(PageVerb.Skip, parts),
            "ban" => WithCard(PageVerb.Ban, parts),
            "shop" => new PageCommand(PageVerb.Shop, 0, 0),
            "shopbuy" => parts.Length == 2 && Id(parts[1]) is int id ? new PageCommand(PageVerb.ShopBuy, id, 0) : null,
            "cashout" => new PageCommand(PageVerb.CashOut, 0, 0),
            _ => null,
        };
    }

    private static PageCommand? WithCard(PageVerb verb, string[] parts)
    {
        if (parts.Length != 3 || Id(parts[1]) is not int id || !int.TryParse(parts[2], out var slot) || slot < 0) return null;

        return new PageCommand(verb, id, slot);
    }

    private static int? Id(string text) => int.TryParse(text, out var id) && id > 0 ? id : null;
}
