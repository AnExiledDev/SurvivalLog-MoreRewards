using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;
using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>
/// The mod's settings as the rewards screen's settings panel shows and changes them. A change goes through the
/// config entry, so it is held to the setting's range, saved to the config file, and applied by whatever watches it.
/// </summary>
internal sealed class SettingsBoard
{
    private const string RestartTag = "ModManager.Restart";
    private const string ShowWhenTag = "ModManager.ShowWhen=";

    private readonly IReadOnlyList<ConfigEntryBase> _entries;

    public SettingsBoard(IReadOnlyList<ConfigEntryBase> entries) => _entries = entries;

    public IReadOnlyList<SettingRow> Rows(Language language) => _entries.Select(entry => Row(entry, language)).ToList();

    /// <returns>Why the value was not set, or null when it was.</returns>
    public string? Set(string key, string text)
    {
        if (_entries.FirstOrDefault(entry => KeyOf(entry) == key) is not { } entry) return $"no setting named {key}";

        if (Parse(entry.SettingType, text) is not { } value) return $"{text} is not a value {key} can take";

        entry.BoxedValue = value;

        return null;
    }

    private SettingRow Row(ConfigEntryBase entry, Language language)
    {
        var type = entry.SettingType;
        var kind = type == typeof(bool) ? SettingKind.Bool : type.IsEnum ? SettingKind.Choice : SettingKind.Int;
        var range = entry.Description.AcceptableValues as AcceptableValueRange<int>;
        var tags = entry.Description.Tags ?? Array.Empty<object>();
        var lines = (entry.Description.Description ?? "").Split('\n');
        var desc = language == Language.Chinese && lines.Length > 1 ? lines[1] : lines[0];

        return new SettingRow(KeyOf(entry), entry.Definition.Section, entry.Definition.Key, kind, ValueText(entry), range?.MinValue ?? 0, range?.MaxValue ?? 0,
            type.IsEnum ? Enum.GetNames(type) : Array.Empty<string>(), desc, tags.Contains(RestartTag), IsShown(tags));
    }

    /// <summary>A setting tagged to show only while another is on is hidden while that one is off.</summary>
    private bool IsShown(object[] tags)
    {
        foreach (var tag in tags.OfType<string>().Where(tag => tag.StartsWith(ShowWhenTag, StringComparison.Ordinal)))
        {
            var key = tag.Substring(ShowWhenTag.Length);

            if (_entries.FirstOrDefault(entry => KeyOf(entry) == key)?.BoxedValue is false) return false;
        }

        return true;
    }

    private static string KeyOf(ConfigEntryBase entry) => entry.Definition.Section + "." + entry.Definition.Key;

    private static string ValueText(ConfigEntryBase entry) => entry.BoxedValue switch
    {
        bool flag => flag ? "true" : "false",
        int number => number.ToString(CultureInfo.InvariantCulture),
        var other => other?.ToString() ?? "",
    };

    private static object? Parse(Type type, string text)
    {
        if (type == typeof(bool)) return bool.TryParse(text, out var flag) ? flag : null;

        if (type == typeof(int)) return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : null;

        if (type.IsEnum && Enum.GetNames(type).Contains(text)) return Enum.Parse(type, text);

        return null;
    }
}
