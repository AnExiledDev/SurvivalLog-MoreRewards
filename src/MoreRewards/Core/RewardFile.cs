using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace MoreRewards.Core;

/// <summary>A value for one field of a game table row, as the rewards file gives it.</summary>
public abstract record FieldValue
{
    private FieldValue()
    {
    }

    public sealed record Number(double Value) : FieldValue;

    public sealed record Text(string Value) : FieldValue;

    public sealed record Flag(bool Value) : FieldValue;

    public sealed record Numbers(IReadOnlyList<int> Values) : FieldValue;
}

/// <summary>One field set on a row, named as the game's table names it (LvUpCost, IntValue0, ConfigName...).</summary>
public sealed record FieldSet(string Field, FieldValue Value);

/// <summary>Text in both of the game's languages.</summary>
public sealed record LocalText(string En, string Zh);

/// <summary>A buff or effect row: an existing one changed, or a new one copied from <paramref name="CopyFrom"/>.</summary>
public sealed record RowEntry(int Id, int? CopyFrom, IReadOnlyList<FieldSet> Fields);

/// <summary>A reward pool the reward is drawn from, and how likely it is to be drawn there next to the others.</summary>
public sealed record PoolEntry(int Group, int Weight);

/// <summary>
/// What a reward does: a game effect script (<c>game:&lt;script&gt;</c>) run with <paramref name="Fields"/>, or an effect
/// More Rewards runs itself (<c>mr:&lt;name&gt;</c>), which reads <paramref name="Fields"/>.
/// </summary>
public sealed record GiveEntry(string Effect, IReadOnlyList<FieldSet> Fields);

/// <summary>
/// One reward in the file. Anything left out keeps the game's value, or the value of the reward it copies.
/// <paramref name="Needs"/> and <paramref name="Furniture"/> are null when the file leaves them as they are.
/// </summary>
public sealed record RewardEntry(
    int Id,
    bool Enabled,
    int? CopyFrom,
    LocalText? Name,
    LocalText? Description,
    int? Cost,
    IReadOnlyList<int>? Needs,
    IReadOnlyList<int>? Furniture,
    IReadOnlyList<PoolEntry> Pools,
    IReadOnlyList<FieldSet> Fields,
    GiveEntry? Give,
    IReadOnlyList<RowEntry> Buffs,
    IReadOnlyList<RowEntry> Effects);

/// <summary>The rewards file as read, with every entry that could not be read.</summary>
public sealed record RewardFile(IReadOnlyList<RewardEntry> Rewards, IReadOnlyList<RowEntry> Buffs, IReadOnlyList<RowEntry> Effects, IReadOnlyList<string> Problems)
{
    public static readonly RewardFile Empty = new(Array.Empty<RewardEntry>(), Array.Empty<RowEntry>(), Array.Empty<RowEntry>(), Array.Empty<string>());

    /// <summary>Both files' entries, this one's first.</summary>
    public RewardFile With(RewardFile other) =>
        new(Rewards.Concat(other.Rewards).ToList(), Buffs.Concat(other.Buffs).ToList(), Effects.Concat(other.Effects).ToList(), Problems.Concat(other.Problems).ToList());

    /// <summary>The file without the rewards that give <paramref name="effect"/>.</summary>
    public RewardFile Without(string effect) =>
        this with { Rewards = Rewards.Where(reward => reward.Give?.Effect != effect).ToList() };

    /// <summary>
    /// Reads the file. Comments and trailing commas are allowed, since people edit it by hand. An entry that cannot be
    /// read is skipped and reported; the rest still apply. A file that is not JSON at all applies nothing.
    /// </summary>
    public static RewardFile Parse(string json)
    {
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        }
        catch (JsonException ex)
        {
            return Empty with { Problems = new[] { $"the file is not valid JSON ({ex.Message}); nothing in it was applied" } };
        }

        using (document)
        {
            var problems = new List<string>();
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object) return Empty with { Problems = new[] { "the file must hold one { } object; nothing in it was applied" } };

            var rewards = ReadList(root, "rewards", "reward", problems, ReadReward);
            var buffs = ReadList(root, "buffs", "buff", problems, ReadRow);
            var effects = ReadList(root, "effects", "effect", problems, ReadRow);

