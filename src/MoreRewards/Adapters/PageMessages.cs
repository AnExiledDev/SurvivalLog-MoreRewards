using System;
using System.Collections.Concurrent;
using System.Threading;
using HarmonyLib;
using MoreRewards.Core;
using Vuplex.WebView.Internal;

namespace MoreRewards.Adapters;

/// <summary>
/// Catches the rewards page's messages before the game's web layer sees them (it would log them as unknown) and queues
/// them for the next frame, so nothing runs on the web view's thread. Static because a Harmony prefix has nowhere else
/// to look.
/// </summary>
internal static class PageMessages
{
    private static readonly ConcurrentQueue<PageCommand> Queue = new();
    private static Action<string> _log = _ => { };
    private static int _hasReportedFailure;

    /// <returns>Whether the page's buttons can reach the plugin.</returns>
    public static bool Apply(Harmony harmony, Action<string> log)
    {
        _log = log;

        try
        {
            harmony.Patch(AccessTools.Method(typeof(BaseWebView), "HandleMessageEmitted"), prefix: new HarmonyMethod(typeof(PageMessages), nameof(TakeOurs)));
            log("listening for rewards page messages");

            return true;
        }
        catch (Exception ex)
        {
            log("could not hook web UI messages, the mod's buttons on the rewards screen will do nothing: " + ex.Message);

            return false;
        }
    }

    public static bool TryTake(out PageCommand command) => Queue.TryDequeue(out command);

    /// <summary>Never throws into the web view: on any failure the message goes on to the game as if the mod were not here.</summary>
    private static bool TakeOurs(string serializedMessage)
    {
        try
        {
            if (serializedMessage == null || !serializedMessage.StartsWith(PageCommands.Prefix, StringComparison.Ordinal)) return true;

            if (PageCommands.Read(serializedMessage) is { } command) Queue.Enqueue(command);

            return false;
        }
        catch (Exception ex)
        {
            ReportOnce(ex);

            return true;
        }
    }

    /// <summary>Runs on the web view's thread, so the once-only flag is set atomically.</summary>
    private static void ReportOnce(Exception ex)
    {
        if (Interlocked.Exchange(ref _hasReportedFailure, 1) == 0) _log("reading a web UI message failed, it was passed on to the game (reported once): " + ex);
    }
}
