using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ExiledToolkit.Web;

namespace MoreRewards.Core;

/// <summary>One level of a game reward as the Rewards tab shows it. <paramref name="Values"/> are the fields of the effect it runs.</summary>
public sealed record GameRewardLevel(int Id, int Cost, LocalText Name, LocalText Description, string Script, IReadOnlyList<FieldSet> Values, IReadOnlyList<PoolEntry> Pools);

/// <summary>
/// The rules on a game reward's first level: whether it can be bought again and how far it stacks, which runs (0 any,
/// 6 endless only) and characters get it, the first day it can be offered, and the other needs of its condition set,
/// which the tab shows but does not change.
/// </summary>
public sealed record GameRules(bool IsReget, int Endless, IReadOnlyList<int> Characters, int Layers, int FromDay, IReadOnlyList<string> AlsoNeeds)
{
    public static readonly GameRules None = new(false, 0, Array.Empty<int>(), 0, 0, Array.Empty<string>());
}

/// <summary>A game reward and the levels above it, as the game made them, before the rewards file changed anything.</summary>
public sealed record GameReward(int Id, int Available, string WebIcon, string Icon, IReadOnlyList<int> Needs, IReadOnlyList<int> Furniture, GameRules Rules, IReadOnlyList<GameRewardLevel> Levels);

/// <summary>A character the player can start as.</summary>
public sealed record GameCharacter(int Id, LocalText Name);

/// <summary>An image a reward can show: its path for web pages and its path for the game's screens.</summary>
public sealed record GameIcon(string WebIcon, string Icon);

/// <summary>What the tab needs of the game, all read at once when the tables are written.</summary>
public sealed record GameSnapshot(
    IReadOnlyList<GameReward> Rewards,
    IReadOnlyDictionary<string, IReadOnlyList<FieldSet>> Templates,
    IReadOnlyList<GameIcon> Items,
    IReadOnlyList<int> Pools,
    IReadOnlyList<GameCharacter> Characters,
    IReadOnlyList<DayEntry> Days)
{
    public static readonly GameSnapshot Empty = new(Array.Empty<GameReward>(), new Dictionary<string, IReadOnlyList<FieldSet>>(), Array.Empty<GameIcon>(), Array.Empty<int>(), Array.Empty<GameCharacter>(), Array.Empty<DayEntry>());
}

/// <summary>What a save from the Rewards tab comes to: the text to write, or why nothing is written.</summary>
public abstract record SaveCheck
{
    private SaveCheck()
    {
    }

    public sealed record Write(string Text) : SaveCheck;

    public sealed record Refused(IReadOnlyList<string> Problems) : SaveCheck;
}

/// <summary>What the plugin sends the Rewards tab, and what it makes of a save.</summary>
public static class RewardsPage
{
    public const string Prefix = "slmod|morerewards|rewards|";
    public const string Load = "load";
    public const string Save = "save|";
    public const string NoScript = "noscript";

    /// <summary>
    /// A page message without the tab's prefix, such as "load". The Toolkit's mailbox hands over the whole message,
    /// prefix included.
    /// </summary>
    public static string Verb(string message) =>
        message.StartsWith(Prefix, StringComparison.Ordinal) ? message.Substring(Prefix.Length) : message;

    private const string ScriptPrefix = "Buff/";
    private const string RewardIcons = "rewards";

    private static readonly JsonDocumentOptions Forgiving = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    /// <summary>
    /// The script that hands the tab its data. The rewards file goes as plain JSON, comments dropped, or as null when it
    /// is not JSON at all, which the tab takes as "do not save over it". Every non-ASCII character is escaped, so the
    /// data can never end the script early. <paramref name="builtInText"/> is the rewards the mod adds itself (the wind
    /// rewards, while Wind Power is installed), or null; the tab shows them next to the game's and offers the wind effect.
    /// </summary>
    public static string DataCall(GameSnapshot game, string? fileText, IReadOnlyList<string> startProblems, string? builtInText)
    {
        var problems = (fileText == null ? new[] { "the rewards file could not be read" } : RewardFile.Parse(fileText).Problems).Concat(startProblems).Distinct().ToList();

        return "window.__slmrRewards&&window.__slmrRewards.data(" + Json(writer => WriteData(writer, game, fileText, problems, builtInText)) + ")";
    }

