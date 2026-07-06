using System.Collections.Generic;
using System.IO.BACnet.Serialize;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// Exception_Schedule serialization (#131): BACnetSpecialEvent per ASHRAE 135-2016 Clause 21, with
/// both period choices (calendar-entry and calendar-reference). A device that supports writing the
/// property must accept all choices (Clause 12.24.8). Golden vectors hand-derived from Clause 20.2.
/// </summary>
public class ExceptionScheduleTests
{
    private static readonly BacnetAddress DummyAddress = new BacnetAddress(BacnetAddressTypes.None, 0, null);

    [Fact]
    public void Special_event_with_date_period_encodes_the_expected_wire_bytes()
    {
        var specialEvent = new BacnetSpecialEvent(
            new BacnetCalendarEntry(new BacnetDate(126, 7, 5, 7)), // 2026-07-05, a Sunday
            new[] { new BacnetTimeValue(new TimeSpan(8, 0, 0), new BacnetValue(22.5f)) },
            eventPriority: 16);

        var buffer = new EncodeBuffer();
        specialEvent.Encode(buffer);

        Assert.Equal(new byte[]
        {
            0x0E,                               // period calendar-entry: opening tag [0]
            0x0C, 0x7E, 0x07, 0x05, 0x07,       //   date choice [0], 2026-07-05 Sunday
            0x0F,                               // closing tag [0]
            0x2E,                               // list-of-time-values: opening tag [2]
            0xB4, 0x08, 0x00, 0x00, 0x00,       //   Time 08:00:00.00
            0x44, 0x41, 0xB4, 0x00, 0x00,       //   REAL 22.5
            0x2F,                               // closing tag [2]
            0x39, 0x10                          // event-priority [3] = 16
        }, buffer.ToArray());
    }

    [Fact]
    public void Special_event_with_calendar_reference_encodes_the_expected_wire_bytes()
    {
        var specialEvent = new BacnetSpecialEvent(
            new BacnetObjectId(BacnetObjectTypes.OBJECT_CALENDAR, 1),
            new BacnetTimeValue[0],
            eventPriority: 4);

        var buffer = new EncodeBuffer();
        specialEvent.Encode(buffer);

        Assert.Equal(new byte[]
        {
            0x1C, 0x01, 0x80, 0x00, 0x01,       // period calendar-reference [1]: calendar,1
            0x2E, 0x2F,                         // empty list-of-time-values [2]
            0x39, 0x04                          // event-priority [3] = 4
        }, buffer.ToArray());
    }

    [Theory]
    [MemberData(nameof(AllPeriodChoices))]
    public void Every_period_choice_round_trips(BacnetSpecialEvent specialEvent)
    {
        var buffer = new EncodeBuffer();
        specialEvent.Encode(buffer);
        var bytes = buffer.ToArray();

        var decoded = new BacnetSpecialEvent();
        var len = decoded.Decode(bytes, 0, (uint)bytes.Length);

        Assert.Equal(bytes.Length, len);
        Assert.Equal(specialEvent.EventPriority, decoded.EventPriority);
        Assert.Equal(specialEvent.ListOfTimeValues.Count, decoded.ListOfTimeValues.Count);

        var reencoded = new EncodeBuffer();
        decoded.Encode(reencoded);
        Assert.Equal(bytes, reencoded.ToArray());
    }

    public static IEnumerable<object[]> AllPeriodChoices()
    {
        var timeValues = new[]
        {
            new BacnetTimeValue(new TimeSpan(6, 0, 0), new BacnetValue(1.5f)),
            new BacnetTimeValue(new TimeSpan(22, 15, 30), new BacnetValue(null))
        };

        yield return new object[] { new BacnetSpecialEvent(
            new BacnetCalendarEntry(new BacnetDate(new DateTime(2026, 12, 24))), timeValues, 1) };
        yield return new object[] { new BacnetSpecialEvent(
            new BacnetCalendarEntry(new BacnetDateRange(new DateTime(2026, 7, 1), new DateTime(2026, 8, 31))), timeValues, 8) };
        yield return new object[] { new BacnetSpecialEvent(
            new BacnetCalendarEntry(new BacnetWeekNDay(BacnetDayOfWeekOptions.Friday)), timeValues, 16) };
        yield return new object[] { new BacnetSpecialEvent(
            new BacnetObjectId(BacnetObjectTypes.OBJECT_CALENDAR, 42), timeValues, 6) };
    }

