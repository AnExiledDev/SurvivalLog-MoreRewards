using System;
using System.Collections.Generic;
using System.Linq;

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

/// <summary>
/// One ban as the player made it: the card banned (<paramref name="Head"/>), every reward it kept away (the card
/// itself, and the rest of its line when the whole line was banned), and the survival points it cost, null for a ban
/// made before the mod kept the price.
/// </summary>
public sealed record BanGroup(int Head, IReadOnlyList<int> Members, int? Paid);

/// <summary>How many shop buys raise today's prices, and how many were made today.</summary>
public readonly record struct ShopTally(int Buys, int Today);

/// <summary>
/// What the mod remembers for the run, kept in the run's own counters so it is saved with the run and starts over
/// with each loop: the extra locks (the game keeps its one lock itself), the bans and what each cost, and the shop's
/// buys. A counter left at 0 means nothing is there.
/// </summary>
public sealed class RunBook
{
    /// <summary>All but one card of the most the mod offers can be locked.</summary>
    public const int MostLocks = RewardSettings.MostOptions - 1;

    private const string LockKey = "slmr.lock.";
    private const string LockSlotKey = "slmr.lockslot.";
    private const string BanKey = "slmr.ban.";
    private const string BanPaidKey = "slmr.banpaid.";
    private const string BanByKey = "slmr.banby.";
    private const string ShopBuysKey = "slmr.shop.buys";
    private const string ShopStartKey = "slmr.shop.reset";
    private const string ShopTodayKey = "slmr.shop.today";
    private const string ShopDayKey = "slmr.shop.day";

    /// <summary>The lock counters' names, made once: the screen's every rebuild reads them.</summary>
    private static readonly string[] LockKeys = Keys(LockKey, MostLocks);

    private static readonly string[] LockSlotKeys = Keys(LockSlotKey, MostLocks);

    private readonly ICounters _counters;

    /// <summary>The ban counters' names by reward, made once each: the shop asks for every reward's.</summary>
    private readonly Dictionary<int, string> _banKeys = new();

    public RunBook(ICounters counters) => _counters = counters;

    private static string[] Keys(string prefix, int count)
    {
        var keys = new string[count];

        for (var k = 0; k < count; k++) keys[k] = prefix + k;

        return keys;
    }

    private string BanKeyOf(int id)
    {
        if (!_banKeys.TryGetValue(id, out var key)) _banKeys[id] = key = BanKey + id;

        return key;
    }

    /// <summary>The extra locks, in the order they were made.</summary>
    public IReadOnlyList<RewardLock> Locks()
    {
        var locks = new List<RewardLock>();

        for (var k = 0; k < MostLocks; k++)
        {
            var id = _counters.Get(LockKeys[k]);

            if (id <= 0) break;

            locks.Add(new RewardLock(id, _counters.Get(LockSlotKeys[k])));
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
            _counters.Set(LockKeys[k], locks[k].Id);
            _counters.Set(LockSlotKeys[k], locks[k].Slot);
        }

        for (var k = count; k < had; k++)
        {
            _counters.Set(LockKeys[k], 0);
            _counters.Set(LockSlotKeys[k], 0);
        }
    }

    public bool IsBanned(int id) => _counters.Get(BanKeyOf(id)) > 0;

    /// <summary>
    /// Bans <paramref name="ids"/> as one ban made on <paramref name="head"/> for <paramref name="paid"/> points. A reward
    /// already banned stays in the ban it came from.
    /// </summary>
    /// <returns>The rewards this ban newly keeps away.</returns>
    public IReadOnlyList<int> Ban(int head, IEnumerable<int> ids, int paid)
    {
        var added = new List<int>();

        foreach (var id in ids.Prepend(head).Distinct())
        {
            if (IsBanned(id)) continue;

            _counters.Set(BanKeyOf(id), 1);
            _counters.Set(BanByKey + id, head);
            added.Add(id);
        }

        if (added.Contains(head)) _counters.Set(BanPaidKey + head, Math.Max(paid, 0) + 1);

        return added;
    }

    /// <summary>The bans among <paramref name="ids"/>, one group for each ban the player made, in the order of <paramref name="ids"/>.</summary>
    public IReadOnlyList<BanGroup> Bans(IEnumerable<int> ids)
    {
        var members = new Dictionary<int, List<int>>();
        var heads = new List<int>();

        foreach (var id in ids.Distinct())
        {
            if (!IsBanned(id)) continue;

            var head = HeadOf(id);

            if (!members.TryGetValue(head, out var group))
            {
                members[head] = group = new List<int>();
                heads.Add(head);
            }

            group.Add(id);
        }

        return heads.Select(head => new BanGroup(head, members[head], PaidFor(head))).ToList();
    }

    /// <summary>Lifts the ban made on <paramref name="head"/>, every reward in it among <paramref name="ids"/>.</summary>
    /// <returns>The ban that was lifted, null when there was none.</returns>
    public BanGroup? Unban(int head, IEnumerable<int> ids)
    {
        if (Bans(ids).FirstOrDefault(group => group.Head == head) is not { } ban) return null;

        foreach (var id in ban.Members)
        {
            _counters.Set(BanKeyOf(id), 0);
            _counters.Set(BanByKey + id, 0);
        }

        _counters.Set(BanPaidKey + head, 0);

        return ban;
    }

    /// <summary>A ban made before the mod kept who it was made on stands alone.</summary>
    private int HeadOf(int id) => _counters.Get(BanByKey + id) is var head && head > 0 ? head : id;

    private int? PaidFor(int head) => _counters.Get(BanPaidKey + head) is var paid && paid > 0 ? paid - 1 : null;

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
