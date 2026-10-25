using BepInEx.Configuration;
using MoreRewards.Core;

namespace MoreRewards;

/// <summary>The config keys for the end-of-day screen's locks, card buttons, cash out and shop, read together into a <see cref="ScreenInput"/>.</summary>
internal sealed class ScreenConfig
{
    private const string Shown = "ModManager.ShowWhen=General.Enabled";
    private const string ShopShown = "ModManager.ShowWhen=Shop.ShopEnabled";

    private readonly ConfigEntry<int> _optionCount;
    private readonly ConfigEntry<int> _maxLocked;
    private readonly ConfigEntry<bool> _noRepeatOnRefresh;
    private readonly ConfigEntry<bool> _banButton;
    private readonly ConfigEntry<int> _banCost;
    private readonly ConfigEntry<bool> _banWholeLine;
    private readonly ConfigEntry<int> _unbanRefund;
    private readonly ConfigEntry<bool> _labels;
    private readonly ConfigEntry<int> _cashOut;
    private readonly ConfigEntry<bool> _shopEnabled;
    private readonly ConfigEntry<ShopUnavailable> _shopUnavailable;
    private readonly ConfigEntry<int> _shopMarkup;
    private readonly ConfigEntry<int> _shopIncrease;
    private readonly ConfigEntry<ShopReset> _shopReset;
    private readonly ConfigEntry<int> _shopPerDay;
    private readonly ConfigEntry<bool> _shopLocked;