    [Fact]
    public void Exception_schedule_read_values_write_back_byte_identical()
    {
        // the exact #131 flow: ReadProperty the array, WriteProperty the same values back
        var events = new List<BacnetValue>
        {
            new BacnetValue(new BacnetSpecialEvent(
                new BacnetCalendarEntry(new BacnetDate(126, 7, 6, 1)),
                new[] { new BacnetTimeValue(new TimeSpan(7, 30, 0), new BacnetValue(88.0f)) }, 5)),
            new BacnetValue(new BacnetSpecialEvent(
                new BacnetObjectId(BacnetObjectTypes.OBJECT_CALENDAR, 0),
                new[] { new BacnetTimeValue(new TimeSpan(0, 0, 0), new BacnetValue(12.0f)) }, 12))
        };

        var ack = new EncodeBuffer();
        Services.EncodeReadPropertyAcknowledge(ack,
            new BacnetObjectId(BacnetObjectTypes.OBJECT_SCHEDULE, 1),
            (uint)BacnetPropertyIds.PROP_EXCEPTION_SCHEDULE, ASN1.BACNET_ARRAY_ALL, events);
        var ackBytes = ack.ToArray();

        var len = Services.DecodeReadPropertyAcknowledge(DummyAddress, ackBytes, 0, ackBytes.Length,
            out _, out _, out var readValues);
        Assert.Equal(ackBytes.Length, len);
        Assert.Equal(2, readValues.Count);
        Assert.All(readValues, v => Assert.Equal(BacnetApplicationTags.BACNET_APPLICATION_TAG_SPECIAL_EVENT, v.Tag));

        var write = new EncodeBuffer();
        Services.EncodeWriteProperty(write, new BacnetObjectId(BacnetObjectTypes.OBJECT_SCHEDULE, 1),
            (uint)BacnetPropertyIds.PROP_EXCEPTION_SCHEDULE, ASN1.BACNET_ARRAY_ALL, 0, readValues);
        var writeBytes = write.ToArray();

        // the write built from the decoded values must equal one built from the original values
        var reference = new EncodeBuffer();
        Services.EncodeWriteProperty(reference, new BacnetObjectId(BacnetObjectTypes.OBJECT_SCHEDULE, 1),
            (uint)BacnetPropertyIds.PROP_EXCEPTION_SCHEDULE, ASN1.BACNET_ARRAY_ALL, 0, events);
        Assert.Equal(reference.ToArray(), writeBytes);

        var writeLen = Services.DecodeWriteProperty(DummyAddress, writeBytes, 0, writeBytes.Length,
            out _, out var written);
        Assert.Equal(writeBytes.Length, writeLen);
        Assert.Equal(2, written.value.Count);

        var first = Assert.IsType<BacnetSpecialEvent>(written.value[0].Value);
        Assert.Equal(5u, first.EventPriority);
        Assert.NotNull(first.CalendarEntry?.Date);
        var second = Assert.IsType<BacnetSpecialEvent>(written.value[1].Value);
        Assert.Equal(new BacnetObjectId(BacnetObjectTypes.OBJECT_CALENDAR, 0), second.CalendarReference);
        Assert.Equal(TimeSpan.Zero, second.ListOfTimeValues[0].Time); // midnight survives, no wildcard
    }

    [Fact]
    public void Missing_priority_decodes_as_error()
    {
        // period + empty time values, then truncated before event-priority [3]
        var bytes = new byte[] { 0x1C, 0x01, 0x80, 0x00, 0x01, 0x2E, 0x2F };

        Assert.Equal(-1, new BacnetSpecialEvent().Decode(bytes, 0, (uint)bytes.Length));
    }

    [Fact]
    public void Special_event_without_period_cannot_be_encoded()
    {
        Assert.Throws<InvalidOperationException>(() => new BacnetSpecialEvent().Encode(new EncodeBuffer()));
    }

    [Fact]
    public void Tag_is_guessed_from_the_special_event_type()
    {
        Assert.Equal(BacnetApplicationTags.BACNET_APPLICATION_TAG_SPECIAL_EVENT,
            new BacnetValue(new BacnetSpecialEvent(
                new BacnetObjectId(BacnetObjectTypes.OBJECT_CALENDAR, 1), new BacnetTimeValue[0], 1)).Tag);
    }
}
