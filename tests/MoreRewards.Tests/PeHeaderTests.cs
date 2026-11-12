using System;
using System.Buffers.Binary;
using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class PeHeaderTests
{
    private const int PeAt = 0x108;
    private const int OptionalAt = PeAt + 4 + 20;
    private const int SizeOfImageAt = OptionalAt + 56;
    private const int DirectoryCountAt = OptionalAt + 108;
    private const int ExceptionDirectoryAt = OptionalAt + 112 + 3 * 8;
    private const int GameAssemblySize = 0x4000000;

    /// <summary>Where GameAssembly.dll 1.1.20719 keeps its exception table.</summary>
    private static readonly ExceptionTable GameTable = new(0x627b000, 0x35133c);

    [Fact]
    public void The_size_of_a_module_is_read_from_its_header()
    {
        Assert.Equal(GameAssemblySize, PeHeader.SizeOfImage(FirstPage()));
    }

    [Fact]
    public void A_page_without_the_dos_signature_has_no_size()
    {
        var page = FirstPage();
        page[0] = 0;

        Assert.Null(PeHeader.SizeOfImage(page));
    }

    [Fact]
    public void A_page_without_the_pe_signature_has_no_size()
    {
        var page = FirstPage();
        page[PeAt] = 0;

        Assert.Null(PeHeader.SizeOfImage(page));
    }

    [Theory]
    [InlineData(-8)]
    [InlineData(PeHeader.PageSize - 4)]
    [InlineData(int.MaxValue)]
    public void A_pe_offset_off_the_page_is_never_followed(int peAt)
    {
        var page = FirstPage();
        BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(0x3c), peAt);

        Assert.Null(PeHeader.SizeOfImage(page));
    }

    [Fact]
    public void A_page_too_short_for_a_header_has_no_size()
    {
        Assert.Null(PeHeader.SizeOfImage(new byte[] { (byte)'M', (byte)'Z' }));
    }

    [Fact]
    public void The_exception_table_is_read_from_a_64_bit_header()
    {
        Assert.Equal(GameTable, PeHeader.ExceptionTableOf(FirstPage()));
    }

    [Fact]
    public void A_32_bit_module_has_no_exception_table_to_read()
    {
        var page = FirstPage();
        BinaryPrimitives.WriteUInt16LittleEndian(page.AsSpan(OptionalAt), 0x10b);

        Assert.Null(PeHeader.ExceptionTableOf(page));
    }

    [Fact]
    public void A_header_with_too_few_directories_has_no_exception_table()
    {
        var page = FirstPage();
        BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(DirectoryCountAt), 3);

        Assert.Null(PeHeader.ExceptionTableOf(page));
    }

    [Fact]
    public void An_empty_exception_directory_is_no_table()
    {
        var page = FirstPage();
        BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(ExceptionDirectoryAt + 4), 0);

        Assert.Null(PeHeader.ExceptionTableOf(page));
    }

    [Fact]
    public void A_page_without_the_pe_signature_has_no_exception_table()
    {
        var page = FirstPage();
        page[PeAt] = 0;

        Assert.Null(PeHeader.ExceptionTableOf(page));
    }

    /// <summary>
    /// A fake first page of a 64-bit module: "MZ", the offset of "PE\0\0", then the optional header's magic, SizeOfImage,
    /// the number of data directories and the exception directory where the optional header keeps them.
    /// </summary>
    private static byte[] FirstPage()
    {
        var page = new byte[PeHeader.PageSize];
        page[0] = (byte)'M';
        page[1] = (byte)'Z';
        BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(0x3c), PeAt);
        page[PeAt] = (byte)'P';
        page[PeAt + 1] = (byte)'E';
        BinaryPrimitives.WriteUInt16LittleEndian(page.AsSpan(OptionalAt), 0x20b);
        BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(SizeOfImageAt), GameAssemblySize);
        BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(DirectoryCountAt), 16);
        BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(ExceptionDirectoryAt), GameTable.Rva);
        BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(ExceptionDirectoryAt + 4), GameTable.Size);

        return page;
    }
}
