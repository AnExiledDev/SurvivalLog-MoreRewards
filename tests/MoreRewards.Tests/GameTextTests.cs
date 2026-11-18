using System.Collections.Generic;
using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

/// <summary>
/// The game loads its English and Chinese text after its tables. Rewards read before both are there show their text
/// keys (Talent_Name_31641) in the Rewards tab instead of their names, so nothing is read until both have loaded.
/// </summary>
public class GameTextTests
{
    private static readonly Dictionary<string, string> Map = new() { ["Talent_Name_31641"] = "Solar Collection" };

    [Fact]
    public void The_rewards_wait_until_both_languages_have_loaded()
    {
        Assert.False(GameText.IsLoaded(null, null));
        Assert.False(GameText.IsLoaded(Map, null));
        Assert.False(GameText.IsLoaded(null, Map));
        Assert.True(GameText.IsLoaded(Map, Map));
    }
}