    public ScreenConfig(ConfigFile config, ConfigEntry<int> optionCount)
    {
        _optionCount = optionCount;
        _maxLocked = config.Bind("Locks", "MaxLocked", ScreenSettings.GameMaxLocked, new ConfigDescription(
            "How many rewards you can lock at once, so they stay on the screen after a refresh and are still there the next day. 1 is the game's own number; it can go up to 1 less than OptionCount, so a refresh always has a card to change.\n" +
            "可同时锁定的生存规划数量，锁定的规划在刷新后和第二天仍会保留。1 是游戏原本的数量；最多可设为 OptionCount 减 1，保证每次刷新至少有一张卡会变。",
            new AcceptableValueRange<int>(ScreenSettings.GameMaxLocked, RewardSettings.MostOptions - 1), Shown));
        _noRepeatOnRefresh = config.Bind("Cards", "NoRepeatOnRefresh", false, new ConfigDescription(
            "true makes a refresh show rewards you have not seen yet that day, while the day's groups still have others to give.\n" +
            "设为 true 时，刷新只会给出当天还没出现过的生存规划，直到当天的规划组没有其他可给为止。", null, Shown));
        _banButton = config.Bind("Cards", "BanButton", true, new ConfigDescription(
            "Adds a 「Ban」 button to each card. A banned reward is swapped for another and never comes back for the rest of the run.\n" +
            "在每张卡上加一个「禁用」按钮。被禁用的生存规划会换成另一张，本局剩余时间内不会再出现。", null, Shown));
        _banCost = config.Bind("Cards", "BanCost", 2, new ConfigDescription(
            "How many survival points a ban costs. 0 makes it free.\n" +
            "每次禁用花费的生存点。0 表示免费。",
            new AcceptableValueRange<int>(0, ScreenSettings.MostBanCost), Shown));
        _banWholeLine = config.Bind("Cards", "BanWholeLine", false, new ConfigDescription(
            "true makes a ban also ban every level of the same reward (for example all levels of one recipe line).\n" +
            "设为 true 时，禁用一张卡也会禁用同一系列的所有等级（例如同一配方线的全部等级）。", null, Shown));
        _unbanRefund = config.Bind("Cards", "UnbanRefund", 50, new ConfigDescription(
            "How much of what a ban cost, in percent, you get back when you remove it from the 「Bans」 list. Points round down.\n" +
            "在「禁用列表」中解除禁用时退还禁用花费的百分比。生存点向下取整。",
            new AcceptableValueRange<int>(0, ScreenSettings.MostUnbanRefund), Shown));
        _labels = config.Bind("Cards", "Labels", true, new ConfigDescription(
            "Shows on each card whether its reward is permanent, one-time, or lasts for a time (for example 3 DAYS).\n" +
            "在每张卡上显示该生存规划是永久、一次性，还是持续一段时间（例如持续3天）。", null, Shown));
        _cashOut = config.Bind("Cards", "CashOut", 0, new ConfigDescription(
            "Cash you get for each survival point you still hold when you leave the rewards screen; the points are spent. 0 is off and keeps your points.\n" +
            "离开生存规划界面时，每个剩余生存点可换得的现金，生存点会被用掉。0 为关闭，保留生存点。",
            new AcceptableValueRange<int>(0, ScreenSettings.MostCashOut), Shown));
        _shopEnabled = config.Bind("Shop", "ShopEnabled", false, new ConfigDescription(
            "Adds a 「Shop」 button to the rewards screen: a searchable list of every reward you can get, bought with survival points at a higher price than a card.\n" +
            "在生存规划界面加一个「商店」按钮：可搜索所有能获得的生存规划，用生存点购买，价格比卡片更高。", null, Shown));
        _shopUnavailable = config.Bind("Shop", "ShopUnavailable", ShopUnavailable.Grey, new ConfigDescription(
            "What the shop does with a reward you cannot buy now. Grey shows it greyed out with the reason; Hide leaves it out.\n" +
            "商店如何显示现在无法购买的生存规划。Grey 显示为灰色并写明原因；Hide 不显示。", null, ShopShown));
        _shopMarkup = config.Bind("Shop", "ShopMarkup", 25, new ConfigDescription(
            "How much more, in percent, a reward costs in the shop than on a card. Prices round up.\n" +
            "商店价格比卡片价格高出的百分比。价格向上取整。",
            new AcceptableValueRange<int>(0, ScreenSettings.MostShopMarkup), ShopShown));
        _shopIncrease = config.Bind("Shop", "ShopIncrease", 15, new ConfigDescription(
            "How much more, in percent of the card price, each shop purchase adds to every shop price until the prices reset.\n" +
            "每次在商店购买后，所有商店价格再增加的百分比（按卡片价格计），直到价格重置。",
            new AcceptableValueRange<int>(0, ScreenSettings.MostShopIncrease), ShopShown));
        _shopReset = config.Bind("Shop", "ShopReset", ShopReset.Day, new ConfigDescription(
            "When the shop's price increase goes back to the markup alone: every Day, every Week (7 days), or Never.\n" +
            "商店的涨价何时恢复为只有加价：Day 每天，Week 每 7 天，Never 从不。", null, ShopShown));
        _shopPerDay = config.Bind("Shop", "ShopPerDay", 2, new ConfigDescription(
            "How many shop purchases you can make in a day. 0 is no limit.\n" +
            "每天可在商店购买的次数。0 表示不限。",
            new AcceptableValueRange<int>(0, ScreenSettings.MostShopPerDay), ShopShown));
        _shopLocked = config.Bind("Shop", "ShopLocked", true, new ConfigDescription(
            "true lets the shop sell a reward before the reward it needs first, at double the price.\n" +
            "设为 true 时，商店可以在前置规划之前出售生存规划，价格翻倍。", null, ShopShown));
    }

    public ConfigEntryBase[] Entries => new ConfigEntryBase[]
    {
        _optionCount, _maxLocked, _noRepeatOnRefresh, _banButton, _banCost, _banWholeLine, _unbanRefund, _labels, _cashOut,
        _shopEnabled, _shopUnavailable, _shopMarkup, _shopIncrease, _shopReset, _shopPerDay, _shopLocked,
    };

    public ScreenSettings Read() => ScreenSettings.From(new ScreenInput
    {
        OptionCount = _optionCount.Value,
        MaxLocked = _maxLocked.Value,
        NoRepeatOnRefresh = _noRepeatOnRefresh.Value,
        BanButton = _banButton.Value,
        BanCost = _banCost.Value,
        BanWholeLine = _banWholeLine.Value,
        UnbanRefund = _unbanRefund.Value,
        Labels = _labels.Value,
        CashOut = _cashOut.Value,
        ShopEnabled = _shopEnabled.Value,
        ShopHidesUnavailable = _shopUnavailable.Value == ShopUnavailable.Hide,
        ShopMarkup = _shopMarkup.Value,
        ShopIncrease = _shopIncrease.Value,
        ShopReset = _shopReset.Value,
        ShopPerDay = _shopPerDay.Value,
        ShopLocked = _shopLocked.Value,
    });
}
