namespace System.IO.BACnet;

public struct BacnetPropertyValue
{
    public BacnetPropertyReference property;
    public IList<BacnetValue> value;
    public byte priority;

    public override string ToString()
    {
        return property.ToString();
    }
}
