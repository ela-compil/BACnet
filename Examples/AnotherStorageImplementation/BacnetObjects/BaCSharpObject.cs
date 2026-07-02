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
using System.Diagnostics;

namespace BaCSharp
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

    // All children classes are serializable, except Device and Structured View
    // That's why the default constructor is present everywhere, and members attributs public
    // So state persistance implementation is quite easy
    [Serializable]
    //
    // You want to serialize a List<BaCSharpObject>, you need to add XmlInclude mark for each type
    // For instance if the List include some BacnetFile : [XmlInclude(typeof(BacnetFile))]
    //
    // Somes classes a parametrable, like AnalogInput. In this case you need to add it for each
    // type used : [XmlInclude(typeof(AnalogInput<double>))] ... or anything else
    //]
    public abstract class BaCSharpObject
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

        // An optional property
        public string m_PROP_DESCRIPTION;
        [BaCSharpType(BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING)]
        public virtual string PROP_DESCRIPTION
        {
            get { return m_PROP_DESCRIPTION; }
        }

        public delegate void WriteNotificationCallbackHandler(BaCSharpObject sender, BacnetPropertyIds propId);
        // One event for each object if needed
        public event WriteNotificationCallbackHandler OnWriteNotify;
        // One global event for all the content
        public static event WriteNotificationCallbackHandler OnExternalCOVNotify;

        //To get back the raw buffer for specific decoding if needed
        protected BacnetClient sender;

        protected ErrorCodes ErrorCode_PropertyWrite;

        IList<BacnetPropertyReference> AllMyProperties = null;

        // Somes classes needs to access to the device object
        protected DeviceObject Mydevice;
        public virtual DeviceObject deviceOwner
        {
            set 
            {
                // could be only called by a device object
                MethodBase m = new StackFrame(1).GetMethod();
                if (m.DeclaringType == typeof(DeviceObject))
                    Mydevice = value; 
            }
        }

        public BaCSharpObject(){}

        public BaCSharpObject(BacnetObjectId ObjId, String ObjName, String Description)
        {
            m_PROP_OBJECT_IDENTIFIER = ObjId;
            m_PROP_OBJECT_NAME = ObjName;
            m_PROP_OBJECT_TYPE = (uint)ObjId.type;
            m_PROP_DESCRIPTION = Description;
        }

        public virtual void Dispose(){}

        public override string ToString()
        {
            return m_PROP_OBJECT_IDENTIFIER.ToString();
        }

        public bool Equals(BacnetObjectId objId)
        {
            return this.m_PROP_OBJECT_IDENTIFIER.Equals(objId);
        }

        // Object was not created with a new in the code but by a Deserialization
        // using the default constructor
        // some additionnal up to run code must be executed 
        // Only a call to the method (without parameter) on the device object should be made
        public virtual void Post_NewtonSoft_Json_Deserialization(DeviceObject device)
        {
            Mydevice = device;
        }

        // Managed in BacnetActivity.cs, for COV notification to client
        public void ExternalCOVManagement(BacnetPropertyIds propId)
        {
            if (OnExternalCOVNotify != null)
                OnExternalCOVNotify(this, propId);
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
                            ret = new BacnetValue[] { new BacnetValue((o[0] as BaCSharpTypeAttribute).BacnetNativeType, val) };
                        }
                    else
                        ret = new BacnetValue[] { new BacnetValue(null) };


                    return ret;
                }
            }

            return propVal;
        }
        public ErrorCodes ReadPropertyValue(BacnetPropertyReference PropRef, out IList<BacnetValue> propVal)
        {
            propVal = null;

            try
            {

                string PropName = PropRef.ToString();
                if (PropName[0] != 'P') PropName = "PROP_"+PropName; // private property, not in the Enum list

                propVal = FindPropValue(PropName);

                if (propVal == null)
                    return ErrorCodes.NotExist;

                // number of elements required
                if (PropRef.propertyArrayIndex == 0)
                {
                    propVal = new BacnetValue[] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT, (uint)propVal.Count) };
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
        public ErrorCodes ReadPropertyValue(BacnetClient sender, BacnetAddress adr, BacnetPropertyReference PropRef, out IList<BacnetValue> propVal)
        {
            return ReadPropertyValue(PropRef, out propVal);
        }

        public bool ReadPropertyMultiple(BacnetClient sender, BacnetAddress adr, IList<BacnetPropertyReference> properties, out IList<BacnetPropertyValue> values)
        {

            values = new BacnetPropertyValue[properties.Count];

            int count = 0;
            foreach (BacnetPropertyReference entry in properties)
            {
                BacnetPropertyValue new_entry = new BacnetPropertyValue();
                new_entry.property = entry;
                if (ReadPropertyValue(sender, adr, entry, out new_entry.value) != ErrorCodes.Good)
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
                if ((Name.Substring(0, 9) == "get_PROP_")&&Char.IsDigit(Name,9))
                    return Convert.ToUInt32(Name.Substring(9)); // Private property get_PROP_number
            }
            catch { }
           
            try
            {
                if (Name.Substring(0, 4) == "get_")
                    return (uint)(BacnetPropertyIds)Enum.Parse(typeof(BacnetPropertyIds), Name.Substring(4), true);
                if (Name.Substring(0, 5) == "get2_")
                    return (uint)(BacnetPropertyIds)Enum.Parse(typeof(BacnetPropertyIds), Name.Substring(5), true);
            }
            catch { }

            return (uint)((int)BacnetPropertyIds.MAX_BACNET_PROPERTY_ID);
        }

        public bool ReadPropertyAll(BacnetClient sender, BacnetAddress adr, out IList<BacnetPropertyValue> values)
        {

            if (AllMyProperties == null)
            {
                AllMyProperties = new List<BacnetPropertyReference>();

                MethodInfo[] allmethod = this.GetType().GetMethods();   // all the methods in this class

                foreach (MethodInfo m in allmethod)
                {
                    uint PropId = BacnetMethodNametoId(m.Name);         // looking for all with a 'Bacnet name'
                    if (PropId < (uint)BacnetPropertyIds.MAX_BACNET_PROPERTY_ID)
                    {
                        BacnetPropertyReference propref = new BacnetPropertyReference(PropId, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL);
                        // could be get_ and get2_, only one is required
                        if (!AllMyProperties.Contains(propref))
                            AllMyProperties.Add(propref);
                    }
                }
            }

            return ReadPropertyMultiple(sender, adr, AllMyProperties, out values);
        }

        public ErrorCodes WritePropertyValue(BacnetPropertyValue value, bool writeFromNetwork)
        {
            // First try to found the set2_ method in the class code
            MethodInfo m = this.GetType().GetMethod("set2_" + value.property.ToString());
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
                        if (OnExternalCOVNotify != null) OnExternalCOVNotify(this, (BacnetPropertyIds)value.property.propertyIdentifier);
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

                    try
                    {
                        if (value.value.Count == 1)
                        {
                            try
                            {
                                p.SetValue(this, value.value[0].Value, null);   // The value is not a List< >
                            }
                            catch { }
                        }
                        else
                            p.SetValue(this, value.value, null);    // The value is a List <  >                              
                    }
                    catch
                    {
                        p.SetValue(this, value.value[0].Value, null); // The value is not a List< > but a List<> was given 
                    }


                    if (ErrorCode_PropertyWrite == ErrorCodes.Good)
                    {
                        if (OnWriteNotify != null) OnWriteNotify(this, (BacnetPropertyIds)value.property.propertyIdentifier);
                        if (OnExternalCOVNotify != null) OnExternalCOVNotify(this, (BacnetPropertyIds)value.property.propertyIdentifier);
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

        public ErrorCodes WritePropertyValue(BacnetClient sender, BacnetAddress adr, BacnetPropertyValue value, bool writeFromNetwork)
        {
            this.sender = sender;
            return WritePropertyValue (value, writeFromNetwork);
        }

    }
}
