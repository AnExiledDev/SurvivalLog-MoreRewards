using System.Text.Json;
using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class ExtraRewardsTests
{
    private const int FirstId = 39010;
    private const int LastId = 39099;
    private const int TemperedGlass = 20312;

    internal static RewardFile Extra() => RewardFile.Parse(RewardFileTests.Resource("MoreRewards.extra-rewards.json"));

    [Fact]
    public void The_extra_rewards_file_reads_with_no_problems()
    {
        var file = Extra();

        Assert.Empty(file.Problems);
        Assert.NotEmpty(file.Rewards);
    }

    [Fact]
    public void Every_new_extra_reward_copies_a_game_reward_and_uses_only_its_own_id_range()
    {
        var made = Extra().Rewards.Where(reward => reward.CopyFrom != null).ToList();

        Assert.NotEmpty(made);
        Assert.All(made.SelectMany(reward => reward.Ids), id => Assert.InRange(id, FirstId, LastId));
    }

    /// <summary>An entry for a game reward only puts it in pools, so ExtraRewards never changes what the game's rewards do.</summary>
    [Fact]
    public void An_extra_entry_for_a_game_reward_only_adds_pools()
    {
        var game = Extra().Rewards.Where(reward => reward.CopyFrom == null).ToList();

        Assert.NotEmpty(game);
        Assert.All(game, reward =>
        {
            Assert.InRange(reward.Id, 30000, FirstId - 1);
            Assert.NotEmpty(reward.Pools);
            Assert.Empty(reward.Gives);
            Assert.Empty(reward.Fields);
            Assert.Null(reward.Levels);
            Assert.Null(reward.Name);
            Assert.Null(reward.Cost);
        });
    }

    [Fact]
    public void Every_extra_reward_has_english_and_chinese_text_on_every_level()
    {
        foreach (var reward in Extra().Rewards.Where(reward => reward.CopyFrom != null))
        {
            var texts = reward.Levels == null
                ? new[] { reward.Name, reward.Description }
                : reward.Levels.SelectMany(level => new[] { level.Name ?? reward.Name, level.Description ?? reward.Description });

            Assert.All(texts, text =>
            {
                Assert.NotNull(text);
                Assert.False(string.IsNullOrWhiteSpace(text.En), $"reward {reward.Id} has no English text");
                Assert.False(string.IsNullOrWhiteSpace(text.Zh), $"reward {reward.Id} has no Chinese text");
                Assert.NotEqual(text.En, text.Zh);
            });
        }
    }

    [Fact]
    public void The_supply_crate_gives_tempered_glass()
    {
        var crate = Assert.Single(Extra().Rewards, reward => reward.Gives.Any(give => give.Effect == RewardPlan.MrPrefix + RewardPlan.SupplyCrate));
        var give = crate.Gives.Single();
        var items = Assert.IsType<FieldValue.Numbers>(give.Fields.Single(set => set.Field == RewardPlan.ItemsField).Value).Values;
        var counts = Assert.IsType<FieldValue.Numbers>(give.Fields.Single(set => set.Field == RewardPlan.CountsField).Value).Values;

        Assert.Contains(TemperedGlass, items);
        Assert.Equal(items.Count, counts.Count);
        Assert.All(counts, count => Assert.True(count > 0));
    }

    [Fact]
    public void Each_more_rewards_effect_the_mod_offers_has_a_card()
    {
        var given = Extra().Rewards.SelectMany(reward => reward.Gives).Select(give => give.Effect).ToHashSet();

        foreach (var effect in new[] { RewardPlan.Investor, RewardPlan.MoreCards, RewardPlan.MoreLocks, RewardPlan.FreeRefresh, RewardPlan.BanTokens, RewardPlan.SupplyCrate, RewardPlan.Lottery })
        {
            Assert.Contains(RewardPlan.MrPrefix + effect, given);
        }
    }
}

/// <summary>
/// Builds the extra rewards against the game's tables, from the Data Dump in game-ref, so a reward that copies a
/// row the game no longer has, or runs a script it dropped, fails here after a game update instead of in a player's game.
/// </summary>
[Trait("Category", "GameBuild")]
public class ExtraRewardsGameTests
{
    [Fact]
    public void The_extra_rewards_build_against_the_game_tables_with_no_problems()
    {
        var plan = RewardPlan.Build(ExtraRewardsTests.Extra(), DumpTables.Load());

        Assert.Empty(plan.Problems);
    }

    [Fact]
    public void The_extra_and_wind_rewards_build_together_with_no_problems()
    {
        var wind = RewardFile.Parse(RewardFileTests.Resource("MoreRewards.wind-rewards.json"));
        var plan = RewardPlan.Build(ExtraRewardsTests.Extra().Over(wind), DumpTables.Load());

        Assert.Empty(plan.Problems);
    }

