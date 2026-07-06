/**************************************************************************
*                           MIT License
*
* Copyright (C) 2015 Frederic Chaxel <fchaxel@free.fr>
*
* Permission is hereby granted, free of charge, to any person obtaining
* a copy of this software and associated documentation files (the
* "Software"), to deal in the Software without restriction, including
* without limitation the rights to use, copy, modify, merge, publish,
* distribute, sublicense, and/or sell copies of the Software, and to
* permit persons to whom the Software is furnished to do so, subject to
* the following conditions:
*
* The above copyright notice and this permission notice shall be included
* in all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*
*********************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO.BACnet;
using System.IO.BACnet.Serialize;
using System.Threading;

namespace BaCSharp
{
    // The Schedule object of ASHRAE 135-2016 Clause 12.24: Weekly_Schedule (BACnetARRAY[7] of
    // BACnetDailySchedule, Monday..Sunday), Exception_Schedule (BACnetARRAY of BACnetSpecialEvent),
    // the 12.24.4 Present_Value evaluation (see ScheduleCalculation) and dispatch of the value to
    // every List_Of_Object_Property_References member at Priority_For_Writing.
    public class Schedule : BaCSharpObject
    {
        protected int tmrId;
        protected object lockObj = new object();

        public bool m_PROP_OUT_OF_SERVICE = true; // start decoupled; enable after wiring (see Program.cs)
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN)]
        public virtual bool PROP_OUT_OF_SERVICE
        {
            get { return m_PROP_OUT_OF_SERVICE; }
            set
            {
                lock (lockObj)
                {
                    m_PROP_OUT_OF_SERVICE = value;
                    m_PROP_STATUS_FLAGS.SetBit((byte)3, value);
                }
                ExternalCOVManagement(BacnetPropertyIds.PROP_OUT_OF_SERVICE);
                Recalculate();
            }
        }

        public BacnetBitString m_PROP_STATUS_FLAGS = new BacnetBitString();
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING)]
        public virtual BacnetBitString PROP_STATUS_FLAGS
        {
            get { return m_PROP_STATUS_FLAGS; }
        }

        public List<BacnetDeviceObjectPropertyReference> m_PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES = new List<BacnetDeviceObjectPropertyReference>();

        public virtual IList<BacnetValue> get2_PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES()
        {
            lock (lockObj)
                return m_PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES.Select(r => new BacnetValue(r)).ToList();
        }

        public virtual void set2_PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES(IList<BacnetValue> values, byte priority)
        {
            if (!TryGetTypedValues(values, out List<BacnetDeviceObjectPropertyReference> references))
                return;

            lock (lockObj)
                m_PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES = references;

            Recalculate();
        }

        public object m_PROP_PRESENT_VALUE;
        public virtual object PROP_PRESENT_VALUE
        {
            get { return m_PROP_PRESENT_VALUE; }
        }

        // Present_Value is writable only while Out_Of_Service; the write still dispatches to the
        // referenced properties as if it had come from the internal calculation (12.24.14)
        public virtual void set2_PROP_PRESENT_VALUE(IList<BacnetValue> values, byte priority)
        {
            if (!m_PROP_OUT_OF_SERVICE)
            {
                ErrorCode_PropertyWrite = ErrorCodes.WriteAccessDenied;
                return;
            }

            bool changed;
            lock (lockObj)
            {
                var newValue = values.Count == 1 ? values[0].Value : null;
                changed = !Equals(m_PROP_PRESENT_VALUE, newValue);
                m_PROP_PRESENT_VALUE = newValue;
            }

            if (changed)
                DoDispatchValue(); // our own COV is raised by WritePropertyValue once this setter returns
        }

        public object m_PROP_SCHEDULE_DEFAULT;
        public virtual object PROP_SCHEDULE_DEFAULT
        {
            get { return m_PROP_SCHEDULE_DEFAULT; }
            set
            {
                m_PROP_SCHEDULE_DEFAULT = value;
                ExternalCOVManagement(BacnetPropertyIds.PROP_SCHEDULE_DEFAULT);
                Recalculate();
            }
        }

        // CONFIGURATION_ERROR when the non-NULL scheduled values and Schedule_Default do not share
        // one primitive datatype (12.24.13)
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED)]
        public virtual uint PROP_RELIABILITY
        {
            get
            {
                lock (lockObj)
                    return HasConsistentDatatypes() ? 0u : (uint)BacnetReliability.RELIABILITY_CONFIGURATION_ERROR;
            }
        }

        public uint m_PROP_PRIORITY_FOR_WRITING = 16;
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)]
        public virtual uint PROP_PRIORITY_FOR_WRITING
        {
            get { return m_PROP_PRIORITY_FOR_WRITING; }
            set
            {
                if (value >= 1 && value <= 16)
                    m_PROP_PRIORITY_FOR_WRITING = value;
                else
                    ErrorCode_PropertyWrite = ErrorCodes.OutOfRange;
            }
        }

        public BacnetDateRange m_PROP_EFFECTIVE_PERIOD = AlwaysInEffect();

        public virtual IList<BacnetValue> get2_PROP_EFFECTIVE_PERIOD()
        {
            lock (lockObj)
                return new[] { new BacnetValue(m_PROP_EFFECTIVE_PERIOD) };
        }

        public virtual void set2_PROP_EFFECTIVE_PERIOD(IList<BacnetValue> values, byte priority)
        {
            BacnetDateRange range;
            if (values.Count == 1 && values[0].Value is BacnetDateRange r)
            {
                range = r;
            }
            else if (values.Count == 2 && values[0].Value is DateTime start && values[1].Value is DateTime end)
            {
                // the pre-4.0 two-date shape, still used by in-process callers (network writes
                // arrive as one decoded BacnetDateRange); the DateTime(1,1,1) sentinel stands
                // for a fully wildcarded (open) boundary
                range = new BacnetDateRange(BacnetDate.FromDateTime(start), BacnetDate.FromDateTime(end));
            }
            else
            {
                ErrorCode_PropertyWrite = ErrorCodes.InvalidDataType;
                return;
            }

            lock (lockObj)
                m_PROP_EFFECTIVE_PERIOD = range;

            Recalculate();
        }

        public BacnetDailySchedule[] m_PROP_WEEKLY_SCHEDULE = NewEmptyWeek();

        public virtual IList<BacnetValue> get2_PROP_WEEKLY_SCHEDULE()
        {
            lock (lockObj)
                return m_PROP_WEEKLY_SCHEDULE.Select(day => new BacnetValue(day)).ToList();
        }

        public virtual void set2_PROP_WEEKLY_SCHEDULE(IList<BacnetValue> values, byte priority)
        {
            var index = ArrayIndex_PropertyWrite;
            if (index == 0)
            {
                // a BACnetARRAY[7] has a fixed size
                ErrorCode_PropertyWrite = ErrorCodes.WriteAccessDenied;
                return;
            }
            if (index != ASN1.BACNET_ARRAY_ALL && (index < 1 || index > 7))
            {
                ErrorCode_PropertyWrite = ErrorCodes.OutOfRange;
                return;
            }

            if (!TryGetTypedValues(values, out List<BacnetDailySchedule> days))
                return;

            if (days.Count != (index == ASN1.BACNET_ARRAY_ALL ? 7 : 1))
            {
                ErrorCode_PropertyWrite = ErrorCodes.OutOfRange;
                return;
            }

            if (days.Any(day => HasDuplicateTime(day.DaySchedule)))
            {
                ErrorCode_PropertyWrite = ErrorCodes.DuplicateEntry;
                return;
            }

            lock (lockObj)
            {
                if (index == ASN1.BACNET_ARRAY_ALL)
                    m_PROP_WEEKLY_SCHEDULE = days.ToArray();
                else
                    m_PROP_WEEKLY_SCHEDULE[index - 1] = days[0];
            }

            Recalculate();
        }

        public List<BacnetSpecialEvent> m_PROP_EXCEPTION_SCHEDULE = new List<BacnetSpecialEvent>();

        public virtual IList<BacnetValue> get2_PROP_EXCEPTION_SCHEDULE()
        {
            lock (lockObj)
                return m_PROP_EXCEPTION_SCHEDULE.Select(specialEvent => new BacnetValue(specialEvent)).ToList();
        }

        public virtual void set2_PROP_EXCEPTION_SCHEDULE(IList<BacnetValue> values, byte priority)
        {
            var index = ArrayIndex_PropertyWrite;
            if (index == 0)
            {
                Resize_PROP_EXCEPTION_SCHEDULE(values);
                return;
            }

            if (!TryGetTypedValues(values, out List<BacnetSpecialEvent> events))
                return;

            if (events.Any(specialEvent => HasDuplicateTime(specialEvent.ListOfTimeValues)))
            {
                ErrorCode_PropertyWrite = ErrorCodes.DuplicateEntry;
                return;
            }

            lock (lockObj)
            {
                if (index == ASN1.BACNET_ARRAY_ALL)
                {
                    m_PROP_EXCEPTION_SCHEDULE = events;
                }
                else if (events.Count == 1 && index >= 1 && index <= m_PROP_EXCEPTION_SCHEDULE.Count)
                {
                    m_PROP_EXCEPTION_SCHEDULE[(int)index - 1] = events[0];
                }
                else
                {
                    ErrorCode_PropertyWrite = ErrorCodes.OutOfRange;
                    return;
                }
            }

            Recalculate();
        }

        // writing array index 0 resizes the array; new elements carry an empty list of time
        // values (12.24.8) under an always-matching wildcard date - harmless, since an event
        // with no time-value at or before now never contributes a value
        private void Resize_PROP_EXCEPTION_SCHEDULE(IList<BacnetValue> values)
        {
            if (values.Count != 1 || !(values[0].Value is uint newSize))
            {
                ErrorCode_PropertyWrite = ErrorCodes.InvalidDataType;
                return;
            }

            lock (lockObj)
            {
                while (m_PROP_EXCEPTION_SCHEDULE.Count > newSize)
                    m_PROP_EXCEPTION_SCHEDULE.RemoveAt(m_PROP_EXCEPTION_SCHEDULE.Count - 1);
                while (m_PROP_EXCEPTION_SCHEDULE.Count < newSize)
                    m_PROP_EXCEPTION_SCHEDULE.Add(new BacnetSpecialEvent(
                        new BacnetCalendarEntry(BacnetDate.Any), new BacnetTimeValue[0], 16));
            }

            Recalculate();
        }

        public Schedule(int ObjId, String ObjName, String Description)
            : base(new BacnetObjectId(BacnetObjectTypes.OBJECT_SCHEDULE, (uint)ObjId), ObjName, Description)
        {
            m_PROP_STATUS_FLAGS.SetBit((byte)0, false);
            m_PROP_STATUS_FLAGS.SetBit((byte)1, false);
            m_PROP_STATUS_FLAGS.SetBit((byte)2, false);
            m_PROP_STATUS_FLAGS.SetBit((byte)3, m_PROP_OUT_OF_SERVICE);

            // exception schedules can be driven by Calendar objects: follow their changes
            OnExternalCOVNotify += OnDeviceObjectChanged;
        }

        public override void Post_NewtonSoft_Json_Deserialization(DeviceObject device)
        {
            base.Post_NewtonSoft_Json_Deserialization(device);

            // Json.NET restores integers as Int64/UInt64: narrow them back
            foreach (var day in m_PROP_WEEKLY_SCHEDULE)
                NarrowJsonIntegers(day.DaySchedule);
            foreach (var specialEvent in m_PROP_EXCEPTION_SCHEDULE)
                NarrowJsonIntegers(specialEvent.ListOfTimeValues);
            m_PROP_PRESENT_VALUE = NarrowJsonInteger(m_PROP_PRESENT_VALUE);
            m_PROP_SCHEDULE_DEFAULT = NarrowJsonInteger(m_PROP_SCHEDULE_DEFAULT);

            Recalculate();
        }

        public void AddSchedule(int day, DateTime time, object value)
        {
            lock (lockObj)
                m_PROP_WEEKLY_SCHEDULE[day].DaySchedule.Add(new BacnetTimeValue(time.TimeOfDay, new BacnetValue(value)));

            Recalculate();
        }

        public void AddSpecialEvent(BacnetSpecialEvent specialEvent)
        {
            lock (lockObj)
                m_PROP_EXCEPTION_SCHEDULE.Add(specialEvent);

            Recalculate();
        }

        public void AddPropertyReference(BacnetDeviceObjectPropertyReference reference)
        {
            lock (lockObj)
                m_PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES.Add(reference);
        }

        private void OnDeviceObjectChanged(BaCSharpObject sender, BacnetPropertyIds propId)
        {
            if (!(sender is Calendar))
                return;
            if (propId != BacnetPropertyIds.PROP_PRESENT_VALUE && propId != BacnetPropertyIds.PROP_DATE_LIST)
                return;

            Recalculate();
        }

        private bool? ResolveCalendarPresentValue(BacnetObjectId calendarId)
        {
            var calendar = Mydevice?.FindBacnetObject(calendarId) as Calendar;
            return calendar?.m_PROP_PRESENT_VALUE;
        }

        // Evaluate 12.24.4 now, dispatch a changed Present_Value, and arm the timer for the next
        // transition (or just past midnight). Called on every affecting write, on Out_Of_Service
        // changes, on Calendar changes and when the timer fires.
        public void Recalculate()
        {
            var changed = false;
            lock (lockObj)
            {
                tmrId++; // invalidate a pending timer callback; we arm a fresh one below

                if (Mydevice == null)
                    return;

                m_PROP_STATUS_FLAGS.SetBit((byte)1, !HasConsistentDatatypes()); // FAULT

                if (m_PROP_OUT_OF_SERVICE)
                    return;

                var now = DateTime.Now;
                var computed = ScheduleCalculation.ComputePresentValue(now, m_PROP_WEEKLY_SCHEDULE,
                    m_PROP_EXCEPTION_SCHEDULE, m_PROP_EFFECTIVE_PERIOD, ResolveCalendarPresentValue);
                var newValue = computed.HasValue ? computed.Value.Value : m_PROP_SCHEDULE_DEFAULT;

                if (!Equals(m_PROP_PRESENT_VALUE, newValue))
                {
                    m_PROP_PRESENT_VALUE = newValue;
                    changed = true;
                }

                var next = ScheduleCalculation.NextRecalculationTime(now, m_PROP_WEEKLY_SCHEDULE,
                    m_PROP_EXCEPTION_SCHEDULE, ResolveCalendarPresentValue);
                var delay = next - now;
                if (delay < TimeSpan.FromMilliseconds(100))
                    delay = TimeSpan.FromMilliseconds(100);

                tmr = new Timer(TimerFired, tmrId, delay, Timeout.InfiniteTimeSpan);
            }

            if (changed)
            {
                DoDispatchValue();
                ExternalCOVManagement(BacnetPropertyIds.PROP_PRESENT_VALUE);
            }
        }

        Timer tmr;
        private void TimerFired(object state)
        {
            lock (lockObj)
            {
                if ((int)state != tmrId)
                    return;
            }

            Recalculate();
        }

        // Copy the Present_Value into each referenced property, at Priority_For_Writing
        protected virtual void DoDispatchValue()
        {
            if (Mydevice == null)
                return;

            List<BacnetDeviceObjectPropertyReference> references;
            object presentValue;
            uint priority;
            lock (lockObj)
            {
                references = new List<BacnetDeviceObjectPropertyReference>(m_PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES);
                presentValue = m_PROP_PRESENT_VALUE;
                priority = m_PROP_PRIORITY_FOR_WRITING;
            }

            foreach (var reference in references)
            {
                // an error (or missing target) on one member must not stop the others (12.24.4)
                if (reference.deviceIdentifier.type != BacnetObjectTypes.OBJECT_DEVICE) // local object
                {
                    BaCSharpObject bcs = Mydevice.FindBacnetObject(reference.objectIdentifier);
                    if (bcs == null)
                        continue;

                    var value = new BacnetPropertyValue
                    {
                        priority = (byte)priority,
                        property = new BacnetPropertyReference((uint)reference.propertyIdentifier, reference.arrayIndex),
                        value = new BacnetValue[] { new BacnetValue(presentValue) }
                    };
                    bcs.WritePropertyValue(value, false);
                }
                else
                {
                    KeyValuePair<BacnetClient, BacnetAddress>? recipient = null;
                    try
                    {
                        // SuroundingDevices is updated with Iam messages
                        recipient = Mydevice.SuroundingDevices[reference.deviceIdentifier.instance];
                    }
                    catch { }
                    if (recipient == null)
                        continue;

                    BacnetValue[] value = { new BacnetValue(presentValue) };
                    ThreadPool.QueueUserWorkItem(o =>
                        {
                            recipient.Value.Key.WritePriority = priority;
                            recipient.Value.Key.BeginWritePropertyRequest(recipient.Value.Value, reference.objectIdentifier, (BacnetPropertyIds)reference.propertyIdentifier, value, false);
                        }
                        , null);
                }
            }
        }

        public override void Dispose()
        {
            OnExternalCOVNotify -= OnDeviceObjectChanged;
            lock (lockObj)
                tmrId++; // invalidate any pending timer callback; the object is going away
        }

        private bool HasConsistentDatatypes()
        {
            Type reference = m_PROP_SCHEDULE_DEFAULT?.GetType();

            foreach (var timeValue in m_PROP_WEEKLY_SCHEDULE.SelectMany(day => day.DaySchedule)
                .Concat(m_PROP_EXCEPTION_SCHEDULE.SelectMany(specialEvent => specialEvent.ListOfTimeValues)))
            {
                var type = timeValue.Value.Value?.GetType();
                if (type == null)
                    continue; // NULL values are always allowed

                if (reference == null)
                    reference = type;
                else if (type != reference)
                    return false;
            }

            return true;
        }

        private static bool HasDuplicateTime(List<BacnetTimeValue> timeValues)
        {
            return timeValues.GroupBy(timeValue => timeValue.Time).Any(group => group.Count() > 1);
        }

        private static BacnetDailySchedule[] NewEmptyWeek()
        {
            var week = new BacnetDailySchedule[7];
            for (var i = 0; i < 7; i++)
                week[i] = new BacnetDailySchedule();
            return week;
        }

        private static BacnetDateRange AlwaysInEffect()
        {
            return new BacnetDateRange(BacnetDate.Any, BacnetDate.Any);
        }

        private static void NarrowJsonIntegers(List<BacnetTimeValue> timeValues)
        {
            for (var i = 0; i < timeValues.Count; i++)
            {
                var timeValue = timeValues[i];
                var narrowed = NarrowJsonInteger(timeValue.Value.Value);
                if (!ReferenceEquals(narrowed, timeValue.Value.Value))
                    timeValues[i] = new BacnetTimeValue(timeValue.Time, new BacnetValue(timeValue.Value.Tag, narrowed));
            }
        }

        private static object NarrowJsonInteger(object value)
        {
            if (value is Int64 signed)
                return Convert.ToInt32(signed);
            if (value is UInt64 unsigned)
                return Convert.ToUInt32(unsigned);
            return value;
        }
    }
}
