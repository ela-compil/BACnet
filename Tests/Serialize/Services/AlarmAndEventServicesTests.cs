using System.Collections.Generic;
using System.IO.BACnet.EventNotification;
using System.IO.BACnet.EventNotification.EventValues;
using System.IO.BACnet.Serialize;
using System.IO.BACnet.Tests.TestData;
using System.Linq;
using NUnit.Framework;

namespace System.IO.BACnet.Tests.Serialize
{
    [TestFixture]
    public class AlarmAndEventServicesTests
    {
        [Test]
        public void should_encode_confirmendcovnotificationrequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = ASHRAE.F_1_2();

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.2
            var expectedBytes = new byte[]
            {
                0x00, 0x02, 0x0F, 0x01,

                0x09, 0x12, 0x1C, 0x02, 0x00, 0x00, 0x04, 0x2C, 0x00, 0x00, 0x00, 0x0A,
                0x39, 0x00, 0x4E, 0x09, 0x55, 0x2E, 0x44, 0x42, 0x82, 0x00, 0x00, 0x2F,
                0x09, 0x6F, 0x2E, 0x82, 0x04, 0x00, 0x2F, 0x4F
            };

            // act
            APDU.EncodeConfirmedServiceRequest(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU206, 15);

            AlarmAndEventServices.EncodeCOVNotify(buffer, data.SubscriberProcessIdentifier,
                data.InitiatingDeviceIdentifier, data.MonitoredObjectIdentifier, data.TimeRemaining, data.Values);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_decode_confirmendcovnotificationrequest_after_encode()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = ASHRAE.F_1_2();

            // act
            AlarmAndEventServices.EncodeCOVNotify(buffer, data.SubscriberProcessIdentifier,
                data.InitiatingDeviceIdentifier, data.MonitoredObjectIdentifier, data.TimeRemaining, data.Values);

            var encodedBytes = buffer.ToArray();

            AlarmAndEventServices.DecodeCOVNotify(Helper.DummyAddress, encodedBytes, 0, encodedBytes.Length,
                out var subscriberProcessIdentifier, out var initiatingDeviceIdentifier,
                out var monitoredObjectIdentifier, out var timeRemaining, out var values);

            var valuesArray = values.ToArray();

            // assert
            Assert.That(valuesArray.Length, Is.EqualTo(2));
            Assert.That(subscriberProcessIdentifier, Is.EqualTo(data.SubscriberProcessIdentifier));
            Assert.That(initiatingDeviceIdentifier.Instance, Is.EqualTo(data.InitiatingDeviceIdentifier));
            Assert.That(monitoredObjectIdentifier, Is.EqualTo(data.MonitoredObjectIdentifier));
            Assert.That(timeRemaining, Is.EqualTo(data.TimeRemaining));
            Helper.AssertPropertiesAndFieldsAreEqual(data.Values[0], valuesArray[0]);
            Helper.AssertPropertiesAndFieldsAreEqual(data.Values[1], valuesArray[1]);
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
            APDU.EncodeSimpleAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION, 15);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_unconfirmendcovnotificationrequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = ASHRAE.F_1_3();

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.3
            var expectedBytes = new byte[]
            {
                0x10, 0x02,

                0x09, 0x12, 0x1C, 0x02, 0x00, 0x00, 0x04, 0x2C, 0x00, 0x00, 0x00, 0x0A,
                0x39, 0x00, 0x4E, 0x09, 0x55, 0x2E, 0x44, 0x42, 0x82, 0x00, 0x00, 0x2F,
                0x09, 0x6F, 0x2E, 0x82, 0x04, 0x00, 0x2F, 0x4F
            };