    /// <summary>The game's tables as the Data Dump wrote them, answering the way the mod's live tables do.</summary>
    private sealed class DumpTables : IRewardTables
    {
        private const int RewardType = 2;

        private readonly Dictionary<int, JsonElement> _talents;
        private readonly HashSet<int> _buffs;
        private readonly Dictionary<int, JsonElement> _effects;
        private readonly Dictionary<int, JsonElement> _conditions;
        private readonly Dictionary<int, int> _pools;
        private readonly List<DayEntry> _days;

        private DumpTables(Func<string, Dictionary<int, JsonElement>> table)
        {
            _talents = table("Config_Talent");
            _buffs = table("Config_Buff").Keys.ToHashSet();
            _effects = table("Config_Effect");
            _conditions = table("Config_ConditionSet");
            _pools = table("Config_RandomGroup").ToDictionary(pool => pool.Key, pool => Ints(pool.Value, "IdList").Count);
            _days = Rows("Config_DailyRandom").Select(Day).ToList();
        }

        public static DumpTables Load() => new(name => Rows(name).ToDictionary(row => row.GetProperty("ID").GetInt32()));

        public bool IsReward(int id) => _talents.TryGetValue(id, out var row) && row.GetProperty("Type").GetInt32() == RewardType;

        public bool HasTalent(int id) => _talents.ContainsKey(id);

        public bool HasBuff(int id) => _buffs.Contains(id);

        public bool HasEffect(int id) => _effects.ContainsKey(id);

        public bool HasCondition(int id) => _conditions.ContainsKey(id);

        public bool HasDay(int day) => _days.Any(entry => entry.Day == day);

        public bool HasPool(int id) => _pools.ContainsKey(id);

        public int RewardBuff(int id) => _talents.TryGetValue(id, out var row) && row.TryGetProperty("BuffID", out var buff) ? buff.GetInt32() : 0;

        public int RewardCondition(int id) =>
            _talents.TryGetValue(id, out var row) && row.TryGetProperty("ConditionSetId", out var set) && _conditions.ContainsKey(set.GetInt32()) ? set.GetInt32() : 0;

        public int BlankCondition() => _conditions.Where(entry => IsBlank(entry.Value)).Select(entry => entry.Key).DefaultIfEmpty(0).Min();

        public int? EffectRunning(string script) =>
            _effects.Where(entry => entry.Value.TryGetProperty("ConfigName", out var name) && name.GetString() == script).Select(entry => (int?)entry.Key).Min();

        public int NextTier(int id)
        {
            var next = id + 1;
            var condition = RewardCondition(next);

            return IsReward(next) && condition != 0 && Ints(_conditions[condition], "TalentNeed").Contains(id) ? next : 0;
        }

        public IReadOnlyList<DayEntry> GameDays() => _days;

        public int PoolSize(int pool) => _pools.TryGetValue(pool, out var size) ? size : 0;

        private static IEnumerable<JsonElement> Rows(string table) =>
            File.ReadLines(DataDump(table + ".jsonl")).Where(line => line.Length > 0).Select(line => JsonDocument.Parse(line).RootElement.Clone());

        private static DayEntry Day(JsonElement row)
        {
            var pools = Ints(row, "RandomPlanID");
            var amounts = Ints(row, "RandomAmount");

            return new DayEntry(row.GetProperty("Day").GetInt32(), pools.Select((pool, i) => new DayPool(pool, i < amounts.Count ? amounts[i] : 0)).ToList());
        }

        private static List<int> Ints(JsonElement row, string field) =>
            row.TryGetProperty(field, out var list) && list.ValueKind == JsonValueKind.Array ? list.EnumerateArray().Select(value => value.GetInt32()).ToList() : new List<int>();

        /// <summary>A condition set that asks for nothing: every field but its ID and Source is empty.</summary>
        private static bool IsBlank(JsonElement row) =>
            row.EnumerateObject().Where(field => field.Name is not ("ID" or "Source")).All(field => field.Value.ValueKind switch
            {
                JsonValueKind.Number => field.Value.GetDouble() == 0,
                JsonValueKind.String => field.Value.GetString()!.Length == 0,
                JsonValueKind.Array => field.Value.GetArrayLength() == 0,
                JsonValueKind.False or JsonValueKind.Null => true,
                _ => false,
            });

        private static string DataDump(string file)
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                var path = Path.Combine(dir.FullName, "game-ref", "BepInEx", "data-dump", file);

                if (File.Exists(path)) return path;
            }

            throw new FileNotFoundException($"no game-ref/BepInEx/data-dump/{file} above the tests; run tools/refresh-game-ref.sh");
        }
    }
}
