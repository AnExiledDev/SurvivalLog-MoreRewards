using System;
using System.Linq;
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
        Assert.False(book.IsBanned(30011));
        Assert.Empty(book.Bans(new[] { 30011 }));
        Assert.Equal(new ShopTally(0, 0), book.Shop(3, ShopReset.Never));
    }

    [Fact]
    public void A_whole_line_ban_is_listed_and_lifted_as_one_with_what_it_cost()
    {
        var book = new RunBook(new FakeCounters());
        var ids = new[] { 1, 2, 3, 4 };

        Assert.Equal(new[] { 2, 1, 3 }, book.Ban(2, new[] { 1, 2, 3 }, 6));

        var ban = Assert.Single(book.Bans(ids));

        Assert.Equal(2, ban.Head);
        Assert.Equal(new[] { 1, 2, 3 }, ban.Members);
        Assert.Equal(6, ban.Paid);

        Assert.Equal(ban.Members, book.Unban(2, ids)?.Members);
        Assert.False(book.IsBanned(1) || book.IsBanned(2) || book.IsBanned(3));
        Assert.Empty(book.Bans(ids));
        Assert.Null(book.Unban(2, ids));
    }

    [Fact]
    public void A_reward_already_banned_stays_in_its_own_ban()
    {
        var book = new RunBook(new FakeCounters());
        var ids = new[] { 1, 2, 3 };

        book.Ban(1, Array.Empty<int>(), 2);

        Assert.Equal(new[] { 3, 2 }, book.Ban(3, new[] { 1, 2 }, 4));
        Assert.Equal(new[] { new[] { 1 }, new[] { 2, 3 } }, book.Bans(ids).Select(ban => ban.Members.ToArray()));

        book.Unban(3, ids);

        Assert.True(book.IsBanned(1));
        Assert.False(book.IsBanned(2));
    }

    [Fact]
    public void A_ban_made_before_bans_kept_their_cost_stands_alone_with_no_price()
    {
        var counters = new FakeCounters();
        var book = new RunBook(counters);

        counters.Set("slmr.ban.5", 1);
        counters.Set("slmr.ban.6", 1);

        var bans = book.Bans(new[] { 5, 6 });

        Assert.Equal(new[] { 5, 6 }, bans.Select(ban => ban.Head));
        Assert.All(bans, ban => Assert.Null(ban.Paid));
    }

    [Fact]
    public void A_free_ban_keeps_a_price_of_zero_rather_than_none()
    {
        var book = new RunBook(new FakeCounters());

        book.Ban(7, Array.Empty<int>(), 0);

        Assert.Equal(0, Assert.Single(book.Bans(new[] { 7 })).Paid);
    }

    [Theory]
    [InlineData(10, 50, 5)]
    [InlineData(5, 50, 2)]
    [InlineData(10, 0, 0)]
    [InlineData(10, 100, 10)]
    [InlineData(10, 300, 10)]
    public void Lifting_a_ban_gives_back_the_set_share_of_what_it_cost(int paid, int percent, int refund)
    {
        Assert.Equal(refund, BanRefund.Of(new BanGroup(1, new[] { 1 }, paid), 99, percent));
    }

    [Fact]
    public void A_ban_with_no_kept_price_refunds_a_share_of_the_ban_cost_now()
    {
        Assert.Equal(4, BanRefund.Of(new BanGroup(1, new[] { 1 }, null), 8, 50));
        Assert.Equal(0, BanRefund.Of(new BanGroup(1, new[] { 1 }, 0), 8, 50));
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
