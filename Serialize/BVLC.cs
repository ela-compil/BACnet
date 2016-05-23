using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace System.IO.BACnet.Serialize
{
    // Special thanks to VTS tool (BBMD services not activated but programmed !) and Steve Karg stack
    public class BVLC
    {
        private readonly BacnetIpUdpProtocolTransport _myBbmdTransport;
        readonly string _broadcastAdd;
        private bool _bbmdFdServiceActivated;

        public const byte BVLL_TYPE_BACNET_IP = 0x81;
        public const byte BVLC_HEADER_LENGTH = 4;
        public const BacnetMaxAdpu BVLC_MAX_APDU = BacnetMaxAdpu.MAX_APDU1476;

        // Two lists for optional BBMD activity
        readonly List<KeyValuePair<IPEndPoint, DateTime>> _foreignDevices = new List<KeyValuePair<IPEndPoint, DateTime>>();
        private readonly List<KeyValuePair<IPEndPoint, IPAddress>> _bbmds = new List<KeyValuePair<IPEndPoint, IPAddress>>();

        // Contains the rules to accept FRD based on the IP adress
        // If empty it's equal to *.*.*.*, everyone allows
        private readonly List<Regex> _autorizedFdr = new List<Regex>();

        public BVLC(BacnetIpUdpProtocolTransport transport)
        {
            _myBbmdTransport = transport;
            _broadcastAdd = _myBbmdTransport.GetBroadcastAddress().ToString().Split(':')[0];
        }

        public string FDList()
        {
            var sb = new StringBuilder();
            lock (_foreignDevices)
            {
                // remove oldest Device entries (Time expiration > TTL + 30s delay)
                _foreignDevices.Remove(_foreignDevices.Find(item => DateTime.Now > item.Value));

                foreach (var client in _foreignDevices)
                    sb.Append($"{client.Key.Address}:{client.Key.Port};");
            }
            return sb.ToString();
        }

        public void AddFDRAutorisationRule(Regex ipRule)
        {
            _autorizedFdr.Add(ipRule);
        }

        // Used to initiate the BBMD & FD behaviour, if BBMD is null it start the FD activity only
        public void AddBBMDPeer(IPEndPoint bbmd, IPAddress mask)
        {
            _bbmdFdServiceActivated = true;

            if (bbmd == null)
                return;

            lock (_bbmds)
                _bbmds.Add(new KeyValuePair<IPEndPoint, IPAddress>(bbmd, mask));
        }

        // Add a FD to the table or renew it
        private void RegisterForeignDevice(IPEndPoint sender, int ttl)
        {
            lock (_foreignDevices)
            {
                // remove it, if any
                _foreignDevices.Remove(_foreignDevices.Find(item => item.Key.Equals(sender)));
                // TTL + 30s grace period
                var expiration = DateTime.Now.AddSeconds(ttl + 30);
                // add it
                if (_autorizedFdr.Count == 0) // No rules, accept all
                {
                    _foreignDevices.Add(new KeyValuePair<IPEndPoint, DateTime>(sender, expiration));
                    return;
                }
                foreach (var r in _autorizedFdr)
                {
                    if (r.Match(sender.Address.ToString()).Success)
                    {
                        _foreignDevices.Add(new KeyValuePair<IPEndPoint, DateTime>(sender, expiration));
                        return;
                    }
                }
                Trace.TraceInformation("Rejected FDR registration, IP : " + sender.Address);
            }
        }

        // Send a Frame to each registered foreign devices, except the original sender
        private void SendToFDs(byte[] buffer, int msgLength, Net.IPEndPoint ePsender = null)
        {
            lock (_foreignDevices)
            {
                // remove oldest Device entries (Time expiration > TTL + 30s delay)
                _foreignDevices.Remove(_foreignDevices.Find(item => DateTime.Now > item.Value));
                // Send to all others, except the original sender
                foreach (var client in _foreignDevices)
                {
                    if (!client.Key.Equals(ePsender))
                        _myBbmdTransport.Send(buffer, msgLength, client.Key);
                }
            }
        }

        private static IPEndPoint BBMDSentAdd(IPEndPoint bbmd, IPAddress Mask)
        {
            var bm = Mask.GetAddressBytes();
            var bip = bbmd.Address.GetAddressBytes();

            /* annotation in Steve Karg bacnet stack :
         
            The B/IP address to which the Forwarded-NPDU message is
            sent is formed by inverting the broadcast distribution
            mask in the BDT entry and logically ORing it with the
            BBMD address of the same entry. This process
            produces either the directed broadcast address of the remote
            subnet or the unicast address of the BBMD on that subnet
            depending on the contents of the broadcast distribution
            mask. 
             
            remark from me :
               for instance remote BBMD 192.168.0.1 - mask 255.255.255.255
                    messages are forward directly to 192.168.0.1
               remote BBMD 192.168.0.1 - mask 255.255.255.0
                    messages are forward to 192.168.0.255, ie certainly the local broadcast
                    address, but these datagrams are generaly destroy by the final IP router
             */

            for (var i = 0; i < bm.Length; i++)
                bip[i] = (byte)(bip[i] | (~bm[i]));

            return new IPEndPoint(new IPAddress(bip), bbmd.Port);
        }

        // Send a Frame to each registered BBMD except the original sender
        private void SendToBbmDs(byte[] buffer, int msgLength)
        {
            lock (_bbmds)
            {
                foreach (var e in _bbmds)
                {
                    var endpoint = BBMDSentAdd(e.Key, e.Value);
                    _myBbmdTransport.Send(buffer, msgLength, endpoint);
                }
            }
        }

        private static void First4BytesHeaderEncode(IList<byte> b, BacnetBvlcFunctions function, int msgLength)
        {
            b[0] = BVLL_TYPE_BACNET_IP;
            b[1] = (byte)function;
            b[2] = (byte)(((msgLength) & 0xFF00) >> 8);
            b[3] = (byte)(((msgLength) & 0x00FF) >> 0);
        }

        private void Forward_NPDU(byte[] buffer, int msgLength, bool toGlobalBroadcast, IPEndPoint ePsender)
        {
            // Forms the forwarded NPDU from the original one, and send it to all
            // orignal     - 4 bytes BVLC -  NPDU  - APDU
            // change to   -  10 bytes BVLC  -  NPDU  - APDU

            // copy, 6 bytes shifted
            var b = new byte[msgLength + 6];    // normaly only 'small' frames are present here, so no need to check if it's to big for Udp
            Array.Copy(buffer, 0, b, 6, msgLength);

            // 10 bytes for the BVLC Header, with the embedded 6 bytes IP:Port of the original sender
            First4BytesHeaderEncode(b, BacnetBvlcFunctions.BVLC_FORWARDED_NPDU, msgLength + 6);
            BacnetAddress bacSender;
            BacnetIpUdpProtocolTransport.Convert(ePsender, out bacSender); // to embbed in the forward BVLC header
            for (var i = 0; i < bacSender.adr.Length; i++)
                b[4 + i] = bacSender.adr[i];

            // To BBMD
            SendToBbmDs(b, msgLength + 6);
            // To FD, except the sender
            SendToFDs(b, msgLength + 6, ePsender);
            // Broadcast if required
            if (toGlobalBroadcast)
                _myBbmdTransport.Send(b, msgLength + 6, new IPEndPoint(IPAddress.Parse(_broadcastAdd), _myBbmdTransport.SharedPort));
        }

        // Send ack or nack
        private void SendResult(IPEndPoint sender, BacnetBvlcResults resultCode)
        {
            var b = new byte[6];
            First4BytesHeaderEncode(b, BacnetBvlcFunctions.BVLC_RESULT, 6);
            b[4] = (byte)(((ushort)resultCode & 0xFF00) >> 8);
            b[5] = (byte)((ushort)resultCode & 0xFF);

            _myBbmdTransport.Send(b, 6, sender);
        }

        public void SendRegisterAsForeignDevice(IPEndPoint bbmd, short ttl)
        {
            var b = new byte[6];
            First4BytesHeaderEncode(b, BacnetBvlcFunctions.BVLC_REGISTER_FOREIGN_DEVICE, 6);
            b[4] = (byte)((ttl & 0xFF00) >> 8);
            b[5] = (byte)(ttl & 0xFF);
            _myBbmdTransport.Send(b, 6, bbmd);
        }

        public void SendRemoteWhois(byte[] buffer, IPEndPoint bbmd, int msgLength)
        {
            Encode(buffer, 0, BacnetBvlcFunctions.BVLC_DISTRIBUTE_BROADCAST_TO_NETWORK, msgLength);
            _myBbmdTransport.Send(buffer, msgLength, bbmd);

        }
        // Encode is called by internal services if the BBMD is also an active device
        public int Encode(byte[] buffer, int offset, BacnetBvlcFunctions function, int msgLength)
        {
            // offset always 0, we are the first after udp

            // do the job
            First4BytesHeaderEncode(buffer, function, msgLength);

            // optional BBMD service
            if (_bbmdFdServiceActivated && (function == BacnetBvlcFunctions.BVLC_ORIGINAL_BROADCAST_NPDU))
            {
                var me = _myBbmdTransport.LocalEndPoint;
                // just sometime working, enable to get the local ep, always 0.0.0.0 if the socket is open with
                // System.Net.IPAddress.Any
                // So in this case don't send a bad message
                if ((me.Address.ToString() != "0.0.0.0"))
                    Forward_NPDU(buffer, msgLength, false, me);   // send to all BBMDs and FDs
            }
            return 4; // ready to send
        }

        // Decode is called each time an Udp Frame is received
        public int Decode(byte[] buffer, int offset, out BacnetBvlcFunctions function, out int msgLength, IPEndPoint sender)
        {

            // offset always 0, we are the first after udp
            // and a previous test by the caller guaranteed at least 4 bytes into the buffer

            function = (BacnetBvlcFunctions)buffer[1];
            msgLength = (buffer[2] << 8) | (buffer[3] << 0);
            if ((buffer[0] != BVLL_TYPE_BACNET_IP) || (buffer.Length != msgLength)) return -1;

            switch (function)
            {
                case BacnetBvlcFunctions.BVLC_RESULT:
                    return 4;   // only for the upper layers

                case BacnetBvlcFunctions.BVLC_ORIGINAL_UNICAST_NPDU:
                    return 4;   // only for the upper layers

                case BacnetBvlcFunctions.BVLC_ORIGINAL_BROADCAST_NPDU: // Normaly received in an IP local or global broadcast packet
                    // Send to FDs & BBMDs, not broadcast or it will be made twice !
                    if (_bbmdFdServiceActivated)
                        Forward_NPDU(buffer, msgLength, false, sender);
                    return 4;   // also for the upper layers

                case BacnetBvlcFunctions.BVLC_FORWARDED_NPDU:   // Sent only by a BBMD, broadcast on it network, or broadcast demand by one of it's FDs
                    if (_bbmdFdServiceActivated && (msgLength >= 10))
                    {
                        bool ret;
                        lock (_bbmds)
                            ret = _bbmds.Exists(items => items.Key.Address.Equals(sender.Address));    // verify sender (@ not Port!) presence in the table

                        if (ret)    // message from a know BBMD address, sent to all FDs and broadcast
                        {
                            SendToFDs(buffer, msgLength);  // send without modification

                            // Assume all BVLC_FORWARDED_NPDU are directly sent to me in the 
                            // unicast mode and not by the way of the local broadcast address
                            // ie my mask must be 255.255.255.255 in the others BBMD tables
                            // If not, it's not really a big problem, devices on the local net will 
                            // receive two times the message (after all it's just WhoIs, Iam, ...)
                            _myBbmdTransport.Send(buffer, msgLength, new IPEndPoint(IPAddress.Parse(_broadcastAdd), _myBbmdTransport.SharedPort));
                        }
                    }

                    return 10;  // also for the upper layers

                case BacnetBvlcFunctions.BVLC_DISTRIBUTE_BROADCAST_TO_NETWORK:  // Sent by a Foreign Device, not a BBMD
                    if (_bbmdFdServiceActivated)
                    {
                        // Send to FDs except the sender, BBMDs and broadcast
                        lock (_foreignDevices)
                        {
                            if (_foreignDevices.Exists(item => item.Key.Equals(sender))) // verify previous registration
                                Forward_NPDU(buffer, msgLength, true, sender);
                            else
                                SendResult(sender, BacnetBvlcResults.BVLC_RESULT_DISTRIBUTE_BROADCAST_TO_NETWORK_NAK);
                        }
                    }
                    return 4;   // also for the upper layers

                case BacnetBvlcFunctions.BVLC_REGISTER_FOREIGN_DEVICE:
                    if (_bbmdFdServiceActivated && (msgLength == 6))
                    {
                        var ttl = (buffer[4] << 8) + buffer[5]; // unit is second
                        RegisterForeignDevice(sender, ttl);
                        SendResult(sender, BacnetBvlcResults.BVLC_RESULT_SUCCESSFUL_COMPLETION);  // ack
                    }
                    return -1;  // not for the upper layers

                // We don't care about Read/Write operation in the BBMD/FDR tables (who realy use it ?)
                case BacnetBvlcFunctions.BVLC_READ_FOREIGN_DEVICE_TABLE:
                    //SendResult(sender, BacnetBvlcResults.BVLC_RESULT_READ_FOREIGN_DEVICE_TABLE_NAK);
                    return -1;
                case BacnetBvlcFunctions.BVLC_DELETE_FOREIGN_DEVICE_TABLE_ENTRY:
                    //SendResult(sender, BacnetBvlcResults.BVLC_RESULT_DELETE_FOREIGN_DEVICE_TABLE_ENTRY_NAK);
                    return -1;
                case BacnetBvlcFunctions.BVLC_READ_BROADCAST_DIST_TABLE:
                    //SendResult(sender, BacnetBvlcResults.BVLC_RESULT_READ_BROADCAST_DISTRIBUTION_TABLE_NAK);
                    return -1;
                case BacnetBvlcFunctions.BVLC_WRITE_BROADCAST_DISTRIBUTION_TABLE:
                    //SendResult(sender, BacnetBvlcResults.BVLC_RESULT_WRITE_BROADCAST_DISTRIBUTION_TABLE_NAK);
                    return -1;
                // error encoding function or experimental one
                default:
                    return -1;
            }
        }
    }
}