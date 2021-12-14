namespace System.IO.BACnet;

public abstract class BacnetTransportBase : IBacnetTransport
{
    public ILog Log { get; set; }
    public int HeaderLength { get; protected set; }
    public int MaxBufferLength { get; protected set; }
    public BacnetAddressTypes Type { get; protected set; }
    public BacnetMaxAdpu MaxAdpuLength { get; protected set; }
    public byte MaxInfoFrames { get; set; } = 0xFF;

    protected BacnetTransportBase()
    {
        Log = LogManager.GetLogger(GetType());
    }

    public abstract void Start();

    public abstract BacnetAddress GetBroadcastAddress();

    public virtual bool WaitForAllTransmits(int timeout)
    {
        return true; // not used 
    }

    public abstract int Send(byte[] buffer, int offset, int dataLength, BacnetAddress address, bool waitForTransmission, int timeout);

    public event MessageRecievedHandler MessageRecieved;

    protected void InvokeMessageRecieved(byte[] buffer, int offset, int msgLength, BacnetAddress remoteAddress)
    {
        MessageRecieved?.Invoke(this, buffer, offset, msgLength, remoteAddress);
    }

    public abstract void Dispose();
}
