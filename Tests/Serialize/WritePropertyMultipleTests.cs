using System.Collections.Generic;
using System.Linq;
using System.IO.BACnet.Serialize;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// WritePropertyMultiple is a SEQUENCE OF WriteAccessSpecification (ASHRAE 135), i.e. a single
/// request may target several objects. Regression test for #158: the decoder must return every
/// object, not just the first.
/// </summary>
public class WritePropertyMultipleTests
{
    private static BacnetPropertyValue PresentValue(float value) => new BacnetPropertyValue
    {
        property = new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_PRESENT_VALUE, ASN1.BACNET_ARRAY_ALL),
        value = new List<BacnetValue> { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL, value) },
        priority = (byte)ASN1.BACNET_NO_PRIORITY,
    };

    [Fact]
    public void Decodes_all_objects_in_a_multi_object_request()
    {
        // Encode two write-access-specifications back to back (as a multi-object WPM body).
        var buffer = new EncodeBuffer();
        Services.EncodeWritePropertyMultiple(buffer, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 1017),
            new List<BacnetPropertyValue> { PresentValue(50f) });
        Services.EncodeWritePropertyMultiple(buffer, new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 1016),
            new List<BacnetPropertyValue> { PresentValue(40f) });
        var bytes = buffer.ToArray();

        var address = new BacnetAddress(BacnetAddressTypes.IP, "192.168.1.1");
        var len = Services.DecodeWritePropertyMultiple(address, bytes, 0, bytes.Length, out var specs);

        Assert.True(len >= 0);
        Assert.Equal(2, specs.Count);

        var list = specs.ToList();
        Assert.Equal(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 1017), list[0].objectIdentifier);
        Assert.Equal(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 1016), list[1].objectIdentifier);
        Assert.Equal(50f, (float)list[0].propertyValues.First().value.First().Value);
        Assert.Equal(40f, (float)list[1].propertyValues.First().value.First().Value);
        Assert.Equal((uint)BacnetPropertyIds.PROP_PRESENT_VALUE, list[0].propertyValues.First().property.propertyIdentifier);
    }
}
