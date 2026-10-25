using System.Text.Json;
using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class PageDataTests
{
    [Fact]
    public void Game_text_with_quotes_and_markup_comes_through_unchanged()
    {
        const string name = "Say \"hi\" </script>\\ \u2028 done\n";
        var row = new ShopRow(7, name, "d", "a.png", 6, 9, null, RewardLife.OneTime, "body", false);

        using var json = JsonDocument.Parse(PageData.Shop(new[] { row }, 20, null, 40, Language.English));
        var parsed = json.RootElement.GetProperty("rows")[0];

        Assert.Equal(name, parsed.GetProperty("name").GetString());
        Assert.DoesNotContain("</script>", PageData.Text(name));
        Assert.Equal(0, parsed.GetProperty("life").GetInt32());
        Assert.Equal(JsonValueKind.Null, parsed.GetProperty("why").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("left").ValueKind);
        Assert.Equal(40, json.RootElement.GetProperty("rise").GetInt32());
    }

    [Fact]
    public void The_setup_tells_the_shop_how_its_prices_rise_and_reset()
    {
        var settings = ScreenSettings.From(new ScreenInput { ShopEnabled = true, ShopIncrease = 15, ShopReset = ShopReset.Week });
        var setup = new PageSetup(Language.English, settings, 0, Array.Empty<RewardLock>(), new Dictionary<int, RewardLife>(), 0.05f, 10);

        using var json = JsonDocument.Parse(PageData.Setup(setup));

        Assert.True(json.RootElement.GetProperty("shop").GetBoolean());
        Assert.Equal(15, json.RootElement.GetProperty("shopIncrease").GetInt32());
        Assert.Equal("week", json.RootElement.GetProperty("shopReset").GetString());
    }

    [Fact]
    public void The_setup_carries_the_locks_settings_and_which_rewards_are_one_time()
    {
        var settings = ScreenSettings.From(new ScreenInput { OptionCount = 8, MaxLocked = 3, BanButton = true, BanCost = 2, CashOut = 15 });
        var lives = new Dictionary<int, RewardLife> { [1] = RewardLife.OneTime, [2] = RewardLife.Permanent, [3] = RewardLife.Timed(259200) };
        var setup = new PageSetup(Language.Chinese, settings, 30011, new[] { new RewardLock(30012, 3) }, lives, 0.05f, 10);

        using var json = JsonDocument.Parse(PageData.Setup(setup));
        var root = json.RootElement;

        Assert.Equal("zh", root.GetProperty("lang").GetString());
        Assert.Equal(3, root.GetProperty("maxLocked").GetInt32());
        Assert.Equal(30011, root.GetProperty("gamePin").GetInt32());
        Assert.Equal(30012, root.GetProperty("extras")[0].GetInt32());
        Assert.True(root.GetProperty("ban").GetBoolean());
        Assert.Equal(15, root.GetProperty("cashOut").GetInt32());
        Assert.Equal(0.05, root.GetProperty("rate").GetDouble(), 4);
        Assert.Equal(0, root.GetProperty("life").GetProperty("1").GetInt32());
        Assert.False(root.GetProperty("life").TryGetProperty("2", out _));
        Assert.Equal(259200, root.GetProperty("life").GetProperty("3").GetInt32());
        Assert.False(root.TryGetProperty("notInterested", out _));
    }

    [Fact]
    public void The_ban_list_carries_each_ban_with_its_names_and_refund()
    {
        var rows = new[] { new BanRow(30012, new[] { "A \"one\"", "B" }, 3) };

        using var json = JsonDocument.Parse(PageData.Bans(rows, 50, Language.English));
        var root = json.RootElement;
        var row = root.GetProperty("rows")[0];

        Assert.Equal(50, root.GetProperty("refund").GetInt32());
        Assert.Equal(30012, row.GetProperty("id").GetInt32());
        Assert.Equal("A \"one\"", row.GetProperty("names")[0].GetString());
        Assert.Equal(3, row.GetProperty("refund").GetInt32());
    }

    [Fact]
    public void The_settings_carry_each_setting_with_its_kind_range_and_choices()
    {
        var rows = new[]
        {
            new SettingRow("Shop.ShopPerDay", "Shop", "ShopPerDay", SettingKind.Int, "2", 0, 20, Array.Empty<string>(), "How many", false, true),
            new SettingRow("Shop.ShopReset", "Shop", "ShopReset", SettingKind.Choice, "Day", 0, 0, new[] { "Never", "Day", "Week" }, "When", false, true),
            new SettingRow("General.Enabled", "General", "Enabled", SettingKind.Bool, "true", 0, 0, Array.Empty<string>(), "On", true, true),
        };

        using var json = JsonDocument.Parse(PageData.Settings(rows, Language.English));
        var list = json.RootElement.GetProperty("rows");

        Assert.Equal("int", list[0].GetProperty("kind").GetString());
        Assert.Equal(20, list[0].GetProperty("max").GetInt32());
        Assert.Equal("2", list[0].GetProperty("value").GetString());
        Assert.Equal("Week", list[1].GetProperty("options")[2].GetString());
        Assert.Equal("bool", list[2].GetProperty("kind").GetString());
        Assert.True(list[2].GetProperty("restart").GetBoolean());
        Assert.True(list[2].GetProperty("shown").GetBoolean());
    }
}
