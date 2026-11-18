using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreRewards.Core;

/// <summary>Which rows the game's tables hold, as far as building a plan needs to know.</summary>
public interface IRewardTables
{
    /// <summary>
    /// True for an end-of-day reward: a Type 2 talent row. The game knows these by row ID alone
    /// (SurvivalPlanningComponent.HasActivated), so the row's TalentID does not matter; for 75 of them it is ID / 10.
    /// </summary>
    bool IsReward(int id);

    /// <summary>True for any talent row, reward or not, so a new reward never takes a talent's ID.</summary>
    bool HasTalent(int id);

    bool HasBuff(int id);

    bool HasEffect(int id);

    bool HasCondition(int id);

    /// <summary>A condition set that asks for nothing, to copy for a reward that has none; 0 when the game has none.</summary>
    int BlankCondition();

    /// <summary>Whether the game's day table has a row for the day.</summary>
    bool HasDay(int day);

    bool HasPool(int id);

    /// <summary>The buff a reward gives; 0 when it gives none.</summary>
    int RewardBuff(int id);

    /// <summary>The condition set that decides when a reward can be offered; 0 when it has none.</summary>
    int RewardCondition(int id);

    /// <summary>An effect row that runs the script; null when no effect runs it.</summary>
    int? EffectRunning(string script);

    /// <summary>The game reward one level up from <paramref name="id"/> (id + 1, needing id); 0 when there is none.</summary>
    int NextTier(int id);

    /// <summary>The game's day table: each day's pools and how many rewards it draws from each, in draw order.</summary>
    IReadOnlyList<DayEntry> GameDays();

    /// <summary>How many rewards the game's pool holds; 0 when there is no such pool.</summary>
    int PoolSize(int pool);
}

public enum Table
{
    Effect,
    Buff,
    Condition,
    Reward,
}

/// <summary>One row to change. With <paramref name="CopyFrom"/> set, the row is first made as a copy of that row of the same table.</summary>
public sealed record RowEdit(Table Table, int Id, int? CopyFrom, IReadOnlyList<FieldSet> Fields);

/// <summary>Puts <paramref name="Reward"/> in pool <paramref name="Pool"/> with <paramref name="Weight"/>, or changes its weight there.</summary>
public sealed record PoolEdit(int Pool, int Reward, int Weight);

/// <summary>A day's new pools and amounts, in draw order. A pool at 0 is left out.</summary>
public sealed record DayEdit(int Day, IReadOnlyList<int> Pools, IReadOnlyList<int> Amounts);

/// <summary>One text the game looks up by key, in both languages.</summary>
public sealed record TextEntry(string Key, string En, string Zh);

/// <summary>A More Rewards effect a reward gives: whoever owns <paramref name="RewardId"/> gets <paramref name="Effect"/> with <paramref name="Fields"/>.</summary>
public sealed record MrGrant(string Effect, int RewardId, IReadOnlyList<FieldSet> Fields);

/// <summary>Whoever gets <paramref name="LevelBuff"/> also gets <paramref name="Buffs"/>: a reward's second give onwards.</summary>
public sealed record Companion(int LevelBuff, IReadOnlyList<int> Buffs);

