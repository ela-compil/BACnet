/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2014 Morten Kvistgaard <mk@pch-engineering.dk>
*
* Permission is hereby granted, free of charge, to any person obtaining
* a copy of this software and associated documentation files (the
* "Software"), to deal in the Software without restriction, including
* without limitation the rights to use, copy, modify, merge, publish,
* distribute, sublicense, and/or sell copies of the Software, and to
* permit persons to whom the Software is furnished to do so, subject to
* the following conditions:
*
* The above copyright notice and this permission notice shall be included
* in all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*
*********************************************************************/

namespace System.IO.BACnet;

/// <summary>
/// This is the standard BACNet udp transport
/// </summary>
public class BacnetIpUdpProtocolTransport : BacnetTransportBase
{
    private UdpClient _sharedConn;
    private UdpClient _exclusiveConn;
    private readonly bool _exclusivePort;
    private readonly bool _dontFragment;
    private readonly string _localEndpoint;
    private BacnetAddress _broadcastAddress;
    private bool _disposing;

    public BVLC Bvlc { get; private set; }
    public int SharedPort { get; }
    public int ExclusivePort { get; }

    private int _receiveBufferSize = 1024 * 1024; // 1 MB

    /// <summary>
    /// UDP receive buffer size (SO_RCVBUF) in bytes, defaulting to 1 MB. A larger buffer lets the OS
    /// hold a bigger burst of incoming messages before it starts dropping packets. It may be changed
    /// at any time — including after <see cref="Start"/> — and is applied to the live socket(s)
    /// immediately. On Linux the effective value is capped by net.core.rmem_max (raise that via
    /// sysctl to go higher); the getter returns the requested value, not the OS-adjusted one.
    /// </summary>
    public int ReceiveBufferSize
    {
        get => _receiveBufferSize;
        set { _receiveBufferSize = value; ApplyReceiveBufferSize(); }
    }

    // Give 0.0.0.0:xxxx if the socket is open with System.Net.IPAddress.Any
    // Today only used by _GetBroadcastAddress method & the bvlc layer class in BBMD mode
    // Some more complex solutions could avoid this, that's why this property is virtual
    public virtual IPEndPoint LocalEndPoint => (IPEndPoint)_exclusiveConn.Client.LocalEndPoint;

    /// <param name="maxApdu">
    /// Largest APDU this endpoint sends and advertises (in I-Am and confirmed
    /// request headers). The default <see cref="BVLC.BVLC_MAX_APDU"/> (1476)
    /// predates UDP/IP overhead, so full-size frames exceed a 1500-byte MTU
    /// and get IP-fragmented; pass <see cref="BacnetMaxAdpu.MAX_APDU1024"/> to
    /// keep every frame, including segmented responses, below the MTU.
    /// </param>
    public BacnetIpUdpProtocolTransport(int port, bool useExclusivePort = false, bool dontFragment = false,
        int maxPayload = 1472, string localEndpointIp = "", BacnetMaxAdpu maxApdu = BVLC.BVLC_MAX_APDU)
    {
        SharedPort = port;
        MaxBufferLength = maxPayload;
        Type = BacnetAddressTypes.IP;
        HeaderLength = BVLC.BVLC_HEADER_LENGTH;
        MaxAdpuLength = maxApdu;

        _exclusivePort = useExclusivePort;
        _dontFragment = dontFragment;
        _localEndpoint = localEndpointIp;
    }

    public BacnetIpUdpProtocolTransport(int sharedPort, int exclusivePort, bool dontFragment = false,
        int maxPayload = 1472, string localEndpointIp = "", BacnetMaxAdpu maxApdu = BVLC.BVLC_MAX_APDU)
        : this(sharedPort, false, dontFragment, maxPayload, localEndpointIp, maxApdu)
    {
        ExclusivePort = exclusivePort;
    }

    public override bool Equals(object obj)
    {
        var a = obj as BacnetIpUdpProtocolTransport;
        return a?.SharedPort == SharedPort;
    }

