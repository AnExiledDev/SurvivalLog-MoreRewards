using Xunit;

namespace MoreRewards.Tests;

/// <summary>The mod's buttons on the rewards page: what is drawn on which cards, and what each click tells the plugin.</summary>
public class PageButtonsTests
{
    private const string Prefix = "slmod|morerewards|";

    private static PageScript Rewards(string cards, int points = 20, string setup = "")
    {
        var page = new PageScript();

        page.Run($"var frame = addRewardsFrame(); showRewards(frame, {cards}, {points}); setupWith({{{setup}}});");
        page.Tick();

        return page;
    }

    [Fact]
    public void The_page_asks_for_its_setup_while_the_rewards_screen_is_up_and_stops_once_told()
    {
        var page = new PageScript();

        page.Run("var frame = addRewardsFrame(); showCards(frame, 3);");
        page.Tick();

        Assert.Equal("needs setup", page.Take());

        page.Run("setupWith();");
        page.Tick();

        Assert.Equal("ok", page.Take());
    }

    [Fact]
    public void Nothing_asks_for_a_setup_when_the_rewards_screen_is_not_up()
    {
        var page = new PageScript();

        page.Tick();

        Assert.Equal("ok", page.Take());
    }

    [Fact]
    public void Only_cards_still_on_offer_get_not_interested_and_ban()
    {
        var page = Rewards("[{id: 1, state: 'affordable', pinned: false}, {id: 2, state: 'bought', pinned: false}, {id: 3, state: 'affordable', pinned: true}, {id: 4, state: 'locked', pinned: false}]");

        Assert.Equal(2, page.Number("slotOf(frame, 0).querySelectorAll('button').filter(function (b) { return b.parentElement.classList.contains('mr-acts'); }).length"));
        Assert.False(HasActs(page, 1));
        Assert.False(HasActs(page, 2));
        Assert.False(HasActs(page, 3));
    }

    [Fact]
    public void Each_card_says_whether_it_lasts_the_run_or_acts_once()
    {
        var page = Rewards("[{id: 1, state: 'affordable', pinned: false}, {id: 2, state: 'affordable', pinned: false}]", setup: "life: {1: 1}");

        Assert.Equal("ONE-TIME", page.Text("slotOf(frame, 0).querySelector('.mr-life').textContent"));
        Assert.Equal("PERMANENT", page.Text("slotOf(frame, 1).querySelector('.mr-life').textContent"));
    }

    [Fact]
    public void Labels_switched_off_draw_no_label()
    {
        var page = Rewards("[{id: 1, state: 'affordable', pinned: false}]", setup: "labels: false");

        Assert.False(page.Is("slotOf(frame, 0).querySelector('.mr-life')"));
    }

    [Fact]
    public void Not_interested_and_ban_tell_the_plugin_the_card_and_its_slot()
    {
        var page = Rewards("[{id: 11, state: 'affordable', pinned: false}, {id: 12, state: 'too-expensive', pinned: false}]");

        page.Run("fire(slotOf(frame, 1).querySelector('.mr-acts').children[0], 'click'); fire(slotOf(frame, 1).querySelector('.ban'), 'click');");

        Assert.Equal(Prefix + "skip|12|1," + Prefix + "ban|12|1", page.Text("posted.join(',')"));
    }

    [Fact]
    public void The_ban_button_shows_its_cost()
    {
        var page = Rewards("[{id: 11, state: 'affordable', pinned: false}]", setup: "banCost: 3");

        Assert.Equal("Ban3", page.Text("slotOf(frame, 0).querySelector('.ban').textContent"));
    }

    [Fact]
    public void A_card_swapped_into_a_slot_gets_buttons_for_the_new_card()
    {
        var page = Rewards("[{id: 11, state: 'affordable', pinned: false}]");

        page.Run("stateOf(frame).cards[0] = {id: 99, state: 'affordable', pinned: false};");
        page.Tick();
        page.Run("fire(slotOf(frame, 0).querySelector('.ban'), 'click');");

        Assert.Equal(Prefix + "ban|99|0", page.Text("posted.join(',')"));
        Assert.Equal(1, page.Number("slotOf(frame, 0).querySelectorAll('.mr-acts').length"));
    }

    [Fact]
    public void With_more_locks_allowed_the_lock_button_asks_the_plugin_instead_of_the_game()
    {
        var page = Rewards("[{id: 11, state: 'affordable', pinned: true}, {id: 12, state: 'affordable', pinned: false}]", setup: "maxLocked: 3");

        var reachedGame = page.Is("fire(slotOf(frame, 1).querySelector('.pin-btn'), 'click')");

        Assert.False(reachedGame);
        Assert.Equal(Prefix + "pin|12|1", page.Text("posted.join(',')"));
        Assert.True(page.Is("rootOf(frame).classList.contains('slmr-locks')"));
        Assert.Equal("\U0001F512 1/3", page.Text("frame.contentDocument.querySelector('.pin-count').textContent"));
        Assert.EndsWith("(1/3)", page.Text("slotOf(frame, 1).querySelector('.pin-tip').textContent"));
    }

