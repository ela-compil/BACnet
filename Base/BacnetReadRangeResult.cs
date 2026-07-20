namespace System.IO.BACnet;

/// <summary>
/// The result of a ReadRange request: the encoded application data for the returned items and how
/// many items were returned (ASHRAE 135 §15.8).
/// </summary>
public readonly struct BacnetReadRangeResult
{
    public BacnetReadRangeResult(byte[] range, uint itemCount)
    {
        Range = range;
        ItemCount = itemCount;
    }

    /// <summary>The encoded application data of the returned items.</summary>
    public byte[] Range { get; }

    /// <summary>The number of items returned (may be fewer than requested).</summary>
    public uint ItemCount { get; }
}
