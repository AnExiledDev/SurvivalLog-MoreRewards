using System;
using System.Collections.Generic;
using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class PerksTests
{
    private static MrGrant Grant(string effect, int rewardId, params (string Field, double Value)[] fields)
    {
        var sets = new List<FieldSet>();

        foreach (var (field, value) in fields) sets.Add(new FieldSet(field, new FieldValue.Number(value)));

        return new MrGrant(effect, rewardId, sets);
    }

    private static MrGrant Crate(int rewardId, int[] items, int[] counts) =>
        new(RewardPlan.SupplyCrate, rewardId, new[]
        {
            new FieldSet(RewardPlan.ItemsField, new FieldValue.Numbers(items)),
            new FieldSet(RewardPlan.CountsField, new FieldValue.Numbers(counts)),
        });

    [Fact]
    public void Perks_add_up_over_the_owned_rewards_only()
    {
        var grants = new[]
        {
            Grant(RewardPlan.MoreCards, 1, (RewardPlan.CountField, 1)),
            Grant(RewardPlan.MoreCards, 2, (RewardPlan.CountField, 1)),
            Grant(RewardPlan.MoreCards, 3, (RewardPlan.CountField, 1)),
            Grant(RewardPlan.MoreLocks, 4, (RewardPlan.CountField, 1)),
            Grant(RewardPlan.FreeRefresh, 5, (RewardPlan.CountField, 1)),
            Grant(RewardPlan.BanTokens, 6, (RewardPlan.CountField, 3)),
            Grant(RewardPlan.Investor, 7, (RewardPlan.RateField, 2), (RewardPlan.CapField, 5)),
            Grant(RewardPlan.Investor, 8, (RewardPlan.RateField, 2), (RewardPlan.CapField, 5)),
        };
        var owned = new HashSet<int> { 1, 2, 4, 5, 6, 7, 8 };

        var perks = Perks.Of(grants, owned.Contains);

        Assert.Equal(new Perks(2, 1, 1, 3, 4, 10), perks);
    }

    [Fact]
    public void Owning_nothing_adds_nothing()
    {
        var grants = new[] { Grant(RewardPlan.MoreCards, 1, (RewardPlan.CountField, 1)) };

        Assert.Equal(Perks.None, Perks.Of(grants, _ => false));
    }

    [Fact]
    public void More_cards_stop_at_the_most_the_screen_shows()
    {
        var settings = ScreenSettings.From(new ScreenInput { OptionCount = 18 });

        Assert.Equal(RewardSettings.MostOptions, settings.With(Perks.None with { MoreCards = 5 }, 0).OptionCount);
        Assert.Equal(19, settings.With(Perks.None with { MoreCards = 1 }, 0).OptionCount);
    }

    [Fact]
    public void More_locks_still_leave_one_card_unlocked()
    {
        var settings = ScreenSettings.From(new ScreenInput { OptionCount = 3 });

        Assert.Equal(2, settings.With(Perks.None with { MoreLocks = 1 }, 0).MaxLocked);
        Assert.Equal(2, settings.With(Perks.None with { MoreLocks = 4 }, 0).MaxLocked);
        Assert.Equal(4, settings.With(Perks.None with { MoreCards = 2, MoreLocks = 4 }, 0).MaxLocked);
    }

    [Fact]
    public void Free_bans_show_the_ban_button_at_no_cost_until_they_run_out()
    {
        var settings = ScreenSettings.From(new ScreenInput { BanButton = false, BanCost = 4 });

        var withBans = settings.With(Perks.None, 2);
        var usedUp = settings.With(Perks.None, 0);

        Assert.True(withBans.BanButton);
        Assert.Equal(0, withBans.BanCost);
        Assert.False(usedUp.BanButton);
        Assert.Equal(4, usedUp.BanCost);
    }

    [Fact]
    public void A_free_refresh_costs_nothing_and_the_cost_comes_back_once_it_is_used()
    {
        var keeper = new CostKeeper(RewardSettings.From(6, -1));

        keeper.Next(2);
        keeper.UseFree(true);

        Assert.Equal(new CostWrite(0, 2, IsPutBack: false), keeper.Next(4));

        keeper.UseFree(false);

        Assert.Equal(new CostWrite(4, 2, IsPutBack: false), keeper.Next(0));
    }

    [Fact]
    public void The_rewards_bonus_adds_to_the_character_own_rate_when_no_rate_is_set()
    {
        var keeper = new RateKeeper(null);

        keeper.UseBonus(4);

        Assert.Equal(new RateWrite(40, 50, IsPutBack: false), keeper.Next(50, 0, 1000f));
    }

    [Fact]
    public void The_rewards_bonus_adds_on_top_of_a_set_rate()
    {
        var keeper = new RateKeeper(10);

        keeper.UseBonus(4);

        Assert.Equal(new RateWrite(90, 50, IsPutBack: false), keeper.Next(50, 0, 1000f));
    }

    [Fact]
    public void Free_bans_count_for_the_run_and_free_refreshes_start_again_each_day()
    {
        var book = new RunBook(new FakeCounters());

        book.UseFreeBan();
        book.UseFreeBan();
        book.UseFreeRefresh();
        book.NewDayOfRefreshes();

        Assert.Equal(2, book.FreeBansUsed);
        Assert.Equal(0, book.FreeRefreshesUsed);

        book.UseFreeRefresh();

        Assert.Equal(1, book.FreeRefreshesUsed);
    }

    [Fact]
    public void A_lottery_draws_from_its_whole_range()
    {
        var grants = new[] { Grant(RewardPlan.Lottery, 39050, (RewardPlan.LeastField, 0), (RewardPlan.MostField, 15)) };
        var range = Assert.Single(Payouts.LotteriesOf(grants, 39050));
        var asked = (Min: -1, Max: -1);

        Payouts.Draw(range, (min, max) => { asked = (min, max); return min; });

        Assert.Equal(new PointRange(0, 15), range);
        Assert.Equal((0, 16), asked);
        Assert.Empty(Payouts.LotteriesOf(grants, 39049));
    }

    [Fact]
    public void A_lottery_with_its_ends_backwards_never_pays_less_than_its_least()
    {
        var grants = new[] { Grant(RewardPlan.Lottery, 1, (RewardPlan.LeastField, 8), (RewardPlan.MostField, 3)) };

        Assert.Equal(new PointRange(8, 8), Assert.Single(Payouts.LotteriesOf(grants, 1)));
    }

    [Fact]
    public void A_crate_gives_each_item_with_its_count_and_at_least_one()
    {
        var grants = new[] { Crate(39049, new[] { 20312, 20002, 0, 20302 }, new[] { 2, 0, 5 }) };

        var crate = Payouts.CratesOf(grants, 39049);

        Assert.Equal(new[] { new CrateItem(20312, 2), new CrateItem(20002, 1), new CrateItem(20302, 1) }, crate);
    }
}
