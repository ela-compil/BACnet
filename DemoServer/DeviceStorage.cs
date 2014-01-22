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

namespace System.IO.BACnet.Storage
{
    [Serializable]
    public class DeviceStorage
    {
        [System.Xml.Serialization.XmlIgnore]
        public uint DeviceId { get; set; }

        public delegate void ChangeOfValueHandler(DeviceStorage sender, BACNET_OBJECT_ID object_id, BACNET_PROPERTY_ID property_id, uint array_index, IList<BACNET_VALUE> value);
        public event ChangeOfValueHandler ChangeOfValue;
        public delegate void ReadOverrideHandler(BACNET_OBJECT_ID object_id, BACNET_PROPERTY_ID property_id, uint array_index, out IList<BACNET_VALUE> value, out ErrorCodes status, out bool handled);
        public event ReadOverrideHandler ReadOverride;
        public delegate void WriteOverrideHandler(BACNET_OBJECT_ID object_id, BACNET_PROPERTY_ID property_id, uint array_index, IList<BACNET_VALUE> value, out ErrorCodes status, out bool handled);
        public event WriteOverrideHandler WriteOverride;

        public Object[] Objects { get; set; }

        public DeviceStorage()
        {
            DeviceId = (uint)new Random().Next();
            Objects = new Object[0];
        }

        private Property FindProperty(BACNET_OBJECT_ID object_id, BACNET_PROPERTY_ID property_id)
        {
            //liniear search
            Object obj = FindObject(object_id);
            return FindProperty(obj, property_id);
        }

        private Property FindProperty(Object obj, BACNET_PROPERTY_ID property_id)
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

        private Object FindObject(BACNET_OBJECT_TYPE object_type)
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

        private Object FindObject(BACNET_OBJECT_ID object_id)
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
        }

        public int ReadPropertyValue(BACNET_OBJECT_ID object_id, BACNET_PROPERTY_ID property_id)
        {
            IList<BACNET_VALUE> value;
            if (ReadProperty(object_id, property_id, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL, out value) == ErrorCodes.Good)
            {
                if(value == null || value.Count < 1)
                    return 0;
                return (int)Convert.ChangeType(value[0].Value, typeof(int));
            }
            else
                return 0;
        }

        public ErrorCodes ReadProperty(BACNET_OBJECT_ID object_id, BACNET_PROPERTY_ID property_id, uint array_index, out IList<BACNET_VALUE> value)
        {
            value = new BACNET_VALUE[0];

            //wildcard device_id
            if (object_id.type == BACNET_OBJECT_TYPE.OBJECT_DEVICE && object_id.instance >= System.IO.BACnet.Serialize.ASN1.BACNET_MAX_INSTANCE)
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
            value = p.BacnetValue;

            return ErrorCodes.Good;
        }

        public ErrorCodes[] ReadPropertyMultiple(BACNET_OBJECT_ID object_id, ICollection<BACNET_PROPERTY_REFERENCE> properties, out IList<BACNET_PROPERTY_VALUE> values)
        {
            BACNET_PROPERTY_VALUE[] values_ret = new BACNET_PROPERTY_VALUE[properties.Count];
            ErrorCodes[] ret = new ErrorCodes[properties.Count];

            int count = 0;
            foreach (BACNET_PROPERTY_REFERENCE entry in properties)
            {
                BACNET_PROPERTY_VALUE new_entry = new BACNET_PROPERTY_VALUE();
                new_entry.property = entry;
                ret[count] = ReadProperty(object_id, (BACNET_PROPERTY_ID)entry.propertyIdentifier, entry.propertyArrayIndex, out new_entry.value);
                values_ret[count] = new_entry;
                count++;
            }

            values = values_ret;
            return ret;
        }

        public ErrorCodes[] ReadPropertyAll(BACNET_OBJECT_ID object_id, out IList<BACNET_PROPERTY_VALUE> values)
        {
            values = null;

            //find
            Object obj = FindObject(object_id);
            if (obj == null) return new ErrorCodes[] { ErrorCodes.NotExist };

            //build
            ErrorCodes[] ret = new ErrorCodes[obj.Properties.Length];
            BACNET_PROPERTY_VALUE[] _values = new BACNET_PROPERTY_VALUE[obj.Properties.Length];
            for (int i = 0; i < obj.Properties.Length; i++)
            {
                BACNET_PROPERTY_VALUE new_entry = new BACNET_PROPERTY_VALUE();
                new_entry.property = new BACNET_PROPERTY_REFERENCE((uint)obj.Properties[i].Id, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL);
                ReadProperty(object_id, obj.Properties[i].Id, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL, out new_entry.value);
                _values[i] = new_entry;
            }
            values = _values;

            return ret;
        }

        public void WritePropertyValue(BACNET_OBJECT_ID object_id, BACNET_PROPERTY_ID property_id, int value)
        {
            IList<BACNET_VALUE> read_values;

            //get existing type
            if (ReadProperty(object_id, property_id, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL, out read_values) != ErrorCodes.Good)
                return;
            if (read_values == null || read_values.Count == 0)
                return;

            //write
            BACNET_VALUE[] write_values = new BACNET_VALUE[]{new BACNET_VALUE(read_values[0].Tag, Convert.ChangeType(value, read_values[0].Value.GetType()))};
            WriteProperty(object_id, property_id, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL, write_values);
        }

