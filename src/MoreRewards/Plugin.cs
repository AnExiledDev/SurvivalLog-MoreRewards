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
    private const string Version = "0.1.0";

    public override void Load()
    {
        var isEnabled = Config.Bind("General", "Enabled", true,
            "Turn the mod on. Off leaves the end-of-day rewards and the refresh cost as the game has them.").Value;
        var optionCount = Config.Bind("Rewards", "OptionCount", RewardSettings.GameOptionCount, new ConfigDescription(
            "How many rewards to choose from at the end of a day and after each refresh. 3 is the game's own number.",
            new AcceptableValueRange<int>(RewardSettings.GameOptionCount, RewardSettings.MostOptions))).Value;
        var refreshCost = Config.Bind("Rewards", "RefreshCost", 0, new ConfigDescription(
            "Survival points a refresh costs. 0 is automatic: the game's cost with 3 rewards, going up by 1 for every 2 rewards more (4 rewards cost 3, 6 cost 4, 10 cost 6). Any other number is the cost of every refresh, whatever OptionCount is.",
            new AcceptableValueRange<int>(0, RewardSettings.MostRefreshCost))).Value;
        var settings = RewardSettings.From(optionCount, refreshCost);
        var log = new RewardsLog(Log, Path.Combine(Paths.BepInExRootPath, "more-rewards.log"));

        PatchKit.Log = log.Write;
        log.TrimIfLarge();
        log.Write($"===== More Rewards {Version} started: {(isEnabled ? "on" : "off")}, {settings.OptionCount} rewards, refresh cost {(settings.FixedRefreshCost > 0 ? settings.FixedRefreshCost.ToString() : "automatic")}");

        if (!isEnabled) return;

        if (!settings.AddsOptions && !settings.ChangesCost)
        {
            log.Write("these settings are the game's own, so nothing is changed");

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
}
