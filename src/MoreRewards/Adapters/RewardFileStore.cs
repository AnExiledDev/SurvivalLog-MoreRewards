using System;
using System.IO;
using System.Reflection;
using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>Reads the rewards file from the config folder, and the wind rewards the mod carries.</summary>
internal static class RewardFileStore
{
    private const string DefaultResource = "MoreRewards.rewards.default.json";
    private const string WindResource = "MoreRewards.wind-rewards.json";

    /// <summary>The player's rewards file, written there first from the mod's default when it is missing.</summary>
    public static RewardFile Load(string path, Action<string> log)
    {
        try
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, Resource(DefaultResource));
                log($"wrote the default rewards file to {path}");
            }

            var file = RewardFile.Parse(File.ReadAllText(path));

            foreach (var problem in file.Problems) log("rewards file: " + problem);

            return file;
        }
        catch (Exception ex)
        {
            log($"rewards file could not be read, so it changes nothing: {ex.Message}");

            return RewardFile.Empty;
        }
    }

    /// <summary>The wind rewards, Wind Collection I, II and III, which apply while Wind Power is installed.</summary>
    public static RewardFile Wind(Action<string> log)
    {
        var file = RewardFile.Parse(Resource(WindResource));

        foreach (var problem in file.Problems) log("wind rewards: " + problem);

        return file;
    }

    /// <summary>The wind rewards as the mod carries them, for the Rewards tab to show.</summary>
    public static string WindText() => Resource(WindResource);

    private static string Resource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name) ?? throw new InvalidOperationException($"{name} is missing from the mod");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