    public override int GetHashCode()
    {
        return SharedPort.GetHashCode();
    }

    public override string ToString()
    {
        return $"Udp:{SharedPort}";
    }

    private void Open()
    {
        if (!_exclusivePort)
        {
            /* We need a shared broadcast "listen" port. This is the 0xBAC0 port */
            /* This will enable us to have more than 1 client, on the same machine. Perhaps it's not that important though. */
            /* We (might) only recieve the broadcasts on this. Any unicasts to this might be eaten by another local client */
            if (_sharedConn == null)
            {
                _sharedConn = new UdpClient { ExclusiveAddressUse = false };
                _sharedConn.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                var ep = new IPEndPoint(IPAddress.Any, SharedPort);
                DisableConnReset(_sharedConn);
                _sharedConn.Client.Bind(ep);
                SetDontFragment(_sharedConn, _dontFragment);
                Log.LogInformation($"Binded shared {ep} using UDP");
            }
            /* This is our own exclusive port. We'll recieve everything sent to this. */
            /* So this is how we'll present our selves to the world */
            if (_exclusiveConn == null)
            {
                var ep = new IPEndPoint(IPAddress.Any, ExclusivePort);
                if (!string.IsNullOrEmpty(_localEndpoint)) ep = new IPEndPoint(IPAddress.Parse(_localEndpoint), ExclusivePort);
                _exclusiveConn = new UdpClient { ExclusiveAddressUse = false };
                _exclusiveConn.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _exclusiveConn.Client.Bind(ep);
                // Gets the Endpoint : the assigned Udp port number in fact
                ep = (IPEndPoint)_exclusiveConn.Client.LocalEndPoint;
                // closes the socket
                _exclusiveConn.Close();
                // Re-opens it with the freeed port number, to be sure it's a real active/server socket
                // which cannot be disarmed for listen by .NET for incoming call after a few inactivity
                // minutes ... yes it's like this at least on several systems
                _exclusiveConn = new UdpClient { ExclusiveAddressUse = false };
                _exclusiveConn.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _exclusiveConn.EnableBroadcast = true;
                _exclusiveConn.Client.Bind(ep);
                SetDontFragment(_exclusiveConn, _dontFragment);
                DisableConnReset(_exclusiveConn);
            }
        }
        else
        {
            var ep = new IPEndPoint(IPAddress.Any, SharedPort);
            if (!string.IsNullOrEmpty(_localEndpoint)) ep = new IPEndPoint(IPAddress.Parse(_localEndpoint), SharedPort);
            _exclusiveConn = new UdpClient { ExclusiveAddressUse = true };
            DisableConnReset(_exclusiveConn);
            _exclusiveConn.Client.Bind(ep);
            SetDontFragment(_exclusiveConn, _dontFragment);
            _exclusiveConn.EnableBroadcast = true;
            Log.LogInformation($"Binded exclusively to {ep} using UDP");
        }

        ApplyReceiveBufferSize();

        Bvlc = new BVLC(this);
    }

    private void ApplyReceiveBufferSize()
    {
        // SO_RCVBUF can be set on an already-bound socket. Over-requesting is clamped (Linux) or
        // honored (Windows), never fatal - but guard anyway for constrained platforms.
        try { if (_sharedConn != null) _sharedConn.Client.ReceiveBufferSize = _receiveBufferSize; } catch { }
        try { if (_exclusiveConn != null) _exclusiveConn.Client.ReceiveBufferSize = _receiveBufferSize; } catch { }
    }

