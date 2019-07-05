using System.IO.BACnet.Serialize;
using System.IO.BACnet.Tests.TestData;
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
            APDU.EncodeConfirmedServiceRequest(buffer,
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
            var data = ASHRAE.F_2_1();

            // example taken from ANNEX F - Examples of APDU Encoding - F.2.1
            var expectedBytes = new byte[]
            {
                0x30, 0x00, 0x06, 0x10, 0x0E, 0x31, 0x00, 0x65, 0x1B, 0x43, 0x68, 0x69, 0x6C, 0x6C, 0x65, 0x72, 0x30,
                0x31, 0x20, 0x4F, 0x6E, 0x2D, 0x54, 0x69, 0x6D, 0x65, 0x3D, 0x34, 0x2E, 0x33, 0x20, 0x48, 0x6F, 0x75,
                0x72, 0x73, 0x0F
            };

            // act
            APDU.EncodeComplexAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, 0);

            FileAccessServices.EncodeAtomicReadFileAcknowledge(buffer, data.IsStream, data.EndOfFile, data.Position,
                data.BlockCount, data.Blocks, data.Counts);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_decode_atomicreadfilerequest_data_ack_after_encode()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var input = ASHRAE.F_2_1();

            // act
            FileAccessServices.EncodeAtomicReadFileAcknowledge(buffer, input.IsStream, input.EndOfFile, input.Position,
                input.BlockCount, input.Blocks, input.Counts);
            FileAccessServices.DecodeAtomicReadFileAcknowledge(buffer.buffer, 0, buffer.GetLength(), out var endOfFile,
                out var isStream, out var position, out var count, out var targetBuffer, out var targetOffset);

            // assert
            Assert.That(endOfFile, Is.EqualTo(input.EndOfFile));
            Assert.That(isStream, Is.EqualTo(input.IsStream));
            Assert.That(position, Is.EqualTo(input.Position));
            Assert.That(count, Is.EqualTo(input.Counts[0]));
            Assert.That(targetBuffer.Skip(targetOffset).Take((int)count), Is.EquivalentTo(input.Blocks[0]));
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
            APDU.EncodeConfirmedServiceRequest(buffer,
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

        [TestCase(true)]
        [TestCase(false)]
        public void should_decode_atomicreadfile_after_encode(bool isStream)
        {
            // arrange
            var buffer = new EncodeBuffer();
            var oid = new BacnetObjectId(BacnetObjectTypes.OBJECT_FILE, 42);

            // act
            FileAccessServices.EncodeAtomicReadFile(buffer, isStream, oid, 5, 10);
            FileAccessServices.DecodeAtomicReadFile(buffer.buffer, 0, buffer.GetLength(), out var wasStream,
                out var objectId, out var position, out var count);

            Assert.That(wasStream, Is.EqualTo(isStream));
            Assert.That(objectId, Is.EqualTo(oid));
            Assert.That(position, Is.EqualTo(5));
            Assert.That(count, Is.EqualTo(10));
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
            APDU.EncodeConfirmedServiceRequest(buffer,
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
            APDU.EncodeConfirmedServiceRequest(buffer,
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

        [TestCase(true)]
        [TestCase(false)]
        public void should_decode_atomicwritefile_after_encode(bool isStream)
        {
            // arrange
            var buffer = new EncodeBuffer();
            var oid = new BacnetObjectId(BacnetObjectTypes.OBJECT_FILE, 42);
            var input = new[]
            {
                new byte[] {0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x20, 0x57, 0x6f, 0x72, 0x6c, 0x64, 0x21},
                new byte[] {0x42}
            };

            // act
            FileAccessServices.EncodeAtomicWriteFile(buffer, isStream, oid, 5, (uint)input.Length, input,
                input.Select(d => d.Length).ToArray());
            FileAccessServices.DecodeAtomicWriteFile(buffer.buffer, 0, buffer.GetLength(), out var wasStream,
                out var objectId, out var position, out var blockCount, out var blocks, out var counts);

            // assert
            Assert.That(wasStream, Is.EqualTo(isStream));
            Assert.That(objectId, Is.EqualTo(oid));
            Assert.That(position, Is.EqualTo(5));
            Assert.That(blockCount, Is.EqualTo((uint)(isStream ? 1 : input.Length)));
            for (var i = 0; i < (isStream ? 1 : input.Length); i++)
                Assert.That(blocks[i], Is.EqualTo(input[i]));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void should_decode_atomicwritefile_ack_after_encode(bool isStream)
        {
            // arrange
            var buffer = new EncodeBuffer();
            const int expectedPosition = 30;

            // act
            FileAccessServices.EncodeAtomicWriteFileAcknowledge(buffer, isStream, expectedPosition);
            FileAccessServices.DecodeAtomicWriteFileAcknowledge(buffer.buffer, 0, buffer.GetLength(), out var wasStream,
                out var position);

            // assert
            Assert.That(wasStream, Is.EqualTo(isStream));
            Assert.That(position, Is.EqualTo(expectedPosition));
        }
    }
}