    [Fact]
    public void When_every_lock_is_used_the_other_lock_buttons_show_as_full()
    {
        var page = Rewards("[{id: 11, state: 'affordable', pinned: true}, {id: 12, state: 'affordable', pinned: true}, {id: 13, state: 'affordable', pinned: false}]", setup: "maxLocked: 2");

        Assert.True(page.Is("rootOf(frame).classList.contains('slmr-full')"));
        Assert.False(page.Is("rootOf(frame).classList.contains('slmr-locks')"));
        Assert.StartsWith("All locks are in use", page.Text("slotOf(frame, 2).querySelector('.pin-tip').textContent"));
    }

    [Fact]
    public void With_one_lock_the_game_keeps_its_own_lock_button()
    {
        var page = Rewards("[{id: 11, state: 'affordable', pinned: false}]");

        Assert.True(page.Is("fire(slotOf(frame, 0).querySelector('.pin-btn'), 'click')"));
        Assert.Equal("", page.Text("posted.join(',')"));
        Assert.False(page.Is("frame.contentDocument.querySelector('.pin-count')"));
        Assert.False(page.Is("rootOf(frame).classList.contains('slmr-locks')"));
    }

    [Theory]
    [InlineData(0, 0.05, 10, 0)]
    [InlineData(10, 0.05, 10, 1)]
    [InlineData(100, 0.05, 10, 5)]
    [InlineData(1000, 0.05, 10, 10)]
    [InlineData(100, 0.29, 50, 29)]
    public void Interest_is_the_rate_rounded_down_at_least_one_and_at_most_the_cap(int points, double rate, int cap, int expected)
    {
        var page = new PageScript();

        Assert.Equal(expected, page.Number($"window.__slmorerewards.interest({points}, {rate.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {cap})"));
    }

    [Fact]
    public void The_footer_shows_tomorrows_interest_and_what_leaving_cashes_out()
    {
        var page = Rewards("[{id: 11, state: 'affordable', pinned: false}]", points: 100, setup: "cashOut: 15");

        Assert.Equal("Interest tomorrow: +5Leaving cashes out: $1500", page.Text("frame.contentDocument.querySelector('.mr-foot').textContent"));
    }

    [Fact]
    public void Leaving_cashes_out_first_and_still_leaves()
    {
        var page = Rewards("[{id: 11, state: 'affordable', pinned: false}]", setup: "cashOut: 15");

        Assert.True(page.Is("fire(frame.contentDocument.querySelector('.primary-btn'), 'click')"));
        Assert.Equal(Prefix + "cashout", page.Text("posted.join(',')"));
    }

    [Fact]
    public void Leaving_does_not_cash_out_when_cashing_out_is_off()
    {
        var page = Rewards("[{id: 11, state: 'affordable', pinned: false}]");

        page.Run("fire(frame.contentDocument.querySelector('.primary-btn'), 'click');");

        Assert.Equal("", page.Text("posted.join(',')"));
    }

    [Fact]
    public void The_shop_opens_only_when_the_player_asked_for_it()
    {
        var page = Rewards("[{id: 11, state: 'affordable', pinned: false}]");

        page.Run("window.__slmorerewards.shop({lang: 'en', points: 20, rise: 25, left: null, rows: [shopRow(5, 'Axe', 6)]});");

        Assert.False(page.Is("frame.contentDocument.body.querySelector('.shop')"));

        page.Run("fire(frame.contentDocument.querySelector('.mr-shop-btn'), 'click');");
        page.Run("window.__slmorerewards.shop({lang: 'en', points: 20, rise: 25, left: null, rows: [shopRow(5, 'Axe', 6)]});");

        Assert.Equal(Prefix + "shop", page.Text("posted.join(',')"));
        Assert.False(page.Is("frame.contentDocument.body.querySelector('.shop').hidden"));
        Assert.Contains("+25%", page.Text("frame.contentDocument.body.querySelector('.rate').textContent"));
    }

    [Fact]
    public void No_shop_button_when_the_shop_is_off()
    {
        var page = Rewards("[{id: 11, state: 'affordable', pinned: false}]", setup: "shop: false");

        Assert.False(page.Is("frame.contentDocument.querySelector('.mr-shop-btn')"));
    }

