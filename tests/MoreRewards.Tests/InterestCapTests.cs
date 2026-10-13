using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class InterestCapTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(9, 9)]
    [InlineData(10, 10)]
    [InlineData(37, 10)]
    public void The_game_code_pays_at_most_ten(int interest, int paid)
    {
        Assert.Equal(paid, PaidBy(InterestCap.GameCode, interest));
    }

    [Theory]
    [InlineData(15, 15)]
    [InlineData(19, 19)]
    [InlineData(20, 20)]
    [InlineData(37, 20)]
    public void A_cap_of_twenty_pays_up_to_twenty(int interest, int paid)
    {
        Assert.Equal(paid, PaidBy(Rewritten(20), interest));
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(37, 5)]
    public void A_cap_below_the_game_lowers_the_interest(int interest, int paid)
    {
        Assert.Equal(paid, PaidBy(Rewritten(5), interest));
    }

    [Fact]
    public void The_highest_cap_still_fits_the_compare()
    {
        var code = Rewritten(InterestCap.MostCap);

        Assert.Equal(99, PaidBy(code, 99));
        Assert.Equal(InterestCap.MostCap, PaidBy(code, 5000));
    }

    [Fact]
    public void Only_the_cap_changes_in_the_code()
    {
        var code = Rewritten(20);
        var changed = new List<int>();

        for (var at = 0; at < code.Length; at++)
        {
            if (code[at] != InterestCap.GameCode[at]) changed.Add(at);
        }

        Assert.Equal(new[] { InterestCap.CapAt + 2, InterestCap.CapAt + 6 }, changed);
    }

    [Fact]
    public void Code_that_is_not_the_game_own_is_left_alone()
    {
        var updated = Rewritten(20);
        var shifted = new byte[InterestCap.GameCode.Count];

        Assert.Null(InterestCap.Rewrite(updated, 30));
        Assert.Null(InterestCap.Rewrite(shifted, 20));
        Assert.Null(InterestCap.Rewrite(new byte[3], 20));
    }

    [Fact]
    public void A_module_that_ends_before_the_interest_code_is_not_read()
    {
        var end = InterestCap.GameCodeRva + InterestCap.GameCode.Count;

        Assert.True(InterestCap.FitsIn(end));
        Assert.False(InterestCap.FitsIn(end - 1));
        Assert.False(InterestCap.FitsIn(PeHeader.PageSize));
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(-3, null)]
    [InlineData(10, null)]
    [InlineData(20, 20)]
    [InlineData(5, 5)]
    [InlineData(100, 100)]
    [InlineData(500, 100)]
    public void Zero_and_ten_keep_the_game_cap_and_a_cap_too_high_is_held_to_the_most(int maxInterest, int? cap)
    {
        Assert.Equal(cap, InterestCap.From(maxInterest));
    }

    private static byte[] Rewritten(int cap) =>
        InterestCap.Rewrite(InterestCap.GameCode, cap) ?? throw new InvalidOperationException("the game code was not rewritten");

    /// <summary>
    /// Runs the code from the cap to its end, as the game does with interest above 0 in eax, and returns eax. A fake CPU
    /// for the three instructions there: cmp eax,imm8 (83 F8), jl rel8 (7C) and mov eax,imm32 (B8).
    /// </summary>
    private static int PaidBy(IReadOnlyList<byte> code, int interest)
    {
        var eax = interest;
        var isLess = false;
        var at = InterestCap.CapAt;

        while (at < code.Count)
        {
            switch (code[at])
            {
                case 0x83 when code[at + 1] == 0xF8:
                    isLess = eax < (sbyte)code[at + 2];
                    at += 3;

                    break;
                case 0x7C:
                    at += 2 + (isLess ? (sbyte)code[at + 1] : 0);

                    break;
                case 0xB8:
                    eax = BinaryPrimitives.ReadInt32LittleEndian(new[] { code[at + 1], code[at + 2], code[at + 3], code[at + 4] });
                    at += 5;

                    break;
                default:
                    throw new InvalidOperationException($"no fake for byte {code[at]:X2} at {at}");
            }
        }

        return eax;
    }
}
