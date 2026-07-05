using System.Collections.Generic;
using System.IO.BACnet.Storage;
using System.Linq;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// XML device-storage persistence of the schedule value types: they are stored as base64 of
/// their ASN.1 encoding (the same convention octet strings use) and must survive a
/// serialize/deserialize round trip.
/// </summary>
public class PropertyScheduleTests
{
    [Fact]
    public void Weekly_schedule_property_round_trips_through_storage()
    {
        var week = new List<BacnetValue>();
        week.Add(new BacnetValue(new BacnetDailySchedule(new[]
        {
            new BacnetTimeValue(new TimeSpan(8, 0, 0), new BacnetValue(21.5f)),
            new BacnetTimeValue(new TimeSpan(18, 0, 0), new BacnetValue(null))
        })));
        for (var i = 1; i < 7; i++)
            week.Add(new BacnetValue(new BacnetDailySchedule()));

        var property = new Property
        {
            Id = BacnetPropertyIds.PROP_WEEKLY_SCHEDULE,
            Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_WEEKLY_SCHEDULE,
            BacnetValue = week
        };

        var restored = property.BacnetValue;

        Assert.Equal(7, restored.Count);
        var monday = Assert.IsType<BacnetDailySchedule>(restored[0].Value);
        Assert.Equal(2, monday.DaySchedule.Count);
        Assert.Equal(new TimeSpan(8, 0, 0), monday.DaySchedule[0].Time);
        Assert.Equal(21.5f, monday.DaySchedule[0].Value.Value);
        Assert.True(restored.Skip(1).All(v => ((BacnetDailySchedule)v.Value).DaySchedule.Count == 0));
    }

    [Fact]
    public void Exception_schedule_property_round_trips_through_storage()
    {
        var property = new Property
        {
            Id = BacnetPropertyIds.PROP_EXCEPTION_SCHEDULE,
            Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_SPECIAL_EVENT,
            BacnetValue = new List<BacnetValue>
            {
                new BacnetValue(new BacnetSpecialEvent(
                    new BacnetCalendarEntry(new BacnetDate(new DateTime(2026, 12, 24))),
                    new[] { new BacnetTimeValue(new TimeSpan(6, 0, 0), new BacnetValue(16.0f)) }, 7)),
                new BacnetValue(new BacnetSpecialEvent(
                    new BacnetObjectId(BacnetObjectTypes.OBJECT_CALENDAR, 2), new BacnetTimeValue[0], 3))
            }
        };

        var restored = property.BacnetValue;

        Assert.Equal(2, restored.Count);
        var first = Assert.IsType<BacnetSpecialEvent>(restored[0].Value);
        Assert.Equal(7u, first.EventPriority);
        Assert.NotNull(first.CalendarEntry?.Date);
        var second = Assert.IsType<BacnetSpecialEvent>(restored[1].Value);
        Assert.Equal(new BacnetObjectId(BacnetObjectTypes.OBJECT_CALENDAR, 2), second.CalendarReference);
    }

    [Fact]
    public void Date_list_property_round_trips_through_storage()
    {
        var property = new Property
        {
            Id = BacnetPropertyIds.PROP_DATE_LIST,
            Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_CALENDAR_ENTRY,
            BacnetValue = new List<BacnetValue>
            {
                new BacnetValue(new BacnetCalendarEntry(new BacnetDate(255, 12, 25))),
                new BacnetValue(new BacnetCalendarEntry(new BacnetDateRange(new DateTime(2026, 7, 1), new DateTime(2026, 8, 31)))),
                new BacnetValue(new BacnetCalendarEntry(new BacnetWeekNDay(BacnetDayOfWeekOptions.Monday)))
            }
        };

        var restored = property.BacnetValue;

        Assert.Equal(3, restored.Count);
        Assert.NotNull(((BacnetCalendarEntry)restored[0].Value).Date);
        Assert.NotNull(((BacnetCalendarEntry)restored[1].Value).DateRange);
        Assert.NotNull(((BacnetCalendarEntry)restored[2].Value).WeekNDay);
    }
}
