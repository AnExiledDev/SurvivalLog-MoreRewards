using System.Globalization;
using System.Text;

namespace MoreRewards.Core;

/// <summary>
/// The shop's call to the page, written a row at a time so a long list can be built across frames. The points, the
/// buys left and the price rise go in last, as they are when the call is sent.
/// </summary>
public sealed class ShopJson
{
    private readonly StringBuilder _rows = new();

    public int Count { get; private set; }

    public void Add(ShopRow row)
    {
        if (Count > 0) _rows.Append(',');

        PageData.AppendRow(_rows, row);
        Count++;
    }

    /// <param name="leftToday">Shop buys left today, null for no limit.</param>
    /// <param name="rise">How far the shop's prices are above the card prices now, in percent.</param>
    public string Body(int points, int? leftToday, int rise, Language language)
    {
        var json = new StringBuilder(_rows.Length + 96).Append("{\"lang\":").Append(PageData.Text(PageData.LanguageCode(language)));

        json.Append(",\"points\":").Append(points);
        json.Append(",\"rise\":").Append(rise);
        json.Append(",\"left\":").Append(leftToday?.ToString(CultureInfo.InvariantCulture) ?? "null");
        json.Append(",\"rows\":[").Append(_rows).Append("]}");

        return json.ToString();
    }

    public string Call(int points, int? leftToday, int rise, Language language) =>
        "window.__slmorerewards && window.__slmorerewards.shop(" + Body(points, leftToday, rise, language) + ")";
}
