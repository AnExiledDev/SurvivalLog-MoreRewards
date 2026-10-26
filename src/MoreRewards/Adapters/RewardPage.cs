using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using GameCore.HotUpdate.ReduxUI;
using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>
/// The mod's half that lives in the game's web UI: page.js, run in the root document the game's pages sit in. It lays
/// out the rewards page (RogueSelect.html) when it shows more than the game's three cards. The web view can reload at
/// any time and lose the script, so every couple of seconds the page is asked for its notes, and the script is sent
/// again when the page answers that it is missing. Each distinct note is logged once, so a page that keeps failing the
/// same way cannot flood the log. The web view's callback only stores the answer; the next frame reads it.
/// </summary>
internal sealed class RewardPage
{
    private const float CheckSeconds = 2f;
    private const int MostLoggedSends = 5;
    private const string ScriptResource = "MoreRewards.page.js";
    private const string Check = "window.__slmorerewards?String(window.__slmorerewards.take()):'" + PageReply.Missing + "'";
    private const string TakeAfterInstall = "\n;(function(){try{return String(window.__slmorerewards.take());}catch(e){return 'page script did not start: '+e;}})()";

    private readonly Action<string> _log;
    private readonly Action _onNeedsSetup;
    private readonly HashSet<string> _loggedNotes = new();
    private string? _script;
    private string? _install;
    private string? _reply;
    private Il2CppSystem.Action<string>? _onReply;

    /// <summary>Off the whole and half seconds the once-a-second checks run on, so they do not share a frame.</summary>
    private float _nextCheckAt = 0.75f;
    private int _sends;
    private bool _hasReportedRunFailure;

    /// <param name="onNeedsSetup">Called when the rewards screen is open and the page has not been told how to draw the mod's buttons.</param>
    public RewardPage(Action<string> log, Action onNeedsSetup)
    {
        _log = log;
        _onNeedsSetup = onNeedsSetup;
    }

    /// <summary>Most frames there is no answer and no check due, and this reads two fields.</summary>
    public void Tick(float now)
    {
        if (Interlocked.Exchange(ref _reply, null) is { } reply && Answer(reply, now)) return;

        if (now < _nextCheckAt) return;

        _nextCheckAt = now + CheckSeconds;
        Run(Check, keepsReply: true);
    }

    /// <returns>Whether the script was sent again, which stands in for this frame's check.</returns>
    private bool Answer(string reply, float now)
    {
        var turn = PageReply.Read(reply);

        foreach (var note in turn.Notes)
        {
            if (_loggedNotes.Add(note)) _log("page: " + note);
        }

        if (turn.NeedsSetup) _onNeedsSetup();

        if (!turn.NeedsScript) return false;

        _nextCheckAt = now + CheckSeconds;
        Install();

        return true;
    }

    private void Install()
    {
        if (!Run(_install ??= Script() + TakeAfterInstall, keepsReply: true)) return;

        _sends++;

        if (_sends <= MostLoggedSends) _log("page script sent to the web UI" + (_sends == MostLoggedSends ? $" ({MostLoggedSends} times; further sends are not logged)" : ""));
    }

    /// <summary>Hands the page a call from the plugin; what it returns is not read.</summary>
    public void Send(string javaScript) => Run(javaScript, keepsReply: false);

    /// <param name="keepsReply">The result is the page's answer to a check, read on the next frame.</param>
    /// <returns>Whether the web UI was there to run it.</returns>
    private bool Run(string javaScript, bool keepsReply)
    {
        var webView = GameSingleton.Existing<ReduxUISystem>()?.GetWebUILayer()?.canvasWebViewPrefab?.WebView;

        if (webView == null) return false;

        try
        {
            // One callback, made once, for every check; a call whose answer is not read passes none.
            webView.ExecuteJavaScript(javaScript, keepsReply ? (_onReply ??= (Action<string>)TakeReply) : null);

            return true;
        }
        catch (Exception ex)
        {
            if (!_hasReportedRunFailure) _log("could not run script in the web UI (reported once): " + ex.Message);

            _hasReportedRunFailure = true;

            return false;
        }
    }

    /// <summary>The web view's callback; it only stores the answer for the next frame.</summary>
    private void TakeReply(string? result) => Interlocked.Exchange(ref _reply, result ?? "");

    private string Script()
    {
        if (_script != null) return _script;

        using var stream = typeof(RewardPage).Assembly.GetManifestResourceStream(ScriptResource) ?? throw new InvalidOperationException(ScriptResource + " is not embedded");
        using var reader = new StreamReader(stream);

        return _script = reader.ReadToEnd();
    }
}
