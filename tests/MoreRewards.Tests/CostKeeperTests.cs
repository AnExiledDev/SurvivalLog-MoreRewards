using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class CostKeeperTests
{
    [Fact]
    public void Six_rewards_put_the_refresh_at_four_points()
    {
        var keeper = new CostKeeper(RewardSettings.From(6, -1));

        Assert.Equal(new CostWrite(4, 2, IsPutBack: false), keeper.Next(2));
    }

    [Fact]
    public void A_cost_already_written_is_left_alone()
    {
        var keeper = new CostKeeper(RewardSettings.From(6, -1));

        keeper.Next(2);

        Assert.Null(keeper.Next(4));
        Assert.Null(keeper.Next(4));
    }

    [Fact]
    public void A_cost_changed_by_something_else_is_put_back_and_not_scaled_again()
    {
        var keeper = new CostKeeper(RewardSettings.From(6, -1));

        keeper.Next(2);

        Assert.Equal(new CostWrite(4, 2, IsPutBack: true), keeper.Next(9));
        Assert.Null(keeper.Next(4));
    }

    [Fact]
    public void The_game_own_settings_never_write_the_field()
    {
        var keeper = new CostKeeper(RewardSettings.From(3, -1));

        Assert.Null(keeper.Next(2));
        Assert.Null(keeper.Next(2));
    }

    [Fact]
    public void A_cost_the_game_has_not_loaded_waits()
    {
        var keeper = new CostKeeper(RewardSettings.From(10, -1));

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

    [Fact]
    public void A_free_refresh_writes_zero_and_keeps_it()
    {
        var keeper = new CostKeeper(RewardSettings.From(10, 0));

        Assert.Equal(new CostWrite(0, 2, IsPutBack: false), keeper.Next(2));
        Assert.Null(keeper.Next(0));
    }

    [Fact]
    public void A_free_refresh_the_game_reloads_is_put_back_to_zero()
    {
        var keeper = new CostKeeper(RewardSettings.From(3, 0));

        keeper.Next(2);

        Assert.Equal(new CostWrite(0, 2, IsPutBack: true), keeper.Next(2));
    }

    [Fact]
    public void A_changed_option_count_works_the_cost_out_from_the_game_own()
    {
        var keeper = new CostKeeper(RewardSettings.From(6, -1));

        keeper.Next(2);
        keeper.Use(RewardSettings.From(10, -1));

        Assert.Equal(new CostWrite(6, 2, IsPutBack: false), keeper.Next(4));
        Assert.Null(keeper.Next(6));
    }

    [Fact]
    public void Leaving_a_free_refresh_writes_the_new_cost()
    {
        var keeper = new CostKeeper(RewardSettings.From(10, 0));

        keeper.Next(2);
        keeper.Use(RewardSettings.From(6, -1));

        Assert.Equal(new CostWrite(4, 2, IsPutBack: false), keeper.Next(0));
    }

    [Fact]
    public void A_change_before_the_cost_has_loaded_waits_for_it()
    {
        var keeper = new CostKeeper(RewardSettings.From(6, -1));

        keeper.Use(RewardSettings.From(10, -1));

        Assert.Null(keeper.Next(0));
        Assert.Equal(6, keeper.Next(2)?.Cost);
    }

    [Fact]
    public void Going_back_to_the_game_settings_puts_the_game_cost_back()
    {
        var keeper = new CostKeeper(RewardSettings.From(6, -1));

        keeper.Next(2);
        keeper.Use(RewardSettings.From(3, -1));

        Assert.Equal(new CostWrite(2, 2, IsPutBack: false), keeper.Next(4));
    }
}
