using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class RewardSettingsTests
{
    [Fact]
    public void The_defaults_change_nothing()
    {
        var settings = RewardSettings.From(3, 0);

        Assert.False(settings.AddsOptions);
        Assert.False(settings.ChangesCost);
    }

    [Fact]
    public void Extra_rewards_also_change_the_cost()
    {
        var settings = RewardSettings.From(6, 0);

        Assert.True(settings.AddsOptions);
        Assert.True(settings.ChangesCost);
    }

    [Fact]
    public void A_fixed_cost_alone_changes_only_the_cost()
    {
        var settings = RewardSettings.From(3, 5);

        Assert.False(settings.AddsOptions);
        Assert.True(settings.ChangesCost);
    }

    [Theory]
    [InlineData(-4, 3)]
    [InlineData(0, 3)]
    [InlineData(2, 3)]
    [InlineData(10, 10)]
    [InlineData(20, 20)]
    [InlineData(500, 20)]
    public void The_number_of_rewards_never_drops_below_the_game_or_runs_away(int asked, int kept)
    {
        Assert.Equal(kept, RewardSettings.From(asked, 0).OptionCount);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(12, 12)]
    [InlineData(1000, 99)]
    public void A_cost_out_of_range_is_held_to_it(int asked, int kept)
    {
        Assert.Equal(kept, RewardSettings.From(3, asked).FixedRefreshCost);
    }
}
