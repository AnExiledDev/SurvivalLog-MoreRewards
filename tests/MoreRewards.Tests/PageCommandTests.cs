using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class PageCommandTests
{
    [Fact]
    public void Card_buttons_name_the_card_and_its_slot()
    {
        Assert.Equal(new PageCommand(PageVerb.Pin, 30012, 4), PageCommands.Read("slmod|morerewards|pin|30012|4"));
        Assert.Equal(new PageCommand(PageVerb.Skip, 30012, 0), PageCommands.Read("slmod|morerewards|skip|30012|0"));
        Assert.Equal(new PageCommand(PageVerb.Ban, 30012, 2), PageCommands.Read("slmod|morerewards|ban|30012|2"));
    }

    [Fact]
    public void Shop_and_leaving_need_no_card()
    {
        Assert.Equal(PageVerb.Shop, PageCommands.Read("slmod|morerewards|shop")?.Verb);
        Assert.Equal(new PageCommand(PageVerb.ShopBuy, 30012, 0), PageCommands.Read("slmod|morerewards|shopbuy|30012"));
        Assert.Equal(PageVerb.CashOut, PageCommands.Read("slmod|morerewards|cashout")?.Verb);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("slmod|newloopplus|pin|30012|4")]
    [InlineData("slmod|morerewards|pin|abc|4")]
    [InlineData("slmod|morerewards|pin|0|4")]
    [InlineData("slmod|morerewards|pin|30012|-1")]
    [InlineData("slmod|morerewards|pin|30012")]
    [InlineData("slmod|morerewards|fly")]
    public void Messages_that_are_not_ours_or_not_usable_do_nothing(string? message)
    {
        Assert.Null(PageCommands.Read(message));
    }
}
