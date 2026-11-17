using System.Text.Json;
using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class RewardsPageTests
{
    private const string Call = "window.__slmrRewards&&window.__slmrRewards.data(";

    private static readonly GameSnapshot Game = new(
        new[]
        {
            new GameReward(31271, 1, "../../Res/Reward/solar.png", "Reward/solar", new[] { 31000 }, new[] { 910001 }, new[]
            {
                new GameRewardLevel(31271, 4, new LocalText("Solar", "太阳能"), new LocalText("More solar power.", "太阳能更多。"), "Buff/AE_PowerSolarEfficiency",
                    new[] { new FieldSet("ID", new FieldValue.Number(5)), new FieldSet("FloatValue0", new FieldValue.Number(0.1)), new FieldSet("FloatValue1", new FieldValue.Number(0)), new FieldSet("Note", new FieldValue.Text("")), new FieldSet("Stack", new FieldValue.Flag(false)) },
                    new[] { new PoolEntry(1013, 2) }),
                new GameRewardLevel(31272, 6, new LocalText("Solar II", "太阳能 Lv2"), new LocalText("Even more.", "更多。"), "Buff/AE_PowerSolarEfficiency",
                    new[] { new FieldSet("FloatValue0", new FieldValue.Number(0.2)), new FieldSet("Stack", new FieldValue.Flag(true)) },
                    new[] { new PoolEntry(1013, 1), new PoolEntry(1020, 3) }),
            }),
        },
        new Dictionary<string, IReadOnlyList<FieldSet>> { ["Buff/AE_PowerSolarEfficiency"] = new[] { new FieldSet("ConfigName", new FieldValue.Text("x")), new FieldSet("FloatValue0", new FieldValue.Number(0.1)) } },
        new[] { new GameIcon("../../Res/Food/apple.png", "Food/apple"), new GameIcon("../../Res/Reward/solar.png", "Reward/solar") },
        new[] { 1020, 1013, 1013 });

    private static JsonElement Data(string call)
    {
        Assert.StartsWith(Call, call);
        Assert.EndsWith(")", call);
        Assert.All(call, c => Assert.True(c < 128, "non-ASCII in the call"));
        Assert.DoesNotContain("</", call);

        return JsonDocument.Parse(call.Substring(Call.Length, call.Length - Call.Length - 1)).RootElement.Clone();
    }

    [Fact]
    public void Game_rewards_go_with_every_level_and_its_pools()
    {
        var data = Data(RewardsPage.DataCall(Game, """{ "rewards": [] }""", Array.Empty<string>(), null));
        var reward = Assert.Single(data.GetProperty("game").EnumerateArray());

        Assert.Equal(31271, reward.GetProperty("id").GetInt32());
        Assert.Equal(1, reward.GetProperty("available").GetInt32());
        Assert.Equal("Res/Reward/solar.png", reward.GetProperty("icon").GetString());
        Assert.Equal("Reward/solar", reward.GetProperty("unityIcon").GetString());
        Assert.Equal(31000, Assert.Single(reward.GetProperty("needs").EnumerateArray()).GetInt32());
        Assert.Equal(910001, Assert.Single(reward.GetProperty("furniture").EnumerateArray()).GetInt32());

        var levels = reward.GetProperty("levels").EnumerateArray().ToList();
        Assert.Equal(2, levels.Count);
        Assert.Equal(31272, levels[1].GetProperty("id").GetInt32());
        Assert.Equal(6, levels[1].GetProperty("cost").GetInt32());
        Assert.Equal("太阳能 Lv2", levels[1].GetProperty("name").GetProperty("zh").GetString());
        Assert.Equal("Even more.", levels[1].GetProperty("desc").GetProperty("en").GetString());
        Assert.Equal("AE_PowerSolarEfficiency", levels[1].GetProperty("script").GetString());
        Assert.Equal(1, levels[1].GetProperty("pools").GetProperty("1013").GetInt32());
        Assert.Equal(3, levels[1].GetProperty("pools").GetProperty("1020").GetInt32());
        Assert.Equal(2, levels[0].GetProperty("pools").GetProperty("1013").GetInt32());
    }

    [Fact]
    public void Values_hold_only_the_fields_an_effect_sets()
    {
        var data = Data(RewardsPage.DataCall(Game, "{}", Array.Empty<string>(), null));
        var values = data.GetProperty("game")[0].GetProperty("levels")[0].GetProperty("values");

        Assert.Equal(new[] { "FloatValue0" }, values.EnumerateObject().Select(p => p.Name));
        Assert.Equal(0.1, values.GetProperty("FloatValue0").GetDouble());
        Assert.True(data.GetProperty("game")[0].GetProperty("levels")[1].GetProperty("values").GetProperty("Stack").GetBoolean());
    }

    [Fact]
    public void Templates_drop_the_script_folder_and_the_name_field()
    {
        var templates = Data(RewardsPage.DataCall(Game, "{}", Array.Empty<string>(), null)).GetProperty("templates");
        var template = Assert.Single(templates.EnumerateObject());

        Assert.Equal("AE_PowerSolarEfficiency", template.Name);
        Assert.Equal(new[] { "FloatValue0" }, template.Value.EnumerateObject().Select(p => p.Name));
    }

    [Fact]
    public void Pools_come_once_each_in_order()
    {
        var pools = Data(RewardsPage.DataCall(Game, "{}", Array.Empty<string>(), null)).GetProperty("pools");

        Assert.Equal(new[] { 1013, 1020 }, pools.EnumerateArray().Select(p => p.GetInt32()));
    }

    [Fact]
    public void Icons_list_the_rewards_first_then_the_items_by_folder_each_once()
    {
        var icons = Data(RewardsPage.DataCall(Game, "{}", Array.Empty<string>(), null)).GetProperty("icons").EnumerateArray().ToList();

        Assert.Equal(2, icons.Count);
        Assert.Equal("rewards", icons[0].GetProperty("group").GetString());
        Assert.Equal("Res/Reward/solar.png", icons[0].GetProperty("icon").GetString());
        Assert.Equal("Food", icons[1].GetProperty("group").GetString());
        Assert.Equal("Food/apple", icons[1].GetProperty("unityIcon").GetString());
    }

    [Fact]
    public void The_file_goes_as_plain_json_without_comments()
    {
        var data = Data(RewardsPage.DataCall(Game, "// note\n{ \"rewards\": [ { \"id\": 31271, \"cost\": 2 }, ] }", Array.Empty<string>(), null));

        Assert.Equal(2, data.GetProperty("file").GetProperty("rewards")[0].GetProperty("cost").GetInt32());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData(null)]
    public void A_file_that_is_not_json_goes_as_null(string? text)
    {
        var data = Data(RewardsPage.DataCall(Game, text, Array.Empty<string>(), null));

        Assert.Equal(JsonValueKind.Null, data.GetProperty("file").ValueKind);
        Assert.NotEmpty(data.GetProperty("problems").EnumerateArray());
    }

    [Fact]
    public void Problems_come_once_each()
    {
        var data = Data(RewardsPage.DataCall(Game, """{ "rewards": [ { "id": 1, "levels": [] } ] }""", new[] { "late", "late" }, null));
        var problems = data.GetProperty("problems").EnumerateArray().Select(p => p.GetString()).ToList();

        Assert.Equal(problems.Distinct().Count(), problems.Count);
        Assert.Contains("late", problems);
        Assert.Contains(problems, p => p!.Contains("1 to 10 levels"));
    }

    [Fact]
    public void Without_wind_power_there_are_no_built_in_rewards()
    {
        var data = Data(RewardsPage.DataCall(Game, "{}", Array.Empty<string>(), null));

        Assert.False(data.GetProperty("windPower").GetBoolean());
        Assert.Equal(JsonValueKind.Null, data.GetProperty("builtIn").ValueKind);
    }

    [Fact]
    public void With_wind_power_the_wind_rewards_go_as_built_in()
    {
        var wind = RewardFileTests.Resource("MoreRewards.wind-rewards.json");
        var data = Data(RewardsPage.DataCall(Game, "{}", Array.Empty<string>(), wind));
        var reward = Assert.Single(data.GetProperty("builtIn").GetProperty("rewards").EnumerateArray());

        Assert.True(data.GetProperty("windPower").GetBoolean());
        Assert.Equal(39001, reward.GetProperty("id").GetInt32());
        Assert.Equal(3, reward.GetProperty("levels").GetArrayLength());
    }

    [Fact]
    public void A_save_with_a_problem_is_refused()
    {
        var check = RewardsPage.CheckSave("""{ "rewards": [ { "id": 1, "levels": [] } ] }""");

        var refused = Assert.IsType<SaveCheck.Refused>(check);
        Assert.Contains(refused.Problems, p => p.Contains("1 to 10 levels"));
    }

    [Fact]
    public void A_good_save_is_written_indented_with_the_chinese_kept()
    {
        var check = RewardsPage.CheckSave("""{"rewards":[{"id":39101,"copyFrom":31271,"name":{"en":"Boost","zh":"增益"},"pools":{"1013":2}}]}""");

        var write = Assert.IsType<SaveCheck.Write>(check);
        Assert.Contains("增益", write.Text);
        Assert.Contains("\n", write.Text.TrimEnd());
        Assert.Empty(RewardFile.Parse(write.Text).Problems);
    }

    [Theory]
    [InlineData("../../Res/a.png", "Res/a.png")]
    [InlineData("Res/a.png", "Res/a.png")]
    [InlineData("", "")]
    public void Paths_are_made_to_start_at_the_web_root(string webIcon, string expected) =>
        Assert.Equal(expected, RewardsPage.FromRoot(webIcon));
}
