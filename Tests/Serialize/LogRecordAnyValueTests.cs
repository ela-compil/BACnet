using System.IO.BACnet.Serialize;
using System.Linq;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// The any-value [10] choice of a log datum is constructed - opening tag [10], application-tagged
/// value(s), closing tag [10] - and devices log values such as a Double there. Decoding it threw a
/// NotImplementedException, so nothing after the first such record of a trend log could be read
/// (#240).
/// </summary>
public class LogRecordAnyValueTests
{
    // timestamp [0]{Date,Time}, logDatum [1]{any-value [10]{Double 13.0}}, statusFlags [2]
    private static readonly byte[] WireRecord =
    {
        0x0E, 0xA4, 126, 8, 7, 5, 0xB4, 8, 13, 46, 88, 0x0F,   // 2026-08-07 (Friday) 08:13:46.88
        0x1E, 0xAE,                                            // [1] { [10] {
        0x55, 0x08, 0x40, 0x2A, 0, 0, 0, 0, 0, 0,               //   Double 13.0
        0xAF, 0x1F,                                            // }[10] }[1]
        0x2A, 0x04, 0x00                                       // status {f,f,f,f}
    };

    private static readonly DateTime Stamp = new(2026, 8, 7, 8, 13, 46, 880);

    private static BacnetLogRecord RoundTrip(object value)
    {
        var buffer = new EncodeBuffer();
        Services.EncodeLogRecord(buffer, new BacnetLogRecord(BacnetTrendLogValueType.TL_TYPE_ANY, value, Stamp,
            BacnetStatusFlags.STATUS_FLAG_FAULT));

        var len = Services.DecodeLogRecord(buffer.buffer, 0, buffer.GetLength(), 1, out var records);

        Assert.Equal(buffer.GetLength(), len);
        var record = Assert.Single(records);
        Assert.Equal(BacnetTrendLogValueType.TL_TYPE_ANY, record.type);
        Assert.Equal(Stamp, record.timestamp);
        Assert.Equal(BacnetStatusFlags.STATUS_FLAG_FAULT, record.statusFlags);
        return record;
    }

    [Fact]
    public void A_double_wrapped_in_any_value_decodes_unwrapped()
    {
        var len = Services.DecodeLogRecord(WireRecord, 0, WireRecord.Length, 1, out var records);

        Assert.Equal(WireRecord.Length, len);
        var record = Assert.Single(records);
        Assert.Equal(BacnetTrendLogValueType.TL_TYPE_ANY, record.type);
        Assert.Equal(Stamp, record.timestamp);
        Assert.Equal(13.0, Assert.IsType<double>(record.Value));
        Assert.Equal((BacnetStatusFlags)0, record.statusFlags);
    }

    [Fact]
    public void A_double_in_any_value_encodes_like_the_device_sends_it()
    {
        var buffer = new EncodeBuffer();
        Services.EncodeLogRecord(buffer, new BacnetLogRecord(BacnetTrendLogValueType.TL_TYPE_ANY, 13.0, Stamp, 0));

        Assert.Equal(WireRecord, buffer.ToArray());
    }

    [Fact]
    public void The_record_after_an_any_value_record_decodes_too()
    {
        var range = WireRecord.Concat(WireRecord).ToArray();

        var first = Services.DecodeLogRecord(range, 0, range.Length, 1, out _);
        var second = Services.DecodeLogRecord(range, first, range.Length - first, 1, out var records);

        Assert.Equal(WireRecord.Length, second);
        Assert.Equal(13.0, records[0].Value);
    }

    [Fact]
    public void Plain_values_round_trip_through_any_value()
    {
        Assert.Equal(21.5f, RoundTrip(21.5f).Value);
        Assert.Equal(42u, RoundTrip(42u).Value);
        Assert.Equal(-7, RoundTrip(-7).Value);
        Assert.Equal(true, RoundTrip(true).Value);
        Assert.Equal("on", RoundTrip("on").Value);
    }

    [Fact]
    public void A_bacnet_value_keeps_its_tag_through_any_value()
    {
        var value = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED, 3u);

        Assert.Equal(3u, RoundTrip(value).Value);
    }

    [Fact]
    public void Several_values_in_any_value_decode_as_an_array()
    {
        var values = new[] { new BacnetValue(1.5), new BacnetValue(2u) };

        var decoded = Assert.IsType<BacnetValue[]>(RoundTrip(values).Value);

        Assert.Equal(new object[] { 1.5, 2u }, decoded.Select(v => v.Value));
        Assert.Equal(BacnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE, decoded[0].Tag);
    }

    [Fact]
    public void An_empty_any_value_decodes_as_null()
    {
        Assert.Null(RoundTrip(null).Value);
    }

    [Fact]
    public void Context_tagged_content_of_any_value_is_rejected()
    {
        var record = (byte[])WireRecord.Clone();
        record[14] = 0x0D; // context tag [0] with 5 octets instead of the application-tagged Double

        Assert.Equal(-1, Services.DecodeLogRecord(record, 0, record.Length, 1, out _));
    }

    [Fact]
    public void Any_value_without_closing_tag_is_rejected()
    {
        var truncated = WireRecord.Take(24).ToArray(); // ends after the Double

        Assert.Equal(-1, Services.DecodeLogRecord(truncated, 0, truncated.Length, 1, out _));
    }
}
