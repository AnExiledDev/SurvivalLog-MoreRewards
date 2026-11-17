using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreRewards.Core;

/// <summary>Which rows the game's tables hold, as far as building a plan needs to know.</summary>
public interface IRewardTables
{
    /// <summary>True for an end-of-day reward (a Type 2 talent row whose ID is its TalentID).</summary>
    bool IsReward(int id);

    /// <summary>True for any talent row, reward or not, so a new reward never takes a talent's ID.</summary>
    bool HasTalent(int id);

    bool HasBuff(int id);

    bool HasEffect(int id);

    bool HasCondition(int id);

    bool HasPool(int id);

    /// <summary>The buff a reward gives; 0 when it gives none.</summary>
    int RewardBuff(int id);

    /// <summary>The condition set that decides when a reward can be offered; 0 when it has none.</summary>
    int RewardCondition(int id);

    /// <summary>An effect row that runs the script; null when no effect runs it.</summary>
    int? EffectRunning(string script);
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

/// <summary>One text the game looks up by key, in both languages.</summary>
public sealed record TextEntry(string Key, string En, string Zh);

/// <summary>A More Rewards effect a reward gives: whoever owns <paramref name="RewardId"/> gets <paramref name="Effect"/> with <paramref name="Fields"/>.</summary>
public sealed record MrGrant(string Effect, int RewardId, IReadOnlyList<FieldSet> Fields);

/// <summary>Everything the rewards files change in the game's tables, in the order it must be applied.</summary>
public sealed record RewardPlan(IReadOnlyList<RowEdit> Rows, IReadOnlyList<PoolEdit> Pools, IReadOnlyList<TextEntry> Texts, IReadOnlyList<MrGrant> Grants, IReadOnlyList<string> Problems)
{
    public const string GamePrefix = "game:";

    public const string MrPrefix = "mr:";

    /// <summary>The effect More Rewards runs for Wind Power: FloatValue0 is the share more power its turbines make.</summary>
    public const string WindPower = "wind-power";

    public const string WindShare = "FloatValue0";

    /// <summary>The effects More Rewards runs itself.</summary>
    public static readonly IReadOnlyList<string> MrEffects = new[] { WindPower };

    /// <summary>The script an mr give's effect runs: a marker that does nothing, so the game only records the reward as owned.</summary>
    public const string MarkerScript = "Buff_Empty";

    private const string ScriptPrefix = "Buff/";
    private const string KeyPrefix = "MoreRewards_";

    /// <summary>
    /// Works out the edits. Effects come first and buffs second, so a buff can point at an effect the file makes, then
    /// condition sets, then rewards, so a reward can point at any of them. A disabled reward changes nothing, including
    /// its own buffs and effects. Anything that cannot be built is reported and left out; the rest still applies.
    /// </summary>
    public static RewardPlan Build(RewardFile file, IRewardTables tables)
    {
        var builder = new Builder(tables);
        var enabled = Distinct(file.Rewards.Where(reward => reward.Enabled), builder.Problems);

        builder.AddRows(Table.Effect, file.Effects.Concat(enabled.SelectMany(reward => reward.Effects)));
        builder.AddRows(Table.Buff, file.Buffs.Concat(enabled.SelectMany(reward => reward.Buffs)));

        foreach (var reward in enabled) builder.AddReward(reward);

        return new RewardPlan(builder.Rows, builder.Pools, builder.Texts, builder.Grants, builder.Problems);
    }

    /// <summary>The first enabled entry for each ID; a second one is reported and left out.</summary>
    private static List<RewardEntry> Distinct(IEnumerable<RewardEntry> rewards, List<string> problems)
    {
        var seen = new HashSet<int>();
        var kept = new List<RewardEntry>();

        foreach (var reward in rewards)
        {
            if (seen.Add(reward.Id)) kept.Add(reward);
            else problems.Add($"reward {reward.Id} is listed twice; only the first one applies");
        }

        return kept;
    }

    private sealed class Builder
    {
        private readonly IRewardTables _tables;
        private readonly HashSet<int> _madeEffects = new();
        private readonly HashSet<int> _madeBuffs = new();

