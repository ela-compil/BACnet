using System.IO.BACnet.Serialize;
using NUnit.Framework;
using static System.IO.BACnet.Tests.Helper;

namespace System.IO.BACnet.Tests.Serialize
{
    [TestFixture]
    class RemoteDeviceManagementServicesTests
    {
        [Test]
        public void should_encode_devicecommunicationcontrolrequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.4.1
            var expectedBytes = new byte[]
            {
                0x00, 0x04, 0x05, 0x11, 0x09, 0x05, 0x19, 0x01, 0x2D, 0x08, 0x00, 0x23, 0x65, 0x67, 0x62, 0x64, 0x66,
                0x21
            };

            // act
            APDU.EncodeConfirmedServiceRequest(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_DEVICE_COMMUNICATION_CONTROL, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU1024, 5);

            RemoteDeviceManagementServices.EncodeDeviceCommunicationControl(buffer, 5, EnableDisable.DISABLE, "#egbdf!");

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_devicecommunicationcontrolrequest_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.4.1
            var expectedBytes = new byte[] { 0x20, 0x05, 0x11 };

            // act
            APDU.EncodeSimpleAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_DEVICE_COMMUNICATION_CONTROL, 5);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_confirmedprivatetransferservice_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var dataBuffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.4.2
            var expectedBytes = new byte[]
            {
                0x00, 0x04, 0x55, 0x12, 0x09, 0x19, 0x19, 0x08, 0x2E, 0x44, 0x42, 0x90, 0xCC, 0xCD, 0x62, 0x16, 0x49,
                0x2F
            };

            // act

            ASN1.encode_application_real(dataBuffer, 72.4f);
            ASN1.encode_application_octet_string(dataBuffer, new byte[] {0x16, 0x49}, 0, 2);

            APDU.EncodeConfirmedServiceRequest(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_PRIVATE_TRANSFER, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU1024, 85);

            RemoteDeviceManagementServices.EncodePrivateTransferConfirmed(buffer, 25, 8, dataBuffer.ToArray());

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_confirmedprivatetransferservice_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.4.3
            var expectedBytes = new byte[] {0x30, 0x55, 0x12, 0x09, 0x19, 0x19, 0x08};

