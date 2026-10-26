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

/// <summary>A ban as the ban list shows it: the rewards it keeps away, by name, and what removing it pays back.</summary>
public sealed record BanRow(int Head, IReadOnlyList<string> Names, int Refund);

public enum SettingKind
{
    Bool,
    Int,
    Choice,
}

/// <summary>A setting as the rewards screen's settings show it.</summary>
/// <param name="Key">"Section.Key", what the page sends back to change it.</param>
/// <param name="Value">The value now, as the config file writes it.</param>
/// <param name="Options">The names a <see cref="SettingKind.Choice"/> can take.</param>
/// <param name="Desc">The description in the game's language.</param>
/// <param name="IsRestart">A change waits for the next game start.</param>
/// <param name="IsShown">False while the setting it depends on is off.</param>
public sealed record SettingRow(string Key, string Section, string Name, SettingKind Kind, string Value, int Min, int Max, IReadOnlyList<string> Options, string Desc, bool IsRestart, bool IsShown);

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
        ShopOf(rows).Call(points, leftToday, rise, language);

    public static string BansCall(IReadOnlyList<BanRow> rows, int refundPercent, Language language) =>
        "window.__slmorerewards && window.__slmorerewards.bans(" + Bans(rows, refundPercent, language) + ")";

    public static string SettingsCall(IReadOnlyList<SettingRow> rows, Language language) =>
        "window.__slmorerewards && window.__slmorerewards.settings(" + Settings(rows, language) + ")";

    public static string ToastCall(string text) => "window.__slmorerewards && window.__slmorerewards.toast(" + Text(text) + ")";

    public static string Setup(PageSetup setup)
    {
        var s = setup.Settings;
        var json = new StringBuilder("{");

        json.Append("\"lang\":").Append(Text(LanguageCode(setup.Language)));
        json.Append(",\"maxLocked\":").Append(s.MaxLocked);
        json.Append(",\"gamePin\":").Append(setup.GamePin);
        json.Append(",\"extras\":[").Append(string.Join(",", setup.Extras.Select(held => held.Id))).Append(']');
        json.Append(",\"ban\":").Append(Flag(s.BanButton));
        json.Append(",\"banCost\":").Append(s.BanCost);
        json.Append(",\"labels\":").Append(Flag(s.Labels));
        json.Append(",\"cashOut\":").Append(s.CashOut);
        json.Append(",\"shop\":").Append(Flag(s.ShopEnabled));
        json.Append(",\"shopIncrease\":").Append(s.ShopIncrease);
        json.Append(",\"shopReset\":").Append(Text(s.ShopReset.ToString().ToLowerInvariant()));
        json.Append(",\"rate\":").Append(setup.InterestRate.ToString("0.####", CultureInfo.InvariantCulture));
        json.Append(",\"cap\":").Append(setup.InterestCap);
        json.Append(",\"life\":{").Append(string.Join(",", setup.Lives.Where(entry => entry.Value != RewardLife.Permanent).Select(entry => "\"" + entry.Key + "\":" + entry.Value.Seconds))).Append('}');

        return json.Append('}').ToString();
    }

    /// <param name="leftToday">Shop buys left today, null for no limit.</param>
    /// <param name="rise">How far the shop's prices are above the card prices now, in percent.</param>
    public static string Shop(IReadOnlyList<ShopRow> rows, int points, int? leftToday, int rise, Language language) =>
        ShopOf(rows).Body(points, leftToday, rise, language);

    private static ShopJson ShopOf(IReadOnlyList<ShopRow> rows)
    {
        var json = new ShopJson();

        foreach (var row in rows) json.Add(row);

        return json;
    }

    /// <param name="refundPercent">The share of a ban's cost that removing it pays back, in percent.</param>
    public static string Bans(IReadOnlyList<BanRow> rows, int refundPercent, Language language) =>
        "{\"lang\":" + Text(LanguageCode(language)) +
        ",\"refund\":" + refundPercent +
        ",\"rows\":[" + string.Join(",", rows.Select(row => "{\"id\":" + row.Head + ",\"names\":[" + string.Join(",", row.Names.Select(Text)) + "],\"refund\":" + row.Refund + "}")) + "]}";

    public static string Settings(IReadOnlyList<SettingRow> rows, Language language) =>
        "{\"lang\":" + Text(LanguageCode(language)) + ",\"rows\":[" + string.Join(",", rows.Select(Setting)) + "]}";

    /// <summary>A JSON string; the separators U+2028 and U+2029 are escaped too, as older JavaScript reads them as line ends.</summary>
    public static string Text(string? text) => AppendText(new StringBuilder(), text).ToString();

    /// <summary><see cref="Text"/> written straight onto <paramref name="json"/>.</summary>
    internal static StringBuilder AppendText(StringBuilder json, string? text)
    {
        json.Append('"');

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

        return json.Append('"');
    }

    internal static void AppendRow(StringBuilder json, ShopRow row)
    {
        json.Append("{\"id\":").Append(row.Id.ToString(CultureInfo.InvariantCulture));
        AppendText(json.Append(",\"name\":"), row.Name);
        AppendText(json.Append(",\"desc\":"), row.Desc);
        AppendText(json.Append(",\"icon\":"), row.Icon);
        json.Append(",\"cost\":").Append(row.Cost.ToString(CultureInfo.InvariantCulture));
        json.Append(",\"price\":").Append(row.Price.ToString(CultureInfo.InvariantCulture));

        if (row.Why == null) json.Append(",\"why\":null");
        else AppendText(json.Append(",\"why\":"), row.Why);

        json.Append(",\"life\":").Append(row.Life.Seconds.ToString(CultureInfo.InvariantCulture));
        AppendText(json.Append(",\"cat\":"), row.Category);
        json.Append(",\"early\":").Append(Flag(row.IsEarly)).Append('}');
    }

    private static string Setting(SettingRow row) =>
        "{\"key\":" + Text(row.Key) +
        ",\"section\":" + Text(row.Section) +
        ",\"name\":" + Text(row.Name) +
        ",\"kind\":" + Text(row.Kind.ToString().ToLowerInvariant()) +
        ",\"value\":" + Text(row.Value) +
        ",\"min\":" + row.Min +
        ",\"max\":" + row.Max +
        ",\"options\":[" + string.Join(",", row.Options.Select(Text)) + "]" +
        ",\"desc\":" + Text(row.Desc) +
        ",\"restart\":" + Flag(row.IsRestart) +
        ",\"shown\":" + Flag(row.IsShown) + "}";

    private static string Flag(bool value) => value ? "true" : "false";

    internal static string LanguageCode(Language language) => language == Language.Chinese ? "zh" : "en";
}
