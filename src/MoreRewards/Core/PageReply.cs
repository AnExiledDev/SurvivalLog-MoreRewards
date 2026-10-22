using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreRewards.Core;

/// <summary>What the page's answer to a check asks of the plugin: send the script again, send the screen's setup, and lines for the log.</summary>
public sealed record PageTurn(bool NeedsScript, IReadOnlyList<string> Notes, bool NeedsSetup = false)
{
    public static readonly PageTurn Quiet = new(false, Array.Empty<string>());
}

/// <summary>
/// Reads the web UI's answer to the plugin's check. The page answers <see cref="Missing"/> when it has lost the
/// script (the web view reloaded), <see cref="Nothing"/> when it has nothing to say, and otherwise one note per line.
/// A line that is <see cref="NeedsSetup"/> asks for the rewards screen's setup and is not a note.
/// </summary>
public static class PageReply
{
    public const string Missing = "missing";

    public const string Nothing = "ok";

    public const string NeedsSetup = "needs setup";

    public static PageTurn Read(string? reply)
    {
        if (string.IsNullOrWhiteSpace(reply) || reply == Nothing) return PageTurn.Quiet;

        if (reply == Missing) return new PageTurn(true, Array.Empty<string>());

        var lines = reply.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new PageTurn(false, lines.Where(line => line != NeedsSetup).ToList(), lines.Contains(NeedsSetup));
    }
}
