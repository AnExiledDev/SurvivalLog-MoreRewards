using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

/// <summary>
/// The setup check reads the game only when the page is owed a setup, and otherwise looks at the run once a second.
/// These guard that an unchanged screen reads nothing most frames and that a real change still goes out.
/// </summary>
public class SetupWatchTests
{
    [Fact]
    public void With_nothing_owed_the_run_is_looked_at_once_a_second_and_nothing_else_is_read()
    {
        var watch = SentFor(list: 1, settings: 1);

        Assert.Equal(SetupStep.CheckRun, watch.Step(0f, true, 1, 1));

        for (var now = 0.016f; now < 0.99f; now += 0.016f) Assert.Equal(SetupStep.Skip, watch.Step(now, true, 1, 1));

        Assert.Equal(SetupStep.CheckRun, watch.Step(1f, true, 1, 1));
        Assert.Equal(SetupStep.Skip, watch.Step(1.5f, true, 1, 1));
    }

    [Fact]
    public void Before_the_screen_has_opened_this_run_nothing_is_read()
    {
        var watch = new SetupWatch();

        watch.Ask();

        Assert.Equal(SetupStep.Skip, watch.Step(0f, false, 3, 2));
        Assert.Equal(SetupStep.Skip, watch.Step(5f, false, 4, 2));
    }

    [Fact]
    public void A_rebuilt_card_list_is_tried_every_frame_for_a_second_then_twice_a_second_until_it_goes_out()
    {
        var watch = SentFor(list: 1, settings: 1);

        Assert.Equal(SetupStep.TrySend, watch.Step(10f, true, 2, 1));
        Assert.Equal(SetupStep.TrySend, watch.Step(10.5f, true, 2, 1));
        Assert.Equal(SetupStep.TrySend, watch.Step(10.98f, true, 2, 1));
        Assert.Equal(SetupStep.TrySend, watch.Step(11f, true, 2, 1));
        Assert.Equal(SetupStep.Skip, watch.Step(11.2f, true, 2, 1));
        Assert.Equal(SetupStep.TrySend, watch.Step(11.5f, true, 2, 1));
        Assert.Equal(SetupStep.TrySend, watch.Step(60f, true, 2, 1));
    }

    [Fact]
    public void A_changed_setting_is_owed_too()
    {
        var watch = SentFor(list: 1, settings: 1);

        Assert.Equal(SetupStep.TrySend, watch.Step(0f, true, 1, 2));
    }

    [Fact]
    public void A_page_that_asks_again_is_owed_even_with_nothing_new()
    {
        var watch = SentFor(list: 1, settings: 1);

        watch.Step(0f, true, 1, 1);
        watch.Ask();

        Assert.Equal(SetupStep.TrySend, watch.Step(0.1f, true, 1, 1));
    }

    [Fact]
    public void Once_the_setup_goes_out_the_trying_stops()
    {
        var watch = SentFor(list: 1, settings: 1);

        Assert.Equal(SetupStep.TrySend, watch.Step(0f, true, 2, 1));

        watch.Sent(2, 1);

        Assert.Equal(SetupStep.CheckRun, watch.Step(0.016f, true, 2, 1));

        for (var now = 0.032f; now < 0.99f; now += 0.016f) Assert.Equal(SetupStep.Skip, watch.Step(now, true, 2, 1));
    }

    [Fact]
    public void Another_rebuild_after_the_first_second_gets_its_own_second_of_every_frame_tries()
    {
        var watch = SentFor(list: 1, settings: 1);

        watch.Step(0f, true, 2, 1);
        watch.Step(1.2f, true, 2, 1);

        Assert.Equal(SetupStep.TrySend, watch.Step(1.3f, true, 3, 1));
        Assert.Equal(SetupStep.TrySend, watch.Step(1.32f, true, 3, 1));
    }

    private static SetupWatch SentFor(int list, int settings)
    {
        var watch = new SetupWatch();

        watch.Sent(list, settings);

        return watch;
    }
}
