using System;
using System.Collections.Generic;
using System.Linq;
using System.IO.BACnet;

namespace BaCSharp
{
    /// <summary>
    /// The Present_Value calculation of the Schedule object (ASHRAE 135-2016 Clause 12.24.4), kept
    /// free of device and timer state so it can be unit tested: the highest-priority special event
    /// in effect whose current value is non-NULL wins, then the weekly schedule of the current day,
    /// then null (the caller substitutes Schedule_Default).
    /// </summary>
    public static class ScheduleCalculation
    {
        /// <param name="now">The moment to evaluate the schedule for.</param>
        /// <param name="weeklySchedule">The seven Weekly_Schedule days, Monday first.</param>
        /// <param name="exceptionSchedule">The Exception_Schedule events, in array order.</param>
        /// <param name="effectivePeriod">The date range gating the whole object.</param>
        /// <param name="calendarPresentValue">Resolves a calendar-reference period to the referenced
        /// Calendar object's Present_Value; null when the object cannot be found.</param>
        public static BacnetValue? ComputePresentValue(
            DateTime now,
            BacnetDailySchedule[] weeklySchedule,
            IReadOnlyList<BacnetSpecialEvent> exceptionSchedule,
            BacnetDateRange effectivePeriod,
            Func<BacnetObjectId, bool?> calendarPresentValue)
        {
            if (!effectivePeriod.IsAFittingDate(now))
                return null;

            foreach (var specialEvent in ByDescendingImportance(exceptionSchedule))
            {
                if (!IsInEffect(specialEvent, now, calendarPresentValue))
                    continue;

                var current = CurrentValue(specialEvent.ListOfTimeValues, now.TimeOfDay);
                if (IsNonNull(current))
                    return current;
            }

            var today = weeklySchedule != null ? weeklySchedule[DayIndex(now.DayOfWeek)] : null;
            if (today != null)
            {
                var weeklyValue = CurrentValue(today.DaySchedule, now.TimeOfDay);
                if (IsNonNull(weeklyValue))
                    return weeklyValue;
            }

            return null;
        }

        /// <summary>
        /// The next moment the Present_Value may change: the earliest remaining transition today in
        /// the weekly schedule or any in-effect special event, capped at just past midnight (which
        /// also covers day, calendar and Effective_Period changes).
        /// </summary>
        public static DateTime NextRecalculationTime(
            DateTime now,
            BacnetDailySchedule[] weeklySchedule,
            IReadOnlyList<BacnetSpecialEvent> exceptionSchedule,
            Func<BacnetObjectId, bool?> calendarPresentValue)
        {
            var next = now.Date.AddDays(1).AddMilliseconds(500);

            var today = weeklySchedule != null ? weeklySchedule[DayIndex(now.DayOfWeek)] : null;
            if (today != null)
                next = EarliestTransition(today.DaySchedule, now, next);

            if (exceptionSchedule != null)
                foreach (var specialEvent in exceptionSchedule)
                    if (IsInEffect(specialEvent, now, calendarPresentValue))
                        next = EarliestTransition(specialEvent.ListOfTimeValues, now, next);

            return next;
        }

        /// <summary>Monday = 0 .. Sunday = 6, matching the Weekly_Schedule array order.</summary>
        public static int DayIndex(DayOfWeek dayOfWeek)
        {
            return ((int)dayOfWeek + 6) % 7;
        }

        private static IEnumerable<BacnetSpecialEvent> ByDescendingImportance(IReadOnlyList<BacnetSpecialEvent> events)
        {
            if (events == null)
                return Enumerable.Empty<BacnetSpecialEvent>();

            // EventPriority 1 is the most important; equal priorities tie-break on the array
            // index (Clause 12.24.8), which OrderBy - a guaranteed-stable sort, unlike
            // List<T>.Sort - preserves from the source order
            return events.OrderBy(specialEvent => specialEvent.EventPriority);
        }

        private static bool IsInEffect(BacnetSpecialEvent specialEvent, DateTime now, Func<BacnetObjectId, bool?> calendarPresentValue)
        {
            if (specialEvent.CalendarEntry != null)
                return specialEvent.CalendarEntry.IsAFittingDate(now);

            if (specialEvent.CalendarReference != null)
                return calendarPresentValue?.Invoke(specialEvent.CalendarReference.Value) == true;

            return false;
        }

        // the latest time-value at or before the given time of day; the list may be unordered
        private static BacnetValue? CurrentValue(List<BacnetTimeValue> timeValues, TimeSpan timeOfDay)
        {
            if (timeValues == null)
                return null;

            BacnetValue? current = null;
            var currentTime = TimeSpan.MinValue;
            foreach (var timeValue in timeValues)
            {
                if (timeValue.Time <= timeOfDay && timeValue.Time >= currentTime)
                {
                    currentTime = timeValue.Time;
                    current = timeValue.Value;
                }
            }

            return current;
        }

        private static bool IsNonNull(BacnetValue? value)
        {
            return value != null && value.Value.Tag != BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL;
        }

        private static DateTime EarliestTransition(List<BacnetTimeValue> timeValues, DateTime now, DateTime upperBound)
        {
            if (timeValues == null)
                return upperBound;

            foreach (var timeValue in timeValues)
            {
                var moment = now.Date.Add(timeValue.Time);
                if (moment > now && moment < upperBound)
                    upperBound = moment;
            }

            return upperBound;
        }
    }
}
