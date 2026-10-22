using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MoreRewards.Core;

/// <summary>Whether a reward keeps working for the rest of the run or does its thing once.</summary>
public enum RewardLife
{
    Permanent,
    OneTime,
}

/// <summary>
/// Sorts rewards for the shop's chips and the cards' labels, from the script the reward's buff runs (Config_Effect's
/// ConfigName, such as "Buff/Buff_Trap_Damage") and the fields that script is given.
/// </summary>
public static class RewardKind
{
    public const string DefaultScript = "Buff/Buff_Default";

    /// <summary>A buff that ticks once a day (86400 seconds) and is then gone.</summary>
    public const float OneDay = 86400f;

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

    /// <summary>Scripts that act once: a bonus paid on the spot, a one-off fire fight, a start-of-day notice.</summary>
    private static readonly Regex OnceScripts = new("Buff_306110|Buff_306310|Buff_306410|Multitask|Annual_Bonus|Default_StartNotify|Buff_3058|Buff_3059|Fight_Fire");

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

    public static RewardLife Life(string category, string script, float buffInterval) =>
        category == "airdrops" || buffInterval == OneDay || OnceScripts.IsMatch(script) ? RewardLife.OneTime : RewardLife.Permanent;
}
