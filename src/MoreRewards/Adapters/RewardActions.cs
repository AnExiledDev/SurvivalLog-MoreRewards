using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using MoreRewards.Core;
using Il2CppIntSet = Il2CppSystem.Collections.Generic.ICollection<int>;

namespace MoreRewards.Adapters;

/// <summary>The interest the screen previews: the fraction of the points held paid when the day ends, and the most a day pays.</summary>
internal readonly record struct InterestTerms(float Rate, int Cap);

/// <summary>
/// Carries out what the rewards page asks for (locks, bans and the ban list, the shop, the settings, cashing out) on the next frame,
/// and tells the page what it needs to draw the mod's buttons whenever the screen's card list is rebuilt or the page
/// asks for it. Every action checks the screen still shows what the page saw, so a click that arrives late does nothing.
/// The game's own lock is still the game's: the mod moves it with the game's own pin call, and keeps only the extras.
/// </summary>
internal sealed class RewardActions
{
    private readonly RewardPage _page;
    private readonly RewardCatalog _catalog;
    private readonly Func<InterestTerms> _interest;
    private readonly SettingsBoard _settings;
    private readonly Action<string> _log;
    private int _sentVersion = -1;
    private int _sentSettings = -1;
    private bool _isSetupAsked;

    public RewardActions(RewardPage page, RewardCatalog catalog, Func<InterestTerms> interest, SettingsBoard settings, Action<string> log)
    {
        _page = page;
        _catalog = catalog;
        _interest = interest;
        _settings = settings;
        _log = log;
    }

    /// <summary>The page lost what it was told (it reloaded); send it again on the next frame.</summary>
    public void AskSetup() => _isSetupAsked = true;

    public void Tick(float now)
    {
        while (PageMessages.TryTake(out var command)) Do(command);

        if (ListPatch.State is not { } state || (!_isSetupAsked && _sentVersion == ListPatch.Version && _sentSettings == LiveScreen.Version)) return;

        _isSetupAsked = false;
        _sentVersion = ListPatch.Version;
        _sentSettings = LiveScreen.Version;
        SendSetup(state);
    }

    private void Do(PageCommand command)
    {
        try
        {
            switch (command.Verb)
            {
                case PageVerb.Pin: Pin(command.Id, command.Slot); break;
                case PageVerb.Ban: Ban(command.Id, command.Slot); break;
                case PageVerb.Shop: SendShop(); break;
                case PageVerb.ShopBuy: ShopBuy(command.Id); break;
                case PageVerb.CashOut: CashOut(); break;
                case PageVerb.Bans: SendBans(); break;
                case PageVerb.Unban: Unban(command.Id); break;
                case PageVerb.Settings: SendSettings(); break;
                case PageVerb.Set: Set(command.Key ?? "", command.Value ?? ""); break;
                default: throw new ArgumentOutOfRangeException(nameof(command), command.Verb, null);
            }
        }
        catch (Exception ex)
        {
            _log($"the rewards screen's {command.Verb} for reward {command.Id} failed, nothing more was done: " + ex);
        }
    }

    private void SendSetup(State_Web_RogueSelect state)
    {
        var interest = _interest();
        var shown = OfferPatch.FromGame(state.CachedSurvivalPlanIds);

        _page.Send(PageData.SetupCall(new PageSetup(GameLanguage.Current(), LiveScreen.Settings, state.PinnedPlanId, LiveScreen.Book.Locks(), _catalog.Lives(shown), interest.Rate, interest.Cap)));
    }

