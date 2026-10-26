using System.Globalization;
using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

/// <summary>
/// The shop's list is now written a row at a time across frames. The page must get exactly the call it got when the
/// whole list was written in one go.
/// </summary>
public class ShopJsonTests
{
    private static readonly ShopRow[] Rows =
    {
        new(30001, "Say \"hi\" </b>", "line one\nline two \u2028", "Icons/a.png", 6, 9, null, RewardLife.Permanent, "body", false),
        new(30002, "铁锅", "做饭更快", "", 12, 18, "第 3 天后可用", RewardLife.OneTime, "cooking", true),
        new(30003, "Trap", "tab\there", "x", 0, 0, "Needs Axe", RewardLife.Timed(7200), "traps", false),
    };

    [Theory]
    [InlineData(0, null, 0, Language.English)]
    [InlineData(1, 2, 15, Language.Chinese)]
    [InlineData(3, 0, 40, Language.English)]
    [InlineData(3, null, 25, Language.Chinese)]
    public void The_call_built_row_by_row_matches_the_one_built_in_one_go(int rowCount, int? leftToday, int rise, Language language)
    {
        var rows = Rows.Take(rowCount).ToList();
        var json = new ShopJson();

        foreach (var row in rows) json.Add(row);

        Assert.Equal(rowCount, json.Count);
        Assert.Equal(OldShop(rows, 120, leftToday, rise, language), json.Body(120, leftToday, rise, language));
        Assert.Equal("window.__slmorerewards && window.__slmorerewards.shop(" + OldShop(rows, 120, leftToday, rise, language) + ")", json.Call(120, leftToday, rise, language));
        Assert.Equal(json.Call(120, leftToday, rise, language), PageData.ShopCall(rows, 120, leftToday, rise, language));
    }

    [Fact]
    public void The_points_are_the_ones_held_when_the_call_is_sent()
    {
        var json = new ShopJson();

        json.Add(Rows[0]);

        Assert.Contains("\"points\":5,", json.Body(5, null, 0, Language.English));
        Assert.Contains("\"points\":80,", json.Body(80, null, 0, Language.English));
    }

    /// <summary>The shop's JSON as it was written before, in one go; kept here as the shape the page reads.</summary>
    private static string OldShop(IReadOnlyList<ShopRow> rows, int points, int? leftToday, int rise, Language language) =>
        "{\"lang\":" + PageData.Text(language == Language.Chinese ? "zh" : "en") +
        ",\"points\":" + points +
        ",\"rise\":" + rise +
        ",\"left\":" + (leftToday?.ToString(CultureInfo.InvariantCulture) ?? "null") +
        ",\"rows\":[" + string.Join(",", rows.Select(OldRow)) + "]}";

    private static string OldRow(ShopRow row) =>
        "{\"id\":" + row.Id +
        ",\"name\":" + PageData.Text(row.Name) +
        ",\"desc\":" + PageData.Text(row.Desc) +
        ",\"icon\":" + PageData.Text(row.Icon) +
        ",\"cost\":" + row.Cost +
        ",\"price\":" + row.Price +
        ",\"why\":" + (row.Why == null ? "null" : PageData.Text(row.Why)) +
        ",\"life\":" + row.Life.Seconds +
        ",\"cat\":" + PageData.Text(row.Category) +
        ",\"early\":" + (row.IsEarly ? "true" : "false") + "}";
}
