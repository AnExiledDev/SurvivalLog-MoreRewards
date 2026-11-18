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

    private bool Bool(string expression) => _engine.Evaluate($"!!({expression})").AsBoolean();

    private string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

    private static string Resource(string name)
    {
        using var stream = typeof(RewardsTabTests).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