    /// <summary>
    ///   Prevent exception on setting Don't Fragment on OSX
    /// </summary>
    /// <remarks>
    ///   https://github.com/dotnet/runtime/issues/27653
    /// </remarks>
    /// <param name="client"></param>
    /// <param name="dontFragment"></param>
    private void SetDontFragment(UdpClient client, bool dontFragment)
    {
        // Setting DontFragment throws on macOS (dotnet/runtime#27653). The old guard tested
        // Environment.OSVersion.Platform against PlatformID.MacOSX, but modern .NET reports Unix
        // (never MacOSX) there, so it never actually skipped macOS. RuntimeInformation detects the
        // OS correctly on .NET Framework and .NET Core alike.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;

        try
        {
            client.DontFragment = dontFragment;
        }
        catch (SocketException e)
        {
            Log.LogWarning(e, "Unable to set DontFragment");
        }
    }

    /// <summary>
    ///   Done to prevent exceptions in Socket.BeginReceive()
    /// </summary>
    /// <remarks>
    ///   http://microsoft.public.win32.programmer.networks.narkive.com/RlxW2V6m/udp-comms-and-connection-reset-problem
    /// </remarks>
    private static void DisableConnReset(UdpClient client)
    {
        // SIO_UDP_CONNRESET is a Windows-only ioctl. RuntimeInformation.IsOSPlatform is the modern,
        // analyzer-recognised guard (silences CA1416 when targeting net8/net10) and is correct on
        // both .NET Framework and .NET Core, unlike Environment.OSVersion.Platform.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        const uint IOC_IN = 0x80000000;
        const uint IOC_VENDOR = 0x18000000;
        const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

        client?.Client.IOControl(unchecked((int)SIO_UDP_CONNRESET),
            new[] { System.Convert.ToByte(false) }, null);
    }

    protected void Close()
    {
        _exclusiveConn.Close();
    }

    public override void Start()
    {
        _disposing = false;

        Open();

        _sharedConn?.BeginReceive(OnReceiveData, _sharedConn);
        _exclusiveConn?.BeginReceive(OnReceiveData, _exclusiveConn);
    }

    private void OnReceiveData(IAsyncResult asyncResult)
    {
        var connection = (UdpClient)asyncResult.AsyncState;

        try
        {
            IPEndPoint ep = null;
            byte[] receiveBuffer;

            try
            {
                receiveBuffer = connection.EndReceive(asyncResult, ref ep);
            }
            finally
            {
                if (connection.Client != null && !_disposing)
                {
                    // BeginReceive ASAP to enable parallel processing (e.g. query additional data while processing a notification).
                    // Defer to the thread pool so a synchronously-completing receive can't recurse into OnReceiveData and overflow the stack.
                    ThreadPool.UnsafeQueueUserWorkItem(static state =>
                    {
                        var (transport, udp) = ((BacnetIpUdpProtocolTransport transport, UdpClient udp))state!;
                        if (udp.Client == null || transport._disposing) return;

                        try
                        {
                            udp.BeginReceive(transport.OnReceiveData, udp);
                        }
                        catch (Exception ex)
                        {
                            transport.Log.LogError($"Failed to restart data receive. {ex}");
                        }
                    }, (this, connection));
                }
            }

            var receivedLength = receiveBuffer.Length;

            if (receivedLength == 0) // Empty frame : port scanner maybe
                return;

            //verify message
            Convert(ep, out var remoteAddress);

            if (receivedLength < BVLC.BVLC_HEADER_LENGTH)
            {
                Log.LogWarning($"Some garbage data got in: {ConvertToHex(receiveBuffer)}");
                return;
            }

            // Basic Header lenght
            var headerLength = Bvlc.Decode(receiveBuffer, 0, out var function, out var _, ep);

            if (headerLength == -1)
            {
                Log.LogWarning($"Unknow BVLC Header in: {ConvertToHex(receiveBuffer)}");
                return;
            }

            switch (function)
            {
                case BacnetBvlcFunctions.BVLC_RESULT:
                    // response to BVLC_REGISTER_FOREIGN_DEVICE, could be BVLC_DISTRIBUTE_BROADCAST_TO_NETWORK
                    // but we are not a BBMD, we don't care
                    Log.LogDebug("Receive Register as Foreign Device Response");
                    break;

                case BacnetBvlcFunctions.BVLC_FORWARDED_NPDU:
                    // BVLC_FORWARDED_NPDU frame by a BBMD, change the remote_address to the original one
                    // stored in the BVLC header, we don't care about the BBMD address
                    var ip = ((long)receiveBuffer[7] << 24) + ((long)receiveBuffer[6] << 16) +
                             ((long)receiveBuffer[5] << 8) + receiveBuffer[4];

                    var port = (receiveBuffer[8] << 8) + receiveBuffer[9]; // 0xbac0 maybe
                    Convert(new IPEndPoint(ip, port), out remoteAddress);
                    break;
            }

            if (function != BacnetBvlcFunctions.BVLC_ORIGINAL_UNICAST_NPDU &&
                function != BacnetBvlcFunctions.BVLC_ORIGINAL_BROADCAST_NPDU &&
                function != BacnetBvlcFunctions.BVLC_FORWARDED_NPDU)
            {
                Log.LogDebug($"{function} - ignoring");
                return;
            }

            if (receivedLength <= headerLength)
            {
                Log.LogWarning($"Missing data, only header received: {ConvertToHex(receiveBuffer)}");
                return;
            }

            InvokeMessageRecieved(receiveBuffer, headerLength, receivedLength - headerLength, remoteAddress);
        }
        catch (ObjectDisposedException)
        {
            Log.LogDebug("Connection has been disposed");
        }
        catch (Exception e)
        {
            if (connection.Client == null || _disposing)
                return;

            Log.LogError(e, "Exception in OnRecieveData");
        }
    }

