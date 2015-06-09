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
using System.Reflection;

namespace AnotherStorageImplementation
{
    public enum ErrorCodes
    {
        Good = 0,
        GenericError = -1,
        NotExist = -2,
        NotForMe = -3,
        WriteAccessDenied = -4,
        OutOfRange = -5
    }

    [Serializable]
    abstract class BacnetObject
    {
        // 3 common properties to all kind of Bacnet objects ... I suppose ! 

        public string m_PROP_OBJECT_NAME;
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING)]
        public virtual string PROP_OBJECT_NAME
        {
            get { return m_PROP_OBJECT_NAME; }
        }
        public BacnetObjectId m_PROP_OBJECT_IDENTIFIER;
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID)]
        public virtual BacnetObjectId PROP_OBJECT_IDENTIFIER
        {
            get { return m_PROP_OBJECT_IDENTIFIER; }
        }
        public uint m_PROP_OBJECT_TYPE;
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED)]
        public virtual uint PROP_OBJECT_TYPE
        {
            get { return m_PROP_OBJECT_TYPE; }
        }

        public delegate void WriteNotificationCallbackHandler(BacnetObject sender, BacnetPropertyIds propId);
        // One event for each object if needed
        public event WriteNotificationCallbackHandler OnWriteNotify;
        // One global event for all the content
        public static event WriteNotificationCallbackHandler OnCOVNotify;

        public ErrorCodes ErrorCode_PropertyWrite;

        public BacnetObject(BacnetObjectId ObjId, String ObjName)
        {
            m_PROP_OBJECT_IDENTIFIER = ObjId;
            m_PROP_OBJECT_NAME = ObjName;
            m_PROP_OBJECT_TYPE = (uint)ObjId.type;
        }

        public bool Equals(BacnetObjectId objId)
        {
            return this.m_PROP_OBJECT_IDENTIFIER.Equals(objId);
        }

        public void COVManagement(BacnetPropertyIds propId)
        {
            if (OnCOVNotify != null)
                OnCOVNotify(this, propId);
        }

        public IList<BacnetValue> FindPropValue(String propName)
        {
            IList<BacnetValue> propVal = null;

            // find first the property into the programmed methods
            // so that if a property exist in a  class and programmed in 
            // the heritage, the programmed one is the winner !
            MethodInfo m = this.GetType().GetMethod("get2_"+propName);
            if (m != null)
            {
                propVal = (IList<BacnetValue>)m.Invoke(this, null);
                return propVal;
            }
            // second find the property into the programmed property
            PropertyInfo p = this.GetType().GetProperty(propName);
            if (p != null)
            {
                object[] o = p.GetCustomAttributes(true);
                BacnetValue b;
                if (o.Length == 0)
                {
                    b = new BacnetValue(p.GetValue(this, null));
                    return new BacnetValue[] { b };
                }
                else
                {
                    object val = p.GetValue(this, null);
                    IList<BacnetValue> ret=null;

                    if (val != null)
                        try
                        {
                            ret = (IList<BacnetValue>)val;    // the value is already IList<  >
                        }
                        catch
                        {
                            ret = new BacnetValue[] { new BacnetValue((o[0] as BaCSharpTypeAttribute).SerializeType, val) };
                        }
                    else
                        ret = new BacnetValue[] { new BacnetValue(null) };


                    return ret;
                    /*
                    if ((o[0] as BacnetSerializeAttribute).IsIlist)
                        return (IList<BacnetValue>)p.GetValue(this, null);
                    else
                    {
                        b = new BacnetValue((o[0] as BacnetSerializeAttribute).SerializeType, p.GetValue(this, null));
                        return new BacnetValue[] { b };
                    }
                     * */
                }
            }

            return propVal;
        }

        public ErrorCodes ReadPropertyValue(BacnetPropertyReference PropRef, out IList<BacnetValue> propVal)
        {
            propVal = null;

            try
            {
                propVal=FindPropValue(PropRef.ToString());
                if (propVal == null)
                    return ErrorCodes.NotExist;

                // number of elements required
                if (PropRef.propertyArrayIndex == 0)
                {
                    propVal= new BacnetValue[] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT, (uint)propVal.Count) };
                    return ErrorCodes.Good;
                }

                // only a particular element
                else if (PropRef.propertyArrayIndex != System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL)
                    propVal = new BacnetValue[] { propVal[(int)PropRef.propertyArrayIndex - 1] };
             
                return ErrorCodes.Good;

            }
            catch
            {
                return ErrorCodes.GenericError;
            }
        }

        public bool ReadPropertyMultiple(IList<BacnetPropertyReference> properties, out IList<BacnetPropertyValue> values)
        {

            values = new BacnetPropertyValue[properties.Count];

            int count = 0;
            foreach (BacnetPropertyReference entry in properties)
            {
                BacnetPropertyValue new_entry = new BacnetPropertyValue();
                new_entry.property = entry;
                if (ReadPropertyValue(entry, out new_entry.value) != ErrorCodes.Good)
                    new_entry.value = new BacnetValue[] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ERROR, new BacnetError(BacnetErrorClasses.ERROR_CLASS_OBJECT, BacnetErrorCodes.ERROR_CODE_UNKNOWN_PROPERTY)) };
                values[count] = new_entry;
                count++;
            }

            return true;
        }

        // this method is virtual since some childrens classes could 
        // hide some parents properties by sending a fake number
        // see MultiStateOutput for instance
        protected virtual uint BacnetMethodNametoId(String Name)
        {
            try
            {
                if (Name.Substring(0, 4) == "get_")
                    return (uint)(BacnetPropertyIds)Enum.Parse(typeof(BacnetPropertyIds), Name.Substring(4), true);
                if (Name.Substring(0, 5) == "get2_")
                    return (uint)(BacnetPropertyIds)Enum.Parse(typeof(BacnetPropertyIds), Name.Substring(5), true);
            }
            catch { }

            return (uint)((int)BacnetPropertyIds.MAX_BACNET_PROPERTY_ID+1);
        }

        public bool ReadPropertyAll(out IList<BacnetPropertyValue> values)
        {
            IList<BacnetPropertyReference> properties = new List<BacnetPropertyReference>();

            MethodInfo[] allmethod = this.GetType().GetMethods();   // all the methods in this class

            foreach (MethodInfo m in allmethod)
            {
                uint PropId = BacnetMethodNametoId(m.Name);         // looking for all with a 'Bacnet name'
                if (PropId <= (uint)BacnetPropertyIds.MAX_BACNET_PROPERTY_ID)
                    if (properties.Count(o => o.propertyIdentifier == (uint)PropId) == 0) // could be get_ and get2_, only one is required
                        properties.Add(new BacnetPropertyReference(PropId, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL));
            }

            return ReadPropertyMultiple(properties, out values);
        }

        public ErrorCodes WritePropertyValue(BacnetPropertyValue value, bool writeFromNetwork)
        {
            // First try to found the set2_ method in the class code
            MethodInfo m = this.GetType().GetMethod("set2_"+value.property.ToString());
            try
            {
                if (m != null)
                {
                    // Yes Invoke
                    ErrorCode_PropertyWrite = ErrorCodes.Good;
                    m.Invoke(this, new object[] { value.value, value.priority });

                    if (ErrorCode_PropertyWrite == ErrorCodes.Good)
                    {
                        if (OnWriteNotify != null) OnWriteNotify(this, (BacnetPropertyIds)value.property.propertyIdentifier);
                        if (OnCOVNotify != null) OnCOVNotify(this, (BacnetPropertyIds)value.property.propertyIdentifier);
                    }

                    return ErrorCode_PropertyWrite;
                }
            }
            catch
            {
                return ErrorCodes.GenericError;
            }            

            // Second, if not found, try to find in the programmed properties list
            PropertyInfo p = this.GetType().GetProperty(value.property.ToString());

            if (p != null)
            {
                if (p.GetSetMethod() == null) return ErrorCodes.WriteAccessDenied;

                try
                {
                    // since Property cannot return error, this member could be set if a problem occure
                    // or an Exception can be throw
                    ErrorCode_PropertyWrite = ErrorCodes.Good;

                    object[] o = p.GetCustomAttributes(true);

                    if (o.Length == 0)
                    {
                        p.SetValue(this, value.value[0].Value, null);
                    }
                    else
                    {
                        try
                        {
                            p.SetValue(this, value.value[0].Value, null); // The value is not a List< >  
                        }
                        catch
                        {
                            p.SetValue(this, value.value, null);    // The value is a List <  >
                        }
                    }

                    if (ErrorCode_PropertyWrite == ErrorCodes.Good)
                    {
                        if (OnWriteNotify != null) OnWriteNotify(this, (BacnetPropertyIds)value.property.propertyIdentifier);
                        if (OnCOVNotify != null) OnCOVNotify(this, (BacnetPropertyIds)value.property.propertyIdentifier);
                    }

                    return ErrorCode_PropertyWrite;
                }      
                catch
                {
                    return ErrorCodes.GenericError;
                }
            }

            return ErrorCodes.NotExist;
        }

    }
}
