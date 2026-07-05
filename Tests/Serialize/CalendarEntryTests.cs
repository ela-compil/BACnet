using System.Collections.Generic;
using System.IO.BACnet.Serialize;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// BACnetCalendarEntry CHOICE serialization and date-matching: date [0] (with per-octet wildcards
/// and specials per ASHRAE 135-2016 §20.2.12), date-range [1], weekNDay [2] including the
/// week-of-month octet values 1-9. Golden vectors hand-derived from the Clause 20.2 encoding rules.
/// </summary>
public class CalendarEntryTests
{
    [Fact]
    public void Date_choice_encodes_primitive_context_tag_golden_bytes()
    {
        var buffer = new EncodeBuffer();
        new BacnetCalendarEntry(new BacnetDate(126, 7, 5, 7)).Encode(buffer); // 2026-07-05, a Sunday

        Assert.Equal(new byte[] { 0x0C, 0x7E, 0x07, 0x05, 0x07 }, buffer.ToArray());
    }

    [Fact]
    public void Week_n_day_choice_encodes_primitive_context_tag_golden_bytes()
    {
        var buffer = new EncodeBuffer();
        new BacnetCalendarEntry(new BacnetWeekNDay(
            BacnetDayOfWeekOptions.Friday,
            weekOfMonth: BacnetWeekOfMonthOptions.Days1To7)).Encode(buffer);

        Assert.Equal(new byte[] { 0x2B, 0xFF, 0x01, 0x05 }, buffer.ToArray());
    }

    [Fact]
    public void Date_range_choice_encodes_constructed_golden_bytes()
    {
        var buffer = new EncodeBuffer();
        new BacnetCalendarEntry(new BacnetDateRange(
            new DateTime(2026, 1, 1), new DateTime(2026, 12, 31))).Encode(buffer);

        Assert.Equal(new byte[]
        {
            0x1E,                               // opening tag [1]
            0xA4, 0x7E, 0x01, 0x01, 0x04,       // Date 2026-01-01 (Thursday)
            0xA4, 0x7E, 0x0C, 0x1F, 0x04,       // Date 2026-12-31 (Thursday)
            0x1F                                // closing tag [1]
        }, buffer.ToArray());
    }

    [Theory]
    [InlineData(new byte[] { 0x0C, 0xFF, 0xFF, 0xFF, 0x05 })] // date pattern: every Friday, any year/month/day
    [InlineData(new byte[] { 0x0C, 0xFF, 0x0D, 0x20, 0xFF })] // last day of every odd month
    [InlineData(new byte[] { 0x2B, 0x0E, 0x06, 0xFF })]       // even months, last 7 days
    public void Wildcard_patterns_round_trip_per_octet(byte[] wire)
    {
        var entry = new BacnetCalendarEntry();
        var len = entry.Decode(wire, 0, (uint)wire.Length);
        Assert.Equal(wire.Length, len);

        var buffer = new EncodeBuffer();
        entry.Encode(buffer);
        Assert.Equal(wire, buffer.ToArray());
    }

    [Fact]
    public void Date_list_read_yields_one_value_per_entry_and_writes_back_identical()
    {
        var entries = new List<BacnetValue>
        {
            new BacnetValue(new BacnetCalendarEntry(new BacnetDate(new DateTime(2026, 12, 24)))),
            new BacnetValue(new BacnetCalendarEntry(new BacnetDateRange(new DateTime(2026, 7, 1), new DateTime(2026, 7, 31)))),
            new BacnetValue(new BacnetCalendarEntry(new BacnetWeekNDay(BacnetDayOfWeekOptions.Sunday)))
        };

        var buffer = new EncodeBuffer();
        Services.EncodeReadPropertyAcknowledge(buffer,
            new BacnetObjectId(BacnetObjectTypes.OBJECT_CALENDAR, 1),
            (uint)BacnetPropertyIds.PROP_DATE_LIST, ASN1.BACNET_ARRAY_ALL, entries);
        var bytes = buffer.ToArray();

        var len = Services.DecodeReadPropertyAcknowledge(new BacnetAddress(BacnetAddressTypes.None, 0, null),
            bytes, 0, bytes.Length, out _, out _, out var values);

        Assert.True(len > 0);
        Assert.Equal(3, values.Count);
        Assert.All(values, v => Assert.Equal(BacnetApplicationTags.BACNET_APPLICATION_TAG_CALENDAR_ENTRY, v.Tag));
        Assert.NotNull(((BacnetCalendarEntry)values[0].Value).Date);
        Assert.NotNull(((BacnetCalendarEntry)values[1].Value).DateRange);
        Assert.NotNull(((BacnetCalendarEntry)values[2].Value).WeekNDay);

        var rewritten = new EncodeBuffer();
        Services.EncodeReadPropertyAcknowledge(rewritten,
            new BacnetObjectId(BacnetObjectTypes.OBJECT_CALENDAR, 1),
            (uint)BacnetPropertyIds.PROP_DATE_LIST, ASN1.BACNET_ARRAY_ALL, values);
        Assert.Equal(bytes, rewritten.ToArray());
    }

