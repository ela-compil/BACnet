using System.IO.BACnet.Serialize;
using System.Linq;
using System.Text;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Golden-vector encode tests from ASHRAE 135 Annex F.2, AtomicReadFile / AtomicWriteFile
/// (stream and record access). Harvested from #25 (DarkStarDS9), adapted to the v4 API.
/// </summary>
public class AshraeAnnexF2Tests
{
    private static readonly byte[][] Stream = { Encoding.ASCII.GetBytes("Chiller01 On-Time=4.3 Hours") };
    private static readonly byte[][] Records = { Encoding.ASCII.GetBytes("12:00,45.6"), Encoding.ASCII.GetBytes("12:15,44.8") };
    private static int[] Counts(byte[][] b) => b.Select(x => x.Length).ToArray();

    private static readonly BacnetObjectId File1 = new BacnetObjectId(BacnetObjectTypes.OBJECT_FILE, 1);
    private static readonly BacnetObjectId File2 = new BacnetObjectId(BacnetObjectTypes.OBJECT_FILE, 2);

    [Fact] // F.2.1 - AtomicReadFile stream complex-ack
    public void F_2_1_AtomicReadFile_stream_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeComplexAck(buffer, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, 0);
        Services.EncodeAtomicReadFileAcknowledge(buffer, true, false, 0, 1, Stream, Counts(Stream));

        Assert.Equal(new byte[]
        {
            0x30, 0x00, 0x06, 0x10, 0x0E, 0x31, 0x00, 0x65, 0x1B, 0x43, 0x68, 0x69, 0x6C, 0x6C, 0x65, 0x72, 0x30,
            0x31, 0x20, 0x4F, 0x6E, 0x2D, 0x54, 0x69, 0x6D, 0x65, 0x3D, 0x34, 0x2E, 0x33, 0x20, 0x48, 0x6F, 0x75,
            0x72, 0x73, 0x0F
        }, buffer.ToArray());
    }

    [Fact] // F.2.1 - AtomicReadFile record request
    public void F_2_1_AtomicReadFile_record_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU206, 18);
        Services.EncodeAtomicReadFile(buffer, false, File2, 14, 3);

        Assert.Equal(new byte[] { 0x00, 0x02, 0x12, 0x06, 0xC4, 0x02, 0x80, 0x00, 0x02, 0x1E, 0x31, 0x0E, 0x21, 0x03, 0x1F }, buffer.ToArray());
    }

    [Fact] // F.2.1 - AtomicReadFile record complex-ack
    public void F_2_1_AtomicReadFile_record_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeComplexAck(buffer, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, 18);
        Services.EncodeAtomicReadFileAcknowledge(buffer, false, true, 14, 2, Records, Counts(Records));

        Assert.Equal(new byte[]
        {
            0x30, 0x12, 0x06, 0x11, 0x1E, 0x31, 0x0E, 0x21, 0x02, 0x65, 0x0A, 0x31, 0x32, 0x3A, 0x30, 0x30, 0x2C,
            0x34, 0x35, 0x2E, 0x36, 0x65, 0x0A, 0x31, 0x32, 0x3A, 0x31, 0x35, 0x2C, 0x34, 0x34, 0x2E, 0x38, 0x1F
        }, buffer.ToArray());
    }

    [Fact] // F.2.2 - AtomicWriteFile stream request
    public void F_2_2_AtomicWriteFile_stream_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU206, 85);
        Services.EncodeAtomicWriteFile(buffer, true, File1, 30, 1, Stream, Counts(Stream));

        Assert.Equal(new byte[]
        {
            0x00, 0x02, 0x55, 0x07, 0xC4, 0x02, 0x80, 0x00, 0x01, 0x0E, 0x31, 0x1E, 0x65, 0x1B, 0x43, 0x68, 0x69,
            0x6C, 0x6C, 0x65, 0x72, 0x30, 0x31, 0x20, 0x4F, 0x6E, 0x2D, 0x54, 0x69, 0x6D, 0x65, 0x3D, 0x34, 0x2E,
            0x33, 0x20, 0x48, 0x6F, 0x75, 0x72, 0x73, 0x0F
        }, buffer.ToArray());
    }

    [Fact] // F.2.2 - AtomicWriteFile stream complex-ack
    public void F_2_2_AtomicWriteFile_stream_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeComplexAck(buffer, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, 85);
        Services.EncodeAtomicWriteFileAcknowledge(buffer, true, 30);

        Assert.Equal(new byte[] { 0x30, 0x55, 0x07, 0x09, 0x1E }, buffer.ToArray());
    }

    [Fact] // F.2.2 - AtomicWriteFile record request
    public void F_2_2_AtomicWriteFile_record_request()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeConfirmedServiceRequest(buffer, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST,
            BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, BacnetMaxSegments.MAX_SEG0, BacnetMaxAdpu.MAX_APDU206, 85);
        Services.EncodeAtomicWriteFile(buffer, false, File2, -1, 2, Records, Counts(Records));

        Assert.Equal(new byte[]
        {
            0x00, 0x02, 0x55, 0x07, 0xC4, 0x02, 0x80, 0x00, 0x02, 0x1E, 0x31, 0xFF, 0x21, 0x02, 0x65, 0x0A, 0x31,
            0x32, 0x3A, 0x30, 0x30, 0x2C, 0x34, 0x35, 0x2E, 0x36, 0x65, 0x0A, 0x31, 0x32, 0x3A, 0x31, 0x35, 0x2C,
            0x34, 0x34, 0x2E, 0x38, 0x1F
        }, buffer.ToArray());
    }

    [Fact] // F.2.2 - AtomicWriteFile record complex-ack
    public void F_2_2_AtomicWriteFile_record_ack()
    {
        var buffer = new EncodeBuffer();
        APDU.EncodeComplexAck(buffer, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, 85);
        Services.EncodeAtomicWriteFileAcknowledge(buffer, false, 14);

        Assert.Equal(new byte[] { 0x30, 0x55, 0x07, 0x19, 0x0E }, buffer.ToArray());
    }
}
