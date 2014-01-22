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

using System;
using System.Collections.Generic;
using System.Text;
using System.IO.BACnet.Serialize;
using System.Diagnostics;

namespace System.IO.BACnet
{
    public delegate void MessageRecievedHandler(IBacnetTransport sender, byte[] buffer, int offset, int msg_length, BACNET_ADDRESS remote_address);

    public interface IBacnetTransport : IDisposable
    {
        event MessageRecievedHandler MessageRecieved;
        int Send(byte[] buffer, int offset, int data_length, BACNET_ADDRESS address, bool wait_for_transmission, int timeout);
        BACNET_ADDRESS GetBroadcastAddress();
        AddressTypes Type { get; }
        void Start();

        int HeaderLength { get; }
        int MaxBufferLength { get; }
        BACNET_MAX_ADPU MaxAdpuLength { get; }
    }

    /// <summary>
    /// This is the standard BACNet udp transport
    /// </summary>
    public class BacnetIpUdpProtocolTransport : IBacnetTransport, IDisposable
    {
        private System.Net.Sockets.UdpClient m_shared_conn;
        private System.Net.Sockets.UdpClient m_exclusive_conn;
        private int m_port;
        private byte[] m_local_buffer;
        private bool m_exclusive_port = false;

        public AddressTypes Type { get { return AddressTypes.IP; } }
        public event MessageRecievedHandler MessageRecieved;
        public int SharedPort { get { return m_port; } }
        public int ExclusivePort { get { return ((Net.IPEndPoint)m_exclusive_conn.Client.LocalEndPoint).Port; } }

        public int HeaderLength { get { return BVLC.BVLC_HEADER_LENGTH; } }
        public int MaxBufferLength { get { return BVLC.MSTP_MAX_NDPU + BVLC.BVLC_HEADER_LENGTH; } }
        public BACNET_MAX_ADPU MaxAdpuLength { get { return BVLC.BVLC_MAX_APDU; } }

