using System;

namespace MoreRewards.Core;

/// <summary>What the rewards page asked the plugin to do.</summary>
public enum PageVerb
{
    /// <summary>Lock or unlock the card <see cref="PageCommand.Id"/> at <see cref="PageCommand.Slot"/>.</summary>
    Pin,

    /// <summary>Swap the card out and keep it away for the rest of the run.</summary>
    Ban,

    /// <summary>The shop was opened and wants its list.</summary>
    Shop,

    /// <summary>Buy <see cref="PageCommand.Id"/> from the shop.</summary>
    ShopBuy,

    /// <summary>The player is leaving the screen: turn the points left into cash.</summary>
    CashOut,

    /// <summary>The ban list was opened and wants its rows.</summary>
    Bans,

    /// <summary>Lift the ban made on <see cref="PageCommand.Id"/> (with the rest of its line, if it was banned as one).</summary>
    Unban,

    /// <summary>The settings were opened and want their rows.</summary>
    Settings,

    /// <summary>Set the setting <see cref="PageCommand.Key"/> ("Section.Key") to <see cref="PageCommand.Value"/>.</summary>
    Set,
}

/// <param name="Key">The setting a <see cref="PageVerb.Set"/> changes, as "Section.Key".</param>
/// <param name="Value">The value it is set to, as the page wrote it.</param>
public readonly record struct PageCommand(PageVerb Verb, int Id, int Slot, string? Key = null, string? Value = null);

/// <summary>
/// Reads the messages the rewards page posts to the game, "slmod|morerewards|verb|args", args split by "|". Anything else, or a
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
            "ban" => WithCard(PageVerb.Ban, parts),
            "shop" => new PageCommand(PageVerb.Shop, 0, 0),
            "shopbuy" => parts.Length == 2 && Id(parts[1]) is int id ? new PageCommand(PageVerb.ShopBuy, id, 0) : null,
            "cashout" => new PageCommand(PageVerb.CashOut, 0, 0),
            "bans" => new PageCommand(PageVerb.Bans, 0, 0),
            "unban" => parts.Length == 2 && Id(parts[1]) is int head ? new PageCommand(PageVerb.Unban, head, 0) : null,
            "settings" => new PageCommand(PageVerb.Settings, 0, 0),
            "set" => parts.Length == 3 && parts[1].IndexOf('.') > 0 && parts[2].Length > 0 ? new PageCommand(PageVerb.Set, 0, 0, parts[1], parts[2]) : null,
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
