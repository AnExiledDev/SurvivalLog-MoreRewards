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
/// One thing a reward does: a game effect script (<c>game:&lt;script&gt;</c>) run with <paramref name="Fields"/>, or an
/// effect More Rewards runs itself (<c>mr:&lt;name&gt;</c>), which reads <paramref name="Fields"/>. A reward with levels
/// adds the fields of <paramref name="Levels"/> at each level; a level past the end of the list uses the last one.
/// </summary>
public sealed record GiveEntry(string Effect, IReadOnlyList<FieldSet> Fields, IReadOnlyList<IReadOnlyList<FieldSet>> Levels)
{
    public GiveEntry(string effect, IReadOnlyList<FieldSet> fields)
        : this(effect, fields, Array.Empty<IReadOnlyList<FieldSet>>())
    {
    }

    /// <summary>The fields at <paramref name="level"/>, counted from 0.</summary>
    public IReadOnlyList<FieldSet> FieldsAt(int level) =>
        Levels.Count == 0 ? Fields : Fields.Concat(Levels[Math.Min(level, Levels.Count - 1)]).ToList();
}

/// <summary>One level of a reward. Anything left out is the reward's own value; null pools keep the reward's pools.</summary>
public sealed record LevelEntry(int? Cost, LocalText? Name, LocalText? Description, IReadOnlyList<PoolEntry>? Pools, IReadOnlyList<FieldSet> Fields);

/// <summary>
/// One reward in the file. Anything left out keeps the game's value, or the value of the reward it copies.
/// <paramref name="Needs"/>, <paramref name="Furniture"/>, <paramref name="FromDay"/> and <paramref name="Layers"/> are null
/// when the file leaves them as they are. <paramref name="Layers"/> is how many times a reward the player can buy again
/// stacks. <paramref name="ToDay"/> is the last day it can be offered, null for no last day. <paramref name="AnyDay"/>
/// is a weight: the reward goes in the biggest pool of every day at that weight. <paramref name="AlwaysOffered"/> takes
/// one card each day for it while it can be offered. All three apply to every level.
/// <paramref name="Levels"/> is null for a reward of one level. Level n (from 0) is the reward with ID
/// <paramref name="Id"/> + n, as the game numbers Sunlight Collection I, II and III.
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
    int? FromDay,
    int? ToDay,
    int? AnyDay,
    bool AlwaysOffered,
    int? Layers,
    IReadOnlyList<PoolEntry> Pools,
    IReadOnlyList<FieldSet> Fields,
    IReadOnlyList<GiveEntry> Gives,
    IReadOnlyList<LevelEntry>? Levels,
    IReadOnlyList<RowEntry> Buffs,
    IReadOnlyList<RowEntry> Effects)
{
    /// <summary>The most levels and the most gives a reward can have.</summary>
    public const int MaxLevels = 10;

    public const int MaxGives = 10;

    public int LevelCount => Levels?.Count ?? 1;

    /// <summary>The reward IDs its levels use.</summary>
    public IEnumerable<int> Ids => Enumerable.Range(Id, LevelCount);
}

/// <summary>How many rewards one pool adds to a day's draw.</summary>
public sealed record DayPool(int Pool, int Amount);

/// <summary>
/// One day of the game's day table, in the order the game draws its pools. A day in the file replaces that day's pools
/// and amounts; a day left out keeps the game's.
/// </summary>
public sealed record DayEntry(int Day, IReadOnlyList<DayPool> Pools);

/// <summary>The rewards file as read, with every entry that could not be read.</summary>
public sealed record RewardFile(IReadOnlyList<RewardEntry> Rewards, IReadOnlyList<RowEntry> Buffs, IReadOnlyList<RowEntry> Effects, IReadOnlyList<DayEntry> Days, IReadOnlyList<string> Problems)
{
    public static readonly RewardFile Empty = new(Array.Empty<RewardEntry>(), Array.Empty<RowEntry>(), Array.Empty<RowEntry>(), Array.Empty<DayEntry>(), Array.Empty<string>());

    /// <summary>
    /// This file's entries with those of <paramref name="under"/> that use none of the same reward IDs. An entry here,
    /// switched off or not, replaces the one below, so the player's file can change or turn off a reward the mod carries.
    /// </summary>
    public RewardFile Over(RewardFile under)
    {
        var taken = new HashSet<int>(Rewards.SelectMany(reward => reward.Ids));
        var kept = under.Rewards.Where(reward => !reward.Ids.Any(taken.Contains));
        var days = new HashSet<int>(Days.Select(day => day.Day));

        return new RewardFile(
            Rewards.Concat(kept).ToList(),
            Buffs.Concat(under.Buffs).ToList(),
            Effects.Concat(under.Effects).ToList(),
            Days.Concat(under.Days.Where(day => !days.Contains(day.Day))).ToList(),
            Problems.Concat(under.Problems).ToList());
    }

    /// <summary>The file without the rewards that give <paramref name="effect"/>.</summary>
    public RewardFile Without(string effect) =>
        this with { Rewards = Rewards.Where(reward => reward.Gives.All(give => give.Effect != effect)).ToList() };

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
            var days = ReadList(root, "days", "day", problems, ReadDay);

