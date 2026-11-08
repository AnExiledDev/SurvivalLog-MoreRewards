using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

/// <summary>
/// Checks the game build the mods are built against still holds the code the mod rewrites. It reads the game's own
/// GameAssembly.dll from game-ref (copied by tools/refresh-game-ref.sh), so a game update that changes the code fails
/// here as soon as game-ref is refreshed, instead of in a player's game.
/// </summary>
[Trait("Category", "GameBuild")]
public class GameBuildTests
{
    [Fact]
    public void The_game_holds_its_interest_code_exactly_once_with_its_own_cap()
    {
        var hits = ExecutableSections(GameAssembly())
            .SelectMany(code => Enumerable.Range(0, code.Length - InterestCap.GameCode.Count + 1)
                .Where(at => code[at] == InterestCap.GameCode[0])
                .Select(at => InterestCap.CapIn(code.AsSpan(at, InterestCap.GameCode.Count).ToArray()))
                .Where(cap => cap is not null))
            .ToList();

        Assert.Equal(new int?[] { InterestCap.GameCap }, hits);
    }

    private static byte[] GameAssembly()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var file = Path.Combine(dir.FullName, "game-ref", "GameAssembly.dll");

            if (File.Exists(file)) return File.ReadAllBytes(file);
        }

        throw new FileNotFoundException("no game-ref/GameAssembly.dll above the tests; run tools/refresh-game-ref.sh");
    }

    /// <summary>The file's sections that hold code, from the PE section table.</summary>
    private static IEnumerable<byte[]> ExecutableSections(byte[] file)
    {
        const uint holdsCode = 0x20000000;
        var pe = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(0x3c));
        var sections = BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(pe + 6));
        var table = pe + 24 + BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(pe + 20));

        foreach (var at in Enumerable.Range(0, sections).Select(i => table + i * 40))
        {
            if ((BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(at + 36)) & holdsCode) == 0) continue;

            var rawSize = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(at + 16));
            var rawAt = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(at + 20));

            yield return file.AsSpan(rawAt, rawSize).ToArray();
        }
    }
}
