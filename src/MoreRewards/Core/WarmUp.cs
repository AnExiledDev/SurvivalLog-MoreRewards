using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreRewards.Core;

/// <summary>
/// Runs the mod's own code once on made-up values, on a worker thread at load, so the first rewards screen, shop and
/// ban list do not pay to compile it (and to build the category patterns) in the middle of a frame. Pure .NET only:
/// nothing here touches the game.
/// </summary>
public static class WarmUp
{
    public static void Run()
    {
        var settings = ScreenSettings.Game;
        var book = new RunBook(new ScratchCounters());

        book.SetLocks(LockBook.With(book.Locks(), new RewardLock(4, 1)));
        LockBook.Click(4, 0, book.Locks(), settings.MaxLocked);
        book.Ban(1, new[] { 2 }, 3);
        book.RecordShopBuy(1, settings.ShopReset);

        var bans = book.Bans(new[] { 1, 2, 3 });
        var tally = book.Shop(1, settings.ShopReset);
        var line = RewardLine.Of(1, new Dictionary<int, IReadOnlyList<int>> { [1] = new[] { 2 }, [2] = Array.Empty<int>() });

        book.Unban(1, line);

        foreach (var language in new[] { Language.English, Language.Chinese })
        {
            var facts = new RewardFacts(true, false, false, false, false, Array.Empty<string>(), new[] { "b" }, 0, true);
            var why = Blocker.Why(facts, 1, language, settings.ShopLocked) ?? Blocker.NoBuysLeft(language);
            var category = RewardKind.Category("Buff/Buff_Trap_Damage", Array.Empty<string>());
            var life = RewardKind.Life(RewardKind.Category(RewardKind.DefaultScript, new[] { "ItemSatietyStart" }), 86400f);
            var cardCost = ShopPrice.CardCost(5, 0.1f);
            var shop = new ShopJson();

            RewardKind.IsTellingField("BuffSpeed");
            shop.Add(new ShopRow(1, "a\"<", "b\n", "", cardCost, ShopPrice.Of(cardCost, settings, tally.Buys, true), why, life, category, true));
            shop.Call(10, ShopPrice.LeftToday(settings, tally), ShopPrice.Rise(settings, tally.Buys), language);
            PageData.SetupCall(new PageSetup(language, settings, 1, book.Locks(), new Dictionary<int, RewardLife> { [1] = life }, 0.05f, 10));
            PageData.BansCall(bans.Select(ban => new BanRow(ban.Head, new[] { "a" }, BanRefund.Of(ban, 2, 50))).ToList(), 50, language);
            PageData.SettingsCall(new[] { new SettingRow("A.B", "A", "B", SettingKind.Choice, "x", 0, 1, new[] { "x" }, "d", false, true) }, language);
            PageData.ToastCall(Blocker.Say(language, "a", "b"));
        }

        PageReply.Read("a note\n" + PageReply.NeedsSetup);
        PageReply.Read(PageReply.Missing);
        PageCommands.Read(PageCommands.Prefix + "pin|1|0");
        PageCommands.Read(PageCommands.Prefix + "shopbuy|1");

        var watch = new SetupWatch();

        watch.Ask();
        watch.Step(0f, true, 1, 1);
        watch.Sent(1, 1);
        new SlicedJob(new[] { true, true }).Advance(() => false);

        // The catalog's cache: a reference-type value shares its compiled code with the catalog's table.
        var budget = new FrameBudget();
        var built = new KeyedBuild<TableKey, string>(_ => new ScratchBuild());

        budget.Start(0f);
        built.Advance(new TableKey(1, 2), budget.IsSpent);
        built.Get(new TableKey(1, 2));
        built.Ready(new TableKey(1, 3));
    }

    private sealed class ScratchBuild : ISlicedBuild<string>
    {
        public string Result { get; private set; } = "";

        public IEnumerable<bool> Steps()
        {
            yield return true;

            Result = "built";
        }
    }

    private sealed class ScratchCounters : ICounters
    {
        private readonly Dictionary<string, int> _values = new();

        public int Get(string key) => _values.TryGetValue(key, out var value) ? value : 0;

        public void Set(string key, int value) => _values[key] = value;
    }
}
