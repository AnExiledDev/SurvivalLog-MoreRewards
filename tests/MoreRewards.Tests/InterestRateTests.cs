using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class InterestRateTests
{
    private const float GameScaling = 1000f;
    private const int GameBaseUnits = 50;

    [Theory]
    [InlineData(0, null)]
    [InlineData(-3, null)]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    [InlineData(10, 10)]
    [InlineData(100, 100)]
    [InlineData(500, 100)]
    public void Zero_keeps_the_game_rate_and_a_rate_too_high_is_held_to_the_most(int interestRate, int? rate)
    {
        Assert.Equal(rate, InterestRate.From(interestRate));
    }

    [Fact]
    public void Ten_percent_is_a_hundred_of_the_game_units()
    {
        Assert.Equal(100, InterestRate.Units(10, GameScaling));
        Assert.Equal(GameBaseUnits, InterestRate.Units(InterestRate.GameRate, GameScaling));
    }

    [Fact]
    public void Every_rate_reaches_the_game_exactly()
    {
        for (var rate = 1; rate <= InterestRate.MostRate; rate++)
        {
            foreach (var baseUnits in new[] { 0, GameBaseUnits })
            {
                var strengthening = InterestRate.Units(rate, GameScaling) - baseUnits;
                var stored = StoredByGame(InterestRate.GameValue(strengthening, GameScaling));

                Assert.Equal(rate * 10, baseUnits + stored);
            }
        }
    }

    /// <summary>What AttributeComponent.SetStrengtheningValue_Float (rva 0x2de1e00) keeps: the value times the scaling, cut to a whole number.</summary>
    private static int StoredByGame(float value) => (int)(float)(value * GameScaling);
}