    public bool SendRegisterAsForeignDevice(IPEndPoint bbmd, short ttl)
    {
        if (bbmd.AddressFamily != AddressFamily.InterNetwork)
            return false;

        Bvlc.SendRegisterAsForeignDevice(bbmd, ttl);
        return true;
    }
    public bool SendRemoteWhois(byte[] buffer, IPEndPoint bbmd, int msgLength)
    {
        if (bbmd.AddressFamily != AddressFamily.InterNetwork)
            return false;

        Bvlc.SendRemoteWhois(buffer, bbmd, msgLength);
        return true;
    }

    public static string ConvertToHex(byte[] buffer)
    {
        return BitConverter.ToString(buffer).Replace("-", "");
    }

    public int Send(byte[] buffer, int dataLength, IPEndPoint ep)
    {
        // return _exclusiveConn.Send(buffer, data_length, ep);
        ThreadPool.QueueUserWorkItem(o =>
        {
            try
            {
                _exclusiveConn.Send(buffer, dataLength, ep);
            }
            catch
            {
                    // not much you can do about at this point
                }
        }, null);

        return dataLength;
    }

    public override int Send(byte[] buffer, int offset, int dataLength, BacnetAddress address, bool waitForTransmission, int timeout)
    {
        if (_exclusiveConn == null) return 0;

        //add header
        var fullLength = dataLength + HeaderLength;
        Bvlc.Encode(buffer, offset - BVLC.BVLC_HEADER_LENGTH, address.net == 0xFFFF
            ? BacnetBvlcFunctions.BVLC_ORIGINAL_BROADCAST_NPDU
            : BacnetBvlcFunctions.BVLC_ORIGINAL_UNICAST_NPDU, fullLength);

        //create end point
        Convert(address, out var ep);

        try
        {
            // broadcasts are transported from our local unicast socket also
            return _exclusiveConn.Send(buffer, fullLength, ep);
        }
        catch
        {
            return 0;
        }
    }

    public static void Convert(IPEndPoint ep, out BacnetAddress address)
    {
        var tmp1 = ep.Address.GetAddressBytes();
        var tmp2 = BitConverter.GetBytes((ushort)ep.Port);
        Array.Reverse(tmp2);
        Array.Resize(ref tmp1, tmp1.Length + tmp2.Length);
        Array.Copy(tmp2, 0, tmp1, tmp1.Length - tmp2.Length, tmp2.Length);
        address = new BacnetAddress(BacnetAddressTypes.IP, 0, tmp1);
    }

