using Jint;
using Xunit;

namespace MoreRewards.Tests;

/// <summary>
/// rewards-tab.js as the plugin ships it, on the reward editor from Exiled's Toolkit's source (editor.js), run in
/// Jint. Only what the tab sets up when it loads: drawing it needs a page.
/// </summary>
public class RewardsTabTests
{
    private readonly Engine _engine = new();

    public RewardsTabTests()
    {
        _engine.Execute("var window = this;");
        _engine.Execute(Resource("ExiledToolkit.editor.js"));
        // Keep the tab's side (T) and its editor (ed) so the tests can reach them, and what T said when make ran.
        _engine.Execute("var make = window.ExiledEditor.make; window.ExiledEditor.make = function (t) { window.T = t; window.addsUpAtMake = t.levelsAddUp; window.ed = make(t); return window.ed; };");
        _engine.Execute(Resource("MoreRewards.rewards-tab.js"));
    }

    // Each wind reward level is a talent of its own, and the game adds them up. A new Better wind turbines effect that
    // started at 15%, 30% and 45% gave 90% at level III, twice what the wind rewards give.
    [Fact]
    public void A_new_wind_turbine_effect_adds_15_percent_per_level()
    {
        _engine.Execute("""
            var g = T.makeGive(ed.FX_BY_ID['mr:wind-power'], '', 0, null, 3);
            var wind = { id: 39100, levels: [{ cost: 10 }, { cost: 16 }, { cost: 24 }], gives: [g] };
            """);

        Assert.True(Bool("ed.addsUp(wind)"));
        Assert.Equal("+15%", Text("ed.totalText(wind, 0, false)"));
        Assert.Equal("+30%", Text("ed.totalText(wind, 1, false)"));
        Assert.Equal("+45%", Text("ed.totalText(wind, 2, false)"));
    }

    // make words Higher loan limit as a raise per level only when it already knows the levels add up. Told later, a
    // level of 300 read "Loan limit increased to 300", lower than the 500 every run starts with.
    [Fact]
    public void The_tab_tells_the_editor_its_levels_add_up_before_making_it()
    {
        Assert.True(Bool("addsUpAtMake === true"));
        Assert.Equal("Raises the loan limit by 300 (from 500).", Text("ed.FX_BY_ID['game:loan'].desc({ IntValue0: 300 }, false)"));
    }

    // An effect's column holds what that one level gives, and IN TOTAL what the player has with the levels below it.
    [Fact]
    public void The_levels_table_says_each_number_is_for_that_level_alone()
    {
        _engine.Execute("""
            var g = T.makeGive(ed.FX_BY_ID['mr:wind-power'], '', 0, null, 3);
            var wind = { id: 39100, levels: [{ cost: 10 }, { cost: 16 }, { cost: 24 }], gives: [g] };
            var html = T.levelTableHtml(wind);
            """);

        Assert.Contains("MORE POWER<small>this level</small>", Text("html"));
        Assert.Contains(">IN TOTAL<", Text("html"));
        Assert.Contains("+45%", Text("html"));
        Assert.Contains("Each level adds to the ones below it", Text("html"));
        Assert.DoesNotContain("Discounts multiply", Text("html"));
    }

    // Discounts multiply in the game, and the table under a discount reward says so.
    [Fact]
    public void The_levels_table_under_a_discount_says_discounts_multiply()
    {
        _engine.Execute("""
            var discount = { id: 39200, levels: [{ cost: 10 }, { cost: 16 }], gives: [{ fx: ed.FX_BY_ID['game:short-life-discount'], per: { FloatValue1: [10, 10], FloatValue0: [5, 5] }, pick: {} }] };
            """);

        Assert.Contains("Discounts multiply", Text("T.levelTableHtml(discount)"));
        Assert.Contains("-19%", Text("T.levelTableHtml(discount)"));
    }

    // A reward whose cards the editor wrote before they named their totals is still the editor's text, so the next
    // change to it writes the cards again with the totals.
    [Fact]
    public void Card_text_the_editor_wrote_before_the_totals_still_counts_as_the_editors()
    {
        _engine.Execute("""
            T.st.data = { windPower: true };
            T.st.view = 'days';
            var raw = { id: 39100, fields: { WebIcon: '../../wind.png' }, gives: [{ effect: 'mr:wind-power', fields: { FloatValue0: 0.15 } }], levels: [{ cost: 10 }, { cost: 16 }] };
            T.st.file = { rewards: [raw] };
            T.buildEntries();
            var e = T.st.entries[0];
            raw.levels.forEach(function (l, i) { l.description = { en: ed.plainDesc(e, i, false), zh: ed.plainDesc(e, i, true) }; });
            T.buildEntries();
            """);

        Assert.NotEqual(Text("ed.plainDesc(e, 1, false)"), Text("ed.autoDesc(e, 1, false)"));
        Assert.True(Bool("T.st.entries[0].autoText"));

        _engine.Execute("raw.levels[1].description = { en: 'My own text.', zh: '我自己的' }; T.buildEntries();");

        Assert.False(Bool("T.st.entries[0].autoText"));
    }