    [Fact]
    public void The_shop_lists_what_can_be_bought_first_cheapest_first_and_greys_the_rest()
    {
        var page = new PageScript();
        const string data = "{points: 20, left: null, rows: [shopRow(1, 'Axe', 5), shopRow(2, 'Bow', 3), shopRow(3, 'Cart', 1, 'Banned this run'), shopRow(4, 'Dam', 50)]}";

        Assert.Equal("2,1,4,3", page.Text($"window.__slmorerewards.shopList({data}, {{query: '', cat: 'all'}}, 'en').rows.map(function (e) {{ return e.row.id; }}).join(',')"));
        Assert.Equal("2,1", page.Text($"window.__slmorerewards.shopList({data}, {{query: '', cat: 'all', onlyBuyable: true}}, 'en').rows.map(function (e) {{ return e.row.id; }}).join(',')"));
        Assert.True(page.Is($"window.__slmorerewards.shopList({data}, {{query: '', cat: 'all'}}, 'en').rows[2].isPoor"));
    }

    [Fact]
    public void Searching_the_shop_matches_names_and_descriptions_in_any_case()
    {
        var page = new PageScript();
        const string data = "{points: 20, left: null, rows: [shopRow(1, 'Axe', 5), shopRow(2, 'Bow', 3)]}";

        Assert.Equal("1", page.Text($"window.__slmorerewards.shopList({data}, {{query: '  AXE does '}}, 'en').rows.map(function (e) {{ return e.row.id; }}).join(',')"));
    }

    [Fact]
    public void Shop_categories_count_what_matches_and_an_empty_one_falls_back_to_all()
    {
        var page = new PageScript();
        const string data = "{points: 20, left: null, rows: [shopRow(1, 'Axe', 5, null, 'traps'), shopRow(2, 'Bow', 3, null, 'food'), shopRow(3, 'Cart', 3, null, 'food')]}";

        Assert.Equal("all3,food2,traps1", page.Text($"window.__slmorerewards.shopList({data}, {{query: ''}}, 'en').cats.map(function (c) {{ return c.key + c.count; }}).join(',')"));
        Assert.Equal("all", page.Text($"window.__slmorerewards.shopList({data}, {{query: 'axe', cat: 'food'}}, 'en').cat"));
    }

    [Fact]
    public void With_no_buys_left_today_nothing_in_the_shop_can_be_bought()
    {
        var page = new PageScript();
        const string data = "{points: 20, left: 0, rows: [shopRow(1, 'Axe', 5)]}";

        Assert.Equal("No shop buys left today", page.Text($"window.__slmorerewards.shopList({data}, {{query: ''}}, 'en').rows[0].why"));
    }

    [Fact]
    public void Buying_in_the_shop_asks_the_plugin_and_greyed_rewards_have_no_buy_button()
    {
        var page = OpenShop("[shopRow(5, 'Axe', 6), shopRow(6, 'Bow', 6, 'Banned this run')]");

        page.Run("posted.length = 0; var rows = frame.contentDocument.querySelectorAll('.sr'); fire(rows[0].querySelector('button'), 'click');");

        Assert.Equal(Prefix + "shopbuy|5", page.Text("posted.join(',')"));
        Assert.False(page.Is("rows[1].querySelector('button')"));
    }

    [Fact]
    public void Typing_in_the_shop_search_stays_in_the_box_and_escape_closes_the_shop()
    {
        var page = OpenShop("[shopRow(5, 'Axe', 6)]");

        page.Run("var heard = false; frame.contentDocument.body.addEventListener('keydown', function () { heard = true; }); var input = frame.contentDocument.querySelector('input');");
        page.Run("fire(input, 'keydown', 'a'); fire(input, 'keydown', 'Escape');");

        Assert.False(page.Is("heard"));
        Assert.True(page.Is("frame.contentDocument.body.querySelector('.shop').hidden"));
    }

    [Fact]
    public void A_notice_shows_on_the_rewards_page_and_goes_away()
    {
        var page = Rewards("[{id: 11, state: 'affordable', pinned: false}]");

        page.Run("window.__slmorerewards.toast('All 3 locks are in use');");

        Assert.Equal("All 3 locks are in use", page.Text("frame.contentDocument.body.querySelector('.toast2').textContent"));

        page.Run("runTimeouts();");

        Assert.False(page.Is("frame.contentDocument.body.querySelector('.toast2')"));
    }

    private static PageScript OpenShop(string rows)
    {
        var page = Rewards("[{id: 11, state: 'affordable', pinned: false}]");

        page.Run("fire(frame.contentDocument.querySelector('.mr-shop-btn'), 'click');");
        page.Run($"window.__slmorerewards.shop({{lang: 'en', points: 20, rise: 25, left: null, rows: {rows}}});");

        return page;
    }

    private static bool HasActs(PageScript page, int slot) => page.Is($"slotOf(frame, {slot}).querySelector('.mr-acts')");
}
