using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class PageCommandTests
{
    [Fact]
    public void Card_buttons_name_the_card_and_its_slot()
    {
        Assert.Equal(new PageCommand(PageVerb.Pin, 30012, 4), PageCommands.Read("slmod|morerewards|pin|30012|4"));
        Assert.Equal(new PageCommand(PageVerb.Ban, 30012, 2), PageCommands.Read("slmod|morerewards|ban|30012|2"));
    }

    [Fact]
    public void Shop_and_leaving_need_no_card()
    {
        Assert.Equal(PageVerb.Shop, PageCommands.Read("slmod|morerewards|shop")?.Verb);
        Assert.Equal(new PageCommand(PageVerb.ShopBuy, 30012, 0), PageCommands.Read("slmod|morerewards|shopbuy|30012"));
        Assert.Equal(PageVerb.CashOut, PageCommands.Read("slmod|morerewards|cashout")?.Verb);
    }

    [Fact]
    public void The_ban_list_names_the_ban_to_lift()
    {
        Assert.Equal(PageVerb.Bans, PageCommands.Read("slmod|morerewards|bans")?.Verb);
        Assert.Equal(new PageCommand(PageVerb.Unban, 30012, 0), PageCommands.Read("slmod|morerewards|unban|30012"));
        Assert.Null(PageCommands.Read("slmod|morerewards|unban|x"));
    }

    [Fact]
    public void A_setting_change_names_the_section_the_key_and_the_value()
    {
        Assert.Equal(PageVerb.Settings, PageCommands.Read("slmod|morerewards|settings")?.Verb);
        Assert.Equal(new PageCommand(PageVerb.Set, 0, 0, "Shop.ShopPerDay", "3"), PageCommands.Read("slmod|morerewards|set|Shop.ShopPerDay|3"));
        Assert.Null(PageCommands.Read("slmod|morerewards|set|ShopPerDay|3"));
        Assert.Null(PageCommands.Read("slmod|morerewards|set|Shop.ShopPerDay|"));
        Assert.Null(PageCommands.Read("slmod|morerewards|skip|30012|0"));
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
