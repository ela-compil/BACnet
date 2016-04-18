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

namespace BaCSharp
{
    //INTRINSIC_REPORTING part on AnalogObject
    abstract partial class AnalogObject<T> : BaCSharpObject
    {        
        public uint m_PROP_NOTIFICATION_CLASS;
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)]
        public virtual uint PROP_NOTIFICATION_CLASS
        {
            get { return m_PROP_NOTIFICATION_CLASS; }
            set { m_PROP_NOTIFICATION_CLASS = value; }
        }

        public T m_PROP_HIGH_LIMIT;
        public virtual T PROP_HIGH_LIMIT
        {
            get { return m_PROP_HIGH_LIMIT; }
            set 
            {
                double hl = Convert.ToDouble(value);
                double ll = Convert.ToDouble(m_PROP_LOW_LIMIT);

                if (hl>=ll)
                    m_PROP_HIGH_LIMIT = value; 
            }
        }

        public T m_PROP_LOW_LIMIT;
        public virtual T PROP_LOW_LIMIT
        {
            get { return m_PROP_LOW_LIMIT; }
            set
            {
                double ll = Convert.ToDouble(value);
                double hl = Convert.ToDouble(m_PROP_HIGH_LIMIT);
                if (ll >= hl)
                    m_PROP_LOW_LIMIT = value;
            }
        }

        public T m_PROP_DEADBAND;
        public virtual T PROP_DEADBAND
        {
            get { return m_PROP_DEADBAND; }
            set { m_PROP_DEADBAND = value; }
        }
        
        public BacnetBitString m_PROP_LIMIT_ENABLE= new BacnetBitString();
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING)]
        public virtual BacnetBitString PROP_LIMIT_ENABLE
        {
            get { return m_PROP_LIMIT_ENABLE; }
            set { m_PROP_LIMIT_ENABLE = value; }
        }

        public BacnetBitString m_PROP_EVENT_ENABLE = new BacnetBitString();
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING)]
        public virtual BacnetBitString PROP_EVENT_ENABLE
        {
            get { return m_PROP_EVENT_ENABLE; }
            set 
            {
                if (value.bits_used == 3)
                    m_PROP_EVENT_ENABLE = value; 
            }
        }

        public BacnetBitString m_PROP_ACKED_TRANSITIONS = new BacnetBitString();
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING)]
        public virtual BacnetBitString PROP_ACKED_TRANSITIONS
        {
            get { return m_PROP_ACKED_TRANSITIONS; }
            set 
            { 
                if (value.bits_used==3)
                    m_PROP_ACKED_TRANSITIONS = value; 
            }
        }

        public BacnetValue[] m_PROP_EVENT_TIME_STAMPS = new BacnetValue[3];
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL)]
        public virtual BacnetValue[] PROP_EVENT_TIME_STAMPS
        {
            get { return m_PROP_EVENT_TIME_STAMPS; }
        }

        public uint m_PROP_NOTIFY_TYPE = 0;
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED)]
        public virtual uint PROP_NOTIFY_TYPE
        {  
            get { return m_PROP_NOTIFY_TYPE; }
            set { if (value < 2) m_PROP_NOTIFY_TYPE = value; } // 0 : Alarm, 1 Event, see BacnetEventNotificationData.BacnetNotifyTypes
        }       

        public T Last_PRESENT_VALUE;

        public void AnalogObjectEvent()
        {
            Last_PRESENT_VALUE = m_PROP_PRESENT_VALUE;

            for (int i = 0; i < 3; i++)
            {
                BacnetGenericTime stamp = new BacnetGenericTime(DateTime.Now, BacnetTimestampTags.TIME_STAMP_DATETIME);
                m_PROP_EVENT_TIME_STAMPS[i] = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_TIMESTAMP, stamp);
            }
            Enable_Reporting(false);
        }

        public void Enable_Reporting(bool State, uint NotificationClassId=0)
        {
            // INTRINSIC_REPORTING
            m_PROP_LIMIT_ENABLE.SetBit((byte)0, State); // Low Limit enabled
            m_PROP_LIMIT_ENABLE.SetBit((byte)1, State); // High Limit enabled

            m_PROP_EVENT_ENABLE.SetBit((byte)0, State); // TO_OFFNORMAL (High or Low)
            m_PROP_EVENT_ENABLE.SetBit((byte)1, State); // TO_FAULT (not used here)
            m_PROP_EVENT_ENABLE.SetBit((byte)2, State); // TO_NORMAL (back to a good value)

            m_PROP_ACKED_TRANSITIONS.SetBit((byte)0, true);
            m_PROP_ACKED_TRANSITIONS.SetBit((byte)1, true);
            m_PROP_ACKED_TRANSITIONS.SetBit((byte)2, true);

            m_PROP_NOTIFICATION_CLASS = NotificationClassId;

            IntrinsicReportingManagement();
        }

        private void IntrinsicReportingManagement()
        {
            if (Mydevice == null) return;

            if (m_PROP_LIMIT_ENABLE.value[0] == 0)
            {
                Last_PRESENT_VALUE = m_PROP_PRESENT_VALUE;
                return;
            }

            // T type must be convertible to double of course
            double pv = Convert.ToDouble(m_PROP_PRESENT_VALUE);
            double hl = Convert.ToDouble(m_PROP_HIGH_LIMIT);
            double ll = Convert.ToDouble(m_PROP_LOW_LIMIT);
            double db = Convert.ToDouble(m_PROP_DEADBAND);

            bool LimitEnabledHigh = (m_PROP_LIMIT_ENABLE.value[0] & (uint)BacnetEventNotificationData.BacnetLimitEnable.EVENT_HIGH_LIMIT_ENABLE) != 0;
            bool LimitEnabledLow = (m_PROP_LIMIT_ENABLE.value[0] & (uint)BacnetEventNotificationData.BacnetLimitEnable.EVENT_LOW_LIMIT_ENABLE) != 0;

            bool EventToOffNormal = (m_PROP_EVENT_ENABLE.value[0] & (uint)BacnetEventNotificationData.BacnetEventEnable.EVENT_ENABLE_TO_OFFNORMAL) != 0;
            bool EventToNormal = (m_PROP_EVENT_ENABLE.value[0] & (uint)BacnetEventNotificationData.BacnetEventEnable.EVENT_ENABLE_TO_NORMAL) != 0;

            bool NotifyState = false;

            uint fromState = m_PROP_EVENT_STATE; 
            int toState = -1;

            switch (fromState) 
            {
                case (uint)BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_NORMAL :
                    /*  If LimitHigh flag is enabled and Present_Value exceed the High_Limit and Event to Offnormal is enabled then 
                       the notification must be done */
                    if ((pv > hl)&&LimitEnabledHigh)
                    {
                        toState = (int)BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_HIGH_LIMIT;
                        NotifyState = EventToOffNormal;
                    }
                    /* If LowLimit flag is enabled and Present_Value exceed the Low_Limit and Event to Offnormal is enabled then 
                       the notification must be done */
                    if ((pv < ll)&LimitEnabledLow)
                    {
                        toState = (int)BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_LOW_LIMIT;
                        NotifyState = EventToOffNormal;
                    }
                    break;
                case (uint)BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_HIGH_LIMIT:
                    /* Present_Value fall below the High_Limit - Deadband ? */
                    if (pv < (hl - db))
                    {
                        toState = (int)BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_NORMAL;
                        NotifyState =  EventToNormal;
                    }
                    /* Present_Value fall below the Low_Limit ? */
                    if ((pv < ll) && LimitEnabledLow)
                    {
                        toState = (int)BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_LOW_LIMIT;
                        if (!NotifyState)
                            NotifyState = EventToOffNormal;
                    }                    
                    break;
                case (uint)BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_LOW_LIMIT:
                    /* Present_Value exceed the Low_Limit + Deadband ? */
                    if (pv > (ll + db))
                    {
                        toState = (int)BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_NORMAL;
                        NotifyState =  EventToNormal;
                    }
                    /* Present_Value exceed the High_Limit ? */
                    if ((pv > hl) && LimitEnabledHigh)
                    {
                        toState = (int)BacnetEventNotificationData.BacnetEventStates.EVENT_STATE_HIGH_LIMIT;
                        if (!NotifyState)
                            NotifyState = EventToOffNormal;
                    }
                    break;
            }

            if (toState != -1)
            {
                // Update Event_State
                m_PROP_EVENT_STATE = (uint)toState;

                BacnetGenericTime stamp = new BacnetGenericTime(DateTime.Now, BacnetTimestampTags.TIME_STAMP_DATETIME);

                // Update EVENT_TIME_STAMPS, 1 is for FAULT (not used) 
                if (toState == 0)
                    m_PROP_EVENT_TIME_STAMPS[2] = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_TIMESTAMP, stamp); // Normal
                else
                    m_PROP_EVENT_TIME_STAMPS[1] = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_TIMESTAMP, stamp); // Limit
            }
            Last_PRESENT_VALUE = m_PROP_PRESENT_VALUE;

            if (NotifyState==false) return;

            // look for the related notification class object
            NotificationClass nc=(NotificationClass)Mydevice.FindBacnetObject(new BacnetObjectId(BacnetObjectTypes.OBJECT_NOTIFICATION_CLASS,m_PROP_NOTIFICATION_CLASS));
  
            if (nc!=null)
                nc.SendIntrinsectEvent(
                    m_PROP_OBJECT_IDENTIFIER,
                    (BacnetEventNotificationData.BacnetNotifyTypes)m_PROP_NOTIFY_TYPE,
                    BacnetEventNotificationData.BacnetEventTypes.EVENT_CHANGE_OF_VALUE,
                    (BacnetEventNotificationData.BacnetEventStates)fromState,
                    (BacnetEventNotificationData.BacnetEventStates)toState);
        }
    }
}