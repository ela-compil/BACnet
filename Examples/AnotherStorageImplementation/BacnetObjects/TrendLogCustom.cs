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
    // Shows how to customize a base class

    class TrendLogCustom : TrendLog
    {
        // first a constructor calling the parent constructor
        public TrendLogCustom(int ObjId, String ObjName, String Description, uint Logsize, BacnetTrendLogValueType DefaultValueType)
            : base(ObjId, ObjName, Description, Logsize, DefaultValueType) { }
        // second an empty constructor (needed only by the serialisation process)
        public TrendLogCustom() { }

        // This property is inherited from BaCSharpObject, and replaced here
        public override string PROP_OBJECT_NAME
        {
            get
            {
                return "Custom Trend : " + base.PROP_OBJECT_NAME;
            }
        }

        // This property is read only in the base class (only get)
        public virtual new uint PROP_RECORD_COUNT
        {
            get
            {
                return base.PROP_RECORD_COUNT;
            }
            set
            {
                if (value == 0)
                {
                    base.Clear();
                }
            }
        }
        // This property is read only in the base class (only get)
        public virtual new uint PROP_BUFFER_SIZE
        {
            get
            {
                return base.PROP_BUFFER_SIZE;
            }
            set
            {
                if (value > 10) // a minimal value
                {
                    if (TrendBuffer != null)
                    {
                        // sure it's not the way resize could be done to be full OK
                        Array.Resize<BacnetLogRecord>(ref TrendBuffer, (int)value);
                        m_PROP_BUFFER_SIZE = value;
                        LogPtr = LogPtr % TrendBuffer.Length;
                        m_PROP_RECORD_COUNT = (uint)Math.Min(m_PROP_RECORD_COUNT, value);
                    }
                    else
                        m_PROP_BUFFER_SIZE = value;
                }
            }
        }
        // This property is brend new
        public virtual string PROP_PROFILE_NAME
        {
            get
            {
                return "A profile";
            }
        }
        // This property is brend new and proprietary
        public virtual bool PROP_3101
        {
            get
            { return true; }
        }

    }
}