/// <summary>
/// Everything the rewards files change in the game's tables, in the order it must be applied, and the rules the mod
/// keeps itself while a run goes on.
/// </summary>
/// <param name="LastDays">Each level of a reward with a "toDay": the last day it can be offered.</param>
/// <param name="AlwaysOffered">Each level of every "alwaysOffered" reward, in file order.</param>
public sealed record RewardPlan(
    IReadOnlyList<RowEdit> Rows,
    IReadOnlyList<PoolEdit> Pools,
    IReadOnlyList<TextEntry> Texts,
    IReadOnlyList<MrGrant> Grants,
    IReadOnlyList<Companion> Companions,
    IReadOnlyList<DayEdit> Days,
    IReadOnlyDictionary<int, int> LastDays,
    IReadOnlyList<int> AlwaysOffered,
    IReadOnlyList<string> Problems)
{
    public const string GamePrefix = "game:";

    public const string MrPrefix = "mr:";

    /// <summary>The effect More Rewards runs for Wind Power: FloatValue0 is the share more power its turbines make.</summary>
    public const string WindPower = "wind-power";

    public const string WindShare = "FloatValue0";

    /// <summary>Raises the interest: IntValue0 is percent more on the rate, IntValue1 is more on the most a day pays.</summary>
    public const string Investor = "investor";

    /// <summary>IntValue0 more cards on the rewards screen.</summary>
    public const string MoreCards = "more-cards";

    /// <summary>IntValue0 more locks on the rewards screen.</summary>
    public const string MoreLocks = "more-locks";

    /// <summary>IntValue0 free refreshes each day.</summary>
    public const string FreeRefresh = "free-refresh";

    /// <summary>IntValue0 free bans, used up one at a time.</summary>
    public const string BanTokens = "ban-tokens";

    /// <summary>Puts items in the bag when bought: Items is the item IDs and Counts how many of each (1 when left out).</summary>
    public const string SupplyCrate = "supply-crate";

    /// <summary>Pays out a random number of survival points when bought, from IntValue0 to IntValue1.</summary>
    public const string Lottery = "lottery";

    public const string CountField = "IntValue0";
    public const string RateField = "IntValue0";
    public const string CapField = "IntValue1";
    public const string LeastField = "IntValue0";
    public const string MostField = "IntValue1";
    public const string ItemsField = "Items";
    public const string CountsField = "Counts";

    /// <summary>The effects More Rewards runs itself.</summary>
    public static readonly IReadOnlyList<string> MrEffects = new[] { WindPower, Investor, MoreCards, MoreLocks, FreeRefresh, BanTokens, SupplyCrate, Lottery };

    /// <summary>The script an mr give's effect runs: a marker that does nothing, so the game only records the reward as owned.</summary>
    public const string MarkerScript = "Buff_Empty";

    private const string ScriptPrefix = "Buff/";
    private const string KeyPrefix = "MoreRewards_";

    /// <summary>
    /// Works out the edits. Effects come first and buffs second, so a buff can point at an effect the file makes, then
    /// condition sets, then rewards, so a reward can point at any of them. A disabled reward changes nothing, including
    /// its own buffs and effects. Anything that cannot be built is reported and left out; the rest still applies.
    /// The "anyDay" rewards go in last, once the file's days are known, since those days decide which pools they go in.
    /// </summary>
    public static RewardPlan Build(RewardFile file, IRewardTables tables)
    {
        var builder = new Builder(tables);
        var enabled = Distinct(file.Rewards.Where(reward => reward.Enabled), builder.Problems);

        builder.AddRows(Table.Effect, file.Effects.Concat(enabled.SelectMany(reward => reward.Effects)));
        builder.AddRows(Table.Buff, file.Buffs.Concat(enabled.SelectMany(reward => reward.Buffs)));

        foreach (var reward in enabled) builder.AddReward(reward);

        foreach (var day in file.Days) builder.AddDay(day);

        builder.AddAnyDay(MainPools.Of(MainPools.Days(tables.GameDays(), builder.Days), tables.PoolSize));

        return new RewardPlan(builder.Rows, builder.Pools, builder.Texts, builder.Grants, builder.Companions, builder.Days, builder.LastDays, builder.AlwaysOffered, builder.Problems);
    }

    /// <summary>The first enabled entry to use each ID; a later one that uses any of them, at any level, is reported and left out.</summary>
    private static List<RewardEntry> Distinct(IEnumerable<RewardEntry> rewards, List<string> problems)
    {
        var seen = new HashSet<int>();
        var kept = new List<RewardEntry>();

        foreach (var reward in rewards)
        {
            var taken = reward.Ids.FirstOrDefault(seen.Contains);

            if (taken != 0)
            {
                problems.Add($"reward {reward.Id} was skipped: reward {taken} is already used by an entry above it");

                continue;
            }

            seen.UnionWith(reward.Ids);
            kept.Add(reward);
        }

        return kept;
    }

    /// <summary>
    /// One level of an entry, worked out: the row it is, whether that row is new, and the row it copies (for a row the
    /// game has, the row itself).
    /// </summary>
    private sealed record Level(
        int Index,
        int Id,
        bool IsNew,
        int Source,
        IReadOnlyList<int>? Needs,
        int? FromDay,
        int? Cost,
        LocalText? Name,
        LocalText? Description,
        IReadOnlyList<PoolEntry> Pools,
        IReadOnlyList<FieldSet> Fields);

    /// <summary>What one entry adds, kept apart until every level of it has been checked.</summary>
    private sealed class Pending
    {
        public List<RowEdit> Rows { get; } = new();

        public List<PoolEdit> Pools { get; } = new();

        public List<TextEntry> Texts { get; } = new();

        public List<MrGrant> Grants { get; } = new();

        public List<Companion> Companions { get; } = new();

        /// <summary>The levels that get a condition set of their own, with the same ID as the level.</summary>
        public HashSet<int> OwnConditions { get; } = new();
    }

    /// <summary>A level of an "anyDay" reward, with the pools the file already puts it in, which keep their own weight.</summary>
    private sealed record AnyDayLevel(int Id, int Weight, IReadOnlyList<PoolEntry> Pools);

    private sealed class Builder
    {
        private readonly IRewardTables _tables;
        private readonly HashSet<int> _madeEffects = new();
        private readonly HashSet<int> _madeBuffs = new();
        private readonly List<AnyDayLevel> _anyDay = new();

        public Builder(IRewardTables tables)
        {
            _tables = tables;
        }

        public List<RowEdit> Rows { get; } = new();

        public List<PoolEdit> Pools { get; } = new();

        public List<TextEntry> Texts { get; } = new();

        public List<MrGrant> Grants { get; } = new();

        public List<Companion> Companions { get; } = new();

        public List<string> Problems { get; } = new();

        public List<DayEdit> Days { get; } = new();

        public Dictionary<int, int> LastDays { get; } = new();

        public List<int> AlwaysOffered { get; } = new();

        /// <summary>
        /// Each level of every "anyDay" reward goes in each main pool at its "anyDay" weight. A pool the level's own
        /// "pools" already name keeps the weight given there.
        /// </summary>
        public void AddAnyDay(IReadOnlyList<int> mainPools)
        {
            foreach (var level in _anyDay)
            {
                foreach (var pool in mainPools.Where(pool => level.Pools.All(own => own.Group != pool))) Pools.Add(new PoolEdit(pool, level.Id, level.Weight));
            }
        }

        /// <summary>A day the file names twice keeps the first; one the game has no row for, or with a pool it lacks, is left out.</summary>
        public void AddDay(DayEntry day)
        {
            var missing = day.Pools.Where(pool => !_tables.HasPool(pool.Pool)).Select(pool => pool.Pool).ToList();
            var problem = Days.Any(edit => edit.Day == day.Day) ? "is in the file twice; the first one is used"
                : !_tables.HasDay(day.Day) ? "is not in the game's day table"
                : missing.Count > 0 ? $"uses pool {string.Join(", ", missing)}, which the game does not have"
                : null;

            if (problem != null)
            {
                Problems.Add($"day {day.Day} {problem}");

                return;
            }

            var kept = day.Pools.Where(pool => pool.Amount > 0).ToList();

            Days.Add(new DayEdit(day.Day, kept.Select(pool => pool.Pool).ToList(), kept.Select(pool => pool.Amount).ToList()));
        }

        public void AddRows(Table table, IEnumerable<RowEntry> entries)
        {
            foreach (var entry in entries)
            {
                var name = table == Table.Effect ? "effect" : "buff";

                if (entry.CopyFrom is { } source && !Exists(table, source))
                {
                    Problems.Add($"{name} {entry.Id} was skipped: copyFrom {source} is not a {name} the game has");

                    continue;
                }

                if (entry.CopyFrom == null && !Exists(table, entry.Id))
                {
                    Problems.Add($"{name} {entry.Id} was skipped: the game has no such {name}; give a copyFrom to make a new one");

                    continue;
                }

                var fields = entry.CopyFrom == null ? entry.Fields : Prepend(entry.Fields, Whole("ID", entry.Id));

                Rows.Add(new RowEdit(table, entry.Id, entry.CopyFrom, fields));
                Made(table).Add(entry.Id);
            }
        }

        /// <summary>Either every row of every level of the reward is made or, on any problem, none is.</summary>
        public void AddReward(RewardEntry reward)
        {
            var pending = new Pending();
            var problem = Levels(reward, out var levels);

            foreach (var level in levels)
            {
                problem ??= AddCondition(reward, level, pending) ?? AddGives(reward, level, pending) ?? AddLayers(reward, level, pending) ?? PoolProblem(level);

                if (problem != null) break;

                AddLevel(reward, level, pending);
            }

            if (problem != null)
            {
                Problems.Add($"reward {reward.Id} was skipped: {problem}");

                return;
            }

            SwitchOffLevelsAbove(reward, levels, pending);

            foreach (var row in pending.Rows.Where(row => row.Table is Table.Effect or Table.Buff)) Made(row.Table).Add(row.Id);

            Rows.AddRange(pending.Rows);
            Pools.AddRange(pending.Pools);
            Texts.AddRange(pending.Texts);
            Grants.AddRange(pending.Grants);
            Companions.AddRange(pending.Companions);
            AddDayRules(reward, levels);
        }

        /// <summary>
        /// The rules that apply to every level: the last day, the main pools and the card it always gets. A last day
        /// before the first day is kept, and reported, since the reward is then never offered.
        /// </summary>
        private void AddDayRules(RewardEntry reward, List<Level> levels)
        {
            if (reward.ToDay is int last and > 0 && reward.FromDay is int first && last < first) Problems.Add($"reward {reward.Id} has a toDay of {last}, before its fromDay of {first}, so it is never offered");

            foreach (var level in levels)
            {
                if (reward.ToDay is int lastDay and > 0) LastDays[level.Id] = lastDay;

                if (reward.AnyDay is int weight and > 0) _anyDay.Add(new AnyDayLevel(level.Id, weight, level.Pools));

                if (reward.AlwaysOffered) AlwaysOffered.Add(level.Id);
            }
        }

        /// <summary>
        /// Level n is reward ID + n. A level the game already has as the next level of the one below is changed in place,
        /// as Sunlight Collection II is to I. Any other level is a new row, copied from the same level of the reward it
        /// copies (that reward's top level when it has fewer), and needs the level below it.
        /// </summary>
        private string? Levels(RewardEntry reward, out List<Level> levels)
        {
            levels = new List<Level>();

            var chain = Chain(reward.CopyFrom ?? reward.Id);

            for (var i = 0; i < reward.LevelCount; i++)
            {
                var id = reward.Id + i;
                var level = reward.Levels?[i];
                var isGameLevel = _tables.IsReward(id) && (i == 0 || (!levels[i - 1].IsNew && _tables.NextTier(id - 1) == id));
                var where = i == 0 ? "" : $"level {i + 1} (reward {id}) ";

                if (!isGameLevel && _tables.IsReward(id)) return $"{where}is a reward the game has, but not the next level of {id - 1}";

                if (!isGameLevel && _tables.HasTalent(id)) return $"{where}that ID is a talent, not an end-of-day reward";

                if (!isGameLevel && i == 0 && reward.CopyFrom == null) return "the game has no such reward; give a copyFrom to make a new one";

                if (!isGameLevel && chain.Count == 0) return $"copyFrom {reward.CopyFrom} is not an end-of-day reward the game has";

                levels.Add(new Level(
                    i,
                    id,
                    !isGameLevel,
                    isGameLevel ? id : chain[Math.Min(i, chain.Count - 1)],
                    i == 0 ? reward.Needs : isGameLevel ? null : new[] { id - 1 },
                    i == 0 ? reward.FromDay : null,
                    level?.Cost ?? reward.Cost,
                    level?.Name ?? Numbered(reward.Name, i, reward.LevelCount),
                    level?.Description ?? reward.Description,
                    level?.Pools ?? reward.Pools,
                    level == null ? reward.Fields : reward.Fields.Concat(level.Fields).ToList()));
            }

            return null;
        }

        /// <summary>
        /// A game reward whose "levels" list is shorter than the game's chain switches the game's higher levels off, so they
        /// are never offered. A reward with no "levels" list leaves them alone.
        /// </summary>
        private void SwitchOffLevelsAbove(RewardEntry reward, List<Level> levels, Pending pending)
        {
            if (reward.Levels == null || reward.CopyFrom != null || levels.Count == 0 || levels[^1].IsNew) return;

            var top = levels[^1].Id;

            foreach (var id in Chain(reward.Id).Where(id => id > top)) pending.Rows.Add(new RowEdit(Table.Reward, id, null, new[] { Whole("Available", 0) }));
        }

        /// <summary>The reward and the game's levels above it, as far as they go; empty when it is not a reward.</summary>
        private List<int> Chain(int first)
        {
            var chain = new List<int>();

            for (var id = first; id != 0 && _tables.IsReward(id) && chain.Count < RewardEntry.MaxLevels; id = _tables.NextTier(id)) chain.Add(id);

            return chain;
        }

        /// <summary>
        /// A reward's needs live in its condition set. A new level gets its own, copied from the one its source uses, so
        /// changing its needs never changes the other reward's. A reward with no condition set gets one copied from a
        /// set that asks for nothing.
        /// </summary>
        private string? AddCondition(RewardEntry reward, Level level, Pending pending)
        {
            var sourceCondition = _tables.RewardCondition(level.Source);
            var fields = new List<FieldSet>();

            if (level.Needs is { } needs) fields.Add(new FieldSet("TalentNeed", new FieldValue.Numbers(needs)));

            if (reward.Furniture is { } furniture) fields.Add(new FieldSet("NeedFurnitureObtainable", new FieldValue.Numbers(furniture)));

            if (level.FromDay is { } day) fields.Add(Whole("SurvivalToDay", Math.Max(0, day)));

            if (!level.IsNew && sourceCondition != 0)
            {
                if (fields.Count > 0) pending.Rows.Add(new RowEdit(Table.Condition, sourceCondition, null, fields));

                return null;
            }

            if (sourceCondition == 0 && fields.Count == 0) return null;

            var copy = sourceCondition != 0 ? sourceCondition : _tables.BlankCondition();

            if (copy == 0) return "the game has no empty condition set to copy, so \"needs\", \"furniture\" and \"fromDay\" cannot be set";

            if (_tables.HasCondition(level.Id)) return $"it needs condition set {level.Id}, which the game already uses";

            var own = Prepend(fields, Whole("ID", level.Id));

            if (sourceCondition == 0) own.Add(Text("Source", $"{KeyPrefix}{level.Id}"));

            pending.Rows.Add(new RowEdit(Table.Condition, level.Id, copy, own));
            pending.OwnConditions.Add(level.Id);

            return null;
        }

        /// <summary>
        /// How far a reward the player can buy again stacks. It is a number on the level's buff, so a level that makes no
        /// buff of its own can set it only on a reward the game has.
        /// </summary>
        private string? AddLayers(RewardEntry reward, Level level, Pending pending)
        {
            if (reward.Layers is not { } layers || reward.Gives.Count > 0) return null;

            if (level.IsNew) return "\"layers\" on a new reward needs \"gives\", so it does not change the buff of the reward it copies";

            var buff = _tables.RewardBuff(level.Id);

            if (buff == 0) return $"reward {level.Id} gives no buff, so \"layers\" cannot be set";

            pending.Rows.Add(new RowEdit(Table.Buff, buff, null, new[] { Whole("MaxLayerCount", Math.Max(1, layers)) }));

            return null;
        }

        /// <summary>
        /// The first give makes the level's own buff (ID * 10, as the game numbers them) and effect: the buff copied from
        /// the source's buff so it shows the same way, the effect copied from a row that runs the script. Each further
        /// give makes a hidden buff (ID * 10 + n) that the player gets along with the first.
        /// </summary>
        private string? AddGives(RewardEntry reward, Level level, Pending pending)
        {
            if (reward.Gives.Count == 0) return null;

            var sourceBuff = _tables.RewardBuff(level.Source);
            var riders = new List<int>();

            if (sourceBuff == 0) return $"reward {level.Source} gives no buff to copy";

            for (var k = 0; k < reward.Gives.Count; k++)
            {
                var give = reward.Gives[k];
                var what = reward.Gives.Count == 1 ? "\"give\"" : $"give {k + 1}";
                var isMr = IsMr(give);

                if (!isMr && !give.Effect.StartsWith(GamePrefix, StringComparison.Ordinal)) return $"{what} is \"{give.Effect}\"; only game effects (\"{GamePrefix}...\") and More Rewards effects (\"{MrPrefix}...\") can be given";

                if (isMr && !MrEffects.Contains(give.Effect.Substring(MrPrefix.Length))) return $"{what} is \"{give.Effect}\", which More Rewards does not have; it has {string.Join(", ", MrEffects.Select(name => MrPrefix + name))}";

                var script = isMr ? MarkerScript : give.Effect.Substring(GamePrefix.Length);

                if (_tables.EffectRunning(ScriptPrefix + script) is not { } template) return $"no effect in the game runs \"{GamePrefix}{script}\"";

                var buff = BuffOf(level.Id) + k;
                var effect = EffectOf(buff);
                var isOwnBuff = k == 0 && !level.IsNew && sourceBuff == buff;

                if (_madeBuffs.Contains(buff) || _madeEffects.Contains(effect) || (!isOwnBuff && (_tables.HasBuff(buff) || _tables.HasEffect(effect)))) return $"it needs buff {buff} and effect {effect}, and one of them is already in use";

                var effectFields = new List<FieldSet> { Whole("ID", effect), Text("ConfigName", ScriptPrefix + script) };
                var buffFields = new List<FieldSet> { Whole("ID", buff), Whole("EffectConfigId", effect) };

                // An mr give's fields are read by More Rewards, not by the marker script, so the game row keeps none of them.
                if (isMr) pending.Grants.Add(new MrGrant(give.Effect.Substring(MrPrefix.Length), level.Id, give.FieldsAt(level.Index)));
                else effectFields.AddRange(give.FieldsAt(level.Index));

                if (k == 0)
                {
                    if (reward.Layers is { } layers) buffFields.Add(Whole("MaxLayerCount", Math.Max(1, layers)));

                    AddText(buffFields, "Name", "Buff", buff, level.Name, pending);
                    AddText(buffFields, "Des", "Buff", buff, level.Description, pending);
                }
                else
                {
                    buffFields.Add(new FieldSet("IsHide", new FieldValue.Flag(true)));
                    riders.Add(buff);
                }

                pending.Rows.Add(new RowEdit(Table.Effect, effect, template, effectFields));
                pending.Rows.Add(new RowEdit(Table.Buff, buff, sourceBuff, buffFields));
            }

            if (riders.Count > 0) pending.Companions.Add(new Companion(BuffOf(level.Id), riders));

            return null;
        }

        private string? PoolProblem(Level level)
        {
            var missing = level.Pools.Where(pool => !_tables.HasPool(pool.Group)).Select(pool => pool.Group).ToList();

            return missing.Count == 0 ? null : $"the game has no pool {string.Join(", ", missing)}";
        }

        private void AddLevel(RewardEntry reward, Level level, Pending pending)
        {
            var fields = new List<FieldSet>();

            if (level.IsNew)
            {
                fields.Add(Whole("ID", level.Id));
                fields.Add(Whole("TalentID", level.Id));
                fields.Add(Whole("Lv", 1));
                fields.Add(Whole("Available", 1));

            }

            if (pending.OwnConditions.Contains(level.Id)) fields.Add(Whole("ConditionSetId", level.Id));

            fields.AddRange(level.Fields);

            if (level.Cost is { } cost) fields.Add(Whole("LvUpCost", cost));

            if (reward.Gives.Count > 0) fields.Add(Whole("BuffID", BuffOf(level.Id)));

            AddText(fields, "Name", "Reward", level.Id, level.Name, pending);
            AddText(fields, "Dec", "Reward", level.Id, level.Description, pending);
            pending.Rows.Add(new RowEdit(Table.Reward, level.Id, level.IsNew ? level.Source : null, fields));
            pending.Pools.AddRange(level.Pools.Select(pool => new PoolEdit(pool.Group, level.Id, pool.Weight)));
        }

        /// <summary>
        /// Text gets keys of its own, so a copied row never changes the text of the row it came from. The _Local field
        /// holds the Chinese, as the game's rows do.
        /// </summary>
        private static void AddText(List<FieldSet> fields, string field, string table, int id, LocalText? text, Pending pending)
        {
            if (text == null) return;

            var key = $"{KeyPrefix}{table}_{field}_{id}";

            fields.Add(Text(field, key));
            fields.Add(Text(field + "_Local", text.Zh));
            pending.Texts.Add(new TextEntry(key, text.En, text.Zh));
        }

        private bool Exists(Table table, int id) =>
            Made(table).Contains(id) || (table == Table.Effect ? _tables.HasEffect(id) : _tables.HasBuff(id));

        private HashSet<int> Made(Table table) => table == Table.Effect ? _madeEffects : _madeBuffs;
    }

    private static readonly string[] Roman = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };

    /// <summary>"Wind Collection" at level 2 of 3 is "Wind Collection II" and "... Lv2", as the game names its levels.</summary>
    private static LocalText? Numbered(LocalText? name, int index, int count) =>
        name == null || count == 1 ? name : new LocalText($"{name.En} {Roman[index]}", $"{name.Zh} Lv{index + 1}");

    private static bool IsMr(GiveEntry give) => give.Effect.StartsWith(MrPrefix, StringComparison.Ordinal);

    /// <summary>The game numbers a reward's buff as its ID times 10.</summary>
    public static int BuffOf(int rewardId) => rewardId * 10;

    /// <summary>The game numbers a reward buff's effect 200000000 + the buff's ID.</summary>
    public static int EffectOf(int buff) => 200000000 + buff;

    private static FieldSet Whole(string field, int value) => new(field, new FieldValue.Number(value));

    private static FieldSet Text(string field, string value) => new(field, new FieldValue.Text(value));

    private static List<FieldSet> Prepend(IReadOnlyList<FieldSet> fields, FieldSet first)
    {
        var all = new List<FieldSet> { first };

        all.AddRange(fields);

        return all;
    }
}
