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
        var give = Assert.Single(reward.Gives);
        Assert.Equal("game:AE_PowerSolarEfficiency", give.Effect);
        Assert.Equal(new FieldSet("FloatValue0", new FieldValue.Number(0.1)), Assert.Single(give.Fields));
        Assert.Null(reward.Levels);
    }

    [Fact]
    public void The_day_rules_are_read_and_left_out_they_change_nothing()
    {
        var file = RewardFile.Parse("""{ "rewards": [ { "id": 31271, "toDay": 40, "anyDay": 10, "alwaysOffered": true }, { "id": 31301 } ] }""");

        Assert.Empty(file.Problems);
        Assert.Equal(40, file.Rewards[0].ToDay);
        Assert.Equal(10, file.Rewards[0].AnyDay);
        Assert.True(file.Rewards[0].AlwaysOffered);
        Assert.Null(file.Rewards[1].ToDay);
        Assert.Null(file.Rewards[1].AnyDay);
        Assert.False(file.Rewards[1].AlwaysOffered);
    }

    [Fact]
    public void Levels_and_several_gives_are_read()
    {
        var file = RewardFile.Parse("""
            { "rewards": [ { "id": 39001, "copyFrom": 31271,
              "gives": [ { "effect": "mr:wind-power", "levels": [ { "FloatValue0": 0.1 }, { "FloatValue0": 0.2 } ] }, { "effect": "game:X" } ],
              "levels": [ { "cost": 3, "name": "One", "pools": { "1013": 2 }, "fields": { "Icon": "a" } }, { "description": "Two" } ] } ] }
            """);

        Assert.Empty(file.Problems);
        var reward = Assert.Single(file.Rewards);
        Assert.Equal(new[] { 39001, 39002 }, reward.Ids);
        Assert.Equal(new[] { "mr:wind-power", "game:X" }, reward.Gives.Select(give => give.Effect));
        Assert.Equal(new FieldSet("FloatValue0", new FieldValue.Number(0.2)), Assert.Single(reward.Gives[0].FieldsAt(5)));
        Assert.Empty(reward.Gives[1].FieldsAt(1));

        var levels = reward.Levels!;
        Assert.Equal(3, levels[0].Cost);
        Assert.Equal(new LocalText("One", "One"), levels[0].Name);
        Assert.Equal(new[] { new PoolEntry(1013, 2) }, levels[0].Pools);
        Assert.Equal(new FieldSet("Icon", new FieldValue.Text("a")), Assert.Single(levels[0].Fields));
        Assert.Null(levels[1].Cost);
        Assert.Null(levels[1].Pools);
        Assert.Equal(new LocalText("Two", "Two"), levels[1].Description);
    }

    [Theory]
    [InlineData("""{ "id": 1, "give": { "effect": "game:X" }, "gives": [] }""", "both")]
    [InlineData("""{ "id": 1, "levels": [] }""", "1 to 10 levels")]
    [InlineData("""{ "id": 1, "levels": [ {}, {}, {}, {}, {}, {}, {}, {}, {}, {}, {} ] }""", "1 to 10 levels")]
    [InlineData("""{ "id": 1, "gives": [ { "fields": {} } ] }""", "no \"effect\"")]
    [InlineData("""{ "id": 1, "levels": [ 5 ] }""", "level 1")]
    public void A_reward_with_bad_levels_or_gives_is_reported_and_skipped(string reward, string reason)
    {
        var file = RewardFile.Parse($$"""{ "rewards": [ {{reward}}, { "id": 2 } ] }""");

        Assert.Equal(2, Assert.Single(file.Rewards).Id);
        Assert.Contains(file.Problems, problem => problem.Contains(reason));
    }

    [Fact]
    public void An_entry_in_the_player_s_file_replaces_a_wind_reward_that_uses_any_of_the_same_ids()
    {
        var mine = RewardFile.Parse("""{ "rewards": [ { "id": 39002, "enabled": false }, { "id": 31271, "cost": 1 } ] }""");
        var wind = RewardFile.Parse("""{ "rewards": [ { "id": 39001, "copyFrom": 31271, "levels": [ {}, {}, {} ] }, { "id": 39010, "copyFrom": 31271 } ] }""");

        Assert.Equal(new[] { 39002, 31271, 39001, 39010 }, mine.Over(wind).Rewards.Select(reward => reward.Id));
        Assert.False(mine.Over(wind).Rewards.Single(reward => reward.Id == 39001).Enabled);
    }

    // Turning off a reward the mod carries must keep its rows, or a save that owns it loses it.
    [Fact]
    public void A_switched_off_entry_keeps_the_new_reward_below_it_switched_off()
    {
        var mine = RewardFile.Parse("""{ "rewards": [ { "id": 39010, "enabled": false } ] }""");
        var below = RewardFile.Parse("""{ "rewards": [ { "id": 39010, "copyFrom": 31271 }, { "id": 31272, "cost": 1 } ] }""");
        var both = mine.Over(below);

        Assert.Equal(new[] { 39010, 39010, 31272 }, both.Rewards.Select(reward => reward.Id));
        Assert.False(both.Rewards[1].Enabled);
        Assert.Equal(31271, both.Rewards[1].CopyFrom);
    }

    [Fact]
    public void An_enabled_entry_replaces_the_new_reward_below_it()
    {
        var mine = RewardFile.Parse("""{ "rewards": [ { "id": 39010, "copyFrom": 31271, "cost": 2 } ] }""");
        var below = RewardFile.Parse("""{ "rewards": [ { "id": 39010, "copyFrom": 31271 } ] }""");

        Assert.Single(mine.Over(below).Rewards);
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
              { "id": 3 },
              { "id": 4, "gives": [ { "effect": "game:X" }, { "effect": "mr:wind-power" } ] } ] }
            """);

        Assert.Equal(new[] { 2, 3 }, file.Without("mr:wind-power").Rewards.Select(reward => reward.Id));
    }

    [Fact]
    public void The_wind_rewards_read_cleanly()
    {
        var wind = RewardFile.Parse(Resource("MoreRewards.wind-rewards.json"));

        Assert.Empty(wind.Problems);
        Assert.Equal(new[] { 39001, 39002, 39003 }, Assert.Single(wind.Rewards).Ids);
    }

    [Fact]
    public void The_default_file_reads_cleanly_and_changes_nothing()
    {
        var file = RewardFile.Parse(Resource("MoreRewards.rewards.default.json"));

        // No entries at all, so the Rewards tab shows no reward as changed on a fresh install. The example is a comment.
        Assert.Empty(file.Problems);
        Assert.Empty(file.Rewards);
        Assert.Empty(file.Buffs);
        Assert.Empty(file.Effects);
    }
}
