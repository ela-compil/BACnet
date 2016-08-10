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

using System.Text;
using System.IO.BACnet.Serialize;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace System.IO.BACnet
{
    /// <summary>
    /// This is the standard BACNet udp transport
    /// </summary>
    public class BacnetIpUdpProtocolTransport : IBacnetTransport
    {
        private UdpClient _sharedConn;
        private UdpClient _exclusiveConn;
        private readonly bool _exclusivePort;
        private readonly bool _dontFragment;
        private readonly string _localEndpoint;
        private BacnetAddress _broadcastAddress;
        private bool _disposing;

        public BVLC Bvlc { get; private set; }
        public BacnetAddressTypes Type => BacnetAddressTypes.IP;
        public event MessageRecievedHandler MessageRecieved;
        public int SharedPort { get; }
        public int ExclusivePort => ((IPEndPoint)_exclusiveConn.Client.LocalEndPoint).Port;
        public int HeaderLength => BVLC.BVLC_HEADER_LENGTH;
        public BacnetMaxAdpu MaxAdpuLength => BVLC.BVLC_MAX_APDU;
        public byte MaxInfoFrames { get { return 0xff; } set { /* ignore */ } }     //the udp doesn't have max info frames
        public int MaxBufferLength { get; }

        // Give 0.0.0.0:xxxx if the socket is open with System.Net.IPAddress.Any
        // Today only used by _GetBroadcastAddress method & the bvlc layer class in BBMD mode
        // Some more complex solutions could avoid this, that's why this property is virtual
        public virtual IPEndPoint LocalEndPoint => (IPEndPoint)_exclusiveConn.Client.LocalEndPoint;

        public BacnetIpUdpProtocolTransport(int port, bool useExclusivePort = false, bool dontFragment = false, int maxPayload = 1472, string localEndpointIp = "")
        {
            SharedPort = port;
            MaxBufferLength = maxPayload;
            _exclusivePort = useExclusivePort;
            _dontFragment = dontFragment;
            _localEndpoint = localEndpointIp;
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
                    if (!string.IsNullOrEmpty(_localEndpoint)) ep = new IPEndPoint(IPAddress.Parse(_localEndpoint), SharedPort);
                    _sharedConn.Client.Bind(ep);
                    _sharedConn.DontFragment = _dontFragment;
                }
                /* This is our own exclusive port. We'll recieve everything sent to this. */
                /* So this is how we'll present our selves to the world */
                if (_exclusiveConn == null)
                {
                    var ep = new IPEndPoint(IPAddress.Any, 0);
                    if (!string.IsNullOrEmpty(_localEndpoint)) ep = new IPEndPoint(IPAddress.Parse(_localEndpoint), 0);
                    _exclusiveConn = new UdpClient(ep) { DontFragment = _dontFragment };
                }
            }
            else
            {
                var ep = new IPEndPoint(IPAddress.Any, SharedPort);
                if (!string.IsNullOrEmpty(_localEndpoint)) ep = new IPEndPoint(IPAddress.Parse(_localEndpoint), SharedPort);
                _exclusiveConn = new UdpClient { ExclusiveAddressUse = true };
                _exclusiveConn.Client.Bind(ep);
                _exclusiveConn.DontFragment = _dontFragment;
                _exclusiveConn.EnableBroadcast = true;
            }

            Bvlc = new BVLC(this);
        }

        protected void Close()
        {
            _exclusiveConn.Close();
        }

        public void Start()
        {
            _disposing = false;

            Open();

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
                catch (Exception ex) // ICMP port unreachable
                {
                    if (!(ex is ObjectDisposedException && _disposing)) // do not restart data receive when disposing
                    {
                        //restart data receive
                        conn.BeginReceive(OnReceiveData, conn);
                    }
                    return;
                }

                if (rx == 0)    // Empty frame : port scanner maybe
                {
                    //restart data receive
                    conn.BeginReceive(OnReceiveData, conn);
                    return;
                }

                try
                {
                    //verify message
                    BacnetAddress remoteAddress;
                    Convert(ep, out remoteAddress);
                    if (rx < BVLC.BVLC_HEADER_LENGTH)
                    {
                        Trace.TraceWarning("Some garbage data got in");
                    }
                    else
                    {
                        // Basic Header lenght
                        BacnetBvlcFunctions function;
                        int msgLength;
                        var headerLength = Bvlc.Decode(localBuffer, 0, out function, out msgLength, ep);

                        if (headerLength == -1)
                        {
                            Trace.WriteLine("Unknow BVLC Header");
                            return;
                        }

                        // response to BVLC_REGISTER_FOREIGN_DEVICE (could be BVLC_DISTRIBUTE_BROADCAST_TO_NETWORK ... but we are not a BBMD, don't care)
                        if (function == BacnetBvlcFunctions.BVLC_RESULT)
                        {
                            Trace.WriteLine("Receive Register as Foreign Device Response");
                        }

                        // a BVLC_FORWARDED_NPDU frame by a BBMD, change the remote_address to the original one (stored in the BVLC header) 
                        // we don't care about the BBMD address
                        if (function == BacnetBvlcFunctions.BVLC_FORWARDED_NPDU)
                        {
                            var ip = ((long)localBuffer[7] << 24) + ((long)localBuffer[6] << 16) + ((long)localBuffer[5] << 8) + localBuffer[4];
                            var port = (localBuffer[8] << 8) + localBuffer[9];    // 0xbac0 maybe
                            ep = new IPEndPoint(ip, port);

                            Convert(ep, out remoteAddress);

                        }

                        if ((function != BacnetBvlcFunctions.BVLC_ORIGINAL_UNICAST_NPDU) &&
                            (function != BacnetBvlcFunctions.BVLC_ORIGINAL_BROADCAST_NPDU) &&
                            (function != BacnetBvlcFunctions.BVLC_FORWARDED_NPDU))
                            return;

                        if ((MessageRecieved != null) && (rx>headerLength))
                            MessageRecieved(this, localBuffer, headerLength, rx - headerLength, remoteAddress);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Exception in udp recieve: {ex.Message}");
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
                    Trace.TraceError($"Exception in Ip OnRecieveData: {ex.Message}");
                    conn.BeginReceive(OnReceiveData, conn);
                }
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

        public bool WaitForAllTransmits(int timeout)
        {
            //we got no sending queue in udp, so just return true
            return true;
        }

        public static string ConvertToHex(byte[] buffer, int length)
        {
            return BitConverter.ToString(buffer).Replace("-", "");
        }

        // Modif FC : used for BBMD communication
        public int Send(byte[] buffer, int dataLength, IPEndPoint ep)
        {
            try
            {
                // return m_exclusive_conn.Send(buffer, data_length, ep);
                ThreadPool.QueueUserWorkItem(o =>
                    _exclusiveConn.Send(buffer, dataLength, ep), null);
                return dataLength;
            }
            catch
            {
                return 0;
            }
        }

        public int Send(byte[] buffer, int offset, int dataLength, BacnetAddress address, bool waitForTransmission, int timeout)
        {
            if (_exclusiveConn == null) return 0;

            //add header
            var fullLength = dataLength + HeaderLength;
            Bvlc.Encode(buffer, offset - BVLC.BVLC_HEADER_LENGTH, address.net == 0xFFFF
                ? BacnetBvlcFunctions.BVLC_ORIGINAL_BROADCAST_NPDU
                : BacnetBvlcFunctions.BVLC_ORIGINAL_UNICAST_NPDU, fullLength);

            //create end point
            IPEndPoint ep;
            Convert(address, out ep);

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

        public static void Convert(IPEndPoint ep, out BacnetAddress addr)
        {
            var tmp1 = ep.Address.GetAddressBytes();
            var tmp2 = BitConverter.GetBytes((ushort)ep.Port);
            Array.Reverse(tmp2);
            Array.Resize(ref tmp1, tmp1.Length + tmp2.Length);
            Array.Copy(tmp2, 0, tmp1, tmp1.Length - tmp2.Length, tmp2.Length);
            addr = new BacnetAddress(BacnetAddressTypes.IP, 0, tmp1);
        }

        public static void Convert(BacnetAddress addr, out IPEndPoint ep)
        {
            long ipAddress = BitConverter.ToUInt32(addr.adr, 0);
            var port = (ushort)((addr.adr[4] << 8) | (addr.adr[5] << 0));
            ep = new IPEndPoint(ipAddress, port);
        }

        // A lot of problems on Mono (Raspberry) to get the correct broadcast @
        // so this method is overridable (this allows the implementation of operating system specific code)
        // Marc solution http://stackoverflow.com/questions/8119414/how-to-query-the-subnet-masks-using-mono-on-linux for instance
        protected virtual BacnetAddress _GetBroadcastAddress()
        {
            // general broadcast
            var ep = new IPEndPoint(IPAddress.Parse("255.255.255.255"), SharedPort);
            // restricted local broadcast (directed ... routable)
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
                foreach (var ip in adapter.GetIPProperties().UnicastAddresses)
                   if (LocalEndPoint.Address.Equals(ip.Address))
                   {
                       try
                       {
                           var strCurrentIP = ip.Address.ToString().Split('.');
                           var strIPNetMask = ip.IPv4Mask.ToString().Split('.');
                           var broadcastStr = new StringBuilder();
                           for (var i = 0; i < 4; i++)
                           {
                               broadcastStr.Append(
                                   ((byte)(int.Parse(strCurrentIP[i]) | ~int.Parse(strIPNetMask[i]))).ToString());
                               if (i != 3) broadcastStr.Append('.');
                           }
                           ep = new IPEndPoint(IPAddress.Parse(broadcastStr.ToString()), SharedPort);
                       }
                       catch
                       {
                            // on mono IPv4Mask feature not implemented   
                       }
                    }

            BacnetAddress broadcast;
            Convert(ep, out broadcast);
            broadcast.net = 0xFFFF;
            return broadcast;
        }

        public BacnetAddress GetBroadcastAddress()
        {
            return _broadcastAddress ?? (_broadcastAddress = _GetBroadcastAddress());
        }

        public void Dispose()
        {
            _disposing = true;
            _exclusiveConn?.Close();
            _exclusiveConn = null;
            _sharedConn?.Close();
            _sharedConn = null;
        }
    }
}
