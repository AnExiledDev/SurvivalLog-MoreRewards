using System;
using System.Collections.Generic;

namespace MoreRewards.Core;

/// <summary>The run's named whole numbers. In the game these are its GameCounter values, saved with the run and cleared each loop.</summary>
public interface ICounters
{
    /// <returns>0 for a name never set.</returns>
    int Get(string key);

    void Set(string key, int value);
}

/// <summary>A card the player locked, and the slot it keeps.</summary>
public readonly record struct RewardLock(int Id, int Slot);

/// <summary>How many shop buys raise today's prices, and how many were made today.</summary>
public readonly record struct ShopTally(int Buys, int Today);

/// <summary>
/// What the mod remembers for the run, kept in the run's own counters so it is saved with the run and starts over
/// with each loop: the extra locks (the game keeps its one lock itself), banned and skipped rewards, and the shop's
/// buys. A counter left at 0 means nothing is there.
/// </summary>
public sealed class RunBook
{
    /// <summary>All but one card of the most the mod offers can be locked.</summary>
    public const int MostLocks = RewardSettings.MostOptions - 1;

    private const string LockKey = "slmr.lock.";
    private const string LockSlotKey = "slmr.lockslot.";
    private const string BanKey = "slmr.ban.";
    private const string SkipKey = "slmr.skip.";
    private const string ShopBuysKey = "slmr.shop.buys";
    private const string ShopStartKey = "slmr.shop.reset";
    private const string ShopTodayKey = "slmr.shop.today";
    private const string ShopDayKey = "slmr.shop.day";

    private readonly ICounters _counters;

    public RunBook(ICounters counters) => _counters = counters;

    /// <summary>The extra locks, in the order they were made.</summary>
    public IReadOnlyList<RewardLock> Locks()
    {
        var locks = new List<RewardLock>();

        for (var k = 0; k < MostLocks; k++)
        {
            var id = _counters.Get(LockKey + k);

            if (id <= 0) break;

            locks.Add(new RewardLock(id, _counters.Get(LockSlotKey + k)));
        }

        return locks;
    }

    /// <summary>Writes the extra locks with no gaps, so <see cref="Locks"/> stops at the first empty one.</summary>
    public void SetLocks(IReadOnlyList<RewardLock> locks)
    {
        var had = Locks().Count;
        var count = Math.Min(locks.Count, MostLocks);

        for (var k = 0; k < count; k++)
        {
            _counters.Set(LockKey + k, locks[k].Id);
            _counters.Set(LockSlotKey + k, locks[k].Slot);
        }

        for (var k = count; k < had; k++)
        {
            _counters.Set(LockKey + k, 0);
            _counters.Set(LockSlotKey + k, 0);
        }
    }

    public bool IsBanned(int id) => _counters.Get(BanKey + id) > 0;

    public void Ban(int id) => _counters.Set(BanKey + id, 1);

    /// <summary>A skipped card stays away until the day ends.</summary>
    public bool IsSkipped(int id, int day) => _counters.Get(SkipKey + id) == day;

    public void Skip(int id, int day) => _counters.Set(SkipKey + id, day);

    /// <summary>Whether the player has passed on <paramref name="id"/> today or for the run.</summary>
    public bool IsUnwanted(int id, int day) => IsBanned(id) || IsSkipped(id, day);

    public ShopTally Shop(int day, ShopReset reset)
    {
        var buys = HasRiseRunOut(day, reset) ? 0 : _counters.Get(ShopBuysKey);
        var today = _counters.Get(ShopDayKey) == day ? _counters.Get(ShopTodayKey) : 0;

        return new ShopTally(buys, today);
    }

    public void RecordShopBuy(int day, ShopReset reset)
    {
        var tally = Shop(day, reset);

        if (tally.Buys == 0) _counters.Set(ShopStartKey, day);

        _counters.Set(ShopBuysKey, tally.Buys + 1);
        _counters.Set(ShopDayKey, day);
        _counters.Set(ShopTodayKey, tally.Today + 1);
    }

    /// <summary>The price rise lasts from the first buy until the day or the week it started in is over.</summary>
    private bool HasRiseRunOut(int day, ShopReset reset)
    {
        var started = _counters.Get(ShopStartKey);

        return reset switch
        {
            ShopReset.Day => day != started,
            ShopReset.Week => day - started >= 7 || day < started,
            ShopReset.Never => false,
            _ => throw new ArgumentOutOfRangeException(nameof(reset), reset, null),
        };
    }
}