    /// <summary>A reply to a save, shown in the tab's status line.</summary>
    public static string SavedCall(bool isSaved, string message) =>
        $"window.__slmrRewards&&window.__slmrRewards.saved({(isSaved ? "true" : "false")},{JsText.Literal(message)})";

    /// <summary>A save is written only if every entry of it reads cleanly, laid out for reading by hand.</summary>
    public static SaveCheck CheckSave(string json)
    {
        var problems = RewardFile.Parse(json).Problems;

        if (problems.Count > 0) return new SaveCheck.Refused(problems);

        using var document = JsonDocument.Parse(json, Forgiving);
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            document.RootElement.WriteTo(writer);
        }

        return new SaveCheck.Write(Encoding.UTF8.GetString(stream.ToArray()) + Environment.NewLine);
    }

    /// <summary>Game pages sit two folders below the web root and reach images with "../../"; the tab runs at the root.</summary>
    public static string FromRoot(string webIcon)
    {
        var path = webIcon;

        while (path.StartsWith("../", StringComparison.Ordinal)) path = path.Substring(3);

        return path;
    }

    private static void WriteData(Utf8JsonWriter writer, GameSnapshot game, string? fileText, IReadOnlyList<string> problems, string? builtInText)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("windPower", builtInText != null);
        writer.WritePropertyName("builtIn");
        WriteFile(writer, builtInText);
        writer.WriteStartArray("game");

        foreach (var reward in game.Rewards.OrderBy(reward => reward.Id)) WriteReward(writer, reward);

        writer.WriteEndArray();
        writer.WriteStartObject("templates");

        foreach (var template in game.Templates.Select(pair => (Script: Unprefixed(pair.Key), pair.Value)).OrderBy(pair => pair.Script, StringComparer.Ordinal))
        {
            writer.WriteStartObject(template.Script);

            foreach (var field in template.Value) WriteValue(writer, field);

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.WriteStartArray("pools");

        foreach (var pool in game.Pools.Distinct().OrderBy(pool => pool)) writer.WriteNumberValue(pool);

        writer.WriteEndArray();
        WriteCharacters(writer, game.Characters);
        WriteDays(writer, game.Days);
        writer.WriteStartArray("icons");

        foreach (var (group, icon) in IconChoices(game)) WriteIcon(writer, group, icon);

        writer.WriteEndArray();
        writer.WritePropertyName("file");
        WriteFile(writer, fileText);
        writer.WriteStartArray("problems");

        foreach (var problem in problems) writer.WriteStringValue(problem);

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    /// <summary>The rewards' images first, as "rewards", then the items', grouped by the folder they sit in. Each image comes once.</summary>
    private static IEnumerable<(string Group, GameIcon Icon)> IconChoices(GameSnapshot game)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var rewards = game.Rewards.OrderBy(reward => reward.Id).Select(reward => (Group: RewardIcons, Icon: new GameIcon(FromRoot(reward.WebIcon), reward.Icon)));
        var items = game.Items.Select(item => new GameIcon(FromRoot(item.WebIcon), item.Icon)).Select(icon => (Group: Folder(icon.WebIcon), Icon: icon)).OrderBy(choice => choice.Group, StringComparer.Ordinal);

        foreach (var choice in rewards.Concat(items))
        {
            if (choice.Icon.WebIcon.Length == 0 || !seen.Add(choice.Icon.WebIcon)) continue;

            yield return choice;
        }
    }

    /// <summary>"Res/Food/a.png" sits in Food.</summary>
    private static string Folder(string fromRoot)
    {
        var parts = fromRoot.Split('/');

        return parts.Length >= 3 ? parts[parts.Length - 2] : "";
    }

    private static void WriteIcon(Utf8JsonWriter writer, string group, GameIcon icon)
    {
        writer.WriteStartObject();
        writer.WriteString("icon", icon.WebIcon);
        writer.WriteString("unityIcon", icon.Icon);
        writer.WriteString("group", group);
        writer.WriteEndObject();
    }

    private static void WriteFile(Utf8JsonWriter writer, string? fileText)
    {
        try
        {
            using var document = JsonDocument.Parse(fileText ?? "", Forgiving);

            document.RootElement.WriteTo(writer);
        }
        catch (JsonException)
        {
            writer.WriteNullValue();
        }
    }

    private static void WriteReward(Utf8JsonWriter writer, GameReward reward)
    {
        writer.WriteStartObject();
        writer.WriteNumber("id", reward.Id);
        writer.WriteNumber("available", reward.Available);
        writer.WriteString("icon", FromRoot(reward.WebIcon));
        writer.WriteString("unityIcon", reward.Icon);
        WriteInts(writer, "needs", reward.Needs);
        WriteInts(writer, "furniture", reward.Furniture);
        writer.WriteBoolean("isReget", reward.Rules.IsReget);
        writer.WriteNumber("endless", reward.Rules.Endless);
        WriteInts(writer, "chars", reward.Rules.Characters);
        writer.WriteNumber("layers", reward.Rules.Layers);
        writer.WriteNumber("fromDay", reward.Rules.FromDay);
        writer.WriteStartArray("also");

        foreach (var need in reward.Rules.AlsoNeeds) writer.WriteStringValue(need);

        writer.WriteEndArray();
        writer.WriteStartArray("levels");

        foreach (var level in reward.Levels) WriteLevel(writer, level);

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteCharacters(Utf8JsonWriter writer, IReadOnlyList<GameCharacter> characters)
    {
        writer.WriteStartArray("characters");

        foreach (var character in characters.OrderBy(character => character.Id))
        {
            writer.WriteStartObject();
            writer.WriteNumber("id", character.Id);
            WriteText(writer, "name", character.Name);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    /// <summary>Each day's pools as [pool, amount] pairs, in the order the game draws them.</summary>
    private static void WriteDays(Utf8JsonWriter writer, IReadOnlyList<DayEntry> days)
    {
        writer.WriteStartArray("days");

        foreach (var day in days.OrderBy(day => day.Day))
        {
            writer.WriteStartObject();
            writer.WriteNumber("day", day.Day);
            writer.WriteStartArray("pools");

            foreach (var pool in day.Pools)
            {
                writer.WriteStartArray();
                writer.WriteNumberValue(pool.Pool);
                writer.WriteNumberValue(pool.Amount);
                writer.WriteEndArray();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteInts(Utf8JsonWriter writer, string name, IReadOnlyList<int> values)
    {
        writer.WriteStartArray(name);

        foreach (var value in values) writer.WriteNumberValue(value);

        writer.WriteEndArray();
    }

    private static string Unprefixed(string script) =>
        script.StartsWith(ScriptPrefix, StringComparison.Ordinal) ? script.Substring(ScriptPrefix.Length) : script;

    private static void WriteLevel(Utf8JsonWriter writer, GameRewardLevel level)
    {
        writer.WriteStartObject();
        writer.WriteNumber("id", level.Id);
        writer.WriteNumber("cost", level.Cost);
        WriteText(writer, "name", level.Name);
        WriteText(writer, "desc", level.Description);
        writer.WriteString("script", Unprefixed(level.Script));
        writer.WriteStartObject("values");

        foreach (var field in level.Values) WriteValue(writer, field);

        writer.WriteEndObject();
        writer.WriteStartObject("pools");

        foreach (var pool in level.Pools) writer.WriteNumber(pool.Group.ToString(System.Globalization.CultureInfo.InvariantCulture), pool.Weight);

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    /// <summary>Only the fields an effect actually sets: zeros, empty text and false say nothing about it. Its ID and script name are not numbers to edit.</summary>
    private static void WriteValue(Utf8JsonWriter writer, FieldSet field)
    {
        if (field.Field is "ID" or "ConfigName") return;

        switch (field.Value)
        {
            case FieldValue.Number { Value: not 0.0 } number:
                writer.WriteNumber(field.Field, number.Value);
                break;
            case FieldValue.Text { Value.Length: > 0 } text:
                writer.WriteString(field.Field, text.Value);
                break;
            case FieldValue.Flag { Value: true }:
                writer.WriteBoolean(field.Field, true);
                break;
        }
    }

    private static void WriteText(Utf8JsonWriter writer, string name, LocalText text)
    {
        writer.WriteStartObject(name);
        writer.WriteString("en", text.En);
        writer.WriteString("zh", text.Zh);
        writer.WriteEndObject();
    }

    private static string Json(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            write(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
