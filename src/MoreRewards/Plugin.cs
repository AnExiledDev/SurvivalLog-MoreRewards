using System;
using System.Linq;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using ExiledToolkit;
using ExiledToolkit.Game;
using ExiledToolkit.Kit;
using HarmonyLib;
using MoreRewards.Adapters;
using MoreRewards.Core;

namespace MoreRewards;

[BepInPlugin(Guid, "More Rewards", Version)]
[BepInDependency(ExiledToolkit.Plugin.Guid, ">=0.20.0")]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.morerewards";
    private const string Version = "0.7.7";
    private const long MaxLogBytes = 1024 * 1024;

    public override void Load()
    {
        ModUiSize.Bind(Config, "MoreRewards");

        var enabled = Config.Bind("General", "Enabled", true, new ConfigDescription(
            "Turn the mod on or off. false leaves the end-of-day rewards, the refresh cost and the interest on survival points as the game has them, and takes the mod's buttons off the rewards screen. Applies at once.\n" +
            "开启或关闭本模组。设为 false 时，每天结束时的生存规划、刷新花费和生存点回息都保持游戏原样，生存规划界面上也不再显示本模组的按钮。立即生效。"));
        var optionCount = Config.Bind("Rewards", "OptionCount", RewardSettings.GameOptionCount, new ConfigDescription(
            "How many rewards you can choose from at the end of a day and after each refresh. 3 is the game's number, and you can set any number from 3 to 20.\n" +
            "每天结束时和每次刷新后可供选择的生存规划数量。3 是游戏原本的数量，可设为 3 到 20 之间的任意数。",
            new AcceptableValueRange<int>(RewardSettings.GameOptionCount, RewardSettings.MostOptions), "ModManager.ShowWhen=General.Enabled", WorldRule.Tag));
        var refreshCost = Config.Bind("Rewards", "RefreshCost", RewardSettings.AutomaticRefreshCost, new ConfigDescription(
            "How many survival points a refresh costs. -1 is automatic: the game's cost with 3 rewards, going up by 1 at 4, 6, 8 rewards and so on (4 or 5 rewards cost 3, 6 or 7 cost 4, 10 cost 6); 0 makes every refresh free, and any number from 1 to 99 is the cost of every refresh.\n" +
            "每次刷新花费的生存点。-1 为自动：3 个生存规划时与游戏原价相同，数量到 4、6、8 个等时各加 1 点（4 或 5 个花 3 点，6 或 7 个花 4 点，10 个花 6 点）；0 表示刷新免费；1 到 99 则是每次刷新的固定花费。",
            new AcceptableValueRange<int>(RewardSettings.AutomaticRefreshCost, RewardSettings.MostRefreshCost), "ModManager.ShowWhen=General.Enabled", WorldRule.Tag));
        var interestRate = Config.Bind("Interest", "InterestRate", InterestRate.DefaultRate, new ConfigDescription(
            "The interest rate, in percent, on the survival points you hold when a day ends. 0 keeps the game's rate of 5%, and any number from 1 to 100 sets a new rate; the interest still stops at MaxInterest.\n" +
            "每天结算时持有生存点的回息比例（百分比）。设为 0 保持游戏原本的 5%，设为 1 到 100 则以该数作为新的比例；回息仍受 MaxInterest 上限限制。",
            new AcceptableValueRange<int>(0, InterestRate.MostRate), "ModManager.ShowWhen=General.Enabled", WorldRule.Tag));
        var maxInterest = Config.Bind("Interest", "MaxInterest", InterestCap.DefaultCap, new ConfigDescription(
            "The most interest your survival points can earn when a day ends. The interest is the points you hold times InterestRate, rounded down, and at least 1 while you hold any points; 0 keeps the game's limit of 10, and any number from 1 to 100 sets a new limit.\n" +
            "每天结算时生存点回息的上限。回息为持有生存点乘以 InterestRate 的比例，向下取整，只要持有生存点最少 1 点；设为 0 保持游戏原本的上限 10，设为 1 到 100 则以该数作为新的上限。",
            new AcceptableValueRange<int>(0, InterestCap.MostCap), "ModManager.ShowWhen=General.Enabled", WorldRule.Tag));
        var log = new ModLog(Log, "more-rewards.log") { MaxFileBytes = MaxLogBytes };

        log.Write($"===== More Rewards {Version} started: {(enabled.Value ? "on" : "off")}, interest {InterestText(enabled.Value ? InterestCap.From(maxInterest.Value) : null)}");

        // Every setting is watched from the start, with the mod off too, so it can be turned on in game and each
        // world's own values reach it.
        var capKeeper = WatchCap(enabled, maxInterest, log, log.WriteError);
        var rateKeeper = WatchRate(enabled, interestRate, log);
        var screen = new ScreenConfig(Config, optionCount);
        var board = new SettingsBoard(new ConfigEntryBase[] { enabled, optionCount, refreshCost, interestRate, maxInterest }.Concat(screen.Entries).Distinct().ToList());

        WatchRewards(enabled, optionCount, refreshCost, screen, log);
        RewardsTicker.Log = log.Write;
        AddComponent<RewardsTicker>();
        WatchEnabled(enabled, () => new InterestTerms((rateKeeper.Rate ?? InterestRate.GameRate) / 100f, capKeeper()), board, log);
    }

    /// <summary>
    /// Hooks the rewards screen the first time the mod is on, and turns it off and on after that. Off, the hooks leave
    /// the game's rewards alone and the page takes the mod's buttons away; the hooks stay, so turning it back on needs no
    /// restart. With the mod off from the start the rewards screen is never hooked.
    /// </summary>
    private static void WatchEnabled(ConfigEntry<bool> enabled, Func<InterestTerms> interest, SettingsBoard board, ModLog log)
    {
        RewardPage? page = null;
        var isLoading = true;

        LiveSettings.Watch(enabled, on =>
        {
            if (on == LiveScreen.IsOn) return;

            LiveScreen.IsOn = on;
            LiveScreen.Changed();

            if (on && page == null)
            {
                page = WatchScreen(interest, board, log);
                WarmUpOnWorker(log);
            }

            if (!on) page?.Off();

            if (!isLoading) log.Write(on ? "turned on" : "turned off: the game's rewards, refresh cost and interest, and none of the mod's buttons");
        });

        isLoading = false;
    }

    /// <summary>Compiles the mod's own code on a worker, so the first rewards screen and shop do not pay for it in a frame.</summary>
    private static void WarmUpOnWorker(ModLog log) =>
        Task.Run(() =>
        {
            try
            {
                WarmUp.Run();
            }
            catch (Exception ex)
            {
                // Only the first screen is slower; a worker writes only to the mod's file.
                log.WriteFileOnly("warm-up failed (harmless): " + ex.Message);
            }
        });

    /// <summary>Off, the game's three rewards and refresh cost, and the game's screen.</summary>
    private static void WatchRewards(ConfigEntry<bool> enabled, ConfigEntry<int> optionCount, ConfigEntry<int> refreshCost, ScreenConfig screen, ModLog log)
    {
        RewardSettings SettingsNow() => enabled.Value ? RewardSettings.From(optionCount.Value, refreshCost.Value) : RewardSettings.Game;

        var costKeeper = new CostKeeper(SettingsNow());

        RewardsTicker.Checks.Add(new CostField(costKeeper, log.Write).Tick);

        LiveSettings.Watch(() =>
        {
            var settings = SettingsNow();

            costKeeper.Use(settings);
            log.Write($"{settings.OptionCount} rewards, refresh cost {RefreshCostText(settings)}");
        }, enabled, optionCount, refreshCost);
        LiveSettings.Watch(() =>
        {
            LiveScreen.Settings = enabled.Value ? screen.Read() : ScreenSettings.Game;
            LiveScreen.Changed();
            log.Write("rewards screen: " + LiveScreen.Settings);
        }, screen.Entries.Append(enabled).ToArray());
    }

    /// <summary>Hooks the rewards screen and wires the page's buttons to the actions that carry them out.</summary>
    /// <returns>The page, which the mod tells when it is turned off.</returns>
    private static RewardPage WatchScreen(Func<InterestTerms> interest, SettingsBoard board, ModLog log)
    {
        var hooks = new PatchKit(new Harmony(Guid), log.Write);
        RewardActions? actions = null;
        var page = new RewardPage(log.Write, () => actions?.AskSetup());
        var budget = new FrameBudget();
        var catalog = new RewardCatalog(budget);

        actions = new RewardActions(page, catalog, interest, board, budget, log.Write);
        OfferPatch.Apply(hooks, log.Write);
        ListPatch.Apply(hooks, log.Write);
        RewardsTicker.Checks.Add(page.Tick);
        RewardsTicker.Checks.Add(actions.Tick);
        RewardsTicker.Checks.Add(catalog.Tick);

        return page;
    }

    /// <summary>Off, the character keeps its own rate.</summary>
    private static RateKeeper WatchRate(ConfigEntry<bool> enabled, ConfigEntry<int> interestRate, ModLog log)
    {
        int? RateNow() => enabled.Value ? InterestRate.From(interestRate.Value) : null;

        var rateKeeper = new RateKeeper(RateNow());

        RewardsTicker.Checks.Add(new RateField(rateKeeper, log.Write).Tick);

        LiveSettings.Watch(() =>
        {
            var rate = RateNow();

            rateKeeper.Use(rate);
            LiveScreen.Changed();
            log.Write($"interest rate {RateText(rate)}");
        }, enabled, interestRate);

        return rateKeeper;
    }

    /// <summary>
    /// Writes MaxInterest into the game's code now and after each change, and puts the game's cap back when it, or
    /// the mod, is turned off. The code is only written once the cap differs from the game's, so with MaxInterest or
    /// the mod off from the start the game's code is never touched.
    /// </summary>
    /// <returns>The cap in force now.</returns>
    private static Func<int> WatchCap(ConfigEntry<bool> enabled, ConfigEntry<int> maxInterest, ModLog log, Action<string> logError)
    {
        var written = InterestCap.GameCap;

        LiveSettings.Watch(() =>
        {
            var cap = (enabled.Value ? InterestCap.From(maxInterest.Value) : null) ?? InterestCap.GameCap;

            if (cap == written) return;

            if (!InterestCode.Apply(cap, log.Write, logError)) return;

            written = cap;
            LiveScreen.Changed();
        }, enabled, maxInterest);

        return () => written;
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
