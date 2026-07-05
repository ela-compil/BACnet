using System.Collections.Generic;
using System.IO.BACnet.Serialize;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Regression for #101: a <see cref="BacnetPropertyReference"/> built for a scalar (non-array) property
/// must omit the optional array index so ReadPropertyMultiple/WritePropertyMultiple succeed. The index
/// is only encoded when it differs from <see cref="ASN1.BACNET_ARRAY_ALL"/>, so the default constructor
/// value has to be that sentinel — not 0, which selects the array length and errors on scalar properties.
/// </summary>
public class BacnetPropertyReferenceTests
{
    private const uint PresentValue = (uint)BacnetPropertyIds.PROP_PRESENT_VALUE;

    [Fact]
    public void OmittingArrayIndex_DefaultsToArrayAll()
    {
        var reference = new BacnetPropertyReference(PresentValue);

        Assert.Equal(ASN1.BACNET_ARRAY_ALL, reference.propertyArrayIndex);
    }

    [Fact]
    public void EnumOverload_SetsIdentifierAndDefaultsArrayIndex()
    {
        var reference = new BacnetPropertyReference(BacnetPropertyIds.PROP_PRESENT_VALUE);

        Assert.Equal(PresentValue, reference.propertyIdentifier);
        Assert.Equal(ASN1.BACNET_ARRAY_ALL, reference.propertyArrayIndex);
    }

    [Fact]
    public void OmittingArrayIndex_DoesNotEncodeArrayIndexTag()
    {
        var encoded = EncodeSingleReference(new BacnetPropertyReference(PresentValue));

        // Tag 0 (property identifier) present, no context tag 1 (array index).
        Assert.Equal(new byte[] { 0x0C, 0x00, 0x00, 0x00, 0x05, 0x1E, 0x09, 0x55, 0x1F }, encoded);
    }

    [Fact]
    public void ExplicitArrayIndexZero_EncodesArrayIndexTag()
    {
        var encoded = EncodeSingleReference(new BacnetPropertyReference(PresentValue, 0));

        // Same as above but with context tag 1 = 0 (0x19 0x00) selecting the array length.
        Assert.Equal(new byte[] { 0x0C, 0x00, 0x00, 0x00, 0x05, 0x1E, 0x09, 0x55, 0x19, 0x00, 0x1F }, encoded);
    }

    private static byte[] EncodeSingleReference(BacnetPropertyReference reference)
    {
        var buffer = new EncodeBuffer();
        ASN1.encode_read_access_specification(buffer, new BacnetReadAccessSpecification(
            new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 5),
            new List<BacnetPropertyReference> { reference }));
        return buffer.ToArray();
    }
}
