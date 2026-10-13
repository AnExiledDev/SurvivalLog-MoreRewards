using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using MoreRewards.Adapters;
using MoreRewards.Core;

namespace MoreRewards;

[BepInPlugin(Guid, "More Rewards", Version)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.morerewards";
    private const string Version = "0.2.0";

    public override void Load()
    {
        var isEnabled = Config.Bind("General", "Enabled", true,
            "Turn the mod on or off. false leaves the end-of-day rewards, the refresh cost and the interest on survival points as the game has them.\n" +
            "开启或关闭本模组。设为 false 时，每天结束时的生存规划、刷新花费和生存点回息都保持游戏原样。").Value;
        var optionCount = Config.Bind("Rewards", "OptionCount", RewardSettings.GameOptionCount, new ConfigDescription(
            "How many rewards you can choose from at the end of a day and after each refresh. 3 is the game's own number, and you can set any number from 3 to 20.\n" +
            "每天结束时和每次刷新后可供选择的生存规划数量。3 是游戏原本的数量，可设为 3 到 20 之间的任意数。",
            new AcceptableValueRange<int>(RewardSettings.GameOptionCount, RewardSettings.MostOptions))).Value;
        var refreshCost = Config.Bind("Rewards", "RefreshCost", RewardSettings.AutomaticRefreshCost, new ConfigDescription(
            "How many survival points a refresh costs. -1 is automatic: the game's cost with 3 rewards, plus 1 for every 2 rewards more (4 rewards cost 3, 6 cost 4, 10 cost 6); 0 makes every refresh free, and any number from 1 to 99 is the cost of every refresh.\n" +
            "每次刷新花费的生存点。-1 为自动：3 个生存规划时与游戏原价相同，每多 2 个加 1 点（4 个花 3 点，6 个花 4 点，10 个花 6 点）；0 表示刷新免费；1 到 99 则是每次刷新的固定花费。",
            new AcceptableValueRange<int>(RewardSettings.AutomaticRefreshCost, RewardSettings.MostRefreshCost))).Value;
        var maxInterest = Config.Bind("Interest", "MaxInterest", InterestCap.DefaultCap, new ConfigDescription(
            "The most interest your survival points can earn when a day ends. The game pays 5% of the points you hold, at least 1 and at most 10; 0 keeps the game's limit of 10, and any number from 1 to 100 sets a new limit.\n" +
            "每天结算时生存点回息的上限。游戏按持有生存点的 5% 发放回息，最少 1 点、最多 10 点；设为 0 保持游戏原本的上限 10，设为 1 到 100 则以该数作为新的上限。",
            new AcceptableValueRange<int>(0, InterestCap.MostCap))).Value;
        var settings = RewardSettings.From(optionCount, refreshCost);
        var interestCap = InterestCap.From(maxInterest);
        var log = new RewardsLog(Log, Path.Combine(Paths.BepInExRootPath, "more-rewards.log"));

        PatchKit.Log = log.Write;
        log.TrimIfLarge();
        log.Write($"===== More Rewards {Version} started: {(isEnabled ? "on" : "off")}, {settings.OptionCount} rewards, refresh cost {RefreshCostText(settings)}, interest up to {interestCap ?? InterestCap.GameCap}");

        if (!isEnabled) return;

        if (interestCap is { } cap) InterestCode.Apply(cap, log.Write);

        if (!settings.AddsOptions && !settings.ChangesCost)
        {
            log.Write("the rewards and the refresh cost are the game's own, so they are left as they are");

            return;
        }

        if (settings.AddsOptions)
        {
            OfferPatch.Apply(new Harmony(Guid), settings.OptionCount);
            RewardsTicker.Checks.Add(new RewardPage(log.Write).Tick);
        }

        if (settings.ChangesCost) RewardsTicker.Checks.Add(new CostField(new CostKeeper(settings), log.Write).Tick);

        RewardsTicker.Log = log.Write;
        AddComponent<RewardsTicker>();
    }

    private static string RefreshCostText(RewardSettings settings) => settings.FixedRefreshCost switch
    {
        null => "automatic",
        0 => "free",
        var cost => cost.ToString(),
    };
}
