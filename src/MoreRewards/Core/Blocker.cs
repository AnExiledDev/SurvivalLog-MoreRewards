using System.Collections.Generic;

namespace MoreRewards.Core;

public enum Language
{
    English,
    Chinese,
}

/// <summary>
/// What the game knows about a reward that decides whether the player can get it now, as the game checks it in
/// SurvivalPlanningComponent.IsValidTalentId (rva 0x307b6a0 in game 1.1.20151).
/// </summary>
/// <param name="IsAvailable">Config_Talent.Available: the game has the reward switched on.</param>
/// <param name="IsModeBlocked">The reward's DemoTwoMode or EndlessMode keeps it out of this kind of run.</param>
/// <param name="OnlyFor">The characters it is for, by name, when it is not for the one being played; empty otherwise.</param>
/// <param name="MissingNeeds">The rewards it needs first that the player does not have, by name.</param>
/// <param name="FromDay">The first day it can be offered, 0 for any day.</param>
/// <param name="IsValid">The game's own answer, which also covers conditions the mod does not spell out.</param>
public sealed record RewardFacts(
    bool IsAvailable,
    bool IsBanned,
    bool IsOwned,
    bool IsReget,
    bool IsModeBlocked,
    IReadOnlyList<string> OnlyFor,
    IReadOnlyList<string> MissingNeeds,
    int FromDay,
    bool IsValid);

/// <summary>Why the player cannot get a reward right now, in the game's language, or null when they can.</summary>
public static class Blocker
{
    /// <param name="allowsEarly">The shop sells rewards before their needs are met (at double price).</param>
    public static string? Why(RewardFacts facts, int day, Language language, bool allowsEarly = false)
    {
        if (!facts.IsAvailable) return Say(language, "Switched off in the game", "游戏中已关闭");

        if (facts.IsBanned) return Say(language, "Banned this run", "本局已禁用");

        if (facts.IsOwned && !facts.IsReget) return Say(language, "You own it", "已拥有");

        if (facts.IsModeBlocked) return Say(language, "Not in this game mode", "当前模式不可用");

        if (facts.OnlyFor.Count > 0) return language == Language.Chinese ? "仅限 " + string.Join("、", facts.OnlyFor) : "For " + string.Join(", ", facts.OnlyFor) + " only";

        if (facts.FromDay > day) return language == Language.Chinese ? "第 " + facts.FromDay + " 天起" : "From day " + facts.FromDay;

        if (facts.MissingNeeds.Count > 0)
        {
            if (allowsEarly) return null;

            return language == Language.Chinese ? "需要先获得 " + string.Join("、", facts.MissingNeeds) : "Needs " + string.Join(", ", facts.MissingNeeds) + " first";
        }

        return facts.IsValid ? null : Say(language, "Not available yet", "暂不可用");
    }

    public static string NoBuysLeft(Language language) => Say(language, "No shop buys left today", "今日购买次数已用完");

    public static string Say(Language language, string english, string chinese) => language == Language.Chinese ? chinese : english;
}
