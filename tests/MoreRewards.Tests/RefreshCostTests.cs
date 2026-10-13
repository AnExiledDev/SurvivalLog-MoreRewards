using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class RefreshCostTests
{
    private const int GameCost = 2;

    // The player's own examples: 3 options cost 2, 4 cost 3, 6 cost 4.
    [Theory]
    [InlineData(3, 2)]
    [InlineData(4, 3)]
    [InlineData(5, 3)]
    [InlineData(6, 4)]
    [InlineData(7, 4)]
    [InlineData(8, 5)]
    [InlineData(10, 6)]
    [InlineData(20, 11)]
    public void The_automatic_cost_grows_with_the_rewards_rounding_down(int options, int cost)
    {
        Assert.Equal(cost, RefreshCost.Auto(GameCost, options));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void Three_rewards_cost_what_the_game_charges(int gameCost)
    {
        Assert.Equal(gameCost, RefreshCost.Auto(gameCost, RewardSettings.GameOptionCount));
    }

    [Fact]
    public void More_rewards_never_cost_less()
    {
        for (var options = RewardSettings.GameOptionCount; options < RewardSettings.MostOptions; options++)
        {
            Assert.True(RefreshCost.Auto(GameCost, options + 1) >= RefreshCost.Auto(GameCost, options));
        }
    }

    [Fact]
    public void A_cost_the_game_has_not_loaded_is_never_scaled()
    {
        Assert.Equal(0, RefreshCost.Auto(0, 10));
    }

    [Fact]
    public void A_fixed_cost_wins_over_the_number_of_rewards()
    {
        Assert.Equal(1, RefreshCost.Target(GameCost, RewardSettings.From(10, 1)));
        Assert.Equal(7, RefreshCost.Target(GameCost, RewardSettings.From(3, 7)));
    }

    [Fact]
    public void Without_a_fixed_cost_the_cost_follows_the_rewards()
    {
        Assert.Equal(4, RefreshCost.Target(GameCost, RewardSettings.From(6, -1)));
    }

    [Theory]
    [InlineData(3, 2)]
    [InlineData(4, 3)]
    [InlineData(10, 6)]
    public void Minus_one_is_the_automatic_cost(int options, int cost)
    {
        Assert.Equal(cost, RefreshCost.Target(GameCost, RewardSettings.From(options, RewardSettings.AutomaticRefreshCost)));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(20)]
    public void Zero_makes_every_refresh_free_however_many_rewards(int options)
    {
        Assert.Equal(0, RefreshCost.Target(GameCost, RewardSettings.From(options, 0)));
    }
}
