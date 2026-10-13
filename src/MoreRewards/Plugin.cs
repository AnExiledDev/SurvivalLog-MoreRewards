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
    private const string Version = "0.3.0";

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
            "How many survival points a refresh costs. -1 is automatic: the game's cost with 3 rewards, going up by 1 at 4, 6, 8 rewards and so on (4 or 5 rewards cost 3, 6 or 7 cost 4, 10 cost 6); 0 makes every refresh free, and any number from 1 to 99 is the cost of every refresh.\n" +
            "每次刷新花费的生存点。-1 为自动：3 个生存规划时与游戏原价相同，数量到 4、6、8 个等时各加 1 点（4 或 5 个花 3 点，6 或 7 个花 4 点，10 个花 6 点）；0 表示刷新免费；1 到 99 则是每次刷新的固定花费。",
            new AcceptableValueRange<int>(RewardSettings.AutomaticRefreshCost, RewardSettings.MostRefreshCost))).Value;
        var interestRate = Config.Bind("Interest", "InterestRate", InterestRate.DefaultRate, new ConfigDescription(
            "The interest rate, in percent, on the survival points you hold when a day ends. 0 keeps the game's rate of 5%, and any number from 1 to 100 sets a new rate; the interest still stops at MaxInterest.\n" +
            "每天结算时持有生存点的回息比例（百分比）。设为 0 保持游戏原本的 5%，设为 1 到 100 则以该数作为新的比例；回息仍受 MaxInterest 上限限制。",
            new AcceptableValueRange<int>(0, InterestRate.MostRate))).Value;
        var maxInterest = Config.Bind("Interest", "MaxInterest", InterestCap.DefaultCap, new ConfigDescription(
            "The most interest your survival points can earn when a day ends. The interest is the points you hold times InterestRate, rounded down, and at least 1 while you hold any points; 0 keeps the game's limit of 10, and any number from 1 to 100 sets a new limit.\n" +
            "每天结算时生存点回息的上限。回息为持有生存点乘以 InterestRate 的比例，向下取整，只要持有生存点最少 1 点；设为 0 保持游戏原本的上限 10，设为 1 到 100 则以该数作为新的上限。",
            new AcceptableValueRange<int>(0, InterestCap.MostCap))).Value;
        var settings = RewardSettings.From(optionCount, refreshCost);
        var rate = InterestRate.From(interestRate);
        var interestCap = InterestCap.From(maxInterest);
        var log = new RewardsLog(Log, Path.Combine(Paths.BepInExRootPath, "more-rewards.log"));

        PatchKit.Log = log.Write;
        log.TrimIfLarge();
        log.Write($"===== More Rewards {Version} started: {(isEnabled ? "on" : "off")}, {settings.OptionCount} rewards, refresh cost {RefreshCostText(settings)}, interest rate {RateText(rate)}, interest {InterestText(interestCap)}");

        if (!isEnabled) return;

        if (interestCap is { } cap) InterestCode.Apply(cap, log.Write);

        if (rate is { } keptRate) RewardsTicker.Checks.Add(new RateField(new RateKeeper(keptRate), log.Write).Tick);

        if (!settings.AddsOptions && !settings.ChangesCost) log.Write("the rewards and the refresh cost are the game's own, so they are left as they are");

        if (settings.AddsOptions)
        {
            OfferPatch.Apply(new Harmony(Guid), settings.OptionCount);
            RewardsTicker.Checks.Add(new RewardPage(log.Write).Tick);
        }

        if (settings.ChangesCost) RewardsTicker.Checks.Add(new CostField(new CostKeeper(settings), log.Write).Tick);

        if (RewardsTicker.Checks.Count == 0) return;

        RewardsTicker.Log = log.Write;
        AddComponent<RewardsTicker>();
    }

    private static string RefreshCostText(RewardSettings settings) => settings.FixedRefreshCost switch
    {
        null => "automatic",
        0 => "free",
        int cost => cost.ToString(),
    };

    private static string RateText(int? rate) => rate is { } kept ? $"{kept}%" : $"off (the game's {InterestRate.GameRate}%)";

    private static string InterestText(int? interestCap) =>
        interestCap is { } cap ? $"up to {cap}" : $"off (the game's cap of {InterestCap.GameCap})";
}
