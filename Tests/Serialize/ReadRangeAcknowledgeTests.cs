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
    // hand-derived per ASHRAE 135 Clause 20.2: log record = timestamp [0]{Date,Time},
    // logDatum [1]{real-value [2]}, statusFlags [2] (4-bit bitstring)
    private static readonly byte[] GoldenRecord1 =
    {
        0x0E, 0xA4, 126, 7, 5, 7, 0xB4, 12, 1, 0, 0, 0x0F,   // 2026-07-05 (Sunday) 12:01:00.00
        0x1E, 0x2C, 0x42, 0xCA, 0x00, 0x00, 0x1F,            // REAL 101.0
        0x2A, 0x04, 0x00                                     // status {f,f,f,f}
    };

    private static readonly byte[] GoldenRecord2 =
    {
        0x0E, 0xA4, 126, 7, 5, 7, 0xB4, 12, 2, 0, 0, 0x0F,   // 12:02:00.00
        0x1E, 0x2C, 0x42, 0xCC, 0x00, 0x00, 0x1F,            // REAL 102.0
        0x2A, 0x04, 0x00
    };

    [Fact]
    public void Golden_by_time_ack_extracts_exactly_the_item_data()
    {
        var ack = new System.Collections.Generic.List<byte>
        {
            0x0C, 0x05, 0x00, 0x00, 0x02,   // objectIdentifier [0]: trend-log,2
            0x19, 0x83,                     // propertyIdentifier [1]: log-buffer (131)
            0x3A, 0x05, 0xA0,               // resultFlags [3]: {FIRST_ITEM, LAST_ITEM}
            0x49, 0x02,                     // itemCount [4]: 2
            0x5E                            // itemData [5] opening
        };
        ack.AddRange(GoldenRecord1);
        ack.AddRange(GoldenRecord2);
        ack.Add(0x5F);                      // itemData [5] closing
        ack.AddRange(new byte[] { 0x69, 0x06 }); // firstSequenceNumber [6]: 6 - the by-time trailer

        var bytes = ack.ToArray();
        var itemCount = Services.DecodeReadRangeAcknowledge(bytes, 0, bytes.Length, out var range);

        Assert.Equal(2u, itemCount);
        var expectedRange = new byte[GoldenRecord1.Length + GoldenRecord2.Length];
        GoldenRecord1.CopyTo(expectedRange, 0);
        GoldenRecord2.CopyTo(expectedRange, GoldenRecord1.Length);
        Assert.Equal(expectedRange, range); // records only - no closing tag, no [6] trailer

        var len = Services.DecodeLogRecord(range, 0, range.Length, 1, out var records);
        Assert.Equal(GoldenRecord1.Length, len);
        Assert.Equal(new DateTime(2026, 7, 5, 12, 1, 0), records[0].timestamp);
        Assert.Equal(101.0f, records[0].Value);
    }

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
