using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace MoreRewards.Core;

/// <summary>
/// The most interest the game pays on survival points when a day ends. SurvivalResultsManager.SettleDailySurvivalPoints
/// pays the points held times the interest rate, rounded down, at most 10, and at least 1 while any points are held.
/// The 10 is in no table: it is two numbers in the game's code, <c>cmp eax,0Ah / jl +5 / mov eax,0Ah</c>, which keep
/// the smaller of the interest and 10. The mod writes its own cap over both numbers and leaves the rest of the rule,
/// the rate (<see cref="InterestRate"/>) and the minimum of 1, as it is.
/// The method moves with every game update while its code stays the same (0x601 bytes in, in 1.1.19611, 1.1.20151 and
/// 1.1.20719), so the code is found by searching the method (<see cref="FindIn"/>), never at a fixed address.
/// </summary>
public static class InterestCap
{
    public const int GameCap = 10;

    /// <summary>Double the game's: at the default rate of 10% the cap is reached at 200 points held instead of 100.</summary>
    public const int DefaultCap = 20;

    /// <summary>The compare holds a signed byte, so the cap must stay below 128.</summary>
    public const int MostCap = 100;

    /// <summary>
    /// How much of SettleDailySurvivalPoints is searched from its start: the method is 2992 bytes long in game 1.1.20719,
    /// with <see cref="GameCode"/> 0x601 bytes in.
    /// </summary>
    public const int SearchBytes = 0x2000;

    /// <summary>
    /// The game's code with the cap: the rounded-down interest into eax, the minimum of 1 for interest that rounds
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

    /// <returns>
    /// Whether a module of <paramref name="sizeOfImage"/> bytes holds the <see cref="SearchBytes"/> from a method at
    /// <paramref name="methodRva"/>, so reading them is safe.
    /// </returns>
    public static bool FitsIn(long methodRva, int sizeOfImage) => methodRva >= 0 && methodRva <= sizeOfImage - SearchBytes;

    /// <returns>
    /// Where in <paramref name="body"/> the game's code starts, with the game's cap or one this mod wrote, or null when it
    /// is there not exactly once: an update that changed the code, or put a second copy in reach, writes nothing.
    /// </returns>
    public static int? FindIn(ReadOnlySpan<byte> body)
    {
        int? found = null;

        for (var at = 0; at <= body.Length - GameCode.Count; at++)
        {
            if (body[at] != GameCode[0] || CapIn(body.Slice(at, GameCode.Count).ToArray()) is null) continue;

            if (found is not null) return null;

            found = at;
        }

        return found;
    }

    /// <returns>The cap to write, or null to keep the game's: 0 (or less) and the game's own 10 change nothing.</returns>
    public static int? From(int maxInterest) => maxInterest <= 0 || maxInterest == GameCap ? null : Math.Min(maxInterest, MostCap);

    /// <returns>
    /// <paramref name="code"/> with <paramref name="cap"/> in place of the cap it holds (the game's 10, or a cap this
    /// mod wrote before), or null when the code is not the game's (an update moved or changed it), so nothing is
    /// written. A cap of <see cref="GameCap"/> puts the game's own code back.
    /// </returns>
    public static byte[]? Rewrite(IReadOnlyList<byte> code, int cap)
    {
        if (cap < 1 || cap > MostCap) throw new ArgumentOutOfRangeException(nameof(cap), cap, "a cap comes from From, or is the game's");

        if (CapIn(code) is null) return null;

        var rewritten = GameCode.ToArray();
        rewritten[CompareCapAt] = (byte)cap;
        BinaryPrimitives.WriteInt32LittleEndian(rewritten.AsSpan(MoveCapAt), cap);

        return rewritten;
    }

    /// <returns>The cap <paramref name="code"/> holds, when it is the game's code with the same cap from 1 to <see cref="MostCap"/> in both places; otherwise null.</returns>
    public static int? CapIn(IReadOnlyList<byte> code)
    {
        if (code.Count != GameCode.Count) return null;

        var bytes = code.ToArray();
        var cap = bytes[CompareCapAt];

        if (cap < 1 || cap > MostCap || BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(MoveCapAt)) != cap) return null;

        var asGame = GameCode.ToArray();

        asGame[CompareCapAt] = cap;
        BinaryPrimitives.WriteInt32LittleEndian(asGame.AsSpan(MoveCapAt), cap);

        return bytes.SequenceEqual(asGame) ? cap : null;
    }
}
