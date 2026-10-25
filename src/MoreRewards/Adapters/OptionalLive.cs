using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

namespace MoreRewards.Adapters;

/// <summary>
/// Applies settings through Exiled's Toolkit when it is installed, so a change on the Mod Manager's Mods screen takes
/// effect at once. Without the Toolkit, or with one older than 0.4.0, the mod listens for changes itself, so a change
/// made on the rewards screen's settings still applies at once. Only <see cref="WatchWithToolkit"/> names a Toolkit
/// type, so the mod loads without it.
/// </summary>
internal static class OptionalLive
{
    public const string ToolkitGuid = "com.dev.survivallog.exiledtoolkit";

    public const string MissingNote = "Exiled's Toolkit is not installed. Settings changed on the rewards screen still apply at once; install Toolkit 0.4.0 or newer to change them from the Mod Manager too.";

    private static readonly Lazy<ManualLogSource> Log = new(() => Logger.CreateLogSource(typeof(OptionalLive).Assembly.GetName().Name ?? "Settings"));

    /// <summary>Whether a Toolkit is installed. Valid from the mod's Load on, since the Toolkit loads first.</summary>
    public static bool IsToolkitInstalled => IL2CPPChainloader.Instance.Plugins.ContainsKey(ToolkitGuid);

    /// <summary>Calls <paramref name="apply"/> with the entry's value now, and again after each change.</summary>
    public static void Watch<T>(ConfigEntry<T> entry, Action<T> apply) => Watch(() => apply(entry.Value), entry);

    /// <summary>
    /// Calls <paramref name="apply"/> now, and again after any of the entries changes: through the Toolkit on the next
    /// frame when it is installed, otherwise straight away on the thread that made the change (the rewards screen's
    /// settings change them on the main thread).
    /// </summary>
    public static void Watch(Action apply, params ConfigEntryBase[] entries)
    {
        if (IsToolkitInstalled && TryWatchWithToolkit(apply, entries)) return;

        foreach (var file in entries.Select(entry => entry.ConfigFile).Distinct())
        {
            file.SettingChanged += (_, args) =>
            {
                if (entries.Contains(args.ChangedSetting)) ApplyOnce(apply);
            };
        }

        ApplyOnce(apply);
    }

    private static bool TryWatchWithToolkit(Action apply, ConfigEntryBase[] entries)
    {
        try
        {
            WatchWithToolkit(apply, entries);

            return true;
        }
        catch (Exception ex) when (ex is TypeLoadException or MissingMethodException or FileNotFoundException or FileLoadException)
        {
            Log.Value.LogWarning("Exiled's Toolkit is older than 0.4.0, so only the rewards screen's settings apply at once: " + ex.Message);

            return false;
        }
    }

    // Kept in its own method so the Toolkit's types are only looked up when it is installed.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WatchWithToolkit(Action apply, ConfigEntryBase[] entries) => ExiledToolkit.LiveSettings.Watch(apply, entries);

    // Matches the Toolkit: a setting that fails to apply is logged and does not stop the mod's start.
    private static void ApplyOnce(Action apply)
    {
        try
        {
            apply();
        }
        catch (Exception ex)
        {
            Log.Value.LogError("could not apply a setting: " + ex);
        }
    }
}
