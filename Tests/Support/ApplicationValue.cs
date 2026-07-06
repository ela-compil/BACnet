using System.IO.BACnet.Serialize;
using Xunit;

namespace System.IO.BACnet.Tests.Support;

/// <summary>Round-trip helpers for application-encoded <see cref="BacnetValue"/>s.</summary>
internal static class ApplicationValue
{
    public static readonly BacnetAddress DummyAddress = new BacnetAddress(BacnetAddressTypes.None, 0, null);

    public static BacnetValue Decode(byte[] wire,
        BacnetPropertyIds propertyId = BacnetPropertyIds.MAX_BACNET_PROPERTY_ID)
    {
        var len = ASN1.bacapp_decode_application_data(DummyAddress, wire, 0, wire.Length,
            BacnetObjectTypes.MAX_BACNET_OBJECT_TYPE, propertyId, out var value);
        Assert.Equal(wire.Length, len);
        return value;
    }

    public static void AssertReencodesTo(byte[] wire, BacnetValue value)
    {
        var buffer = new EncodeBuffer();
        ASN1.bacapp_encode_application_data(buffer, value);
        Assert.Equal(wire, buffer.ToArray());
    }
}
