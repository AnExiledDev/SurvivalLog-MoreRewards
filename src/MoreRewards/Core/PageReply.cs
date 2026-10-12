using System;
using System.Collections.Generic;

namespace MoreRewards.Core;

/// <summary>What the page's answer to a check asks of the plugin: send the script again, and lines for the log.</summary>
public sealed record PageTurn(bool NeedsScript, IReadOnlyList<string> Notes)
{
    public static readonly PageTurn Quiet = new(false, Array.Empty<string>());
}

/// <summary>
/// Reads the web UI's answer to the plugin's check. The page answers <see cref="Missing"/> when it has lost the
/// script (the web view reloaded), <see cref="Nothing"/> when it has nothing to say, and otherwise one note per line.
/// </summary>
public static class PageReply
{
    public const string Missing = "missing";

    public const string Nothing = "ok";

    public static PageTurn Read(string? reply)
    {
        if (string.IsNullOrWhiteSpace(reply) || reply == Nothing) return PageTurn.Quiet;

        if (reply == Missing) return new PageTurn(true, Array.Empty<string>());

        return new PageTurn(false, reply.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
