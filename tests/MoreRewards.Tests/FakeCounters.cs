using MoreRewards.Core;

namespace MoreRewards.Tests;

/// <summary>The run's counters as the game keeps them: a name never set reads 0.</summary>
internal sealed class FakeCounters : ICounters
{
    public Dictionary<string, int> Values { get; } = new();

    public int Get(string key) => Values.TryGetValue(key, out var value) ? value : 0;

    public void Set(string key, int value) => Values[key] = value;
}
