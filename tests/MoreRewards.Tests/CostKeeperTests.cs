using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class CostKeeperTests
{
    [Fact]
    public void Six_rewards_put_the_refresh_at_four_points()
    {
        var keeper = new CostKeeper(RewardSettings.From(6, 0));

        Assert.Equal(new CostWrite(4, 2, IsPutBack: false), keeper.Next(2));
    }

    [Fact]
    public void A_cost_already_written_is_left_alone()
    {
        var keeper = new CostKeeper(RewardSettings.From(6, 0));

        keeper.Next(2);

        Assert.Null(keeper.Next(4));
        Assert.Null(keeper.Next(4));
    }

    [Fact]
    public void A_cost_changed_by_something_else_is_put_back_and_not_scaled_again()
    {
        var keeper = new CostKeeper(RewardSettings.From(6, 0));

        keeper.Next(2);

        Assert.Equal(new CostWrite(4, 2, IsPutBack: true), keeper.Next(9));
        Assert.Null(keeper.Next(4));
    }

    [Fact]
    public void The_game_own_settings_never_write_the_field()
    {
        var keeper = new CostKeeper(RewardSettings.From(3, 0));

        Assert.Null(keeper.Next(2));
        Assert.Null(keeper.Next(2));
    }

    [Fact]
    public void A_cost_the_game_has_not_loaded_waits()
    {
        var keeper = new CostKeeper(RewardSettings.From(10, 0));

        Assert.Null(keeper.Next(0));
        Assert.Equal(6, keeper.Next(2)?.Cost);
    }

    [Fact]
    public void A_fixed_cost_is_written_whatever_the_game_charges()
    {
        var keeper = new CostKeeper(RewardSettings.From(3, 5));

        Assert.Equal(5, keeper.Next(2)?.Cost);
        Assert.Null(keeper.Next(5));
    }
}
