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
        Assert.True(parsed.GetProperty("once").GetBoolean());
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
        var lives = new Dictionary<int, RewardLife> { [1] = RewardLife.OneTime, [2] = RewardLife.Permanent };
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
        Assert.True(root.GetProperty("life").TryGetProperty("1", out _));
        Assert.False(root.GetProperty("life").TryGetProperty("2", out _));
    }
}
