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
using System.Runtime.InteropServices;

namespace BaCSharp
{
    public class TrendLog : BaCSharpObject
    {
        public uint m_PROP_RECORD_COUNT = 0; 
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)]
        public virtual uint PROP_RECORD_COUNT
        {
            get { return m_PROP_RECORD_COUNT; }
        }
        public uint m_PROP_BUFFER_SIZE;
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)]
        public virtual uint PROP_BUFFER_SIZE
        {
            get { return m_PROP_BUFFER_SIZE; }
        }

        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED)]
        public virtual uint PROP_EVENT_STATE
        {
            get { return 0; }
        }

        public BacnetBitString m_PROP_STATUS_FLAGS = new BacnetBitString();
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING)]
        public virtual BacnetBitString PROP_STATUS_FLAGS
        {
            get { return m_PROP_STATUS_FLAGS; }
        }

        public bool m_PROP_ENABLE = true;
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN)]
        public virtual bool PROP_ENABLE
        {
            get { return m_PROP_ENABLE; }
            set
            {
                m_PROP_ENABLE = value;
                ExternalCOVManagement(BacnetPropertyIds.PROP_ENABLE);
            }
        }

        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN)]
        public virtual bool PROP_STOP_WHEN_FULL
        {
            get { return false; }
        }

        // protected : not serialized, please use a separate process than json serialisation
        protected  BacnetLogRecord[] Trend;
        protected int LogPtr = 0;

        public  BacnetTrendLogValueType DefaultValueType;

        public TrendLog(int ObjId, String ObjName, String Description, uint Logsize, BacnetTrendLogValueType DefaultValueType)
            : base(new BacnetObjectId(BacnetObjectTypes.OBJECT_TRENDLOG,(uint)ObjId), ObjName, Description)
        {
            m_PROP_STATUS_FLAGS.SetBit((byte)0, false);
            m_PROP_STATUS_FLAGS.SetBit((byte)1, false);
            m_PROP_STATUS_FLAGS.SetBit((byte)2, false);
            m_PROP_STATUS_FLAGS.SetBit((byte)3, false);

            this.DefaultValueType = DefaultValueType;
            m_PROP_BUFFER_SIZE = Logsize;
        }

        public TrendLog() { }

        public override void Post_NewtonSoft_Json_Deserialization(DeviceObject device)
        {
            base.Post_NewtonSoft_Json_Deserialization(device);
            m_PROP_RECORD_COUNT = 0;
        }

        public virtual void AddValue(object Value, DateTime TimeStamp, uint Status, BacnetTrendLogValueType? ValueType=null)
        {
            if (Trend == null)
                Trend = new BacnetLogRecord[m_PROP_BUFFER_SIZE];

            if (ValueType!=null)
                Trend[LogPtr] = new BacnetLogRecord((BacnetTrendLogValueType)ValueType, Value, TimeStamp, Status);
            else
                Trend[LogPtr] = new BacnetLogRecord(DefaultValueType, Value, TimeStamp, Status);

            LogPtr = (LogPtr + 1) % Trend.Length;   // circular buffer
            if (m_PROP_RECORD_COUNT < Trend.Length)
                m_PROP_RECORD_COUNT++;
        }

        public virtual void AddValue(object Value, uint Status, BacnetTrendLogValueType? ValueType=null)
        {
            AddValue(Value, DateTime.Now, Status, ValueType);
        }

        // By Morten Kvistgaard
        public virtual byte[] GetEncodedTrends(uint start, int count, out BacnetResultFlags status)
        {

            status = BacnetResultFlags.NONE;
            start--;    //position is 1 based

            if (start >= m_PROP_RECORD_COUNT || (start + count) > m_PROP_RECORD_COUNT)
                return null;

            if (start == 0) status |= BacnetResultFlags.FIRST_ITEM;
            if ((start + count) >= m_PROP_RECORD_COUNT) status |= BacnetResultFlags.LAST_ITEM;
            else status |= BacnetResultFlags.MORE_ITEMS;

            System.IO.BACnet.Serialize.EncodeBuffer buffer = new System.IO.BACnet.Serialize.EncodeBuffer();

            int offset;

            if (m_PROP_RECORD_COUNT < m_PROP_BUFFER_SIZE) // the buffer is not full
                offset = 0;
            else
                offset = LogPtr;    // circular buffer

            for (uint i = start; i < (start + count); i++)
            {
                System.IO.BACnet.Serialize.Services.EncodeLogRecord(buffer, Trend[(offset + i) % Trend.Length]);
            }

            return buffer.ToArray();
        }
    }
}
