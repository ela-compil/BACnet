using System.IO.BACnet.Serialize;
using NUnit.Framework;

namespace System.IO.BACnet.Tests.Serialize
{
    [TestFixture]
    public class ServicesTests
    {
        [Test]
        public void should_encode_logrecord_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var record1 = new BacnetLogRecord(BacnetTrendLogValueType.TL_TYPE_REAL, 18.0, new DateTime(1998,3,23,19,54,27), 0);
            var record2 = new BacnetLogRecord(BacnetTrendLogValueType.TL_TYPE_REAL, 18.1, new DateTime(1998,3,23,19,56,27), 0);

            // example taken from ANNEX F - Examples of APDU Encoding
            var expectedBytes = new byte[]
            {
                0x0E, 0xA4, 0x62, 0x03, 0x17, 0x01, 0xB4, 0x13, 0x36, 0x1B, 0x00, 0x0F,
                0x1E, 0x2C, 0x41, 0x90, 0x00, 0x00, 0x1F, 0x2A, 0x04, 0x00, 0x0E, 0xA4,
                0x62, 0x03, 0x17, 0x01, 0xB4, 0x13, 0x38, 0x1B, 0x00, 0x0F, 0x1E, 0x2C,
                0x41, 0x90, 0xCC, 0xCD, 0x1F, 0x2A, 0x04, 0x00
            };

            // act
            Services.EncodeLogRecord(buffer, record1);
            Services.EncodeLogRecord(buffer, record2);
            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_decode_multiple_logrecords()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var record1 = new BacnetLogRecord(BacnetTrendLogValueType.TL_TYPE_REAL, 18.0, new DateTime(1998, 3, 23, 19, 54, 27), 0);
            var record2 = new BacnetLogRecord(BacnetTrendLogValueType.TL_TYPE_REAL, 18.1, new DateTime(1998, 3, 23, 19, 56, 27), 0);

            // act
            Services.EncodeLogRecord(buffer, record1);
            Services.EncodeLogRecord(buffer, record2);
            Services.DecodeLogRecord(buffer.buffer, 0, buffer.GetLength(), 2, out var decodedRecords);

            /*
             * Debug - write packet to network to analyze in WireShark
             *

            var client = new BacnetClient(new BacnetIpUdpProtocolTransport(48708, true));
            client.Start();
            client.ReadRangeResponse(new BacnetAddress(BacnetAddressTypes.IP, "192.168.1.99"), 42, null,
                new BacnetObjectId(BacnetObjectTypes.OBJECT_TRENDLOG, 1),
                new BacnetPropertyReference((uint) BacnetPropertyIds.PROP_LOG_BUFFER, ASN1.BACNET_ARRAY_ALL),
                BacnetResultFlags.LAST_ITEM, 2, buffer.ToArray(), BacnetReadRangeRequestTypes.RR_BY_TIME, 79201);
            */

            // assert
            Helper.AssertPropertiesAndFieldsAreEqual(record1, decodedRecords[0]);
            Helper.AssertPropertiesAndFieldsAreEqual(record2, decodedRecords[1]);
        }
    }
}
