using System;
using System.Collections.Generic;
using System.Linq;
using ExiledToolkit.Game;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>
/// Pays out the rewards that act once, when they are bought: a lottery's survival points, a supply crate's items and
/// the free bans of a reward that can be bought again.
/// Every buy goes through SurvivalResultsManager.OnBuySurvivalPlan, the shop's too. It takes the points and adds the
/// reward, or does neither when the player cannot pay, so a buy went through when the points went down or the owned
/// list grew. Items go in the bag the way the game's item rewards do (AE_AddItemInBag): one at a time, and what does
/// not fit is dropped at the player's feet. Static because Harmony hooks are.
/// </summary>
internal static class BuyPatch
{
    private const int HoursPerDay = 24;
    private const string Doing = "paying out a bought reward";

    private static readonly Random Dice = new();

    private static Action<string> _log = _ => { };
    private static Action<string, string> _tell = (_, _) => { };
    private static Func<IReadOnlyList<MrGrant>> _grants = () => Array.Empty<MrGrant>();
    private static PatchKit? _hooks;

    /// <summary>The points held and the rewards owned before a buy. Not read when the hook before it failed.</summary>
    internal readonly record struct BuyMark(int Points, int Owned, bool IsRead);

    public static void Apply(PatchKit hooks, Func<IReadOnlyList<MrGrant>> grants, Action<string, string> tell, Action<string> log)
    {
        _hooks = hooks;
        _grants = grants;
        _tell = tell;
        _log = log;
        hooks.Patch(typeof(BuyPatch), typeof(SurvivalResultsManager), nameof(SurvivalResultsManager.OnBuySurvivalPlan), new[] { typeof(int) }, prefix: nameof(Before), postfix: nameof(After),
            done: "hooked buying a reward", consequence: "lottery tickets and supply crates pay out nothing, and new perks show up within a second instead of at once");
    }

    private static void Before(SurvivalResultsManager __instance, out BuyMark __state)
    {
        __state = default;

        try
        {
            __state = new BuyMark(__instance?.GetSurvivalPoint() ?? 0, OwnedCount(), IsRead: true);
        }
        catch (Exception ex)
        {
            _hooks?.Fail(Doing, ex);
        }
    }

    private static void After(SurvivalResultsManager __instance, int talentId, BuyMark __state)
    {
        try
        {
            if (!LiveScreen.IsOn || __instance == null || !__state.IsRead) return;

            var row = GameSingleton.Existing<ConfigManager>()?.Get_Config_Talent(talentId);
            var cost = ShopPrice.CardCost(row?.LvUpCost ?? 1, LeadingRole.GetSettlementBuyDiscountRatio());
            var isBought = row != null && ShopPrice.IsBought(__state.Points, __instance.GetSurvivalPoint(), __state.Owned, OwnedCount(), cost, RunGame.Role() is { IsDead: false });

            if (!isBought) return;

            var grants = _grants();

            PayLotteries(__instance, Payouts.LotteriesOf(grants, talentId));
            GiveItems(Payouts.CratesOf(grants, talentId));

            // A reward that can be bought again is never owned, so Perks never counts its bans. They are added now.
            if (row?.IsReget == true) LiveScreen.AddFreeBans(Payouts.BanTokensOf(grants, talentId));

            OfferPatch.Bought(talentId);
            PerkWatch.Refresh();
        }
        catch (Exception ex)
        {
            _hooks?.Fail(Doing, ex);
        }
    }

    internal static int OwnedCount() => RunGame.Planning()?._SaveCache_k__BackingField?.Count ?? 0;

    private static void PayLotteries(SurvivalResultsManager results, IReadOnlyList<PointRange> lotteries)
    {
        foreach (var range in lotteries)
        {
            var won = Payouts.Draw(range, Dice.Next);

            if (won > 0) results.AddSurvivalPoint(won);

            _tell($"Lottery ticket: {won} survival points", $"彩票：{won} 生存点");
            _log($"lottery ticket paid {won} survival points ({range.Least} to {range.Most})");
        }
    }

    private static void GiveItems(IReadOnlyList<CrateItem> crate)
    {
        if (crate.Count == 0) return;

        var world = GameSingleton.Existing<BattleLogicWorld>();
        var config = GameSingleton.Existing<ConfigManager>();
        var role = world?._AgentManager?.GetLeadingRole();

        if (world == null || config == null || role == null || world._ItemManager is not { } bag) return;

        var hour = world._GameTimeManager?.GetHour() ?? 0;
        var given = new List<string>();
        var droppedAny = false;

        foreach (var (item, count) in crate)
        {
            if (config.Get_Config_Item(item) is not { } row) continue;

            var timeLeft = HoursPerDay * row.Life;
            var dropped = count - IntoBag(bag, role.InstanceId, item, count, hour, timeLeft);

            if (dropped > 0) world._DropItemManager?.DropItem(item, dropped, hour, timeLeft);

            droppedAny |= dropped > 0;
            given.Add($"{count} {ConstantTextTools.GetLocalText(row.ItemName) ?? item.ToString()}");
        }

        if (given.Count == 0) return;

        var list = string.Join(", ", given);

        _tell($"Supply crate: {list}" + (droppedAny ? " (your bag was full, so some are on the ground)" : ""), $"补给箱：{list}" + (droppedAny ? "（背包已满，部分物品掉在地上）" : ""));
        _log($"supply crate gave {list}" + (droppedAny ? ", some on the ground" : ""));
    }

    /// <returns>How many went in; the game adds one at a time, and stops at the first that does not fit.</returns>
    private static int IntoBag(ItemManager bag, long owner, int item, int count, int hour, int timeLeft)
    {
        for (var k = 0; k < count; k++)
        {
            if (!bag.TryAddItemToBag(owner, item, 1, hour, timeLeft, 0, 1f, null, 0, null, null, 0, 0, false, false, false, false, 0)) return k;
        }

        return count;
    }
}
