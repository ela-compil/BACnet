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

namespace AnotherStorageImplementation
{
    [Serializable]
    class TrendLog : BacnetObject
    {

        uint m_PROP_RECORD_COUNT=0;
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)]
        public virtual uint PROP_RECORD_COUNT
        {
            get { return m_PROP_RECORD_COUNT; }
        }
        uint m_PROP_BUFFER_SIZE;
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)]
        public virtual uint PROP_BUFFER_SIZE
        {
            get { return m_PROP_BUFFER_SIZE; }
        }

        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED)]
        public virtual uint PROP_POLARITY
        {
            get { return 0; }
        }
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED)]
        public virtual uint PROP_EVENT_STATE
        {
            get { return 0; }
        }

        BacnetBitString m_PROP_STATUS_FLAGS = new BacnetBitString();
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING)]
        public virtual BacnetBitString PROP_STATUS_FLAGS
        {
            get { return m_PROP_STATUS_FLAGS; }
        }

        bool m_PROP_ENABLED = true;
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN)]
        public virtual bool PROP_ENABLED
        {
            get { return m_PROP_ENABLED; }
            set
            {
                m_PROP_ENABLED = value;
                COVManagement(BacnetPropertyIds.PROP_PRESENT_VALUE);
            }
        }

        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN)]
        public virtual bool PROP_STOP_WHEN_FULL
        {
            get { return false; }
        }

        BacnetLogRecord[] Trend;
        int LogCount = 0;

        public TrendLog(int ObjId, String ObjName, uint Logsize)
            : base(new BacnetObjectId(BacnetObjectTypes.OBJECT_TRENDLOG,(uint)ObjId), ObjName)
        {
            m_PROP_STATUS_FLAGS.SetBit((byte)0, false);
            m_PROP_STATUS_FLAGS.SetBit((byte)1, false);
            m_PROP_STATUS_FLAGS.SetBit((byte)2, false);
            m_PROP_STATUS_FLAGS.SetBit((byte)3, false);

            m_PROP_BUFFER_SIZE = Logsize;
            Trend = new BacnetLogRecord[Logsize];
            for (int i = 0; i < Logsize; i++)
            {
                Trend[i] = new BacnetLogRecord(BacnetTrendLogValueType.TL_TYPE_UNSIGN, 0, DateTime.Now, 0);
            }
        }
        public void AddValue(BacnetTrendLogValueType ValueType, object Value, DateTime TimeStamp, uint Status)
        {
            Trend[LogCount] = new BacnetLogRecord(ValueType, Value, TimeStamp, Status);
            LogCount = (LogCount + 1) % Trend.Length;
            if (m_PROP_RECORD_COUNT < Trend.Length)
                m_PROP_RECORD_COUNT++;
        }

        public void AddValue(BacnetTrendLogValueType ValueType, object Value, uint Status)
        {
            AddValue(ValueType, Value, DateTime.Now, Status);
        }

        // By Morten Kvistgaard
        public byte[] GetEncodedTrends(uint start, int count, out BacnetResultFlags status)
        {
            status = BacnetResultFlags.NONE;
            start--;    //position is 1 based

            if (start >= m_PROP_RECORD_COUNT || (start + count) > m_PROP_RECORD_COUNT)
                return null;

            if (start == 0) status |= BacnetResultFlags.FIRST_ITEM;
            if ((start + count) >= m_PROP_RECORD_COUNT) status |= BacnetResultFlags.LAST_ITEM;
            else status |= BacnetResultFlags.MORE_ITEMS;

            System.IO.BACnet.Serialize.EncodeBuffer buffer = new System.IO.BACnet.Serialize.EncodeBuffer();
            for (uint i = start; i < (start + count); i++)
            {
                System.IO.BACnet.Serialize.Services.EncodeLogRecord(buffer, Trend[i]);
            }

            return buffer.ToArray();
        }
    }
}
