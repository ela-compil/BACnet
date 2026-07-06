using System.Collections.Generic;
using System.IO.BACnet.Serialize;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// BACnetDeviceObjectPropertyReference ::= SEQUENCE { object-identifier [0],
/// property-identifier [1], property-array-index [2] OPTIONAL, device-identifier [3] OPTIONAL }
/// (ASHRAE 135-2016 Clause 21) - the element type of a Schedule object's
/// List_Of_Object_Property_References.
/// </summary>
public class BacnetDeviceObjectPropertyReferenceTests
{
    [Fact]
    public void Local_reference_round_trips_without_optional_fields()
    {
        var reference = new BacnetDeviceObjectPropertyReference(
            new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 1),
            BacnetPropertyIds.PROP_PRESENT_VALUE);

        var buffer = new EncodeBuffer();
        reference.Encode(buffer);
        var bytes = buffer.ToArray();

        // objectId [0] + propertyId [1] only: optional index and device id are omitted
        Assert.Equal(new byte[] { 0x0C, 0x00, 0x80, 0x00, 0x01, 0x19, 0x55 }, bytes);

        var decoded = new BacnetDeviceObjectPropertyReference();
        var len = decoded.Decode(bytes, 0, (uint)bytes.Length);

        Assert.Equal(bytes.Length, len);
        Assert.Equal(reference.ObjectId, decoded.ObjectId);
        Assert.Equal(reference.PropertyId, decoded.PropertyId);
        Assert.Equal(ASN1.BACNET_ARRAY_ALL, decoded.arrayIndex);
        Assert.Null(decoded.DeviceId);
    }

    [Fact]
    public void Remote_reference_with_array_index_round_trips()
    {
        var reference = new BacnetDeviceObjectPropertyReference(
            new BacnetObjectId(BacnetObjectTypes.OBJECT_SCHEDULE, 7),
            BacnetPropertyIds.PROP_WEEKLY_SCHEDULE,
            new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 4194302),
            arrayIndex: 3);

        var buffer = new EncodeBuffer();
        reference.Encode(buffer);
        var bytes = buffer.ToArray();

        var decoded = new BacnetDeviceObjectPropertyReference();
        var len = decoded.Decode(bytes, 0, (uint)bytes.Length);

        Assert.Equal(bytes.Length, len);
        Assert.Equal(reference.ObjectId, decoded.ObjectId);
        Assert.Equal(reference.PropertyId, decoded.PropertyId);
        Assert.Equal(3u, decoded.arrayIndex);
        Assert.Equal(new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 4194302), decoded.DeviceId);
    }

    [Fact]
    public void Reference_list_read_writes_back_byte_identical()
    {
        var references = new List<BacnetValue>
        {
            new BacnetValue(new BacnetDeviceObjectPropertyReference(
                new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 1), BacnetPropertyIds.PROP_PRESENT_VALUE)),
            new BacnetValue(new BacnetDeviceObjectPropertyReference(
                new BacnetObjectId(BacnetObjectTypes.OBJECT_BINARY_OUTPUT, 2), BacnetPropertyIds.PROP_PRESENT_VALUE,
                new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 99)))
        };

        var ack = new EncodeBuffer();
        Services.EncodeReadPropertyAcknowledge(ack,
            new BacnetObjectId(BacnetObjectTypes.OBJECT_SCHEDULE, 1),
            (uint)BacnetPropertyIds.PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES, ASN1.BACNET_ARRAY_ALL, references);
        var ackBytes = ack.ToArray();

        var len = Services.DecodeReadPropertyAcknowledge(new BacnetAddress(BacnetAddressTypes.None, 0, null),
            ackBytes, 0, ackBytes.Length, out _, out _, out var values);

        Assert.Equal(ackBytes.Length, len);
        Assert.Equal(2, values.Count);
        Assert.All(values, v => Assert.IsType<BacnetDeviceObjectPropertyReference>(v.Value));

        var rewritten = new EncodeBuffer();
        Services.EncodeReadPropertyAcknowledge(rewritten,
            new BacnetObjectId(BacnetObjectTypes.OBJECT_SCHEDULE, 1),
            (uint)BacnetPropertyIds.PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES, ASN1.BACNET_ARRAY_ALL, values);
        Assert.Equal(ackBytes, rewritten.ToArray());
    }

    [Fact]
    public void Malformed_reference_decodes_as_error()
    {
        Assert.Equal(-1, new BacnetDeviceObjectPropertyReference().Decode(new byte[] { 0x99, 0x00 }, 0, 2));
    }
}
