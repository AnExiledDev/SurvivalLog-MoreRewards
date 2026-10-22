using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class ShopTests
{
    private static ScreenSettings Shop(int markup = 50, int increase = 10, int perDay = 0) =>
        ScreenSettings.From(new ScreenInput { ShopEnabled = true, ShopMarkup = markup, ShopIncrease = increase, ShopPerDay = perDay });

    [Fact]
    public void Picking_a_reward_in_the_shop_costs_the_markup_on_top_of_the_card_price()
    {
        Assert.Equal(9, ShopPrice.Of(6, Shop(), 0, isEarly: false));
    }

    [Fact]
    public void Each_buy_since_the_reset_raises_the_price_and_it_rounds_up()
    {
        Assert.Equal(10, ShopPrice.Of(6, Shop(), 1, isEarly: false));
        Assert.Equal(11, ShopPrice.Of(6, Shop(), 3, isEarly: false));
    }

    [Fact]
    public void Buying_before_the_needs_are_met_costs_double()
    {
        Assert.Equal(18, ShopPrice.Of(6, Shop(), 0, isEarly: true));
    }

    [Fact]
    public void With_no_markup_or_rise_the_shop_charges_the_card_price()
    {
        Assert.Equal(6, ShopPrice.Of(6, Shop(0, 0), 5, isEarly: false));
    }

    [Fact]
    public void The_card_price_takes_the_earned_discount_off_as_the_game_does()
    {
        Assert.Equal(6, ShopPrice.CardCost(6, 0f));
        Assert.Equal(5, ShopPrice.CardCost(6, 0.2f));
        Assert.Equal(0, ShopPrice.CardCost(6, 1.5f));
    }

    [Fact]
    public void A_daily_limit_counts_down_and_no_limit_has_no_count()
    {
        Assert.Equal(1, ShopPrice.LeftToday(Shop(perDay: 3), new ShopTally(5, 2)));
        Assert.Equal(0, ShopPrice.LeftToday(Shop(perDay: 3), new ShopTally(5, 4)));
        Assert.Null(ShopPrice.LeftToday(Shop(perDay: 0), new ShopTally(5, 40)));
    }
}
