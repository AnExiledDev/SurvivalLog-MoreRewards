using System;

namespace MoreRewards.Core;

/// <summary>
/// The interest rate the game pays on survival points when a day ends. SurvivalResultsManager.SettleDailySurvivalPoints
/// (rva 0x27fb1f0) takes it from the character's PointsInterest attribute (AttrName 403), plus a share from
/// BTEffectManager.TriggerSurvivalAccumulation that nothing in the game's tables raises. Every playable character
/// starts the attribute at 5%: Config_PlayAttribute rows 1, 4 and 1002, copied in by LeadingRole.Initialize (rva
/// 0x2e69a20). The Savings Plan reward (Config_Talent 30251) never touches the rate; it pays its own 5% of the day's earnings, at most 10,
/// next to the interest.
/// </summary>
public static class InterestRate
{
    public const int GameRate = 5;

    /// <summary>Double the game's.</summary>
    public const int DefaultRate = 10;

    /// <summary>Interest equal to all the points held; the attribute itself stops at 200% (Config_AttributeStandard row 403).</summary>
    public const int MostRate = 100;

    /// <returns>The rate in percent, or null to keep the game's: 0 (or less) changes nothing.</returns>
    public static int? From(int interestRate) => interestRate <= 0 ? null : Math.Min(interestRate, MostRate);

    /// <param name="scaling">The game's units per 1 (GameKey.Attr_ScalingRatio, 1000).</param>
    /// <returns><paramref name="rate"/> in percent as the game's attribute holds it.</returns>
    public static int Units(int rate, float scaling) => (int)Math.Round(rate * (double)scaling / 100);

    /// <returns>
    /// What to hand AttributeComponent.SetStrengtheningValue_Float for <paramref name="units"/>; it stores
    /// <c>(int)(value * scaling)</c>, so the value must not land a hair under the units.
    /// </returns>
    public static float GameValue(int units, float scaling) => units / scaling;
}
