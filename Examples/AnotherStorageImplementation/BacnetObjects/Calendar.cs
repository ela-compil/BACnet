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
using System.Text;
using System.IO.BACnet;
using System.Threading;

namespace BaCSharp
{
    class Calendar: BaCSharpObject
    { 

        public bool m_PROP_PRESENT_VALUE=false;
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN)]
        public virtual bool PROP_PRESENT_VALUE
        {
            get { return m_PROP_PRESENT_VALUE; }
        }

        public List<BacnetCalendarEntry> DateList = new List<BacnetCalendarEntry>();

        public virtual IList<BacnetValue> get2_PROP_DATE_LIST()
        {
            lock (LockObj)
                return DateList.Select(entry => new BacnetValue(entry)).ToList();
        }

        public virtual void set2_PROP_DATE_LIST(IList<BacnetValue> values, byte priority)
        {
            var newList = new List<BacnetCalendarEntry>();
            foreach (BacnetValue value in values)
            {
                if (value.Value is BacnetCalendarEntry entry)
                {
                    newList.Add(entry);
                }
                else
                {
                    ErrorCode_PropertyWrite = ErrorCodes.OutOfRange;
                    return;
                }
            }

            lock (LockObj)
                DateList = newList;

            DayChanged(tmrId);
        }

        private int tmrId = 0;
        private object LockObj = new object();

        public Calendar(int ObjId, String ObjName, String Description)
             : base(new BacnetObjectId(BacnetObjectTypes.OBJECT_CALENDAR,(uint)ObjId), ObjName, Description)
        {
            DayChanged(tmrId);
        }

        public override void Post_NewtonSoft_Json_Deserialization(DeviceObject device)
        {
            base.Post_NewtonSoft_Json_Deserialization(device);
            DayChanged(tmrId);
        }

        public override void Dispose()
        {
            lock (LockObj)
                tmrId++; // it is used to 'desactivate the effect' of the timer call sleeping in the ThreadPool
        }

        // Simple methods to add .NET dates, ranges and week-n-day patterns
        public void AddDate(DateTime date)
        {
            AddEntry(new BacnetCalendarEntry(new BacnetDate(date)));
        }

        public void AddRange(DateTime start, DateTime end)
        {
            AddEntry(new BacnetCalendarEntry(new BacnetDateRange(start, end)));
        }

        public void AddEntry(BacnetDate bd)
        {
            AddEntry(new BacnetCalendarEntry(bd));
        }
        public void AddEntry(BacnetDateRange bdr)
        {
            AddEntry(new BacnetCalendarEntry(bdr));
        }
        public void AddEntry(BacnetWeekNDay bwd)
        {
            AddEntry(new BacnetCalendarEntry(bwd));
        }
        public void AddEntry(BacnetCalendarEntry entry)
        {
            lock (LockObj)
                DateList.Add(entry);

            DayChanged(tmrId);
        }

        Timer tmr;
        protected virtual void DayChanged(object o)
        {
            lock (LockObj)
            {
                if ((int)o != tmrId)
                    return;

                tmrId++; // it is used to 'desactivate the effect' of the timer call sleeping in the ThreadPool 

                // Update Present_Value
                bool NewPresentValue = DateList.Any(entry => entry.IsAFittingDate(DateTime.Now));

                if (NewPresentValue != m_PROP_PRESENT_VALUE)
                {
                    m_PROP_PRESENT_VALUE = NewPresentValue;
                    ExternalCOVManagement(BacnetPropertyIds.PROP_PRESENT_VALUE);
                }

                // Place a callback for tomorrow 0:0:1
                DateTime Now = DateTime.Now;
                DateTime Tomorrow = new DateTime(Now.Year, Now.Month, Now.Day, 0, 0, 1).AddDays(1);
                tmr = new Timer(new TimerCallback(DayChanged), tmrId, (long)((Tomorrow - Now).TotalMilliseconds), Timeout.Infinite);
            }
        }
    }
}
