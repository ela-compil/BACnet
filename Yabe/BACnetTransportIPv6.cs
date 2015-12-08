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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.BACnet.Serialize;
using System.Diagnostics;
using System.IO.BACnet;
using System.Text.RegularExpressions;
using System.Net;

// based on Addendum 135-2012aj-4

namespace System.IO.BACnet
{
    public class BacnetIpV6UdpProtocolTransport : IBacnetTransport, IDisposable
    {
        private System.Net.Sockets.UdpClient m_shared_conn;
        private System.Net.Sockets.UdpClient m_exclusive_conn;
        private int m_port;

        private BVLCV6 bvlc;

        private bool m_exclusive_port = false;
        private bool m_dont_fragment;
        private int m_max_payload;
        private string m_local_endpoint;

        private int m_VMac;

        public BacnetAddressTypes Type { get { return BacnetAddressTypes.IPV6; } }
        public event MessageRecievedHandler MessageRecieved;
        public int SharedPort { get { return m_port; } }

        // Two frames type, unicast with 10 bytes or broadcast with 7 bytes
        // Here it's the biggest header, resize will be done after, if needed
        public int HeaderLength { get { return BVLCV6.BVLC_HEADER_LENGTH; } }

        public BacnetMaxAdpu MaxAdpuLength { get { return BVLCV6.BVLC_MAX_APDU; } }
        public byte MaxInfoFrames { get { return 0xff; } set { /* ignore */ } }     //the udp doesn't have max info frames
        public int MaxBufferLength { get { return m_max_payload; } }

        public BVLCV6 Bvlc
        {
            get { return bvlc; }
        }

