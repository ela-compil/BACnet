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
        public int Timeout { get { return m_timeout; } }
        public int Retries { get { return m_retries; } }

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
            m_tx_buffer = new byte[m_client.GetMaxBufferLength()];
            m_apdu_response = new byte[m_client.GetMaxBufferLength()];
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

        public delegate void ConfirmedServiceRequestHandler(BacnetClient sender, BACNET_ADDRESS adr, BACNET_PDU_TYPE type, BACNET_CONFIRMED_SERVICE service, BACNET_MAX_SEGMENTS max_segments, BACNET_MAX_ADPU max_adpu, byte invoke_id, byte sequence_number, byte proposed_window_number, byte[] buffer, int offset, int length);
        public event ConfirmedServiceRequestHandler OnConfirmedServiceRequest;
        public delegate void ReadPropertyRequestHandler(BacnetClient sender, BACNET_ADDRESS adr, byte invoke_id, BACNET_OBJECT_ID object_id, BACNET_PROPERTY_REFERENCE property);
        public event ReadPropertyRequestHandler OnReadPropertyRequest;
        public delegate void ReadPropertyMultipleRequestHandler(BacnetClient sender, BACNET_ADDRESS adr, byte invoke_id, IList<BACNET_OBJECT_ID> object_ids, IList<IList<BACNET_PROPERTY_REFERENCE>> property_id_and_array_index);
        public event ReadPropertyMultipleRequestHandler OnReadPropertyMultipleRequest;
        public delegate void WritePropertyRequestHandler(BacnetClient sender, BACNET_ADDRESS adr, byte invoke_id, BACNET_OBJECT_ID object_id, BACNET_PROPERTY_VALUE value);
        public event WritePropertyRequestHandler OnWritePropertyRequest;
        public delegate void WritePropertyMultipleRequestHandler(BacnetClient sender, BACNET_ADDRESS adr, byte invoke_id, BACNET_OBJECT_ID object_id, ICollection<BACNET_PROPERTY_VALUE> values);
        public event WritePropertyMultipleRequestHandler OnWritePropertyMultipleRequest;

        protected void ProcessConfirmedServiceRequest(BACNET_ADDRESS adr, BACNET_PDU_TYPE type, BACNET_CONFIRMED_SERVICE service, BACNET_MAX_SEGMENTS max_segments, BACNET_MAX_ADPU max_adpu, byte invoke_id, byte sequence_number, byte proposed_window_number, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("ConfirmedServiceRequest", null);
                if (OnConfirmedServiceRequest != null) OnConfirmedServiceRequest(this, adr, type, service, max_segments, max_adpu, invoke_id, sequence_number, proposed_window_number, buffer, offset, length);
                if (service == BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_READ_PROPERTY && OnReadPropertyRequest != null)
                {
                    BACNET_OBJECT_ID object_id;
                    BACNET_PROPERTY_REFERENCE property;
                    if (SERVICES.DecodeReadProperty(buffer, offset, length, out object_id, out property) >= 0)
                        OnReadPropertyRequest(this, adr, invoke_id, object_id, property);
                    else
                        Trace.TraceWarning("Couldn't decode DecodeReadProperty");
                }
                else if (service == BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_WRITE_PROPERTY && OnWritePropertyRequest != null)
                {
                    BACNET_OBJECT_ID object_id;
                    BACNET_PROPERTY_VALUE value;
                    if (SERVICES.DecodeWriteProperty(buffer, offset, length, out object_id, out value) >= 0)
                        OnWritePropertyRequest(this, adr, invoke_id, object_id, value);
                    else
                        Trace.TraceWarning("Couldn't decode DecodeWriteProperty");
                }
                else if (service == BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_READ_PROP_MULTIPLE && OnReadPropertyMultipleRequest != null)
                {
                    IList<BACNET_OBJECT_ID> object_ids;
                    IList<IList<BACNET_PROPERTY_REFERENCE>> property_id_and_array_index;
                    if (SERVICES.DecodeReadPropertyMultiple(buffer, offset, length, out object_ids, out property_id_and_array_index) >= 0)
                        OnReadPropertyMultipleRequest(this, adr, invoke_id, object_ids, property_id_and_array_index);
                    else
                        Trace.TraceWarning("Couldn't decode DecodeReadPropertyMultiple");
                }
                else if (service == BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_WRITE_PROP_MULTIPLE && OnWritePropertyMultipleRequest != null)
                {
                    BACNET_OBJECT_ID object_id;
                    ICollection<BACNET_PROPERTY_VALUE> values;
                    if (SERVICES.DecodeWritePropertyMultiple(buffer, offset, length, out object_id, out values) >= 0)
                        OnWritePropertyMultipleRequest(this, adr, invoke_id, object_id, values);
                    else
                        Trace.TraceWarning("Couldn't decode DecodeWritePropertyMultiple");
                }
                else if (service == BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_COV_NOTIFICATION && OnCOVNotification != null)
                {
                    uint subscriberProcessIdentifier;
                    BACNET_OBJECT_ID initiatingDeviceIdentifier;
                    BACNET_OBJECT_ID monitoredObjectIdentifier;
                    uint timeRemaining;
                    ICollection<BACNET_PROPERTY_VALUE> values;
                    if (SERVICES.DecodeCOVNotifyUnconfirmed(buffer, offset, length, out subscriberProcessIdentifier, out initiatingDeviceIdentifier, out monitoredObjectIdentifier, out timeRemaining, out values) >= 0)
                        OnCOVNotification(this, adr, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, invoke_id, true, values);
                    else
                        Trace.TraceWarning("Couldn't decode COVNotify");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in ProcessConfirmedServiceRequest: " + ex.Message);
            }
        }

        public delegate void UnconfirmedServiceRequestHandler(BacnetClient sender, BACNET_ADDRESS adr, BACNET_PDU_TYPE type, BACNET_UNCONFIRMED_SERVICE service, byte[] buffer, int offset, int length);
        public event UnconfirmedServiceRequestHandler OnUnconfirmedServiceRequest;
        public delegate void IamHandler(BacnetClient sender, BACNET_ADDRESS adr, UInt32 device_id, UInt32 max_apdu, BACNET_SEGMENTATION segmentation, UInt16 vendor_id);
        public event IamHandler OnIam;
        public delegate void WhoIsHandler(BacnetClient sender, BACNET_ADDRESS adr, int low_limit, int high_limit);
        public event WhoIsHandler OnWhoIs;

        //used by both 'confirmed' and 'unconfirmed' notify
        public delegate void COVNotificationHandler(BacnetClient sender, BACNET_ADDRESS adr, uint subscriberProcessIdentifier, BACNET_OBJECT_ID initiatingDeviceIdentifier, BACNET_OBJECT_ID monitoredObjectIdentifier, uint timeRemaining, byte invoke_id, bool need_confirm, ICollection<BACNET_PROPERTY_VALUE> values);
        public event COVNotificationHandler OnCOVNotification;

        protected void ProcessUnconfirmedServiceRequest(BACNET_ADDRESS adr, BACNET_PDU_TYPE type, BACNET_UNCONFIRMED_SERVICE service, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("UnconfirmedServiceRequest", null);
                if (OnUnconfirmedServiceRequest != null) OnUnconfirmedServiceRequest(this, adr, type, service, buffer, offset, length);
                if (service == BACNET_UNCONFIRMED_SERVICE.SERVICE_UNCONFIRMED_I_AM && OnIam != null)
                {
                    uint device_id;
                    uint max_adpu;
                    BACNET_SEGMENTATION segmentation;
                    ushort vendor_id;
                    if (SERVICES.DecodeIamBroadcast(buffer, offset, out device_id, out max_adpu, out segmentation, out vendor_id) >= 0)
                        OnIam(this, adr, device_id, max_adpu, segmentation, vendor_id);
                    else
                        Trace.TraceWarning("Couldn't decode IamBroadcast");
                }
                else if (service == BACNET_UNCONFIRMED_SERVICE.SERVICE_UNCONFIRMED_WHO_IS && OnWhoIs != null)
                {
                    int low_limit;
                    int high_limit;
                    if (SERVICES.DecodeWhoIsBroadcast(buffer, offset, length, out low_limit, out high_limit) >= 0)
                        OnWhoIs(this, adr, low_limit, high_limit);
                    else
                        Trace.TraceWarning("Couldn't decode WhoIsBroadcast");
                }
                else if (service == BACNET_UNCONFIRMED_SERVICE.SERVICE_UNCONFIRMED_COV_NOTIFICATION && OnCOVNotification != null)
                {
                    uint subscriberProcessIdentifier;
                    BACNET_OBJECT_ID initiatingDeviceIdentifier;
                    BACNET_OBJECT_ID monitoredObjectIdentifier;
                    uint timeRemaining;
                    ICollection<BACNET_PROPERTY_VALUE> values;
                    if (SERVICES.DecodeCOVNotifyUnconfirmed(buffer, offset, length, out subscriberProcessIdentifier, out initiatingDeviceIdentifier, out monitoredObjectIdentifier, out timeRemaining, out values) >= 0)
                        OnCOVNotification(this, adr, subscriberProcessIdentifier, initiatingDeviceIdentifier, monitoredObjectIdentifier, timeRemaining, 0, false, values);
                    else
                        Trace.TraceWarning("Couldn't decode COVNotifyUnconfirmed");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in ProcessUnconfirmedServiceRequest: " + ex.Message);
            }
        }

        public delegate void SimpleAckHandler(BacnetClient sender, BACNET_ADDRESS adr, BACNET_PDU_TYPE type, BACNET_CONFIRMED_SERVICE service, byte invoke_id, byte[] data, int data_offset, int data_length);
        public event SimpleAckHandler OnSimpleAck;

        protected void ProcessSimpleAck(BACNET_ADDRESS adr, BACNET_PDU_TYPE type, BACNET_CONFIRMED_SERVICE service, byte invoke_id, byte[] buffer, int offset, int length)
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

        public delegate void ComplexAckHandler(BacnetClient sender, BACNET_ADDRESS adr, BACNET_PDU_TYPE type, BACNET_CONFIRMED_SERVICE service, byte invoke_id, byte sequence_number, byte proposed_window_number, byte[] buffer, int offset, int length);
        public event ComplexAckHandler OnComplexAck;

        protected void ProcessComplexAck(BACNET_ADDRESS adr, BACNET_PDU_TYPE type, BACNET_CONFIRMED_SERVICE service, byte invoke_id, byte sequence_number, byte proposed_window_number, byte[] buffer, int offset, int length)
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

        public delegate void ErrorHandler(BacnetClient sender, BACNET_ADDRESS adr, BACNET_PDU_TYPE type, BACNET_CONFIRMED_SERVICE service, byte invoke_id, uint error_class, uint error_code, byte[] buffer, int offset, int length);
        public event ErrorHandler OnError;

        protected void ProcessError(BACNET_ADDRESS adr, BACNET_PDU_TYPE type, BACNET_CONFIRMED_SERVICE service, byte invoke_id, byte[] buffer, int offset, int length)
        {
            try
            {
                Trace.WriteLine("Error", null);

                uint error_class;
                uint error_code;
                if (SERVICES.DecodeError(buffer, offset, length, out error_class, out error_code) < 0)
                    Trace.TraceWarning("Couldn't decode Error");

                if (OnError != null) OnError(this, adr, type, service, invoke_id, error_class, error_code, buffer, offset, length);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in ProcessError: " + ex.Message);
            }
        }

        public delegate void AbortHandler(BacnetClient sender, BACNET_ADDRESS adr, BACNET_PDU_TYPE type, byte invoke_id, byte reason, byte[] buffer, int offset, int length);
        public event AbortHandler OnAbort;

        protected void ProcessAbort(BACNET_ADDRESS adr, BACNET_PDU_TYPE type, byte invoke_id, byte reason, byte[] buffer, int offset, int length)
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

        public delegate void SegmentAckHandler(BacnetClient sender, BACNET_ADDRESS adr, BACNET_PDU_TYPE type, byte[] buffer, int offset, int length);
        public event SegmentAckHandler OnSegmentAck;

        protected void ProcessSegmentAck(BACNET_ADDRESS adr, BACNET_PDU_TYPE type, byte[] buffer, int offset, int length)
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

        private void OnRecieve(IBacnetTransport sender, byte[] buffer, int offset, int msg_length, BACNET_ADDRESS remote_address)
        {
            try
            {
                //finish recieve
                if (m_client == null) return;   //we're disposed 

                //parse
                if (msg_length > 0)
                {
                    BACNET_NPDU_CONTROL npdu_function;
                    BACNET_ADDRESS destination, source;
                    byte hop_count;
                    BACNET_NETWORK_MESSAGE_TYPE nmt;
                    ushort vendor_id;
                    int npdu_len = NPDU.Decode(buffer, offset, out npdu_function, out destination, out source, out hop_count, out nmt, out vendor_id);
                    if (npdu_len >= 0)
                    {
                        offset += npdu_len;
                        msg_length -= npdu_len;
                        BACNET_PDU_TYPE apdu_type;
                        apdu_type = APDU.GetDecodedType(buffer, offset);

                        //store response
                        lock (m_response_lock)
                        {
                            m_apdu_length = msg_length;
                            Array.Copy(buffer, offset, m_apdu_response, 0, m_apdu_length);
                        }

                        switch (apdu_type & BACNET_PDU_TYPE.PDU_TYPE_MASK)
                        {
                            case BACNET_PDU_TYPE.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST:
                                {
                                    BACNET_UNCONFIRMED_SERVICE service;
                                    int apdu_header_len = APDU.DecodeUnconfirmedServiceRequest(buffer, offset, out apdu_type, out service);
                                    offset += apdu_header_len;
                                    msg_length -= apdu_header_len;
                                    ProcessUnconfirmedServiceRequest(remote_address, apdu_type, service, buffer, offset, msg_length);
                                }
                                break;
                            case BACNET_PDU_TYPE.PDU_TYPE_SIMPLE_ACK:
                                {
                                    BACNET_CONFIRMED_SERVICE service;
                                    byte invoke_id;
                                    int apdu_header_len = APDU.DecodeSimpleAck(buffer, offset, out apdu_type, out service, out invoke_id);
                                    offset += apdu_header_len;
                                    msg_length -= apdu_header_len;
                                    ProcessSimpleAck(remote_address, apdu_type, service, invoke_id, buffer, offset, msg_length);
                                    m_response_event.Set();
                                }
                                break;
                            case BACNET_PDU_TYPE.PDU_TYPE_COMPLEX_ACK:
                                {
                                    BACNET_CONFIRMED_SERVICE service;
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
                            case BACNET_PDU_TYPE.PDU_TYPE_SEGMENT_ACK:
                                {
                                    int apdu_header_len = APDU.DecodeSegmentAck(buffer, offset, out apdu_type);
                                    offset += apdu_header_len;
                                    msg_length -= apdu_header_len;
                                    ProcessSegmentAck(remote_address, apdu_type, buffer, offset, msg_length);
                                }
                                break;
                            case BACNET_PDU_TYPE.PDU_TYPE_ERROR:
                                {
                                    BACNET_CONFIRMED_SERVICE service;
                                    byte invoke_id;
                                    int apdu_header_len = APDU.DecodeError(buffer, offset, out apdu_type, out service, out invoke_id);
                                    offset += apdu_header_len;
                                    msg_length -= apdu_header_len;
                                    ProcessError(remote_address, apdu_type, service, invoke_id, buffer, offset, msg_length);
                                    m_response_event.Set();
                                }
                                break;
                            case BACNET_PDU_TYPE.PDU_TYPE_REJECT:
                            case BACNET_PDU_TYPE.PDU_TYPE_ABORT:
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
                            case BACNET_PDU_TYPE.PDU_TYPE_CONFIRMED_SERVICE_REQUEST:
                                {
                                    BACNET_CONFIRMED_SERVICE service;
                                    BACNET_MAX_SEGMENTS max_segments;
                                    BACNET_MAX_ADPU max_adpu;
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
            offset += m_client.GetHeaderLength();
            int org_offset = offset;
            BACNET_ADDRESS broadcast = m_client.GetBroadcastAddress();
            offset += NPDU.Encode(m_tx_buffer, offset, BACNET_NPDU_CONTROL.PriorityNormalMessage, broadcast, null, DEFAULT_HOP_COUNT, BACNET_NETWORK_MESSAGE_TYPE.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeUnconfirmedServiceRequest(m_tx_buffer, offset, BACNET_PDU_TYPE.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BACNET_UNCONFIRMED_SERVICE.SERVICE_UNCONFIRMED_WHO_IS);
            offset += SERVICES.EncodeWhoIsBroadcast(m_tx_buffer, offset, -1, -1);

            m_client.Send(m_tx_buffer, m_client.GetHeaderLength(), offset - org_offset, broadcast, false);
        }

        public void Iam(uint device_id)
        {
            Trace.WriteLine("Sending Iam ... ", null);

            int offset = 0;
            offset += m_client.GetHeaderLength();
            int org_offset = offset;
            BACNET_ADDRESS broadcast = m_client.GetBroadcastAddress();
            offset += NPDU.Encode(m_tx_buffer, offset, BACNET_NPDU_CONTROL.PriorityNormalMessage, broadcast, null, DEFAULT_HOP_COUNT, BACNET_NETWORK_MESSAGE_TYPE.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeUnconfirmedServiceRequest(m_tx_buffer, offset, BACNET_PDU_TYPE.PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST, BACNET_UNCONFIRMED_SERVICE.SERVICE_UNCONFIRMED_I_AM);
            offset += SERVICES.EncodeIamBroadcast(m_tx_buffer, offset, device_id, (uint)m_client.GetMaxBufferLength(), BACNET_SEGMENTATION.SEGMENTATION_NONE, m_vendor_id);

            m_client.Send(m_tx_buffer, m_client.GetHeaderLength(), offset - org_offset, broadcast, false);
        }

        private bool SendRequestAndTakeLock(BACNET_ADDRESS adr, byte expected_invoke_id, BACNET_PDU_TYPE expected_type, byte[] data, int data_length)
        {
            bool success = false;
            for (int r = 0; r < m_retries; r++)
            {
                m_response_event.Reset();
                m_client.Send(data, m_client.GetHeaderLength(), data_length, adr, true);
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
                                BACNET_PDU_TYPE type = APDU.GetDecodedType(m_apdu_response, 0);
                                if ((type & BACNET_PDU_TYPE.PDU_TYPE_MASK) == expected_type)
                                {
                                    success = true;    //keep lock
                                    return success;
                                }
                                else
                                    throw new System.IO.IOException("BACnet error: " + (type & BACNET_PDU_TYPE.PDU_TYPE_MASK));
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

        public bool WriteFileRequest(BACNET_ADDRESS adr, BACNET_OBJECT_ID object_id, ref int position, int count, byte[] file_buffer, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending AtomicWriteFileRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);

            int offset = 0;
            offset += m_client.GetHeaderLength();
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BACNET_NPDU_CONTROL.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BACNET_NETWORK_MESSAGE_TYPE.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeConfirmedServiceRequest(m_tx_buffer, offset, BACNET_PDU_TYPE.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE, BACNET_MAX_SEGMENTS.MAX_SEG0, BACNET_MAX_ADPU.MAX_APDU1476, invoke_id, 0, 0);
            offset += SERVICES.EncodeAtomicWriteFile(m_tx_buffer, offset, true, object_id, position, 1, new byte[][] { file_buffer }, new int[] { count });

            if (SendRequestAndTakeLock(adr, invoke_id, BACNET_PDU_TYPE.PDU_TYPE_COMPLEX_ACK, m_tx_buffer, offset - org_offset))
            {
                BACNET_PDU_TYPE type;
                BACNET_CONFIRMED_SERVICE service;
                byte sequence_number;
                byte proposed_window_number;
                int len = APDU.DecodeComplexAck(m_apdu_response, 0, out type, out service, out invoke_id, out sequence_number, out proposed_window_number);
                if (service == BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_ATOMIC_WRITE_FILE)
                {
                    bool is_stream;
                    if (SERVICES.DecodeAtomicWriteFileAcknowledge(m_apdu_response, len, m_apdu_length - len, out is_stream, out position) < 0)
                        throw new System.IO.IOException("Couldn't decode AtomicWriteFileAcknowledge");
                    return true;
                }
                else
                    throw new System.IO.IOException("BACnet error: " + service);
            }
            else
            {
                return false;
            }
        }

        public bool ReadFileRequest(BACNET_ADDRESS adr, BACNET_OBJECT_ID object_id, ref int position, ref uint count, out bool end_of_file, byte[] file_buffer, int file_buffer_offset, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending AtomicReadFileRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);
            end_of_file = true;

            int offset = 0;
            offset += m_client.GetHeaderLength();
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BACNET_NPDU_CONTROL.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BACNET_NETWORK_MESSAGE_TYPE.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeConfirmedServiceRequest(m_tx_buffer, offset, BACNET_PDU_TYPE.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_ATOMIC_READ_FILE, BACNET_MAX_SEGMENTS.MAX_SEG0, BACNET_MAX_ADPU.MAX_APDU1476, invoke_id, 0, 0);
            offset += SERVICES.EncodeAtomicReadFile(m_tx_buffer, offset, true, object_id, position, count);

            if (SendRequestAndTakeLock(adr, invoke_id, BACNET_PDU_TYPE.PDU_TYPE_COMPLEX_ACK, m_tx_buffer, offset - org_offset))
            {
                BACNET_PDU_TYPE type;
                BACNET_CONFIRMED_SERVICE service;
                byte sequence_number;
                byte proposed_window_number;
                int len = APDU.DecodeComplexAck(m_apdu_response, 0, out type, out service, out invoke_id, out sequence_number, out proposed_window_number);
                if (service == BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_ATOMIC_READ_FILE)
                {
                    bool is_stream;
                    if (SERVICES.DecodeAtomicReadFileAcknowledge(m_apdu_response, len, m_apdu_length - len, out end_of_file, out is_stream, out position, out count, file_buffer, file_buffer_offset) < 0)
                        throw new System.IO.IOException("Couldn't decode AtomicReadFileAcknowledge");
                    return true;
                }
                else
                    throw new System.IO.IOException("BACnet error: " + service);
            }
            else
            {
                return false;
            }
        }

        public bool SubscribeCOVRequest(BACNET_ADDRESS adr, BACNET_OBJECT_ID object_id, uint subscribe_id, bool cancel, bool issue_confirmed_notifications, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending SubscribeCOVRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);

            int offset = 0;
            offset += m_client.GetHeaderLength();
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BACNET_NPDU_CONTROL.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BACNET_NETWORK_MESSAGE_TYPE.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeConfirmedServiceRequest(m_tx_buffer, offset, BACNET_PDU_TYPE.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_SUBSCRIBE_COV, BACNET_MAX_SEGMENTS.MAX_SEG0, BACNET_MAX_ADPU.MAX_APDU1476, invoke_id, 0, 0);
            offset += SERVICES.EncodeSubscribeCOV(m_tx_buffer, offset, subscribe_id, object_id, cancel, issue_confirmed_notifications, 0);

            if (SendRequestAndTakeLock(adr, invoke_id, BACNET_PDU_TYPE.PDU_TYPE_SIMPLE_ACK, m_tx_buffer, offset - org_offset))
            {
                try
                {
                    BACNET_PDU_TYPE type;
                    BACNET_CONFIRMED_SERVICE service;
                    int len = APDU.DecodeSimpleAck(m_apdu_response, 0, out type, out service, out invoke_id);
                    if (service == BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_SUBSCRIBE_COV)
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

        public bool SubscribePropertyRequest(BACNET_ADDRESS adr, BACNET_OBJECT_ID object_id, BACNET_PROPERTY_REFERENCE monitored_property, uint subscribe_id, bool cancel, bool issue_confirmed_notifications, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending SubscribePropertyRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);

            int offset = 0;
            offset += m_client.GetHeaderLength();
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BACNET_NPDU_CONTROL.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BACNET_NETWORK_MESSAGE_TYPE.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeConfirmedServiceRequest(m_tx_buffer, offset, BACNET_PDU_TYPE.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY, BACNET_MAX_SEGMENTS.MAX_SEG0, BACNET_MAX_ADPU.MAX_APDU1476, invoke_id, 0, 0);
            offset += SERVICES.EncodeSubscribeProperty(m_tx_buffer, offset, subscribe_id, object_id, cancel, issue_confirmed_notifications, 0, monitored_property, false, 0f);

            if (SendRequestAndTakeLock(adr, invoke_id, BACNET_PDU_TYPE.PDU_TYPE_SIMPLE_ACK, m_tx_buffer, offset - org_offset))
            {
                try
                {
                    BACNET_PDU_TYPE type;
                    BACNET_CONFIRMED_SERVICE service;
                    int len = APDU.DecodeSimpleAck(m_apdu_response, 0, out type, out service, out invoke_id);
                    if (service == BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_SUBSCRIBE_COV_PROPERTY)
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

        public bool ReadPropertyRequest(BACNET_ADDRESS adr, BACNET_OBJECT_ID object_id, BACNET_PROPERTY_ID property_id, out LinkedList<BACNET_VALUE> value_list, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending ReadPropertyRequest ... ", null);
            if(invoke_id == 0) invoke_id = unchecked ( m_invoke_id++);

            int offset = 0;
            offset += m_client.GetHeaderLength();
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BACNET_NPDU_CONTROL.PriorityNormalMessage | BACNET_NPDU_CONTROL.ExpectingReply, null, null, DEFAULT_HOP_COUNT, BACNET_NETWORK_MESSAGE_TYPE.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeConfirmedServiceRequest(m_tx_buffer, offset, BACNET_PDU_TYPE.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_READ_PROPERTY, BACNET_MAX_SEGMENTS.MAX_SEG0, BACNET_MAX_ADPU.MAX_APDU1476, invoke_id, 0, 0);
            offset += SERVICES.EncodeReadProperty(m_tx_buffer, offset, object_id, (uint)property_id);

            if (SendRequestAndTakeLock(adr, invoke_id, BACNET_PDU_TYPE.PDU_TYPE_COMPLEX_ACK, m_tx_buffer, offset - org_offset))
            {
                try
                {
                    BACNET_PDU_TYPE type;
                    BACNET_CONFIRMED_SERVICE service;
                    byte sequence_number;
                    byte proposed_window_number;
                    int len = APDU.DecodeComplexAck(m_apdu_response, 0, out type, out service, out invoke_id, out sequence_number, out proposed_window_number);
                    if (service == BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_READ_PROPERTY)
                    {
                        BACNET_OBJECT_ID response_object_id;
                        BACNET_PROPERTY_REFERENCE response_property;
                        if (SERVICES.DecodeReadPropertyAcknowledge(m_apdu_response, len, m_apdu_length - len, out response_object_id, out response_property, out value_list) < 0)
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

        public bool WritePropertyRequest(BACNET_ADDRESS adr, BACNET_OBJECT_ID object_id, BACNET_PROPERTY_ID property_id, IEnumerable<BACNET_VALUE> value_list, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending WritePropertyRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);

            int offset = 0;
            offset += m_client.GetHeaderLength();
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BACNET_NPDU_CONTROL.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BACNET_NETWORK_MESSAGE_TYPE.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeConfirmedServiceRequest(m_tx_buffer, offset, BACNET_PDU_TYPE.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_WRITE_PROPERTY, BACNET_MAX_SEGMENTS.MAX_SEG0, BACNET_MAX_ADPU.MAX_APDU1476, invoke_id, 0, 0);
            offset += SERVICES.EncodeWriteProperty(m_tx_buffer, offset, object_id, (uint)property_id, ASN1.BACNET_ARRAY_ALL, 0, value_list);

            if (SendRequestAndTakeLock(adr, invoke_id, BACNET_PDU_TYPE.PDU_TYPE_SIMPLE_ACK, m_tx_buffer, offset - org_offset))
            {
                try
                {
                    BACNET_PDU_TYPE type;
                    BACNET_CONFIRMED_SERVICE service;
                    int len = APDU.DecodeSimpleAck(m_apdu_response, 0, out type, out service, out invoke_id);
                    if (service == BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_WRITE_PROPERTY)
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

        public bool ReadPropertyMultipleRequest(BACNET_ADDRESS adr, BACNET_OBJECT_ID object_id, IEnumerable<BACNET_PROPERTY_REFERENCE> property_id_and_array_index, out ICollection<BACNET_PROPERTY_VALUE> value_list, byte invoke_id = 0)
        {
            Trace.WriteLine("Sending ReadPropertyMultipleRequest ... ", null);
            if (invoke_id == 0) invoke_id = unchecked(m_invoke_id++);

            int offset = 0;
            offset += m_client.GetHeaderLength();
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BACNET_NPDU_CONTROL.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BACNET_NETWORK_MESSAGE_TYPE.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeConfirmedServiceRequest(m_tx_buffer, offset, BACNET_PDU_TYPE.PDU_TYPE_CONFIRMED_SERVICE_REQUEST, BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, BACNET_MAX_SEGMENTS.MAX_SEG0, BACNET_MAX_ADPU.MAX_APDU1476, invoke_id, 0, 0);
            offset += SERVICES.EncodeReadPropertyMultiple(m_tx_buffer, offset, object_id, property_id_and_array_index);

            if (SendRequestAndTakeLock(adr, invoke_id, BACNET_PDU_TYPE.PDU_TYPE_COMPLEX_ACK, m_tx_buffer, offset - org_offset))
            {
                try
                {
                    BACNET_PDU_TYPE type;
                    BACNET_CONFIRMED_SERVICE service;
                    byte sequence_number;
                    byte proposed_window_number;
                    int len = APDU.DecodeComplexAck(m_apdu_response, 0, out type, out service, out invoke_id, out sequence_number, out proposed_window_number);
                    if (service == BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_READ_PROP_MULTIPLE)
                    {
                        BACNET_OBJECT_ID response_object_id;;
                        if (SERVICES.DecodeReadPropertyMultipleAcknowledge(m_apdu_response, len, m_apdu_length - len, out response_object_id, out value_list) < 0)
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

        public void ReadPropertyResponse(BACNET_ADDRESS adr, byte invoke_id, BACNET_OBJECT_ID object_id, BACNET_PROPERTY_REFERENCE property, IEnumerable<BACNET_VALUE> value)
        {
            Trace.WriteLine("Sending ReadPropertyResponse ... ", null);
            int offset = 0;
            offset += m_client.GetHeaderLength();
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BACNET_NPDU_CONTROL.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BACNET_NETWORK_MESSAGE_TYPE.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeComplexAck(m_tx_buffer, offset, BACNET_PDU_TYPE.PDU_TYPE_COMPLEX_ACK, BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_READ_PROPERTY, invoke_id, 0, 0);
            offset += SERVICES.EncodeReadPropertyAcknowledge(m_tx_buffer, offset, object_id, property.propertyIdentifier, property.propertyArrayIndex, value);
            m_client.Send(m_tx_buffer, m_client.GetHeaderLength(), offset - org_offset, adr, false);
        }

        public void ReadPropertyMultipleResponse(BACNET_ADDRESS adr, byte invoke_id, IList<BACNET_OBJECT_ID> object_ids, IList<ICollection<BACNET_PROPERTY_VALUE>> values)
        {
            Trace.WriteLine("Sending ReadPropertyMultipleResponse ... ", null);
            int offset = 0;
            offset += m_client.GetHeaderLength();
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BACNET_NPDU_CONTROL.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BACNET_NETWORK_MESSAGE_TYPE.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeComplexAck(m_tx_buffer, offset, BACNET_PDU_TYPE.PDU_TYPE_COMPLEX_ACK, BACNET_CONFIRMED_SERVICE.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, invoke_id, 0, 0);
            offset += SERVICES.EncodeReadPropertyMultipleAcknowledge(m_tx_buffer, offset, object_ids, values);
            m_client.Send(m_tx_buffer, m_client.GetHeaderLength(), offset - org_offset, adr, false);
        }

        public void ErrorResponse(BACNET_ADDRESS adr, BACNET_CONFIRMED_SERVICE service, byte invoke_id, uint error_class, uint error_code)
        {
            Trace.WriteLine("Sending ErrorResponse ... ", null);
            int offset = 0;
            offset += m_client.GetHeaderLength();
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BACNET_NPDU_CONTROL.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BACNET_NETWORK_MESSAGE_TYPE.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeError(m_tx_buffer, offset, BACNET_PDU_TYPE.PDU_TYPE_ERROR, service, invoke_id);
            offset += SERVICES.EncodeError(m_tx_buffer, offset, error_class, error_code);
            m_client.Send(m_tx_buffer, m_client.GetHeaderLength(), offset - org_offset, adr, false);
        }

        public void SimpleAckResponse(BACNET_ADDRESS adr, BACNET_CONFIRMED_SERVICE service, byte invoke_id)
        {
            Trace.WriteLine("Sending SimpleAckResponse ... ", null);
            int offset = 0;
            offset += m_client.GetHeaderLength();
            int org_offset = offset;
            offset += NPDU.Encode(m_tx_buffer, offset, BACNET_NPDU_CONTROL.PriorityNormalMessage, null, null, DEFAULT_HOP_COUNT, BACNET_NETWORK_MESSAGE_TYPE.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK, 0);
            offset += APDU.EncodeSimpleAck(m_tx_buffer, offset, BACNET_PDU_TYPE.PDU_TYPE_SIMPLE_ACK, service, invoke_id);
            offset += SERVICES.EncodeSimpleAck(m_tx_buffer, 0);
            m_client.Send(m_tx_buffer, m_client.GetHeaderLength(), offset - org_offset, adr, false);
        }

        public void Dispose()
        {
            m_client.Dispose();
            m_client = null;
        }
    }
}
