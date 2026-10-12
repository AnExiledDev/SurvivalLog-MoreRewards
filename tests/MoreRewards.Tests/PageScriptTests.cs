using Xunit;

namespace MoreRewards.Tests;

public class PageScriptTests
{
    [Fact]
    public void Three_cards_are_left_exactly_as_the_game_draws_them()
    {
        var page = new PageScript();

        page.Run("var frame = addRewardsFrame(); showCards(frame, 3);");
        page.Tick();

        Assert.False(page.Is("rootOf(frame).classList.contains('slmr-many')"));
        Assert.False(page.Is("rootOf(frame).classList.contains('slmr-compact')"));
        Assert.Equal(0, page.Number("styleCount(frame)"));
        Assert.Equal("", page.Text("rootOf(frame).style.getPropertyValue('--slmr-columns')"));
    }

    [Fact]
    public void Ten_cards_sit_in_two_compact_rows_of_five()
    {
        var page = new PageScript();

        page.Run("var frame = addRewardsFrame(); showCards(frame, 10);");
        page.Tick();

        Assert.True(page.Is("rootOf(frame).classList.contains('slmr-many')"));
        Assert.True(page.Is("rootOf(frame).classList.contains('slmr-compact')"));
        Assert.Equal("5", page.Text("rootOf(frame).style.getPropertyValue('--slmr-columns')"));
        Assert.Equal(1, page.Number("styleCount(frame)"));
    }

    [Fact]
    public void One_row_of_cards_keeps_the_game_card_size()
    {
        var page = new PageScript();

        page.Run("var frame = addRewardsFrame(); showCards(frame, 4);");
        page.Tick();

        Assert.True(page.Is("rootOf(frame).classList.contains('slmr-many')"));
        Assert.False(page.Is("rootOf(frame).classList.contains('slmr-compact')"));
        Assert.Equal("4", page.Text("rootOf(frame).style.getPropertyValue('--slmr-columns')"));
    }

    [Theory]
    [InlineData(4, 1, 4)]
    [InlineData(5, 1, 5)]
    [InlineData(6, 2, 3)]
    [InlineData(7, 2, 4)]
    [InlineData(8, 2, 4)]
    [InlineData(9, 2, 5)]
    [InlineData(10, 2, 5)]
    [InlineData(11, 3, 4)]
    [InlineData(13, 3, 5)]
    [InlineData(16, 4, 4)]
    [InlineData(20, 4, 5)]
    public void Cards_fill_even_rows_of_at_most_five(int cards, int rows, int columns)
    {
        var page = new PageScript();

        Assert.Equal(rows, page.Number($"window.__slmorerewards.layout({cards}).rows"));
        Assert.Equal(columns, page.Number($"window.__slmorerewards.layout({cards}).columns"));
    }

    [Fact]
    public void Cards_never_need_more_columns_than_the_widest_row_holds()
    {
        var page = new PageScript();

        for (var cards = 4; cards <= 20; cards++)
        {
            var rows = page.Number($"window.__slmorerewards.layout({cards}).rows");
            var columns = page.Number($"window.__slmorerewards.layout({cards}).columns");

            Assert.True(columns <= 5, $"{cards} cards got {columns} columns");
            Assert.True(rows * columns >= cards, $"{cards} cards do not fit {rows} rows of {columns}");
            Assert.True((rows - 1) * columns < cards, $"{cards} cards leave a whole row of {rows} empty");
        }
    }

    [Fact]
    public void The_style_is_added_once_however_often_the_page_is_checked()
    {
        var page = new PageScript();

        page.Run("var frame = addRewardsFrame(); showCards(frame, 10);");
        page.Tick();
        page.Tick();
        page.Tick();

        Assert.Equal(1, page.Number("styleCount(frame)"));
    }

    [Fact]
    public void A_refresh_down_to_three_cards_gives_the_game_its_own_layout_back()
    {
        var page = new PageScript();

        page.Run("var frame = addRewardsFrame(); showCards(frame, 10);");
        page.Tick();
        page.Run("showCards(frame, 3);");
        page.Tick();

        Assert.False(page.Is("rootOf(frame).classList.contains('slmr-many')"));
        Assert.False(page.Is("rootOf(frame).classList.contains('slmr-compact')"));
    }

    [Fact]
    public void A_reloaded_rewards_page_gets_the_layout_again()
    {
        var page = new PageScript();

        page.Run("var first = addRewardsFrame(); showCards(first, 10);");
        page.Tick();
        page.Run("first.src = 'about:blank'; var second = addRewardsFrame(); showCards(second, 8);");
        page.Tick();

        Assert.True(page.Is("rootOf(second).classList.contains('slmr-compact')"));
        Assert.Equal("4", page.Text("rootOf(second).style.getPropertyValue('--slmr-columns')"));
        Assert.Equal(1, page.Number("styleCount(second)"));
    }

    [Fact]
    public void Other_game_pages_with_cards_are_left_alone()
    {
        var page = new PageScript();

        page.Run("var other = addFrame('file:///C:/Game/WebUI/UI/CoreUI1/CoreUI1.html'); showCards(other, 10);");
        page.Tick();

        Assert.False(page.Is("rootOf(other).classList.contains('slmr-many')"));
        Assert.Equal(0, page.Number("styleCount(other)"));
    }

    [Fact]
    public void A_rewards_page_the_script_cannot_reach_is_reported_once()
    {
        var page = new PageScript();

        page.Run("addBlockedRewardsFrame();");
        page.Tick();
        page.Tick();

        Assert.Contains("cannot reach the rewards page document", page.Take());

        page.Tick();

        Assert.Equal("ok", page.Take());
    }

    [Fact]
    public void The_plugin_hears_how_the_cards_were_laid_out()
    {
        var page = new PageScript();

        page.Run("var frame = addRewardsFrame(); showCards(frame, 10);");
        page.Tick();

        Assert.Equal("showing 10 rewards in 2 rows of up to 5", page.Take());
        Assert.Equal("ok", page.Take());
    }

    [Fact]
    public void Sending_the_script_again_keeps_the_one_already_running()
    {
        var page = new PageScript();

        page.Run("window.__slmorerewards.marker = 'first';");
        page.Install();

        Assert.Equal("first", page.Text("window.__slmorerewards.marker"));
        Assert.Equal(1, page.Number("timers.length"));
    }
}