            return new RewardFile(rewards, buffs, effects, days, problems);
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
            OptionalInt(entry, "fromDay"),
            OptionalInt(entry, "toDay"),
            OptionalInt(entry, "anyDay"),
            OptionalBool(entry, "alwaysOffered") ?? false,
            OptionalInt(entry, "layers"),
            ReadPools(entry),
            ReadFields(entry),
            ReadGives(entry),
            ReadLevels(entry),
            ReadList(entry, "buffs", where + " buff", problems, ReadRow),
            ReadList(entry, "effects", where + " effect", problems, ReadRow));
    }

    /// <summary>"gives" is a list; "give" is the one-give form files from before levels use. A bad give skips the reward.</summary>
    private static IReadOnlyList<GiveEntry> ReadGives(JsonElement entry)
    {
        var hasOne = entry.TryGetProperty("give", out var one);
        var hasList = entry.TryGetProperty("gives", out var list);

        if (hasOne && hasList) throw new FormatException("it has both \"give\" and \"gives\"; use one");

        if (hasOne) return new[] { ReadGive(one, "\"give\"") };

        if (!hasList) return Array.Empty<GiveEntry>();

        if (list.ValueKind != JsonValueKind.Array) throw new FormatException("\"gives\" must be a [ ] list");

        var gives = list.EnumerateArray().Select((give, i) => ReadGive(give, $"give {i + 1}")).ToList();

        if (gives.Count > RewardEntry.MaxGives) throw new FormatException($"it has {gives.Count} gives; the most is {RewardEntry.MaxGives}");

        return gives;
    }

    private static GiveEntry ReadGive(JsonElement give, string what)
    {
        if (give.ValueKind != JsonValueKind.Object) throw new FormatException($"{what} must be a {{ }} object");

        var effect = give.TryGetProperty("effect", out var name) && name.ValueKind == JsonValueKind.String ? name.GetString() ?? "" : "";

        if (effect.Length == 0) throw new FormatException($"{what} has no \"effect\"");

        var levels = new List<IReadOnlyList<FieldSet>>();

        if (give.TryGetProperty("levels", out var list))
        {
            if (list.ValueKind != JsonValueKind.Array) throw new FormatException($"{what}: \"levels\" must be a [ ] list");

            foreach (var level in list.EnumerateArray())
            {
                if (level.ValueKind != JsonValueKind.Object) throw new FormatException($"{what}: each level must be a {{ }} object");

                levels.Add(ReadFieldObject(level));
            }
        }

        return new GiveEntry(effect, ReadFields(give), levels);
    }

    /// <summary>A bad level skips the whole reward, so no level of it ends up half made.</summary>
    private static IReadOnlyList<LevelEntry>? ReadLevels(JsonElement entry)
    {
        if (!entry.TryGetProperty("levels", out var list)) return null;

        if (list.ValueKind != JsonValueKind.Array) throw new FormatException("\"levels\" must be a [ ] list");

        var levels = new List<LevelEntry>();

        foreach (var level in list.EnumerateArray())
        {
            if (level.ValueKind != JsonValueKind.Object) throw new FormatException($"level {levels.Count + 1} must be a {{ }} object");

            levels.Add(new LevelEntry(
                OptionalInt(level, "cost"),
                OptionalText(level, "name"),
                OptionalText(level, "description"),
                level.TryGetProperty("pools", out _) ? ReadPools(level) : null,
                ReadFields(level)));
        }

        if (levels.Count is 0 or > RewardEntry.MaxLevels) throw new FormatException($"it needs 1 to {RewardEntry.MaxLevels} levels, not {levels.Count}");

        return levels;
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

    /// <summary>
    /// "pools" is a [ ] list of { "pool", "amount" }, in the order the game draws them. Order matters: the game shows
    /// only the first 3 rewards drawn, so a later pool only shows when the ones before it come up short.
    /// </summary>
    private static DayEntry ReadDay(JsonElement entry, List<string> problems)
    {
        var day = RequireInt(entry, "day");

        if (!entry.TryGetProperty("pools", out var pools) || pools.ValueKind != JsonValueKind.Array) throw new FormatException($"day {day} needs \"pools\", a [ ] list of {{ \"pool\", \"amount\" }}");

        var list = new List<DayPool>();

        foreach (var pool in pools.EnumerateArray())
        {
            if (pool.ValueKind != JsonValueKind.Object) throw new FormatException($"day {day}: each pool must be a {{ \"pool\", \"amount\" }} object");

            var id = RequireInt(pool, "pool");
            var amount = RequireInt(pool, "amount");

            if (amount < 0) throw new FormatException($"day {day}: pool {id} needs an amount of 0 or more");

            if (list.Any(seen => seen.Pool == id)) throw new FormatException($"day {day} has pool {id} twice");

            list.Add(new DayPool(id, amount));
        }

        return new DayEntry(day, list);
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

        return ReadFieldObject(set);
    }

    private static IReadOnlyList<FieldSet> ReadFieldObject(JsonElement set)
    {
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
