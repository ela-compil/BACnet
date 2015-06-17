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
using System.IO.BACnet.Serialize;
using System.Threading;
//
// List of object property identifier
//  Object Identifier
//  Property Identifier
//  optional array index : uint
//  optional device identifier BacnetDeviceObjectPropertyReference, classe existante ASN1encode à faire : IASN1encode
namespace BaCSharp
{
    [Serializable]
    class Schedule : BaCSharpObject
    {
        protected int tmrId;
        protected object lockObj=new object();

        public bool m_PROP_OUT_OF_SERVICE = true; // No need to lock, to much simple type
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN)]
        public virtual bool PROP_OUT_OF_SERVICE
        {
            get { return m_PROP_OUT_OF_SERVICE; }
            set
            {  
                m_PROP_OUT_OF_SERVICE = value;
                InternalCOVManagement(BacnetPropertyIds.PROP_OUT_OF_SERVICE);
                DoScheduling();
            }
        }

        BacnetBitString m_PROP_STATUS_FLAGS = new BacnetBitString();
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING)]
        public virtual BacnetBitString PROP_STATUS_FLAGS
        {
            get { return m_PROP_STATUS_FLAGS; }
        }

        /*
        BacnetDeviceObjectPropertyReferenceList m_PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES;
        public virtual BacnetDeviceObjectPropertyReferenceList PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES
        {
            get { return m_PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES; }
        }
        */
        List<object> m_PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES;
        public virtual List<object> PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES
        {
            get 
            {
                lock (lockObj)
                {
                    return m_PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES;
                }
            }
        }

        public object m_PROP_PRESENT_VALUE; // No need to lock, object reference, invariant
        public bool IsPresentValueDefault = true; 
        public virtual object PROP_PRESENT_VALUE
        {
            get { return m_PROP_PRESENT_VALUE; } 
        }

        public virtual object internal_PROP_PRESENT_VALUE
        {
            get { return m_PROP_PRESENT_VALUE; }
            set
            {
                m_PROP_PRESENT_VALUE = value;
                InternalCOVManagement(BacnetPropertyIds.PROP_PRESENT_VALUE);
                DoDispatchValue();
            }
        }

        public object m_PROP_SCHEDULE_DEFAULT;  // No need to lock, object reference, invariant
        public virtual object PROP_SCHEDULE_DEFAULT
        {
            get { return m_PROP_SCHEDULE_DEFAULT; }
            set
            {
                m_PROP_SCHEDULE_DEFAULT = value;
                InternalCOVManagement(BacnetPropertyIds.PROP_SCHEDULE_DEFAULT);

                if (IsPresentValueDefault==true)
                {
                    internal_PROP_PRESENT_VALUE = m_PROP_SCHEDULE_DEFAULT;
                }
            }
        }
        
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED)]
        public virtual uint PROP_RELIABILITY
        {
            get { return 0; }
        }

        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)]
        public uint m_PROP_PRIORITY = 0;
        public virtual uint PROP_PRIORITY
        {
            get { return m_PROP_PRIORITY; }
            set 
            {
                if (value <= 16)
                    m_PROP_PRIORITY = value;
                else 
                    ErrorCode_PropertyWrite = ErrorCodes.OutOfRange;
            }
        }
        
        List<BacnetValue> m_PROP_EFFECTIVE_PERIOD = new List<BacnetValue>();
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE)]
        public virtual List<BacnetValue> PROP_EFFECTIVE_PERIOD
        {
            get { return m_PROP_EFFECTIVE_PERIOD; }
            set 
            {
                lock (lockObj)
                {
                    m_PROP_EFFECTIVE_PERIOD = value;
                    DoScheduling();
                }
            }
        }
        
        public BacnetWeeklySchedule m_PROP_WEEKLY_SCHEDULE = new BacnetWeeklySchedule();
        public virtual object PROP_WEEKLY_SCHEDULE
        {
            get { return m_PROP_WEEKLY_SCHEDULE; }
            set 
            {
                lock (lockObj)
                {
                    int day = 0;

                    m_PROP_WEEKLY_SCHEDULE = new BacnetWeeklySchedule();

                    // A quite strange decoding due to the strange re-ordering of
                    // empty value (day without schedule) by the stack
                    List<BacnetValue> valuesList = (List<BacnetValue>)value;
                    foreach (BacnetValue bv in valuesList)
                    {
                        BacnetValue[] bvarray = (BacnetValue[])bv.Value;

                        for (int i = 0; i < bvarray.Length; i++)
                        {
                            if (bvarray[i].Value is BacnetValue[])
                            {
                                day++;
                            }
                            else
                            {
                                if (m_PROP_WEEKLY_SCHEDULE.days[day] == null)
                                    m_PROP_WEEKLY_SCHEDULE.days[day] = new List<DaySchedule>();

                                DateTime time = (DateTime)bvarray[i].Value;
                                i++;
                                DaySchedule ds = new DaySchedule(time, bvarray[i].Value);
                                m_PROP_WEEKLY_SCHEDULE.days[day].Add(ds);
                            }
                        }
                        day++;
                    }
                    DoScheduling();
                }
            }
        }

        public Schedule(int ObjId, String ObjName, String Description)
            : base(new BacnetObjectId(BacnetObjectTypes.OBJECT_SCHEDULE, (uint)ObjId), ObjName, Description)
        {
            m_PROP_STATUS_FLAGS.SetBit((byte)0, false);
            m_PROP_STATUS_FLAGS.SetBit((byte)1, false);
            m_PROP_STATUS_FLAGS.SetBit((byte)2, false);
            m_PROP_STATUS_FLAGS.SetBit((byte)3, false);

            m_PROP_EFFECTIVE_PERIOD.Add(new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE, DateTime.Now));
            m_PROP_EFFECTIVE_PERIOD.Add(new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE, DateTime.Now.AddYears(10)));
        }

        public void AddSchedule(int day, DateTime time, object value)
        {
            lock (lockObj)
            {
                if (m_PROP_WEEKLY_SCHEDULE.days[day] == null)
                    m_PROP_WEEKLY_SCHEDULE.days[day] = new List<DaySchedule>();

                m_PROP_WEEKLY_SCHEDULE.days[day].Add(new DaySchedule(time, value));

                DoScheduling();
            }
        }

        public void AddPropertyReference(BacnetDeviceObjectPropertyReference reference)
        {
            lock (lockObj)
            {
                if (m_PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES == null)
                    m_PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES = new List<object>();
                m_PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES.Add(reference);
            }
        }

        // timer Handler : it's time to change something
        protected virtual void TimeSchedule(Object state)
        {
            lock (lockObj)
            {
                if ((int)((object[])state)[0] != tmrId)    // an old timer, since we cannot stop a launched timer
                    return;
                else
                {
                    internal_PROP_PRESENT_VALUE = ((object[])state)[1];
                    IsPresentValueDefault = false;
                    DoScheduling();
                }
            }
        }

        // Copy the Present value into each reference properties value
        protected virtual void DoDispatchValue()
        {
            if (m_PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES == null)
                return;

            foreach (object obj in m_PROP_LIST_OF_OBJECT_PROPERTY_REFERENCES)
            {
                BacnetDeviceObjectPropertyReference reference = (BacnetDeviceObjectPropertyReference)obj;

                if (reference.deviceIndentifier.type != BacnetObjectTypes.OBJECT_DEVICE) // local object
                {
                    BaCSharpObject bcs= Mydevice.FindBacnetObject(reference.objectIdentifier);
                    if (bcs != null)
                    {
                        BacnetPropertyValue value = new BacnetPropertyValue();
                        
                        if (m_PROP_PRIORITY==0)
                            value.priority = (byte)16;
                        else
                            value.priority = (byte)m_PROP_PRIORITY;

                        value.property = new BacnetPropertyReference(reference.propertyIdentifier, reference.arrayIndex);
                        value.value = new BacnetValue[] { new BacnetValue(m_PROP_PRESENT_VALUE) };
                        bcs.WritePropertyValue(value, false);
                    }
                }
                else
                {
                    KeyValuePair<BacnetClient, BacnetAddress>? recipient = null;

                    try
                    {
                        recipient = Mydevice.SuroundingDevices[reference.deviceIndentifier.instance];
                    }
                    catch { }
                    if (recipient == null)
                        return;

                    BacnetValue[] value = new BacnetValue[] { new BacnetValue(m_PROP_PRESENT_VALUE) };
                    uint wp = m_PROP_PRIORITY;
                    System.Threading.ThreadPool.QueueUserWorkItem((o) =>
                        {
                            recipient.Value.Key.WritePriority = wp;
                            recipient.Value.Key.BeginWritePropertyRequest(recipient.Value.Value, reference.objectIdentifier, (BacnetPropertyIds)reference.propertyIdentifier, value, false);
                        }
                        , null);
                }
            }
        }
        //
        // Add a Thread pool Timer for the next event
        //
        protected virtual void DoScheduling()
        {
            lock (lockObj)
            {
                if (m_PROP_OUT_OF_SERVICE == true)
                {
                    tmrId++;    // in order to invalidate the action of the timer in the ThreadPool
                    return;
                }

                DateTime Now = DateTime.Now; // Add 1 second to avoid mutiple call 

                // test Validity Date and return if out
                //
                // dt.Ticks = 0  is the way always date (encoded FF-FF-FF-FF) is put into a DateTime struct
                DateTime dt = (DateTime)m_PROP_EFFECTIVE_PERIOD[0].Value;
                if (((Now - dt).TotalSeconds < 0) && (dt.Ticks != 0))
                    return;

                dt = (DateTime)m_PROP_EFFECTIVE_PERIOD[1].Value;
                if (((Now - dt).TotalSeconds > 0) && (dt.Ticks != 0))
                    return;

                int DayOfWeek = Now.DayOfWeek == 0 ? 6 : (int)Now.DayOfWeek - 1; // Put Sunday at the end of the enumaration

                int timeShift = 0;
                int delay = Int32.MaxValue;
                object NewValue = null;
                // Values are not ordered so we need to check quite all the day
                // in order to found the next date after now
                for (int i = 0; i < 7; i++)
                {

                    if (m_PROP_WEEKLY_SCHEDULE.days[(i + DayOfWeek) % 7] != null)
                        foreach (DaySchedule schedule in m_PROP_WEEKLY_SCHEDULE.days[(i + DayOfWeek) % 7])
                        {
                            DateTime eventdate = new DateTime(Now.Year, Now.Month, Now.Day, schedule.dt.Hour, schedule.dt.Minute, schedule.dt.Second);
                            int interval = (int)(eventdate - Now).TotalSeconds + timeShift; // could be negative

                            if (                                
                                ((interval < delay) && (interval >= 0)) ||
                                ((interval < 0) && (delay == Int32.MaxValue)) ||
                                ((interval > delay) && (delay < 0))
                                )
                            {
                                if (interval == 0) interval += 7 * 24 * 60 * 60; // Add one week
                                delay = interval;
                                NewValue = schedule.Value;
                            }
                        }

                    if ((NewValue != null) && (delay > 0))
                        break;      // No need to go next day

                    timeShift = timeShift + 24 * 60 * 60;   // a day after
                }

                if (delay < 0) delay = delay + 7 * 24 * 60 * 60; // Add one week

                // Put the next TimerEvent in the ThreadPool (Threading.Timer)
                if ((NewValue != null)&&(delay!=Int32.MaxValue))
                {
                    tmrId++;
                    Timer tmr = new Timer(new TimerCallback(TimeSchedule), new object[] { tmrId, NewValue }, delay * 1000, Timeout.Infinite);
                }
            }

        }
    }

    [Serializable]
    class DaySchedule
    {
        public DateTime dt;
        public object Value;

        public DaySchedule(DateTime dt, object Value)
        {
            this.dt = dt;
            this.Value = Value;
        }
    }

    [Serializable]
    class BacnetWeeklySchedule : ASN1.IASN1encode
    {
        public List<DaySchedule>[] days = new List<DaySchedule>[7];

        public void ASN1encode(EncodeBuffer buffer)
        {
            for (int i = 0; i < 7; i++)
            {
                ASN1.encode_opening_tag(buffer, 0);
                if (days[i] != null)
                {
                    List<DaySchedule> dsl = days[i];
                        foreach (DaySchedule ds in dsl)
                        {
                            ASN1.bacapp_encode_application_data(buffer, new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME, ds.dt));
                            ASN1.bacapp_encode_application_data(buffer, new BacnetValue(ds.Value));

                        }
                }
                ASN1.encode_closing_tag(buffer, 0);
            }
        }
    }
}
