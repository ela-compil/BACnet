using System.Linq;

namespace System.IO.BACnet.Tests
{
    class InMemoryTransport : IBacnetTransport
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public byte MaxInfoFrames { get; set; }
        public int HeaderLength { get; }
        public int MaxBufferLength => 1024;
        public BacnetAddressTypes Type => BacnetAddressTypes.None;
        public BacnetMaxAdpu MaxAdpuLength { get; }
        public void Start()
        {
            // nothing to do
        }

        public BacnetAddress GetBroadcastAddress()
            => new BacnetAddress(Type, 0, new byte[] {42});

        public bool WaitForAllTransmits(int timeout)
        {
            throw new NotImplementedException();
        }

        public int Send(byte[] buffer, int offset, int dataLength, BacnetAddress address, bool waitForTransmission, int timeout)
        {
            BytesSent?.Invoke(address, buffer.Skip(offset).Take(dataLength).ToArray());
            return dataLength;
        }

        public void ReceiveBytes(BacnetAddress address, byte[] bytes)
            => MessageRecieved?.Invoke(this, bytes, 0, bytes.Length, address);

        public event MessageRecievedHandler MessageRecieved;

        public event Action<BacnetAddress, byte[]> BytesSent;
    }
}
