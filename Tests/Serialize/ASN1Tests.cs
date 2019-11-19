using System.IO.BACnet.Serialize;
using NUnit.Framework;

namespace System.IO.BACnet.Tests.Serialize
{
    [TestFixture]
    public class ASN1Tests
    {
        public const int INT24_MAX_VALUE = 8_388_607;
        public const int INT24_MIN_VALUE = -8_388_608;

        [TestCase("2018-02-26", 1)]
        [TestCase("2018-02-27", 2)]
        [TestCase("2018-02-28", 3)]
        [TestCase("2018-03-01", 4)]
        [TestCase("2018-03-02", 5)]
        [TestCase("2018-03-03", 6)]
        [TestCase("2018-03-04", 7)]
        public void should_encode_dayofweek_according_to_standard(DateTime value, byte expectedDayOfWeek) // 1 = Monday .. 7 = Sunday
        {
            // arrange
            var buffer = new EncodeBuffer();

            // act
            ASN1.encode_bacnet_date(buffer, value);

            // assert
            Assert.That(buffer.GetLength(), Is.EqualTo(4));
            Assert.That(buffer.buffer[3], Is.EqualTo(expectedDayOfWeek));
        }

        [TestCase(new byte[] { 0x00, 0x00, 0x00 }, ExpectedResult = 0)]
        [TestCase(new byte[] { 0x00, 0x00, 0x01 }, ExpectedResult = 1)]
        [TestCase(new byte[] { 0xFF, 0xFF, 0xFF }, ExpectedResult = -1)]
        [TestCase(new byte[] { 0x7F, 0xFF, 0xFF }, ExpectedResult = INT24_MAX_VALUE)]
        [TestCase(new byte[] { 0x80, 0x00, 0x00 }, ExpectedResult = INT24_MIN_VALUE)]
        public int should_decode_signed24_value(byte[] buffer)
        {
            // act
            ASN1.decode_signed24(buffer, 0, out var value);
            return value;
        }

        [TestCase(sbyte.MinValue, ExpectedResult = new byte[] { 0x80 })]
        [TestCase(sbyte.MaxValue, ExpectedResult = new byte[] { 0x7F })]
        [TestCase(short.MinValue, ExpectedResult = new byte[] { 0x80, 0x00 })]
        [TestCase(short.MaxValue, ExpectedResult = new byte[] { 0x7F, 0xFF })]
        [TestCase(INT24_MIN_VALUE, ExpectedResult = new byte[] { 0x80, 0x00, 0x00 })]
        [TestCase(INT24_MAX_VALUE, ExpectedResult = new byte[] { 0x7F, 0xFF, 0xFF })]
        [TestCase(int.MinValue, ExpectedResult = new byte[] { 0x80, 0x00, 0x00, 0x00 })]
        [TestCase(int.MaxValue, ExpectedResult = new byte[] { 0x7F, 0xFF, 0xFF, 0xFF })]
        public byte[] should_encode_signed_value(int value)
        {
            // arrange
            var buffer = new EncodeBuffer();

            // act
            ASN1.encode_bacnet_signed(buffer, value);
            return buffer.ToArray();
        }
    }
}
