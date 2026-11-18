using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

// The Toolkit's mailbox hands the tab each page message whole, prefix included. In 0.9.0 to 0.10.1 the tab compared
// the whole message to bare verbs, so the tab script was never sent and the page asked for it again every few seconds.
public class RewardsPageVerbTests
{
    [Theory]
    [InlineData("slmod|morerewards|rewards|noscript", RewardsPage.NoScript)]
    [InlineData("slmod|morerewards|rewards|load", RewardsPage.Load)]
    public void Strips_the_prefix_the_page_sends(string message, string verb) =>
        Assert.Equal(verb, RewardsPage.Verb(message));

    [Fact]
    public void Keeps_a_save_body_whole()
    {
        var body = "{\"rewards\":[{\"name\":\"slmod|morerewards|rewards|x\"}]}";

        var verb = RewardsPage.Verb(RewardsPage.Prefix + RewardsPage.Save + body);

        Assert.Equal(RewardsPage.Save + body, verb);
    }

    [Fact]
    public void Leaves_a_message_with_no_prefix_alone() => Assert.Equal("load", RewardsPage.Verb("load"));
}
