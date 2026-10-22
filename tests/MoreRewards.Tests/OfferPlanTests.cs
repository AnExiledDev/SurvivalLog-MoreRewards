using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class OfferPlanTests
{
    private static readonly RewardGroup[] OneGroup = { new(1, 3) };
    private static readonly RewardLock[] NoExtras = Array.Empty<RewardLock>();

    /// <summary>The game's draw: the group's first reward (100 and up) not in the ones it is told to keep out.</summary>
    private static RewardDraw Pool(int size = 50) => (group, avoid) =>
        Enumerable.Range(group * 100, size).Where(reward => !avoid.Contains(reward)).Select(reward => (int?)reward).FirstOrDefault();

    /// <summary>A draw that ignores what it is told to keep out, as a careless draw might.</summary>
    private static RewardDraw Careless(params int[] order)
    {
        var next = 0;

        return (_, _) => next < order.Length ? order[next++] : null;
    }

    private static OfferAsk Ask(int count, int gamePin = 0, int gamePinSlot = 0, RewardLock[]? extras = null, Func<int, bool>? isUnwanted = null, bool noRepeat = false) =>
        new(count, gamePin, gamePinSlot, extras ?? NoExtras, isUnwanted ?? (_ => false), noRepeat);

    [Fact]
    public void With_nothing_locked_or_passed_on_the_game_pick_stays_and_the_rest_is_filled()
    {
        var offer = OfferPlan.Day(new[] { 107, 108, 109 }, Ask(5), OneGroup, Pool());

        Assert.Equal(new[] { 107, 108, 109, 100, 101 }, offer);
    }

    [Fact]
    public void At_the_game_three_cards_with_nothing_changed_the_offer_is_the_game_own()
    {
        var offer = OfferPlan.Day(new[] { 107, 108, 109 }, Ask(3), OneGroup, Pool());

        Assert.Equal(new[] { 107, 108, 109 }, offer);
    }

    [Fact]
    public void Banned_and_skipped_cards_are_swapped_for_wanted_ones()
    {
        var unwanted = new HashSet<int> { 108, 100 };

        var offer = OfferPlan.Day(new[] { 107, 108, 109 }, Ask(4, isUnwanted: unwanted.Contains), OneGroup, Pool());

        Assert.Equal(new[] { 107, 109, 101, 102 }, offer);
    }

    [Fact]
    public void A_draw_that_keeps_giving_unwanted_cards_gives_up_instead_of_looping()
    {
        var offer = OfferPlan.Day(Array.Empty<int>(), Ask(3, isUnwanted: _ => true), OneGroup, Pool());

        Assert.Empty(offer);
    }

    [Fact]
    public void Every_locked_card_keeps_its_slot_and_counts_toward_the_total()
    {
        var extras = new[] { new RewardLock(555, 4), new RewardLock(444, 1) };

        var offer = OfferPlan.Day(new[] { 107, 333, 109 }, Ask(6, gamePin: 333, gamePinSlot: 2, extras: extras), OneGroup, Pool());

        Assert.Equal(6, offer.Count);
        Assert.Equal(444, offer[1]);
        Assert.Equal(333, offer[2]);
        Assert.Equal(555, offer[4]);
    }

    [Fact]
    public void A_locked_card_is_never_offered_a_second_time()
    {
        var extras = new[] { new RewardLock(100, 0) };

        var offer = OfferPlan.Day(new[] { 100, 101, 102 }, Ask(5, extras: extras), OneGroup, Careless(100, 101, 103, 104));

        Assert.Equal(offer.Distinct().Count(), offer.Count);
        Assert.Equal(100, offer[0]);
    }

    [Fact]
    public void Extra_locks_past_the_end_of_a_short_offer_go_last()
    {
        var extras = new[] { new RewardLock(555, 9) };

        var offer = OfferPlan.Day(new[] { 107 }, Ask(10, extras: extras), OneGroup, Pool(1));

        Assert.Equal(new[] { 107, 100, 555 }, offer);
    }

    [Fact]
    public void Locks_never_push_the_offer_over_the_count_asked_for()
    {
        var extras = new[] { new RewardLock(555, 0) };

        var offer = OfferPlan.Day(new[] { 107, 333, 109 }, Ask(3, gamePin: 333, gamePinSlot: 1, extras: extras), OneGroup, Pool());

        Assert.Equal(new[] { 555, 333, 107 }, offer);
    }

    [Fact]
    public void A_fixed_day_keeps_its_list_and_only_gains_the_extra_locks()
    {
        var unwanted = new HashSet<int> { 801 };
        var extras = new[] { new RewardLock(555, 1) };

        var offer = OfferPlan.Day(new[] { 800, 801 }, Ask(6, extras: extras, isUnwanted: unwanted.Contains), null, Pool());

        Assert.Equal(new[] { 800, 555, 801 }, offer);
    }

    [Fact]
    public void A_refresh_keeps_the_locked_cards_and_draws_new_ones_around_them()
    {
        var extras = new[] { new RewardLock(555, 0) };

        var offer = OfferPlan.Refresh(new[] { 110, 111, 112 }, Ask(5, extras: extras), OneGroup, Pool(), Pool());

        Assert.Equal(new[] { 555, 110, 111, 112, 100 }, offer);
    }

    [Fact]
    public void A_refresh_with_nothing_new_gives_the_game_answer_so_it_costs_nothing()
    {
        RewardDraw nothing = (_, _) => null;
        var extras = new[] { new RewardLock(555, 0) };

        Assert.Empty(OfferPlan.Refresh(Array.Empty<int>(), Ask(5, extras: extras), OneGroup, nothing, Pool()));
        Assert.Equal(new[] { 333 }, OfferPlan.Refresh(new[] { 333 }, Ask(5, gamePin: 333, extras: extras), OneGroup, nothing, Pool()));
    }

    [Fact]
    public void A_refresh_of_only_unwanted_cards_counts_as_nothing_new()
    {
        RewardDraw nothing = (_, _) => null;

        var offer = OfferPlan.Refresh(new[] { 110, 111 }, Ask(4, isUnwanted: _ => true), OneGroup, nothing, Pool());

        Assert.Empty(offer);
    }

    [Fact]
    public void No_repeat_leaves_a_refresh_short_rather_than_bring_back_cards_just_shown()
    {
        var unseenSize2 = Pool(2);

        var repeats = OfferPlan.Refresh(Array.Empty<int>(), Ask(4), OneGroup, unseenSize2, (g, avoid) => Pool()(g, avoid));
        var noRepeat = OfferPlan.Refresh(Array.Empty<int>(), Ask(4, noRepeat: true), OneGroup, unseenSize2, Pool());

        Assert.Equal(4, repeats.Count);
        Assert.Equal(new[] { 100, 101 }, noRepeat);
    }

    [Fact]
    public void A_card_passed_on_is_swapped_for_a_new_one_in_the_same_slot()
    {
        var skipped = new HashSet<int> { 101 };

        var offer = OfferPlan.Replace(new[] { 100, 101, 102 }, 1, Ask(3, isUnwanted: skipped.Contains), OneGroup, Pool());

        Assert.Equal(new[] { 100, 103, 102 }, offer);
    }

    [Fact]
    public void A_swap_never_brings_back_a_card_on_screen_a_locked_one_or_one_passed_on()
    {
        var unwanted = new HashSet<int> { 101, 103 };
        var extras = new[] { new RewardLock(104, 0) };

        var offer = OfferPlan.Replace(new[] { 100, 101, 102 }, 1, Ask(4, extras: extras, isUnwanted: unwanted.Contains), OneGroup, Pool());

        Assert.Equal(new[] { 100, 105, 102 }, offer);
    }

    [Fact]
    public void A_swap_with_nothing_left_to_draw_only_takes_the_card_away()
    {
        var offer = OfferPlan.Replace(new[] { 100, 101, 102 }, 0, Ask(3, isUnwanted: id => id == 100), OneGroup, Pool(3));

        Assert.Equal(new[] { 101, 102 }, offer);
    }

    [Fact]
    public void On_a_fixed_day_a_swap_only_takes_the_card_away()
    {
        var offer = OfferPlan.Replace(new[] { 100, 101, 102 }, 2, Ask(3), null, Pool());

        Assert.Equal(new[] { 100, 101 }, offer);
    }
}
