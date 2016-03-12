/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2014 Morten Kvistgaard <mk@pch-engineering.dk>
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
using System.Text;
using System.IO.BACnet;
using System.Reflection;
using System.Linq;

namespace System.IO.BACnet.Storage
{
    /// <summary>
    /// This is a basic example of a BACNet storage. This one is XML based. It has no fancy optimizing or anything.
    /// </summary>
    [Serializable]
    public class DeviceStorage
    {
        [System.Xml.Serialization.XmlIgnore]
        public uint DeviceId { get; set; }

        public delegate void ChangeOfValueHandler(DeviceStorage sender, BacnetObjectId object_id, BacnetPropertyIds property_id, uint array_index, IList<BacnetValue> value);
        public event ChangeOfValueHandler ChangeOfValue;
        public delegate void ReadOverrideHandler(BacnetObjectId object_id, BacnetPropertyIds property_id, uint array_index, out IList<BacnetValue> value, out ErrorCodes status, out bool handled);
        public event ReadOverrideHandler ReadOverride;
        public delegate void WriteOverrideHandler(BacnetObjectId object_id, BacnetPropertyIds property_id, uint array_index, IList<BacnetValue> value, out ErrorCodes status, out bool handled);
        public event WriteOverrideHandler WriteOverride;

        public Object[] Objects { get; set; }

        public DeviceStorage()
        {
            DeviceId = (uint)new Random().Next();
            Objects = new Object[0];
        }

        private Property FindProperty(BacnetObjectId object_id, BacnetPropertyIds property_id)
        {
            //liniear search
            Object obj = FindObject(object_id);
            return FindProperty(obj, property_id);
        }

        private Property FindProperty(Object obj, BacnetPropertyIds property_id)
        {
            //liniear search
            if (obj != null)
            {
                foreach (Property p in obj.Properties)
                {
                    if (p.Id == property_id)
                        return p;
                }
            }
            return null;
        }

        private Object FindObject(BacnetObjectTypes object_type)
        {
            //liniear search
            foreach (Object obj in Objects)
            {
                if (obj.Type == object_type)
                {
                    return obj;
                }
            }
            return null;
        }

        private Object FindObject(BacnetObjectId object_id)
        {
            //liniear search
            foreach (Object obj in Objects)
            {
                if (obj.Type == object_id.type && obj.Instance == object_id.instance)
                {
                    return obj;
                }
            }
            return null;
        }

        public enum ErrorCodes
        {
            Good = 0,
            GenericError = -1,
            NotExist = -2,
            NotForMe = -3,
            WriteAccessDenied = -4
        }

        public int ReadPropertyValue(BacnetObjectId object_id, BacnetPropertyIds property_id)
        {
            IList<BacnetValue> value;
            if (ReadProperty(object_id, property_id, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL, out value) == ErrorCodes.Good)
            {
                if (value == null || value.Count < 1)
                    return 0;
                return (int)Convert.ChangeType(value[0].Value, typeof(int));
            }
            else
                return 0;
        }

        public ErrorCodes ReadProperty(BacnetObjectId object_id, BacnetPropertyIds property_id, uint array_index, out IList<BacnetValue> value)
        {
            value = new BacnetValue[0];

            //wildcard device_id
            if (object_id.type == BacnetObjectTypes.OBJECT_DEVICE && object_id.instance >= System.IO.BACnet.Serialize.ASN1.BACNET_MAX_INSTANCE)
                object_id.instance = DeviceId;

            //overrides
            bool handled;
            ErrorCodes status;
            if (ReadOverride != null)
            {
                ReadOverride(object_id, property_id, array_index, out value, out status, out handled);
                if (handled) return status;
            }

            //find in storage
            Property p = FindProperty(object_id, property_id);
            if (p == null) return ErrorCodes.NotExist;

            //get value ... check for array index
            if (array_index == 0)
            {
                value = new BacnetValue[] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT, (uint)p.BacnetValue.Count) };
            }
            else if (array_index != System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL)
            {
                value = new BacnetValue[] { p.BacnetValue[(int)array_index - 1] };
            }
            else
            {
                value = p.BacnetValue;
            }

