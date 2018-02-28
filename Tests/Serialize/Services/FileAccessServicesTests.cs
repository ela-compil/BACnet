using System.IO.BACnet.Serialize;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace System.IO.BACnet.Tests.Serialize
{
    [TestFixture]
    class FileAccessServicesTests
    {
        [Test]
        public void should_encode_atomicreadfilerequest_data_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.2.1
            var expectedBytes = new byte[]
                {0x00, 0x02, 0x00, 0x06, 0xC4, 0x02, 0x80, 0x00, 0x01, 0x0E, 0x31, 0x00, 0x21, 0x1B, 0x0F};

            // act
            APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
                BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU206, 0);

            FileAccessServices.EncodeAtomicReadFile(buffer, true, new BacnetObjectId(BacnetObjectTypes.OBJECT_FILE, 1),
                0, 27);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_atomicreadfilerequest_data_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = new[]
            {
                Encoding.ASCII.GetBytes("Chiller01 On-Time=4.3 Hours")
            };

            // example taken from ANNEX F - Examples of APDU Encoding - F.2.1
            var expectedBytes = new byte[]
            {
                0x30, 0x00, 0x06, 0x10, 0x0E, 0x31, 0x00, 0x65, 0x1B, 0x43, 0x68, 0x69, 0x6C, 0x6C, 0x65, 0x72, 0x30,
                0x31, 0x20, 0x4F, 0x6E, 0x2D, 0x54, 0x69, 0x6D, 0x65, 0x3D, 0x34, 0x2E, 0x33, 0x20, 0x48, 0x6F, 0x75,
                0x72, 0x73, 0x0F
            };

            // act
            APDU.EncodeComplexAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, 0);

            FileAccessServices.EncodeAtomicReadFileAcknowledge(buffer, true, false, 0, 1, data,
                data.Select(arr => arr.Length).ToArray());

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_atomicreadfilerequest_record_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.2.1
            var expectedBytes = new byte[]
                {0x00, 0x02, 0x12, 0x06, 0xC4, 0x02, 0x80, 0x00, 0x02, 0x1E, 0x31, 0x0E, 0x21, 0x03, 0x1F};

            // act
            APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
                BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU206, 18);

            FileAccessServices.EncodeAtomicReadFile(buffer, false, new BacnetObjectId(BacnetObjectTypes.OBJECT_FILE, 2),
                14, 3);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_atomicreadfilerequest_record_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = new[]
            {
                Encoding.ASCII.GetBytes("12:00,45.6"),
                Encoding.ASCII.GetBytes("12:15,44.8")
            };

            // example taken from ANNEX F - Examples of APDU Encoding - F.2.1
            var expectedBytes = new byte[]
            {
                0x30, 0x12, 0x06, 0x11, 0x1E, 0x31, 0x0E, 0x21, 0x02, 0x65, 0x0A, 0x31, 0x32, 0x3A, 0x30, 0x30, 0x2C,
                0x34, 0x35, 0x2E, 0x36, 0x65, 0x0A, 0x31, 0x32, 0x3A, 0x31, 0x35, 0x2C, 0x34, 0x34, 0x2E, 0x38, 0x1F
            };

            // act
            APDU.EncodeComplexAck(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, 18);

            FileAccessServices.EncodeAtomicReadFileAcknowledge(buffer, false, true, 14, 2, data,
                data.Select(arr => arr.Length).ToArray());

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_atomicwritefilerequest_data_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = new[]
            {
                Encoding.ASCII.GetBytes("Chiller01 On-Time=4.3 Hours")
            };

            // example taken from ANNEX F - Examples of APDU Encoding - F.2.2
            var expectedBytes = new byte[]
            {
                0x00, 0x02, 0x55, 0x07, 0xC4, 0x02, 0x80, 0x00, 0x01, 0x0E, 0x31, 0x1E, 0x65, 0x1B, 0x43, 0x68, 0x69,
                0x6C, 0x6C, 0x65, 0x72, 0x30, 0x31, 0x20, 0x4F, 0x6E, 0x2D, 0x54, 0x69, 0x6D, 0x65, 0x3D, 0x34, 0x2E,
                0x33, 0x20, 0x48, 0x6F, 0x75, 0x72, 0x73, 0x0F
            };
            // act
            APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
                BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU206, 85);

            FileAccessServices.EncodeAtomicWriteFile(buffer, true, new BacnetObjectId(BacnetObjectTypes.OBJECT_FILE, 1),
                30, 1, data, data.Select(arr => arr.Length).ToArray());

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_atomicwritefilerequest_data_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.2.2
            var expectedBytes = new byte[] {0x30, 0x55, 0x07, 0x09, 0x1E};

            // act
            APDU.EncodeComplexAck(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, 85);

            FileAccessServices.EncodeAtomicWriteFileAcknowledge(buffer, true, 30);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_atomicwritefilerequest_record_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = new[]
            {
                Encoding.ASCII.GetBytes("12:00,45.6"),
                Encoding.ASCII.GetBytes("12:15,44.8")
            };

            // example taken from ANNEX F - Examples of APDU Encoding - F.2.2
            var expectedBytes = new byte[]
            {
                0x00, 0x02, 0x55, 0x07, 0xC4, 0x02, 0x80, 0x00, 0x02, 0x1E, 0x31, 0xFF, 0x21, 0x02, 0x65, 0x0A, 0x31,
                0x32, 0x3A, 0x30, 0x30, 0x2C, 0x34, 0x35, 0x2E, 0x36, 0x65, 0x0A, 0x31, 0x32, 0x3A, 0x31, 0x35, 0x2C,
                0x34, 0x34, 0x2E, 0x38, 0x1F
            };

            // act
            APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
                BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU206, 85);

            FileAccessServices.EncodeAtomicWriteFile(buffer, false,
                new BacnetObjectId(BacnetObjectTypes.OBJECT_FILE, 2), -1, 2, data,
                data.Select(arr => arr.Length).ToArray());

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_atomicwritefilerequest_record_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.2.2
            var expectedBytes = new byte[] {0x30, 0x55, 0x07, 0x19, 0x0E};

            // act
            APDU.EncodeComplexAck(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, 85);

            FileAccessServices.EncodeAtomicWriteFileAcknowledge(buffer, false, 14);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }
    }
}