        public BacnetIpV6UdpProtocolTransport(int port, int VMac=-1, bool use_exclusive_port = false, bool dont_fragment = false, int max_payload = 1472, string local_endpoint_ip = "")
        {
            m_port = port;
            m_max_payload = max_payload;
            m_exclusive_port = use_exclusive_port;
            m_dont_fragment = dont_fragment;
            m_local_endpoint = local_endpoint_ip;
            m_VMac = VMac;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BacnetIpV6UdpProtocolTransport)) return false;
            BacnetIpV6UdpProtocolTransport a = (BacnetIpV6UdpProtocolTransport)obj;
            return a.m_port == m_port;
        }

        public override int GetHashCode()
        {
            return m_port.GetHashCode();
        }

        public override string ToString()
        {
            return "UdpV6:" + m_port;
        }

        private void Open()
        {

            System.Net.Sockets.UdpClient multicastListener=null;

            if (!m_exclusive_port)
            {
                /* We need a shared broadcast "listen" port. This is the 0xBAC0 port */
                /* This will enable us to have more than 1 client, on the same machine. Perhaps it's not that important though. */
                /* We (might) only receive the broadcasts on this. Any unicasts to this might be eaten by another local client */
                if (m_shared_conn == null)
                {
                    m_shared_conn = new Net.Sockets.UdpClient(System.Net.Sockets.AddressFamily.InterNetworkV6);
                    m_shared_conn.ExclusiveAddressUse = false;
                    m_shared_conn.Client.SetSocketOption(Net.Sockets.SocketOptionLevel.Socket, Net.Sockets.SocketOptionName.ReuseAddress, true);
                    System.Net.EndPoint ep = new System.Net.IPEndPoint(System.Net.IPAddress.IPv6Any, m_port);
                    if (!string.IsNullOrEmpty(m_local_endpoint)) ep = new System.Net.IPEndPoint(Net.IPAddress.Parse(m_local_endpoint), m_port);
                    m_shared_conn.Client.Bind(ep);

                    multicastListener = m_shared_conn;
                }
                /* This is our own exclusive port. We'll recieve everything sent to this. */
                /* So this is how we'll present our selves to the world */
                if (m_exclusive_conn == null)
                {
                    System.Net.EndPoint ep = new Net.IPEndPoint(System.Net.IPAddress.IPv6Any, 0);
                    if (!string.IsNullOrEmpty(m_local_endpoint)) ep = new Net.IPEndPoint(Net.IPAddress.Parse(m_local_endpoint), 0);
                    m_exclusive_conn = new Net.Sockets.UdpClient((Net.IPEndPoint)ep);
                }
            }
            else
            {
                System.Net.EndPoint ep = new Net.IPEndPoint(System.Net.IPAddress.IPv6Any, m_port);
                if (!string.IsNullOrEmpty(m_local_endpoint)) ep = new Net.IPEndPoint(Net.IPAddress.Parse(m_local_endpoint), m_port);
                m_exclusive_conn = new Net.Sockets.UdpClient(System.Net.Sockets.AddressFamily.InterNetworkV6);
                m_exclusive_conn.ExclusiveAddressUse = true;
                m_exclusive_conn.Client.Bind((Net.IPEndPoint)ep);

                multicastListener = m_exclusive_conn;
            }

            multicastListener.JoinMulticastGroup(IPAddress.Parse("[FF02::BAC0]"));
            multicastListener.JoinMulticastGroup(IPAddress.Parse("[FF04::BAC0]"));
            multicastListener.JoinMulticastGroup(IPAddress.Parse("[FF05::BAC0]"));
            multicastListener.JoinMulticastGroup(IPAddress.Parse("[FF08::BAC0]"));
            multicastListener.JoinMulticastGroup(IPAddress.Parse("[FF0E::BAC0]"));

            bvlc = new BVLCV6(this, m_VMac);
        }

        private void Close()
        {
            if (m_shared_conn != null)
                m_shared_conn.BeginReceive(OnReceiveData, m_shared_conn);

            if (m_exclusive_conn != null)
                m_exclusive_conn.BeginReceive(OnReceiveData, m_exclusive_conn);
        }

        public void Start()
        {
            Open();

            if (m_shared_conn != null)
                m_shared_conn.BeginReceive(OnReceiveData, m_shared_conn);

            if (m_exclusive_conn != null)
                m_exclusive_conn.BeginReceive(OnReceiveData, m_exclusive_conn);

        }

        private void OnReceiveData(IAsyncResult asyncResult)
        {
            System.Net.Sockets.UdpClient conn = (System.Net.Sockets.UdpClient)asyncResult.AsyncState;
            try
            {
                System.Net.IPEndPoint ep = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
                byte[] local_buffer;
                int rx = 0;

                try
                {
                    local_buffer = conn.EndReceive(asyncResult, ref ep);
                    rx = local_buffer.Length;
                }
                catch (Exception) // ICMP port unreachable
                {
                    //restart data receive
                    conn.BeginReceive(OnReceiveData, conn);
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
                    BacnetAddress remote_address;
                    Convert((System.Net.IPEndPoint)ep, out remote_address);                    
                    BacnetBvlcV6Functions function;
                    int msg_length;
                    if (rx < BVLCV6.BVLC_HEADER_LENGTH-3)
                    {
                        Trace.TraceWarning("Some garbage data got in");
                    }
                    else
                    {
                        // Basic Header lenght
                        int HEADER_LENGTH = bvlc.Decode(local_buffer, 0, out function, out msg_length, ep, remote_address);

                        if (HEADER_LENGTH == -1)
                        {
                            Trace.WriteLine("Unknow BVLC Header");
                            return;
                        }

                        // response to BVLC_REGISTER_FOREIGN_DEVICE (could be BVLC_DISTRIBUTE_BROADCAST_TO_NETWORK ... but we are not a BBMD, don't care)
                        if (function == BacnetBvlcV6Functions.BVLC_RESULT)
                        {
                            Trace.WriteLine("Receive Register as Foreign Device Response");
                        }

                        // a BVLC_FORWARDED_NPDU frame by a BBMD, change the remote_address to the original one (stored in the BVLC header) 
                        // we don't care about the BBMD address
                        if (function == BacnetBvlcV6Functions.BVLC_FORWARDED_NPDU)
                        {
                            Array.Copy(local_buffer,7,remote_address.adr,0,18);
                        }

                        if ((function == BacnetBvlcV6Functions.BVLC_ORIGINAL_UNICAST_NPDU) || (function == BacnetBvlcV6Functions.BVLC_ORIGINAL_BROADCAST_NPDU) || (function == BacnetBvlcV6Functions.BVLC_FORWARDED_NPDU))
                            //send to upper layers
                            if ((MessageRecieved != null) && (rx > HEADER_LENGTH)) MessageRecieved(this, local_buffer, HEADER_LENGTH, rx - HEADER_LENGTH, remote_address);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Exception in udp recieve: " + ex.Message);
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
                    Trace.TraceError("Exception in Ip OnRecieveData: " + ex.Message);
                    conn.BeginReceive(OnReceiveData, conn);
                }
            }
        }

        public bool WaitForAllTransmits(int timeout)
        {
            //we got no sending queue in udp, so just return true
            return true;
        }

        public static string ConvertToHex(byte[] buffer, int length)
        {
            string ret = "";

            for (int i = 0; i < length; i++)
                ret += buffer[i].ToString("X2");

            return ret;
        }

        // Modif FC : used for BBMD communication
        public int Send(byte[] buffer, int data_length, System.Net.IPEndPoint ep)
        {
            try
            {
                // return m_exclusive_conn.Send(buffer, data_length, ep);
                System.Threading.ThreadPool.QueueUserWorkItem((o) => m_exclusive_conn.Send(buffer, data_length, ep), null);
                return data_length;
            }
            catch
            {
                return 0;
            }
        }

        public int Send(byte[] buffer, int offset, int data_length, BacnetAddress address, bool wait_for_transmission, int timeout)
        {
            if (m_exclusive_conn == null) return 0;

            //add header
            int full_length = data_length + HeaderLength;

            if (address.net == 0xFFFF)
            {                               
                byte[] newBuffer = new byte[full_length - 3];
                Array.Copy(buffer, 3, newBuffer, 0, full_length - 3);
                full_length -= 3;
                buffer = newBuffer;
                bvlc.Encode(buffer, offset - BVLCV6.BVLC_HEADER_LENGTH, BacnetBvlcV6Functions.BVLC_ORIGINAL_BROADCAST_NPDU, full_length, address);
            }
            else
                bvlc.Encode(buffer, offset - BVLCV6.BVLC_HEADER_LENGTH, BacnetBvlcV6Functions.BVLC_ORIGINAL_UNICAST_NPDU, full_length, address);

            //create end point
            System.Net.IPEndPoint ep;
            Convert(address, out ep);

            try
            {
                //send
                return m_exclusive_conn.Send(buffer, full_length, ep);    //multicast are transported from our local unicast socket also
            }
            catch
            {
                return 0;
            }
        }

        public static void Convert(System.Net.IPEndPoint ep, out BacnetAddress addr)
        {
            byte[] tmp1 = ep.Address.GetAddressBytes();
            byte[] tmp2 = BitConverter.GetBytes((ushort)ep.Port);
            Array.Reverse(tmp2);
            Array.Resize<byte>(ref tmp1, tmp1.Length + tmp2.Length);
            Array.Copy(tmp2, 0, tmp1, tmp1.Length - tmp2.Length, tmp2.Length);
            addr = new BacnetAddress(BacnetAddressTypes.IPV6, 0, tmp1);
        }

        public static void Convert(BacnetAddress addr, out System.Net.IPEndPoint ep)
        {
            ushort port = (ushort)((addr.adr[16] << 8) | (addr.adr[17] << 0));
            byte[] Ipv6 = new byte[16];
            Array.Copy(addr.adr, Ipv6, 16);
            ep = new System.Net.IPEndPoint(new IPAddress(Ipv6), (int)port);
        }

        public BacnetAddress GetBroadcastAddress()
        {
            BacnetAddress ret;
            // could be FF08, FF05, FF04, FF02
            System.Net.IPEndPoint ep = new Net.IPEndPoint(IPAddress.Parse("[FF0E::BAC0]"), m_port);
            Convert(ep, out ret);
            ret.net = 0xFFFF;

            return ret;
        }

        public void Dispose()
        {
            try
            {
                m_exclusive_conn.Close();
                m_exclusive_conn = null;
                m_shared_conn.Close(); // maybe an exception if null
                m_shared_conn = null;
            }
            catch { }
        }
    }

    public enum BacnetBvlcV6Functions : byte
    {
        BVLC_RESULT = 0,
        BVLC_ORIGINAL_UNICAST_NPDU = 1,
        BVLC_ORIGINAL_BROADCAST_NPDU = 2,
        BVLC_ADDRESS_RESOLUTION = 3,
        BVLC_FORWARDED_ADDRESS_RESOLUTION = 4,
        BVLC_ADDRESS_RESOLUTION_ACK = 5,
        BVLC_VIRTUAL_ADDRESS_RESOLUTION = 6,
        BVLC_VIRTUAL_ADDRESS_RESOLUTION_ACK = 7,
        BVLC_FORWARDED_NPDU = 8,
        BVLC_REGISTER_FOREIGN_DEVICE = 9,
        BVLC_DELETE_FOREIGN_DEVICE_TABLE_ENTRY = 0xA,
        BVLC_SECURE_BVLC = 0xB,
        BVLC_DISTRIBUTE_BROADCAST_TO_NETWORK = 0xC
    };

    public enum BacnetBvlcV6Results : ushort
    {
        BVLC_RESULT_SUCCESSFUL_COMPLETION = 0x0000,
        BVLC_RESULT_ADDRESS_RESOLUTION_NAK = 0x0030,
        BVLC_RESULT_VIRTUAL_ADDRESS_RESOLUTION_NAK = 0x0060,
        BVLC_RESULT_REGISTER_FOREIGN_DEVICE_NAK = 0X0090,
        BVLC_RESULT_DISTRIBUTE_BROADCAST_TO_NETWORK_NAK = 0x00B0
    };

    public class BVLCV6
    {
        BacnetIpV6UdpProtocolTransport MyTransport;
        String BroadcastAdd;

        public const byte BVLL_TYPE_BACNET_IPV6 = 0x82;
        public const byte BVLC_HEADER_LENGTH = 10; // Not all the time, could be 7 for bacnet broadcast
        public const BacnetMaxAdpu BVLC_MAX_APDU = BacnetMaxAdpu.MAX_APDU1476;

        public byte[] VMAC=new byte[3];

        public BVLCV6(BacnetIpV6UdpProtocolTransport Transport, int VMAC)
        {
            MyTransport = Transport;
            BroadcastAdd = MyTransport.GetBroadcastAddress().ToString().Split(':')[0];

            if (VMAC == -1)
            {
                new Random().NextBytes(this.VMAC);
                this.VMAC[0] = (byte)((this.VMAC[0] & 0x7F) | 0x40); // ensure 01xxxxxx on the High byte
                // normaly unicity should be tested on the net
            }
            else // Device Id is the Vmac Id
            {
                this.VMAC[0] = (byte)((VMAC >> 16) & 0x3F); // ensure the 2 high bits are 0 on the High byte
                this.VMAC[1] = (byte)((VMAC >> 8) & 0xFF);
                this.VMAC[2] = (byte)(VMAC & 0xFF);
            }
        }

        private void First7BytesHeaderEncode(byte[] b, BacnetBvlcV6Functions function, int msg_length)
        {
            b[0] = BVLL_TYPE_BACNET_IPV6;
            b[1] = (byte)function;
            b[2] = (byte)(((msg_length) & 0xFF00) >> 8);
            b[3] = (byte)(((msg_length) & 0x00FF) >> 0);
            b[4] = VMAC[0];
            b[5] = VMAC[1];
            b[6] = VMAC[2];
        }

        // Send ack or nack
        private void SendResult(System.Net.IPEndPoint sender, BacnetBvlcV6Results ResultCode)
        {
            byte[] b = new byte[9];
            First7BytesHeaderEncode(b,  BacnetBvlcV6Functions.BVLC_RESULT, 9);
            b[7] = (byte)(((ushort)ResultCode & 0xFF00) >> 8);
            b[8] = (byte)((ushort)ResultCode & 0xFF);
            MyTransport.Send(b, 9, sender);
        }

        // Send ack
        private void SendAddressResolutionAck(System.Net.IPEndPoint sender, byte[] VMacDest, BacnetBvlcV6Functions function)
        {
            byte[] b = new byte[10];
            First7BytesHeaderEncode(b, function, 10);
            Array.Copy(b, 7, VMAC, 0, 3);
            MyTransport.Send(b, 10, sender);
        }

        // Encode is called by internal services if the BBMD is also an active device
        public int Encode(byte[] buffer, int offset, BacnetBvlcV6Functions function, int msg_length, BacnetAddress address)
        {
            // offset always 0, we are the first after udp

            First7BytesHeaderEncode(buffer, function, msg_length);

            // do the job
            if (function==BacnetBvlcV6Functions.BVLC_ORIGINAL_BROADCAST_NPDU)
                return 7; // ready to send
            if (function == BacnetBvlcV6Functions.BVLC_ORIGINAL_UNICAST_NPDU)
            {
                buffer[7] = address.VMac[0];
                buffer[8] = address.VMac[1];
                buffer[9] = address.VMac[2];
                return 10; // ready to send
            }

            return 0; // ?
        }

        // Decode is called each time an Udp Frame is received
        public int Decode(byte[] buffer, int offset, out BacnetBvlcV6Functions function, out int msg_length, System.Net.IPEndPoint sender, BacnetAddress remote_address)
        {

            // offset always 0, we are the first after udp
            // and a previous test by the caller guaranteed at least 4 bytes into the buffer

            function = (BacnetBvlcV6Functions)buffer[1];
            msg_length = (buffer[2] << 8) | (buffer[3] << 0);
            if ((buffer[0] != BVLL_TYPE_BACNET_IPV6) || (buffer.Length != msg_length)) return -1;

            Array.Copy(buffer, 4, remote_address.VMac, 0, 3);

            switch (function)
            {
                case BacnetBvlcV6Functions.BVLC_RESULT:
                    return 9;   // only for the upper layers
                case BacnetBvlcV6Functions.BVLC_ORIGINAL_UNICAST_NPDU:
                    return 10;   // only for the upper layers
                case BacnetBvlcV6Functions.BVLC_ORIGINAL_BROADCAST_NPDU: 
                    return 7;   // also for the upper layers
                case BacnetBvlcV6Functions.BVLC_ADDRESS_RESOLUTION:
                    // need to verify that the VMAC is mine
                    if ((VMAC[0]==buffer[7])&&(VMAC[1]==buffer[8])&&(VMAC[2]==buffer[9]))
                        SendAddressResolutionAck(sender,remote_address.VMac ,BacnetBvlcV6Functions.BVLC_ADDRESS_RESOLUTION_ACK);
                    return -1;  // not for the upper layers
                case BacnetBvlcV6Functions.BVLC_FORWARDED_ADDRESS_RESOLUTION:
                    // no need to verify the target VMAC
                    SendAddressResolutionAck(sender,remote_address.VMac, BacnetBvlcV6Functions.BVLC_ADDRESS_RESOLUTION_ACK);
                    return -1;  // not for the upper layers
                case BacnetBvlcV6Functions.BVLC_ADDRESS_RESOLUTION_ACK:
                    return -1;  // not for the upper layers
                case BacnetBvlcV6Functions.BVLC_VIRTUAL_ADDRESS_RESOLUTION:
                    // no need to verify the target VMAC
                    SendAddressResolutionAck(sender, remote_address.VMac, BacnetBvlcV6Functions.BVLC_VIRTUAL_ADDRESS_RESOLUTION_ACK); 
                    return -1;  // not for the upper layers
                case BacnetBvlcV6Functions.BVLC_VIRTUAL_ADDRESS_RESOLUTION_ACK:
                    return -1;  // not for the upper layers
                case BacnetBvlcV6Functions.BVLC_FORWARDED_NPDU:   
                    return 25;  //only  for the upper layers
                case BacnetBvlcV6Functions.BVLC_REGISTER_FOREIGN_DEVICE:
                    return -1;  // not for the upper layers
                case BacnetBvlcV6Functions.BVLC_DELETE_FOREIGN_DEVICE_TABLE_ENTRY:
                    return -1;  // not for the upper layers    
                case BacnetBvlcV6Functions.BVLC_SECURE_BVLC:
                    return -1;  // not for the upper layers
                case BacnetBvlcV6Functions.BVLC_DISTRIBUTE_BROADCAST_TO_NETWORK:
                    return -1;  // not for the upper layers
                // error encoding function or experimental one
                default:
                    return -1;
            }
        }
    }
}