        public Builder(IRewardTables tables)
        {
            _tables = tables;
        }

        public List<RowEdit> Rows { get; } = new();

        public List<PoolEdit> Pools { get; } = new();

        public List<TextEntry> Texts { get; } = new();

        public List<MrGrant> Grants { get; } = new();

        public List<string> Problems { get; } = new();

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

        /// <summary>Either every row of the reward is made or, on any problem, none is.</summary>
        public void AddReward(RewardEntry reward)
        {
            var rows = new List<RowEdit>();
            var problem = Check(reward) ?? AddCondition(reward, rows) ?? AddGive(reward, rows);

            if (problem == null && PoolProblem(reward) is { } pool) problem = pool;

            if (problem != null)
            {
                Problems.Add($"reward {reward.Id} was skipped: {problem}");

                return;
            }

            foreach (var row in rows.Where(row => row.Table is Table.Effect or Table.Buff)) Made(row.Table).Add(row.Id);

            Rows.AddRange(rows);
            Rows.Add(RewardRow(reward));
            Pools.AddRange(reward.Pools.Select(pool => new PoolEdit(pool.Group, reward.Id, pool.Weight)));

            if (reward.Give is { } give && IsMr(give)) Grants.Add(new MrGrant(give.Effect.Substring(MrPrefix.Length), reward.Id, give.Fields));
        }

        private string? Check(RewardEntry reward)
        {
            if (_tables.IsReward(reward.Id)) return null;

            if (_tables.HasTalent(reward.Id)) return "that ID is a talent, not an end-of-day reward";

            if (reward.CopyFrom == null) return "the game has no such reward; give a copyFrom to make a new one";

            return _tables.IsReward(reward.CopyFrom.Value) ? null : $"copyFrom {reward.CopyFrom} is not an end-of-day reward the game has";
        }

        /// <summary>
        /// A reward's needs live in its condition set. A new reward gets its own, copied from the one it copies, so
        /// changing its needs never changes the other reward's.
        /// </summary>
        private string? AddCondition(RewardEntry reward, List<RowEdit> rows)
        {
            var source = Source(reward);
            var sourceCondition = _tables.RewardCondition(source);
            var fields = new List<FieldSet>();

            if (reward.Needs is { } needs) fields.Add(new FieldSet("TalentNeed", new FieldValue.Numbers(needs)));

            if (reward.Furniture is { } furniture) fields.Add(new FieldSet("NeedFurnitureObtainable", new FieldValue.Numbers(furniture)));

            if (IsNew(reward))
            {
                if (sourceCondition == 0)
                {
                    return fields.Count == 0 ? null : $"reward {source} has no condition set to copy, so \"needs\" and \"furniture\" cannot be set";
                }

                if (_tables.HasCondition(reward.Id)) return $"it needs condition set {reward.Id}, which the game already uses";

                rows.Add(new RowEdit(Table.Condition, reward.Id, sourceCondition, Prepend(fields, Whole("ID", reward.Id))));

                return null;
            }

            if (fields.Count == 0) return null;

            if (sourceCondition == 0) return "it has no condition set, so \"needs\" and \"furniture\" cannot be set";

            rows.Add(new RowEdit(Table.Condition, sourceCondition, null, fields));

            return null;
        }