    // End-of-day rewards come after the disaster, when there is no money, so NEW REWARD leaves out the effects that only
    // work before it: money, shopping, the loan, the countdown. Every other effect is still there, named or raw, and so
    // are the effects More Rewards runs itself.
    [Fact]
    public void New_reward_leaves_out_effects_that_only_work_before_the_disaster()
    {
        Load("""{ "rewards": [] }""", templates: "{ Savings_Plan: {}, AE_HomePackageMax: {}, PreDisaster_AddGold: {}, PreDisaster_AddWeight: {}, LoanMaxBonus: {}, AE_FurnitureDiscount: {}, VehicleTimeReduce: {}, AE_NoNameYet: {} }");

        Assert.Equal("game:AE_HomePackageMax,game:savings,mr:ban-tokens,mr:free-refresh,mr:investor,mr:lottery,mr:more-cards,mr:more-locks,mr:supply-crate,raw:AE_NoNameYet", Text("ed.cat.list.map(function (f) { return f.id; }).sort().join(',')"));
    }

    // Many of the game's effects only make sense with the card text of a reward that uses them. The effect's page shows
    // that text, and the last level's text when it reads differently, since end-of-day rewards have no line with the
    // numbers for each level. An effect with no name says its boxes hold the game's numbers.
    [Fact]
    public void An_effect_page_shows_the_card_text_of_the_rewards_that_use_it()
    {
        Load("""{ "rewards": [ { "id": 31271 } ] }""", templates: "{ AE_HomePackageMax: {}, AE_NoNameYet: {} }", game: """
            [
              { id: 31271, levels: [
                { id: 31271, cost: 10, name: { en: 'Big Entryway', zh: '大玄关' }, desc: { en: 'The Entryway holds more packages.', zh: '玄关能放更多包裹。' }, script: 'AE_HomePackageMax', values: { FloatValue0: 1 } },
                { id: 31272, cost: 14, name: { en: 'Big Entryway II', zh: '大玄关 Lv2' }, desc: { en: 'The Entryway holds even more packages.', zh: '玄关能放更多的包裹。' }, script: 'AE_HomePackageMax', values: { FloatValue0: 2 } } ] },
              { id: 31301, levels: [
                { id: 31301, cost: 10, name: { en: 'Odd Luck', zh: '怪运' }, desc: { en: 'Something rare happens.', zh: '会发生罕见的事。' }, script: 'AE_NoNameYet', values: { IntValue0: 3 } } ] }
            ]
            """);
        _engine.Execute("T.st.picker = { selId: 'game:AE_HomePackageMax' };");

        var named = Text("T.detailHtml(T.st.entries[0], ed.cat.list)");

        Assert.Contains("WHAT THE GAME SAYS", named);
        Assert.Contains("Big Entryway", named);
        Assert.Contains("The Entryway holds more packages.", named);
        Assert.Contains("Level 2: The Entryway holds even more packages.", named);
        Assert.DoesNotContain("has no name yet", named);

        _engine.Execute("T.st.picker = { selId: 'raw:AE_NoNameYet' };");

        var raw = Text("T.detailHtml(T.st.entries[0], ed.cat.list)");

        Assert.Contains("Something rare happens.", raw);
        Assert.Contains(Text("ed.RAW_NOTE[0]"), raw);
        Assert.DoesNotContain("no friendly names", raw);
    }

    // The three day rules go into the file only when they are set, and read back as they were written.
    [Fact]
    public void The_day_rules_are_saved_only_when_set_and_read_back()
    {
        Load("""{ "rewards": [ { "id": 31271, "toDay": 20, "anyDay": 10, "alwaysOffered": true }, { "id": 31301, "pools": { "1005": 2 } } ] }""");

        Assert.Equal("20 10 true", Text("[T.st.entries[0].rules.toDay, T.st.entries[0].rules.anyDay, T.st.entries[0].rules.always].join(' ')"));
        Assert.Equal("20 10 true", Text("(function (o) { return [o.toDay, o.anyDay, o.alwaysOffered].join(' '); })(T.outEntry(T.st.entries[0]))"));
        Assert.False(Bool("['toDay', 'anyDay', 'alwaysOffered'].some(function (k) { return k in T.outEntry(T.st.entries[1]); })"));
    }

    // Day 1 draws from 9102 (30 rewards) and 1005 (4), day 7 from 9103 (72) and 9121 (14). The plugin picks the same
    // pools (MainPools.cs), so the tab and the game agree on where "any day" puts a reward.
    [Fact]
    public void Each_days_biggest_pool_follows_the_days_changed_in_the_tab()
    {
        Load("""{ "rewards": [] }""");

        Assert.Equal("9102,9103", Text("T.mainPools()"));

        _engine.Execute("T.st.dayEdits[7] = [{ pool: 9121, amount: 2 }, { pool: 9103, amount: 0 }];");

        Assert.Equal("9102,9121", Text("T.mainPools()"));
    }

