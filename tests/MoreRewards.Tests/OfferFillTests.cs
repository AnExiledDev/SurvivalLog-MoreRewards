using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class OfferFillTests
{
    private static readonly RewardGroup[] TwoFromOneThenOneFromTwo = { new(1, 2), new(2, 1) };
    private static readonly RewardGroup[] OneGroup = { new(1, 1) };

    /// <summary>
    /// The game's draw as the hooks use it: the group's first reward not offered yet, or none when the group is spent.
    /// Group A holds 100-199, group B 200-299, and so on.
    /// </summary>
    private static RewardDraw Pools(params (int Group, int Size)[] pools) => (group, offered) =>
    {
        var size = pools.FirstOrDefault(pool => pool.Group == group).Size;

        return Enumerable.Range(group * 100, size).Where(reward => !offered.Contains(reward)).Select(reward => (int?)reward).FirstOrDefault();
    };

    [Fact]
    public void Ten_rewards_take_seven_more_in_the_day_own_mix()
    {
        var added = OfferFill.TopUp(new[] { 100, 101, 200 }, 10, TwoFromOneThenOneFromTwo, Pools((1, 50), (2, 50)));

        Assert.Equal(new[] { 102, 103, 201, 104, 105, 202, 106 }, added);
    }

    [Fact]
    public void A_spent_group_drops_out_and_the_others_fill_the_rest()
    {
        var added = OfferFill.TopUp(new[] { 100, 101, 200 }, 8, TwoFromOneThenOneFromTwo, Pools((1, 3), (2, 50)));

        Assert.Equal(new[] { 102, 201, 202, 203, 204 }, added);
    }

    [Fact]
    public void When_every_group_is_spent_the_offer_stays_short()
    {
        var added = OfferFill.TopUp(new[] { 100, 101, 200 }, 10, TwoFromOneThenOneFromTwo, Pools((1, 3), (2, 2)));

        Assert.Equal(new[] { 102, 201 }, added);
    }

    [Fact]
    public void A_reward_already_offered_is_never_offered_twice()
    {
        RewardDraw repeats = (_, _) => 100;

        var added = OfferFill.TopUp(new[] { 100, 101, 200 }, 6, TwoFromOneThenOneFromTwo, repeats);

        Assert.Empty(added);
    }

    [Fact]
    public void A_refresh_tries_unseen_rewards_before_ones_shown_before()
    {
        var seenBefore = new[] { 104, 105, 106 };
        RewardDraw unseen = (group, offered) => Pools((1, 5))(group, offered.Concat(seenBefore).ToList());
        RewardDraw any = Pools((1, 50));

        var added = OfferFill.TopUpRefresh(new[] { 100, 101 }, 7, new[] { new RewardGroup(1, 2) }, unseen, any);

        Assert.Equal(new[] { 102, 103, 104, 105, 106 }, added);
    }

    [Fact]
    public void A_refresh_with_nothing_new_stays_empty_so_it_costs_nothing()
    {
        RewardDraw nothingUnseen = (_, _) => null;

        var added = OfferFill.TopUpRefresh(Array.Empty<int>(), 6, OneGroup, nothingUnseen, Pools((1, 50)));

        Assert.Empty(added);
    }

    [Fact]
    public void One_new_reward_from_the_mod_lets_a_refresh_fill_up_with_ones_shown_before()
    {
        var added = OfferFill.TopUpRefresh(Array.Empty<int>(), 4, OneGroup, Pools((1, 1)), Pools((1, 50)));

        Assert.Equal(new[] { 100, 101, 102, 103 }, added);
    }

    [Fact]
    public void A_new_reward_from_the_game_lets_a_refresh_fill_up_with_ones_shown_before()
    {
        RewardDraw nothingUnseen = (_, _) => null;

        var added = OfferFill.TopUpRefresh(new[] { 100 }, 4, OneGroup, nothingUnseen, Pools((1, 50)));

        Assert.Equal(new[] { 101, 102, 103 }, added);
    }

    [Fact]
    public void A_full_offer_draws_nothing()
    {
        var draws = 0;
        RewardDraw counting = (_, _) => ++draws + 900;

        var added = OfferFill.TopUp(new[] { 100, 101, 200 }, 3, TwoFromOneThenOneFromTwo, counting);

        Assert.Empty(added);
        Assert.Equal(0, draws);
    }

    [Fact]
    public void Groups_that_give_none_of_the_game_rewards_give_none_of_the_extras()
    {
        var added = OfferFill.TopUp(new[] { 100, 101, 102 }, 5, new[] { new RewardGroup(1, 3), new RewardGroup(2, 0), new RewardGroup(3, -1) }, Pools((1, 50), (2, 50), (3, 50)));

        Assert.Equal(new[] { 103, 104 }, added);
    }

    [Fact]
    public void A_day_without_groups_adds_nothing()
    {
        var added = OfferFill.TopUp(new[] { 100, 101, 102 }, 10, Array.Empty<RewardGroup>(), Pools((1, 50)));

        Assert.Empty(added);
    }
}
