using GameCore.HotUpdate.Battle.Logic;
using MoreRewards.Core;

namespace MoreRewards.Adapters;

/// <summary>The run's GameCounter values, which the game saves with the run and clears when a new loop starts.</summary>
internal sealed class GameCounters : ICounters
{
    public int Get(string key) => GameCounter.GetCounter(key);

    public void Set(string key, int value) => GameCounter.SetCounter(key, value);
}
