using System.IO.BACnet.Serialize;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Encode/decode round-trips for the ASN.1 application primitives, plus a few
/// absolute-value checks against the worked examples in ANSI/ASHRAE 135-2016 Annex F.
/// </summary>
public class Asn1PrimitiveTests
{
    // --- Unsigned integers (across the 1/2/3/4-byte width boundaries) ---

    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData(255u)]
    [InlineData(256u)]
    [InlineData(65535u)]
    [InlineData(65536u)]
    [InlineData(16777215u)]
    [InlineData(16777216u)]
    [InlineData(uint.MaxValue)]
    public void Unsigned_roundtrips(uint value)
    {
        var buffer = new EncodeBuffer();
        ASN1.encode_bacnet_unsigned(buffer, value);
        var bytes = buffer.ToArray();

        var len = ASN1.decode_unsigned(bytes, 0, (uint)bytes.Length, out var decoded);

        Assert.Equal(bytes.Length, len);
        Assert.Equal(value, decoded);
    }

    // --- Signed integers (negatives and width boundaries) ---

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(127)]
    [InlineData(-128)]
    [InlineData(32767)]
    [InlineData(-32768)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void Signed_roundtrips(int value)
    {
        var buffer = new EncodeBuffer();
        ASN1.encode_bacnet_signed(buffer, value);
        var bytes = buffer.ToArray();

        var len = ASN1.decode_signed(bytes, 0, (uint)bytes.Length, out var decoded);

        Assert.Equal(bytes.Length, len);
        Assert.Equal(value, decoded);
    }

    // --- Real (IEEE-754 single precision, big-endian) ---

    [Theory]
    [InlineData(0f)]
    [InlineData(1f)]
    [InlineData(-1f)]
    [InlineData(65.0f)]
    [InlineData(3.1415927f)]
    [InlineData(float.MaxValue)]
    [InlineData(float.MinValue)]
    public void Real_roundtrips(float value)
    {
        var buffer = new EncodeBuffer();
        ASN1.encode_bacnet_real(buffer, value);
        var bytes = buffer.ToArray();

        var len = ASN1.decode_real(bytes, 0, out var decoded);

        Assert.Equal(4, bytes.Length);
        Assert.Equal(4, len);
        Assert.Equal(value, decoded);
    }

    [Fact]
    public void Real_matches_standard_encoding()
    {
        // ANSI/ASHRAE 135-2016 Annex F: the value 65.0 encodes to X'42820000'.
        var buffer = new EncodeBuffer();
        ASN1.encode_bacnet_real(buffer, 65.0f);

        Assert.Equal(new byte[] { 0x42, 0x82, 0x00, 0x00 }, buffer.ToArray());
    }

    // --- Double (IEEE-754 double precision, big-endian) ---

    [Theory]
    [InlineData(0d)]
    [InlineData(1d)]
    [InlineData(-1d)]
    [InlineData(65.0d)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    public void Double_roundtrips(double value)
    {
        var buffer = new EncodeBuffer();
        ASN1.encode_bacnet_double(buffer, value);
        var bytes = buffer.ToArray();

        var len = ASN1.decode_double(bytes, 0, out var decoded);

        Assert.Equal(8, bytes.Length);
        Assert.Equal(8, len);
        Assert.Equal(value, decoded);
    }

    // --- Object identifier (type + instance packed into 4 bytes) ---

    [Theory]
    [InlineData(BacnetObjectTypes.OBJECT_ANALOG_INPUT, 10u)]
    [InlineData(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 0u)]
    [InlineData(BacnetObjectTypes.OBJECT_DEVICE, 4194303u)] // BACNET_MAX_INSTANCE
    public void ObjectId_roundtrips(BacnetObjectTypes type, uint instance)
    {
        var buffer = new EncodeBuffer();
        ASN1.encode_bacnet_object_id(buffer, type, instance);
        var bytes = buffer.ToArray();

        var len = ASN1.decode_object_id(bytes, 0, out BacnetObjectTypes decodedType, out var decodedInstance);

        Assert.Equal(4, bytes.Length);
        Assert.Equal(4, len);
        Assert.Equal(type, decodedType);
        Assert.Equal(instance, decodedInstance);
    }

    [Fact]
    public void ObjectId_matches_standard_encoding()
    {
        // Annex F: Analog Input, instance 10 -> X'0000000A'.
        var buffer = new EncodeBuffer();
        ASN1.encode_bacnet_object_id(buffer, BacnetObjectTypes.OBJECT_ANALOG_INPUT, 10);

        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x0A }, buffer.ToArray());
    }
}
