using System.Collections.Generic;
using System.Diagnostics;
using System.IO.BACnet.Serialize;
using System.IO.BACnet.Tests.Support;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// These tests drive the library's own timeout/cancellation semantics (and pass an explicit token
// where cancellation is under test), so they deliberately don't thread xUnit's TestContext token.
#pragma warning disable xUnit1051

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

    [Fact]
    public async Task ReadPropertyAsync_honours_cancellation()
    {
        // Nothing ever answers (frames are dropped), so only cancellation can end the wait - and it
        // must do so long before the 10 s timeout would.
        using var client = new BacnetClient(new SilentTransport(), timeout: 10000, retries: 1);
        client.Start();

        using var cts = new CancellationTokenSource(150);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.ReadPropertyAsync(Anywhere,
            new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 1), BacnetPropertyIds.PROP_PRESENT_VALUE,
            cancellationToken: cts.Token));
    }

    [Fact]
    public async Task ReadPropertyAsync_reassembles_a_segmented_response()
    {
        // Produce a genuine multi-segment ReadProperty-ACK with the (proven) send path, then feed those
        // segments to the client under test and confirm the awaited read reassembles them in order.
        // This exercises the async wait's per-segment re-arm - the path that PR #118's approach lost.
        var text = MakeCharString(3000);
        const byte invokeId = 55;
        var objectId = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 1234);
        var property = new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_DESCRIPTION, ASN1.BACNET_ARRAY_ALL);
        var adr = new BacnetAddress(BacnetAddressTypes.IP, "10.0.0.2:47808");
        var bigValue = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING, text);

        var producerTransport = new RecordingTransport(maxApdu: BacnetMaxAdpu.MAX_APDU480) { AutoSegmentAck = true };
        using var producer = new BacnetClient(producerTransport);
        producer.Start();
        var segmentation = producer.GetSegmentBuffer(BacnetMaxSegments.MAX_SEG16, BacnetMaxAdpu.MAX_APDU480);
        producer.ReadPropertyResponse(adr, invokeId, segmentation, objectId, property, new[] { bigValue });

        // The send path streams the remaining segments on a background thread (driven by AutoSegmentAck),
        // so wait until the whole sequence has been emitted before harvesting the frames.
        var segments = WaitForSegments(producerTransport);
        Assert.True(segments.Count > 1, $"test setup: expected a segmented response, produced {segments.Count} frame(s)");

        var consumerTransport = new RecordingTransport(maxApdu: BacnetMaxAdpu.MAX_APDU480);
        using var client = new BacnetClient(consumerTransport, timeout: 5000, retries: 1);
        client.MaxSegments = BacnetMaxSegments.MAX_SEG16;   // advertise that segmented responses are accepted
        var segmentsSeen = 0;
        client.OnSegment += (_, _, _, _, _, _, _, _, _, _, _) => Interlocked.Increment(ref segmentsSeen);
        client.Start();

        var readTask = client.ReadPropertyAsync(adr, objectId,
            (BacnetPropertyIds)property.propertyIdentifier, invokeId: invokeId);

        foreach (var segment in segments)
            consumerTransport.Receive(segment, adr);

        var values = await readTask;

        Assert.True(segmentsSeen > 1, $"expected several segments to be received, saw {segmentsSeen}");
        Assert.Single(values);
        Assert.Equal(text, values[0].Value);
    }

    private static string MakeCharString(int length)
    {
        var builder = new StringBuilder(length);
        while (builder.Length < length)
            builder.Append("0123456789");
        return builder.ToString(0, length);
    }

    // Poll a recording transport until it has emitted a complete ComplexACK segment sequence
    // (the last segment clears MORE_FOLLOWS). The NPDU here is 2 bytes, so the PDU-type octet is [2].
    private static List<byte[]> WaitForSegments(RecordingTransport transport)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < 5000)
        {
            List<byte[]> complexAcks;
            lock (transport.Sent)
            {
                complexAcks = transport.Sent
                    .Select(s => s.Frame)
                    .Where(f => ((BacnetPduTypes)f[2] & BacnetPduTypes.PDU_TYPE_MASK) == BacnetPduTypes.PDU_TYPE_COMPLEX_ACK)
                    .ToList();
            }
            if (complexAcks.Count > 0 && ((BacnetPduTypes)complexAcks[^1][2] & BacnetPduTypes.MORE_FOLLOWS) == 0)
                return complexAcks;
            Thread.Sleep(20);
        }
        throw new Exception("segmented response did not complete within 5 s");
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

    /// <summary>
    /// Transport that accepts sends but never delivers anything back, so requests go unanswered.
    /// </summary>
    private sealed class SilentTransport : IBacnetTransport
    {
        public byte MaxInfoFrames { get; set; } = 0xFF;
        public int HeaderLength => 0;
        public int MaxBufferLength => 1500;
        public BacnetAddressTypes Type => BacnetAddressTypes.None;
        public BacnetMaxAdpu MaxAdpuLength => BacnetMaxAdpu.MAX_APDU1476;

#pragma warning disable CS0067 // required by the interface; this transport never receives
        public event MessageRecievedHandler MessageRecieved;
#pragma warning restore CS0067

        public void Start() { }

        public BacnetAddress GetBroadcastAddress() => new(BacnetAddressTypes.None, 0, null);

        public bool WaitForAllTransmits(int timeout) => true;

        public int Send(byte[] buffer, int offset, int dataLength, BacnetAddress address, bool waitForTransmission, int timeout) => dataLength;

        public void Dispose() { }
    }
}
