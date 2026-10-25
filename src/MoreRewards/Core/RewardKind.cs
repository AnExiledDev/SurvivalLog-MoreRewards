using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MoreRewards.Core;

/// <summary>Whether a reward keeps working for the rest of the run, does its thing once, or lasts a while.</summary>
public readonly record struct RewardLife
{
    public static readonly RewardLife Permanent = new(-1);
    public static readonly RewardLife OneTime = new(0);

    private RewardLife(int seconds) => Seconds = seconds;

    /// <summary>How long the reward lasts, in game seconds (a day is 86400); -1 for permanent and 0 for one-time.</summary>
    public int Seconds { get; }

    public bool IsTimed => Seconds > 0;

    /// <returns>A reward that lasts <paramref name="seconds"/>; none at all is one-time.</returns>
    public static RewardLife Timed(int seconds) => seconds > 0 ? new RewardLife(seconds) : OneTime;
}

/// <summary>
/// Sorts rewards for the shop's chips and the cards' labels, from the script the reward's buff runs (Config_Effect's
/// ConfigName, such as "Buff/Buff_Trap_Damage") and the fields that script is given, and how long the buff lasts.
/// </summary>
public static class RewardKind
{
    public const string DefaultScript = "Buff/Buff_Default";

    /// <summary>Config_Buff.BuffDuring of a buff that acts once, on the spot, and is gone (-1 is a buff that stays).</summary>
    public const float Instant = 1f;

    /// <summary>The first pattern that matches names the category; "body" takes the rest.</summary>
    private static readonly (string Category, Regex Pattern)[] Categories =
    {
        ("recipes", new Regex("UnlockCraftRecipe")),
        ("airdrops", new Regex("AddItemToBag")),
        ("traps", new Regex("Trap")),
        ("farming", new Regex("Plant|Flower|Seed|Vase|Ivy|Harvest|Soil")),
        ("cooking", new Regex("Cook|Perfect")),
        ("power", new Regex("Power|Generator|Solar|Fuel")),
        ("trade", new Regex("Stranger|Trade|Discount|Loan|Savings|Wish")),
        ("skills", new Regex("Exp|Proficiency")),
        ("defense", new Regex("Tower|Defense|Shield|Bite")),
        ("storage", new Regex("Storage|Shelf|Fridge|Grid|Burden")),
        ("crafting", new Regex("Production|Repair|305510")),
        ("food", new Regex("Food|Satiety|Nutrition|Eat|Metabolism|Stomach|Corrosion|Endurance|Process_Adapt|Deep_Sleep|Tired")),
    };

    /// <param name="setFields">The names of the script's fields that are set (not zero or empty); only the default script needs them.</param>
    public static string Category(string script, IEnumerable<string> setFields)
    {
        if (script == DefaultScript)
        {
            var fields = setFields.ToList();

            if (fields.Any(field => field.Contains("Satiety"))) return "food";

            return fields.Any(field => field.Contains("Speed")) ? "speed" : "body";
        }

        return Categories.FirstOrDefault(entry => entry.Pattern.IsMatch(script)).Category ?? "body";
    }

    /// <param name="buffDuring">The reward's Config_Buff.BuffDuring, in game seconds; null for a reward with no buff, a lasting upgrade.</param>
    public static RewardLife Life(string category, float? buffDuring)
    {
        if (category == "airdrops") return RewardLife.OneTime;

        if (buffDuring is not { } during || during < 0) return RewardLife.Permanent;

        return during > Instant ? RewardLife.Timed((int)Math.Round(during)) : RewardLife.OneTime;
    }
}
