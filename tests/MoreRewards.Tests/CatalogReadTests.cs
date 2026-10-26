using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

/// <summary>
/// The rewards table now reads only the default script's fields that can change a reward's category, and the mod's
/// code runs once on a worker at load. These guard that the category comes out the same and the warm-up is safe.
/// </summary>
public class CatalogReadTests
{
    [Theory]
    [InlineData("BuffHealthMax")]
    [InlineData("BuffSatietyMax,MoveSpeed")]
    [InlineData("MoveSpeed,BuffHealthMax")]
    [InlineData("BuffHealthMax,WorkSpeedRatio,BuffWaterMax")]
    [InlineData("BuffWaterMax,ItemSatietyStart")]
    [InlineData("BuffHealthMax,BuffWaterMax")]
    public void Reading_only_the_telling_fields_gives_the_same_category_as_reading_them_all(string fields)
    {
        var setFields = fields.Split(',');
        var telling = setFields.Where(RewardKind.IsTellingField).ToList();

        Assert.Equal(RewardKind.Category(RewardKind.DefaultScript, setFields), RewardKind.Category(RewardKind.DefaultScript, telling));
    }

    [Theory]
    [InlineData("BuffSatietyMax", true)]
    [InlineData("MoveSpeed", true)]
    [InlineData("BuffHealthMax", false)]
    [InlineData("BuffWaterMax", false)]
    public void Only_satiety_and_speed_fields_are_telling(string field, bool isTelling)
    {
        Assert.Equal(isTelling, RewardKind.IsTellingField(field));
    }

    [Fact]
    public void The_warm_up_runs_through_without_failing()
    {
        var failure = Record.Exception(() => WarmUp.Run());

        Assert.Null(failure);
    }
}
