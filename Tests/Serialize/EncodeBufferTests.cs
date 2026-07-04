using System.IO.BACnet.Serialize;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// A fixed-size EncodeBuffer whose max_offset was raised past the physical
/// array (the segment encoder does this when the APDU limit exceeds the
/// transport payload) must flag NotEnoughBuffer instead of throwing.
/// </summary>
public class EncodeBufferTests
{
    [Fact]
    public void Add_beyond_physical_buffer_flags_NotEnoughBuffer_instead_of_throwing()
    {
        var buffer = new EncodeBuffer(new byte[8], 0) { max_offset = 12 };

        for (var i = 0; i < 12; i++)
            buffer.Add((byte)i);

        Assert.True((buffer.result & EncodeResult.NotEnoughBuffer) > 0);
        Assert.Equal(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }, buffer.buffer);
    }

    [Fact]
    public void Add_within_buffer_and_max_offset_stays_Good()
    {
        var buffer = new EncodeBuffer(new byte[8], 0);

        for (var i = 0; i < 8; i++)
            buffer.Add((byte)i);

        Assert.Equal(EncodeResult.Good, buffer.result);
        Assert.Equal(8, buffer.GetLength());
    }
}
