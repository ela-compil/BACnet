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

    public class AnalogOutput<T> : AnalogValueAndOutput<T>
    {
        public AnalogOutput(int ObjId, String ObjName, String Description, T InitialValue, BacnetUnitsId Unit)
            : base(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_OUTPUT, (uint)ObjId), ObjName, Description, InitialValue, Unit, true)
        {
        }
        public AnalogOutput() { }
    }

    public class AnalogValue<T> : AnalogValueAndOutput<T>
    {
        public AnalogValue(int ObjId, String ObjName, String Description, T InitialValue, BacnetUnitsId Unit, bool WithPriorityArray)
            : base(new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, (uint)ObjId), ObjName, Description, InitialValue, Unit, WithPriorityArray)
        {
        }
        public AnalogValue() { }
    }

    public class AnalogValueAndOutput<T> : AnalogObject<T>
    {
        public bool UsePriorityArray = false;

        protected T m_PROP_RELINQUISH_DEFAULT;
        // BacnetSerialize made freely by the stack depending on the type
        public virtual T PROP_RELINQUISH_DEFAULT
        {
            get { return m_PROP_RELINQUISH_DEFAULT; }
            set { 
                    m_PROP_RELINQUISH_DEFAULT=value;
                    ExternalCOVManagement(BacnetPropertyIds.PROP_RELINQUISH_DEFAULT);
                }
        }

        public BacnetValue[] m_PROP_PRIORITY_ARRAY = new BacnetValue[16];
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL)]
        public virtual BacnetValue[] PROP_PRIORITY_ARRAY
        {
            get { return m_PROP_PRIORITY_ARRAY; }
        }

        public AnalogValueAndOutput(BacnetObjectId ObjId, String ObjName, String Description, T InitialValue, BacnetUnitsId Unit, bool WithPriorityArray)
            : base(ObjId, ObjName, Description, InitialValue, Unit)
        {
            if (WithPriorityArray == true)
            {
                UsePriorityArray = true;
                m_PROP_RELINQUISH_DEFAULT = InitialValue;
            }

            this.m_PRESENT_VALUE_ReadOnly = false;
        }

        public AnalogValueAndOutput() { }

        public override void Post_NewtonSoft_Json_Deserialization(DeviceObject device)
        {
            base.Post_NewtonSoft_Json_Deserialization(device);

            // basic int becom int64 for instance during serialization/deserialization
            for (int i = 0; i < 16; i++)
                if  (m_PROP_PRIORITY_ARRAY[i].Tag!=BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL)
                    m_PROP_PRIORITY_ARRAY[i] = new BacnetValue(m_PROP_PRIORITY_ARRAY[i].Tag,Convert.ChangeType(m_PROP_PRIORITY_ARRAY[i].Value,typeof(T)));
        }

        // Do not shows PROP_PRIORITY_ARRAY &  PROP_RELINQUISH_DEFAULT if not in use
        protected override uint BacnetMethodNametoId(String Name)
        {
            if ((UsePriorityArray == false) && ((Name == "get_PROP_PRIORITY_ARRAY") || (Name == "get_PROP_RELINQUISH_DEFAULT")))  // Hide these properties
                return (uint)((int)BacnetPropertyIds.MAX_BACNET_PROPERTY_ID);
            else
                return base.BacnetMethodNametoId(Name);
        }

        // Since set_PROP_PRESENT_VALUE offered by the inherited property PROP_PRESENT_VALUE cannot
        // receive null value to clear the Priority Array, and do not have the priority
        // it is 'override' here with the set2_xxx (which replace set_xx in the call if exist)        
        public virtual void set2_PROP_PRESENT_VALUE(IList<BacnetValue> Value, byte WritePriority)
        {
            if (UsePriorityArray == false)
            {
                PROP_PRESENT_VALUE = (T)Convert.ChangeType(Value[0].Value, typeof(T));
                //PROP_PRESENT_VALUE = (T) Value[0].Value;
                return;
            }
            else
            {
                m_PROP_PRIORITY_ARRAY[(int)WritePriority - 1] = Value[0];

                bool done = false;
                for (int i = 0; i < 16; i++)
                {
                    if (m_PROP_PRIORITY_ARRAY[i].Value != null)    // A value is OK
                    {
                        PROP_PRESENT_VALUE = (T)(T)Convert.ChangeType(m_PROP_PRIORITY_ARRAY[i].Value, typeof(T));
                        //PROP_PRESENT_VALUE = (T)m_PROP_PRIORITY_ARRAY[i].Value;
                        done = true;
                        break;
                    }
                }
                if (done == false)  // Nothing in the array : PROP_PRESENT_VALUE = PROP_RELINQUISH_DEFAULT
                {
                    PROP_PRESENT_VALUE = m_PROP_RELINQUISH_DEFAULT;
                }

                return;
            }
        }

        // This will shows a property which is only programmed with methods
        // ... just for the example
        protected bool binternal = true;

        public virtual IList<BacnetValue> get2_PROP_OPTIONAL()
        {
            return new BacnetValue[1] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN, binternal) };
         }

        public virtual void set2_PROP_OPTIONAL(IList<BacnetValue> Value, byte WritePriority)
        {
            binternal = (bool)Value[0].Value;
        }
        
    }
}
