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
        // Keep the tab's side (T) and its editor (ed) so the tests can reach them.
        _engine.Execute("var make = window.ExiledEditor.make; window.ExiledEditor.make = function (t) { window.T = t; window.ed = make(t); return window.ed; };");
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

    [Fact]
    public void The_tab_tells_the_editor_its_levels_add_up()
    {
        Assert.True(Bool("T.levelsAddUp === true"));
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
