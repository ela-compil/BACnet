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
    /// <summary>
    /// This can be both client and server
    /// </summary>
    public class BacnetClient : IDisposable
    {
        private IBacnetTransport m_client;
        private byte[] m_tx_buffer;
        private ushort m_vendor_id = 260;
        private System.Threading.ManualResetEvent m_response_event = new Threading.ManualResetEvent(false);
        private byte[] m_apdu_response;
        private int m_apdu_length;
        private object m_response_lock = new object();
        private int m_timeout;
        private int m_retries;
        private byte m_invoke_id = 0;

        public static byte DEFAULT_HOP_COUNT = 0xFF;

        public IBacnetTransport Transport { get { return m_client; } }
        public int Timeout { get { return m_timeout; } set { m_timeout = value; } }
        public int Retries { get { return m_retries; } set { m_retries = value; } }

        public BacnetClient(int port = 0xBAC0, int timeout = 1000, int retries = 3) : 
            this(new BacnetIpUdpProtocolTransport(port), timeout, retries)
        {
        }

        public BacnetClient(string port_name, int baud_rate, int timeout = 1000, int retries = 3) :
            this(new BacnetMstpProtocolTransport(port_name, baud_rate), timeout, retries)
        {
        }

        public BacnetClient(IBacnetTransport transport, int timeout = 1000, int retries = 3)
        {
            m_client = transport;
            m_tx_buffer = new byte[m_client.MaxBufferLength];
            m_apdu_response = new byte[m_client.MaxBufferLength];
            m_timeout = timeout;
            m_retries = retries;
        }

        public override bool Equals(object obj)
        {
            if(!(obj is BacnetClient)) return false;
            BacnetClient a = (BacnetClient)obj;
            return m_client.Equals(a.m_client);
        }

        public override int GetHashCode()
        {
            return m_client.GetHashCode();
        }

        public override string ToString()
        {
            return m_client.ToString();
        }

        public void Start()
        {
            m_client.Start();
            m_client.MessageRecieved += new MessageRecievedHandler(OnRecieve);
            Trace.TraceInformation("Started communication");
        }

        public delegate void ConfirmedServiceRequestHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, BacnetMaxSegments max_segments, BacnetMaxAdpu max_adpu, byte invoke_id, byte sequence_number, byte proposed_window_number, byte[] buffer, int offset, int length);
        public event ConfirmedServiceRequestHandler OnConfirmedServiceRequest;
        public delegate void ReadPropertyRequestHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, BacnetObjectId object_id, BacnetPropertyReference property);
        public event ReadPropertyRequestHandler OnReadPropertyRequest;
        public delegate void ReadPropertyMultipleRequestHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, IList<BacnetObjectId> object_ids, IList<IList<BacnetPropertyReference>> property_id_and_array_index);
        public event ReadPropertyMultipleRequestHandler OnReadPropertyMultipleRequest;
        public delegate void WritePropertyRequestHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, BacnetObjectId object_id, BacnetPropertyValue value);
        public event WritePropertyRequestHandler OnWritePropertyRequest;
        public delegate void WritePropertyMultipleRequestHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, BacnetObjectId object_id, ICollection<BacnetPropertyValue> values);
        public event WritePropertyMultipleRequestHandler OnWritePropertyMultipleRequest;
        public delegate void AtomicWriteFileRequestHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, bool is_stream, BacnetObjectId object_id, int position, uint block_count, byte[][] blocks, int[] counts);
        public event AtomicWriteFileRequestHandler OnAtomicWriteFileRequest;
        public delegate void AtomicReadFileRequestHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, bool is_stream, BacnetObjectId object_id, int position, uint count);
        public event AtomicReadFileRequestHandler OnAtomicReadFileRequest;
        public delegate void SubscribeCOVRequestHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime);
        public event SubscribeCOVRequestHandler OnSubscribeCOV;
        public delegate void SubscribeCOVPropertyRequestHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier, BacnetPropertyReference monitoredProperty, bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime, float covIncrement);
        public event SubscribeCOVPropertyRequestHandler OnSubscribeCOVProperty;

        protected void ProcessConfirmedServiceRequest(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, BacnetMaxSegments max_segments, BacnetMaxAdpu max_adpu, byte invoke_id, byte sequence_number, byte proposed_window_number, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("ConfirmedServiceRequest", null);
                if (OnConfirmedServiceRequest != null) OnConfirmedServiceRequest(this, adr, type, service, max_segments, max_adpu, invoke_id, sequence_number, proposed_window_number, buffer, offset, length);
                if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY && OnReadPropertyRequest != null)
                {
                    BacnetObjectId object_id;
                    BacnetPropertyReference property;
                    if (Services.DecodeReadProperty(buffer, offset, length, out object_id, out property) >= 0)
                        OnReadPropertyRequest(this, adr, invoke_id, object_id, property);
                    else
                        Trace.TraceWarning("Couldn't decode DecodeReadProperty");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY && OnWritePropertyRequest != null)
                {
                    BacnetObjectId object_id;
                    BacnetPropertyValue value;
                    if (Services.DecodeWriteProperty(buffer, offset, length, out object_id, out value) >= 0)
                        OnWritePropertyRequest(this, adr, invoke_id, object_id, value);
                    else
                        Trace.TraceWarning("Couldn't decode DecodeWriteProperty");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE && OnReadPropertyMultipleRequest != null)
                {
                    IList<BacnetObjectId> object_ids;
                    IList<IList<BacnetPropertyReference>> property_id_and_array_index;
                    if (Services.DecodeReadPropertyMultiple(buffer, offset, length, out object_ids, out property_id_and_array_index) >= 0)
                        OnReadPropertyMultipleRequest(this, adr, invoke_id, object_ids, property_id_and_array_index);
                    else
                        Trace.TraceWarning("Couldn't decode DecodeReadPropertyMultiple");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE && OnWritePropertyMultipleRequest != null)
                {
                    BacnetObjectId object_id;
                    ICollection<BacnetPropertyValue> values;
                    if (Services.DecodeWritePropertyMultiple(buffer, offset, length, out object_id, out values) >= 0)
                        OnWritePropertyMultipleRequest(this, adr, invoke_id, object_id, values);
                    else
                        Trace.TraceWarning("Couldn't decode DecodeWritePropertyMultiple");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION && OnCOVNotification != null)
                {
                    uint subscriberProcessIdentifier;
                    BacnetObjectId initiatingDeviceIdentifier;
                    BacnetObjectId monitoredObjectIdentifier;
                    uint timeRemaining;
                    ICollection<BacnetPropertyValue> values;
                    if (Services.DecodeCOVNotifyUnconfirmed(buffer, offset, length, out subscriberProcessIdentifier, out initiatingDeviceIdentifier, out monitoredObjectIdentifier, out timeRemaining, out values) >= 0)
                        OnCOVNotification(this, adr, invoke_id, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, true, values);
                    else
                        Trace.TraceWarning("Couldn't decode COVNotify");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE && OnAtomicWriteFileRequest != null)
                {
                    bool is_stream;
                    BacnetObjectId object_id;
                    int position;
                    uint block_count;
                    byte[][] blocks;
                    int[] counts;
                    if (Services.DecodeAtomicWriteFile(buffer, offset, length, out is_stream, out object_id, out position, out block_count, out blocks, out counts) >= 0)
                        OnAtomicWriteFileRequest(this, adr, invoke_id, is_stream, object_id, position, block_count, blocks, counts);
                    else
                        Trace.TraceWarning("Couldn't decode AtomicWriteFile");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE && OnAtomicReadFileRequest != null)
                {
                    bool is_stream;
                    BacnetObjectId object_id;
                    int position;
                    uint count;
                    if (Services.DecodeAtomicReadFile(buffer, offset, length, out is_stream, out object_id, out position, out count) >= 0)
                        OnAtomicReadFileRequest(this, adr, invoke_id, is_stream, object_id, position, count);
                    else
                        Trace.TraceWarning("Couldn't decode AtomicReadFile");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV && OnSubscribeCOV != null)
                {
                    uint subscriberProcessIdentifier;
                    BacnetObjectId monitoredObjectIdentifier;
                    bool cancellationRequest;
                    bool issueConfirmedNotifications;
                    uint lifetime;
                    if (Services.DecodeSubscribeCOV(buffer, offset, length, out subscriberProcessIdentifier, out monitoredObjectIdentifier, out cancellationRequest, out issueConfirmedNotifications, out lifetime) >= 0)
                        OnSubscribeCOV(this, adr, invoke_id, subscriberProcessIdentifier, monitoredObjectIdentifier, cancellationRequest, issueConfirmedNotifications, lifetime);
                    else
                        Trace.TraceWarning("Couldn't decode SubscribeCOV");
                }
                else if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY && OnSubscribeCOVProperty != null)
                {
                    uint subscriberProcessIdentifier;
                    BacnetObjectId monitoredObjectIdentifier;
                    BacnetPropertyReference monitoredProperty;
                    bool cancellationRequest;
                    bool issueConfirmedNotifications;
                    uint lifetime;
                    float covIncrement;
                    if (Services.DecodeSubscribeProperty(buffer, offset, length, out subscriberProcessIdentifier, out monitoredObjectIdentifier, out monitoredProperty, out cancellationRequest, out issueConfirmedNotifications, out lifetime, out covIncrement) >= 0)
                        OnSubscribeCOVProperty(this, adr, invoke_id, subscriberProcessIdentifier, monitoredObjectIdentifier, monitoredProperty, cancellationRequest, issueConfirmedNotifications, lifetime, covIncrement);
                    else
                        Trace.TraceWarning("Couldn't decode SubscribeCOVProperty");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in ProcessConfirmedServiceRequest: " + ex.Message);
            }
        }

        public delegate void UnconfirmedServiceRequestHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetUnconfirmedServices service, byte[] buffer, int offset, int length);
        public event UnconfirmedServiceRequestHandler OnUnconfirmedServiceRequest;
        public delegate void IamHandler(BacnetClient sender, BacnetAddress adr, UInt32 device_id, UInt32 max_apdu, BacnetSegmentations segmentation, UInt16 vendor_id);
        public event IamHandler OnIam;
        public delegate void WhoIsHandler(BacnetClient sender, BacnetAddress adr, int low_limit, int high_limit);
        public event WhoIsHandler OnWhoIs;

        //used by both 'confirmed' and 'unconfirmed' notify
        public delegate void COVNotificationHandler(BacnetClient sender, BacnetAddress adr, byte invoke_id, uint subscriberProcessIdentifier, BacnetObjectId initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, bool need_confirm, ICollection<BacnetPropertyValue> values);
        public event COVNotificationHandler OnCOVNotification;

        protected void ProcessUnconfirmedServiceRequest(BacnetAddress adr, BacnetPduTypes type, BacnetUnconfirmedServices service, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("UnconfirmedServiceRequest", null);
                if (OnUnconfirmedServiceRequest != null) OnUnconfirmedServiceRequest(this, adr, type, service, buffer, offset, length);
                if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_AM && OnIam != null)
                {
                    uint device_id;
                    uint max_adpu;
                    BacnetSegmentations segmentation;
                    ushort vendor_id;
                    if (Services.DecodeIamBroadcast(buffer, offset, out device_id, out max_adpu, out segmentation, out vendor_id) >= 0)
                        OnIam(this, adr, device_id, max_adpu, segmentation, vendor_id);
                    else
                        Trace.TraceWarning("Couldn't decode IamBroadcast");
                }
                else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS && OnWhoIs != null)
                {
                    int low_limit;
                    int high_limit;
                    if (Services.DecodeWhoIsBroadcast(buffer, offset, length, out low_limit, out high_limit) >= 0)
                        OnWhoIs(this, adr, low_limit, high_limit);
                    else
                        Trace.TraceWarning("Couldn't decode WhoIsBroadcast");
                }
                else if (service == BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_COV_NOTIFICATION && OnCOVNotification != null)
                {
                    uint subscriberProcessIdentifier;
                    BacnetObjectId initiatingDeviceIdentifier;
                    BacnetObjectId monitoredObjectIdentifier;
                    uint timeRemaining;
                    ICollection<BacnetPropertyValue> values;
                    if (Services.DecodeCOVNotifyUnconfirmed(buffer, offset, length, out subscriberProcessIdentifier, out initiatingDeviceIdentifier, out monitoredObjectIdentifier, out timeRemaining, out values) >= 0)
                        OnCOVNotification(this, adr, 0, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, false, values);
                    else
                        Trace.TraceWarning("Couldn't decode COVNotifyUnconfirmed");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in ProcessUnconfirmedServiceRequest: " + ex.Message);
            }
        }

        public delegate void SimpleAckHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, byte[] data, int data_offset, int data_length);
        public event SimpleAckHandler OnSimpleAck;

        protected void ProcessSimpleAck(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("SimpleAck", null);
                if (OnSimpleAck != null) OnSimpleAck(this, adr, type, service, invoke_id, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in ProcessSimpleAck: " + ex.Message);
            }
        }

        public delegate void ComplexAckHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, byte sequence_number, byte proposed_window_number, byte[] buffer, int offset, int length);
        public event ComplexAckHandler OnComplexAck;

        protected void ProcessComplexAck(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, byte sequence_number, byte proposed_window_number, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("ComplexAck", null);
                if (OnComplexAck != null) OnComplexAck(this, adr, type, service, invoke_id, sequence_number, proposed_window_number, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in ProcessComplexAck: " + ex.Message);
            }
        }

        public delegate void ErrorHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, uint error_class, uint error_code, byte[] buffer, int offset, int length);
        public event ErrorHandler OnError;

        protected void ProcessError(BacnetAddress adr, BacnetPduTypes type, BacnetConfirmedServices service, byte invoke_id, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("Error", null);

                uint error_class;
                uint error_code;
                if (Services.DecodeError(buffer, offset, length, out error_class, out error_code) < 0)
                    Trace.TraceWarning("Couldn't decode Error");

                if (OnError != null) OnError(this, adr, type, service, invoke_id, error_class, error_code, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in ProcessError: " + ex.Message);
            }
        }

        public delegate void AbortHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, byte invoke_id, byte reason, byte[] buffer, int offset, int length);
        public event AbortHandler OnAbort;

        protected void ProcessAbort(BacnetAddress adr, BacnetPduTypes type, byte invoke_id, byte reason, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("Abort", null);
                if (OnAbort != null) OnAbort(this, adr, type, invoke_id, reason, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in ProcessAbort: " + ex.Message);
            }
        }

        public delegate void SegmentAckHandler(BacnetClient sender, BacnetAddress adr, BacnetPduTypes type, byte[] buffer, int offset, int length);
        public event SegmentAckHandler OnSegmentAck;

        protected void ProcessSegmentAck(BacnetAddress adr, BacnetPduTypes type, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("SegmentAck", null);
                if (OnSegmentAck != null) OnSegmentAck(this, adr, type, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in ProcessSegmentAck: " + ex.Message);
            }
        }

        private void OnRecieve(IBacnetTransport sender, byte[] buffer, int offset, int msg_length, BacnetAddress remote_address)
        {
            try
            {
                //finish recieve
                if (m_client == null) return;   //we're disposed 

                //parse
                if (msg_length > 0)
                {
                    BacnetNpduControls npdu_function;
                    BacnetAddress destination, source;
                    byte hop_count;
                    BacnetNetworkMessageTypes nmt;
                    ushort vendor_id;
                    int npdu_len = NPDU.Decode(buffer, offset, out npdu_function, out destination, out source, out hop_count, out nmt, out vendor_id);
                    if (npdu_len >= 0)
                    {
                        offset += npdu_len;
                        msg_length -= npdu_len;
                        BacnetPduTypes apdu_type;
                        apdu_type = APDU.GetDecodedType(buffer, offset);

                        //store response
                        lock (m_response_lock)
                        {
                            m_apdu_length = msg_length;
                            Array.Copy(buffer, offset, m_apdu_response, 0, m_apdu_length);
                        }

                        switch (apdu_type & BacnetPduTypes.PDU_TYPE_MASK)
                        {
                            case BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST:
                                {
                                    BacnetUnconfirmedServices service;
                                    int apdu_header_len = APDU.DecodeUnconfirmedServiceRequest(buffer, offset, out apdu_type, out service);
                                    offset += apdu_header_len;
                                    msg_length -= apdu_header_len;
                                    ProcessUnconfirmedServiceRequest(remote_address, apdu_type, service, buffer, offset, msg_length);
                                }
                                break;
                            case BacnetPduTypes.PDU_TYPE_SIMPLE_ACK:
                                {
                                    BacnetConfirmedServices service;
                                    byte invoke_id;
                                    int apdu_header_len = APDU.DecodeSimpleAck(buffer, offset, out apdu_type, out service, out invoke_id);
                                    offset += apdu_header_len;
                                    msg_length -= apdu_header_len;
                                    ProcessSimpleAck(remote_address, apdu_type, service, invoke_id, buffer, offset, msg_length);
                                    m_response_event.Set();
                                }
                                break;
                            case BacnetPduTypes.PDU_TYPE_COMPLEX_ACK:
                                {
                                    BacnetConfirmedServices service;
                                    byte invoke_id;
                                    byte sequence_number;
                                    byte proposed_window_number;
                                    int apdu_header_len = APDU.DecodeComplexAck(buffer, offset, out apdu_type, out service, out invoke_id, out sequence_number, out proposed_window_number);
                                    offset += apdu_header_len;
                                    msg_length -= apdu_header_len;
                                    ProcessComplexAck(remote_address, apdu_type, service, (byte)invoke_id, sequence_number, proposed_window_number, buffer, offset, msg_length);
                                    m_response_event.Set();
                                }
                                break;
                            case BacnetPduTypes.PDU_TYPE_SEGMENT_ACK:
                                {
                                    int apdu_header_len = APDU.DecodeSegmentAck(buffer, offset, out apdu_type);
                                    offset += apdu_header_len;
                                    msg_length -= apdu_header_len;
                                    ProcessSegmentAck(remote_address, apdu_type, buffer, offset, msg_length);
                                }
                                break;
                            case BacnetPduTypes.PDU_TYPE_ERROR:
                                {
                                    BacnetConfirmedServices service;
                                    byte invoke_id;
                                    int apdu_header_len = APDU.DecodeError(buffer, offset, out apdu_type, out service, out invoke_id);
                                    offset += apdu_header_len;
                                    msg_length -= apdu_header_len;
                                    ProcessError(remote_address, apdu_type, service, invoke_id, buffer, offset, msg_length);
                                    m_response_event.Set();
                                }
                                break;
                            case BacnetPduTypes.PDU_TYPE_REJECT:
                            case BacnetPduTypes.PDU_TYPE_ABORT:
                                {
                                    byte invoke_id;
                                    byte reason;
                                    int apdu_header_len = APDU.DecodeAbort(buffer, offset, out apdu_type, out invoke_id, out reason);
                                    offset += apdu_header_len;
                                    msg_length -= apdu_header_len;
                                    ProcessAbort(remote_address, apdu_type, invoke_id, reason, buffer, offset, msg_length);
                                    m_response_event.Set();
                                }
                                break;
                            case BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST:
                                {
                                    BacnetConfirmedServices service;
                                    BacnetMaxSegments max_segments;
                                    BacnetMaxAdpu max_adpu;
                                    byte invoke_id;
                                    byte sequence_number;
                                    byte proposed_window_number;
                                    int apdu_header_len = APDU.DecodeConfirmedServiceRequest(buffer, offset, out apdu_type, out service, out max_segments, out max_adpu, out invoke_id, out sequence_number, out proposed_window_number);
                                    offset += apdu_header_len;
                                    msg_length -= apdu_header_len;
                                    ProcessConfirmedServiceRequest(remote_address, apdu_type, service, max_segments, max_adpu, invoke_id, sequence_number, proposed_window_number, buffer, offset, msg_length);
                                }
                                break;
                            default:
                                Trace.TraceWarning("Something else arrived: " + apdu_type);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in OnRecieve: " + ex.Message);
            }
        }

        public void WhoIs()
        {
            Trace.WriteLine("Sending WhoIs ... ", null);

            int offset = 0;
            offset += m_client.HeaderLength;
            int org_offset = offset;
            BacnetAddress broadcast = m_client.GetBroadcastAddress();
            offset += NPDU.Encode(m_tx_buffer, offset, BacnetNpduControls.PriorityNormalMessage, broadcast, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeUnconfirmedServiceRequest(m_tx_buffer, offset, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_WHO_IS);
            offset += Services.EncodeWhoIsBroadcast(m_tx_buffer, offset, -1, -1);

            m_client.Send(m_tx_buffer, m_client.HeaderLength, offset - org_offset, broadcast, false, 0);
        }

        public void Iam(uint device_id)
        {
            Trace.WriteLine("Sending Iam ... ", null);

            int offset = 0;
            offset += m_client.HeaderLength;
            int org_offset = offset;
            BacnetAddress broadcast = m_client.GetBroadcastAddress();
            offset += NPDU.Encode(m_tx_buffer, offset, BacnetNpduControls.PriorityNormalMessage, broadcast, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeUnconfirmedServiceRequest(m_tx_buffer, offset, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_I_AM);
            offset += Services.EncodeIamBroadcast(m_tx_buffer, offset, device_id, (uint)m_client.MaxAdpuLength, BacnetSegmentations.SEGMENTATION_NONE, m_vendor_id);

            m_client.Send(m_tx_buffer, m_client.HeaderLength, offset - org_offset, broadcast, false, 0);
        }

        private bool SendRequestAndTakeLock(BacnetAddress adr, byte expected_invoke_id, BacnetPduTypes expected_type, byte[] data, int data_length)
        {
            bool success = false;
            for (int r = 0; r < m_retries; r++)
            {
                m_response_event.Reset();
                if (m_client.Send(data, m_client.HeaderLength, data_length, adr, true, m_timeout) < 0)
                    return false;
                while (true)
                {
                    if (m_response_event.WaitOne(m_timeout))
                    {
                        System.Threading.Monitor.Enter(m_response_lock);
                        try
                        {
                            int response_invoke_id = APDU.GetDecodedInvokeId(m_apdu_response, 0);
                            if (response_invoke_id == expected_invoke_id)
                            {
                                BacnetPduTypes type = APDU.GetDecodedType(m_apdu_response, 0);
                                if ((type & BacnetPduTypes.PDU_TYPE_MASK) == expected_type)
                                {
                                    success = true;    //keep lock
                                    return success;
                                }
                                else
                                    throw new System.IO.IOException("BACnet error: " + (type & BacnetPduTypes.PDU_TYPE_MASK));
                            }
                            m_response_event.Reset();
                        }
                        finally
                        {
                            //only keep lock if success!
                            if (!success) 
                                System.Threading.Monitor.Exit(m_response_lock);
                        }
                    }
                    else
                        break;
                }
            }
            return success;
        }

        public int GetMaxApdu()
        {
            switch (m_client.MaxAdpuLength)
            {
                case BacnetMaxAdpu.MAX_APDU1476:
                    return 1476;
                case BacnetMaxAdpu.MAX_APDU1024:
                    return 1024;
                case BacnetMaxAdpu.MAX_APDU480:
                    return 480;
                case BacnetMaxAdpu.MAX_APDU206:
                    return 206;
                case BacnetMaxAdpu.MAX_APDU128:
                    return 128;
                case BacnetMaxAdpu.MAX_APDU50:
                    return 50;
                default:
                    throw new NotImplementedException();
            }
        }

        public int GetFileBufferMaxSize()
        {
            return GetMaxApdu() - 7;
        }

        public bool WriteFileRequest(BacnetAddress adr, BacnetObjectId object_id, ref int position, int count, byte[] file_buffer, byte invoke_id = 0)
        {
            int tmp;
            Trace.WriteLine("Sending AtomicWriteFileRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);

            int offset = 0;
            offset += m_client.HeaderLength;
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BacnetNpduControls.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeConfirmedServiceRequest(m_tx_buffer, offset, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, BacnetMaxSegments.MAX_SEG0, m_client.MaxAdpuLength, invoke_id, 0, 0);
            tmp = Services.EncodeAtomicWriteFile(m_tx_buffer, offset, true, object_id, position, 1, new byte[][] { file_buffer }, new int[] { count });
            if (tmp < 0) throw new Exception("Couldn't encode AtomicWriteFile");
            offset += tmp;

            if (SendRequestAndTakeLock(adr, invoke_id, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK, m_tx_buffer, offset - org_offset))
            {
                try
                {
                    BacnetPduTypes type;
                    BacnetConfirmedServices service;
                    byte sequence_number;
                    byte proposed_window_number;
                    int len = APDU.DecodeComplexAck(m_apdu_response, 0, out type, out service, out invoke_id, out sequence_number, out proposed_window_number);
                    if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE)
                    {
                        bool is_stream;
                        if (Services.DecodeAtomicWriteFileAcknowledge(m_apdu_response, len, m_apdu_length - len, out is_stream, out position) < 0)
                            throw new System.IO.IOException("Couldn't decode AtomicWriteFileAcknowledge");
                        return true;
                    }
                    else
                        throw new System.IO.IOException("BACnet error: " + service);
                }
                finally
                {
                    System.Threading.Monitor.Exit(m_response_lock);
                }
            }
            else
            {
                return false;
            }
        }

        public bool ReadFileRequest(BacnetAddress adr, BacnetObjectId object_id, ref int position, ref uint count, out bool end_of_file, byte[] file_buffer, int file_buffer_offset, byte invoke_id = 0)
        {
            int tmp;
            Trace.WriteLine("Sending AtomicReadFileRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);
            end_of_file = true;

            int offset = 0;
            offset += m_client.HeaderLength;
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BacnetNpduControls.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeConfirmedServiceRequest(m_tx_buffer, offset, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, BacnetMaxSegments.MAX_SEG0, m_client.MaxAdpuLength, invoke_id, 0, 0);
            tmp = Services.EncodeAtomicReadFile(m_tx_buffer, offset, true, object_id, position, count);
            if (tmp < 0) throw new Exception("Couldn't encode AtomicReadFile");
            offset += tmp;

            if (SendRequestAndTakeLock(adr, invoke_id, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK, m_tx_buffer, offset - org_offset))
            {
                try
                {
                    BacnetPduTypes type;
                    BacnetConfirmedServices service;
                    byte sequence_number;
                    byte proposed_window_number;
                    int len = APDU.DecodeComplexAck(m_apdu_response, 0, out type, out service, out invoke_id, out sequence_number, out proposed_window_number);
                    if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE)
                    {
                        bool is_stream;
                        if (Services.DecodeAtomicReadFileAcknowledge(m_apdu_response, len, m_apdu_length - len, out end_of_file, out is_stream, out position, out count, file_buffer, file_buffer_offset) < 0)
                            throw new System.IO.IOException("Couldn't decode AtomicReadFileAcknowledge");
                        return true;
                    }
                    else
                        throw new System.IO.IOException("BACnet error: " + service);
                }
                finally
                {
                    System.Threading.Monitor.Exit(m_response_lock);
                }
            }
            else
            {
                return false;
            }
        }

        public bool SubscribeCOVRequest(BacnetAddress adr, BacnetObjectId object_id, uint subscribe_id, bool cancel, bool issue_confirmed_notifications, uint lifetime, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending SubscribeCOVRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);

            int offset = 0;
            offset += m_client.HeaderLength;
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BacnetNpduControls.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeConfirmedServiceRequest(m_tx_buffer, offset, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV, BacnetMaxSegments.MAX_SEG0, m_client.MaxAdpuLength, invoke_id, 0, 0);
            offset += Services.EncodeSubscribeCOV(m_tx_buffer, offset, subscribe_id, object_id, cancel, issue_confirmed_notifications, lifetime);

            if (SendRequestAndTakeLock(adr, invoke_id, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK, m_tx_buffer, offset - org_offset))
            {
                try
                {
                    BacnetPduTypes type;
                    BacnetConfirmedServices service;
                    int len = APDU.DecodeSimpleAck(m_apdu_response, 0, out type, out service, out invoke_id);
                    if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV)
                    {
                        return true;
                    }
                    else
                        throw new System.IO.IOException("BACnet error: " + service);
                }
                finally
                {
                    System.Threading.Monitor.Exit(m_response_lock);
                }
            }
            else
            {
                return false;
            }
        }

        public bool SubscribePropertyRequest(BacnetAddress adr, BacnetObjectId object_id, BacnetPropertyReference monitored_property, uint subscribe_id, bool cancel, bool issue_confirmed_notifications, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending SubscribePropertyRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);

            int offset = 0;
            offset += m_client.HeaderLength;
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BacnetNpduControls.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeConfirmedServiceRequest(m_tx_buffer, offset, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY, BacnetMaxSegments.MAX_SEG0, m_client.MaxAdpuLength, invoke_id, 0, 0);
            offset += Services.EncodeSubscribeProperty(m_tx_buffer, offset, subscribe_id, object_id, cancel, issue_confirmed_notifications, 0, monitored_property, false, 0f);

            if (SendRequestAndTakeLock(adr, invoke_id, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK, m_tx_buffer, offset - org_offset))
            {
                try
                {
                    BacnetPduTypes type;
                    BacnetConfirmedServices service;
                    int len = APDU.DecodeSimpleAck(m_apdu_response, 0, out type, out service, out invoke_id);
                    if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY)
                    {
                        return true;
                    }
                    else
                        throw new System.IO.IOException("BACnet error: " + service);
                }
                finally
                {
                    System.Threading.Monitor.Exit(m_response_lock);
                }
            }
            else
            {
                return false;
            }
        }

        public bool ReadPropertyRequest(BacnetAddress adr, BacnetObjectId object_id, BacnetPropertyIds property_id, out IList<BacnetValue> value_list, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending ReadPropertyRequest ... ", null);
            if(invoke_id == 0) invoke_id = unchecked ( m_invoke_id++);

            int offset = 0;
            offset += m_client.HeaderLength;
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BacnetNpduControls.PriorityNormalMessage | BacnetNpduControls.ExpectingReply, null, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeConfirmedServiceRequest(m_tx_buffer, offset, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, BacnetMaxSegments.MAX_SEG0, m_client.MaxAdpuLength, invoke_id, 0, 0);
            offset += Services.EncodeReadProperty(m_tx_buffer, offset, object_id, (uint)property_id);

            if (SendRequestAndTakeLock(adr, invoke_id, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK, m_tx_buffer, offset - org_offset))
            {
                try
                {
                    BacnetPduTypes type;
                    BacnetConfirmedServices service;
                    byte sequence_number;
                    byte proposed_window_number;
                    int len = APDU.DecodeComplexAck(m_apdu_response, 0, out type, out service, out invoke_id, out sequence_number, out proposed_window_number);
                    if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY)
                    {
                        BacnetObjectId response_object_id;
                        BacnetPropertyReference response_property;
                        if (Services.DecodeReadPropertyAcknowledge(m_apdu_response, len, m_apdu_length - len, out response_object_id, out response_property, out value_list) < 0)
                            throw new System.IO.IOException("Couldn't decode ReadPropertyAcknowledge");
                        return true;
                    }
                    else
                        throw new System.IO.IOException("BACnet error: " + service);
                }
                finally
                {
                    System.Threading.Monitor.Exit(m_response_lock);
                }
            }
            else
            {
                value_list = null;
                return false;
            }
        }

        public bool WritePropertyRequest(BacnetAddress adr, BacnetObjectId object_id, BacnetPropertyIds property_id, IEnumerable<BacnetValue> value_list, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending WritePropertyRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);

            int offset = 0;
            offset += m_client.HeaderLength;
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BacnetNpduControls.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeConfirmedServiceRequest(m_tx_buffer, offset, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, BacnetMaxSegments.MAX_SEG0, m_client.MaxAdpuLength, invoke_id, 0, 0);
            offset += Services.EncodeWriteProperty(m_tx_buffer, offset, object_id, (uint)property_id, ASN1.BACNET_ARRAY_ALL, 0, value_list);

            if (SendRequestAndTakeLock(adr, invoke_id, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK, m_tx_buffer, offset - org_offset))
            {
                try
                {
                    BacnetPduTypes type;
                    BacnetConfirmedServices service;
                    int len = APDU.DecodeSimpleAck(m_apdu_response, 0, out type, out service, out invoke_id);
                    if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY)
                    {
                        return true;
                    }
                    else
                        throw new System.IO.IOException("BACnet error: " + service);
                }
                finally
                {
                    System.Threading.Monitor.Exit(m_response_lock);
                }
            }
            else
            {
                value_list = null;
                return false;
            }
        }

        public bool ReadPropertyMultipleRequest(BacnetAddress adr, BacnetObjectId object_id, IEnumerable<BacnetPropertyReference> property_id_and_array_index, out ICollection<BacnetPropertyValue> value_list, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending ReadPropertyMultipleRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);

            int offset = 0;
            offset += m_client.HeaderLength;
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BacnetNpduControls.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeConfirmedServiceRequest(m_tx_buffer, offset, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, BacnetMaxSegments.MAX_SEG0, m_client.MaxAdpuLength, invoke_id, 0, 0);
            offset += Services.EncodeReadPropertyMultiple(m_tx_buffer, offset, object_id, property_id_and_array_index);

            if (SendRequestAndTakeLock(adr, invoke_id, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK, m_tx_buffer, offset - org_offset))
            {
                try
                {
                    BacnetPduTypes type;
                    BacnetConfirmedServices service;
                    byte sequence_number;
                    byte proposed_window_number;
                    int len = APDU.DecodeComplexAck(m_apdu_response, 0, out type, out service, out invoke_id, out sequence_number, out proposed_window_number);
                    if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE)
                    {
                        BacnetObjectId response_object_id;
                        if (Services.DecodeReadPropertyMultipleAcknowledge(m_apdu_response, len, m_apdu_length - len, out response_object_id, out value_list) < 0)
                            throw new System.IO.IOException("Couldn't decode ReadPropertyMultipleAcknowledge");
                        return true;
                    }
                    else
                        throw new System.IO.IOException("BACnet error: " + service);
                }
                finally
                {
                    System.Threading.Monitor.Exit(m_response_lock);
                }
            }
            else
            {
                value_list = null;
                return false;
            }
        }

        public bool Notify(BacnetAddress adr, uint subscriberProcessIdentifier, uint initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, bool issueConfirmedNotifications, IList<BacnetPropertyValue> values)
        {
            Trace.WriteLine("Sending Notify ... ", null);
            int offset = 0;
            offset += m_client.HeaderLength;
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BacnetNpduControls.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            if (issueConfirmedNotifications)
            {
                byte invoke_id = unchecked(m_invoke_id++);
                offset += APDU.EncodeConfirmedServiceRequest(m_tx_buffer, offset, BacnetPduTypes.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION, BacnetMaxSegments.MAX_SEG0, m_client.MaxAdpuLength, invoke_id, 0, 0);
                offset += Services.EncodeCOVNotifyConfirmed(m_tx_buffer, offset, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, values);

                if (SendRequestAndTakeLock(adr, invoke_id, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK, m_tx_buffer, offset - org_offset))
                {
                    try
                    {
                        BacnetPduTypes type;
                        BacnetConfirmedServices service;
                        APDU.DecodeSimpleAck(m_apdu_response, 0, out type, out service, out invoke_id);
                        if (service == BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE)
                            return true;
                        else
                            return false;
                        }
                    finally
                    {
                        System.Threading.Monitor.Exit(m_response_lock);
                    }
                }
                else
                    return false;
            }
            else
            {
                offset += APDU.EncodeUnconfirmedServiceRequest(m_tx_buffer, offset, BacnetPduTypes.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BacnetUnconfirmedServices.SERVICE_UNCONFIRMED_COV_NOTIFICATION);
                offset += Services.EncodeCOVNotifyUnconfirmed(m_tx_buffer, offset, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, values);
                m_client.Send(m_tx_buffer, m_client.HeaderLength, offset - org_offset, adr, false, 0);
                return true;
            }
        }

        public void ReadPropertyResponse(BacnetAddress adr, byte invoke_id, BacnetObjectId object_id, BacnetPropertyReference property, IEnumerable<BacnetValue> value)
        {
            Trace.WriteLine("Sending ReadPropertyResponse ... ", null);
            int offset = 0;
            offset += m_client.HeaderLength;
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BacnetNpduControls.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeComplexAck(m_tx_buffer, offset, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, invoke_id, 0, 0);
            offset += Services.EncodeReadPropertyAcknowledge(m_tx_buffer, offset, object_id, property.propertyIdentifier, property.propertyArrayIndex, value);
            m_client.Send(m_tx_buffer, m_client.HeaderLength, offset - org_offset, adr, false, 0);
        }

        public void ReadPropertyMultipleResponse(BacnetAddress adr, byte invoke_id, IList<BacnetObjectId> object_ids, IList<ICollection<BacnetPropertyValue>> values)
        {
            Trace.WriteLine("Sending ReadPropertyMultipleResponse ... ", null);
            int offset = 0;
            offset += m_client.HeaderLength;
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BacnetNpduControls.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeComplexAck(m_tx_buffer, offset, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, invoke_id, 0, 0);
            offset += Services.EncodeReadPropertyMultipleAcknowledge(m_tx_buffer, offset, object_ids, values);
            m_client.Send(m_tx_buffer, m_client.HeaderLength, offset - org_offset, adr, false, 0);
        }

        public void ReadFileResponse(BacnetAddress adr, byte invoke_id, int position, uint count, bool end_of_file, byte[] file_buffer)
        {
            Trace.WriteLine("Sending AtomicReadFileResponse ... ", null);
            int offset = 0;
            offset += m_client.HeaderLength;
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BacnetNpduControls.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeComplexAck(m_tx_buffer, offset, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_READ_FILE, invoke_id, 0, 0);
            offset += Services.EncodeAtomicReadFileAcknowledge(m_tx_buffer, offset, true, end_of_file, position, 1, new byte[][] { file_buffer }, new int[] { (int)count });
            m_client.Send(m_tx_buffer, m_client.HeaderLength, offset - org_offset, adr, false, 0);
        }

        public void WriteFileResponse(BacnetAddress adr, byte invoke_id, int position)
        {
            Trace.WriteLine("Sending AtomicWriteFileResponse ... ", null);
            int offset = 0;
            offset += m_client.HeaderLength;
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BacnetNpduControls.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeComplexAck(m_tx_buffer, offset, BacnetPduTypes.PDU_TYPE_COMPLEX_ACK, BacnetConfirmedServices.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, invoke_id, 0, 0);
            offset += Services.EncodeAtomicWriteFileAcknowledge(m_tx_buffer, offset, true, position);
            m_client.Send(m_tx_buffer, m_client.HeaderLength, offset - org_offset, adr, false, 0);
        }

        public void ErrorResponse(BacnetAddress adr, BacnetConfirmedServices service, byte invoke_id, BacnetErrorClasses error_class, BacnetErrorCodes error_code)
        {
            Trace.WriteLine("Sending ErrorResponse ... ", null);
            int offset = 0;
            offset += m_client.HeaderLength;
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BacnetNpduControls.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeError(m_tx_buffer, offset, BacnetPduTypes.PDU_TYPE_ERROR, service, invoke_id);
            offset += Services.EncodeError(m_tx_buffer, offset, error_class, error_code);
            m_client.Send(m_tx_buffer, m_client.HeaderLength, offset - org_offset, adr, false, 0);
        }

        public void SimpleAckResponse(BacnetAddress adr, BacnetConfirmedServices service, byte invoke_id)
        {
            Trace.WriteLine("Sending SimpleAckResponse ... ", null);
            int offset = 0;
            offset += m_client.HeaderLength;
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BacnetNpduControls.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BacnetNetworkMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeSimpleAck(m_tx_buffer, offset, BacnetPduTypes.PDU_TYPE_SIMPLE_ACK, service, invoke_id);
            offset += Services.EncodeSimpleAck(m_tx_buffer, 0);
            m_client.Send(m_tx_buffer, m_client.HeaderLength, offset - org_offset, adr, false, 0);
        }

        public void Dispose()
        {
            m_client.Dispose();
            m_client = null;
        }
    }
}
