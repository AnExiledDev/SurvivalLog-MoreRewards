using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class LockBookTests
{
    private static readonly RewardLock[] None = Array.Empty<RewardLock>();

    [Fact]
    public void The_first_lock_is_the_game_own_so_one_lock_works_as_in_the_game()
    {
        Assert.Equal(LockMove.PinGame, LockBook.Click(30011, 0, None, 1));
        Assert.Equal(LockMove.PinGame, LockBook.Click(30011, 0, None, 4));
    }

    [Fact]
    public void With_the_game_lock_in_use_more_cards_take_extra_locks_up_to_the_most()
    {
        var extras = new[] { new RewardLock(2, 1) };

        Assert.Equal(LockMove.AddExtra, LockBook.Click(3, 1, None, 3));
        Assert.Equal(LockMove.AddExtra, LockBook.Click(3, 1, extras, 3));
        Assert.Equal(LockMove.Full, LockBook.Click(4, 1, new[] { new RewardLock(2, 1), new RewardLock(3, 2) }, 3));
    }

    [Fact]
    public void One_lock_allowed_means_a_second_card_cannot_be_locked()
    {
        Assert.Equal(LockMove.Full, LockBook.Click(2, 1, None, 1));
    }

    [Fact]
    public void Clicking_a_locked_card_unlocks_it_whichever_lock_holds_it()
    {
        var extras = new[] { new RewardLock(2, 1) };

        Assert.Equal(LockMove.UnpinGame, LockBook.Click(1, 1, extras, 3));
        Assert.Equal(LockMove.RemoveExtra, LockBook.Click(2, 1, extras, 3));
    }

    [Fact]
    public void An_extra_lock_can_be_unlocked_even_when_the_most_was_lowered_below_what_is_held()
    {
        var extras = new[] { new RewardLock(2, 1), new RewardLock(3, 2) };

        Assert.Equal(LockMove.RemoveExtra, LockBook.Click(3, 1, extras, 1));
    }

    [Fact]
    public void Relocking_a_card_moves_it_to_its_new_slot_without_keeping_it_twice()
    {
        var extras = LockBook.With(new[] { new RewardLock(2, 1) }, new RewardLock(2, 4));

        Assert.Equal(new[] { new RewardLock(2, 4) }, extras);
        Assert.Empty(LockBook.Without(extras, 2));
    }
}
