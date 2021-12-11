namespace System.IO.BACnet;

public interface IBacnetTransport : IDisposable
{
    byte MaxInfoFrames { get; set; }
    int HeaderLength { get; }
    int MaxBufferLength { get; }
    BacnetAddressTypes Type { get; }
    BacnetMaxAdpu MaxAdpuLength { get; }

    void Start();
    BacnetAddress GetBroadcastAddress();
    bool WaitForAllTransmits(int timeout);
    int Send(byte[] buffer, int offset, int dataLength, BacnetAddress address, bool waitForTransmission, int timeout);

    event MessageRecievedHandler MessageRecieved;
}