    private void Pin(int id, int slot)
    {
        if (RunGame.Results() is not { } results || !IsShown(id, slot)) return;

        var book = LiveScreen.Book;
        var extras = book.Locks();
        var maxLocked = LiveScreen.Settings.MaxLocked;

        switch (LockBook.Click(id, results._pinnedPlanId, extras, maxLocked))
        {
            case LockMove.UnpinGame:
            case LockMove.PinGame:
                results.OnPinSurvivalPlan(id, slot);
                break;
            case LockMove.AddExtra:
                book.SetLocks(LockBook.With(extras, new RewardLock(id, slot)));
                Ac_RogueSelect_WebPinChanged.SendAction(results._pinnedPlanId);
                break;
            case LockMove.RemoveExtra:
                book.SetLocks(LockBook.Without(extras, id));
                Ac_RogueSelect_WebPinChanged.SendAction(results._pinnedPlanId);
                break;
            case LockMove.Full:
                Toast(Say($"All {maxLocked} locks are in use", $"{maxLocked} 个锁定已全部使用"));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(id), id, "unknown lock move");
        }
    }

    /// <summary>Keeps the card (and its line, with BanWholeLine) away for the rest of the run, then fills its slot from the day's groups.</summary>
    private void Ban(int id, int slot)
    {
        var settings = LiveScreen.Settings;

        if (!settings.BanButton || !IsShown(id, slot)) return;

        if (RunGame.Results() is not { } results || RunGame.Planning() is not { } planning || ListPatch.State is not { } state) return;

        if (settings.BanCost > 0 && !results.TrySpendSurvivalPoint(settings.BanCost))
        {
            Toast(Say($"Banning costs {settings.BanCost} survival points", $"禁用需要 {settings.BanCost} 生存点"));

            return;
        }

        var line = settings.BanWholeLine ? _catalog.LineOf(id) : Array.Empty<int>();
        var gone = LiveScreen.Book.Ban(id, line, settings.BanCost);

        Release(results, gone);
        Toast(Say("Banned: ", "已禁用：") + Names(gone));
        SwapOut(results, planning, state, RunGame.Day(), gone);
    }

    private void SendBans()
    {
        var settings = LiveScreen.Settings;
        var rows = Bans().Select(ban => new BanRow(ban.Head, ban.Members.Select(_catalog.NameOf).ToList(), BanRefund.Of(ban, settings.BanCost, settings.UnbanRefund))).ToList();

        _page.Send(PageData.BansCall(rows, settings.UnbanRefund, GameLanguage.Current()));
    }

    /// <summary>The ban's rewards can be offered again from the next draw, and part of what it cost comes back.</summary>
    private void Unban(int head)
    {
        if (RunGame.Results() is not { } results) return;

        var settings = LiveScreen.Settings;

        if (LiveScreen.Book.Unban(head, _catalog.Plans().Select(talent => talent.ID)) is { } ban)
        {
            var refund = BanRefund.Of(ban, settings.BanCost, settings.UnbanRefund);

            if (refund > 0) results.AddSurvivalPoint(refund);

            _log($"unbanned {string.Join(", ", ban.Members)}, refunded {refund} survival points");
            Toast(Say("Ban removed: ", "已解除禁用：") + Names(ban.Members) + (refund > 0 ? Say($" (+{refund} survival points)", $"（+{refund} 生存点）") : ""));
        }

        SendBans();
    }

    private IReadOnlyList<BanGroup> Bans() => LiveScreen.Book.Bans(_catalog.Plans().Select(talent => talent.ID));

    private void SendSettings() => _page.Send(PageData.SettingsCall(_settings.Rows(GameLanguage.Current()), GameLanguage.Current()));

    private void Set(string key, string value)
    {
        if (_settings.Set(key, value) is { } problem) _log("the rewards screen's settings could not change a setting: " + problem);
        else _log($"the rewards screen set {key} to {value}");

        SendSettings();
    }

    private string Names(IEnumerable<int> ids) => string.Join(Say(", ", "、"), ids.Select(_catalog.NameOf));

    /// <summary>A banned card loses its lock, so it does not come back tomorrow.</summary>
    private static void Release(SurvivalResultsManager results, IReadOnlyCollection<int> gone)
    {
        var book = LiveScreen.Book;
        var extras = book.Locks();

        if (extras.Any(held => gone.Contains(held.Id))) book.SetLocks(extras.Where(held => !gone.Contains(held.Id)).ToList());

        if (gone.Contains(results._pinnedPlanId)) results.OnPinSurvivalPlan(results._pinnedPlanId, 0);
    }

    /// <summary>Every unbought card on screen that is now passed on gets a new card in its slot, last slot first so the others keep their places.</summary>
    private static void SwapOut(SurvivalResultsManager results, SurvivalPlanningComponent planning, State_Web_RogueSelect state, int day, IReadOnlyCollection<int> gone)
    {
        IReadOnlyList<int> offer = OfferPatch.FromGame(state.CachedSurvivalPlanIds);
        var groups = OfferPatch.DayGroups(day);
        var cooldown = results.GetPlanCooldownIds()?.Cast<Il2CppIntSet>();
        RewardDraw draw = (group, offered) => OfferPatch.First(planning.GetRandomIdsFromGroup(group, 1, day, OfferPatch.ToGame(offered), cooldown));
        var ask = OfferPatch.Ask(planning, day, results._pinnedPlanId, results._pinnedPlanSlot);

        for (var slot = offer.Count - 1; slot >= 0; slot--)
        {
            if (gone.Contains(offer[slot]) && !state.BuyedTalentIds.Contains(offer[slot])) offer = OfferPlan.Replace(offer, slot, ask, groups, draw);
        }

        Ac_RogueSelect_WebRefreshSuccess.SendAction(OfferPatch.ToGame(offer));
    }

    private void SendShop()
    {
        if (!LiveScreen.Settings.ShopEnabled || RunGame.Results() is not { } results || RunGame.Planning() is not { } planning) return;

        var settings = LiveScreen.Settings;
        var book = LiveScreen.Book;
        var day = RunGame.Day();
        var language = GameLanguage.Current();
        var tally = book.Shop(day, settings.ShopReset);
        var rows = _catalog.ShopRows(planning, day, settings, book, language);

        _page.Send(PageData.ShopCall(rows, results.GetSurvivalPoint(), ShopPrice.LeftToday(settings, tally), ShopPrice.Rise(settings, tally.Buys), language));
    }

    /// <summary>
    /// The shop takes its extra (markup, rise, early) first, then buys through the game's own buy, which takes the
    /// card's price and applies the reward. A buy the game turns down gets the extra back.
    /// </summary>
    private void ShopBuy(int id)
    {
        var settings = LiveScreen.Settings;

        if (!settings.ShopEnabled || RunGame.Results() is not { } results || RunGame.Planning() is not { } planning) return;

        var book = LiveScreen.Book;
        var day = RunGame.Day();
        var language = GameLanguage.Current();

        if (_catalog.ShopCheck(id, planning, day, settings, book, language) is not { } check) return;

        if (check.Why != null)
        {
            Toast(check.Why);

            return;
        }

        var tally = book.Shop(day, settings.ShopReset);

        if (ShopPrice.LeftToday(settings, tally) == 0)
        {
            Toast(Blocker.NoBuysLeft(language));

            return;
        }

        var price = ShopPrice.Of(check.CardCost, settings, tally.Buys, check.IsEarly);
        var extra = price - check.CardCost;

        if (results.GetSurvivalPoint() < price || (extra > 0 && !results.TrySpendSurvivalPoint(extra)))
        {
            Toast(Say($"Not enough survival points ({price} needed)", $"生存点不足（需要 {price}）"));

            return;
        }

        var before = results.GetSurvivalPoint();

        results.OnBuySurvivalPlan(id);

        if (results.GetSurvivalPoint() < before || (check.CardCost == 0 && planning.HasActivated(id)))
        {
            book.RecordShopBuy(day, settings.ShopReset);
            _log($"shop: bought reward {id} for {price} survival points");
        }
        else if (extra > 0)
        {
            results.AddSurvivalPoint(extra);
        }

        SendShop();
    }

    private void CashOut()
    {
        var rate = LiveScreen.Settings.CashOut;

        if (rate <= 0 || RunGame.Results() is not { } results || RunGame.Role() is not { } role) return;

        var points = results.GetSurvivalPoint();

        if (points <= 0 || !results.TrySpendSurvivalPoint(points)) return;

        role.AddGold(points * rate);
        _log($"cashed out {points} survival points for {points * rate} cash");
    }

    /// <summary>Whether the screen still shows <paramref name="id"/> in <paramref name="slot"/>, as the page saw it.</summary>
    private static bool IsShown(int id, int slot)
    {
        var shown = ListPatch.State?.CachedSurvivalPlanIds;

        return shown != null && slot >= 0 && slot < shown.Count && shown[slot] == id;
    }

    private void Toast(string text) => _page.Send(PageData.ToastCall(text));

    private static string Say(string english, string chinese) => Blocker.Say(GameLanguage.Current(), english, chinese);
}
