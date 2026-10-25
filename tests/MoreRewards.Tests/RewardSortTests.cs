using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class RewardSortTests
{
    private static readonly string[] NoFields = Array.Empty<string>();

    [Fact]
    public void Banning_any_level_of_a_line_reaches_every_level_of_it()
    {
        var needs = new Dictionary<int, IReadOnlyList<int>>
        {
            [2] = new[] { 1 },
            [3] = new[] { 2 },
            [9] = new[] { 8 },
        };

        Assert.Equal(new[] { 1, 2, 3 }, RewardLine.Of(2, needs).OrderBy(id => id));
        Assert.Equal(new[] { 1, 2, 3 }, RewardLine.Of(3, needs).OrderBy(id => id));
    }

    [Fact]
    public void A_reward_with_no_links_is_its_own_line()
    {
        Assert.Equal(new[] { 7 }, RewardLine.Of(7, new Dictionary<int, IReadOnlyList<int>> { [2] = new[] { 1 } }));
    }

    [Fact]
    public void The_category_comes_from_the_first_pattern_the_script_matches()
    {
        Assert.Equal("recipes", RewardKind.Category("Buff/Buff_UnlockCraftRecipe_Trap", NoFields));
        Assert.Equal("traps", RewardKind.Category("Buff/Buff_Trap_Damage", NoFields));
        Assert.Equal("body", RewardKind.Category("Buff/Buff_Something_Else", NoFields));
    }

    [Fact]
    public void The_plain_buff_script_is_sorted_by_what_it_changes()
    {
        Assert.Equal("food", RewardKind.Category(RewardKind.DefaultScript, new[] { "BuffSatietyMax" }));
        Assert.Equal("speed", RewardKind.Category(RewardKind.DefaultScript, new[] { "MoveSpeed" }));
        Assert.Equal("body", RewardKind.Category(RewardKind.DefaultScript, new[] { "BuffHealthMax" }));
    }

    [Fact]
    public void How_long_a_reward_lasts_comes_from_its_buff_duration()
    {
        Assert.Equal(RewardLife.Permanent, RewardKind.Life("body", -1f));
        Assert.Equal(RewardLife.Permanent, RewardKind.Life("traps", null));
        Assert.Equal(RewardLife.OneTime, RewardKind.Life("body", RewardKind.Instant));
        Assert.Equal(RewardLife.OneTime, RewardKind.Life("body", 0f));
        Assert.Equal(RewardLife.Timed(259200), RewardKind.Life("trade", 259200f));
        Assert.Equal(21600, RewardKind.Life("body", 21600f).Seconds);
        Assert.True(RewardKind.Life("body", 86400f).IsTimed);
        Assert.False(RewardLife.Permanent.IsTimed || RewardLife.OneTime.IsTimed);
    }

    [Fact]
    public void Airdrops_are_one_time_whatever_their_buff_says()
    {
        Assert.Equal(RewardLife.OneTime, RewardKind.Life("airdrops", -1f));
        Assert.Equal(RewardLife.OneTime, RewardKind.Life("airdrops", null));
    }
}
