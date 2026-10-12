using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using MoreRewards.Core;
using Il2CppInts = Il2CppSystem.Collections.Generic.List<int>;

namespace MoreRewards.Adapters;

/// <summary>
/// Adds rewards to the end-of-day choice. The game picks the rewards in SurvivalPlanningComponent's
/// GetRandomSurvivalPlanIds: GetRandomSurvivalPlanIds(int) (rva 0x2e129e0) when a day ends, from
/// CountUpTimer.TriggerDaySettlement, and GetRandomSurvivalPlanIds(int, List, List) (rva 0x2e126c0) on a refresh, from
/// SurvivalResultsManager.OnRefreshSurvivalPlans. Both read the day's Config_DailyRandom, draw RandomAmount[i] rewards
/// from each group RandomPlanID[i] with GetRandomIdsFromGroup, and cut the list to 3. A day with no groups gets its
/// fixed SpecifiedPlanID list instead, which is left alone. The hooks run after the game's pick and draw the extras
/// from the same groups the same way, keeping out every reward already offered. Buying does not check the offer list
/// (SurvivalResultsManager.OnBuySurvivalPlan, rva 0x2699610), so an extra reward is bought like the game's own.
/// Static because Harmony hooks are.
/// </summary>
internal static class OfferPatch
{
    private const string DoingDay = "adding end-of-day rewards, the game's own rewards stay";
    private const string DoingRefresh = "adding refresh rewards, the game's own rewards stay";

    private static readonly Type[] DayParameters = { typeof(int) };
    private static readonly Type[] RefreshParameters = { typeof(int), typeof(Il2CppInts), typeof(Il2CppInts) };

    private static int _optionCount = RewardSettings.GameOptionCount;

    public static void Apply(Harmony harmony, int optionCount)
    {
        _optionCount = optionCount;

        var type = typeof(SurvivalPlanningComponent);
        const string method = nameof(SurvivalPlanningComponent.GetRandomSurvivalPlanIds);

        PatchKit.Patch(harmony, typeof(OfferPatch), type, method, DayParameters, nameof(AddToDayRewards),
            $"hooked the end-of-day rewards: up to {optionCount} to choose from",
            "the end of the day keeps the game's 3 rewards");
        PatchKit.Patch(harmony, typeof(OfferPatch), type, method, RefreshParameters, nameof(AddToRefresh),
            $"hooked the rewards refresh: up to {optionCount} to choose from",
            "a refresh keeps the game's 3 rewards");
    }

    private static void AddToDayRewards(SurvivalPlanningComponent __instance, int day, Il2CppInts __result)
    {
        try
        {
            RewardDraw draw = (group, offered) => First(__instance.GetRandomIdsFromGroup(group, 1, day, ToGame(offered)));

            TopUp(__instance, day, __result, "end of day", (offered, groups) => OfferFill.TopUp(offered, _optionCount, groups, draw));
        }
        catch (Exception ex)
        {
            PatchKit.Fail(DoingDay, ex);
        }
    }

    /// <summary>
    /// The game's refresh keeps out rewards already bought and draws only ones not shown before (displayedIds), so the
    /// extras do too, and fall back to ones shown before only when the unseen ones run out and at least one new reward
    /// turned up (<see cref="OfferFill.TopUpRefresh"/>).
    /// </summary>
    private static void AddToRefresh(SurvivalPlanningComponent __instance, int day, Il2CppInts displayedIds, Il2CppInts buyedIds, Il2CppInts __result)
    {
        try
        {
            RewardDraw unseen = (group, offered) => First(__instance.GetRandomIdsFromGroup(group, 1, day, displayedIds, buyedIds, ToGame(offered)));
            RewardDraw seenBefore = (group, offered) => First(__instance.GetRandomIdsFromGroup(group, 1, day, null, buyedIds, ToGame(offered)));

            TopUp(__instance, day, __result, "refresh", (offered, groups) => OfferFill.TopUpRefresh(offered, _optionCount, groups, unseen, seenBefore));
        }
        catch (Exception ex)
        {
            PatchKit.Fail(DoingRefresh, ex);
        }
    }

    private static void TopUp(SurvivalPlanningComponent planning, int day, Il2CppInts offered, string when, Func<IReadOnlyList<int>, IReadOnlyList<RewardGroup>, IReadOnlyList<int>> fill)
    {
        if (planning == null || offered == null || offered.Count >= _optionCount || DayGroups(day) is not { } groups) return;

        var gameCount = offered.Count;
        var added = fill(FromGame(offered), groups);

        foreach (var reward in added) offered.Add(reward);

        var shortBy = _optionCount - offered.Count;

        PatchKit.Log($"day {day} {when}: the game offered {gameCount}, added {added.Count}" + (shortBy > 0 ? $" (the day's groups had no more to give, {shortBy} short)" : ""));
    }

    /// <summary>The day's reward groups, or null on a day the game gives a fixed list, which is left as it is.</summary>
    private static IReadOnlyList<RewardGroup>? DayGroups(int day)
    {
        var config = GameSingleton.Existing<ConfigManager>()?.GetDailyRandomConfig(day);
        var ids = config?.RandomPlanID;
        var amounts = config?.RandomAmount;

        if (ids == null || amounts == null || ids.Count == 0 || amounts.Count == 0) return null;

        var groups = new List<RewardGroup>();

        for (var i = 0; i < ids.Count && i < amounts.Count; i++) groups.Add(new RewardGroup(ids[i], amounts[i]));

        return groups;
    }

    private static int? First(Il2CppInts? rewards) => rewards != null && rewards.Count > 0 ? rewards[0] : null;

    private static List<int> FromGame(Il2CppInts rewards)
    {
        var list = new List<int>(rewards.Count);

        for (var i = 0; i < rewards.Count; i++) list.Add(rewards[i]);

        return list;
    }

    private static Il2CppInts ToGame(IReadOnlyList<int> rewards)
    {
        var list = new Il2CppInts();

        foreach (var reward in rewards) list.Add(reward);

        return list;
    }
}