    [Fact]
    public void Any_day_puts_a_reward_in_each_days_biggest_pool_and_a_pool_it_names_keeps_its_weight()
    {
        Load("""{ "rewards": [ { "id": 31301, "anyDay": 10, "pools": { "9103": 3 } } ] }""");

        Assert.Equal(10, Weight(9102, 31301));
        Assert.Equal(3, Weight(9103, 31301));
        Assert.Equal(0, Weight(9121, 31301));
        Assert.Contains("pools 9102 at weight 10", Text("T.poolsHtml(T.st.entries[0])"));
    }

    // With 5 cards a day the test draw shows 5: the always offered reward first, then the game's picks and More
    // Rewards' extra cards. A reward past its last day never shows.
    [Fact]
    public void The_test_draw_shows_every_card_of_the_day_with_always_offered_first()
    {
        Load("""{ "rewards": [ { "id": 31271, "alwaysOffered": true }, { "id": 31451, "toDay": 3 } ] }""");

        for (var run = 0; run < 20; run++)
        {
            _engine.Execute("var drawn = T.testDraw(7); var shown = drawn.filter(function (c) { return c.isShown; });");

            Assert.Equal(5, (int)_engine.Evaluate("shown.length").AsNumber());
            Assert.Equal(31271, (int)_engine.Evaluate("shown[0].id").AsNumber());
            Assert.Equal("Always offered", Text("shown[0].pool ? 'pool' : 'Always offered'"));
            Assert.False(Bool("drawn.some(function (c) { return c.id === 31451; })"));
            Assert.True(Bool("new Set(drawn.map(function (c) { return c.id; })).size === drawn.length"));
        }
    }

    [Fact]
    public void An_always_offered_reward_outside_its_days_gets_no_card()
    {
        Load("""{ "rewards": [ { "id": 31271, "alwaysOffered": true, "fromDay": 7 } ] }""", 3);

        _engine.Execute("var shown = T.testDraw(1).filter(function (c) { return c.isShown; });");

        Assert.Equal(3, (int)_engine.Evaluate("shown.length").AsNumber());
        Assert.False(Bool("shown.some(function (c) { return !c.pool; })"));
    }

    // The day view used to say only the first 3 cards show, which is wrong once OptionCount is above 3.
    [Fact]
    public void The_day_view_says_how_many_cards_a_day_shows_and_marks_the_biggest_pool()
    {
        Load("""{ "rewards": [] }""");

        Assert.Contains("You get 5 cards a day (OptionCount), so More Rewards draws the other 2", Text("T.dayHtml(1)"));
        Assert.Contains("BIGGEST", Text("T.dayHtml(1)"));
        Assert.Contains("4 rewards, Day 1</small>", Text("T.dayHtml(1)"));
        Assert.Contains("2 rewards, Days 7+</small>", Text("T.dayHtml(7)"));

        Load("""{ "rewards": [] }""", 3);

        Assert.DoesNotContain("cards a day", Text("T.dayHtml(1)"));
    }

    // Day 1 draws two from 9102 and one from 1005; day 7 one each from 9103 and 9121. Twelve game rewards fill the pools.
    // Loading closes any open menu, which lets go of its page listeners, so the page gets a stand-in for that.
    private void Load(string file, int cards = 5, string templates = "{}", string game = "game")
    {
        _engine.Execute($$"""
            window.document = window.document || { addEventListener: function () {}, removeEventListener: function () {} };
            (function () {
              var game = [];
              var pools = [[9102, 31271], [9102, 31301], [9102, 31331], [9102, 31361], [1005, 31391], [1005, 31421],
                [9103, 31451], [9103, 31481], [9103, 31571], [9103, 31601], [9121, 31511], [9121, 31541]];
              pools.forEach(function (p) {
                var into = {};
                into[p[0]] = 5;
                game.push({ id: p[1], levels: [{ id: p[1], cost: 10, name: { en: 'Reward ' + p[1], zh: 'Reward ' + p[1] }, pools: into }] });
              });
              window.__slmrRewards.data({
                game: {{game}}, pools: [1005, 9102, 9103, 9121], cards: {{cards}},
                days: [{ day: 1, pools: [[9102, 2], [1005, 1]] }, { day: 7, pools: [[9103, 1], [9121, 1]] }],
                poolSizes: { 1005: 4, 9102: 30, 9103: 72, 9121: 14 },
                templates: {{templates}},
                file: {{file}}
              });
            })();
            """);
    }

    private int Weight(int pool, int id) => (int)_engine.Evaluate($"(function () {{ var m = (T.poolIndex()[{pool}] || []).find(function (x) {{ return x.id === {id}; }}); return m ? m.weight : 0; }})()").AsNumber();

    private bool Bool(string expression) => _engine.Evaluate($"!!({expression})").AsBoolean();

    private string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

    private static string Resource(string name)
    {
        using var stream = typeof(RewardsTabTests).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
