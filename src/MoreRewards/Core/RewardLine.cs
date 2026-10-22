using System.Collections.Generic;

namespace MoreRewards.Core;

/// <summary>
/// A reward's line is every reward joined to it through the rewards it needs first, either way round: banning
/// Cellar Craft II with the whole line also bans Cellar Craft I and III.
/// </summary>
public static class RewardLine
{
    /// <param name="needs">Each reward and the rewards it needs first.</param>
    public static IReadOnlyCollection<int> Of(int id, IReadOnlyDictionary<int, IReadOnlyList<int>> needs)
    {
        var links = new Dictionary<int, List<int>>();

        foreach (var (reward, needed) in needs)
        {
            foreach (var first in needed)
            {
                Link(links, reward, first);
                Link(links, first, reward);
            }
        }

        var line = new HashSet<int> { id };
        var waiting = new Stack<int>();

        waiting.Push(id);

        while (waiting.Count > 0)
        {
            if (!links.TryGetValue(waiting.Pop(), out var next)) continue;

            foreach (var linked in next)
            {
                if (line.Add(linked)) waiting.Push(linked);
            }
        }

        return line;
    }

    private static void Link(Dictionary<int, List<int>> links, int from, int to)
    {
        if (!links.TryGetValue(from, out var list)) links[from] = list = new List<int>();

        list.Add(to);
    }
}
