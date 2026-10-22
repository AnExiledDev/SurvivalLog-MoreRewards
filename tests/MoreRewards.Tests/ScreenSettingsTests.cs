using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class ScreenSettingsTests
{
    [Fact]
    public void At_least_one_card_is_always_left_unlocked_so_a_refresh_has_a_slot_to_fill()
    {
        Assert.Equal(2, ScreenSettings.From(new ScreenInput { OptionCount = 3, MaxLocked = 9 }).MaxLocked);
        Assert.Equal(9, ScreenSettings.From(new ScreenInput { OptionCount = 10, MaxLocked = 9 }).MaxLocked);
    }

    [Fact]
    public void The_game_one_lock_is_the_least()
    {
        var settings = ScreenSettings.From(new ScreenInput { OptionCount = 10, MaxLocked = 0 });

        Assert.Equal(1, settings.MaxLocked);
        Assert.False(settings.LocksMore);
    }

    [Fact]
    public void Numbers_out_of_range_are_held_to_the_range()
    {
        var settings = ScreenSettings.From(new ScreenInput { BanCost = 500, CashOut = -3, ShopMarkup = 999, ShopIncrease = -1, ShopPerDay = 1000 });

        Assert.Equal(ScreenSettings.MostBanCost, settings.BanCost);
        Assert.Equal(0, settings.CashOut);
        Assert.Equal(ScreenSettings.MostShopMarkup, settings.ShopMarkup);
        Assert.Equal(0, settings.ShopIncrease);
        Assert.Equal(ScreenSettings.MostShopPerDay, settings.ShopPerDay);
    }
}
