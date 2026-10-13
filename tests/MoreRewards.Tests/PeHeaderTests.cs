using System;
using System.Buffers.Binary;
using MoreRewards.Core;
using Xunit;

namespace MoreRewards.Tests;

public class PeHeaderTests
{
    private const int PeAt = 0x108;
    private const int SizeOfImageAt = PeAt + 4 + 20 + 56;
    private const int GameAssemblySize = 0x4000000;

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

    /// <summary>A fake first page of a module: "MZ", the offset of "PE\0\0", then SizeOfImage where the optional header keeps it.</summary>
    private static byte[] FirstPage()
    {
        var page = new byte[PeHeader.PageSize];
        page[0] = (byte)'M';
        page[1] = (byte)'Z';
        BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(0x3c), PeAt);
        page[PeAt] = (byte)'P';
        page[PeAt + 1] = (byte)'E';
        BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(SizeOfImageAt), GameAssemblySize);

        return page;
    }
}