        /// <summary>
        /// A give makes the reward's own buff (ID * 10, as the game numbers them) and effect: the buff copied from the
        /// reward's buff so it shows the same way, the effect copied from a row that runs the script.
        /// </summary>
        private string? AddGive(RewardEntry reward, List<RowEdit> rows)
        {
            if (reward.Give is not { } give) return null;

            var isMr = IsMr(give);

            if (!isMr && !give.Effect.StartsWith(GamePrefix, StringComparison.Ordinal)) return $"\"give\" is \"{give.Effect}\"; only game effects (\"{GamePrefix}...\") and More Rewards effects (\"{MrPrefix}...\") can be given";

            if (isMr && !MrEffects.Contains(give.Effect.Substring(MrPrefix.Length))) return $"\"give\" is \"{give.Effect}\", which More Rewards does not have; it has {string.Join(", ", MrEffects.Select(name => MrPrefix + name))}";

            var script = isMr ? MarkerScript : give.Effect.Substring(GamePrefix.Length);

            if (_tables.EffectRunning(ScriptPrefix + script) is not { } template) return $"no effect in the game runs \"{GamePrefix}{script}\"";

            var buff = BuffOf(reward.Id);
            var effect = EffectOf(buff);
            var sourceBuff = _tables.RewardBuff(Source(reward));
            var isOwnBuff = !IsNew(reward) && sourceBuff == buff;

            if (sourceBuff == 0) return $"reward {Source(reward)} gives no buff to copy";

            if (_madeBuffs.Contains(buff) || _madeEffects.Contains(effect) || (!isOwnBuff && (_tables.HasBuff(buff) || _tables.HasEffect(effect)))) return $"it needs buff {buff} and effect {effect}, and one of them is already in use";

            var effectFields = new List<FieldSet> { Whole("ID", effect), Text("ConfigName", ScriptPrefix + script) };
            var buffFields = new List<FieldSet> { Whole("ID", buff), Whole("EffectConfigId", effect) };

            // An mr give's fields are read by More Rewards, not by the marker script, so the game row keeps none of them.
            if (!isMr) effectFields.AddRange(give.Fields);

            AddText(buffFields, "Name", "Buff", buff, reward.Name);
            AddText(buffFields, "Des", "Buff", buff, reward.Description);
            rows.Add(new RowEdit(Table.Effect, effect, template, effectFields));
            rows.Add(new RowEdit(Table.Buff, buff, sourceBuff, buffFields));

            return null;
        }

        private string? PoolProblem(RewardEntry reward)
        {
            var missing = reward.Pools.Where(pool => !_tables.HasPool(pool.Group)).Select(pool => pool.Group).ToList();

            return missing.Count == 0 ? null : $"the game has no pool {string.Join(", ", missing)}";
        }

        private RowEdit RewardRow(RewardEntry reward)
        {
            var fields = new List<FieldSet>();

            if (IsNew(reward))
            {
                fields.Add(Whole("ID", reward.Id));
                fields.Add(Whole("TalentID", reward.Id));
                fields.Add(Whole("Lv", 1));
                fields.Add(Whole("Available", 1));

                if (_tables.RewardCondition(Source(reward)) != 0) fields.Add(Whole("ConditionSetId", reward.Id));
            }

            fields.AddRange(reward.Fields);

            if (reward.Cost is { } cost) fields.Add(Whole("LvUpCost", cost));

            if (reward.Give != null) fields.Add(Whole("BuffID", BuffOf(reward.Id)));

            AddText(fields, "Name", "Reward", reward.Id, reward.Name);
            AddText(fields, "Dec", "Reward", reward.Id, reward.Description);

            return new RowEdit(Table.Reward, reward.Id, IsNew(reward) ? reward.CopyFrom : null, fields);
        }

        /// <summary>
        /// Text gets keys of its own, so a copied row never changes the text of the row it came from. The _Local field
        /// holds the Chinese, as the game's rows do.
        /// </summary>
        private void AddText(List<FieldSet> fields, string field, string table, int id, LocalText? text)
        {
            if (text == null) return;

            var key = $"{KeyPrefix}{table}_{field}_{id}";

            fields.Add(Text(field, key));
            fields.Add(Text(field + "_Local", text.Zh));
            Texts.Add(new TextEntry(key, text.En, text.Zh));
        }

        private bool IsNew(RewardEntry reward) => !_tables.IsReward(reward.Id);

        private int Source(RewardEntry reward) => IsNew(reward) ? reward.CopyFrom ?? 0 : reward.Id;

        private bool Exists(Table table, int id) =>
            Made(table).Contains(id) || (table == Table.Effect ? _tables.HasEffect(id) : _tables.HasBuff(id));

        private HashSet<int> Made(Table table) => table == Table.Effect ? _madeEffects : _madeBuffs;
    }

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
