using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class PageReplyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ok")]
    public void A_page_with_nothing_to_say_asks_for_nothing(string? reply)
    {
        var turn = PageReply.Read(reply);

        Assert.False(turn.NeedsScript);
        Assert.Empty(turn.Notes);
    }

    [Fact]
    public void A_page_that_lost_the_script_asks_for_it_again()
    {
        var turn = PageReply.Read("missing");

        Assert.True(turn.NeedsScript);
        Assert.Empty(turn.Notes);
    }

    [Fact]
    public void Each_line_the_page_sends_is_its_own_note()
    {
        var turn = PageReply.Read("showing 10 rewards in 2 rows of up to 5\n\n  cannot reach the rewards page document  \n");

        Assert.False(turn.NeedsScript);
        Assert.Equal(new[] { "showing 10 rewards in 2 rows of up to 5", "cannot reach the rewards page document" }, turn.Notes);
        Assert.False(turn.NeedsSetup);
    }

    [Fact]
    public void A_rewards_screen_without_its_setup_asks_for_it_and_logs_nothing_for_it()
    {
        var turn = PageReply.Read("showing 10 rewards in 2 rows of up to 5\nneeds setup");

        Assert.True(turn.NeedsSetup);
        Assert.False(turn.NeedsScript);
        Assert.Equal(new[] { "showing 10 rewards in 2 rows of up to 5" }, turn.Notes);
    }
}