            // act
            APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_COV_NOTIFICATION);

            AlarmAndEventServices.EncodeCOVNotify(buffer, data.SubscriberProcessIdentifier,
                data.InitiatingDeviceIdentifier, data.MonitoredObjectIdentifier, data.TimeRemaining, data.Values);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_confirmendeventnotificationrequest_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = ASHRAE.F_1_4();

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.4
            var expectedBytes = new byte[]
            {
                0x00, 0x02, 0x10, 0x02, 0x09, 0x01, 0x1C, 0x02, 0x00, 0x00, 0x04, 0x2C, 0x00, 0x00, 0x00, 0x02,
                0x3E, 0x19, 0x10, 0x3F, 0x49, 0x04, 0x59, 0x64, 0x69, 0x05, 0x89, 0x00, 0x99, 0x01, 0xA9, 0x00,
                0xB9, 0x03, 0xCE, 0x5E, 0x0C, 0x42, 0xA0, 0x33, 0x33, 0x1A, 0x04, 0x80, 0x2C, 0x3F, 0x80, 0x00,
                0x00, 0x3C, 0x42, 0xA0, 0x00, 0x00, 0x5F, 0xCF
            };

            // act
            APDU.EncodeConfirmedServiceRequest(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_EVENT_NOTIFICATION, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU206, 16);

            AlarmAndEventServices.EncodeEventNotifyData(buffer, data);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_decode_confirmendeventnotificationrequest_after_encode()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var input = ASHRAE.F_1_4();

            // act
            AlarmAndEventServices.EncodeEventNotifyData(buffer, input);

            var encodedBytes = buffer.ToArray();

            AlarmAndEventServices.DecodeEventNotifyData(encodedBytes, 0, encodedBytes.Length, out var output);

            // assert
            Assert.That(output, Is.Not.SameAs(input));
            Helper.AssertPropertiesAndFieldsAreEqual(input, output);
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
            APDU.EncodeSimpleAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_EVENT_NOTIFICATION, 16);

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
            APDU.EncodeConfirmedServiceRequest(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_ACKNOWLEDGE_ALARM, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU206, 7);

            AlarmAndEventServices.EncodeAlarmAcknowledge(buffer, 1, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 2),
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
            APDU.EncodeSimpleAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_ACKNOWLEDGE_ALARM, 7);

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
                ExceedingValue = 80.1f,
                StatusFlags = BacnetBitString.Parse("1000"),
                Deadband = 1.0f,
                ExceededLimit = 80.0f
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
            APDU.EncodeUnconfirmedServiceRequest(buffer,
                BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_EVENT_NOTIFICATION);

            AlarmAndEventServices.EncodeEventNotifyData(buffer, data);

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
            APDU.EncodeConfirmedServiceRequest(buffer,
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
            var data = ASHRAE.F_1_6();

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.6

            var expectedBytes = new byte[]
            {
                0x30, 0x01, 0x03, 0xC4, 0x00, 0x00, 0x00, 0x02, 0x91, 0x03, 0x82, 0x05, 0x60, 0xC4, 0x00, 0x00, 0x00,
                0x03, 0x91, 0x04, 0x82, 0x05, 0xE0
            };

            // act
            APDU.EncodeComplexAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_GET_ALARM_SUMMARY, 1);

            AlarmAndEventServices.EncodeAlarmSummary(buffer, data[0].ObjectIdentifier, data[0].AlarmState,
                data[0].AcknowledgedTransitions);

            AlarmAndEventServices.EncodeAlarmSummary(buffer, data[1].ObjectIdentifier, data[1].AlarmState,
                data[1].AcknowledgedTransitions);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_decode_alarmsummary_after_encode()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var input = ASHRAE.F_1_6();
            IList<BacnetAlarmSummaryData> output = new List<BacnetAlarmSummaryData>();


            // act
            AlarmAndEventServices.EncodeAlarmSummary(buffer, input[0].ObjectIdentifier, input[0].AlarmState,
                input[0].AcknowledgedTransitions);

            AlarmAndEventServices.EncodeAlarmSummary(buffer, input[1].ObjectIdentifier, input[1].AlarmState,
                input[1].AcknowledgedTransitions);

            var encodedBytes = buffer.ToArray();

            AlarmAndEventServices.DecodeAlarmSummary(encodedBytes, 0, encodedBytes.Length, ref output);

            // assert
            Assert.That(output.Count, Is.EqualTo(2));
            Helper.AssertPropertiesAndFieldsAreEqual(input[0], output[0]);
            Helper.AssertPropertiesAndFieldsAreEqual(input[1], output[1]);
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
            APDU.EncodeConfirmedServiceRequest(buffer,
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

            var data = ASHRAE.F_1_8();

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
            APDU.EncodeComplexAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_GET_EVENT_INFORMATION, 1);

            AlarmAndEventServices.EncodeGetEventInformationAcknowledge(buffer, data.Data, data.MoreEvents);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_decode_eventinformation_after_encode()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var input = ASHRAE.F_1_8();
            IList<BacnetGetEventInformationData> output = new List<BacnetGetEventInformationData>();

            // act
            AlarmAndEventServices.EncodeGetEventInformationAcknowledge(buffer, input.Data, input.MoreEvents);
            var encodedBytes = buffer.ToArray();
            AlarmAndEventServices.DecodeEventInformation(encodedBytes, 0, encodedBytes.Length, ref output, out var moreEvents);

            // assert
            Assert.That(moreEvents, Is.EqualTo(input.MoreEvents));
            Assert.That(output.Count, Is.EqualTo(2));
            Helper.AssertPropertiesAndFieldsAreEqual(input.Data[0], output[0]);
            Helper.AssertPropertiesAndFieldsAreEqual(input.Data[1], output[1]);
        }

        [Test]
        public void should_encode_lifesafetyoperation_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.9
            var expectedBytes = new byte[]
            {
                0x00, 0x02, 0x0F, 0x1B, 0x09, 0x12, 0x1C, 0x00, 0x4D, 0x44, 0x4C, 0x29, 0x04, 0x3C, 0x05, 0x40, 0x00,
                0x01
            };

            // act
            APDU.EncodeConfirmedServiceRequest(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_LIFE_SAFETY_OPERATION, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU206, 15);

            AlarmAndEventServices.EncodeLifeSafetyOperation(buffer, 18, "MDL",
                (uint) BacnetLifeSafetyOperations.LIFE_SAFETY_OP_RESET,
                new BacnetObjectId(BacnetObjectTypes.OBJECT_LIFE_SAFETY_POINT, 1));

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_lifesafetyoperation_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.9
            var expectedBytes = new byte[]
            {
                0x20, 0x0F, 0x1B
            };

            // act
            APDU.EncodeSimpleAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_LIFE_SAFETY_OPERATION, 15);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_subscribecov_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = ASHRAE.F_1_10();

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.10
            var expectedBytes = new byte[]
                {0x00, 0x02, 0x0F, 0x05, 0x09, 0x12, 0x1C, 0x00, 0x00, 0x00, 0x0A, 0x29, 0x01, 0x39, 0x00};

            // act
            APDU.EncodeConfirmedServiceRequest(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU206, 15);

            AlarmAndEventServices.EncodeSubscribeCOV(buffer, data.SubscriberProcessIdentifier,
                data.MonitoredObjectIdentifier, data.CancellationRequest, data.IssueConfirmedNotifications,
                data.Lifetime);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_decode_subscribecov_after_encode()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var input = ASHRAE.F_1_10();
            IList<BacnetGetEventInformationData> output = new List<BacnetGetEventInformationData>();

            // act
            AlarmAndEventServices.EncodeSubscribeCOV(buffer, input.SubscriberProcessIdentifier,
                input.MonitoredObjectIdentifier, input.CancellationRequest, input.IssueConfirmedNotifications,
                input.Lifetime);
            var encodedBytes = buffer.ToArray();
            AlarmAndEventServices.DecodeSubscribeCOV(encodedBytes, 0, encodedBytes.Length,
                out var subscriberProcessIdentifier, out var monitoredObjectIdentifier, out var cancellationRequest,
                out var issueConfirmedNotifications, out var lifetime);

            // assert
            Assert.That(subscriberProcessIdentifier, Is.EqualTo(input.SubscriberProcessIdentifier));
            Assert.That(monitoredObjectIdentifier, Is.EqualTo(input.MonitoredObjectIdentifier));
            Assert.That(cancellationRequest, Is.EqualTo(input.CancellationRequest));
            Assert.That(issueConfirmedNotifications, Is.EqualTo(input.IssueConfirmedNotifications));
            Assert.That(lifetime, Is.EqualTo(input.Lifetime));
        }

        [Test]
        public void should_encode_subscribecov_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.10
            var expectedBytes = new byte[]
            {
                0x20, 0x0F, 0x05
            };

            // act
            APDU.EncodeSimpleAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV, 15);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_encode_subscribecovproperty_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var data = ASHRAE.F_1_11();

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.11
            var expectedBytes = new byte[]
            {
                0x00, 0x02, 0x0F, 0x1C, 0x09, 0x12, 0x1C, 0x00, 0x00, 0x00, 0x0A, 0x29, 0x01, 0x39, 0x3C, 0x4E, 0x09,
                0x55, 0x4F, 0x5C, 0x3F, 0x80, 0x00, 0x00
            };
            // act
            APDU.EncodeConfirmedServiceRequest(buffer,
                BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY, BacnetMaxSegments.MAX_SEG0,
                BacnetMaxAdpu.MAX_APDU206, 15);

            AlarmAndEventServices.EncodeSubscribeProperty(buffer, data.SubscriberProcessIdentifier,
                data.MonitoredObjectIdentifier, data.CancellationRequest, data.IssueConfirmedNotifications,
                data.Lifetime, data.MonitoredProperty, data.CovIncrementPresent, data.CovIncrement);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }

        [Test]
        public void should_decode_subscribecovproperty_after_encode()
        {
            // arrange
            var buffer = new EncodeBuffer();
            var input = ASHRAE.F_1_11();

            // act
            AlarmAndEventServices.EncodeSubscribeProperty(buffer, input.SubscriberProcessIdentifier,
                input.MonitoredObjectIdentifier, input.CancellationRequest, input.IssueConfirmedNotifications,
                input.Lifetime, input.MonitoredProperty, input.CovIncrementPresent, input.CovIncrement);

            var encodedBytes = buffer.ToArray();

            AlarmAndEventServices.DecodeSubscribeProperty(encodedBytes, 0, encodedBytes.Length,
                out var subscriberProcessIdentifier, out var monitoredObjectIdentifier, out var monitoredProperty,
                out var cancellationRequest, out var issueConfirmedNotifications, out var lifetime,
                out var covIncrement);

            // assert
            Assert.That(subscriberProcessIdentifier, Is.EqualTo(input.SubscriberProcessIdentifier));
            Assert.That(monitoredObjectIdentifier, Is.EqualTo(input.MonitoredObjectIdentifier));
            Assert.That(monitoredProperty, Is.EqualTo(input.MonitoredProperty));
            Assert.That(cancellationRequest, Is.EqualTo(input.CancellationRequest));
            Assert.That(issueConfirmedNotifications, Is.EqualTo(input.IssueConfirmedNotifications));
            Assert.That(lifetime, Is.EqualTo(input.Lifetime));
            Assert.That(covIncrement, Is.EqualTo(input.CovIncrement));
        }

        [Test]
        public void should_encode_subscribecovproperty_ack_according_to_ashrae_example()
        {
            // arrange
            var buffer = new EncodeBuffer();

            // example taken from ANNEX F - Examples of APDU Encoding - F.1.11
            var expectedBytes = new byte[]
            {
                0x20, 0x0F, 0x1C
            };

            // act
            APDU.EncodeSimpleAck(buffer, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY, 15);

            var encodedBytes = buffer.ToArray();

            // assert
            Assert.That(encodedBytes, Is.EquivalentTo(expectedBytes));
        }    
    }
}