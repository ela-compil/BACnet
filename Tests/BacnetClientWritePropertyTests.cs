using System.Collections.Generic;
using System.IO.BACnet.Serialize;
using System.IO.BACnet.Tests.Support;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// WriteProperty requests over an in-memory transport pair: the optional array index must reach
/// the device as the property-array-index (context tag [2]) and stay absent by default.
/// </summary>
public class BacnetClientWritePropertyTests
{
    [Fact]
    public void Write_property_carries_the_array_index_to_the_device()
    {
        var (transportA, transportB) = LoopbackTransport.CreatePair();
        var client = new BacnetClient(transportA, timeout: 500);
        var server = new BacnetClient(transportB, timeout: 500);
        using (client)
        using (server)
        {
            client.Start();
            server.Start();

            var receivedIndexes = new List<uint>();
            server.OnWritePropertyRequest += (sender, adr, invokeId, objectId, value, maxSegments) =>
            {
                receivedIndexes.Add(value.property.propertyArrayIndex);
                sender.SimpleAckResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, invokeId);
            };

            var analogValue = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 1);
            var value = new[] { new BacnetValue(21.5f) };

            client.WritePropertyRequest(client.Transport.GetBroadcastAddress(), analogValue,
                BacnetPropertyIds.PROP_PRIORITY_ARRAY, value, arrayIndex: 5);
            client.WritePropertyRequest(client.Transport.GetBroadcastAddress(), analogValue,
                BacnetPropertyIds.PROP_PRIORITY_ARRAY, value);

            Assert.Equal(new uint[] { 5, ASN1.BACNET_ARRAY_ALL }, receivedIndexes);
        }
    }
}
