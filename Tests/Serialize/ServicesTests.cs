using System.Collections.Generic;
using System.IO.BACnet.EventNotification;
using System.IO.BACnet.EventNotification.EventValues;
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
            var record1 = new BacnetLogRecord(BacnetTrendLogValueType.TL_TYPE_REAL, 18.0,
                new DateTime(1998, 3, 23, 19, 54, 27), 0);
            var record2 = new BacnetLogRecord(BacnetTrendLogValueType.TL_TYPE_REAL, 18.1,
                new DateTime(1998, 3, 23, 19, 56, 27), 0);

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
            var record1 = new BacnetLogRecord(BacnetTrendLogValueType.TL_TYPE_REAL, 18.0,
                new DateTime(1998, 3, 23, 19, 54, 27), 0);
            var record2 = new BacnetLogRecord(BacnetTrendLogValueType.TL_TYPE_REAL, 18.1,
                new DateTime(1998, 3, 23, 19, 56, 27), 0);

            // act
            Services.EncodeLogRecord(buffer, record1);
            Services.EncodeLogRecord(buffer, record2);
            Services.DecodeLogRecord(buffer.buffer, 0, buffer.GetLength(), 2, out var decodedRecords);

            /*
             * Debug - write packet to network to analyze in WireShark
             *

            var client = new BacnetClient(new BacnetIpUdpProtocolTransport(47808, true));
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

        [Test]
        public void should_encode_confirmendcovnotificationrequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = new[]
            {
                new BacnetPropertyValue
                {
                    property = new BacnetPropertyReference((uint) BacnetPropertyIds.PROP_PRESENT_VALUE,
                        ASN1.BACNET_ARRAY_ALL),
                    value = new List<BacnetValue> {new BacnetValue((float) 65.0)}
                },
                new BacnetPropertyValue
                {
                    property = new BacnetPropertyReference((uint) BacnetPropertyIds.PROP_STATUS_FLAGS,
                        ASN1.BACNET_ARRAY_ALL),
                    value = new List<BacnetValue> {new BacnetValue(BacnetBitString.Parse("0000"))}
                }
            };

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.2
            var expectedBytes = new byte[]
            {
                0x00, 0x02, 0x0F, 0x01,

                0x09, 0x12, 0x1C, 0x02, 0x00, 0x00, 0x04, 0x2C, 0x00, 0x00, 0x00, 0x0A,
                0x39, 0x00, 0x4E, 0x09, 0x55, 0x2E, 0x44, 0x42, 0x82, 0x00, 0x00, 0x2F,
                0x09, 0x6F, 0x2E, 0x82, 0x04, 0x00, 0x2F, 0x4F
            };

            // act
            APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
                BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU206, 15);

            Services.EncodeCOVNotifyConfirmed(buffer, 18, 4,
                new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 10), 0, data);
            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_confirmendcovnotificationrequest_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.2
            var expectedBytes = new byte[]
            {
                0x20, 0x0F, 0x01
            };

            // act
            APDU.EncodeSimpleAck(buffer, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK,
                BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION, 15);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_unconfirmendcovnotificationrequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = new[]
            {
                new BacnetPropertyValue
                {
                    property = new BacnetPropertyReference((uint) BacnetPropertyIds.PROP_PRESENT_VALUE,
                        ASN1.BACNET_ARRAY_ALL),
                    value = new List<BacnetValue> {new BacnetValue((float) 65.0)}
                },
                new BacnetPropertyValue
                {
                    property = new BacnetPropertyReference((uint) BacnetPropertyIds.PROP_STATUS_FLAGS,
                        ASN1.BACNET_ARRAY_ALL),
                    value = new List<BacnetValue> {new BacnetValue(BacnetBitString.Parse("0000"))}
                }
            };

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.3
            var expectedBytes = new byte[]
            {
                0x10, 0x02,

                0x09, 0x12, 0x1C, 0x02, 0x00, 0x00, 0x04, 0x2C, 0x00, 0x00, 0x00, 0x0A,
                0x39, 0x00, 0x4E, 0x09, 0x55, 0x2E, 0x44, 0x42, 0x82, 0x00, 0x00, 0x2F,
                0x09, 0x6F, 0x2E, 0x82, 0x04, 0x00, 0x2F, 0x4F
            };

            // act
            APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST,
                BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_COV_NOTIFICATION);

            Services.EncodeCOVNotifyConfirmed(buffer, 18, 4,
                new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 10), 0, data);
            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_confirmendeventnotificationrequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = new StateTransition<OutOfRange>(new OutOfRange()
            {
                ExceedingValue = (float) 80.1,
                StatusFlags = BacnetBitString.Parse("1000"),
                Deadband = (float) 1.0,
                ExceededLimit = (float) 80.0
            })
            {
                ProcessIdentifier = 1,
                InitiatingObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 4),
                EventObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 2),
                TimeStamp = new BacnetGenericTime(default(DateTime), BacnetTimestampTags.TIME_STAMP_SEQUENCE, 16),
                NotificationClass = 4,
                Priority = 100,
                NotifyType = BacnetNotifyTypes.NOTIFY_ALARM,
                AckRequired = true,
                FromState = BacnetEventStates.EVENT_STATE_NORMAL,
                ToState = BacnetEventStates.EVENT_STATE_HIGH_LIMIT
            };

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.4
            var expectedBytes = new byte[]
            {
                0x00, 0x02, 0x10, 0x02, 0x09, 0x01, 0x1C, 0x02, 0x00, 0x00, 0x04, 0x2C, 0x00, 0x00, 0x00, 0x02,
                0x3E, 0x19, 0x10, 0x3F, 0x49, 0x04, 0x59, 0x64, 0x69, 0x05, 0x89, 0x00, 0x99, 0x01, 0xA9, 0x00,
                0xB9, 0x03, 0xCE, 0x5E, 0x0C, 0x42, 0xA0, 0x33, 0x33, 0x1A, 0x04, 0x80, 0x2C, 0x3F, 0x80, 0x00,
                0x00, 0x3C, 0x42, 0xA0, 0x00, 0x00, 0x5F, 0xCF
            };

            // act
            APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
                BacnetConfirmedServices.SERVICE_CONFIRMED_EVENT_NOTIFICATION, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU206, 16);

            Services.EncodeEventNotifyData(buffer, data);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_confirmendeventnotificationrequest_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.4
            var expectedBytes = new byte[]
            {
                0x20, 0x10, 0x02
            };

            // act
            APDU.EncodeSimpleAck(buffer, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK,
                BacnetConfirmedServices.SERVICE_CONFIRMED_EVENT_NOTIFICATION, 16);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));

        }

        [Test]
        public void should_encode_acknowledgealarmrequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.4
            var expectedBytes = new byte[]
            {
                0x00, 0x02, 0x07, 0x00, 0x09, 0x01, 0x1C, 0x00, 0x00, 0x00, 0x02, 0x29, 0x03,
                0x3E, 0x19, 0x10, 0x3F, 0x4C, 0x00, 0x4D, 0x44, 0x4C, 0x5E, 0x2E, 0xA4, 0x5C,
                0x06, 0x15, 0x07 /* instead of FF */, 0xB4, 0x0D, 0x03, 0x29, 0x09, 0x2F, 0x5F
            };

            // act
            APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
                BacnetConfirmedServices.SERVICE_CONFIRMED_ACKNOWLEDGE_ALARM, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU206, 7);

            Services.EncodeAlarmAcknowledge(buffer, 1, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 2),
                (uint) BacnetEventStates.EVENT_STATE_HIGH_LIMIT, "MDL",
                new BacnetGenericTime(default(DateTime), BacnetTimestampTags.TIME_STAMP_SEQUENCE, 16),
                new BacnetGenericTime(new DateTime(1992, 6, 21, 13, 3, 41).AddMilliseconds(90),
                    BacnetTimestampTags.TIME_STAMP_DATETIME));

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_acknowledgealarmrequest_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.4
            var expectedBytes = new byte[]
            {
                0x20, 0x07, 0x00
            };

            // act
            APDU.EncodeSimpleAck(buffer, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK,
                BacnetConfirmedServices.SERVICE_CONFIRMED_ACKNOWLEDGE_ALARM, 7);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));

        }

        [Test]
        public void should_encode_unconfirmedeventnotificationrequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = new StateTransition<OutOfRange>(new OutOfRange()
            {
                ExceedingValue = (float)80.1,
                StatusFlags = BacnetBitString.Parse("1000"),
                Deadband = (float)1.0,
                ExceededLimit = (float)80.0
            })
            {
                ProcessIdentifier = 1,
                InitiatingObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 9),
                EventObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 2),
                TimeStamp = new BacnetGenericTime(default(DateTime), BacnetTimestampTags.TIME_STAMP_SEQUENCE, 16),
                NotificationClass = 4,
                Priority = 100,
                NotifyType = BacnetNotifyTypes.NOTIFY_ALARM,
                AckRequired = true,
                FromState = BacnetEventStates.EVENT_STATE_NORMAL,
                ToState = BacnetEventStates.EVENT_STATE_HIGH_LIMIT
            };

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.5
            var expectedBytes = new byte[]
            {
                0x10, 0x03, 0x09, 0x01, 0x1C, 0x02, 0x00, 0x00, 0x09, 0x2C, 0x00, 0x00, 0x00, 0x02, 0x3E, 0x19, 0x10,
                0x3F, 0x49, 0x04, 0x59, 0x64, 0x69, 0x05, 0x89, 0x00, 0x99, 0x01, 0xA9, 0x00, 0xB9, 0x03, 0xCE, 0x5E,
                0x0C, 0x42, 0xA0, 0x33, 0x33, 0x1A, 0x04, 0x80, 0x2C, 0x3F, 0x80, 0x00, 0x00, 0x3C, 0x42, 0xA0, 0x00,
                0x00, 0x5F, 0xCF
            };

            // act
            APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST,
                BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_EVENT_NOTIFICATION);

            Services.EncodeEventNotifyData(buffer, data);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_getalarmsummary_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.6
            var expectedBytes = new byte[]
            {
                0x00, 0x02, 0x01, 0x03
            };

            // act
            APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
                BacnetConfirmedServices.SERVICE_CONFIRMED_GET_ALARM_SUMMARY, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU206, 1);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));

        }

        [Test]
        public void should_encode_getalarmsummary_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.6

            var expectedBytes = new byte[]
            {
                0x30, 0x01, 0x03, 0xC4, 0x00, 0x00, 0x00, 0x02, 0x91, 0x03, 0x82, 0x05, 0x60, 0xC4, 0x00, 0x00, 0x00,
                0x03, 0x91, 0x04, 0x82, 0x05, 0xE0
            };

            // act
            APDU.EncodeComplexAck(buffer, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK,
                BacnetConfirmedServices.SERVICE_CONFIRMED_GET_ALARM_SUMMARY, 1);

            Services.EncodeAlarmSummary(buffer, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 2),
                BacnetEventStates.EVENT_STATE_HIGH_LIMIT, BacnetBitString.Parse("011"));

            Services.EncodeAlarmSummary(buffer, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 3),
                BacnetEventStates.EVENT_STATE_LOW_LIMIT, BacnetBitString.Parse("111"));

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_geteventinformation_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.8
            var expectedBytes = new byte[]
            {
                0x00 /* docs say 0x02, but that's wrong! */, 0x02, 0x01, 0x1D
            };

            // act
            APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
                BacnetConfirmedServices.SERVICE_CONFIRMED_GET_EVENT_INFORMATION, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU206, 1);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_geteventinformation_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            var data = new[]
            {
                new BacnetGetEventInformationData()
                {
                    objectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 2),
                    eventState = BacnetEventStates.EVENT_STATE_HIGH_LIMIT,
                    acknowledgedTransitions = BacnetBitString.Parse("011"),
                    eventTimeStamps = new[]
                    {
                        new BacnetGenericTime(new DateTime(1, 1, 1, 15, 35, 00).AddMilliseconds(200), BacnetTimestampTags.TIME_STAMP_TIME),
                        new BacnetGenericTime(default(DateTime), BacnetTimestampTags.TIME_STAMP_TIME),
                        new BacnetGenericTime(default(DateTime), BacnetTimestampTags.TIME_STAMP_TIME),
                    },
                    notifyType = BacnetNotifyTypes.NOTIFY_ALARM,
                    eventEnable = BacnetBitString.Parse("111"),
                    eventPriorities = new uint[] {15, 15, 20}
                },
                new BacnetGetEventInformationData()
                {
                objectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 3),
                eventState = BacnetEventStates.EVENT_STATE_NORMAL,
                acknowledgedTransitions = BacnetBitString.Parse("110"),
                eventTimeStamps = new[]
                {
                    new BacnetGenericTime(new DateTime(1, 1, 1, 15, 40, 00), BacnetTimestampTags.TIME_STAMP_TIME),
                    new BacnetGenericTime(default(DateTime), BacnetTimestampTags.TIME_STAMP_TIME),
                    new BacnetGenericTime(new DateTime(1, 1, 1, 15, 45, 30).AddMilliseconds(300), BacnetTimestampTags.TIME_STAMP_TIME),
                },
                notifyType = BacnetNotifyTypes.NOTIFY_ALARM,
                eventEnable = BacnetBitString.Parse("111"),
                eventPriorities = new uint[] {15, 15, 20}
                }
            };

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.8
            var expectedBytes = new byte[]
            {
                0x30, 0x01, 0x1D, 0x0E, 0x0C, 0x00, 0x00, 0x00, 0x02, 0x19, 0x03, 0x2A, 0x05, 0x60, 0x3E, 0x0C, 0x0F,
                0x23, 0x00, 0x14, 0x0C, 0xFF, 0xFF, 0xFF, 0xFF, 0x0C, 0xFF, 0xFF, 0xFF, 0xFF, 0x3F, 0x49, 0x00, 0x5A,
                0x05, 0xE0, 0x6E, 0x21, 0x0F, 0x21, 0x0F, 0x21, 0x14, 0x6F, 0x0C, 0x00, 0x00, 0x00, 0x03, 0x19, 0x00,
                0x2A, 0x05, 0xC0, 0x3E, 0x0C, 0x0F, 0x28, 0x00, 0x00, 0x0C, 0xFF, 0xFF, 0xFF, 0xFF, 0x0C, 0x0F, 0x2D,
                0x1E, 0x1E, 0x3F, 0x49, 0x00, 0x5A, 0x05, 0xE0, 0x6E, 0x21, 0x0F, 0x21, 0x0F, 0x21, 0x14, 0x6F, 0x0F,
                0x19, 0x00
            };

            // act
            APDU.EncodeComplexAck(buffer, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK,
                BacnetConfirmedServices.SERVICE_CONFIRMED_GET_EVENT_INFORMATION, 1);

            Services.EncodeGetEventInformationAcknowledge(buffer, data, false);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void GenerateCode()
        {
            Console.WriteLine(Helper.Doc2Code(@"
X'30' PDU Type = 3 (BACnet-ComplexACK-PDU, SEG=0, MOR=0)
X'01' Invoke ID=01
X'1D' Service ACK Choice = 29, (GetEventInformation-ACK)
X'0E' PD opening Tag 0
X'0C' SD context Tag 0 (ObjectIdentifier, L=4)
X'00000002' Analog Input, Instance Number = 2
X'19' SD context Tag 1 (Enumerated, L=1)
X'03' 3 (HIGH_LIMIT)
X'2A' SD context Tag 2 (Bit String, L=2)
X'0560' 0,1,1 (FALSE, TRUE, TRUE)
X'3E' PD opening Tag 3
X'0C' SD context Tag 0 (Time L=4)
X'0F230014' Time 15:35:00.20
X'0C' SD context Tag 0 (Time L=4)
X'FFFFFFFF' Time unspecified
X'0C' SD context Tag 0 (Time L=4)
X'FFFFFFFF' Time unspecified
X'3F' PD closing Tag 3
X'49' SD context Tag 4 (Enumerated, L=1)
X'00' 0 (ALARM)
X'5A' SD context Tag 5 (Bit String, L=2)
X'05E0' 1,1,1 (TRUE, TRUE, TRUE)
X'6E' PD opening Tag 6
X'21' Application Tag 2 (Unsigned Integer, L=1)
X'0F' 15 (Priority)
X'21' Application Tag 2 (Unsigned Integer, L=1)
X'0F' 15 (Priority)
X'21' Application Tag 2 (Unsigned Integer, L=1)
X'14' 20 (Priority)
X'6F' PD closing Tag 6
X'0C' SD context Tag 0 (ObjectIdentifier, L=4)
X'00000003' Analog Input, Instance Number = 3
X'19' SD context Tag 1 (Enumerated, L=1)
X'00' 0 (NORMAL)
X'2A' SD context Tag 2 (Bit String, L=2)
X'05C0' 1,1,0 (TRUE, TRUE, FALSE)
X'3E' PD opening Tag 3
X'0C' SD context Tag 0 (Time L=4)
X'0F280000' Time 15:40:00.00
X'0C' SD context Tag 0 (Time L=4)
X'FFFFFFFF' Time unspecified
X'0C' SD context Tag 0 (Time L=4)
X'0F2D1E1E' 15:45:30.30
X'3F' PD closing Tag 3
X'49' SD context Tag 4 (Enumerated, L=1)
X'00' 0 (ALARM)
X'5A' SD context Tag 5 (Bit String, L=2)
X'05E0' 1,1,1 (TRUE, TRUE, TRUE)
X'6E' PD opening Tag 6
X'21' Application Tag 2 (Unsigned Integer, L=1)
X'0F' 15 (Priority)
X'21' Application Tag 2 (Unsigned Integer, L=1)
X'0F' 15 (Priority)
X'21' Application Tag 2 (Unsigned Integer, L=1)
X'14' 20 (Priority)
X'6F' PD closing Tag 6
X'0F' PD closing Tag 0
X'19' SD context Tag 1 (Boolean, L=1)
X'00' FALSE (More Events)
"));
        }
    }
}