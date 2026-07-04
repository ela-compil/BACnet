namespace System.IO.BACnet.Tests.Support;

/// <summary>
/// In-memory transport wiring two <see cref="BacnetClient"/> instances directly to each other:
/// frames handed to <see cref="Send"/> are delivered synchronously to the peer's MessageRecieved.
/// </summary>
internal class LoopbackTransport : IBacnetTransport
{
    private LoopbackTransport _peer;

    public byte MaxInfoFrames { get; set; } = 0xFF;
    public int HeaderLength => 0;
    public int MaxBufferLength => 1500;
    public BacnetAddressTypes Type => BacnetAddressTypes.None;
    public BacnetMaxAdpu MaxAdpuLength => BacnetMaxAdpu.MAX_APDU1476;

    public event MessageRecievedHandler MessageRecieved;

    public static (LoopbackTransport, LoopbackTransport) CreatePair()
    {
        var a = new LoopbackTransport();
        var b = new LoopbackTransport();
        a._peer = b;
        b._peer = a;
        return (a, b);
    }

    public void Start()
    {
    }

    public BacnetAddress GetBroadcastAddress() => new BacnetAddress(BacnetAddressTypes.None, 0, null);

    public bool WaitForAllTransmits(int timeout) => true;

    public int Send(byte[] buffer, int offset, int dataLength, BacnetAddress address, bool waitForTransmission, int timeout)
    {
        var frame = new byte[dataLength];
        Array.Copy(buffer, offset, frame, 0, dataLength);
        _peer.MessageRecieved?.Invoke(_peer, frame, 0, dataLength, new BacnetAddress(BacnetAddressTypes.None, 0, null));
        return dataLength;
    }

    public void Dispose()
    {
    }
}
