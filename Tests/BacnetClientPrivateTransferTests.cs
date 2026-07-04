using System.IO.BACnet.Tests.Support;
using Xunit;

namespace System.IO.BACnet.Tests;

/// <summary>
/// End-to-end BacnetClient tests for the PrivateTransfer services (#154) over an in-memory
/// transport pair: request dispatch, ack round-trip, Result(-) and the unconfirmed variant.
/// </summary>
public class BacnetClientPrivateTransferTests
{
    private static (BacnetClient client, BacnetClient server) CreateClientServerPair()
    {
        var (transportA, transportB) = LoopbackTransport.CreatePair();
        var client = new BacnetClient(transportA, timeout: 500);
        var server = new BacnetClient(transportB, timeout: 500);
        client.Start();
        server.Start();
        return (client, server);
    }

    [Fact]
    public void Confirmed_request_roundtrip_with_result_block()
    {
        var (client, server) = CreateClientServerPair();
        using (client)
        using (server)
        {
            uint receivedVendor = 0, receivedService = 0;
            byte[] receivedParameters = null;
            var receivedNeedConfirm = false;

            server.OnPrivateTransfer += (sender, adr, invokeId, vendorId, serviceNumber, serviceParameters, needConfirm, maxSegments) =>
            {
                receivedVendor = vendorId;
                receivedService = serviceNumber;
                receivedParameters = serviceParameters;
                receivedNeedConfirm = needConfirm;
                sender.PrivateTransferResponse(adr, invokeId, null, vendorId, serviceNumber, new byte[] { 0xAA, 0xBB });
            };

            var ok = client.PrivateTransferRequest(client.Transport.GetBroadcastAddress(), 25, 8,
                new byte[] { 0x44, 0x42, 0x90, 0xCC, 0xCD }, out var resultBlock);

            Assert.True(ok);
            Assert.Equal(25u, receivedVendor);
            Assert.Equal(8u, receivedService);
            Assert.Equal(new byte[] { 0x44, 0x42, 0x90, 0xCC, 0xCD }, receivedParameters);
            Assert.True(receivedNeedConfirm);
            Assert.Equal(new byte[] { 0xAA, 0xBB }, resultBlock);
        }
    }

    [Fact]
    public void Confirmed_request_roundtrip_without_result_block()
    {
        var (client, server) = CreateClientServerPair();
        using (client)
        using (server)
        {
            server.OnPrivateTransfer += (sender, adr, invokeId, vendorId, serviceNumber, serviceParameters, needConfirm, maxSegments) =>
            {
                Assert.Null(serviceParameters);
                sender.PrivateTransferResponse(adr, invokeId, null, vendorId, serviceNumber);
            };

            var ok = client.PrivateTransferRequest(client.Transport.GetBroadcastAddress(), 25, 8, null, out var resultBlock);

            Assert.True(ok);
            Assert.Null(resultBlock);
        }
    }

    [Fact]
    public void Confirmed_request_error_response_throws()
    {
        var (client, server) = CreateClientServerPair();
        using (client)
        using (server)
        {
            server.OnPrivateTransfer += (sender, adr, invokeId, vendorId, serviceNumber, serviceParameters, needConfirm, maxSegments) =>
            {
                sender.PrivateTransferErrorResponse(adr, invokeId, BacnetErrorClasses.ERROR_CLASS_SERVICES,
                    BacnetErrorCodes.ERROR_CODE_SERVICE_REQUEST_DENIED, vendorId, serviceNumber);
            };

            var ex = Assert.Throws<Exception>(() =>
                client.PrivateTransferRequest(client.Transport.GetBroadcastAddress(), 25, 8, null, out _));

            Assert.Contains("ERROR_CODE_SERVICE_REQUEST_DENIED", ex.Message);
        }
    }

    [Fact]
    public void Unconfirmed_request_raises_event()
    {
        var (client, server) = CreateClientServerPair();
        using (client)
        using (server)
        {
            uint receivedVendor = 0, receivedService = 0;
            byte[] receivedParameters = null;
            var receivedNeedConfirm = true;

            server.OnPrivateTransfer += (sender, adr, invokeId, vendorId, serviceNumber, serviceParameters, needConfirm, maxSegments) =>
            {
                receivedVendor = vendorId;
                receivedService = serviceNumber;
                receivedParameters = serviceParameters;
                receivedNeedConfirm = needConfirm;
            };

            client.SendUnconfirmedPrivateTransfer(client.Transport.GetBroadcastAddress(), 18, 12, new byte[] { 0x16, 0x49 });

            Assert.Equal(18u, receivedVendor);
            Assert.Equal(12u, receivedService);
            Assert.Equal(new byte[] { 0x16, 0x49 }, receivedParameters);
            Assert.False(receivedNeedConfirm);
        }
    }

    [Fact]
    public void Confirmed_request_without_handler_is_rejected()
    {
        var (client, server) = CreateClientServerPair();
        using (client)
        using (server)
        {
            // no OnPrivateTransfer handler on the server -> UNRECOGNIZED_SERVICE reject
            var ex = Assert.Throws<Exception>(() =>
                client.PrivateTransferRequest(client.Transport.GetBroadcastAddress(), 25, 8, null, out _));

            Assert.Contains("Reject", ex.Message);
        }
    }
}