    public static void Convert(BacnetAddress address, out IPEndPoint ep)
    {
        long ipAddress = BitConverter.ToUInt32(address.adr, 0);
        var port = (ushort)((address.adr[4] << 8) | (address.adr[5] << 0));
        ep = new IPEndPoint(ipAddress, port);
    }

    // Get the IPAddress only if one is present
    // this could be usefull to find the broadcast address even if the socket is open on the default interface
    // removes somes virtual interfaces (certainly not all)
    private static UnicastIPAddressInformation GetAddressDefaultInterface()
    {
        var unicastAddresses = NetworkInterface.GetAllNetworkInterfaces()
            .Where(i => i.OperationalStatus == OperationalStatus.Up)
            .Where(i => i.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Where(i => !(i.Name.Contains("VirtualBox") || i.Name.Contains("VMware")))
            .SelectMany(i => i.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
            .ToArray();

        if (unicastAddresses.Length == 1)
            return unicastAddresses.Single();

        // Zero or several candidate interfaces: the broadcast interface is ambiguous.
        // Fail loudly instead of silently broadcasting to 255.255.255.255 or an arbitrary
        // NIC — the caller must pass localEndpointIp to select the interface to use.
        throw new InvalidOperationException(
            $"Cannot determine the BACnet/IP broadcast address automatically: found {unicastAddresses.Length} " +
            $"candidate IPv4 interface(s) [{string.Join(", ", unicastAddresses.Select(a => a.Address.ToString()))}]. " +
            "Specify localEndpointIp in the BacnetIpUdpProtocolTransport constructor to select the interface.");
    }

    // A lot of problems on Mono (Raspberry) to get the correct broadcast @
    // so this method is overridable (this allows the implementation of operating system specific code)
    // Marc solution http://stackoverflow.com/questions/8119414/how-to-query-the-subnet-masks-using-mono-on-linux for instance
    protected virtual BacnetAddress _GetBroadcastAddress()
    {
        // general broadcast by default if nothing better is found
        var ep = new IPEndPoint(IPAddress.Parse("255.255.255.255"), SharedPort);

        UnicastIPAddressInformation ipAddr = null;

        if (LocalEndPoint.Address.ToString() == "0.0.0.0")
        {
            ipAddr = GetAddressDefaultInterface();
        }
        else
        {
            // restricted local broadcast (directed ... routable)
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
                foreach (var ip in adapter.GetIPProperties().UnicastAddresses)
                    if (LocalEndPoint.Address.Equals(ip.Address))
                    {
                        ipAddr = ip;
                        break;
                    }
        }

        if (ipAddr != null)
        {
            try
            {
                var strCurrentIP = ipAddr.Address.ToString().Split('.');
                var strIPNetMask = ipAddr.IPv4Mask.ToString().Split('.');
                var broadcastStr = new StringBuilder();
                for (var i = 0; i < 4; i++)
                {
                    broadcastStr.Append(((byte)(int.Parse(strCurrentIP[i]) | ~int.Parse(strIPNetMask[i]))).ToString());
                    if (i != 3) broadcastStr.Append('.');
                }
                ep = new IPEndPoint(IPAddress.Parse(broadcastStr.ToString()), SharedPort);
            }
            catch
            {
                // on mono IPv4Mask feature not implemented
            }
        }

        Convert(ep, out var broadcast);
        broadcast.net = 0xFFFF;
        return broadcast;
    }

    public override BacnetAddress GetBroadcastAddress()
    {
        return _broadcastAddress ??= _GetBroadcastAddress();
    }

    public override void Dispose()
    {
        _disposing = true;
        _exclusiveConn?.Close();
        _exclusiveConn = null;
        _sharedConn?.Close();
        _sharedConn = null;
    }
}