        public BacnetIpUdpProtocolTransport(int port, bool use_exclusive_port = false)
        {
            m_port = port;
            m_local_buffer = new byte[MaxBufferLength];
            m_exclusive_port = use_exclusive_port;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BacnetIpUdpProtocolTransport)) return false;
            BacnetIpUdpProtocolTransport a = (BacnetIpUdpProtocolTransport)obj;
            return a.m_port == m_port;
        }

        public override int GetHashCode()
        {
            return m_port.GetHashCode();
        }

        public override string ToString()
        {
            return "Udp:" + m_port;
        }

        private void Open()
        {
            if (!m_exclusive_port)
            {
                /* We need a shared broadcast "listen" port. This is the 0xBAC0 port */
                /* This will enable us to have more than 1 client, on the same machine. Perhaps it's not that important though. */
                /* We (might) only recieve the broadcasts on this. Any unicasts to this might be eaten by another local client */
                if (m_shared_conn == null)
                {
                    m_shared_conn = new Net.Sockets.UdpClient();
                    m_shared_conn.ExclusiveAddressUse = false;
                    m_shared_conn.Client.SetSocketOption(Net.Sockets.SocketOptionLevel.Socket, Net.Sockets.SocketOptionName.ReuseAddress, true);
                    System.Net.EndPoint ep = new System.Net.IPEndPoint(System.Net.IPAddress.Any, m_port);
                    m_shared_conn.Client.Bind(ep);
                }
                /* This is our own exclusive port. We'll recieve everything sent to this. */
                /* So this is how we'll present our selves to the world */
                if (m_exclusive_conn == null)
                {
                    m_exclusive_conn = new Net.Sockets.UdpClient(0);
                }
            }
            else
            {
                m_exclusive_conn = new Net.Sockets.UdpClient(m_port);
            }
        }

        public void Start()
        {
            Open();
            if (m_shared_conn != null) StartRecieve(m_shared_conn);
            StartRecieve(m_exclusive_conn);
        }

        private void StartRecieve(System.Net.Sockets.UdpClient conn)
        {
            System.Net.EndPoint ep = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
            conn.Client.BeginReceiveFrom(m_local_buffer, 0, m_local_buffer.Length, Net.Sockets.SocketFlags.None, ref ep, OnRecieveData, conn);
        }

        private void OnRecieveData(IAsyncResult asyncResult)
        {
            try
            {
                if (m_exclusive_conn == null) return;
                System.Net.EndPoint ep = new Net.IPEndPoint(System.Net.IPAddress.Any, 0);
                System.Net.Sockets.UdpClient conn = (System.Net.Sockets.UdpClient)asyncResult.AsyncState;
                int rx = conn.Client.EndReceiveFrom(asyncResult, ref ep);

                //closed (can't happen in udp I think)
                if (rx == 0)
                {
                    Trace.TraceError("Udp connection closed?");
                    return;
                }

                try
                {
                    //verify message
                    BACNET_ADDRESS remote_address;
                    Convert((System.Net.IPEndPoint)ep, out remote_address);
                    BACNET_BVLC_FUNCTION function;
                    int msg_length;
                    if (rx < BVLC.BVLC_HEADER_LENGTH || BVLC.Decode(m_local_buffer, 0, out function, out msg_length) < 0)
                    {
                        Trace.TraceWarning("Some garbage data got in");
                    }
                    else
                    {
                        //send to upper layers
                        if (MessageRecieved != null) MessageRecieved(this, m_local_buffer, BVLC.BVLC_HEADER_LENGTH, rx - BVLC.BVLC_HEADER_LENGTH, remote_address);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Exception in udp recieve: " + ex.Message);
                }
                finally
                {
                    //restart data recieve
                    StartRecieve(conn);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception in Ip OnRecieveData: " + ex.Message);
            }
        }

        public static string ConvertToHex(byte[] buffer, int length)
        {
            string ret = "";

            for (int i = 0; i < length; i++)
                ret += buffer[i].ToString("X2");

            return ret;
        }

        public int Send(byte[] buffer, int offset, int data_length, BACNET_ADDRESS address, bool wait_for_transmission, int timeout)
        {
            if (m_exclusive_conn == null) return 0;

            //add header
            int full_length = data_length + HeaderLength;
            BVLC.Encode(buffer, offset - BVLC.BVLC_HEADER_LENGTH, address.net == 0xFFFF ? BACNET_BVLC_FUNCTION.BVLC_ORIGINAL_BROADCAST_NPDU : BACNET_BVLC_FUNCTION.BVLC_ORIGINAL_UNICAST_NPDU, full_length);

            //create end point
            System.Net.IPEndPoint ep;
            Convert(address, out ep);

            //send
            return m_exclusive_conn.Send(buffer, full_length, ep);    //broadcasts are transported from our local unicast socket also
        }

        private static void Convert(System.Net.IPEndPoint ep, out BACNET_ADDRESS addr)
        {
            byte[] tmp1 = ep.Address.GetAddressBytes();
            byte[] tmp2 = BitConverter.GetBytes((ushort)ep.Port);
            Array.Reverse(tmp2);
            Array.Resize<byte>(ref tmp1, tmp1.Length + tmp2.Length);
            Array.Copy(tmp2, 0, tmp1, tmp1.Length - tmp2.Length, tmp2.Length);
            addr = new BACNET_ADDRESS(AddressTypes.IP, 0, tmp1);
        }

        private static void Convert(BACNET_ADDRESS addr, out System.Net.IPEndPoint ep)
        {
            long ip_address = BitConverter.ToUInt32(addr.adr, 0);
            ushort port = (ushort)((addr.adr[4] << 8) | (addr.adr[5] << 0));
            ep = new System.Net.IPEndPoint(ip_address, (int)port);
        }

        public BACNET_ADDRESS GetBroadcastAddress()
        {
            System.Net.IPEndPoint ep = new Net.IPEndPoint(System.Net.IPAddress.Parse("255.255.255.255"), m_port);
            BACNET_ADDRESS broadcast;
            Convert(ep, out broadcast);
            broadcast.net = 0xFFFF;
            return broadcast;
        }

        public void Dispose()
        {
            m_shared_conn.Close();
            m_shared_conn = null;
            m_exclusive_conn.Close();
            m_exclusive_conn = null;
        }
    }

    public interface IBacnetSerialTransport : IDisposable
    {
        void Open();
        void Write(byte[] buffer, int offset, int length);
        int Read(byte[] buffer, int offset, int length, int timeout_ms);
        void Close();
        int BytesToRead { get; }
    }

    public class BacnetSerialPortTransport : IBacnetSerialTransport
    {
        private string m_port_name;
        private int m_baud_rate;
        private System.IO.Ports.SerialPort m_port;

        public BacnetSerialPortTransport(string port_name, int baud_rate)
        {
            m_port_name = port_name;
            m_baud_rate = baud_rate;
            m_port = new Ports.SerialPort(m_port_name, m_baud_rate, System.IO.Ports.Parity.None, 8, Ports.StopBits.One);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            else if (!(obj is BacnetSerialPortTransport)) return false;
            BacnetSerialPortTransport a = (BacnetSerialPortTransport)obj;
            return m_port_name.Equals(a.m_port_name);
        }

        public override int GetHashCode()
        {
            return m_port_name.GetHashCode();
        }

        public override string ToString()
        {
            return m_port_name.ToString();
        }

        public void Open()
        {
            m_port.Open();
        }

        public void Write(byte[] buffer, int offset, int length)
        {
            if (m_port == null) return;
            m_port.Write(buffer, offset, length);
        }

        public int Read(byte[] buffer, int offset, int length, int timeout_ms)
        {
            if (m_port == null) return 0;
            m_port.ReadTimeout = timeout_ms;
            try
            {
                int rx = m_port.Read(buffer, offset, length);
                return rx;
            }
            catch (TimeoutException)
            {
                return -BacnetMstpProtocolTransport.ETIMEDOUT;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public void Close()
        {
            if (m_port == null) return;
            m_port.Close();
        }

        public int BytesToRead
        {
            get { return m_port == null ? 0 : m_port.BytesToRead; }
        }

        public void Dispose()
        {
            Close();
        }
    }

    public class BacnetPipeTransport : IBacnetSerialTransport
    {
        private string m_name;
        private System.IO.Pipes.PipeStream m_conn;
        private IAsyncResult m_current_read;
        private IAsyncResult m_current_connect;

        public string Name { get { return m_name; } }

        public BacnetPipeTransport(string name, bool is_server = false)
        {
            m_name = name;
            if (!is_server)
                m_conn = new System.IO.Pipes.NamedPipeClientStream(".", m_name, System.IO.Pipes.PipeDirection.InOut, System.IO.Pipes.PipeOptions.Asynchronous);
            else
                m_conn = new System.IO.Pipes.NamedPipeServerStream(m_name, Pipes.PipeDirection.InOut, 20, Pipes.PipeTransmissionMode.Byte, Pipes.PipeOptions.Asynchronous);
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", EntryPoint = "PeekNamedPipe", SetLastError = true)]
        private static extern bool PeekNamedPipe(IntPtr handle, IntPtr buffer, uint nBufferSize, IntPtr bytesRead, ref uint bytesAvail, IntPtr BytesLeftThisMessage);

        public int PeekPipe()
        {
            uint bytes_avail = 0;
            if (PeekNamedPipe(m_conn.SafePipeHandle.DangerousGetHandle(), IntPtr.Zero, 0, IntPtr.Zero, ref bytes_avail, IntPtr.Zero))
                return (int)bytes_avail;
            else
                return 0;
        }

        public override string ToString()
        {
            return m_name.ToString();
        }

        public override int GetHashCode()
        {
            return m_name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            else if (!(obj is BacnetPipeTransport)) return false;
            BacnetPipeTransport a = (BacnetPipeTransport)obj;
            return m_name.Equals(a.m_name);
        }

        public void Open()
        {
            if (m_conn == null) return;
            if (m_conn is System.IO.Pipes.NamedPipeClientStream)
                ((System.IO.Pipes.NamedPipeClientStream)m_conn).Connect(3000);
        }

        public void Write(byte[] buffer, int offset, int length)
        {
            if (!m_conn.IsConnected) return;
            try
            {
                m_conn.Write(buffer, offset, length);
                m_conn.Flush();
            }
            catch (System.IO.IOException)
            {
                Disconnect();
            }
        }

        private void Disconnect()
        {
            if (m_conn is System.IO.Pipes.NamedPipeServerStream)
            {
                try
                {
                    ((System.IO.Pipes.NamedPipeServerStream)m_conn).Disconnect();
                }
                catch
                {
                }
                m_current_connect = null;
            }
            m_current_read = null;
        }

        private bool WaitForConnection(int timeout_ms)
        {
            if (m_conn.IsConnected) return true;
            if (m_conn is System.IO.Pipes.NamedPipeServerStream)
            {
                System.IO.Pipes.NamedPipeServerStream server = (System.IO.Pipes.NamedPipeServerStream)m_conn;
                if (m_current_connect == null)
                {
                    try
                    {
                        m_current_connect = server.BeginWaitForConnection(null, null);
                    }
                    catch (System.IO.IOException)
                    {
                        Disconnect();
                        m_current_connect = server.BeginWaitForConnection(null, null);
                    }
                }

                if (m_current_connect.IsCompleted || m_current_connect.AsyncWaitHandle.WaitOne(timeout_ms))
                {
                    try
                    {
                        server.EndWaitForConnection(m_current_connect);
                    }
                    catch (System.IO.IOException)
                    {
                        Disconnect();
                    }
                    m_current_connect = null;
                }
                return m_conn.IsConnected;
            }
            else
                return true;
        }

        public int Read(byte[] buffer, int offset, int length, int timeout_ms)
        {
            if (!WaitForConnection(timeout_ms)) return -BacnetMstpProtocolTransport.ETIMEDOUT;

            if (m_current_read == null)
            {
                try
                {
                    m_current_read = m_conn.BeginRead(buffer, offset, length, null, null);
                }
                catch (Exception)
                {
                    Disconnect();
                    return -1;
                }
            }

            if (m_current_read.IsCompleted || m_current_read.AsyncWaitHandle.WaitOne(timeout_ms))
            {
                try
                {
                    int rx = m_conn.EndRead(m_current_read);
                    m_current_read = null;
                    return rx;
                }
                catch (Exception)
                {
                    Disconnect();
                    return -1;
                }
            }
            else
                return -BacnetMstpProtocolTransport.ETIMEDOUT;
        }

        public void Close()
        {
            if (m_conn == null) return;
            m_conn.Close();
            m_conn = null;
        }

        public int BytesToRead
        {
            get
            {
                return PeekPipe();
            }
        }

        public static string[] AvailablePorts
        {
            get
            {
                try
                {
                    String[] listOfPipes = System.IO.Directory.GetFiles(@"\\.\pipe\");
                    if (listOfPipes == null) return new string[0];
                    else
                    {
                        for (int i = 0; i < listOfPipes.Length; i++)
                            listOfPipes[i] = listOfPipes[i].Replace(@"\\.\pipe\", "");
                        return listOfPipes;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Exception in AvailablePorts: " + ex.Message);
                    return InteropAvailablePorts;
                }
            }
        }

        #region " Interop Get Pipe Names "

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);
        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool FindClose(IntPtr hFindFile);

        static string[] InteropAvailablePorts
        {
            get
            {
                List<string> ret = new List<string>();
                WIN32_FIND_DATA data;
                IntPtr handle = FindFirstFile(@"\\.\pipe\*", out data);
                if (handle != new IntPtr(-1))
                {
                    do
                        ret.Add(data.cFileName);
                    while (FindNextFile(handle, out data) != 0);
                    FindClose(handle);
                }
                return ret.ToArray();
            }
        }
        #endregion

        public void Dispose()
        {
            Close();
        }
    }

    /// <summary>
    /// This is the standard BACNet MSTP transport
    /// </summary>
    public class BacnetMstpProtocolTransport : IBacnetTransport, IDisposable
    {
        private IBacnetSerialTransport m_port;
        private short m_TS;             //"This Station," the MAC address of this node. TS is generally read from a hardware DIP switch, or from nonvolatile memory. Valid values for TS are 0 to 254. The value 255 is used to denote broadcast when used as a destination address but is not allowed as a value for TS.
        private byte m_NS;              //"Next Station," the MAC address of the node to which This Station passes the token. If the Next Station is unknown, NS shall be equal to TS
        private byte m_PS;              //"Poll Station," the MAC address of the node to which This Station last sent a Poll For Master. This is used during token maintenance
        private byte m_max_master;
        private byte m_max_info_frames;
        private byte[] m_local_buffer;
        private int m_local_offset;
        private System.Threading.Thread m_transmit_thread;
        private byte m_frame_count = 0;
        private byte m_token_count = 0;
        private byte m_max_poll = 50;                //The number of tokens received or used before a Poll For Master cycle is executed
        private bool m_sole_master = false;
        private byte m_retry_token = 1;
        private byte m_reply_source;
        private System.Threading.ManualResetEvent m_reply_mutex = new Threading.ManualResetEvent(false);
        private MessageFrame m_reply = null;
        private LinkedList<MessageFrame> m_send_queue = new LinkedList<MessageFrame>();

        public const int T_FRAME_ABORT = 80;        //ms    The minimum time without a DataAvailable or ReceiveError event within a frame before a receiving node may discard the frame
        public const int T_NO_TOKEN = 500;          //ms    The time without a DataAvailable or ReceiveError event before declaration of loss of token
        public const int T_REPLY_TIMEOUT = 295;     //ms    The minimum time without a DataAvailable or ReceiveError event that a node must wait for a station to begin replying to a confirmed request
        public const int T_USAGE_TIMEOUT = 95;      //ms    The minimum time without a DataAvailable or ReceiveError event that a node must wait for a remote node to begin using a token or replying to a Poll For Master frame:
        public const int T_REPLY_DELAY = 250;       //ms    The maximum time a node may wait after reception of a frame that expects a reply before sending the first octet of a reply or Reply Postponed frame
        public const int ETIMEDOUT = 110;

        public AddressTypes Type { get { return AddressTypes.MSTP; } }
        public short SourceAddress { get { return m_TS; } set { m_TS = value; } }
        public byte MaxMaster { get { return m_max_master; } set { m_max_master = value; } }
        public byte MaxInfoFrames { get { return m_max_info_frames; } set { m_max_info_frames = value; } }
        public bool StateLogging { get; set; }

        public int HeaderLength { get { return MSTP.MSTP_HEADER_LENGTH; } }
        public int MaxBufferLength { get { return MSTP.MSTP_MAX_NDPU + MSTP.MSTP_HEADER_LENGTH + 2 + 1; } }
        public BACNET_MAX_ADPU MaxAdpuLength { get { return MSTP.MSTP_MAX_APDU; } }

        public delegate void FrameRecievedHandler(BacnetMstpProtocolTransport sender, MSTP_FRAME_TYPE frame_type, byte destination_address, byte source_address, int msg_length);
        public event MessageRecievedHandler MessageRecieved;
        public event FrameRecievedHandler FrameRecieved;

        public BacnetMstpProtocolTransport(IBacnetSerialTransport transport, short source_address = -1, byte max_master = 127, byte max_info_frames = 1)
        {
            m_max_info_frames = max_info_frames;
            m_TS = source_address;
            m_max_master = max_master;
            m_local_buffer = new byte[MaxBufferLength];
            m_port = transport;
        }

        public BacnetMstpProtocolTransport(string port_name, int baud_rate, short source_address = -1, byte max_master = 127, byte max_info_frames = 1)
            : this(new BacnetSerialPortTransport(port_name, baud_rate), source_address, max_master, max_info_frames)
        {
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            else if (!(obj is BacnetMstpProtocolTransport)) return false;
            BacnetMstpProtocolTransport a = (BacnetMstpProtocolTransport)obj;
            return m_port.Equals(a.m_port);
        }

        public override int GetHashCode()
        {
            return m_port.GetHashCode();
        }

        public override string ToString()
        {
            return m_port.ToString();
        }

        public void Start()
        {
            if (m_port == null) return;
            m_port.Open();

            m_transmit_thread = new Threading.Thread(new Threading.ThreadStart(mstp_thread));
            m_transmit_thread.IsBackground = true;
            m_transmit_thread.Name = "MSTP Thread";
            m_transmit_thread.Priority = Threading.ThreadPriority.Highest;
            m_transmit_thread.Start();
        }

        private class MessageFrame
        {
            public MSTP_FRAME_TYPE frame_type;
            public byte destination_address;
            public byte[] data;
            public int data_length;
            public System.Threading.ManualResetEvent send_mutex;
            public MessageFrame(MSTP_FRAME_TYPE frame_type, byte destination_address, byte[] data, int data_length)
            {
                this.frame_type = frame_type;
                this.destination_address = destination_address;
                this.data = data;
                this.data_length = data_length;
                send_mutex = new Threading.ManualResetEvent(false);
            }
        }

        private void QueueFrame(MSTP_FRAME_TYPE frame_type, byte destination_address)
        {
            m_send_queue.AddLast(new MessageFrame(frame_type, destination_address, null, 0));
        }

        private void SendFrame(MSTP_FRAME_TYPE frame_type, byte destination_address)
        {
            SendFrame(new MessageFrame(frame_type, destination_address, null, 0));
        }

        private void SendFrame(MessageFrame frame)
        {
            if (m_TS == -1 || m_port == null) return;
            int tx;
            if (frame.data == null || frame.data.Length == 0)
            {
                byte[] tmp_transmit_buffer = new byte[MSTP.MSTP_HEADER_LENGTH];
                tx = MSTP.Encode(tmp_transmit_buffer, 0, frame.frame_type, frame.destination_address, (byte)m_TS, 0);
                m_port.Write(tmp_transmit_buffer, 0, tx);
            }
            else
            {
                tx = MSTP.Encode(frame.data, 0, frame.frame_type, frame.destination_address, (byte)m_TS, frame.data_length);
                m_port.Write(frame.data, 0, tx);
            }
            frame.send_mutex.Set();

            //debug
            if (StateLogging) Trace.WriteLine("         " + frame.frame_type + " " + frame.destination_address.ToString("X2") + " ");
        }

        private void RemoveCurrentMessage(int msg_length)
        {
            int full_msg_length = MSTP.MSTP_HEADER_LENGTH + msg_length + (msg_length > 0 ? 2 : 0);
            if (m_local_offset > full_msg_length)
                Array.Copy(m_local_buffer, full_msg_length, m_local_buffer, 0, m_local_offset - full_msg_length);
            m_local_offset -= full_msg_length;
        }

        private enum StateChanges
        {
            /* Initializing */
            Reset,
            DoneInitializing,

            /* Idle, NoToken */
            GenerateToken,
            ReceivedDataNeedingReply,
            ReceivedToken,

            /* PollForMaster */
            ReceivedUnexpectedFrame,        //also from WaitForReply
            DoneWithPFM,
            ReceivedReplyToPFM,
            SoleMaster,                     //also from DoneWithToken
            DeclareSoleMaster,

            /* UseToken */
            SendAndWait,
            NothingToSend,
            SendNoWait,

            /* DoneWithToken */
            SendToken,
            ResetMaintenancePFM,
            SendMaintenancePFM,
            SoleMasterRestartMaintenancePFM,
            SendAnotherFrame,
            NextStationUnknown,

            /* WaitForReply */
            ReplyTimeOut,
            InvalidFrame,
            ReceivedReply,
            ReceivedPostpone,

            /* PassToken */
            FindNewSuccessor,
            SawTokenUser,

            /* AnswerDataRequest */
            Reply,
            DeferredReply,
        }

        private StateChanges PollForMaster()
        {
            MSTP_FRAME_TYPE frame_type;
            byte destination_address;
            byte source_address;
            int msg_length;

            while (true)
            {
                //send
                SendFrame(MSTP_FRAME_TYPE.FRAME_TYPE_POLL_FOR_MASTER, m_PS);

                //wait
                GetMessageStatus status = GetNextMessage(T_USAGE_TIMEOUT, out frame_type, out destination_address, out source_address, out msg_length);

                if (status == GetMessageStatus.Good)
                {
                    try
                    {
                        if (frame_type == MSTP_FRAME_TYPE.FRAME_TYPE_REPLY_TO_POLL_FOR_MASTER && destination_address == m_TS)
                        {
                            m_sole_master = false;
                            m_NS = source_address;
                            m_PS = (byte)m_TS;
                            m_token_count = 0;
                            return StateChanges.ReceivedReplyToPFM;
                        }
                        else
                            return StateChanges.ReceivedUnexpectedFrame;
                    }
                    finally
                    {
                        RemoveCurrentMessage(msg_length);
                    }
                }
                else
                {
                    if (m_sole_master)
                    {
                        /* SoleMaster */
                        m_frame_count = 0;
                        return StateChanges.SoleMaster;
                    }
                    else
                    {
                        if (m_NS != m_TS)
                        {
                            /* DoneWithPFM */
                            return StateChanges.DoneWithPFM;
                        }
                        else
                        {
                            if ((m_PS + 1) % (m_max_master + 1) != m_TS)
                            {
                                /* SendNextPFM */
                                m_PS = (byte)((m_PS + 1) % (m_max_master + 1));
                                continue;
                            }
                            else
                            {
                                /* DeclareSoleMaster */
                                m_sole_master = true;
                                m_frame_count = 0;
                                return StateChanges.DeclareSoleMaster;
                            }
                        }
                    }
                }
            }
        }

        private StateChanges DoneWithToken()
        {
            if (m_frame_count < m_max_info_frames)
            {
                /* SendAnotherFrame */
                return StateChanges.SendAnotherFrame;
            }
            else if (!m_sole_master && m_NS == m_TS)
            {
                /* NextStationUnknown */
                m_PS = (byte)((m_TS + 1) % (m_max_master + 1));
                return StateChanges.NextStationUnknown;
            }
            else if (m_token_count < (m_max_poll - 1))
            {
                m_token_count++;
                if (m_sole_master && m_NS != ((m_TS+1)%(m_max_master+1)))
                {
                    /* SoleMaster */
                    m_frame_count = 0;
                    return StateChanges.SoleMaster;
                }
                else
                {
                    /* SendToken */
                    return StateChanges.SendToken;
                }
            }
            else if ((m_PS + 1) % (m_max_master + 1) == m_NS)
            {
                if (!m_sole_master)
                {
                    /* ResetMaintenancePFM */
                    m_PS = (byte)m_TS;
                    m_token_count = 1;
                    return StateChanges.ResetMaintenancePFM;
                }
                else
                {
                    /* SoleMasterRestartMaintenancePFM */
                    m_PS = (byte)((m_NS + 1) % (m_max_master + 1));
                    m_NS = (byte)m_TS;
                    m_token_count = 1;
                    return StateChanges.SoleMasterRestartMaintenancePFM;
                }
            }
            else
            {
                /* SendMaintenancePFM */
                m_PS = (byte)((m_PS + 1) % (m_max_master + 1));
                return StateChanges.SendMaintenancePFM;
            }
        }

        private StateChanges WaitForReply()
        {
            MSTP_FRAME_TYPE frame_type;
            byte destination_address;
            byte source_address;
            int msg_length;

            //fetch message
            GetMessageStatus status = GetNextMessage(T_REPLY_TIMEOUT, out frame_type, out destination_address, out source_address, out msg_length);

            if (status == GetMessageStatus.Good)
            {
                try
                {
                    if (destination_address == (byte)m_TS && (frame_type == MSTP_FRAME_TYPE.FRAME_TYPE_TEST_RESPONSE || frame_type == MSTP_FRAME_TYPE.FRAME_TYPE_BACNET_DATA_NOT_EXPECTING_REPLY))
                    {
                        //signal upper layer
                        if (MessageRecieved != null && frame_type != MSTP_FRAME_TYPE.FRAME_TYPE_TEST_RESPONSE)
                        {
                            BACNET_ADDRESS remote_address = new BACNET_ADDRESS(AddressTypes.MSTP, 0, new byte[] { source_address });
                            try
                            {
                                MessageRecieved(this, m_local_buffer, MSTP.MSTP_HEADER_LENGTH, msg_length, remote_address);
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceError("Exception in MessageRecieved event: " + ex.Message);
                            }
                        }

                        /* ReceivedReply */
                        return StateChanges.ReceivedReply;
                    }
                    else if (frame_type == MSTP_FRAME_TYPE.FRAME_TYPE_REPLY_POSTPONED)
                    {
                        /* ReceivedPostpone */
                        return StateChanges.ReceivedPostpone;
                    }
                    else
                    {
                        /* ReceivedUnexpectedFrame */
                        return StateChanges.ReceivedUnexpectedFrame;
                    }
                }
                finally
                {
                    RemoveCurrentMessage(msg_length);
                }
            }
            else if (status == GetMessageStatus.Timeout)
            {
                /* ReplyTimeout */
                m_frame_count = m_max_info_frames;
                return StateChanges.ReplyTimeOut;
            }
            else
            {
                /* InvalidFrame */
                return StateChanges.InvalidFrame;
            }
        }

        private StateChanges UseToken()
        {
            if (m_send_queue.Count == 0)
            {
                /* NothingToSend */
                m_frame_count = m_max_info_frames;
                return StateChanges.NothingToSend;
            }
            else
            {
                /* SendNoWait / SendAndWait */
                MessageFrame message_frame = m_send_queue.First.Value;
                m_send_queue.RemoveFirst();
                SendFrame(message_frame);
                m_frame_count++;
                if (message_frame.frame_type == MSTP_FRAME_TYPE.FRAME_TYPE_BACNET_DATA_EXPECTING_REPLY || message_frame.frame_type == MSTP_FRAME_TYPE.FRAME_TYPE_TEST_REQUEST)
                    return StateChanges.SendAndWait;
                else
                    return StateChanges.SendNoWait;
            }
        }

        private StateChanges PassToken()
        {
            MSTP_FRAME_TYPE frame_type;
            byte destination_address;
            byte source_address;
            int msg_length;

            for (int i = 0; i <= m_retry_token; i++)
            {
                //send 
                SendFrame(MSTP_FRAME_TYPE.FRAME_TYPE_TOKEN, m_NS);

                //wait for it to be used
                GetMessageStatus status = GetNextMessage(T_USAGE_TIMEOUT, out frame_type, out destination_address, out source_address, out msg_length);
                if (status == GetMessageStatus.Good || status == GetMessageStatus.DecodeError)
                    return StateChanges.SawTokenUser;   //don't remove current message
            }

            //give up
            m_PS = (byte)((m_NS + 1) % (m_max_master + 1));
            m_NS = (byte)m_TS;
            m_token_count = 0;
            return StateChanges.FindNewSuccessor;
        }

        private StateChanges Idle()
        {
            int no_token_timeout = T_NO_TOKEN + 10 * m_TS;
            MSTP_FRAME_TYPE frame_type;
            byte destination_address;
            byte source_address;
            int msg_length;

            while (m_port != null)
            {
                //get message
                GetMessageStatus status = GetNextMessage(no_token_timeout, out frame_type, out destination_address, out source_address, out msg_length);

                if (status == GetMessageStatus.Good)
                {
                    try
                    {
                        if (destination_address == m_TS || destination_address == 0xFF)
                        {
                            switch (frame_type)
                            {
                                case MSTP_FRAME_TYPE.FRAME_TYPE_POLL_FOR_MASTER:
                                    if (destination_address == 0xFF)
                                        QueueFrame(MSTP_FRAME_TYPE.FRAME_TYPE_REPLY_TO_POLL_FOR_MASTER, source_address);
                                    else
                                    {
                                        //respond to PFM
                                        SendFrame(MSTP_FRAME_TYPE.FRAME_TYPE_REPLY_TO_POLL_FOR_MASTER, source_address);
                                    }
                                    break;
                                case MSTP_FRAME_TYPE.FRAME_TYPE_TOKEN:
                                    if (destination_address != 0xFF)
                                    {
                                        m_frame_count = 0;
                                        m_sole_master = false;
                                        return StateChanges.ReceivedToken;
                                    }
                                    break;
                                case MSTP_FRAME_TYPE.FRAME_TYPE_TEST_REQUEST:
                                    if (destination_address == 0xFF)
                                        QueueFrame(MSTP_FRAME_TYPE.FRAME_TYPE_TEST_RESPONSE, source_address);
                                    else
                                    {
                                        //respond to test
                                        SendFrame(MSTP_FRAME_TYPE.FRAME_TYPE_TEST_RESPONSE, source_address);
                                    }
                                    break;
                                case MSTP_FRAME_TYPE.FRAME_TYPE_BACNET_DATA_NOT_EXPECTING_REPLY:
                                case MSTP_FRAME_TYPE.FRAME_TYPE_BACNET_DATA_EXPECTING_REPLY:
                                    //signal upper layer
                                    if (MessageRecieved != null)
                                    {
                                        BACNET_ADDRESS remote_address = new BACNET_ADDRESS(AddressTypes.MSTP, 0, new byte[] { source_address });
                                        try
                                        {
                                            MessageRecieved(this, m_local_buffer, MSTP.MSTP_HEADER_LENGTH, msg_length, remote_address);
                                        }
                                        catch (Exception ex)
                                        {
                                            Trace.TraceError("Exception in MessageRecieved event: " + ex.Message);
                                        }
                                    }
                                    if (frame_type == MSTP_FRAME_TYPE.FRAME_TYPE_BACNET_DATA_EXPECTING_REPLY)
                                    {
                                        m_reply_source = source_address;
                                        m_reply = null;
                                        m_reply_mutex.Reset();
                                        return StateChanges.ReceivedDataNeedingReply;
                                    }
                                    break;
                            }
                        }
                    }
                    finally
                    {
                        RemoveCurrentMessage(msg_length);
                    }
                }
                else if (status == GetMessageStatus.Timeout)
                {
                    /* GenerateToken */
                    m_PS = (byte)((m_TS + 1) % (m_max_master + 1));
                    m_NS = (byte)m_TS;
                    m_token_count = 0;
                    return StateChanges.GenerateToken;
                }
                else if (status == GetMessageStatus.ConnectionClose)
                {
                    Trace.WriteLine("No connection", null);
                }
                else if (status == GetMessageStatus.ConnectionError)
                {
                    Trace.WriteLine("Connection Error", null);
                }
                else
                {
                    Trace.WriteLine("Garbage", null);
                }
            }

            return StateChanges.Reset;
        }

        private StateChanges AnswerDataRequest()
        {
            if (m_reply_mutex.WaitOne(T_REPLY_DELAY))
            {
                SendFrame(m_reply);
                m_send_queue.Remove(m_reply);
                return StateChanges.Reply;
            }
            else
            {
                SendFrame(MSTP_FRAME_TYPE.FRAME_TYPE_REPLY_POSTPONED, m_reply_source);
                return StateChanges.DeferredReply;
            }
        }

        private StateChanges Initialize()
        {
            m_token_count = m_max_poll;     /* cause a Poll For Master to be sent when this node first receives the token */
            m_frame_count = 0;
            m_sole_master = false;
            m_NS = (byte)m_TS;
            m_PS = (byte)m_TS;
            return StateChanges.DoneInitializing;
        }

        private void mstp_thread()
        {
            try
            {
                StateChanges state_change = StateChanges.Reset;

                while (m_port != null)
                {
                    if (StateLogging) Trace.WriteLine(state_change.ToString(), null);
                    switch (state_change)
                    {
                        case StateChanges.Reset:
                            state_change = Initialize();
                            break;
                        case StateChanges.DoneInitializing:
                        case StateChanges.ReceivedUnexpectedFrame:
                        case StateChanges.Reply:
                        case StateChanges.DeferredReply:
                        case StateChanges.SawTokenUser:
                            state_change = Idle();
                            break;
                        case StateChanges.GenerateToken:
                        case StateChanges.FindNewSuccessor:
                        case StateChanges.SendMaintenancePFM:
                        case StateChanges.SoleMasterRestartMaintenancePFM:
                        case StateChanges.NextStationUnknown:
                            state_change = PollForMaster();
                            break;
                        case StateChanges.DoneWithPFM:
                        case StateChanges.ResetMaintenancePFM:
                        case StateChanges.ReceivedReplyToPFM:
                        case StateChanges.SendToken:
                            state_change = PassToken();
                            break;
                        case StateChanges.ReceivedDataNeedingReply:
                            state_change = AnswerDataRequest();
                            break;
                        case StateChanges.ReceivedToken:
                        case StateChanges.SoleMaster:
                        case StateChanges.DeclareSoleMaster:
                        case StateChanges.SendAnotherFrame:
                            state_change = UseToken();
                            break;
                        case StateChanges.NothingToSend:
                        case StateChanges.SendNoWait:
                        case StateChanges.ReplyTimeOut:
                        case StateChanges.InvalidFrame:
                        case StateChanges.ReceivedReply:
                        case StateChanges.ReceivedPostpone:
                            state_change = DoneWithToken();
                            break;
                        case StateChanges.SendAndWait:
                            state_change = WaitForReply();
                            break;
                    }
                }
                Trace.WriteLine("MSTP thread is closing down", null);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception in MSTP thread: " + ex.Message);
            }
        }

        private void RemoveGarbage()
        {
            //scan for preambles
            for (int i = 0; i < (m_local_offset - 1); i++)
            {
                if (m_local_buffer[i] == MSTP.MSTP_PREAMBLE1 && m_local_buffer[i + 1] == MSTP.MSTP_PREAMBLE2)
                {
                    if (i > 0)
                    {
                        //move back
                        Array.Copy(m_local_buffer, i, m_local_buffer, 0, m_local_offset - i);
                        m_local_offset -= i;
                        Trace.WriteLine("Garbage", null);
                    }
                    return;
                }
            }

            //one preamble?
            if (m_local_offset > 0 && m_local_buffer[m_local_offset - 1] == MSTP.MSTP_PREAMBLE1)
            {
                m_local_buffer[0] = MSTP.MSTP_PREAMBLE1;
                m_local_offset = 1;
                Trace.WriteLine("Garbage", null);
                return;
            }

            //no preamble?
            if (m_local_offset > 0)
            {
                m_local_offset = 0;
                Trace.WriteLine("Garbage", null);
            }
        }

        private enum GetMessageStatus
        {
            Good,
            Timeout,
            SubTimeout,
            ConnectionClose,
            ConnectionError,
            DecodeError,
        }

        private GetMessageStatus GetNextMessage(int timeout_ms, out MSTP_FRAME_TYPE frame_type, out byte destination_address, out byte source_address, out int msg_length)
        {
            int timeout;

            frame_type = MSTP_FRAME_TYPE.FRAME_TYPE_TOKEN;
            destination_address = 0;
            source_address = 0;
            msg_length = 0;

            //fetch header
            while (m_local_offset < MSTP.MSTP_HEADER_LENGTH)
            {
                if (m_port == null) return GetMessageStatus.ConnectionClose;

                if (m_local_offset > 0)
                    timeout = T_FRAME_ABORT;    //set sub timeout
                else
                    timeout = timeout_ms;       //set big silence timeout

                //read 
                int rx = m_port.Read(m_local_buffer, m_local_offset, MSTP.MSTP_HEADER_LENGTH - m_local_offset, timeout);
                if (rx == -ETIMEDOUT)
                {
                    //drop message
                    GetMessageStatus status = m_local_offset == 0 ? GetMessageStatus.Timeout : GetMessageStatus.SubTimeout;
                    m_local_buffer[0] = 0xFF;
                    RemoveGarbage();
                    return status;
                }
                else if (rx < 0)
                {
                    //drop message
                    m_local_buffer[0] = 0xFF;
                    RemoveGarbage();
                    return GetMessageStatus.ConnectionError;
                }
                else if (rx == 0)
                {
                    //drop message
                    m_local_buffer[0] = 0xFF;
                    RemoveGarbage();
                    return GetMessageStatus.ConnectionClose;
                }
                m_local_offset += rx;

                //remove paddings & garbage
                RemoveGarbage();
            }

            //decode
            if (MSTP.Decode(m_local_buffer, 0, m_local_offset, out frame_type, out destination_address, out source_address, out msg_length) < 0)
            {
                //drop message
                m_local_buffer[0] = 0xFF;
                RemoveGarbage();
                return GetMessageStatus.DecodeError;
            }

            //valid length?
            int full_msg_length = msg_length + MSTP.MSTP_HEADER_LENGTH + (msg_length > 0 ? 2 : 0);
            if (msg_length > MSTP.MSTP_MAX_NDPU)
            {
                //drop message
                m_local_buffer[0] = 0xFF;
                RemoveGarbage();
                return GetMessageStatus.DecodeError;
            }

            //fetch data
            if (msg_length > 0)
            {
                timeout = T_FRAME_ABORT;    //set sub timeout
                while (m_local_offset < full_msg_length)
                {
                    //read 
                    int rx = m_port.Read(m_local_buffer, m_local_offset, full_msg_length - m_local_offset, timeout);
                    if (rx == -ETIMEDOUT)
                    {
                        //drop message
                        GetMessageStatus status = m_local_offset == 0 ? GetMessageStatus.Timeout : GetMessageStatus.SubTimeout;
                        m_local_buffer[0] = 0xFF;
                        RemoveGarbage();
                        return status;
                    }
                    else if (rx < 0)
                    {
                        //drop message
                        m_local_buffer[0] = 0xFF;
                        RemoveGarbage();
                        return GetMessageStatus.ConnectionError;
                    }
                    else if (rx == 0)
                    {
                        //drop message
                        m_local_buffer[0] = 0xFF;
                        RemoveGarbage();
                        return GetMessageStatus.ConnectionClose;
                    }
                    m_local_offset += rx;
                }

                //verify data crc
                if (MSTP.Decode(m_local_buffer, 0, m_local_offset, out frame_type, out destination_address, out source_address, out msg_length) < 0)
                {
                    //drop message
                    m_local_buffer[0] = 0xFF;
                    RemoveGarbage();
                    return GetMessageStatus.DecodeError;
                }
            }

            //signal frame event
            if (FrameRecieved != null)
            {
                MSTP_FRAME_TYPE _frame_type = frame_type;
                byte _destination_address = destination_address;
                byte _source_address = source_address;
                int _msg_length = msg_length;
                System.Threading.ThreadPool.QueueUserWorkItem((o) => { FrameRecieved(this, _frame_type, _destination_address, _source_address, _msg_length); }, null);
            }

            if(StateLogging) Trace.WriteLine("" + frame_type + " " + destination_address.ToString("X2") + " ");

            //done
            return GetMessageStatus.Good;
        }

        public int Send(byte[] buffer, int offset, int data_length, BACNET_ADDRESS address, bool wait_for_transmission, int timeout)
        {
            if (m_TS == -1) throw new Exception("Source address must be set up before sending messages");

            //add to queue
            BACNET_NPDU_CONTROL function = NPDU.DecodeFunction(buffer, offset);
            MSTP_FRAME_TYPE frame_type = (function & BACNET_NPDU_CONTROL.ExpectingReply) == BACNET_NPDU_CONTROL.ExpectingReply ? MSTP_FRAME_TYPE.FRAME_TYPE_BACNET_DATA_EXPECTING_REPLY : MSTP_FRAME_TYPE.FRAME_TYPE_BACNET_DATA_NOT_EXPECTING_REPLY;
            byte[] copy = new byte[data_length + MSTP.MSTP_HEADER_LENGTH + 2];
            Array.Copy(buffer, offset, copy, MSTP.MSTP_HEADER_LENGTH, data_length);
            MessageFrame f = new MessageFrame(frame_type, address.adr[0], copy, data_length);
            m_send_queue.AddLast(f);
            if (m_reply == null)
            {
                m_reply = f;
                m_reply_mutex.Set();
            }

            //wait for message to be sent
            if (wait_for_transmission) 
                if (!f.send_mutex.WaitOne(timeout))
                    return -ETIMEDOUT;

            return data_length;
        }

        public BACNET_ADDRESS GetBroadcastAddress()
        {
            return new BACNET_ADDRESS(AddressTypes.MSTP, 0xFFFF, new byte[] { 0xFF });
        }

        public void Dispose()
        {
            if (m_port != null)
            {
                try
                {
                    m_port.Close();
                }
                catch
                {
                }
                m_port = null;
            }
        }
    }
}
