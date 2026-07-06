using System.IO.BACnet.Serialize;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// ReadRange-ACK item-data extraction: the range runs to the matching closing tag [5]. For
/// by-sequence/by-time acks a first-sequence-number [6] follows it, which the old "rest of the
/// buffer minus one byte" logic wrongly included in the returned range - decoding the trailing
/// bytes as a log record then failed.
/// </summary>
public class ReadRangeAcknowledgeTests
{
    private static byte[] TwoLogRecords()
    {
        var items = new EncodeBuffer();
        Services.EncodeLogRecord(items, new BacnetLogRecord(BacnetTrendLogValueType.TL_TYPE_REAL,
            101.0f, new DateTime(2026, 7, 5, 12, 1, 0), 0));
        Services.EncodeLogRecord(items, new BacnetLogRecord(BacnetTrendLogValueType.TL_TYPE_REAL,
            102.0f, new DateTime(2026, 7, 5, 12, 2, 0), 0));
        return items.ToArray();
    }

    private static void AssertRangeDecodesToTheTwoRecords(byte[] ackBytes)
    {
        var itemCount = Services.DecodeReadRangeAcknowledge(ackBytes, 0, ackBytes.Length, out var range);
        Assert.Equal(2u, itemCount);

        var offset = 0;
        var decoded = 0;
        while (offset < range.Length)
        {
            var len = Services.DecodeLogRecord(range, offset, range.Length, 1, out var records);
            Assert.True(len > 0, $"log record decode failed at offset {offset} of {range.Length}");
            Assert.Equal(new DateTime(2026, 7, 5, 12, 1 + decoded, 0), records[0].timestamp);
            offset += len;
            decoded++;
        }

        Assert.Equal(2, decoded); // the whole range is records - no trailing ack bytes
    }

    [Fact]
    public void By_time_ack_range_excludes_the_trailing_first_sequence_number()
    {
        var ack = new EncodeBuffer();
        Services.EncodeReadRangeAcknowledge(ack, new BacnetObjectId(BacnetObjectTypes.OBJECT_TRENDLOG, 2),
            (uint)BacnetPropertyIds.PROP_LOG_BUFFER, ASN1.BACNET_ARRAY_ALL,
            BacnetBitString.ConvertFromInt((uint)BacnetResultFlags.FIRST_ITEM | (uint)BacnetResultFlags.LAST_ITEM, 3),
            2, TwoLogRecords(), BacnetReadRangeRequestTypes.RR_BY_TIME, 6);

        AssertRangeDecodesToTheTwoRecords(ack.ToArray());
    }

    [Fact]
    public void By_position_ack_range_still_extracts_exactly()
    {
        var ack = new EncodeBuffer();
        Services.EncodeReadRangeAcknowledge(ack, new BacnetObjectId(BacnetObjectTypes.OBJECT_TRENDLOG, 2),
            (uint)BacnetPropertyIds.PROP_LOG_BUFFER, ASN1.BACNET_ARRAY_ALL,
            BacnetBitString.ConvertFromInt((uint)BacnetResultFlags.FIRST_ITEM | (uint)BacnetResultFlags.LAST_ITEM, 3),
            2, TwoLogRecords(), BacnetReadRangeRequestTypes.RR_BY_POSITION, 0);

        AssertRangeDecodesToTheTwoRecords(ack.ToArray());
    }
}
