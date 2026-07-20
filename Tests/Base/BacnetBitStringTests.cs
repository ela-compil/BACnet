using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// BacnetBitString.ConvertFromInt bit-length calculation (regression for #8 / #11):
/// bits_used must be floor(log2(value)) + 1, which the old Math.Ceiling(log2) got wrong
/// for exact powers of two and for value 1.
/// </summary>
public class BacnetBitStringTests
{
    [Theory]
    [InlineData(1u, 1)]    // 0b1
    [InlineData(2u, 2)]    // 0b10
    [InlineData(3u, 2)]    // 0b11
    [InlineData(4u, 3)]    // 0b100  - old Ceiling(log2) gave 2
    [InlineData(5u, 3)]
    [InlineData(7u, 3)]    // 0b111
    [InlineData(8u, 4)]    // 0b1000 - old gave 3
    [InlineData(255u, 8)]
    [InlineData(256u, 9)]  // 0b1_0000_0000
    public void ConvertFromInt_uses_correct_bit_length(uint value, int expectedBits)
    {
        Assert.Equal(expectedBits, BacnetBitString.ConvertFromInt(value).bits_used);
    }

    [Theory]
    [InlineData(0b0000u)] // no flags
    [InlineData(0b0010u)] // fault only
    [InlineData(0b1111u)] // all four
    public void ConvertFromInt_honors_explicit_bits_used(uint value)
    {
        // BACnetStatusFlags is a fixed 4-bit string regardless of which bits are set.
        Assert.Equal(4, BacnetBitString.ConvertFromInt(value, 4).bits_used);
    }

    [Fact]
    public void SetBit_then_GetBit_reads_back_every_bit()
    {
        for (byte i = 0; i < 32; i++)
        {
            var bitString = new BacnetBitString();
            bitString.SetBit(i, true);
            Assert.True(bitString.GetBit(i));
        }
    }

    [Fact]
    public void SetBit_grows_bits_used_to_one_past_the_highest_bit()
    {
        for (byte i = 0; i < 32; i++)
        {
            var bitString = new BacnetBitString();
            bitString.SetBit(i, true);
            Assert.Equal(i + 1, bitString.bits_used);
        }
    }

    [Fact]
    public void SetBit_to_false_still_extends_the_string()
    {
        // Clearing a bit that was never set still marks the string as that wide.
        var bitString = new BacnetBitString();
        bitString.SetBit(2, false);
        Assert.Equal(3, bitString.bits_used);
    }

    [Fact]
    public void ConvertToInt_reads_back_a_single_set_bit()
    {
        for (byte i = 0; i < 32; i++)
        {
            var bitString = new BacnetBitString();
            bitString.SetBit(i, true);
            Assert.Equal(1u << i, bitString.ConvertToInt());
        }
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData(0b0000_1000u)]
    [InlineData(0b0101_0111_0010_0110u)]
    [InlineData(0x40000000u)]
    [InlineData(uint.MaxValue)]
    public void ConvertFromInt_then_ConvertToInt_round_trips(uint value)
    {
        Assert.Equal(value, BacnetBitString.ConvertFromInt(value).ConvertToInt());
    }

    [Fact]
    public void ConvertFromInt_sets_the_bit_for_each_power_of_two()
    {
        for (byte i = 0; i < 32; i++)
        {
            var bitString = BacnetBitString.ConvertFromInt(1u << i);
            Assert.True(bitString.GetBit(i));
        }
    }
}
