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
            new GameReward(31271, 1, "../../Res/Reward/solar.png", "Reward/solar", new[] { 31000 }, new[] { 910001 },
            new GameRules(true, 6, new[] { 2, 3 }, 4, 9, new[] { "EventTag: TAG101" }), new[]
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
        new[] { 1020, 1013, 1013 },
        new[] { new GameCharacter(2, new LocalText("Student", "大学生")), new GameCharacter(1, new LocalText("Worker", "打工仔")) },
        new[] { new DayEntry(57, new[] { new DayPool(9107, 3), new DayPool(1005, 3) }), new DayEntry(1, new[] { new DayPool(9114, 1) }) },
        new Dictionary<int, int> { [9114] = 7, [1005] = 4, [9107] = 375 });

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
        var data = Data(RewardsPage.DataCall(Game, """{ "rewards": [] }""", Array.Empty<string>(), BuiltInRewards.None));
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
        var data = Data(RewardsPage.DataCall(Game, "{}", Array.Empty<string>(), BuiltInRewards.None));
        var values = data.GetProperty("game")[0].GetProperty("levels")[0].GetProperty("values");

        Assert.Equal(new[] { "FloatValue0" }, values.EnumerateObject().Select(p => p.Name));
        Assert.Equal(0.1, values.GetProperty("FloatValue0").GetDouble());
        Assert.True(data.GetProperty("game")[0].GetProperty("levels")[1].GetProperty("values").GetProperty("Stack").GetBoolean());
    }

    [Fact]
    public void Templates_drop_the_script_folder_and_the_name_field()
    {
        var templates = Data(RewardsPage.DataCall(Game, "{}", Array.Empty<string>(), BuiltInRewards.None)).GetProperty("templates");
        var template = Assert.Single(templates.EnumerateObject());

        Assert.Equal("AE_PowerSolarEfficiency", template.Name);
        Assert.Equal(new[] { "FloatValue0" }, template.Value.EnumerateObject().Select(p => p.Name));
    }

    [Fact]
    public void A_game_reward_sends_its_rules_and_other_needs()
    {
        var reward = Data(RewardsPage.DataCall(Game, "{}", Array.Empty<string>(), BuiltInRewards.None)).GetProperty("game")[0];

        Assert.True(reward.GetProperty("isReget").GetBoolean());
        Assert.Equal(6, reward.GetProperty("endless").GetInt32());
        Assert.Equal(new[] { 2, 3 }, reward.GetProperty("chars").EnumerateArray().Select(c => c.GetInt32()));
        Assert.Equal(4, reward.GetProperty("layers").GetInt32());
        Assert.Equal(9, reward.GetProperty("fromDay").GetInt32());
        Assert.Equal("EventTag: TAG101", Assert.Single(reward.GetProperty("also").EnumerateArray()).GetString());
    }

    [Fact]
    public void Characters_come_by_ID_with_both_names()
    {
        var characters = Data(RewardsPage.DataCall(Game, "{}", Array.Empty<string>(), BuiltInRewards.None)).GetProperty("characters");

        Assert.Equal(new[] { 1, 2 }, characters.EnumerateArray().Select(c => c.GetProperty("id").GetInt32()));
        Assert.Equal("Student", characters[1].GetProperty("name").GetProperty("en").GetString());
    }

    [Fact]
    public void Days_come_in_order_with_their_pools_in_draw_order()
    {
        var days = Data(RewardsPage.DataCall(Game, "{}", Array.Empty<string>(), BuiltInRewards.None)).GetProperty("days");

        Assert.Equal(new[] { 1, 57 }, days.EnumerateArray().Select(d => d.GetProperty("day").GetInt32()));
        Assert.Equal(new[] { 9107, 3, 1005, 3 }, days[1].GetProperty("pools").EnumerateArray().SelectMany(p => p.EnumerateArray()).Select(n => n.GetInt32()));
    }

    [Fact]
    public void The_tab_gets_each_pool_size_and_how_many_cards_a_day_shows()
    {
        var data = Data(RewardsPage.DataCall(Game, "{}", Array.Empty<string>(), BuiltInRewards.None, 20));

        Assert.Equal(20, data.GetProperty("cards").GetInt32());
        Assert.Equal(375, data.GetProperty("poolSizes").GetProperty("9107").GetInt32());
        Assert.Equal(new[] { "1005", "9107", "9114" }, data.GetProperty("poolSizes").EnumerateObject().Select(p => p.Name));
        Assert.Equal(3, Data(RewardsPage.DataCall(Game, "{}", Array.Empty<string>(), BuiltInRewards.None)).GetProperty("cards").GetInt32());
    }

    [Fact]
    public void Pools_come_once_each_in_order()
    {
        var pools = Data(RewardsPage.DataCall(Game, "{}", Array.Empty<string>(), BuiltInRewards.None)).GetProperty("pools");

        Assert.Equal(new[] { 1013, 1020 }, pools.EnumerateArray().Select(p => p.GetInt32()));
    }

    [Fact]
    public void Icons_list_the_rewards_first_then_the_items_by_folder_each_once()
    {
        var icons = Data(RewardsPage.DataCall(Game, "{}", Array.Empty<string>(), BuiltInRewards.None)).GetProperty("icons").EnumerateArray().ToList();

        Assert.Equal(2, icons.Count);
        Assert.Equal("rewards", icons[0].GetProperty("group").GetString());
        Assert.Equal("Res/Reward/solar.png", icons[0].GetProperty("icon").GetString());
        Assert.Equal("Food", icons[1].GetProperty("group").GetString());
        Assert.Equal("Food/apple", icons[1].GetProperty("unityIcon").GetString());
    }

    [Fact]
    public void The_file_goes_as_plain_json_without_comments()
    {
        var data = Data(RewardsPage.DataCall(Game, "// note\n{ \"rewards\": [ { \"id\": 31271, \"cost\": 2 }, ] }", Array.Empty<string>(), BuiltInRewards.None));

        Assert.Equal(2, data.GetProperty("file").GetProperty("rewards")[0].GetProperty("cost").GetInt32());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData(null)]
    public void A_file_that_is_not_json_goes_as_null(string? text)
    {
        var data = Data(RewardsPage.DataCall(Game, text, Array.Empty<string>(), BuiltInRewards.None));

        Assert.Equal(JsonValueKind.Null, data.GetProperty("file").ValueKind);
        Assert.NotEmpty(data.GetProperty("problems").EnumerateArray());
    }

    [Fact]
    public void Problems_come_once_each()
    {
        var data = Data(RewardsPage.DataCall(Game, """{ "rewards": [ { "id": 1, "levels": [] } ] }""", new[] { "late", "late" }, BuiltInRewards.None));
        var problems = data.GetProperty("problems").EnumerateArray().Select(p => p.GetString()).ToList();

        Assert.Equal(problems.Distinct().Count(), problems.Count);
        Assert.Contains("late", problems);
        Assert.Contains(problems, p => p!.Contains("1 to 10 levels"));
    }

    [Fact]
    public void Without_wind_power_there_are_no_built_in_rewards()
    {
        var data = Data(RewardsPage.DataCall(Game, "{}", Array.Empty<string>(), BuiltInRewards.None));

        Assert.False(data.GetProperty("windPower").GetBoolean());
        Assert.Equal(JsonValueKind.Null, data.GetProperty("builtIn").ValueKind);
    }

    [Fact]
    public void With_wind_power_the_wind_rewards_go_as_built_in()
    {
        var wind = RewardFileTests.Resource("MoreRewards.wind-rewards.json");
        var data = Data(RewardsPage.DataCall(Game, "{}", Array.Empty<string>(), new BuiltInRewards(wind, null)));
        var reward = Assert.Single(data.GetProperty("builtIn").GetProperty("rewards").EnumerateArray());

        Assert.True(data.GetProperty("windPower").GetBoolean());
        Assert.Equal(39001, reward.GetProperty("id").GetInt32());
        Assert.Equal(3, reward.GetProperty("levels").GetArrayLength());
    }

    /// <summary>The tab names ExtraRewards on the extra ones, so it has to know which built-in rewards they are.</summary>
    [Fact]
    public void With_extra_rewards_on_they_go_as_built_in_and_their_ids_are_listed()
    {
        var extra = RewardFileTests.Resource("MoreRewards.extra-rewards.json");
        var wind = RewardFileTests.Resource("MoreRewards.wind-rewards.json");
        var data = Data(RewardsPage.DataCall(Game, "{}", Array.Empty<string>(), new BuiltInRewards(wind, extra)));
        var builtIn = data.GetProperty("builtIn").GetProperty("rewards").EnumerateArray().Select(reward => reward.GetProperty("id").GetInt32()).ToList();
        var extraIds = data.GetProperty("extraIds").EnumerateArray().Select(id => id.GetInt32()).ToList();

        Assert.Contains(39001, builtIn);
        Assert.Contains(39049, builtIn);
        Assert.Contains(39049, extraIds);
        Assert.DoesNotContain(39001, extraIds);
    }

    [Fact]
    public void With_extra_rewards_off_no_ids_are_listed()
    {
        var data = Data(RewardsPage.DataCall(Game, "{}", Array.Empty<string>(), BuiltInRewards.None));

        Assert.Equal(0, data.GetProperty("extraIds").GetArrayLength());
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
