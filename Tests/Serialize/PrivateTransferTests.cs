using System.IO.BACnet.Serialize;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Decode and round-trip tests for the PrivateTransfer services (#154), using the golden vectors
/// from ASHRAE 135 Annex F.4.2 / F.4.3 and the max-conveyable-APDU test message of clause 19.4.
/// The encode direction of the Annex F vectors is covered in <see cref="AshraeAnnexFTests"/>.
/// </summary>
public class PrivateTransferTests
{
    // Annex F.4.2 service parameters: Real 72.4 followed by octet string X'1649'
    private static readonly byte[] AnnexFServiceParameters = new byte[]
    {
        0x44, 0x42, 0x90, 0xCC, 0xCD, 0x62, 0x16, 0x49
    };

    [Fact] // F.4.2 - ConfirmedPrivateTransfer request
    public void F_4_2_ConfirmedPrivateTransfer_request_decode()
    {
        var apdu = new byte[]
        {
            0x00, 0x04, 0x55, 0x12, 0x09, 0x19, 0x19, 0x08, 0x2E, 0x44, 0x42, 0x90, 0xCC, 0xCD, 0x62, 0x16, 0x49, 0x2F
        };
        const int headerLen = 4; // PDU type, max APDU, invoke id, service choice

        var len = Services.DecodePrivateTransfer(apdu, headerLen, apdu.Length - headerLen,
            out var vendorId, out var serviceNumber, out var data);

        Assert.Equal(apdu.Length - headerLen, len);
        Assert.Equal(25u, vendorId);
        Assert.Equal(8u, serviceNumber);
        Assert.Equal(AnnexFServiceParameters, data);
    }

    [Fact] // F.4.2 - ConfirmedPrivateTransfer complex-ack (no result block)
    public void F_4_2_ConfirmedPrivateTransfer_ack_decode()
    {
        var apdu = new byte[] { 0x30, 0x55, 0x12, 0x09, 0x19, 0x19, 0x08 };
        const int headerLen = 3; // PDU type, invoke id, service choice

        var len = Services.DecodePrivateTransfer(apdu, headerLen, apdu.Length - headerLen,
            out var vendorId, out var serviceNumber, out var data);

        Assert.Equal(apdu.Length - headerLen, len);
        Assert.Equal(25u, vendorId);
        Assert.Equal(8u, serviceNumber);
        Assert.Null(data);
    }

    [Fact] // F.4.3 - UnconfirmedPrivateTransfer request
    public void F_4_3_UnconfirmedPrivateTransfer_request_decode()
    {
        var apdu = new byte[]
        {
            0x10, 0x04, 0x09, 0x19, 0x19, 0x08, 0x2E, 0x44, 0x42, 0x90, 0xCC, 0xCD, 0x62, 0x16, 0x49, 0x2F
        };
        const int headerLen = 2; // PDU type, service choice

        var len = Services.DecodePrivateTransfer(apdu, headerLen, apdu.Length - headerLen,
            out var vendorId, out var serviceNumber, out var data);

        Assert.Equal(apdu.Length - headerLen, len);
        Assert.Equal(25u, vendorId);
        Assert.Equal(8u, serviceNumber);
        Assert.Equal(AnnexFServiceParameters, data);
    }

    [Fact] // 19.4.2 - the ASHRAE max conveyable APDU test message (vendor 0, service 0, 1462-octet payload)
    public void Clause_19_4_2_max_conveyable_apdu_test_message()
    {
        var parameters = new EncodeBuffer();
        ASN1.encode_application_octet_string(parameters, new byte[1462], 0, 1462);

        var buffer = new EncodeBuffer();
        Services.EncodePrivateTransferConfirmed(buffer, 0, 0, parameters.ToArray());
        var encoded = buffer.ToArray();

        // 19.4.2: SD context tags 0 and 1 with value 0, then the octet string with extended length 1462
        Assert.Equal(new byte[] { 0x09, 0x00, 0x19, 0x00, 0x2E, 0x65, 0xFE, 0x05, 0xB6 }, encoded[..9]);
        Assert.Equal(0x2F, encoded[^1]);

        var len = Services.DecodePrivateTransfer(encoded, 0, encoded.Length,
            out var vendorId, out var serviceNumber, out var data);

        Assert.Equal(encoded.Length, len);
        Assert.Equal(0u, vendorId);
        Assert.Equal(0u, serviceNumber);
        Assert.Equal(parameters.ToArray(), data);
    }

    [Fact] // service-parameters is OPTIONAL - absent must encode without the [2] tags and decode to null
    public void Request_without_parameters_roundtrip()
    {
        var buffer = new EncodeBuffer();
        Services.EncodePrivateTransferConfirmed(buffer, 25, 8, null);
        var encoded = buffer.ToArray();

        Assert.Equal(new byte[] { 0x09, 0x19, 0x19, 0x08 }, encoded);

        var len = Services.DecodePrivateTransfer(encoded, 0, encoded.Length,
            out var vendorId, out var serviceNumber, out var data);

        Assert.Equal(encoded.Length, len);
        Assert.Equal(25u, vendorId);
        Assert.Equal(8u, serviceNumber);
        Assert.Null(data);
    }

    [Fact] // vendor-id is Unsigned16 and service-number Unsigned - exercise multi-octet values
    public void Large_vendor_and_service_number_roundtrip()
    {
        var buffer = new EncodeBuffer();
        Services.EncodePrivateTransferConfirmed(buffer, 65535, 305419896, new byte[] { 0x01 });
        var encoded = buffer.ToArray();

        var len = Services.DecodePrivateTransfer(encoded, 0, encoded.Length,
            out var vendorId, out var serviceNumber, out var data);

        Assert.Equal(encoded.Length, len);
        Assert.Equal(65535u, vendorId);
        Assert.Equal(305419896u, serviceNumber);
        Assert.Equal(new byte[] { 0x01 }, data);
    }

    [Fact]
    public void Truncated_or_malformed_request_is_rejected()
    {
        // missing closing tag 2
        Assert.Equal(-1, Services.DecodePrivateTransfer(new byte[] { 0x09, 0x19, 0x19, 0x08, 0x2E, 0x44 }, 0, 6, out _, out _, out _));
        // wrong leading tag
        Assert.Equal(-1, Services.DecodePrivateTransfer(new byte[] { 0x19, 0x19, 0x09, 0x08 }, 0, 4, out _, out _, out _));
    }

    [Fact] // ConfirmedPrivateTransfer-Error: error-type [0], vendor-id [1], service-number [2], error-parameters [3]
    public void PrivateTransferError_roundtrip()
    {
        var buffer = new EncodeBuffer();
        Services.EncodePrivateTransferError(buffer, BacnetErrorClasses.ERROR_CLASS_SERVICES,
            BacnetErrorCodes.ERROR_CODE_SERVICE_REQUEST_DENIED, 25, 8, new byte[] { 0x21, 0x07 });
        var encoded = buffer.ToArray();

        var len = Services.DecodePrivateTransferError(encoded, 0, encoded.Length,
            out var errorClass, out var errorCode, out var vendorId, out var serviceNumber, out var data);

        Assert.Equal(encoded.Length, len);
        Assert.Equal(BacnetErrorClasses.ERROR_CLASS_SERVICES, errorClass);
        Assert.Equal(BacnetErrorCodes.ERROR_CODE_SERVICE_REQUEST_DENIED, errorCode);
        Assert.Equal(25u, vendorId);
        Assert.Equal(8u, serviceNumber);
        Assert.Equal(new byte[] { 0x21, 0x07 }, data);

        // the generic Error decoder used by ProcessError must still extract the error-type from it
        Assert.True(Services.DecodeError(encoded, 0, out var genericClass, out var genericCode) > 0);
        Assert.Equal(BacnetErrorClasses.ERROR_CLASS_SERVICES, genericClass);
        Assert.Equal(BacnetErrorCodes.ERROR_CODE_SERVICE_REQUEST_DENIED, genericCode);
    }

    [Fact]
    public void PrivateTransferError_without_parameters_roundtrip()
    {
        var buffer = new EncodeBuffer();
        Services.EncodePrivateTransferError(buffer, BacnetErrorClasses.ERROR_CLASS_DEVICE,
            BacnetErrorCodes.ERROR_CODE_OTHER, 25, 8, null);
        var encoded = buffer.ToArray();

        var len = Services.DecodePrivateTransferError(encoded, 0, encoded.Length,
            out var errorClass, out var errorCode, out var vendorId, out var serviceNumber, out var data);

        Assert.Equal(encoded.Length, len);
        Assert.Equal(BacnetErrorClasses.ERROR_CLASS_DEVICE, errorClass);
        Assert.Equal(BacnetErrorCodes.ERROR_CODE_OTHER, errorCode);
        Assert.Equal(25u, vendorId);
        Assert.Equal(8u, serviceNumber);
        Assert.Null(data);
    }
}
