using System.Collections.Generic;
using System.IO.BACnet.Tests.Support;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// The asynchronous request methods must be genuinely non-blocking and must correlate every reply to
/// its own request by invoke-id, so that many confirmed requests can be in flight on a single client
/// at once without one caller receiving another's value (the failure reported in issue #46).
/// </summary>
public class BacnetClientAsyncTests
{
    private static readonly BacnetAddress Anywhere = new(BacnetAddressTypes.None, 0, null);

    // Answers each ReadProperty by echoing the requested object instance back as the value, so a
    // reply delivered to the wrong waiter is detectable: the value would not match the instance asked
    // for. Segmentation is left null - the payload is a single unsigned integer.
    private static void AnswerWithInstance(BacnetClient sender, BacnetAddress adr, byte invokeId,
        BacnetObjectId objectId, BacnetPropertyReference property, BacnetMaxSegments maxSegments)
    {
        var value = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT, (uint)objectId.instance);
        sender.ReadPropertyResponse(adr, invokeId, null, objectId, property, new[] { value });
    }

    [Fact]
    public async Task ReadPropertyAsync_returns_the_devices_value()
    {
        var (clientTransport, deviceTransport) = LoopbackTransport.CreatePair();
        using var client = new BacnetClient(clientTransport);
        using var device = new BacnetClient(deviceTransport);

        device.OnReadPropertyRequest += AnswerWithInstance;
        client.Start();
        device.Start();

        var values = await client.ReadPropertyAsync(Anywhere,
            new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 42), BacnetPropertyIds.PROP_PRESENT_VALUE);

        Assert.Equal(42u, Convert.ToUInt32(values.Single().Value));
    }

    [Fact]
    public async Task ReadPropertyAsync_throws_the_devices_error()
    {
        var (clientTransport, deviceTransport) = LoopbackTransport.CreatePair();
        using var client = new BacnetClient(clientTransport);
        using var device = new BacnetClient(deviceTransport);

        device.OnReadPropertyRequest += (sender, adr, invokeId, objectId, property, maxSegments) =>
            sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, invokeId,
                BacnetErrorClasses.ERROR_CLASS_PROPERTY, BacnetErrorCodes.ERROR_CODE_UNKNOWN_PROPERTY);
        client.Start();
        device.Start();

        var ex = await Assert.ThrowsAnyAsync<Exception>(() => client.ReadPropertyAsync(Anywhere,
            new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 1), BacnetPropertyIds.PROP_PRESENT_VALUE));

        Assert.Contains("UNKNOWN_PROPERTY", ex.Message);
    }

    [Fact]
    public async Task Concurrent_reads_each_receive_their_own_reply()
    {
        // Replies arrive on background threads with jitter, so they complete out of order and many
        // requests overlap - the conditions under which a byte invoke-id or a shared wait handle would
        // cross-deliver values.
        var (clientTransport, deviceTransport) = ConcurrentLoopbackTransport.CreatePair();
        using var client = new BacnetClient(clientTransport, timeout: 15000, retries: 1);
        using var device = new BacnetClient(deviceTransport, timeout: 15000, retries: 1);

        device.OnReadPropertyRequest += AnswerWithInstance;
        client.Start();
        device.Start();

        // Stay at or below 255 distinct outstanding requests: the invoke-id is a byte, so beyond a
        // rollover two live requests could legitimately share an id. That ceiling is a separate matter
        // from the correlation this test covers.
        const int count = 200;
        var reads = Enumerable.Range(0, count).Select(async i =>
        {
            var values = await client.ReadPropertyAsync(Anywhere,
                new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, (uint)i), BacnetPropertyIds.PROP_PRESENT_VALUE);
            return (requested: (uint)i, returned: Convert.ToUInt32(values.Single().Value));
        });

        foreach (var (requested, returned) in await Task.WhenAll(reads))
            Assert.Equal(requested, returned);
    }

    /// <summary>
    /// Loopback transport that delivers each frame to its peer on a thread-pool thread after a short
    /// random delay, so requests and replies genuinely overlap and arrive out of order.
    /// </summary>
    private sealed class ConcurrentLoopbackTransport : IBacnetTransport
    {
        private ConcurrentLoopbackTransport _peer;
        private readonly Random _random;

        private ConcurrentLoopbackTransport(int seed) => _random = new Random(seed);

        public byte MaxInfoFrames { get; set; } = 0xFF;
        public int HeaderLength => 0;
        public int MaxBufferLength => 1500;
        public BacnetAddressTypes Type => BacnetAddressTypes.None;
        public BacnetMaxAdpu MaxAdpuLength => BacnetMaxAdpu.MAX_APDU1476;

        public event MessageRecievedHandler MessageRecieved;

        public static (ConcurrentLoopbackTransport, ConcurrentLoopbackTransport) CreatePair()
        {
            var a = new ConcurrentLoopbackTransport(1);
            var b = new ConcurrentLoopbackTransport(2);
            a._peer = b;
            b._peer = a;
            return (a, b);
        }

        public void Start() { }

        public BacnetAddress GetBroadcastAddress() => new(BacnetAddressTypes.None, 0, null);

        public bool WaitForAllTransmits(int timeout) => true;

        public int Send(byte[] buffer, int offset, int dataLength, BacnetAddress address, bool waitForTransmission, int timeout)
        {
            var frame = new byte[dataLength];
            Array.Copy(buffer, offset, frame, 0, dataLength);

            int delay;
            lock (_random)
                delay = _random.Next(0, 15);

            var peer = _peer;
            _ = Task.Run(async () =>
            {
                await Task.Delay(delay);
                peer.MessageRecieved?.Invoke(peer, frame, 0, frame.Length, new BacnetAddress(BacnetAddressTypes.None, 0, null));
            });

            return dataLength;
        }

        public void Dispose() { }
    }
}
