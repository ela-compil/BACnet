using System.IO.BACnet.Serialize;
using NUnit.Framework;

namespace System.IO.BACnet.Tests.Serialize
{
    [TestFixture]
    public class ASN1Tests
    {
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
    }
}
