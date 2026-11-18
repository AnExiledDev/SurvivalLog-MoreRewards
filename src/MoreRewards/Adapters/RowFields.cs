using System;
using System.Collections.Generic;
using System.Reflection;
using MoreRewards.Core;
using Il2CppInts = Il2CppSystem.Collections.Generic.List<int>;

namespace MoreRewards.Adapters;

/// <summary>
/// Reads and writes the fields of a game table row by the name the table gives them. The game's rows keep each field
/// in a compiler-made backing field, which the interop layer shows as a property named <c>_Field_k__BackingField</c>.
/// </summary>
internal static class RowFields
{
    private const string Suffix = "_k__BackingField";
    private const BindingFlags Instance = BindingFlags.Public | BindingFlags.Instance;

    private static readonly Dictionary<(Type, string), PropertyInfo?> Found = new();

    /// <summary>Copies every field of <paramref name="from"/> onto <paramref name="to"/>, a row of the same table.</summary>
    public static void CopyAll(object from, object to)
    {
        foreach (var property in to.GetType().GetProperties(Instance))
        {
            if (!IsField(property) || !property.CanRead || !property.CanWrite) continue;

            property.SetValue(to, property.GetValue(from));
        }
    }

    /// <summary>Every field of the row the file could set, by its table name, for the Rewards tab to show.</summary>
    public static List<FieldSet> ReadAll(object row)
    {
        var fields = new List<FieldSet>();

        foreach (var property in row.GetType().GetProperties(Instance))
        {
            if (!IsField(property) || !property.CanRead) continue;

            if (ToValue(property.GetValue(row)) is { } value) fields.Add(new FieldSet(property.Name.Substring(1, property.Name.Length - 1 - Suffix.Length), value));
        }

        return fields;
    }

    /// <summary>True when every field but the ID and the label is zero, false, empty or unset: a row that asks for nothing.</summary>
    public static bool IsBlank(object row)
    {
        foreach (var property in row.GetType().GetProperties(Instance))
        {
            if (!IsField(property) || !property.CanRead || property.Name is "_ID" + Suffix or "_Source" + Suffix) continue;

            if (!IsEmpty(property.GetValue(row))) return false;
        }

        return true;
    }

    private static bool IsEmpty(object? value) => value switch
    {
        null => true,
        int number => number == 0,
        long number => number == 0,
        float number => number == 0,
        double number => number == 0,
        bool flag => !flag,
        string text => text.Length == 0,
        _ => value.GetType().GetProperty("Count")?.GetValue(value) is 0,
    };

    private static FieldValue? ToValue(object? value) => value switch
    {
        int number => new FieldValue.Number(number),
        long number => new FieldValue.Number(number),
        float number => new FieldValue.Number(number),
        double number => new FieldValue.Number(number),
        bool flag => new FieldValue.Flag(flag),
        string text => new FieldValue.Text(text),
        Il2CppInts list => new FieldValue.Numbers(ToArray(list)),
        _ => null,
    };

    private static int[] ToArray(Il2CppInts list)
    {
        var values = new int[list.Count];

        for (var i = 0; i < values.Length; i++) values[i] = list[i];

        return values;
    }

    /// <summary>A field's value as the row holds it, so it can be put back; null when the row has no such field.</summary>
    public static object? Get(object row, string field) => Find(row.GetType(), field)?.GetValue(row);

    /// <summary>Puts back a value <see cref="Get"/> read.</summary>
    public static void Restore(object row, string field, object? value) => Find(row.GetType(), field)?.SetValue(row, value);

    /// <summary>Sets one field, or returns why it could not be set.</summary>
    public static string? Set(object row, string field, FieldValue value)
    {
        var property = Find(row.GetType(), field);

        if (property == null) return $"{row.GetType().Name} has no field {field}";

        if (Convert(property.PropertyType, value) is not { } converted) return $"{field} on {row.GetType().Name} cannot hold {Describe(value)}";

        property.SetValue(row, converted);

        return null;
    }

    private static object? Convert(Type type, FieldValue value) => value switch
    {
        FieldValue.Number number when type == typeof(int) => RewardFile.AsWhole(number.Value),
        FieldValue.Number number when type == typeof(long) => RewardFile.AsWhole(number.Value) is { } whole ? (long)whole : null,
        FieldValue.Number number when type == typeof(float) => (float)number.Value,
        FieldValue.Number number when type == typeof(double) => number.Value,
        FieldValue.Flag flag when type == typeof(bool) => flag.Value,
        FieldValue.Text text when type == typeof(string) => text.Value,
        FieldValue.Numbers numbers when type == typeof(Il2CppInts) => ToList(numbers.Values),
        _ => null,
    };

    private static Il2CppInts ToList(IReadOnlyList<int> values)
    {
        var list = new Il2CppInts();

        foreach (var value in values) list.Add(value);

        return list;
    }

    private static string Describe(FieldValue value) => value switch
    {
        FieldValue.Number number => number.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
        FieldValue.Text text => $"\"{text.Value}\"",
        FieldValue.Flag flag => flag.Value ? "true" : "false",
        FieldValue.Numbers => "a list of numbers",
        _ => "that value",
    };

    private static PropertyInfo? Find(Type type, string field)
    {
        if (Found.TryGetValue((type, field), out var cached)) return cached;

        var property = type.GetProperty("_" + field + Suffix, Instance);

        Found[(type, field)] = property;

        return property;
    }

    private static bool IsField(PropertyInfo property) => property.Name.StartsWith("_", StringComparison.Ordinal) && property.Name.EndsWith(Suffix, StringComparison.Ordinal);
}