            // act
            APDU.EncodeComplexAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_PRIVATE_TRANSFER, 85);
            RemoteDeviceManagementServices.EncodePrivateTransferAcknowledge(buffer, 25, 8, null);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_unconfirmedprivatetransferservice_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var dataBuffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.4.3
            var expectedBytes = new byte[]
                {0x10, 0x04, 0x09, 0x19, 0x19, 0x08, 0x2E, 0x44, 0x42, 0x90, 0xCC, 0xCD, 0x62, 0x16, 0x49, 0x2F};

            // act

            ASN1.encode_application_real(dataBuffer, 72.4f);
            ASN1.encode_application_octet_string(dataBuffer, new byte[] { 0x16, 0x49 }, 0, 2);

            APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_PRIVATE_TRANSFER);

            RemoteDeviceManagementServices.EncodePrivateTransferUnconfirmed(buffer, 25, 8, dataBuffer.ToArray());

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_reinitializedevicerequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.4.4
            var expectedBytes = new byte[]
                {0x00, 0x01, 0x02, 0x14, 0x09, 0x01, 0x1D, 0x09, 0x00, 0x41, 0x62, 0x43, 0x64, 0x45, 0x66, 0x47, 0x68};

            // act
            APDU.EncodeConfirmedServiceRequest(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_REINITIALIZE_DEVICE, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU128, 2);

            RemoteDeviceManagementServices.EncodeReinitializeDevice(buffer,
                BacnetReinitializedStates.BACNET_REINIT_WARMSTART, "AbCdEfGh");

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_reinitializedevicerequest_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.4.4
            var expectedBytes = new byte[] { 0x20, 0x02, 0x14 };

            // act
            APDU.EncodeSimpleAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_REINITIALIZE_DEVICE, 2);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_timesynchronizationrequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.4.7
            var expectedBytes = new byte[] {0x10, 0x06, 0xA4, 0x5C, 0x0B, 0x11, 0x02, 0xB4, 0x16, 0x2D, 0x1E, 0x46};

            // act
            APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_TIME_SYNCHRONIZATION);

            RemoteDeviceManagementServices.EncodeTimeSync(buffer,
                new DateTime(1992, 11, 17, 22, 45, 30).AddMilliseconds(700));

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_whohas_via_objectname_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.4.8
            var expectedBytes = new byte[] {0x10, 0x07, 0x3D, 0x07, 0x00, 0x4F, 0x41, 0x54, 0x65, 0x6D, 0x70};

            // act
            APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_HAS);

            RemoteDeviceManagementServices.EncodeWhoHasBroadcast(buffer, -1, -1, default(BacnetObjectId), "OATemp");

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_ihave_following_name_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.4.8
            var expectedBytes = new byte[]
            {
                0x10, 0x01, 0xC4, 0x02, 0x00, 0x00, 0x08, 0xC4, 0x00, 0x00, 0x00, 0x03, 0x75, 0x07, 0x00, 0x4F, 0x41,
                0x54, 0x65, 0x6D, 0x70
            };

            // act
            APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_HAVE);

            RemoteDeviceManagementServices.EncodeIhaveBroadcast(buffer,
                new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 8),
                new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 3), "OATemp");

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_whohas_via_id_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.4.8
            var expectedBytes = new byte[] {0x10, 0x07, 0x2C, 0x00, 0x00, 0x00, 0x03};

            // act
            APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_HAS);

            RemoteDeviceManagementServices.EncodeWhoHasBroadcast(buffer, -1, -1,
                new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 3), null);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_whois_via_id_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.4.9
            var expectedBytes = new byte[] { 0x10, 0x08, 0x09, 0x03, 0x19, 0x03 };

            // act
            APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS);

            RemoteDeviceManagementServices.EncodeWhoIsBroadcast(buffer, 3, 3);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_iam_following_id_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.4.9
            var expectedBytes = new byte[]
                {0x10, 0x00, 0xC4, 0x02, 0x00, 0x00, 0x03, 0x22, 0x04, 0x00, 0x91, 0x03, 0x21, 0x63};

            // act
            APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_AM);

            RemoteDeviceManagementServices.EncodeIamBroadcast(buffer, 3, 1024, BacnetSegmentations.SEGMENTATION_NONE,
                99);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_whois_all_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.4.9
            var expectedBytes = new byte[] { 0x10, 0x08 };

            // act
            APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS);

            RemoteDeviceManagementServices.EncodeWhoIsBroadcast(buffer, -1, -1);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        // example taken from ANNEX F - Examples of APDU Encoding - F.4.9
        [TestCase(new byte[] { 0x10, 0x00, 0xC4, 0x02, 0x00, 0x00, 0x01, 0x22, 0x01, 0xE0, 0x91, 0x01, 0x21, 0x63 },
            1, 480, BacnetSegmentations.SEGMENTATION_TRANSMIT, 99)]
        [TestCase(new byte[] { 0x10, 0x00, 0xC4, 0x02, 0x00, 0x00, 0x02, 0x21, 0xCE, 0x91, 0x02, 0x21, 0x21 }, 
            2, 206, BacnetSegmentations.SEGMENTATION_RECEIVE, 33)]
        [TestCase(new byte[] { 0x10, 0x00, 0xC4, 0x02, 0x00, 0x00, 0x03, 0x22, 0x04, 0x00, 0x91, 0x03, 0x21, 0x63 },
            3, 1024, BacnetSegmentations.SEGMENTATION_NONE, 99)]
        [TestCase(new byte[] { 0x10, 0x00, 0xC4, 0x02, 0x00, 0x00, 0x04, 0x21, 0x80, 0x91, 0x00, 0x21, 0x42 },
            4, 128, BacnetSegmentations.SEGMENTATION_BOTH, 66)]
        public void should_encode_iam_following_all_according_to_ashrae_example(byte[] expectedBytes, int deviceId,
            int maxApdu, BacnetSegmentations segmentation, int vendorId)
        {
            // arrange
            var buffer = new EncodeBuffer();

            // act
            APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_AM);

            RemoteDeviceManagementServices.EncodeIamBroadcast(buffer, (uint) deviceId, (uint) maxApdu, segmentation, (ushort) vendorId);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void GenerateCode()
        {
            Console.WriteLine(Doc2Code(@"
X'10' PDU Type=1 (Unconfirmed-Service-Request-PDU)
X'00' Service Choice=0 (I-Am-Request)
X'C4' Application Tag 12 (Object Identifier, L=4) (I-Am Device Identifier)
X'02000004' Device, Instance Number=4
X'21' Application Tag 2 (Unsigned Integer, L=1) (Max APDU Length Accepted)
X'80' 128
X'91' Application Tag 9 (Enumerated, L=1) (Segmentation Supported)
X'00' 0 (SEGMENTED_BOTH)
X'21' Application Tag 2 (Unsigned Integer, L=1) (Vendor ID)
X'42' 66
"));
        }
    }
}
