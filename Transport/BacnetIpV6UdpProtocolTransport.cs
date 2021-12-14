/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2015 Frederic Chaxel <fchaxel@free.fr> 
*                    Morten Kvistgaard <mk@pch-engineering.dk>
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

// based on Addendum 135-2012aj-4

namespace System.IO.BACnet;

public class BacnetIpV6UdpProtocolTransport : BacnetTransportBase
{
    private readonly bool _exclusivePort;
    private readonly string _localEndpoint;
    private readonly int _vMac;
    private bool _dontFragment;
    private UdpClient _exclusiveConn;
    private UdpClient _sharedConn;

    public BVLCV6 Bvlc { get; private set; }
    public int SharedPort { get; }

    // Give [::]:xxxx if the socket is open with System.Net.IPAddress.IPv6Any
    // Used the bvlc layer class in BBMD mode
    // Some more complex solutions could avoid this, that's why this property is virtual
    public virtual IPEndPoint LocalEndPoint => (IPEndPoint)_exclusiveConn.Client.LocalEndPoint;

    public BacnetIpV6UdpProtocolTransport(int port, int vMac = -1, bool useExclusivePort = false,
        bool dontFragment = false, int maxPayload = 1472, string localEndpointIp = "")
    {
        SharedPort = port;
        MaxBufferLength = maxPayload;
        Type = BacnetAddressTypes.IPV6;
        MaxAdpuLength = BVLCV6.BVLC_MAX_APDU;

        // Two frames type, unicast with 10 bytes or broadcast with 7 bytes
        // Here it's the biggest header, resize will be done after, if needed
        HeaderLength = BVLCV6.BVLC_HEADER_LENGTH;

        _exclusivePort = useExclusivePort;
        _dontFragment = dontFragment;
        _localEndpoint = localEndpointIp;
        _vMac = vMac;
    }

    public override void Start()
    {
        Open();

        _sharedConn?.BeginReceive(OnReceiveData, _sharedConn);
        _exclusiveConn?.BeginReceive(OnReceiveData, _exclusiveConn);
    }

    public override int Send(byte[] buffer, int offset, int dataLength, BacnetAddress address,
        bool waitForTransmission, int timeout)
    {
        if (_exclusiveConn == null) return 0;

        //add header
        var fullLength = dataLength + HeaderLength;

        if (address.net == 0xFFFF)
        {
            var newBuffer = new byte[fullLength - 3];
            Array.Copy(buffer, 3, newBuffer, 0, fullLength - 3);
            fullLength -= 3;
            buffer = newBuffer;
            Bvlc.Encode(buffer, offset - BVLCV6.BVLC_HEADER_LENGTH,
                BacnetBvlcV6Functions.BVLC_ORIGINAL_BROADCAST_NPDU, fullLength, address);
        }
        else
        {
            Bvlc.Encode(buffer, offset - BVLCV6.BVLC_HEADER_LENGTH, BacnetBvlcV6Functions.BVLC_ORIGINAL_UNICAST_NPDU,
                fullLength, address);
        }

        // create end point
        Convert(address, out var ep);

        try
        {
            // send
            // multicast are transported from our local unicast socket also
            return _exclusiveConn.Send(buffer, fullLength, ep);
        }
        catch
        {
            return 0;
        }
    }

    public override BacnetAddress GetBroadcastAddress()
    {
        // could be FF08, FF05, FF04, FF02
        var ep = new IPEndPoint(IPAddress.Parse("[FF0E::BAC0]"), SharedPort);
        Convert(ep, out var ret);
        ret.net = 0xFFFF;
        return ret;
    }

    public override void Dispose()
    {
        _exclusiveConn?.Close();
        _exclusiveConn = null;
        _sharedConn?.Close();
        _sharedConn = null;
    }

    public override bool Equals(object obj)
    {
        var a = obj as BacnetIpV6UdpProtocolTransport;
        return a?.SharedPort == SharedPort;
    }

    public override int GetHashCode()
    {
        return SharedPort.GetHashCode();
    }

