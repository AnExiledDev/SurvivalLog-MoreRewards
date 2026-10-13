using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class RateKeeperTests
{
    private const float GameScaling = 1000f;
    private const int GameBaseUnits = 50;

    [Fact]
    public void Ten_percent_adds_five_to_the_game_five()
    {
        var keeper = new RateKeeper(10);

        Assert.Equal(new RateWrite(50, GameBaseUnits, IsPutBack: false), keeper.Next(GameBaseUnits, 0, GameScaling));
    }

    [Fact]
    public void A_rate_already_in_place_is_left_alone()
    {
        var keeper = new RateKeeper(10);

        Assert.Null(keeper.Next(GameBaseUnits, 50, GameScaling));
        Assert.Null(keeper.Next(GameBaseUnits, 50, GameScaling));
    }

    [Fact]
    public void A_loaded_character_gets_the_rate_again()
    {
        var keeper = new RateKeeper(10);

        keeper.Next(GameBaseUnits, 0, GameScaling);

        Assert.Null(keeper.Next(GameBaseUnits, 50, GameScaling));
        Assert.Equal(new RateWrite(50, GameBaseUnits, IsPutBack: true), keeper.Next(GameBaseUnits, 0, GameScaling));
    }

    [Fact]
    public void A_rate_below_the_game_lowers_it()
    {
        var keeper = new RateKeeper(3);

        Assert.Equal(-20, keeper.Next(GameBaseUnits, 0, GameScaling)?.Strengthening);
    }

    [Fact]
    public void A_character_with_another_base_still_totals_the_rate()
    {
        var keeper = new RateKeeper(10);

        keeper.Next(GameBaseUnits, 0, GameScaling);

        Assert.Equal(20, keeper.Next(80, 50, GameScaling)?.Strengthening);
        Assert.Equal(100, keeper.Next(0, 0, GameScaling)?.Strengthening);
    }

    [Fact]
    public void The_game_rate_on_a_new_character_writes_nothing()
    {
        var keeper = new RateKeeper(InterestRate.GameRate);

        Assert.Null(keeper.Next(GameBaseUnits, 0, GameScaling));
    }
}
