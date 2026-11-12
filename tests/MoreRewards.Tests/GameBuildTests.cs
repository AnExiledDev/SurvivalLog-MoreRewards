using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

/// <summary>
/// Checks the game build the mods are built against still holds the code the mod rewrites. It reads the game's
/// GameAssembly.dll from game-ref (copied by tools/refresh-game-ref.sh), so a game update that changes the code fails
/// here as soon as game-ref is refreshed, instead of in a player's game.
/// </summary>
[Trait("Category", "GameBuild")]
public class GameBuildTests
{
    [Fact]
    public void The_game_holds_its_interest_code_exactly_once_with_its_own_cap()
    {
        var game = GameImage.Load();

        Assert.Equal(new int?[] { InterestCap.GameCap }, game.InterestCode().Select(at => InterestCap.CapIn(game.Read(at, InterestCap.GameCode.Count))));
    }

    [Fact]
    public void The_interest_code_lies_in_one_game_method_and_searching_it_to_its_end_finds_it()
    {
        var game = GameImage.Load();
        var code = game.InterestCode().Single();
        var (start, length) = game.MethodHolding(code);

        Assert.InRange(code, start, start + length - InterestCap.GameCode.Count);
        Assert.Equal(code - start, InterestCap.FindIn(game.Read(start, length)));
    }

    [Fact]
    public void The_interop_has_exactly_one_method_info_for_settling_the_day_so_the_mod_can_find_the_method()
    {
        using var pe = new PEReader(File.OpenRead(GameRef("BepInEx", "interop", "HotUpdate.dll")));
        var metadata = pe.GetMetadataReader();
        var type = metadata.TypeDefinitions.Select(metadata.GetTypeDefinition)
            .Single(t => metadata.GetString(t.Namespace) == "GameCore.HotUpdate.Battle.Logic" && metadata.GetString(t.Name) == "SurvivalResultsManager");
        var fields = type.GetFields().Select(f => metadata.GetString(metadata.GetFieldDefinition(f).Name))
            .Where(name => name.StartsWith(InterestCap.MethodInfoField, StringComparison.Ordinal));

        Assert.Single(fields);
    }

    private static string GameRef(params string[] parts)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var file = Path.Combine([dir.FullName, "game-ref", .. parts]);

            if (File.Exists(file)) return file;
        }

        throw new FileNotFoundException($"no game-ref/{string.Join('/', parts)} above the tests; run tools/refresh-game-ref.sh");
    }

    /// <summary>GameAssembly.dll from game-ref, read the way the game maps it: by offsets from the module's start.</summary>
    private sealed class GameImage
    {
        private const uint HoldsCode = 0x20000000;

        private readonly byte[] _file;
        private readonly List<Section> _sections;
        private readonly int _size;
        private readonly ExceptionTable _table;

        private GameImage(byte[] file)
        {
            var firstPage = file.AsSpan(0, PeHeader.PageSize).ToArray();

            _file = file;
            _sections = SectionsOf(file);
            _size = PeHeader.SizeOfImage(firstPage) ?? throw new InvalidDataException("game-ref/GameAssembly.dll has no PE header");
            _table = PeHeader.ExceptionTableOf(firstPage) ?? throw new InvalidDataException("game-ref/GameAssembly.dll has no exception table");
        }

        public static GameImage Load() => new(File.ReadAllBytes(GameRef("GameAssembly.dll")));

        /// <returns>Every place in the game's code holding the interest code with any cap, as an offset from the module's start.</returns>
        public IReadOnlyList<int> InterestCode() =>
            _sections.Where(section => (section.Flags & HoldsCode) != 0)
                .SelectMany(section => Enumerable.Range(0, section.RawSize - InterestCap.GameCode.Count + 1)
                    .Where(at => _file[section.RawAt + at] == InterestCap.GameCode[0])
                    .Where(at => InterestCap.CapIn(_file.AsSpan(section.RawAt + at, InterestCap.GameCode.Count).ToArray()) is not null)
                    .Select(at => section.Rva + at))
                .ToList();

        /// <summary>A section's bytes past its raw data are zeros once mapped, as the loader leaves them.</summary>
        public byte[] Read(int rva, int count)
        {
            var bytes = new byte[count];
            var section = _sections.Single(s => s.Rva <= rva && rva < s.Rva + Math.Max(s.VirtualSize, s.RawSize));
            var available = Math.Clamp(section.RawSize - (rva - section.Rva), 0, count);

            _file.AsSpan(section.RawAt + rva - section.Rva, available).CopyTo(bytes);

            return bytes;
        }

        /// <returns>
        /// The method holding <paramref name="rva"/>: from the last table entry starting at or before it, back to the
        /// first entry that starts a method long enough to reach it.
        /// </returns>
        public (int Start, int Length) MethodHolding(int rva)
        {
            var entries = _table.Size / 12;
            var last = Enumerable.Range(0, entries).Last(index => BeginOf(index) <= rva);

            foreach (var index in Enumerable.Range(0, last + 1).Reverse().Take(64))
            {
                var begin = BeginOf(index);

                if (FunctionTable.MethodLength(Read, _size, _table, begin) is { } length && begin + length > rva) return (begin, length);
            }

            throw new InvalidDataException($"no method in the exception table holds {rva:x}");
        }

        private int BeginOf(int index) => BinaryPrimitives.ReadInt32LittleEndian(Read(_table.Rva + index * 12, 4));

        private static List<Section> SectionsOf(byte[] file)
        {
            var pe = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(0x3c));
            var count = BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(pe + 6));
            var table = pe + 24 + BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(pe + 20));

            return Enumerable.Range(0, count).Select(i => table + i * 40).Select(at => new Section(
                BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(at + 8)),
                BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(at + 12)),
                BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(at + 16)),
                BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(at + 20)),
                BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(at + 36)))).ToList();
        }

        private readonly record struct Section(int VirtualSize, int Rva, int RawSize, int RawAt, uint Flags);
    }
}