    public override string ToString()
    {
        return "Udp IPv6:" + SharedPort;
    }

    private void Open()
    {
        UdpClient multicastListener = null;

        if (!_exclusivePort)
        {
            /* We need a shared multicast "listen" port. This is the 0xBAC0 port */
            /* This will enable us to have more than 1 client, on the same machine. Perhaps it's not that important though. */
            /* We (might) only receive the multicast on this. Any unicasts to this might be eaten by another local client */
            if (_sharedConn == null)
            {
                _sharedConn = new UdpClient(AddressFamily.InterNetworkV6) { ExclusiveAddressUse = false };
                _sharedConn.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                EndPoint ep = new IPEndPoint(IPAddress.IPv6Any, SharedPort);
                if (!string.IsNullOrEmpty(_localEndpoint))
                    ep = new IPEndPoint(IPAddress.Parse(_localEndpoint), SharedPort);
                _sharedConn.Client.Bind(ep);
                multicastListener = _sharedConn;
            }
            /* This is our own exclusive port. We'll recieve everything sent to this. */
            /* So this is how we'll present our selves to the world */
            if (_exclusiveConn == null)
            {
                EndPoint ep = new IPEndPoint(IPAddress.IPv6Any, 0);
                if (!string.IsNullOrEmpty(_localEndpoint)) ep = new IPEndPoint(IPAddress.Parse(_localEndpoint), 0);
                _exclusiveConn = new UdpClient((IPEndPoint)ep);
            }
        }
        else
        {
            EndPoint ep = new IPEndPoint(IPAddress.IPv6Any, SharedPort);
            if (!string.IsNullOrEmpty(_localEndpoint))
                ep = new IPEndPoint(IPAddress.Parse(_localEndpoint), SharedPort);
            _exclusiveConn = new UdpClient(AddressFamily.InterNetworkV6)
            {
                ExclusiveAddressUse = true
            };
            _exclusiveConn.Client.Bind((IPEndPoint)ep);
            multicastListener = _exclusiveConn;
        }

        multicastListener.JoinMulticastGroup(IPAddress.Parse("[FF02::BAC0]"));
        multicastListener.JoinMulticastGroup(IPAddress.Parse("[FF04::BAC0]"));
        multicastListener.JoinMulticastGroup(IPAddress.Parse("[FF05::BAC0]"));
        multicastListener.JoinMulticastGroup(IPAddress.Parse("[FF08::BAC0]"));
        multicastListener.JoinMulticastGroup(IPAddress.Parse("[FF0E::BAC0]"));

        // If this option is enabled Yabe cannot see itself !
        // multicastListener.MulticastLoopback = false;

        Bvlc = new BVLCV6(this, _vMac);
    }

    protected void Close()
    {
        _sharedConn?.BeginReceive(OnReceiveData, _sharedConn);
        _exclusiveConn?.BeginReceive(OnReceiveData, _exclusiveConn);
    }

