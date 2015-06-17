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
    [Serializable] 
    public abstract partial class AnalogObject<T> : BaCSharpObject
    {
        public uint m_PROP_UNITS;
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED)]
        public virtual uint PROP_UNITS
        {
            get { return m_PROP_UNITS; }
        }
        public uint m_PROP_EVENT_STATE = 0;
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED)]
        public virtual uint PROP_EVENT_STATE
        {
            get { return m_PROP_EVENT_STATE; } // See BacnetEventNotificationData.BacnetEventStates  
        }

        public BacnetBitString m_PROP_STATUS_FLAGS = new BacnetBitString();
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING)]
        public virtual BacnetBitString PROP_STATUS_FLAGS
        {
            get { return m_PROP_STATUS_FLAGS; }
        }

        public bool m_PROP_OUT_OF_SERVICE = false;
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN)]
        public virtual bool PROP_OUT_OF_SERVICE
        {
            get { return m_PROP_OUT_OF_SERVICE; }
            set { 
                    m_PROP_OUT_OF_SERVICE=value;
                    InternalCOVManagement(BacnetPropertyIds.PROP_OUT_OF_SERVICE);
                }
        }

        public bool m_PRESENT_VALUE_ReadOnly = false;
        public T m_PROP_PRESENT_VALUE;
        // BacnetSerialize made freely by the stack depending on the type
        public virtual T PROP_PRESENT_VALUE
        {
            get { return m_PROP_PRESENT_VALUE; }
            set 
            {
                if (m_PRESENT_VALUE_ReadOnly == false)
                    internal_PROP_PRESENT_VALUE = value;
                else
                    ErrorCode_PropertyWrite = ErrorCodes.WriteAccessDenied;
            }
        }

        // This property shows the same attribut as the previous, but without restriction
        // for internal usage, not for network callbacks
        public virtual T internal_PROP_PRESENT_VALUE
        {
            get { return m_PROP_PRESENT_VALUE; }
            set { 
                    m_PROP_PRESENT_VALUE = value; 
                    InternalCOVManagement(BacnetPropertyIds.PROP_PRESENT_VALUE);
                    IntrinsicReportingManagement();
                }
        }

        public AnalogObject() {}

        public AnalogObject(BacnetObjectId ObjId, String ObjName, String Description, T InitialValue, BacnetUnitsId Unit)
            : base(ObjId, ObjName, Description)
        {

            m_PROP_STATUS_FLAGS.SetBit((byte)0, false);
            m_PROP_STATUS_FLAGS.SetBit((byte)1, false);
            m_PROP_STATUS_FLAGS.SetBit((byte)2, false);
            m_PROP_STATUS_FLAGS.SetBit((byte)3, false);

            m_PROP_UNITS = (uint)Unit;

            m_PROP_PRESENT_VALUE = InitialValue;

            AnalogObjectEvent();

        }
    }
}
