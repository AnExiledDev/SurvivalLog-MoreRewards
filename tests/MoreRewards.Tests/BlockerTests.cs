using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class BlockerTests
{
    private static readonly string[] Nobody = Array.Empty<string>();

    private static RewardFacts Facts(bool isAvailable = true, bool isBanned = false, bool isOwned = false, bool isReget = false, bool isModeBlocked = false,
        string[]? onlyFor = null, string[]? missing = null, int fromDay = 0, bool isValid = true) =>
        new(isAvailable, isBanned, isOwned, isReget, isModeBlocked, onlyFor ?? Nobody, missing ?? Nobody, fromDay, isValid);

    [Fact]
    public void A_reward_the_game_would_offer_has_no_reason()
    {
        Assert.Null(Blocker.Why(Facts(), 5, Language.English));
    }

    [Fact]
    public void Each_reason_reads_in_plain_words()
    {
        Assert.Equal("Switched off in the game", Blocker.Why(Facts(isAvailable: false), 5, Language.English));
        Assert.Equal("Banned this run", Blocker.Why(Facts(isBanned: true), 5, Language.English));
        Assert.Equal("You own it", Blocker.Why(Facts(isOwned: true), 5, Language.English));
        Assert.Equal("Not in this game mode", Blocker.Why(Facts(isModeBlocked: true), 5, Language.English));
        Assert.Equal("For Ann, Bo only", Blocker.Why(Facts(onlyFor: new[] { "Ann", "Bo" }), 5, Language.English));
        Assert.Equal("From day 12", Blocker.Why(Facts(fromDay: 12), 5, Language.English));
        Assert.Equal("Needs Cellar Craft I first", Blocker.Why(Facts(missing: new[] { "Cellar Craft I" }), 5, Language.English));
        Assert.Equal("Not available yet", Blocker.Why(Facts(isValid: false), 5, Language.English));
    }

    [Fact]
    public void A_reward_that_can_be_taken_again_is_not_blocked_by_owning_it()
    {
        Assert.Null(Blocker.Why(Facts(isOwned: true, isReget: true), 5, Language.English));
    }

    [Fact]
    public void The_day_it_unlocks_is_no_longer_a_reason()
    {
        Assert.Null(Blocker.Why(Facts(fromDay: 12), 12, Language.English));
    }

    [Fact]
    public void Early_buys_let_the_shop_sell_a_reward_whose_needs_are_not_met()
    {
        var facts = Facts(missing: new[] { "Cellar Craft I" }, isValid: false);

        Assert.Null(Blocker.Why(facts, 5, Language.English, allowsEarly: true));
        Assert.NotNull(Blocker.Why(facts, 5, Language.English, allowsEarly: false));
    }

    [Fact]
    public void Early_buys_never_get_round_the_day_or_the_character()
    {
        Assert.NotNull(Blocker.Why(Facts(missing: new[] { "X" }, fromDay: 12, isValid: false), 5, Language.English, allowsEarly: true));
        Assert.NotNull(Blocker.Why(Facts(missing: new[] { "X" }, onlyFor: new[] { "Ann" }, isValid: false), 5, Language.English, allowsEarly: true));
    }

    [Fact]
    public void Reasons_follow_the_game_language()
    {
        Assert.Equal("已拥有", Blocker.Why(Facts(isOwned: true), 5, Language.Chinese));
        Assert.Equal("第 12 天起", Blocker.Why(Facts(fromDay: 12), 5, Language.Chinese));
    }
}
