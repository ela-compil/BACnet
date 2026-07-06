namespace System.IO.BACnet;

public struct BacnetGenericTime
{
    public BacnetTimestampTags Tag;
    public DateTime Time;
    public ushort Sequence;

    /// <summary>
    /// Set by the decoders when the timestamp's time octets are not fully specified
    /// (135 §20.2.13): <see cref="Time"/> then holds the usual best-effort clamped value while
    /// this keeps the original octets, so re-encoding the timestamp (e.g. echoing it back in an
    /// AcknowledgeAlarm) reproduces the wire bytes instead of the clamped time.
    /// </summary>
    public BacnetTime? PartialTime;

    public BacnetGenericTime(DateTime time, BacnetTimestampTags tag, ushort sequence = 0)
    {
        Time = time;
        Tag = tag;
        Sequence = sequence;
        PartialTime = null;
    }

    public override string ToString()
    {
        return PartialTime != null ? $"{PartialTime}" : $"{Time}";
    }
}
