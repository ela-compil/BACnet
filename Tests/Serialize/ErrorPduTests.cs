using System.IO.BACnet.Serialize;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// The Error PDU carries the plain 'Error' production - two application-tagged
/// enumerations with no enclosing context tags (ASHRAE 135 §20.1.7). Since 3.x
/// the encoder wrapped them in context tags (#199), which foreign stacks
/// mis-decode; the tolerant decoder hid it from same-stack round-trips, so
/// these tests assert the raw wire bytes.
/// </summary>
public class ErrorPduTests
{
    [Fact]
    public void Plain_error_encoding_matches_the_error_pdu_production()
    {
        var buffer = new EncodeBuffer();

        Services.EncodeError(buffer, BacnetErrorClasses.ERROR_CLASS_OBJECT,
            BacnetErrorCodes.ERROR_CODE_UNKNOWN_OBJECT);

        // enumerated 1 (object), enumerated 31 (unknown-object) - nothing else
        Assert.Equal(new byte[] { 0x91, 0x01, 0x91, 0x1F }, buffer.ToArray());
    }

    [Fact]
    public void Full_error_pdu_bytes_for_read_property_unknown_object()
    {
        var buffer = new EncodeBuffer();

        APDU.EncodeError(buffer, BacnetPduTypes.PDU_TYPE_ERROR,
            BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, 7);
        Services.EncodeError(buffer, BacnetErrorClasses.ERROR_CLASS_OBJECT,
            BacnetErrorCodes.ERROR_CODE_UNKNOWN_OBJECT);

        // [pdu-type][invoke-id][error-choice] class code (135 §20.1.7)
        Assert.Equal(new byte[] { 0x50, 0x07, 0x0C, 0x91, 0x01, 0x91, 0x1F }, buffer.ToArray());
    }

    [Fact]
    public void Explicit_tag_wraps_for_context_tagged_error_productions()
    {
        var buffer = new EncodeBuffer();

        // e.g. the trend-log 'failure' log-datum is a [8]-wrapped Error
        Services.EncodeError(buffer, BacnetErrorClasses.ERROR_CLASS_PROPERTY,
            BacnetErrorCodes.ERROR_CODE_UNKNOWN_PROPERTY, 8);

        Assert.Equal(new byte[] { 0x8E, 0x91, 0x02, 0x91, 0x20, 0x8F }, buffer.ToArray());
    }

    [Theory]
    [InlineData(new byte[] { 0x91, 0x01, 0x91, 0x1F })]             // plain (spec)
    [InlineData(new byte[] { 0x0E, 0x91, 0x01, 0x91, 0x1F, 0x0F })] // legacy 3.x wrapped
    public void DecodeError_accepts_plain_and_legacy_wrapped_forms(byte[] encoded)
    {
        var len = Services.DecodeError(encoded, 0, out var errorClass, out var errorCode);

        Assert.Equal(encoded.Length, len);
        Assert.Equal(BacnetErrorClasses.ERROR_CLASS_OBJECT, errorClass);
        Assert.Equal(BacnetErrorCodes.ERROR_CODE_UNKNOWN_OBJECT, errorCode);
    }
}
