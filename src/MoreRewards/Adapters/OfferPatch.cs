using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using MoreRewards.Core;
using Il2CppInts = Il2CppSystem.Collections.Generic.List<int>;
using Il2CppIntSet = Il2CppSystem.Collections.Generic.ICollection<int>;

namespace MoreRewards.Adapters;

/// <summary>
/// Shapes the end-of-day choice. The game picks the rewards in SurvivalPlanningComponent's GetRandomSurvivalPlanIds:
/// GetRandomSurvivalPlanIds(int, ICollection, int, int) (rva 0x2fd6b40) when a day ends, from
/// CountUpTimer.TriggerDaySettlement, and GetRandomSurvivalPlanIds(int, List, List, ICollection, int, int) (rva 0x2fd6eb0)
/// on a refresh, from SurvivalResultsManager.OnRefreshSurvivalPlans. Both read the day's Config_DailyRandom, draw
/// RandomAmount[i] rewards from each group RandomPlanID[i] with GetRandomIdsFromGroup, and cut the list to 3. A day with
/// no groups gets its fixed SpecifiedPlanID list instead, which only gains the extra locks. The hooks run after the
/// game's pick and hand it to <see cref="OfferPlan"/>, which takes out the rewards the player passed on, puts the extra
/// locks back in their slots and draws the rest from the same groups the same way. Buying does not check the offer
/// list (SurvivalResultsManager.OnBuySurvivalPlan), so an extra reward is bought like the game's own.
/// Both overloads end in a cooldownIds collection (rewards shown recently, which the game keeps out); the extras pass it
/// on to GetRandomIdsFromGroup so they skip the same rewards. The last two ints are the game's pinned reward and its
/// slot. Static because Harmony hooks are.
/// </summary>
internal static class OfferPatch
{
    private const string DoingDay = "shaping the end-of-day rewards, the game's own rewards stay";
    private const string DoingRefresh = "shaping the refresh rewards, the game's own rewards stay";

    private static readonly Type[] DayParameters = { typeof(int), typeof(Il2CppIntSet), typeof(int), typeof(int) };
    private static readonly Type[] RefreshParameters = { typeof(int), typeof(Il2CppInts), typeof(Il2CppInts), typeof(Il2CppIntSet), typeof(int), typeof(int) };

    public static void Apply(Harmony harmony)
    {
        var type = typeof(SurvivalPlanningComponent);
        const string method = nameof(SurvivalPlanningComponent.GetRandomSurvivalPlanIds);

        PatchKit.Patch(harmony, typeof(OfferPatch), type, method, DayParameters, nameof(ShapeDay),
            "hooked the end-of-day rewards", "the end of the day keeps the game's 3 rewards, with no extra locks and nothing kept away");
        PatchKit.Patch(harmony, typeof(OfferPatch), type, method, RefreshParameters, nameof(ShapeRefresh),
            "hooked the rewards refresh", "a refresh keeps the game's 3 rewards, with no extra locks and nothing kept away");
    }

    /// <summary>The day's reward groups, or null on a day the game gives a fixed list.</summary>
    public static IReadOnlyList<RewardGroup>? DayGroups(int day)
    {
        var config = GameSingleton.Existing<ConfigManager>()?.GetDailyRandomConfig(day);
        var ids = config?.RandomPlanID;
        var amounts = config?.RandomAmount;

        if (ids == null || amounts == null || ids.Count == 0 || amounts.Count == 0) return null;

        var groups = new List<RewardGroup>();

        for (var i = 0; i < ids.Count && i < amounts.Count; i++) groups.Add(new RewardGroup(ids[i], amounts[i]));

        return groups;
    }

    /// <summary>
    /// What the player set and passed on. An extra lock on a reward the game would no longer offer (bought, or its
    /// needs gone) is let go here, as the game lets go of its own pin.
    /// </summary>
    public static OfferAsk Ask(SurvivalPlanningComponent planning, int day, int pinnedId, int pinnedSlot)
    {
        var settings = LiveScreen.Settings;
        var book = LiveScreen.Book;
        var extras = book.Locks();
        var kept = extras.Where(held => planning.CanKeepPinnedPlan(held.Id)).ToList();

        if (kept.Count != extras.Count) book.SetLocks(kept);

        return new OfferAsk(settings.OptionCount, pinnedId, pinnedSlot, kept, id => book.IsUnwanted(id, day), settings.NoRepeatOnRefresh);
    }

    public static int? First(Il2CppInts? rewards) => rewards != null && rewards.Count > 0 ? rewards[0] : null;

    public static List<int> FromGame(Il2CppInts rewards)
    {
        var list = new List<int>(rewards.Count);

        for (var i = 0; i < rewards.Count; i++) list.Add(rewards[i]);

        return list;
    }

    public static Il2CppInts ToGame(IReadOnlyList<int> rewards)
    {
        var list = new Il2CppInts();

        foreach (var reward in rewards) list.Add(reward);

        return list;
    }

    private static void ShapeDay(SurvivalPlanningComponent __instance, int day, Il2CppIntSet cooldownIds, int pinnedId, int pinnedSlot, Il2CppInts __result)
    {
        try
        {
            if (__instance == null || __result == null) return;

            RewardDraw draw = (group, offered) => First(__instance.GetRandomIdsFromGroup(group, 1, day, ToGame(offered), cooldownIds));
            var gameOffer = FromGame(__result);

            Replace(__result, gameOffer, OfferPlan.Day(gameOffer, Ask(__instance, day, pinnedId, pinnedSlot), DayGroups(day), draw), $"day {day} end of day");
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
    private static void ShapeRefresh(SurvivalPlanningComponent __instance, int day, Il2CppInts displayedIds, Il2CppInts buyedIds, Il2CppIntSet cooldownIds, int pinnedId, int pinnedSlot, Il2CppInts __result)
    {
        try
        {
            if (__instance == null || __result == null) return;

            RewardDraw unseen = (group, offered) => First(__instance.GetRandomIdsFromGroup(group, 1, day, displayedIds, buyedIds, ToGame(offered), cooldownIds));
            RewardDraw seenBefore = (group, offered) => First(__instance.GetRandomIdsFromGroup(group, 1, day, null, buyedIds, ToGame(offered), cooldownIds));
            var gameOffer = FromGame(__result);

            Replace(__result, gameOffer, OfferPlan.Refresh(gameOffer, Ask(__instance, day, pinnedId, pinnedSlot), DayGroups(day), unseen, seenBefore), $"day {day} refresh");
        }
        catch (Exception ex)
        {
            PatchKit.Fail(DoingRefresh, ex);
        }
    }

    private static void Replace(Il2CppInts result, IReadOnlyList<int> gameOffer, IReadOnlyList<int> offer, string when)
    {
        if (offer.SequenceEqual(gameOffer)) return;

        result.Clear();

        foreach (var reward in offer) result.Add(reward);

        var shortBy = LiveScreen.Settings.OptionCount - offer.Count;

        PatchKit.Log($"{when}: the game offered {gameOffer.Count}, now {offer.Count}" + (shortBy > 0 ? $" ({shortBy} short, the day had no more to give)" : ""));
    }
}
