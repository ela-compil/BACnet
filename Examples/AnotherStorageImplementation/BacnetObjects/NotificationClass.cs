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

namespace BaCSharp
{
    public class NotificationClass : BaCSharpObject
    {
        // Don't understand the reason of this redondancy value, already in PROP_OBJECT_IDENTIFIER !
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)]
        public virtual uint PROP_NOTIFICATION_CLASS
        {
            get { return m_PROP_OBJECT_IDENTIFIER.instance; }
        }

        protected IList<BacnetValue> m_PROP_PRIORITY = new BacnetValue[3];
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL)]
        public virtual IList<BacnetValue> PROP_PRIORITY
        {
            get { return m_PROP_PRIORITY; }
            set {
                m_PROP_PRIORITY = value; 
            }
        }

        public List<BacnetValue> m_PROP_RECIPIENT_LIST = new List<BacnetValue>();
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL)]
        public virtual List<BacnetValue> PROP_RECIPIENT_LIST
        {
            get 
            {                 
                return m_PROP_RECIPIENT_LIST;             
            }
            set 
            {
                if (value == null)
                {
                    m_PROP_RECIPIENT_LIST = value;
                    return;
                }

                if ((value.Count % 7) != 0)
                    return;

                m_PROP_RECIPIENT_LIST.Clear();
                for (int i = 0; i < value.Count / 7; i++)
                {
                    m_PROP_RECIPIENT_LIST.Add(new BacnetValue(new DeviceReportingRecipient(value[i * 7], value[i * 7 + 1], value[i * 7 + 2], value[i * 7 + 3], value[i * 7 + 4], value[i * 7 + 5], value[i * 7 + 6])));
                }
            }
        }

        public BacnetBitString m_PROP_ACK_REQUIRED = new BacnetBitString();
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING)]
        public virtual BacnetBitString PROP_ACK_REQUIRED
        {
            get { return m_PROP_ACK_REQUIRED; }
        }

        public BacnetObjectId Device;

        public NotificationClass(int ObjId, String ObjName, String Description, BacnetObjectId Device)
            : base(new BacnetObjectId(BacnetObjectTypes.OBJECT_NOTIFICATION_CLASS, (uint)ObjId), ObjName, Description)
        {
            this.Device = Device;

            for (int i = 0; i < 3; i++)
                m_PROP_PRIORITY[i]=new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT, (uint)127);

            m_PROP_ACK_REQUIRED.SetBit((byte)0, false);
            m_PROP_ACK_REQUIRED.SetBit((byte)1, false);
            m_PROP_ACK_REQUIRED.SetBit((byte)2, false);

        }
        public NotificationClass() { }

        public override void Post_NewtonSoft_Json_Deserialization(DeviceObject device)
        {
            base.Post_NewtonSoft_Json_Deserialization(device);

            // In this copy the type become int64
            for (int i=0; i<3;i++)
                m_PROP_PRIORITY[i] = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT,(uint)Convert.ToUInt16(m_PROP_PRIORITY[i].Value));
        }

        public void AddReportingRecipient(DeviceReportingRecipient recipient)
        {
            m_PROP_RECIPIENT_LIST.Add(new BacnetValue(recipient));
        }

        public void SendIntrinsectEvent(BacnetObjectId SenderObject,
                    BacnetNotifyTypes notifyType,
                    BacnetEventTypes evenType,
                    BacnetEventStates fromstate,
                    BacnetEventStates tostate)
        {

            if ((m_PROP_RECIPIENT_LIST == null) || (m_PROP_RECIPIENT_LIST.Count == 0))
                return;

            BacnetEventNotificationData bacnetEvent = new BacnetEventNotificationData();

            // The struct is the same of all recipients, except one attribut
            bacnetEvent.notificationClass = m_PROP_OBJECT_IDENTIFIER.instance;
            bacnetEvent.initiatingObjectIdentifier = Device;
            bacnetEvent.eventObjectIdentifier = SenderObject;
            bacnetEvent.toState = tostate;
            bacnetEvent.fromState = fromstate;
            bacnetEvent.notifyType = notifyType;
            bacnetEvent.eventType = evenType;

            BacnetGenericTime timeStamp = new BacnetGenericTime();
            timeStamp.Tag = BacnetTimestampTags.TIME_STAMP_DATETIME;
            timeStamp.Time = DateTime.Now;

            bacnetEvent.timeStamp = timeStamp;
            bacnetEvent.priority = 127;

            for (int i = 0; i < m_PROP_RECIPIENT_LIST.Count; i++)
            {

                bool DoASend = true;

                DeviceReportingRecipient devReportEntry = (DeviceReportingRecipient)m_PROP_RECIPIENT_LIST[i].Value;

                // Time is OK ?
                if (TimeSpan.Compare(DateTime.Now.TimeOfDay, devReportEntry.fromTime.TimeOfDay) == -1)
                    DoASend = false;
                if (TimeSpan.Compare(devReportEntry.toTime.TimeOfDay, DateTime.Now.TimeOfDay) == -1)
                    DoASend = false;

                // Day is OK ?
                int DayOfWeek = (int)DateTime.Now.DayOfWeek;
                if (DayOfWeek == 0) DayOfWeek = 7;  // Put Sunday at the end of the enumaration
                DayOfWeek = DayOfWeek - 1;          // start at 0

                if ((devReportEntry.WeekofDay.value[0] & (1 << DayOfWeek)) == 0)
                    DoASend = false;

                // new State is OK ?
                if ((tostate == BacnetEventStates.EVENT_STATE_OFFNORMAL) && ((devReportEntry.evenType.value[0] & 1) != 1))
                    DoASend = false;
                if ((tostate == BacnetEventStates.EVENT_STATE_NORMAL) && ((devReportEntry.evenType.value[0] & 2) != 2))
                    DoASend = false;
                if ((tostate == BacnetEventStates.EVENT_STATE_FAULT) && ((devReportEntry.evenType.value[0] & 4) != 4))
                    DoASend = false;

                // Find the receiver endPoint
                KeyValuePair<BacnetClient, BacnetAddress>? recipient = null;

                if ((devReportEntry.adr != null)&&(Mydevice.DirectIp!=null))
                    recipient = new KeyValuePair<BacnetClient, BacnetAddress>
                        (
                        Mydevice.DirectIp,
                        devReportEntry.adr
                        );
                else
                    try
                    {
                        recipient = Mydevice.SuroundingDevices[devReportEntry.Id.instance];
                    }
                    catch { }

                if (recipient == null)
                    DoASend = false;

                if (DoASend == true)
                {
                    uint processIdentifier = devReportEntry.processIdentifier;

                    object bacnetEventlock = new object();    // we need to change safely one element in the struct
                    System.Threading.ThreadPool.QueueUserWorkItem((o) =>
                    {
                        lock (bacnetEventlock)
                        {
                            bacnetEvent.processIdentifier = processIdentifier;
                            recipient.Value.Key.SendUnconfirmedEventNotification(recipient.Value.Value, bacnetEvent);
                        }

                    }, null);
                }
            }

        }      
    }
}
