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

    public class MultiStateInput : AnalogInput<uint>
    {

        public uint m_PROP_NUMBER_OF_STATES;
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT)]
        public virtual uint PROP_NUMBER_OF_STATES
        {
            get { return m_PROP_NUMBER_OF_STATES; }
        }

        public BacnetValue[] m_PROP_STATE_TEXT;
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID)]
        public virtual BacnetValue[] PROP_STATE_TEXT
        {
            get { return m_PROP_STATE_TEXT; }
        }

        public MultiStateInput(int ObjId, String ObjName, String Description, uint StatesNumber, uint InitialValue, bool WithPriorityArray)
            : base(new BacnetObjectId(BacnetObjectTypes.OBJECT_MULTI_STATE_INPUT, (uint)ObjId), ObjName, Description, InitialValue, BacnetUnitsId.UNITS_DEGREES_PHASE)
        {
            // InitialValue must be within 1 and m_PROP_NUMBER_OF_STATES
            m_PROP_NUMBER_OF_STATES = StatesNumber;
            m_PROP_STATE_TEXT = new BacnetValue[StatesNumber];
        }
        public MultiStateInput() { }

        protected override uint BacnetMethodNametoId(String Name)
        {
            if (Name == "get_PROP_UNITS")   // Hide this property
                return (uint)((int)BacnetPropertyIds.MAX_BACNET_PROPERTY_ID );
            else
                return base.BacnetMethodNametoId(Name);
        }
    }
}
