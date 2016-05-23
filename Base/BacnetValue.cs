using System.Linq;

namespace System.IO.BACnet
{
    public struct BacnetValue
    {
        public BacnetApplicationTags Tag;
        public object Value;

        public BacnetValue(BacnetApplicationTags tag, object value)
        {
            Tag = tag;
            Value = value;
        }

        public BacnetValue(object value)
        {
            Value = value;
            Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL;

            //guess at the tag
            if (value != null)
                Tag=TagFromType(value.GetType());                           
        }

        public BacnetApplicationTags TagFromType(Type t)
        {
            if (t == typeof(string))
                return BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING;
            if (t == typeof(int) || t == typeof(short) || t == typeof(sbyte))
                return BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT;
            if (t == typeof(uint) || t == typeof(ushort) || t == typeof(byte))
                return BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT;
            if (t == typeof(bool))
                return BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN;
            if (t == typeof(float))
                return BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL;
            if (t == typeof(double))
                return BacnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE;
            if (t == typeof(BacnetBitString))
                return BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING;
            if (t == typeof(BacnetObjectId))
                return BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID;

            return BacnetApplicationTags.BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC_ENCODED;
        }

        public override string ToString()
        {
            if (Value == null)
                return string.Empty;

            if (Value.GetType() != typeof(byte[]))
                return Value.ToString();

            var tmp = (byte[])Value;
            return tmp.Aggregate(string.Empty, (current, b) =>
                current + b.ToString("X2"));
        }
    }
}