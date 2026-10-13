using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace MoreRewards.Core;

/// <summary>
/// The most interest the game pays on survival points when a day ends. SurvivalResultsManager.SettleDailySurvivalPoints
/// (rva 0x2699ec0) pays the points held times the interest rate, rounded down, at least 1 and at most 10. The 10 is in
/// no table: it is two numbers in the game's code, <c>cmp eax,0Ah / jl +5 / mov eax,0Ah</c> at rva 0x269a4d4, which keep
/// the smaller of the interest and 10. The mod writes its own cap over both numbers and leaves the rest of the rule,
/// the rate and the minimum of 1, to the game.
/// </summary>
public static class InterestCap
{
    public const int GameCap = 10;

    /// <summary>Double the game's: at the base rate of 5% the cap is reached at 400 points held instead of 200.</summary>
    public const int DefaultCap = 20;

    /// <summary>The compare holds a signed byte, so the cap must stay below 128.</summary>
    public const int MostCap = 100;

    /// <summary>
    /// The game's code from rva 0x269a4c1: the rounded-down interest into eax, the minimum of 1 for interest that rounds
    /// to 0, then the cap. Checking all of it, not just the cap, tells this cap apart from the game's other caps of 10.
    /// </summary>
    public static readonly IReadOnlyList<byte> GameCode = new byte[]
    {
        0xF2, 0x0F, 0x2C, 0xC0, // cvttsd2si eax, xmm0
        0x85, 0xC0, // test eax, eax
        0x7F, 0x0B, // jg the cap
        0x41, 0x0F, 0x2F, 0xF0, // comiss xmm6, xmm8
        0x76, 0x0F, // jbe past the cap
        0x41, 0x8B, 0xC7, // mov eax, r15d (1)
        0xEB, 0x0A, // jmp past the cap
        0x83, 0xF8, 0x0A, // cmp eax, 10
        0x7C, 0x05, // jl past the cap
        0xB8, 0x0A, 0x00, 0x00, 0x00, // mov eax, 10
    };

    /// <summary>Where in <see cref="GameCode"/> the cap starts, the compare the game jumps to with interest above 0.</summary>
    public const int CapAt = 19;

    private const int CompareCapAt = CapAt + 2;
    private const int MoveCapAt = CapAt + 6;

    /// <returns>The cap to write, or null to keep the game's: 0 (or less) and the game's own 10 change nothing.</returns>
    public static int? From(int maxInterest) => maxInterest <= 0 || maxInterest == GameCap ? null : Math.Min(maxInterest, MostCap);

    /// <returns>
    /// <paramref name="code"/> with <paramref name="cap"/> in place of the game's 10, or null when the code is not the
    /// game's (an update moved or changed it), so nothing is written.
    /// </returns>
    public static byte[]? Rewrite(IReadOnlyList<byte> code, int cap)
    {
        if (cap < 1 || cap > MostCap) throw new ArgumentOutOfRangeException(nameof(cap), cap, "a cap comes from From");

        if (!code.SequenceEqual(GameCode)) return null;

        var rewritten = GameCode.ToArray();
        rewritten[CompareCapAt] = (byte)cap;
        BinaryPrimitives.WriteInt32LittleEndian(rewritten.AsSpan(MoveCapAt), cap);

        return rewritten;
    }
}
