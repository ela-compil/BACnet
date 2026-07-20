namespace System.IO.BACnet;

/// <summary>
/// The result of a confirmed PrivateTransfer request: the vendor and service the device echoed back
/// and the vendor-specific result payload (ASHRAE 135 §16.2).
/// </summary>
public readonly struct BacnetPrivateTransferResult
{
    public BacnetPrivateTransferResult(uint vendorId, uint serviceNumber, byte[] resultBlock)
    {
        VendorId = vendorId;
        ServiceNumber = serviceNumber;
        ResultBlock = resultBlock;
    }

    /// <summary>
    /// The vendor identifier echoed by the device.
    /// </summary>
    public uint VendorId { get; }

    /// <summary>
    /// The service number echoed by the device.
    /// </summary>
    public uint ServiceNumber { get; }

    /// <summary>
    /// The vendor-specific result payload; may be null when the device returns none.
    /// </summary>
    public byte[] ResultBlock { get; }
}