        public ErrorCodes WriteProperty(BACNET_OBJECT_ID object_id, BACNET_PROPERTY_ID property_id, uint array_index, IList<BACNET_VALUE> value, bool add_if_not_exits = false)
        {
            //wildcard device_id
            if (object_id.type == BACNET_OBJECT_TYPE.OBJECT_DEVICE && object_id.instance >= System.IO.BACnet.Serialize.ASN1.BACNET_MAX_INSTANCE)
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
                if(!add_if_not_exits) return ErrorCodes.NotExist;

                //add obj
                Object obj = FindObject(object_id);
                if (obj == null)
                {
                    obj = new Object();
                    obj.Type = object_id.type;
                    obj.Instance = object_id.instance;
                    Object[] arr = Objects;
                    Array.Resize<Object>(ref arr, arr.Length + 1);
                    arr[arr.Length -1] = obj;
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
            if (p.Tag == BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_NULL && value != null)
            {
                foreach (BACNET_VALUE v in value)
                {
                    if (v.Tag != BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_NULL)
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

        public ErrorCodes[] WritePropertyMultiple(BACNET_OBJECT_ID object_id, ICollection<BACNET_PROPERTY_VALUE> values)
        {
            ErrorCodes[] ret = new ErrorCodes[values.Count];

            int count = 0;
            foreach (BACNET_PROPERTY_VALUE entry in values)
            {
                ret[count] = WriteProperty(object_id, (BACNET_PROPERTY_ID)entry.property.propertyIdentifier, entry.property.propertyArrayIndex, entry.value);
                count++;
            }

            return ret;
        }

        public void Save(string path)
        {
            System.Xml.Serialization.XmlSerializer s = new System.Xml.Serialization.XmlSerializer(typeof(DeviceStorage));
            using (System.IO.FileStream fs = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                s.Serialize(fs, this);
            }
        }

        public static DeviceStorage Load(string path)
        {
            if (!System.IO.File.Exists(path)) throw new Exception("No AppSettings found");
            System.Xml.Serialization.XmlSerializer s = new System.Xml.Serialization.XmlSerializer(typeof(DeviceStorage));
            using (System.IO.FileStream fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                DeviceStorage ret = (DeviceStorage)s.Deserialize(fs);

                //set device_id
                Object obj = ret.FindObject(BACNET_OBJECT_TYPE.OBJECT_DEVICE);
                if (obj != null)
                    ret.DeviceId = obj.Instance;

                return ret;
            }
        }
        
    }

    [Serializable]
    public class Object
    {
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public BACNET_OBJECT_TYPE Type { get; set; }

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
        public BACNET_PROPERTY_ID Id { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute("Id")]
        public string IdText
        {
            get
            {
                return Id.ToString();
            }
            set
            {
                Id = (BACNET_PROPERTY_ID)Enum.Parse((typeof(BACNET_PROPERTY_ID)), value);
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public BACNET_APPLICATION_TAG Tag { get; set; }

        [System.Xml.Serialization.XmlElement]
        public string[] Value { get; set; }

        private BACNET_VALUE DeserializeValue(string value, BACNET_APPLICATION_TAG type)
        {
            switch (type)
            {
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_NULL:
                    return new BACNET_VALUE(type, null);
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_BOOLEAN:
                    return new BACNET_VALUE(type, bool.Parse(value));
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_UNSIGNED_INT:
                    return new BACNET_VALUE(type, uint.Parse(value));
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_SIGNED_INT:
                    return new BACNET_VALUE(type, int.Parse(value));
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_REAL:
                    return new BACNET_VALUE(type, float.Parse(value));
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_DOUBLE:
                    return new BACNET_VALUE(type, double.Parse(value));
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OCTET_STRING:
                    return new BACNET_VALUE(type, Convert.FromBase64String(value));
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC_DECODED:
                    return new BACNET_VALUE(type, Convert.FromBase64String(value));
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_CHARACTER_STRING:
                    return new BACNET_VALUE(type, value);
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_BIT_STRING:
                    return new BACNET_VALUE(type, BACNET_BIT_STRING.Parse(value));
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_ENUMERATED:
                    return new BACNET_VALUE(type, uint.Parse(value));
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_DATE:
                    return new BACNET_VALUE(type, DateTime.Parse(value));
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_TIME:
                    return new BACNET_VALUE(type, DateTime.Parse(value));
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OBJECT_ID:
                    return new BACNET_VALUE(type, BACNET_OBJECT_ID.Parse(value));
                default:
                    return new BACNET_VALUE(type, null);
            }
        }

        private string SerializeValue(BACNET_VALUE value, BACNET_APPLICATION_TAG type)
        {
            switch (type)
            {
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_NULL:
                    return "";
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_REAL:
                    return ((float)value.Value).ToString(System.Globalization.CultureInfo.InvariantCulture);
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_DOUBLE:
                    return ((double)value.Value).ToString(System.Globalization.CultureInfo.InvariantCulture);
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_OCTET_STRING:
                    return Convert.ToBase64String((byte[])value.Value);
                case BACNET_APPLICATION_TAG.BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC_DECODED:
                    {
                        if (value.Value is byte[])
                        {
                            return Convert.ToBase64String((byte[])value.Value);
                        }
                        else
                        {
                            string ret = "";
                            BACNET_VALUE[] arr = (BACNET_VALUE[])value.Value;
                            foreach (BACNET_VALUE v in arr)
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
        public IList<BACNET_VALUE> BacnetValue
        {
            get
            {
                if (Value == null) return new BACNET_VALUE[0];
                BACNET_VALUE[] ret = new BACNET_VALUE[Value.Length];
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
                foreach(BACNET_VALUE v in value)
                    count++;

                string[] str_values = new string[count];
                count = 0;
                foreach (BACNET_VALUE v in value)
                {
                    str_values[count] = SerializeValue(v, Tag);
                    count++;
                }
                Value = str_values;
            }
        }
    }
}
