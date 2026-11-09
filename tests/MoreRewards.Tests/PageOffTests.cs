using Xunit;

namespace MoreRewards.Tests;

/// <summary>
/// The mod turned off while the game runs: the rewards page loses everything the mod drew on it, the game's lock
/// and tips are back, and turning it on again draws it all afresh.
/// </summary>
public class PageOffTests
{
    private const string FiveCards = "[{id: 11, state: 'affordable', pinned: true}, {id: 12, state: 'affordable', pinned: false}, " +
        "{id: 13, state: 'affordable', pinned: false}, {id: 14, state: 'affordable', pinned: false}, {id: 15, state: 'affordable', pinned: false}]";

    private const string SettingsRow = "{key: 'General.Enabled', section: 'General', name: 'Enabled', kind: 'bool', value: 'true', min: 0, max: 0, options: [], desc: 'On', restart: false, shown: true}";

    private static readonly string[] ModParts = { ".mr-acts", ".mr-life", ".mr-foot", ".mr-foot-btns", ".pin-count" };

    /// <summary>Five cards with the game's lock tip on each, and the mod drawing three locks, bans, labels and cash out.</summary>
    private static PageScript Drawn()
    {
        var page = new PageScript();

        page.Run($"var frame = addRewardsFrame(); showRewards(frame, {FiveCards}, 40);");
        page.Run("frame.contentDocument.querySelectorAll('.pin-tip').forEach(function (tip) { tip.textContent = 'Lock'; });");
        page.Run("setupWith({maxLocked: 3, cashOut: 2});");
        page.Tick();

        return page;
    }

    private static int Count(PageScript page, string selector) => (int)page.Number($"frame.contentDocument.querySelectorAll('{selector}').length");

    [Fact]
    public void Turned_off_the_page_takes_away_every_button_label_and_lock_count_the_mod_drew()
    {
        var page = Drawn();

        foreach (var part in ModParts) Assert.True(Count(page, part) > 0, part + " was not drawn to begin with");

        Assert.True(page.Is("rootOf(frame).classList.contains('slmr-locks')"));

        page.Run("window.__slmorerewards.off();");
        page.Tick();

        foreach (var part in ModParts) Assert.Equal(0, Count(page, part));

        Assert.False(page.Is("rootOf(frame).classList.contains('slmr-locks')"));
        Assert.False(page.Is("rootOf(frame).classList.contains('slmr-full')"));
    }

    [Fact]
    public void Turned_off_the_lock_tips_say_what_the_game_had_them_say()
    {
        var page = Drawn();

        Assert.EndsWith("(1/3)", page.Text("slotOf(frame, 1).querySelector('.pin-tip').textContent"));

        page.Run("window.__slmorerewards.off();");

        for (var slot = 1; slot < 5; slot++) Assert.Equal("Lock", page.Text($"slotOf(frame, {slot}).querySelector('.pin-tip').textContent"));
    }

    [Fact]
    public void Turned_off_a_lock_click_goes_to_the_game_and_the_plugin_hears_nothing()
    {
        var page = Drawn();

        page.Run("window.__slmorerewards.off(); posted = [];");
        page.Tick();

        Assert.True(page.Is("fire(slotOf(frame, 2).querySelector('.pin-btn'), 'click')"));
        Assert.Equal("", page.Text("posted.join(',')"));
    }

    [Fact]
    public void Extra_cards_already_on_screen_keep_their_rows_while_the_mod_is_off()
    {
        var page = Drawn();

        page.Run("window.__slmorerewards.off();");
        page.Tick();

        Assert.True(page.Is("rootOf(frame).classList.contains('slmr-many')"));
    }

    [Fact]
    public void Turned_off_an_open_shop_or_settings_closes_and_a_late_reply_does_not_open_it_again()
    {
        var page = Drawn();

        page.Run("fire(frame.contentDocument.querySelector('.mr-shop-btn'), 'click');");
        page.Run("window.__slmorerewards.shop({lang: 'en', points: 20, rise: 25, left: null, rows: [shopRow(5, 'Axe', 6)]});");
        page.Run("fire(frame.contentDocument.querySelector('.mr-set-btn'), 'click');");
        page.Run("window.__slmorerewards.settings({lang: 'en', rows: [" + SettingsRow + "]});");

        Assert.Equal(1, Count(page, ".shop"));
        Assert.Equal(1, Count(page, ".mr-pane"));

        page.Run("window.__slmorerewards.off();");
        page.Run("window.__slmorerewards.shop({lang: 'en', points: 20, rise: 25, left: null, rows: [shopRow(5, 'Axe', 6)]});");
        page.Run("window.__slmorerewards.settings({lang: 'en', rows: [" + SettingsRow + "]});");

        Assert.Equal(0, Count(page, ".shop"));
        Assert.Equal(0, Count(page, ".mr-pane"));
    }

    [Fact]
    public void Turned_on_again_the_page_draws_everything_afresh()
    {
        var page = Drawn();
        var acts = Count(page, ".mr-acts");

        page.Run("window.__slmorerewards.off();");
        page.Tick();
        page.Run("setupWith({maxLocked: 3, cashOut: 2});");
        page.Tick();

        Assert.Equal(acts, Count(page, ".mr-acts"));
        Assert.Equal(5, Count(page, ".mr-life"));
        Assert.Equal(1, Count(page, ".mr-foot-btns"));
        Assert.Equal("\U0001F512 1/3", page.Text("frame.contentDocument.querySelector('.pin-count').textContent"));
        Assert.EndsWith("(1/3)", page.Text("slotOf(frame, 1).querySelector('.pin-tip').textContent"));
    }
}
