namespace System.IO.BACnet;

[Serializable]
public struct BacnetObjectId : IComparable<BacnetObjectId>
{
    public BacnetObjectTypes type;
    public uint instance;

    public BacnetObjectTypes Type
    {
        get => type;
        set => type = value;
    }

    public uint Instance
    {
        get => instance;
        set => instance = value;
    }

    public BacnetObjectId(BacnetObjectTypes type, uint instance)
    {
        this.type = type;
        this.instance = instance;
    }

    public override string ToString()
    {
        return $"{Type}:{Instance}";
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
        if (Type == other.Type)
            return Instance.CompareTo(other.Instance);

        if (Type == BacnetObjectTypes.OBJECT_DEVICE)
            return -1;

        if (other.Type == BacnetObjectTypes.OBJECT_DEVICE)
            return 1;

        // cast to int for comparison otherwise unpredictable behaviour with outbound enum (proprietary type)
        return ((int)Type).CompareTo((int)other.Type);
    }

    public static bool operator ==(BacnetObjectId a, BacnetObjectId b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(BacnetObjectId a, BacnetObjectId b)
    {
        return !(a == b);
    }

    public static BacnetObjectId Parse(string value)
    {
        var pattern = new Regex($"(?<{nameof(Type)}>.+):(?<{nameof(Instance)}>.+)");

        if (string.IsNullOrEmpty(value) || !pattern.IsMatch(value))
            return new BacnetObjectId();

        var objectType = (BacnetObjectTypes)Enum.Parse(typeof(BacnetObjectTypes),
            pattern.Match(value).Groups[nameof(Type)].Value);

        var objectInstance = uint.Parse(pattern.Match(value).Groups[nameof(Instance)].Value);

        return new BacnetObjectId(objectType, objectInstance);
    }

};