            return new RewardFile(rewards, buffs, effects, problems);
        }
    }

    private static RewardEntry ReadReward(JsonElement entry, List<string> problems)
    {
        var id = RequireInt(entry, "id");
        var where = $"reward {id}";

        return new RewardEntry(
            id,
            OptionalBool(entry, "enabled") ?? true,
            OptionalInt(entry, "copyFrom"),
            OptionalText(entry, "name"),
            OptionalText(entry, "description"),
            OptionalInt(entry, "cost"),
            OptionalInts(entry, "needs"),
            OptionalInts(entry, "furniture"),
            ReadPools(entry),
            ReadFields(entry),
            entry.TryGetProperty("give", out var give) ? ReadGive(give) : null,
            ReadList(entry, "buffs", where + " buff", problems, ReadRow),
            ReadList(entry, "effects", where + " effect", problems, ReadRow));
    }

    private static GiveEntry ReadGive(JsonElement give)
    {
        if (give.ValueKind != JsonValueKind.Object) throw new FormatException("\"give\" must be a { } object");

        var effect = give.TryGetProperty("effect", out var name) && name.ValueKind == JsonValueKind.String ? name.GetString() ?? "" : "";

        if (effect.Length == 0) throw new FormatException("\"give\" has no \"effect\"");

        return new GiveEntry(effect, ReadFields(give));
    }

    /// <summary>{ "1013": 1, "1020": 3 }: each pool's ID, and the reward's weight in it.</summary>
    private static IReadOnlyList<PoolEntry> ReadPools(JsonElement entry)
    {
        if (!entry.TryGetProperty("pools", out var pools)) return Array.Empty<PoolEntry>();

        if (pools.ValueKind != JsonValueKind.Object) throw new FormatException("\"pools\" must be a { } object of pool ID and weight");

        var list = new List<PoolEntry>();

        foreach (var pool in pools.EnumerateObject())
        {
            if (!int.TryParse(pool.Name, NumberStyles.None, CultureInfo.InvariantCulture, out var group)) throw new FormatException($"pool \"{pool.Name}\" must be a pool ID");

            if (pool.Value.ValueKind != JsonValueKind.Number || !pool.Value.TryGetInt32(out var weight) || weight < 1) throw new FormatException($"pool {group} needs a weight of 1 or more");

            list.Add(new PoolEntry(group, weight));
        }

        return list;
    }

    private static RowEntry ReadRow(JsonElement row, List<string> problems) => new(RequireInt(row, "id"), OptionalInt(row, "copyFrom"), ReadFields(row));

    /// <summary>A list whose bad entries are reported by position and left out.</summary>
    private static List<T> ReadList<T>(JsonElement parent, string name, string what, List<string> problems, Func<JsonElement, List<string>, T> read)
    {
        var items = new List<T>();

        if (!parent.TryGetProperty(name, out var list)) return items;

        if (list.ValueKind != JsonValueKind.Array)
        {
            problems.Add($"\"{name}\" must be a [ ] list; it was skipped");

            return items;
        }

        var position = 0;

        foreach (var element in list.EnumerateArray())
        {
            position++;

            try
            {
                if (element.ValueKind != JsonValueKind.Object) throw new FormatException("it must be a { } object");

                items.Add(read(element, problems));
            }
            catch (FormatException ex)
            {
                problems.Add($"{what} #{position} was skipped: {ex.Message}");
            }
        }

        return items;
    }

    private static IReadOnlyList<FieldSet> ReadFields(JsonElement entry)
    {
        if (!entry.TryGetProperty("fields", out var set)) return Array.Empty<FieldSet>();

        if (set.ValueKind != JsonValueKind.Object) throw new FormatException("\"fields\" must be a { } object");

        var fields = new List<FieldSet>();

        foreach (var field in set.EnumerateObject()) fields.Add(new FieldSet(field.Name, ReadValue(field.Name, field.Value)));

        return fields;
    }

    private static FieldValue ReadValue(string field, JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Number => new FieldValue.Number(value.GetDouble()),
        JsonValueKind.String => new FieldValue.Text(value.GetString() ?? ""),
        JsonValueKind.True => new FieldValue.Flag(true),
        JsonValueKind.False => new FieldValue.Flag(false),
        JsonValueKind.Array => new FieldValue.Numbers(ReadInts($"field {field}", value)),
        _ => throw new FormatException($"field {field} must be a number, text, true/false or a list of whole numbers"),
    };

    private static List<int> ReadInts(string what, JsonElement list)
    {
        var values = new List<int>();

        foreach (var item in list.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt32(out var number)) throw new FormatException($"{what} must list whole numbers only");

            values.Add(number);
        }

        return values;
    }

    private static IReadOnlyList<int>? OptionalInts(JsonElement entry, string name)
    {
        if (!entry.TryGetProperty(name, out var value)) return null;

        if (value.ValueKind != JsonValueKind.Array) throw new FormatException($"\"{name}\" must be a [ ] list of whole numbers");

        return ReadInts($"\"{name}\"", value);
    }

    private static int RequireInt(JsonElement entry, string name) =>
        OptionalInt(entry, name) ?? throw new FormatException($"\"{name}\" is missing");

    private static int? OptionalInt(JsonElement entry, string name)
    {
        if (!entry.TryGetProperty(name, out var value)) return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)) return number;

        throw new FormatException($"\"{name}\" must be a whole number, not {value.GetRawText()}");
    }

    private static bool? OptionalBool(JsonElement entry, string name)
    {
        if (!entry.TryGetProperty(name, out var value)) return null;

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new FormatException($"\"{name}\" must be true or false"),
        };
    }

    /// <summary>Either a plain string, used for both languages, or { "en": ..., "zh": ... }; a missing text in one language uses the other.</summary>
    private static LocalText? OptionalText(JsonElement entry, string name)
    {
        if (!entry.TryGetProperty(name, out var value)) return null;

        if (value.ValueKind == JsonValueKind.String) return new LocalText(value.GetString() ?? "", value.GetString() ?? "");

        if (value.ValueKind != JsonValueKind.Object) throw new FormatException($"\"{name}\" must be text or {{ \"en\": ..., \"zh\": ... }}");

        var en = value.TryGetProperty("en", out var enText) && enText.ValueKind == JsonValueKind.String ? enText.GetString() ?? "" : "";
        var zh = value.TryGetProperty("zh", out var zhText) && zhText.ValueKind == JsonValueKind.String ? zhText.GetString() ?? "" : "";

        if (en.Length == 0 && zh.Length == 0) throw new FormatException($"\"{name}\" has neither \"en\" nor \"zh\" text");

        return new LocalText(en.Length > 0 ? en : zh, zh.Length > 0 ? zh : en);
    }

    /// <summary>A whole number field value, or null when the number has a fraction.</summary>
    public static int? AsWhole(double value) =>
        value is >= int.MinValue and <= int.MaxValue && Math.Floor(value) == value ? (int)value : null;
}