    [Fact]
    public void Unknown_choice_tag_decodes_as_error()
    {
        Assert.Equal(-1, new BacnetCalendarEntry().Decode(new byte[] { 0x3C, 0x01, 0x02, 0x03, 0x04 }, 0, 5));
    }

    [Fact]
    public void Tag_is_guessed_from_the_calendar_entry_type()
    {
        Assert.Equal(BacnetApplicationTags.BACNET_APPLICATION_TAG_CALENDAR_ENTRY,
            new BacnetValue(new BacnetCalendarEntry(new BacnetDate(new DateTime(2026, 1, 1)))).Tag);
    }

    [Fact]
    public void Day_32_matches_the_last_day_of_any_month()
    {
        var lastDayOfFebruary = new BacnetDate(255, 2, 32);

        Assert.True(lastDayOfFebruary.IsAFittingDate(new DateTime(2026, 2, 28)));
        Assert.False(lastDayOfFebruary.IsAFittingDate(new DateTime(2026, 2, 27)));
        Assert.True(lastDayOfFebruary.IsAFittingDate(new DateTime(2028, 2, 29))); // leap year
    }

    [Fact]
    public void Day_32_converts_to_the_last_day_of_the_month()
    {
        Assert.Equal(new DateTime(2026, 2, 28), new BacnetDate(126, 2, 32).ToDateTime());
    }

    [Theory]
    [InlineData(BacnetWeekOfMonthOptions.Days8To14, 10, true)]
    [InlineData(BacnetWeekOfMonthOptions.Days8To14, 15, false)]
    [InlineData(BacnetWeekOfMonthOptions.Days29To31, 30, true)]
    [InlineData(BacnetWeekOfMonthOptions.Last7Days, 25, true)]           // July has 31 days: last 7 = 25-31
    [InlineData(BacnetWeekOfMonthOptions.Last7Days, 24, false)]
    [InlineData(BacnetWeekOfMonthOptions.Prior7DaysToLast7Days, 18, true)]  // 18-24
    [InlineData(BacnetWeekOfMonthOptions.Prior7DaysToLast7Days, 25, false)]
    [InlineData(BacnetWeekOfMonthOptions.Prior7DaysToLast14Days, 11, true)] // 11-17
    [InlineData(BacnetWeekOfMonthOptions.Prior7DaysToLast21Days, 4, true)]  // 4-10
    [InlineData(BacnetWeekOfMonthOptions.AnyWeek, 1, true)]
    public void Week_of_month_arithmetic_matches_the_spec_slices(BacnetWeekOfMonthOptions week, int day, bool expected)
    {
        var pattern = new BacnetWeekNDay(BacnetDayOfWeekOptions.AnyDayOfWeek, weekOfMonth: week);

        Assert.Equal(expected, pattern.IsAFittingDate(new DateTime(2026, 7, day)));
    }

    [Theory]
    [InlineData(BacnetMonthOptions.OddMonths, 7, true)]
    [InlineData(BacnetMonthOptions.OddMonths, 8, false)]
    [InlineData(BacnetMonthOptions.EvenMonths, 8, true)]
    [InlineData(BacnetMonthOptions.July, 7, true)]
    [InlineData(BacnetMonthOptions.July, 6, false)]
    public void Month_options_match_odd_even_and_specific_months(BacnetMonthOptions month, int actualMonth, bool expected)
    {
        var pattern = new BacnetWeekNDay(BacnetDayOfWeekOptions.AnyDayOfWeek, month);

        Assert.Equal(expected, pattern.IsAFittingDate(new DateTime(2026, actualMonth, 1)));
    }

    [Fact]
    public void Sunday_maps_between_bacnet_7_and_dotnet_0()
    {
        var sundays = new BacnetWeekNDay(BacnetDayOfWeekOptions.Sunday);

        Assert.True(sundays.IsAFittingDate(new DateTime(2026, 7, 5)));   // a Sunday
        Assert.False(sundays.IsAFittingDate(new DateTime(2026, 7, 6)));  // a Monday
    }

    [Fact]
    public void Fully_wildcarded_date_range_fits_any_date()
    {
        // the default Effective_Period of a Schedule object is exactly this: always in effect
        var range = new BacnetDateRange(new BacnetDate(255, 255, 255), new BacnetDate(255, 255, 255));

        Assert.True(range.IsAFittingDate(new DateTime(1926, 1, 1)));
        Assert.True(range.IsAFittingDate(new DateTime(2126, 12, 31)));
    }

    [Fact]
    public void Half_open_date_range_checks_only_the_specified_boundary()
    {
        var until2026 = new BacnetDateRange(new BacnetDate(255, 255, 255), new BacnetDate(126, 12, 31));

        Assert.True(until2026.IsAFittingDate(new DateTime(1990, 5, 5)));
        Assert.False(until2026.IsAFittingDate(new DateTime(2027, 1, 1)));
    }
}
