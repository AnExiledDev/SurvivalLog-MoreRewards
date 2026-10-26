using Xunit;

namespace MoreRewards.Tests;

/// <summary>
/// The page script checks the rewards page four times a second. Once it has found the page and its parts it keeps them
/// and searches nothing, and it lets go of them when the game removes or reloads the page.
/// </summary>
public class PageCheckCostTests
{
    private const string Cards = "[{id: 11, state: 'affordable', pinned: true}, {id: 12, state: 'affordable', pinned: false}, {id: 13, state: 'bought', pinned: false}, {id: 14, state: 'locked', pinned: false}, {id: 15, state: 'affordable', pinned: false}]";

    [Fact]
    public void Once_the_rewards_page_is_drawn_a_check_searches_nothing()
    {
        var page = new PageScript();

        page.Run($"var frame = addRewardsFrame(); showRewards(frame, {Cards}, 300); setupWith({{maxLocked: 3, cashOut: 5}});");
        page.Tick();
        page.Tick();

        var before = page.Number("searches");

        page.Tick();
        page.Tick();

        Assert.Equal(before, page.Number("searches"));
        Assert.True(page.Is("rootOf(frame).classList.contains('slmr-many')"));
        Assert.Equal("\U0001F512 1/3", page.Text("frame.contentDocument.querySelector('.pin-count').textContent"));
    }

    [Fact]
    public void With_one_lock_allowed_a_check_searches_nothing_either()
    {
        var page = new PageScript();

        page.Run($"var frame = addRewardsFrame(); showRewards(frame, {Cards}, 300); setupWith();");
        page.Tick();
        page.Tick();

        var before = page.Number("searches");

        page.Tick();

        Assert.Equal(before, page.Number("searches"));
        Assert.False(page.Is("frame.contentDocument.querySelector('.pin-count')"));
    }

    [Fact]
    public void A_lock_made_on_the_page_still_shows_in_the_count_and_the_tips()
    {
        var page = new PageScript();

        page.Run($"var frame = addRewardsFrame(); showRewards(frame, {Cards}, 300); setupWith({{maxLocked: 3}});");
        page.Tick();

        Assert.EndsWith("(1/3)", page.Text("slotOf(frame, 4).querySelector('.pin-tip').textContent"));

        page.Run("stateOf(frame).cards[1].pinned = true;");
        page.Tick();

        Assert.Equal("\U0001F512 2/3", page.Text("frame.contentDocument.querySelector('.pin-count').textContent"));
        Assert.EndsWith("(2/3)", page.Text("slotOf(frame, 4).querySelector('.pin-tip').textContent"));
    }

    [Fact]
    public void A_frame_the_game_removed_is_let_go()
    {
        var page = new PageScript();

        page.Run("var frame = addRewardsFrame(); showCards(frame, 3);");
        page.Tick();

        Assert.Equal("needs setup", page.Take());

        page.Run("removeFrame(frame);");
        page.Tick();

        Assert.Equal("ok", page.Take());
    }

    [Fact]
    public void A_new_rewards_frame_after_the_old_one_was_removed_gets_the_layout()
    {
        var page = new PageScript();

        page.Run("var first = addRewardsFrame(); showCards(first, 10);");
        page.Tick();
        page.Run("removeFrame(first); var second = addRewardsFrame(); showCards(second, 8);");
        page.Tick();

        Assert.True(page.Is("rootOf(second).classList.contains('slmr-compact')"));
        Assert.Equal("4", page.Text("rootOf(second).style.getPropertyValue('--slmr-columns')"));
        Assert.Equal(1, page.Number("styleCount(second)"));
    }

    [Fact]
    public void A_page_reloaded_in_the_same_frame_gets_the_layout_again()
    {
        var page = new PageScript();

        page.Run("var frame = addRewardsFrame(); showCards(frame, 10);");
        page.Tick();
        page.Run("reloadFrame(frame); showCards(frame, 8);");
        page.Tick();

        Assert.True(page.Is("rootOf(frame).classList.contains('slmr-compact')"));
        Assert.Equal("4", page.Text("rootOf(frame).style.getPropertyValue('--slmr-columns')"));
        Assert.Equal(1, page.Number("styleCount(frame)"));
    }

    [Fact]
    public void A_card_grid_the_page_drew_again_is_found_again()
    {
        var page = new PageScript();

        page.Run("var frame = addRewardsFrame(); showCards(frame, 10);");
        page.Tick();
        page.Run(
            "var old = frame.contentDocument.querySelector('.card-grid'); var grid = new FakeElement('div', 'card-grid');" +
            "old.parentElement.insertBefore(grid, old); old.remove();" +
            "for (var i = 0; i < 8; i++) grid.appendChild(new FakeElement('div', 'card-slot')).appendChild(new FakeElement('div', 'card affordable'));");
        page.Tick();

        Assert.Equal("4", page.Text("rootOf(frame).style.getPropertyValue('--slmr-columns')"));
    }

    [Fact]
    public void The_column_count_is_written_only_when_it_changes()
    {
        var page = new PageScript();

        page.Run(
            "var frame = addRewardsFrame(); showCards(frame, 10); var writes = 0; var style = rootOf(frame).style; var set = style.setProperty;" +
            "style.setProperty = function (name, value) { writes++; set(name, value); };");
        page.Tick();
        page.Tick();
        page.Tick();

        Assert.Equal(1, page.Number("writes"));

        page.Run("showCards(frame, 8);");
        page.Tick();

        Assert.Equal(2, page.Number("writes"));
        Assert.Equal("4", page.Text("rootOf(frame).style.getPropertyValue('--slmr-columns')"));
    }
}
