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
    public void Airdrops_daily_ticks_and_one_off_scripts_are_one_time()
    {
        Assert.Equal(RewardLife.OneTime, RewardKind.Life("airdrops", "Buff/Buff_AddItemToBag", 0f));
        Assert.Equal(RewardLife.OneTime, RewardKind.Life("body", "Buff/Buff_X", RewardKind.OneDay));
        Assert.Equal(RewardLife.OneTime, RewardKind.Life("trade", "Buff/Buff_Annual_Bonus", 0f));
        Assert.Equal(RewardLife.Permanent, RewardKind.Life("traps", "Buff/Buff_Trap_Damage", 0f));
    }
}
