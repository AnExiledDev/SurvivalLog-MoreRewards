using System;
using System.Buffers.Binary;
using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

/// <summary>A method's code is searched from its start to its end as the exception table gives it, never into the next method.</summary>
public class FunctionTableTests
{
    [Fact]
    public void A_method_in_one_entry_is_as_long_as_that_entry()
    {
        var module = new FakeModule();
        module.Method(0x2000, 0x33F0);

        Assert.Equal(0x13F0, module.LengthOf(0x2000));
    }

    [Fact]
    public void A_method_split_into_its_setup_and_a_chained_piece_runs_to_the_end_of_the_piece()
    {
        // SettleDailySurvivalPoints in game 1.1.20719: 247 bytes of setup, then 2745 bytes in a chained entry.
        var module = new FakeModule();
        var own = module.Method(0x2000, 0x20F7);
        module.Piece(0x20F7, 0x2BB0, own);

        Assert.Equal(2992, module.LengthOf(0x2000));
    }

    [Fact]
    public void A_piece_chained_to_a_piece_of_the_method_still_counts()
    {
        var module = new FakeModule();
        var own = module.Method(0x2000, 0x2100);
        var piece = module.Piece(0x2100, 0x2200, own);
        module.Piece(0x2200, 0x2300, piece);

        Assert.Equal(0x300, module.LengthOf(0x2000));
    }

    [Fact]
    public void The_next_method_is_never_counted_even_right_after()
    {
        var module = new FakeModule();
        module.Method(0x2000, 0x2100);
        module.Method(0x2100, 0x2200);

        Assert.Equal(0x100, module.LengthOf(0x2000));
    }

    [Fact]
    public void A_piece_of_another_method_ends_the_method()
    {
        var module = new FakeModule();
        var other = module.Method(0x1800, 0x1900);
        module.Method(0x2000, 0x2100);
        module.Piece(0x2100, 0x2200, other);

        Assert.Equal(0x100, module.LengthOf(0x2000));
    }

    [Fact]
    public void A_piece_that_does_not_start_where_the_method_left_off_is_not_counted()
    {
        var module = new FakeModule();
        var own = module.Method(0x2000, 0x2100);
        module.Piece(0x2110, 0x2200, own);

        Assert.Equal(0x100, module.LengthOf(0x2000));
    }

    [Fact]
    public void An_address_where_no_entry_starts_is_no_method()
    {
        var module = new FakeModule();
        module.Method(0x2000, 0x2100);
        module.Method(0x2100, 0x2200);

        Assert.Null(module.LengthOf(0x2010));
        Assert.Null(module.LengthOf(0x1000));
        Assert.Null(module.LengthOf(0x3000));
    }

    [Fact]
    public void A_piece_of_a_method_is_not_a_method_of_its_own()
    {
        var module = new FakeModule();
        var own = module.Method(0x2000, 0x2100);
        module.Piece(0x2100, 0x2200, own);

        Assert.Null(module.LengthOf(0x2100));
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(FakeModule.Size + 0x2000L)]
    public void A_method_outside_the_module_is_never_read(long methodRva)
    {
        var module = new FakeModule();
        module.Method(0x2000, 0x2100);

        Assert.Null(module.LengthOf(methodRva));
    }

    [Fact]
    public void A_table_that_runs_past_the_end_of_the_module_is_never_read()
    {
        var module = new FakeModule();
        module.Method(0x2000, 0x2100);

        Assert.Null(module.LengthOf(0x2000, new ExceptionTable(FakeModule.Size - 6, 12)));
    }

    [Fact]
    public void An_entry_that_runs_past_the_end_of_the_module_is_no_method()
    {
        var module = new FakeModule();
        module.Method(0x2000, FakeModule.Size + 1);

        Assert.Null(module.LengthOf(0x2000));
    }

    [Fact]
    public void A_chain_that_loops_ends_the_method_instead_of_running_forever()
    {
        var module = new FakeModule();
        module.Method(0x2000, 0x2100);
        module.LoopingPiece(0x2100, 0x2200);

        Assert.Equal(0x100, module.LengthOf(0x2000));
    }

    [Fact]
    public void An_entry_that_points_at_another_entry_instead_of_unwind_info_is_no_method()
    {
        var module = new FakeModule();
        module.IndirectMethod(0x2000, 0x2100);

        Assert.Null(module.LengthOf(0x2000));
    }

    /// <summary>A module of zeros holding an exception table at <see cref="TableAt"/> and the unwind info its entries point at.</summary>
    private sealed class FakeModule
    {
        public const int Size = 0x10000;
        private const int TableAt = 0x400;
        private const int EntrySize = 12;
        private const int ChainInfo = 0x4;

        private readonly byte[] _bytes = new byte[Size];
        private int _nextUnwind = 0x8000;
        private int _entries;

        public int? LengthOf(long methodRva) => LengthOf(methodRva, new ExceptionTable(TableAt, _entries * EntrySize));

        public int? LengthOf(long methodRva, ExceptionTable table) =>
            FunctionTable.MethodLength((rva, count) => _bytes.AsSpan(rva, count).ToArray(), Size, table, methodRva);

        /// <summary>Adds the entry a method starts with, which has unwind info of its own.</summary>
        public (int Begin, int End, int Unwind) Method(int begin, int end) => Add(begin, end, OwnUnwind());

        /// <summary>Adds a piece whose unwind info chains back to <paramref name="parent"/>.</summary>
        public (int Begin, int End, int Unwind) Piece(int begin, int end, (int Begin, int End, int Unwind) parent) =>
            Add(begin, end, ChainedUnwind(parent));

        /// <summary>Adds a piece whose unwind info chains back to the piece itself.</summary>
        public void LoopingPiece(int begin, int end)
        {
            var unwind = _nextUnwind;
            ChainedUnwind((begin, end, unwind));
            Add(begin, end, unwind);
        }

        /// <summary>Adds an entry with the low bit of its unwind address set, which points at another entry.</summary>
        public void IndirectMethod(int begin, int end) => Add(begin, end, OwnUnwind() | 1);

        private (int Begin, int End, int Unwind) Add(int begin, int end, int unwind)
        {
            var at = TableAt + _entries++ * EntrySize;

            WriteEntry(at, (begin, end, unwind));

            return (begin, end, unwind);
        }

        /// <summary>Version 1, no flags, two unwind codes.</summary>
        private int OwnUnwind()
        {
            var at = NextUnwind(4 + 2 * 2);
            _bytes[at] = 1;
            _bytes[at + 2] = 2;

            return at;
        }

        /// <summary>Version 1 with the chain flag and one unwind code, padded to two, then the parent's entry.</summary>
        private int ChainedUnwind((int Begin, int End, int Unwind) parent)
        {
            var at = NextUnwind(4 + 2 * 2 + EntrySize);
            _bytes[at] = 1 | (ChainInfo << 3);
            _bytes[at + 2] = 1;
            WriteEntry(at + 4 + 2 * 2, parent);

            return at;
        }

        private int NextUnwind(int size)
        {
            var at = _nextUnwind;
            _nextUnwind += size;

            return at;
        }

        private void WriteEntry(int at, (int Begin, int End, int Unwind) entry)
        {
            BinaryPrimitives.WriteInt32LittleEndian(_bytes.AsSpan(at), entry.Begin);
            BinaryPrimitives.WriteInt32LittleEndian(_bytes.AsSpan(at + 4), entry.End);
            BinaryPrimitives.WriteInt32LittleEndian(_bytes.AsSpan(at + 8), entry.Unwind);
        }
    }
}
