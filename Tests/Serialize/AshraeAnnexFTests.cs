using System.Collections.Generic;
using System.IO.BACnet.Serialize;
using System.Linq;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Golden-vector encode tests from ASHRAE 135 Annex F, "Examples of APDU Encoding" — alarm/event
/// (F.1) and remote-device management (F.4) — plus encode/decode round-trips that exercise the
/// matching decoders. Vectors harvested from the community test effort in #25 (DarkStarDS9) and
/// adapted to the v4 API / xUnit. The F.1 alarm/event examples are expressed against the flat
/// BacnetEventNotificationData / COV API (not #25's refactored event model).
/// </summary>
public class AshraeAnnexFTests
{
    private static readonly BacnetAddress Adr = new BacnetAddress(BacnetAddressTypes.None, 0, null);

    // AnalogInput#10 Present_Value = 65.0, Status_Flags = {false,false,false,false}
    private static BacnetPropertyValue[] CovValues() => new[]
    {
        new BacnetPropertyValue
        {
            property = new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_PRESENT_VALUE, ASN1.BACNET_ARRAY_ALL),
            value = new List<BacnetValue> { new BacnetValue(65.0f) }
        },
        new BacnetPropertyValue
        {
            property = new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_STATUS_FLAGS, ASN1.BACNET_ARRAY_ALL),
            value = new List<BacnetValue> { new BacnetValue(BacnetBitString.Parse("0000")) }
        }
    };

    [Fact] // F.1.2 - ConfirmedCOVNotification request
    public void F_1_2_ConfirmedCOVNotification_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION,
            BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU206, 15);
        Services.EncodeCOVNotifyConfirmed(buffer, 18, 4,
            new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 10), 0, CovValues());

        Assert.Equal(new byte[]
        {
            0x00, 0x02, 0x0F, 0x01,
            0x09, 0x12, 0x1C, 0x02, 0x00, 0x00, 0x04, 0x2C, 0x00, 0x00, 0x00, 0x0A,
            0x39, 0x00, 0x4E, 0x09, 0x55, 0x2E, 0x44, 0x42, 0x82, 0x00, 0x00, 0x2F,
            0x09, 0x6F, 0x2E, 0x82, 0x04, 0x00, 0x2F, 0x4F
        }, buffer.ToArray());
    }

    [Fact] // F.1.2 - ConfirmedCOVNotification simple-ack
    public void F_1_2_ConfirmedCOVNotification_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeSimpleAck(buffer, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK,
            BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION, 15);

        Assert.Equal(new byte[] { 0x20, 0x0F, 0x01 }, buffer.ToArray());
    }

    [Fact] // F.1.3 - UnconfirmedCOVNotification request
    public void F_1_3_UnconfirmedCOVNotification_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST,
            BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_COV_NOTIFICATION);
        Services.EncodeCOVNotifyUnconfirmed(buffer, 18, 4,
            new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 10), 0, CovValues());

        Assert.Equal(new byte[]
        {
            0x10, 0x02,
            0x09, 0x12, 0x1C, 0x02, 0x00, 0x00, 0x04, 0x2C, 0x00, 0x00, 0x00, 0x0A,
            0x39, 0x00, 0x4E, 0x09, 0x55, 0x2E, 0x44, 0x42, 0x82, 0x00, 0x00, 0x2F,
            0x09, 0x6F, 0x2E, 0x82, 0x04, 0x00, 0x2F, 0x4F
        }, buffer.ToArray());
    }

    [Fact] // F.1.4 - ConfirmedEventNotification (OutOfRange) - adapted to the flat event struct
    public void F_1_4_ConfirmedEventNotification_request()
    {
        var data = new BacnetEventNotificationData
        {
            processIdentifier = 1,
            initiatingObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 4),
            eventObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 2),
            timeStamp = new BacnetGenericTime(default(DateTime), BacnetTimestampTags.TIME_STAMP_SEQUENCE, 16),
            notificationClass = 4,
            priority = 100,
            eventType = BacnetEventTypes.EVENT_OUT_OF_RANGE,
            notifyType = BacnetNotifyTypes.NOTIFY_ALARM,
            ackRequired = true,
            fromState = BacnetEventStates.EVENT_STATE_NORMAL,
            toState = BacnetEventStates.EVENT_STATE_HIGH_LIMIT,
            outOfRange_exceedingValue = 80.1f,
            outOfRange_statusFlags = BacnetBitString.Parse("1000"),
            outOfRange_deadband = 1.0f,
            outOfRange_exceededLimit = 80.0f,
        };

        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_EVENT_NOTIFICATION,
            BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU206, 16);
        Services.EncodeEventNotifyConfirmed(buffer, data);

        Assert.Equal(new byte[]
        {
            0x00, 0x02, 0x10, 0x02, 0x09, 0x01, 0x1C, 0x02, 0x00, 0x00, 0x04, 0x2C, 0x00, 0x00, 0x00, 0x02,
            0x3E, 0x19, 0x10, 0x3F, 0x49, 0x04, 0x59, 0x64, 0x69, 0x05, 0x89, 0x00, 0x99, 0x01, 0xA9, 0x00,
            0xB9, 0x03, 0xCE, 0x5E, 0x0C, 0x42, 0xA0, 0x33, 0x33, 0x1A, 0x04, 0x80, 0x2C, 0x3F, 0x80, 0x00,
            0x00, 0x3C, 0x42, 0xA0, 0x00, 0x00, 0x5F, 0xCF
        }, buffer.ToArray());
    }

    [Fact] // F.4.1 - DeviceCommunicationControl request
    public void F_4_1_DeviceCommunicationControl_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_DEVICE_COMMUNICATION_CONTROL,
            BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU1024, 5);
        Services.EncodeDeviceCommunicationControl(buffer, 5, 1 /* disable */, "#egbdf!");

        Assert.Equal(
            new byte[] { 0x00, 0x04, 0x05, 0x11, 0x09, 0x05, 0x19, 0x01, 0x2D, 0x08, 0x00, 0x23, 0x65, 0x67, 0x62, 0x64, 0x66, 0x21 },
            buffer.ToArray());
    }

    [Fact] // F.4.1 - DeviceCommunicationControl simple-ack
    public void F_4_1_DeviceCommunicationControl_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeSimpleAck(buffer, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK,
            BacnetConfirmedServices.SERVICE_CONFIRMED_DEVICE_COMMUNICATION_CONTROL, 5);

        Assert.Equal(new byte[] { 0x20, 0x05, 0x11 }, buffer.ToArray());
    }

    [Fact] // F.4.4 - ReinitializeDevice request
    public void F_4_4_ReinitializeDevice_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_REINITIALIZE_DEVICE,
            BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU128, 2);
        Services.EncodeReinitializeDevice(buffer, BacnetReinitializedStates.BACNET_REINIT_WARMSTART, "AbCdEfGh");

        Assert.Equal(
            new byte[] { 0x00, 0x01, 0x02, 0x14, 0x09, 0x01, 0x1D, 0x09, 0x00, 0x41, 0x62, 0x43, 0x64, 0x45, 0x66, 0x47, 0x68 },
            buffer.ToArray());
    }

    [Fact] // F.4.4 - ReinitializeDevice simple-ack
    public void F_4_4_ReinitializeDevice_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeSimpleAck(buffer, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK,
            BacnetConfirmedServices.SERVICE_CONFIRMED_REINITIALIZE_DEVICE, 2);

        Assert.Equal(new byte[] { 0x20, 0x02, 0x14 }, buffer.ToArray());
    }

    [Fact] // F.4.7 - TimeSynchronization request (1992-11-17 22:45:30.70)
    public void F_4_7_TimeSynchronization_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST,
            BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_TIME_SYNCHRONIZATION);
        Services.EncodeTimeSync(buffer, new DateTime(1992, 11, 17, 22, 45, 30).AddMilliseconds(700));

        Assert.Equal(new byte[] { 0x10, 0x06, 0xA4, 0x5C, 0x0B, 0x11, 0x02, 0xB4, 0x16, 0x2D, 0x1E, 0x46 }, buffer.ToArray());
    }

    [Fact] // F.4.8 - Who-Has by object name
    public void F_4_8_WhoHas_by_name()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST,
            BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_HAS);
        Services.EncodeWhoHasBroadcast(buffer, -1, -1, null, "OATemp");

        Assert.Equal(new byte[] { 0x10, 0x07, 0x3D, 0x07, 0x00, 0x4F, 0x41, 0x54, 0x65, 0x6D, 0x70 }, buffer.ToArray());
    }

    [Fact] // F.4.8 - Who-Has by object id
    public void F_4_8_WhoHas_by_id()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST,
            BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_HAS);
        Services.EncodeWhoHasBroadcast(buffer, -1, -1, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 3), null);

        Assert.Equal(new byte[] { 0x10, 0x07, 0x2C, 0x00, 0x00, 0x00, 0x03 }, buffer.ToArray());
    }

    [Fact] // F.4.8 - I-Have
    public void F_4_8_IHave()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST,
            BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_HAVE);
        Services.EncodeIhaveBroadcast(buffer, new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 8),
            new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 3), "OATemp");

        Assert.Equal(new byte[]
        {
            0x10, 0x01, 0xC4, 0x02, 0x00, 0x00, 0x08, 0xC4, 0x00, 0x00, 0x00, 0x03, 0x75, 0x07, 0x00, 0x4F, 0x41,
            0x54, 0x65, 0x6D, 0x70
        }, buffer.ToArray());
    }

    [Fact] // F.4.9 - Who-Is by id range 3..3
    public void F_4_9_WhoIs_by_id()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST,
            BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS);
        Services.EncodeWhoIsBroadcast(buffer, 3, 3);

        Assert.Equal(new byte[] { 0x10, 0x08, 0x09, 0x03, 0x19, 0x03 }, buffer.ToArray());
    }

    [Fact] // F.4.9 - Who-Is (global)
    public void F_4_9_WhoIs_all()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST,
            BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS);
        Services.EncodeWhoIsBroadcast(buffer, -1, -1);

        Assert.Equal(new byte[] { 0x10, 0x08 }, buffer.ToArray());
    }

    [Theory] // F.4.9 - I-Am (device / max-apdu / segmentation / vendor)
    [InlineData(1u, 480u, BacnetSegmentations.SEGMENTATION_TRANSMIT, (ushort)99, new byte[] { 0x10, 0x00, 0xC4, 0x02, 0x00, 0x00, 0x01, 0x22, 0x01, 0xE0, 0x91, 0x01, 0x21, 0x63 })]
    [InlineData(2u, 206u, BacnetSegmentations.SEGMENTATION_RECEIVE, (ushort)33, new byte[] { 0x10, 0x00, 0xC4, 0x02, 0x00, 0x00, 0x02, 0x21, 0xCE, 0x91, 0x02, 0x21, 0x21 })]
    [InlineData(3u, 1024u, BacnetSegmentations.SEGMENTATION_NONE, (ushort)99, new byte[] { 0x10, 0x00, 0xC4, 0x02, 0x00, 0x00, 0x03, 0x22, 0x04, 0x00, 0x91, 0x03, 0x21, 0x63 })]
    [InlineData(4u, 128u, BacnetSegmentations.SEGMENTATION_BOTH, (ushort)66, new byte[] { 0x10, 0x00, 0xC4, 0x02, 0x00, 0x00, 0x04, 0x21, 0x80, 0x91, 0x00, 0x21, 0x42 })]
    public void F_4_9_IAm(uint deviceId, uint maxApdu, BacnetSegmentations segmentation, ushort vendorId, byte[] expected)
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST,
            BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_AM);
        Services.EncodeIamBroadcast(buffer, deviceId, maxApdu, segmentation, vendorId);

        Assert.Equal(expected, buffer.ToArray());
    }

    [Fact] // F.1.6 - GetAlarmSummary complex-ack (two summaries)
    public void F_1_6_GetAlarmSummary_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeComplexAck(buffer, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK,
            BacnetConfirmedServices.SERVICE_CONFIRMED_GET_ALARM_SUMMARY, 1);
        Services.EncodeAlarmSummary(buffer, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 2),
            (uint)BacnetEventStates.EVENT_STATE_HIGH_LIMIT, BacnetBitString.Parse("011"));
        Services.EncodeAlarmSummary(buffer, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 3),
            (uint)BacnetEventStates.EVENT_STATE_LOW_LIMIT, BacnetBitString.Parse("111"));

        Assert.Equal(new byte[]
        {
            0x30, 0x01, 0x03, 0xC4, 0x00, 0x00, 0x00, 0x02, 0x91, 0x03, 0x82, 0x05, 0x60, 0xC4, 0x00, 0x00, 0x00,
            0x03, 0x91, 0x04, 0x82, 0x05, 0xE0
        }, buffer.ToArray());
    }

    [Fact] // F.1.8 - GetEventInformation complex-ack (unspecified timestamps -> wildcard)
    public void F_1_8_GetEventInformation_ack()
    {
        var events = new[]
        {
            new BacnetGetEventInformationData
            {
                objectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 2),
                eventState = BacnetEventStates.EVENT_STATE_HIGH_LIMIT,
                acknowledgedTransitions = BacnetBitString.Parse("011"),
                eventTimeStamps = new[]
                {
                    new BacnetGenericTime(new DateTime(1, 1, 1, 15, 35, 0).AddMilliseconds(200), BacnetTimestampTags.TIME_STAMP_TIME),
                    new BacnetGenericTime(ASN1.BACNET_TIME_WILDCARD, BacnetTimestampTags.TIME_STAMP_TIME),
                    new BacnetGenericTime(ASN1.BACNET_TIME_WILDCARD, BacnetTimestampTags.TIME_STAMP_TIME),
                },
                notifyType = BacnetNotifyTypes.NOTIFY_ALARM,
                eventEnable = BacnetBitString.Parse("111"),
                eventPriorities = new uint[] { 15, 15, 20 }
            },
            new BacnetGetEventInformationData
            {
                objectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 3),
                eventState = BacnetEventStates.EVENT_STATE_NORMAL,
                acknowledgedTransitions = BacnetBitString.Parse("110"),
                eventTimeStamps = new[]
                {
                    new BacnetGenericTime(new DateTime(1, 1, 1, 15, 40, 0), BacnetTimestampTags.TIME_STAMP_TIME),
                    new BacnetGenericTime(ASN1.BACNET_TIME_WILDCARD, BacnetTimestampTags.TIME_STAMP_TIME),
                    new BacnetGenericTime(new DateTime(1, 1, 1, 15, 45, 30).AddMilliseconds(300), BacnetTimestampTags.TIME_STAMP_TIME),
                },
                notifyType = BacnetNotifyTypes.NOTIFY_ALARM,
                eventEnable = BacnetBitString.Parse("111"),
                eventPriorities = new uint[] { 15, 15, 20 }
            }
        };

        var buffer = new EncodeBuffer();
        APDU.EncodeComplexAck(buffer, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK,
            BacnetConfirmedServices.SERVICE_CONFIRMED_GET_EVENT_INFORMATION, 1);
        Services.EncodeGetEventInformationAcknowledge(buffer, events, false);

        Assert.Equal(new byte[]
        {
            0x30, 0x01, 0x1D, 0x0E, 0x0C, 0x00, 0x00, 0x00, 0x02, 0x19, 0x03, 0x2A, 0x05, 0x60, 0x3E, 0x0C, 0x0F,
            0x23, 0x00, 0x14, 0x0C, 0xFF, 0xFF, 0xFF, 0xFF, 0x0C, 0xFF, 0xFF, 0xFF, 0xFF, 0x3F, 0x49, 0x00, 0x5A,
            0x05, 0xE0, 0x6E, 0x21, 0x0F, 0x21, 0x0F, 0x21, 0x14, 0x6F, 0x0C, 0x00, 0x00, 0x00, 0x03, 0x19, 0x00,
            0x2A, 0x05, 0xC0, 0x3E, 0x0C, 0x0F, 0x28, 0x00, 0x00, 0x0C, 0xFF, 0xFF, 0xFF, 0xFF, 0x0C, 0x0F, 0x2D,
            0x1E, 0x1E, 0x3F, 0x49, 0x00, 0x5A, 0x05, 0xE0, 0x6E, 0x21, 0x0F, 0x21, 0x0F, 0x21, 0x14, 0x6F, 0x0F,
            0x19, 0x00
        }, buffer.ToArray());
    }

    [Fact] // F.1.10 - SubscribeCOV request
    public void F_1_10_SubscribeCOV_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU206, 15);
        Services.EncodeSubscribeCOV(buffer, 18, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 10), false, true, 0);

        Assert.Equal(new byte[] { 0x00, 0x02, 0x0F, 0x05, 0x09, 0x12, 0x1C, 0x00, 0x00, 0x00, 0x0A, 0x29, 0x01, 0x39, 0x00 }, buffer.ToArray());
    }

    [Fact] // F.1.11 - SubscribeCOVProperty request
    public void F_1_11_SubscribeCOVProperty_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU206, 15);
        Services.EncodeSubscribeProperty(buffer, 18, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 10), false, true, 60,
            new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_PRESENT_VALUE, ASN1.BACNET_ARRAY_ALL), true, 1.0f);

        Assert.Equal(new byte[]
        {
            0x00, 0x02, 0x0F, 0x1C, 0x09, 0x12, 0x1C, 0x00, 0x00, 0x00, 0x0A, 0x29, 0x01, 0x39, 0x3C, 0x4E, 0x09,
            0x55, 0x4F, 0x5C, 0x3F, 0x80, 0x00, 0x00
        }, buffer.ToArray());
    }

    [Fact] // F.4.2 - ConfirmedPrivateTransfer request
    public void F_4_2_ConfirmedPrivateTransfer_request()
    {
        var data = new EncodeBuffer();
        ASN1.encode_application_real(data, 72.4f);
        ASN1.encode_application_octet_string(data, new byte[] { 0x16, 0x49 }, 0, 2);

        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_PRIVATE_TRANSFER, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU1024, 85);
        Services.EncodePrivateTransferConfirmed(buffer, 25, 8, data.ToArray());

        Assert.Equal(new byte[]
        {
            0x00, 0x04, 0x55, 0x12, 0x09, 0x19, 0x19, 0x08, 0x2E, 0x44, 0x42, 0x90, 0xCC, 0xCD, 0x62, 0x16, 0x49, 0x2F
        }, buffer.ToArray());
    }

    [Fact] // F.4.3 - ConfirmedPrivateTransfer complex-ack (no result block)
    public void F_4_3_ConfirmedPrivateTransfer_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeComplexAck(buffer, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK,
            BacnetConfirmedServices.SERVICE_CONFIRMED_PRIVATE_TRANSFER, 85);
        Services.EncodePrivateTransferAcknowledge(buffer, 25, 8, null);

        Assert.Equal(new byte[] { 0x30, 0x55, 0x12, 0x09, 0x19, 0x19, 0x08 }, buffer.ToArray());
    }

    [Fact] // F.4.3 - UnconfirmedPrivateTransfer request
    public void F_4_3_UnconfirmedPrivateTransfer_request()
    {
        var data = new EncodeBuffer();
        ASN1.encode_application_real(data, 72.4f);
        ASN1.encode_application_octet_string(data, new byte[] { 0x16, 0x49 }, 0, 2);

        var buffer = new EncodeBuffer();
        APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST,
            BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_PRIVATE_TRANSFER);
        Services.EncodePrivateTransferUnconfirmed(buffer, 25, 8, data.ToArray());

        Assert.Equal(new byte[]
        {
            0x10, 0x04, 0x09, 0x19, 0x19, 0x08, 0x2E, 0x44, 0x42, 0x90, 0xCC, 0xCD, 0x62, 0x16, 0x49, 0x2F
        }, buffer.ToArray());
    }

    [Fact] // F.1.4 - ConfirmedEventNotification simple-ack
    public void F_1_4_ConfirmedEventNotification_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeSimpleAck(buffer, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK, BacnetConfirmedServices.SERVICE_CONFIRMED_EVENT_NOTIFICATION, 16);
        Assert.Equal(new byte[] { 0x20, 0x10, 0x02 }, buffer.ToArray());
    }

    [Fact] // F.1.5 - AcknowledgeAlarm simple-ack
    public void F_1_5_AcknowledgeAlarm_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeSimpleAck(buffer, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK, BacnetConfirmedServices.SERVICE_CONFIRMED_ACKNOWLEDGE_ALARM, 7);
        Assert.Equal(new byte[] { 0x20, 0x07, 0x00 }, buffer.ToArray());
    }

    [Fact] // F.1.9 - LifeSafetyOperation simple-ack
    public void F_1_9_LifeSafetyOperation_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeSimpleAck(buffer, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK, BacnetConfirmedServices.SERVICE_CONFIRMED_LIFE_SAFETY_OPERATION, 15);
        Assert.Equal(new byte[] { 0x20, 0x0F, 0x1B }, buffer.ToArray());
    }

    [Fact] // F.1.10 - SubscribeCOV simple-ack
    public void F_1_10_SubscribeCOV_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeSimpleAck(buffer, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV, 15);
        Assert.Equal(new byte[] { 0x20, 0x0F, 0x05 }, buffer.ToArray());
    }

    [Fact] // F.1.11 - SubscribeCOVProperty simple-ack
    public void F_1_11_SubscribeCOVProperty_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeSimpleAck(buffer, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY, 15);
        Assert.Equal(new byte[] { 0x20, 0x0F, 0x1C }, buffer.ToArray());
    }

    [Fact] // F.1.5 - AcknowledgeAlarm request
    public void F_1_5_AcknowledgeAlarm_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_ACKNOWLEDGE_ALARM, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU206, 7);
        Services.EncodeAlarmAcknowledge(buffer, 1, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 2),
            (uint)BacnetEventStates.EVENT_STATE_HIGH_LIMIT, "MDL",
            new BacnetGenericTime(default(DateTime), BacnetTimestampTags.TIME_STAMP_SEQUENCE, 16),
            new BacnetGenericTime(new DateTime(1992, 6, 21, 13, 3, 41).AddMilliseconds(90), BacnetTimestampTags.TIME_STAMP_DATETIME));

        Assert.Equal(new byte[]
        {
            0x00, 0x02, 0x07, 0x00, 0x09, 0x01, 0x1C, 0x00, 0x00, 0x00, 0x02, 0x29, 0x03, 0x3E, 0x19, 0x10, 0x3F,
            0x4C, 0x00, 0x4D, 0x44, 0x4C, 0x5E, 0x2E, 0xA4, 0x5C, 0x06, 0x15, 0x07, 0xB4, 0x0D, 0x03, 0x29, 0x09, 0x2F, 0x5F
        }, buffer.ToArray());
    }

    [Fact] // F.1.5 - UnconfirmedEventNotification (OutOfRange, initiating Device 9)
    public void F_1_5_UnconfirmedEventNotification_request()
    {
        var data = new BacnetEventNotificationData
        {
            processIdentifier = 1,
            initiatingObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 9),
            eventObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 2),
            timeStamp = new BacnetGenericTime(default(DateTime), BacnetTimestampTags.TIME_STAMP_SEQUENCE, 16),
            notificationClass = 4,
            priority = 100,
            eventType = BacnetEventTypes.EVENT_OUT_OF_RANGE,
            notifyType = BacnetNotifyTypes.NOTIFY_ALARM,
            ackRequired = true,
            fromState = BacnetEventStates.EVENT_STATE_NORMAL,
            toState = BacnetEventStates.EVENT_STATE_HIGH_LIMIT,
            outOfRange_exceedingValue = 80.1f,
            outOfRange_statusFlags = BacnetBitString.Parse("1000"),
            outOfRange_deadband = 1.0f,
            outOfRange_exceededLimit = 80.0f,
        };

        var buffer = new EncodeBuffer();
        APDU.EncodeUnconfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST,
            BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_EVENT_NOTIFICATION);
        Services.EncodeEventNotifyUnconfirmed(buffer, data);

        Assert.Equal(new byte[]
        {
            0x10, 0x03, 0x09, 0x01, 0x1C, 0x02, 0x00, 0x00, 0x09, 0x2C, 0x00, 0x00, 0x00, 0x02, 0x3E, 0x19, 0x10,
            0x3F, 0x49, 0x04, 0x59, 0x64, 0x69, 0x05, 0x89, 0x00, 0x99, 0x01, 0xA9, 0x00, 0xB9, 0x03, 0xCE, 0x5E,
            0x0C, 0x42, 0xA0, 0x33, 0x33, 0x1A, 0x04, 0x80, 0x2C, 0x3F, 0x80, 0x00, 0x00, 0x3C, 0x42, 0xA0, 0x00,
            0x00, 0x5F, 0xCF
        }, buffer.ToArray());
    }

    [Fact] // F.1.6 - GetAlarmSummary request (no parameters)
    public void F_1_6_GetAlarmSummary_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_GET_ALARM_SUMMARY, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU206, 1);

        Assert.Equal(new byte[] { 0x00, 0x02, 0x01, 0x03 }, buffer.ToArray());
    }

    [Fact] // F.1.8 - GetEventInformation request (get all)
    public void F_1_8_GetEventInformation_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_GET_EVENT_INFORMATION, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU206, 1);
        Services.EncodeGetEventInformation(buffer, false, default(BacnetObjectId));

        Assert.Equal(new byte[] { 0x00, 0x02, 0x01, 0x1D }, buffer.ToArray());
    }

    [Fact] // F.1.9 - LifeSafetyOperation request
    public void F_1_9_LifeSafetyOperation_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_LIFE_SAFETY_OPERATION, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU206, 15);
        Services.EncodeLifeSafetyOperation(buffer, 18, "MDL",
            (uint)BacnetLifeSafetyOperations.LIFE_SAFETY_OP_RESET, new BacnetObjectId(BacnetObjectTypes.OBJECT_LIFE_SAFETY_POINT, 1));

        Assert.Equal(new byte[]
        {
            0x00, 0x02, 0x0F, 0x1B, 0x09, 0x12, 0x1C, 0x00, 0x4D, 0x44, 0x4C, 0x29, 0x04, 0x3C, 0x05, 0x40, 0x00, 0x01
        }, buffer.ToArray());
    }

    [Fact] // F.1.3 - UnconfirmedCOVNotification decodes back to the monitored object and property values
    public void F_1_3_UnconfirmedCOVNotification_round_trips()
    {
        var buffer = new EncodeBuffer();
        Services.EncodeCOVNotifyUnconfirmed(buffer, 18, 4,
            new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 10), 0, CovValues());

        Services.DecodeCOVNotifyUnconfirmed(Adr, buffer.buffer, 0, buffer.GetLength(),
            out var subscriberProcessIdentifier, out var initiatingDevice, out var monitoredObject, out var timeRemaining, out var values);

        Assert.Equal(18u, subscriberProcessIdentifier);
        Assert.Equal(4u, initiatingDevice.instance);
        Assert.Equal(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 10), monitoredObject);
        Assert.Equal(0u, timeRemaining);
        Assert.Equal(2, values.Count);
    }

    [Fact] // F.1.4 - ConfirmedEventNotification (OutOfRange) decodes back to the notification data
    public void F_1_4_ConfirmedEventNotification_round_trips()
    {
        var data = new BacnetEventNotificationData
        {
            processIdentifier = 1,
            initiatingObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 4),
            eventObjectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 2),
            timeStamp = new BacnetGenericTime(default(DateTime), BacnetTimestampTags.TIME_STAMP_SEQUENCE, 16),
            notificationClass = 4,
            priority = 100,
            eventType = BacnetEventTypes.EVENT_OUT_OF_RANGE,
            notifyType = BacnetNotifyTypes.NOTIFY_ALARM,
            ackRequired = true,
            fromState = BacnetEventStates.EVENT_STATE_NORMAL,
            toState = BacnetEventStates.EVENT_STATE_HIGH_LIMIT,
            outOfRange_exceedingValue = 80.1f,
            outOfRange_statusFlags = BacnetBitString.Parse("1000"),
            outOfRange_deadband = 1.0f,
            outOfRange_exceededLimit = 80.0f,
        };

        var buffer = new EncodeBuffer();
        Services.EncodeEventNotifyConfirmed(buffer, data);

        Services.DecodeEventNotifyData(buffer.buffer, 0, buffer.GetLength(), out var decoded);

        Assert.Equal(data.processIdentifier, decoded.processIdentifier);
        Assert.Equal(data.initiatingObjectIdentifier, decoded.initiatingObjectIdentifier);
        Assert.Equal(data.eventObjectIdentifier, decoded.eventObjectIdentifier);
        Assert.Equal(data.eventType, decoded.eventType);
        Assert.Equal(data.notifyType, decoded.notifyType);
        Assert.Equal(data.fromState, decoded.fromState);
        Assert.Equal(data.toState, decoded.toState);
        Assert.Equal(data.outOfRange_exceedingValue, decoded.outOfRange_exceedingValue);
        Assert.Equal(data.outOfRange_exceededLimit, decoded.outOfRange_exceededLimit);
    }

    [Fact] // F.1.6 - GetAlarmSummary ack decodes back to the two alarm summaries
    public void F_1_6_GetAlarmSummary_ack_round_trips()
    {
        var buffer = new EncodeBuffer();
        Services.EncodeAlarmSummary(buffer, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 2),
            (uint)BacnetEventStates.EVENT_STATE_HIGH_LIMIT, BacnetBitString.Parse("011"));
        Services.EncodeAlarmSummary(buffer, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 3),
            (uint)BacnetEventStates.EVENT_STATE_LOW_LIMIT, BacnetBitString.Parse("111"));

        IList<BacnetGetEventInformationData> alarms = new List<BacnetGetEventInformationData>();
        Services.DecodeAlarmSummaryOrEvent(buffer.buffer, 0, buffer.GetLength(), false, ref alarms, out var moreEvent);

        Assert.False(moreEvent);
        Assert.Equal(2, alarms.Count);
        Assert.Equal(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 2), alarms[0].objectIdentifier);
        Assert.Equal(BacnetEventStates.EVENT_STATE_HIGH_LIMIT, alarms[0].eventState);
        Assert.Equal(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 3), alarms[1].objectIdentifier);
        Assert.Equal(BacnetEventStates.EVENT_STATE_LOW_LIMIT, alarms[1].eventState);
    }

    [Fact] // F.1.8 - GetEventInformation ack decodes back to the two events
    public void F_1_8_GetEventInformation_ack_round_trips()
    {
        var events = new[]
        {
            new BacnetGetEventInformationData
            {
                objectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 2),
                eventState = BacnetEventStates.EVENT_STATE_HIGH_LIMIT,
                acknowledgedTransitions = BacnetBitString.Parse("011"),
                eventTimeStamps = new[]
                {
                    new BacnetGenericTime(new DateTime(1, 1, 1, 15, 35, 0).AddMilliseconds(200), BacnetTimestampTags.TIME_STAMP_TIME),
                    new BacnetGenericTime(ASN1.BACNET_TIME_WILDCARD, BacnetTimestampTags.TIME_STAMP_TIME),
                    new BacnetGenericTime(ASN1.BACNET_TIME_WILDCARD, BacnetTimestampTags.TIME_STAMP_TIME),
                },
                notifyType = BacnetNotifyTypes.NOTIFY_ALARM,
                eventEnable = BacnetBitString.Parse("111"),
                eventPriorities = new uint[] { 15, 15, 20 }
            },
            new BacnetGetEventInformationData
            {
                objectIdentifier = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 3),
                eventState = BacnetEventStates.EVENT_STATE_NORMAL,
                acknowledgedTransitions = BacnetBitString.Parse("110"),
                eventTimeStamps = new[]
                {
                    new BacnetGenericTime(new DateTime(1, 1, 1, 15, 40, 0), BacnetTimestampTags.TIME_STAMP_TIME),
                    new BacnetGenericTime(ASN1.BACNET_TIME_WILDCARD, BacnetTimestampTags.TIME_STAMP_TIME),
                    new BacnetGenericTime(new DateTime(1, 1, 1, 15, 45, 30).AddMilliseconds(300), BacnetTimestampTags.TIME_STAMP_TIME),
                },
                notifyType = BacnetNotifyTypes.NOTIFY_ALARM,
                eventEnable = BacnetBitString.Parse("111"),
                eventPriorities = new uint[] { 15, 15, 20 }
            }
        };

        var buffer = new EncodeBuffer();
        Services.EncodeGetEventInformationAcknowledge(buffer, events, false);

        IList<BacnetGetEventInformationData> decoded = new List<BacnetGetEventInformationData>();
        Services.DecodeAlarmSummaryOrEvent(buffer.buffer, 0, buffer.GetLength(), true, ref decoded, out var moreEvent);

        Assert.False(moreEvent);
        Assert.Equal(2, decoded.Count);
        Assert.Equal(events[0].objectIdentifier, decoded[0].objectIdentifier);
        Assert.Equal(events[0].eventState, decoded[0].eventState);
        Assert.Equal(events[0].notifyType, decoded[0].notifyType);
        Assert.Equal(events[1].objectIdentifier, decoded[1].objectIdentifier);
        Assert.Equal(events[1].eventState, decoded[1].eventState);
    }

    [Fact] // F.1.10 - SubscribeCOV request decodes back to the subscription parameters
    public void F_1_10_SubscribeCOV_round_trips()
    {
        var buffer = new EncodeBuffer();
        Services.EncodeSubscribeCOV(buffer, 18, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 10), false, true, 0);

        Services.DecodeSubscribeCOV(buffer.buffer, 0, buffer.GetLength(),
            out var subscriberProcessIdentifier, out var monitoredObject, out var cancellationRequest,
            out var issueConfirmedNotifications, out var lifetime);

        Assert.Equal(18u, subscriberProcessIdentifier);
        Assert.Equal(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 10), monitoredObject);
        Assert.False(cancellationRequest);
        Assert.True(issueConfirmedNotifications);
        Assert.Equal(0u, lifetime);
    }

    [Fact] // F.1.11 - SubscribeCOVProperty request decodes back to the property subscription parameters
    public void F_1_11_SubscribeCOVProperty_round_trips()
    {
        var buffer = new EncodeBuffer();
        Services.EncodeSubscribeProperty(buffer, 18, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 10), false, true, 60,
            new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_PRESENT_VALUE, ASN1.BACNET_ARRAY_ALL), true, 1.0f);

        Services.DecodeSubscribeProperty(buffer.buffer, 0, buffer.GetLength(),
            out var subscriberProcessIdentifier, out var monitoredObject, out var monitoredProperty,
            out var cancellationRequest, out var issueConfirmedNotifications, out var lifetime, out var covIncrement);

        Assert.Equal(18u, subscriberProcessIdentifier);
        Assert.Equal(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 10), monitoredObject);
        Assert.Equal((uint)BacnetPropertyIds.PROP_PRESENT_VALUE, monitoredProperty.propertyIdentifier);
        Assert.False(cancellationRequest);
        Assert.True(issueConfirmedNotifications);
        Assert.Equal(60u, lifetime);
        Assert.Equal(1.0f, covIncrement);
    }

    [Fact] // F.4.1 - DeviceCommunicationControl request decodes back to its parameters
    public void F_4_1_DeviceCommunicationControl_round_trips()
    {
        var buffer = new EncodeBuffer();
        Services.EncodeDeviceCommunicationControl(buffer, 5, 1 /* disable */, "#egbdf!");

        Services.DecodeDeviceCommunicationControl(buffer.buffer, 0, buffer.GetLength(),
            out var timeDuration, out var enableDisable, out var password);

        Assert.Equal(5u, timeDuration);
        Assert.Equal(1u, enableDisable);
        Assert.Equal("#egbdf!", password);
    }

    [Fact] // F.4.4 - ReinitializeDevice request decodes back to the state and password
    public void F_4_4_ReinitializeDevice_round_trips()
    {
        var buffer = new EncodeBuffer();
        Services.EncodeReinitializeDevice(buffer, BacnetReinitializedStates.BACNET_REINIT_WARMSTART, "AbCdEfGh");

        Services.DecodeReinitializeDevice(buffer.buffer, 0, buffer.GetLength(), out var state, out var password);

        Assert.Equal(BacnetReinitializedStates.BACNET_REINIT_WARMSTART, state);
        Assert.Equal("AbCdEfGh", password);
    }

    [Fact] // F.4.7 - TimeSynchronization request decodes back to the same instant (hundredths resolution)
    public void F_4_7_TimeSynchronization_round_trips()
    {
        var when = new DateTime(1992, 11, 17, 22, 45, 30).AddMilliseconds(700);

        var buffer = new EncodeBuffer();
        Services.EncodeTimeSync(buffer, when);

        Services.DecodeTimeSync(buffer.buffer, 0, buffer.GetLength(), out var decoded);

        Assert.Equal(when, decoded);
    }

    [Fact] // F.4.8 - Who-Has by object id decodes back to the object id
    public void F_4_8_WhoHas_by_id_round_trips()
    {
        var buffer = new EncodeBuffer();
        Services.EncodeWhoHasBroadcast(buffer, -1, -1, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 3), null);

        Services.DecodeWhoHasBroadcast(buffer.buffer, 0, buffer.GetLength(),
            out var lowLimit, out var highLimit, out var objId, out var objName);

        Assert.Equal(-1, lowLimit);
        Assert.Equal(-1, highLimit);
        Assert.Equal(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 3), objId);
        Assert.True(string.IsNullOrEmpty(objName));
    }

    [Fact] // F.4.8 - Who-Has by object name decodes back to the object name
    public void F_4_8_WhoHas_by_name_round_trips()
    {
        var buffer = new EncodeBuffer();
        Services.EncodeWhoHasBroadcast(buffer, -1, -1, null, "OATemp");

        Services.DecodeWhoHasBroadcast(buffer.buffer, 0, buffer.GetLength(),
            out var lowLimit, out var highLimit, out var objId, out var objName);

        Assert.Equal(-1, lowLimit);
        Assert.Equal(-1, highLimit);
        Assert.Null(objId);
        Assert.Equal("OATemp", objName);
    }

    [Fact] // F.4.9 - Who-Is with an id range decodes back to the range
    public void F_4_9_WhoIs_by_id_round_trips()
    {
        var buffer = new EncodeBuffer();
        Services.EncodeWhoIsBroadcast(buffer, 3, 3);

        Services.DecodeWhoIsBroadcast(buffer.buffer, 0, buffer.GetLength(), out var lowLimit, out var highLimit);

        Assert.Equal(3, lowLimit);
        Assert.Equal(3, highLimit);
    }

    [Fact] // F.4.9 - Who-Is with no parameters decodes back to the unbounded range
    public void F_4_9_WhoIs_all_round_trips()
    {
        var buffer = new EncodeBuffer();
        Services.EncodeWhoIsBroadcast(buffer, -1, -1);

        Services.DecodeWhoIsBroadcast(buffer.buffer, 0, buffer.GetLength(), out var lowLimit, out var highLimit);

        Assert.Equal(-1, lowLimit);
        Assert.Equal(-1, highLimit);
    }
}
