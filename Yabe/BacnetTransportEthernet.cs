/**************************************************************************
*                           MIT License
* 
* Copyright (C) 2015 Frederic Chaxel <fchaxel@free.fr>
*
* lot of code ported from https://github.com/LorenVS/bacstack
*      Copyright (C) 2014 Loren Van Spronsen, thank to him.
* Thank to Christopher Gunther for the idea, and the starting code
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
using System.Linq;
using System.IO.BACnet.Serialize;
using System.Diagnostics;
using SharpPcap;
using SharpPcap.LibPcap;

namespace System.IO.BACnet
{
    // A reference to PacketDotNet.dll & SharpPcap.dll should be made
    // in order to use this code
    // This class is not in the file BacnetTransport.cs to avoid integration 
    // of two dll when Bacnet/Ethernet is not used

    class BacnetEthernetProtocolTransport : IBacnetTransport
    {
        private string deviceName;
        private LibPcapLiveDevice _device;
        private byte[] _deviceMac; // Mac of the device

        public event MessageRecievedHandler MessageRecieved;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="FriendlydeviceName">Something like "Local Lan 1", "Wireless network", ...</param>
        public BacnetEthernetProtocolTransport(string FriendlydeviceName)
        {
            this.deviceName = FriendlydeviceName;
        }

        public BacnetAddressTypes Type { get { return BacnetAddressTypes.Ethernet; } }

        public BacnetAddress GetBroadcastAddress() { return new BacnetAddress(BacnetAddressTypes.Ethernet, "FF-FF-FF-FF-FF-FF"); }
        public int HeaderLength { get { return 6 + 6 + 2 + 3; } }
        public BacnetMaxAdpu MaxAdpuLength { get { return BacnetMaxAdpu.MAX_APDU1476; } }
        public int MaxBufferLength { get { return 1500; } }
        public byte MaxInfoFrames { get { return 0xff; } set { /* ignore */ } }     //the ethernet doesn't have max info frames

        public bool WaitForAllTransmits(int timeout) { return true; } // not used 

        public override string ToString()
        {
            return "Ethernet";
        }

        private LibPcapLiveDevice Open()
        {
            var devices = LibPcapLiveDeviceList.Instance.Where(dev => dev.Interface != null);

            if ((deviceName != null) && (deviceName != "")) // specified interface
            {
                try
                {
                    var device = devices.Where(dev => dev.Interface.FriendlyName == deviceName).FirstOrDefault();
                    device.Open(DeviceMode.Normal, 1000);  // 1000 ms read timeout
                    return device;
                }
                catch
                {
                    return null;
                }

            }
            else // no interface specified, open the first Ethernet link layer (included Wifi).
            {
                foreach (var device in devices)
                {
                    device.Open(DeviceMode.Normal, 1000);  // 1000 ms read timeout
                    if (device.LinkType == PacketDotNet.LinkLayers.Ethernet
                        && device.Interface.MacAddress != null)
                        return device;
                    device.Close();
                }

                return null;
            }
        }

        public void Start()
        {
            _device = Open();
            if (_device == null) throw new Exception("Cannot open Ethernet interface");

            _deviceMac = _device.Interface.MacAddress.GetAddressBytes();

            // filter to only bacnet packets
            _device.Filter = "ether proto 0x82";

            System.Threading.Thread th = new Threading.Thread(CaptureThread);
            th.IsBackground = true;
            th.Start();
        }

        void CaptureThread()
        {
            _device.NonBlockingMode = true;  // Without that it's very, very slow
            for (; ; )
            {
                try
                {
                    RawCapture packet = _device.GetNextPacket();
                    if (packet != null)
                        OnPacketArrival(packet);
                    else
                        System.Threading.Thread.Sleep(10);  // NonBlockingMode, we need to slow the overhead
                }
                catch { return; } // closed interface sure !
            }
        }

        private bool _isOutboundPacket(byte[] buffer, int offset)
        {
            // check to see if the source mac 100%
            // matches the device mac address of the local device

            for (int i = 0; i < 6; i++)
            {
                if (buffer[offset + i] != _deviceMac[i])
                    return false;
            }

            return true;
        }

        byte[] Mac(byte[] buffer, int offset)
        {
            byte[] b = new byte[6];
            Buffer.BlockCopy(buffer, offset, b, 0, 6);
            return b;
        }

        void OnPacketArrival(RawCapture packet)
        {
            // don't process any packet too short to not be valid
            if (packet.Data.Length <= 17)
                return;

            byte[] buffer = packet.Data;
            int offset = 0;

            int length;
            byte dsap, ssap, control;

            // Got frames send by me, not for me, not broadcast
            byte[] dest = Mac(buffer, offset);
            if (!_isOutboundPacket(dest, 0) && (dest[0] != 255))
                return;

            offset += 6;

            // source address
            BacnetAddress Bac_source = new BacnetAddress(BacnetAddressTypes.Ethernet, 0, Mac(buffer, offset));
            offset += 6;

            // len
            length = buffer[offset] * 256 + buffer[offset + 1];
            offset += 2;

            // 3 bytes LLC hearder
            dsap = buffer[offset++];
            ssap = buffer[offset++];
            control = buffer[offset++];

            length -= 3; // Bacnet content length eq. ethernet lenght minus LLC header length

            // don't process non-BACnet packets
            if (dsap != 0x82 || ssap != 0x82 || control != 0x03)
                return;

            if (MessageRecieved != null)
                MessageRecieved(this, buffer, HeaderLength, length, Bac_source);
        }

        public int Send(byte[] buffer, int offset, int data_length, BacnetAddress address, bool wait_for_transmission, int timeout)
        {
            int hdr_offset = 0;

            for (int i = 0; i < 6; i++)
                buffer[hdr_offset++] = address.adr[i];

            // write the source mac address bytes
            for (int i = 0; i < 6; i++)
                buffer[hdr_offset++] = _deviceMac[i];

            // the next 2 bytes are used for the packet length
            buffer[hdr_offset++] = (byte)(((data_length + 3) & 0xFF00) >> 8);
            buffer[hdr_offset++] = (byte)((data_length + 3) & 0xFF);

            // DSAP and SSAP
            buffer[hdr_offset++] = 0x82;
            buffer[hdr_offset++] = 0x82;

            // LLC control field
            buffer[hdr_offset] = 0x03;

            lock (_device)
            {
                _device.SendPacket(buffer, data_length + HeaderLength);
            }

            return data_length + HeaderLength;
        }

        public void Dispose()
        {
            lock (_device)
                _device.Close();
        }
    }
}