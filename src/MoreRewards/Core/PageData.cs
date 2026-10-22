using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MoreRewards.Core;

/// <summary>A reward as the shop lists it.</summary>
/// <param name="Cost">The card's own price, any discount taken off.</param>
/// <param name="Price">What the shop asks.</param>
/// <param name="Why">Why it cannot be bought now; null when it can.</param>
/// <param name="IsEarly">Its needs are not met yet, so the price is doubled.</param>
public sealed record ShopRow(int Id, string Name, string Desc, string Icon, int Cost, int Price, string? Why, RewardLife Life, string Category, bool IsEarly);

/// <summary>What the page is told when the rewards screen opens, so it can draw the mod's buttons.</summary>
/// <param name="InterestRate">The fraction of the points held that is paid as interest when the day ends.</param>
/// <param name="InterestCap">The most interest a day can pay.</param>
public sealed record PageSetup(Language Language, ScreenSettings Settings, int GamePin, IReadOnlyList<RewardLock> Extras, IReadOnlyDictionary<int, RewardLife> Lives, float InterestRate, int InterestCap);

/// <summary>
/// Writes what the plugin hands the page as a JavaScript call, the data as JSON (which is also JavaScript), so no
/// game text can break out of the string it is in.
/// </summary>
public static class PageData
{
    public static string SetupCall(PageSetup setup) => "window.__slmorerewards && window.__slmorerewards.setup(" + Setup(setup) + ")";

    public static string ShopCall(IReadOnlyList<ShopRow> rows, int points, int? leftToday, int rise, Language language) =>
        "window.__slmorerewards && window.__slmorerewards.shop(" + Shop(rows, points, leftToday, rise, language) + ")";

    public static string ToastCall(string text) => "window.__slmorerewards && window.__slmorerewards.toast(" + Text(text) + ")";

    public static string Setup(PageSetup setup)
    {
        var s = setup.Settings;
        var json = new StringBuilder("{");

        json.Append("\"lang\":").Append(Text(LanguageCode(setup.Language)));
        json.Append(",\"maxLocked\":").Append(s.MaxLocked);
        json.Append(",\"gamePin\":").Append(setup.GamePin);
        json.Append(",\"extras\":[").Append(string.Join(",", setup.Extras.Select(held => held.Id))).Append(']');
        json.Append(",\"notInterested\":").Append(Flag(s.NotInterested));
        json.Append(",\"ban\":").Append(Flag(s.BanButton));
        json.Append(",\"banCost\":").Append(s.BanCost);
        json.Append(",\"labels\":").Append(Flag(s.Labels));
        json.Append(",\"cashOut\":").Append(s.CashOut);
        json.Append(",\"shop\":").Append(Flag(s.ShopEnabled));
        json.Append(",\"shopIncrease\":").Append(s.ShopIncrease);
        json.Append(",\"shopReset\":").Append(Text(s.ShopReset.ToString().ToLowerInvariant()));
        json.Append(",\"rate\":").Append(setup.InterestRate.ToString("0.####", CultureInfo.InvariantCulture));
        json.Append(",\"cap\":").Append(setup.InterestCap);
        json.Append(",\"life\":{").Append(string.Join(",", setup.Lives.Where(entry => entry.Value == RewardLife.OneTime).Select(entry => "\"" + entry.Key + "\":1"))).Append('}');

        return json.Append('}').ToString();
    }

    /// <param name="leftToday">Shop buys left today, null for no limit.</param>
    /// <param name="rise">How far the shop's prices are above the card prices now, in percent.</param>
    public static string Shop(IReadOnlyList<ShopRow> rows, int points, int? leftToday, int rise, Language language)
    {
        var json = new StringBuilder("{\"lang\":").Append(Text(LanguageCode(language)));

        json.Append(",\"points\":").Append(points);
        json.Append(",\"rise\":").Append(rise);
        json.Append(",\"left\":").Append(leftToday?.ToString(CultureInfo.InvariantCulture) ?? "null");
        json.Append(",\"rows\":[").Append(string.Join(",", rows.Select(Row))).Append("]}");

        return json.ToString();
    }

    /// <summary>A JSON string; the separators U+2028 and U+2029 are escaped too, as older JavaScript reads them as line ends.</summary>
    public static string Text(string? text)
    {
        var json = new StringBuilder("\"");

        foreach (var c in text ?? "")
        {
            switch (c)
            {
                case '"': json.Append("\\\""); break;
                case '\\': json.Append("\\\\"); break;
                case '\n': json.Append("\\n"); break;
                case '\r': json.Append("\\r"); break;
                case '\t': json.Append("\\t"); break;
                case '<': json.Append("\\u003c"); break;
                default:
                    if (c < ' ' || c == '\u2028' || c == '\u2029') json.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else json.Append(c);

                    break;
            }
        }

        return json.Append('"').ToString();
    }

    private static string Row(ShopRow row) =>
        "{\"id\":" + row.Id +
        ",\"name\":" + Text(row.Name) +
        ",\"desc\":" + Text(row.Desc) +
        ",\"icon\":" + Text(row.Icon) +
        ",\"cost\":" + row.Cost +
        ",\"price\":" + row.Price +
        ",\"why\":" + (row.Why == null ? "null" : Text(row.Why)) +
        ",\"once\":" + Flag(row.Life == RewardLife.OneTime) +
        ",\"cat\":" + Text(row.Category) +
        ",\"early\":" + Flag(row.IsEarly) + "}";

    private static string Flag(bool value) => value ? "true" : "false";

    private static string LanguageCode(Language language) => language == Language.Chinese ? "zh" : "en";
}
