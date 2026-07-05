using System.Collections.Generic;
using BaCSharp;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// The Schedule object's Present_Value evaluation (ASHRAE 135-2016 Clause 12.24.4), tested on the
/// pure ScheduleCalculation core: special events by priority (index tie-break) when in effect and
/// non-NULL, then the current day's weekly schedule, then null = Schedule_Default; all gated by
/// Effective_Period. Monday 2026-07-06 is the reference day.
/// </summary>
public class ScheduleAlgorithmTests
{
    private static readonly DateTime MondayMorning = new DateTime(2026, 7, 6, 9, 0, 0);
    private static readonly BacnetDateRange Always =
        new BacnetDateRange(new BacnetDate(255, 255, 255), new BacnetDate(255, 255, 255));

    private static BacnetDailySchedule[] Week(params BacnetTimeValue[] mondayEntries)
    {
        var week = new BacnetDailySchedule[7];
        for (var i = 0; i < 7; i++)
            week[i] = new BacnetDailySchedule();
        week[0].DaySchedule.AddRange(mondayEntries);
        return week;
    }

    private static BacnetTimeValue At(int hour, object value) =>
        new BacnetTimeValue(new TimeSpan(hour, 0, 0), new BacnetValue(value));

    private static BacnetSpecialEvent TodayEvent(uint priority, params BacnetTimeValue[] timeValues) =>
        new BacnetSpecialEvent(new BacnetCalendarEntry(new BacnetDate(MondayMorning)), timeValues, priority);

    private static object Compute(DateTime now, BacnetDailySchedule[] weekly,
        IReadOnlyList<BacnetSpecialEvent> exceptions = null, BacnetDateRange? period = null,
        Func<BacnetObjectId, bool?> calendar = null)
    {
        return ScheduleCalculation.ComputePresentValue(now, weekly,
            exceptions ?? new List<BacnetSpecialEvent>(), period ?? Always, calendar)?.Value;
    }

    [Fact]
    public void Weekly_schedule_uses_the_latest_entry_at_or_before_now()
    {
        var weekly = Week(At(8, 22.0f), At(6, 20.0f)); // deliberately unordered

        Assert.Equal(22.0f, Compute(MondayMorning, weekly));                       // 09:00 -> 08:00 entry
        Assert.Equal(20.0f, Compute(MondayMorning.Date.AddHours(7), weekly));      // 07:00 -> 06:00 entry
        Assert.Null(Compute(MondayMorning.Date.AddHours(5), weekly));              // 05:00 -> Schedule_Default
    }

    [Fact]
    public void Null_schedule_value_relinquishes_to_the_default()
    {
        var weekly = Week(At(6, 20.0f), new BacnetTimeValue(new TimeSpan(18, 0, 0), new BacnetValue(null)));

        Assert.Equal(20.0f, Compute(MondayMorning, weekly));
        Assert.Null(Compute(MondayMorning.Date.AddHours(19), weekly));
    }

    [Fact]
    public void Array_element_matches_the_bacnet_weekday()
    {
        Assert.Equal(0, ScheduleCalculation.DayIndex(DayOfWeek.Monday));
        Assert.Equal(6, ScheduleCalculation.DayIndex(DayOfWeek.Sunday));

        var weekly = Week(At(0, 1.0f));
        Assert.Null(Compute(new DateTime(2026, 7, 5, 9, 0, 0), weekly)); // Sunday: Monday's entries don't apply
    }

    [Fact]
    public void In_effect_exception_overrides_the_weekly_schedule()
    {
        var weekly = Week(At(8, 22.0f));
        var exceptions = new List<BacnetSpecialEvent> { TodayEvent(5, At(8, 88.0f)) };

        Assert.Equal(88.0f, Compute(MondayMorning, weekly, exceptions));
    }

