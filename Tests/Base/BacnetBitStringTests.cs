using NUnit.Framework;

namespace System.IO.BACnet.Base
{
    public class BacnetBitStringTests
    {
        [Test]
        public void should_set_bit_test()
        {
            for (byte i = 0; i < 32; i++)
            {
                var bitString = new BacnetBitString();
                bitString.SetBit(i, true);
                Assert.AreEqual(true, bitString.GetBit(i));
            }
        }

        [Test]
        public void should_calculate_bits_used_when_setting_bit_to_false_test()
        {
            var bitString = new BacnetBitString();
            bitString.SetBit(2, false);
            Assert.AreEqual(3, bitString.bits_used);
        }

        [Test]
        public void should_calculate_bits_used_test()
        {
            for (byte i = 0; i < 32; i++)
            {
                var bitString = new BacnetBitString();
                bitString.SetBit(i, true);
                Assert.AreEqual(i + 1, bitString.bits_used);
            }
        }

        [Test]
        public void should_convert_to_uint_test()
        {
            for (byte i = 0; i < 32; i++)
            {
                var bitString = new BacnetBitString();
                bitString.SetBit(i, true);
                Assert.AreEqual((uint)(1 << i), bitString.ConvertToInt());
            }
        }
        
        [Test]
        public void should_set_bit_when_converting_from_uint_test()
        {
            for (byte i = 0; i < 32; i++)
            {
                var bitString = BacnetBitString.ConvertFromInt((uint)(1 << i));
                Assert.AreEqual(true, bitString.GetBit(i));
            }
        }

        [Test]
        public void should_respect_provided_bits_used_value_whe_converting_from_uint_test()
        {
            const byte bitsUsed = 3;
            var bitString = BacnetBitString.ConvertFromInt(0, bitsUsed);
            Assert.AreEqual(bitsUsed, bitString.bits_used);
        }

        [TestCase((uint)0b00000000000000000000000000000000, 0)]
        [TestCase((uint)0b00000000000000000000000000000001, 1)]
        [TestCase((uint)0b01000000000000000000000000000000, 31)]
        [TestCase((uint)0b01111111111111111111111111111111, 31)]
        [TestCase(0b10101010101010101010101010101010, 32)]
        [TestCase(0b10000000000000000000000000000000, 32)]
        [TestCase(0b11111111111111111111111111111111, 32)]
        public void should_calculate_bits_used_when_converting_from_uint_test(uint value, byte bitsUsed)
        {
            var bitString = BacnetBitString.ConvertFromInt(value);
            Assert.AreEqual(bitsUsed, bitString.bits_used);
        }

        [TestCase((uint)0b00000000000000000000000000000000)]
        [TestCase((uint)0b00000000000000000000000000000001)]
        [TestCase((uint)0b01000000000000000000000000000000)]
        [TestCase((uint)0b01111111111111111111111111111111)]
        [TestCase(0b10101010101010101010101010101010)]
        [TestCase(0b10000000000000000000000000000000)]
        [TestCase(0b11111111111111111111111111111111)]
        public void should_convert_back_to_uint_when_converting_from_uint_test(uint value)
        {
            var bitString = BacnetBitString.ConvertFromInt(value);
            Assert.AreEqual(value, bitString.ConvertToInt());
        }
    }
}