using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class RewardPlanTests
{
    /// <summary>A small slice of the game's tables: Sunlight Collection I and II, their buffs, effects, needs and pools.</summary>
    private sealed class FakeTables : IRewardTables
    {
        public HashSet<int> Rewards { get; } = new() { 31271, 31272 };
        public HashSet<int> Talents { get; } = new() { 31271, 31272, 50001 };
        public HashSet<int> Buffs { get; } = new() { 312710, 312720 };
        public HashSet<int> Effects { get; } = new() { 200312710, 200312720, 200100910 };
        public HashSet<int> Conditions { get; } = new() { 31271, 31272 };
        public HashSet<int> PoolIds { get; } = new() { 1013, 1020 };
        public Dictionary<string, int> Scripts { get; } = new() { ["Buff/AE_PowerSolarEfficiency"] = 200312710, ["Buff/Buff_Empty"] = 200100910 };

        public bool IsReward(int id) => Rewards.Contains(id);
        public bool HasTalent(int id) => Talents.Contains(id);
        public bool HasBuff(int id) => Buffs.Contains(id);
        public bool HasEffect(int id) => Effects.Contains(id);
        public bool HasCondition(int id) => Conditions.Contains(id);
        public bool HasPool(int id) => PoolIds.Contains(id);
        public int RewardBuff(int id) => Rewards.Contains(id) ? id * 10 : 0;
        public int RewardCondition(int id) => Conditions.Contains(id) ? id : 0;
        public int? EffectRunning(string script) => Scripts.TryGetValue(script, out var id) ? id : null;
    }

    private static RewardPlan Plan(string json, FakeTables? tables = null) => RewardPlan.Build(RewardFile.Parse(json), tables ?? new FakeTables());

    private static FieldValue? Value(RowEdit row, string field) => row.Fields.LastOrDefault(set => set.Field == field)?.Value;

    private static RowEdit Row(RewardPlan plan, Table table, int id) => Assert.Single(plan.Rows, row => row.Table == table && row.Id == id);

    [Fact]
    public void A_new_reward_is_a_copy_with_its_own_condition_buff_effect_and_text()
    {
        var plan = Plan("""
            { "rewards": [ { "id": 39001, "copyFrom": 31271, "name": { "en": "Wind I", "zh": "风 1" }, "cost": 10,
              "needs": [31271], "pools": { "1013": 1, "1020": 3 },
              "give": { "effect": "game:AE_PowerSolarEfficiency", "fields": { "FloatValue0": 0.15 } } } ] }
            """);

        Assert.Empty(plan.Problems);

        var reward = Row(plan, Table.Reward, 39001);
        Assert.Equal(31271, reward.CopyFrom);
        Assert.Equal(new FieldValue.Number(39001), Value(reward, "TalentID"));
        Assert.Equal(new FieldValue.Number(10), Value(reward, "LvUpCost"));
        Assert.Equal(new FieldValue.Number(390010), Value(reward, "BuffID"));
        Assert.Equal(new FieldValue.Number(39001), Value(reward, "ConditionSetId"));
        Assert.Equal(new FieldValue.Text("风 1"), Value(reward, "Name_Local"));

        var condition = Row(plan, Table.Condition, 39001);
        Assert.Equal(31271, condition.CopyFrom);
        Assert.Equal(new[] { 31271 }, Assert.IsType<FieldValue.Numbers>(Value(condition, "TalentNeed")).Values);

        var buff = Row(plan, Table.Buff, 390010);
        Assert.Equal(312710, buff.CopyFrom);
        Assert.Equal(new FieldValue.Number(200390010), Value(buff, "EffectConfigId"));

        var effect = Row(plan, Table.Effect, 200390010);
        Assert.Equal(200312710, effect.CopyFrom);
        Assert.Equal(new FieldValue.Number(0.15), Value(effect, "FloatValue0"));

        Assert.Equal(new[] { new PoolEdit(1013, 39001, 1), new PoolEdit(1020, 39001, 3) }, plan.Pools);
        Assert.Contains(plan.Texts, text => text.En == "Wind I" && text.Zh == "风 1");
        Assert.Empty(plan.Grants);
    }

    [Fact]
    public void A_copied_reward_never_changes_the_text_or_needs_of_the_reward_it_copies()
    {
        var plan = Plan("""{ "rewards": [ { "id": 39001, "copyFrom": 31271, "name": "Wind", "needs": [] } ] }""");

        Assert.DoesNotContain(plan.Rows, row => row.Id == 31271);
        Assert.All(plan.Texts, text => Assert.Contains("39001", text.Key));
    }

    [Fact]
    public void An_mr_give_runs_the_marker_effect_and_hands_its_share_to_the_mod()
    {
        var plan = Plan("""{ "rewards": [ { "id": 39001, "copyFrom": 31271, "give": { "effect": "mr:wind-power", "fields": { "FloatValue0": 0.15 } } } ] }""");

        Assert.Empty(plan.Problems);

        var effect = Row(plan, Table.Effect, 200390010);
        Assert.Equal(200100910, effect.CopyFrom);
        Assert.Null(Value(effect, "FloatValue0"));

        var grant = Assert.Single(plan.Grants);
        Assert.Equal(RewardPlan.WindPower, grant.Effect);
        Assert.Equal(39001, grant.RewardId);
        Assert.Equal(new[] { new FieldSet(RewardPlan.WindShare, new FieldValue.Number(0.15)) }, grant.Fields);
    }

    [Fact]
    public void Changing_a_game_reward_edits_its_own_rows_without_copying()
    {
        var plan = Plan("""{ "rewards": [ { "id": 31271, "cost": 4, "needs": [31272], "pools": { "1013": 5 } } ] }""");

        Assert.Empty(plan.Problems);

        var reward = Row(plan, Table.Reward, 31271);
        Assert.Null(reward.CopyFrom);
        Assert.Equal(new FieldValue.Number(4), Value(reward, "LvUpCost"));
        Assert.Null(Value(reward, "TalentID"));
        Assert.Null(Row(plan, Table.Condition, 31271).CopyFrom);
        Assert.Equal(new[] { new PoolEdit(1013, 31271, 5) }, plan.Pools);
    }

    [Fact]
    public void A_reward_with_any_problem_writes_none_of_its_rows_and_the_others_still_apply()
    {
        var plan = Plan("""
            { "rewards": [
              { "id": 39001, "copyFrom": 31271, "pools": { "7777": 1 }, "give": { "effect": "game:AE_PowerSolarEfficiency" } },
              { "id": 39002, "copyFrom": 31272 } ] }
            """);

        Assert.DoesNotContain(plan.Rows, row => row.Id is 39001 or 390010 or 200390010);
        Assert.Contains(plan.Problems, problem => problem.Contains("39001") && problem.Contains("7777"));
        Row(plan, Table.Reward, 39002);
    }

    [Theory]
    [InlineData("""{ "id": 50001, "copyFrom": 31271 }""", "talent")]
    [InlineData("""{ "id": 39001 }""", "copyFrom")]
    [InlineData("""{ "id": 39001, "copyFrom": 50001 }""", "not an end-of-day reward")]
    [InlineData("""{ "id": 39001, "copyFrom": 31271, "give": { "effect": "game:NoSuchScript" } }""", "NoSuchScript")]
    [InlineData("""{ "id": 39001, "copyFrom": 31271, "give": { "effect": "mr:nothing" } }""", "mr:nothing")]
    [InlineData("""{ "id": 39001, "copyFrom": 31271, "give": { "effect": "AE_PowerSolarEfficiency" } }""", "game:")]
    public void A_reward_that_cannot_be_built_is_skipped_and_says_why(string reward, string reason)
    {
        var plan = Plan($$"""{ "rewards": [ {{reward}} ] }""");

        Assert.Empty(plan.Rows);
        Assert.Contains(plan.Problems, problem => problem.Contains(reason));
    }

    [Fact]
    public void A_new_reward_never_takes_a_buff_or_effect_the_game_already_uses()
    {
        var tables = new FakeTables();
        tables.Effects.Add(200390010);

        var plan = Plan("""{ "rewards": [ { "id": 39001, "copyFrom": 31271, "give": { "effect": "game:AE_PowerSolarEfficiency" } } ] }""", tables);

        Assert.Empty(plan.Rows);
        Assert.Contains(plan.Problems, problem => problem.Contains("already in use"));
    }

    [Fact]
    public void A_disabled_reward_changes_nothing_including_its_own_buffs()
    {
        var plan = Plan("""{ "rewards": [ { "id": 31271, "enabled": false, "cost": 1, "buffs": [ { "id": 312710, "fields": { "Time": 5 } } ] } ] }""");

        Assert.Empty(plan.Rows);
        Assert.Empty(plan.Pools);
    }

    [Fact]
    public void The_second_entry_for_an_id_is_left_out()
    {
        var plan = Plan("""{ "rewards": [ { "id": 31271, "cost": 1 }, { "id": 31271, "cost": 2 } ] }""");

        Assert.Equal(new FieldValue.Number(1), Value(Row(plan, Table.Reward, 31271), "LvUpCost"));
        Assert.Contains(plan.Problems, problem => problem.Contains("listed twice"));
    }

    [Fact]
    public void The_wind_rewards_build_cleanly_against_the_game_tables()
    {
        var tables = new FakeTables();
        tables.Rewards.Add(31273);
        tables.Talents.Add(31273);
        tables.Buffs.Add(312730);
        tables.Conditions.Add(31273);

        foreach (var pool in new[] { 1021, 1022, 9105, 9106, 9107, 9108, 9110, 9111, 9112, 9130, 9131, 9132, 9133 }) tables.PoolIds.Add(pool);

        var plan = RewardPlan.Build(RewardFile.Parse(RewardFileTests.Resource("MoreRewards.wind-rewards.json")), tables);

        Assert.Empty(plan.Problems);
        Assert.Equal(new[] { 39001, 39002, 39003 }, plan.Rows.Where(row => row.Table == Table.Reward).Select(row => row.Id));
        Assert.Equal(new[] { 39001, 39002, 39003 }, plan.Grants.Select(grant => grant.RewardId));
    }
}
