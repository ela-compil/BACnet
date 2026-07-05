using System.Collections.Generic;
using System.IO.BACnet.Serialize;
using System.Linq;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Weekly_Schedule serialization (#26): BACnetDailySchedule ::= SEQUENCE { day-schedule [0] SEQUENCE OF
/// BACnetTimeValue } per ASHRAE 135-2016 Clause 21, served as a BACnetARRAY[7] (Clause 12.24.7).
/// Golden vectors are hand-derived from the Clause 20.2 encoding rules.
/// </summary>
public class WeeklyScheduleTests
{
    private static BacnetDailySchedule OneEntryDay() => new BacnetDailySchedule(new[]
    {
        new BacnetTimeValue(new TimeSpan(8, 0, 0), new BacnetValue(22.5f))
    });

    [Fact]
    public void Daily_schedule_with_one_time_value_encodes_golden_bytes()
    {
        var buffer = new EncodeBuffer();
        ASN1.bacapp_encode_application_data(buffer, new BacnetValue(OneEntryDay()));

        Assert.Equal(new byte[]
        {
            0x0E,                               // opening tag [0]
            0xB4, 0x08, 0x00, 0x00, 0x00,       // Time 08:00:00.00
            0x44, 0x41, 0xB4, 0x00, 0x00,       // REAL 22.5
            0x0F                                // closing tag [0]
        }, buffer.ToArray());
    }

    [Fact]
    public void Empty_daily_schedule_encodes_opening_and_closing_tag_only()
    {
        var buffer = new EncodeBuffer();
        new BacnetDailySchedule().Encode(buffer);

        Assert.Equal(new byte[] { 0x0E, 0x0F }, buffer.ToArray());
    }

    [Fact]
    public void Empty_daily_schedule_decodes_to_empty_list()
    {
        var schedule = new BacnetDailySchedule();
        var len = schedule.Decode(new byte[] { 0x0E, 0x0F }, 0, 2);

        Assert.Equal(2, len);
        Assert.Empty(schedule.DaySchedule);
    }

    [Fact]
    public void Midnight_entry_encodes_as_zeros_not_as_time_wildcard()
    {
        // 00:00 entries define whole-day schedules (Clause 12.24) and must never degrade to FF FF FF FF
        var buffer = new EncodeBuffer();
        new BacnetTimeValue(TimeSpan.Zero, new BacnetValue(false)).Encode(buffer);

        Assert.Equal(new byte[] { 0xB4, 0x00, 0x00, 0x00, 0x00, 0x10 }, buffer.ToArray());
    }

    [Fact]
    public void Null_schedule_value_round_trips()
    {
        // NULL at 17:00 = "relinquish"; encodes as the bare NULL application tag
        var buffer = new EncodeBuffer();
        new BacnetTimeValue(new TimeSpan(17, 0, 0), new BacnetValue(null)).Encode(buffer);
        var bytes = buffer.ToArray();

        Assert.Equal(new byte[] { 0xB4, 0x11, 0x00, 0x00, 0x00, 0x00 }, bytes);

        var decoded = new BacnetTimeValue();
        var len = decoded.Decode(bytes, 0, (uint)bytes.Length);

        Assert.Equal(bytes.Length, len);
        Assert.Equal(new TimeSpan(17, 0, 0), decoded.Time);
        Assert.Equal(BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL, decoded.Value.Tag);
        Assert.Null(decoded.Value.Value);
    }

    [Fact]
    public void Read_property_ack_with_seven_daily_schedules_yields_seven_values()
    {
        var week = SevenDays();

        var buffer = new EncodeBuffer();
        Services.EncodeReadPropertyAcknowledge(buffer,
            new BacnetObjectId(BacnetObjectTypes.OBJECT_SCHEDULE, 1),
            (uint)BacnetPropertyIds.PROP_WEEKLY_SCHEDULE, ASN1.BACNET_ARRAY_ALL, week);
        var bytes = buffer.ToArray();

        var len = Services.DecodeReadPropertyAcknowledge(new BacnetAddress(BacnetAddressTypes.None, 0, null),
            bytes, 0, bytes.Length, out var objectId, out var property, out var values);

        Assert.True(len > 0);
        Assert.Equal(BacnetObjectTypes.OBJECT_SCHEDULE, objectId.type);
        Assert.Equal((uint)BacnetPropertyIds.PROP_WEEKLY_SCHEDULE, property.propertyIdentifier);
        Assert.Equal(7, values.Count);
        Assert.All(values, v => Assert.Equal(BacnetApplicationTags.BACNET_APPLICATION_TAG_WEEKLY_SCHEDULE, v.Tag));

        var monday = Assert.IsType<BacnetDailySchedule>(values[0].Value);
        var entry = Assert.Single(monday.DaySchedule);
        Assert.Equal(new TimeSpan(8, 0, 0), entry.Time);
        Assert.Equal(22.5f, entry.Value.Value);
        Assert.Empty(((BacnetDailySchedule)values[6].Value).DaySchedule);
    }

    [Fact]
    public void Decoded_weekly_schedule_written_back_re_encodes_byte_identical()
    {
        // the #26/#131 acceptance criterion: what was read can be written back unchanged
        var ackBuffer = new EncodeBuffer();
        Services.EncodeReadPropertyAcknowledge(ackBuffer,
            new BacnetObjectId(BacnetObjectTypes.OBJECT_SCHEDULE, 1),
            (uint)BacnetPropertyIds.PROP_WEEKLY_SCHEDULE, ASN1.BACNET_ARRAY_ALL, SevenDays());
        var ackBytes = ackBuffer.ToArray();

        Services.DecodeReadPropertyAcknowledge(new BacnetAddress(BacnetAddressTypes.None, 0, null),
            ackBytes, 0, ackBytes.Length, out _, out _, out var values);

        var rewritten = new EncodeBuffer();
        Services.EncodeReadPropertyAcknowledge(rewritten,
            new BacnetObjectId(BacnetObjectTypes.OBJECT_SCHEDULE, 1),
            (uint)BacnetPropertyIds.PROP_WEEKLY_SCHEDULE, ASN1.BACNET_ARRAY_ALL, values);

        Assert.Equal(ackBytes, rewritten.ToArray());
    }

    [Fact]
    public void Write_property_request_of_weekly_schedule_round_trips()
    {
        var buffer = new EncodeBuffer();
        Services.EncodeWriteProperty(buffer, new BacnetObjectId(BacnetObjectTypes.OBJECT_SCHEDULE, 1),
            (uint)BacnetPropertyIds.PROP_WEEKLY_SCHEDULE, ASN1.BACNET_ARRAY_ALL, 0, SevenDays());
        var bytes = buffer.ToArray();

        var len = Services.DecodeWriteProperty(new BacnetAddress(BacnetAddressTypes.None, 0, null),
            bytes, 0, bytes.Length, out var objectId, out var value);

        Assert.True(len > 0);
        Assert.Equal(BacnetObjectTypes.OBJECT_SCHEDULE, objectId.type);
        Assert.Equal(7, value.value.Count);
        var tuesday = Assert.IsType<BacnetDailySchedule>(value.value[1].Value);
        Assert.Equal(2, tuesday.DaySchedule.Count);
        Assert.Equal(new TimeSpan(18, 30, 0), tuesday.DaySchedule[1].Time);
    }

    [Fact]
    public void Array_index_zero_still_decodes_as_the_element_count()
    {
        var buffer = new EncodeBuffer();
        Services.EncodeReadPropertyAcknowledge(buffer,
            new BacnetObjectId(BacnetObjectTypes.OBJECT_SCHEDULE, 1),
            (uint)BacnetPropertyIds.PROP_WEEKLY_SCHEDULE, 0, new[] { new BacnetValue(7u) });
        var bytes = buffer.ToArray();

        var len = Services.DecodeReadPropertyAcknowledge(new BacnetAddress(BacnetAddressTypes.None, 0, null),
            bytes, 0, bytes.Length, out _, out var property, out var values);

        Assert.True(len > 0);
        Assert.Equal(0u, property.propertyArrayIndex);
        var count = Assert.Single(values);
        Assert.Equal(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT, count.Tag);
        Assert.Equal(7u, count.Value);
    }

    [Fact]
    public void Single_array_element_read_decodes_one_daily_schedule()
    {
        var buffer = new EncodeBuffer();
        Services.EncodeReadPropertyAcknowledge(buffer,
            new BacnetObjectId(BacnetObjectTypes.OBJECT_SCHEDULE, 1),
            (uint)BacnetPropertyIds.PROP_WEEKLY_SCHEDULE, 1, new[] { new BacnetValue(OneEntryDay()) });
        var bytes = buffer.ToArray();

        var len = Services.DecodeReadPropertyAcknowledge(new BacnetAddress(BacnetAddressTypes.None, 0, null),
            bytes, 0, bytes.Length, out _, out var property, out var values);

        Assert.True(len > 0);
        Assert.Equal(1u, property.propertyArrayIndex);
        var day = Assert.Single(values);
        Assert.IsType<BacnetDailySchedule>(day.Value);
    }

    [Fact]
    public void Tag_is_guessed_from_the_daily_schedule_type()
    {
        Assert.Equal(BacnetApplicationTags.BACNET_APPLICATION_TAG_WEEKLY_SCHEDULE,
            new BacnetValue(new BacnetDailySchedule()).Tag);
    }

    [Fact]
    public void Truncated_daily_schedule_decode_returns_minus_one()
    {
        var buffer = new EncodeBuffer();
        OneEntryDay().Encode(buffer);
        var bytes = buffer.ToArray();

        var truncated = bytes.Take(bytes.Length - 1).ToArray(); // drop the closing tag
        Assert.Equal(-1, new BacnetDailySchedule().Decode(truncated, 0, (uint)truncated.Length));
    }

    private static List<BacnetValue> SevenDays()
    {
        // Monday one entry, Tuesday two (character-string value guards extended-length primitives),
        // the rest empty - shapes a realistic sparse week
        var tuesday = new BacnetDailySchedule(new[]
        {
            new BacnetTimeValue(new TimeSpan(6, 15, 0), new BacnetValue("occupied")),
            new BacnetTimeValue(new TimeSpan(18, 30, 0), new BacnetValue(null))
        });

        var week = new List<BacnetValue> { new BacnetValue(OneEntryDay()), new BacnetValue(tuesday) };
        for (var i = 2; i < 7; i++)
            week.Add(new BacnetValue(new BacnetDailySchedule()));
        return week;
    }
}
