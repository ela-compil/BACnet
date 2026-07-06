using System.Collections.Generic;
using System.IO.BACnet.Serialize;
using System.IO.BACnet.Tests.Support;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// BACnetTimeStamp and BACnetDateTime round-trips including wildcarded octets (ASHRAE 135-2016
/// §20.2.13): decoded Event_Time_Stamps keep their CHOICE alternative and any unspecified time
/// octets (BacnetGenericTime.PartialTime), and a Date+Time property pair merges into the
/// per-octet BacnetDateTime whenever a merged DateTime cannot carry the wildcards.
/// </summary>
public class TimestampAndDateTimeTests
{
    private static BacnetValue DecodeEventTimeStamp(byte[] wire)
    {
        return ApplicationValue.Decode(wire, BacnetPropertyIds.PROP_EVENT_TIME_STAMPS);
    }

    [Fact]
    public void Datetime_timestamp_round_trips_and_keeps_its_choice()
    {
        var wire = new byte[]
        {
            0x2E,                          // dateTime choice [2]
            0xA4, 126, 7, 6, 1,            // Date 2026-07-06 (Monday)
            0xB4, 8, 0, 0, 0,              // Time 08:00:00.00
            0x2F
        };

        var value = DecodeEventTimeStamp(wire);

        var stamp = Assert.IsType<BacnetGenericTime>(value.Value);
        Assert.Equal(BacnetTimestampTags.TIME_STAMP_DATETIME, stamp.Tag);
        Assert.Equal(new DateTime(2026, 7, 6, 8, 0, 0), stamp.Time);
        Assert.Null(stamp.PartialTime);
        ApplicationValue.AssertReencodesTo(wire, value); // used to throw: the decoded value was a bare DateTime
    }

    [Fact]
    public void Fully_wildcarded_datetime_timestamp_round_trips()
    {
        // the timestamp of a transition that never happened
        var wire = new byte[]
        {
            0x2E,
            0xA4, 0xFF, 0xFF, 0xFF, 0xFF,
            0xB4, 0xFF, 0xFF, 0xFF, 0xFF,
            0x2F
        };

        var value = DecodeEventTimeStamp(wire);

        var stamp = Assert.IsType<BacnetGenericTime>(value.Value);
        Assert.Equal(BacnetTimestampTags.TIME_STAMP_DATETIME, stamp.Tag);
        Assert.NotNull(stamp.PartialTime);
        ApplicationValue.AssertReencodesTo(wire, value);
    }

    [Fact]
    public void Partially_wildcarded_time_timestamp_round_trips_with_a_clamped_best_effort()
    {
        var wire = new byte[] { 0x0C, 11, 22, 0xFF, 0xFF }; // time choice [0]: 11:22:**.**

        var value = DecodeEventTimeStamp(wire);

        var stamp = Assert.IsType<BacnetGenericTime>(value.Value);
        Assert.Equal(BacnetTimestampTags.TIME_STAMP_TIME, stamp.Tag);
        Assert.Equal(new TimeSpan(11, 22, 0), stamp.Time.TimeOfDay); // best-effort for consumers
        Assert.Equal(new BacnetTime(11, 22, 255, 255), stamp.PartialTime); // lossless for re-encode
        ApplicationValue.AssertReencodesTo(wire, value);
    }

    [Fact]
    public void Sequence_timestamp_still_decodes_as_an_unsigned()
    {
        var value = DecodeEventTimeStamp(new byte[] { 0x19, 5 }); // sequenceNumber choice [1]

        Assert.Equal(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT, value.Tag);
        Assert.Equal(5u, value.Value);
    }

    private static IList<BacnetValue> MergeThroughReadAccessResult(params BacnetValue[] values)
    {
        var result = new BacnetReadAccessResult(new BacnetObjectId(BacnetObjectTypes.OBJECT_DATETIME_VALUE, 1),
            new List<BacnetPropertyValue>
            {
                new BacnetPropertyValue
                {
                    property = new BacnetPropertyReference(BacnetPropertyIds.PROP_PRESENT_VALUE),
                    value = new List<BacnetValue>(values)
                }
            });

        var buffer = new EncodeBuffer();
        ASN1.encode_read_access_result(buffer, result);
        var bytes = buffer.ToArray();

        var len = ASN1.decode_read_access_result(ApplicationValue.DummyAddress, bytes, 0, bytes.Length, out var decoded);
        Assert.True(len > 0);
        return decoded.values[0].value;
    }

    [Fact]
    public void Specific_date_time_pair_still_merges_into_a_DateTime()
    {
        var merged = MergeThroughReadAccessResult(
            new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE, new DateTime(2026, 7, 6)),
            new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME, new DateTime(1, 1, 1, 8, 0, 0)));

        var value = Assert.Single(merged);
        Assert.Equal(BacnetApplicationTags.BACNET_APPLICATION_TAG_DATETIME, value.Tag);
        Assert.Equal(new DateTime(2026, 7, 6, 8, 0, 0), value.Value);
    }

    [Fact]
    public void Wildcarded_date_time_pair_merges_into_a_per_octet_BacnetDateTime()
    {
        // a date pattern plus a partially-wildcarded time: neither part fits a DateTime, and the
        // old merge crashed on the cast
        var merged = MergeThroughReadAccessResult(
            new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE, new BacnetDate(255, 13, 32)),
            new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME, new BacnetTime(11, 22, 255, 255)));

        var value = Assert.Single(merged);
        Assert.Equal(BacnetApplicationTags.BACNET_APPLICATION_TAG_DATETIME, value.Tag);
        var dateTime = Assert.IsType<BacnetDateTime>(value.Value);
        Assert.Equal((byte)13, dateTime.Date.month);
        Assert.Equal((byte)32, dateTime.Date.day);
        Assert.Equal(new BacnetTime(11, 22, 255, 255), dateTime.Time);

        var buffer = new EncodeBuffer();
        ASN1.bacapp_encode_application_data(buffer, value);
        Assert.Equal(new byte[]
        {
            0xA4, 0xFF, 13, 32, 0xFF,
            0xB4, 11, 22, 0xFF, 0xFF
        }, buffer.ToArray());
    }

    [Fact]
    public void BacnetDateTime_struct_round_trips_byte_identical()
    {
        var original = new BacnetDateTime(new BacnetDate(126, 7, 6, 1), new BacnetTime(8, 0));

        var buffer = new EncodeBuffer();
        original.Encode(buffer);
        var bytes = buffer.ToArray();

        var decoded = new BacnetDateTime();
        var len = decoded.Decode(bytes, 0, (uint)bytes.Length);

        Assert.Equal(bytes.Length, len);
        Assert.Equal(original, decoded);
        Assert.Equal(new DateTime(2026, 7, 6, 8, 0, 0), decoded.ToDateTime());
        Assert.True(decoded.IsFullySpecified);
    }
}