    [Fact]
    public void Higher_priority_exception_wins_and_equal_priorities_tie_break_on_index()
    {
        var byPriority = new List<BacnetSpecialEvent> { TodayEvent(12, At(8, 99.0f)), TodayEvent(5, At(8, 88.0f)) };
        Assert.Equal(88.0f, Compute(MondayMorning, Week(), byPriority));

        var byIndex = new List<BacnetSpecialEvent> { TodayEvent(5, At(8, 11.0f)), TodayEvent(5, At(8, 22.0f)) };
        Assert.Equal(11.0f, Compute(MondayMorning, Week(), byIndex));
    }

    [Fact]
    public void Exception_without_a_current_value_falls_through_to_the_next_candidate()
    {
        var weekly = Week(At(8, 22.0f));
        var exceptions = new List<BacnetSpecialEvent>
        {
            TodayEvent(1, At(10, 77.0f)),                                              // starts later today
            TodayEvent(2, new BacnetTimeValue(new TimeSpan(8, 0, 0), new BacnetValue(null))) // current value NULL
        };

        // neither high-priority event yields a non-NULL value at 09:00 -> weekly schedule rules
        Assert.Equal(22.0f, Compute(MondayMorning, weekly, exceptions));
    }

    [Fact]
    public void Exception_on_another_day_is_not_in_effect()
    {
        var tomorrow = new BacnetSpecialEvent(
            new BacnetCalendarEntry(new BacnetDate(MondayMorning.AddDays(1))), new[] { At(0, 66.0f) }, 1);

        Assert.Null(Compute(MondayMorning, Week(), new List<BacnetSpecialEvent> { tomorrow }));
    }

    [Fact]
    public void Calendar_reference_follows_the_referenced_present_value()
    {
        var calendarId = new BacnetObjectId(BacnetObjectTypes.OBJECT_CALENDAR, 3);
        var exceptions = new List<BacnetSpecialEvent>
        {
            new BacnetSpecialEvent(calendarId, new[] { At(0, 66.0f) }, 1)
        };

        Assert.Equal(66.0f, Compute(MondayMorning, Week(), exceptions, calendar: _ => true));
        Assert.Null(Compute(MondayMorning, Week(), exceptions, calendar: _ => false));
        Assert.Null(Compute(MondayMorning, Week(), exceptions, calendar: _ => null)); // unresolvable object
    }

    [Fact]
    public void Effective_period_gates_everything()
    {
        var weekly = Week(At(0, 22.0f));
        var lastYear = new BacnetDateRange(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        Assert.Null(Compute(MondayMorning, weekly, period: lastYear));
        Assert.Equal(22.0f, Compute(MondayMorning, weekly, period: Always));
    }

    [Fact]
    public void Next_recalculation_is_the_earliest_remaining_transition_or_midnight()
    {
        var weekly = Week(At(10, 1.0f), At(14, 2.0f));
        var none = new List<BacnetSpecialEvent>();

        Assert.Equal(MondayMorning.Date.AddHours(10),
            ScheduleCalculation.NextRecalculationTime(MondayMorning, weekly, none, null));
        Assert.Equal(MondayMorning.Date.AddHours(14),
            ScheduleCalculation.NextRecalculationTime(MondayMorning.Date.AddHours(12), weekly, none, null));
        Assert.Equal(MondayMorning.Date.AddDays(1).AddMilliseconds(500),
            ScheduleCalculation.NextRecalculationTime(MondayMorning.Date.AddHours(15), weekly, none, null));
    }

    [Fact]
    public void Next_recalculation_considers_only_in_effect_exceptions()
    {
        var inEffect = new List<BacnetSpecialEvent> { TodayEvent(1, At(11, 1.0f)) };
        var notInEffect = new List<BacnetSpecialEvent>
        {
            new BacnetSpecialEvent(new BacnetCalendarEntry(new BacnetDate(MondayMorning.AddDays(1))),
                new[] { At(11, 1.0f) }, 1)
        };

        Assert.Equal(MondayMorning.Date.AddHours(11),
            ScheduleCalculation.NextRecalculationTime(MondayMorning, Week(), inEffect, null));
        Assert.Equal(MondayMorning.Date.AddDays(1).AddMilliseconds(500),
            ScheduleCalculation.NextRecalculationTime(MondayMorning, Week(), notInEffect, null));
    }
}
