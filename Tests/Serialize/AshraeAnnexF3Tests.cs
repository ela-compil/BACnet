using System.Collections.Generic;
using System.IO.BACnet.Serialize;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Golden-vector encode tests from ASHRAE 135 Annex F, "Examples of APDU Encoding" — object access
/// (F.3) and file access (F.2). Vectors harvested from #25 (DarkStarDS9), adapted to the v4 API.
/// </summary>
public class AshraeAnnexF3Tests
{
    private static readonly BacnetObjectId Ai5 = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 5);
    private static readonly BacnetObjectId Ai16 = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 16);
    private const uint PresentValue = (uint)BacnetPropertyIds.PROP_PRESENT_VALUE;
    private const uint Reliability = (uint)BacnetPropertyIds.PROP_RELIABILITY;

    [Fact] // F.3.5 - ReadProperty request
    public void F_3_5_ReadProperty_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU50, 1);
        Services.EncodeReadProperty(buffer, Ai5, PresentValue, ASN1.BACNET_ARRAY_ALL);

        Assert.Equal(new byte[] { 0x00, 0x00, 0x01, 0x0C, 0x0C, 0x00, 0x00, 0x00, 0x05, 0x19, 0x55 }, buffer.ToArray());
    }

    [Fact] // F.3.5 - ReadProperty complex-ack (Present_Value = 72.3)
    public void F_3_5_ReadProperty_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeComplexAck(buffer, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK,
            BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, 1);
        Services.EncodeReadPropertyAcknowledge(buffer, Ai5, PresentValue, ASN1.BACNET_ARRAY_ALL,
            new List<BacnetValue> { new BacnetValue(72.3f) });

        Assert.Equal(new byte[]
        {
            0x30, 0x01, 0x0C, 0x0C, 0x00, 0x00, 0x00, 0x05, 0x19, 0x55, 0x3E, 0x44, 0x42, 0x90, 0x99, 0x9A, 0x3F
        }, buffer.ToArray());
    }

    [Fact] // F.3.7 - ReadPropertyMultiple request
    public void F_3_7_ReadPropertyMultiple_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU1024, 241);
        Services.EncodeReadPropertyMultiple(buffer, Ai16, new List<BacnetPropertyReference>
        {
            new BacnetPropertyReference(PresentValue, ASN1.BACNET_ARRAY_ALL),
            new BacnetPropertyReference(Reliability, ASN1.BACNET_ARRAY_ALL)
        });

        Assert.Equal(new byte[]
        {
            0x00, 0x04, 0xF1, 0x0E, 0x0C, 0x00, 0x00, 0x00, 0x10, 0x1E, 0x09, 0x55, 0x09, 0x67, 0x1F
        }, buffer.ToArray());
    }

    [Fact] // F.3.7 - ReadPropertyMultiple complex-ack
    public void F_3_7_ReadPropertyMultiple_ack()
    {
        var result = new List<BacnetReadAccessResult>
        {
            new BacnetReadAccessResult(Ai16, new List<BacnetPropertyValue>
            {
                new BacnetPropertyValue
                {
                    property = new BacnetPropertyReference(PresentValue, ASN1.BACNET_ARRAY_ALL),
                    value = new List<BacnetValue> { new BacnetValue(72.3f) }
                },
                new BacnetPropertyValue
                {
                    property = new BacnetPropertyReference(Reliability, ASN1.BACNET_ARRAY_ALL),
                    value = new List<BacnetValue>
                    {
                        new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED,
                            (uint)BacnetReliability.RELIABILITY_NO_FAULT_DETECTED)
                    }
                }
            })
        };

        var buffer = new EncodeBuffer();
        APDU.EncodeComplexAck(buffer, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK,
            BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, 241);
        Services.EncodeReadPropertyMultipleAcknowledge(buffer, result);

        Assert.Equal(new byte[]
        {
            0x30, 0xF1, 0x0E, 0x0C, 0x00, 0x00, 0x00, 0x10, 0x1E, 0x29, 0x55, 0x4E, 0x44, 0x42, 0x90, 0x99, 0x9A,
            0x4F, 0x29, 0x67, 0x4E, 0x91, 0x00, 0x4F, 0x1F
        }, buffer.ToArray());
    }

    [Fact] // F.3.9 - WriteProperty request (AnalogValue#1 Present_Value = 180.0)
    public void F_3_9_WriteProperty_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU1024, 89);
        Services.EncodeWriteProperty(buffer, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 1),
            PresentValue, ASN1.BACNET_ARRAY_ALL, 0, new List<BacnetValue> { new BacnetValue(180f) });

        Assert.Equal(new byte[]
        {
            0x00, 0x04, 0x59, 0x0F, 0x0C, 0x00, 0x80, 0x00, 0x01, 0x19, 0x55, 0x3E, 0x44, 0x43, 0x34, 0x00, 0x00, 0x3F
        }, buffer.ToArray());
    }

    [Fact] // F.2.1 - AtomicReadFile request (stream, File#1, position 0, count 27)
    public void F_2_1_AtomicReadFile_stream_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU206, 0);
        Services.EncodeAtomicReadFile(buffer, true, new BacnetObjectId(BacnetObjectTypes.OBJECT_FILE, 1), 0, 27);

        Assert.Equal(new byte[]
        {
            0x00, 0x02, 0x00, 0x06, 0xC4, 0x02, 0x80, 0x00, 0x01, 0x0E, 0x31, 0x00, 0x21, 0x1B, 0x0F
        }, buffer.ToArray());
    }

    private static List<BacnetPropertyValue> PresentValueWrite(float value) => new List<BacnetPropertyValue>
    {
        new BacnetPropertyValue
        {
            property = new BacnetPropertyReference(PresentValue, ASN1.BACNET_ARRAY_ALL),
            value = new List<BacnetValue> { new BacnetValue(value) },
            priority = (byte)ASN1.BACNET_NO_PRIORITY
        }
    };

    [Fact] // F.3.10 - WritePropertyMultiple request (three AnalogValue objects)
    public void F_3_10_WritePropertyMultiple_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU1024, 1);
        Services.EncodeWritePropertyMultiple(buffer, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 5), PresentValueWrite(67f));
        Services.EncodeWritePropertyMultiple(buffer, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 6), PresentValueWrite(67f));
        Services.EncodeWritePropertyMultiple(buffer, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 7), PresentValueWrite(72f));

        Assert.Equal(new byte[]
        {
            0x00, 0x04, 0x01, 0x10, 0x0C, 0x00, 0x80, 0x00, 0x05, 0x1E, 0x09, 0x55, 0x2E, 0x44, 0x42, 0x86, 0x00,
            0x00, 0x2F, 0x1F, 0x0C, 0x00, 0x80, 0x00, 0x06, 0x1E, 0x09, 0x55, 0x2E, 0x44, 0x42, 0x86, 0x00, 0x00,
            0x2F, 0x1F, 0x0C, 0x00, 0x80, 0x00, 0x07, 0x1E, 0x09, 0x55, 0x2E, 0x44, 0x42, 0x90, 0x00, 0x00, 0x2F,
            0x1F
        }, buffer.ToArray());
    }

    [Fact] // F.3.10 - WritePropertyMultiple simple-ack
    public void F_3_10_WritePropertyMultiple_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeSimpleAck(buffer, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK,
            BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE, 1);

        Assert.Equal(new byte[] { 0x20, 0x01, 0x10 }, buffer.ToArray());
    }

    [Fact] // F.3.3 - CreateObject complex-ack (AnalogValue#13)
    public void F_3_3_CreateObject_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeComplexAck(buffer, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK,
            BacnetConfirmedServices.SERVICE_CONFIRMED_CREATE_OBJECT, 86);
        Services.EncodeCreateObjectAcknowledge(buffer, new BacnetObjectId(BacnetObjectTypes.OBJECT_FILE, 13));

        Assert.Equal(new byte[] { 0x30, 0x56, 0x0A, 0xC4, 0x02, 0x80, 0x00, 0x0D }, buffer.ToArray());
    }

    [Fact] // F.3.4 - DeleteObject simple-ack
    public void F_3_4_DeleteObject_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeSimpleAck(buffer, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK,
            BacnetConfirmedServices.SERVICE_CONFIRMED_DELETE_OBJECT, 87);

        Assert.Equal(new byte[] { 0x20, 0x57, 0x0B }, buffer.ToArray());
    }

    [Fact] // F.3.1 - AddListElement simple-ack
    public void F_3_1_AddListElement_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeSimpleAck(buffer, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK,
            BacnetConfirmedServices.SERVICE_CONFIRMED_ADD_LIST_ELEMENT, 1);

        Assert.Equal(new byte[] { 0x20, 0x01, 0x08 }, buffer.ToArray());
    }

    [Fact] // F.3.2 - RemoveListElement simple-ack
    public void F_3_2_RemoveListElement_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeSimpleAck(buffer, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK,
            BacnetConfirmedServices.SERVICE_CONFIRMED_REMOVE_LIST_ELEMENT, 52);

        Assert.Equal(new byte[] { 0x20, 0x34, 0x09 }, buffer.ToArray());
    }

    [Fact] // F.3.9 - WriteProperty simple-ack
    public void F_3_9_WriteProperty_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeSimpleAck(buffer, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK,
            BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, 89);

        Assert.Equal(new byte[] { 0x20, 0x59, 0x0F }, buffer.ToArray());
    }

    [Fact] // F.3.8 - ReadRange request (TrendLog#1 Log_Buffer, by time, 4 records)
    public void F_3_8_ReadRange_request()
    {
        var buffer = new EncodeBuffer();
        // ReadRange asks for a segmented response, so the PDU sets SEGMENTED_RESPONSE_ACCEPTED.
        APDU.EncodeConfirmedServiceRequest(buffer,
            BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST | BacnetPduTypes.SEGMENTED_RESPONSE_ACCEPTED,
            BacnetConfirmedServices.SERVICE_CONFIRMED_READ_RANGE, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU206, 1);
        Services.EncodeReadRange(buffer, new BacnetObjectId(BacnetObjectTypes.OBJECT_TRENDLOG, 1),
            (uint)BacnetPropertyIds.PROP_LOG_BUFFER, ASN1.BACNET_ARRAY_ALL,
            BacnetReadRangeRequestTypes.RR_BY_TIME, 0, new DateTime(1998, 3, 23, 19, 52, 34), 4);

        Assert.Equal(new byte[]
        {
            0x02, 0x02, 0x01, 0x1A, 0x0C, 0x05, 0x00, 0x00, 0x01, 0x19, 0x83, 0x7E, 0xA4, 0x62, 0x03, 0x17, 0x01,
            0xB4, 0x13, 0x34, 0x22, 0x00, 0x31, 0x04, 0x7F
        }, buffer.ToArray());
    }

    private static BacnetReadAccessSpecification PvSpec(uint instance) =>
        new BacnetReadAccessSpecification(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, instance),
            new List<BacnetPropertyReference> { new BacnetPropertyReference(PresentValue, ASN1.BACNET_ARRAY_ALL) });

    private static BacnetPropertyValue PvResult(BacnetValue value) => new BacnetPropertyValue
    {
        property = new BacnetPropertyReference(PresentValue, ASN1.BACNET_ARRAY_ALL),
        value = new List<BacnetValue> { value }
    };

    [Fact] // F.3.7 - ReadPropertyMultiple request (multiple objects)
    public void F_3_7_ReadPropertyMultiple_multipleObjects_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU1024, 2);
        Services.EncodeReadPropertyMultiple(buffer, new List<BacnetReadAccessSpecification> { PvSpec(33), PvSpec(50), PvSpec(35) });

        Assert.Equal(new byte[]
        {
            0x00, 0x04, 0x02, 0x0E, 0x0C, 0x00, 0x00, 0x00, 0x21, 0x1E, 0x09, 0x55, 0x1F, 0x0C, 0x00, 0x00, 0x00,
            0x32, 0x1E, 0x09, 0x55, 0x1F, 0x0C, 0x00, 0x00, 0x00, 0x23, 0x1E, 0x09, 0x55, 0x1F
        }, buffer.ToArray());
    }

    [Fact] // F.3.7 - ReadPropertyMultiple ack (multiple objects, one unknown-object error)
    public void F_3_7_ReadPropertyMultiple_multipleObjects_ack()
    {
        var results = new List<BacnetReadAccessResult>
        {
            new BacnetReadAccessResult(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 33),
                new List<BacnetPropertyValue> { PvResult(new BacnetValue(42.3f)) }),
            new BacnetReadAccessResult(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 50),
                new List<BacnetPropertyValue> { PvResult(new BacnetValue(
                    new BacnetError(BacnetErrorClasses.ERROR_CLASS_OBJECT, BacnetErrorCodes.ERROR_CODE_UNKNOWN_OBJECT))) }),
            new BacnetReadAccessResult(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 35),
                new List<BacnetPropertyValue> { PvResult(new BacnetValue(435.7f)) }),
        };

        var buffer = new EncodeBuffer();
        APDU.EncodeComplexAck(buffer, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK,
            BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, 2);
        Services.EncodeReadPropertyMultipleAcknowledge(buffer, results);

        Assert.Equal(new byte[]
        {
            0x30, 0x02, 0x0E, 0x0C, 0x00, 0x00, 0x00, 0x21, 0x1E, 0x29, 0x55, 0x4E, 0x44, 0x42, 0x29, 0x33, 0x33,
            0x4F, 0x1F, 0x0C, 0x00, 0x00, 0x00, 0x32, 0x1E, 0x29, 0x55, 0x5E, 0x91, 0x01, 0x91, 0x1F, 0x5F, 0x1F,
            0x0C, 0x00, 0x00, 0x00, 0x23, 0x1E, 0x29, 0x55, 0x4E, 0x44, 0x43, 0xD9, 0xD9, 0x9A, 0x4F, 0x1F
        }, buffer.ToArray());
    }

    [Fact] // F.3.8 - ReadRange ack (TrendLog Log_Buffer, two records by sequence)
    public void F_3_8_ReadRange_ack()
    {
        var record1 = new BacnetLogRecord(BacnetTrendLogValueType.TL_TYPE_REAL, 18.0f,
            new DateTime(1998, 3, 23, 19, 54, 27), (BacnetStatusFlags)0);
        var record2 = new BacnetLogRecord(BacnetTrendLogValueType.TL_TYPE_REAL, 18.1f,
            new DateTime(1998, 3, 23, 19, 56, 27), (BacnetStatusFlags)0);

        var appData = new EncodeBuffer();
        Services.EncodeLogRecord(appData, record1);
        Services.EncodeLogRecord(appData, record2);

        var buffer = new EncodeBuffer();
        APDU.EncodeComplexAck(buffer, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK,
            BacnetConfirmedServices.SERVICE_CONFIRMED_READ_RANGE, 1);
        Services.EncodeReadRangeAcknowledge(buffer, new BacnetObjectId(BacnetObjectTypes.OBJECT_TRENDLOG, 1),
            (uint)BacnetPropertyIds.PROP_LOG_BUFFER, ASN1.BACNET_ARRAY_ALL, BacnetBitString.Parse("110"), 2,
            appData.ToArray(), BacnetReadRangeRequestTypes.RR_BY_SEQUENCE, 79201);

        Assert.Equal(new byte[]
        {
            0x30, 0x01, 0x1A, 0x0C, 0x05, 0x00, 0x00, 0x01, 0x19, 0x83, 0x3A, 0x05, 0xC0, 0x49, 0x02, 0x5E, 0x0E,
            0xA4, 0x62, 0x03, 0x17, 0x01, 0xB4, 0x13, 0x36, 0x1B, 0x00, 0x0F, 0x1E, 0x2C, 0x41, 0x90, 0x00, 0x00,
            0x1F, 0x2A, 0x04, 0x00, 0x0E, 0xA4, 0x62, 0x03, 0x17, 0x01, 0xB4, 0x13, 0x38, 0x1B, 0x00, 0x0F, 0x1E,
            0x2C, 0x41, 0x90, 0xCC, 0xCD, 0x1F, 0x2A, 0x04, 0x00, 0x5F, 0x6B, 0x01, 0x35, 0x61
        }, buffer.ToArray());
    }
}
