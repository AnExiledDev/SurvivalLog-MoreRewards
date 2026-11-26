using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class BuyCheckTests
{
    [Fact]
    public void A_free_reward_bought_again_counts_as_bought()
    {
        // It moves neither the points nor the owned list. Counting it as turned down refunds the shop markup for a
        // reward the game gave.
        Assert.True(ShopPrice.IsBought(pointsBefore: 10, pointsAfter: 10, ownedBefore: 3, ownedAfter: 3, cardCost: 0, isAlive: true));
    }

    [Fact]
    public void A_free_buy_while_dead_is_turned_down()
    {
        Assert.False(ShopPrice.IsBought(10, 10, 3, 3, cardCost: 0, isAlive: false));
    }

    [Fact]
    public void A_buy_that_cost_nothing_and_was_not_free_was_turned_down()
    {
        Assert.False(ShopPrice.IsBought(10, 10, 3, 3, cardCost: 5, isAlive: true));
    }

    [Fact]
    public void A_buy_that_took_points_went_through()
    {
        Assert.True(ShopPrice.IsBought(10, 5, 3, 3, cardCost: 5, isAlive: true));
    }

    [Fact]
    public void A_buy_that_added_an_owned_reward_went_through()
    {
        Assert.True(ShopPrice.IsBought(10, 10, 3, 4, cardCost: 5, isAlive: true));
    }
}
