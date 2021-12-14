namespace System.IO.BACnet;

public struct BacnetGenericTime
{
    public BacnetTimestampTags Tag;
    public DateTime Time;
    public ushort Sequence;

    public BacnetGenericTime(DateTime time, BacnetTimestampTags tag, ushort sequence = 0)
    {
        Time = time;
        Tag = tag;
        Sequence = sequence;
    }

    public override string ToString()
    {
        return $"{Time}";
    }
}