            return ErrorCodes.Good;
        }

        public void ReadPropertyMultiple(BacnetObjectId object_id, ICollection<BacnetPropertyReference> properties, out IList<BacnetPropertyValue> values)
        {
            BacnetPropertyValue[] values_ret = new BacnetPropertyValue[properties.Count];

            int count = 0;
            foreach (BacnetPropertyReference entry in properties)
            {
                BacnetPropertyValue new_entry = new BacnetPropertyValue();
                new_entry.property = entry;
                if (ReadProperty(object_id, (BacnetPropertyIds)entry.propertyIdentifier, entry.propertyArrayIndex, out new_entry.value) != ErrorCodes.Good)
                    new_entry.value = new BacnetValue[] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ERROR, new BacnetError(BacnetErrorClasses.ERROR_CLASS_OBJECT, BacnetErrorCodes.ERROR_CODE_UNKNOWN_PROPERTY)) };
                values_ret[count] = new_entry;
                count++;
            }

            values = values_ret;
        }

        public bool ReadPropertyAll(BacnetObjectId object_id, out IList<BacnetPropertyValue> values)
        {
            values = null;

            //find
            Object obj = FindObject(object_id);
            if (obj == null) return false;

            //build
            ErrorCodes[] ret = new ErrorCodes[obj.Properties.Length];
            BacnetPropertyValue[] _values = new BacnetPropertyValue[obj.Properties.Length];
            for (int i = 0; i < obj.Properties.Length; i++)
            {
                BacnetPropertyValue new_entry = new BacnetPropertyValue();
                new_entry.property = new BacnetPropertyReference((uint)obj.Properties[i].Id, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL);
                if (ReadProperty(object_id, obj.Properties[i].Id, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL, out new_entry.value) != ErrorCodes.Good)
                    new_entry.value = new BacnetValue[] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ERROR, new BacnetError(BacnetErrorClasses.ERROR_CLASS_OBJECT, BacnetErrorCodes.ERROR_CODE_UNKNOWN_PROPERTY)) };
                _values[i] = new_entry;
            }
            values = _values;

            return true;
        }

        public void WritePropertyValue(BacnetObjectId object_id, BacnetPropertyIds property_id, int value)
        {
            IList<BacnetValue> read_values;

            //get existing type
            if (ReadProperty(object_id, property_id, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL, out read_values) != ErrorCodes.Good)
                return;
            if (read_values == null || read_values.Count == 0)
                return;

            //write
            BacnetValue[] write_values = new BacnetValue[] { new BacnetValue(read_values[0].Tag, Convert.ChangeType(value, read_values[0].Value.GetType())) };
            WriteProperty(object_id, property_id, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL, write_values);
        }

        public ErrorCodes WriteProperty(BacnetObjectId object_id, BacnetPropertyIds property_id, uint array_index, IList<BacnetValue> value, bool add_if_not_exits = false)
        {
            //wildcard device_id
            if (object_id.type == BacnetObjectTypes.OBJECT_DEVICE && object_id.instance >= System.IO.BACnet.Serialize.ASN1.BACNET_MAX_INSTANCE)
                object_id.instance = DeviceId;

            //overrides
            bool handled;
            ErrorCodes status;
            if (WriteOverride != null)
            {
                WriteOverride(object_id, property_id, array_index, value, out status, out handled);
                if (handled) return status;
            }

            //find
            Property p = FindProperty(object_id, property_id);
            if (p == null)
            {
                if (!add_if_not_exits) return ErrorCodes.NotExist;

                //add obj
                Object obj = FindObject(object_id);
                if (obj == null)
                {
                    obj = new Object();
                    obj.Type = object_id.type;
                    obj.Instance = object_id.instance;
                    Object[] arr = Objects;
                    Array.Resize<Object>(ref arr, arr.Length + 1);
                    arr[arr.Length - 1] = obj;
                    Objects = arr;
                }

                //add property
                p = new Property();
                p.Id = property_id;
                Property[] props = obj.Properties;
                Array.Resize<Property>(ref props, props.Length + 1);
                props[props.Length - 1] = p;
                obj.Properties = props;
            }

            //set type if needed
            if (p.Tag == BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL && value != null)
            {
                foreach (BacnetValue v in value)
                {
                    if (v.Tag != BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL)
                    {
                        p.Tag = v.Tag;
                        break;
                    }
                }
            }

            //write
            p.BacnetValue = value;

            //send event ... for subscriptions
            if (ChangeOfValue != null) ChangeOfValue(this, object_id, property_id, array_index, value);

            return ErrorCodes.Good;
        }

        // Write PROP_PRESENT_VALUE or PROP_RELINQUISH_DEFAULT in an object with a 16 level PROP_PRIORITY_ARRAY (BACNET_APPLICATION_TAG_NULL)
        public ErrorCodes WriteCommandableProperty(BacnetObjectId object_id, BacnetPropertyIds property_id, BacnetValue value, uint priority)
        {

            if (!(property_id == BacnetPropertyIds.PROP_PRESENT_VALUE))
                return DeviceStorage.ErrorCodes.NotForMe;

            Property p_presentvalue = FindProperty(object_id, BacnetPropertyIds.PROP_PRESENT_VALUE);
            if (p_presentvalue == null)
                return DeviceStorage.ErrorCodes.NotForMe;

            Property p_relinquish = FindProperty(object_id, BacnetPropertyIds.PROP_RELINQUISH_DEFAULT);
            if (p_relinquish == null)
                return DeviceStorage.ErrorCodes.NotForMe;

            Property p_outofservice = FindProperty(object_id, BacnetPropertyIds.PROP_OUT_OF_SERVICE);
            if (p_outofservice == null)
                return DeviceStorage.ErrorCodes.NotForMe;

            Property p_array = FindProperty(object_id, BacnetPropertyIds.PROP_PRIORITY_ARRAY);
            if (p_array == null)
                return DeviceStorage.ErrorCodes.NotForMe;

            DeviceStorage.ErrorCodes errorcode = DeviceStorage.ErrorCodes.GenericError;

            try
            {
                // If PROP_OUT_OF_SERVICE=True, value is accepted as is : http://www.bacnetwiki.com/wiki/index.php?title=Priority_Array                 
                if (((bool)p_outofservice.BacnetValue[0].Value == true) && (property_id == BacnetPropertyIds.PROP_PRESENT_VALUE))
                {
                    p_presentvalue.BacnetValue = new BacnetValue[1] { value };
                    return DeviceStorage.ErrorCodes.Good;
                }

                IList<BacnetValue> valueArray = null;

                //
                //http://www.chipkin.com/changing-the-bacnet-present-value-or-why-the-present-value-doesn%E2%80%99t-change/
                //
                // Write Property PROP_PRESENT_VALUE : A value is placed in the PROP_PRIORITY_ARRAY
                if (property_id == BacnetPropertyIds.PROP_PRESENT_VALUE)
                {
                    errorcode = DeviceStorage.ErrorCodes.Good;

                    valueArray = p_array.BacnetValue;
                    if (value.Value == null)
                        valueArray[(int)priority - 1] = new BacnetValue(null);
                    else
                        valueArray[(int)priority - 1] = value;
                    p_array.BacnetValue = valueArray;
                }

                // Look on the priority Array to find the first value to be set in PROP_PRESENT_VALUE
                if (errorcode == DeviceStorage.ErrorCodes.Good)
                {

                    bool done = false;
                    for (int i = 0; i < 16; i++)
                    {
                        if (valueArray[i].Value != null)    // A value is OK
                        {
                            p_presentvalue.BacnetValue = new BacnetValue[1] { valueArray[i] };

                            done = true;
                            break;
                        }
                    }
                    if (done == false)  // Nothing in the array : PROP_PRESENT_VALUE = PROP_RELINQUISH_DEFAULT
                    {
                        IList<BacnetValue> DefaultValue = p_relinquish.BacnetValue;
                        p_presentvalue.BacnetValue = DefaultValue;
                    }
                }
            }
            catch
            {
                errorcode = DeviceStorage.ErrorCodes.GenericError;
            }

            return errorcode;
        }



        public ErrorCodes[] WritePropertyMultiple(BacnetObjectId object_id, ICollection<BacnetPropertyValue> values)
        {
            ErrorCodes[] ret = new ErrorCodes[values.Count];

            int count = 0;
            foreach (BacnetPropertyValue entry in values)
            {
                ret[count] = WriteProperty(object_id, (BacnetPropertyIds)entry.property.propertyIdentifier, entry.property.propertyArrayIndex, entry.value);
                count++;
            }

            return ret;
        }

        /// <summary>
        /// Store the class, as XML file
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            System.Xml.Serialization.XmlSerializer s = new System.Xml.Serialization.XmlSerializer(typeof(DeviceStorage));
            using (System.IO.FileStream fs = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                s.Serialize(fs, this);
            }
        }

        /// <summary>
        /// Load XML values into class
        /// </summary>
        /// <param name="path">Embedded or external file</param>
        /// <param name="Deviceid">Optional deviceId other than the one in the Xml file</param>
        /// <returns></returns>
        public static DeviceStorage Load(string path, uint? Deviceid = null)
        {
            Assembly _assembly;
            StreamReader _textStreamReader;

            _assembly = Assembly.GetExecutingAssembly();
            try
            {
                // check if the xml file is an embedded resource
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream(path));
            }
            catch
            {
                // if not check the external file
                if (!System.IO.File.Exists(path)) throw new Exception("No AppSettings found");
                _textStreamReader = new StreamReader(path);
            }

            System.Xml.Serialization.XmlSerializer s = new System.Xml.Serialization.XmlSerializer(typeof(DeviceStorage));
            using (_textStreamReader)
            {
                DeviceStorage ret = (DeviceStorage)s.Deserialize(_textStreamReader);

                //set device_id
                Object obj = ret.FindObject(BacnetObjectTypes.OBJECT_DEVICE);
                if (obj != null)
                    ret.DeviceId = obj.Instance;

                // use the deviceId in the Xml file or another one
                if (Deviceid.HasValue)
                {
                    ret.DeviceId = Deviceid.Value;
                    if (obj != null)
                    {
                        // change the value
                        obj.Instance = Deviceid.Value;
                        IList<BacnetValue> val = new BacnetValue[1] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID, "OBJECT_DEVICE:" + Deviceid.Value.ToString()) };
                        ret.WriteProperty(new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, System.IO.BACnet.Serialize.ASN1.BACNET_MAX_INSTANCE), BacnetPropertyIds.PROP_OBJECT_IDENTIFIER, 1, val, true);
                    }
                }
                return ret;
            }
        }
    }

    [Serializable]
    public class Object
    {
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public BacnetObjectTypes Type { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint Instance { get; set; }

        public Property[] Properties { get; set; }

        public Object()
        {
            Properties = new Property[0];
        }
    }

    [Serializable]
    public class Property
    {
        [System.Xml.Serialization.XmlIgnore]
        public BacnetPropertyIds Id { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute("Id")]
        public string IdText
        {
            get
            {
                return Id.ToString();
            }
            set
            {
                Id = (BacnetPropertyIds)Enum.Parse((typeof(BacnetPropertyIds)), value);
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public BacnetApplicationTags Tag { get; set; }

        [System.Xml.Serialization.XmlElement]
        public string[] Value { get; set; }

        public static BacnetValue DeserializeValue(string value, BacnetApplicationTags type)
        {
            switch (type)
            {
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL:
                    if (value == "")
                        return new BacnetValue(type, null); // Modif FC
                    else
                        return new BacnetValue(value);
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN:
                    return new BacnetValue(type, bool.Parse(value));
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT:
                    return new BacnetValue(type, uint.Parse(value));
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT:
                    return new BacnetValue(type, int.Parse(value));
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL:
                    return new BacnetValue(type, float.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE:
                    return new BacnetValue(type, double.Parse(value, System.Globalization.CultureInfo.InvariantCulture));
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING:
                    try
                    {
                        return new BacnetValue(type, Convert.FromBase64String(value));
                    }
                    catch
                    {
                        return new BacnetValue(type, value);
                    }
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC_DECODED:
                    try
                    {
                        return new BacnetValue(type, Convert.FromBase64String(value));
                    }
                    catch
                    {
                        return new BacnetValue(type, value);
                    }
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING:
                    return new BacnetValue(type, value);
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING:
                    return new BacnetValue(type, BacnetBitString.Parse(value));
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED:
                    return new BacnetValue(type, uint.Parse(value));
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_DATE:
                    return new BacnetValue(type, DateTime.Parse(value));
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_TIME:
                    return new BacnetValue(type, DateTime.Parse(value));
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID:
                    return new BacnetValue(type, BacnetObjectId.Parse(value));
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_READ_ACCESS_SPECIFICATION:
                    return new BacnetValue(type, BacnetReadAccessSpecification.Parse(value));
                default:
                    return new BacnetValue(type, null);
            }
        }

        public static string SerializeValue(BacnetValue value, BacnetApplicationTags type)
        {
            switch (type)
            {
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL:
                    return value.ToString(); // Modif FC
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL:
                    return ((float)value.Value).ToString(System.Globalization.CultureInfo.InvariantCulture);
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE:
                    return ((double)value.Value).ToString(System.Globalization.CultureInfo.InvariantCulture);
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_OCTET_STRING:
                    return Convert.ToBase64String((byte[])value.Value);
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC_DECODED:
                    {
                        if (value.Value is byte[])
                        {
                            return Convert.ToBase64String((byte[])value.Value);
                        }
                        else
                        {
                            string ret = "";
                            BacnetValue[] arr = (BacnetValue[])value.Value;
                            foreach (BacnetValue v in arr)
                            {
                                ret += ";" + SerializeValue(v, v.Tag);
                            }
                            return ret.Length > 0 ? ret.Substring(1) : "";
                        }
                    }
                default:
                    return value.Value.ToString();
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public IList<BacnetValue> BacnetValue
        {
            get
            {
                if (Value == null) return new BacnetValue[0];
                BacnetValue[] ret = new BacnetValue[Value.Length];
                for (int i = 0; i < ret.Length; i++)
                {
                    ret[i] = DeserializeValue(Value[i], Tag);
                }
                return ret;
            }
            set
            {
                //count
                int count = 0;
                foreach(BacnetValue v in value)
                    count++;

                string[] str_values = new string[count];
                count = 0;
                foreach (BacnetValue v in value)
                {
                    str_values[count] = SerializeValue(v, Tag);
                    count++;
                }
                Value = str_values;
            }
        }
    }
}
