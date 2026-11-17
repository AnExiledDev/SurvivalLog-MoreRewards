using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class WindLinkTests
{
    private static MrGrant Wind(int reward, double share) => new(RewardPlan.WindPower, reward, new[] { new FieldSet(RewardPlan.WindShare, new FieldValue.Number(share)) });

    [Fact]
    public void Only_the_wind_rewards_owned_add_to_the_share()
    {
        var grants = new[] { Wind(39001, 0.15), Wind(39002, 0.15), Wind(39003, 0.15) };

        Assert.Equal(0.30, WindLink.Share(grants, reward => reward != 39003), 6);
    }

    [Fact]
    public void Nothing_owned_leaves_the_turbines_as_they_are()
    {
        Assert.Equal(1f, WindLink.Multiplier(WindLink.Share(new[] { Wind(39001, 0.15) }, _ => false)));
    }

    [Fact]
    public void A_grant_for_another_effect_adds_nothing()
    {
        var other = new MrGrant("something-else", 39001, new[] { new FieldSet(RewardPlan.WindShare, new FieldValue.Number(0.5)) });

        Assert.Equal(0, WindLink.Share(new[] { other }, _ => true));
    }

    [Theory]
    [InlineData(0.45, 1.45f)]
    [InlineData(-0.5, 1f)]
    [InlineData(double.NaN, 1f)]
    public void The_multiplier_is_one_plus_the_share_and_never_below_one(double share, float multiplier)
    {
        Assert.Equal(multiplier, WindLink.Multiplier(share), 5);
    }
}
