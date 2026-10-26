using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

/// <summary>
/// The rewards table is read from the game a slice a frame and kept until the game's table or its text table (the
/// language) changes. These guard that a frame with nothing new builds nothing, that a real change is seen, and that a
/// build split across frames still ends.
/// </summary>
public class SlicedWorkTests
{
    private static readonly Func<bool> AlwaysSpent = () => true;
    private static readonly Func<bool> NeverSpent = () => false;
    private static readonly TableKey English = new(Table: 100, Text: 1);
    private static readonly TableKey Chinese = new(Table: 100, Text: 2);
    private static readonly TableKey Reloaded = new(Table: 200, Text: 1);

    [Fact]
    public void The_same_table_and_language_reuse_what_was_built()
    {
        var builds = new KeyedBuild<TableKey, string>(key => new CountingBuild(3, Name(key)));

        var first = builds.Get(English);

        Assert.Same(first, builds.Get(English));
        Assert.Same(first, builds.Advance(English, AlwaysSpent));
        Assert.Same(first, builds.Ready(English));
        Assert.False(builds.IsBuilding);
        Assert.Equal(1, builds.Builds);
    }

    [Fact]
    public void A_language_change_throws_the_old_table_away_and_builds_again()
    {
        var builds = new KeyedBuild<TableKey, string>(key => new CountingBuild(3, Name(key)));

        builds.Get(English);

        Assert.Null(builds.Ready(Chinese));
        Assert.Equal("100/2", builds.Get(Chinese));
        Assert.Null(builds.Ready(English));
        Assert.Equal(2, builds.Builds);
    }

    [Fact]
    public void A_reloaded_game_table_throws_the_old_table_away_and_builds_again()
    {
        var builds = new KeyedBuild<TableKey, string>(key => new CountingBuild(3, Name(key)));

        builds.Get(English);

        Assert.Equal("200/1", builds.Get(Reloaded));
        Assert.Null(builds.Ready(English));
        Assert.Equal(2, builds.Builds);
    }

    [Fact]
    public void A_build_takes_one_step_a_frame_when_the_frame_is_spent_and_still_ends()
    {
        var build = new CountingBuild(5, "table");
        var builds = new KeyedBuild<TableKey, string>(_ => build);

        Assert.Null(builds.Advance(English, AlwaysSpent));
        Assert.Equal(1, build.StepsRun);

        var frames = 1;
        string? table = null;

        while (table == null && frames < 100)
        {
            table = builds.Advance(English, AlwaysSpent);
            frames++;
        }

        // Five steps, one a frame, then the frame that ends the build.
        Assert.Equal("table", table);
        Assert.Equal(6, frames);
        Assert.Equal(5, build.StepsRun);
        Assert.Equal(1, builds.Builds);
    }

    [Fact]
    public void Continue_carries_on_the_build_in_progress_without_starting_another()
    {
        var builds = new KeyedBuild<TableKey, string>(key => new CountingBuild(4, Name(key)));

        builds.Advance(English, AlwaysSpent);

        for (var frame = 0; frame < 100 && builds.IsBuilding; frame++) builds.Continue(AlwaysSpent);

        Assert.Equal("100/1", builds.Ready(English));
        Assert.Equal(1, builds.Builds);
    }

    [Fact]
    public void A_language_change_in_the_middle_of_a_build_starts_over_for_the_new_one()
    {
        var builds = new KeyedBuild<TableKey, string>(key => new CountingBuild(4, Name(key)));

        builds.Advance(English, AlwaysSpent);
        builds.Advance(Chinese, AlwaysSpent);

        Assert.Equal(2, builds.Builds);
        Assert.Equal("100/2", builds.Get(Chinese));
        Assert.Null(builds.Ready(English));
    }

    [Fact]
    public void Get_finishes_at_once_even_when_each_step_asks_to_wait()
    {
        var build = new CountingBuild(4, "table", stopsEachStep: true);
        var builds = new KeyedBuild<TableKey, string>(_ => build);

        Assert.Equal("table", builds.Get(English));
        Assert.Equal(4, build.StepsRun);
    }

    [Fact]
    public void A_step_that_fails_drops_the_build_and_the_next_ask_starts_a_fresh_one()
    {
        var attempt = 0;
        var builds = new KeyedBuild<TableKey, string>(key => new CountingBuild(3, Name(key), failsAtStep: ++attempt == 1 ? 2 : 0));

        Assert.Throws<InvalidOperationException>(() => builds.Get(English));
        Assert.False(builds.IsBuilding);

        Assert.Equal("100/1", builds.Get(English));
        Assert.Equal(2, builds.Builds);
    }

    [Fact]
    public void A_job_waits_for_the_next_frame_when_a_step_asks_and_runs_on_while_time_allows()
    {
        var job = new SlicedJob(new[] { true, false, true, true });

        Assert.False(job.Advance(NeverSpent));
        Assert.True(job.Advance(NeverSpent));
        Assert.True(job.IsDone);
    }

    [Fact]
    public void Jobs_in_the_same_frame_share_one_slice()
    {
        var clock = 0L;
        var budget = new FrameBudget(() => clock, sliceTicks: 100);

        budget.Start(1f);
        clock = 100;

        Assert.True(budget.IsSpent());

        // A second job in the same frame gets what is left of the slice, which is nothing.
        budget.Start(1f);

        Assert.True(budget.IsSpent());
    }

    [Fact]
    public void A_new_frame_gets_a_whole_new_slice()
    {
        var clock = 0L;
        var budget = new FrameBudget(() => clock, sliceTicks: 100);

        budget.Start(1f);
        clock = 150;
        budget.Start(1.016f);

        Assert.False(budget.IsSpent());

        clock = 249;

        Assert.False(budget.IsSpent());

        clock = 250;

        Assert.True(budget.IsSpent());
    }

    [Fact]
    public void Asking_whether_the_slice_is_spent_hands_out_the_same_check_every_time()
    {
        var budget = new FrameBudget();

        Assert.Same(budget.IsSpent, budget.IsSpent);
    }

    private static string Name(TableKey key) => key.Table + "/" + key.Text;

    /// <summary>A build of <c>steps</c> steps that counts the ones it ran; it can stop for the frame after each step or fail at one.</summary>
    private sealed class CountingBuild : ISlicedBuild<string>
    {
        private readonly int _steps;
        private readonly string _result;
        private readonly bool _stopsEachStep;
        private readonly int _failsAtStep;

        public CountingBuild(int steps, string result, bool stopsEachStep = false, int failsAtStep = 0)
        {
            _steps = steps;
            _result = result;
            _stopsEachStep = stopsEachStep;
            _failsAtStep = failsAtStep;
        }

        public int StepsRun { get; private set; }

        public string Result { get; private set; } = "";

        public IEnumerable<bool> Steps()
        {
            for (var step = 1; step <= _steps; step++)
            {
                if (step == _failsAtStep) throw new InvalidOperationException("the game's table could not be read");

                StepsRun++;

                yield return !_stopsEachStep;
            }

            Result = _result;
        }
    }
}
