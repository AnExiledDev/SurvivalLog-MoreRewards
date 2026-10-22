using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class RunBookTests
{
    [Fact]
    public void Locks_come_back_in_the_order_they_were_made()
    {
        var book = new RunBook(new FakeCounters());

        book.SetLocks(new[] { new RewardLock(30012, 4), new RewardLock(30021, 1) });

        Assert.Equal(new[] { new RewardLock(30012, 4), new RewardLock(30021, 1) }, book.Locks());
    }

    [Fact]
    public void Removing_a_lock_leaves_no_gap_that_would_hide_the_ones_after_it()
    {
        var book = new RunBook(new FakeCounters());

        book.SetLocks(new[] { new RewardLock(1, 0), new RewardLock(2, 1), new RewardLock(3, 2) });
        book.SetLocks(new[] { new RewardLock(1, 0), new RewardLock(3, 2) });

        Assert.Equal(new[] { new RewardLock(1, 0), new RewardLock(3, 2) }, book.Locks());
    }

    [Fact]
    public void A_new_run_has_no_locks_bans_or_shop_buys()
    {
        var book = new RunBook(new FakeCounters());

        Assert.Empty(book.Locks());
        Assert.False(book.IsUnwanted(30011, 3));
        Assert.Equal(new ShopTally(0, 0), book.Shop(3, ShopReset.Never));
    }

    [Fact]
    public void A_skipped_card_comes_back_the_next_day_but_a_banned_one_never_does()
    {
        var book = new RunBook(new FakeCounters());

        book.Skip(30011, 4);
        book.Ban(30021);

        Assert.True(book.IsUnwanted(30011, 4));
        Assert.False(book.IsUnwanted(30011, 5));
        Assert.True(book.IsUnwanted(30021, 5));
        Assert.True(book.IsUnwanted(30021, 40));
    }

    [Fact]
    public void Shop_buys_raise_prices_until_the_day_ends_when_the_rise_resets_daily()
    {
        var book = new RunBook(new FakeCounters());

        book.RecordShopBuy(6, ShopReset.Day);
        book.RecordShopBuy(6, ShopReset.Day);

        Assert.Equal(new ShopTally(2, 2), book.Shop(6, ShopReset.Day));
        Assert.Equal(new ShopTally(0, 0), book.Shop(7, ShopReset.Day));
    }

    [Fact]
    public void A_weekly_rise_lasts_seven_days_from_the_first_buy_while_the_daily_count_starts_over()
    {
        var book = new RunBook(new FakeCounters());

        book.RecordShopBuy(3, ShopReset.Week);
        book.RecordShopBuy(5, ShopReset.Week);

        Assert.Equal(new ShopTally(2, 0), book.Shop(9, ShopReset.Week));
        Assert.Equal(new ShopTally(0, 0), book.Shop(10, ShopReset.Week));
    }

    [Fact]
    public void A_rise_that_never_resets_keeps_counting_across_days()
    {
        var book = new RunBook(new FakeCounters());

        book.RecordShopBuy(2, ShopReset.Never);
        book.RecordShopBuy(30, ShopReset.Never);

        Assert.Equal(2, book.Shop(80, ShopReset.Never).Buys);
    }

    [Fact]
    public void After_a_reset_the_next_buy_starts_a_new_rise()
    {
        var book = new RunBook(new FakeCounters());

        book.RecordShopBuy(1, ShopReset.Week);
        book.RecordShopBuy(1, ShopReset.Week);
        book.RecordShopBuy(9, ShopReset.Week);

        Assert.Equal(1, book.Shop(15, ShopReset.Week).Buys);
        Assert.Equal(0, book.Shop(16, ShopReset.Week).Buys);
    }
}
