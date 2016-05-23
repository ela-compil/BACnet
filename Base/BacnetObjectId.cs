namespace System.IO.BACnet
{
    [Serializable]
    public struct BacnetObjectId : IComparable<BacnetObjectId>
    {
        public BacnetObjectTypes type;
        public uint instance;
        public BacnetObjectId(BacnetObjectTypes type, uint instance)
        {
            this.type = type;
            this.instance = instance;
        }
        public BacnetObjectTypes Type
        {
            get { return type; }
            set { type = value; }
        }
        public uint Instance
        {
            get { return instance; }
            set { instance = value; }
        }
        public override string ToString()
        {
            return $"{type}:{instance}";
        }
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return obj != null && obj.ToString().Equals(ToString());
        }

        public int CompareTo(BacnetObjectId other)
        {
            if (type == BacnetObjectTypes.OBJECT_DEVICE) return -1;
            if (other.type == BacnetObjectTypes.OBJECT_DEVICE) return 1;

            if (type == other.type)
                return instance.CompareTo(other.instance);

            // cast to int for comparison otherwise unpredictable behaviour with outbound enum (proprietary type)
            return ((int)type).CompareTo((int)other.type);
        }
        public static BacnetObjectId Parse(string value)
        {
            var ret = new BacnetObjectId();
            if (string.IsNullOrEmpty(value)) return ret;
            var p = value.IndexOf(":");
            if (p < 0) return ret;
            var strType = value.Substring(0, p);
            var strInstance = value.Substring(p + 1);
            ret.type = (BacnetObjectTypes)Enum.Parse(typeof(BacnetObjectTypes), strType);
            ret.instance = uint.Parse(strInstance);
            return ret;
        }

    };
}