using System.Reflection;
using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class RewardFileTests
{
    internal static string Resource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    [Fact]
    public void A_reward_is_read_with_every_part()
    {
        var file = RewardFile.Parse("""
            // a comment
            { "rewards": [ {
              "id": 39101, "copyFrom": 31271,
              "name": { "en": "Boost", "zh": "增益" }, "description": "plain",
              "cost": 5, "needs": [31271], "furniture": [910001],
              "pools": { "1013": 2 },
              "fields": { "WebIcon": "icon.png" },
              "give": { "effect": "game:AE_PowerSolarEfficiency", "fields": { "FloatValue0": 0.1 } },
            } ] }
            """);

        Assert.Empty(file.Problems);
        var reward = Assert.Single(file.Rewards);
        Assert.Equal(39101, reward.Id);
        Assert.True(reward.Enabled);
        Assert.Equal(31271, reward.CopyFrom);
        Assert.Equal(new LocalText("Boost", "增益"), reward.Name);
        Assert.Equal(new LocalText("plain", "plain"), reward.Description);
        Assert.Equal(5, reward.Cost);
        Assert.Equal(new[] { 31271 }, reward.Needs);
        Assert.Equal(new[] { 910001 }, reward.Furniture);
        Assert.Equal(new[] { new PoolEntry(1013, 2) }, reward.Pools);
        Assert.Equal(new FieldSet("WebIcon", new FieldValue.Text("icon.png")), Assert.Single(reward.Fields));
        Assert.Equal("game:AE_PowerSolarEfficiency", reward.Give!.Effect);
        Assert.Equal(new FieldSet("FloatValue0", new FieldValue.Number(0.1)), Assert.Single(reward.Give.Fields));
    }

    [Fact]
    public void A_reward_with_a_bad_weight_is_reported_and_the_others_still_read()
    {
        var file = RewardFile.Parse("""{ "rewards": [ { "id": 1, "pools": { "1013": 0 } }, { "id": 2 } ] }""");

        Assert.Equal(2, Assert.Single(file.Rewards).Id);
        Assert.Contains(file.Problems, problem => problem.Contains("weight of 1 or more"));
    }

    [Fact]
    public void A_file_that_is_not_json_applies_nothing_and_says_why()
    {
        var file = RewardFile.Parse("{ not json");

        Assert.Empty(file.Rewards);
        Assert.NotEmpty(file.Problems);
    }

    [Fact]
    public void Without_drops_only_the_rewards_that_give_that_effect()
    {
        var file = RewardFile.Parse("""
            { "rewards": [
              { "id": 1, "give": { "effect": "mr:wind-power" } },
              { "id": 2, "give": { "effect": "game:X" } },
              { "id": 3 } ] }
            """);

        Assert.Equal(new[] { 2, 3 }, file.Without("mr:wind-power").Rewards.Select(reward => reward.Id));
    }

    [Fact]
    public void The_wind_rewards_read_cleanly_and_match_sunlight_collection()
    {
        var wind = RewardFile.Parse(Resource("MoreRewards.wind-rewards.json"));

        Assert.Empty(wind.Problems);
        Assert.Equal(new[] { 39001, 39002, 39003 }, wind.Rewards.Select(reward => reward.Id));
        Assert.Equal(new int?[] { 31271, 31272, 31273 }, wind.Rewards.Select(reward => reward.CopyFrom));
        Assert.Equal(new int?[] { 10, 16, 24 }, wind.Rewards.Select(reward => reward.Cost));
        Assert.All(wind.Rewards, reward => Assert.Equal("mr:wind-power", reward.Give!.Effect));
        Assert.All(wind.Rewards, reward => Assert.Equal(new[] { 910001, 910002, 910003 }, reward.Furniture));
        Assert.Null(wind.Rewards[0].Needs);
        Assert.Equal(new[] { 39001 }, wind.Rewards[1].Needs);
        Assert.Equal(new[] { 39002 }, wind.Rewards[2].Needs);
    }

    /// <summary>Sunlight Collection's pools in the game's Data Dump, card by card.</summary>
    [Fact]
    public void Each_wind_reward_is_in_the_same_pools_as_its_sunlight_card_with_the_same_weights()
    {
        var wind = RewardFile.Parse(Resource("MoreRewards.wind-rewards.json"));
        var both = new[] { 9105, 9106, 9111, 9130, 9131 };
        var all = new[] { 9107, 9108, 9112, 9132, 9133 };

        Assert.Equal(Pools((1013, 1), (1020, 3), (9110, 1)).Concat(Ones(both)).Concat(Ones(all)).OrderBy(pool => pool.Group), wind.Rewards[0].Pools.OrderBy(pool => pool.Group));
        Assert.Equal(Pools((1013, 1), (1021, 3)).Concat(Ones(both)).Concat(Ones(all)).OrderBy(pool => pool.Group), wind.Rewards[1].Pools.OrderBy(pool => pool.Group));
        Assert.Equal(Pools((1013, 1), (1022, 3)).Concat(Ones(all)).OrderBy(pool => pool.Group), wind.Rewards[2].Pools.OrderBy(pool => pool.Group));
    }

    [Fact]
    public void The_default_file_reads_cleanly_and_changes_nothing()
    {
        var file = RewardFile.Parse(Resource("MoreRewards.rewards.default.json"));

        Assert.Empty(file.Problems);
        Assert.All(file.Rewards, reward => Assert.False(reward.Enabled));
        Assert.Empty(file.Buffs);
        Assert.Empty(file.Effects);
    }

    private static IEnumerable<PoolEntry> Pools(params (int Group, int Weight)[] pools) => pools.Select(pool => new PoolEntry(pool.Group, pool.Weight));

    private static IEnumerable<PoolEntry> Ones(IEnumerable<int> groups) => groups.Select(group => new PoolEntry(group, 1));
}