    private void OnReceiveData(IAsyncResult asyncResult)
    {
        var conn = (UdpClient)asyncResult.AsyncState;
        try
        {
            var ep = new IPEndPoint(IPAddress.Any, 0);
            byte[] localBuffer;
            int rx;

            try
            {
                localBuffer = conn.EndReceive(asyncResult, ref ep);
                rx = localBuffer.Length;
            }
            catch (Exception) // ICMP port unreachable
            {
                //restart data receive
                conn.BeginReceive(OnReceiveData, conn);
                return;
            }

            if (rx == 0) // Empty frame : port scanner maybe
            {
                //restart data receive
                conn.BeginReceive(OnReceiveData, conn);
                return;
            }

            try
            {
                //verify message
                Convert(ep, out var remoteAddress);
                if (rx < BVLCV6.BVLC_HEADER_LENGTH - 3)
                {
                    Log.Warn("Some garbage data got in");
                }
                else
                {
                    // Basic Header lenght
                    var headerLength = Bvlc.Decode(localBuffer, 0, out var function, out _, ep, remoteAddress);

                    switch (headerLength)
                    {
                        case 0:
                            return;
                        case -1:
                            Log.Debug("Unknow BVLC Header");
                            return;
                    }

                    // response to BVLC_REGISTER_FOREIGN_DEVICE (could be BVLC_DISTRIBUTE_BROADCAST_TO_NETWORK ... but we are not a BBMD, don't care)
                    if (function == BacnetBvlcV6Functions.BVLC_RESULT)
                    {
                        Log.Debug("Receive Register as Foreign Device Response");
                    }

                    // a BVLC_FORWARDED_NPDU frame by a BBMD, change the remote_address to the original one (stored in the BVLC header) 
                    // we don't care about the BBMD address
                    if (function == BacnetBvlcV6Functions.BVLC_FORWARDED_NPDU)
                    {
                        Array.Copy(localBuffer, 7, remoteAddress.adr, 0, 18);
                    }

                    if (function != BacnetBvlcV6Functions.BVLC_ORIGINAL_UNICAST_NPDU &&
                        function != BacnetBvlcV6Functions.BVLC_ORIGINAL_BROADCAST_NPDU &&
                        function != BacnetBvlcV6Functions.BVLC_FORWARDED_NPDU)
                        return;

                    if (rx > headerLength)
                        InvokeMessageRecieved(localBuffer, headerLength, rx - headerLength, remoteAddress);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception in udp recieve", ex);
            }
            finally
            {
                //restart data receive
                conn.BeginReceive(OnReceiveData, conn);
            }
        }
        catch (Exception ex)
        {
            //restart data receive
            if (conn.Client != null)
            {
                Log.Error("Exception in Ip OnRecieveData", ex);
                conn.BeginReceive(OnReceiveData, conn);
            }
        }
    }

    public static string ConvertToHex(byte[] buffer, int length)
    {
        var ret = "";

        for (var i = 0; i < length; i++)
            ret += buffer[i].ToString("X2");

        return ret;
    }

    // Modif FC : used for BBMD communication
    public int Send(byte[] buffer, int dataLength, IPEndPoint ep)
    {
        try
        {
            // return _exclusiveConn.Send(buffer, data_length, ep);
            ThreadPool.QueueUserWorkItem(o => _exclusiveConn.Send(buffer, dataLength, ep), null);
            return dataLength;
        }
        catch
        {
            return 0;
        }
    }

    public bool SendRegisterAsForeignDevice(IPEndPoint bbmd, short ttl)
    {
        if (bbmd.AddressFamily != AddressFamily.InterNetworkV6)
            return false;

        Bvlc.SendRegisterAsForeignDevice(bbmd, ttl);
        return true;
    }

    public bool SendRemoteWhois(byte[] buffer, IPEndPoint bbmd, int msgLength)
    {
        if (bbmd.AddressFamily != AddressFamily.InterNetworkV6)
            return false;

        // This message was build using the default (10) header lenght, but it's smaller (7)
        var newBuffer = new byte[msgLength - 3];
        Array.Copy(buffer, 3, newBuffer, 0, msgLength - 3);
        msgLength -= 3;

        Bvlc.SendRemoteWhois(newBuffer, bbmd, msgLength);
        return true;
    }

    public static void Convert(IPEndPoint ep, out BacnetAddress address)
    {
        var tmp1 = ep.Address.GetAddressBytes();
        var tmp2 = BitConverter.GetBytes((ushort)ep.Port);
        Array.Reverse(tmp2);
        Array.Resize(ref tmp1, tmp1.Length + tmp2.Length);
        Array.Copy(tmp2, 0, tmp1, tmp1.Length - tmp2.Length, tmp2.Length);
        address = new BacnetAddress(BacnetAddressTypes.IPV6, 0, tmp1);
    }

    public static void Convert(BacnetAddress address, out IPEndPoint ep)
    {
        var port = (ushort)((address.adr[16] << 8) | (address.adr[17] << 0));
        var ipv6 = new byte[16];
        Array.Copy(address.adr, ipv6, 16);
        ep = new IPEndPoint(new IPAddress(ipv6), port);
    }
}
